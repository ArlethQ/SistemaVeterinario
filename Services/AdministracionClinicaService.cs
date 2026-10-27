using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IAdministracionClinicaService
{
    IReadOnlyList<TipoCatalogoAdministrativo> ObtenerTiposCatalogo();
    Task<List<CatalogoAdministrativoDto>> ObtenerCatalogoAsync(string tipo);
    Task<(bool Exito, string Mensaje)> GuardarCatalogoAsync(string tipo, CatalogoAdministrativoDto item, int idUsuario);
    Task<List<RecordatorioVacunaDto>> ObtenerRecordatoriosAsync(DateTime desde, DateTime hasta, bool porFechaRevision);
    Task<List<AlertaClinicaDto>> ObtenerAlertasAsync(DateTime desde, DateTime hasta, string estado);
    Task<List<PacienteFallecidoDto>> ObtenerFallecidosAsync(DateTime desde, DateTime hasta);
    Task<List<HistorialPacienteItemDto>> ObtenerResumenHistorialAsync(int nhcl);
    Task<List<MedicionCrecimientoDto>> ObtenerCurvaCrecimientoAsync(int nhcl);
    Task<List<LocalidadAdministrativaDto>> ObtenerLocalidadesAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerProvinciasAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerCiudadesAsync(int idProvincia);
    Task<(bool Exito, string Mensaje)> GuardarLocalidadAsync(LocalidadAdministrativaDto localidad, int idUsuario);
    Task<List<EspecialidadLocalidadDto>> ObtenerEspecialidadesLocalidadAsync();
    Task<(bool Exito, string Mensaje)> GuardarEspecialidadLocalidadAsync(EspecialidadLocalidadDto relacion, int idUsuario);
}

public sealed class AdministracionClinicaService : IAdministracionClinicaService
{
    private readonly ISqlDataService _db;

    private sealed record CatalogoSql(
        string Nombre,
        string SelectSql,
        string InsertSql,
        string UpdateSql,
        bool RequierePadre = false);

    private static readonly IReadOnlyDictionary<string, CatalogoSql> Catalogos =
        new Dictionary<string, CatalogoSql>(StringComparer.OrdinalIgnoreCase)
        {
            ["especies"] = new("Especies",
                "SELECT especie_id AS Id, RTRIM(especie_descripcion) AS Descripcion, especie_estado AS Estado FROM vt_especie ORDER BY especie_descripcion",
                "INSERT INTO vt_especie (especie_descripcion, especie_estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_especie SET especie_descripcion=@Descripcion, especie_estado=@Estado WHERE especie_id=@Id"),
            ["razas"] = new("Razas",
                "SELECT r.id_raza AS Id, RTRIM(r.des_raza) AS Descripcion, r.estado AS Estado, r.id_especie AS IdPadre, RTRIM(e.especie_descripcion) AS PadreDescripcion FROM vt_razas r INNER JOIN vt_especie e ON e.especie_id=r.id_especie ORDER BY r.des_raza",
                "INSERT INTO vt_razas (des_raza, id_especie, estado) VALUES (@Descripcion, @IdPadre, @Estado)",
                "UPDATE vt_razas SET des_raza=@Descripcion, id_especie=@IdPadre, estado=@Estado WHERE id_raza=@Id", true),
            ["generos"] = new("Géneros",
                "SELECT id AS Id, RTRIM(sexo) AS Descripcion, estado AS Estado FROM vt_sexo ORDER BY sexo",
                "INSERT INTO vt_sexo (id, sexo, estado) VALUES ((SELECT ISNULL(MAX(id),0)+1 FROM vt_sexo), @Descripcion, @Estado)",
                "UPDATE vt_sexo SET sexo=@Descripcion, estado=@Estado WHERE id=@Id"),
            ["especialidades"] = new("Especialidades",
                "SELECT es_id AS Id, RTRIM(es_descripcion) AS Descripcion, es_estado AS Estado FROM vt_especialidades ORDER BY es_descripcion",
                "INSERT INTO vt_especialidades (es_descripcion, es_estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_especialidades SET es_descripcion=@Descripcion, es_estado=@Estado WHERE es_id=@Id"),
            ["clases"] = new("Clases de fauna",
                "SELECT id_clas AS Id, RTRIM(des_clas) AS Descripcion, estado_clas AS Estado FROM vt_clase ORDER BY des_clas",
                "INSERT INTO vt_clase (des_clas, estado_clas) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_clase SET des_clas=@Descripcion, estado_clas=@Estado WHERE id_clas=@Id"),
            ["categorias-ingreso"] = new("Categorías de ingreso",
                "SELECT id AS Id, RTRIM(ingreso) AS Descripcion, estado AS Estado FROM vt_ingreso ORDER BY ingreso",
                "INSERT INTO vt_ingreso (ingreso, estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_ingreso SET ingreso=@Descripcion, estado=@Estado WHERE id=@Id"),
            ["tipos-cliente"] = new("Tipos de cliente",
                "SELECT id_personal AS Id, RTRIM(des_personal) AS Descripcion, estado AS Estado FROM vt_tipo_paciente_clasifica ORDER BY des_personal",
                "INSERT INTO vt_tipo_paciente_clasifica (des_personal, estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_tipo_paciente_clasifica SET des_personal=@Descripcion, estado=@Estado WHERE id_personal=@Id"),
            ["tipos-paciente"] = new("Tipos de paciente",
                "SELECT tip_sec AS Id, RTRIM(tip_descripcion) AS Descripcion, tip_estado AS Estado FROM vt_tipoPaciente ORDER BY tip_descripcion",
                "INSERT INTO vt_tipoPaciente (tip_descripcion, tip_estado) VALUES (@Descripcion, @Estado)",
                "UPDATE vt_tipoPaciente SET tip_descripcion=@Descripcion, tip_estado=@Estado WHERE tip_sec=@Id")
        };

