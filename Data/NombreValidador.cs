using System.Text.RegularExpressions;

namespace HosvetKardex.Web.Data;

public static class NombreValidador
{
    private static readonly Regex CaracteresValidosRegex = new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s'-]+$", RegexOptions.Compiled);
    private static readonly Regex EspaciosMultiplesRegex = new(@"\s+", RegexOptions.Compiled);

    public static string Sanitizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        return EspaciosMultiplesRegex.Replace(texto.Trim(), " ");
    }

    public static (bool EsValido, string? MensajeError) Validar(string? texto, string nombreCampo = "El campo")
    {
        if (string.IsNullOrWhiteSpace(texto))
            return (false, $"{nombreCampo} es obligatorio.");

        if (!CaracteresValidosRegex.IsMatch(texto))
            return (false, "Solo se permiten letras, tildes y guiones/apóstrofes válidos.");

        var sanitizado = Sanitizar(texto);
        if (sanitizado.Length < 2)
            return (false, $"{nombreCampo} debe tener al menos 2 caracteres.");

        return (true, null);
    }
}
