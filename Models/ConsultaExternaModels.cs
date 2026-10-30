using System.ComponentModel.DataAnnotations;

namespace HosvetKardex.Web.Models;

// ============================================================================
// 1. FICHA MÉDICA / HISTORIA CLÍNICA SOAP (Reemplaza vt_hisDatMedicos.aspx - 11,291 líneas)
// ============================================================================
public class FichaMedicaCompletaDto
{
    public int Sec { get; set; }
    public int MedOrden { get; set; }
    public int MedNhcl { get; set; }
    public int MedIdUser { get; set; }
    public int MedCliente { get; set; }
    public int MedEspecialidad { get; set; } = 1;
    public int MedEdadAnios { get; set; }
    public int MedEdadMeses { get; set; }
    public int MedLocalidad { get; set; } = 1;
    public DateTime MedFecha { get; set; } = DateTime.Now;
    public string MedTipo { get; set; } = "HDEV"; // HDEV o TUERI
    public string MedEspecie { get; set; } = string.Empty;
    public string MedTipoConsulta { get; set; } = "Consulta Externa";

    // Anamnesis y Evaluación SOAP
    [Required(ErrorMessage = "El motivo de consulta es obligatorio")]
    public string MedMotivo { get; set; } = string.Empty;
    public string MedAnamnesis { get; set; } = string.Empty; // Subjetivo
    public string MedAntecedente { get; set; } = string.Empty;
    public string MedObjetivo { get; set; } = string.Empty;
    public string MedAnalisis { get; set; } = string.Empty;
    public string MedPlan { get; set; } = string.Empty;
    public string Tratamiento { get; set; } = string.Empty;
    public string ObservacionesYRecomendaciones { get; set; } = string.Empty;
    public string MedApuntes { get; set; } = string.Empty;

    // Constantes Fisiológicas y Examen Físico Clínico
    public decimal Peso { get; set; }
    public decimal Temperatura { get; set; } = 38.5m;
    public decimal FrecuenciaCardiaca { get; set; } = 90;
    public decimal FrecuenciaRespiratoria { get; set; } = 24;
    public string Mucosas { get; set; } = "Rosadas y húmedas";
    public int TiempoLlenadoCapilar { get; set; } = 2; // seg
    public int Deshidratacion { get; set; } // %
    public string ReflejoTusigeno { get; set; } = "Negativo";
    public string Auscultacion { get; set; } = "Normal sin soplos ni ruidos agregados";
    public string PalpacionTiroides { get; set; } = "Sin alteraciones";
    public int IndiceCondicionCorporal { get; set; } = 5; // Escala 1 a 9
    public string Cp { get; set; } = "Normal";
    public string Linfonodos { get; set; } = "Normales / Simétricos";
    public string PalpacionAbdominal { get; set; } = "Blando, no doloroso";
    public string PielYAnexos { get; set; } = "Íntegra, pelaje normal";
    public string CavidadOral { get; set; } = "Sin alteraciones";
    public string Otros { get; set; } = string.Empty;

    // Triaje de Temperamento, Pronóstico y Control
    public string Temperamento { get; set; } = "TRANQUILO";
    public string ColorTriaje { get; set; } = "imagenes/temperamento/blanco.png";
    public string Esterilizado { get; set; } = "N";
    public string MedPronostico { get; set; } = "Bueno";
    public string RequiereCirugia { get; set; } = "N"; // med_pregunta
    public string EstadoParte { get; set; } = "PENDIENTE";
    public string EsEmergencia { get; set; } = "N"; // S = genera control médico
    public DateTime? MedFechaControl { get; set; }
    public string? MedFechaControlStr => MedFechaControl?.ToString("yyyy-MM-dd");
    public string ContpSi => MedFechaControl.HasValue ? "S" : "N";
    public string ContpNo => MedFechaControl.HasValue ? "N" : "S";
    public string IndicacionesGeneralesReceta { get; set; } = string.Empty;
}

public class DiagnosticoCie10Dto
{
    public int Sec { get; set; }
    public int Nhcl { get; set; }
    public int IdOrden { get; set; }
    public string CodigoDiag { get; set; } = string.Empty;
    public string NombreCie10 { get; set; } = string.Empty;
    public string DescripcionObservacion { get; set; } = string.Empty;
    public string TipoDiag { get; set; } = "Definitivo"; // Presuntivo / Definitivo
}

public class CatalogoCie10ItemDto
{
    public int CieCodigo { get; set; }
    public string CieDiagnostico { get; set; } = string.Empty;
    public int CieGrupoDiag { get; set; }
    public string GrupoDescripcion { get; set; } = string.Empty;
}

