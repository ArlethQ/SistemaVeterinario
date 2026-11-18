using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.Models.EContax;

namespace Simetric.Services.EContax;

public sealed class EContaxBancoService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EContaxContabilidadService _contabilidad;

    public EContaxBancoService(
        IDbContextFactory<AppDbContext> dbFactory,
        EContaxContabilidadService contabilidad)
    {
        _dbFactory = dbFactory;
        _contabilidad = contabilidad;
    }

    public async Task<List<EContaxCuentaBancaria>> ListarCuentasAsync(int userId)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.EContaxCuentasBancarias.AsNoTracking()
            .Where(x => x.IdEmpresa == actor.IdEmpresa)
            .OrderBy(x => x.Banco).ThenBy(x => x.NumeroCuenta)
            .ToListAsync();
    }

    public async Task<EContaxCuentaBancaria> GuardarCuentaAsync(int userId, EContaxCuentaBancaria dto)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(dto.Banco) || string.IsNullOrWhiteSpace(dto.NumeroCuenta))
            throw new InvalidOperationException("Banco y número de cuenta son obligatorios.");

        if (dto.IdCuentaBanco == 0)
        {
            var entity = new EContaxCuentaBancaria
            {
                IdEmpresa = actor.IdEmpresa,
                IdSucursal = dto.IdSucursal ?? actor.IdSucursal,
                Banco = dto.Banco.Trim(),
                NumeroCuenta = dto.NumeroCuenta.Trim(),
                TipoCuenta = string.IsNullOrWhiteSpace(dto.TipoCuenta) ? "CORRIENTE" : dto.TipoCuenta.Trim().ToUpperInvariant(),
                Moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "USD" : dto.Moneda.Trim().ToUpperInvariant(),
                CodigoCuentaContable = string.IsNullOrWhiteSpace(dto.CodigoCuentaContable) ? "1.1.1.3.1" : dto.CodigoCuentaContable.Trim(),
                SaldoInicial = dto.SaldoInicial,
                SaldoActual = dto.SaldoInicial,
                Activa = dto.Activa,
                FechaCreacion = DateTime.UtcNow
            };
            db.EContaxCuentasBancarias.Add(entity);
            await db.SaveChangesAsync();
            return entity;
        }
        else
        {
            var entity = await db.EContaxCuentasBancarias
                .FirstOrDefaultAsync(x => x.IdCuentaBanco == dto.IdCuentaBanco && x.IdEmpresa == actor.IdEmpresa)
                ?? throw new InvalidOperationException("Cuenta bancaria no encontrada.");

            entity.Banco = dto.Banco.Trim();
            entity.NumeroCuenta = dto.NumeroCuenta.Trim();
            entity.TipoCuenta = dto.TipoCuenta.Trim().ToUpperInvariant();
            entity.CodigoCuentaContable = dto.CodigoCuentaContable.Trim();
            entity.Activa = dto.Activa;
            await db.SaveChangesAsync();
            return entity;
        }
    }

    public async Task<List<EContaxMovimientoBanco>> ListarMovimientosAsync(int userId, int? idCuentaBanco = null, DateTime? desde = null, DateTime? hasta = null, bool? soloNoConciliados = null)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.EContaxMovimientosBancos.AsNoTracking().Where(x => x.IdEmpresa == actor.IdEmpresa);

        if (idCuentaBanco is > 0) q = q.Where(x => x.IdCuentaBanco == idCuentaBanco.Value);
        if (desde.HasValue) q = q.Where(x => x.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(x => x.Fecha < hasta.Value.Date.AddDays(1));
        if (soloNoConciliados == true) q = q.Where(x => !x.Conciliado);

        return await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.IdMovimientoBanco).ToListAsync();
    }

    public async Task<EContaxMovimientoBanco> RegistrarMovimientoAsync(
        int userId,
        int idCuentaBanco,
        DateTime fecha,
        string tipo,
        decimal monto,
        string concepto,
        string? numeroReferencia = null,
        string? beneficiario = null,
        string? codigoCuentaContrapartida = null,
        string? moduloOrigen = null,
        int? idDocumentoOrigen = null,
        bool generarAsiento = true)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (monto <= 0)
            throw new InvalidOperationException("El monto del movimiento debe ser mayor a cero.");

        var cuenta = await db.EContaxCuentasBancarias
            .FirstOrDefaultAsync(x => x.IdCuentaBanco == idCuentaBanco && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Cuenta bancaria no encontrada.");

        var tipoNorm = tipo.Trim().ToUpperInvariant();
        var esIngreso = tipoNorm is "DEPOSITO" or "NOTA_CREDITO_BANCARIA";
        cuenta.SaldoActual += esIngreso ? monto : -monto;

        var mov = new EContaxMovimientoBanco
        {
            IdEmpresa = actor.IdEmpresa,
            IdCuentaBanco = cuenta.IdCuentaBanco,
            Fecha = fecha.Date,
            Tipo = tipoNorm,
            NumeroReferencia = numeroReferencia?.Trim(),
            Beneficiario = beneficiario?.Trim(),
            Concepto = string.IsNullOrWhiteSpace(concepto) ? tipoNorm : concepto.Trim(),
            Monto = Math.Round(monto, 2),
            Conciliado = false,
            ModuloOrigen = moduloOrigen ?? "BANCO",
            IdDocumentoOrigen = idDocumentoOrigen,
            CreadoPor = actor.IdUsuario,
            FechaCreacion = DateTime.UtcNow
        };
        db.EContaxMovimientosBancos.Add(mov);
        await db.SaveChangesAsync();

        if (generarAsiento)
        {
            var codContra = string.IsNullOrWhiteSpace(codigoCuentaContrapartida)
                ? (tipoNorm == "COMISION" ? "5.2.1.3.2" : (esIngreso ? "1.1.1.1.1" : "5.2.1.2.73"))
                : codigoCuentaContrapartida.Trim();

            var ctaBanco = await db.EContaxCuentasContables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == cuenta.CodigoCuentaContable)
                ?? await db.EContaxCuentasContables.AsNoTracking().FirstAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == "1.1.1.3.1");

            var ctaContra = await db.EContaxCuentasContables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == codContra)
                ?? await db.EContaxCuentasContables.AsNoTracking().FirstAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == "1.1.1.1.1");

            var detalles = esIngreso
                ? new List<EContaxAsientoDetalle>
                {
                    new() { IdCuenta = ctaBanco.IdCuenta, CodigoCuenta = ctaBanco.Codigo, NombreCuenta = ctaBanco.Nombre, Detalle = mov.Concepto, Debe = mov.Monto },
                    new() { IdCuenta = ctaContra.IdCuenta, CodigoCuenta = ctaContra.Codigo, NombreCuenta = ctaContra.Nombre, Detalle = mov.Concepto, Haber = mov.Monto }
                }
                : new List<EContaxAsientoDetalle>
                {
                    new() { IdCuenta = ctaContra.IdCuenta, CodigoCuenta = ctaContra.Codigo, NombreCuenta = ctaContra.Nombre, Detalle = mov.Concepto, Debe = mov.Monto },
                    new() { IdCuenta = ctaBanco.IdCuenta, CodigoCuenta = ctaBanco.Codigo, NombreCuenta = ctaBanco.Nombre, Detalle = mov.Concepto, Haber = mov.Monto }
                };

            var asiento = await _contabilidad.CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
            {
                Fecha = mov.Fecha,
                TipoAsiento = esIngreso ? "INGRESO" : "EGRESO",
                ModuloOrigen = "BANCO",
                IdDocumentoOrigen = mov.IdMovimientoBanco,
                ReferenciaDocumento = mov.NumeroReferencia ?? $"BAN-{mov.IdMovimientoBanco}",
                Glosa = $"{cuenta.Banco} ({cuenta.NumeroCuenta}): {mov.Concepto}",
                Detalles = detalles
            });

            mov.IdAsiento = asiento.IdAsiento;
            await db.SaveChangesAsync();
        }

        return mov;
    }

    public async Task<List<EContaxConciliacionBancaria>> ListarConciliacionesAsync(int userId, int? idCuentaBanco = null)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.EContaxConciliacionesBancarias.AsNoTracking().Where(x => x.IdEmpresa == actor.IdEmpresa);
        if (idCuentaBanco is > 0) q = q.Where(x => x.IdCuentaBanco == idCuentaBanco.Value);
        return await q.OrderByDescending(x => x.FechaCorte).ThenByDescending(x => x.IdConciliacion).ToListAsync();
    }

    public async Task<EContaxConciliacionBancaria> CrearConciliacionAsync(int userId, int idCuentaBanco, string periodo, DateTime fechaCorte, decimal saldoBanco, string? observacion)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var cuenta = await db.EContaxCuentasBancarias.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdCuentaBanco == idCuentaBanco && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Cuenta bancaria no encontrada.");

        var saldoLibro = cuenta.SaldoActual;
        var entity = new EContaxConciliacionBancaria
        {
            IdEmpresa = actor.IdEmpresa,
            IdCuentaBanco = idCuentaBanco,
            Periodo = periodo.Trim(),
            FechaCorte = fechaCorte.Date,
            SaldoLibro = saldoLibro,
            SaldoBanco = saldoBanco,
            Diferencia = Math.Round(saldoBanco - saldoLibro, 2),
            Estado = "BORRADOR",
            Observacion = observacion?.Trim(),
            CreadoPor = actor.IdUsuario,
            FechaCreacion = DateTime.UtcNow
        };
        db.EContaxConciliacionesBancarias.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task ToggleConciliarMovimientoAsync(int userId, int idMovimientoBanco, int? idConciliacion)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var mov = await db.EContaxMovimientosBancos
            .FirstOrDefaultAsync(x => x.IdMovimientoBanco == idMovimientoBanco && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Movimiento bancario no encontrado.");

        mov.Conciliado = !mov.Conciliado;
        mov.IdConciliacion = mov.Conciliado ? idConciliacion : null;
        await db.SaveChangesAsync();
    }

    public async Task CerrarConciliacionAsync(int userId, int idConciliacion)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var conc = await db.EContaxConciliacionesBancarias
            .FirstOrDefaultAsync(x => x.IdConciliacion == idConciliacion && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Conciliación no encontrada.");

        conc.Estado = "CERRADA";
        await db.SaveChangesAsync();
    }
}
