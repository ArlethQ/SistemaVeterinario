namespace HosvetKardex.Web.Data;

public record ValidacionDocumentoResultado(bool EsValido, string Mensaje, string ValorLimpio);

public static class EcuadorDocumentoValidador
{
    public static string LimpiarPasaporte(string pasaporte)
    {
        if (string.IsNullOrWhiteSpace(pasaporte)) return string.Empty;
        return pasaporte
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace("#", "")
            .Trim()
            .ToUpperInvariant();
    }

    public static ValidacionDocumentoResultado Validar(string tipoDoc, string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return new(false, "El número de documento es obligatorio.", string.Empty);

        tipoDoc = tipoDoc?.Trim().ToUpperInvariant() ?? "CEDULA";

        if (tipoDoc == "PASAPORTE")
        {
            var limpio = LimpiarPasaporte(valor);
            if (limpio.Length < 6 || limpio.Length > 15)
                return new(false, "El pasaporte debe tener entre 6 y 15 caracteres.", limpio);

            if (!limpio.All(char.IsLetterOrDigit))
                return new(false, "El pasaporte solo puede contener caracteres alfanuméricos ([A-Z0-9]).", limpio);

            return new(true, "Pasaporte válido.", limpio);
        }

        // Para Cédula y RUC: Solo dígitos numéricos
        string raw = valor.Trim();
        if (!raw.All(char.IsDigit))
            return new(false, "Solo se permiten números (sin letras ni caracteres especiales).", raw);

        if (tipoDoc == "CEDULA")
        {
            if (raw.Length > 10)
                return new(false, "La cédula debe tener un máximo de 10 dígitos numéricos.", raw[..10]);

            return new(true, "Cédula válida.", raw);
        }

        if (tipoDoc == "RUC")
        {
            if (raw.Length > 13)
                return new(false, "El RUC debe tener un máximo de 13 dígitos numéricos.", raw[..13]);

            return new(true, "RUC válido.", raw);
        }

        return new(true, string.Empty, raw);
    }
}
