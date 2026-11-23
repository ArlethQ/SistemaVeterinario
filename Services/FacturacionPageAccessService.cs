using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.Services;

namespace HosvetKardex.Web.Services;

/// <summary>
/// Impide inicializar una pantalla de emisión antes de que la veterinaria tenga
/// emisor y caja. Si una sucursal (ej. Quito) no tiene emisor/caja propios,
/// reutiliza la configuración fiscal activa principal de la veterinaria.
/// </summary>
public sealed class FacturacionPageAccessService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly EmisorOnboardingService _onboarding;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly NavigationManager _navigation;

    public FacturacionPageAccessService(
        AuthenticationStateProvider authenticationStateProvider,
        EmisorOnboardingService onboarding,
        IDbContextFactory<AppDbContext> dbFactory,
        NavigationManager navigation)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _onboarding = onboarding;
        _dbFactory = dbFactory;
        _navigation = navigation;
    }

    public async Task<int> ResolveBillingUserIdAsync(int requestedUserId)
    {
        if (requestedUserId <= 0)
            return requestedUserId;

        if (await _onboarding.RefreshRequirementAsync(requestedUserId) == InitialSetupStep.None)
            return requestedUserId;

        await using var context = await _dbFactory.CreateDbContextAsync();
        var candidatos = await context.Caja
            .AsNoTracking()
            .Where(c => c.IdUsuario.HasValue && c.IdUsuario.Value >= 1_000_000 && (c.Estado == true || c.Estado == null))
            .Select(c => c.IdUsuario!.Value)
            .Distinct()
            .OrderBy(id => id == 1_000_001 ? 0 : 1)
            .ThenBy(id => id)
            .ToListAsync();

        foreach (var candidateId in candidatos)
        {
            if (await _onboarding.RefreshRequirementAsync(candidateId) == InitialSetupStep.None)
                return candidateId;
        }

        // Mantener el estado de onboarding acorde al usuario actual si nadie está configurado aún
        await _onboarding.RefreshRequirementAsync(requestedUserId);
        return requestedUserId;
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

        var effectiveBillingUserId = await ResolveBillingUserIdAsync(userId);
        var requiredStep = await _onboarding.RefreshRequirementAsync(effectiveBillingUserId);
        if (requiredStep == InitialSetupStep.None)
            return true;

        var destination = requiredStep == InitialSetupStep.Emisor
            ? "/facturacion/configuracion/emisor"
            : "/facturacion/configuracion/caja";

        _navigation.NavigateTo(destination, replace: true);
        return false;
    }
}
