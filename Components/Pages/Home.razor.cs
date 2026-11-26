using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;
using HosvetKardex.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace HosvetKardex.Web.Components.Pages;

public partial class Home : IDisposable
{

    [Inject] private ISqlDataService Sql { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private SessionStateService Session { get; set; } = default!;
    [Inject] private INotificacionClinicaService NotificacionClinicaService { get; set; } = default!;
    [Inject] private NotificacionClinicaHub NotificacionHub { get; set; } = default!;
    [Inject] private Simetric.Services.EContax.EContaxInventarioService Inventario { get; set; } = default!;

    private static readonly System.Globalization.CultureInfo Ecuador = new("es-EC");
    private static readonly (string Nombre, string Color)[] Areas =
        [("Consultas", "#1769ff"), ("Hospitalización", "#00b9de"), ("Cirugías", "#9259f5"), ("Otros servicios", "#ffbc49")];

    private sealed record Acceso(string Nombre, string Ruta, string Icono);
    private static readonly Acceso[] Accesos =
    [
        new("Nueva Orden", "/admision/ordenes", "bi-plus-square-fill"),
        new("Bandeja de Órdenes", "/kardex/ordenes", "bi-envelope"),
        new("Agenda de Citas", "/admision/citas", "bi-calendar2-check"),
        new("Pacientes", "/admision/clientes-pacientes", "bi-people"),
        new("Hospitalización", "/hospitalizacion/ficha", "bi-hospital"),
        new("Quirófano", "/quirofano/preanestesia", "bi-scissors"),
        new("Kardex", "/kardex/enfermeria", "bi-stack"),
        new("Compras", "/contabilidad/compras", "bi-cart3"),
        new("Proveedores", "/contabilidad/proveedores", "bi-truck"),
        new("Facturación", "/facturacion/nueva", "bi-receipt"),
        new("Reportes", "/reportes/clinicos", "bi-bar-chart-line"),
        new("Inventario", "/contabilidad/inventarios", "bi-box-seam")
    ];

    private IEnumerable<(string Nombre, string Ruta, string Icono, string Color, int Valor)> Indicadores =>
    [
        ("Órdenes Activas", "/kardex/ordenes", "bi-clipboard2-pulse-fill", "blue", _totalOrdenesActivas),
        ("Hospitalización 24h", "/hospitalizacion/ficha", "bi-hospital", "purple", _totalHospitalizados),
        ("Citas de Hoy", "/admision/citas", "bi-calendar3", "orange", _citasHoy.Count),
        ("Documentos Disponibles", "/contabilidad/compra-documentos", "bi-file-earmark-text", "purple", _saldoDocumentos)
    ];

    private IEnumerable<(string Nombre, string Ruta, string Icono, string Color, string Detalle)> Alertas =>
    [
        ("Productos por caducar", "/contabilidad/inventarios", "bi-box-seam", "red",
            _inventario is null ? "Consultar vencimientos" : $"{_inventario.LotesPorVencer} lotes próximos a vencer"),
        ("Stock bajo", "/contabilidad/inventarios", "bi-box-seam", "orange",
            _inventario is null ? "Consultar existencias" : $"{_inventario.ProductosBajoMinimo} productos bajo el mínimo"),
        ("Citas por confirmar", "/admision/citas", "bi-calendar3", "blue",
            $"{_citasHoy.Count(c => c.CiEstadoCita is not ("C" or "A"))} citas pendientes de confirmación")
    ];

    private sealed class AtencionPorDia
    {
        public DateTime Fecha { get; set; }
        public int Area { get; set; }
        public int Total { get; set; }
    }

    private readonly DateTime _hoy = DateTime.UtcNow.AddHours(-5).Date;
    private int _periodo = 7;
    private bool _errorAtenciones;
    private bool _personalizando;
    private string? _preferenciasAccesos;
    private HashSet<string> _accesosOcultos = [];
    private List<CitaMedicaDto> _citasHoy = [];
    private List<AtencionPorDia> _atenciones = [];
    private Simetric.DTOs.EContax.EContaxInventarioDashboardDto? _inventario;
    private NotificacionClinicaEstadoDto _notificacionesClinicas = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var estado = await AuthStateProvider.GetAuthenticationStateAsync();
        var usuario = estado.User.FindFirst("IdUsuario")?.Value;
        if (string.IsNullOrEmpty(usuario)) return;

        _preferenciasAccesos = $"eForVets.dashboard.accesos.{usuario}";
        var guardados = await JS.InvokeAsync<string?>("localStorage.getItem", _preferenciasAccesos);
        if (string.IsNullOrEmpty(guardados)) return;

        _accesosOcultos = guardados.Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        StateHasChanged();
    }

