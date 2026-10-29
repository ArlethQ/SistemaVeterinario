using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public class SessionStateService
{
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);
    private DateTimeOffset? _lastActivityUtc;

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

    public void SetSession(UsuarioModel user, int localidad = 1, string localidadDesc = "HOSPITAL DE ESPECIALIDADES VETERINARIAS USFQ")
    {
        IdUser = user.UserNumero;
        TipoUser = user.UserTipoUserNumero;
        EspUser = user.UserEspNumero;
        LoginUser = user.UserLogin;
        NombreUsuario = user.UserNombre;
        Iniciales = string.IsNullOrWhiteSpace(user.UserIniciales) ? "HV" : user.UserIniciales.Trim();
        Localidad = localidad;
        LocalidadDescripcion = localidadDesc;
        Apostrofe = localidad == 2 ? "EQ" : "HV";
        _lastActivityUtc = DateTimeOffset.UtcNow;
        NotifyStateChanged();
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

    public void Logout()
    {
        ClearSession();
        NotifyStateChanged();
    }

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
}

public interface IAuthService
{
    Task<(bool Exito, string Mensaje, UsuarioModel? Usuario, bool RequiereCambioClave)> LoginAsync(string username, string password, int localidad = 1);
    Task<List<MenuItemModel>> ObtenerMenuUsuarioAsync(int idUser, int tipoUser);
    Task<List<RolUsuarioModel>> ObtenerRolesAsync();
    Task<List<UsuarioModel>> ObtenerUsuariosParaPermisosAsync();
    Task<List<MenuItemModel>> ObtenerMenusAdministracionAsync();
    Task<HashSet<int>> ObtenerAccesosUsuarioAsync(int idUser);
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

        LoginAttempts.TryRemove(loginKey, out _);
        await _sql.RegistrarBitacoraAsync($"Ingresa al Sistema: {user.UserNombre}", user.UserNumero);
        _session.SetSession(user, localidad);

