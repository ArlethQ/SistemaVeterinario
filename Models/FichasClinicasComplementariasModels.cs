namespace HosvetKardex.Web.Models;

public sealed class PasoTurnoDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Turno { get; set; } = "Matutino";
    public string Subjetivo { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string Analisis { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string ExamenesRealizados { get; set; } = string.Empty;
    public string Pendientes { get; set; } = string.Empty;
    public string Alertas { get; set; } = string.Empty;
    public string Entrega { get; set; } = string.Empty;
    public string Recibe { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
}

public sealed class InterconsultaHospitalariaDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;
    public DateTime? FechaAgendada { get; set; }
    public string Tipo { get; set; } = "Examen";
    public string Detalle { get; set; } = string.Empty;
    public string EquipoLaboratorio { get; set; } = string.Empty;
    public string Especialista { get; set; } = string.Empty;
    public string MedicoSolicitante { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public string ResultadoResumen { get; set; } = string.Empty;
    public bool RegistradoHistoria { get; set; }
    public int IdUsuario { get; set; }
}

public sealed class TransfusionDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Producto { get; set; } = "Sangre entera";
    public string IdentificadorUnidad { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public string GrupoReceptor { get; set; } = string.Empty;
    public string GrupoDonante { get; set; } = string.Empty;
    public bool TipificacionVerificada { get; set; }
    public bool CrossmatchMayor { get; set; }
    public bool CrossmatchMenor { get; set; }
    public bool ConsentimientoConfirmado { get; set; }
    public decimal VolumenPrescritoMl { get; set; }
    public decimal VolumenAdministradoMl { get; set; }
    public decimal VelocidadMlHora { get; set; }
    public DateTime? HoraInicio { get; set; }
    public DateTime? HoraFin { get; set; }
    public string DiagnosticoIndicacion { get; set; } = string.Empty;
    public string MedicoResponsable { get; set; } = string.Empty;
    public string DobleVerificacion { get; set; } = string.Empty;
    public string Estado { get; set; } = "En preparación";
    public string Observaciones { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public List<TransfusionMonitoreoDto> Monitoreos { get; set; } = [];
}

public sealed class TransfusionMonitoreoDto
{
    public int Id { get; set; }
    public int IdTransfusion { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public string Momento { get; set; } = "T0";
    public decimal? Temperatura { get; set; }
    public int? FrecuenciaCardiaca { get; set; }
    public int? FrecuenciaRespiratoria { get; set; }
    public int? Pas { get; set; }
    public int? Pad { get; set; }
    public int? Pam { get; set; }
    public string Mucosas { get; set; } = string.Empty;
    public string EstadoConciencia { get; set; } = string.Empty;
    public string ReaccionesAdversas { get; set; } = string.Empty;
    public string AccionTomada { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
}
