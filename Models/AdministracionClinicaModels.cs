namespace HosvetKardex.Web.Models;

public sealed class CatalogoAdministrativoDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = "A";
    public int? IdPadre { get; set; }
    public string PadreDescripcion { get; set; } = string.Empty;
}

public sealed record TipoCatalogoAdministrativo(string Clave, string Nombre, bool RequierePadre = false);

public sealed class RecordatorioVacunaDto
{
    public int Nhcl { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string QuienManda { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string CorreoEnviado { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public DateTime? ProximaRevision { get; set; }
    public string Telefono { get; set; } = string.Empty;
}

public sealed class AlertaClinicaDto
{
    public int Orden { get; set; }
    public int Nhcl { get; set; }
    public DateTime FechaEnvio { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Pronostico { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
}

public sealed class PacienteFallecidoDto
{
    public DateTime Fecha { get; set; }
    public int Nhcl { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class HistorialPacienteItemDto
{
    public string Categoria { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int NumeroVeces { get; set; }
}

public sealed class MedicionCrecimientoDto
{
    public DateTime Fecha { get; set; }
    public decimal? Peso { get; set; }
    public decimal? FrecuenciaCardiaca { get; set; }
    public decimal? FrecuenciaRespiratoria { get; set; }
    public int EdadMeses { get; set; }
}

public sealed class PacienteResumenCrecimientoDto
{
    public int Nhcl { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public string Cliente { get; set; } = string.Empty;
}

public sealed class LocalidadAdministrativaDto
{
    public int Id { get; set; }
    public string Localidad { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public string Administrador { get; set; } = string.Empty;
    public string Estado { get; set; } = "A";
    public string Empresa { get; set; } = string.Empty;
    public string Prefijo { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int IdCiudad { get; set; }
    public string Ciudad { get; set; } = string.Empty;
    public int IdProvincia { get; set; }
    public string Provincia { get; set; } = string.Empty;
}

public sealed class EspecialidadLocalidadDto
{
    public int Id { get; set; }
    public int IdEspecialidad { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public int IdLocalidad { get; set; }
    public string Localidad { get; set; } = string.Empty;
    public string Estado { get; set; } = "A";
}

public sealed class IngresoCirugiaDto { public int Sec { get; set; } public int Nhcl { get; set; } public int IdOrden { get; set; } public DateTime Fecha { get; set; }=DateTime.Now; public string Sala { get; set; }=""; public string Entra { get; set; }=""; public string Sale { get; set; }=""; public string Transporte { get; set; }=""; public string Posicion { get; set; }=""; public string Observaciones { get; set; }=""; }
public sealed class DespachoClinicoDto { public int IdOrden { get; set; } public int Nhcl { get; set; } public string Paciente { get; set; }=""; public string Propietario { get; set; }=""; public DateTime Fecha { get; set; } public int TipoDespacho { get; set; } public string Etapa { get; set; }=""; public string Tipo => TipoDespacho==1?"Imagenología":"Laboratorio"; }
public sealed class FisioterapiaOrdenDto { public int Sec { get; set; } public int Nhcl { get; set; } public int IdOrden { get; set; } public int IdProducto { get; set; } public string Descripcion { get; set; }=""; public string Observaciones { get; set; }=""; public decimal Cantidad { get; set; }=1; public DateTime Fecha { get; set; }=DateTime.Today; }
public sealed class IStatDto
{
    public int Id { get; set; }
    public int Nhcl { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int IdUser { get; set; }
    public string QuienIngresa { get; set; } = "";
    public string Especialidad { get; set; } = "";

    // Panel Gases y Ácido-Base
    public string Ph { get; set; } = "";
    public string Pressure { get; set; } = ""; // pCO2
    public string Oxygen { get; set; } = ""; // pO2
    public string Bicarbonato { get; set; } = ""; // HCO3
    public string Carbon { get; set; } = ""; // TCO2
    public string Base { get; set; } = ""; // BE
    public string Saturation { get; set; } = ""; // sO2
    public string Lactato { get; set; } = ""; // Lac

    // Panel Electrolitos y Minerales
    public string Sodio { get; set; } = ""; // Na+
    public string Potasio { get; set; } = ""; // K+
    public string Chloride { get; set; } = ""; // Cl-
    public string Ionized { get; set; } = ""; // iCa
    public string Gap { get; set; } = ""; // Anion Gap

    // Panel Bioquímica y Hematología
    public string Hematocrito { get; set; } = ""; // Hto
    public string Hemoglobina { get; set; } = ""; // Hb
    public string Glucosa { get; set; } = ""; // Glu
    public string Urea { get; set; } = ""; // BUN / Urea
    public string Creatinina { get; set; } = ""; // Crea
}


