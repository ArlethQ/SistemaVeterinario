using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Simetric.Models.EContax;
using Simetric.Services.EContax;

namespace HosvetKardex.Web.Components.Pages.Contabilidad.ActivosFijos;

public abstract class ActivosFijosPage : ComponentBase
{
    [Inject] protected AuthenticationStateProvider Auth { get; set; } = null!;
    [Inject] protected EContaxActivosFijosService ActivosFijos { get; set; } = null!;
    protected virtual EContaxActivosFijosService Servicio => ActivosFijos;
    [Inject] protected EContaxSeguridadService Seguridad { get; set; } = null!;
    [Inject] protected ILogger<ActivosFijosPage> Logger { get; set; } = null!;

    protected int Usuario { get; private set; }
    protected bool CargandoPantalla { get; private set; } = true;
    protected bool Ocupado { get; private set; }
    protected string Error { get; private set; } = "";
    protected string Aviso { get; private set; } = "";
    protected EContaxAccion Permisos { get; private set; }
    protected abstract string Ruta { get; }
    protected abstract Task CargarAsync();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await EjecutarAsync(async () =>
            {
                var auth = await Auth.GetAuthenticationStateAsync();
                if (!int.TryParse(auth.User.FindFirst("IdUsuario")?.Value, out var usuario) || usuario <= 0)
                    throw new UnauthorizedAccessException("Inicia sesión para acceder a Activos Fijos.");
                Usuario = usuario;
                Permisos = await Seguridad.GetAccionesRutaAsync(usuario, Ruta);
                await CargarAsync();
            });
        }
        finally
        {
            CargandoPantalla = false;
        }
    }

    protected async Task EjecutarAsync(Func<Task> operacion, string aviso = "")
    {
        if (Ocupado) return;
        Ocupado = true;
        Error = Aviso = "";
        try
        {
            await operacion();
            Aviso = aviso;
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException
            && ex is not ObjectDisposedException)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error en Activos Fijos, ruta {Ruta}", Ruta);
            Error = "No se pudo completar la operación.";
        }
        finally
        {
            Ocupado = false;
        }
    }
}