    public AdministracionClinicaService(ISqlDataService db) => _db = db;

    public IReadOnlyList<TipoCatalogoAdministrativo> ObtenerTiposCatalogo() => Catalogos
        .Select(x => new TipoCatalogoAdministrativo(x.Key, x.Value.Nombre, x.Value.RequierePadre))
        .ToList();

    public async Task<List<CatalogoAdministrativoDto>> ObtenerCatalogoAsync(string tipo)
    {
        var config = ObtenerConfiguracion(tipo);
        return (await _db.QueryAsync<CatalogoAdministrativoDto>(config.SelectSql)).ToList();
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
        int filas = await _db.ExecuteAsync(item.Id > 0 ? config.UpdateSql : config.InsertSql, item);
        if (filas <= 0) return (false, "No se pudo guardar el registro en la base de datos.");

        await _db.RegistrarBitacoraAsync($"{(item.Id > 0 ? "Actualización" : "Creación")} de {config.Nombre}: {item.Descripcion}", idUsuario);
        return (true, "Registro guardado correctamente.");
    }

    public async Task<List<RecordatorioVacunaDto>> ObtenerRecordatoriosAsync(DateTime desde, DateTime hasta, bool porFechaRevision)
    {
        string campoFecha = porFechaRevision ? "v.fecharevision" : "v.fechaenvio";
        string sql = $@"
            SELECT v.nhcl AS Nhcl, RTRIM(p.mas_nombre) AS Paciente,
                RTRIM(c.pa_apellidos + ' ' + c.pa_nombres) AS Propietario,
                RTRIM(u.user_nombre) AS QuienManda,
                RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad,
                RTRIM(ISNULL(v.correoenvio, '')) AS CorreoEnviado,
                v.fechaenvio AS FechaEnvio, v.fecharevision AS ProximaRevision,
                RTRIM(COALESCE(NULLIF(c.pa_movil,''), c.pa_telefono, '')) AS Telefono
            FROM vt_vitacoravacuna v
            INNER JOIN vt_usuarios u ON v.quienmanda=u.user_numero
            INNER JOIN vt_pacientes p ON v.nhcl=p.mas_nhcl
            INNER JOIN vt_clientes c ON p.mas_idcliente=c.pa_codigo
            LEFT JOIN vt_especialidades e ON e.es_id=u.user_esp_numero
            WHERE {campoFecha} >= @Desde AND {campoFecha} < DATEADD(day,1,@Hasta)
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
                RTRIM(ISNULL(f.med_pronostico,'')) AS Pronostico,
                RTRIM(ISNULL(f.estadoparte,'')) AS Estado,
                RTRIM(COALESCE(NULLIF(c.pa_movil,''), c.pa_telefono, '')) AS Telefono
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
                RTRIM(c.pa_apellidos + ' ' + c.pa_nombres) AS Propietario,
                RTRIM(ISNULL(m.motivo,'')) AS Motivo
            FROM vt_pacientes p
            INNER JOIN vt_clientes c ON p.mas_idcliente=c.pa_codigo
            LEFT JOIN vt_motivomuerte m ON p.motivofallece=m.sec
            WHERE p.fallecido='TRUE' AND p.fechafallecido>=@Desde AND p.fechafallecido<DATEADD(day,1,@Hasta)
            ORDER BY p.fechafallecido DESC";
        return (await _db.QueryAsync<PacienteFallecidoDto>(sql, new { Desde = desde.Date, Hasta = hasta.Date })).ToList();
    }

