using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Simetric.Models.EContax;

public sealed class EContaxCuentaContable
{
    [Key]
    public int IdCuenta { get; set; }
    public int IdEmpresa { get; set; }
    [Required, MaxLength(40)]
    public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(250)]
    public string Nombre { get; set; } = string.Empty;
    [MaxLength(40)]
    public string? CodigoPadre { get; set; }
    public int Nivel { get; set; } = 1;
    [Required, MaxLength(20)]
    public string TipoCuenta { get; set; } = "ACTIVO"; // ACTIVO, PASIVO, PATRIMONIO, INGRESO, GASTO
    [Required, MaxLength(15)]
    public string Naturaleza { get; set; } = "DEUDORA"; // DEUDORA, ACREEDORA
    public bool EsMovimiento { get; set; } = true;
    public bool AceptaAuxiliar { get; set; } = true;
    [Required, MaxLength(20)]
    public string ClasificacionFlujo { get; set; } = "OPERACION"; // EFECTIVO, OPERACION, INVERSION, FINANCIAMIENTO, NINGUNO
    public bool Activa { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}

public sealed class EContaxConfigCuentaContable
{
    [Key]
    public int IdConfig { get; set; }
    public int IdEmpresa { get; set; }
    [Required, MaxLength(60)]
    public string Clave { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string Descripcion { get; set; } = string.Empty;
    [Required, MaxLength(40)]
    public string CodigoCuenta { get; set; } = string.Empty;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

public sealed class EContaxAsientoContable
{
    [Key]
    public int IdAsiento { get; set; }
    public int IdEmpresa { get; set; }
    public int? IdSucursal { get; set; }
    [Required, MaxLength(30)]
    public string NumeroAsiento { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Required, MaxLength(25)]
    public string TipoAsiento { get; set; } = "DIARIO"; // DIARIO, INGRESO, EGRESO, AJUSTE, CIERRE, APERTURA, REVERSO
    [Required, MaxLength(40)]
    public string ModuloOrigen { get; set; } = "MANUAL";
    public int? IdDocumentoOrigen { get; set; }
    [MaxLength(80)]
    public string? ReferenciaDocumento { get; set; }
    [Required, MaxLength(500)]
    public string Glosa { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDebe { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalHaber { get; set; }
    [Required, MaxLength(20)]
    public string Estado { get; set; } = "CONTABILIZADO"; // BORRADOR, CONTABILIZADO, REVERSADO, ANULADO
    public int? IdAsientoReverso { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int? ActualizadoPor { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public List<EContaxAsientoDetalle> Detalles { get; set; } = new();
}

public sealed class EContaxAsientoDetalle
{
    [Key]
    public int IdDetalle { get; set; }
    public int IdAsiento { get; set; }
    public int IdCuenta { get; set; }
    [Required, MaxLength(40)]
    public string CodigoCuenta { get; set; } = string.Empty;
    [Required, MaxLength(250)]
    public string NombreCuenta { get; set; } = string.Empty;
    public int Linea { get; set; }
    [MaxLength(300)]
    public string? Detalle { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Debe { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Haber { get; set; }
    [MaxLength(30)]
    public string? TipoTercero { get; set; }
    public int? IdTercero { get; set; }
    [MaxLength(30)]
    public string? IdentificacionTercero { get; set; }
    [MaxLength(200)]
    public string? NombreTercero { get; set; }
    [MaxLength(60)]
    public string? CentroCosto { get; set; }
}

public sealed class EContaxCuentaBancaria
{
    [Key]
    public int IdCuentaBanco { get; set; }
    public int IdEmpresa { get; set; }
    public int? IdSucursal { get; set; }
    [Required, MaxLength(120)]
    public string Banco { get; set; } = string.Empty;
    [Required, MaxLength(60)]
    public string NumeroCuenta { get; set; } = string.Empty;
    [Required, MaxLength(30)]
    public string TipoCuenta { get; set; } = "CORRIENTE"; // CORRIENTE, AHORROS, CAJA_CHICA
    [Required, MaxLength(10)]
    public string Moneda { get; set; } = "USD";
    [MaxLength(40)]
    public string CodigoCuentaContable { get; set; } = "1.1.1.3.1";
    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoInicial { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoActual { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public sealed class EContaxMovimientoBanco
{
    [Key]
    public int IdMovimientoBanco { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCuentaBanco { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Required, MaxLength(30)]
    public string Tipo { get; set; } = "DEPOSITO"; // DEPOSITO, EGRESO, TRANSFERENCIA, NOTA_CREDITO_BANCARIA, NOTA_DEBITO_BANCARIA, COMISION
    [MaxLength(60)]
    public string? NumeroReferencia { get; set; }
    [MaxLength(200)]
    public string? Beneficiario { get; set; }
    [Required, MaxLength(300)]
    public string Concepto { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }
    public bool Conciliado { get; set; }
    public int? IdConciliacion { get; set; }
    [MaxLength(40)]
    public string? ModuloOrigen { get; set; }
    public int? IdDocumentoOrigen { get; set; }
    public int? IdAsiento { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public sealed class EContaxConciliacionBancaria
{
    [Key]
    public int IdConciliacion { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCuentaBanco { get; set; }
    [Required, MaxLength(20)]
    public string Periodo { get; set; } = string.Empty;
    public DateTime FechaCorte { get; set; } = DateTime.Today;
    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoLibro { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoBanco { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Diferencia { get; set; }
    [Required, MaxLength(20)]
    public string Estado { get; set; } = "BORRADOR"; // BORRADOR, CERRADA
    [MaxLength(400)]
    public string? Observacion { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public sealed class EContaxAnticipo
{
    [Key]
    public int IdAnticipo { get; set; }
    public int IdEmpresa { get; set; }
    public int? IdSucursal { get; set; }
    [Required, MaxLength(30)]
    public string NumeroAnticipo { get; set; } = string.Empty;
    [Required, MaxLength(20)]
    public string Tipo { get; set; } = "CLIENTE"; // CLIENTE, PROVEEDOR
    public int? IdTercero { get; set; }
    [Required, MaxLength(30)]
    public string IdentificacionTercero { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string NombreTercero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoDisponible { get; set; }
    [Required, MaxLength(30)]
    public string FormaPago { get; set; } = "TRANSFERENCIA";
    public int? IdCuentaBanco { get; set; }
    [MaxLength(80)]
    public string? ReferenciaPago { get; set; }
    [MaxLength(400)]
    public string? Observacion { get; set; }
    [Required, MaxLength(20)]
    public string Estado { get; set; } = "ACTIVO"; // ACTIVO, PARCIAL, APLICADO, ANULADO
    public int? IdAsiento { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public sealed class EContaxAnticipoAplicacion
{
    [Key]
    public int IdAplicacion { get; set; }
    public int IdAnticipo { get; set; }
    public int IdEmpresa { get; set; }
    [Required, MaxLength(30)]
    public string TipoDocumentoDestino { get; set; } = string.Empty; // FACTURA_VENTA, CUENTA_COBRAR, FACTURA_COMPRA, CUENTA_PAGAR
    public int? IdDocumentoDestino { get; set; }
    [Required, MaxLength(80)]
    public string ReferenciaDestino { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoAplicado { get; set; }
    [MaxLength(300)]
    public string? Observacion { get; set; }
    public int? IdAsiento { get; set; }
    public int CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
