using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace HosvetKardex.Web.Services;

public class SessionStateService
{
    private const string StorageKey = "hosvet.session";
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);
    private readonly ProtectedLocalStorage _storage;
    private DateTimeOffset? _lastActivityUtc;

    public SessionStateService(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public int IdUser { get; private set; }
    public int TipoUser { get; private set; }
    public int EspUser { get; private set; }
    public int Localidad { get; private set; } = 1;
    public string LocalidadDescripcion { get; private set; } = string.Empty;
    public string Apostrofe { get; private set; } = string.Empty;
    public string LoginUser { get; private set; } = string.Empty;
    public string NombreUsuario { get; private set; } = string.Empty;
    public string Iniciales { get; private set; } = string.Empty;
    public bool IsAuthenticated => IdUser > 0 && _lastActivityUtc.HasValue && DateTimeOffset.UtcNow - _lastActivityUtc.Value < SessionTimeout;
    public int? OrdenActiva { get; private set; }
    public int? NhclActivo { get; private set; }
    public string? PacienteActivo { get; private set; }

    public SessionUserProxy? CurrentUser => IsAuthenticated
        ? new(IdUser, NombreUsuario, Iniciales, Localidad, Apostrofe)
        : null;
    public SessionUserProxy? UsuarioActual => CurrentUser;

    public record SessionUserProxy(int IdUsuario, string NombreCompleto, string Iniciales, int IdLocalidad, string PrefijoLocalidad)
    {
        public int UserNumero => IdUsuario;
    }

    public void SeleccionarOrden(int idOrden, int nhcl, string? paciente = null)
    {
        OrdenActiva = idOrden;
        NhclActivo = nhcl;
        PacienteActivo = paciente;
        NotifyStateChanged();
    }

    public event Action? OnChange;

    public async Task SetSessionAsync(UsuarioModel user, int localidad = 1, string localidadDesc = "HOSPITAL DE ESPECIALIDADES VETERINARIAS")
    {
        IdUser = user.UserNumero;
        TipoUser = user.UserTipoUserNumero;
        EspUser = user.UserEspNumero;
        LoginUser = user.UserLogin;
        NombreUsuario = user.UserNombre;
        Iniciales = string.IsNullOrWhiteSpace(user.UserIniciales) ? "HV" : user.UserIniciales.Trim();
        Localidad = localidad;
        if (!string.IsNullOrWhiteSpace(localidadDesc))
        {
            localidadDesc = System.Text.RegularExpressions.Regex.Replace(localidadDesc, @"\bUSFQ\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        }
        LocalidadDescripcion = localidadDesc;
        Apostrofe = localidad == 2 ? "EQ" : "HV";
        _lastActivityUtc = DateTimeOffset.UtcNow;
        await SaveAsync();
        NotifyStateChanged();
    }

    public async Task<bool> RestoreAsync()
    {
        if (IsAuthenticated) return true;

        try
        {
            var result = await _storage.GetAsync<PersistedSession>(StorageKey);
            if (!result.Success || result.Value is null || result.Value.ExpiresUtc <= DateTimeOffset.UtcNow)
            {
                await _storage.DeleteAsync(StorageKey);
                return false;
            }

            var saved = result.Value;
            IdUser = saved.IdUser;
            TipoUser = saved.TipoUser;
            EspUser = saved.EspUser;
            Localidad = saved.Localidad;
            LocalidadDescripcion = saved.LocalidadDescripcion;
            Apostrofe = saved.Apostrofe;
            LoginUser = saved.LoginUser;
            NombreUsuario = saved.NombreUsuario;
            Iniciales = saved.Iniciales;
            _lastActivityUtc = DateTimeOffset.UtcNow;
            await SaveAsync();
            NotifyStateChanged();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public bool ValidateAndTouch()
    {
        if (!IsAuthenticated)
        {
            ClearSession();
            return false;
        }

        _lastActivityUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public async Task LogoutAsync()
    {
        ClearSession();
        await _storage.DeleteAsync(StorageKey);
        NotifyStateChanged();
    }

    private async Task SaveAsync() => await _storage.SetAsync(StorageKey, new PersistedSession(
        IdUser, TipoUser, EspUser, Localidad, LocalidadDescripcion, Apostrofe,
        LoginUser, NombreUsuario, Iniciales, DateTimeOffset.UtcNow.Add(SessionTimeout)));

    private void ClearSession()
    {
        IdUser = 0;
        TipoUser = 0;
        EspUser = 0;
        LoginUser = string.Empty;
        NombreUsuario = string.Empty;
        Iniciales = string.Empty;
        Apostrofe = string.Empty;
        LocalidadDescripcion = string.Empty;
        OrdenActiva = null;
        NhclActivo = null;
        PacienteActivo = null;
        _lastActivityUtc = null;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public sealed record PersistedSession(
        int IdUser, int TipoUser, int EspUser, int Localidad, string LocalidadDescripcion,
        string Apostrofe, string LoginUser, string NombreUsuario, string Iniciales,
        DateTimeOffset ExpiresUtc);
}

public interface IAuthService
{
    Task<(bool Exito, string Mensaje, UsuarioModel? Usuario, bool RequiereCambioClave)> LoginAsync(string username, string password, int localidad = 1);
    Task<List<MenuItemModel>> ObtenerMenuUsuarioAsync(int idUser, int tipoUser);
    Task<List<RolUsuarioModel>> ObtenerRolesAsync();
    Task<List<UsuarioModel>> ObtenerUsuariosParaPermisosAsync();
    Task<List<MenuItemModel>> ObtenerMenusAdministracionAsync();
    Task<HashSet<int>> ObtenerAccesosUsuarioAsync(int idUser);
    Task<HashSet<int>> ObtenerAccesosRolAsync(int idRol);
    Task<bool> GuardarRolConPermisosAsync(RolUsuarioModel rol, IReadOnlyCollection<int> menuIds, int adminId);
    Task<(bool Exito, string Mensaje)> EliminarRolAsync(int idRol, int adminId);
    Task<bool> TieneAccesoRutaAsync(string ruta);
    Task<bool> GuardarRolYAccesosAsync(int idUser, int tipoUser, IReadOnlyCollection<int> menuIds, int adminId);
    Task<bool> GuardarRolAsync(RolUsuarioModel rol, int adminId);
    Task<bool> GuardarMenuAsync(MenuItemModel menu, int adminId);
    Task<List<UsuarioAdministracionModel>> ObtenerUsuariosAdministracionAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerEspecialidadesUsuarioAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerLocalidadesUsuarioAsync();
    Task<List<int>> ObtenerLocalidadesAsignadasAsync(int idUser);
    Task<(bool Exito, string Mensaje)> GuardarUsuarioAdministracionAsync(UsuarioAdministracionModel usuario, string? claveTemporal, int adminId);
    Task<(bool Exito, string Mensaje)> CambiarEstadoUsuarioAsync(int idUser, bool activo, int adminId);
    Task<(bool Exito, string Mensaje)> RestablecerClaveUsuarioAsync(int idUser, string nuevaClave, int adminId);
}

public class AuthService : IAuthService
{
    private const int MaxLoginAttempts = 5;
    private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(15);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, LoginAttempt> LoginAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ISqlDataService _sql;
    private readonly SessionStateService _session;

    public AuthService(ISqlDataService sql, SessionStateService session)
    {
        _sql = sql;
        _session = session;
    }

    public async Task<(bool Exito, string Mensaje, UsuarioModel? Usuario, bool RequiereCambioClave)> LoginAsync(string username, string password, int localidad = 1)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Los datos introducidos no son válidos.", null, false);

        string loginKey = username.Trim().ToUpperInvariant();
        if (LoginAttempts.TryGetValue(loginKey, out var attempt) &&
            attempt.WindowStartedUtc.Add(LoginWindow) > DateTimeOffset.UtcNow &&
            attempt.Count >= MaxLoginAttempts)
        {
            return (false, "No fue posible iniciar sesión. Inténtalo nuevamente más tarde.", null, false);
        }

        string claveHash = _sql.HashPasswordSha1(password);

        const string query = @"
            SELECT 
                user_numero AS UserNumero,
                RTRIM(ISNULL(user_login, '')) AS UserLogin,
                RTRIM(ISNULL(user_clave, '')) AS UserClave,
                RTRIM(ISNULL(user_nombre, '')) AS UserNombre,
                RTRIM(ISNULL(user_iniciales, '')) AS UserIniciales,
                ISNULL(user_tipouser_numero, 1) AS UserTipoUserNumero,
                ISNULL(user_esp_numero, 1) AS UserEspNumero,
                RTRIM(ISNULL(user_estado, 'A')) AS UserEstado,
                user_fechacambio AS UserFechaCambio,
                user_observaciones AS UserObservaciones
            FROM vt_usuarios
            WHERE user_estado = 'A' 
              AND user_login = @Login 
              AND user_clave = @Clave";

        var user = await _sql.QueryFirstOrDefaultAsync<UsuarioModel>(query, new { Login = username.Trim(), Clave = claveHash });
        if (user is null)
        {
            LoginAttempts.AddOrUpdate(
                loginKey,
                _ => new LoginAttempt(1, DateTimeOffset.UtcNow),
                (_, current) => current.WindowStartedUtc.Add(LoginWindow) <= DateTimeOffset.UtcNow
                    ? new LoginAttempt(1, DateTimeOffset.UtcNow)
                    : current with { Count = current.Count + 1 });
            await _sql.RegistrarBitacoraAsync($"Datos introducidos no válidos: {username}", 0);
            return (false, "Los datos introducidos no son válidos.", null, false);
        }

        const string localidadQuery = @"
            SELECT TOP (1) RTRIM(ISNULL(l.loc_descripcion, ''))
            FROM vt_localidades l
            INNER JOIN vt_usuarios u ON u.user_numero = @IdUsuario
            WHERE l.loc_id = @IdLocalidad
              AND ISNULL(l.loc_estado, 'A') = 'A'
              AND (
                    EXISTS (
                        SELECT 1
                        FROM vt_usuarioLocalidades ul
                        WHERE ul.ul_idUsuario = u.user_numero
                          AND ul.ul_idLocalidad = l.loc_id
                          AND ISNULL(ul.ul_estado, 'A') = 'A'
                    )
                    OR EXISTS (
                        SELECT 1
                        FROM EMPRESA e
                        WHERE e.estado = 1
                          AND (
                                e.idEmpresa = l.idEmpresa
                                OR (
                                    NULLIF(LTRIM(RTRIM(e.ruc)), '') IS NOT NULL
                                    AND LTRIM(RTRIM(e.ruc)) = LTRIM(RTRIM(l.loc_ruc))
                                )
                          )
                          AND (
                                e.idTitular = u.idUsuarioFacturacion
                                OR EXISTS (
                                    SELECT 1
                                    FROM Usuarios uf
                                    WHERE uf.IdUsuario = e.idTitular
                                      AND uf.Estado = 1
                                      AND NULLIF(LTRIM(RTRIM(uf.Email)), '') IS NOT NULL
                                      AND UPPER(LTRIM(RTRIM(uf.Email))) = UPPER(LTRIM(RTRIM(u.user_email)))
                                )
                          )
                    )
              )";
        var localidadDescripcion = await _sql.QueryFirstOrDefaultAsync<string>(
            localidadQuery,
            new { IdUsuario = user.UserNumero, IdLocalidad = localidad });
        if (string.IsNullOrWhiteSpace(localidadDescripcion))
        {
            await _sql.RegistrarBitacoraAsync(
                $"Acceso rechazado a localidad {localidad}: {user.UserNombre}",
                user.UserNumero);
            return (false, "El usuario no tiene acceso a la veterinaria o sucursal seleccionada.", null, false);
        }

        LoginAttempts.TryRemove(loginKey, out _);
        await _sql.RegistrarBitacoraAsync($"Ingresa al Sistema: {user.UserNombre}", user.UserNumero);
        await _session.SetSessionAsync(user, localidad);

        return (true, "Bienvenido", user, false);
    }

    private sealed record LoginAttempt(int Count, DateTimeOffset WindowStartedUtc);

    private static bool _modulosSincronizados;

    private async Task AsegurarCatalogoModulosCompletoAsync()
    {
        if (_modulosSincronizados) return;
        const string sql = @"
            DECLARE @Modulos TABLE (
                Sec INT,
                Descripcion VARCHAR(50),
                Padre INT,
                Url VARCHAR(150)
            );

            INSERT INTO @Modulos (Sec, Descripcion, Padre, Url) VALUES
                (101, 'Dashboard', 1, 'dashboard'),
                (102, 'Planes y suscripciones', 1, 'suscripciones'),
                (201, 'Clientes, tutores y mascotas', 2, 'admision/clientes-pacientes'),
                (202, 'Recepción de órdenes', 2, 'admision/ordenes'),
                (203, 'Agenda de citas', 2, 'admision/citas'),
                (204, 'Triaje de ingreso', 2, 'admision/triaje'),
                (301, 'Bandeja de órdenes', 3, 'kardex/ordenes'),
                (302, 'Kardex enfermería', 3, 'kardex/enfermeria'),
                (303, 'Signos y balance hídrico', 3, 'kardex/signos'),
                (401, 'Ficha de hospitalización', 4, 'hospitalizacion/ficha'),
                (402, 'Monitoreo 24 horas', 4, 'hospitalizacion/monitoreo'),
                (403, 'Turnos, exámenes y transfusión', 4, 'hospitalizacion/fichas-complementarias'),
                (404, 'Calculadora de infusiones', 4, 'hospitalizacion/calculadora-infusiones'),
                (501, 'Pre-anestesia', 5, 'quirofano/preanestesia'),
                (502, 'Checklist preoperatorio', 5, 'quirofano/preanestesia/checklist'),
                (503, 'Pausa de seguridad', 5, 'quirofano/pausa-seguridad'),
                (504, 'Hoja de anestesia', 5, 'quirofano/anestesia'),
                (505, 'Protocolo quirúrgico', 5, 'quirofano/protocolo'),
                (506, 'Sala de recuperación', 5, 'quirofano/recuperacion'),
                (601, 'Historia clínica SOAP', 6, 'consulta/fichamedica'),
                (602, 'Apuntes a historias', 6, 'consulta/historial/apuntes'),
                (603, 'Vacunación', 6, 'consulta/vacunas'),
                (604, 'Evolución médica', 6, 'consulta/evoluciones'),
                (605, 'Recetario médico', 6, 'consulta/recetario'),
                (606, 'Recetas sin orden', 6, 'consulta/recetario/sin-orden'),
                (607, 'Consentimientos y alta', 6, 'consulta/autorizaciones'),
                (608, 'Seguimiento clínico', 6, 'seguimiento/clinico'),
                (609, 'Expediente / Lotes fichas', 6, 'consulta/imprimir-fichas'),
                (701, 'Operaciones clínicas', 7, 'operaciones/clinicas'),
                (702, 'Programación clínica', 7, 'programacion/clinica'),
                (801, 'Órdenes de laboratorio', 8, 'diagnostico/laboratorio'),
                (802, 'Rayos X y ecografía', 8, 'diagnostico/imagenologia'),
                (803, 'Bandeja de resultados', 8, 'diagnostico/bandeja'),
                (901, 'Pre-facturación veterinaria', 9, 'facturacion/proformas'),
                (902, 'Productos y servicios', 9, 'facturacion/productos'),
                (903, 'Nueva factura electrónica', 9, 'facturacion/nueva'),
                (904, 'Facturas emitidas', 9, 'facturas'),
                (905, 'Nota de crédito', 9, 'facturacion/nota-credito'),
                (906, 'Notas de crédito emitidas', 9, 'facturacion/notas-credito-generadas'),
                (907, 'Nota de débito', 9, 'facturacion/nota-debito'),
                (908, 'Notas de débito emitidas', 9, 'facturacion/notas-debito-generadas'),
                (909, 'Guía de remisión', 9, 'facturacion/guia-remision'),
                (910, 'Guías emitidas', 9, 'facturacion/guias-remision-generadas'),
                (911, 'Importar compra / retención', 9, 'compras/importar-xml'),
                (912, 'Retenciones', 9, 'facturacion/retenciones-generadas'),
                (913, 'Recargar documentos', 9, 'contabilidad/compra-documentos'),
                (914, 'Liquidación de compra', 9, 'compras/nueva-liquidacion'),
                (915, 'Liquidaciones emitidas', 9, 'compras/liquidaciones-generadas'),
                (916, 'Configurar emisor', 9, 'facturacion/configuracion/emisor'),
                (917, 'Firma electrónica', 9, 'facturacion/configuracion/firma'),
                (918, 'Caja y secuenciales', 9, 'facturacion/configuracion/caja'),
                (1001, 'Inicio contable', 10, 'contabilidad'),
                (1002, 'Cuentas por cobrar y abonos', 10, 'contabilidad/cuentas-por-cobrar'),
                (1003, 'Clientes contables', 10, 'contabilidad/clientes'),
                (1004, 'Proveedores', 10, 'contabilidad/proveedores'),
                (1005, 'Importar compras', 10, 'contabilidad/compras/importar-xml'),
                (1006, 'Órdenes de compra', 10, 'contabilidad/compras/ordenes'),
                (1007, 'Recepciones de compra', 10, 'contabilidad/compras/recepciones'),
                (1008, 'Inventario y kardex', 10, 'contabilidad/inventarios'),
                (1009, 'Bodegas', 10, 'contabilidad/inventarios/bodegas'),
                (1010, 'Reportes contables', 10, 'contabilidad/reportes/documentos'),
                (1011, 'Parametrización', 10, 'contabilidad/parametrizacion'),
                (1012, 'Configuración contable', 10, 'contabilidad/configuracion/general'),
                (1013, 'Usuarios contables', 10, 'contabilidad/administracion/usuarios'),
                (1014, 'Roles y permisos contables', 10, 'contabilidad/administracion/roles'),
                (1015, 'Reportes clínicos', 10, 'reportes/clinicos'),
                (1101, 'Catálogos clínicos', 11, 'administracion/catalogos'),
                (1102, 'Diagnósticos y laboratorio', 11, 'administracion/catalogos-operativos'),
                (1103, 'Localidades y especialidades', 11, 'administracion/localidades'),
                (1201, 'Usuarios', 12, 'administracion/usuarios'),
                (1202, 'Roles', 12, 'administracion/roles'),
                (1203, 'Menús', 12, 'administracion/menus'),
                (1204, 'Asignar permisos', 12, 'administracion/roles-permisos'),
                (1205, 'Bitácora de auditoría', 12, 'administracion/bitacora');

            UPDATE dest
            SET dest.descripcion = src.Descripcion,
                dest.secNuevoMenu = src.Sec,
                dest.padreNuevoMenu = src.Padre,
                dest.habilitado = 1
            FROM vt_menuDinamico dest
            INNER JOIN @Modulos src
                ON LOWER(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(dest.url, ''))), '~/', ''), '/', '')) = LOWER(REPLACE(src.Url, '/', ''));

            INSERT INTO vt_menuDinamico (secNuevoMenu, descripcion, padreNuevoMenu, habilitado, url)
            SELECT src.Sec, src.Descripcion, src.Padre, 1, src.Url
            FROM @Modulos src
            WHERE NOT EXISTS (
                SELECT 1 FROM vt_menuDinamico ex
                WHERE LOWER(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(ex.url, ''))), '~/', ''), '/', '')) = LOWER(REPLACE(src.Url, '/', ''))
            )
            ORDER BY src.Sec;";

        try
        {
            await _sql.ExecuteAsync(sql);
            _modulosSincronizados = true;
        }
        catch { }
    }

    public async Task<List<MenuItemModel>> ObtenerMenuUsuarioAsync(int idUser, int tipoUser)
    {
        try
        {
            await AsegurarCatalogoModulosCompletoAsync();
            await AsegurarTablaRolesAccesosAsync();
            string sql = tipoUser == 1
                ? @"SELECT id AS Id,
                           RTRIM(CAST(ISNULL(secNuevoMenu, 0) AS VARCHAR(50))) AS SecNuevoMenu,
                           RTRIM(ISNULL(descripcion, '')) AS Descripcion,
                           RTRIM(CAST(ISNULL(padreNuevoMenu, 0) AS VARCHAR(50))) AS PadreNuevoMenu,
                           CAST(CASE WHEN ISNULL(habilitado, 0) = 1 THEN 1 ELSE 0 END AS BIT) AS Habilitado,
                           RTRIM(ISNULL(url, '')) AS Url
                    FROM vt_menuDinamico
                    WHERE ISNULL(habilitado, 0) = 1 AND ISNULL(LTRIM(RTRIM(url)), '') <> ''
                    ORDER BY ISNULL(secNuevoMenu, 999999), id"
                : @"SELECT m.id AS Id,
                           RTRIM(CAST(ISNULL(m.secNuevoMenu, 0) AS VARCHAR(50))) AS SecNuevoMenu,
                           RTRIM(ISNULL(m.descripcion, '')) AS Descripcion,
                           RTRIM(CAST(ISNULL(m.padreNuevoMenu, 0) AS VARCHAR(50))) AS PadreNuevoMenu,
                           CAST(CASE WHEN ISNULL(m.habilitado, 0) = 1 THEN 1 ELSE 0 END AS BIT) AS Habilitado,
                           RTRIM(ISNULL(m.url, '')) AS Url
                    FROM vt_menuDinamico m
                    WHERE ISNULL(m.habilitado, 0) = 1
                      AND ISNULL(LTRIM(RTRIM(m.url)), '') <> ''
                      AND (
                          EXISTS (
                              SELECT 1 FROM vt_accesos a
                              WHERE a.ac_idusuario = @IdUser AND TRY_CAST(a.ac_modulo AS INT) = m.id
                          )
                          OR (
                              NOT EXISTS (SELECT 1 FROM vt_accesos a2 WHERE a2.ac_idusuario = @IdUser AND TRY_CAST(a2.ac_modulo AS INT) IS NOT NULL)
                              AND EXISTS (
                                  SELECT 1 FROM vt_roles_accesos ra
                                  INNER JOIN vt_usuarios u ON u.user_tipouser_numero = ra.ra_idrol
                                  WHERE u.user_numero = @IdUser AND ra.ra_modulo = m.id
                              )
                          )
                      )
                    ORDER BY ISNULL(m.secNuevoMenu, 999999), m.id";

            var items = (await _sql.QueryAsync<MenuItemModel>(sql, new { IdUser = idUser })).ToList();
            return items;
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<RolUsuarioModel>> ObtenerRolesAsync()
    {
        const string sql = @"SELECT tipuser_numero AS Id, RTRIM(ISNULL(tipuser_descripcion, '')) AS Descripcion
                             FROM vt_tipos_usuarios
                             WHERE ISNULL(tipuser_estado, 'A') = 'A'
                             ORDER BY CASE WHEN tipuser_numero IN (1, 2) THEN 0 ELSE 1 END, tipuser_numero, tipuser_descripcion";
        try { return (await _sql.QueryAsync<RolUsuarioModel>(sql)).ToList(); }
        catch { return []; }
    }

    public async Task<List<UsuarioModel>> ObtenerUsuariosParaPermisosAsync()
    {
        const string sql = @"SELECT user_numero AS UserNumero,
                                    RTRIM(ISNULL(user_login, '')) AS UserLogin,
                                    RTRIM(ISNULL(user_nombre, '')) AS UserNombre,
                                    RTRIM(ISNULL(user_iniciales, '')) AS UserIniciales,
                                    ISNULL(user_tipouser_numero, 2) AS UserTipoUserNumero,
                                    RTRIM(ISNULL(user_estado, 'A')) AS UserEstado
                             FROM vt_usuarios
                             WHERE user_estado = 'A'
                             ORDER BY user_nombre";
        try { return (await _sql.QueryAsync<UsuarioModel>(sql)).ToList(); }
        catch { return []; }
    }

    public async Task<List<MenuItemModel>> ObtenerMenusAdministracionAsync()
    {
        await AsegurarCatalogoModulosCompletoAsync();
        const string sql = @"SELECT id AS Id,
                                    RTRIM(CAST(ISNULL(secNuevoMenu, 0) AS VARCHAR(50))) AS SecNuevoMenu,
                                    RTRIM(ISNULL(descripcion, '')) AS Descripcion,
                                    RTRIM(CAST(ISNULL(padreNuevoMenu, 0) AS VARCHAR(50))) AS PadreNuevoMenu,
                                    CAST(CASE WHEN ISNULL(habilitado, 0) = 1 THEN 1 ELSE 0 END AS BIT) AS Habilitado,
                                    RTRIM(ISNULL(url, '')) AS Url
                             FROM vt_menuDinamico
                             ORDER BY ISNULL(secNuevoMenu, 999999), Descripcion";
        try { return (await _sql.QueryAsync<MenuItemModel>(sql)).ToList(); }
        catch { return []; }
    }

    private async Task AsegurarTablaRolesAccesosAsync()
    {
        const string sql = @"
            IF OBJECT_ID('dbo.vt_roles_accesos', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.vt_roles_accesos (
                    ra_id INT IDENTITY(1,1) PRIMARY KEY,
                    ra_idrol INT NOT NULL,
                    ra_modulo INT NOT NULL,
                    CONSTRAINT UQ_vt_roles_accesos UNIQUE (ra_idrol, ra_modulo)
                );

                INSERT INTO dbo.vt_roles_accesos (ra_idrol, ra_modulo)
                SELECT DISTINCT u.user_tipouser_numero, TRY_CAST(a.ac_modulo AS INT)
                FROM dbo.vt_usuarios u
                INNER JOIN dbo.vt_accesos a ON a.ac_idusuario = u.user_numero
                WHERE ISNULL(u.user_tipouser_numero, 0) > 1
                  AND TRY_CAST(a.ac_modulo AS INT) IS NOT NULL;
            END";
        try { await _sql.ExecuteAsync(sql); }
        catch { }
    }

    public async Task<HashSet<int>> ObtenerAccesosRolAsync(int idRol)
    {
        if (idRol <= 0) return [];
        await AsegurarTablaRolesAccesosAsync();
        if (idRol == 1)
        {
            const string sqlAdmin = "SELECT id FROM vt_menuDinamico WHERE ISNULL(habilitado, 0) = 1";
            try { return (await _sql.QueryAsync<int>(sqlAdmin)).ToHashSet(); }
            catch { return []; }
        }
        const string sql = "SELECT ra_modulo FROM vt_roles_accesos WHERE ra_idrol = @IdRol";
        try { return (await _sql.QueryAsync<int>(sql, new { IdRol = idRol })).ToHashSet(); }
        catch { return []; }
    }

    public async Task<bool> GuardarRolConPermisosAsync(RolUsuarioModel rol, IReadOnlyCollection<int> menuIds, int adminId)
    {
        if (!_session.IsAuthenticated || _session.TipoUser != 1 || _session.IdUser != adminId || string.IsNullOrWhiteSpace(rol.Descripcion))
            return false;

        try
        {
            await AsegurarTablaRolesAccesosAsync();
            var ids = menuIds.Distinct().ToArray();
            bool esEdicion = rol.Id > 0;
            string insertRoleAccess = ids.Length == 0
                ? string.Empty
                : "INSERT INTO vt_roles_accesos (ra_idrol, ra_modulo) SELECT @RolId, id FROM vt_menuDinamico WHERE id IN @MenuIds;";

            string syncUsersSql = ids.Length == 0
                ? @"DELETE a
                    FROM vt_accesos a
                    INNER JOIN vt_usuarios u ON u.user_numero = a.ac_idusuario
                    INNER JOIN @Anteriores ant ON ant.modulo = TRY_CAST(a.ac_modulo AS INT)
                    WHERE u.user_tipouser_numero = @RolId;"
                : @"DELETE a
                    FROM vt_accesos a
                    INNER JOIN vt_usuarios u ON u.user_numero = a.ac_idusuario
                    INNER JOIN @Anteriores ant ON ant.modulo = TRY_CAST(a.ac_modulo AS INT)
                    WHERE u.user_tipouser_numero = @RolId
                      AND TRY_CAST(a.ac_modulo AS INT) NOT IN @MenuIds;

                    INSERT INTO vt_accesos (ac_idusuario, ac_modulo)
                    SELECT u.user_numero, CAST(m.id AS VARCHAR(50))
                    FROM vt_usuarios u
                    CROSS JOIN vt_menuDinamico m
                    WHERE u.user_tipouser_numero = @RolId
                      AND m.id IN @MenuIds
                      AND NOT EXISTS (
                          SELECT 1 FROM vt_accesos ex
                          WHERE ex.ac_idusuario = u.user_numero AND TRY_CAST(ex.ac_modulo AS INT) = m.id
                      );";

            string sql = $@"
                BEGIN TRY
                    BEGIN TRANSACTION;
                    DECLARE @RolId INT = @Id;
                    IF @RolId > 0
                    BEGIN
                        IF @RolId NOT IN (1, 2)
                        BEGIN
                            UPDATE vt_tipos_usuarios
                            SET tipuser_descripcion = @Descripcion,
                                tipuser_estado = 'A'
                            WHERE tipuser_numero = @RolId;
                        END
                    END
                    ELSE
                    BEGIN
                        INSERT INTO vt_tipos_usuarios (tipuser_descripcion, tipuser_estado)
                        VALUES (@Descripcion, 'A');
                        SET @RolId = CAST(SCOPE_IDENTITY() AS INT);
                    END

                    IF @RolId <> 1
                    BEGIN
                        DECLARE @Anteriores TABLE (modulo INT);
                        INSERT INTO @Anteriores (modulo)
                        SELECT ra_modulo FROM vt_roles_accesos WHERE ra_idrol = @RolId;

                        IF NOT EXISTS (SELECT 1 FROM @Anteriores)
                        BEGIN
                            INSERT INTO @Anteriores (modulo)
                            SELECT DISTINCT TRY_CAST(a.ac_modulo AS INT)
                            FROM vt_accesos a
                            INNER JOIN vt_usuarios u ON u.user_numero = a.ac_idusuario
                            WHERE u.user_tipouser_numero = @RolId
                              AND TRY_CAST(a.ac_modulo AS INT) IS NOT NULL;
                        END

                        DELETE FROM vt_roles_accesos WHERE ra_idrol = @RolId;
                        {insertRoleAccess}
                        {syncUsersSql}
                    END

                    COMMIT TRANSACTION;
                    SELECT @RolId;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH";

            var nuevoId = await _sql.ExecuteScalarAsync<int>(sql, new { rol.Id, Descripcion = rol.Descripcion.Trim(), MenuIds = ids });
            if (nuevoId <= 0) return false;
            rol.Id = nuevoId;
            await _sql.RegistrarBitacoraAsync($"{(esEdicion ? "Actualiza" : "Crea")} rol y permisos predeterminados: {rol.Descripcion.Trim()} ({ids.Length} módulos)", adminId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Exito, string Mensaje)> EliminarRolAsync(int idRol, int adminId)
    {
        if (!EsAdministradorValido(adminId))
            return (false, "No autorizado.");

        if (idRol is 1 or 2)
            return (false, "Los roles predeterminados (Administrador y Médico) no se pueden eliminar.");

        try
        {
            var usuariosAsignados = await _sql.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM vt_usuarios WHERE user_tipouser_numero = @IdRol",
                new { IdRol = idRol });

            if (usuariosAsignados > 0)
                return (false, $"No se puede eliminar este rol porque hay {usuariosAsignados} usuario(s) que tienen este rol asignado.");

            var nombreRol = await _sql.QueryFirstOrDefaultAsync<string>(
                "SELECT RTRIM(ISNULL(tipuser_descripcion, '')) FROM vt_tipos_usuarios WHERE tipuser_numero = @IdRol",
                new { IdRol = idRol }) ?? $"#{idRol}";

            await AsegurarTablaRolesAccesosAsync();
            await _sql.ExecuteAsync(@"
                DELETE FROM vt_roles_accesos WHERE ra_idrol = @IdRol;
                DELETE FROM vt_tipos_usuarios WHERE tipuser_numero = @IdRol AND tipuser_numero NOT IN (1, 2);",
                new { IdRol = idRol });

            await _sql.RegistrarBitacoraAsync($"Elimina rol: {nombreRol} (ID {idRol})", adminId);
            return (true, $"Rol \"{nombreRol}\" eliminado correctamente.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar el rol: {ex.Message}");
        }
    }

    public async Task<HashSet<int>> ObtenerAccesosUsuarioAsync(int idUser)
    {
        await AsegurarTablaRolesAccesosAsync();
        const string sql = @"
            IF EXISTS (SELECT 1 FROM vt_accesos WHERE ac_idusuario = @IdUser AND TRY_CAST(ac_modulo AS INT) IS NOT NULL)
                SELECT TRY_CAST(ac_modulo AS INT) FROM vt_accesos WHERE ac_idusuario = @IdUser AND TRY_CAST(ac_modulo AS INT) IS NOT NULL;
            ELSE
                SELECT ra.ra_modulo
                FROM vt_roles_accesos ra
                INNER JOIN vt_usuarios u ON u.user_tipouser_numero = ra.ra_idrol
                WHERE u.user_numero = @IdUser;";
        try { return (await _sql.QueryAsync<int>(sql, new { IdUser = idUser })).ToHashSet(); }
        catch { return []; }
    }

    public async Task<bool> TieneAccesoRutaAsync(string ruta)
    {
        if (!_session.IsAuthenticated) return false;
        if (_session.TipoUser == 1) return true;

        string rutaNormalizada = NormalizarRuta(ruta);
        if (rutaNormalizada == "/dashboard") return true;

        var menus = await ObtenerMenuUsuarioAsync(_session.IdUser, _session.TipoUser);
        bool autorizado = menus.Any(menu =>
        {
            string rutaMenu = NormalizarRuta(menu.Url);
            return rutaMenu.Length > 1 && (rutaNormalizada == rutaMenu || rutaNormalizada.StartsWith(rutaMenu + "/", StringComparison.OrdinalIgnoreCase));
        });

        if (!autorizado)
            await _sql.RegistrarBitacoraAsync($"Acceso denegado a ruta: {rutaNormalizada}", _session.IdUser);

        return autorizado;
    }

    private static string NormalizarRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return string.Empty;
        string valor = ruta.Trim();
        if (Uri.TryCreate(valor, UriKind.Absolute, out var uri)) valor = uri.AbsolutePath;
        int queryIndex = valor.IndexOfAny(['?', '#']);
        if (queryIndex >= 0) valor = valor[..queryIndex];
        valor = valor.Replace('\\', '/').TrimStart('~');
        if (!valor.StartsWith('/')) valor = "/" + valor;
        return valor.Length > 1 ? valor.TrimEnd('/').ToLowerInvariant() : valor;
    }

    public async Task<bool> GuardarRolYAccesosAsync(int idUser, int tipoUser, IReadOnlyCollection<int> menuIds, int adminId)
    {
        if (!_session.IsAuthenticated || _session.TipoUser != 1 || _session.IdUser != adminId)
            return false;

        var ids = menuIds.Distinct().ToArray();
        var insert = ids.Length == 0
            ? string.Empty
            : "INSERT INTO vt_accesos (ac_idusuario, ac_modulo) SELECT @IdUser, CAST(id AS VARCHAR(50)) FROM vt_menuDinamico WHERE id IN @MenuIds;";
        var sql = $@"
            BEGIN TRY
                BEGIN TRANSACTION;
                UPDATE vt_usuarios SET user_tipouser_numero = @TipoUser WHERE user_numero = @IdUser;
                DELETE FROM vt_accesos WHERE ac_idusuario = @IdUser;
                {insert}
                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH";

        var result = await _sql.ExecuteAsync(sql, new { IdUser = idUser, TipoUser = tipoUser, MenuIds = ids });
        if (result < 0) return false;
        await _sql.RegistrarBitacoraAsync($"Actualiza rol y permisos del usuario {idUser}", adminId);
        return true;
    }

    public async Task<bool> GuardarRolAsync(RolUsuarioModel rol, int adminId)
    {
        if (!_session.IsAuthenticated || _session.TipoUser != 1 || _session.IdUser != adminId || string.IsNullOrWhiteSpace(rol.Descripcion)) return false;
        if (rol.Id is 1 or 2) return true;
        var sql = rol.Id > 0
            ? "UPDATE vt_tipos_usuarios SET tipuser_descripcion = @Descripcion, tipuser_estado = 'A' WHERE tipuser_numero = @Id AND tipuser_numero NOT IN (1, 2)"
            : "INSERT INTO vt_tipos_usuarios (tipuser_descripcion, tipuser_estado) VALUES (@Descripcion, 'A')";
        var result = await _sql.ExecuteAsync(sql, new { rol.Id, Descripcion = rol.Descripcion.Trim() });
        if (result < 0) return false;
        await _sql.RegistrarBitacoraAsync($"{(rol.Id > 0 ? "Actualiza" : "Crea")} rol: {rol.Descripcion.Trim()}", adminId);
        return true;
    }

    public async Task<bool> GuardarMenuAsync(MenuItemModel menu, int adminId)
    {
        if (!_session.IsAuthenticated || _session.TipoUser != 1 || _session.IdUser != adminId || string.IsNullOrWhiteSpace(menu.Descripcion)) return false;
        var sql = menu.Id > 0
            ? @"UPDATE vt_menuDinamico SET secNuevoMenu = TRY_CAST(@Seccion AS INT), descripcion = @Descripcion,
                    padreNuevoMenu = TRY_CAST(@Padre AS INT), habilitado = @Habilitado, url = @Url WHERE id = @Id"
            : @"INSERT INTO vt_menuDinamico (secNuevoMenu, descripcion, padreNuevoMenu, habilitado, url)
                VALUES (TRY_CAST(@Seccion AS INT), @Descripcion, TRY_CAST(@Padre AS INT), @Habilitado, @Url)";
        var result = await _sql.ExecuteAsync(sql, new { menu.Id, Seccion = menu.SecNuevoMenu?.Trim() ?? string.Empty, Descripcion = menu.Descripcion.Trim(), Padre = menu.PadreNuevoMenu?.Trim() ?? string.Empty, Habilitado = menu.Habilitado ? 1 : 0, Url = menu.Url?.Trim() ?? string.Empty });
        if (result < 0) return false;
        await _sql.RegistrarBitacoraAsync($"{(menu.Id > 0 ? "Actualiza" : "Crea")} menú: {menu.Descripcion.Trim()}", adminId);
        return true;
    }

    public async Task<List<UsuarioAdministracionModel>> ObtenerUsuariosAdministracionAsync()
    {
        if (!_session.IsAuthenticated || _session.TipoUser != 1) return [];
        const string sql = @"SELECT user_numero AS Id, RTRIM(ISNULL(user_nombre, '')) AS Nombre,
                                    RTRIM(ISNULL(user_login, '')) AS Login, RTRIM(ISNULL(user_iniciales, '')) AS Iniciales,
                                    RTRIM(ISNULL(user_email, '')) AS Email, RTRIM(ISNULL(user_celular, '')) AS Celular,
                                    RTRIM(ISNULL(user_convensional, '')) AS Convencional,
                                    RTRIM(ISNULL(user_cedula, '')) AS Cedula,
                                    ISNULL(user_tipouser_numero, 2) AS RolId, ISNULL(user_esp_numero, 1) AS EspecialidadId,
                                    RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad,
                                    RTRIM(ISNULL(user_tipomedico, 'N')) AS TipoMedico,
                                    LTRIM(RTRIM(ISNULL(CONVERT(nvarchar(max), user_observaciones), ''))) AS Observaciones,
                                    RTRIM(ISNULL(user_estado, 'A')) AS Estado
                             FROM vt_usuarios u
                             LEFT JOIN vt_especialidades e ON e.es_id=u.user_esp_numero
                             ORDER BY user_nombre";
        try { return (await _sql.QueryAsync<UsuarioAdministracionModel>(sql)).ToList(); }
        catch { return []; }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerEspecialidadesUsuarioAsync()
    {
        try { return (await _sql.QueryAsync<CatalogoItemGenericoDto>("SELECT es_id AS Id, RTRIM(es_descripcion) AS Descripcion FROM vt_especialidades ORDER BY es_descripcion")).ToList(); }
        catch { return []; }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerLocalidadesUsuarioAsync()
    {
        try { return (await _sql.QueryAsync<CatalogoItemGenericoDto>("SELECT loc_id AS Id, RTRIM(loc_descripcion) AS Descripcion FROM vt_localidades WHERE loc_estado='A' ORDER BY loc_descripcion")).ToList(); }
        catch { return []; }
    }

    public async Task<List<int>> ObtenerLocalidadesAsignadasAsync(int idUser) =>
        (await _sql.QueryAsync<int>("SELECT ul_idLocalidad FROM vt_usuarioLocalidades WHERE ul_idUsuario=@IdUser AND ISNULL(ul_estado,'A')='A'", new { IdUser=idUser })).ToList();

    public async Task<(bool Exito, string Mensaje)> GuardarUsuarioAdministracionAsync(UsuarioAdministracionModel usuario, string? claveTemporal, int adminId) => await SafeGuardarUsuarioAdministracionAsync(usuario, claveTemporal, adminId);
    private async Task<(bool Exito, string Mensaje)> SafeGuardarUsuarioAdministracionAsync(UsuarioAdministracionModel usuario, string? claveTemporal, int adminId)
    {
        try { return await DoGuardarUsuarioAdministracionAsync(usuario, claveTemporal, adminId); }
        catch (Exception ex) { return (false, $"Error al guardar el usuario: {ex.Message}"); }
    }
    private async Task<(bool Exito, string Mensaje)> DoGuardarUsuarioAdministracionAsync(UsuarioAdministracionModel usuario, string? claveTemporal, int adminId)
    {
        if (!EsAdministradorValido(adminId)) return (false, "No autorizado.");
        if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Login) || usuario.RolId <= 0) return (false, "Nombre, usuario y rol son obligatorios.");
        var duplicado = await _sql.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM vt_usuarios WHERE user_login = @Login AND user_numero <> @Id", new { Login = usuario.Login.Trim(), usuario.Id });
        if (duplicado > 0) return (false, "El nombre de usuario ya está registrado.");
        if (usuario.Id == _session.IdUser && usuario.RolId != 1) return (false, "No puedes quitarte tu propio rol de administrador.");
        if (usuario.Id > 0 && usuario.RolId != 1 && !await PuedeReducirAdministradoresAsync(usuario.Id)) return (false, "No se puede cambiar el rol del último administrador activo.");

        if (usuario.Id == 0 && (string.IsNullOrWhiteSpace(claveTemporal) || claveTemporal.Length < 8)) return (false, "La contraseña temporal debe tener al menos 8 caracteres.");
        int rolAnterior = usuario.Id > 0 ? await _sql.ExecuteScalarAsync<int>("SELECT ISNULL(user_tipouser_numero, 0) FROM vt_usuarios WHERE user_numero=@Id", new { usuario.Id }) : 0;
        var sql = usuario.Id > 0
            ? @"UPDATE vt_usuarios SET user_nombre=@Nombre, user_login=@Login, user_iniciales=@Iniciales,
                    user_email=@Email, user_celular=@Celular, user_convensional=@Convencional, user_cedula=@Cedula,
                    user_tipouser_numero=@RolId, user_esp_numero=@EspecialidadId, user_tipomedico=@TipoMedico,
                    user_observaciones=@Observaciones WHERE user_numero=@Id"
            : @"INSERT INTO vt_usuarios (user_nombre,user_login,user_clave,user_iniciales,user_email,user_celular,user_convensional,user_cedula,user_tipouser_numero,user_esp_numero,user_tipomedico,user_observaciones,user_estado,user_fechacambio)
                VALUES (@Nombre,@Login,@Clave,@Iniciales,@Email,@Celular,@Convencional,@Cedula,@RolId,@EspecialidadId,@TipoMedico,@Observaciones,'A',GETDATE())";
        var args = new { usuario.Id, Nombre=usuario.Nombre.Trim(), Login=usuario.Login.Trim(), Iniciales=usuario.Iniciales.Trim(), Email=usuario.Email.Trim(), Celular=usuario.Celular.Trim(), Convencional=usuario.Convencional.Trim(), Cedula=usuario.Cedula.Trim(), usuario.RolId, EspecialidadId = usuario.RolId == 2 ? usuario.EspecialidadId : 0, TipoMedico = usuario.RolId == 2 ? usuario.TipoMedico.Trim() : "N", Observaciones=usuario.Observaciones.Trim(), Clave=_sql.HashPasswordSha1(claveTemporal ?? string.Empty) };
        var result = await _sql.ExecuteAsync(sql, args);
        if (result < 0) return (false, "No se pudo guardar el usuario.");
        var idUser = usuario.Id > 0 ? usuario.Id : await _sql.ExecuteScalarAsync<int>("SELECT user_numero FROM vt_usuarios WHERE user_login=@Login", new { Login=usuario.Login.Trim() });
        if (usuario.Id == 0 || rolAnterior != usuario.RolId)
        {
            await AsegurarTablaRolesAccesosAsync();
            await _sql.ExecuteAsync("DELETE FROM vt_accesos WHERE ac_idusuario=@IdUser", new { IdUser = idUser });
            if (usuario.RolId != 1)
            {
                await _sql.ExecuteAsync("INSERT INTO vt_accesos (ac_idusuario, ac_modulo) SELECT @IdUser, CAST(ra_modulo AS VARCHAR(50)) FROM vt_roles_accesos WHERE ra_idrol=@RolId", new { IdUser = idUser, usuario.RolId });
            }
        }
        await _sql.ExecuteAsync("DELETE FROM vt_usuarioLocalidades WHERE ul_idUsuario=@IdUser", new { IdUser=idUser });
        foreach (var localidadId in usuario.LocalidadesIds.Distinct())
            await _sql.ExecuteAsync("INSERT INTO vt_usuarioLocalidades (ul_idUsuario,ul_idLocalidad,ul_estado) VALUES (@IdUser,@LocalidadId,'A')", new { IdUser=idUser, LocalidadId=localidadId });
        await _sql.RegistrarBitacoraAsync($"{(usuario.Id > 0 ? "Actualiza" : "Crea")} usuario: {usuario.Login.Trim()}", adminId);
        return (true, "Usuario guardado correctamente.");
    }

    public async Task<(bool Exito, string Mensaje)> CambiarEstadoUsuarioAsync(int idUser, bool activo, int adminId) => await SafeCambiarEstadoUsuarioAsync(idUser, activo, adminId);
    private async Task<(bool Exito, string Mensaje)> SafeCambiarEstadoUsuarioAsync(int idUser, bool activo, int adminId)
    {
        try { return await DoCambiarEstadoUsuarioAsync(idUser, activo, adminId); }
        catch (Exception ex) { return (false, $"Error al cambiar estado: {ex.Message}"); }
    }
    private async Task<(bool Exito, string Mensaje)> DoCambiarEstadoUsuarioAsync(int idUser, bool activo, int adminId)
    {
        if (!EsAdministradorValido(adminId)) return (false, "No autorizado.");
        if (idUser == _session.IdUser && !activo) return (false, "No puedes desactivar tu propia cuenta.");
        if (!activo && !await PuedeReducirAdministradoresAsync(idUser)) return (false, "No se puede desactivar al último administrador activo.");
        var result = await _sql.ExecuteAsync("UPDATE vt_usuarios SET user_estado=@Estado WHERE user_numero=@IdUser", new { Estado=activo?"A":"I", IdUser=idUser });
        if (result < 0) return (false, "No se pudo actualizar el estado.");
        await _sql.RegistrarBitacoraAsync($"{(activo ? "Activa" : "Desactiva")} usuario {idUser}", adminId);
        return (true, activo ? "Usuario activado." : "Usuario desactivado.");
    }

    public async Task<(bool Exito, string Mensaje)> RestablecerClaveUsuarioAsync(int idUser, string nuevaClave, int adminId) => await SafeRestablecerClaveUsuarioAsync(idUser, nuevaClave, adminId);
    private async Task<(bool Exito, string Mensaje)> SafeRestablecerClaveUsuarioAsync(int idUser, string nuevaClave, int adminId)
    {
        try { return await DoRestablecerClaveUsuarioAsync(idUser, nuevaClave, adminId); }
        catch (Exception ex) { return (false, $"Error al restablecer contraseña: {ex.Message}"); }
    }
    private async Task<(bool Exito, string Mensaje)> DoRestablecerClaveUsuarioAsync(int idUser, string nuevaClave, int adminId)
    {
        if (!EsAdministradorValido(adminId)) return (false, "No autorizado.");
        if (string.IsNullOrWhiteSpace(nuevaClave) || nuevaClave.Length < 8) return (false, "La contraseña debe tener al menos 8 caracteres.");
        var result = await _sql.ExecuteAsync("UPDATE vt_usuarios SET user_clave=@Clave,user_fechacambio=GETDATE() WHERE user_numero=@IdUser", new { Clave=_sql.HashPasswordSha1(nuevaClave), IdUser=idUser });
        if (result < 0) return (false, "No se pudo restablecer la contraseña.");
        await _sql.RegistrarBitacoraAsync($"Restablece contraseña del usuario {idUser}", adminId);
        return (true, "Contraseña restablecida correctamente.");
    }

    private bool EsAdministradorValido(int adminId) => _session.IsAuthenticated && _session.TipoUser == 1 && _session.IdUser == adminId;
    private async Task<bool> PuedeReducirAdministradoresAsync(int idUser)
    {
        var objetivoEsAdmin = await _sql.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM vt_usuarios WHERE user_numero=@IdUser AND user_tipouser_numero=1 AND user_estado='A'", new { IdUser=idUser });
        if (objetivoEsAdmin == 0) return true;
        var total = await _sql.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM vt_usuarios WHERE user_tipouser_numero=1 AND user_estado='A'");
        return total > 1;
    }
}
