using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Simetric.Services;

namespace HosvetKardex.Web.Services;

/// <summary>
/// Impide inicializar una pantalla de emisión antes de que la veterinaria tenga
/// emisor y caja. Así las vistas físicas pueden vivir directamente en el proyecto
/// veterinario sin provocar excepciones al resolver series.
/// </summary>
public sealed class FacturacionPageAccessService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly EmisorOnboardingService _onboarding;
    private readonly NavigationManager _navigation;

    public FacturacionPageAccessService(
        AuthenticationStateProvider authenticationStateProvider,
        EmisorOnboardingService onboarding,
        NavigationManager navigation)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _onboarding = onboarding;
        _navigation = navigation;
    }

    public async Task<bool> EnsureConfiguredAsync()
    {
        var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var rawId = authenticationState.User.FindFirst("IdUsuario")?.Value
                    ?? authenticationState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(rawId, out var userId))
        {
            _navigation.NavigateTo("/login", replace: true);
            return false;
        }

        var requiredStep = await _onboarding.RefreshRequirementAsync(userId);
        if (requiredStep == InitialSetupStep.None)
            return true;

        var destination = requiredStep == InitialSetupStep.Emisor
            ? "/facturacion/configuracion/emisor"
            : "/facturacion/configuracion/caja";

        _navigation.NavigateTo(destination, replace: true);
        return false;
    }
}
