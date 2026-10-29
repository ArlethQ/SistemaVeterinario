using System.ComponentModel.DataAnnotations;

namespace HosvetKardex.Web.Models;

// ============================================================================
// 1. EVALUACIÓN PRE-ANESTÉSICA (Reemplaza vt_PreAnestesia.aspx / qvt_preanestesia)
// ============================================================================
public class PreAnestesiaDto
{
    public int IdPreAnestesico { get; set; }
    public int Orden { get; set; }
    public int Nhcl { get; set; }
    public int IdUser { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.Now;

    // Clasificación ASA (1 a 5) y Estado General
    [Range(1, 5, ErrorMessage = "Seleccione Riesgo ASA entre 1 y 5")]
    public int Riesgo { get; set; } = 1;
    public int Ebueno { get; set; } = 1;
    public int Edeprimido { get; set; }
    public int Einsuficiente { get; set; }
    public string InformacionEspecial { get; set; } = string.Empty;
    public string GrupoSanguineo { get; set; } = string.Empty;

    // Exámenes Complementarios y Sistemas
    public string Uroanalisis { get; set; } = string.Empty;
    public string Hematologico { get; set; } = string.Empty;
    public string QuimicaSanguinea { get; set; } = string.Empty;
    public string SistemaRespiratorio { get; set; } = string.Empty;
    public string SistemaCirculatorio { get; set; } = string.Empty;
    public string SistemaNervioso { get; set; } = string.Empty;
    public string SistemasOtros { get; set; } = string.Empty;
    public string AnestesiasAnteriores { get; set; } = string.Empty;
    public string TerapiaAnterior { get; set; } = string.Empty;

    public DateTime? UltimaIngestion { get; set; } = DateTime.Now;
    public DateTime? HoraDolor { get; set; } = DateTime.Now;

    // Regiones Quirúrgicas / Bloqueos Regionales (0 o 1)
    public int Rcabeza { get; set; }
    public int Rsentidos { get; set; }
    public int Rendoscopias { get; set; }
    public int Rintradural { get; set; }
    public int Rextradural { get; set; }
    public int Rcuello { get; set; }
    public int Rextratoraxica { get; set; }
    public int Rintratoraxica { get; set; }
    public int Rabdomen { get; set; }
    public int Rextremidades { get; set; }
    public int Robstetricas { get; set; }
    public int Rperiniales { get; set; }

    public string Firma { get; set; } = string.Empty;

    // Conveniencia booleana para UI
    public bool CheckCabeza { get => Rcabeza == 1; set => Rcabeza = value ? 1 : 0; }
    public bool CheckSentidos { get => Rsentidos == 1; set => Rsentidos = value ? 1 : 0; }
    public bool CheckEndoscopias { get => Rendoscopias == 1; set => Rendoscopias = value ? 1 : 0; }
    public bool CheckIntradural { get => Rintradural == 1; set => Rintradural = value ? 1 : 0; }
    public bool CheckExtradural { get => Rextradural == 1; set => Rextradural = value ? 1 : 0; }
    public bool CheckCuello { get => Rcuello == 1; set => Rcuello = value ? 1 : 0; }
    public bool CheckExtratoraxica { get => Rextratoraxica == 1; set => Rextratoraxica = value ? 1 : 0; }
    public bool CheckIntratoraxica { get => Rintratoraxica == 1; set => Rintratoraxica = value ? 1 : 0; }
    public bool CheckAbdomen { get => Rabdomen == 1; set => Rabdomen = value ? 1 : 0; }
    public bool CheckExtremidades { get => Rextremidades == 1; set => Rextremidades = value ? 1 : 0; }
    public bool CheckObstetricas { get => Robstetricas == 1; set => Robstetricas = value ? 1 : 0; }
    public bool CheckPerineales { get => Rperiniales == 1; set => Rperiniales = value ? 1 : 0; }
}

public class ProductoQuirofanoDto
{
    public int Id { get; set; }
    public int OrdenId { get; set; }
    public int Nhcl { get; set; }
    public string Hora { get; set; } = DateTime.Now.ToString("HH:mm");
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string CodigoProd { get; set; } = string.Empty;
    public string ProdDescripcion { get; set; } = string.Empty;
    public decimal PrecioProd { get; set; }
    public int StockProd { get; set; }
    public string IndicacionesProd { get; set; } = string.Empty;
    public decimal CantProd { get; set; } = 1;
    public string Via { get; set; } = "IV";
    public string Iniciales { get; set; } = string.Empty;
    public string Funcion { get; set; } = string.Empty;
    public string Funcionario { get; set; } = string.Empty;
    public string Estado { get; set; } = "Vigente";
    public string Modulo { get; set; } = "PreAnestesia";
    public string MedBodega { get; set; } = string.Empty;
    public string MedMedida { get; set; } = "UND";
    public string MedIva { get; set; } = "IVA0";
    public string MedIdDinamics { get; set; } = string.Empty;
}

// ============================================================================
// 2. HOJA DE ANESTESIA INTRAOPERATORIA (Reemplaza vt_hojaanestesia.aspx - 20,425 líneas)
// ============================================================================
public class HojaAnestesiaCompletaDto
{
    public int IdRegistro { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public int NumFormulario { get; set; } = 1;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Sexo { get; set; } = string.Empty;
    public string Asa { get; set; } = string.Empty;
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public int TipoAnestesia { get; set; }
    public string Firma { get; set; } = string.Empty;
    public bool Atendido { get; set; }

    // Manejo de Vía Aérea (qvt_ManejoViaAnestesia)
    public bool MascaraFacial { get; set; }
    public bool CateterNasal { get; set; }
    public bool MascaraLaringea { get; set; }
    public string NumMascaraLaringea { get; set; } = string.Empty;
    public bool Traqueotomo { get; set; }
    public bool IntubOral { get; set; }
    public bool IntubNasal { get; set; }
    public string NumTubo { get; set; } = string.Empty;
    public bool IntubBalon { get; set; }
    public int DifCormack { get; set; }
    public int NIntentos { get; set; }
    public int VolTidal { get; set; }
    public string FR { get; set; } = string.Empty;
    public int O2 { get; set; }

    // Monitoreo Intraoperatorio (qvt_MonitoreoAnestesia)
    public bool EKG { get; set; }
    public bool SPO2 { get; set; }
    public bool CO2ET { get; set; }
    public bool PANI { get; set; }
    public bool TempMonitoreo { get; set; }
    public bool PVC { get; set; }
    public bool SondaVesical { get; set; }
    public bool SNG { get; set; }
    public bool AnalizadorGases { get; set; }
    public bool Gasometria { get; set; }
    public string OtraMonitorizacion { get; set; } = string.Empty;

    // Técnica y Ventilación (qvt_tecnicaAnestesia + qvt_ventilacionAnestesia)
    public bool TecnicaGeneral { get; set; }
    public bool TecnicaRegional { get; set; }
    public bool TecnicaEpidural { get; set; }
    public bool TecnicaSedacion { get; set; }
    public bool TecnicaLocal { get; set; }
    public string Induccion { get; set; } = string.Empty;
    public string Mantenimiento { get; set; } = string.Empty;
    public string BloqueoRegional { get; set; } = string.Empty;
    public bool VentEspontanea { get; set; }
    public bool VentMecanica { get; set; }
    public bool VentAsistida { get; set; }
    public string PEEP { get; set; } = string.Empty;
    public string PosicionPaciente { get; set; } = string.Empty;

    // Balance Intraoperatorio: Ingresos (qvt_IngresosAnestesia) y Egresos (qvt_egresosAnestesia)
    public double IngDextrosa { get; set; }
    public double IngSalina09 { get; set; }
    public double IngLactatoRinger { get; set; }
    public double IngSangre { get; set; }
    public double IngPlasma { get; set; }
    public double IngExpansor { get; set; }
    public double IngManitol { get; set; }
    public double IngOtros { get; set; }
    public double TotalIngresos => IngDextrosa + IngSalina09 + IngLactatoRinger + IngSangre + IngPlasma + IngExpansor + IngManitol + IngOtros;

    public double EgrSangrado { get; set; }
    public double EgrDiuresis { get; set; }
    public double EgrPerdidasInsensibles { get; set; }
    public double EgrOtros { get; set; }
    public double TotalEgresos => EgrSangrado + EgrDiuresis + EgrPerdidasInsensibles + EgrOtros;
    public double BalanceTotal => TotalIngresos - TotalEgresos;

    // Duración y Comentarios (qvt_duracionAnestesia)
    public int DuracionAnestesiaHoras { get; set; }
    public int DuracionAnestesiaMinutos { get; set; }
    public int DuracionCirugiaHoras { get; set; }
    public int DuracionCirugiaMin { get; set; }
    public string Comentarios { get; set; } = string.Empty;

    // 15 Nombres de Agentes/Fármacos Anestésicos (qvt_graficaDescripcion)
    public string[] NombresAgentes { get; set; } = new string[15]
    {
        "Propofol (mg)", "Isoflurano / Sevoflurano (%)", "Fentanilo (mcg)", "Ketamina (mg)", "Midazolam (mg)",
        "Lidocaína (mg)", "Bupivacaína (mg)", "Meloxicam / Analgesia", "Cefazolina / Antibiótico", "Atropina (mg)",
        "Adrenalina / Vasopresor", "Ácido Tranexámico", "Ringer Lactato (ml/h)", "Cloruro Sodio 0.9% (ml/h)", "Otro Agente"
    };

    // 16 Columnas de Tiempo Intraoperatorio (qvt_graficaAnestesia + qvt_graficaAnestesiaTiempo)
    public List<SlotGraficaAnestesiaDto> SlotsTiempo { get; set; } = Enumerable.Range(1, 16)
        .Select(i => new SlotGraficaAnestesiaDto
        {
            Orden = i,
            HoraLabel = DateTime.Today.AddHours(8).AddMinutes((i - 1) * 15).ToString("HH:mm")
        })
        .ToList();
}

public class SlotGraficaAnestesiaDto
{
    public int Id { get; set; }
    public int Orden { get; set; } // 1..16
    public string HoraLabel { get; set; } = string.Empty; // tiempo1..tiempo16
    public int? PresionSistolica { get; set; }
    public int? PresionDiastolica { get; set; }
    public string TensionArterialMedia { get; set; } = string.Empty;
    public int? FrecuenciaCardiaca { get; set; }
    public int? Temperatura { get; set; }
    public int? O2L { get; set; } // SpO2 %
    public int? EtCO2 { get; set; }
    public string Induccion { get; set; } = string.Empty;
    public string FinAnestesia { get; set; } = string.Empty;
    public string RespEspontanea { get; set; } = string.Empty;
    public string RespAsistida { get; set; } = string.Empty;
    public string RespControlada { get; set; } = string.Empty;