    private async Task AlternarAccesoAsync(string ruta)
    {
        if (!_accesosOcultos.Remove(ruta))
            _accesosOcultos.Add(ruta);

        if (_preferenciasAccesos is not null)
            await JS.InvokeVoidAsync("localStorage.setItem", _preferenciasAccesos, string.Join("|", _accesosOcultos));
    }

    private static string CssNumero(double valor) => valor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static (string, string) EstadoCita(string estado) => estado switch
    {
        "C" => ("Confirmada", "green"),
        "A" => ("Atendida", "blue"),
        _ => ("Por confirmar", "orange")
    };

    private static string ServicioCita(CitaMedicaDto cita) => cita.CiTipoConsulta switch
    {
        2 when !string.IsNullOrWhiteSpace(cita.Especialidad) => cita.Especialidad,
        2 => "Especialidad",
        3 => "Cirugía",
        4 => "Control",
        5 => "Imagen",
        _ => "Consulta"
    };

    private static string EstadoOrden(OrdenActivaResumenDto orden) =>
        orden.Etapa.Contains("Hosp", StringComparison.OrdinalIgnoreCase) ? "Hospitalización" :
        orden.Estado == "E" ? "En atención" : "En espera";

    private string UrlOrden(OrdenActivaResumenDto orden)
    {
        if (Puede("hospitalizacion/ficha") && orden.Etapa.Contains("Hosp", StringComparison.OrdinalIgnoreCase))
            return $"/hospitalizacion/ficha/{orden.OrdenAtencionId}/{orden.Nhcl}";
        if (Puede("kardex/enfermeria"))
            return $"/kardex/enfermeria?orden={orden.OrdenAtencionId}&nhcl={orden.Nhcl}";
        return Puede("kardex/signos") ? $"/kardex/signos/{orden.OrdenAtencionId}/{orden.Nhcl}" : "/kardex/ordenes";
    }

