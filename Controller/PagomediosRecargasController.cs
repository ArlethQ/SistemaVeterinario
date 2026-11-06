using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.DTOs;
using Simetric.Models;
using HosvetKardex.Web.Services;

namespace HosvetKardex.Web.Controller;

[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/pagomedios/notificacion-compra-documentos")]
public sealed class PagomediosRecargasController(
    IDbContextFactory<AppDbContext> dbFactory,
    NumericaBackOfficeService numericaBackOffice,
    ILogger<PagomediosRecargasController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public Task<IActionResult> Get() => ProcesarAsync(
        Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase));

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var datos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (Request.HasFormContentType)
        {
            foreach (var item in await Request.ReadFormAsync()) datos[item.Key] = item.Value.ToString();
        }
        else
        {
            try
            {
                using var documento = await JsonDocument.ParseAsync(Request.Body);
                AgregarJson(datos, documento.RootElement);
            }
            catch (JsonException)
            {
                return BadRequest("La notificación de pago no contiene datos válidos.");
            }
        }

        foreach (var item in Request.Query) datos[item.Key] = item.Value.ToString();
        return await ProcesarAsync(datos);
    }

    private async Task<IActionResult> ProcesarAsync(Dictionary<string, string> datos)
    {
        var estado = Valor(datos, "status", "paymentStatus", "state");
        var referencia = Valor(datos, "reference", "transactionReference");
        var autorizacion = Valor(datos, "authorizationCode", "authorization_code", "authorization");
        var customValue = Valor(datos, "customValue", "custom_value", "customvalue");
        var compraId = Valor(datos, "purchase", "purchaseId", "purchase_id") ?? Segmento(customValue, "purchase");
        var usuarioTexto = Valor(datos, "uid", "userId", "user_id") ?? Segmento(customValue, "user");

        if (!int.TryParse(usuarioTexto, out var usuarioId) || usuarioId <= 0 || string.IsNullOrWhiteSpace(compraId))
        {
            logger.LogWarning("Notificación de recarga sin usuario o compra válidos.");
            return Redirigir("pendiente", compraId, referencia, autorizacion, 0);
        }

        await using var context = await dbFactory.CreateDbContextAsync();
        var usuario = await context.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == usuarioId);
        if (usuario?.estadoAsociado == true && usuario.idJefe is > 0)
            usuario = await context.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == usuario.idJefe.Value);

        if (usuario is null) return Redirigir("pendiente", compraId, referencia, autorizacion, 0);

        var historial = LeerHistorial(usuario.HistorialComprasDocumentosJson);
        var compra = historial.FirstOrDefault(x => string.Equals(x.Id, compraId, StringComparison.OrdinalIgnoreCase));
        if (compra is null) return Redirigir("pendiente", compraId, referencia, autorizacion, usuario.SaldoDocumentos);

        if (EsRechazado(estado) && !compra.SaldoAplicado)
        {
            historial.Remove(compra);
            usuario.HistorialComprasDocumentosJson = JsonSerializer.Serialize(historial, JsonOptions);
            await context.SaveChangesAsync();
            return Redirigir("rechazado", compraId, referencia, autorizacion, usuario.SaldoDocumentos);
        }

        var aprobado = compra.SaldoAplicado || EsAprobado(estado);
        compra.Reference = referencia ?? compra.Reference;
        compra.AuthorizationCode = autorizacion ?? compra.AuthorizationCode;
        compra.CustomValue = customValue ?? compra.CustomValue;
        compra.Estado = aprobado ? "Aprobado" : "Pendiente";

        if (aprobado && !compra.SaldoAplicado)
        {
            if (compra.EsIlimitado)
                compra.VigenciaHasta = DateTime.Now.AddYears(1);
            else
                usuario.SaldoDocumentos = Math.Max(0, usuario.SaldoDocumentos) + compra.Documentos;

            compra.SaldoAplicado = true;
            usuario.FechaUltimaRecargaDocumentos = DateTime.Now;
            var ventaBackOffice = new ReporteVentaBackOffice
            {
                Cliente = Cortar($"{usuario.Nombres} {usuario.Apellidos} | {usuario.Email}", 150),
                Producto = "E-FORVETS",
                PlanPaquete = Cortar(compra.EsIlimitado ? "Ilimitados durante 1 año" : $"{compra.Documentos} documentos", 100),
                Valor = compra.MontoTotal,
                Fecha = DateTime.Now,
                Canal = "Web Veterinaria",
                Vendedor = "Pago en línea",
                Estado = "pagada",
                FormaPago = "DeUna",
                Observacion = Cortar($"Recarga desde el sistema veterinario. Ref: {referencia} / {autorizacion}. Compra #{compra.Id}", 500)
            };
            await numericaBackOffice.RegistrarVentaAsync(ventaBackOffice, HttpContext.RequestAborted);
        }

        usuario.HistorialComprasDocumentosJson = JsonSerializer.Serialize(historial, JsonOptions);
        await context.SaveChangesAsync();
        return Redirigir(aprobado ? "aprobado" : "pendiente", compraId, referencia, autorizacion, usuario.SaldoDocumentos);
    }

    private IActionResult Redirigir(string estado, string? compra, string? referencia, string? autorizacion, int saldo)
    {
        var url = $"/contabilidad/compra-documentos?estado={Uri.EscapeDataString(estado)}&purchase={Uri.EscapeDataString(compra ?? string.Empty)}&saldo={saldo}";
        if (!string.IsNullOrWhiteSpace(referencia)) url += $"&reference={Uri.EscapeDataString(referencia)}";
        if (!string.IsNullOrWhiteSpace(autorizacion)) url += $"&authorizationCode={Uri.EscapeDataString(autorizacion)}";
        return Redirect(url);
    }

    private static List<CompraDocumentosHistorialItem> LeerHistorial(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<CompraDocumentosHistorialItem>>(json, JsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }

    private static bool EsAprobado(string? estado) => Normalizar(estado) is
        "approved" or "aprobado" or "paid" or "pagado" or "success" or "successful" or "completed" or "completado";

    private static bool EsRechazado(string? estado) => Normalizar(estado) is
        "rejected" or "rechazado" or "declined" or "failed" or "fallido" or "cancelled" or "canceled";

    private static string Normalizar(string? valor) => (valor ?? string.Empty).Trim().ToLowerInvariant();
    private static string Cortar(string valor, int maximo) => valor.Length <= maximo ? valor : valor[..maximo];

    private static string? Segmento(string? customValue, string clave) => customValue?
        .Split('|', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Split(':', 2))
        .FirstOrDefault(x => x.Length == 2 && x[0].Equals(clave, StringComparison.OrdinalIgnoreCase))?[1];

    private static string? Valor(IReadOnlyDictionary<string, string> datos, params string[] claves)
    {
        foreach (var clave in claves)
            if (datos.TryGetValue(clave, out var valor) && !string.IsNullOrWhiteSpace(valor)) return valor.Trim();
        return null;
    }

    private static void AgregarJson(IDictionary<string, string> destino, JsonElement elemento, string prefijo = "")
    {
        if (elemento.ValueKind != JsonValueKind.Object) return;
        foreach (var propiedad in elemento.EnumerateObject())
        {
            var clave = string.IsNullOrEmpty(prefijo) ? propiedad.Name : $"{prefijo}.{propiedad.Name}";
            if (propiedad.Value.ValueKind == JsonValueKind.Object) AgregarJson(destino, propiedad.Value, clave);
            else destino[clave] = propiedad.Value.ToString();
        }
    }
}