    // Valores de los 15 agentes en este intervalo de tiempo (valorDescripcion1..15)
    public string[] ValoresAgentes { get; set; } = new string[15]
    {
        "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""
    };
}

// ============================================================================
// 3. PROTOCOLO OPERATORIO, EQUIPO Y CHEQUEO (vt_protocolo + vt_quirTeam + vt_QuirofanoIngreso)
// ============================================================================
public class ProtocoloQuirurgicoCompletoDto
{
    // Protocolo Operatorio (qvt_Protocolo)
    public int IdProtocolo { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string TipoCirugia { get; set; } = "Electiva";
    public string Principal { get; set; } = string.Empty; // Cirugía realizada / Procedimiento principal
    public string Incision { get; set; } = string.Empty;
    public string Hallazgos { get; set; } = string.Empty;
    public string ProcedimientoQuir { get; set; } = string.Empty;
    public string Sintesis { get; set; } = string.Empty;
    public string Complicaciones { get; set; } = "Ninguna";
    public int Sangrado { get; set; } // ml
    public int Disposicion { get; set; } = 1;
    public int Drenaje { get; set; }
    public string Muestra { get; set; } = "NO";
    public DateTime? FechaMuestra { get; set; }
    public string Congelacion { get; set; } = "NO";
    public DateTime? FechaConge { get; set; }

    // Chequeo e Ingreso a Quirófano / Conteo de Gasas (qvt_ingresocirugia)
    public int SecIngresoCirugia { get; set; }
    public int Sala { get; set; } = 1;
    public string Posicion { get; set; } = "Decúbito Dorsal";
    public string CondicionPiel { get; set; } = "Íntegra";
    public string Lavado { get; set; } = "Clorhexidina";
    public string UnidadElectro { get; set; } = "Bipolar / Monopolar";
    public string LocalizacionPlaca { get; set; } = "Dorso / Extremidad";
    public string GasasIniciales { get; set; } = "10";
    public string GasasAdicionales { get; set; } = "0";
    public string GasasRecuentoFinal { get; set; } = "10 (Completo)";
    public string Compresas { get; set; } = "Completas";
    public string MaterialDejado { get; set; } = "Ninguno";
    public string CasoContaminado { get; set; } = "Limpia";
    public string TransferidoA { get; set; } = "Sala de Recuperación";
    public string ObservacionesEnfermeria { get; set; } = string.Empty;

    // Equipo Quirúrgico (qvt_team)
    public List<MiembroEquipoQuirurgicoDto> Equipo { get; set; } = new();
}

public class MiembroEquipoQuirurgicoDto
{
    public int Sec { get; set; }
    public int IdOrden { get; set; }
    public int IdUsuario { get; set; }
    public string NombreProfesional { get; set; } = string.Empty;
    public string RolFuncion { get; set; } = "Cirujano Principal"; // tipo / observacion
    public bool Principal { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
}

// ============================================================================
// 4. SALA DE RECUPERACIÓN Y POST-OPERATORIO (vt_Recuperacion + vt_postquirurgico)
// ============================================================================
public class RecuperacionSignoVitalDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Hora { get; set; } = DateTime.Now.ToString("HH:mm");
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Range(1, 350, ErrorMessage = "Ingrese pulso válido")]
    public int Pulso { get; set; } = 85;
    [Range(20, 260, ErrorMessage = "Ingrese PAS válida")]
    public int Sistolica { get; set; } = 120;
    [Range(10, 180, ErrorMessage = "Ingrese PAD válida")]
    public int Diastolica { get; set; } = 75;
    public int PresionMedia { get; set; } = 90;
    [Range(1, 120, ErrorMessage = "Ingrese FR válida")]
    public int Respiracion { get; set; } = 20;
    public decimal Temperatura { get; set; } = 38.2m;
    public decimal Oxigeno { get; set; } = 2.0m; // L/min
    public int Saturacion { get; set; } = 98; // SpO2 %
    public int Usuario { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
}

public class PostOperatorioChecklistDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime? FechaHoraSalida { get; set; } = DateTime.Now;
    public string Destino { get; set; } = "Hospitalización";
    public string Ind { get; set; } = "SI";
    public string ObsInd { get; set; } = string.Empty;
    public string Sig { get; set; } = "SI";
    public string ObsSig { get; set; } = string.Empty;
    public string Apo { get; set; } = "SI";
    public string ObsApo { get; set; } = string.Empty;
    public string Liq { get; set; } = "SI";
    public string ObsLiq { get; set; } = string.Empty;
    public string Son { get; set; } = "NO";
    public string ObsSon { get; set; } = string.Empty;
    public string Drenes { get; set; } = "NO";
    public string ObsDrenes { get; set; } = string.Empty;
    public string Drena { get; set; } = "NO";
    public string ObsDrena { get; set; } = string.Empty;
    public string Sng { get; set; } = "NO";
    public string ObsSng { get; set; } = string.Empty;
    public string Otros { get; set; } = "NO";
    public string ObsOtros { get; set; } = string.Empty;
}

public class LiquidoRecuperacionDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Hora { get; set; } = DateTime.Now.ToString("HH:mm");
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Descripcion { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string Tipo { get; set; } = "I"; // 'I' = Administrado/Ingerido, 'E' = Eliminado
    public string MedIdProdu { get; set; } = string.Empty;
    public decimal MedCantidad { get; set; } = 1;
    public string MedBodega { get; set; } = string.Empty;
    public decimal MedPrecio { get; set; }
    public string MedMedida { get; set; } = "UND";
    public string MedIva { get; set; } = "IVA0";
    public string MedIdDinamics { get; set; } = string.Empty;
}

public class ObservacionRecuperacionDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Hora { get; set; } = DateTime.Now.ToString("HH:mm");
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Observacion { get; set; } = string.Empty;
    public int Usuario { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
}

// ============================================================================
// 5. PAUSA DE SEGURIDAD Y LISTA DE CHEQUEO QUIRÚRGICO (vt_ControlIngreso + qvt_verificacion)
// ============================================================================
public class PausaSeguridadDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Hora { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public int IdUsuario { get; set; }
    public int IdLocalidad { get; set; }

