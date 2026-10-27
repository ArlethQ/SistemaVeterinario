using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public class SessionStateService
{
    public int IdUser { get; private set; } = 1;
    public int TipoUser { get; private set; } = 1;
    public int EspUser { get; private set; } = 1;
    public int Localidad { get; private set; } = 1;
    public string LocalidadDescripcion { get; private set; } = "HOSPITAL DE ESPECIALIDADES VETERINARIAS USFQ";
    public string Apostrofe { get; private set; } = "HV";
    public string LoginUser { get; private set; } = "admin";
    public string NombreUsuario { get; private set; } = "Médico / Enfermería HOSVET";
    public string Iniciales { get; private set; } = "HV";
    public bool IsAuthenticated { get; private set; } = true;
    public int? OrdenActiva { get; private set; }
    public int? NhclActivo { get; private set; }
    public string? PacienteActivo { get; private set; }

    public SessionUserProxy? CurrentUser => new(IdUser, NombreUsuario, Iniciales, Localidad, Apostrofe);
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
        IsAuthenticated = true;
        NotifyStateChanged();
    }

    public void Logout()
    {
        IdUser = 0;
        IsAuthenticated = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

public interface IAuthService
{
    Task<(bool Exito, string Mensaje, UsuarioModel? Usuario, bool RequiereCambioClave)> LoginAsync(string username, string password, int localidad = 1);
    Task<List<MenuItemModel>> ObtenerMenuUsuarioAsync(int idUser, int tipoUser);
    Task<List<RolUsuarioModel>> ObtenerRolesAsync();
    Task<List<UsuarioModel>> ObtenerUsuariosParaPermisosAsync();
    Task<HashSet<int>> ObtenerAccesosUsuarioAsync(int idUser);
    Task<bool> GuardarRolYAccesosAsync(int idUser, int tipoUser, IReadOnlyCollection<int> menuIds, int adminId);
}

public class AuthService : IAuthService
{
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
            return (false, "Usuario y contraseña requeridos.", null, false);

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
            await _sql.RegistrarBitacoraAsync($"Datos introducidos no válidos: {username}", 0);
            return (false, "Los datos introducidos no son válidos.", null, false);
        }

        await _sql.RegistrarBitacoraAsync($"Ingresa al Sistema: {user.UserNombre}", user.UserNumero);
        _session.SetSession(user, localidad);

        return (true, "Bienvenido", user, false);
    }

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
}
