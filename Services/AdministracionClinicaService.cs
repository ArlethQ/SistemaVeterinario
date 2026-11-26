using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IAdministracionClinicaService
{
    IReadOnlyList<TipoCatalogoAdministrativo> ObtenerTiposCatalogo();
    Task<List<CatalogoAdministrativoDto>> ObtenerCatalogoAsync(string tipo);
    Task<(bool Exito, string Mensaje)> GuardarCatalogoAsync(string tipo, CatalogoAdministrativoDto item, int idUsuario);
    Task<(bool Exito, string Mensaje)> EliminarCatalogoAsync(string tipo, int id, int idUsuario);
    Task<List<RecordatorioVacunaDto>> ObtenerRecordatoriosAsync(DateTime desde, DateTime hasta, bool porFechaRevision);
    Task<List<AlertaClinicaDto>> ObtenerAlertasAsync(DateTime desde, DateTime hasta, string estado);
    Task<List<PacienteFallecidoDto>> ObtenerFallecidosAsync(DateTime desde, DateTime hasta);
    Task<List<HistorialPacienteItemDto>> ObtenerResumenHistorialAsync(int nhcl);
    Task<List<OrdenHistorialClinicoResumenDto>> ObtenerHistorialOrdenesPacienteAsync(int nhcl);
    Task<List<ProcedimientoOrdenResumenDto>> ObtenerProcedimientosOrdenAsync(int idOrden, int nhcl);
    Task<List<MedicionCrecimientoDto>> ObtenerCurvaCrecimientoAsync(int nhcl);
    Task<PacienteResumenCrecimientoDto?> ObtenerInfoPacienteCrecimientoAsync(int nhcl);
    Task<List<LocalidadAdministrativaDto>> ObtenerLocalidadesAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerProvinciasAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerCiudadesAsync(int idProvincia);
    Task<(bool Exito, string Mensaje)> GuardarLocalidadAsync(LocalidadAdministrativaDto localidad, int idUsuario);
    Task<List<EspecialidadLocalidadDto>> ObtenerEspecialidadesLocalidadAsync();
    Task<(bool Exito, string Mensaje)> GuardarEspecialidadLocalidadAsync(EspecialidadLocalidadDto relacion, int idUsuario);
    Task<List<EspecialidadAdministrativaDto>> ObtenerEspecialidadesAdminAsync();
    Task<(bool Exito, string Mensaje)> GuardarEspecialidadAsync(EspecialidadAdministrativaDto especialidad, int idUsuario);
    Task<(bool Exito, string Mensaje)> CambiarEstadoEspecialidadAsync(int idEspecialidad, string nuevoEstado, int idUsuario);
    Task<IngresoCirugiaDto> ObtenerIngresoCirugiaAsync(int idOrden, int nhcl);
    Task<bool> GuardarIngresoCirugiaAsync(IngresoCirugiaDto ingreso, int idUsuario);
    Task<List<DespachoClinicoDto>> ObtenerDespachosAsync(DateTime desde, DateTime hasta, int tipo = 0);
    Task<bool> MarcarDespachoProcesadoAsync(int idOrden);
    Task<List<FisioterapiaOrdenDto>> ObtenerCatalogoFisioterapiaAsync();
    Task<List<FisioterapiaOrdenDto>> ObtenerFisioterapiaAsync(int idOrden, int nhcl);
    Task<bool> GuardarFisioterapiaAsync(FisioterapiaOrdenDto orden);
    Task<bool> EliminarFisioterapiaAsync(int sec);
    Task<List<IStatDto>> ObtenerIStatAsync(int nhcl);
    Task<bool> GuardarIStatAsync(IStatDto resultado, int idUsuario);
    Task<bool> EliminarIStatAsync(int id, int idUsuario);
}

public sealed class AdministracionClinicaService : IAdministracionClinicaService
{
    private readonly ISqlDataService _db;
    private readonly SessionStateService _session;

    private int UsuarioFacturacion => 1_000_000 + (_session.Localidad > 0 ? _session.Localidad : 1);

    private sealed record CatalogoSql(
        string Nombre,
        string SelectSql,
        string InsertSql,
        string UpdateSql,
        string UpdateEstadoPredeterminadoSql,
        string ContarUsoSql,
        string DeleteSql,
        bool RequierePadre = false);