    // Fase 1: Antes de la Inducción de Anestesia (Enfermero y Anestesista)
    public string Rpta1 { get; set; } = "S"; // Confirmación identidad, sitio, procedimiento, consentimiento
    public string Rpta2 { get; set; } = "S"; // Marcación de sitio quirúrgico / No procede
    public string Rpta3 { get; set; } = "S"; // Comprobación aparatos y medicación anestésica
    public string Rpta4 { get; set; } = "S"; // Pulsioxímetro colocado y funcionando
    public string Rpta5 { get; set; } = "N"; // Alergias conocidas
    public string Rpta6 { get; set; } = "N"; // Vía aérea difícil / riesgo aspiración
    public string Rpta7 { get; set; } = "N"; // Riesgo hemorragia > 500ml

    // Fase 2: Antes de la Incisión Cutánea - Pausa Quirúrgica (Enfermero, Anestesista y Cirujano)
    public string Rpta8 { get; set; } = "S"; // Presentación de miembros del equipo
    public string Rpta9 { get; set; } = "S"; // Confirmación verbal identidad, sitio, procedimiento
    public string Rpta10 { get; set; } = "S"; // Profilaxis antibiótica últimos 60 min / No procede
    public string EventosCriticos { get; set; } = string.Empty; // Cirujano: Pasos críticos o imprevistos
    public string TiempoOperacion { get; set; } = string.Empty; // Cirujano: Duración prevista
    public string PerdidaSangre { get; set; } = string.Empty; // Cirujano: Pérdida de sangre prevista
    public string Rpta14 { get; set; } = "N"; // Anestesista: Problema específico
    public string ProblemaEspecifico { get; set; } = string.Empty; // Anestesista: Detalle
    public string Rpta15 { get; set; } = "S"; // Enfermería: Esterilidad confirmada
    public string Rpta16 { get; set; } = "N"; // Enfermería: Dudas instrumental/equipos
    public string ProblemasInstrumental { get; set; } = string.Empty; // Enfermería: Detalle dudas
    public string Rpta17 { get; set; } = "S"; // Imágenes diagnósticas esenciales visualizables

    // Fase 3: Antes de salir de Quirófano (Enfermero, Anestesista y Cirujano)
    public string Rpta18 { get; set; } = "S"; // Nombre del procedimiento confirmado
    public string Rpta19 { get; set; } = "S"; // Recuento instrumentos, gasas, agujas
    public string Rpta20 { get; set; } = "S"; // Etiquetado de muestras en voz alta
    public string Rpta21 { get; set; } = "N"; // Problemas instrumental a resolver
    public string AspectosRecuperacion { get; set; } = string.Empty; // Aspectos críticos de recuperación
    public string Cerrado { get; set; } = "S";
}
