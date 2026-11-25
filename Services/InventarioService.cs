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
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;
        string terminoNorm = NormalizarTexto(filtro?.Trim() ?? string.Empty);

        try
        {
            const string sql = @"
                SELECT
                    CAST(p.CODIGO AS VARCHAR(50)) AS Id,
                    ISNULL(NULLIF(LTRIM(RTRIM(p.CODIGO_PRINCIPAL)), ''), CAST(p.CODIGO AS VARCHAR(50))) AS Number,
                    ISNULL(p.NOMBRE, '') AS DisplayName,
                    CASE WHEN ISNULL(p.CODIGOIMPUESTO, '') = '2' THEN 'IVA' + ISNULL(p.PORCENTAJEIMPUESTO, '15') ELSE 'IVA0' END AS TaxGroupCode,
                    CAST(SUM(e.stock_actual) AS DECIMAL(18,4)) AS Inventory,
                    CAST(SUM(CASE WHEN e.stock_actual - ISNULL(e.stock_reservado, 0) > 0 THEN e.stock_actual - ISNULL(e.stock_reservado, 0) ELSE 0 END) AS DECIMAL(18,4)) AS Available,
                    CAST(MAX(CASE WHEN ISNULL(p.VALOR_UNITARIO, 0) > 0 THEN p.VALOR_UNITARIO ELSE ISNULL(e.costo_promedio, 0) END) AS DECIMAL(18,4)) AS UnitPrice,
                    ISNULL(NULLIF(LTRIM(RTRIM(p.UNIDAD_MEDIDA)), ''), 'UNIDAD') AS BaseUnitOfMeasureCode,
                    CAST(CASE WHEN ISNULL(p.FACTOR_CONVERSION_INVENTARIO, 0) <= 0 THEN 1 ELSE p.FACTOR_CONVERSION_INVENTARIO END AS DECIMAL(18,4)) AS FactorConversion,
                    COALESCE(
                        NULLIF(MAX(CASE WHEN b.nombre IS NOT NULL THEN b.nombre + CASE WHEN ISNULL(b.codigo, '') <> '' THEN ' (' + b.codigo + ')' ELSE '' END ELSE '' END), ''),
                        (
                            SELECT TOP 1
                                LTRIM(RTRIM(REPLACE(SUBSTRING(m.observacion, CHARINDEX('Bodega:', m.observacion) + 7, 120), ']', '')))
                            FROM dbo.ECONTAX_INVENTARIO_MOVIMIENTO m
                            WHERE m.codigo_producto = p.CODIGO
                              AND m.id_sucursal = @IdLocalidad
                              AND m.observacion LIKE '%Bodega:%'
                            ORDER BY m.id_movimiento DESC
                        ),
                        (
                            SELECT STRING_AGG(bs.nombre + CASE WHEN ISNULL(bs.codigo, '') <> '' THEN ' (' + bs.codigo + ')' ELSE '' END, ', ')
                            FROM dbo.ECONTAX_BODEGA bs
                            WHERE bs.id_sucursal = @IdLocalidad AND bs.estado = 1
                        ),
                        'Bodega de la sucursal'
                    ) AS Location
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e
                INNER JOIN dbo.PRODUCTO p ON p.CODIGO = e.codigo_producto
                LEFT JOIN dbo.ECONTAX_BODEGA b ON b.id_bodega = e.id_bodega
                WHERE e.id_sucursal = @IdLocalidad
                  AND ISNULL(p.ESTADO, 1) <> 0
                  AND ISNULL(p.INVENTARIO, 1) = 1
                  AND ISNULL(UPPER(p.TIPOCOMPRAVENA), 'B') NOT IN ('SERVICIO', 'S')
                GROUP BY
                    p.CODIGO,
                    p.CODIGO_PRINCIPAL,
                    p.NOMBRE,
                    p.CODIGOIMPUESTO,
                    p.PORCENTAJEIMPUESTO,
                    p.UNIDAD_MEDIDA,
                    p.FACTOR_CONVERSION_INVENTARIO
                HAVING SUM(CASE WHEN e.stock_actual - ISNULL(e.stock_reservado, 0) > 0 THEN e.stock_actual - ISNULL(e.stock_reservado, 0) ELSE 0 END) > 0
                ORDER BY p.NOMBRE";

            var enStockSede = (await _sql.QueryAsync<FarmacoStockDto>(sql, new { IdLocalidad = sedeId })).ToList();
            if (!string.IsNullOrWhiteSpace(terminoNorm))
            {
                enStockSede = enStockSede
                    .Where(x =>
                        NormalizarTexto(x.DisplayName).Contains(terminoNorm) ||
                        NormalizarTexto(x.Number).Contains(terminoNorm) ||
                        NormalizarTexto(x.Id).Contains(terminoNorm))
                    .ToList();
            }

            resultados.AddRange(enStockSede.Take(60));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar las existencias de la sede {IdLocalidad}.", sedeId);
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
