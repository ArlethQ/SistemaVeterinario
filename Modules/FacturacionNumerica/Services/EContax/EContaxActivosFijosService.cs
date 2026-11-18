using System.Data;
using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.Models.EContax;

namespace Simetric.Services.EContax;

public sealed record EContaxAfSaldo(EContaxAfActivo Activo, decimal Costo, decimal Depreciacion,
    string Ubicacion, string Responsable, bool Baja)
{
    public decimal ValorLibros => Costo - Depreciacion;
}

public sealed record EContaxAfDetalleResumen(string Codigo, string Nombre, decimal Importe);

public sealed record EContaxAfLoteResumen(EContaxAfDepreciacionLote Lote, int Activos, decimal Total);

public class EContaxActivosFijosService
{
    internal const string Origen = "ACTIVOS_FIJOS";
    internal const string OrigenAmortizacion = "AMORTIZACION_INTANGIBLES";
    private readonly IDbContextFactory<AppDbContext> factory;
    private readonly EContaxSeguridadService seguridad;
    private readonly EContaxContabilidadService contabilidad;
    private readonly bool intangibles;

    public EContaxActivosFijosService(IDbContextFactory<AppDbContext> factory,
        EContaxSeguridadService seguridad, EContaxContabilidadService contabilidad)
        : this(factory, seguridad, contabilidad, false) { }

    protected EContaxActivosFijosService(IDbContextFactory<AppDbContext> factory,
        EContaxSeguridadService seguridad, EContaxContabilidadService contabilidad, bool intangibles)
    {
        this.factory = factory;
        this.seguridad = seguridad;
        this.contabilidad = contabilidad;
        this.intangibles = intangibles;
    }

    private string CategoriasRuta => intangibles ? EContaxRoutes.AmCategorias : EContaxRoutes.AfCategorias;
    private string RegistroRuta => intangibles ? EContaxRoutes.AmActivos : EContaxRoutes.AfActivos;
    private string MovimientosRuta => intangibles ? EContaxRoutes.AmMovimientos : EContaxRoutes.AfMovimientos;
    private string LotesRuta => intangibles ? EContaxRoutes.AmDepreciacion : EContaxRoutes.AfDepreciacion;
    private string ReportesRuta => intangibles ? EContaxRoutes.AmReportes : EContaxRoutes.AfReportes;
    private string OrigenContable => intangibles ? OrigenAmortizacion : Origen;
    private string CalculoMensual => intangibles ? "amortización" : "depreciación";
    private string NombreModulo => intangibles ? "Intangibles" : "Activos Fijos";

