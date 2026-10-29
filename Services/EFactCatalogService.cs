using System.Net.Http.Json;
using HosvetKardex.Web.Models;
using Microsoft.Extensions.Options;

namespace HosvetKardex.Web.Services;

public sealed class EFactApiOptions
{
    public const string SectionName = "EFactApi";
    public string BaseUrl { get; set; } = string.Empty;
    public int UserId { get; set; }
}

public interface IEFactCatalogService
{
    Task<List<CatalogoOperativoDto>> ObtenerCategoriasAsync();
    Task<List<CatalogoOperativoDto>> ObtenerSubcategoriasAsync();
    Task<List<CatalogoOperativoDto>> ObtenerProductosAsync();
    Task<List<TarifaIvaDto>> ObtenerTarifasIvaAsync();
    Task<bool> GuardarCategoriaAsync(CatalogoOperativoDto item);
    Task<bool> GuardarSubcategoriaAsync(CatalogoOperativoDto item);
    Task<bool> GuardarProductoAsync(CatalogoOperativoDto item);
    Task<bool> EliminarProductoAsync(int codigo);
}

public sealed class EFactCatalogService(
    HttpClient http,
    IOptions<EFactApiOptions> options,
    ILogger<EFactCatalogService> logger) : IEFactCatalogService
{
    private readonly EFactApiOptions _options = options.Value;

    public async Task<List<CatalogoOperativoDto>> ObtenerCategoriasAsync()
    {
        try
        {
            var items = await http.GetFromJsonAsync<List<CategoriaDto>>(Url("api/categorias")) ?? [];
            return items.Select(x => new CatalogoOperativoDto
            {
                Id = x.IdCategoria,
                Descripcion = x.Descripcion ?? string.Empty,
                Estado = x.Estado ? "A" : "I"
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron obtener las categorías de E-FACT para el usuario {UserId}.", _options.UserId);
            return [];
        }
    }

    public async Task<List<CatalogoOperativoDto>> ObtenerProductosAsync()
    {
        try
        {
            var categoriasTask = ObtenerCategoriasAsync();
            var subcategoriasTask = ObtenerSubcategoriasAsync();
            var productosTask = http.GetFromJsonAsync<List<ProductoDto>>(Url("api/productos"));
            await Task.WhenAll(categoriasTask, subcategoriasTask, productosTask);

            var categorias = (await categoriasTask).ToDictionary(x => x.Id, x => x.Descripcion);
            var subcategorias = (await subcategoriasTask).ToDictionary(x => x.Id, x => x.Descripcion);
            var items = await productosTask ?? [];
            return items.Select(x => new CatalogoOperativoDto
            {
                Id = x.Codigo,
                Descripcion = x.Nombre ?? string.Empty,
                Codigo = x.CodigoPrincipal ?? string.Empty,
                Precio = x.ValorUnitario,
                Precio2 = x.Precio2,
                Precio3 = x.Precio3,
                IdGrupo = x.TipoProducto,
                Grupo = x.TipoProducto.HasValue && categorias.TryGetValue(x.TipoProducto.Value, out var categoria) ? categoria : string.Empty,
                IdSubgrupo = x.Idsubtipo,
                Subgrupo = x.Idsubtipo.HasValue && subcategorias.TryGetValue(x.Idsubtipo.Value, out var subcategoria) ? subcategoria : string.Empty,
                Tipo = x.TipoCompravena ?? "PRODUCTO",
                CodigoImpuesto = x.Codigoimpuesto ?? string.Empty,
                PorcentajeImpuesto = x.Porcentajeimpuesto ?? string.Empty,
                Estado = x.Estado == false ? "I" : "A"
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron obtener los productos de E-FACT para el usuario {UserId}.", _options.UserId);
            return [];
        }
    }

    public async Task<List<TarifaIvaDto>> ObtenerTarifasIvaAsync()
    {
        try
        {
            var lookups = await http.GetFromJsonAsync<LookupsDto>(Url("api/productos/lookups"));
            return lookups?.Ivas ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron obtener las tarifas de IVA de E-FACT.");
            return [];
        }
    }

    public async Task<List<CatalogoOperativoDto>> ObtenerSubcategoriasAsync()
    {
        try
        {
            var items = await http.GetFromJsonAsync<List<SubcategoriaDto>>(Url("api/subcategorias")) ?? [];
            return items.Select(x => new CatalogoOperativoDto
            {
                Id = x.IdSubcategoria,
                Descripcion = x.Descripcion ?? string.Empty,
                IdGrupo = x.IdCategoria,
                Grupo = x.CategoriaDescripcion ?? string.Empty,
                Estado = x.Estado ? "A" : "I"
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron obtener las subcategorías de E-FACT para el usuario {UserId}.", _options.UserId);
            return [];
        }
    }

    public async Task<bool> GuardarCategoriaAsync(CatalogoOperativoDto item)
    {
        if (!ConfiguracionValida() || string.IsNullOrWhiteSpace(item.Descripcion)) return false;
        try
        {
            var payload = new { Descripcion = item.Descripcion.Trim(), Estado = item.Estado != "I" };
            var response = item.Id > 0
                ? await http.PutAsJsonAsync(Url($"api/categorias/{item.Id}"), payload)
                : await http.PostAsJsonAsync(Url("api/categorias"), payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar la categoría {Categoria} en E-FACT.", item.Id);
            return false;
        }
    }

    public async Task<bool> GuardarProductoAsync(CatalogoOperativoDto item)
    {
        if (!ConfiguracionValida() || string.IsNullOrWhiteSpace(item.Descripcion) || !item.Precio.HasValue) return false;
        try
        {
            ProductoDto producto;
            if (item.Id > 0)
            {
                producto = await http.GetFromJsonAsync<ProductoDto>(Url($"api/productos/{item.Id}")) ?? new ProductoDto();
            }
            else
            {
                producto = new ProductoDto { TipoCompravena = "PRODUCTO", Estado = true };
            }

            producto.Nombre = item.Descripcion.Trim();
            producto.CodigoPrincipal = string.IsNullOrWhiteSpace(item.Codigo) ? null : item.Codigo.Trim();
            producto.ValorUnitario = item.Precio;
            producto.Precio2 = item.Precio2;
            producto.Precio3 = item.Precio3;
            producto.TipoProducto = item.IdGrupo;
            producto.Idsubtipo = item.IdSubgrupo;
            producto.TipoCompravena = item.Tipo is "SERVICIO" ? "SERVICIO" : "PRODUCTO";
            producto.Codigoimpuesto = item.CodigoImpuesto == "2" ? "2" : null;
            producto.Porcentajeimpuesto = producto.Codigoimpuesto == "2" ? item.PorcentajeImpuesto : null;
            producto.Estado = item.Estado != "I";

            var response = item.Id > 0
                ? await http.PutAsJsonAsync(Url($"api/productos/{item.Id}"), producto)
                : await http.PostAsJsonAsync(Url("api/productos"), producto);
            if (response.IsSuccessStatusCode)
            {
                InventarioService.InvalidarCache();
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar el producto {Producto} en E-FACT.", item.Id);
            return false;
        }
    }

    public async Task<bool> GuardarSubcategoriaAsync(CatalogoOperativoDto item)
    {
        if (!ConfiguracionValida() || string.IsNullOrWhiteSpace(item.Descripcion) || !item.IdGrupo.HasValue) return false;
        try
        {
            var payload = new { Descripcion = item.Descripcion.Trim(), IdCategoria = item.IdGrupo, Estado = item.Estado != "I" };
            var response = item.Id > 0
                ? await http.PutAsJsonAsync(Url($"api/subcategorias/{item.Id}"), payload)
                : await http.PostAsJsonAsync(Url("api/subcategorias"), payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar la subcategoría {Subcategoria} en E-FACT.", item.Id);
            return false;
        }
    }

    public async Task<bool> EliminarProductoAsync(int codigo)
    {
        if (!ConfiguracionValida() || codigo <= 0) return false;
        try
        {
            var response = await http.DeleteAsync(Url($"api/productos/{codigo}"));
            if (response.IsSuccessStatusCode)
            {
                InventarioService.InvalidarCache();
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo eliminar el producto {Producto} de E-FACT.", codigo);
            return false;
        }
    }

    private string Url(string path) => $"{path}?userId={_options.UserId}";
    private bool ConfiguracionValida() => _options.UserId > 0 && !string.IsNullOrWhiteSpace(_options.BaseUrl);

    private sealed class CategoriaDto
    {
        public int IdCategoria { get; set; }
        public string? Descripcion { get; set; }
        public bool Estado { get; set; }
    }

    private sealed class SubcategoriaDto
    {
        public int IdSubcategoria { get; set; }
        public int IdCategoria { get; set; }
        public string? CategoriaDescripcion { get; set; }
        public string? Descripcion { get; set; }
        public bool Estado { get; set; }
    }

    private sealed class ProductoDto
    {
        public int Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? CodigoPrincipal { get; set; }
        public decimal? ValorUnitario { get; set; }
        public decimal? Precio2 { get; set; }
        public decimal? Precio3 { get; set; }
        public string? TipoCompravena { get; set; }
        public int? TipoProducto { get; set; }
        public int? Idsubtipo { get; set; }
        public string? Codigoimpuesto { get; set; }
        public string? Porcentajeimpuesto { get; set; }
        public bool? Estado { get; set; }
        public string? Observacion { get; set; }
    }

    private sealed class LookupsDto
    {
        public List<TarifaIvaDto> Ivas { get; set; } = [];
    }
}