    private static readonly IReadOnlyDictionary<string, CatalogoSql> Catalogos =
        new Dictionary<string, CatalogoSql>(StringComparer.OrdinalIgnoreCase)
        {
            ["especies"] = new("Especies",
                "SELECT especie_id AS Id, RTRIM(especie_descripcion) AS Descripcion, ISNULL(especie_estado, 'A') AS Estado FROM vt_especie ORDER BY especie_descripcion",
                "INSERT INTO vt_especie (especie_descripcion, especie_estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_especie SET especie_descripcion=@Descripcion, especie_estado=@Estado WHERE especie_id=@Id",
                "UPDATE vt_especie SET especie_estado=@Estado WHERE especie_id=@Id",
                @"SELECT (SELECT COUNT(1) FROM vt_pacientes WHERE ISNULL(mas_especie,0) = @Id)
                       + (SELECT COUNT(1) FROM vt_razas WHERE id_especie = @Id)
                       + (SELECT COUNT(1) FROM vt_fichamedica WHERE ISNULL(med_especie,0) = @Id)",
                "DELETE FROM vt_especie WHERE especie_id = @Id"),
            ["razas"] = new("Razas",
                "SELECT r.id_raza AS Id, RTRIM(r.des_raza) AS Descripcion, ISNULL(r.estado, 'A') AS Estado, r.id_especie AS IdPadre, RTRIM(e.especie_descripcion) AS PadreDescripcion FROM vt_razas r INNER JOIN vt_especie e ON e.especie_id=r.id_especie ORDER BY r.des_raza",
                @"INSERT INTO vt_razas (des_raza, id_especie, estado) VALUES (@Descripcion, @IdPadre, @Estado);
                  IF (@IdPadre = 3 AND NOT EXISTS (SELECT 1 FROM vt_clase WHERE RTRIM(des_clas) = @Descripcion))
                      INSERT INTO vt_clase (des_clas, estado_clas) VALUES (@Descripcion, @Estado);",
                @"DECLARE @AntiguaRaza VARCHAR(100), @AntiguoPadre INT;
                  SELECT @AntiguaRaza = RTRIM(des_raza), @AntiguoPadre = id_especie FROM vt_razas WHERE id_raza=@Id;
                  UPDATE vt_razas SET des_raza=@Descripcion, id_especie=@IdPadre, estado=@Estado WHERE id_raza=@Id;
                  IF (@IdPadre = 3 OR @AntiguoPadre = 3)
                  BEGIN
                      IF EXISTS (SELECT 1 FROM vt_clase WHERE RTRIM(des_clas) = @AntiguaRaza)
                          UPDATE vt_clase SET des_clas=@Descripcion, estado_clas=@Estado WHERE RTRIM(des_clas) = @AntiguaRaza;
                      ELSE IF (@IdPadre = 3)
                          INSERT INTO vt_clase (des_clas, estado_clas) VALUES (@Descripcion, @Estado);
                  END",
                @"UPDATE vt_razas SET estado=@Estado WHERE id_raza=@Id;
                  IF (@IdPadre = 3)
                      UPDATE vt_clase SET estado_clas=@Estado WHERE RTRIM(des_clas) = @Descripcion;",
                "SELECT COUNT(1) FROM vt_pacientes WHERE ISNULL(mas_raza,0) = @Id",
                @"DECLARE @Desc VARCHAR(100), @Esp INT;
                  SELECT @Desc = RTRIM(des_raza), @Esp = id_especie FROM vt_razas WHERE id_raza = @Id;
                  DELETE FROM vt_razas WHERE id_raza = @Id;
                  IF (@Esp = 3)
                  BEGIN
                      IF NOT EXISTS (SELECT 1 FROM vt_pacientes WHERE ISNULL(tue_clase,0) IN (SELECT id_clas FROM vt_clase WHERE RTRIM(des_clas) = @Desc))
                         AND NOT EXISTS (SELECT 1 FROM vt_razas WHERE id_especie = 3 AND RTRIM(des_raza) = @Desc)
                      BEGIN
                          DELETE FROM vt_clase WHERE RTRIM(des_clas) = @Desc;
                      END
                  END", true),
            ["especialidades"] = new("Especialidades",
                "SELECT es_id AS Id, RTRIM(es_descripcion) AS Descripcion, ISNULL(es_estado, 'A') AS Estado FROM vt_especialidades ORDER BY es_descripcion",
                "INSERT INTO vt_especialidades (es_descripcion, es_estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_especialidades SET es_descripcion=@Descripcion, es_estado=@Estado WHERE es_id=@Id",
                "UPDATE vt_especialidades SET es_estado=@Estado WHERE es_id=@Id",
                @"SELECT (SELECT COUNT(1) FROM vt_usuarios WHERE ISNULL(user_esp_numero,0) = @Id)
                       + (SELECT COUNT(1) FROM vt_usuarioEspecialidades WHERE ue_idEspecialidad = @Id)
                       + (SELECT COUNT(1) FROM vt_citas WHERE ISNULL(ci_especialidad,0) = @Id)
                       + (SELECT COUNT(1) FROM vt_fichamedica WHERE ISNULL(med_especialidad,0) = @Id)",
                "DELETE FROM vt_especialidades WHERE es_id = @Id"),
            ["clases"] = new("Clases de fauna y animales exóticos",
                "SELECT id_clas AS Id, RTRIM(des_clas) AS Descripcion, ISNULL(estado_clas, 'A') AS Estado FROM vt_clase ORDER BY des_clas",
                @"INSERT INTO vt_clase (des_clas, estado_clas) VALUES (@Descripcion, @Estado);
                  IF NOT EXISTS (SELECT 1 FROM vt_razas WHERE id_especie = 3 AND RTRIM(des_raza) = @Descripcion)
                      INSERT INTO vt_razas (des_raza, id_especie, estado) VALUES (@Descripcion, 3, @Estado);",
                @"DECLARE @AntiguaDesc VARCHAR(100);
                  SELECT @AntiguaDesc = RTRIM(des_clas) FROM vt_clase WHERE id_clas=@Id;
                  UPDATE vt_clase SET des_clas=@Descripcion, estado_clas=@Estado WHERE id_clas=@Id;
                  IF EXISTS (SELECT 1 FROM vt_razas WHERE id_especie = 3 AND RTRIM(des_raza) = @AntiguaDesc)
                      UPDATE vt_razas SET des_raza=@Descripcion, estado=@Estado WHERE id_especie = 3 AND RTRIM(des_raza) = @AntiguaDesc;
                  ELSE IF NOT EXISTS (SELECT 1 FROM vt_razas WHERE id_especie = 3 AND RTRIM(des_raza) = @Descripcion)
                      INSERT INTO vt_razas (des_raza, id_especie, estado) VALUES (@Descripcion, 3, @Estado);",
                @"UPDATE vt_clase SET estado_clas=@Estado WHERE id_clas=@Id;
                  UPDATE vt_razas SET estado=@Estado WHERE id_especie = 3 AND RTRIM(des_raza) = @Descripcion;",
                @"SELECT (SELECT COUNT(1) FROM vt_pacientes WHERE ISNULL(tue_clase,0) = @Id)
                       + (SELECT COUNT(1) FROM vt_nombres_cientificos WHERE clas_id = @Id)",
                @"DECLARE @Desc VARCHAR(100);
                  SELECT @Desc = RTRIM(des_clas) FROM vt_clase WHERE id_clas = @Id;
                  DELETE FROM vt_clase WHERE id_clas = @Id;
                  IF NOT EXISTS (SELECT 1 FROM vt_pacientes WHERE ISNULL(mas_raza,0) IN (SELECT id_raza FROM vt_razas WHERE id_especie = 3 AND RTRIM(des_raza) = @Desc))
                  BEGIN
                      DELETE FROM vt_razas WHERE id_especie = 3 AND RTRIM(des_raza) = @Desc;
                  END"),
            ["categorias-ingreso"] = new("Categorías de ingreso",
                "SELECT id AS Id, RTRIM(ingreso) AS Descripcion, ISNULL(estado, 'A') AS Estado FROM vt_ingreso ORDER BY ingreso",
                "INSERT INTO vt_ingreso (ingreso, estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_ingreso SET ingreso=@Descripcion, estado=@Estado WHERE id=@Id",
                "UPDATE vt_ingreso SET estado=@Estado WHERE id=@Id",
                "SELECT COUNT(1) FROM vt_pacientes WHERE ISNULL(tue_categoria,0) = @Id",
                "DELETE FROM vt_ingreso WHERE id = @Id")
        };

    public AdministracionClinicaService(ISqlDataService db, SessionStateService session)
    {
        _db = db;
        _session = session;
    }

    public static bool EsPredeterminado(string tipo, int id) => (tipo ?? string.Empty).ToLowerInvariant() switch
    {
        "especies" => id <= 4,
        "clases" => id <= 5,
        "categorias-ingreso" => id <= 6,
        "especialidades" => id <= 24,
        "razas" => id <= 378,
        _ => false
    };

    public IReadOnlyList<TipoCatalogoAdministrativo> ObtenerTiposCatalogo() => Catalogos
        .Select(x => new TipoCatalogoAdministrativo(x.Key, x.Value.Nombre, x.Value.RequierePadre))
        .ToList();

    public async Task<List<CatalogoAdministrativoDto>> ObtenerCatalogoAsync(string tipo)
    {
        var config = ObtenerConfiguracion(tipo);
        var items = (await _db.QueryAsync<CatalogoAdministrativoDto>(config.SelectSql)).ToList();
        foreach (var item in items)
        {
            item.EsPredeterminado = EsPredeterminado(tipo, item.Id);
        }
        return items;
    }

    public async Task<(bool Exito, string Mensaje)> GuardarCatalogoAsync(string tipo, CatalogoAdministrativoDto item, int idUsuario)
    {
        if (string.IsNullOrWhiteSpace(item.Descripcion))
            return (false, "La descripción es obligatoria.");

        var config = ObtenerConfiguracion(tipo);
        if (config.RequierePadre && !item.IdPadre.HasValue)
            return (false, "Debe seleccionar una especie.");

        item.Descripcion = item.Descripcion.Trim();
        item.Estado = item.Estado == "I" ? "I" : "A";

        bool esPredeterminado = item.Id > 0 && EsPredeterminado(tipo, item.Id);
        string sql = item.Id == 0
            ? config.InsertSql
            : (esPredeterminado ? config.UpdateEstadoPredeterminadoSql : config.UpdateSql);

        try
        {
            await _db.ExecuteAsync(sql, item);
            string accion = item.Id == 0 ? "Creación" : (esPredeterminado ? "Cambio de estado" : "Actualización");
            await _db.RegistrarBitacoraAsync($"{accion} de {config.Nombre}: {item.Descripcion}", idUsuario);
            return (true, "Registro guardado correctamente.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al guardar el registro: {ex.Message}");
        }
    }

    public async Task<(bool Exito, string Mensaje)> EliminarCatalogoAsync(string tipo, int id, int idUsuario)
    {
        if (EsPredeterminado(tipo, id))
        {
            return (false, "Los registros predeterminados del sistema no se pueden eliminar. Puede desactivarlos si no desea utilizarlos.");
        }

        var config = ObtenerConfiguracion(tipo);
        try
        {
            int uso = await _db.ExecuteScalarAsync<int>(config.ContarUsoSql, new { Id = id });
            if (uso > 0)
            {
                return (false, $"No se puede eliminar porque este registro está siendo utilizado por {uso} elemento(s) en el sistema (pacientes, citas u otros registros vinculados). Puede desactivarlo en su lugar.");
            }

            await _db.ExecuteAsync(config.DeleteSql, new { Id = id });
            await _db.RegistrarBitacoraAsync($"Eliminación de {config.Nombre} (ID: {id})", idUsuario);
            return (true, "Registro eliminado correctamente.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar el registro: {ex.Message}");
        }
    }

    public async Task<List<RecordatorioVacunaDto>> ObtenerRecordatoriosAsync(DateTime desde, DateTime hasta, bool porFechaRevision)
    {
        string campoFecha = porFechaRevision ? "v.vacuna_revacunacion" : "v.vacuna_fecha";
        string sql = $@"
            SELECT v.vacuna_nhcl AS Nhcl, RTRIM(p.mas_nombre) AS Paciente,
                RTRIM(ISNULL(c.pa_apellidos, '') + ' ' + ISNULL(c.pa_nombres, '')) AS Propietario,
                RTRIM(u.user_nombre) AS QuienManda,
                RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad,
                RTRIM(ISNULL(c.pa_email, '')) AS CorreoEnviado,
                v.vacuna_fecha AS FechaEnvio, v.vacuna_revacunacion AS ProximaRevision,
                RTRIM(ISNULL(c.pa_telefono, '')) AS Telefono,
                RTRIM(ISNULL(c.pa_movil, '')) AS Movil,
                RTRIM(ISNULL(c.pa_direccion, '')) AS Direccion
            FROM vt_vacunas v
            INNER JOIN vt_usuarios u ON v.vacuna_iduser=u.user_numero
            INNER JOIN vt_pacientes p ON v.vacuna_nhcl=p.mas_nhcl
            INNER JOIN vt_clientes c ON p.mas_idcliente=c.pa_codigo
            LEFT JOIN vt_especialidades e ON e.es_id=u.user_esp_numero
            WHERE ISNULL(v.vacuna_estado, 'A') = 'A'
              AND {campoFecha} >= @Desde AND {campoFecha} < DATEADD(day,1,@Hasta)
            ORDER BY {campoFecha}";
        return (await _db.QueryAsync<RecordatorioVacunaDto>(sql, new { Desde = desde.Date, Hasta = hasta.Date })).ToList();
    }

    public async Task<List<AlertaClinicaDto>> ObtenerAlertasAsync(DateTime desde, DateTime hasta, string estado)
    {
        const string sql = @"
            SELECT f.med_orden AS Orden, f.med_nhcl AS Nhcl, f.med_fecha AS FechaEnvio,
                RTRIM(ISNULL(u.user_nombre,'')) AS Medico,
                RTRIM(c.pa_apellidos + ' ' + c.pa_nombres) AS Cliente,
                RTRIM(p.mas_nombre) AS Paciente, RTRIM(ISNULL(e.es_descripcion,'')) AS Especialidad,
                RTRIM(ISNULL(f.pronosticotueri,'')) AS Pronostico,
                RTRIM(ISNULL(f.estadoparte,'')) AS Estado,
                RTRIM(ISNULL(c.pa_telefono,'')) AS Telefono,
                RTRIM(ISNULL(c.pa_movil,'')) AS Movil,
                RTRIM(ISNULL(c.pa_email,'')) AS Email,
                ISNULL(f.med_edadanios,0) AS Anios,
                RTRIM(ISNULL(f.med_especie,'')) AS Especie
            FROM vt_fichamedica f
            INNER JOIN vt_pacientes p ON f.med_nhcl=p.mas_nhcl
            INNER JOIN vt_clientes c ON f.med_cliente=c.pa_codigo
            LEFT JOIN vt_usuarios u ON u.user_numero=f.med_iduser
            LEFT JOIN vt_especialidades e ON e.es_id=f.med_especialidad
            WHERE f.med_pregunta='S' AND f.med_fecha>=@Desde AND f.med_fecha<DATEADD(day,1,@Hasta)
                AND (@Estado='' OR f.estadoparte=@Estado)
            ORDER BY f.med_fecha DESC";
        return (await _db.QueryAsync<AlertaClinicaDto>(sql, new { Desde = desde.Date, Hasta = hasta.Date, Estado = estado })).ToList();
    }

    public async Task<List<PacienteFallecidoDto>> ObtenerFallecidosAsync(DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT p.fechafallecido AS Fecha, p.mas_nhcl AS Nhcl, RTRIM(p.mas_nombre) AS Paciente,
                LTRIM(RTRIM(CONCAT(ISNULL(c.pa_apellidos,''), ' ', ISNULL(c.pa_nombres,'')))) AS Propietario,
                RTRIM(ISNULL(m.motivo,'')) AS Motivo
            FROM vt_pacientes p
            LEFT JOIN vt_clientes c ON p.mas_idcliente=c.pa_codigo
            LEFT JOIN vt_motivomuerte m ON p.motivofallece=m.sec
            WHERE CONVERT(VARCHAR(10), ISNULL(p.fallecido, 0)) IN ('1','True','TRUE')
                AND p.fechafallecido>=@Desde AND p.fechafallecido<DATEADD(day,1,@Hasta)
            ORDER BY p.fechafallecido DESC";
        return (await _db.QueryAsync<PacienteFallecidoDto>(sql, new { Desde = desde.Date, Hasta = hasta.Date })).ToList();
    }

    public async Task<List<HistorialPacienteItemDto>> ObtenerResumenHistorialAsync(int nhcl)
    {
        var resultado = new List<HistorialPacienteItemDto>();
        var consultas = new[]
        {
            ("Diagnósticos", @"SELECT CAST(c.cie_codigo AS varchar(30)) AS Codigo, RTRIM(c.cie_diagnostico) AS Descripcion, ISNULL(o.ordenAten_fecha, GETDATE()) AS Fecha, ISNULL(d.dig_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_diagnosticos d INNER JOIN vt_cie10 c ON d.dig_codigoDiag=c.cie_codigo LEFT JOIN vt_ordenAtencion o ON d.dig_id_orden=o.ordenAten_id WHERE d.dig_nhcl=@Nhcl"),
            ("Medicamentos", @"SELECT CASE WHEN p.CODIGO IS NOT NULL THEN CAST(p.CODIGO AS varchar(30)) ELSE '' END AS Codigo, COALESCE(NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(p.NOMBRE COLLATE DATABASE_DEFAULT, '')), CHAR(13), ''), CHAR(10), ''), ''), RTRIM(ISNULL(m.med_otroMedicamento, ''))) AS Descripcion, ISNULL(m.med_fecha, GETDATE()) AS Fecha, ISNULL(m.med_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_medicinas m LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.med_idProdu=p.CODIGO WHERE m.med_nhcl=@Nhcl AND m.med_tipo='M' AND (p.CODIGO IS NOT NULL OR NULLIF(RTRIM(ISNULL(m.med_otroMedicamento, '')), '') IS NOT NULL)"),
            ("Insumos", @"SELECT CASE WHEN p.CODIGO IS NOT NULL THEN CAST(p.CODIGO AS varchar(30)) ELSE '' END AS Codigo, COALESCE(NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(p.NOMBRE COLLATE DATABASE_DEFAULT, '')), CHAR(13), ''), CHAR(10), ''), ''), RTRIM(ISNULL(m.med_otroMedicamento, ''))) AS Descripcion, ISNULL(m.med_fecha, GETDATE()) AS Fecha, ISNULL(m.med_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_medicinas m LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.med_idProdu=p.CODIGO WHERE m.med_nhcl=@Nhcl AND m.med_tipo='I' AND (p.CODIGO IS NOT NULL OR NULLIF(RTRIM(ISNULL(m.med_otroMedicamento, '')), '') IS NOT NULL)"),
            ("Fisioterapia", @"SELECT CAST(p.CODIGO AS varchar(30)) AS Codigo, RTRIM(p.NOMBRE) AS Descripcion, ISNULL(m.fisioO_fecha, GETDATE()) AS Fecha, ISNULL(m.fisioO_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_fisioterapia_ordenes m INNER JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.fisioO_idProdu=p.CODIGO WHERE m.fisioO_nhcl=@Nhcl"),
            ("Imagenología", @"SELECT CAST(p.CODIGO AS varchar(30)) AS Codigo, RTRIM(p.NOMBRE) AS Descripcion, ISNULL(m.rxO_fecha, GETDATE()) AS Fecha, ISNULL(m.rxO_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_rxOrdenes m INNER JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.rxO_idProdu=p.CODIGO WHERE m.rxO_nhcl=@Nhcl"),
            ("Procedimientos", @"SELECT CAST(p.CODIGO AS varchar(30)) AS Codigo, REPLACE(REPLACE(RTRIM(ISNULL(p.NOMBRE, '')), CHAR(13), ''), CHAR(10), '') AS Descripcion, ISNULL(m.proc_fecha, GETDATE()) AS Fecha, ISNULL(m.proc_idOrden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_procedimientos m INNER JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.proc_idProdu=p.CODIGO WHERE m.proc_nhcl=@Nhcl"),
            ("Laboratorio", @"SELECT CAST(p.CODIGO AS varchar(30)) AS Codigo, RTRIM(p.NOMBRE) AS Descripcion, ISNULL(m.labO_fecha, GETDATE()) AS Fecha, ISNULL(m.labO_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_labOrdenes m INNER JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.labO_idProdu=p.CODIGO WHERE m.labO_nhcl=@Nhcl")
        };

        foreach (var (categoria, sql) in consultas)
        {
            try
            {
                var items = await _db.QueryAsync<HistorialPacienteItemDto>(sql, new { Nhcl = nhcl });
                foreach (var item in items) item.Categoria = categoria;
                resultado.AddRange(items);
            }
            catch
            {
                // Algunas instalaciones no tienen todos los módulos habilitados.
            }
        }
        return resultado;
    }

    private sealed class TablaColumnaRow
    {
        public string Tabla { get; set; } = string.Empty;
        public string Columna { get; set; } = string.Empty;
    }

    public async Task<List<OrdenHistorialClinicoResumenDto>> ObtenerHistorialOrdenesPacienteAsync(int nhcl)
    {
        if (nhcl <= 0) return [];

        const string sqlMeta = @"
            SELECT o.name AS Tabla, c.name AS Columna
            FROM sys.objects o
            INNER JOIN sys.columns c ON c.object_id = o.object_id
            WHERE o.type = 'U'
              AND o.name IN (
                  'vt_ordenAtencion', 'vt_fichamedica', 'vt_diagnosticos', 'vt_EvolucionMedico', 'vt_medicinas', 'vt_vacunas',
                  'vt_labOrdenes', 'vt_rxOrdenes', 'vt_fisioterapia_ordenes', 'vt_procedimientos',
                  'vt_labInformes', 'vt_rxInformes', 'vt_ecoInformes', 'vt_endoInformes', 'vt_medInformes',
                  'hvt_ingresoHospitalizacion', 'hvt_bloque1ficha', 'hvt_bloque2ficha', 'hvt_bloque3ficha', 'hvt_bloqueMonitoreo',
                  'qvt_enfSignosVitales', 'qvt_productosUtilizados',
                  'qvt_calendario_cirugias', 'qvt_preanestesia', 'qvt_verificacion', 'qvt_RegistroAnestesia',
                  'qvt_monitorizacion_anestesia', 'qvt_Protocolo', 'qvt_recuperacion', 'qvt_post_operatorio',
                  'qvt_observaciones_recuperacion', 'vt_cirugia_ordenes', 'vt_autorizaciones', 'qvt_formulario_alta'
              )";

        var metaRows = await _db.QueryAsync<TablaColumnaRow>(sqlMeta);
        var tablas = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in metaRows)
        {
            if (!tablas.TryGetValue(r.Tabla, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                tablas[r.Tabla] = set;
            }
            set.Add(r.Columna);
        }

        bool Tiene(string tabla, string col) =>
            tablas.TryGetValue(tabla, out var cols) && cols.Contains(col);

        string ExistsClause(string tabla, string col, string extraWhere = "") =>
            Tiene(tabla, col)
                ? $"EXISTS (SELECT 1 FROM dbo.{tabla} t WHERE t.{col} = o.ordenAten_id{extraWhere})"
                : string.Empty;

        string CombinarFlag(params string[] clausulas)
        {
            var validas = clausulas.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            return validas.Count == 0
                ? "CAST(0 AS bit)"
                : $"CASE WHEN {string.Join(" OR ", validas)} THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END";
        }

        string horaExpr = Tiene("vt_ordenAtencion", "ordenAten_hora")
            ? "RTRIM(ISNULL(CONVERT(nvarchar(20), o.ordenAten_hora), ''))"
            : "''";
        string etapaExpr = Tiene("vt_ordenAtencion", "ordenAten_etapa")
            ? "RTRIM(ISNULL(CONVERT(nvarchar(60), o.ordenAten_etapa), ''))"
            : "''";
        string esProcExpr = Tiene("vt_ordenAtencion", "ordenAten_esProcedimiento")
            ? "CAST(CASE WHEN ISNULL(o.ordenAten_esProcedimiento, 0) = 1 THEN 1 ELSE 0 END AS bit)"
            : "CAST(0 AS bit)";

        string motivoExpr = Tiene("vt_fichamedica", "med_orden") && Tiene("vt_fichamedica", "med_motivo")
            ? "ISNULL((SELECT TOP 1 RTRIM(CONVERT(nvarchar(500), f.med_motivo)) FROM dbo.vt_fichamedica f WHERE f.med_orden = o.ordenAten_id ORDER BY f.sec DESC), '')"
            : "''";

        string diagExpr = Tiene("vt_diagnosticos", "dig_id_orden")
            ? @"ISNULL(STUFF((
                    SELECT ', ' + RTRIM(c.cie_diagnostico)
                    FROM dbo.vt_diagnosticos d
                    INNER JOIN dbo.vt_cie10 c ON d.dig_codigoDiag = c.cie_codigo
                    WHERE d.dig_id_orden = o.ordenAten_id
                    FOR XML PATH(''), TYPE
                ).value('.', 'nvarchar(max)'), 1, 2, ''), '')"
            : "''";

