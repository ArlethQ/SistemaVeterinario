using Microsoft.AspNetCore.Components;

namespace Simetric.Services.EContax;

public static class EContaxRoutes
{
    public const string ServiceKey = "e-conta";
    public const string Root = "/contabilidad";
    public const string Dashboard = "/contabilidad";
    public const string DashboardAlias = "/contabilidad/dashboard";
    public const string Profile = "/contabilidad/perfil";
    public const string Afiliacion = "/contabilidad/afiliacion";
    public const string Soporte = "/contabilidad/soporte";
    public const string FacturacionNueva = "/contabilidad/facturacion/nueva";
    public const string FacturacionDocumentos = "/contabilidad/facturacion/documentos";
    public const string FacturacionNotaCredito = "/contabilidad/facturacion/nota-credito";
    public const string FacturacionNotasCreditoGeneradas = "/contabilidad/facturacion/notas-credito-generadas";
    public const string FacturacionNotaDebito = "/contabilidad/facturacion/nota-debito";
    public const string FacturacionNotasDebitoGeneradas = "/contabilidad/facturacion/notas-debito-generadas";
    public const string FacturacionGuiaRemision = "/contabilidad/facturacion/guia-remision";
    public const string FacturacionGuiasRemisionGeneradas = "/contabilidad/facturacion/guias-remision-generadas";
    public const string FacturacionRetencionesGeneradas = "/contabilidad/facturacion/retenciones-generadas";
    public const string Cotizaciones = "/contabilidad/cotizaciones";
    public const string Contribuyentes = "/contabilidad/contribuyentes";
    public const string Compras = "/contabilidad/compras";
    public const string ComprasDescargarSri = "/contabilidad/compras/descargar-sri";
    public const string Declaraciones = "/contabilidad/declaraciones";
    public const string ComprasImportarXml = "/contabilidad/compras/importar-xml";
    public const string ComprasOrdenes = "/contabilidad/compras/ordenes";
    public const string ComprasOrdenNueva = "/contabilidad/compras/ordenes/nueva";
    public const string ComprasRecepciones = "/contabilidad/compras/recepciones";
    public const string ComprasRecepcionNueva = "/contabilidad/compras/recepciones/nueva";
    public const string ComprasNuevaLiquidacion = "/contabilidad/compras/nueva-liquidacion";
    public const string ComprasLiquidacionesGeneradas = "/contabilidad/compras/liquidaciones-generadas";
    public const string CuentasPorCobrar = "/contabilidad/cuentas-por-cobrar";
    public const string CuentasPorCobrarEstadoCuenta = "/contabilidad/cuentas-por-cobrar/estado-cuenta";
    public const string CuentasPorPagar = "/contabilidad/cuentas-por-pagar";
    public const string CuentasPorPagarMisCompras = "/contabilidad/mis-compras";
    public const string CuentasPorPagarNuevaFactura = "/contabilidad/compras/nueva-factura";
    public const string CuentasPorPagarRegistrar = "/contabilidad/cuentas-por-pagar/registrar";
    public const string CuentasPorPagarEstadoCuenta = "/contabilidad/cuentas-por-pagar/estado-cuenta";
    public const string ReportesDocumentos = "/contabilidad/reportes/documentos";
    public const string ReportesLogs = "/contabilidad/reportes/logs";
    public const string CompraDocumentos = "/contabilidad/compra-documentos";
    public const string HistorialCompras = "/contabilidad/historial-compras";
    public const string AmActivos = "/contabilidad/activos-fijos/intangibles/registro";
    public const string AmCategorias = "/contabilidad/activos-fijos/intangibles/categorias";
    public const string AmMovimientos = "/contabilidad/activos-fijos/intangibles/movimientos";
    public const string AmDepreciacion = "/contabilidad/activos-fijos/intangibles/amortizacion";
    public const string AmReportes = "/contabilidad/activos-fijos/intangibles/reportes";
    public const string AfActivos = "/contabilidad/activos-fijos/registro";
    public const string AfCategorias = "/contabilidad/activos-fijos/categorias";
    public const string AfMovimientos = "/contabilidad/activos-fijos/movimientos";
    public const string AfDepreciacion = "/contabilidad/activos-fijos/depreciacion";
    public const string AfReportes = "/contabilidad/activos-fijos/reportes";
    public const string Anticipos = "/contabilidad/anticipos";
    public const string Bancos = "/contabilidad/bancos";
    public const string ContabilidadPlanCuentas = "/contabilidad/libros/plan-cuentas";
    public const string ContabilidadAsientos = "/contabilidad/libros/asientos";
    public const string ContabilidadLibroDiario = "/contabilidad/libros/libro-diario";
    public const string ContabilidadLibroMayor = "/contabilidad/libros/libro-mayor";
    public const string ContabilidadBalanceComprobacion = "/contabilidad/libros/balance-comprobacion";
    public const string ContabilidadBalanceGeneral = "/contabilidad/libros/balance-general";
    public const string ContabilidadEstadoResultados = "/contabilidad/libros/estado-resultados";
    public const string ContabilidadFlujoEfectivo = "/contabilidad/libros/flujo-efectivo";
    public const string Proveedores = "/contabilidad/proveedores";
    public const string Parametrizacion = "/contabilidad/parametrizacion";
    public const string SolicitudNueva = "/contabilidad/solicitud/nueva";
    public const string SolicitudPagos = "/contabilidad/solicitud/pagos";
    public const string SolicitudPagoResultado = "/contabilidad/solicitud/pago/resultado";
    public const string Suscripciones = "/contabilidad/suscripciones";
    public const string Organizacion = "/contabilidad/organizacion";
    public const string Usuarios = "/contabilidad/usuarios";
    public const string Clientes = "/contabilidad/clientes";
    public const string Emisor = "/contabilidad/emisor";
    public const string Productos = "/contabilidad/productos";
    public const string Inventarios = "/contabilidad/inventarios";
    public const string InventariosKardex = "/contabilidad/inventarios/kardex";
    public const string InventariosBodegas = "/contabilidad/inventarios/bodegas";
    public const string InventariosOperaciones = "/contabilidad/inventarios/operaciones";
    public const string InventariosReportes = "/contabilidad/inventarios/reportes";
    public const string InventariosAsignarStock = "/contabilidad/inventarios/asignar-stock";
    public const string ConfiguracionCategorias = "/contabilidad/configuracion/categorias";
    public const string ConfiguracionPuntosEmision = "/contabilidad/configuracion/puntos-emision";
    public const string ConfiguracionTiposCliente = "/contabilidad/configuracion/tipos-cliente";
    public const string ConfiguracionSeguridad = "/contabilidad/configuracion/seguridad";
    public const string ConfiguracionFirma = "/contabilidad/configuracion/firma";
    public const string ConfiguracionCentroNormativo = "/contabilidad/configuracion/centro-normativo";
    public const string ConfiguracionGeneral = "/contabilidad/configuracion/general";
    public const string Tutoriales = "/contabilidad/ayuda/tutoriales";
    public const string AdministracionFormasPago = "/contabilidad/administracion/formas-pago";
    public const string AdministracionIdentificaciones = "/contabilidad/administracion/identificaciones";
    public const string AdministracionImpuestos = "/contabilidad/administracion/impuestos";
    public const string AdministracionRetenciones = "/contabilidad/administracion/retenciones";
    public const string AdministracionRoles = "/contabilidad/administracion/roles";
    public const string AdministracionRolesInternos = "/contabilidad/administracion/roles-internos";
    public const string AdministracionUsuarios = "/contabilidad/administracion/usuarios";
    public const string AdministracionCajasSecuencias = "/contabilidad/administracion/cajas-secuencias";
    public const string AdministracionAuditoriaSql = "/contabilidad/administracion/auditoria-sql";
    public const string AdministracionLogsInicio = "/contabilidad/administracion/logs-inicio";

    public static bool IsEContaxPath(NavigationManager navigationManager, string location)
    {
        var relativePath = navigationManager.ToBaseRelativePath(location);
        var separatorIndex = relativePath.IndexOfAny(new[] { '?', '#' });
        var pathOnly = (separatorIndex >= 0 ? relativePath[..separatorIndex] : relativePath).Trim('/');

        return pathOnly.Equals("e-contax", StringComparison.OrdinalIgnoreCase) ||
               pathOnly.StartsWith("contabilidad/", StringComparison.OrdinalIgnoreCase);
    }
}
