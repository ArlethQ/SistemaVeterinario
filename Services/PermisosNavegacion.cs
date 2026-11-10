using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public sealed class PermisosNavegacion(int rol, IEnumerable<MenuItemModel> menus)
{
    public IReadOnlyList<MenuItemModel> Menus { get; } = menus.ToList();
    private readonly HashSet<string> _rutas = menus.Select(m => Normalizar(m.Url ?? "")).ToHashSet();

    public bool Permite(string ruta)
    {
        if (rol <= 0) return false;
        var clave = Normalizar(ruta);
        if (rol == 1) return true;
        if (clave == "administracion/menus") return false;
        if (clave is "administracion/empresas" or "administracion/usuarios" or "administracion/roles"
            or "administracion/roles-permisos" or "administracion/bitacora" or "administracion/catalogos"
            or "administracion/catalogos-operativos" or "administracion/localidades" or "facturacion/productos")
            return false;

        // El triaje forma parte del flujo de recepción de órdenes. Quien puede
        // revisar órdenes debe poder abrir la bandeja de triaje correspondiente.
        if (clave == "admision/triaje" && _rutas.Contains("admision/ordenes"))
            return true;

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
