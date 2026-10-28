using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IGestionMigradaService
{
    Task<List<ReporteAtencionDto>> ObtenerAtencionesAsync(DateTime desde, DateTime hasta);
    Task<List<ReporteDiagnosticoDto>> ObtenerDiagnosticosAsync(DateTime desde, DateTime hasta);
    Task<List<ParteOperatorioResumenDto>> ObtenerPartesOperatoriosAsync(DateTime desde, DateTime hasta);
    Task<List<HorarioMedicoDto>> ObtenerHorariosAsync();
    Task<bool> GuardarHorarioAsync(HorarioMedicoDto item, int idUsuario);
    Task<bool> EliminarHorarioAsync(int id, int idUsuario);
    Task<List<ExcepcionHorarioDto>> ObtenerExcepcionesAsync(DateTime desde, DateTime hasta);
    Task<bool> GuardarExcepcionAsync(ExcepcionHorarioDto item, int idUsuario);
    Task<bool> DesactivarExcepcionAsync(int id, int idUsuario);
    Task<List<CirugiaProgramadaDto>> ObtenerCirugiasAsync(DateTime desde, DateTime hasta);
    Task<bool> GuardarCirugiaAsync(CirugiaProgramadaDto item, int idUsuario);
    Task<bool> CancelarCirugiaAsync(int id, int idUsuario);
    Task<List<SelectorClinicoDto>> ObtenerMedicosAsync();
    Task<List<SelectorClinicoDto>> ObtenerLocalidadesSelectorAsync();
    Task<List<SelectorClinicoDto>> BuscarPacientesAsync(string filtro);
    Task<List<SelectorClinicoDto>> BuscarProcedimientosAsync(string filtro);
    Task<List<CatalogoOperativoDto>> ObtenerCatalogoAsync(string tipo);
    Task<bool> GuardarCatalogoAsync(string tipo, CatalogoOperativoDto item, int idUsuario);
    Task<List<BitacoraDto>> ObtenerBitacoraAsync(DateTime desde, DateTime hasta, string filtro);
}