        bool tieneFicha = Tiene("vt_fichamedica", "med_orden");
        bool puedeDiferenciarTriaje = tieneFicha && Tiene("vt_fichamedica", "med_motivo");
        string condSoloTriaje = puedeDiferenciarTriaje
            ? @"LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.med_motivo, '')))) = 'Triaje y signos vitales'"
              + (Tiene("vt_fichamedica", "med_anamnesis") ? " AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.med_anamnesis, '')))), '') IS NULL" : "")
              + (Tiene("vt_fichamedica", "med_objetivo") ? " AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.med_objetivo, '')))), '') IS NULL" : "")
              + (Tiene("vt_fichamedica", "med_analisis") ? " AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.med_analisis, '')))), '') IS NULL" : "")
              + (Tiene("vt_fichamedica", "med_plan") ? " AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.med_plan, '')))), '') IS NULL" : "")
              + (Tiene("vt_fichamedica", "tratamiento") ? " AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.tratamiento, '')))), '') IS NULL" : "")
              + (Tiene("vt_fichamedica", "observacionesyrecomendaciones") ? " AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.observacionesyrecomendaciones, '')))), '') IS NULL" : "")
              + (Tiene("vt_fichamedica", "med_apuntes") ? " AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), ISNULL(t.med_apuntes, '')))), '') IS NULL" : "")
              + (Tiene("vt_fichamedica", "med_pregunta") ? " AND ISNULL(t.med_pregunta, 'N') <> 'S'" : "")
              + (Tiene("vt_fichamedica", "med_requiereHospitalizacion") ? " AND ISNULL(t.med_requiereHospitalizacion, 0) = 0" : "")
              + (Tiene("vt_diagnosticos", "dig_id_orden") ? " AND NOT EXISTS (SELECT 1 FROM dbo.vt_diagnosticos d WHERE d.dig_id_orden = o.ordenAten_id)" : "")
            : "1 = 0";