    public async Task<List<HistorialPacienteItemDto>> ObtenerResumenHistorialAsync(int nhcl)
    {
        var resultado = new List<HistorialPacienteItemDto>();
        var consultas = new[]
        {
            ("Diagnósticos", @"SELECT CAST(c.cie_codigo AS varchar(30)) AS Codigo, RTRIM(c.cie_diagnostico) AS Descripcion, COUNT(*) AS NumeroVeces FROM vt_diagnosticos d INNER JOIN vt_cie10 c ON d.dig_codigoDiag=c.cie_codigo WHERE d.dig_nhcl=@Nhcl GROUP BY c.cie_codigo,c.cie_diagnostico"),
            ("Medicamentos", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, COUNT(*) AS NumeroVeces FROM vt_medicinas m INNER JOIN vt_productos p ON m.med_idProdu=p.prod_id WHERE m.med_nhcl=@Nhcl AND m.med_tipo='M' GROUP BY p.prod_descripcion"),
            ("Insumos", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, COUNT(*) AS NumeroVeces FROM vt_medicinas m INNER JOIN vt_productos p ON m.med_idProdu=p.prod_id WHERE m.med_nhcl=@Nhcl AND m.med_tipo='I' GROUP BY p.prod_descripcion"),
            ("Fisioterapia", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, COUNT(*) AS NumeroVeces FROM vt_fisioterapia_ordenes m INNER JOIN vt_productos p ON m.fisioO_idProdu=p.prod_id WHERE m.fisioO_nhcl=@Nhcl GROUP BY p.prod_descripcion"),
            ("Imagenología", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, COUNT(*) AS NumeroVeces FROM vt_rxOrdenes m INNER JOIN vt_productos p ON m.rxO_idProdu=p.prod_id WHERE m.rxO_nhcl=@Nhcl GROUP BY p.prod_descripcion"),
            ("Procedimientos", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, COUNT(*) AS NumeroVeces FROM vt_procedimientos m INNER JOIN vt_productos p ON m.proc_idProdu=p.prod_id WHERE m.proc_nhcl=@Nhcl GROUP BY p.prod_descripcion"),
            ("Laboratorio", @"SELECT '' AS Codigo, RTRIM(p.lab_descripcion) AS Descripcion, COUNT(*) AS NumeroVeces FROM vt_labOrdenes m INNER JOIN vt_pruebas_laboratorio p ON m.labO_idProdu=p.lab_id WHERE m.labO_nhcl=@Nhcl GROUP BY p.lab_descripcion")
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

    public async Task<List<MedicionCrecimientoDto>> ObtenerCurvaCrecimientoAsync(int nhcl)
    {
        const string sql = @"
            SELECT f.med_fecha AS Fecha, TRY_CAST(f.peso AS decimal(18,2)) AS Peso,
                TRY_CAST(f.frecuenciacardiaca AS decimal(18,2)) AS FrecuenciaCardiaca,
                TRY_CAST(f.frecuenciarespiratoria AS decimal(18,2)) AS FrecuenciaRespiratoria,
                DATEDIFF(month,p.mas_fechanace,f.med_fecha) AS EdadMeses
            FROM vt_fichaMedica f INNER JOIN vt_pacientes p ON f.med_nhcl=p.mas_nhcl
            WHERE f.med_nhcl=@Nhcl ORDER BY f.med_fecha";
        return (await _db.QueryAsync<MedicionCrecimientoDto>(sql, new { Nhcl = nhcl })).ToList();
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
        const string insertar = @"INSERT INTO vt_localidades (loc_descripcion,loc_direccion,loc_telefono,loc_ruc,loc_administrador,loc_estado,loc_nom_empresa,loc_prefijo,loc_region,idCiudad,prov_id) VALUES (@Localidad,@Direccion,@Telefono,@Ruc,@Administrador,@Estado,@Empresa,@Prefijo,@Region,@IdCiudad,@IdProvincia)";
        const string actualizar = @"UPDATE vt_localidades SET loc_descripcion=@Localidad,loc_direccion=@Direccion,loc_telefono=@Telefono,loc_ruc=@Ruc,loc_administrador=@Administrador,loc_estado=@Estado,loc_nom_empresa=@Empresa,loc_prefijo=@Prefijo,loc_region=@Region,idCiudad=@IdCiudad,prov_id=@IdProvincia WHERE loc_id=@Id";
        int filas = await _db.ExecuteAsync(x.Id > 0 ? actualizar : insertar, x);
        if (filas <= 0) return (false, "No se pudo guardar la localidad.");
        await _db.RegistrarBitacoraAsync($"{(x.Id > 0 ? "Actualización" : "Creación")} de localidad: {x.Localidad}", idUsuario);
        return (true, "Localidad guardada correctamente.");
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

    private static CatalogoSql ObtenerConfiguracion(string tipo) =>
        Catalogos.TryGetValue(tipo, out var config)
            ? config
            : throw new ArgumentException("Tipo de catálogo no permitido.", nameof(tipo));
}

