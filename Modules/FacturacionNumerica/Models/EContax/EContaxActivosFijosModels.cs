using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Simetric.Models.EContax;

[Table("ECONTAX_AF_CATEGORIA")]
public sealed class EContaxAfCategoria
{
    [Key] public int Id { get; set; }
    public int IdEmpresa { get; set; }
    [MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    public int VidaUtilMeses { get; set; }
    public int IdCuentaActivo { get; set; }
    public int IdCuentaGasto { get; set; }
    public int IdCuentaAcumulada { get; set; }
}

[Table("ECONTAX_AF_ACTIVO")]
public sealed class EContaxAfActivo
{
    [Key] public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCategoria { get; set; }
    [MaxLength(40)] public string Codigo { get; set; } = string.Empty;
    [MaxLength(180)] public string Nombre { get; set; } = string.Empty;
    public DateTime PuestaEnServicio { get; set; }
    [Precision(18, 2)] public decimal ValorResidual { get; set; }
    public int VidaUtilMeses { get; set; }
    [MaxLength(160)] public string Referencia { get; set; } = string.Empty;
}

[Table("ECONTAX_AF_MOVIMIENTO")]
public sealed class EContaxAfMovimiento
{
    [Key] public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdActivo { get; set; }
    public DateTime Fecha { get; set; }
    [MaxLength(20)] public string Tipo { get; set; } = string.Empty;
    [Precision(18, 2)] public decimal CambioCosto { get; set; }
    [Precision(18, 2)] public decimal CambioDepreciacion { get; set; }
    [MaxLength(120)] public string Ubicacion { get; set; } = string.Empty;
    [MaxLength(120)] public string Responsable { get; set; } = string.Empty;
    [MaxLength(250)] public string Motivo { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public DateTime RegistradoUtc { get; set; }
    public Guid IdOperacion { get; set; }
}

[Table("ECONTAX_AF_DEPRECIACION_LOTE")]
public sealed class EContaxAfDepreciacionLote
{
    [Key] public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public DateTime Periodo { get; set; }
    [MaxLength(20)] public string Estado { get; set; } = string.Empty;
    public int? IdAsiento { get; set; }
    public int? IdAsientoReverso { get; set; }
    public int CreadoPor { get; set; }
    public int? AprobadoPor { get; set; }
    public int? ContabilizadoPor { get; set; }
    public int? ReversadoPor { get; set; }
    public DateTime CreadoUtc { get; set; }
    public DateTime? AprobadoUtc { get; set; }
    public DateTime? ContabilizadoUtc { get; set; }
    public DateTime? ReversadoUtc { get; set; }
}

[Table("ECONTAX_AF_DEPRECIACION_DETALLE")]
public sealed class EContaxAfDepreciacionDetalle
{
    [Key] public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdLote { get; set; }
    public int IdActivo { get; set; }
    [Precision(18, 2)] public decimal Importe { get; set; }
    public int IdCuentaGasto { get; set; }
    public int IdCuentaAcumulada { get; set; }
}
