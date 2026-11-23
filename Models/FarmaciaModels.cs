namespace HosvetKardex.Web.Models;

public class FarmaciaOrdenColaDto
{
    public string OrigenTabla { get; set; } = "MON"; // "MON" = hvt_cabMonitoreoMedBC, "QUI" = qvt_productosUtilizados
    public int IdRegistro { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string CodigoProducto { get; set; } = string.Empty;
    public int CodigoProductoInt { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string UnidadMedida { get; set; } = "UNIDAD";
    public string Bodega { get; set; } = string.Empty;
    public string Via { get; set; } = string.Empty;
    public string Indicaciones { get; set; } = string.Empty;
    public string Solicitante { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Hora { get; set; } = string.Empty;
    public string EstadoFarmacia { get; set; } = "PENDIENTE"; // "PENDIENTE" o "ENTREGADO"
    public string DetalleEntrega { get; set; } = string.Empty;
    public decimal StockFisico { get; set; }
    public decimal StockReservado { get; set; }
    public decimal StockLibre => Math.Max(0, StockFisico - StockReservado);
}

public class FarmaciaVentaLineaInputDto
{
    public string IdProducto { get; set; } = string.Empty;
    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string Bodega { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "UNIDAD";
    public decimal FactorConversion { get; set; } = 1m;
    public bool PorPaquete { get; set; }
    public decimal CantidadIngresada { get; set; } = 1m;
    public decimal CantidadBase => PorPaquete && FactorConversion > 1m
        ? Math.Round(CantidadIngresada * FactorConversion, 4)
        : CantidadIngresada;
    public decimal PrecioUnitario { get; set; }
    public decimal StockDisponible { get; set; }
    public string Iva { get; set; } = "IVA0";
    public string Indicaciones { get; set; } = string.Empty;
    public decimal Subtotal => Math.Round(CantidadBase * PrecioUnitario, 2);
}

public class FarmaciaVentaMostradorInputDto
{
    public int? IdOrden { get; set; }
    public int? Nhcl { get; set; }
    public string ClienteReferencia { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public List<FarmaciaVentaLineaInputDto> Lineas { get; set; } = [];
}

public class FarmaciaVentaHistorialDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime Fecha { get; set; }
    public string Hora { get; set; } = string.Empty;
    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = "UNIDAD";
    public decimal PrecioUnitario { get; set; }
    public decimal Total => Math.Round(Cantidad * PrecioUnitario, 2);
    public string Bodega { get; set; } = string.Empty;
    public string ClienteReferencia { get; set; } = string.Empty;
    public string DespachadoPor { get; set; } = string.Empty;
}

public class FarmaciaStockSedeItemDto
{
    public int CodigoProducto { get; set; }
    public string CodigoPrincipal { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "UNIDAD";
    public decimal FactorConversion { get; set; } = 1m;
    public string Bodega { get; set; } = string.Empty;
    public decimal StockFisico { get; set; }
    public decimal StockReservado { get; set; }
    public decimal StockLibre => Math.Max(0, StockFisico - StockReservado);
    public decimal PrecioUnitario { get; set; }
    public decimal CostoPromedio { get; set; }
}

public class FarmaciaRecetaSoapOrdenDto
{
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public List<FarmaciaRecetaSoapItemDto> Items { get; set; } = [];
    public bool TienePendientes => Items.Any(x => x.EstaEnCola);
    public decimal TotalACobrar => Items.Where(x => x.EstaEnCola && x.SeVende && x.CantidadAVender > 0).Sum(x => x.SubtotalVenta);
}

public class FarmaciaRecetaSoapItemDto
{
    public int Sec { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public int CodigoProductoInt { get; set; }
    public string CodigoPrincipal { get; set; } = string.Empty;
    public string NombreRecetado { get; set; } = string.Empty;
    public string NombreProductoInventario { get; set; } = string.Empty;
    public string Indicaciones { get; set; } = string.Empty;
    public decimal CantidadRecetada { get; set; }
    public decimal CantidadReservadaLinea { get; set; }
    public string Estado { get; set; } = "Por Entregar"; // "Por Entregar", "Entregado", "Parcial", "No Comprado"
    public string Observaciones { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "UNIDAD";
    public decimal FactorConversion { get; set; } = 1m;
    public string Bodega { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public decimal StockFisicoSede { get; set; }
    public decimal StockReservadoTotalSede { get; set; }
    public decimal StockLibreSede => Math.Max(0, StockFisicoSede - StockReservadoTotalSede);

    public bool EstaEnCola => string.Equals(Estado, "Por Entregar", StringComparison.OrdinalIgnoreCase);

    // Máximo disponible para esta línea = lo que sigue libre en la sede + lo que ya tiene reservado esta línea
    public decimal MaximoDisponibleParaEstaLinea =>
        Math.Min(StockFisicoSede, StockLibreSede + (EstaEnCola ? CantidadReservadaLinea : 0m));

    public bool TieneProductoEnCatalogo => CodigoProductoInt > 0;
    public bool CubrePedidoCompleto => TieneProductoEnCatalogo && MaximoDisponibleParaEstaLinea >= CantidadRecetada;
    public bool StockParcial => TieneProductoEnCatalogo && MaximoDisponibleParaEstaLinea > 0 && MaximoDisponibleParaEstaLinea < CantidadRecetada;
    public bool SinStockEnSede => !TieneProductoEnCatalogo || MaximoDisponibleParaEstaLinea <= 0;
    public decimal FaltanteParaPedido => Math.Max(0, CantidadRecetada - MaximoDisponibleParaEstaLinea);

    // Campos editables por el encargado de Farmacia antes de completar la compra
    public bool SeVende { get; set; } = true;
    public decimal CantidadAVender { get; set; }
    public decimal SubtotalVenta => SeVende && CantidadAVender > 0 ? Math.Round(CantidadAVender * PrecioUnitario, 2) : 0m;
}