        string flagVacunas = CombinarFlag(
            ExistsClause("vt_vacunas", "vacuna_orden",
                Tiene("vt_vacunas", "vacuna_estado") ? " AND ISNULL(t.vacuna_estado, 'A') = 'A'" : ""));

        string flagEvol = CombinarFlag(
            ExistsClause("vt_EvolucionMedico", "idOrden",
                Tiene("vt_EvolucionMedico", "estadoEvolucion") ? " AND ISNULL(t.estadoEvolucion, 'A') = 'A'" : ""));

        string flagReceta = CombinarFlag(ExistsClause("vt_medicinas", "med_id_orden"));

        string flagDiagComp = CombinarFlag(
            ExistsClause("vt_labOrdenes", "labO_id_orden"),
            ExistsClause("vt_rxOrdenes", "rxO_id_orden"),
            ExistsClause("vt_fisioterapia_ordenes", "fisioO_id_orden",
                Tiene("vt_fisioterapia_ordenes", "fisioO_estado") ? " AND ISNULL(t.fisioO_estado, 'A') = 'A'" : ""),
            ExistsClause("vt_procedimientos", "proc_idOrden",
                Tiene("vt_procedimientos", "proc_estado") ? " AND ISNULL(CONVERT(nvarchar(30), t.proc_estado), 'A') <> 'I'" : ""),
            ExistsClause("vt_labInformes", "blazor_id_orden",
                Tiene("vt_labInformes", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : ""),
            ExistsClause("vt_rxInformes", "blazor_id_orden",
                Tiene("vt_rxInformes", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : ""),
            ExistsClause("vt_ecoInformes", "blazor_id_orden",
                Tiene("vt_ecoInformes", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : ""),
            ExistsClause("vt_endoInformes", "blazor_id_orden",
                Tiene("vt_endoInformes", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : ""),
            ExistsClause("vt_medInformes", "blazor_id_orden",
                Tiene("vt_medInformes", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : "")
        );

        var condicionesIngHosp = new List<string>();
        if (Tiene("hvt_ingresoHospitalizacion", "reqHospitalizacionOverride"))
            condicionesIngHosp.Add("ISNULL(t.reqHospitalizacionOverride, 0) = 1");
        if (Tiene("vt_fichamedica", "med_requiereHospitalizacion"))
            condicionesIngHosp.Add("EXISTS (SELECT 1 FROM dbo.vt_fichamedica fm WHERE fm.med_orden = o.ordenAten_id AND ISNULL(fm.med_requiereHospitalizacion, 0) = 1)");
        if (Tiene("hvt_ingresoHospitalizacion", "indicaciones"))
            condicionesIngHosp.Add("NULLIF(LTRIM(RTRIM(ISNULL(t.indicaciones, ''))), '') IS NOT NULL");
        if (Tiene("hvt_ingresoHospitalizacion", "hidratacionFluidoterapia"))
            condicionesIngHosp.Add("NULLIF(LTRIM(RTRIM(ISNULL(t.hidratacionFluidoterapia, ''))), '') IS NOT NULL");
        if (Tiene("hvt_ingresoHospitalizacion", "dietaCuidados"))
            condicionesIngHosp.Add("NULLIF(LTRIM(RTRIM(ISNULL(t.dietaCuidados, ''))), '') IS NOT NULL");

        string extraIngHosp = (Tiene("hvt_ingresoHospitalizacion", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : "")
            + (condicionesIngHosp.Count > 0 ? $" AND ({string.Join(" OR ", condicionesIngHosp)})" : "");

        string flagHosp = CombinarFlag(
            ExistsClause("hvt_ingresoHospitalizacion", "idOrden", extraIngHosp),
            ExistsClause("hvt_bloque1ficha", "idorden"),
            ExistsClause("hvt_bloque2ficha", "idorden"),
            ExistsClause("hvt_bloque3ficha", "idorden"),
            ExistsClause("hvt_bloqueMonitoreo", "idorden"),
            ExistsClause("qvt_enfSignosVitales", "idOrdenAtencion"),
            ExistsClause("qvt_productosUtilizados", "ordenId",
                Tiene("qvt_productosUtilizados", "estado") ? " AND ISNULL(t.estado, 'Vigente') IN ('Vigente', 'A')" : "")
        );

        // Las hospitalizaciones con ficha médica siempre muestran SOAP completo
        string flagSoap = tieneFicha
            ? $"CASE WHEN EXISTS (SELECT 1 FROM dbo.vt_fichamedica t WHERE t.med_orden = o.ordenAten_id AND (NOT ({condSoloTriaje}) OR ({flagHosp}) = 1)) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END"
            : "CAST(0 AS bit)";

        string flagTriaje = puedeDiferenciarTriaje
            ? $"CASE WHEN EXISTS (SELECT 1 FROM dbo.vt_fichamedica t WHERE t.med_orden = o.ordenAten_id AND ({condSoloTriaje})) AND ({flagHosp}) = 0 AND NOT EXISTS (SELECT 1 FROM dbo.vt_fichamedica t WHERE t.med_orden = o.ordenAten_id AND NOT ({condSoloTriaje})) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END"
            : "CAST(0 AS bit)";

        string flagCirugia = CombinarFlag(
            ExistsClause("qvt_calendario_cirugias", "id_orden",
                Tiene("qvt_calendario_cirugias", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : ""),
            ExistsClause("vt_cirugia_ordenes", "cirO_id_orden",
                Tiene("vt_cirugia_ordenes", "cirO_estado") ? " AND ISNULL(t.cirO_estado, 'PENDIENTE_CITA') <> 'ANULADA'" : ""),
            ExistsClause("qvt_preanestesia", "orden"),
            ExistsClause("qvt_verificacion", "fichamedr"),
            ExistsClause("qvt_RegistroAnestesia", "idOrden"),
            ExistsClause("qvt_monitorizacion_anestesia", "id_orden"),
            ExistsClause("qvt_Protocolo", "idOrden"),
            ExistsClause("qvt_recuperacion", "idOrden"),
            ExistsClause("qvt_post_operatorio", "idOrden"),
            ExistsClause("qvt_observaciones_recuperacion", "idOrden",
                Tiene("qvt_observaciones_recuperacion", "estado") ? " AND ISNULL(t.estado, 'A') = 'A'" : "")
        );

        string flagConsent = CombinarFlag(
            ExistsClause("vt_autorizaciones", "orden"),
            ExistsClause("qvt_formulario_alta", "idOrden")
        );

        string origenCirExpr = Tiene("vt_cirugia_ordenes", "cirO_id_orden") && Tiene("vt_cirugia_ordenes", "cirO_nhcl")
            ? $@"CASE WHEN ({flagCirugia}) = 1 AND ({flagSoap}) = 0 THEN (
                    SELECT TOP 1 co.cirO_id_orden
                    FROM dbo.vt_cirugia_ordenes co
                    WHERE co.cirO_nhcl = o.ordenAten_nhcl
                      AND co.cirO_id_orden < o.ordenAten_id
                      {(Tiene("vt_cirugia_ordenes", "cirO_estado") ? "AND ISNULL(co.cirO_estado, 'PENDIENTE_CITA') <> 'ANULADA'" : "")}
                    ORDER BY co.cirO_id_orden DESC
                ) ELSE NULL END"
            : "CAST(NULL AS int)";

        string sql = $@"
            SELECT
                o.ordenAten_id AS IdOrden,
                o.ordenAten_nhcl AS Nhcl,
                ISNULL(o.ordenAten_fecha, GETDATE()) AS Fecha,
                {horaExpr} AS Hora,
                RTRIM(ISNULL(CONVERT(nvarchar(30), o.ordenAten_estado), '')) AS Estado,
                {etapaExpr} AS Etapa,
                {esProcExpr} AS EsProcedimiento,
                RTRIM(ISNULL(u.user_nombre, 'Sin médico asignado')) AS Medico,
                RTRIM(ISNULL(e.es_descripcion, 'Consulta General')) AS Especialidad,
                {motivoExpr} AS Motivo,
                {diagExpr} AS Diagnosticos,
                {flagSoap} AS TieneSoap,
                {flagTriaje} AS TieneTriaje,
                {flagVacunas} AS TieneVacunas,
                {flagEvol} AS TieneEvoluciones,
                {flagReceta} AS TieneReceta,
                {flagDiagComp} AS TieneDiagnosticosComplementarios,
                {flagHosp} AS TieneHospitalizacion,
                {flagCirugia} AS TieneCirugia,
                {flagConsent} AS TieneConsentimientos,
                {origenCirExpr} AS IdOrdenOrigenCirugia
            FROM dbo.vt_ordenAtencion o
            LEFT JOIN dbo.vt_usuarios u ON u.user_numero = o.ordenAten_idMedico
            LEFT JOIN dbo.vt_especialidades e ON e.es_id = o.ordenAten_esp_numero
            WHERE o.ordenAten_nhcl = @Nhcl
            ORDER BY o.ordenAten_fecha DESC, o.ordenAten_id DESC";

        return (await _db.QueryAsync<OrdenHistorialClinicoResumenDto>(sql, new { Nhcl = nhcl })).ToList();
    }

    public async Task<List<ProcedimientoOrdenResumenDto>> ObtenerProcedimientosOrdenAsync(int idOrden, int nhcl)
    {
        if (idOrden <= 0 || nhcl <= 0) return [];
        try
        {
            var cols = new HashSet<string>(
                await _db.QueryAsync<string>(@"
                    SELECT c.name
                    FROM sys.columns c
                    INNER JOIN sys.objects o ON o.object_id = c.object_id
                    WHERE o.type = 'U' AND o.name = 'vt_procedimientos'"),
                StringComparer.OrdinalIgnoreCase);

            if (!cols.Contains("proc_idOrden")) return [];

            string cantCol = cols.Contains("proc_cantidad") ? "CAST(ISNULL(NULLIF(p.proc_cantidad, 0), 1) AS decimal(18,2))" : "CAST(1 AS decimal(18,2))";
            string obsCol = cols.Contains("proc_observaciones") ? "ISNULL(CONVERT(nvarchar(250), p.proc_observaciones), '')" : "''";
            string estCol = cols.Contains("proc_estado") ? "ISNULL(CONVERT(nvarchar(30), p.proc_estado), '')" : "''";
            string descCol = cols.Contains("proc_descripcion")
                ? "COALESCE(NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(pr.NOMBRE COLLATE DATABASE_DEFAULT, '')), CHAR(13), ''), CHAR(10), ''), ''), NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(250), p.proc_descripcion))), ''), CONCAT('Procedimiento #', p.proc_idProdu))"
                : "COALESCE(NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(pr.NOMBRE COLLATE DATABASE_DEFAULT, '')), CHAR(13), ''), CHAR(10), ''), ''), CONCAT('Procedimiento #', p.proc_idProdu))";
            string filtroEst = cols.Contains("proc_estado") ? "AND ISNULL(CONVERT(nvarchar(30), p.proc_estado), 'A') <> 'I'" : "";

            string sql = $@"
                SELECT
                    ISNULL(p.proc_idProdu, 0) AS IdProducto,
                    RTRIM(ISNULL(CONVERT(nvarchar(50), pr.CODIGO_PRINCIPAL), CAST(p.proc_idProdu AS nvarchar(50)))) AS Codigo,
                    {descCol} AS Descripcion,
                    {cantCol} AS Cantidad,
                    {obsCol} AS Observaciones,
                    {estCol} AS Estado,
                    ISNULL(p.proc_fecha, GETDATE()) AS Fecha
                FROM dbo.vt_procedimientos p
                LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO pr ON pr.CODIGO = p.proc_idProdu
                WHERE p.proc_idOrden = @IdOrden AND p.proc_nhcl = @Nhcl {filtroEst}
                ORDER BY p.proc_fecha DESC, p.proc_idProdu";

            return (await _db.QueryAsync<ProcedimientoOrdenResumenDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl })).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<MedicionCrecimientoDto>> ObtenerCurvaCrecimientoAsync(int nhcl)
    {
        const string sql = @"
            SELECT ISNULL(f.med_orden, 0) AS IdOrden,
                f.med_fecha AS Fecha,
                TRY_CAST(f.peso AS decimal(18,2)) AS Peso,
                TRY_CAST(f.frecuenciacardiaca AS decimal(18,2)) AS FrecuenciaCardiaca,
                TRY_CAST(f.frecuenciarespiratoria AS decimal(18,2)) AS FrecuenciaRespiratoria,
                DATEDIFF(month,p.mas_fechanace,f.med_fecha) AS EdadMeses
            FROM vt_fichaMedica f INNER JOIN vt_pacientes p ON f.med_nhcl=p.mas_nhcl
            WHERE f.med_nhcl=@Nhcl ORDER BY f.med_fecha";
        return (await _db.QueryAsync<MedicionCrecimientoDto>(sql, new { Nhcl = nhcl })).ToList();
    }

    public async Task<PacienteResumenCrecimientoDto?> ObtenerInfoPacienteCrecimientoAsync(int nhcl)
    {
        const string sql = @"
            SELECT 
                p.mas_nhcl AS Nhcl,
                RTRIM(ISNULL(p.mas_nombre, '')) AS Nombre,
                RTRIM(ISNULL(e.especie_descripcion, '')) AS Especie,
                RTRIM(ISNULL(r.des_raza, '')) AS Raza,
                CASE WHEN RTRIM(p.mas_sexo) = 'H' THEN 'Hembra' WHEN RTRIM(p.mas_sexo) = 'M' THEN 'Macho' ELSE 'Indeterminado' END AS Sexo,
                p.mas_fechanace AS FechaNacimiento,
                RTRIM(ISNULL(c.pa_nombres, '') + ' ' + ISNULL(c.pa_apellidos, '')) AS Cliente
            FROM vt_pacientes p
            LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
            LEFT JOIN vt_razas r ON p.mas_raza = r.id_raza
            LEFT JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            WHERE p.mas_nhcl = @Nhcl";
        return await _db.QueryFirstOrDefaultAsync<PacienteResumenCrecimientoDto>(sql, new { Nhcl = nhcl });
    }

    public async Task<List<LocalidadAdministrativaDto>> ObtenerLocalidadesAsync()
    {
        const string sql = @"SELECT l.loc_id AS Id, RTRIM(l.loc_descripcion) AS Localidad,
            RTRIM(ISNULL(l.loc_direccion,'')) AS Direccion, RTRIM(ISNULL(l.loc_telefono,'')) AS Telefono,
            RTRIM(ISNULL(l.loc_ruc,'')) AS Ruc, RTRIM(ISNULL(l.loc_administrador,'')) AS Administrador,
            l.loc_estado AS Estado, RTRIM(ISNULL(l.loc_nom_empresa,'')) AS Empresa,
            RTRIM(ISNULL(l.loc_prefijo,'')) AS Prefijo, RTRIM(ISNULL(l.loc_region,'')) AS Region,
            ISNULL(l.idCiudad,0) AS IdCiudad, RTRIM(ISNULL(c.descripcion,'')) AS Ciudad,
            ISNULL(l.prov_id,0) AS IdProvincia, RTRIM(ISNULL(p.prov_descripcion,'')) AS Provincia
            FROM vt_localidades l LEFT JOIN vt_provincias p ON l.prov_id=p.prov_id
            LEFT JOIN vt_ciudad c ON l.idCiudad=c.id_Ciudad ORDER BY l.loc_descripcion";
        return (await _db.QueryAsync<LocalidadAdministrativaDto>(sql)).ToList();
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerProvinciasAsync() =>
        (await _db.QueryAsync<CatalogoItemGenericoDto>("SELECT prov_id AS Id, RTRIM(prov_descripcion) AS Descripcion FROM vt_provincias ORDER BY prov_descripcion")).ToList();

    public async Task<List<CatalogoItemGenericoDto>> ObtenerCiudadesAsync(int idProvincia) =>
        (await _db.QueryAsync<CatalogoItemGenericoDto>("SELECT id_Ciudad AS Id, RTRIM(descripcion) AS Descripcion FROM vt_ciudad WHERE id_provincia=@IdProvincia ORDER BY descripcion", new { IdProvincia = idProvincia })).ToList();

    public async Task<(bool Exito, string Mensaje)> GuardarLocalidadAsync(LocalidadAdministrativaDto x, int idUsuario)
    {
        if (string.IsNullOrWhiteSpace(x.Localidad) || string.IsNullOrWhiteSpace(x.Direccion) || x.IdProvincia <= 0 || x.IdCiudad <= 0)
            return (false, "Localidad, dirección, provincia y ciudad son obligatorias.");
        const string insertar = @"INSERT INTO vt_localidades (loc_descripcion,loc_direccion,loc_telefono,loc_ruc,loc_administrador,loc_estado,loc_nom_empresa,loc_prefijo,loc_region,idCiudad,prov_id) VALUES (@Localidad,@Direccion,@Telefono,@Ruc,@Administrador,@Estado,@Empresa,@Prefijo,@Region,@IdCiudad,@IdProvincia); SELECT CONVERT(INT, SCOPE_IDENTITY());";
        const string actualizar = @"UPDATE vt_localidades SET loc_descripcion=@Localidad,loc_direccion=@Direccion,loc_telefono=@Telefono,loc_ruc=@Ruc,loc_administrador=@Administrador,loc_estado=@Estado,loc_nom_empresa=@Empresa,loc_prefijo=@Prefijo,loc_region=@Region,idCiudad=@IdCiudad,prov_id=@IdProvincia WHERE loc_id=@Id";
        var esNueva = x.Id <= 0;
        if (esNueva)
            x.Id = await _db.ExecuteScalarAsync<int>(insertar, x);
        else if (await _db.ExecuteAsync(actualizar, x) <= 0)
            return (false, "No se pudo guardar la localidad.");

        if (x.Id <= 0) return (false, "No se pudo obtener el identificador de la veterinaria.");

        // Cada localidad es un tenant fiscal independiente. El usuario técnico
        // nunca inicia sesión: Numerica lo usa para aislar productos, clientes,
        // documentos, caja y contabilidad de cada veterinaria.
        const string prepararTenant = """
            SET XACT_ABORT ON;
            DECLARE @IdFiscal INT = 1000000 + @Id;

            IF @EsNueva = 1 AND NOT EXISTS
                (SELECT 1 FROM dbo.vt_usuarioLocalidades WHERE ul_idUsuario=@IdUsuario AND ul_idLocalidad=@Id)
                INSERT INTO dbo.vt_usuarioLocalidades (ul_idUsuario,ul_idLocalidad,ul_estado)
                VALUES (@IdUsuario,@Id,'A');

            IF OBJECT_ID('dbo.Usuarios','U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdFiscal)
                BEGIN
                    SET IDENTITY_INSERT dbo.Usuarios ON;
                    BEGIN TRY
                        INSERT INTO dbo.Usuarios
                            (IdUsuario,Nombres,Apellidos,NombreEmpresa,Email,PasswordHash,
                             CuentaBloqueada,FechaCreacion,Estado,ClaveTemporal,Identificacion,SaldoDocumentos)
                        VALUES
                            (@IdFiscal,@Localidad,N'',COALESCE(NULLIF(@Empresa,N''),@Localidad),
                             CONCAT('facturacion-localidad-',@Id,'@local.invalid'),
                             CONVERT(NVARCHAR(128),HASHBYTES('SHA2_512',CONVERT(NVARCHAR(36),NEWID())),2),
                             1,GETDATE(),CASE WHEN @Estado='A' THEN 1 ELSE 0 END,1,NULLIF(@Ruc,N''),0);
                        SET IDENTITY_INSERT dbo.Usuarios OFF;
                    END TRY
                    BEGIN CATCH
                        SET IDENTITY_INSERT dbo.Usuarios OFF;
                        THROW;
                    END CATCH;
                END
                ELSE
                    UPDATE dbo.Usuarios
                    SET Nombres=@Localidad,
                        NombreEmpresa=COALESCE(NULLIF(@Empresa,N''),@Localidad),
                        Identificacion=NULLIF(@Ruc,N''),
                        Estado=CASE WHEN @Estado='A' THEN 1 ELSE 0 END
                    WHERE IdUsuario=@IdFiscal;
            END;

            IF OBJECT_ID('dbo.VeterinariaFacturacionTenant','U') IS NOT NULL
            BEGIN
                UPDATE dbo.VeterinariaFacturacionTenant
                SET IdUsuarioFacturacion=@IdFiscal,
                    Estado=CASE WHEN @Estado='A' THEN 1 ELSE 0 END
                WHERE IdLocalidad=@Id;
                IF @@ROWCOUNT=0
                    INSERT INTO dbo.VeterinariaFacturacionTenant(IdLocalidad,IdUsuarioFacturacion,Estado)
                    VALUES(@Id,@IdFiscal,CASE WHEN @Estado='A' THEN 1 ELSE 0 END);
            END;
            """;
        await _db.ExecuteAsync(prepararTenant, new
        {
            x.Id,
            IdUsuario = idUsuario,
            EsNueva = esNueva,
            x.Localidad,
            x.Empresa,
            x.Ruc,
            x.Estado
        });
        await _db.RegistrarBitacoraAsync($"{(esNueva ? "Creación" : "Actualización")} de localidad: {x.Localidad}", idUsuario);
        return (true, esNueva
            ? "Veterinaria creada y asignada a su administrador. Ya puede seleccionarla y configurar emisor, firma y caja."
            : "Veterinaria actualizada correctamente.");
    }

    public async Task<List<EspecialidadLocalidadDto>> ObtenerEspecialidadesLocalidadAsync()
    {
        const string sql = @"SELECT r.id AS Id,r.esp_id AS IdEspecialidad,RTRIM(e.es_descripcion) AS Especialidad,r.id_localidad AS IdLocalidad,RTRIM(l.loc_descripcion) AS Localidad,r.estado AS Estado FROM vt_especialidad_localidad r INNER JOIN vt_especialidades e ON r.esp_id=e.es_id INNER JOIN vt_localidades l ON r.id_localidad=l.loc_id ORDER BY l.loc_descripcion,e.es_descripcion";
        return (await _db.QueryAsync<EspecialidadLocalidadDto>(sql)).ToList();
    }

    public async Task<(bool Exito, string Mensaje)> GuardarEspecialidadLocalidadAsync(EspecialidadLocalidadDto x, int idUsuario)
    {
        if (x.IdEspecialidad <= 0 || x.IdLocalidad <= 0) return (false, "Seleccione especialidad y localidad.");
        int duplicados = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM vt_especialidad_localidad WHERE esp_id=@IdEspecialidad AND id_localidad=@IdLocalidad AND id<>@Id", x);
        if (duplicados > 0) return (false, "La especialidad ya está asignada a esa localidad.");
        const string insertar = "INSERT INTO vt_especialidad_localidad (esp_id,id_localidad,estado) VALUES (@IdEspecialidad,@IdLocalidad,@Estado)";
        const string actualizar = "UPDATE vt_especialidad_localidad SET esp_id=@IdEspecialidad,id_localidad=@IdLocalidad,estado=@Estado WHERE id=@Id";
        int filas = await _db.ExecuteAsync(x.Id > 0 ? actualizar : insertar, x);
        if (filas <= 0) return (false, "No se pudo guardar la asignación.");
        await _db.RegistrarBitacoraAsync("Asignación de especialidad a localidad", idUsuario);
        return (true, "Asignación guardada correctamente.");
    }

    public async Task<List<EspecialidadAdministrativaDto>> ObtenerEspecialidadesAdminAsync()
    {
        const string sql = @"SELECT 
            es_id AS Id, 
            RTRIM(ISNULL(es_codigo, '')) AS Codigo,
            RTRIM(es_descripcion) AS Descripcion, 
            ISNULL(es_estado, 'A') AS Estado,
            CASE WHEN UPPER(RTRIM(ISNULL(es_servicio, ''))) = 'S' THEN 1 ELSE 0 END AS RequiereTriaje
        FROM vt_especialidades 
        ORDER BY es_descripcion";
        return (await _db.QueryAsync<EspecialidadAdministrativaDto>(sql)).ToList();
    }

    public async Task<(bool Exito, string Mensaje)> GuardarEspecialidadAsync(EspecialidadAdministrativaDto especialidad, int idUsuario)
    {
        if (string.IsNullOrWhiteSpace(especialidad.Descripcion))
            return (false, "El nombre de la especialidad es obligatorio.");

        string servicio = especialidad.RequiereTriaje ? "S" : "";
        string estado = especialidad.Estado == "A" ? "A" : "I";
        string desc = especialidad.Descripcion.Trim().ToUpperInvariant();
        string codigo = new string((especialidad.Codigo ?? string.Empty).Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(codigo))
        {
            string letras = new string(desc.Where(char.IsLetter).ToArray());
            codigo = letras.Length >= 3 ? letras[..3] : (letras.Length > 0 ? letras : "GEN");
        }
        if (codigo.Length > 10) codigo = codigo[..10];

        if (especialidad.Id > 0)
        {
            int duplicados = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM vt_especialidades WHERE UPPER(RTRIM(es_descripcion)) = @Desc AND es_id <> @Id",
                new { Desc = desc, Id = especialidad.Id });
            if (duplicados > 0)
                return (false, "Ya existe otra especialidad con ese nombre.");

            const string updateSql = @"
                UPDATE vt_especialidades 
                SET es_descripcion = @Descripcion, 
                    es_codigo = @Codigo,
                    es_estado = @Estado, 
                    es_servicio = @Servicio 
                WHERE es_id = @Id";
            int filas = await _db.ExecuteAsync(updateSql, new
            {
                Descripcion = desc,
                Codigo = codigo,
                Estado = estado,
                Servicio = servicio,
                Id = especialidad.Id
            });
            if (filas > 0)
            {
                await _db.RegistrarBitacoraAsync($"Actualización especialidad {desc} ({codigo})", idUsuario);
                return (true, $"Especialidad '{desc}' (Código: {codigo}) actualizada correctamente.");
            }
            return (false, "No se encontró la especialidad para actualizar.");
        }
        else
        {
            int duplicados = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM vt_especialidades WHERE UPPER(RTRIM(es_descripcion)) = @Desc",
                new { Desc = desc });
            if (duplicados > 0)
                return (false, "Ya existe una especialidad con ese nombre.");

            const string insertSql = @"
                INSERT INTO vt_especialidades (es_descripcion, es_codigo, es_estado, es_servicio) 
                VALUES (@Descripcion, @Codigo, @Estado, @Servicio)";
            await _db.ExecuteAsync(insertSql, new
            {
                Descripcion = desc,
                Codigo = codigo,
                Estado = estado,
                Servicio = servicio
            });
            await _db.RegistrarBitacoraAsync($"Creación de especialidad {desc} ({codigo})", idUsuario);
            return (true, $"Especialidad '{desc}' (Código: {codigo}) creada exitosamente.");
        }
    }

    public async Task<(bool Exito, string Mensaje)> CambiarEstadoEspecialidadAsync(int idEspecialidad, string nuevoEstado, int idUsuario)
    {
        if (idEspecialidad <= 0)
            return (false, "Identificador de especialidad no válido.");

        string estado = nuevoEstado == "A" ? "A" : "I";
        const string sql = "UPDATE vt_especialidades SET es_estado = @Estado WHERE es_id = @Id";
        int filas = await _db.ExecuteAsync(sql, new { Estado = estado, Id = idEspecialidad });
        if (filas > 0)
        {
            string accion = estado == "A" ? "Activación" : "Desactivación / eliminación";
            await _db.RegistrarBitacoraAsync($"{accion} especialidad ID {idEspecialidad}", idUsuario);
            return (true, estado == "A" ? "Especialidad activada correctamente." : "Especialidad desactivada / quitada correctamente.");
        }
        return (false, "No se pudo cambiar el estado de la especialidad.");
    }

    public async Task<IngresoCirugiaDto> ObtenerIngresoCirugiaAsync(int idOrden, int nhcl) =>
        await _db.QueryFirstOrDefaultAsync<IngresoCirugiaDto>(@"SELECT TOP 1 
            sec AS Sec,
            nhcl AS Nhcl,
            id_orden AS IdOrden,
            fecha AS Fecha,
            CAST(ISNULL(sala, 1) AS VARCHAR(10)) AS Sala,
            ISNULL(CONVERT(VARCHAR(5), entra, 108), '') AS Entra,
            ISNULL(CONVERT(VARCHAR(5), sale, 108), '') AS Sale,
            RTRIM(ISNULL(transporte,'')) AS Transporte,
            RTRIM(ISNULL(posicion,'')) AS Posicion,
            RTRIM(ISNULL(observaciones1,'')) AS Observaciones 
        FROM qvt_ingresocirugia 
        WHERE id_orden=@IdOrden AND nhcl=@Nhcl 
        ORDER BY sec DESC", new { IdOrden = idOrden, Nhcl = nhcl }) ?? new() { IdOrden = idOrden, Nhcl = nhcl };

    public async Task<bool> GuardarIngresoCirugiaAsync(IngresoCirugiaDto x, int idUsuario)
    {
        int salaId = 1;
        if (!string.IsNullOrWhiteSpace(x.Sala))
        {
            if (!int.TryParse(x.Sala.Trim(), out salaId))
            {
                var digits = new string(x.Sala.Where(char.IsDigit).ToArray());
                if (!int.TryParse(digits, out salaId)) salaId = 1;
            }
        }

        DateTime? entraDt = ParseHoraOFecha(x.Entra, x.Fecha);
        DateTime? saleDt = ParseHoraOFecha(x.Sale, x.Fecha);

        string transporte = x.Transporte?.Trim() ?? "";
        if (transporte.Length > 150) transporte = transporte.Substring(0, 150);

        string posicion = x.Posicion?.Trim() ?? "";
        if (posicion.Length > 50) posicion = posicion.Substring(0, 50);

        string obs = x.Observaciones?.Trim() ?? "";
        if (obs.Length > 150) obs = obs.Substring(0, 150);

        if (x.Sec <= 0)
        {
            var existeSec = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 sec FROM qvt_ingresocirugia WHERE id_orden=@IdOrden AND nhcl=@Nhcl ORDER BY sec DESC",
                new { x.IdOrden, x.Nhcl });
            if (existeSec.HasValue && existeSec.Value > 0)
            {
                x.Sec = existeSec.Value;
            }
        }

        const string ins = @"INSERT INTO qvt_ingresocirugia(
            nhcl, id_orden, fecha, sala, entra, sale, transporte, posicion, observaciones1, iduser
        ) VALUES (
            @Nhcl, @IdOrden, @Fecha, @Sala, @Entra, @Sale, @Transporte, @Posicion, @Observaciones, @IdUsuario
        )";

        const string upd = @"UPDATE qvt_ingresocirugia SET 
            fecha = @Fecha,
            sala = @Sala,
            entra = @Entra,
            sale = @Sale,
            transporte = @Transporte,
            posicion = @Posicion,
            observaciones1 = @Observaciones,
            iduser = @IdUsuario 
        WHERE sec = @Sec";

        return await _db.ExecuteAsync(x.Sec > 0 ? upd : ins, new
        {
            x.Nhcl,
            x.IdOrden,
            x.Fecha,
            Sala = salaId,
            Entra = entraDt,
            Sale = saleDt,
            Transporte = transporte,
            Posicion = posicion,
            Observaciones = obs,
            IdUsuario = idUsuario,
            x.Sec
        }) > 0;
    }

    private static DateTime? ParseHoraOFecha(string? val, DateTime fechaBase)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        val = val.Trim();

        if (DateTime.TryParse(val, out var dtCompleto) && dtCompleto.Year > 1900)
            return dtCompleto;

        if (TimeSpan.TryParse(val, out var ts))
            return fechaBase.Date.Add(ts);

        if (int.TryParse(val, out var horaEntera) && horaEntera >= 0 && horaEntera <= 23)
            return fechaBase.Date.AddHours(horaEntera);

        return null;
    }

    public async Task<List<DespachoClinicoDto>> ObtenerDespachosAsync(DateTime desde,DateTime hasta,int tipo=0) =>
        (await _db.QueryAsync<DespachoClinicoDto>(@"SELECT o.ordenAten_id AS IdOrden,o.ordenAten_nhcl AS Nhcl,RTRIM(p.mas_nombre) AS Paciente,RTRIM(c.pa_apellidos+' '+c.pa_nombres) AS Propietario,o.ordenAten_fecha AS Fecha,o.ordenAten_Despacho AS TipoDespacho,RTRIM(ISNULL(o.ordenAten_etapa,'')) AS Etapa FROM vt_ordenAtencion o INNER JOIN vt_pacientes p ON o.ordenAten_nhcl=p.mas_nhcl INNER JOIN vt_clientes c ON p.mas_idcliente=c.pa_codigo WHERE o.ordenAten_fecha>=@Desde AND o.ordenAten_fecha<DATEADD(day,1,@Hasta) AND o.ordenAten_Despacho IN(1,2) AND (@Tipo=0 OR o.ordenAten_Despacho=@Tipo) ORDER BY o.ordenAten_fecha DESC",new{Desde=desde.Date,Hasta=hasta.Date,Tipo=tipo})).ToList();

    public async Task<bool> MarcarDespachoProcesadoAsync(int idOrden) => await _db.ExecuteAsync("UPDATE vt_ordenAtencion SET ordenAten_etapa='Procesado',ordenAten_fechaProcesado=GETDATE() WHERE ordenAten_id=@IdOrden",new{IdOrden=idOrden})>0;

    public async Task<List<FisioterapiaOrdenDto>> ObtenerCatalogoFisioterapiaAsync() =>
        (await _db.QueryAsync<FisioterapiaOrdenDto>(@"
            SELECT
                p.CODIGO AS IdProducto,
                RTRIM(ISNULL(p.NOMBRE, '')) AS Descripcion,
                CAST(ISNULL(p.VALOR_UNITARIO, 0.00) AS decimal(18,2)) AS Precio
            FROM VeterinariaFacturacion.dbo.PRODUCTO p
            INNER JOIN VeterinariaFacturacion.dbo.PRODUCTOTIPO t ON t.IDTIPOPRODUCTO = p.TIPO_PRODUCTO
            WHERE ISNULL(p.ESTADO, 1) = 1
              AND p.IDUSUARIO >= 1000000
              AND UPPER(LTRIM(RTRIM(ISNULL(t.DESCRIPCION, '')))) LIKE '%FISIOTERAPIA%'
            ORDER BY p.NOMBRE ASC")).ToList();

    public async Task<List<FisioterapiaOrdenDto>> ObtenerFisioterapiaAsync(int idOrden, int nhcl) =>
        (await _db.QueryAsync<FisioterapiaOrdenDto>(@"
            SELECT
                f.sec AS Sec,
                f.fisioO_nhcl AS Nhcl,
                f.fisioO_id_orden AS IdOrden,
                f.fisioO_idProdu AS IdProducto,
                RTRIM(COALESCE(p.NOMBRE COLLATE DATABASE_DEFAULT, NULLIF(RTRIM(f.fisoO_observaciones), ''), 'Sesión de Fisioterapia')) AS Descripcion,
                RTRIM(ISNULL(f.fisoO_observaciones, '')) AS Observaciones,
                TRY_CAST(f.fisioO_cantidad AS decimal(18,2)) AS Cantidad,
                CAST(ISNULL(p.VALOR_UNITARIO, 0.00) AS decimal(18,2)) AS Precio,
                f.fisioO_fecha AS Fecha
            FROM vt_fisioterapia_ordenes f
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON f.fisioO_idProdu = p.CODIGO
            WHERE f.fisioO_id_orden = @IdOrden
              AND f.fisioO_nhcl = @Nhcl
              AND ISNULL(f.fisioO_estado, 'A') = 'A'
            ORDER BY f.fisioO_fecha DESC", new { IdOrden = idOrden, Nhcl = nhcl })).ToList();

    public async Task<bool> GuardarFisioterapiaAsync(FisioterapiaOrdenDto x) => await _db.ExecuteAsync(@"INSERT INTO vt_fisioterapia_ordenes(fisioO_nhcl,fisioO_idProdu,fisoO_observaciones,fisioO_cantidad,fisioO_id_orden,fisioO_estado,fisioO_fecha) VALUES(@Nhcl,@IdProducto,@Observaciones,@Cantidad,@IdOrden,'A',@Fecha)",x)>0;

    public async Task<bool> EliminarFisioterapiaAsync(int sec) =>
        await _db.ExecuteAsync("DELETE FROM vt_fisioterapia_ordenes WHERE sec = @Sec", new { Sec = sec }) > 0;

    public async Task<List<IStatDto>> ObtenerIStatAsync(int nhcl) =>
        (await _db.QueryAsync<IStatDto>(@"
            SELECT 
                i.sec AS Id,
                i.is_nhcl AS Nhcl,
                i.is_fecha AS Fecha,
                ISNULL(i.is_iduser, 0) AS IdUser,
                RTRIM(ISNULL(u.user_nombre, '')) AS QuienIngresa,
                RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad,
                RTRIM(ISNULL(i.ph,'')) AS Ph,
                RTRIM(ISNULL(i.pressure,'')) AS Pressure,
                RTRIM(ISNULL(i.oxygen,'')) AS Oxygen,
                RTRIM(ISNULL(i.bicarbonato,'')) AS Bicarbonato,
                RTRIM(ISNULL(i.carbon,'')) AS Carbon,
                RTRIM(ISNULL(i.base,'')) AS Base,
                RTRIM(ISNULL(i.saturation,'')) AS Saturation,
                RTRIM(ISNULL(i.lactato,'')) AS Lactato,
                RTRIM(ISNULL(i.sodio,'')) AS Sodio,
                RTRIM(ISNULL(i.potasio,'')) AS Potasio,
                RTRIM(ISNULL(i.chloride,'')) AS Chloride,
                RTRIM(ISNULL(i.ionized,'')) AS Ionized,
                RTRIM(ISNULL(i.gap,'')) AS Gap,
                RTRIM(ISNULL(i.hematocrito,'')) AS Hematocrito,
                RTRIM(ISNULL(i.hemoglobina,'')) AS Hemoglobina,
                RTRIM(ISNULL(i.glucosa,'')) AS Glucosa,
                RTRIM(ISNULL(i.urea,'')) AS Urea,
                RTRIM(ISNULL(i.creatinina,'')) AS Creatinina
            FROM vt_istat i
            LEFT JOIN vt_usuarios u ON u.user_numero = i.is_iduser
            LEFT JOIN vt_especialidades e ON e.es_id = u.user_esp_numero
            WHERE i.is_nhcl = @Nhcl AND i.is_estado = 'A' 
            ORDER BY i.is_fecha DESC", new { Nhcl = nhcl })).ToList();

    public async Task<bool> GuardarIStatAsync(IStatDto x, int idUsuario) => await _db.ExecuteAsync(@"
        INSERT INTO vt_istat(
            is_nhcl, is_fecha, is_iduser, is_cliente, is_estado, is_tipo,
            ph, pressure, oxygen, bicarbonato, carbon, base, saturation, lactato,
            sodio, potasio, chloride, ionized, gap,
            hematocrito, hemoglobina, glucosa, urea, creatinina
        ) VALUES (
            @Nhcl, @Fecha, @IdUsuario, 0, 'A', 'ISTAT',
            @Ph, @Pressure, @Oxygen, @Bicarbonato, @Carbon, @Base, @Saturation, @Lactato,
            @Sodio, @Potasio, @Chloride, @Ionized, @Gap,
            @Hematocrito, @Hemoglobina, @Glucosa, @Urea, @Creatinina
        )", new
    {
        x.Nhcl,
        Fecha = x.Fecha == default ? DateTime.Now : x.Fecha,
        IdUsuario = idUsuario,
        x.Ph,
        x.Pressure,
        x.Oxygen,
        x.Bicarbonato,
        x.Carbon,
        x.Base,
        x.Saturation,
        x.Lactato,
        x.Sodio,
        x.Potasio,
        x.Chloride,
        x.Ionized,
        x.Gap,
        x.Hematocrito,
        x.Hemoglobina,
        x.Glucosa,
        x.Urea,
        x.Creatinina
    }) > 0;

    public async Task<bool> EliminarIStatAsync(int id,int idUsuario) => await _db.ExecuteAsync("UPDATE vt_istat SET is_estado='I',is_elimina=@IdUsuario,is_fechadelete=GETDATE() WHERE sec=@Id",new{Id=id,IdUsuario=idUsuario})>0;

    private static CatalogoSql ObtenerConfiguracion(string tipo) =>
        Catalogos.TryGetValue(tipo, out var config)
            ? config
            : throw new ArgumentException("Tipo de catálogo no permitido.", nameof(tipo));
}

