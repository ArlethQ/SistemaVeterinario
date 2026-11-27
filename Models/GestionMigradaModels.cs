namespace HosvetKardex.Web.Models;

public sealed class ReporteAtencionDto { public int Orden { get; set; } public int Nhcl { get; set; } public DateTime Fecha { get; set; } public int IdMedico { get; set; } public string Medico { get; set; }=""; public string Paciente { get; set; }=""; public string Cliente { get; set; }=""; public string Diagnostico { get; set; }=""; public string CodigoDiagnostico { get; set; }=""; public string OtrosDiagnostico { get; set; }=""; public int IdLocalidad { get; set; } public string Localidad { get; set; }=""; public int IdEspecialidad { get; set; } public string Especialidad { get; set; }=""; public string Direccion { get; set; }=""; public string RegistradoPor { get; set; }=""; }
public sealed class ReporteDiagnosticoDto { public int Orden { get; set; } public int Nhcl { get; set; } public DateTime Fecha { get; set; } public string Paciente { get; set; }=""; public string Cliente { get; set; }=""; public string Medico { get; set; }=""; public string CodigoDiagnostico { get; set; }=""; public string Diagnostico { get; set; }=""; public string Tipo { get; set; }=""; public int EdadAnios { get; set; } public int EdadMeses { get; set; } public string Sexo { get; set; }=""; public int IdLocalidad { get; set; } }
public sealed class ParteOperatorioResumenDto { public int Id { get; set; } public int IdOrden { get; set; } public DateTime Fecha { get; set; } public int Nhcl { get; set; } public string Paciente { get; set; }=""; public string Propietario { get; set; }=""; public string Procedimiento { get; set; }=""; public string Diagnostico { get; set; }=""; }
public sealed class HorarioMedicoDto { public int Id { get; set; } public int IdMedico { get; set; } public string Medico { get; set; }=""; public string Dia { get; set; }="Lunes"; public string DiaIngles { get; set; }="Monday"; public string Desde { get; set; }="08:00"; public string Hasta { get; set; }="17:00"; public int Intervalo { get; set; }=20; public int IdLocalidad { get; set; } public string Localidad { get; set; }=""; public string Piso { get; set; }=""; public string Consultorio { get; set; }=""; public DateTime? VigenciaDesde { get; set; } public DateTime? VigenciaHasta { get; set; } }
public sealed class PisoClinicoDto { public int Id { get; set; } public string Nombre { get; set; }=""; public int IdLocalidad { get; set; } public string Localidad { get; set; }=""; public string Estado { get; set; }="A"; }
public sealed class ConsultorioClinicoDto { public int Id { get; set; } public int IdPiso { get; set; } public string Piso { get; set; }=""; public string Nombre { get; set; }=""; public string Estado { get; set; }="A"; }
public sealed class JaulaClinicaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public int IdLocalidad { get; set; }
    public string Localidad { get; set; } = "";
    public int IdPiso { get; set; }
    public string Piso { get; set; } = "";
    public int IdConsultorio { get; set; }
    public string Consultorio { get; set; } = "";
    public string Observacion { get; set; } = "";
    public bool Ocupada { get; set; }
    public int? NhclActual { get; set; }
    public int? OrdenActual { get; set; }
    public string PacienteActual { get; set; } = "";
    public string Estado { get; set; } = "A";
}
public sealed class SalaQuirofanoDto
{
    public int Id { get; set; }
    public int IdPiso { get; set; }
    public int IdLocalidad { get; set; }
    public string Codigo { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string Piso { get; set; } = "";
    public string Localidad { get; set; } = "";
    public bool Activa { get; set; } = true;
}

public sealed class CirugiaProgramadaDto
{
    public int Id { get; set; }
    public int? IdOrden { get; set; }
    public int? IdSala { get; set; }
    public string Sala { get; set; } = "";
    public int IdMedico { get; set; }
    public string Medico { get; set; } = "";
    public int? IdAnestesiologo { get; set; }
    public string Anestesiologo { get; set; } = "";
    public string CirujanosAdicionalesIds { get; set; } = "";
    public string CirujanosAdicionalesNombres { get; set; } = "";
    public List<int> IdsCirujanosAdicionales { get; set; } = [];
    public int Nhcl { get; set; }
    public string Paciente { get; set; } = "";
    public int IdProcedimiento { get; set; }
    public string Procedimiento { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string HoraInicio { get; set; } = "08:00";
    public string HoraFin { get; set; } = "09:00";
    public int IdLocalidad { get; set; }
    public string Estado { get; set; } = "A";

    public bool IncluyeMedico(int idMedico)
    {
        if (idMedico <= 0) return false;
        if (IdMedico == idMedico || IdAnestesiologo == idMedico) return true;
        if (IdsCirujanosAdicionales.Contains(idMedico)) return true;
        if (string.IsNullOrWhiteSpace(CirujanosAdicionalesIds)) return false;
        foreach (var part in CirujanosAdicionalesIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id == idMedico) return true;
        }
        return false;
    }
}
public sealed class CatalogoOperativoDto { public int Id { get; set; } public string Descripcion { get; set; }=""; public string Codigo { get; set; }=""; public decimal? Precio { get; set; } public decimal? Precio2 { get; set; } public decimal? Precio3 { get; set; } public int? IdGrupo { get; set; } public string Grupo { get; set; }=""; public int? IdSubgrupo { get; set; } public string Subgrupo { get; set; }=""; public string Tipo { get; set; }=""; public bool Inventario { get; set; } = true; public bool SujetoFiscalizacion { get; set; } public string UnidadMedida { get; set; } = "UNIDAD"; public decimal FactorConversion { get; set; } = 1m; public string CodigoImpuesto { get; set; }=""; public string PorcentajeImpuesto { get; set; }=""; public string Estado { get; set; }="A"; public bool EsServicio => Tipo == "S" || Tipo == "SERVICIO"; }
public sealed class TarifaIvaDto { public string Codigo { get; set; }=""; public string Descripcion { get; set; }=""; public string Valor { get; set; }=""; }
public sealed class BitacoraDto { public decimal Id { get; set; } public DateTime Fecha { get; set; } public int IdUsuario { get; set; } public string Usuario { get; set; }=""; public string Detalle { get; set; }=""; public string Ip { get; set; }=""; public string Equipo { get; set; }=""; }
public sealed class SelectorClinicoDto { public int Id { get; set; } public string Descripcion { get; set; }=""; public int? IdRelacionado { get; set; } public string Rol { get; set; } = ""; }
public sealed class ExcepcionHorarioDto { public int Id { get; set; } public int IdMedico { get; set; } public string Medico { get; set; }=""; public string Motivo { get; set; }=""; public DateTime Fecha { get; set; }=DateTime.Today; public string Desde { get; set; }="00:00"; public string Hasta { get; set; }="24:00"; public int IdLocalidad { get; set; } public string Localidad { get; set; }=""; public string Estado { get; set; }="A"; }
public sealed class UsuarioLocalidadAsignadaDto { public int IdUsuario { get; set; } public int IdLocalidad { get; set; } }
