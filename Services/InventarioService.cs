using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IInventarioService
{
    Task<List<FarmacoStockDto>> BuscarMedicamentosConStockAsync(string filtro, int idLocalidad, string apostrofe = "HV");
    Task<decimal> ObtenerStockReservadoLocalAsync(string codigoProd, string bodega);
    Task<int> GuardarReservaStockAsync(string idProd, int idUser, decimal cantidad, int localidad, string bodega, int orden, int nhcl, int idReserva, string origenReserva = "QUI", int? idRegistroAne = null, int? hoja = null);
    Task<int> EliminarReservaStockAsync(string idProd, int orden, int idReserva, string origenReserva = "QUI");
    void InvalidarCacheCatalogo();
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

        string terminoNorm = NormalizarTexto(filtro.Trim());

        // El catálogo migrado de PRODUCTO es la fuente exclusiva. Los registros
        // se consultan directamente en VeterinariaFacturacion por localidad activa.
        try
        {
            var productosEFact = await ObtenerCatalogoEFactCacheAsync();
            resultados.AddRange(productosEFact
                .Where(x => x.Estado == "A" &&
                    !x.EsServicio &&
                    x.Inventario &&
                    (NormalizarTexto(x.Descripcion).Contains(terminoNorm) ||
                     NormalizarTexto(x.Codigo).Contains(terminoNorm)))
                .Take(40)
                .Select(x => new FarmacoStockDto
                {
                    Number = string.IsNullOrWhiteSpace(x.Codigo) ? x.Id.ToString() : x.Codigo,
                    DisplayName = x.Descripcion,
                    Id = x.Id.ToString(),
                    TaxGroupCode = x.CodigoImpuesto == "2" ? $"IVA{x.PorcentajeImpuesto}" : "IVA0",
                    Inventory = 0,
                    Available = 0,
                    UnitPrice = x.Precio ?? 0,
                    BaseUnitOfMeasureCode = "UND",
                    Location = "VETERINARIA"
                }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar el catálogo local de productos e insumos.");
        }

        return resultados;
    }

    private static string NormalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var normalized = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
    }

    public static void InvalidarCache()
    {
        _catalogoEFactCache = null;
        _catalogoEFactExpira = DateTime.MinValue;
    }

    public void InvalidarCacheCatalogo() => InvalidarCache();

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

    private sealed class ReservaStockResumen
    {
        public string Codigo { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
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
