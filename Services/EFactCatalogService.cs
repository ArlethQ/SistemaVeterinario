using HosvetKardex.Web.Models;
using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.Models;

namespace HosvetKardex.Web.Services;

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

/// <summary>
/// Catálogo local de productos y servicios. Usa exclusivamente la base integrada
/// VeterinariaFacturacion y separa la información por localidad veterinaria.
/// </summary>
public sealed class EFactCatalogService(
    IDbContextFactory<AppDbContext> dbFactory,
    SessionStateService session,
    ILogger<EFactCatalogService> logger) : IEFactCatalogService
{
    private int UsuarioFacturacion => 1_000_000 + session.Localidad;

    public async Task<List<CatalogoOperativoDto>> ObtenerCategoriasAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Productotipos.AsNoTracking()
            .Where(x => x.Idusuario == UsuarioFacturacion)
            .OrderBy(x => x.Descripcion)
            .Select(x => new CatalogoOperativoDto
            {
                Id = x.Idtipoproducto,
                Descripcion = x.Descripcion ?? string.Empty,
                Tipo = x.Perecible == false ? "S" : "B",
                Estado = x.Estado == false ? "I" : "A"
            })
            .ToListAsync();
    }

    public async Task<List<CatalogoOperativoDto>> ObtenerSubcategoriasAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Productosubtipos.AsNoTracking()
            .Where(x => x.Idusuario == UsuarioFacturacion)
            .OrderBy(x => x.Descripcion)
            .Select(x => new CatalogoOperativoDto
            {
                Id = x.Idsubtipo,
                Descripcion = x.Descripcion ?? string.Empty,
                IdGrupo = x.Idtipoproducto,
                Grupo = x.IdtipoproductoNavigation.Descripcion ?? string.Empty,
                Estado = x.Estado == "I" ? "I" : "A"
            })
            .ToListAsync();
    }

    public async Task<List<CatalogoOperativoDto>> ObtenerProductosAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Productos.AsNoTracking()
            .Where(x => x.Idusuario == UsuarioFacturacion)
            .OrderBy(x => x.Nombre)
            .Select(x => new CatalogoOperativoDto
            {
                Id = x.Codigo,
                Descripcion = x.Nombre ?? string.Empty,
                Codigo = x.CodigoPrincipal ?? string.Empty,
                Precio = x.ValorUnitario,
                Precio2 = x.Precio2,
                Precio3 = x.Precio3,
                IdGrupo = x.TipoProducto,
                Grupo = x.TipoProductoNavigation != null ? x.TipoProductoNavigation.Descripcion ?? string.Empty : string.Empty,
                IdSubgrupo = x.Idsubtipo,
                Subgrupo = x.IdsubtipoNavigation != null ? x.IdsubtipoNavigation.Descripcion ?? string.Empty : string.Empty,
                Tipo = (x.Tipocompravena == "S" || x.Tipocompravena == "SERVICIO") ? "S" : "B",
                Inventario = x.Inventario ?? true,
                CodigoImpuesto = x.Codigoimpuesto ?? string.Empty,
                PorcentajeImpuesto = x.Porcentajeimpuesto ?? string.Empty,
                Estado = x.Estado == false ? "I" : "A"
            })
            .ToListAsync();
    }

    public async Task<List<TarifaIvaDto>> ObtenerTarifasIvaAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Porcentajeivas.AsNoTracking()
            .Where(x => x.Estado == "A" || x.Estado == "1")
            .OrderBy(x => x.Codigo)
            .Select(x => new TarifaIvaDto
            {
                Codigo = x.Codigo,
                Descripcion = x.Descripcion ?? string.Empty,
                Valor = x.Valor ?? string.Empty
            })
            .ToListAsync();
    }

    public async Task<bool> GuardarCategoriaAsync(CatalogoOperativoDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Descripcion)) return false;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var descripcion = item.Descripcion.Trim();
            Productotipo entity;
            if (item.Id > 0)
            {
                entity = await db.Productotipos.FirstOrDefaultAsync(x =>
                    x.Idtipoproducto == item.Id && x.Idusuario == UsuarioFacturacion)
                    ?? throw new InvalidOperationException("La categoría no pertenece a esta veterinaria.");
            }
            else
            {
                entity = new Productotipo { Idusuario = UsuarioFacturacion };
                db.Productotipos.Add(entity);
            }

            entity.Descripcion = descripcion;
            if (item.Id <= 0)
            {
                entity.Perecible = item.Tipo != "S";
            }
            entity.Estado = item.Estado != "I";
            await db.SaveChangesAsync();
            item.Id = entity.Idtipoproducto;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar la categoría local de la veterinaria {Localidad}.", session.Localidad);
            return false;
        }
    }

    public async Task<bool> GuardarSubcategoriaAsync(CatalogoOperativoDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Descripcion) || item.IdGrupo is null or <= 0) return false;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var categoriaValida = await db.Productotipos.AnyAsync(x =>
                x.Idtipoproducto == item.IdGrupo && x.Idusuario == UsuarioFacturacion);
            if (!categoriaValida) return false;

            Productosubtipo entity;
            if (item.Id > 0)
            {
                entity = await db.Productosubtipos.FirstOrDefaultAsync(x =>
                    x.Idsubtipo == item.Id && x.Idusuario == UsuarioFacturacion)
                    ?? throw new InvalidOperationException("La subcategoría no pertenece a esta veterinaria.");
            }
            else
            {
                entity = new Productosubtipo { Idusuario = UsuarioFacturacion };
                db.Productosubtipos.Add(entity);
            }

            entity.Descripcion = item.Descripcion.Trim();
            entity.Idtipoproducto = item.IdGrupo.Value;
            entity.Estado = item.Estado == "I" ? "I" : "A";
            await db.SaveChangesAsync();
            item.Id = entity.Idsubtipo;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar la subcategoría local de la veterinaria {Localidad}.", session.Localidad);
            return false;
        }
    }

    public async Task<bool> GuardarProductoAsync(CatalogoOperativoDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Descripcion) || item.Precio is null) return false;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            Producto entity;
            if (item.Id > 0)
            {
                entity = await db.Productos.FirstOrDefaultAsync(x =>
                    x.Codigo == item.Id && x.Idusuario == UsuarioFacturacion)
                    ?? throw new InvalidOperationException("El producto no pertenece a esta veterinaria.");
            }
            else
            {
                entity = new Producto { Idusuario = UsuarioFacturacion };
                db.Productos.Add(entity);
            }

            entity.Nombre = item.Descripcion.Trim();
            entity.CodigoPrincipal = string.IsNullOrWhiteSpace(item.Codigo) ? null : item.Codigo.Trim();
            entity.ValorUnitario = item.Precio;
            entity.Precio2 = item.Precio2;
            entity.Precio3 = item.Precio3;
            bool esServicio = item.Tipo == "S" || string.Equals(item.Tipo, "SERVICIO", StringComparison.OrdinalIgnoreCase);

            if (item.IdGrupo.HasValue && item.IdGrupo.Value > 0)
            {
                var cat = await db.Productotipos.AsNoTracking().FirstOrDefaultAsync(c =>
                    c.Idtipoproducto == item.IdGrupo.Value && c.Idusuario == UsuarioFacturacion);
                if (cat != null)
                {
                    bool catEsServicio = cat.Perecible == false;
                    if (catEsServicio != esServicio)
                    {
                        item.IdGrupo = null;
                        item.IdSubgrupo = null;
                    }
                }
            }

            entity.TipoProducto = item.IdGrupo;
            entity.Idsubtipo = item.IdSubgrupo;
            entity.Tipocompravena = esServicio ? "S" : "B";
            entity.Inventario = esServicio ? false : item.Inventario;
            entity.Codigoimpuesto = item.CodigoImpuesto == "2" ? "2" : null;
            entity.Porcentajeimpuesto = entity.Codigoimpuesto == "2" ? item.PorcentajeImpuesto : null;
            entity.Estado = item.Estado != "I";
            await db.SaveChangesAsync();
            item.Id = entity.Codigo;
            InventarioService.InvalidarCache();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar el producto local de la veterinaria {Localidad}.", session.Localidad);
            return false;
        }
    }

    public async Task<bool> EliminarProductoAsync(int codigo)
    {
        if (codigo <= 0) return false;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var entity = await db.Productos.FirstOrDefaultAsync(x =>
                x.Codigo == codigo && x.Idusuario == UsuarioFacturacion);
            if (entity is null) return false;
            entity.Estado = false;
            await db.SaveChangesAsync();
            InventarioService.InvalidarCache();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo desactivar el producto local {Producto}.", codigo);
            return false;
        }
    }
}
