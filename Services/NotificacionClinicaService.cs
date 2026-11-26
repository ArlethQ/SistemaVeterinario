using System.Collections.Concurrent;
using HosvetKardex.Web.Data;

namespace HosvetKardex.Web.Services;

public sealed class NotificacionClinicaHub
{
    private readonly ConcurrentDictionary<int, string> _prioridadesRecientes = new();

    public event Action<int?>? OnNuevaActividad;

    public void RegistrarPrioridadOrden(int idOrden, string? colorTriaje)
    {
        if (idOrden > 0 && !string.IsNullOrWhiteSpace(colorTriaje))
            _prioridadesRecientes[idOrden] = colorTriaje.Trim().ToLowerInvariant();
    }

    public string? ObtenerPrioridadReciente(int idOrden) =>
        _prioridadesRecientes.TryGetValue(idOrden, out var color) ? color : null;

    public void NotificarCambio(int? idUsuarioDestino = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250);
                OnNuevaActividad?.Invoke(idUsuarioDestino);
            }
            catch
            {
                // Evitar que un suscriptor desconectado interrumpa el guardado principal.
            }
        });
    }
}

public sealed class NotificacionClinicaItemDto
{
    public string Clave { get; set; } = string.Empty;
    public string Tipo { get; set; } = "ORDEN"; // ORDEN | CITA
    public int IdReferencia { get; set; }
    public int Nhcl { get; set; }
    public bool EsEmergencia { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string HoraTexto { get; set; } = string.Empty;
    public DateTime FechaEvento { get; set; }
    public string RutaDestino { get; set; } = "/dashboard";
    public string TextoAccion { get; set; } = "Abrir";
    public bool Leida { get; set; }
}

public sealed class NotificacionClinicaEstadoDto
{
    public bool Habilitado { get; set; }
    public bool EsMedico { get; set; }
    public bool EsEnfermero { get; set; }
    public List<NotificacionClinicaItemDto> Items { get; set; } = [];
    public int TotalNoLeidas => Items.Count(i => !i.Leida);
    public int TotalEmergenciasNoLeidas => Items.Count(i => !i.Leida && i.EsEmergencia);
}

public interface INotificacionClinicaService
{
    Task<NotificacionClinicaEstadoDto> ObtenerNotificacionesActivasAsync();
}

public sealed class NotificacionClinicaService : INotificacionClinicaService
{
    private readonly ISqlDataService _db;
    private readonly SessionStateService _session;
    private readonly NotificacionClinicaHub _hub;
    private readonly ILogger<NotificacionClinicaService> _logger;

    public NotificacionClinicaService(
        ISqlDataService db,
        SessionStateService session,
        NotificacionClinicaHub hub,
        ILogger<NotificacionClinicaService> logger)
    {
        _db = db;
        _session = session;
        _hub = hub;
        _logger = logger;
    }

    private sealed class PerfilUsuarioRow
    {
        public int TipoUser { get; set; }
        public string RolDescripcion { get; set; } = string.Empty;
    }

