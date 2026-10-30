using System.ComponentModel.DataAnnotations;

namespace HosvetKardex.Web.Models;

public class UsuarioModel
{
    public int UserNumero { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public string UserClave { get; set; } = string.Empty;
    public string UserNombre { get; set; } = string.Empty;
    public string UserIniciales { get; set; } = string.Empty;
    public int UserTipoUserNumero { get; set; }
    public int UserEspNumero { get; set; }
    public string UserEstado { get; set; } = "A";
    public DateTime? UserFechaCambio { get; set; }
    public string? UserObservaciones { get; set; }
}

public class MenuItemModel
{
    public int Id { get; set; }
    public string SecNuevoMenu { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? PadreNuevoMenu { get; set; }
    public bool Habilitado { get; set; }
    public string? Url { get; set; }
    public List<MenuItemModel> Children { get; set; } = [];
}

public class RolUsuarioModel
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class UsuarioAdministracionModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string Convencional { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public int RolId { get; set; } = 2;
    public int EspecialidadId { get; set; } = 1;
    public string Especialidad { get; set; } = string.Empty;
    public string TipoMedico { get; set; } = "P";
    public string Observaciones { get; set; } = string.Empty;
    public List<int> LocalidadesIds { get; set; } = [];
    public string Estado { get; set; } = "A";
}

public class PacienteHeaderDto
{
    public int Nhcl { get; set; }
    public int OrdenAtencionId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteCedula { get; set; } = string.Empty;
    public int ClienteCodigo { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Origen { get; set; } = "Doméstico";
    public DateTime? FechaNacimiento { get; set; }
    public int EdadAnios { get; set; }
    public int EdadMeses { get; set; }
    public int Anios => EdadAnios;
    public int Meses => EdadMeses;
    public string EdadTexto => $"{EdadAnios}a {EdadMeses}m";
    public string Raza { get; set; } = "Mestizo";
    public string PropietarioNombre => ClienteNombre;
    public string TipoOrigen => Origen;
    public string MedicoResponsable { get; set; } = string.Empty;
    public string Cierre { get; set; } = "N";
    public bool Fallecido { get; set; }
    public DateTime? FechaFallecido { get; set; }
    public int? MotivoFallece { get; set; }
}

public class ViaAdministracionDto
{
    public int Id { get; set; }
    public string Via { get; set; } = string.Empty;
}

public class FuncionEnfermeriaDto
{
    public int TipUserNumero { get; set; }
    public string TipUserDescripcion { get; set; } = string.Empty;
}

public class FuncionarioDto
{
    public int UserNumero { get; set; }
    public string UserNombre { get; set; } = string.Empty;
    public string UserIniciales { get; set; } = string.Empty;
}

public class FarmacoStockDto
{
    public string Id { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TaxGroupCode { get; set; } = string.Empty;
    public decimal Inventory { get; set; }
    public decimal Available { get; set; }
    public decimal UnitPrice { get; set; }
    public string BaseUnitOfMeasureCode { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class KardexRegistroInputModel
{
    [Required(ErrorMessage = "Debe seleccionar un producto del catálogo.")]
    public string CodigoProd { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción del producto es obligatoria.")]
    public string ProdDescripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese las indicaciones o frecuencia.")]
    public string IndicacionesProd { get; set; } = string.Empty;

    [Range(0.01, 1000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal CantProd { get; set; } = 1;

    [Required(ErrorMessage = "Seleccione la vía de administración.")]
    public string Via { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione la fecha de administración.")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Seleccione la hora de administración.")]
    public TimeOnly Hora { get; set; } = TimeOnly.FromDateTime(DateTime.Now);

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el funcionario responsable.")]
    public int FuncionarioId { get; set; }

    [Required(ErrorMessage = "Seleccione la función o cargo.")]
    public string Funcion { get; set; } = string.Empty;

    public decimal PrecioProd { get; set; }
    public decimal StockDisponible { get; set; }
    public string Iva { get; set; } = string.Empty;
    public string IdDinamics { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public string Bodega { get; set; } = string.Empty;
    public string Tipo { get; set; } = "FARMACO";
}

public class ProductoUtilizadoDto
{
    public int Id { get; set; }
    public int Orden { get; set; }
    public int Nhcl { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal Precio { get; set; }
    public string Indicaciones { get; set; } = string.Empty;
    public string Via { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public DateTime FechaRaw { get; set; }
    public string Hora { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string Funcion { get; set; } = string.Empty;
    public string Funcionario { get; set; } = string.Empty;
    public string Bodega { get; set; } = string.Empty;
}

public class HoraInicialesFuncionDto
{
    public string Hora { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string Funcion { get; set; } = string.Empty;
    public bool EsTurnoNocturno { get; set; }
}

public class KardexSabanaItemDto
{
    public int NumeroFila { get; set; }
    public string IdProd { get; set; } = string.Empty;
    public string DescripcionCompleta { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string Via { get; set; } = string.Empty;
    public string Indicacion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public DateTime FechaInicio { get; set; }
    public List<DateTime> FechasColumnas { get; set; } = [];
    public List<List<HoraInicialesFuncionDto>> AdministracionesPorDia { get; set; } = [];
}

public class OrdenActivaResumenDto
{
    public int OrdenAtencionId { get; set; }
    public int IdOrdenAtencion => OrdenAtencionId;
    public int Nhcl { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public stringCedulaDto? Cedula { get; set; }
    public string Especie { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public DateTime FechaOrden { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;
}

public record stringCedulaDto(string Valor);
