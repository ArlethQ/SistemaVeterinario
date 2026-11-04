using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HosvetKardex.Web.Services;

/// <summary>
/// Protege los controladores que las vistas Blazor Server consumen dentro del
/// mismo proceso. El token se crea al arrancar y nunca se entrega al navegador.
/// </summary>
public sealed class InternalApiToken
{
    public string Value { get; } = Convert.ToHexString(Guid.NewGuid().ToByteArray());
}

public sealed class InternalApiHeaderHandler(InternalApiToken token) : DelegatingHandler(new HttpClientHandler())
{
    public const string HeaderName = "X-Veterinaria-Internal-Api";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Remove(HeaderName);
        request.Headers.TryAddWithoutValidation(HeaderName, token.Value);
        return base.SendAsync(request, cancellationToken);
    }
}

public sealed class InternalApiAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    InternalApiToken token)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "VeterinariaInternalApi";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(InternalApiHeaderHandler.HeaderName, out var supplied) ||
            !string.Equals(supplied.ToString(), token.Value, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Query.TryGetValue("userId", out var rawUserId)
            ? rawUserId.ToString()
            : "0";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("IdUsuario", userId),
            new Claim(ClaimTypes.Name, "VeterinariaFacturacion")
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