        return (true, "Bienvenido", user, false);
    }

    private sealed record LoginAttempt(int Count, DateTimeOffset WindowStartedUtc);

    public async Task<List<MenuItemModel>> ObtenerMenuUsuarioAsync(int idUser, int tipoUser)
    {
        try
        {
            string sql = tipoUser == 1
                ? @"SELECT id AS Id, RTRIM(secNuevoMenu) AS SecNuevoMenu, RTRIM(Descripcion) AS Descripcion, 
                           RTRIM(PadreNuevoMenu) AS PadreNuevoMenu, Habilitado, RTRIM(Url) AS Url
                    FROM vt_menuDinamico
                    WHERE Habilitado = 1
                    ORDER BY (CASE WHEN PadreNuevoMenu IS NULL OR PadreNuevoMenu = '' THEN 0 ELSE 1 END), secNuevoMenu"
                : @"SELECT m.id AS Id, RTRIM(m.secNuevoMenu) AS SecNuevoMenu, RTRIM(m.Descripcion) AS Descripcion, 
                           RTRIM(m.PadreNuevoMenu) AS PadreNuevoMenu, m.Habilitado, RTRIM(m.Url) AS Url
                    FROM vt_menuDinamico m
                    INNER JOIN vt_accesos a ON a.ac_modulo = m.id
                    WHERE m.Habilitado = 1 AND a.ac_idusuario = @IdUser
                    ORDER BY (CASE WHEN m.PadreNuevoMenu IS NULL OR m.PadreNuevoMenu = '' THEN 0 ELSE 1 END), m.secNuevoMenu";

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
                             FROM vt_tipos_usuarios ORDER BY tipuser_descripcion";
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
        const string sql = @"SELECT id AS Id, RTRIM(ISNULL(secNuevoMenu, '')) AS SecNuevoMenu,
                                    RTRIM(ISNULL(Descripcion, '')) AS Descripcion,
                                    RTRIM(ISNULL(PadreNuevoMenu, '')) AS PadreNuevoMenu,
                                    ISNULL(Habilitado, 0) AS Habilitado, RTRIM(ISNULL(Url, '')) AS Url
                             FROM vt_menuDinamico
                             ORDER BY CASE WHEN PadreNuevoMenu IS NULL OR PadreNuevoMenu = '' THEN 0 ELSE 1 END, secNuevoMenu, Descripcion";
        try { return (await _sql.QueryAsync<MenuItemModel>(sql)).ToList(); }
        catch { return []; }
    }

    public async Task<HashSet<int>> ObtenerAccesosUsuarioAsync(int idUser)
    {
        const string sql = "SELECT ac_modulo FROM vt_accesos WHERE ac_idusuario = @IdUser";
        try { return (await _sql.QueryAsync<int>(sql, new { IdUser = idUser })).ToHashSet(); }
        catch { return []; }
    }

    public async Task<bool> GuardarRolYAccesosAsync(int idUser, int tipoUser, IReadOnlyCollection<int> menuIds, int adminId)
    {
        if (!_session.IsAuthenticated || _session.TipoUser != 1 || _session.IdUser != adminId)
            return false;

        var ids = menuIds.Distinct().ToArray();
        var insert = ids.Length == 0
            ? string.Empty
            : "INSERT INTO vt_accesos (ac_idusuario, ac_modulo) SELECT @IdUser, id FROM vt_menuDinamico WHERE id IN @MenuIds;";
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
        var sql = rol.Id > 0
            ? "UPDATE vt_tipos_usuarios SET tipuser_descripcion = @Descripcion WHERE tipuser_numero = @Id"
            : @"INSERT INTO vt_tipos_usuarios (tipuser_numero, tipuser_descripcion)
                VALUES ((SELECT ISNULL(MAX(tipuser_numero), 0) + 1 FROM vt_tipos_usuarios WITH (UPDLOCK, HOLDLOCK)), @Descripcion)";
        var result = await _sql.ExecuteAsync(sql, new { rol.Id, Descripcion = rol.Descripcion.Trim() });
        if (result < 0) return false;
        await _sql.RegistrarBitacoraAsync($"{(rol.Id > 0 ? "Actualiza" : "Crea")} rol: {rol.Descripcion.Trim()}", adminId);
        return true;
    }

    public async Task<bool> GuardarMenuAsync(MenuItemModel menu, int adminId)
    {
        if (!_session.IsAuthenticated || _session.TipoUser != 1 || _session.IdUser != adminId || string.IsNullOrWhiteSpace(menu.Descripcion)) return false;
        var sql = menu.Id > 0
            ? @"UPDATE vt_menuDinamico SET secNuevoMenu = @Seccion, Descripcion = @Descripcion,
                    PadreNuevoMenu = @Padre, Habilitado = @Habilitado, Url = @Url WHERE id = @Id"
            : @"INSERT INTO vt_menuDinamico (id, secNuevoMenu, Descripcion, PadreNuevoMenu, Habilitado, Url)
                VALUES ((SELECT ISNULL(MAX(id), 0) + 1 FROM vt_menuDinamico WITH (UPDLOCK, HOLDLOCK)), @Seccion, @Descripcion, @Padre, @Habilitado, @Url)";
        var result = await _sql.ExecuteAsync(sql, new { menu.Id, Seccion = menu.SecNuevoMenu?.Trim() ?? string.Empty, Descripcion = menu.Descripcion.Trim(), Padre = menu.PadreNuevoMenu?.Trim() ?? string.Empty, menu.Habilitado, Url = menu.Url?.Trim() ?? string.Empty });
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
        var sql = usuario.Id > 0
            ? @"UPDATE vt_usuarios SET user_nombre=@Nombre, user_login=@Login, user_iniciales=@Iniciales,
                    user_email=@Email, user_celular=@Celular, user_convensional=@Convencional, user_cedula=@Cedula,
                    user_tipouser_numero=@RolId, user_esp_numero=@EspecialidadId, user_tipomedico=@TipoMedico,
                    user_observaciones=@Observaciones WHERE user_numero=@Id"
            : @"INSERT INTO vt_usuarios (user_nombre,user_login,user_clave,user_iniciales,user_email,user_celular,user_convensional,user_cedula,user_tipouser_numero,user_esp_numero,user_tipomedico,user_observaciones,user_estado,user_fechacambio)
                VALUES (@Nombre,@Login,@Clave,@Iniciales,@Email,@Celular,@Convencional,@Cedula,@RolId,@EspecialidadId,@TipoMedico,@Observaciones,'A',GETDATE())";
        var args = new { usuario.Id, Nombre=usuario.Nombre.Trim(), Login=usuario.Login.Trim(), Iniciales=usuario.Iniciales.Trim(), Email=usuario.Email.Trim(), Celular=usuario.Celular.Trim(), Convencional=usuario.Convencional.Trim(), Cedula=usuario.Cedula.Trim(), usuario.RolId, usuario.EspecialidadId, TipoMedico=usuario.TipoMedico.Trim(), Observaciones=usuario.Observaciones.Trim(), Clave=_sql.HashPasswordSha1(claveTemporal ?? string.Empty) };
        var result = await _sql.ExecuteAsync(sql, args);
        if (result < 0) return (false, "No se pudo guardar el usuario.");
        var idUser = usuario.Id > 0 ? usuario.Id : await _sql.ExecuteScalarAsync<int>("SELECT user_numero FROM vt_usuarios WHERE user_login=@Login", new { Login=usuario.Login.Trim() });
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