    internal static bool EsOrigenGestionado(string? origen) =>
        string.Equals(origen, Origen, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(origen, OrigenAmortizacion, StringComparison.OrdinalIgnoreCase);

    internal static void ConfigurarEntidades(ModelBuilder model)
    {
        Configurar<EContaxAfCategoria>(model, "CATEGORIA");
        Configurar<EContaxAfActivo>(model, "ACTIVO");
        Configurar<EContaxAfMovimiento>(model, "MOVIMIENTO");
        Configurar<EContaxAfDepreciacionLote>(model, "DEPRECIACION_LOTE");
        Configurar<EContaxAfDepreciacionDetalle>(model, "DEPRECIACION_DETALLE");
    }

    private static void Configurar<T>(ModelBuilder model, string sufijo) where T : class
    {
        var tabla = "ECONTAX_AF_" + sufijo;
        model.SharedTypeEntity<T>(NombreEntidad<T>(false)).ToTable(tabla, "dbo");
        model.SharedTypeEntity<T>(NombreEntidad<T>(true)).ToTable(EContaxActivosFijosSchema.TablaIntangible(tabla), "dbo");
    }

    private static string NombreEntidad<T>(bool intangible) => (intangible ? "Am." : "Af.") + typeof(T).Name;

    private DbSet<T> Tabla<T>(AppDbContext db) where T : class => db.Set<T>(NombreEntidad<T>(intangibles));
    public static DateTime Hoy => DateTime.UtcNow.AddHours(-5).Date;

    public async Task<List<EContaxAfCategoria>> CategoriasAsync(int usuario, string ruta)
    {
        var actor = await AutorizarAsync(usuario, ruta);
        await using var db = await AbrirAsync();
        return await Empresa<EContaxAfCategoria>(db, actor.IdEmpresa).OrderBy(x => x.Nombre).ToListAsync();
    }

    public async Task<List<EContaxCuentaContable>> CuentasAsync(int usuario)
    {
        await AutorizarAsync(usuario, CategoriasRuta);
        return await contabilidad.ListarPlanCuentasAsync(usuario, soloMovimiento: true, soloActivas: true);
    }

    public Task GuardarCategoriaAsync(int usuario, EContaxAfCategoria datos) =>
        EscribirAsync(usuario, CategoriasRuta,
            datos.Id == 0 ? EContaxAccion.Crear : EContaxAccion.Editar, async (db, actor) =>
        {
            var nombre = Texto(datos.Nombre, 120, true);
            Validar(datos.VidaUtilMeses >= (intangibles ? 1 : 0) && datos.VidaUtilMeses <= 1200, $"La vida útil debe estar entre {(intangibles ? 1 : 0)} y 1200 meses.");
            var ids = new[] { datos.IdCuentaActivo, datos.IdCuentaGasto, datos.IdCuentaAcumulada };
            var cuentas = await db.EContaxCuentasContables.AsNoTracking().Where(x => x.IdEmpresa == actor.IdEmpresa
                && x.EsMovimiento && x.Activa && ids.Contains(x.IdCuenta)).ToDictionaryAsync(x => x.IdCuenta);
            Validar(cuentas.GetValueOrDefault(datos.IdCuentaActivo)?.TipoCuenta == "ACTIVO",
                "Selecciona una cuenta de activo válida.");
            if (datos.VidaUtilMeses > 0)
            {
                Validar(ids.Distinct().Count() == 3 && cuentas.GetValueOrDefault(datos.IdCuentaAcumulada)?.TipoCuenta == "ACTIVO"
                    && cuentas.GetValueOrDefault(datos.IdCuentaGasto)?.TipoCuenta is "GASTO" or "COSTO",
                    $"Selecciona cuentas distintas de activo, {CalculoMensual} acumulada y gasto o costo.");
            }
            var categorias = Empresa<EContaxAfCategoria>(db, actor.IdEmpresa);
            Validar(!await categorias.AnyAsync(x => x.Nombre == nombre && x.Id != datos.Id),
                "Ya existe una categoría con ese nombre.");
            EContaxAfCategoria categoria;
            if (datos.Id == 0)
            {
                categoria = new() { IdEmpresa = actor.IdEmpresa };
                Tabla<EContaxAfCategoria>(db).Add(categoria);
            }
            else
            {
                categoria = await categorias.AsTracking().SingleOrDefaultAsync(x => x.Id == datos.Id)
                    ?? throw new InvalidOperationException("Categoría no encontrada.");
                if ((categoria.VidaUtilMeses == 0) != (datos.VidaUtilMeses == 0))
                    Validar(!await Empresa<EContaxAfActivo>(db, actor.IdEmpresa).AnyAsync(x => x.IdCategoria == datos.Id),
                        "La categoría ya tiene activos. Crea otra categoría para cambiar si el bien se deprecia.");
            }
            categoria.Nombre = nombre;
            categoria.VidaUtilMeses = datos.VidaUtilMeses;
            categoria.IdCuentaActivo = datos.IdCuentaActivo;
            categoria.IdCuentaGasto = datos.VidaUtilMeses == 0 ? 0 : datos.IdCuentaGasto;
            categoria.IdCuentaAcumulada = datos.VidaUtilMeses == 0 ? 0 : datos.IdCuentaAcumulada;
        });

    public Task CrearActivoAsync(int usuario, EContaxAfActivo datos, DateTime fecha, decimal costo,
        decimal depreciacion, bool saldoInicial, string ubicacion, string responsable) =>
        EscribirAsync(usuario, RegistroRuta, EContaxAccion.Crear, async (db, actor) =>
        {
            fecha = fecha.Date;
            await ValidarFechaAsync(db, actor.IdEmpresa, fecha);
            Validar(saldoInicial ? fecha == FinMes(fecha) : depreciacion == 0,
                $"El saldo inicial requiere fecha de cierre del mes. Si es un alta nueva, usa {CalculoMensual} inicial en cero.");
            Validar(saldoInicial || Mes(datos.PuestaEnServicio) == Mes(fecha),
                "Para bienes de meses anteriores, registra un saldo inicial al cierre del mes.");
            var categoria = await Empresa<EContaxAfCategoria>(db, actor.IdEmpresa)
                .SingleOrDefaultAsync(x => x.Id == datos.IdCategoria)
                ?? throw new InvalidOperationException("Selecciona una categoría de esta empresa.");
            Validar(datos.PuestaEnServicio.Date >= new DateTime(1900, 1, 1)
                && datos.PuestaEnServicio.Date <= fecha, "La puesta en servicio debe ser anterior o igual al alta.");
            var codigo = Texto(datos.Codigo, 40, true).ToUpperInvariant();
            Validar(!await Empresa<EContaxAfActivo>(db, actor.IdEmpresa).AnyAsync(x => x.Codigo == codigo),
                "Ya existe un activo con ese código.");
            costo = Dinero(costo);
            depreciacion = Dinero(depreciacion);
            var residual = Dinero(datos.ValorResidual);
            Validar(costo > 0 && residual >= 0 && depreciacion >= 0 && depreciacion <= costo - residual,
                "Revisa el costo, el valor residual y la depreciación inicial.");
            Validar(categoria.VidaUtilMeses > 0 || depreciacion == 0, "Esta categoría no admite depreciación.");
            var activo = new EContaxAfActivo
            {
                IdEmpresa = actor.IdEmpresa, IdCategoria = categoria.Id, Codigo = codigo,
                Nombre = Texto(datos.Nombre, 180, true), PuestaEnServicio = datos.PuestaEnServicio.Date,
                ValorResidual = residual, VidaUtilMeses = categoria.VidaUtilMeses, Referencia = Texto(datos.Referencia, 160)
            };
            Tabla<EContaxAfActivo>(db).Add(activo);
            await db.SaveChangesAsync();
            Tabla<EContaxAfMovimiento>(db).Add(Movimiento(actor, activo.Id, fecha, saldoInicial ? "SALDO_INICIAL" : "ALTA", costo, depreciacion, ubicacion, responsable, ""));
        });

    public Task RegistrarMovimientoAsync(int usuario, int idActivo, DateTime fecha, string tipo,
        decimal cambioCosto, string ubicacion, string responsable, string motivo)
    {
        var idOperacion = Guid.NewGuid();
        return EscribirAsync(usuario, MovimientosRuta, EContaxAccion.Crear, async (db, actor) =>
        {
            if (await Empresa<EContaxAfMovimiento>(db, actor.IdEmpresa).AnyAsync(x => x.IdOperacion == idOperacion))
                return;
            fecha = fecha.Date;
            await ValidarFechaAsync(db, actor.IdEmpresa, fecha);
            var saldo = (await SaldosAsync(db, actor.IdEmpresa, fecha, idActivo)).SingleOrDefault()
                ?? throw new InvalidOperationException("El activo aún no estaba dado de alta en esa fecha.");
            Validar(!saldo.Baja && !await Empresa<EContaxAfMovimiento>(db, actor.IdEmpresa)
                .AnyAsync(x => x.IdActivo == idActivo && x.Fecha > fecha),
                "El activo está dado de baja o tiene movimientos posteriores.");
            var costo = 0m;
            var depreciacion = 0m;
            switch (tipo)
            {
                case "TRASLADO":
                    break;
                case "AJUSTE":
                    costo = Dinero(cambioCosto);
                    Validar(costo != 0 && Dinero(saldo.Costo + costo) > 0
                        && saldo.ValorLibros + costo >= saldo.Activo.ValorResidual, "El ajuste deja un saldo inválido.");
                    break;
                case "BAJA":
                    costo = -saldo.Costo;
                    depreciacion = -saldo.Depreciacion;
                    break;
                default:
                    throw new InvalidOperationException("El tipo de movimiento no es válido.");
            }
            Tabla<EContaxAfMovimiento>(db).Add(Movimiento(actor, idActivo, fecha, tipo, costo, depreciacion,
                tipo == "TRASLADO" ? Texto(ubicacion, 120, true) : saldo.Ubicacion,
                tipo == "TRASLADO" ? Texto(responsable, 120, true) : saldo.Responsable, Texto(motivo, 250, true),
                idOperacion));
        });
    }

    public async Task<List<EContaxAfSaldo>> ConsultarSaldosAsync(int usuario, DateTime corte, string ruta)
    {
        var actor = await AutorizarAsync(usuario, ruta);
        await using var db = await AbrirAsync();
        return await SaldosAsync(db, actor.IdEmpresa, corte.Date);
    }

    public async Task<List<EContaxAfMovimiento>> HistorialAsync(int usuario, DateTime corte, string ruta, int? idActivo)
    {
        var actor = await AutorizarAsync(usuario, ruta);
        await using var db = await AbrirAsync();
        var query = Empresa<EContaxAfMovimiento>(db, actor.IdEmpresa).Where(x => x.Fecha <= corte.Date);
        if (idActivo.HasValue) query = query.Where(x => x.IdActivo == idActivo.Value);
        return await query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).ToListAsync();
    }

