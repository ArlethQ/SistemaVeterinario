using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IInventarioService
{
    Task<List<FarmacoStockDto>> BuscarMedicamentosConStockAsync(string filtro, int idLocalidad, string apostrofe = "HV");
    Task<decimal> ObtenerStockReservadoLocalAsync(string codigoProd, string bodega);
    Task<int> GuardarReservaStockAsync(string idProd, int idUser, decimal cantidad, int localidad, string bodega, int orden, int nhcl, int idReserva, string origenReserva = "QUI", int? idRegistroAne = null, int? hoja = null);
    Task<int> EliminarReservaStockAsync(string idProd, int orden, int idReserva, string origenReserva = "QUI");
}

public class InventarioService : IInventarioService
{
    private readonly ISqlDataService _sql;
    private readonly ILogger<InventarioService> _logger;
    private readonly IEFactCatalogService _eFactCatalog;
    private static readonly SemaphoreSlim _catalogoLock = new(1, 1);
    private static List<CatalogoOperativoDto>? _catalogoEFactCache;
    private static DateTime _catalogoEFactExpira = DateTime.MinValue;

    public InventarioService(ISqlDataService sql, ILogger<InventarioService> logger, IEFactCatalogService eFactCatalog)
    {
        _sql = sql;
        _logger = logger;
        _eFactCatalog = eFactCatalog;
    }

    public async Task<List<FarmacoStockDto>> BuscarMedicamentosConStockAsync(string filtro, int idLocalidad, string apostrofe = "HV")
    {
        var resultados = new List<FarmacoStockDto>();
        if (string.IsNullOrWhiteSpace(filtro) || filtro.Trim().Length < 2)
            return resultados;

        string termino = filtro.Trim().ToUpperInvariant();

        // El catálogo comercial de E-FACT (usuario configurado) es la fuente principal.
        try
        {
            var productosEFact = await ObtenerCatalogoEFactCacheAsync();
            resultados.AddRange(productosEFact
                .Where(x => x.Estado == "A" &&
                    (x.Descripcion.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                     x.Codigo.Contains(termino, StringComparison.OrdinalIgnoreCase)))
                .Take(30)
                .Select(x => new FarmacoStockDto
                {
                    Number = string.IsNullOrWhiteSpace(x.Codigo) ? x.Id.ToString() : x.Codigo,
                    DisplayName = x.Descripcion,
                    Id = x.Id.ToString(),
                    TaxGroupCode = x.CodigoImpuesto == "2" ? $"IVA{x.PorcentajeImpuesto}" : "IVA0",
                    Inventory = 100,
                    Available = 100,
                    UnitPrice = x.Precio ?? 0,
                    BaseUnitOfMeasureCode = "UND",
                    Location = "E-FACT"
                }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar el catálogo E-FACT; se usará el catálogo veterinario local.");
        }

        // Obtener bodegas configuradas para la localidad.
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

        // Consultar únicamente el catálogo local de productos/medicinas.
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
                        CAST(100 - ISNULL(r.Reservado, 0) AS DECIMAL(18,2)) AS Inventory,
                        CAST(100 - ISNULL(r.Reservado, 0) AS DECIMAL(18,2)) AS Available,
                        CAST(ISNULL(pro_precio, 0) AS DECIMAL(18,2)) AS UnitPrice,
                        'UND' AS BaseUnitOfMeasureCode,
                        @Bodega AS Location
                    FROM vt_productos p
                    OUTER APPLY (
                        SELECT SUM(rs.cantidad) AS Reservado
                        FROM vt_reservaProductosStockBC rs
                        WHERE rs.idProd = RTRIM(ISNULL(p.pro_codigo, CAST(p.pro_id AS VARCHAR(20))))
                          AND rs.bodega = @Bodega
                    ) r
                    WHERE pro_descripcion LIKE @Filtro AND ISNULL(pro_estado, 'A') = 'A'
                    ORDER BY pro_descripcion";

                var locales = await _sql.QueryAsync<FarmacoStockDto>(sqlLocal, new { Filtro = $"%{termino}%", Bodega = bodegas.First() });
                resultados.AddRange(locales.Select(item =>
                {
                    item.Inventory = Math.Max(0, item.Inventory);
                    item.Available = item.Inventory;
                    return item;
                }));
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

        return resultados;
    }

    private async Task<List<CatalogoOperativoDto>> ObtenerCatalogoEFactCacheAsync()
    {
        if (_catalogoEFactCache is not null && DateTime.UtcNow < _catalogoEFactExpira)
            return _catalogoEFactCache;

        await _catalogoLock.WaitAsync();
        try
        {
            if (_catalogoEFactCache is not null && DateTime.UtcNow < _catalogoEFactExpira)
                return _catalogoEFactCache;

            _catalogoEFactCache = await _eFactCatalog.ObtenerProductosAsync();
            _catalogoEFactExpira = DateTime.UtcNow.AddMinutes(2);
            return _catalogoEFactCache;
        }
        finally
        {
            _catalogoLock.Release();
        }
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

}
