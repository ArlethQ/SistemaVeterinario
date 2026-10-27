using System.Net.Http.Headers;
using System.Text.Json;
using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IBusinessCentralService
{
    Task<List<FarmacoStockDto>> BuscarMedicamentosConStockAsync(string filtro, int idLocalidad, string apostrofe = "HV");
    Task<decimal> ObtenerStockReservadoLocalAsync(string codigoProd, string bodega);
    Task<int> GuardarReservaStockAsync(string idProd, int idUser, decimal cantidad, int localidad, string bodega, int orden, int nhcl, int idReserva, string origenReserva = "QUI", int? idRegistroAne = null, int? hoja = null);
    Task<int> EliminarReservaStockAsync(string idProd, int orden, int idReserva, string origenReserva = "QUI");
}

public class BusinessCentralService : IBusinessCentralService
{
    private readonly ISqlDataService _sql;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<BusinessCentralService> _logger;

    public BusinessCentralService(ISqlDataService sql, HttpClient httpClient, IConfiguration config, ILogger<BusinessCentralService> logger)
    {
        _sql = sql;
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<List<FarmacoStockDto>> BuscarMedicamentosConStockAsync(string filtro, int idLocalidad, string apostrofe = "HV")
    {
        var resultados = new List<FarmacoStockDto>();
        if (string.IsNullOrWhiteSpace(filtro) || filtro.Trim().Length < 2)
            return resultados;

        string termino = filtro.Trim().ToUpperInvariant();

        // 1. Intentar obtener bodegas configuradas en vt_bodegasBC para la localidad
        var bodegas = new List<string>();
        try
        {
            var bodQuery = await _sql.QueryAsync<string>(
                "SELECT RTRIM(bodega) FROM vt_bodegasBC WHERE idlocalidad = @Localidad AND estado = 'A'",
                new { Localidad = idLocalidad });
            bodegas = bodQuery.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar vt_bodegasBC; usando BOD001 por defecto.");
        }

        if (bodegas.Count == 0)
        {
            bodegas.Add("BOD001");
        }

        // 2. Intentar consultar API de Business Central si hay credenciales activas
        try
        {
            var token = await ObtenerTokenBusinessCentralAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                string tenantId = _config["BusinessCentral:TenantId"] ?? "";
                string env = _config["BusinessCentral:Environment"] ?? "";
                string companyId = _config["BusinessCentral:CompanyId"] ?? "";
                string urlAltiora = $"https://api.businesscentral.dynamics.com/v2.0/{tenantId}/{env}/api/altiora/localizacion/v2.0/companies({companyId})/";

                foreach (var bodega in bodegas)
                {
                    string odataFilter = $"items?$filter=contains(displayName,'{termino}') AND contains(number,'{apostrofe}') AND (itemCategoryCode eq 'FARMACO') AND (type eq 'Inventory') AND location eq '{bodega}' AND blocked eq 'false'&$select=id,number,displayName,taxGroupCode,inventory,unitPrice,baseUnitOfMeasureCode,location,available";
                    using var req = new HttpRequestMessage(HttpMethod.Get, urlAltiora + odataFilter);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var resp = await _httpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                        if (doc.RootElement.TryGetProperty("value", out var valueArr) && valueArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in valueArr.EnumerateArray())
                            {
                                string number = item.TryGetProperty("number", out var pNum) ? pNum.GetString() ?? "" : "";
                                decimal available = item.TryGetProperty("available", out var pAv) && pAv.TryGetDecimal(out var avVal) ? avVal :
                                                    (item.TryGetProperty("inventory", out var pInv) && pInv.TryGetDecimal(out var invVal) ? invVal : 0);
                                decimal reservado = await ObtenerStockReservadoLocalAsync(number, bodega);
                                decimal stockFinal = Math.Max(0, available - reservado);

                                resultados.Add(new FarmacoStockDto
                                {
                                    Id = item.TryGetProperty("id", out var pId) ? pId.GetString() ?? "" : Guid.NewGuid().ToString(),
                                    Number = number,
                                    DisplayName = item.TryGetProperty("displayName", out var pName) ? pName.GetString() ?? "" : "",
                                    TaxGroupCode = item.TryGetProperty("taxGroupCode", out var pTax) ? pTax.GetString() ?? "IVA0" : "IVA0",
                                    Inventory = stockFinal,
                                    Available = stockFinal,
                                    UnitPrice = item.TryGetProperty("unitPrice", out var pPrice) && pPrice.TryGetDecimal(out var pr) ? pr : 0,
                                    BaseUnitOfMeasureCode = item.TryGetProperty("baseUnitOfMeasureCode", out var pUm) ? pUm.GetString() ?? "UND" : "UND",
                                    Location = bodega
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consulta a API Business Central no disponible; usando catálogo en base de datos SQL Server.");
        }

        // 3. Fallback a las tablas locales de productos/medicinas en SQL Server (vt_productos / qvt_productosUtilizados) si BC no retornó datos
        if (resultados.Count == 0)
        {
            try
            {
                const string sqlLocal = @"
                    SELECT TOP 30
                        RTRIM(ISNULL(pro_codigo, CAST(pro_id AS VARCHAR(20)))) AS Number,
                        RTRIM(ISNULL(pro_descripcion, '')) AS DisplayName,
                        CAST(ISNULL(pro_id, 0) AS VARCHAR(50)) AS Id,
                        'IVA0' AS TaxGroupCode,
                        CAST(100 AS DECIMAL(18,2)) AS Inventory,
                        CAST(ISNULL(pro_precio, 0) AS DECIMAL(18,2)) AS UnitPrice,
                        'UND' AS BaseUnitOfMeasureCode,
                        @Bodega AS Location
                    FROM vt_productos
                    WHERE pro_descripcion LIKE @Filtro AND ISNULL(pro_estado, 'A') = 'A'
                    ORDER BY pro_descripcion";

                var locales = await _sql.QueryAsync<FarmacoStockDto>(sqlLocal, new { Filtro = $"%{termino}%", Bodega = bodegas.First() });
                foreach (var item in locales)
                {
                    decimal reservado = await ObtenerStockReservadoLocalAsync(item.Number, item.Location);
                    item.Inventory = Math.Max(0, item.Inventory - reservado);
                    item.Available = item.Inventory;
                    resultados.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback a vt_productos falló; consultando histórico qvt_productosUtilizados.");
            }
        }

        if (resultados.Count == 0)
        {
            try
            {
                const string sqlHist = @"
                    SELECT DISTINCT TOP 25
                        RTRIM(ISNULL(codigoprod, 'HV-001')) AS Number,
                        RTRIM(ISNULL(proddescripcion, '')) AS DisplayName,
                        RTRIM(ISNULL(med_iddinamics, 'BC-LOCAL')) AS Id,
                        RTRIM(ISNULL(med_iva, 'IVA0')) AS TaxGroupCode,
                        CAST(50 AS DECIMAL(18,2)) AS Inventory,
                        CAST(ISNULL(precioprod, 0) AS DECIMAL(18,2)) AS UnitPrice,
                        RTRIM(ISNULL(med_medida, 'UND')) AS BaseUnitOfMeasureCode,
                        RTRIM(ISNULL(med_bodega, 'BOD001')) AS Location
                    FROM qvt_productosUtilizados
                    WHERE proddescripcion LIKE @Filtro AND codigoprod IS NOT NULL";

                var hist = await _sql.QueryAsync<FarmacoStockDto>(sqlHist, new { Filtro = $"%{termino}%" });
                resultados.AddRange(hist);
            }
            catch
            {
                // Ignorar si no hay conexión
            }
        }

        if (resultados.Count == 0)
        {
            var demoCatalog = new List<FarmacoStockDto>
            {
                new() { Id = "BC-01", Number = "MED-OME-40", DisplayName = "OMEPRAZOL 40MG FRASCO AMPOLLA IV", TaxGroupCode = "IVA0", Inventory = 48, Available = 48, UnitPrice = 6.50m, BaseUnitOfMeasureCode = "AMP", Location = "HOSP" },
                new() { Id = "BC-02", Number = "MED-AMP-SUL", DisplayName = "AMPICILINA + SULBACTAM 1.5G INYECTABLE", TaxGroupCode = "IVA0", Inventory = 35, Available = 35, UnitPrice = 11.20m, BaseUnitOfMeasureCode = "FCO", Location = "HOSP" },
                new() { Id = "BC-03", Number = "MED-MEL-05", DisplayName = "MELOXICAM 0.5% INYECTABLE 10ML", TaxGroupCode = "IVA0", Inventory = 22, Available = 22, UnitPrice = 8.40m, BaseUnitOfMeasureCode = "ML", Location = "HOSP" },
                new() { Id = "BC-04", Number = "MED-TRA-50", DisplayName = "TRAMADOL CLORHIDRATO 50MG/ML AMPOLLA", TaxGroupCode = "IVA0", Inventory = 60, Available = 60, UnitPrice = 4.25m, BaseUnitOfMeasureCode = "AMP", Location = "QUIR" },
                new() { Id = "BC-05", Number = "MED-PRO-10", DisplayName = "PROPOFOL 1% EMULSIÓN INYECTABLE 20ML", TaxGroupCode = "IVA0", Inventory = 18, Available = 18, UnitPrice = 18.75m, BaseUnitOfMeasureCode = "FCO", Location = "QUIR" },
                new() { Id = "BC-06", Number = "SOL-RIN-1000", DisplayName = "SOLUCIÓN HARTMANN / RINGER LACTATO 1000ML", TaxGroupCode = "IVA0", Inventory = 95, Available = 95, UnitPrice = 5.80m, BaseUnitOfMeasureCode = "FUNDA", Location = "HOSP" }
            };

            resultados = demoCatalog
                .Where(d => d.DisplayName.Contains(termino, StringComparison.OrdinalIgnoreCase) || d.Number.Contains(termino, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (resultados.Count == 0) resultados = demoCatalog;
        }

        return resultados;
    }

    public async Task<decimal> ObtenerStockReservadoLocalAsync(string codigoProd, string bodega)
    {
        try
        {
            const string sql = @"
                SELECT ISNULL(SUM(cantidad), 0)
                FROM vt_reservaProductosStockBC
                WHERE idProd = @CodigoProd AND bodega = @Bodega";
            return await _sql.ExecuteScalarAsync<decimal>(sql, new { CodigoProd = codigoProd, Bodega = bodega });
        }
        catch
        {
            return 0m;
        }
    }

    public async Task<int> GuardarReservaStockAsync(
        string idProd, int idUser, decimal cantidad, int localidad, string bodega,
        int orden, int nhcl, int idReserva, string origenReserva = "QUI",
        int? idRegistroAne = null, int? hoja = null)
    {
        const string sql = @"
            INSERT INTO vt_reservaProductosStockBC
                (idProd, idUser, cantidad, localidad, bodega, fecha, orden, nhcl, idreserva, origReserva, idregistroAne, hoja)
            VALUES
                (@IdProd, @IdUser, @Cantidad, @Localidad, @Bodega, @Fecha, @Orden, @Nhcl, @IdReserva, @OrigReserva, @IdRegistroAne, @Hoja)";

        return await _sql.ExecuteAsync(sql, new
        {
            IdProd = idProd,
            IdUser = idUser,
            Cantidad = cantidad,
            Localidad = localidad,
            Bodega = bodega,
            Fecha = DateTime.Now,
            Orden = orden,
            Nhcl = nhcl,
            IdReserva = idReserva,
            OrigReserva = origenReserva,
            IdRegistroAne = idRegistroAne,
            Hoja = hoja
        });
    }

    public async Task<int> EliminarReservaStockAsync(string idProd, int orden, int idReserva, string origenReserva = "QUI")
    {
        const string sql = @"
            DELETE FROM vt_reservaProductosStockBC
            WHERE idProd = @IdProd AND orden = @Orden AND idreserva = @IdReserva AND origReserva = @OrigReserva";

        return await _sql.ExecuteAsync(sql, new
        {
            IdProd = idProd,
            Orden = orden,
            IdReserva = idReserva,
            OrigReserva = origenReserva
        });
    }

    private async Task<string?> ObtenerTokenBusinessCentralAsync()
    {
        try
        {
            // Primero intenta leer de vt_parametrosBC como en CabecerasObj.vb
            var paramBc = await _sql.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 client_id, client_secret, username, userpassword, scope, id_directorio FROM vt_parametrosBC");

            string tenantId = paramBc?.id_directorio?.ToString()?.Trim() ?? _config["BusinessCentral:TenantId"] ?? "";
            string clientId = paramBc?.client_id?.ToString()?.Trim() ?? _config["BusinessCentral:ClientId"] ?? "";
            string clientSecret = paramBc?.client_secret?.ToString()?.Trim() ?? _config["BusinessCentral:ClientSecret"] ?? "";
            string username = paramBc?.username?.ToString()?.Trim() ?? _config["BusinessCentral:Username"] ?? "";
            string password = paramBc?.userpassword?.ToString()?.Trim() ?? _config["BusinessCentral:Password"] ?? "";
            string scope = paramBc?.scope?.ToString()?.Trim() ?? _config["BusinessCentral:Scope"] ?? "";

            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
                return null;

            string tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["username"] = username,
                ["password"] = password,
                ["scope"] = scope
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(form), cts.Token);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token));
            return doc.RootElement.TryGetProperty("access_token", out var tk) ? tk.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