    public async Task<List<EContaxAfLoteResumen>> LotesAsync(int usuario)
    {
        var actor = await AutorizarAsync(usuario, LotesRuta);
        await using var db = await AbrirAsync();
        var detalles = Empresa<EContaxAfDepreciacionDetalle>(db, actor.IdEmpresa);
        return await Empresa<EContaxAfDepreciacionLote>(db, actor.IdEmpresa)
            .OrderByDescending(x => x.Periodo).ThenByDescending(x => x.Id)
            .Select(x => new EContaxAfLoteResumen(x, detalles.Count(d => d.IdLote == x.Id),
                detalles.Where(d => d.IdLote == x.Id).Sum(d => d.Importe))).ToListAsync();
    }

    public async Task<DateTime?> SiguientePeriodoAsync(int usuario)
    {
        var actor = await AutorizarAsync(usuario, LotesRuta);
        await using var db = await AbrirAsync();
        return await SiguientePeriodoAsync(db, actor.IdEmpresa);
    }

    public async Task<List<EContaxAfDetalleResumen>> DetallesAsync(int usuario, int lote)
    {
        var actor = await AutorizarAsync(usuario, LotesRuta);
        await using var db = await AbrirAsync();
        return await (from detalle in Empresa<EContaxAfDepreciacionDetalle>(db, actor.IdEmpresa)
            join activo in Empresa<EContaxAfActivo>(db, actor.IdEmpresa) on detalle.IdActivo equals activo.Id
            where detalle.IdLote == lote
            orderby activo.Codigo
            select new EContaxAfDetalleResumen(activo.Codigo, activo.Nombre, detalle.Importe)).ToListAsync();
    }

