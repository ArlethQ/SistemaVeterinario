using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;

namespace HosvetKardex.Web.Data;

public interface ISqlDataService
{
    string GetConnectionString();
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null);
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);
    Task<int> ExecuteAsync(string sql, object? parameters = null);
    Task<DataTable> GetDataTableAsync(string sql, params SqlParameter[] parameters);
    Task<int> RegistrarBitacoraAsync(string detalle, int idUser, string ip = "127.0.0.1", string hostname = "BlazorClient");
    string HashPasswordSha1(string plainText);
    (int Anios, int Meses) CalcularEdad(DateTime? fechaNacimiento);
    bool ValidarCedulaRuc(string identificacion);
}

public class SqlDataService : ISqlDataService
{
    private readonly string _connectionString;
    private readonly ILogger<SqlDataService> _logger;
    private static DateTime _circuitOpenUntil = DateTime.MinValue;

    public SqlDataService(IConfiguration configuration, ILogger<SqlDataService> logger)
    {
        var rawConn = configuration.GetConnectionString("VeterinariaConnection")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'VeterinariaConnection'.");

        var builder = new SqlConnectionStringBuilder(rawConn)
        {
            ConnectTimeout = 2
        };
        _connectionString = builder.ConnectionString;
        _logger = logger;
    }

    public string GetConnectionString() => _connectionString;

    private async Task<SqlConnection> OpenConnectionFastAsync()
    {
        if (DateTime.UtcNow < _circuitOpenUntil)
        {
            throw new InvalidOperationException("Circuit breaker activo: SQL Server remoto no alcanzable (Modo Demo en memoria activo).");
        }

        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            _circuitOpenUntil = DateTime.UtcNow.AddSeconds(60);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = await OpenConnectionFastAsync();
        return await connection.QueryAsync<T>(sql, parameters, commandTimeout: 15);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = await OpenConnectionFastAsync();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, commandTimeout: 15);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = await OpenConnectionFastAsync();
        return await connection.ExecuteScalarAsync<T>(sql, parameters, commandTimeout: 15);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        try
        {
            await using var connection = await OpenConnectionFastAsync();
            return await connection.ExecuteAsync(sql, parameters, commandTimeout: 15);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Modo Demo activo para comando SQL.");
            return -1;
        }
    }

    public async Task<DataTable> GetDataTableAsync(string sql, params SqlParameter[] parameters)
    {
        var dt = new DataTable();
        await using var connection = await OpenConnectionFastAsync();
        await using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 15 };
        if (parameters is { Length: > 0 })
        {
            cmd.Parameters.AddRange(parameters);
        }
        await using var reader = await cmd.ExecuteReaderAsync();
        dt.Load(reader);
        return dt;
    }

    public async Task<int> RegistrarBitacoraAsync(string detalle, int idUser, string ip = "127.0.0.1", string hostname = "BlazorClient")
    {
        const string sql = @"
            INSERT INTO vt_vitacora (vit_detalle, vit_idUser, vit_idFechaMod, ip, hostname)
            VALUES (@Detalle, @IdUser, @FechaMod, @Ip, @Hostname)";

        return await ExecuteAsync(sql, new
        {
            Detalle = detalle,
            IdUser = idUser,
            FechaMod = DateTime.Now,
            Ip = ip,
            Hostname = hostname
        });
    }

    public string HashPasswordSha1(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(plainText.Trim());
        var hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public (int Anios, int Meses) CalcularEdad(DateTime? fechaNacimiento)
    {
        if (!fechaNacimiento.HasValue) return (0, 0);
        var nac = fechaNacimiento.Value;
        var hoy = DateTime.Today;
        int anios = hoy.Year - nac.Year;
        int meses = hoy.Month - nac.Month;
        if (hoy.Day < nac.Day)
        {
            meses--;
        }
        if (meses < 0)
        {
            anios--;
            meses += 12;
        }
        return (Math.Max(0, anios), Math.Max(0, meses));
    }

    public bool ValidarCedulaRuc(string identificacion)
    {
        if (string.IsNullOrWhiteSpace(identificacion)) return false;
        identificacion = identificacion.Trim();
        if (identificacion.Length != 10 && identificacion.Length != 13) return false;
        if (!identificacion.All(char.IsDigit)) return false;

        int provincia = int.Parse(identificacion[..2]);
        if (provincia < 1 || provincia > 24) return false;

        if (identificacion.Length == 10)
        {
            int[] coef = [2, 1, 2, 1, 2, 1, 2, 1, 2];
            int suma = 0;
            for (int i = 0; i < 9; i++)
            {
                int val = (identificacion[i] - '0') * coef[i];
                if (val > 9) val -= 9;
                suma += val;
            }
            int digitoVerificador = (suma % 10 == 0) ? 0 : 10 - (suma % 10);
            return digitoVerificador == (identificacion[9] - '0');
        }
        return true;
    }
}
