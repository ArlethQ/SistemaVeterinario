using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Simetric.Services;

public sealed class FirmaInfoApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public FirmaInfoApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<FirmaInfoApiResult> ConsultarAsync(
        string rutaFirma,
        string passwordFirma,
        CancellationToken cancellationToken = default)
    {
        var endpointUrl = _configuration["FirmaInfoApi:Url"]?.Trim();
        var apiKey = _configuration["FirmaInfoApi:ApiKey"]?.Trim();

        // La API de E-FACT recibe una ruta de archivo y solo puede abrirla si
        // comparte el mismo disco del servidor publicado. En desarrollo, la
        // ruta pertenece al equipo veterinario; enviarla al servidor remoto
        // produciria un falso "archivo no encontrado". Si el archivo existe
        // aqui, se valida directamente con las mismas reglas criptograficas.
        if (File.Exists(rutaFirma))
            return ValidarCertificadoLocal(rutaFirma, passwordFirma);

        // En Veterinaria la firma se almacena en el mismo servidor. Si no se
        // configuro la API central de Numerica, validamos el PKCS#12 localmente
        // en lugar de impedir que el propietario guarde su certificado.
        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpoint) ||
            string.IsNullOrWhiteSpace(apiKey))
            return ValidarCertificadoLocal(rutaFirma, passwordFirma);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
        request.Content = JsonContent.Create(new FirmaInfoApiRequest(rutaFirma, passwordFirma));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var mensaje = ExtraerMensaje(rawBody)
                    ?? $"El servicio de validación de firma respondió con estado {(int)response.StatusCode} y no indicó la causa.";
                return FirmaInfoApiResult.Error(mensaje, rawBody, (int)response.StatusCode);
            }

            var info = JsonSerializer.Deserialize<FirmaInfoApiResponse>(
                rawBody,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return info is null
                ? FirmaInfoApiResult.Error("El servicio de firma devolvio una respuesta vacia o invalida.", rawBody, (int)response.StatusCode)
                : FirmaInfoApiResult.Ok(info, rawBody, (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return FirmaInfoApiResult.Error("La validación de la firma excedió el tiempo de espera. Intenta nuevamente.");
        }
        catch (HttpRequestException)
        {
            return FirmaInfoApiResult.Error("No fue posible conectar con el servicio de validación de firma. Intenta nuevamente.");
        }
        catch (JsonException)
        {
            return FirmaInfoApiResult.Error("El servicio de validación devolvió una respuesta que no se pudo interpretar.");
        }
    }

    private static FirmaInfoApiResult ValidarCertificadoLocal(string rutaFirma, string passwordFirma)
    {
        if (string.IsNullOrWhiteSpace(rutaFirma) || !File.Exists(rutaFirma))
            return FirmaInfoApiResult.Error("No se encontro el archivo .p12 para validar.");

        try
        {
            using var certificado = new X509Certificate2(
                rutaFirma,
                passwordFirma,
                X509KeyStorageFlags.EphemeralKeySet);

            var ahora = DateTimeOffset.Now;
            var fechaEmision = new DateTimeOffset(certificado.NotBefore);
            var fechaExpiracion = new DateTimeOffset(certificado.NotAfter);
            var tieneClavePrivada = certificado.HasPrivateKey;
            var vigente = ahora >= fechaEmision && ahora <= fechaExpiracion;
            var identificaciones = ExtraerIdentificaciones(certificado);
            var ruc = identificaciones.FirstOrDefault(x => x.Length == 13);
            var cedula = identificaciones.FirstOrDefault(x => x.Length == 10)
                ?? (ruc?.Length == 13 ? ruc[..10] : null);
            var estado = ahora < fechaEmision
                ? "AUN_NO_VIGENTE"
                : ahora > fechaExpiracion ? "CADUCADA" : "VIGENTE";

            var info = new FirmaInfoApiResponse
            {
                EsValida = vigente && tieneClavePrivada,
                EstadoVigencia = estado,
                Mensaje = !tieneClavePrivada
                    ? "El certificado no contiene una clave privada."
                    : vigente ? "Firma valida." : "La firma no esta vigente.",
                NombreTitular = certificado.GetNameInfo(X509NameType.SimpleName, false),
                Ruc = ruc,
                Cedula = cedula,
                FechaEmision = fechaEmision,
                FechaExpiracion = fechaExpiracion,
                DiasRestantes = (fechaExpiracion.Date - ahora.Date).Days,
                TieneClavePrivada = tieneClavePrivada,
                EmisorCertificado = certificado.Issuer,
                NumeroSerie = certificado.SerialNumber,
                HuellaDigital = certificado.Thumbprint
            };

            return FirmaInfoApiResult.Ok(info);
        }
        catch (CryptographicException)
        {
            return FirmaInfoApiResult.Error(
                "No se pudo abrir la firma. Verifica que la clave corresponda al archivo .p12 y que el archivo no este danado.");
        }
        catch (UnauthorizedAccessException)
        {
            return FirmaInfoApiResult.Error("No se tienen permisos para leer el archivo .p12.");
        }
        catch (IOException)
        {
            return FirmaInfoApiResult.Error("No se pudo leer el archivo .p12.");
        }
    }

    private static IReadOnlyList<string> ExtraerIdentificaciones(X509Certificate2 certificado)
    {
        var textos = new List<string> { certificado.Subject };
        foreach (var extension in certificado.Extensions)
        {
            try
            {
                textos.Add(extension.Format(false));
            }
            catch (CryptographicException)
            {
            }
        }

        return textos
            .SelectMany(texto => Regex.Matches(texto ?? string.Empty, @"(?<!\d)\d{10,13}(?!\d)")
                .Select(coincidencia => coincidencia.Value))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string? ExtraerMensaje(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            return ExtraerMensaje(document.RootElement);
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string? ExtraerMensaje(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return LimpiarMensaje(element.GetString());

        if (element.ValueKind == JsonValueKind.Array)
        {
            var mensajes = element.EnumerateArray()
                .Select(ExtraerMensaje)
                .Where(mensaje => !string.IsNullOrWhiteSpace(mensaje))
                .Take(3);
            return UnirMensajes(mensajes);
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var nombre in new[] { "mensaje", "message", "detalle", "detail", "error_description", "error", "errors", "title" })
        {
            var property = element.EnumerateObject()
                .FirstOrDefault(item => string.Equals(item.Name, nombre, StringComparison.OrdinalIgnoreCase));
            if (property.Value.ValueKind == JsonValueKind.Undefined)
                continue;

            var mensaje = ExtraerMensaje(property.Value);
            if (!string.IsNullOrWhiteSpace(mensaje))
                return mensaje;
        }

        var mensajesHijos = element.EnumerateObject()
            .Select(property => ExtraerMensaje(property.Value))
            .Where(mensaje => !string.IsNullOrWhiteSpace(mensaje))
            .Take(3);
        return UnirMensajes(mensajesHijos);
    }

    private static string? UnirMensajes(IEnumerable<string?> mensajes)
    {
        var valores = mensajes
            .Where(mensaje => !string.IsNullOrWhiteSpace(mensaje))
            .Select(mensaje => mensaje!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return valores.Length == 0 ? null : string.Join(" ", valores);
    }

    private static string? LimpiarMensaje(string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return null;

        var limpio = mensaje.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        return limpio.Length <= 500 ? limpio : $"{limpio[..497]}...";
    }

    private sealed record FirmaInfoApiRequest(
        [property: JsonPropertyName("RutaFirma")] string RutaFirma,
        [property: JsonPropertyName("PasswordFirma")] string PasswordFirma);
}

public sealed record FirmaInfoApiResult(
    bool Success,
    string Message,
    FirmaInfoApiResponse? Info,
    string? RawJson = null,
    int? HttpStatusCode = null)
{
    public static FirmaInfoApiResult Ok(FirmaInfoApiResponse info, string? rawJson = null, int? httpStatusCode = null) =>
        new(true, string.Empty, info, rawJson, httpStatusCode);

    public static FirmaInfoApiResult Error(string message, string? rawJson = null, int? httpStatusCode = null) =>
        new(false, message, null, rawJson, httpStatusCode);
}

public sealed class FirmaInfoApiResponse
{
    public bool EsValida { get; set; }
    public string? EstadoVigencia { get; set; }
    public string? Mensaje { get; set; }
    public string? NombreTitular { get; set; }
    public string? Ruc { get; set; }
    public string? Cedula { get; set; }
    public DateTimeOffset? FechaEmision { get; set; }
    public DateTimeOffset? FechaExpiracion { get; set; }
    public int DiasRestantes { get; set; }
    public bool TieneClavePrivada { get; set; }
    public string? EmisorCertificado { get; set; }
    public string? NumeroSerie { get; set; }
    public string? HuellaDigital { get; set; }
}
