using Microsoft.Data.SqlClient;
using Simetric.Models;

namespace HosvetKardex.Web.Services;

public sealed class NumericaBackOfficeService(IConfiguration configuration)
{
    private string ConnectionString
    {
        get
        {
            var dedicada = configuration.GetConnectionString("NumericaBackOfficeConnection");
            if (!string.IsNullOrWhiteSpace(dedicada)) return dedicada;

            var veterinaria = configuration.GetConnectionString("VeterinariaConnection")
                ?? throw new InvalidOperationException("No está configurada la conexión de Veterinaria.");
            if (veterinaria.StartsWith("ConnectionString=", StringComparison.OrdinalIgnoreCase))
                veterinaria = veterinaria["ConnectionString=".Length..];

            var builder = new SqlConnectionStringBuilder(veterinaria)
            {
                InitialCatalog = configuration["NumericaBackOffice:Database"] ?? "db_aab800_numerica"
            };
            return builder.ConnectionString;
        }
    }

    public async Task RegistrarVentaAsync(ReporteVentaBackOffice venta, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await ValidarTablaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.REPORTEVENTABACKOFFICE
                (Cliente, Producto, PlanPaquete, Valor, Fecha, Canal, Vendedor,
                 Estado, FormaPago, Observacion, ComprobanteArchivo)
            VALUES
                (@Cliente, @Producto, @PlanPaquete, @Valor, @Fecha, @Canal, @Vendedor,
                 @Estado, @FormaPago, @Observacion, @ComprobanteArchivo);
            """;
        command.Parameters.AddWithValue("@Cliente", venta.Cliente);
        command.Parameters.AddWithValue("@Producto", venta.Producto);
        command.Parameters.AddWithValue("@PlanPaquete", venta.PlanPaquete);
        command.Parameters.AddWithValue("@Valor", venta.Valor);
        command.Parameters.AddWithValue("@Fecha", venta.Fecha);
        command.Parameters.AddWithValue("@Canal", venta.Canal);
        command.Parameters.AddWithValue("@Vendedor", (object?)venta.Vendedor ?? DBNull.Value);
        command.Parameters.AddWithValue("@Estado", venta.Estado);
        command.Parameters.AddWithValue("@FormaPago", venta.FormaPago);
        command.Parameters.AddWithValue("@Observacion", (object?)venta.Observacion ?? DBNull.Value);
        command.Parameters.AddWithValue("@ComprobanteArchivo", (object?)venta.ComprobanteArchivo ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<string>> ObtenerObservacionesTransferenciasPendientesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await ValidarTablaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Observacion
            FROM dbo.REPORTEVENTABACKOFFICE
            WHERE Estado = N'pendiente'
              AND Canal LIKE N'%Transferencia Web%'
              AND Observacion IS NOT NULL;
            """;

        var observaciones = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) observaciones.Add(reader.GetString(0));
        return observaciones;
    }

    private static async Task ValidarTablaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN OBJECT_ID(N'dbo.REPORTEVENTABACKOFFICE', N'U') IS NULL THEN 0 ELSE 1 END";
        var existe = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        if (!existe)
            throw new InvalidOperationException("La tabla dbo.REPORTEVENTABACKOFFICE no existe en la base de Backoffice de Numerica.");
    }
}
