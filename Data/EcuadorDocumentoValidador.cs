namespace HosvetKardex.Web.Data;

public record ValidacionDocumentoResultado(bool EsValido, string Mensaje, string ValorLimpio);

public static class EcuadorDocumentoValidador
{
    private static readonly HashSet<int> ProvinciasValidas = new()
    {
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 30, 50
    };

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

        // Para Cédula y RUC se extraen solo dígitos
        var digitos = new string(valor.Where(char.IsDigit).ToArray());

        if (tipoDoc == "CEDULA")
        {
            if (digitos.Length != 10)
                return new(false, "La cédula debe contener exactamente 10 dígitos numéricos.", digitos);

            int prov = int.Parse(digitos[..2]);
            if (!ProvinciasValidas.Contains(prov))
                return new(false, $"Provincia inválida ({prov:D2}). Debe ser entre 01-24, 30 o 50.", digitos);

            int tercerDigito = digitos[2] - '0';
            if (tercerDigito >= 6)
                return new(false, $"Para cédula, el tercer dígito debe ser menor a 6 (actual: {tercerDigito}).", digitos);

            // Módulo 10 con décimo dígito verificador
            int[] coef = [2, 1, 2, 1, 2, 1, 2, 1, 2];
            int suma = 0;
            for (int i = 0; i < 9; i++)
            {
                int val = (digitos[i] - '0') * coef[i];
                if (val > 9) val -= 9;
                suma += val;
            }
            int verificadorEsperado = (suma % 10 == 0) ? 0 : 10 - (suma % 10);
            int digitoVerificador = digitos[9] - '0';

            if (digitoVerificador != verificadorEsperado)
                return new(false, "Número de cédula inválido (dígito verificador incorrecto).", digitos);

            return new(true, "Cédula válida.", digitos);
        }

        if (tipoDoc == "RUC")
        {
            if (digitos.Length != 13)
                return new(false, "El RUC debe contener exactamente 13 dígitos numéricos.", digitos);

            int prov = int.Parse(digitos[..2]);
            if (!ProvinciasValidas.Contains(prov))
                return new(false, $"Provincia inválida ({prov:D2}). Debe ser entre 01-24, 30 o 50.", digitos);

            int tercerDigito = digitos[2] - '0';

            if (tercerDigito < 6)
            {
                // Persona Natural: Módulo 10 en los primeros 10 dígitos y sucursal entre 001 y 999 (nunca 000)
                int[] coef = [2, 1, 2, 1, 2, 1, 2, 1, 2];
                int suma = 0;
                for (int i = 0; i < 9; i++)
                {
                    int val = (digitos[i] - '0') * coef[i];
                    if (val > 9) val -= 9;
                    suma += val;
                }
                int verificadorEsperado = (suma % 10 == 0) ? 0 : 10 - (suma % 10);
                int digitoVerificador = digitos[9] - '0';

                if (digitoVerificador != verificadorEsperado)
                    return new(false, "RUC Persona Natural inválido (verificador de cédula incorrecto).", digitos);

                string sucursal = digitos.Substring(10, 3);
                if (sucursal == "000")
                    return new(false, "RUC Persona Natural inválido: el establecimiento no puede ser 000 (debe ser 001 a 999).", digitos);

                return new(true, "RUC Persona Natural válido.", digitos);
            }
            else if (tercerDigito == 6)
            {
                // Sociedad Pública: Módulo 11 (coeficientes 3,2,7,6,5,4,3,2), verificador en posición 9 (índice 8)
                // Establecimiento: posiciones 10-13 (4 dígitos) entre 0001 y 9999 (nunca 0000)
                int[] coef = [3, 2, 7, 6, 5, 4, 3, 2];
                int suma = 0;
                for (int i = 0; i < 8; i++)
                {
                    suma += (digitos[i] - '0') * coef[i];
                }
                int residuo = suma % 11;
                int verificadorEsperado = (residuo == 0) ? 0 : 11 - residuo;
                int digitoVerificador = digitos[8] - '0';

                if (digitoVerificador != verificadorEsperado)
                    return new(false, "RUC Sociedad Pública inválido (verificador incorrecto en posición 9).", digitos);

                string sucursal = digitos.Substring(9, 4);
                if (sucursal == "0000")
                    return new(false, "RUC Sociedad Pública inválido: el establecimiento no puede ser 0000 (debe ser 0001 a 9999).", digitos);

                return new(true, "RUC Sociedad Pública válido.", digitos);
            }
            else if (tercerDigito == 9)
            {
                // Sociedad Privada / Extranjeros sin cédula: Módulo 11 (coeficientes 4,3,2,7,6,5,4,3,2), verificador en posición 10 (índice 9)
                // Establecimiento: posiciones 11-13 (3 dígitos) entre 001 y 999 (nunca 000)
                int[] coef = [4, 3, 2, 7, 6, 5, 4, 3, 2];
                int suma = 0;
                for (int i = 0; i < 9; i++)
                {
                    suma += (digitos[i] - '0') * coef[i];
                }
                int residuo = suma % 11;
                int verificadorEsperado = (residuo == 0) ? 0 : 11 - residuo;
                int digitoVerificador = digitos[9] - '0';

                if (digitoVerificador != verificadorEsperado)
                    return new(false, "RUC Sociedad Privada/Extranjero inválido (verificador incorrecto en posición 10).", digitos);

                string sucursal = digitos.Substring(10, 3);
                if (sucursal == "000")
                    return new(false, "RUC Sociedad Privada inválido: el establecimiento no puede ser 000 (debe ser 001 a 999).", digitos);

                return new(true, "RUC Sociedad Privada válido.", digitos);
            }
            else
            {
                return new(false, $"Tercer dígito del RUC inválido ({tercerDigito}). Debe ser <6 (Natural), 6 (Pública) o 9 (Privada).", digitos);
            }
        }

        return new(true, string.Empty, valor.Trim());
    }
}