    public Task SimularAsync(int usuario, DateTime periodo) =>
        EscribirAsync(usuario, LotesRuta, EContaxAccion.Crear, async (db, actor) =>
        {
            periodo = Mes(periodo);
            await ValidarPeriodoAsync(db, actor.IdEmpresa, periodo);
            Validar(!await Empresa<EContaxAfDepreciacionLote>(db, actor.IdEmpresa)
                .AnyAsync(x => x.Estado == "SIMULADO" || x.Estado == "APROBADO"),
                "Contabiliza o descarta el lote pendiente antes de simular otro.");
            var calculo = await CalcularAsync(db, actor.IdEmpresa, periodo);
            var lote = new EContaxAfDepreciacionLote
            {
                IdEmpresa = actor.IdEmpresa, Periodo = periodo, Estado = "SIMULADO",
                CreadoPor = usuario, CreadoUtc = DateTime.UtcNow
            };
            Tabla<EContaxAfDepreciacionLote>(db).Add(lote);
            await db.SaveChangesAsync();
            foreach (var detalle in calculo)
            {
                detalle.IdLote = lote.Id;
                Tabla<EContaxAfDepreciacionDetalle>(db).Add(detalle);
            }
        });

    public Task AprobarAsync(int usuario, int id) =>
        GestionarLoteAsync(usuario, id, EContaxAccion.Editar, async (db, actor, lote) =>
        {
            Validar(lote.Estado == "SIMULADO", "Solo se pueden aprobar lotes simulados.");
            await VerificarCalculoAsync(db, actor.IdEmpresa, lote);
            lote.Estado = "APROBADO";
            lote.AprobadoPor = usuario;
            lote.AprobadoUtc = DateTime.UtcNow;
        });