    private sealed class OrdenNotifRow
    {
        public int IdOrden { get; set; }
        public int Nhcl { get; set; }
        public string Mascota { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Etapa { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string ColorTriaje { get; set; } = string.Empty;
        public bool EsProcedimiento { get; set; }
        public int IdMedico { get; set; }
        public int IdEnfermero { get; set; }
    }

    private sealed class CitaNotifRow
    {
        public int IdCita { get; set; }
        public int Nhcl { get; set; }
        public string Mascota { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Hora { get; set; } = string.Empty;
        public int TipoConsulta { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public string EstadoCita { get; set; } = string.Empty;
    }

    public async Task<NotificacionClinicaEstadoDto> ObtenerNotificacionesActivasAsync()
    {
        var estado = new NotificacionClinicaEstadoDto();
        if (!_session.IsAuthenticated || _session.IdUser <= 0)
            return estado;

        try
        {
            var perfil = await _db.QueryFirstOrDefaultAsync<PerfilUsuarioRow>("""
                SELECT ISNULL(u.user_tipouser_numero, 0) AS TipoUser,
                       ISNULL(t.tipuser_descripcion, '') AS RolDescripcion
                FROM vt_usuarios u WITH (NOLOCK)
                LEFT JOIN vt_tipos_usuarios t WITH (NOLOCK) ON u.user_tipouser_numero = t.tipuser_numero
                WHERE u.user_numero = @IdUser AND ISNULL(u.user_estado, 'A') = 'A';
                """, new { IdUser = _session.IdUser });

            var rolDesc = (perfil?.RolDescripcion ?? string.Empty).ToUpperInvariant();
            var tipoUser = perfil?.TipoUser ?? _session.TipoUser;

            bool esMedico = tipoUser == 2 || rolDesc.Contains("MEDIC") || rolDesc.Contains("VETERIN");
            bool esEnfermero = tipoUser == 11 || rolDesc.Contains("ENFERMER");
            bool esAdmin = _session.EsAdministrador || tipoUser == 1;

            estado.EsMedico = esMedico;
            estado.EsEnfermero = esEnfermero;
            estado.Habilitado = esMedico || esEnfermero || esAdmin;

            if (!estado.Habilitado)
                return estado;

            var hoy = DateTime.Today;
            var items = new List<NotificacionClinicaItemDto>();

            // 1. Órdenes asignadas de hoy (activas o en atención)
            try
            {
                const string sqlOrdenes = """
                    SELECT TOP 25
                        o.ordenAten_id AS IdOrden,
                        ISNULL(o.ordenAten_nhcl, 0) AS Nhcl,
                        ISNULL(p.mas_nombre, '') AS Mascota,
                        ISNULL(o.ordenAten_nombre, '') AS Propietario,
                        ISNULL(es.es_descripcion, '') AS Especialidad,
                        ISNULL(o.ordenAten_etapa, 'Consulta') AS Etapa,
                        ISNULL(NULLIF(RTRIM(o.ordenAten_turno), ''), '') AS Turno,
                        ISNULL(RTRIM(o.ordenAten_hora), '') AS Hora,
                        o.ordenAten_fecha AS Fecha,
                        RTRIM(ISNULL(p.mas_colortriaje, 'green')) AS ColorTriaje,
                        CAST(ISNULL(o.ordenAten_esProcedimiento, 0) AS BIT) AS EsProcedimiento,
                        ISNULL(o.ordenAten_idMedico, 0) AS IdMedico,
                        ISNULL(o.ordenAten_idEnfermero, 0) AS IdEnfermero
                    FROM vt_ordenAtencion o WITH (NOLOCK)
                    LEFT JOIN vt_pacientes p WITH (NOLOCK) ON o.ordenAten_nhcl = p.mas_nhcl
                    LEFT JOIN vt_especialidades es WITH (NOLOCK) ON o.ordenAten_esp_numero = es.es_id
                    WHERE ISNULL(o.ordenAten_estado, 'A') IN ('A', 'E')
                      AND (
                          o.ordenAten_idMedico = @IdUser
                          OR o.ordenAten_idEnfermero = @IdUser
                          OR (
                              @EsEnfermero = 1
                              AND ISNULL(o.ordenAten_idEnfermero, 0) = 0
                              AND (ISNULL(o.ordenAten_esProcedimiento, 0) = 1 OR ISNULL(o.ordenAten_triajeMedico, 0) = 0)
                              AND o.ordenAten_idlocalidad = @Localidad
                          )
                      )
                    ORDER BY o.ordenAten_id DESC;
                    """;

                var ordenes = await _db.QueryAsync<OrdenNotifRow>(sqlOrdenes, new
                {
                    IdUser = _session.IdUser,
                    EsEnfermero = esEnfermero ? 1 : 0,
                    Localidad = _session.Localidad
                });

                foreach (var o in ordenes)
                {
                    var colorEfectivo = _hub.ObtenerPrioridadReciente(o.IdOrden) ?? o.ColorTriaje;
                    bool esEmergencia =
                        string.Equals(colorEfectivo?.Trim(), "red", StringComparison.OrdinalIgnoreCase) ||
                        (o.Etapa?.Contains("EMERGENCIA", StringComparison.OrdinalIgnoreCase) == true) ||
                        (o.Especialidad?.Contains("EMERGENCIA", StringComparison.OrdinalIgnoreCase) == true);

                    var partesDetalle = new List<string>();
                    if (!string.IsNullOrWhiteSpace(o.Especialidad)) partesDetalle.Add(o.Especialidad.Trim());
                    else if (!string.IsNullOrWhiteSpace(o.Etapa)) partesDetalle.Add(o.Etapa.Trim());
                    if (!string.IsNullOrWhiteSpace(o.Turno)) partesDetalle.Add($"Turno {o.Turno}");
                    var fechaHoraOrden = o.Fecha.Date == hoy
                        ? (string.IsNullOrWhiteSpace(o.Hora) ? "Hoy" : $"Hoy {o.Hora}")
                        : (string.IsNullOrWhiteSpace(o.Hora) ? o.Fecha.ToString("dd/MM/yyyy") : $"{o.Fecha:dd/MM/yyyy} {o.Hora}");
                    partesDetalle.Add(fechaHoraOrden);

                    string ruta;
                    string textoAccion;
                    if (esEnfermero && o.IdMedico != _session.IdUser)
                    {
                        ruta = o.IdOrden > 0 && o.Nhcl > 0 ? $"/admision/triaje/{o.IdOrden}/{o.Nhcl}" : "/admision/triaje";
                        textoAccion = "Ir a Triaje";
                    }
                    else if (o.Etapa?.Contains("Hosp", StringComparison.OrdinalIgnoreCase) == true && o.IdOrden > 0 && o.Nhcl > 0)
                    {
                        ruta = $"/hospitalizacion/ficha/{o.IdOrden}/{o.Nhcl}";
                        textoAccion = "Ir a Ficha 24h";
                    }
                    else
                    {
                        ruta = o.IdOrden > 0 && o.Nhcl > 0 ? $"/consulta/fichamedica/{o.IdOrden}/{o.Nhcl}" : "/consulta/fichamedica";
                        textoAccion = "Ir al SOAP";
                    }

                    items.Add(new NotificacionClinicaItemDto
                    {
                        Clave = $"ORD-{o.IdOrden}",
                        Tipo = "ORDEN",
                        IdReferencia = o.IdOrden,
                        Nhcl = o.Nhcl,
                        EsEmergencia = esEmergencia,
                        Titulo = esEmergencia
                            ? $"EMERGENCIA · Orden #{o.IdOrden}"
                            : (o.EsProcedimiento ? $"Procedimiento · Orden #{o.IdOrden}" : $"Orden #{o.IdOrden}"),
                        Paciente = string.IsNullOrWhiteSpace(o.Mascota) ? $"NHCL #{o.Nhcl}" : o.Mascota.Trim(),
                        Propietario = o.Propietario?.Trim() ?? string.Empty,
                        Detalle = string.Join(" · ", partesDetalle),
                        HoraTexto = string.IsNullOrWhiteSpace(o.Hora) ? o.Fecha.ToString("dd/MM") : o.Hora,
                        FechaEvento = CombinarFechaHora(o.Fecha, o.Hora),
                        RutaDestino = ruta,
                        TextoAccion = textoAccion
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error consultando órdenes para notificaciones clínicas.");
            }

            // 2. Citas asignadas de hoy y próximas (no canceladas ni atendidas)
            try
            {
                const string sqlCitas = """
                    SELECT TOP 20
                        ci_id AS IdCita,
                        ISNULL(ci_nhcl, 0) AS Nhcl,
                        ISNULL(ci_nombremas, '') AS Mascota,
                        ISNULL(ci_nombre, '') AS Propietario,
                        ci_fecha AS Fecha,
                        ISNULL(RTRIM(ci_hora), '') AS Hora,
                        ISNULL(ci_tipoConsulta, 1) AS TipoConsulta,
                        ISNULL(ci_observacion, '') AS Observacion,
                        ISNULL(ci_estadocita, 'N') AS EstadoCita
                    FROM vt_citas WITH (NOLOCK)
                    WHERE ci_usuario = @IdUser
                      AND CAST(ci_fecha AS DATE) >= @Hoy
                      AND ISNULL(ci_estado, 'A') = 'A'
                      AND ISNULL(ci_estadocita, 'N') NOT IN ('X', 'A', 'CANCELADA')
                    ORDER BY ci_id DESC;
                    """;

                var citas = await _db.QueryAsync<CitaNotifRow>(sqlCitas, new
                {
                    IdUser = _session.IdUser,
                    Hoy = hoy
                });

                foreach (var c in citas)
                {
                    bool esEmergencia = c.Observacion?.Contains("EMERGENCIA", StringComparison.OrdinalIgnoreCase) == true;
                    string tipoLabel = c.TipoConsulta switch
                    {
                        2 => "Especialidad",
                        3 => "Cirugía",
                        4 => "Control",
                        5 => "Imagen",
                        _ => "Consulta"
                    };

                    string fechaHora = c.Fecha.Date == hoy
                        ? $"Hoy {c.Hora}"
                        : $"{c.Fecha:dd/MM} {c.Hora}";

                    items.Add(new NotificacionClinicaItemDto
                    {
                        Clave = $"CITA-{c.IdCita}",
                        Tipo = "CITA",
                        IdReferencia = c.IdCita,
                        Nhcl = c.Nhcl,
                        EsEmergencia = esEmergencia,
                        Titulo = esEmergencia ? $"EMERGENCIA · Cita {fechaHora}" : $"Nueva cita · {fechaHora}",
                        Paciente = string.IsNullOrWhiteSpace(c.Mascota) ? "Paciente" : c.Mascota.Trim(),
                        Propietario = c.Propietario?.Trim() ?? string.Empty,
                        Detalle = string.IsNullOrWhiteSpace(c.Observacion)
                            ? tipoLabel
                            : $"{tipoLabel} · {c.Observacion.Trim()}",
                        HoraTexto = fechaHora.Trim(),
                        FechaEvento = CombinarFechaHora(c.Fecha, c.Hora),
                        RutaDestino = "/admision/citas",
                        TextoAccion = "Ver agenda"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error consultando citas para notificaciones clínicas.");
            }

            estado.Items = items
                .OrderByDescending(i => i.EsEmergencia)
                .ThenByDescending(i => i.IdReferencia)
                .Take(25)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron consultar las notificaciones clínicas.");
        }

        return estado;
    }

    private static DateTime CombinarFechaHora(DateTime fecha, string? hora)
    {
        if (!string.IsNullOrWhiteSpace(hora) && TimeSpan.TryParse(hora.Trim(), out var ts))
            return fecha.Date.Add(ts);
        return fecha;
    }
}
