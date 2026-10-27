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
    Task<List<CirugiaProgramadaDto>> ObtenerCirugiasAsync(DateTime desde, DateTime hasta);
    Task<bool> GuardarCirugiaAsync(CirugiaProgramadaDto item, int idUsuario);
    Task<List<CatalogoOperativoDto>> ObtenerCatalogoAsync(string tipo);
    Task<bool> GuardarCatalogoAsync(string tipo, CatalogoOperativoDto item, int idUsuario);
    Task<List<BitacoraDto>> ObtenerBitacoraAsync(DateTime desde, DateTime hasta, string filtro);
}

public sealed class GestionMigradaService(ISqlDataService db) : IGestionMigradaService
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
        SELECT q.id Id,q.fecha Fecha,q.nhcl Nhcl,RTRIM(ISNULL(p.mas_nombre,'')) Paciente,
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
        if(x.IdMedico<=0||x.IdLocalidad<=0||string.IsNullOrWhiteSpace(x.Desde)||string.IsNullOrWhiteSpace(x.Hasta)) return false;
        var sql=x.Id>0?"UPDATE vt_horariosMedicos SET hr_iduser=@IdMedico,hr_dia=@Dia,hr_dia2=@DiaIngles,hr_desde=@Desde,hr_hasta=@Hasta,hr_intervalo=@Intervalo,hr_localidad=@IdLocalidad WHERE hr_id=@Id":"INSERT INTO vt_horariosMedicos(hr_iduser,hr_dia,hr_dia2,hr_desde,hr_hasta,hr_intervalo,hr_localidad) VALUES(@IdMedico,@Dia,@DiaIngles,@Desde,@Hasta,@Intervalo,@IdLocalidad)";
        var ok=await db.ExecuteAsync(sql,x)>0;if(ok)await db.RegistrarBitacoraAsync($"Horario médico: {x.IdMedico} {x.Dia}",idUsuario);return ok;
    }

    public async Task<List<CirugiaProgramadaDto>> ObtenerCirugiasAsync(DateTime desde,DateTime hasta)=>(await db.QueryAsync<CirugiaProgramadaDto>(@"
        SELECT sec Id,codmed IdMedico,RTRIM(ISNULL(nommedico,'')) Medico,nhcl Nhcl,RTRIM(ISNULL(nompaciente,'')) Paciente,
        codprocedimiento IdProcedimiento,RTRIM(ISNULL(nomprocedimiento,'')) Procedimiento,fechacirugia Fecha,RTRIM(horainicio) HoraInicio,
        RTRIM(horafinalizacion) HoraFin,localidad IdLocalidad,estado Estado FROM qvt_calendario_cirugias
        WHERE fechacirugia>=@Desde AND fechacirugia<DATEADD(day,1,@Hasta) ORDER BY fechacirugia,horainicio",new{Desde=desde.Date,Hasta=hasta.Date})).ToList();

    public async Task<bool> GuardarCirugiaAsync(CirugiaProgramadaDto x,int idUsuario)
    {
        if(x.IdMedico<=0||x.Nhcl<=0||x.IdProcedimiento<=0||x.IdLocalidad<=0)return false;
        var sql=x.Id>0?"UPDATE qvt_calendario_cirugias SET codmed=@IdMedico,nhcl=@Nhcl,codprocedimiento=@IdProcedimiento,fechacirugia=@Fecha,horainicio=@HoraInicio,horafinalizacion=@HoraFin,localidad=@IdLocalidad WHERE sec=@Id":"INSERT INTO qvt_calendario_cirugias(codmed,nhcl,codprocedimiento,idcliente,fechacirugia,horainicio,horafinalizacion,usering,localidad,estado) SELECT @IdMedico,@Nhcl,@IdProcedimiento,p.mas_idcliente,@Fecha,@HoraInicio,@HoraFin,@Usuario,@IdLocalidad,'A' FROM vt_pacientes p WHERE p.mas_nhcl=@Nhcl";
        var ok=await db.ExecuteAsync(sql,new{x.Id,x.IdMedico,x.Nhcl,x.IdProcedimiento,x.Fecha,x.HoraInicio,x.HoraFin,x.IdLocalidad,Usuario=idUsuario})>0;if(ok)await db.RegistrarBitacoraAsync($"Programación de cirugía NHCL {x.Nhcl}",idUsuario);return ok;
    }

    private static (string Select,string Insert,string Update) SqlCatalogo(string tipo)=>tipo switch{
        "diagnosticos"=>("SELECT c.cie_codigo Id,RTRIM(c.cie_diagnostico) Descripcion,c.cie_grupodiag IdGrupo,RTRIM(ISNULL(g.vt_grupodes,'')) Grupo,RTRIM(ISNULL(c.cie_tipo,'')) Tipo,'A' Estado FROM vt_cie10 c LEFT JOIN vt_grupos_diagnosticos g ON g.vt_grupodiag=c.cie_grupodiag ORDER BY c.cie_diagnostico","INSERT INTO vt_cie10(cie_diagnostico,cie_grupodiag,cie_tipo) VALUES(@Descripcion,@IdGrupo,@Tipo)","UPDATE vt_cie10 SET cie_diagnostico=@Descripcion,cie_grupodiag=@IdGrupo,cie_tipo=@Tipo WHERE cie_codigo=@Id"),
        "productos"=>("SELECT p.prod_id Id,RTRIM(p.prod_descripcion) Descripcion,p.prod_grupo IdGrupo,RTRIM(ISNULL(g.des_grupoprod,'')) Grupo,RTRIM(ISNULL(p.prod_tipo,'')) Tipo,p.prod_estado Estado FROM vt_productos p LEFT JOIN vt_grupos_productos g ON g.id_grupoprod=p.prod_grupo ORDER BY p.prod_descripcion","INSERT INTO vt_productos(prod_descripcion,prod_grupo,prod_estado,prod_tipo) VALUES(@Descripcion,@IdGrupo,@Estado,@Tipo)","UPDATE vt_productos SET prod_descripcion=@Descripcion,prod_grupo=@IdGrupo,prod_estado=@Estado,prod_tipo=@Tipo WHERE prod_id=@Id"),
        _=>("SELECT p.lab_id Id,RTRIM(p.lab_descripcion) Descripcion,p.lab_grupo IdGrupo,RTRIM(ISNULL(g.grupo_descripcion,'')) Grupo,'' Tipo,ISNULL(p.estado,'A') Estado FROM vt_pruebas_laboratorio p LEFT JOIN vt_grupos_laboratorio g ON g.grupo_id=p.lab_grupo ORDER BY p.lab_descripcion","INSERT INTO vt_pruebas_laboratorio(lab_descripcion,lab_grupo,estado) VALUES(@Descripcion,@IdGrupo,@Estado)","UPDATE vt_pruebas_laboratorio SET lab_descripcion=@Descripcion,lab_grupo=@IdGrupo,estado=@Estado WHERE lab_id=@Id")};
    public async Task<List<CatalogoOperativoDto>> ObtenerCatalogoAsync(string tipo){var q=SqlCatalogo(tipo);return(await db.QueryAsync<CatalogoOperativoDto>(q.Select)).ToList();}
    public async Task<bool> GuardarCatalogoAsync(string tipo,CatalogoOperativoDto x,int idUsuario){if(string.IsNullOrWhiteSpace(x.Descripcion))return false;var q=SqlCatalogo(tipo);var ok=await db.ExecuteAsync(x.Id>0?q.Update:q.Insert,x)>0;if(ok)await db.RegistrarBitacoraAsync($"Catálogo {tipo}: {x.Descripcion}",idUsuario);return ok;}
    public async Task<List<BitacoraDto>> ObtenerBitacoraAsync(DateTime desde,DateTime hasta,string filtro)=>(await db.QueryAsync<BitacoraDto>(@"
        SELECT v.vit_id Id,v.vit_idFechaMod Fecha,ISNULL(v.vit_idUser,0) IdUsuario,RTRIM(ISNULL(u.user_nombre,'')) Usuario,
        RTRIM(ISNULL(v.vit_detalle,'')) Detalle,RTRIM(ISNULL(v.ip,'')) Ip,RTRIM(ISNULL(v.hostname,'')) Equipo
        FROM vt_vitacora v LEFT JOIN vt_usuarios u ON u.user_numero=v.vit_idUser
        WHERE v.vit_idFechaMod>=@Desde AND v.vit_idFechaMod<DATEADD(day,1,@Hasta) AND (@Filtro='' OR v.vit_detalle LIKE '%'+@Filtro+'%' OR u.user_nombre LIKE '%'+@Filtro+'%') ORDER BY v.vit_idFechaMod DESC",new{Desde=desde.Date,Hasta=hasta.Date,Filtro=filtro??""})).ToList();
}
