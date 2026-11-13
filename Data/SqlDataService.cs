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
    Task<int> ExecuteBatchAsync(IEnumerable<(string Sql, object? Parameters)> commands);
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
            ConnectTimeout = 15
        };
        _connectionString = builder.ConnectionString;
        _logger = logger;
    }

    public string GetConnectionString() => _connectionString;


    private static bool _schemaVerified = false;
    private static readonly object _schemaLock = new();

    private void EnsureSchemaCompatibility(SqlConnection connection)
    {
        if (_schemaVerified) return;
        lock (_schemaLock)
        {
            if (_schemaVerified) return;
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'qvt_productosUtilizados' 
                          AND COLUMN_NAME = 'codigoprod' 
                          AND DATA_TYPE = 'int'
                    )
                    BEGIN
                        ALTER TABLE qvt_productosUtilizados ALTER COLUMN codigoprod varchar(50) NULL;
                    END;
                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'qvt_productosUtilizados' 
                          AND COLUMN_NAME = 'cantprod' 
                          AND DATA_TYPE = 'int'
                    )
                    BEGIN
                        ALTER TABLE qvt_productosUtilizados ALTER COLUMN cantprod decimal(18,2) NULL;
                    END;

                    IF OBJECT_ID(N'dbo.vt_autorizaciones', N'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH(N'dbo.vt_autorizaciones', N'autorizacion_archivo_url') IS NULL
                            ALTER TABLE dbo.vt_autorizaciones
                                ADD autorizacion_archivo_url nvarchar(1000) NULL;

                        IF COL_LENGTH(N'dbo.vt_autorizaciones', N'autorizacion_archivo_nombre') IS NULL
                            ALTER TABLE dbo.vt_autorizaciones
                                ADD autorizacion_archivo_nombre nvarchar(260) NULL;

                        IF NOT EXISTS (
                            SELECT 1 FROM sys.indexes
                            WHERE object_id = OBJECT_ID(N'dbo.vt_autorizaciones')
                              AND name = N'IX_vt_autorizaciones_nhcl_fecha')
                            CREATE INDEX IX_vt_autorizaciones_nhcl_fecha
                                ON dbo.vt_autorizaciones (autorizacion_nhcl, autorizacion_fecha DESC);
                    END;

                    IF OBJECT_ID(N'dbo.vt_formatos', N'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH(N'dbo.vt_formatos', N'form_es_sistema') IS NULL
                            ALTER TABLE dbo.vt_formatos
                                ADD form_es_sistema bit NULL;

                        UPDATE dbo.vt_formatos
                        SET form_es_sistema = CASE WHEN form_id <= 8 THEN 1 ELSE 0 END
                        WHERE form_es_sistema IS NULL;
                    END;

                    IF OBJECT_ID(N'dbo.vt_ordenAtencion', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.vt_ordenAtencion', N'ordenAten_tipoConsulta') IS NULL
                    BEGIN
                        ALTER TABLE dbo.vt_ordenAtencion
                            ADD ordenAten_tipoConsulta INT NULL;
                    END;

                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'vt_fichamedica'
                          AND COLUMN_NAME = 'med_tipoconsulta'
                          AND CHARACTER_MAXIMUM_LENGTH IS NOT NULL
                          AND CHARACTER_MAXIMUM_LENGTH < 150
                    )
                    BEGIN
                        ALTER TABLE dbo.vt_fichamedica ALTER COLUMN med_tipoconsulta varchar(150) NULL;
                    END;

                    IF OBJECT_ID(N'dbo.vt_fichamedica', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.vt_fichamedica', N'med_requiereHospitalizacion') IS NULL
                    BEGIN
                        ALTER TABLE dbo.vt_fichamedica
                            ADD med_requiereHospitalizacion bit NOT NULL CONSTRAINT DF_vt_fichamedica_req_hosp DEFAULT (0);
                    END;";
                cmd.ExecuteNonQuery();
                _schemaVerified = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Aviso de compatibilidad de esquema qvt_productosUtilizados.");
                _schemaVerified = true;
            }
        }
    }
    private async Task<SqlConnection> OpenConnectionFastAsync()
    {
        if (DateTime.UtcNow < _circuitOpenUntil)
        {
            throw new InvalidOperationException("Circuit breaker activo: SQL Server remoto no alcanzable.");
        }

        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            EnsureSchemaCompatibility(connection);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            _circuitOpenUntil = DateTime.UtcNow.AddSeconds(5);
            throw;
        }
    }

    private async Task<T> ExecuteWithDeadlockRetryAsync<T>(Func<Task<T>> operation)
    {
        const int maxRetries = 3;
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (SqlException ex) when (ex.Number == 1205 && attempt < maxRetries)
            {
                int delayMs = (attempt + 1) * 75 + Random.Shared.Next(15, 60);
                _logger.LogWarning(ex, "Deadlock SQL (1205) detectado; reintentando operación ({Attempt}/{MaxRetries}) en {DelayMs} ms.", attempt + 1, maxRetries, delayMs);
                await Task.Delay(delayMs);
            }
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        return await ExecuteWithDeadlockRetryAsync(async () =>
        {
            await using var connection = await OpenConnectionFastAsync();
            return await connection.QueryAsync<T>(sql, parameters, commandTimeout: 15);
        });
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        return await ExecuteWithDeadlockRetryAsync(async () =>
        {
            await using var connection = await OpenConnectionFastAsync();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, commandTimeout: 15);
        });
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        return await ExecuteWithDeadlockRetryAsync(async () =>
        {
            await using var connection = await OpenConnectionFastAsync();
            return await connection.ExecuteScalarAsync<T>(sql, parameters, commandTimeout: 15);
        });
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        try
        {
            return await ExecuteWithDeadlockRetryAsync(async () =>
            {
                await using var connection = await OpenConnectionFastAsync();
                return await connection.ExecuteAsync(sql, parameters, commandTimeout: 15);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando comando en SQL Server.");
            throw;
        }
    }

    public async Task<int> ExecuteBatchAsync(IEnumerable<(string Sql, object? Parameters)> commands)
    {
        try
        {
            return await ExecuteWithDeadlockRetryAsync(async () =>
            {
                await using var connection = await OpenConnectionFastAsync();
                await using var transaction = await connection.BeginTransactionAsync();
                int total = 0;
                try
                {
                    foreach (var (sql, parameters) in commands)
                    {
                        total += await connection.ExecuteAsync(sql, parameters, transaction, commandTimeout: 15);
                    }
                    await transaction.CommitAsync();
                    return total;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando lote transaccional en SQL Server.");
            throw;
        }
    }

    public async Task<DataTable> GetDataTableAsync(string sql, params SqlParameter[] parameters)
    {
        return await ExecuteWithDeadlockRetryAsync(async () =>
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
        });
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
        string tipo = identificacion.Trim().Length == 13 ? "RUC" : "CEDULA";
        return EcuadorDocumentoValidador.Validar(tipo, identificacion).EsValido;
    }
}
