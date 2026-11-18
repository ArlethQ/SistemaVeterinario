using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.Models.EContax;

namespace Simetric.Services.EContax;

public sealed class EContaxAnticipoService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EContaxContabilidadService _contabilidad;
    private readonly EContaxBancoService _bancos;

    public EContaxAnticipoService(
        IDbContextFactory<AppDbContext> dbFactory,
        EContaxContabilidadService contabilidad,
        EContaxBancoService bancos)
    {
        _dbFactory = dbFactory;
        _contabilidad = contabilidad;
        _bancos = bancos;
    }

    public async Task<List<EContaxAnticipo>> ListarAnticiposAsync(int userId, string? tipo = null, string? estado = null)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.EContaxAnticipos.AsNoTracking().Where(x => x.IdEmpresa == actor.IdEmpresa);
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(x => x.Tipo == tipo.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(x => x.Estado == estado.Trim().ToUpperInvariant());
        return await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.IdAnticipo).ToListAsync();
    }

    public async Task<List<EContaxAnticipoAplicacion>> ListarAplicacionesAsync(int userId, int idAnticipo)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.EContaxAnticiposAplicaciones.AsNoTracking()
            .Where(x => x.IdEmpresa == actor.IdEmpresa && x.IdAnticipo == idAnticipo)
            .OrderByDescending(x => x.Fecha)
            .ToListAsync();
    }

    public async Task<EContaxAnticipo> RegistrarAnticipoAsync(int userId, EContaxAnticipo dto)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (dto.MontoTotal <= 0)
            throw new InvalidOperationException("El monto del anticipo debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(dto.NombreTercero) || string.IsNullOrWhiteSpace(dto.IdentificacionTercero))
            throw new InvalidOperationException("Debe indicar la identificación y nombre del cliente o proveedor.");

        var tipo = dto.Tipo.Trim().ToUpperInvariant();
        if (tipo is not ("CLIENTE" or "PROVEEDOR"))
            throw new InvalidOperationException("Tipo de anticipo inválido.");

        var prefijo = tipo == "CLIENTE" ? $"ANTC-{dto.Fecha:yyyyMM}-" : $"ANTP-{dto.Fecha:yyyyMM}-";
        var ultimo = await db.EContaxAnticipos.AsNoTracking()
            .Where(x => x.IdEmpresa == actor.IdEmpresa && x.NumeroAnticipo.StartsWith(prefijo))
            .OrderByDescending(x => x.NumeroAnticipo)
            .Select(x => x.NumeroAnticipo)
            .FirstOrDefaultAsync();

        var sec = 1;
        if (!string.IsNullOrWhiteSpace(ultimo) && int.TryParse(ultimo[prefijo.Length..], out var p))
            sec = p + 1;

        var entity = new EContaxAnticipo
        {
            IdEmpresa = actor.IdEmpresa,
            IdSucursal = dto.IdSucursal ?? actor.IdSucursal,
            NumeroAnticipo = $"{prefijo}{sec:D5}",
            Tipo = tipo,
            IdTercero = dto.IdTercero,
            IdentificacionTercero = dto.IdentificacionTercero.Trim(),
            NombreTercero = dto.NombreTercero.Trim(),
            Fecha = dto.Fecha == default ? DateTime.Today : dto.Fecha.Date,
            MontoTotal = Math.Round(dto.MontoTotal, 2),
            SaldoDisponible = Math.Round(dto.MontoTotal, 2),
            FormaPago = string.IsNullOrWhiteSpace(dto.FormaPago) ? "TRANSFERENCIA" : dto.FormaPago.Trim().ToUpperInvariant(),
            IdCuentaBanco = dto.IdCuentaBanco,
            ReferenciaPago = dto.ReferenciaPago?.Trim(),
            Observacion = dto.Observacion?.Trim(),
            Estado = "ACTIVO",
            CreadoPor = actor.IdUsuario,
            FechaCreacion = DateTime.UtcNow
        };
        db.EContaxAnticipos.Add(entity);
        await db.SaveChangesAsync();

        string codCajaBanco = "1.1.1.1.1";
        if (entity.IdCuentaBanco is > 0)
        {
            var bco = await db.EContaxCuentasBancarias.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdCuentaBanco == entity.IdCuentaBanco.Value && x.IdEmpresa == actor.IdEmpresa);
            if (bco is not null)
            {
                codCajaBanco = bco.CodigoCuentaContable;
                await _bancos.RegistrarMovimientoAsync(
                    userId,
                    bco.IdCuentaBanco,
                    entity.Fecha,
                    tipo == "CLIENTE" ? "DEPOSITO" : "EGRESO",
                    entity.MontoTotal,
                    $"Anticipo {entity.NumeroAnticipo} - {entity.NombreTercero}",
                    entity.ReferenciaPago ?? entity.NumeroAnticipo,
                    entity.NombreTercero,
                    moduloOrigen: "ANTICIPO",
                    idDocumentoOrigen: entity.IdAnticipo,
                    generarAsiento: false);
            }
        }

        var ctaCajaBanco = await db.EContaxCuentasContables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == codCajaBanco)
            ?? await db.EContaxCuentasContables.AsNoTracking().FirstAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == "1.1.1.1.1");

        var codAnticipo = tipo == "CLIENTE" ? "2.1.1.0" : "1.1.4.3";
        var ctaAnticipo = await db.EContaxCuentasContables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == codAnticipo)
            ?? throw new InvalidOperationException($"No se encontró la cuenta contable {codAnticipo}.");

        var detalles = tipo == "CLIENTE"
            ? new List<EContaxAsientoDetalle>
            {
                new() { IdCuenta = ctaCajaBanco.IdCuenta, CodigoCuenta = ctaCajaBanco.Codigo, NombreCuenta = ctaCajaBanco.Nombre, Detalle = $"Ingreso anticipo {entity.NumeroAnticipo}", Debe = entity.MontoTotal },
                new() { IdCuenta = ctaAnticipo.IdCuenta, CodigoCuenta = ctaAnticipo.Codigo, NombreCuenta = ctaAnticipo.Nombre, Detalle = $"Anticipo cliente {entity.NombreTercero}", Haber = entity.MontoTotal, TipoTercero = "CLIENTE", IdentificacionTercero = entity.IdentificacionTercero, NombreTercero = entity.NombreTercero }
            }
            : new List<EContaxAsientoDetalle>
            {
                new() { IdCuenta = ctaAnticipo.IdCuenta, CodigoCuenta = ctaAnticipo.Codigo, NombreCuenta = ctaAnticipo.Nombre, Detalle = $"Anticipo proveedor {entity.NombreTercero}", Debe = entity.MontoTotal, TipoTercero = "PROVEEDOR", IdentificacionTercero = entity.IdentificacionTercero, NombreTercero = entity.NombreTercero },
                new() { IdCuenta = ctaCajaBanco.IdCuenta, CodigoCuenta = ctaCajaBanco.Codigo, NombreCuenta = ctaCajaBanco.Nombre, Detalle = $"Salida anticipo {entity.NumeroAnticipo}", Haber = entity.MontoTotal }
            };

        var asiento = await _contabilidad.CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = entity.Fecha,
            TipoAsiento = tipo == "CLIENTE" ? "INGRESO" : "EGRESO",
            ModuloOrigen = "ANTICIPO",
            IdDocumentoOrigen = entity.IdAnticipo,
            ReferenciaDocumento = entity.NumeroAnticipo,
            Glosa = $"Registro de anticipo {tipo} {entity.NumeroAnticipo} - {entity.NombreTercero}",
            Detalles = detalles
        });

        entity.IdAsiento = asiento.IdAsiento;
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<EContaxAnticipoAplicacion> AplicarAnticipoAsync(
        int userId,
        int idAnticipo,
        string tipoDocumentoDestino,
        string referenciaDestino,
        DateTime fecha,
        decimal montoAplicar,
        string? observacion = null,
        int? idDocumentoDestino = null)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ant = await db.EContaxAnticipos
            .FirstOrDefaultAsync(x => x.IdAnticipo == idAnticipo && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Anticipo no encontrado.");

        if (ant.Estado == "ANULADO")
            throw new InvalidOperationException("No se puede aplicar un anticipo anulado.");
        if (montoAplicar <= 0 || montoAplicar > ant.SaldoDisponible)
            throw new InvalidOperationException($"El monto a aplicar debe estar entre 0.01 y {ant.SaldoDisponible:N2}.");

        ant.SaldoDisponible = Math.Round(ant.SaldoDisponible - montoAplicar, 2);
        ant.Estado = ant.SaldoDisponible == 0 ? "APLICADO" : "PARCIAL";

        var apl = new EContaxAnticipoAplicacion
        {
            IdAnticipo = ant.IdAnticipo,
            IdEmpresa = actor.IdEmpresa,
            TipoDocumentoDestino = tipoDocumentoDestino.Trim().ToUpperInvariant(),
            IdDocumentoDestino = idDocumentoDestino,
            ReferenciaDestino = referenciaDestino.Trim(),
            Fecha = fecha.Date,
            MontoAplicado = Math.Round(montoAplicar, 2),
            Observacion = observacion?.Trim(),
            CreadoPor = actor.IdUsuario,
            FechaCreacion = DateTime.UtcNow
        };
        db.EContaxAnticiposAplicaciones.Add(apl);
        await db.SaveChangesAsync();

        var ctaAntCli = await db.EContaxCuentasContables.AsNoTracking().FirstAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == "2.1.1.0");
        var ctaCxc = await db.EContaxCuentasContables.AsNoTracking().FirstAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == "1.1.2.5.1");
        var ctaAntProv = await db.EContaxCuentasContables.AsNoTracking().FirstAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == "1.1.4.3");
        var ctaCxp = await db.EContaxCuentasContables.AsNoTracking().FirstAsync(x => x.IdEmpresa == actor.IdEmpresa && x.Codigo == "2.1.3.1.1");

        var detalles = ant.Tipo == "CLIENTE"
            ? new List<EContaxAsientoDetalle>
            {
                new() { IdCuenta = ctaAntCli.IdCuenta, CodigoCuenta = ctaAntCli.Codigo, NombreCuenta = ctaAntCli.Nombre, Detalle = $"Cruzar anticipo {ant.NumeroAnticipo}", Debe = apl.MontoAplicado, TipoTercero = "CLIENTE", IdentificacionTercero = ant.IdentificacionTercero, NombreTercero = ant.NombreTercero },
                new() { IdCuenta = ctaCxc.IdCuenta, CodigoCuenta = ctaCxc.Codigo, NombreCuenta = ctaCxc.Nombre, Detalle = $"Aplicación a {apl.ReferenciaDestino}", Haber = apl.MontoAplicado, TipoTercero = "CLIENTE", IdentificacionTercero = ant.IdentificacionTercero, NombreTercero = ant.NombreTercero }
            }
            : new List<EContaxAsientoDetalle>
            {
                new() { IdCuenta = ctaCxp.IdCuenta, CodigoCuenta = ctaCxp.Codigo, NombreCuenta = ctaCxp.Nombre, Detalle = $"Cruzar CxP {apl.ReferenciaDestino}", Debe = apl.MontoAplicado, TipoTercero = "PROVEEDOR", IdentificacionTercero = ant.IdentificacionTercero, NombreTercero = ant.NombreTercero },
                new() { IdCuenta = ctaAntProv.IdCuenta, CodigoCuenta = ctaAntProv.Codigo, NombreCuenta = ctaAntProv.Nombre, Detalle = $"Aplicación anticipo {ant.NumeroAnticipo}", Haber = apl.MontoAplicado, TipoTercero = "PROVEEDOR", IdentificacionTercero = ant.IdentificacionTercero, NombreTercero = ant.NombreTercero }
            };

        var asiento = await _contabilidad.CrearAsientoInternoAsync(db, actor, new EContaxAsientoContable
        {
            Fecha = apl.Fecha,
            TipoAsiento = "DIARIO",
            ModuloOrigen = "ANTICIPO",
            IdDocumentoOrigen = apl.IdAplicacion,
            ReferenciaDocumento = $"{ant.NumeroAnticipo}->{apl.ReferenciaDestino}",
            Glosa = $"Aplicación de anticipo {ant.NumeroAnticipo} a {apl.ReferenciaDestino}",
            Detalles = detalles
        });

        apl.IdAsiento = asiento.IdAsiento;
        await db.SaveChangesAsync();
        return apl;
    }

    public async Task AnularAnticipoAsync(int userId, int idAnticipo)
    {
        var actor = await _contabilidad.EnsureInicializadoAsync(userId);
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ant = await db.EContaxAnticipos
            .FirstOrDefaultAsync(x => x.IdAnticipo == idAnticipo && x.IdEmpresa == actor.IdEmpresa)
            ?? throw new InvalidOperationException("Anticipo no encontrado.");

        if (ant.SaldoDisponible != ant.MontoTotal)
            throw new InvalidOperationException("No se puede anular un anticipo que ya tiene aplicaciones.");

        ant.Estado = "ANULADO";
        ant.SaldoDisponible = 0;
        await db.SaveChangesAsync();

        if (ant.IdAsiento is > 0)
            await _contabilidad.ReversarAsientoAsync(userId, ant.IdAsiento.Value, $"Anulación anticipo {ant.NumeroAnticipo}");
    }
}