public sealed class GestionMigradaService(ISqlDataService db, IEFactCatalogService eFactCatalog) : IGestionMigradaService
{
    public async Task<List<ReporteAtencionDto>> ObtenerAtencionesAsync(DateTime desde, DateTime hasta) => (await db.QueryAsync<ReporteAtencionDto>(@"
        SELECT o.ordenAten_id Orden,f.med_nhcl Nhcl,f.med_fecha Fecha,RTRIM(ISNULL(u.user_nombre,'')) Medico,
        RTRIM(p.mas_nombre) Paciente,RTRIM(c.pa_apellidos+' '+c.pa_nombres) Cliente,
        RTRIM(COALESCE(ci.cie_diagnostico,CONVERT(varchar(max),d.dig_descripcion),'')) Diagnostico,RTRIM(ISNULL(l.loc_descripcion,'')) Localidad
        FROM vt_fichamedica f INNER JOIN vt_ordenAtencion o ON o.ordenAten_id=f.med_orden
        INNER JOIN vt_pacientes p ON p.mas_nhcl=f.med_nhcl INNER JOIN vt_clientes c ON c.pa_codigo=f.med_cliente
        LEFT JOIN vt_usuarios u ON u.user_numero=f.med_iduser LEFT JOIN vt_diagnosticos d ON d.dig_id_orden=f.med_orden
        LEFT JOIN vt_cie10 ci ON ci.cie_codigo=d.dig_codigoDiag LEFT JOIN vt_localidades l ON l.loc_id=o.ordenAten_idlocalidad
        WHERE o.ordenAten_etapa='PROCESADO' AND f.med_fecha>=@Desde AND f.med_fecha<DATEADD(day,1,@Hasta) ORDER BY f.med_fecha DESC",new{Desde=desde.Date,Hasta=hasta.Date})).ToList();

    public async Task<List<ReporteDiagnosticoDto>> ObtenerDiagnosticosAsync(DateTime desde, DateTime hasta) => (await db.QueryAsync<ReporteDiagnosticoDto>(@"
        SELECT f.med_orden Orden,f.med_nhcl Nhcl,f.med_fecha Fecha,RTRIM(p.mas_nombre) Paciente,
        RTRIM(c.pa_apellidos+' '+c.pa_nombres) Cliente,RTRIM(ISNULL(u.user_nombre,'')) Medico,
        RTRIM(COALESCE(ci.cie_diagnostico,CONVERT(varchar(max),d.dig_descripcion),'')) Diagnostico,
        CASE WHEN d.dig_tipo='P' THEN 'Presuntivo' ELSE 'Definitivo' END Tipo
        FROM vt_fichamedica f INNER JOIN vt_pacientes p ON p.mas_nhcl=f.med_nhcl INNER JOIN vt_clientes c ON c.pa_codigo=f.med_cliente
        INNER JOIN vt_diagnosticos d ON d.dig_id_orden=f.med_orden LEFT JOIN vt_cie10 ci ON ci.cie_codigo=d.dig_codigoDiag
        LEFT JOIN vt_usuarios u ON u.user_numero=f.med_iduser WHERE f.med_fecha>=@Desde AND f.med_fecha<DATEADD(day,1,@Hasta) ORDER BY f.med_fecha DESC",new{Desde=desde.Date,Hasta=hasta.Date})).ToList();

    public async Task<List<ParteOperatorioResumenDto>> ObtenerPartesOperatoriosAsync(DateTime desde, DateTime hasta) => (await db.QueryAsync<ParteOperatorioResumenDto>(@"
        SELECT q.id Id,q.idOrden IdOrden,q.fecha Fecha,q.nhcl Nhcl,RTRIM(ISNULL(p.mas_nombre,'')) Paciente,
        RTRIM(ISNULL(c.pa_apellidos+' '+c.pa_nombres,'')) Propietario,
        RTRIM(ISNULL(q.procedimientoQuir,'')) Procedimiento,RTRIM(ISNULL(q.hallazgos,'')) Diagnostico
        FROM qvt_Protocolo q LEFT JOIN vt_pacientes p ON p.mas_nhcl=q.nhcl
        LEFT JOIN vt_clientes c ON c.pa_codigo=p.mas_idcliente
        WHERE q.fecha>=@Desde AND q.fecha<DATEADD(day,1,@Hasta) ORDER BY q.fecha DESC",new{Desde=desde.Date,Hasta=hasta.Date})).ToList();

    public async Task<List<HorarioMedicoDto>> ObtenerHorariosAsync() => (await db.QueryAsync<HorarioMedicoDto>(@"
        SELECT h.hr_id Id,h.hr_iduser IdMedico,RTRIM(ISNULL(u.user_nombre,'')) Medico,RTRIM(h.hr_dia) Dia,RTRIM(ISNULL(h.hr_dia2,'')) DiaIngles,
        RTRIM(h.hr_desde) Desde,RTRIM(h.hr_hasta) Hasta,h.hr_intervalo Intervalo,h.hr_localidad IdLocalidad,RTRIM(ISNULL(l.loc_descripcion,'')) Localidad
        FROM vt_horariosMedicos h LEFT JOIN vt_usuarios u ON u.user_numero=h.hr_iduser LEFT JOIN vt_localidades l ON l.loc_id=h.hr_localidad ORDER BY u.user_nombre,h.hr_id")).ToList();

    public async Task<bool> GuardarHorarioAsync(HorarioMedicoDto x,int idUsuario)
    {
        if(x.IdMedico<=0||x.IdLocalidad<=0||!HoraValida(x.Desde,x.Hasta)) return false;
        var repetido=await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM vt_horariosMedicos WHERE hr_iduser=@IdMedico AND hr_localidad=@IdLocalidad AND hr_dia2=@DiaIngles AND hr_id<>@Id AND CAST(hr_desde AS time)<CAST(@Hasta AS time) AND CAST(hr_hasta AS time)>CAST(@Desde AS time)",x);
        if(repetido>0)return false;
        var sql=x.Id>0?"UPDATE vt_horariosMedicos SET hr_iduser=@IdMedico,hr_dia=@Dia,hr_dia2=@DiaIngles,hr_desde=@Desde,hr_hasta=@Hasta,hr_intervalo=@Intervalo,hr_localidad=@IdLocalidad WHERE hr_id=@Id":"INSERT INTO vt_horariosMedicos(hr_iduser,hr_dia,hr_dia2,hr_desde,hr_hasta,hr_intervalo,hr_localidad) VALUES(@IdMedico,@Dia,@DiaIngles,@Desde,@Hasta,@Intervalo,@IdLocalidad)";
        var ok=await db.ExecuteAsync(sql,x)>0;if(ok)await db.RegistrarBitacoraAsync($"Horario médico: {x.IdMedico} {x.Dia}",idUsuario);return ok;
    }
    public async Task<bool> EliminarHorarioAsync(int id,int idUsuario){var ok=await db.ExecuteAsync("DELETE FROM vt_horariosMedicos WHERE hr_id=@Id",new{Id=id})>0;if(ok)await db.RegistrarBitacoraAsync($"Elimina horario médico {id}",idUsuario);return ok;}

    public async Task<List<ExcepcionHorarioDto>> ObtenerExcepcionesAsync(DateTime desde,DateTime hasta)=>(await db.QueryAsync<ExcepcionHorarioDto>(@"
        SELECT c.cal_id Id,c.cal_idMedico IdMedico,RTRIM(ISNULL(u.user_nombre,'')) Medico,RTRIM(ISNULL(c.cal_detalle,'')) Motivo,
        c.cal_fecha Fecha,RTRIM(ISNULL(c.cal_desde,'')) Desde,RTRIM(ISNULL(c.cal_hasta,'')) Hasta,c.cal_localidad IdLocalidad,
        RTRIM(ISNULL(l.loc_descripcion,'')) Localidad,ISNULL(c.cal_estado,'A') Estado
        FROM vt_calendarizacionHorariosMedicos c LEFT JOIN vt_usuarios u ON u.user_numero=c.cal_idMedico
        LEFT JOIN vt_localidades l ON l.loc_id=c.cal_localidad WHERE c.cal_fecha>=@Desde AND c.cal_fecha<DATEADD(day,1,@Hasta)
        ORDER BY c.cal_fecha,c.cal_desde",new{Desde=desde.Date,Hasta=hasta.Date})).ToList();

    public async Task<bool> GuardarExcepcionAsync(ExcepcionHorarioDto x,int idUsuario)
    {
        if(x.IdMedico<=0||x.IdLocalidad<=0||x.Fecha.Date<DateTime.Today||string.IsNullOrWhiteSpace(x.Motivo)||!HoraValida(x.Desde,x.Hasta,true))return false;
        var repetido=await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM vt_calendarizacionHorariosMedicos WHERE cal_estado='A' AND cal_idMedico=@IdMedico AND cal_localidad=@IdLocalidad AND CAST(cal_fecha AS date)=CAST(@Fecha AS date) AND cal_id<>@Id AND TRY_CONVERT(time,cal_desde)<TRY_CONVERT(time,CASE WHEN @Hasta='24:00' THEN '23:59:59' ELSE @Hasta END) AND TRY_CONVERT(time,CASE WHEN cal_hasta='24:00' THEN '23:59:59' ELSE cal_hasta END)>TRY_CONVERT(time,@Desde)",x);
        if(repetido>0)return false;
        var sql=x.Id>0?"UPDATE vt_calendarizacionHorariosMedicos SET cal_idMedico=@IdMedico,cal_detalle=@Motivo,cal_fecha=@Fecha,cal_localidad=@IdLocalidad,cal_desde=@Desde,cal_hasta=@Hasta,cal_estado='A' WHERE cal_id=@Id":"INSERT INTO vt_calendarizacionHorariosMedicos(cal_idMedico,cal_detalle,cal_fecha,cal_idUsuario,cal_fechaIng,cal_localidad,cal_desde,cal_hasta,cal_estado) VALUES(@IdMedico,@Motivo,@Fecha,@Usuario,GETDATE(),@IdLocalidad,@Desde,@Hasta,'A')";
        var ok=await db.ExecuteAsync(sql,new{x.Id,x.IdMedico,x.Motivo,Fecha=x.Fecha.Date,x.IdLocalidad,x.Desde,x.Hasta,Usuario=idUsuario})>0;if(ok)await db.RegistrarBitacoraAsync($"Excepción de horario {x.Fecha:yyyy-MM-dd}: {x.Motivo}",idUsuario);return ok;
    }
    public async Task<bool> DesactivarExcepcionAsync(int id,int idUsuario){var ok=await db.ExecuteAsync("UPDATE vt_calendarizacionHorariosMedicos SET cal_estado='I' WHERE cal_id=@Id",new{Id=id})>0;if(ok)await db.RegistrarBitacoraAsync($"Desactiva excepción de horario {id}",idUsuario);return ok;}

    public async Task<List<CirugiaProgramadaDto>> ObtenerCirugiasAsync(DateTime desde,DateTime hasta)=>(await db.QueryAsync<CirugiaProgramadaDto>(@"
        SELECT sec Id,codmed IdMedico,RTRIM(ISNULL(nommedico,'')) Medico,nhcl Nhcl,RTRIM(ISNULL(nompaciente,'')) Paciente,
        codprocedimiento IdProcedimiento,RTRIM(ISNULL(nomprocedimiento,'')) Procedimiento,fechacirugia Fecha,RTRIM(horainicio) HoraInicio,
        RTRIM(horafinalizacion) HoraFin,localidad IdLocalidad,estado Estado FROM qvt_calendario_cirugias
        WHERE fechacirugia>=@Desde AND fechacirugia<DATEADD(day,1,@Hasta) ORDER BY fechacirugia,horainicio",new{Desde=desde.Date,Hasta=hasta.Date})).ToList();

    public async Task<bool> GuardarCirugiaAsync(CirugiaProgramadaDto x,int idUsuario)
    {
        if(x.IdMedico<=0||x.Nhcl<=0||x.IdProcedimiento<=0||x.IdLocalidad<=0||x.Fecha.Date<DateTime.Today||!HoraValida(x.HoraInicio,x.HoraFin))return false;
        var conflicto=await db.QueryFirstOrDefaultAsync<int>(@"SELECT COUNT(1) FROM qvt_calendario_cirugias WHERE estado='A' AND sec<>@Id AND CAST(fechacirugia AS date)=CAST(@Fecha AS date) AND (codmed=@IdMedico OR localidad=@IdLocalidad) AND CAST(horainicio AS time)<CAST(@HoraFin AS time) AND CAST(horafinalizacion AS time)>CAST(@HoraInicio AS time)",x);
        var bloqueo=await db.QueryFirstOrDefaultAsync<int>(@"SELECT COUNT(1) FROM vt_calendarizacionHorariosMedicos WHERE cal_estado='A' AND cal_idMedico=@IdMedico AND cal_localidad=@IdLocalidad AND CAST(cal_fecha AS date)=CAST(@Fecha AS date) AND TRY_CONVERT(time,cal_desde)<TRY_CONVERT(time,@HoraFin) AND TRY_CONVERT(time,CASE WHEN cal_hasta='24:00' THEN '23:59:59' ELSE cal_hasta END)>TRY_CONVERT(time,@HoraInicio)",x);
        if(conflicto>0||bloqueo>0)return false;
        var sql=x.Id>0?@"UPDATE q SET codmed=@IdMedico,nhcl=@Nhcl,codprocedimiento=@IdProcedimiento,fechacirugia=@Fecha,horainicio=@HoraInicio,horafinalizacion=@HoraFin,localidad=@IdLocalidad,nommedico=u.user_nombre,nompaciente=p.mas_nombre,nomprocedimiento=pr.prod_descripcion,idcliente=p.mas_idcliente FROM qvt_calendario_cirugias q INNER JOIN vt_usuarios u ON u.user_numero=@IdMedico INNER JOIN vt_pacientes p ON p.mas_nhcl=@Nhcl INNER JOIN vt_productos pr ON pr.prod_id=@IdProcedimiento WHERE q.sec=@Id":@"INSERT INTO qvt_calendario_cirugias(codmed,nhcl,codprocedimiento,idcliente,fechacirugia,horainicio,horafinalizacion,usering,localidad,nommedico,nompaciente,nomprocedimiento,estado) SELECT @IdMedico,@Nhcl,@IdProcedimiento,p.mas_idcliente,@Fecha,@HoraInicio,@HoraFin,@Usuario,@IdLocalidad,u.user_nombre,p.mas_nombre,pr.prod_descripcion,'A' FROM vt_pacientes p CROSS JOIN vt_usuarios u CROSS JOIN vt_productos pr WHERE p.mas_nhcl=@Nhcl AND u.user_numero=@IdMedico AND pr.prod_id=@IdProcedimiento";
        var ok=await db.ExecuteAsync(sql,new{x.Id,x.IdMedico,x.Nhcl,x.IdProcedimiento,x.Fecha,x.HoraInicio,x.HoraFin,x.IdLocalidad,Usuario=idUsuario})>0;if(ok)await db.RegistrarBitacoraAsync($"Programación de cirugía NHCL {x.Nhcl}",idUsuario);return ok;
    }
    public async Task<bool> CancelarCirugiaAsync(int id,int idUsuario){var ok=await db.ExecuteAsync("UPDATE qvt_calendario_cirugias SET estado='I',fechaelimina=GETDATE(),quienelimina=@Usuario WHERE sec=@Id AND estado='A'",new{Id=id,Usuario=idUsuario})>0;if(ok)await db.RegistrarBitacoraAsync($"Cancela cirugía programada {id}",idUsuario);return ok;}

    public async Task<List<SelectorClinicoDto>> ObtenerMedicosAsync()=>(await db.QueryAsync<SelectorClinicoDto>("SELECT CONVERT(int,user_numero) Id,RTRIM(user_nombre) Descripcion,CONVERT(int,user_esp_numero) IdRelacionado FROM vt_usuarios WHERE user_estado='A' AND user_tipouser_numero=2 ORDER BY user_nombre")).ToList();
    public async Task<List<SelectorClinicoDto>> ObtenerLocalidadesSelectorAsync()=>(await db.QueryAsync<SelectorClinicoDto>("SELECT CONVERT(int,loc_id) Id,RTRIM(loc_descripcion) Descripcion FROM vt_localidades WHERE loc_estado='A' ORDER BY loc_descripcion")).ToList();
    public async Task<List<SelectorClinicoDto>> BuscarPacientesAsync(string filtro)=>(await db.QueryAsync<SelectorClinicoDto>("SELECT TOP 100 mas_nhcl Id,RTRIM(mas_nombre)+' · NHCL '+CAST(mas_nhcl AS varchar(20)) Descripcion,mas_idcliente IdRelacionado FROM vt_pacientes WHERE mas_estado='A' AND (@Filtro='' OR mas_nombre LIKE '%'+@Filtro+'%' OR CAST(mas_nhcl AS varchar(20)) LIKE '%'+@Filtro+'%') ORDER BY mas_nombre",new{Filtro=filtro?.Trim()??""})).ToList();
    public async Task<List<SelectorClinicoDto>> BuscarProcedimientosAsync(string filtro)=>(await db.QueryAsync<SelectorClinicoDto>("SELECT TOP 100 prod_id Id,RTRIM(prod_descripcion) Descripcion,prod_grupo IdRelacionado FROM vt_productos WHERE prod_estado='A' AND (@Filtro='' OR prod_descripcion LIKE '%'+@Filtro+'%' OR CAST(prod_id AS varchar(20)) LIKE '%'+@Filtro+'%') ORDER BY prod_descripcion",new{Filtro=filtro?.Trim()??""})).ToList();

    private static bool HoraValida(string desde,string hasta,bool admiteFinDia=false)
    {
        if(admiteFinDia&&hasta=="24:00")hasta="23:59:59";
        return TimeSpan.TryParse(desde,out var d)&&TimeSpan.TryParse(hasta,out var h)&&d<h;
    }

    private static (string Select,string Insert,string Update,string Existe) SqlCatalogo(string tipo)=>tipo switch{
        "diagnosticos"=>("SELECT c.cie_codigo Id,RTRIM(c.cie_diagnostico) Descripcion,c.cie_grupodiag IdGrupo,RTRIM(ISNULL(g.vt_grupodes,'')) Grupo,RTRIM(ISNULL(c.cie_tipo,'')) Tipo,'A' Estado FROM vt_cie10 c LEFT JOIN vt_grupos_diagnosticos g ON g.vt_grupodiag=c.cie_grupodiag ORDER BY c.cie_diagnostico","INSERT INTO vt_cie10(cie_diagnostico,cie_grupodiag,cie_tipo) VALUES(@Descripcion,@IdGrupo,@Tipo)","UPDATE vt_cie10 SET cie_diagnostico=@Descripcion,cie_grupodiag=@IdGrupo,cie_tipo=@Tipo WHERE cie_codigo=@Id","SELECT COUNT(1) FROM vt_cie10 WHERE UPPER(LTRIM(RTRIM(cie_diagnostico)))=UPPER(LTRIM(RTRIM(@Descripcion))) AND cie_codigo<>@Id"),
        "productos"=>("SELECT p.prod_id Id,RTRIM(p.prod_descripcion) Descripcion,p.prod_grupo IdGrupo,RTRIM(ISNULL(g.des_grupoprod,'')) Grupo,RTRIM(ISNULL(p.prod_tipo,'')) Tipo,p.prod_estado Estado FROM vt_productos p LEFT JOIN vt_grupos_productos g ON g.id_grupoprod=p.prod_grupo ORDER BY p.prod_descripcion","INSERT INTO vt_productos(prod_descripcion,prod_grupo,prod_estado,prod_tipo) VALUES(@Descripcion,@IdGrupo,@Estado,@Tipo)","UPDATE vt_productos SET prod_descripcion=@Descripcion,prod_grupo=@IdGrupo,prod_estado=@Estado,prod_tipo=@Tipo WHERE prod_id=@Id","SELECT COUNT(1) FROM vt_productos WHERE UPPER(LTRIM(RTRIM(prod_descripcion)))=UPPER(LTRIM(RTRIM(@Descripcion))) AND prod_id<>@Id"),
        "laboratorio"=>("SELECT p.lab_id Id,RTRIM(p.lab_descripcion) Descripcion,p.lab_grupo IdGrupo,RTRIM(ISNULL(g.grupo_descripcion,'')) Grupo,'' Tipo,ISNULL(p.estado,'A') Estado FROM vt_pruebas_laboratorio p LEFT JOIN vt_grupos_laboratorio g ON g.grupo_id=p.lab_grupo ORDER BY p.lab_descripcion","INSERT INTO vt_pruebas_laboratorio(lab_descripcion,lab_grupo,estado) VALUES(@Descripcion,@IdGrupo,@Estado)","UPDATE vt_pruebas_laboratorio SET lab_descripcion=@Descripcion,lab_grupo=@IdGrupo,estado=@Estado WHERE lab_id=@Id","SELECT COUNT(1) FROM vt_pruebas_laboratorio WHERE UPPER(LTRIM(RTRIM(lab_descripcion)))=UPPER(LTRIM(RTRIM(@Descripcion))) AND lab_id<>@Id"),
        "grupos-diagnosticos"=>("SELECT vt_grupodiag Id,RTRIM(vt_grupodes) Descripcion,'A' Estado FROM vt_grupos_diagnosticos ORDER BY vt_grupodes","INSERT INTO vt_grupos_diagnosticos(vt_grupodes) VALUES(@Descripcion)","UPDATE vt_grupos_diagnosticos SET vt_grupodes=@Descripcion WHERE vt_grupodiag=@Id","SELECT COUNT(1) FROM vt_grupos_diagnosticos WHERE UPPER(LTRIM(RTRIM(vt_grupodes)))=UPPER(LTRIM(RTRIM(@Descripcion))) AND vt_grupodiag<>@Id"),
        "grupos-productos"=>("SELECT id_grupoprod Id,RTRIM(des_grupoprod) Descripcion,grupoprod_estado Estado FROM vt_grupos_productos ORDER BY des_grupoprod","INSERT INTO vt_grupos_productos(des_grupoprod,grupoprod_estado) VALUES(@Descripcion,@Estado)","UPDATE vt_grupos_productos SET des_grupoprod=@Descripcion,grupoprod_estado=@Estado WHERE id_grupoprod=@Id","SELECT COUNT(1) FROM vt_grupos_productos WHERE UPPER(LTRIM(RTRIM(des_grupoprod)))=UPPER(LTRIM(RTRIM(@Descripcion))) AND id_grupoprod<>@Id"),
        "grupos-laboratorio"=>("SELECT grupo_id Id,RTRIM(grupo_descripcion) Descripcion,estado Estado FROM vt_grupos_laboratorio ORDER BY grupo_descripcion","INSERT INTO vt_grupos_laboratorio(grupo_descripcion,estado) VALUES(@Descripcion,@Estado)","UPDATE vt_grupos_laboratorio SET grupo_descripcion=@Descripcion,estado=@Estado WHERE grupo_id=@Id","SELECT COUNT(1) FROM vt_grupos_laboratorio WHERE UPPER(LTRIM(RTRIM(grupo_descripcion)))=UPPER(LTRIM(RTRIM(@Descripcion))) AND grupo_id<>@Id"),
        _=>throw new ArgumentException("Catálogo no soportado.",nameof(tipo))};
    public async Task<List<CatalogoOperativoDto>> ObtenerCatalogoAsync(string tipo)
    {
        if (tipo == "productos") return await eFactCatalog.ObtenerProductosAsync();
        if (tipo == "grupos-productos") return await eFactCatalog.ObtenerCategoriasAsync();
        if (tipo == "subgrupos-productos") return await eFactCatalog.ObtenerSubcategoriasAsync();
        var q=SqlCatalogo(tipo);return(await db.QueryAsync<CatalogoOperativoDto>(q.Select)).ToList();
    }
    public async Task<bool> GuardarCatalogoAsync(string tipo,CatalogoOperativoDto x,int idUsuario)
    {
        if (tipo == "productos") return await eFactCatalog.GuardarProductoAsync(x);
        if (tipo == "grupos-productos") return await eFactCatalog.GuardarCategoriaAsync(x);
        if (tipo == "subgrupos-productos") return await eFactCatalog.GuardarSubcategoriaAsync(x);
        if(string.IsNullOrWhiteSpace(x.Descripcion))return false;x.Descripcion=x.Descripcion.Trim();var q=SqlCatalogo(tipo);if(await db.QueryFirstOrDefaultAsync<int>(q.Existe,x)>0)return false;var esGrupo=tipo.StartsWith("grupos-",StringComparison.Ordinal);if(!esGrupo&&!x.IdGrupo.HasValue)return false;var ok=await db.ExecuteAsync(x.Id>0?q.Update:q.Insert,x)>0;if(ok)await db.RegistrarBitacoraAsync($"Catálogo {tipo}: {x.Descripcion}",idUsuario);return ok;
    }
    public async Task<List<BitacoraDto>> ObtenerBitacoraAsync(DateTime desde,DateTime hasta,string filtro)=>(await db.QueryAsync<BitacoraDto>(@"
        SELECT v.vit_id Id,v.vit_idFechaMod Fecha,ISNULL(v.vit_idUser,0) IdUsuario,RTRIM(ISNULL(u.user_nombre,'')) Usuario,
        RTRIM(ISNULL(v.vit_detalle,'')) Detalle,RTRIM(ISNULL(v.ip,'')) Ip,RTRIM(ISNULL(v.hostname,'')) Equipo
        FROM vt_vitacora v LEFT JOIN vt_usuarios u ON u.user_numero=v.vit_idUser
        WHERE v.vit_idFechaMod>=@Desde AND v.vit_idFechaMod<DATEADD(day,1,@Hasta) AND (@Filtro='' OR v.vit_detalle LIKE '%'+@Filtro+'%' OR u.user_nombre LIKE '%'+@Filtro+'%') ORDER BY v.vit_idFechaMod DESC",new{Desde=desde.Date,Hasta=hasta.Date,Filtro=filtro??""})).ToList();
}
