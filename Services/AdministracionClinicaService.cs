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
    Task<PacienteResumenCrecimientoDto?> ObtenerInfoPacienteCrecimientoAsync(int nhcl);
    Task<List<LocalidadAdministrativaDto>> ObtenerLocalidadesAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerProvinciasAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerCiudadesAsync(int idProvincia);
    Task<(bool Exito, string Mensaje)> GuardarLocalidadAsync(LocalidadAdministrativaDto localidad, int idUsuario);
    Task<List<EspecialidadLocalidadDto>> ObtenerEspecialidadesLocalidadAsync();
    Task<(bool Exito, string Mensaje)> GuardarEspecialidadLocalidadAsync(EspecialidadLocalidadDto relacion, int idUsuario);
    Task<IngresoCirugiaDto> ObtenerIngresoCirugiaAsync(int idOrden, int nhcl);
    Task<bool> GuardarIngresoCirugiaAsync(IngresoCirugiaDto ingreso, int idUsuario);
    Task<List<DespachoClinicoDto>> ObtenerDespachosAsync(DateTime desde, DateTime hasta, int tipo = 0);
    Task<bool> MarcarDespachoProcesadoAsync(int idOrden);
    Task<List<FisioterapiaOrdenDto>> ObtenerFisioterapiaAsync(int idOrden, int nhcl);
    Task<bool> GuardarFisioterapiaAsync(FisioterapiaOrdenDto orden);
    Task<List<IStatDto>> ObtenerIStatAsync(int nhcl);
    Task<bool> GuardarIStatAsync(IStatDto resultado, int idUsuario);
    Task<bool> EliminarIStatAsync(int id, int idUsuario);
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
            ("Medicamentos", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, ISNULL(m.med_fecha, GETDATE()) AS Fecha, ISNULL(m.med_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_medicinas m INNER JOIN vt_productos p ON m.med_idProdu=p.prod_id WHERE m.med_nhcl=@Nhcl AND m.med_tipo='M'"),
            ("Insumos", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, ISNULL(m.med_fecha, GETDATE()) AS Fecha, ISNULL(m.med_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_medicinas m INNER JOIN vt_productos p ON m.med_idProdu=p.prod_id WHERE m.med_nhcl=@Nhcl AND m.med_tipo='I'"),
            ("Fisioterapia", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, ISNULL(m.fisioO_fecha, GETDATE()) AS Fecha, ISNULL(m.fisioO_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_fisioterapia_ordenes m INNER JOIN vt_productos p ON m.fisioO_idProdu=p.prod_id WHERE m.fisioO_nhcl=@Nhcl"),
            ("Imagenología", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, ISNULL(m.rxO_fecha, GETDATE()) AS Fecha, ISNULL(m.rxO_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_rxOrdenes m INNER JOIN vt_productos p ON m.rxO_idProdu=p.prod_id WHERE m.rxO_nhcl=@Nhcl"),
            ("Procedimientos", @"SELECT '' AS Codigo, RTRIM(p.prod_descripcion) AS Descripcion, ISNULL(m.proc_fecha, GETDATE()) AS Fecha, ISNULL(m.proc_idOrden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_procedimientos m INNER JOIN vt_productos p ON m.proc_idProdu=p.prod_id WHERE m.proc_nhcl=@Nhcl"),
            ("Laboratorio", @"SELECT '' AS Codigo, RTRIM(p.lab_descripcion) AS Descripcion, ISNULL(m.labO_fecha, GETDATE()) AS Fecha, ISNULL(m.labO_id_orden, 0) AS IdOrden, 1 AS NumeroVeces FROM vt_labOrdenes m INNER JOIN vt_pruebas_laboratorio p ON m.labO_idProdu=p.lab_id WHERE m.labO_nhcl=@Nhcl")
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

    public async Task<List<FisioterapiaOrdenDto>> ObtenerFisioterapiaAsync(int idOrden,int nhcl) =>
        (await _db.QueryAsync<FisioterapiaOrdenDto>(@"SELECT f.sec AS Sec,f.fisioO_nhcl AS Nhcl,f.fisioO_id_orden AS IdOrden,f.fisioO_idProdu AS IdProducto,RTRIM(ISNULL(p.prod_descripcion,'')) AS Descripcion,RTRIM(ISNULL(f.fisoO_observaciones,'')) AS Observaciones,TRY_CAST(f.fisioO_cantidad AS decimal(18,2)) AS Cantidad,f.fisioO_fecha AS Fecha FROM vt_fisioterapia_ordenes f LEFT JOIN vt_productos p ON f.fisioO_idProdu=p.prod_id WHERE f.fisioO_id_orden=@IdOrden AND f.fisioO_nhcl=@Nhcl AND ISNULL(f.fisioO_estado,'A')='A' ORDER BY f.fisioO_fecha DESC",new{IdOrden=idOrden,Nhcl=nhcl})).ToList();

    public async Task<bool> GuardarFisioterapiaAsync(FisioterapiaOrdenDto x) => await _db.ExecuteAsync(@"INSERT INTO vt_fisioterapia_ordenes(fisioO_nhcl,fisioO_idProdu,fisoO_observaciones,fisioO_cantidad,fisioO_id_orden,fisioO_estado,fisioO_fecha) VALUES(@Nhcl,@IdProducto,@Observaciones,@Cantidad,@IdOrden,'A',@Fecha)",x)>0;

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