    public Task DescartarAsync(int usuario, int id) =>
        GestionarLoteAsync(usuario, id, EContaxAccion.Eliminar, (_, _, lote) =>
        {
            Validar(lote.Estado is "SIMULADO" or "APROBADO", "Este lote ya no se puede descartar.");
            lote.Estado = "DESCARTADO";
            return Task.CompletedTask;
        });

    public Task ContabilizarAsync(int usuario, int id) =>
        GestionarLoteAsync(usuario, id, EContaxAccion.Editar, async (db, actor, lote) =>
        {
            Validar(lote.Estado == "APROBADO", "Aprueba el lote antes de contabilizar.");
            var fecha = FinMes(lote.Periodo);
            Validar(fecha <= Hoy, "El período debe haber llegado a su último día.");
            var detalles = await VerificarCalculoAsync(db, actor.IdEmpresa, lote);
            if (detalles.Count > 0)
            {
                var asiento = await ContabilizarLoteAsync(db, actor, lote, detalles, false);
                lote.IdAsiento = asiento.IdAsiento;
                await RegistrarDepreciacionAsync(db, actor, fecha, detalles, false);
            }
            lote.Estado = "CONTABILIZADO";
            lote.ContabilizadoPor = usuario;
            lote.ContabilizadoUtc = DateTime.UtcNow;
        });

    public Task ReversarAsync(int usuario, int id) =>
        GestionarLoteAsync(usuario, id, EContaxAccion.Eliminar, async (db, actor, lote) =>
        {
            Validar(lote.Estado == "CONTABILIZADO" && lote.Periodo == await UltimoPeriodoAsync(db, actor.IdEmpresa),
                "Solo se puede revertir el último período contabilizado.");
            Validar(!await Empresa<EContaxAfDepreciacionLote>(db, actor.IdEmpresa)
                .AnyAsync(x => x.Estado == "SIMULADO" || x.Estado == "APROBADO"),
                "Descarta primero el lote pendiente.");
            var fecha = FinMes(lote.Periodo);
            var detalles = await DetallesAsync(db, actor.IdEmpresa, lote.Id);
            var activos = detalles.Select(x => x.IdActivo).ToArray();
            Validar(!await Empresa<EContaxAfMovimiento>(db, actor.IdEmpresa)
                .AnyAsync(x => activos.Contains(x.IdActivo) && x.Fecha > fecha && x.Tipo == "BAJA"),
                "Hay activos del lote dados de baja después de este período.");
            if (lote.IdAsiento is int idAsiento)
            {
                var original = await db.EContaxAsientosContables.SingleAsync(x => x.IdEmpresa == actor.IdEmpresa
                    && x.IdAsiento == idAsiento && x.ModuloOrigen == OrigenContable);
                Validar(original.Estado == "CONTABILIZADO", "El asiento original no está contabilizado.");
                var reverso = await ContabilizarLoteAsync(db, actor, lote, detalles, true);
                lote.IdAsientoReverso = reverso.IdAsiento;
                original.Estado = "REVERSADO";
                original.IdAsientoReverso = reverso.IdAsiento;
                original.ActualizadoPor = usuario;
                original.FechaActualizacion = DateTime.UtcNow;
                await RegistrarDepreciacionAsync(db, actor, fecha, detalles, true);
            }
            lote.Estado = "REVERSADO";
            lote.ReversadoPor = usuario;
            lote.ReversadoUtc = DateTime.UtcNow;
        });

