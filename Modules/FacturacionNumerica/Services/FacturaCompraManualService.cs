using Simetric.DTOs;
using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.Models;
using Simetric.ViewModels;

namespace Simetric.Services;

public sealed class FacturaCompraManualService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FacturaCompraManualService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> GuardarAsync(int idUsuario, FacturaCompraManualVM modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.RucProveedor) || string.IsNullOrWhiteSpace(modelo.Proveedor))
            throw new InvalidOperationException("Selecciona un proveedor del CRUD de proveedores.");
        if (modelo.FechaEmision == default || modelo.FechaEmision.Date > DateTime.Today)
            throw new InvalidOperationException("La fecha de emisión debe ser válida y no puede ser futura.");

        var ruc = SoloDigitos(modelo.RucProveedor);
        if (ruc.Length is not (10 or 13))
            throw new InvalidOperationException("La identificación del proveedor debe tener 10 o 13 dígitos.");
        if (string.IsNullOrWhiteSpace(modelo.FormaPago))
            throw new InvalidOperationException("Selecciona la forma de pago.");
        if (modelo.EsCredito && modelo.PlazoCredito <= 0)
            throw new InvalidOperationException("Ingresa un plazo de crédito mayor a cero.");
        if (modelo.Detalles.Count == 0 || modelo.Detalles.Any(x =>
            string.IsNullOrWhiteSpace(x.Descripcion) ||
            x.Cantidad <= 0 ||
            x.PrecioUnitario < 0 ||
            x.Descuento < 0 ||
            x.Descuento > x.Cantidad * x.PrecioUnitario ||
            x.Tarifa is not (0 or 5 or 8 or 12 or 15)))
            throw new InvalidOperationException("Agrega al menos un detalle válido a la factura.");
        if (modelo.Total <= 0m)
            throw new InvalidOperationException("La factura debe tener un total mayor a cero.");

        await using var context = await _dbFactory.CreateDbContextAsync();
        if (!await context.Proveedores.AsNoTracking().AnyAsync(x => x.estado == true && x.ruc == ruc))
            throw new InvalidOperationException("El proveedor debe existir y estar activo en el CRUD de proveedores.");

        var cliente = await context.Clientes.FirstOrDefaultAsync(x => x.Numeroidentificacion == ruc && (x.Usuario == idUsuario || x.Usuario == null));
        if (cliente == null)
        {
            cliente = new Cliente
            {
                Numeroidentificacion = ruc,
                Tipoidentificacion = string.IsNullOrWhiteSpace(modelo.TipoIdentificacion) ? (ruc.Length == 13 ? "04" : "05") : modelo.TipoIdentificacion,
                Nombrerazonsocial = modelo.Proveedor.Trim(),
                Nombrecomercial = string.IsNullOrWhiteSpace(modelo.NombreComercial) ? modelo.Proveedor.Trim() : modelo.NombreComercial.Trim(),
                Nombres = string.IsNullOrWhiteSpace(modelo.Nombres) ? modelo.Proveedor.Trim() : modelo.Nombres.Trim(),
                Apellidos = string.IsNullOrWhiteSpace(modelo.Apellidos) ? "." : modelo.Apellidos.Trim(),
                Direccion = modelo.Direccion.Trim(),
                Telefonoconvencional = modelo.Telefono.Trim(),
                Correo = modelo.Correo.Trim(),
                Referencia = modelo.Referencia.Trim(),
                Oblgconta = modelo.ObligadoContabilidad,
                TipoCliente = 2,
                Usuario = idUsuario,
                Estado = true,
                Fechaingreso = DateOnly.FromDateTime(DateTime.Today)
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
        }

        var serie = "MANUAL";
        var numero = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var duplicada = await context.ComprasFacturas.AnyAsync(x =>
            x.Usuario == idUsuario && x.CodDocumento == "01" && x.TipoDocumento == "COMPRA" &&
            x.Serie == serie && x.NumFactura == numero && x.CodClientes == cliente.Codcliente && x.Estado == true);
        if (duplicada)
            throw new InvalidOperationException("La factura de compra ya está registrada para este proveedor.");

        var total = modelo.Total;
        var esCredito = modelo.EsCredito;
        var compra = new ComprasFactura
        {
            CodClave = $"MANUAL-COMPRA-{Guid.NewGuid():N}",
            CodClientes = cliente.Codcliente,
            FchAutorizacion = modelo.FechaEmision.Date,
            NumFactura = numero,
            CodDocumento = "01",
            Subtotal = modelo.Subtotal,
            Subtotal0 = modelo.Detalles.Where(x => x.Tarifa == 0).Sum(x => x.Base),
            Subtotal12 = modelo.Detalles.Where(x => x.Tarifa == 12).Sum(x => x.Base),
            Iva = modelo.Iva,
            ValorTotal = total,
            FechaVence = esCredito ? modelo.FechaEmision.Date.AddDays(Math.Max(modelo.PlazoCredito, 1)) : modelo.FechaEmision.Date,
            Usuario = idUsuario,
            Autorizado = "1",
            Mensaje = string.IsNullOrWhiteSpace(modelo.Observacion)
                ? "Factura de compra registrada manualmente; no se envía al SRI."
                : modelo.Observacion.Trim(),
            Serie = serie,
            TipoPago = string.IsNullOrWhiteSpace(modelo.FormaPago) ? "20" : modelo.FormaPago,
            TiempoCredito = esCredito ? Math.Max(modelo.PlazoCredito, 1) : 0,
            Estado = true,
            TipoDocumento = "COMPRA",
            EstadoEnvioSRI = "NO APLICA",
            FechaRegistro = DateTime.Now,
            ValorApagar = esCredito ? total : 0m,
            EstadoPago = esCredito ? "PENDIENTE" : "PAGADO",
            FechaCancelado = esCredito ? null : DateOnly.FromDateTime(DateTime.Today),
            Contabilizado = false,
            Inventario = false
        };

        context.ComprasFacturas.Add(compra);
        await context.SaveChangesAsync();

        foreach (var detalle in modelo.Detalles)
        {
            context.ComprasDetalleFac.Add(new ComprasDetalleFac
            {
                CodFactura = compra.CodFactura,
                CodProducto = 0,
                CodPrincipal = detalle.Codigo.Trim(),
                CantProducto = detalle.Cantidad,
                DescripProducto = detalle.Descripcion.Trim(),
                PrecioProducto = detalle.PrecioUnitario,
                Descuento = detalle.Descuento,
                ValorTProducto = detalle.Base,
                ValorIVA = detalle.Iva,
                ValorTotal = detalle.Base + detalle.Iva,
                PorImp = (int)detalle.Tarifa,
                Tarifa = (int)detalle.Tarifa,
                Inventariado = false,
                Observacion = string.IsNullOrWhiteSpace(detalle.Detalle) ? "Factura de compra manual" : detalle.Detalle.Trim()
            });
        }

        await context.SaveChangesAsync();
        return compra.CodFactura;
    }

    public async Task<List<CompraManualVM>> ListarAsync(int idUsuario, bool incluirAnuladas = true)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var compras = await CargarCabecerasAsync(context, idUsuario, incluirAnuladas);
        var ids = compras.Select(x => x.IdFactura).ToList();
        var pagos = ids.Count == 0
            ? new Dictionary<int, decimal>()
            : await context.CuentasPorPagarPagos.AsNoTracking()
                .Where(x => x.Estado && ids.Contains(x.CodFactura))
                .GroupBy(x => x.CodFactura)
                .Select(x => new { x.Key, Total = x.Sum(p => p.Monto) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

        foreach (var compra in compras)
        {
            var pagosRegistrados = pagos.GetValueOrDefault(compra.IdFactura);
            compra.TotalPagado = compra.SaldoPendiente <= 0m && compra.EstadoPago.Contains("PAG", StringComparison.OrdinalIgnoreCase)
                ? compra.Total
                : pagosRegistrados;
            compra.SaldoPendiente = compra.Estado == "ANULADA" ? 0m : Math.Max(compra.SaldoPendiente - pagosRegistrados, 0m);
        }

        return compras;
    }

    public async Task<CompraManualVM?> ObtenerAsync(int idUsuario, int idFactura)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var compra = (await CargarCabecerasAsync(context, idUsuario, incluirAnuladas: true))
            .FirstOrDefault(x => x.IdFactura == idFactura);
        if (compra == null)
            return null;

        compra.Detalles = await context.ComprasDetalleFac.AsNoTracking()
            .Where(x => x.CodFactura == idFactura)
            .OrderBy(x => x.CodLinea)
            .Select(x => new CompraManualDetalleVM
            {
                Codigo = x.CodPrincipal ?? "",
                Descripcion = x.DescripProducto ?? "",
                Detalle = x.Observacion ?? "",
                Cantidad = x.CantProducto ?? 0m,
                PrecioUnitario = x.PrecioProducto ?? 0m,
                Descuento = x.Descuento ?? 0m,
                Base = x.ValorTProducto ?? 0m,
                Iva = x.ValorIVA ?? 0m,
                Total = x.ValorTotal ?? 0m,
                Tarifa = x.Tarifa ?? 0
            })
            .ToListAsync();
        return compra;
    }

    public async Task<bool> AnularAsync(int idUsuario, int idFactura)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var compra = await context.ComprasFacturas.FirstOrDefaultAsync(x =>
            x.CodFactura == idFactura &&
            x.Usuario == idUsuario &&
            x.CodDocumento == "01" &&
            x.TipoDocumento == "COMPRA" &&
            x.CodClave != null && x.CodClave.StartsWith("MANUAL-COMPRA-") &&
            x.Estado == true);
        if (compra == null)
            return false;

        var tienePagos = await context.CuentasPorPagarPagos.AnyAsync(x => x.CodFactura == idFactura && x.Estado && x.Monto > 0m);
        if (tienePagos)
            throw new InvalidOperationException("No se puede anular una compra que ya tiene pagos registrados.");

        compra.Estado = false;
        compra.EstadoPago = "ANULADO";
        compra.ValorApagar = 0m;
        compra.FechaCancelado = null;
        compra.Mensaje = "Factura de compra manual anulada; no se envía al SRI.";
        await context.SaveChangesAsync();
        return true;
    }

    private static async Task<List<CompraManualVM>> CargarCabecerasAsync(AppDbContext context, int idUsuario, bool incluirAnuladas)
    {
        var comprasQuery = context.ComprasFacturas.AsNoTracking()
            .Where(x => x.Usuario == idUsuario &&
                x.CodDocumento == "01" &&
                x.TipoDocumento == "COMPRA" &&
                x.CodClave != null && x.CodClave.StartsWith("MANUAL-COMPRA-"));
        if (!incluirAnuladas)
            comprasQuery = comprasQuery.Where(x => x.Estado == true);

        return await (
            from compra in comprasQuery
            join cliente in context.Clientes.AsNoTracking() on compra.CodClientes equals cliente.Codcliente into clientes
            from cliente in clientes.DefaultIfEmpty()
            orderby compra.FechaRegistro descending, compra.CodFactura descending
            select new CompraManualVM
            {
                IdFactura = compra.CodFactura,
                NumeroDocumento = FormatearDocumento(compra),
                Proveedor = cliente == null ? "Proveedor sin nombre" : (cliente.Nombrerazonsocial ?? cliente.Nombrecomercial ?? ((cliente.Nombres ?? "") + " " + (cliente.Apellidos ?? "")).Trim()),
                IdentificacionProveedor = cliente == null ? "" : cliente.Numeroidentificacion ?? "",
                FechaEmision = compra.FchAutorizacion,
                FechaVencimiento = compra.FechaVence,
                FormaPago = compra.TipoPago ?? "",
                EstadoPago = compra.EstadoPago ?? "",
                Estado = compra.Estado == true ? "ACTIVA" : "ANULADA",
                Observacion = compra.Mensaje ?? "",
                Total = compra.ValorTotal ?? 0m,
                SaldoPendiente = compra.Estado == true ? Math.Max(compra.ValorApagar ?? 0m, 0m) : 0m
            }).ToListAsync();
    }

    private static string FormatearDocumento(ComprasFactura compra)
    {
        var serie = (compra.Serie ?? "").Replace("-", "").Trim();
        var serieFormateada = serie.Length == 6 ? $"{serie[..3]}-{serie[3..]}" : serie;
        return $"{serieFormateada}-{(compra.NumFactura ?? "").PadLeft(9, '0')}";
    }

    public async Task<int> GuardarFacturaEContaxAsync(int idUsuario, FacturaCompraManualVM modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.RucProveedor) || string.IsNullOrWhiteSpace(modelo.Proveedor))
            throw new InvalidOperationException("Ingresa la identificación y razón social del proveedor.");
        if (modelo.FechaEmision == default || modelo.FechaEmision.Date > DateTime.Today)
            throw new InvalidOperationException("La fecha de emisión debe ser válida y no puede ser futura.");

        var ruc = SoloDigitos(modelo.RucProveedor);
        if (ruc.Length is not (10 or 13))
            throw new InvalidOperationException("La identificación del proveedor debe tener 10 o 13 dígitos.");
        if (string.IsNullOrWhiteSpace(modelo.FormaPago))
            throw new InvalidOperationException("Selecciona la forma de pago.");
        if (modelo.EsCredito && modelo.PlazoCredito <= 0)
            throw new InvalidOperationException("Ingresa un plazo de crédito mayor a cero.");

        var serieDigitos = SoloDigitos(modelo.Serie);
        var numeroDigitos = SoloDigitos(modelo.NumeroFactura);
        if (serieDigitos.Length != 6)
            throw new InvalidOperationException("La serie de la factura del proveedor debe tener 6 dígitos (ej. 001-001).");
        if (string.IsNullOrWhiteSpace(numeroDigitos) || numeroDigitos.Length > 9)
            throw new InvalidOperationException("El número de factura del proveedor debe tener entre 1 y 9 dígitos.");

        var numeroNormalizado = numeroDigitos.PadLeft(9, '0');
        var autorizacion = SoloDigitos(modelo.NumeroAutorizacion);
        if (!string.IsNullOrWhiteSpace(autorizacion) && autorizacion.Length is < 10 or > 49)
            throw new InvalidOperationException("El número de autorización debe tener entre 10 y 49 dígitos.");

        if (modelo.Detalles.Count == 0 || modelo.Detalles.Any(x =>
            string.IsNullOrWhiteSpace(x.Descripcion) ||
            x.Cantidad <= 0 ||
            x.PrecioUnitario < 0 ||
            x.Descuento < 0 ||
            x.Descuento > x.Cantidad * x.PrecioUnitario ||
            x.Tarifa is not (0 or 5 or 8 or 12 or 15)))
            throw new InvalidOperationException("Agrega al menos un detalle válido a la factura.");
        if (modelo.Total <= 0m)
            throw new InvalidOperationException("La factura debe tener un total mayor a cero.");

        await using var context = await _dbFactory.CreateDbContextAsync();
        var cliente = await AsegurarProveedorYClienteAsync(context, idUsuario, ruc, modelo);

        var duplicada = await context.ComprasFacturas.AnyAsync(x =>
            x.Usuario == idUsuario &&
            x.CodDocumento == "01" &&
            x.TipoDocumento == "COMPRA" &&
            x.Serie == serieDigitos &&
            x.NumFactura == numeroNormalizado &&
            x.CodClientes == cliente.Codcliente &&
            x.Estado == true);
        if (duplicada)
            throw new InvalidOperationException($"La factura {serieDigitos[..3]}-{serieDigitos[3..]}-{numeroNormalizado} ya está registrada para este proveedor.");

        var claveAcceso = !string.IsNullOrWhiteSpace(modelo.ClaveAcceso)
            ? modelo.ClaveAcceso.Trim()
            : $"MANUAL-COMPRA-{Guid.NewGuid():N}";

        var total = modelo.Total;
        var esCredito = modelo.EsCredito;
        var subtotal5 = modelo.Detalles.Where(x => x.Tarifa == 5).Sum(x => x.Base);
        var subtotal8 = modelo.Detalles.Where(x => x.Tarifa == 8).Sum(x => x.Base);
        var subtotal12 = modelo.Detalles.Where(x => x.Tarifa is 12 or 15).Sum(x => x.Base);
        var iva5 = modelo.Detalles.Where(x => x.Tarifa == 5).Sum(x => x.Iva);
        var iva8 = modelo.Detalles.Where(x => x.Tarifa == 8).Sum(x => x.Iva);

        var compra = new ComprasFactura
        {
            CodClave = claveAcceso,
            CodClientes = cliente.Codcliente,
            CodEmisor = modelo.CodEmisor,
            IdEmpresa = modelo.IdEmpresa,
            IdSucursal = modelo.IdSucursal,
            FchAutorizacion = modelo.FechaEmision.Date,
            NumFactura = numeroNormalizado,
            NumAutorizacion = string.IsNullOrWhiteSpace(autorizacion) ? null : autorizacion,
            CodDocumento = "01",
            Subtotal = modelo.Subtotal,
            Subtotal0 = modelo.Detalles.Where(x => x.Tarifa == 0).Sum(x => x.Base),
            Subtotal5 = subtotal5,
            Subtotal8 = subtotal8,
            Subtotal12 = subtotal12,
            Descuentos = modelo.Detalles.Sum(x => x.Descuento),
            Iva = modelo.Iva,
            Iva5 = iva5,
            Iva8 = iva8,
            ValorTotal = total,
            FechaVence = esCredito ? modelo.FechaEmision.Date.AddDays(Math.Max(modelo.PlazoCredito, 1)) : modelo.FechaEmision.Date,
            Usuario = idUsuario,
            Autorizado = "1",
            Mensaje = string.IsNullOrWhiteSpace(modelo.Observacion)
                ? "Factura de compra registrada en E-Contax."
                : modelo.Observacion.Trim(),
            Serie = serieDigitos,
            TipoPago = string.IsNullOrWhiteSpace(modelo.FormaPago) ? "20" : modelo.FormaPago,
            TiempoCredito = esCredito ? Math.Max(modelo.PlazoCredito, 1) : 0,
            Estado = true,
            TipoDocumento = "COMPRA",
            EstadoEnvioSRI = string.IsNullOrWhiteSpace(modelo.ClaveAcceso) ? "NO APLICA" : "RECIBIDO",
            FechaRegistro = DateTime.Now,
            ValorApagar = esCredito ? total : 0m,
            EstadoPago = esCredito ? "PENDIENTE" : "PAGADO",
            FechaCancelado = esCredito ? null : DateOnly.FromDateTime(DateTime.Today),
            Contabilizado = false,
            Inventario = false,
            TieneRetencion = false
        };

        context.ComprasFacturas.Add(compra);
        await context.SaveChangesAsync();

        foreach (var detalle in modelo.Detalles)
        {
            context.ComprasDetalleFac.Add(new ComprasDetalleFac
            {
                CodFactura = compra.CodFactura,
                CodProducto = 0,
                CodPrincipal = detalle.Codigo.Trim(),
                CantProducto = detalle.Cantidad,
                DescripProducto = detalle.Descripcion.Trim(),
                PrecioProducto = detalle.PrecioUnitario,
                Descuento = detalle.Descuento,
                ValorTProducto = detalle.Base,
                ValorIVA = detalle.Iva,
                ValorTotal = detalle.Base + detalle.Iva,
                CodImp = 2,
                PorImp = (int)detalle.Tarifa,
                Tarifa = (int)detalle.Tarifa,
                Inventariado = false,
                Observacion = string.IsNullOrWhiteSpace(detalle.Detalle) ? "Factura de compra E-Contax" : detalle.Detalle.Trim()
            });
        }

        await context.SaveChangesAsync();
        return compra.CodFactura;
    }

    public async Task<(int CodFactura, bool Duplicada)> ImportarFacturaXmlEContaxAsync(
        int idUsuario,
        CompraXmlPreviewDto preview,
        int? idEmpresa = null,
        int? idSucursal = null)
    {
        if (preview == null)
            throw new InvalidOperationException("No hay datos del XML para importar.");

        if (preview.CodDocumentoSustento != "01")
            throw new InvalidOperationException("El XML debe corresponder a una factura electrónica (código 01).");

        var ruc = SoloDigitos(preview.RucProveedor);
        if (ruc.Length is not (10 or 13))
            throw new InvalidOperationException("El RUC del proveedor en el XML no es válido.");

        var serieDigitos = SoloDigitos(preview.Serie);
        var numeroNormalizado = SoloDigitos(preview.Secuencial).PadLeft(9, '0');
        if (serieDigitos.Length != 6 || numeroNormalizado.Length > 9)
            throw new InvalidOperationException("La serie o número de factura del XML no tiene el formato esperado.");

        await using var context = await _dbFactory.CreateDbContextAsync();

        if (!string.IsNullOrWhiteSpace(preview.ClaveAcceso))
        {
            var existentePorClave = await context.ComprasFacturas.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Usuario == idUsuario &&
                    x.CodDocumento == "01" &&
                    x.CodClave == preview.ClaveAcceso &&
                    x.Estado == true);
            if (existentePorClave != null)
                return (existentePorClave.CodFactura, true);
        }

        var vm = new FacturaCompraManualVM
        {
            Serie = serieDigitos,
            NumeroFactura = numeroNormalizado,
            NumeroAutorizacion = preview.NumeroAutorizacion,
            ClaveAcceso = preview.ClaveAcceso,
            CodEmisor = preview.CodEmisor,
            IdEmpresa = idEmpresa ?? preview.IdEmpresa,
            IdSucursal = idSucursal ?? preview.IdSucursal,
            RucProveedor = ruc,
            Proveedor = string.IsNullOrWhiteSpace(preview.RazonSocialProveedor) ? ruc : preview.RazonSocialProveedor,
            TipoIdentificacion = ruc.Length == 13 ? "04" : "05",
            ObligadoContabilidad = string.IsNullOrWhiteSpace(preview.ObligadoContabilidad) ? "NO" : preview.ObligadoContabilidad,
            NombreComercial = preview.RazonSocialProveedor,
            Direccion = preview.DireccionProveedor,
            Telefono = string.IsNullOrWhiteSpace(preview.TelefonoProveedor) ? preview.TelefonoFijoProveedor : preview.TelefonoProveedor,
            Correo = preview.EmailProveedor,
            FechaEmision = preview.FechaEmision?.Date ?? DateTime.Today,
            FormaPago = string.IsNullOrWhiteSpace(preview.FormaPago) ? "20" : preview.FormaPago,
            PlazoCredito = preview.FormaPago == "19" ? 30 : 0,
            Observacion = $"Importada desde XML {serieDigitos[..3]}-{serieDigitos[3..]}-{numeroNormalizado}",
            Detalles = (preview.Detalles ?? new List<CompraXmlDetalleDto>())
                .Select(d => new FacturaCompraManualDetalleVM
                {
                    Codigo = string.IsNullOrWhiteSpace(d.CodPrincipal) ? d.CodAuxiliar : d.CodPrincipal,
                    Descripcion = string.IsNullOrWhiteSpace(d.Descripcion) ? "Item XML" : d.Descripcion,
                    Detalle = "Importado desde XML",
                    Cantidad = d.Cantidad > 0 ? d.Cantidad : 1m,
                    PrecioUnitario = d.PrecioUnitario >= 0 ? d.PrecioUnitario : 0m,
                    Descuento = d.Descuento >= 0 ? d.Descuento : 0m,
                    Tarifa = d.Tarifa is 0 or 5 or 8 or 12 or 15 ? d.Tarifa : 0
                })
                .ToList()
        };

        var existentePorNumero = await context.ComprasFacturas.AsNoTracking()
            .Where(x =>
                x.Usuario == idUsuario &&
                x.CodDocumento == "01" &&
                x.TipoDocumento == "COMPRA" &&
                x.Serie == serieDigitos &&
                x.NumFactura == numeroNormalizado &&
                x.Estado == true)
            .Join(
                context.Clientes.AsNoTracking().Where(c => c.Numeroidentificacion == ruc),
                c => c.CodClientes,
                cl => cl.Codcliente,
                (c, _) => c.CodFactura)
            .FirstOrDefaultAsync();

        if (existentePorNumero > 0)
            return (existentePorNumero, true);

        var codFactura = await GuardarFacturaEContaxAsync(idUsuario, vm);
        return (codFactura, false);
    }

    public async Task<List<CompraManualVM>> ListarComprasEContaxAsync(int idUsuario, bool incluirAnuladas = true)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var comprasQuery = context.ComprasFacturas.AsNoTracking()
            .Where(x => x.Usuario == idUsuario &&
                x.CodDocumento == "01" &&
                x.TipoDocumento == "COMPRA");
        if (!incluirAnuladas)
            comprasQuery = comprasQuery.Where(x => x.Estado == true);

        var rawCompras = await (
            from compra in comprasQuery
            join cliente in context.Clientes.AsNoTracking() on compra.CodClientes equals cliente.Codcliente into clientes
            from cliente in clientes.DefaultIfEmpty()
            orderby compra.FechaRegistro descending, compra.CodFactura descending
            select new
            {
                compra.CodFactura,
                compra.Serie,
                compra.NumFactura,
                compra.NumAutorizacion,
                compra.CodClave,
                compra.FchAutorizacion,
                compra.FechaVence,
                compra.TipoPago,
                compra.EstadoPago,
                compra.Estado,
                compra.Mensaje,
                compra.Subtotal,
                compra.Iva,
                compra.ValorTotal,
                compra.ValorApagar,
                compra.TieneRetencion,
                compra.NumRetencion,
                Proveedor = cliente == null
                    ? "Proveedor sin nombre"
                    : (cliente.Nombrerazonsocial ?? cliente.Nombrecomercial ?? ((cliente.Nombres ?? "") + " " + (cliente.Apellidos ?? "")).Trim()),
                IdentificacionProveedor = cliente == null ? "" : (cliente.Numeroidentificacion ?? "")
            }).ToListAsync();

        var ids = rawCompras.Select(x => x.CodFactura).ToList();
        var pagos = ids.Count == 0
            ? new Dictionary<int, decimal>()
            : await context.CuentasPorPagarPagos.AsNoTracking()
                .Where(x => x.Estado && ids.Contains(x.CodFactura))
                .GroupBy(x => x.CodFactura)
                .Select(x => new { x.Key, Total = x.Sum(p => p.Monto) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

        var retencionesPorCompra = ids.Count == 0
            ? new Dictionary<int, string>()
            : await context.RetencionInfo.AsNoTracking()
                .Where(x => x.IcCompra.HasValue && ids.Contains(x.IcCompra.Value))
                .GroupBy(x => x.IcCompra!.Value)
                .Select(g => new { CodFactura = g.Key, NumRetencion = g.OrderByDescending(r => r.Sec).Select(r => r.NumRetencion ?? "").FirstOrDefault() ?? "" })
                .ToDictionaryAsync(x => x.CodFactura, x => x.NumRetencion);

        return rawCompras.Select(x =>
        {
            var pagosRegistrados = pagos.GetValueOrDefault(x.CodFactura);
            var saldoBase = x.Estado == true ? Math.Max(x.ValorApagar ?? 0m, 0m) : 0m;
            var totalPagado = saldoBase <= 0m && (x.EstadoPago ?? "").Contains("PAG", StringComparison.OrdinalIgnoreCase)
                ? (x.ValorTotal ?? 0m)
                : pagosRegistrados;
            var tieneRet = x.TieneRetencion == true || retencionesPorCompra.ContainsKey(x.CodFactura);
            var numRet = !string.IsNullOrWhiteSpace(x.NumRetencion)
                ? x.NumRetencion!
                : retencionesPorCompra.GetValueOrDefault(x.CodFactura, string.Empty);
            var esManual = string.IsNullOrWhiteSpace(x.CodClave) || x.CodClave.StartsWith("MANUAL-COMPRA-", StringComparison.OrdinalIgnoreCase);

            return new CompraManualVM
            {
                IdFactura = x.CodFactura,
                NumeroDocumento = FormatearDocumento(new ComprasFactura { Serie = x.Serie, NumFactura = x.NumFactura }),
                Serie = x.Serie ?? "",
                NumeroFactura = x.NumFactura ?? "",
                NumeroAutorizacion = x.NumAutorizacion ?? "",
                ClaveAcceso = x.CodClave ?? "",
                Proveedor = x.Proveedor,
                IdentificacionProveedor = x.IdentificacionProveedor,
                FechaEmision = x.FchAutorizacion,
                FechaVencimiento = x.FechaVence,
                FormaPago = x.TipoPago ?? "",
                EstadoPago = x.EstadoPago ?? "",
                Estado = x.Estado == true ? "ACTIVA" : "ANULADA",
                Origen = esManual ? "MANUAL" : "XML",
                Observacion = x.Mensaje ?? "",
                Subtotal = x.Subtotal ?? 0m,
                Iva = x.Iva ?? 0m,
                Total = x.ValorTotal ?? 0m,
                TotalPagado = totalPagado,
                SaldoPendiente = x.Estado == true ? Math.Max(saldoBase - pagosRegistrados, 0m) : 0m,
                TieneRetencion = tieneRet,
                NumeroRetencion = numRet
            };
        }).ToList();
    }

    public async Task<CompraManualVM?> ObtenerCompraEContaxAsync(int idUsuario, int idFactura)
    {
        var compra = (await ListarComprasEContaxAsync(idUsuario, incluirAnuladas: true))
            .FirstOrDefault(x => x.IdFactura == idFactura);
        if (compra == null)
            return null;

        await using var context = await _dbFactory.CreateDbContextAsync();
        compra.Detalles = await context.ComprasDetalleFac.AsNoTracking()
            .Where(x => x.CodFactura == idFactura)
            .OrderBy(x => x.CodLinea)
            .Select(x => new CompraManualDetalleVM
            {
                Codigo = x.CodPrincipal ?? "",
                Descripcion = x.DescripProducto ?? "",
                Detalle = x.Observacion ?? "",
                Cantidad = x.CantProducto ?? 0m,
                PrecioUnitario = x.PrecioProducto ?? 0m,
                Descuento = x.Descuento ?? 0m,
                Base = x.ValorTProducto ?? 0m,
                Iva = x.ValorIVA ?? 0m,
                Total = x.ValorTotal ?? 0m,
                Tarifa = x.Tarifa ?? 0
            })
            .ToListAsync();
        return compra;
    }

    public async Task<bool> AnularCompraEContaxAsync(int idUsuario, int idFactura)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var compra = await context.ComprasFacturas.FirstOrDefaultAsync(x =>
            x.CodFactura == idFactura &&
            x.Usuario == idUsuario &&
            x.CodDocumento == "01" &&
            x.TipoDocumento == "COMPRA" &&
            x.Estado == true);
        if (compra == null)
            return false;

        var tieneRetencion = compra.TieneRetencion == true ||
            await context.RetencionInfo.AnyAsync(x => x.IcCompra == idFactura);
        if (tieneRetencion)
            throw new InvalidOperationException("No se puede anular una factura de compra que ya tiene una retención asociada.");

        var tienePagos = await context.CuentasPorPagarPagos.AnyAsync(x => x.CodFactura == idFactura && x.Estado && x.Monto > 0m);
        if (tienePagos)
            throw new InvalidOperationException("No se puede anular una compra que ya tiene pagos registrados.");

        compra.Estado = false;
        compra.EstadoPago = "ANULADO";
        compra.ValorApagar = 0m;
        compra.FechaCancelado = null;
        compra.Mensaje = "Factura de compra anulada en E-Contax.";
        await context.SaveChangesAsync();
        return true;
    }

    private static async Task<Cliente> AsegurarProveedorYClienteAsync(
        AppDbContext context,
        int idUsuario,
        string ruc,
        FacturaCompraManualVM modelo)
    {
        var proveedorDb = await context.Proveedores.FirstOrDefaultAsync(x => x.ruc == ruc);
        if (proveedorDb == null)
        {
            context.Proveedores.Add(new Proveedor
            {
                ruc = ruc,
                nombre = modelo.Proveedor.Trim(),
                nombreComercial = string.IsNullOrWhiteSpace(modelo.NombreComercial) ? modelo.Proveedor.Trim() : modelo.NombreComercial.Trim(),
                direccion = string.IsNullOrWhiteSpace(modelo.Direccion) ? "." : modelo.Direccion.Trim(),
                telefono = modelo.Telefono.Trim(),
                telefonoMovil = modelo.Telefono.Trim(),
                email = modelo.Correo.Trim(),
                estado = true,
                fechaActualizacion = DateTime.Now,
                tipoIdentificacion = string.IsNullOrWhiteSpace(modelo.TipoIdentificacion) ? (ruc.Length == 13 ? "04" : "05") : modelo.TipoIdentificacion,
                personaNatural = ruc.Length == 10 ? '1' : '0',
                obligado = string.Equals(modelo.ObligadoContabilidad, "SI", StringComparison.OrdinalIgnoreCase) ? '1' : '0',
                formaPago = string.IsNullOrWhiteSpace(modelo.FormaPago) ? "20" : modelo.FormaPago,
                plazoPago = modelo.EsCredito ? Math.Max(modelo.PlazoCredito, 1) : 0,
                saldoInicial = 0m,
                llevaRetencion = true
            });
            await context.SaveChangesAsync();
        }
        else if (proveedorDb.estado != true)
        {
            proveedorDb.estado = true;
            await context.SaveChangesAsync();
        }

        var cliente = await context.Clientes.FirstOrDefaultAsync(x =>
            x.Numeroidentificacion == ruc && (x.Usuario == idUsuario || x.Usuario == null));
        if (cliente == null)
        {
            cliente = new Cliente
            {
                Numeroidentificacion = ruc,
                Tipoidentificacion = string.IsNullOrWhiteSpace(modelo.TipoIdentificacion) ? (ruc.Length == 13 ? "04" : "05") : modelo.TipoIdentificacion,
                Nombrerazonsocial = modelo.Proveedor.Trim(),
                Nombrecomercial = string.IsNullOrWhiteSpace(modelo.NombreComercial) ? modelo.Proveedor.Trim() : modelo.NombreComercial.Trim(),
                Nombres = string.IsNullOrWhiteSpace(modelo.Nombres) ? modelo.Proveedor.Trim() : modelo.Nombres.Trim(),
                Apellidos = string.IsNullOrWhiteSpace(modelo.Apellidos) ? "." : modelo.Apellidos.Trim(),
                Direccion = modelo.Direccion.Trim(),
                Telefonoconvencional = modelo.Telefono.Trim(),
                Correo = modelo.Correo.Trim(),
                Referencia = modelo.Referencia.Trim(),
                Oblgconta = modelo.ObligadoContabilidad,
                Idempresa = modelo.IdEmpresa,
                Idsucursal = modelo.IdSucursal,
                TipoCliente = 2,
                Usuario = idUsuario,
                Estado = true,
                Retenciones = true,
                Fechaingreso = DateOnly.FromDateTime(DateTime.Today)
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
        }

        return cliente;
    }

    private static string SoloDigitos(string? valor) => new((valor ?? "").Where(char.IsDigit).ToArray());
}