    private async Task CargarAtencionesAsync()
    {
        try
        {
            _atenciones = (await Sql.QueryAsync<AtencionPorDia>("""
                WITH Atenciones AS (
                    SELECT CAST(ordenAten_fecha AS date) AS Fecha,
                        CASE
                            WHEN ordenAten_etapa LIKE '%hosp%' THEN 1
                            WHEN ordenAten_etapa LIKE '%cirug%' OR ordenAten_etapa LIKE '%quir%' THEN 2
                            WHEN ISNULL(ordenAten_esProcedimiento, 0) = 0
                                AND (ordenAten_etapa IS NULL OR ordenAten_etapa LIKE '%consulta%') THEN 0
                            ELSE 3
                        END AS Area
                    FROM vt_ordenAtencion
                    WHERE ordenAten_fecha >= @Desde AND ordenAten_fecha < @Hasta
                        AND ISNULL(ordenAten_estado, 'A') <> 'I'
                )
                SELECT Fecha, Area, COUNT(*) AS Total FROM Atenciones GROUP BY Fecha, Area;
                """, new { Desde = _hoy.AddDays(-29), Hasta = _hoy.AddDays(1) })).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "No se pudieron consultar las atenciones del panel.");
            _errorAtenciones = true;
        }
    }

    private async Task CargarAlertasInventarioAsync()
    {
        var estado = await AuthStateProvider.GetAuthenticationStateAsync();
        if (!int.TryParse(estado.User.FindFirst("IdUsuario")?.Value, out var usuario)) return;
        try
        {
            _inventario = await Inventario.GetDashboardAsync(usuario, null, null);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "No se pudieron consultar las alertas de inventario del panel.");
        }
    }

    [CascadingParameter] private PermisosNavegacion Permisos { get; set; } = new(0, []);
    private bool Puede(string ruta) => Permisos.Permite(ruta);
    private bool PuedeAlguno(params string[] rutas) => rutas.Any(Puede);
    private bool TieneModulosAdicionales => PuedeAlguno(
        "admision/clientes-pacientes", "admision/ordenes", "admision/citas", "kardex/ordenes",
        "hospitalizacion/monitoreo", "quirofano/protocolo", "quirofano/recuperacion",
        "consulta/fichamedica", "consulta/evoluciones", "consulta/recetario", "consulta/autorizaciones",
        "diagnostico/laboratorio", "diagnostico/imagenologia", "diagnostico/bandeja", "facturacion/proformas");
    private static readonly (string Nombre, string Ruta, string Icono, string Modulo)[] AccionesPorOrden =
    [
        ("Kardex", "kardex/enfermeria", "bi-journal-medical", "kardex"),
        ("Signos", "kardex/signos", "bi-activity", "signos"),
        ("Ficha 24h", "hospitalizacion/ficha", "bi-clipboard2-pulse", "ficha"),
        ("Preanestesia", "quirofano/preanestesia", "bi-clipboard2-check", "preanestesia"),
        ("Anestesia", "quirofano/anestesia", "bi-heart-pulse", "anestesia")
    ];

    private bool TieneAccesosDirectos => AccionesPorOrden.Any(a => Puede(a.Ruta));

    private bool _cargando;
    private int _totalOrdenesActivas;
    private int _totalHospitalizados;
    private int _saldoDocumentos;
    private string _firmaResumen = "No configurada";
    private string? _firmaError;
    private DateTime? _firmaExpiracion;
    private int _ordenDefault;
    private int _nhclDefault;
    private int _ordenManual;
    private int _nhclManual;
    private string _mensajeAccesoRapido = string.Empty;
    private List<OrdenActivaResumenDto> _pacientesRecientes = [];

    protected override async Task OnInitializedAsync()
    {
        Session.OnChange += OnSesionCambiada;
        NotificacionHub.OnNuevaActividad += OnNotificacionHubActividad;
        _cargando = true;
        await Task.Yield();
        try
        {
            var ordenesTask = PuedeAlguno("kardex/ordenes", "hospitalizacion/ficha")
                ? KardexService.ObtenerOrdenesActivasAsync() : Task.FromResult(new List<OrdenActivaResumenDto>());
            var citasTask = Puede("admision/citas")
                ? AdmisionService.ObtenerCitasPorFechaAsync(_hoy, _hoy) : Task.FromResult(new List<CitaMedicaDto>());
            var cuentaTask = PuedeAlguno("contabilidad/compra-documentos", "facturacion/configuracion/firma")
                ? CargarIndicadoresFacturacionAsync() : Task.CompletedTask;

            var atencionesTask = Puede("kardex/ordenes") ? CargarAtencionesAsync() : Task.CompletedTask;
            var inventarioTask = Puede("contabilidad/inventarios") ? CargarAlertasInventarioAsync() : Task.CompletedTask;
            var notifTask = NotificacionClinicaService.ObtenerNotificacionesActivasAsync();

            await Task.WhenAll(ordenesTask, citasTask, cuentaTask, atencionesTask, inventarioTask, notifTask);
            _notificacionesClinicas = await notifTask;

            var ordenes = (await ordenesTask).Where(o => o.Estado is "A" or "E").ToList();
            if (Puede("kardex/ordenes"))
                _pacientesRecientes = ordenes;
            _totalOrdenesActivas = _pacientesRecientes.Count;

            _citasHoy = (await citasTask).Where(c => c.CiEstado != "I" && c.CiEstadoCita != "X")
                .OrderBy(c => TimeSpan.TryParse(c.CiHora, out var hora) ? hora : TimeSpan.MaxValue).ToList();


            _totalHospitalizados = ordenes.Count(o => o.Etapa.Contains("Hosp", StringComparison.OrdinalIgnoreCase));

            if (_pacientesRecientes.Count > 0)
            {
                _ordenDefault = _pacientesRecientes[0].OrdenAtencionId;
                _nhclDefault = _pacientesRecientes[0].Nhcl;
                _ordenManual = _ordenDefault;
                _nhclManual = _nhclDefault;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "No se pudieron cargar todos los indicadores del panel.");
            _mensajeAccesoRapido = "No se pudieron cargar todos los indicadores del panel.";
        }
        finally
        {
            _cargando = false;
        }
    }

    private async Task CargarIndicadoresFacturacionAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        if (!int.TryParse(auth.User.FindFirst("IdUsuario")?.Value, out var usuarioId)) return;

        await using var context = await DbFactory.CreateDbContextAsync();
        var usuario = await context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.IdUsuario == usuarioId);
        if (usuario is null) return;
        if (usuario.estadoAsociado == true && usuario.idJefe is > 0)
            usuario = await context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.IdUsuario == usuario.idJefe.Value) ?? usuario;

        _saldoDocumentos = Math.Max(0, usuario.SaldoDocumentos);
        if (!Puede("facturacion/configuracion/firma")) return;

        var emisor = await context.Emisores.AsNoTracking()
            .Where(x => x.IdUsuario == usuario.IdUsuario && x.Estado)
            .OrderByDescending(x => x.Codigo)
            .FirstOrDefaultAsync();
        if (emisor is null) return;

        var firma = await FirmaValidator.ValidarConApiAsync(emisor);
        _firmaExpiracion = firma.FechaExpiracion;
        _firmaError = firma.IsValid ? null : firma.Message;
        _firmaResumen = firma.DiasRestantes switch
        {
            < 0 => "Caducada",
            0 => "Vence hoy",
            int dias => $"{dias} días restantes",
            _ => firma.TieneConfiguracion ? "Validar vigencia" : "No configurada"
        };
    }

    private void IrModuloManual(string modulo)
    {
        var ord = _ordenManual > 0 ? _ordenManual : _ordenDefault;
        var nhcl = _nhclManual > 0 ? _nhclManual : _nhclDefault;

        if (ord <= 0 || nhcl <= 0)
        {
            _mensajeAccesoRapido = "Seleccione una orden real e ingrese su NHCL antes de abrir el módulo.";
            return;
        }

        _mensajeAccesoRapido = string.Empty;

        var url = modulo switch
        {
            "kardex" => $"/kardex/enfermeria?orden={ord}&nhcl={nhcl}",
            "signos" => $"/kardex/signos/{ord}/{nhcl}",
            "ficha" => $"/hospitalizacion/ficha/{ord}/{nhcl}",
            "preanestesia" => $"/quirofano/preanestesia/{ord}/{nhcl}",
            "anestesia" => $"/quirofano/anestesia/{ord}/{nhcl}",
            _ => $"/kardex/enfermeria?orden={ord}&nhcl={nhcl}"
        };

        Nav.NavigateTo(url);
    }

    private void OnSesionCambiada()
    {
        if (!Session.IsAuthenticated) return;
        _ = InvokeAsync(async () =>
        {
            _notificacionesClinicas = await NotificacionClinicaService.ObtenerNotificacionesActivasAsync();
            StateHasChanged();
        });
    }

    private void OnNotificacionHubActividad(int? idUsuarioDestino)
    {
        _ = InvokeAsync(async () =>
        {
            _notificacionesClinicas = await NotificacionClinicaService.ObtenerNotificacionesActivasAsync();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        Session.OnChange -= OnSesionCambiada;
        NotificacionHub.OnNuevaActividad -= OnNotificacionHubActividad;
    }
}
