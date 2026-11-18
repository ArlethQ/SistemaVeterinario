using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using Simetric.Data;
using Simetric.Data.EContax;
using Simetric.Models.EContax;
using System.Text.RegularExpressions;

namespace Simetric.Services.EContax;

public sealed class EContaxContabilidadService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EContaxTenantService _tenantService;
    private readonly EContaxErpSchemaService _schemaService;

    private static readonly (string Clave, string Descripcion, string CodigoDefault)[] ConfigDefaults =
    [
        ("CAJA_GENERAL", "Caja General", "1.1.1.1.1"),
        ("BANCOS_DEFAULT", "Bancos Principal", "1.1.1.3.1"),
        ("CLIENTES_CXC", "Cuentas por Cobrar Clientes", "1.1.2.5.1"),
        ("PROVEEDORES_CXP", "Cuentas por Pagar Proveedores", "2.1.3.1.1"),
        ("ANTICIPO_CLIENTES", "Anticipos de Clientes", "2.1.1.0"),
        ("ANTICIPO_PROVEEDORES", "Anticipo a Proveedores", "1.1.4.3"),
        ("INVENTARIO_MERCADERIA", "Inventario de Mercaderías", "1.1.3.6"),
        ("IVA_COMPRAS", "IVA sobre Compras", "1.1.5.1.1"),
        ("IVA_VENTAS", "IVA sobre Ventas", "2.1.7.4.1"),
        ("RET_RENTA_PAGAR", "Retenciones en la Fuente por Pagar", "2.1.7.2"),
        ("RET_IVA_PAGAR", "Retenciones de IVA por Pagar", "2.1.7.3"),
        ("RET_RENTA_COBRAR", "Retenciones en la Fuente por Cobrar", "1.1.5.3"),
        ("RET_IVA_COBRAR", "Retenciones de IVA por Cobrar", "1.1.5.2"),
        ("VENTAS_BIENES", "Venta de Bienes / Servicios", "4.1.1"),
        ("DEVOLUCIONES_VENTAS", "Devoluciones sobre Ventas", "4.1.12"),
        ("DESCUENTOS_VENTAS", "Descuentos sobre Ventas", "4.1.4"),
        ("COSTO_VENTAS", "Costo de Ventas", "5.1"),
        ("GASTOS_COMPRAS_DEFAULT", "Compras / Otros Gastos Operacionales", "5.2.1.2.73"),
        ("GASTOS_BANCARIOS", "Comisiones y Gastos Bancarios", "5.2.1.3.2"),
        ("SUELDOS_GASTO", "Gasto Sueldos y Salarios", "5.2.1.2.1"),
        ("APORTE_PATRONAL_GASTO", "Gasto Aporte Patronal IESS", "5.2.1.2.5"),
        ("DECIMO_TERCERO_GASTO", "Gasto Décimo Tercer Sueldo", "5.2.1.2.8"),
        ("DECIMO_CUARTO_GASTO", "Gasto Décimo Cuarto Sueldo", "5.2.1.2.9"),
        ("FONDO_RESERVA_GASTO", "Gasto Fondos de Reserva", "5.2.1.2.7"),
        ("SUELDOS_PAGAR", "Sueldos por Pagar", "2.1.7.7.1"),
        ("IESS_PAGAR", "Retenciones y Aportes IESS por Pagar", "2.1.7.1"),
        ("DECIMO_TERCERO_PAGAR", "Décimo Tercer Sueldo por Pagar", "2.1.7.6.1"),
        ("DECIMO_CUARTO_PAGAR", "Décimo Cuarto Sueldo por Pagar", "2.1.7.6.2"),
        ("FONDO_RESERVA_PAGAR", "Fondos de Reserva por Pagar", "2.1.7.6.6"),
        ("ANTICIPO_EMPLEADOS", "Anticipo de Empleados", "1.1.2.5.4.1")
    ];

    public EContaxContabilidadService(
        IDbContextFactory<AppDbContext> dbFactory,
        EContaxTenantService tenantService,
        EContaxErpSchemaService schemaService)
    {
        _dbFactory = dbFactory;
        _tenantService = tenantService;
        _schemaService = schemaService;
    }

    public async Task<EContaxUserContext> EnsureInicializadoAsync(int userId)
    {
        await _schemaService.EnsureSchemaAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var actor = await _tenantService.GetContextAsync(db, userId);
        await EnsurePlanCuentasEmpresaAsync(db, actor.IdEmpresa);
        return actor;
    }

    public async Task EnsurePlanCuentasEmpresaAsync(AppDbContext db, int idEmpresa)
    {
        var tieneCuentas = await db.EContaxCuentasContables.AnyAsync(x => x.IdEmpresa == idEmpresa);
        if (!tieneCuentas)
        {
            var seed = EContaxPlanCuentasSeed.ObtenerCuentas();
            var now = DateTime.UtcNow;
            var entities = seed.Select(s => new EContaxCuentaContable
            {
                IdEmpresa = idEmpresa,
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                CodigoPadre = s.CodigoPadre,
                Nivel = s.Nivel,
                TipoCuenta = s.TipoCuenta,
                Naturaleza = s.Naturaleza,
                EsMovimiento = s.EsMovimiento,
                AceptaAuxiliar = s.EsMovimiento,
                ClasificacionFlujo = s.ClasificacionFlujo,
                Activa = true,
                FechaCreacion = now
            }).ToList();

            db.EContaxCuentasContables.AddRange(entities);
            await db.SaveChangesAsync();
        }

        var existentes = await db.EContaxConfigCuentasContables
            .Where(x => x.IdEmpresa == idEmpresa)
            .Select(x => x.Clave)
            .ToListAsync();
        var set = existentes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = false;
        foreach (var (clave, desc, cod) in ConfigDefaults)
        {
            if (!set.Contains(clave))
            {
                db.EContaxConfigCuentasContables.Add(new EContaxConfigCuentaContable
                {
                    IdEmpresa = idEmpresa,
                    Clave = clave,
                    Descripcion = desc,
                    CodigoCuenta = cod,
                    FechaActualizacion = DateTime.UtcNow
                });
                added = true;
            }
        }
        if (added)
            await db.SaveChangesAsync();
    }

    // ==================== PLAN DE CUENTAS ====================

    public async Task<List<EContaxCuentaContable>> ListarPlanCuentasAsync(int userId, string? filtro = null, bool soloMovimiento = false, bool soloActivas = false)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.EContaxCuentasContables.AsNoTracking().Where(x => x.IdEmpresa == actor.IdEmpresa);

        if (soloMovimiento)
            q = q.Where(x => x.EsMovimiento);
        if (soloActivas)
            q = q.Where(x => x.Activa);
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var f = filtro.Trim();
            q = q.Where(x => x.Codigo.Contains(f) || x.Nombre.Contains(f));
        }

        return await q.OrderBy(x => x.Codigo).ToListAsync();
    }

    public async Task<EContaxCuentaContable> GuardarCuentaAsync(int userId, EContaxCuentaContable model)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var codigo = model.Codigo.Trim();
        var nombre = model.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El código y el nombre de la cuenta son obligatorios.");

        var partes = codigo.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var nivel = partes.Length;
        var padre = nivel > 1 ? string.Join('.', partes[..^1]) : null;
        var top = partes[0];
        var tipo = string.IsNullOrWhiteSpace(model.TipoCuenta)
            ? top switch { "1" => "ACTIVO", "2" => "PASIVO", "3" => "PATRIMONIO", "4" => "INGRESO", _ => "GASTO" }
            : model.TipoCuenta.Trim().ToUpperInvariant();
        var nat = string.IsNullOrWhiteSpace(model.Naturaleza)
            ? (top is "1" or "5" ? "DEUDORA" : "ACREEDORA")
            : model.Naturaleza.Trim().ToUpperInvariant();

        if (model.IdCuenta == 0)
        {
            if (await db.EContaxCuentasContables.AnyAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == codigo))
                throw new InvalidOperationException($"Ya existe una cuenta con el código {codigo}.");

            var entity = new EContaxCuentaContable
            {
                IdEmpresa = actor.IdEmpresa,
                Codigo = codigo,
                Nombre = nombre,
                CodigoPadre = string.IsNullOrWhiteSpace(model.CodigoPadre) ? padre : model.CodigoPadre.Trim(),
                Nivel = nivel,
                TipoCuenta = tipo,
                Naturaleza = nat,
                EsMovimiento = model.EsMovimiento,
                AceptaAuxiliar = model.AceptaAuxiliar,
                ClasificacionFlujo = string.IsNullOrWhiteSpace(model.ClasificacionFlujo) ? "OPERACION" : model.ClasificacionFlujo.Trim().ToUpperInvariant(),
                Activa = model.Activa,
                FechaCreacion = DateTime.UtcNow
            };
            db.EContaxCuentasContables.Add(entity);

            if (!string.IsNullOrWhiteSpace(entity.CodigoPadre))
            {
                var ctaPadre = await db.EContaxCuentasContables.FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == entity.CodigoPadre);
                if (ctaPadre is not null && ctaPadre.EsMovimiento)
                {
                    ctaPadre.EsMovimiento = false;
                    ctaPadre.FechaActualizacion = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();
            return entity;
        }
        else
        {
            var entity = await db.EContaxCuentasContables.FirstOrDefaultAsync(x => x.IdCuenta == model.IdCuenta && x.IdEmpresa == actor.IdEmpresa)
                ?? throw new InvalidOperationException("Cuenta contable no encontrada.");

            entity.Nombre = nombre;
            entity.TipoCuenta = tipo;
            entity.Naturaleza = nat;
            entity.EsMovimiento = model.EsMovimiento;
            entity.AceptaAuxiliar = model.AceptaAuxiliar;
            entity.ClasificacionFlujo = string.IsNullOrWhiteSpace(model.ClasificacionFlujo) ? entity.ClasificacionFlujo : model.ClasificacionFlujo.Trim().ToUpperInvariant();
            entity.Activa = model.Activa;
            entity.FechaActualizacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return entity;
        }
    }

    public async Task<(int Insertadas, int Actualizadas)> ImportarPlanCuentasExcelAsync(int userId, Stream excelStream)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var rows = MiniExcel.Query(excelStream, useHeaderRow: false).ToList();
        var pat = new Regex(@"^\s*(\d+(?:\.\d+)*)\s+(.+?)\s*$", RegexOptions.Compiled);
        var parsed = new List<(string Codigo, string Nombre)>();

        foreach (IDictionary<string, object> row in rows)
        {
            var firstVal = row.Values.FirstOrDefault(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()))?.ToString();
            if (string.IsNullOrWhiteSpace(firstVal))
                continue;
            var m = pat.Match(firstVal);
            if (m.Success)
            {
                parsed.Add((m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim()));
            }
        }

        if (parsed.Count == 0)
            throw new InvalidOperationException("No se encontraron cuentas válidas en el archivo Excel (formato esperado: '1.1.1  Nombre de cuenta').");

        var codeSet = parsed.Select(x => x.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existentes = await db.EContaxCuentasContables
            .Where(x => x.IdEmpresa == actor.IdEmpresa)
            .ToDictionaryAsync(x => x.Codigo, StringComparer.OrdinalIgnoreCase);

        int ins = 0, upd = 0;
        foreach (var (codigo, nombre) in parsed.DistinctBy(x => x.Codigo))
        {
            var parts = codigo.Split('.');
            var nivel = parts.Length;
            var padre = nivel > 1 ? string.Join('.', parts[..^1]) : null;
            if (padre != null && !codeSet.Contains(padre) && !existentes.ContainsKey(padre))
                padre = null;
            var hasChild = codeSet.Any(c => c.StartsWith(codigo + ".", StringComparison.OrdinalIgnoreCase));
            var top = parts[0];
            var tipo = top switch { "1" => "ACTIVO", "2" => "PASIVO", "3" => "PATRIMONIO", "4" => "INGRESO", _ => "GASTO" };
            var nat = top is "1" or "5" ? "DEUDORA" : "ACREEDORA";
            var flujo = codigo.StartsWith("1.1.1") ? "EFECTIVO"
                : (codigo.StartsWith("1.2") || codigo.StartsWith("1.3")) ? "INVERSION"
                : (codigo.StartsWith("2.2") || codigo.StartsWith("3.")) ? "FINANCIAMIENTO"
                : "OPERACION";

            if (existentes.TryGetValue(codigo, out var ex))
            {
                ex.Nombre = nombre;
                ex.EsMovimiento = !hasChild;
                ex.FechaActualizacion = DateTime.UtcNow;
                upd++;
            }
            else
            {
                db.EContaxCuentasContables.Add(new EContaxCuentaContable
                {
                    IdEmpresa = actor.IdEmpresa,
                    Codigo = codigo,
                    Nombre = nombre,
                    CodigoPadre = padre,
                    Nivel = nivel,
                    TipoCuenta = tipo,
                    Naturaleza = nat,
                    EsMovimiento = !hasChild,
                    AceptaAuxiliar = !hasChild,
                    ClasificacionFlujo = flujo,
                    Activa = true,
                    FechaCreacion = DateTime.UtcNow
                });
                ins++;
            }
        }

        await db.SaveChangesAsync();
        return (ins, upd);
    }

    public async Task<List<EContaxConfigCuentaContable>> ListarConfigCuentasAsync(int userId)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.EContaxConfigCuentasContables.AsNoTracking()
            .Where(x => x.IdEmpresa == actor.IdEmpresa)
            .OrderBy(x => x.Descripcion)
            .ToListAsync();
    }

    public async Task GuardarConfigCuentaAsync(int userId, string clave, string codigoCuenta)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var cta = await db.EContaxCuentasContables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == codigoCuenta.Trim())
            ?? throw new InvalidOperationException($"La cuenta contable '{codigoCuenta}' no existe en el plan de cuentas.");

        var cfg = await db.EContaxConfigCuentasContables
            .FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Clave == clave);
        if (cfg is null)
            throw new InvalidOperationException("Clave de configuración no encontrada.");

        cfg.CodigoCuenta = cta.Codigo;
        cfg.FechaActualizacion = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ==================== ASIENTOS CONTABLES (CRUD + REVERSO) ====================

    public async Task<List<EContaxAsientoContable>> ListarAsientosAsync(
        int userId,
        DateTime? desde = null,
        DateTime? hasta = null,
        string? moduloOrigen = null,
        string? estado = null,
        string? busqueda = null)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.EContaxAsientosContables.AsNoTracking()
            .Include(x => x.Detalles.OrderBy(d => d.Linea))
            .Where(x => x.IdEmpresa == actor.IdEmpresa);

        if (desde.HasValue) q = q.Where(x => x.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(x => x.Fecha < hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(moduloOrigen)) q = q.Where(x => x.ModuloOrigen == moduloOrigen.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(x => x.Estado == estado.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var f = busqueda.Trim();
            q = q.Where(x => x.NumeroAsiento.Contains(f) || x.Glosa.Contains(f) || (x.ReferenciaDocumento != null && x.ReferenciaDocumento.Contains(f)));
        }

        return await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.IdAsiento).ToListAsync();
    }

    public async Task<EContaxAsientoContable?> ObtenerAsientoPorIdAsync(int userId, int idAsiento)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.EContaxAsientosContables.AsNoTracking()
            .Include(x => x.Detalles.OrderBy(d => d.Linea))
            .FirstOrDefaultAsync(x => x.IdAsiento == idAsiento && x.IdEmpresa == actor.IdEmpresa);
    }

    public async Task<EContaxAsientoContable> CrearAsientoAsync(int userId, EContaxAsientoContable dto)
    {
        if (EContaxActivosFijosService.EsOrigenGestionado(dto.ModuloOrigen?.Trim()))
            throw new InvalidOperationException("Crea este asiento desde su módulo de origen.");

        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await CrearAsientoInternoAsync(db, actor, dto);
    }

    public async Task<EContaxAsientoContable> ActualizarAsientoAsync(int userId, EContaxAsientoContable dto)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var asiento = await db.EContaxAsientosContables
            .Include(x => x.Detalles)
            .FirstOrDefaultAsync(x => x.IdAsiento == dto.IdAsiento && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Asiento contable no encontrado.");

        if (EContaxActivosFijosService.EsOrigenGestionado(asiento.ModuloOrigen))
            throw new InvalidOperationException("Gestiona este asiento desde su módulo de origen.");

        if (asiento.Estado is "REVERSADO" or "ANULADO")
            throw new InvalidOperationException("No se puede modificar un asiento reversado o anulado.");

        ValidarYResolverDetalles(db, actor.IdEmpresa, dto.Detalles, out var totalDebe, out var totalHaber);

        asiento.Fecha = dto.Fecha.Date;
        asiento.TipoAsiento = string.IsNullOrWhiteSpace(dto.TipoAsiento) ? "DIARIO" : dto.TipoAsiento.Trim().ToUpperInvariant();
        asiento.Glosa = dto.Glosa.Trim();
        asiento.ReferenciaDocumento = dto.ReferenciaDocumento?.Trim();
        asiento.TotalDebe = totalDebe;
        asiento.TotalHaber = totalHaber;
        asiento.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? asiento.Estado : dto.Estado.Trim().ToUpperInvariant();
        asiento.ActualizadoPor = actor.IdUsuario;
        asiento.FechaActualizacion = DateTime.UtcNow;

        db.EContaxAsientosDetalles.RemoveRange(asiento.Detalles);
        var linea = 1;
        foreach (var d in dto.Detalles.Where(x => x.Debe > 0 || x.Haber > 0))
        {
            asiento.Detalles.Add(new EContaxAsientoDetalle
            {
                IdCuenta = d.IdCuenta,
                CodigoCuenta = d.CodigoCuenta,
                NombreCuenta = d.NombreCuenta,
                Linea = linea++,
                Detalle = d.Detalle,
                Debe = Math.Round(d.Debe, 2),
                Haber = Math.Round(d.Haber, 2),
                TipoTercero = d.TipoTercero,
                IdTercero = d.IdTercero,
                IdentificacionTercero = d.IdentificacionTercero,
                NombreTercero = d.NombreTercero,
                CentroCosto = d.CentroCosto
            });
        }

        await db.SaveChangesAsync();
        return asiento;
    }

    public async Task<EContaxAsientoContable> ReversarAsientoAsync(int userId, int idAsiento, string? motivo = null, DateTime? fechaReverso = null)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var contextoEstrategia = await _dbFactory.CreateDbContextAsync();
        var estrategia = contextoEstrategia.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaccion = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var original = await db.EContaxAsientosContables
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.IdAsiento == idAsiento && x.IdEmpresa == actor.IdEmpresa)
                ?? throw new InvalidOperationException("Asiento contable no encontrado.");

            if (EContaxActivosFijosService.EsOrigenGestionado(original.ModuloOrigen))
                throw new InvalidOperationException("Revierte este asiento desde su módulo de origen.");

            if (original.Estado == "REVERSADO")
                throw new InvalidOperationException("Este asiento ya fue reversado.");
            if (original.Estado == "ANULADO")
                throw new InvalidOperationException("No se puede reversar un asiento anulado.");

            var reverso = new EContaxAsientoContable
            {
                Fecha = (fechaReverso ?? DateTime.Today).Date,
                TipoAsiento = "REVERSO",
                IdSucursal = original.IdSucursal,
                ModuloOrigen = original.ModuloOrigen,
                IdDocumentoOrigen = original.IdDocumentoOrigen,
                ReferenciaDocumento = $"REV-{original.NumeroAsiento}",
                Glosa = string.IsNullOrWhiteSpace(motivo)
                    ? $"Reverso del asiento {original.NumeroAsiento}: {original.Glosa}"
                    : $"Reverso {original.NumeroAsiento} ({motivo.Trim()})",
                Estado = "CONTABILIZADO",
                Detalles = original.Detalles.OrderBy(d => d.Linea).Select(d => new EContaxAsientoDetalle
                {
                    IdCuenta = d.IdCuenta,
                    CodigoCuenta = d.CodigoCuenta,
                    NombreCuenta = d.NombreCuenta,
                    Detalle = $"Reverso: {d.Detalle}",
                    Debe = d.Haber,
                    Haber = d.Debe,
                    TipoTercero = d.TipoTercero,
                    IdTercero = d.IdTercero,
                    IdentificacionTercero = d.IdentificacionTercero,
                    NombreTercero = d.NombreTercero,
                    CentroCosto = d.CentroCosto
                }).ToList()
            };

            var creado = await CrearAsientoInternoAsync(db, actor, reverso);
            original.Estado = "REVERSADO";
            original.IdAsientoReverso = creado.IdAsiento;
            original.ActualizadoPor = actor.IdUsuario;
            original.FechaActualizacion = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await transaccion.CommitAsync();
            return creado;
        });
    }

    internal async Task<EContaxAsientoContable> CrearAsientoInternoAsync(AppDbContext db, EContaxUserContext actor, EContaxAsientoContable dto)
    {
        await EnsurePlanCuentasEmpresaAsync(db, actor.IdEmpresa);

        ValidarYResolverDetalles(db, actor.IdEmpresa, dto.Detalles, out var totalDebe, out var totalHaber);

        var prefijo = $"ASI-{dto.Fecha:yyyyMM}-";
        var ultimo = await db.EContaxAsientosContables.AsNoTracking()
            .Where(x => x.IdEmpresa == actor.IdEmpresa && x.NumeroAsiento.StartsWith(prefijo))
            .OrderByDescending(x => x.NumeroAsiento)
            .Select(x => x.NumeroAsiento)
            .FirstOrDefaultAsync();

        var sec = 1;
        if (!string.IsNullOrWhiteSpace(ultimo) && int.TryParse(ultimo[prefijo.Length..], out var parsedSec))
            sec = parsedSec + 1;

        var entity = new EContaxAsientoContable
        {
            IdEmpresa = actor.IdEmpresa,
            IdSucursal = dto.IdSucursal ?? actor.IdSucursal,
            NumeroAsiento = $"{prefijo}{sec:D5}",
            Fecha = dto.Fecha == default ? DateTime.Today : dto.Fecha.Date,
            TipoAsiento = string.IsNullOrWhiteSpace(dto.TipoAsiento) ? "DIARIO" : dto.TipoAsiento.Trim().ToUpperInvariant(),
            ModuloOrigen = string.IsNullOrWhiteSpace(dto.ModuloOrigen) ? "MANUAL" : dto.ModuloOrigen.Trim().ToUpperInvariant(),
            IdDocumentoOrigen = dto.IdDocumentoOrigen,
            ReferenciaDocumento = dto.ReferenciaDocumento?.Trim(),
            Glosa = string.IsNullOrWhiteSpace(dto.Glosa) ? "Asiento contable" : dto.Glosa.Trim(),
            TotalDebe = totalDebe,
            TotalHaber = totalHaber,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "CONTABILIZADO" : dto.Estado.Trim().ToUpperInvariant(),
            CreadoPor = actor.IdUsuario,
            FechaCreacion = DateTime.UtcNow
        };

        var linea = 1;
        foreach (var d in dto.Detalles.Where(x => x.Debe > 0 || x.Haber > 0))
        {
            entity.Detalles.Add(new EContaxAsientoDetalle
            {
                IdCuenta = d.IdCuenta,
                CodigoCuenta = d.CodigoCuenta,
                NombreCuenta = d.NombreCuenta,
                Linea = linea++,
                Detalle = d.Detalle,
                Debe = Math.Round(d.Debe, 2),
                Haber = Math.Round(d.Haber, 2),
                TipoTercero = d.TipoTercero,
                IdTercero = d.IdTercero,
                IdentificacionTercero = d.IdentificacionTercero,
                NombreTercero = d.NombreTercero,
                CentroCosto = d.CentroCosto
            });
        }

        db.EContaxAsientosContables.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    private static void ValidarYResolverDetalles(
        AppDbContext db,
        int idEmpresa,
        List<EContaxAsientoDetalle> detalles,
        out decimal totalDebe,
        out decimal totalHaber)
    {
        var activos = detalles.Where(x => x.Debe > 0 || x.Haber > 0).ToList();
        if (activos.Count < 2)
            throw new InvalidOperationException("El asiento contable debe tener al menos dos líneas con valores.");

        var codigos = activos.Select(x => x.CodigoCuenta).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var ids = activos.Where(x => x.IdCuenta > 0).Select(x => x.IdCuenta).Distinct().ToList();

        var cuentas = db.EContaxCuentasContables.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa && (ids.Contains(x.IdCuenta) || codigos.Contains(x.Codigo)))
            .ToList();

        foreach (var d in activos)
        {
            if (d.Debe < 0 || d.Haber < 0 || (d.Debe > 0 && d.Haber > 0))
                throw new InvalidOperationException("Cada línea debe tener valor únicamente en Debe o en Haber, y no valores negativos.");

            var cta = (d.IdCuenta > 0
                ? cuentas.FirstOrDefault(x => x.IdCuenta == d.IdCuenta)
                : cuentas.FirstOrDefault(x => string.Equals(x.Codigo, d.CodigoCuenta?.Trim(), StringComparison.OrdinalIgnoreCase)))
                ?? throw new InvalidOperationException($"Cuenta contable no válida: {d.CodigoCuenta} ({d.IdCuenta}).");

            d.IdCuenta = cta.IdCuenta;
            d.CodigoCuenta = cta.Codigo;
            d.NombreCuenta = cta.Nombre;
        }

        totalDebe = Math.Round(activos.Sum(x => x.Debe), 2);
        totalHaber = Math.Round(activos.Sum(x => x.Haber), 2);
        if (totalDebe <= 0 || totalDebe != totalHaber)
            throw new InvalidOperationException($"El asiento no cuadra: Debe = {totalDebe:N2}, Haber = {totalHaber:N2}.");
    }

    private async Task<EContaxCuentaContable> ResolverCuentaConfigAsync(AppDbContext db, int idEmpresa, string clave, string fallbackCodigo)
    {
        var cod = await db.EContaxConfigCuentasContables.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa && x.Clave == clave)
            .Select(x => x.CodigoCuenta)
            .FirstOrDefaultAsync() ?? fallbackCodigo;

        var cta = await db.EContaxCuentasContables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.Codigo == cod)
            ?? await db.EContaxCuentasContables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.Codigo == fallbackCodigo)
            ?? await db.EContaxCuentasContables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.EsMovimiento)
            ?? throw new InvalidOperationException($"No se encontró la cuenta contable configurada para '{clave}'.");

        return cta;
    }

    // ==================== GENERADORES AUTOMÁTICOS DE ASIENTOS ====================

    public async Task<EContaxAsientoContable?> RegistrarAsientoFacturaVentaAsync(
        int userId,
        int? idDocumento,
        string referencia,
        DateTime fecha,
        decimal subtotal,
        decimal descuento,
        decimal iva,
        decimal total,
        bool esContado,
        string? identificacionCliente = null,
        string? nombreCliente = null)
    {
        if (total <= 0) return null;
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ctaActivo = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, esContado ? "CAJA_GENERAL" : "CLIENTES_CXC", esContado ? "1.1.1.1.1" : "1.1.2.5.1");
        var ctaVentas = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "VENTAS_BIENES", "4.1.1");
        var ctaIva = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "IVA_VENTAS", "2.1.7.4.1");

        var detalles = new List<EContaxAsientoDetalle>
        {
            new() { IdCuenta = ctaActivo.IdCuenta, CodigoCuenta = ctaActivo.Codigo, NombreCuenta = ctaActivo.Nombre, Detalle = $"Venta {referencia}", Debe = Math.Round(total, 2), TipoTercero = "CLIENTE", IdentificacionTercero = identificacionCliente, NombreTercero = nombreCliente },
        };
        var baseVenta = Math.Round(total - iva, 2);
        if (baseVenta > 0)
            detalles.Add(new() { IdCuenta = ctaVentas.IdCuenta, CodigoCuenta = ctaVentas.Codigo, NombreCuenta = ctaVentas.Nombre, Detalle = $"Ingreso venta {referencia}", Haber = baseVenta });
        if (iva > 0)
            detalles.Add(new() { IdCuenta = ctaIva.IdCuenta, CodigoCuenta = ctaIva.Codigo, NombreCuenta = ctaIva.Nombre, Detalle = $"IVA ventas {referencia}", Haber = Math.Round(iva, 2) });

        return await CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = fecha,
            TipoAsiento = esContado ? "INGRESO" : "DIARIO",
            ModuloOrigen = "FACTURACION",
            IdDocumentoOrigen = idDocumento,
            ReferenciaDocumento = referencia,
            Glosa = $"Factura de venta {referencia} - {nombreCliente ?? "Consumidor Final"}",
            Detalles = detalles
        });
    }

    public async Task<EContaxAsientoContable?> RegistrarAsientoNotaCreditoAsync(
        int userId,
        int? idDocumento,
        string referencia,
        DateTime fecha,
        decimal subtotal,
        decimal iva,
        decimal total,
        string? identificacionCliente = null,
        string? nombreCliente = null)
    {
        if (total <= 0) return null;
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ctaDev = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "DEVOLUCIONES_VENTAS", "4.1.12");
        var ctaIva = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "IVA_VENTAS", "2.1.7.4.1");
        var ctaCxc = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "CLIENTES_CXC", "1.1.2.5.1");

        var baseNc = Math.Round(total - iva, 2);
        var detalles = new List<EContaxAsientoDetalle>();
        if (baseNc > 0)
            detalles.Add(new() { IdCuenta = ctaDev.IdCuenta, CodigoCuenta = ctaDev.Codigo, NombreCuenta = ctaDev.Nombre, Detalle = $"NC {referencia}", Debe = baseNc });
        if (iva > 0)
            detalles.Add(new() { IdCuenta = ctaIva.IdCuenta, CodigoCuenta = ctaIva.Codigo, NombreCuenta = ctaIva.Nombre, Detalle = $"Reverso IVA NC {referencia}", Debe = Math.Round(iva, 2) });
        detalles.Add(new() { IdCuenta = ctaCxc.IdCuenta, CodigoCuenta = ctaCxc.Codigo, NombreCuenta = ctaCxc.Nombre, Detalle = $"Crédito cliente NC {referencia}", Haber = Math.Round(total, 2), TipoTercero = "CLIENTE", IdentificacionTercero = identificacionCliente, NombreTercero = nombreCliente });

        return await CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = fecha,
            TipoAsiento = "DIARIO",
            ModuloOrigen = "NOTA_CREDITO",
            IdDocumentoOrigen = idDocumento,
            ReferenciaDocumento = referencia,
            Glosa = $"Nota de Crédito {referencia} - {nombreCliente ?? "Cliente"}",
            Detalles = detalles
        });
    }

    public async Task<EContaxAsientoContable?> RegistrarAsientoNotaDebitoAsync(
        int userId,
        int? idDocumento,
        string referencia,
        DateTime fecha,
        decimal subtotal,
        decimal iva,
        decimal total,
        string? identificacionCliente = null,
        string? nombreCliente = null)
    {
        if (total <= 0) return null;
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ctaCxc = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "CLIENTES_CXC", "1.1.2.5.1");
        var ctaIngreso = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "VENTAS_BIENES", "4.1.1");
        var ctaIva = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "IVA_VENTAS", "2.1.7.4.1");

        var baseNd = Math.Round(total - iva, 2);
        var detalles = new List<EContaxAsientoDetalle>
        {
            new() { IdCuenta = ctaCxc.IdCuenta, CodigoCuenta = ctaCxc.Codigo, NombreCuenta = ctaCxc.Nombre, Detalle = $"ND {referencia}", Debe = Math.Round(total, 2), TipoTercero = "CLIENTE", IdentificacionTercero = identificacionCliente, NombreTercero = nombreCliente }
        };
        if (baseNd > 0)
            detalles.Add(new() { IdCuenta = ctaIngreso.IdCuenta, CodigoCuenta = ctaIngreso.Codigo, NombreCuenta = ctaIngreso.Nombre, Detalle = $"Cargo ND {referencia}", Haber = baseNd });
        if (iva > 0)
            detalles.Add(new() { IdCuenta = ctaIva.IdCuenta, CodigoCuenta = ctaIva.Codigo, NombreCuenta = ctaIva.Nombre, Detalle = $"IVA ND {referencia}", Haber = Math.Round(iva, 2) });

        return await CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = fecha,
            TipoAsiento = "DIARIO",
            ModuloOrigen = "NOTA_DEBITO",
            IdDocumentoOrigen = idDocumento,
            ReferenciaDocumento = referencia,
            Glosa = $"Nota de Débito {referencia} - {nombreCliente ?? "Cliente"}",
            Detalles = detalles
        });
    }

    public async Task<EContaxAsientoContable?> RegistrarAsientoCompraAsync(
        int userId,
        string moduloOrigen,
        int? idDocumento,
        string referencia,
        DateTime fecha,
        decimal subtotal,
        decimal iva,
        decimal total,
        bool esInventario = false,
        string? identificacionProveedor = null,
        string? nombreProveedor = null)
    {
        if (total <= 0) return null;
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ctaDebito = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, esInventario ? "INVENTARIO_MERCADERIA" : "GASTOS_COMPRAS_DEFAULT", esInventario ? "1.1.3.6" : "5.2.1.2.73");
        var ctaIva = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "IVA_COMPRAS", "1.1.5.1.1");
        var ctaCxp = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "PROVEEDORES_CXP", "2.1.3.1.1");

        var baseCompra = Math.Round(total - iva, 2);
        var detalles = new List<EContaxAsientoDetalle>();
        if (baseCompra > 0)
            detalles.Add(new() { IdCuenta = ctaDebito.IdCuenta, CodigoCuenta = ctaDebito.Codigo, NombreCuenta = ctaDebito.Nombre, Detalle = $"Compra {referencia}", Debe = baseCompra });
        if (iva > 0)
            detalles.Add(new() { IdCuenta = ctaIva.IdCuenta, CodigoCuenta = ctaIva.Codigo, NombreCuenta = ctaIva.Nombre, Detalle = $"IVA compra {referencia}", Debe = Math.Round(iva, 2) });
        detalles.Add(new() { IdCuenta = ctaCxp.IdCuenta, CodigoCuenta = ctaCxp.Codigo, NombreCuenta = ctaCxp.Nombre, Detalle = $"CxP {referencia}", Haber = Math.Round(total, 2), TipoTercero = "PROVEEDOR", IdentificacionTercero = identificacionProveedor, NombreTercero = nombreProveedor });

        return await CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = fecha,
            TipoAsiento = "DIARIO",
            ModuloOrigen = moduloOrigen,
            IdDocumentoOrigen = idDocumento,
            ReferenciaDocumento = referencia,
            Glosa = $"{(moduloOrigen == "LIQUIDACION_COMPRA" ? "Liquidación de compra" : "Compra")} {referencia} - {nombreProveedor ?? "Proveedor"}",
            Detalles = detalles
        });
    }

    public async Task<EContaxAsientoContable?> RegistrarAsientoRetencionAsync(
        int userId,
        int? idDocumento,
        string referencia,
        DateTime fecha,
        decimal valorRetencionRenta,
        decimal valorRetencionIva,
        string? identificacionProveedor = null,
        string? nombreProveedor = null)
    {
        var total = Math.Round(valorRetencionRenta + valorRetencionIva, 2);
        if (total <= 0) return null;
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ctaCxp = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "PROVEEDORES_CXP", "2.1.3.1.1");
        var ctaRetRenta = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "RET_RENTA_PAGAR", "2.1.7.2");
        var ctaRetIva = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "RET_IVA_PAGAR", "2.1.7.3");

        var detalles = new List<EContaxAsientoDetalle>
        {
            new() { IdCuenta = ctaCxp.IdCuenta, CodigoCuenta = ctaCxp.Codigo, NombreCuenta = ctaCxp.Nombre, Detalle = $"Retención aplicada {referencia}", Debe = total, TipoTercero = "PROVEEDOR", IdentificacionTercero = identificacionProveedor, NombreTercero = nombreProveedor }
        };
        if (valorRetencionRenta > 0)
            detalles.Add(new() { IdCuenta = ctaRetRenta.IdCuenta, CodigoCuenta = ctaRetRenta.Codigo, NombreCuenta = ctaRetRenta.Nombre, Detalle = $"Ret. Fuente {referencia}", Haber = Math.Round(valorRetencionRenta, 2) });
        if (valorRetencionIva > 0)
            detalles.Add(new() { IdCuenta = ctaRetIva.IdCuenta, CodigoCuenta = ctaRetIva.Codigo, NombreCuenta = ctaRetIva.Nombre, Detalle = $"Ret. IVA {referencia}", Haber = Math.Round(valorRetencionIva, 2) });

        return await CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = fecha,
            TipoAsiento = "DIARIO",
            ModuloOrigen = "RETENCION",
            IdDocumentoOrigen = idDocumento,
            ReferenciaDocumento = referencia,
            Glosa = $"Comprobante de Retención {referencia} - {nombreProveedor ?? "Proveedor"}",
            Detalles = detalles
        });
    }

    public async Task<EContaxAsientoContable?> RegistrarAsientoCobroAsync(
        int userId,
        int? idDocumento,
        string referencia,
        DateTime fecha,
        decimal monto,
        string? codigoCuentaCajaOBanco = null,
        string? identificacionCliente = null,
        string? nombreCliente = null)
    {
        if (monto <= 0) return null;
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ctaCajaBanco = !string.IsNullOrWhiteSpace(codigoCuentaCajaOBanco)
            ? await db.EContaxCuentasContables.AsNoTracking().FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == codigoCuentaCajaOBanco)
              ?? await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "CAJA_GENERAL", "1.1.1.1.1")
            : await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "CAJA_GENERAL", "1.1.1.1.1");
        var ctaCxc = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "CLIENTES_CXC", "1.1.2.5.1");

        return await CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = fecha,
            TipoAsiento = "INGRESO",
            ModuloOrigen = "COBRO",
            IdDocumentoOrigen = idDocumento,
            ReferenciaDocumento = referencia,
            Glosa = $"Cobro / Abono {referencia} - {nombreCliente ?? "Cliente"}",
            Detalles =
            [
                new() { IdCuenta = ctaCajaBanco.IdCuenta, CodigoCuenta = ctaCajaBanco.Codigo, NombreCuenta = ctaCajaBanco.Nombre, Detalle = $"Ingreso cobro {referencia}", Debe = Math.Round(monto, 2) },
                new() { IdCuenta = ctaCxc.IdCuenta, CodigoCuenta = ctaCxc.Codigo, NombreCuenta = ctaCxc.Nombre, Detalle = $"Abono CxC {referencia}", Haber = Math.Round(monto, 2), TipoTercero = "CLIENTE", IdentificacionTercero = identificacionCliente, NombreTercero = nombreCliente }
            ]
        });
    }

    public async Task<EContaxAsientoContable?> RegistrarAsientoPagoAsync(
        int userId,
        int? idDocumento,
        string referencia,
        DateTime fecha,
        decimal monto,
        string? codigoCuentaCajaOBanco = null,
        string? identificacionProveedor = null,
        string? nombreProveedor = null)
    {
        if (monto <= 0) return null;
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ctaCxp = await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "PROVEEDORES_CXP", "2.1.3.1.1");
        var ctaCajaBanco = !string.IsNullOrWhiteSpace(codigoCuentaCajaOBanco)
            ? await db.EContaxCuentasContables.AsNoTracking().FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == codigoCuentaCajaOBanco)
              ?? await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "BANCOS_DEFAULT", "1.1.1.3.1")
            : await ResolverCuentaConfigAsync(db, actor.IdEmpresa, "BANCOS_DEFAULT", "1.1.1.3.1");

        return await CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = fecha,
            TipoAsiento = "EGRESO",
            ModuloOrigen = "PAGO",
            IdDocumentoOrigen = idDocumento,
            ReferenciaDocumento = referencia,
            Glosa = $"Pago a proveedor {referencia} - {nombreProveedor ?? "Proveedor"}",
            Detalles =
            [
                new() { IdCuenta = ctaCxp.IdCuenta, CodigoCuenta = ctaCxp.Codigo, NombreCuenta = ctaCxp.Nombre, Detalle = $"Pago CxP {referencia}", Debe = Math.Round(monto, 2), TipoTercero = "PROVEEDOR", IdentificacionTercero = identificacionProveedor, NombreTercero = nombreProveedor },
                new() { IdCuenta = ctaCajaBanco.IdCuenta, CodigoCuenta = ctaCajaBanco.Codigo, NombreCuenta = ctaCajaBanco.Nombre, Detalle = $"Salida pago {referencia}", Haber = Math.Round(monto, 2) }
            ]
        });
    }

    // ==================== REPORTES CONTABLES ====================

    public sealed record LibroMayorMovimientoDto(DateTime Fecha, string NumeroAsiento, string Glosa, string? Detalle, decimal Debe, decimal Haber, decimal SaldoAcumulado);
    public sealed record LibroMayorResultDto(EContaxCuentaContable Cuenta, decimal SaldoInicial, List<LibroMayorMovimientoDto> Movimientos, decimal TotalDebe, decimal TotalHaber, decimal SaldoFinal);

    public async Task<LibroMayorResultDto> ObtenerLibroMayorAsync(int userId, int idCuenta, DateTime desde, DateTime hasta)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var cuenta = await db.EContaxCuentasContables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdCuenta == idCuenta && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Cuenta contable no encontrada.");

        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var previos = await (from d in db.EContaxAsientosDetalles.AsNoTracking()
                             join a in db.EContaxAsientosContables.AsNoTracking() on d.IdAsiento equals a.IdAsiento
                             where a.IdEmpresa == actor.IdEmpresa && a.Estado != "ANULADO" && d.IdCuenta == idCuenta && a.Fecha < inicio
                             select new { d.Debe, d.Haber }).ToListAsync();

        decimal saldoInicial = cuenta.Naturaleza == "DEUDORA"
            ? previos.Sum(x => x.Debe - x.Haber)
            : previos.Sum(x => x.Haber - x.Debe);

        var movsRaw = await (from d in db.EContaxAsientosDetalles.AsNoTracking()
                             join a in db.EContaxAsientosContables.AsNoTracking() on d.IdAsiento equals a.IdAsiento
                             where a.IdEmpresa == actor.IdEmpresa && a.Estado != "ANULADO" && d.IdCuenta == idCuenta && a.Fecha >= inicio && a.Fecha < fin
                             orderby a.Fecha, a.IdAsiento, d.Linea
                             select new { a.Fecha, a.NumeroAsiento, a.Glosa, d.Detalle, d.Debe, d.Haber }).ToListAsync();

        var saldo = saldoInicial;
        var movs = new List<LibroMayorMovimientoDto>(movsRaw.Count);
        foreach (var m in movsRaw)
        {
            saldo += cuenta.Naturaleza == "DEUDORA" ? (m.Debe - m.Haber) : (m.Haber - m.Debe);
            movs.Add(new LibroMayorMovimientoDto(m.Fecha, m.NumeroAsiento, m.Glosa, m.Detalle, m.Debe, m.Haber, saldo));
        }

        return new LibroMayorResultDto(cuenta, saldoInicial, movs, movs.Sum(x => x.Debe), movs.Sum(x => x.Haber), saldo);
    }

    public sealed record BalanceComprobacionRowDto(int IdCuenta, string Codigo, string Nombre, string TipoCuenta, decimal SumasDebe, decimal SumasHaber, decimal SaldoDeudor, decimal SaldoAcreedor);

    public async Task<List<BalanceComprobacionRowDto>> ObtenerBalanceComprobacionAsync(int userId, DateTime desde, DateTime hasta)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var rows = await (from d in db.EContaxAsientosDetalles.AsNoTracking()
                          join a in db.EContaxAsientosContables.AsNoTracking() on d.IdAsiento equals a.IdAsiento
                          join c in db.EContaxCuentasContables.AsNoTracking() on d.IdCuenta equals c.IdCuenta
                          where a.IdEmpresa == actor.IdEmpresa && a.Estado != "ANULADO" && a.Fecha >= inicio && a.Fecha < fin
                          group new { d.Debe, d.Haber } by new { c.IdCuenta, c.Codigo, c.Nombre, c.TipoCuenta } into g
                          orderby g.Key.Codigo
                          select new
                          {
                              g.Key.IdCuenta,
                              g.Key.Codigo,
                              g.Key.Nombre,
                              g.Key.TipoCuenta,
                              Debe = g.Sum(x => x.Debe),
                              Haber = g.Sum(x => x.Haber)
                          }).ToListAsync();

        return rows.Select(r => new BalanceComprobacionRowDto(
            r.IdCuenta,
            r.Codigo,
            r.Nombre,
            r.TipoCuenta,
            r.Debe,
            r.Haber,
            r.Debe > r.Haber ? r.Debe - r.Haber : 0m,
            r.Haber > r.Debe ? r.Haber - r.Debe : 0m)).ToList();
    }

    public sealed record EstadoFinancieroRowDto(string Codigo, string Nombre, string TipoCuenta, decimal Saldo);
    public sealed record BalanceGeneralResultDto(List<EstadoFinancieroRowDto> Activos, List<EstadoFinancieroRowDto> Pasivos, List<EstadoFinancieroRowDto> Patrimonio, decimal ResultadoEjercicio, decimal TotalActivos, decimal TotalPasivos, decimal TotalPatrimonio);

    public async Task<BalanceGeneralResultDto> ObtenerBalanceGeneralAsync(int userId, DateTime fechaCorte)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fin = fechaCorte.Date.AddDays(1);

        var saldos = await (from d in db.EContaxAsientosDetalles.AsNoTracking()
                            join a in db.EContaxAsientosContables.AsNoTracking() on d.IdAsiento equals a.IdAsiento
                            join c in db.EContaxCuentasContables.AsNoTracking() on d.IdCuenta equals c.IdCuenta
                            where a.IdEmpresa == actor.IdEmpresa && a.Estado != "ANULADO" && a.Fecha < fin
                            group new { d.Debe, d.Haber } by new { c.Codigo, c.Nombre, c.TipoCuenta, c.Naturaleza } into g
                            orderby g.Key.Codigo
                            select new
                            {
                                g.Key.Codigo,
                                g.Key.Nombre,
                                g.Key.TipoCuenta,
                                g.Key.Naturaleza,
                                Debe = g.Sum(x => x.Debe),
                                Haber = g.Sum(x => x.Haber)
                            }).ToListAsync();

        var activos = new List<EstadoFinancieroRowDto>();
        var pasivos = new List<EstadoFinancieroRowDto>();
        var patrimonio = new List<EstadoFinancieroRowDto>();
        decimal ingresos = 0m, gastos = 0m;

        foreach (var s in saldos)
        {
            var neto = s.Naturaleza == "DEUDORA" ? (s.Debe - s.Haber) : (s.Haber - s.Debe);
            if (neto == 0) continue;

            switch (s.TipoCuenta)
            {
                case "ACTIVO": activos.Add(new(s.Codigo, s.Nombre, s.TipoCuenta, neto)); break;
                case "PASIVO": pasivos.Add(new(s.Codigo, s.Nombre, s.TipoCuenta, neto)); break;
                case "PATRIMONIO": patrimonio.Add(new(s.Codigo, s.Nombre, s.TipoCuenta, neto)); break;
                case "INGRESO": ingresos += neto; break;
                case "GASTO": gastos += neto; break;
            }
        }

        var resultado = ingresos - gastos;
        var totalActivos = activos.Sum(x => x.Saldo);
        var totalPasivos = pasivos.Sum(x => x.Saldo);
        var totalPatrimonio = patrimonio.Sum(x => x.Saldo) + resultado;

        return new BalanceGeneralResultDto(activos, pasivos, patrimonio, resultado, totalActivos, totalPasivos, totalPatrimonio);
    }

    public sealed record EstadoResultadosResultDto(List<EstadoFinancieroRowDto> Ingresos, List<EstadoFinancieroRowDto> Costos, List<EstadoFinancieroRowDto> Gastos, decimal TotalIngresos, decimal TotalCostos, decimal UtilidadBruta, decimal TotalGastos, decimal UtilidadNeta);

    public async Task<EstadoResultadosResultDto> ObtenerEstadoResultadosAsync(int userId, DateTime desde, DateTime hasta)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var saldos = await (from d in db.EContaxAsientosDetalles.AsNoTracking()
                            join a in db.EContaxAsientosContables.AsNoTracking() on d.IdAsiento equals a.IdAsiento
                            join c in db.EContaxCuentasContables.AsNoTracking() on d.IdCuenta equals c.IdCuenta
                            where a.IdEmpresa == actor.IdEmpresa && a.Estado != "ANULADO" && a.Fecha >= inicio && a.Fecha < fin
                                  && (c.TipoCuenta == "INGRESO" || c.TipoCuenta == "GASTO")
                            group new { d.Debe, d.Haber } by new { c.Codigo, c.Nombre, c.TipoCuenta, c.Naturaleza } into g
                            orderby g.Key.Codigo
                            select new
                            {
                                g.Key.Codigo,
                                g.Key.Nombre,
                                g.Key.TipoCuenta,
                                g.Key.Naturaleza,
                                Debe = g.Sum(x => x.Debe),
                                Haber = g.Sum(x => x.Haber)
                            }).ToListAsync();

        var ingresos = new List<EstadoFinancieroRowDto>();
        var costos = new List<EstadoFinancieroRowDto>();
        var gastos = new List<EstadoFinancieroRowDto>();

        foreach (var s in saldos)
        {
            var neto = s.Naturaleza == "ACREEDORA" ? (s.Haber - s.Debe) : (s.Debe - s.Haber);
            if (neto == 0) continue;

            if (s.TipoCuenta == "INGRESO")
                ingresos.Add(new(s.Codigo, s.Nombre, s.TipoCuenta, neto));
            else if (s.Codigo.StartsWith("5.1"))
                costos.Add(new(s.Codigo, s.Nombre, "COSTO", neto));
            else
                gastos.Add(new(s.Codigo, s.Nombre, "GASTO", neto));
        }

        var totIng = ingresos.Sum(x => x.Saldo);
        var totCos = costos.Sum(x => x.Saldo);
        var totGas = gastos.Sum(x => x.Saldo);
        var utilBruta = totIng - totCos;
        return new EstadoResultadosResultDto(ingresos, costos, gastos, totIng, totCos, utilBruta, totGas, utilBruta - totGas);
    }

    public sealed record FlujoEfectivoItemDto(string Concepto, string Categoria, decimal Monto);
    public sealed record FlujoEfectivoResultDto(decimal EfectivoInicial, List<FlujoEfectivoItemDto> Operacion, List<FlujoEfectivoItemDto> Inversion, List<FlujoEfectivoItemDto> Financiamiento, decimal FlujoNetoOperacion, decimal FlujoNetoInversion, decimal FlujoNetoFinanciamiento, decimal VariacionNeta, decimal EfectivoFinal);

    public async Task<FlujoEfectivoResultDto> ObtenerFlujoEfectivoAsync(int userId, DateTime desde, DateTime hasta)
    {
        var actor = await EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var efectivoInicial = await (from d in db.EContaxAsientosDetalles.AsNoTracking()
                                     join a in db.EContaxAsientosContables.AsNoTracking() on d.IdAsiento equals a.IdAsiento
                                     join c in db.EContaxCuentasContables.AsNoTracking() on d.IdCuenta equals c.IdCuenta
                                     where a.IdEmpresa == actor.IdEmpresa && a.Estado != "ANULADO" && a.Fecha < inicio && c.ClasificacionFlujo == "EFECTIVO"
                                     select d.Debe - d.Haber).SumAsync();

        var asientosConEfectivo = await (from a in db.EContaxAsientosContables.AsNoTracking()
                                         where a.IdEmpresa == actor.IdEmpresa && a.Estado != "ANULADO" && a.Fecha >= inicio && a.Fecha < fin
                                         select a.IdAsiento).ToListAsync();

        var detalles = await (from d in db.EContaxAsientosDetalles.AsNoTracking()
                              join c in db.EContaxCuentasContables.AsNoTracking() on d.IdCuenta equals c.IdCuenta
                              where asientosConEfectivo.Contains(d.IdAsiento)
                              select new { d.IdAsiento, c.Codigo, c.Nombre, c.ClasificacionFlujo, d.Debe, d.Haber }).ToListAsync();

        var porAsiento = detalles.GroupBy(x => x.IdAsiento);
        var dictOp = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var dictInv = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var dictFin = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var grp in porAsiento)
        {
            var flujoEfectivo = grp.Where(x => x.ClasificacionFlujo == "EFECTIVO").Sum(x => x.Debe - x.Haber);
            if (flujoEfectivo == 0) continue;

            var contrapartida = grp.FirstOrDefault(x => x.ClasificacionFlujo != "EFECTIVO");
            var cat = contrapartida?.ClasificacionFlujo ?? "OPERACION";
            var label = contrapartida != null ? $"{contrapartida.Codigo} - {contrapartida.Nombre}" : "Movimientos varios de efectivo";

            var target = cat switch
            {
                "INVERSION" => dictInv,
                "FINANCIAMIENTO" => dictFin,
                _ => dictOp
            };
            target[label] = target.GetValueOrDefault(label) + flujoEfectivo;
        }

        var op = dictOp.Select(x => new FlujoEfectivoItemDto(x.Key, "OPERACION", x.Value)).OrderByDescending(x => Math.Abs(x.Monto)).ToList();
        var inv = dictInv.Select(x => new FlujoEfectivoItemDto(x.Key, "INVERSION", x.Value)).OrderByDescending(x => Math.Abs(x.Monto)).ToList();
        var finList = dictFin.Select(x => new FlujoEfectivoItemDto(x.Key, "FINANCIAMIENTO", x.Value)).OrderByDescending(x => Math.Abs(x.Monto)).ToList();

        var netOp = op.Sum(x => x.Monto);
        var netInv = inv.Sum(x => x.Monto);
        var netFin = finList.Sum(x => x.Monto);
        var variacion = netOp + netInv + netFin;

        return new FlujoEfectivoResultDto(efectivoInicial, op, inv, finList, netOp, netInv, netFin, variacion, efectivoInicial + variacion);
    }
}
