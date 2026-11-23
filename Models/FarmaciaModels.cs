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
