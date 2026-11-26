using HosvetKardex.Web.Data;
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
    Task<List<string>> ObtenerProductosTransfusionAsync();
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
    ISqlDataService clinica,
    SessionStateService session,
    ILogger<EFactCatalogService> logger) : IEFactCatalogService
{
    private int UsuarioFacturacion => 1_000_000 + (session.Localidad > 0 ? session.Localidad : 1);
    private const string CategoriaTransfusiones = "Transfusiones sanguíneas";

    public async Task<List<string>> ObtenerProductosTransfusionAsync()
    {
        var sedeActual = session.Localidad > 0 ? session.Localidad : 1;
        var sedes = await clinica.QueryAsync<int>("""
            IF COL_LENGTH('dbo.vt_localidades', 'idEmpresa') IS NULL
                SELECT CONVERT(int, loc_id)
                FROM dbo.vt_localidades
                WHERE ISNULL(loc_estado, 'A') = 'A';
            ELSE
                EXEC sys.sp_executesql N'
                    SELECT CONVERT(int, sede.loc_id)
                    FROM dbo.vt_localidades sede
                    INNER JOIN dbo.vt_localidades actual
                        ON actual.loc_id = @SedeActual AND sede.idEmpresa = actual.idEmpresa
                    WHERE ISNULL(sede.loc_estado, ''A'') = ''A'';',
                    N'@SedeActual int', @SedeActual = @SedeActual;
            """, new { SedeActual = sedeActual });

        List<string> opciones = [];
        foreach (var sede in sedes.Where(id => id > 0).Append(sedeActual).Distinct().Order())
        {
            var productos = await PrepararProductosTransfusionAsync(1_000_000 + sede);
            if (sede == sedeActual)
                opciones = productos;
        }

        return opciones;
    }

    private async Task<List<string>> PrepararProductosTransfusionAsync(int usuario)
    {
        (string Codigo, string Nombre)[] productos =
        [
            ("TRANSF-01", "SANGRE ENTERA"),
            ("TRANSF-02", "CONCENTRADO ERITROCITARIO"),
            ("TRANSF-03", "PLASMA FRESCO CONGELADO"),
            ("TRANSF-04", "PLASMA"),
            ("TRANSF-05", "ALBÚMINA"),
            ("TRANSF-06", "OTRO")
        ];
        var codigos = productos.Select(x => x.Codigo).ToArray();
        var nombres = productos.Select(x => x.Nombre).ToArray();

        await using var contexto = await dbFactory.CreateDbContextAsync();
        return await contexto.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            await using var transaccion = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var categoria = await db.Productotipos.FirstOrDefaultAsync(t =>
                t.Idusuario == usuario && t.Descripcion == CategoriaTransfusiones);
            if (categoria is null)
            {
                categoria = new Productotipo
                {
                    Idusuario = usuario,
                    Descripcion = CategoriaTransfusiones,
                    Perecible = true,
                    Estado = true
                };
                db.Productotipos.Add(categoria);
            }

            var filas = await db.Productos.Where(p => p.Idusuario == usuario &&
                ((p.CodigoPrincipal != null && codigos.Contains(p.CodigoPrincipal.Trim())) ||
                 (p.Nombre != null && nombres.Contains(p.Nombre.Trim().ToUpper())))).ToListAsync();

            var opciones = new List<string>();
            foreach (var (codigo, nombre) in productos)
            {
                var producto = filas.FirstOrDefault(p =>
                    string.Equals(p.CodigoPrincipal?.Trim(), codigo, StringComparison.OrdinalIgnoreCase))
                    ?? filas.FirstOrDefault(p =>
                        (nombre != "OTRO" || (categoria.Idtipoproducto > 0 && p.TipoProducto == categoria.Idtipoproducto)) &&
                        string.Equals(p.Nombre?.Trim(), nombre, StringComparison.OrdinalIgnoreCase));

                if (producto is null)
                {
                    producto = new Producto
                    {
                        Idusuario = usuario,
                        CodigoPrincipal = codigo,
                        Nombre = nombre,
                        Tipocompravena = "B",
                        UnidadMedida = "UNIDAD",
                        FactorConversionInventario = 1m,
                        Inventario = false,
                        Estado = true
                    };
                    db.Productos.Add(producto);
                }

                if (producto.TipoProducto != categoria.Idtipoproducto)
                {
                    producto.TipoProductoNavigation = categoria;
                    producto.Idsubtipo = null;
                }

                if (producto.Estado != false && producto.Nombre is { } descripcion &&
                    descripcion.Trim().Length is > 0 and <= 60)
                    opciones.Add(descripcion.Trim());
            }

            var nuevos = await db.SaveChangesAsync();
            await transaccion.CommitAsync();
            if (nuevos > 0)
                InventarioService.InvalidarCache();
            return opciones.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        });
    }

    public async Task<List<CatalogoOperativoDto>> ObtenerCategoriasAsync()
    {
        var usuario = UsuarioFacturacion;
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Productotipos.AsNoTracking()
            .Where(x => x.Idusuario >= 1_000_000 &&
                (x.Descripcion != CategoriaTransfusiones || x.Idusuario == usuario))
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
            .Where(x => x.Idusuario >= 1_000_000)
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
        var usuario = UsuarioFacturacion;
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Productos.AsNoTracking()
            .Where(x => x.Idusuario >= 1_000_000 &&
                (x.TipoProductoNavigation == null ||
                 x.TipoProductoNavigation.Descripcion != CategoriaTransfusiones || x.Idusuario == usuario))
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
                SujetoFiscalizacion = x.SujetoFiscalizacion,
                UnidadMedida = string.IsNullOrWhiteSpace(x.UnidadMedida) ? "UNIDAD" : x.UnidadMedida.Trim().ToUpper(),
                FactorConversion = x.FactorConversionInventario <= 0 ? 1m : x.FactorConversionInventario,
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
                    x.Idtipoproducto == item.Id && x.Idusuario >= 1_000_000)
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
            item.Descripcion = entity.Descripcion ?? descripcion;
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
                x.Idtipoproducto == item.IdGrupo && x.Idusuario >= 1_000_000);
            if (!categoriaValida) return false;

            Productosubtipo entity;
            if (item.Id > 0)
            {
                entity = await db.Productosubtipos.FirstOrDefaultAsync(x =>
                    x.Idsubtipo == item.Id && x.Idusuario >= 1_000_000)
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
            item.Descripcion = entity.Descripcion ?? item.Descripcion.Trim();
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
                    x.Codigo == item.Id && x.Idusuario >= 1_000_000)
                    ?? throw new InvalidOperationException("El producto no pertenece a esta veterinaria.");
            }
            else
            {
                entity = new Producto { Idusuario = UsuarioFacturacion };
                db.Productos.Add(entity);
            }

            var cultura = System.Globalization.CultureInfo.GetCultureInfo("es-EC");
            entity.Nombre = string.Join(" ", item.Descripcion
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToUpper(cultura);
            entity.CodigoPrincipal = string.IsNullOrWhiteSpace(item.Codigo) ? null : item.Codigo.Trim();
            entity.ValorUnitario = item.Precio;
            entity.Precio2 = item.Precio2;
            entity.Precio3 = item.Precio3;
            bool esServicio = item.Tipo == "S" || string.Equals(item.Tipo, "SERVICIO", StringComparison.OrdinalIgnoreCase);

            if (item.IdGrupo.HasValue && item.IdGrupo.Value > 0)
            {
                var cat = await db.Productotipos.AsNoTracking().FirstOrDefaultAsync(c =>
                    c.Idtipoproducto == item.IdGrupo.Value && c.Idusuario >= 1_000_000);
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
            entity.SujetoFiscalizacion = esServicio ? false : item.SujetoFiscalizacion;
            entity.UnidadMedida = esServicio
                ? "UNIDAD"
                : (string.IsNullOrWhiteSpace(item.UnidadMedida) ? "UNIDAD" : item.UnidadMedida.Trim().ToUpperInvariant());
            entity.FactorConversionInventario = esServicio || item.FactorConversion <= 0
                ? 1m
                : Math.Round(item.FactorConversion, 4);
            entity.Codigoimpuesto = item.CodigoImpuesto == "2" ? "2" : null;
            entity.Porcentajeimpuesto = entity.Codigoimpuesto == "2" ? item.PorcentajeImpuesto : null;
            entity.Estado = item.Estado != "I";
            await db.SaveChangesAsync();
            item.Id = entity.Codigo;
            item.Descripcion = entity.Nombre ?? item.Descripcion.Trim();
            item.Codigo = entity.CodigoPrincipal ?? string.Empty;
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
                x.Codigo == codigo && x.Idusuario >= 1_000_000);
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
