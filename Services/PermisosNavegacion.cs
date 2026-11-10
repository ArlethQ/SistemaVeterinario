namespace HosvetKardex.Web.Services;

public sealed class PermisosNavegacion(int rol, IEnumerable<string> rutas)
{
    private readonly HashSet<string> _rutas = rutas.Select(Normalizar).ToHashSet();

    public bool Permite(string ruta)
    {
        if (rol <= 0) return false;
        if (rol == 1) return true;

        var clave = Normalizar(ruta);
        if (clave is "administracion/empresas" or "administracion/usuarios" or "administracion/roles"
            or "administracion/roles-permisos" or "administracion/bitacora" or "administracion/catalogos"
            or "administracion/catalogos-operativos" or "administracion/localidades" or "facturacion/productos")
            return false;

        return clave == "dashboard" || _rutas.Contains(clave);
    }

    public static string Normalizar(string ruta)
    {
        if (Uri.TryCreate(ruta, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            ruta = uri.AbsolutePath;

        var clave = ruta.Split('?', '#')[0].Trim().Replace('\\', '/').TrimStart('~').Trim('/').ToLowerInvariant();
        return clave switch
        {
            "contabilidad/dashboard" => "contabilidad",
            "contabilidad/inventarios/kardex" => "contabilidad/inventarios",
            "contabilidad/compras/ordenes/nueva" => "contabilidad/compras/ordenes",
            "contabilidad/compras/recepciones/nueva" => "contabilidad/compras/recepciones",
            "consulta/fichamedica/paciente" => "consulta/fichamedica",
            "consulta/curvacrecimiento" => "consulta/historial",
            _ => clave
        };
    }
}
