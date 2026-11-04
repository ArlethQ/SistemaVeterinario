using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IFacturacionIntegradaService
{
    Task<EstadoFacturacionIntegradaDto> ObtenerEstadoAsync();
    Task<ResultadoSincronizacionClienteDto> SincronizarClienteAsync(int paCodigo, int idUsuario);
    Task<int> SincronizarPropietariosCoincidentesAsync(string filtro, int idUsuario, int limite = 20);
}

public sealed class FacturacionIntegradaService(
    ISqlDataService db,
    ILogger<FacturacionIntegradaService> logger) : IFacturacionIntegradaService
{
    public async Task<EstadoFacturacionIntegradaDto> ObtenerEstadoAsync()
    {
        const string sql = """
            SELECT
                CONVERT(bit, CASE WHEN OBJECT_ID('dbo.CLIENTES', 'U') IS NOT NULL
                                       AND OBJECT_ID('dbo.FACTURA', 'U') IS NOT NULL
                                       AND OBJECT_ID('dbo.DETALLEFACTURA', 'U') IS NOT NULL
                                       AND OBJECT_ID('dbo.EMISOR', 'U') IS NOT NULL
                                       AND OBJECT_ID('dbo.CAJA', 'U') IS NOT NULL
                                       AND OBJECT_ID('dbo.ClienteVeterinariaMap', 'U') IS NOT NULL
                                  THEN 1 ELSE 0 END) AS EsquemaDisponible,
                CONVERT(bit, CASE WHEN OBJECT_ID('dbo.EMISOR', 'U') IS NOT NULL
                                       AND EXISTS (SELECT 1 FROM dbo.EMISOR WHERE ISNULL(ESTADO, 1) = 1)
                                  THEN 1 ELSE 0 END) AS EmisorConfigurado,
                CONVERT(bit, CASE WHEN OBJECT_ID('dbo.CAJA', 'U') IS NOT NULL
                                       AND EXISTS (SELECT 1 FROM dbo.CAJA WHERE ISNULL(Estado, 1) = 1)
                                  THEN 1 ELSE 0 END) AS CajaConfigurada,
                (SELECT COUNT(*) FROM dbo.vt_usuarios WHERE ISNULL(user_estado, 'A') = 'A') AS UsuariosVeterinarios,
                CASE WHEN OBJECT_ID('dbo.Usuarios', 'U') IS NOT NULL
                     THEN (SELECT COUNT(*) FROM dbo.Usuarios WHERE ISNULL(Estado, 1) = 1)
                     ELSE 0 END AS UsuariosFacturacion,
                CASE WHEN OBJECT_ID('dbo.ClienteVeterinariaMap', 'U') IS NOT NULL
                     THEN (SELECT COUNT(*) FROM dbo.ClienteVeterinariaMap)
                     ELSE 0 END AS ClientesSincronizados;
            """;

        try
        {
            var estado = await db.QueryFirstOrDefaultAsync<EstadoFacturacionIntegradaDto>(sql)
                         ?? new EstadoFacturacionIntegradaDto();
            estado.Mensaje = estado.EsquemaDisponible
                ? estado.ListaParaConfigurarFacturas
                    ? "La base integrada está disponible."
                    : "La estructura existe, pero falta configurar emisor o caja."
                : "Ejecute primero el script de integración de Facturación/EContax.";
            return estado;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo comprobar el estado de la facturación integrada.");
            return new EstadoFacturacionIntegradaDto
            {
                Mensaje = "La facturación integrada todavía no está disponible en esta base."
            };
        }
    }

    public async Task<ResultadoSincronizacionClienteDto> SincronizarClienteAsync(int paCodigo, int idUsuario)
    {
        if (paCodigo <= 0 || idUsuario <= 0)
            return new ResultadoSincronizacionClienteDto { Mensaje = "Propietario o usuario inválido." };

        const string sql = """
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF OBJECT_ID('dbo.CLIENTES', 'U') IS NULL
               OR OBJECT_ID('dbo.ClienteVeterinariaMap', 'U') IS NULL
                THROW 51020, 'La estructura de facturación integrada no está instalada.', 1;

            BEGIN TRANSACTION;

            DECLARE
                @CodCliente INT,
                @Identificacion VARCHAR(25),
                @TipoIdentificacion VARCHAR(10);

            SELECT @Identificacion = NULLIF(LTRIM(RTRIM(pa_cedula)), '')
            FROM dbo.vt_clientes WITH (UPDLOCK, HOLDLOCK)
            WHERE pa_codigo = @PaCodigo;

            IF @@ROWCOUNT = 0
                THROW 51021, 'No se encontró el propietario veterinario.', 1;

            SET @TipoIdentificacion = CASE
                WHEN LEN(@Identificacion) = 13 THEN '04'
                WHEN LEN(@Identificacion) = 10 THEN '05'
                ELSE '06'
            END;

            SELECT @CodCliente = m.CodClienteFacturacion
            FROM dbo.ClienteVeterinariaMap m WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN dbo.CLIENTES c ON c.CODCLIENTE=m.CodClienteFacturacion
            WHERE m.PaCodigoVeterinaria = @PaCodigo
              AND c.USUARIO = @IdUsuario;

            IF @CodCliente IS NULL AND @Identificacion IS NOT NULL
            BEGIN
                SELECT @CodCliente = MIN(CODCLIENTE)
                FROM dbo.CLIENTES WITH (UPDLOCK, HOLDLOCK)
                WHERE LTRIM(RTRIM(NUMEROIDENTIFICACION)) = @Identificacion
                  AND USUARIO = @IdUsuario
                HAVING COUNT(*) = 1;
            END;

            IF @CodCliente IS NULL
            BEGIN
                INSERT INTO dbo.CLIENTES
                (
                    APELLIDOS, NOMBRES, NOMBRERAZONSOCIAL,
                    TIPOIDENTIFICACION, NUMEROIDENTIFICACION,
                    DIRECCION, TELEFONOCONVENCIONAL, CELULAR, CORREO,
                    TIPO_CLIENTE, USUARIO, ESTADO, FECHAINGRESO,
                    OBSERVACIONES, PERSONANATURAL
                )
                SELECT
                    NULLIF(LTRIM(RTRIM(pa_apellidos)), ''),
                    NULLIF(LTRIM(RTRIM(pa_nombres)), ''),
                    LTRIM(RTRIM(CONCAT(ISNULL(pa_nombres, ''), ' ', ISNULL(pa_apellidos, '')))),
                    @TipoIdentificacion,
                    @Identificacion,
                    NULLIF(LTRIM(RTRIM(pa_direccion)), ''),
                    NULLIF(LTRIM(RTRIM(pa_telefono)), ''),
                    NULLIF(LTRIM(RTRIM(pa_movil)), ''),
                    NULLIF(LTRIM(RTRIM(pa_email)), ''),
                    1,
                    @IdUsuario,
                    CASE WHEN ISNULL(pa_estado, 'A') = 'A' THEN 1 ELSE 0 END,
                    CONVERT(date, COALESCE(pa_fechaingreso, GETDATE())),
                    NULLIF(LTRIM(RTRIM(pa_observaciones)), ''),
                    1
                FROM dbo.vt_clientes
                WHERE pa_codigo = @PaCodigo;

                SET @CodCliente = CONVERT(INT, SCOPE_IDENTITY());
            END
            ELSE
            BEGIN
                UPDATE c
                SET c.APELLIDOS = NULLIF(LTRIM(RTRIM(v.pa_apellidos)), ''),
                    c.NOMBRES = NULLIF(LTRIM(RTRIM(v.pa_nombres)), ''),
                    c.NOMBRERAZONSOCIAL = LTRIM(RTRIM(CONCAT(ISNULL(v.pa_nombres, ''), ' ', ISNULL(v.pa_apellidos, '')))),
                    c.TIPOIDENTIFICACION = @TipoIdentificacion,
                    c.NUMEROIDENTIFICACION = @Identificacion,
                    c.DIRECCION = NULLIF(LTRIM(RTRIM(v.pa_direccion)), ''),
                    c.TELEFONOCONVENCIONAL = NULLIF(LTRIM(RTRIM(v.pa_telefono)), ''),
                    c.CELULAR = NULLIF(LTRIM(RTRIM(v.pa_movil)), ''),
                    c.CORREO = NULLIF(LTRIM(RTRIM(v.pa_email)), ''),
                    c.USUARIO = @IdUsuario,
                    c.ESTADO = CASE WHEN ISNULL(v.pa_estado, 'A') = 'A' THEN 1 ELSE 0 END
                FROM dbo.CLIENTES c
                CROSS JOIN dbo.vt_clientes v
                WHERE c.CODCLIENTE = @CodCliente
                  AND v.pa_codigo = @PaCodigo;
            END;

            UPDATE dbo.ClienteVeterinariaMap
            SET CodClienteFacturacion = @CodCliente,
                FechaSincronizacion = SYSUTCDATETIME()
            WHERE PaCodigoVeterinaria = @PaCodigo;

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO dbo.ClienteVeterinariaMap
                    (PaCodigoVeterinaria, CodClienteFacturacion)
                VALUES
                    (@PaCodigo, @CodCliente);
            END;

            COMMIT TRANSACTION;

            SELECT
                CONVERT(bit, 1) AS Exito,
                @CodCliente AS CodClienteFacturacion,
                CAST('Propietario preparado para facturación.' AS NVARCHAR(200)) AS Mensaje;
            """;

        try
        {
            return await db.QueryFirstOrDefaultAsync<ResultadoSincronizacionClienteDto>(
                       sql, new { PaCodigo = paCodigo, IdUsuario = idUsuario })
                   ?? new ResultadoSincronizacionClienteDto { Mensaje = "No se confirmó la sincronización." };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo sincronizar el propietario {PaCodigo} con facturación.", paCodigo);
            return new ResultadoSincronizacionClienteDto
            {
                Mensaje = ex.Message
            };
        }
    }

    public async Task<int> SincronizarPropietariosCoincidentesAsync(
        string filtro,
        int idUsuario,
        int limite = 20)
    {
        filtro = (filtro ?? string.Empty).Trim();
        if (idUsuario <= 0 || filtro.Length < 2) return 0;

        var ids = (await db.QueryAsync<int>("""
            SELECT TOP (@Limite) pa_codigo
            FROM dbo.vt_clientes
            WHERE ISNULL(pa_estado,'A')='A'
              AND (LTRIM(RTRIM(ISNULL(pa_cedula,''))) LIKE '%' + @Filtro + '%'
                   OR LTRIM(RTRIM(CONCAT(ISNULL(pa_nombres,''),' ',ISNULL(pa_apellidos,'')))) LIKE '%' + @Filtro + '%'
                   OR LTRIM(RTRIM(CONCAT(ISNULL(pa_apellidos,''),' ',ISNULL(pa_nombres,'')))) LIKE '%' + @Filtro + '%')
            ORDER BY pa_apellidos,pa_nombres,pa_codigo
            """, new { Filtro = filtro, Limite = Math.Clamp(limite, 1, 50) })).ToList();

        var sincronizados = 0;
        foreach (var id in ids)
        {
            if ((await SincronizarClienteAsync(id, idUsuario)).Exito)
                sincronizados++;
        }
        return sincronizados;
    }
}