    private async Task<EContaxAsientoContable> ContabilizarLoteAsync(AppDbContext db, EContaxUserContext actor,
        EContaxAfDepreciacionLote lote, List<EContaxAfDepreciacionDetalle> detalles, bool reverso)
    {
        var debe = detalles.GroupBy(x => reverso ? x.IdCuentaAcumulada : x.IdCuentaGasto)
            .Select(x => new EContaxAsientoDetalle { IdCuenta = x.Key, Debe = x.Sum(d => d.Importe) });
        var haber = detalles.GroupBy(x => reverso ? x.IdCuentaGasto : x.IdCuentaAcumulada)
            .Select(x => new EContaxAsientoDetalle { IdCuenta = x.Key, Haber = x.Sum(d => d.Importe) });
        return await contabilidad.CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = FinMes(lote.Periodo), ModuloOrigen = OrigenContable, IdDocumentoOrigen = lote.Id,
            Glosa = $"{(reverso ? $"Reverso de {CalculoMensual}" : $"{NombreModulo} - {CalculoMensual}")} {lote.Periodo:MM/yyyy}",
            Detalles = debe.Concat(haber).ToList()
        });
    }

    private async Task RegistrarDepreciacionAsync(AppDbContext db, EContaxUserContext actor,
        DateTime fecha, List<EContaxAfDepreciacionDetalle> detalles, bool reverso)
    {
        var saldos = (await SaldosAsync(db, actor.IdEmpresa, fecha)).ToDictionary(x => x.Activo.Id);
        foreach (var detalle in detalles)
        {
            var saldo = saldos[detalle.IdActivo];
            var tipo = intangibles ? (reverso ? "REVERSO_AM" : "AMORTIZACION") : (reverso ? "REVERSO_DEP" : "DEPRECIACION");
            Tabla<EContaxAfMovimiento>(db).Add(Movimiento(actor, detalle.IdActivo, fecha, tipo, 0,
                reverso ? -detalle.Importe : detalle.Importe, saldo.Ubicacion, saldo.Responsable, $"Lote {detalle.IdLote}"));
        }
    }

    private async Task<List<EContaxAfDepreciacionDetalle>> VerificarCalculoAsync(AppDbContext db,
        int empresa, EContaxAfDepreciacionLote lote)
    {
        await ValidarPeriodoAsync(db, empresa, lote.Periodo);
        var guardados = await DetallesAsync(db, empresa, lote.Id);
        var actuales = await CalcularAsync(db, empresa, lote.Periodo);
        Validar(guardados.Select(Clave).SequenceEqual(actuales.Select(Clave)),
            "Los saldos cambiaron desde la simulación. Descarta el lote y vuelve a simular.");
        return guardados;
    }

    private static (int, decimal, int, int) Clave(EContaxAfDepreciacionDetalle x) =>
        (x.IdActivo, x.Importe, x.IdCuentaGasto, x.IdCuentaAcumulada);

    private Task<List<EContaxAfDepreciacionDetalle>> DetallesAsync(AppDbContext db, int empresa, int lote) =>
        Empresa<EContaxAfDepreciacionDetalle>(db, empresa).Where(x => x.IdLote == lote)
            .OrderBy(x => x.IdActivo).ToListAsync();

    private async Task<List<EContaxAfDepreciacionDetalle>> CalcularAsync(AppDbContext db, int empresa, DateTime periodo)
    {
        var corte = FinMes(periodo);
        var categorias = await Empresa<EContaxAfCategoria>(db, empresa).ToDictionaryAsync(x => x.Id);
        var saldos = await SaldosAsync(db, empresa, corte);
        var altas = await Empresa<EContaxAfMovimiento>(db, empresa).Where(x => (x.Tipo == "ALTA" || x.Tipo == "SALDO_INICIAL") && x.Fecha <= corte)
            .ToDictionaryAsync(x => x.IdActivo);
        var cuentas = await db.EContaxCuentasContables.AsNoTracking().Where(x => x.IdEmpresa == empresa
            && x.EsMovimiento && x.Activa).Select(x => x.IdCuenta).ToListAsync();
        var resultado = new List<EContaxAfDepreciacionDetalle>();
        foreach (var saldo in saldos.Where(x => !x.Baja && x.Activo.VidaUtilMeses > 0).OrderBy(x => x.Activo.Id))
        {
            var alta = altas[saldo.Activo.Id];
            var inicio = alta.Tipo == "SALDO_INICIAL" ? Mes(alta.Fecha).AddMonths(1) : Mes(alta.Fecha);
            if (periodo < inicio) continue;
            var restante = saldo.ValorLibros - saldo.Activo.ValorResidual;
            if (restante <= 0) continue;
            var servicio = saldo.Activo.PuestaEnServicio;
            var transcurridos = (periodo.Year - servicio.Year) * 12 + periodo.Month - servicio.Month;
            var meses = Math.Max(1, saldo.Activo.VidaUtilMeses - transcurridos);
            var importe = Dinero(restante / meses);
            if (importe == 0) continue;
            var categoria = categorias[saldo.Activo.IdCategoria];
            Validar(cuentas.Contains(categoria.IdCuentaGasto) && cuentas.Contains(categoria.IdCuentaAcumulada),
                $"Revisa las cuentas de {CalculoMensual} de la categoría {categoria.Nombre}.");
            resultado.Add(new EContaxAfDepreciacionDetalle
            {
                IdEmpresa = empresa, IdActivo = saldo.Activo.Id, Importe = importe,
                IdCuentaGasto = categoria.IdCuentaGasto, IdCuentaAcumulada = categoria.IdCuentaAcumulada
            });
        }
        return resultado;
    }

    private async Task<List<EContaxAfSaldo>> SaldosAsync(AppDbContext db, int empresa, DateTime corte, int? idActivo = null)
    {
        var movimientos = Empresa<EContaxAfMovimiento>(db, empresa).Where(x => x.Fecha <= corte);
        var activos = Empresa<EContaxAfActivo>(db, empresa);
        if (idActivo.HasValue)
        {
            activos = activos.Where(x => x.Id == idActivo.Value);
            movimientos = movimientos.Where(x => x.IdActivo == idActivo.Value);
        }
        var totales = movimientos.GroupBy(x => x.IdActivo).Select(g => new
        {
            IdActivo = g.Key, Costo = g.Sum(x => x.CambioCosto), Depreciacion = g.Sum(x => x.CambioDepreciacion),
            Baja = g.Max(x => x.Tipo == "BAJA" ? 1 : 0) == 1
        });
        var ubicaciones = movimientos.Where(x => x.Tipo == "ALTA" || x.Tipo == "SALDO_INICIAL" || x.Tipo == "TRASLADO");
        var resumen = await (from activo in activos
            join total in totales on activo.Id equals total.IdActivo
            from ultimo in ubicaciones.Where(x => x.IdActivo == activo.Id).OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id).Take(1)
            orderby activo.Codigo
            select new EContaxAfSaldo(activo, total.Costo, total.Depreciacion, ultimo.Ubicacion, ultimo.Responsable, total.Baja))
            .ToListAsync();
        return resumen;
    }

    private static EContaxAfMovimiento Movimiento(EContaxUserContext actor, int activo, DateTime fecha, string tipo,
        decimal costo, decimal depreciacion, string ubicacion, string responsable, string motivo, Guid? idOperacion = null) => new()
    {
        IdEmpresa = actor.IdEmpresa, IdActivo = activo, Fecha = fecha, Tipo = tipo,
        CambioCosto = costo, CambioDepreciacion = depreciacion, Ubicacion = Texto(ubicacion, 120),
        Responsable = Texto(responsable, 120), Motivo = motivo, IdUsuario = actor.IdUsuario,
        RegistradoUtc = DateTime.UtcNow, IdOperacion = idOperacion ?? Guid.NewGuid()
    };

    private static DateTime Mes(DateTime fecha) => new(fecha.Year, fecha.Month, 1);
    private static DateTime FinMes(DateTime fecha) => Mes(fecha).AddMonths(1).AddDays(-1);

    private async Task ValidarPeriodoAsync(AppDbContext db, int empresa, DateTime periodo)
    {
        Validar(periodo >= new DateTime(1900, 1, 1) && periodo <= Mes(Hoy), "Selecciona un período válido.");
        var siguiente = await SiguientePeriodoAsync(db, empresa);
        Validar(siguiente.HasValue, intangibles ? "Registra primero un intangible." : "Registra primero un activo depreciable.");
        Validar(periodo == siguiente, $"El siguiente período disponible es {siguiente:MM/yyyy}.");
    }

    private async Task<DateTime?> SiguientePeriodoAsync(AppDbContext db, int empresa)
    {
        var ultimo = await UltimoPeriodoAsync(db, empresa);
        if (ultimo.HasValue) return ultimo.Value.AddMonths(1);
        var primero = await (from activo in Empresa<EContaxAfActivo>(db, empresa)
            join alta in Empresa<EContaxAfMovimiento>(db, empresa) on activo.Id equals alta.IdActivo
            where (alta.Tipo == "ALTA" || alta.Tipo == "SALDO_INICIAL") && activo.VidaUtilMeses > 0
            select (DateTime?)(alta.Tipo == "SALDO_INICIAL" ? alta.Fecha.AddMonths(1) : alta.Fecha)).MinAsync();
        return primero.HasValue ? Mes(primero.Value) : null;
    }

    private Task<DateTime?> UltimoPeriodoAsync(AppDbContext db, int empresa) =>
        Empresa<EContaxAfDepreciacionLote>(db, empresa).Where(x => x.Estado == "CONTABILIZADO")
            .MaxAsync(x => (DateTime?)x.Periodo);

    private async Task ValidarFechaAsync(AppDbContext db, int empresa, DateTime fecha)
    {
        Validar(fecha >= new DateTime(1900, 1, 1) && fecha <= Hoy, "La fecha del movimiento no es válida.");
        var ultimo = await UltimoPeriodoAsync(db, empresa);
        Validar(!ultimo.HasValue || fecha > FinMes(ultimo.Value), "La fecha pertenece a un período de activos ya contabilizado.");
    }

    private Task GestionarLoteAsync(int usuario, int id, EContaxAccion permiso,
        Func<AppDbContext, EContaxUserContext, EContaxAfDepreciacionLote, Task> operacion) =>
        EscribirAsync(usuario, LotesRuta, permiso, async (db, actor) =>
        {
            var lote = await Empresa<EContaxAfDepreciacionLote>(db, actor.IdEmpresa).AsTracking().SingleOrDefaultAsync(x => x.Id == id)
                ?? throw new InvalidOperationException("El lote no existe.");
            await operacion(db, actor, lote);
        });

    private async Task EscribirAsync(int usuario, string ruta, EContaxAccion accion,
        Func<AppDbContext, EContaxUserContext, Task> operacion)
    {
        var actor = await AutorizarAsync(usuario, ruta, accion);
        await using var origen = await AbrirAsync();
        var estrategia = origen.Database.CreateExecutionStrategy();
        await estrategia.ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync();
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var recurso = $"{OrigenContable}_EMPRESA_{actor.IdEmpresa}";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @resultado int;
                EXEC @resultado = sp_getapplock @Resource = {recurso},
                    @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 15000;
                IF @resultado < 0 THROW 50002, N'El módulo está ocupado. Intenta de nuevo.', 1;
                """);
            await operacion(db, actor);
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
        });
    }

    private async Task<EContaxUserContext> AutorizarAsync(int usuario, string ruta, EContaxAccion accion = EContaxAccion.Ver)
    {
        if (ruta != CategoriasRuta && ruta != RegistroRuta && ruta != MovimientosRuta && ruta != LotesRuta && ruta != ReportesRuta)
            throw new InvalidOperationException("La ruta no pertenece a este módulo.");
        await seguridad.ExigirAccionAsync(usuario, ruta, accion);
        return await contabilidad.EnsureInicializadoAsync(usuario);
    }

    private async Task<AppDbContext> AbrirAsync()
    {
        var db = await factory.CreateDbContextAsync();
        try
        {
            await EContaxActivosFijosSchema.EnsureAsync(db, intangibles);
            return db;
        }
        catch
        {
            await db.DisposeAsync();
            throw;
        }
    }

    private IQueryable<T> Empresa<T>(AppDbContext db, int empresa) where T : class =>
        Tabla<T>(db).AsNoTracking().Where(x => EF.Property<int>(x, "IdEmpresa") == empresa);

    private static string Texto(string? valor, int max, bool requerido = false)
    {
        valor = valor?.Trim() ?? "";
        Validar(valor.Length <= max && (!requerido || valor.Length > 0),
            $"Revisa los campos de texto de hasta {max} caracteres.");
        return valor;
    }

    private static decimal Dinero(decimal monto)
    {
        Validar(monto is >= -9999999999999999.99m and <= 9999999999999999.99m, "Importe fuera de rango.");
        return Math.Round(monto, 2, MidpointRounding.AwayFromZero);
    }

    private static void Validar(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
    }
}
