using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace HosvetKardex.Web.Services;

public sealed class VeterinariaAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly SessionStateService _session;

    public VeterinariaAuthenticationStateProvider(SessionStateService session)
    {
        _session = session;
        _session.OnChange += OnSessionChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Numerica aísla sus documentos por IdUsuario. Cada localidad veterinaria
        // utiliza un usuario fiscal técnico propio y conserva aparte al usuario clínico.
        var idUsuarioFacturacion = 1_000_000 + _session.Localidad;
        var identity = _session.IsAuthenticated
            ? new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, idUsuarioFacturacion.ToString()),
                new Claim("IdUsuario", idUsuarioFacturacion.ToString()),
                new Claim("VeterinariaUserId", _session.IdUser.ToString()),
                new Claim("IdLocalidad", _session.Localidad.ToString()),
                new Claim(ClaimTypes.Name, _session.NombreUsuario),
                new Claim("Login", _session.LoginUser),
                new Claim(ClaimTypes.Role, _session.TipoUser.ToString())
            ], "Veterinaria")
            : new ClaimsIdentity();

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    private void OnSessionChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() => _session.OnChange -= OnSessionChanged;
}