public class AntecedenteClinicoDto
{
    public int AntecedenteId { get; set; }
    public int Nhcl { get; set; }
    public int Orden { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Descripcion { get; set; } = string.Empty;
    public string Alimentacion { get; set; } = string.Empty;
    public string ContactoMascotas { get; set; } = string.Empty;
    public string NombreMedico { get; set; } = string.Empty;
}

public class VacunaPacienteDto
{
    public int VacunaId { get; set; }
    public int Nhcl { get; set; }
    public int Orden { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public DateTime? FechaRevacunacion { get; set; } = DateTime.Today.AddYears(1);
    public int IdProducto { get; set; }
    public string NombreVacuna { get; set; } = string.Empty;
    public string ObservacionVacuna { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Vacunación";
    public string NombreMedico { get; set; } = string.Empty;
}

// ============================================================================
// 2. EVOLUCIÓN MÉDICA HOSPITALARIA Y AMBULATORIA (Reemplaza vt_evoluciones.aspx)
// ============================================================================
public class EvolucionMedicaDto
{
    public int IdEvolucion { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Hora { get; set; } = DateTime.Now.ToString("HH:mm");
    [Required(ErrorMessage = "La descripción clínica de la evolución es obligatoria")]
    public string Descripcion { get; set; } = string.Empty;
    public int IdEscogido { get; set; }
    public int IdUser { get; set; }
    public string NombreMedico { get; set; } = string.Empty;
    public string EstadoEvolucion { get; set; } = "A";

    // Resumen de órdenes vinculadas a esta evolución
    public List<ProductoQuirofanoDto> MedicamentosAsociados { get; set; } = new();
    public List<OrdenExamenEvolucionDto> ExamenesLabAsociados { get; set; } = new();
    public List<OrdenExamenEvolucionDto> ExamenesImagenAsociados { get; set; } = new();
}

public class OrdenExamenEvolucionDto
{
    public int Sec { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public int IdEvolucion { get; set; }
    public string CodigoProd { get; set; } = string.Empty;
    public string DescripcionExamen { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal Precio { get; set; }
    public string Estado { get; set; } = "Por Facturar";
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string TipoCategoria { get; set; } = "LAB"; // LAB o RX
}

// ============================================================================
// 3. RECETARIO MÉDICO CON ORDEN Y EXTERNO (Reemplaza vt_recetasnew y vt_RecetassinOrden)
// ============================================================================
public class RecetaMedicamentoItemDto
{
    public int Sec { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string CodigoProd { get; set; } = string.Empty;
    public string Descripcionamento { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    public string CantidadLetras { get; set; } = "UNO";
    public string Indicaciones { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string Bodega { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Tipo { get; set; } = "M"; // 'M' = Receta, 'I' = Insumo
}

public class RecetaResumenListadoDto
{
    public int IdOrdenOFicha { get; set; }
    public int Nhcl { get; set; }
    public string FechaIngreso { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EsRecetaExterna { get; set; }
}

// ============================================================================
// 4. AUTORIZACIONES, CONSENTIMIENTOS Y ALTA MÉDICA (vt_autorizaciones + qvt_formulario_alta)
// ============================================================================
public class FormatoPlantillaDto
{
    public int FormId { get; set; }
    public string FormNombre { get; set; } = string.Empty;
    public string FormTipo { get; set; } = string.Empty;
    public string FormArchivoHtml { get; set; } = string.Empty;
}

public class AutorizacionClinicaDto
{
    public int AutorizacionId { get; set; }
    public int Nhcl { get; set; }
    public int Orden { get; set; }
    public int Cliente { get; set; }
    public int IdUser { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Documento { get; set; } = "Consentimiento Informado";
    public string Tipo { get; set; } = "HDEV";
    public string DetalleHtml { get; set; } = string.Empty;
    public string NombreMedico { get; set; } = string.Empty;
}

public class FormularioAltaMedicaDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime FechaIng { get; set; } = DateTime.Now;
    public int UserIng { get; set; }
    public string CuidadosGenerales { get; set; } = string.Empty;
    public string Permitido { get; set; } = string.Empty;
    public string NoPermitido { get; set; } = string.Empty;
    public string Dieta { get; set; } = string.Empty;
    public string Medicacion { get; set; } = string.Empty;
}

// ============================================================================
// 5. APUNTES LONGITUDINALES A HISTORIAS CLINICAS (vt_apuntesahistorias / vt_notas)
// ============================================================================
public class PacienteApunteHistoriaDto
{
    public int Nhcl { get; set; }
    public int IdCliente { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Tipo { get; set; } = "HDEV";
    public DateTime? FechaNacimiento { get; set; }

    public string EdadTexto
    {
        get
        {
            if (!FechaNacimiento.HasValue || FechaNacimiento.Value.Date > DateTime.Today) return "No registrada";
            var nacimiento = FechaNacimiento.Value.Date;
            var hoy = DateTime.Today;
            var anios = hoy.Year - nacimiento.Year;
            if (nacimiento > hoy.AddYears(-anios)) anios--;
            var fechaAniversario = nacimiento.AddYears(Math.Max(0, anios));
            var meses = Math.Max(0, ((hoy.Year - fechaAniversario.Year) * 12) + hoy.Month - fechaAniversario.Month);
            return $"{Math.Max(0, anios)} año(s), {meses} mes(es)";
        }
    }
}

public class ApunteHistoriaClinicaDto
{
    public int Id { get; set; }
    public int Nhcl { get; set; }
    public int IdCliente { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Observacion { get; set; } = string.Empty;
    public string RegistradoPor { get; set; } = string.Empty;
    public string Tipo { get; set; } = "HDEV";
}
