using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IFichasClinicasComplementariasService
{
    Task InicializarAsync();
    Task<List<PasoTurnoDto>> ObtenerPasosTurnoAsync(int idOrden, int nhcl);
    Task GuardarPasoTurnoAsync(PasoTurnoDto dto, int idUsuario);
    Task EliminarPasoTurnoAsync(int id, int idOrden);
    Task<List<InterconsultaHospitalariaDto>> ObtenerInterconsultasAsync(int idOrden, int nhcl);
    Task GuardarInterconsultaAsync(InterconsultaHospitalariaDto dto, int idUsuario);
    Task EliminarInterconsultaAsync(int id, int idOrden);
    Task<List<TransfusionDto>> ObtenerTransfusionesAsync(int idOrden, int nhcl);
    Task<int> GuardarTransfusionAsync(TransfusionDto dto, int idUsuario);
    Task AgregarMonitoreoAsync(TransfusionMonitoreoDto dto, int idOrden, int idUsuario);
    Task EliminarTransfusionAsync(int id, int idOrden);
}

public sealed class FichasClinicasComplementariasService : IFichasClinicasComplementariasService
{
    private readonly ISqlDataService _db;
    private bool _inicializado;

    public FichasClinicasComplementariasService(ISqlDataService db) => _db = db;

    public async Task InicializarAsync()
    {
        if (_inicializado) return;
        const string sql = @"
IF OBJECT_ID(N'dbo.clin_paso_turno', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.clin_paso_turno(
  id int IDENTITY PRIMARY KEY, id_orden int NOT NULL, nhcl int NOT NULL, fecha datetime2 NOT NULL,
  turno nvarchar(20) NOT NULL, subjetivo nvarchar(max) NULL, objetivo nvarchar(max) NULL,
  analisis nvarchar(max) NULL, plan_clinico nvarchar(max) NULL, examenes_realizados nvarchar(max) NULL,
  pendientes nvarchar(max) NULL, alertas nvarchar(max) NULL, entrega nvarchar(150) NULL,
  recibe nvarchar(150) NULL, id_usuario int NOT NULL, fecha_registro datetime2 NOT NULL DEFAULT SYSDATETIME());
 CREATE INDEX IX_clin_paso_turno_orden ON dbo.clin_paso_turno(id_orden, fecha DESC);
END;
IF OBJECT_ID(N'dbo.clin_interconsulta', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.clin_interconsulta(
  id int IDENTITY PRIMARY KEY, id_orden int NOT NULL, nhcl int NOT NULL, fecha_solicitud datetime2 NOT NULL,
  fecha_agendada datetime2 NULL, tipo nvarchar(30) NOT NULL, detalle nvarchar(max) NOT NULL,
  equipo_laboratorio nvarchar(200) NULL, especialista nvarchar(200) NULL, medico_solicitante nvarchar(200) NULL,
  estado nvarchar(30) NOT NULL, resultado_resumen nvarchar(max) NULL, registrado_historia bit NOT NULL DEFAULT 0,
  id_usuario int NOT NULL, fecha_registro datetime2 NOT NULL DEFAULT SYSDATETIME());
 CREATE INDEX IX_clin_interconsulta_orden ON dbo.clin_interconsulta(id_orden, fecha_solicitud DESC);
END;
IF OBJECT_ID(N'dbo.clin_transfusion', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.clin_transfusion(
  id int IDENTITY PRIMARY KEY, id_orden int NOT NULL, nhcl int NOT NULL, fecha datetime2 NOT NULL,
  producto nvarchar(60) NOT NULL, identificador_unidad nvarchar(100) NULL, fecha_vencimiento date NULL,
  grupo_receptor nvarchar(50) NULL, grupo_donante nvarchar(50) NULL, tipificacion_verificada bit NOT NULL DEFAULT 0,
  crossmatch_mayor bit NOT NULL DEFAULT 0, crossmatch_menor bit NOT NULL DEFAULT 0,
  consentimiento_confirmado bit NOT NULL DEFAULT 0, volumen_prescrito_ml decimal(12,2) NOT NULL DEFAULT 0,
  volumen_administrado_ml decimal(12,2) NOT NULL DEFAULT 0, velocidad_ml_hora decimal(12,2) NOT NULL DEFAULT 0,
  hora_inicio datetime2 NULL, hora_fin datetime2 NULL, diagnostico_indicacion nvarchar(max) NULL,
  medico_responsable nvarchar(200) NULL, doble_verificacion nvarchar(250) NULL,
  estado nvarchar(30) NOT NULL, observaciones nvarchar(max) NULL, id_usuario int NOT NULL,
  fecha_registro datetime2 NOT NULL DEFAULT SYSDATETIME());
 CREATE INDEX IX_clin_transfusion_orden ON dbo.clin_transfusion(id_orden, fecha DESC);
END;
IF OBJECT_ID(N'dbo.clin_transfusion_monitoreo', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.clin_transfusion_monitoreo(
  id int IDENTITY PRIMARY KEY, id_transfusion int NOT NULL, fecha_hora datetime2 NOT NULL,
  momento nvarchar(30) NOT NULL, temperatura decimal(5,2) NULL, frecuencia_cardiaca int NULL,
  frecuencia_respiratoria int NULL, pas int NULL, pad int NULL, pam int NULL,
  mucosas nvarchar(100) NULL, estado_conciencia nvarchar(100) NULL, reacciones_adversas nvarchar(max) NULL,
  accion_tomada nvarchar(max) NULL, responsable nvarchar(150) NULL,
  CONSTRAINT FK_clin_transfusion_monitoreo FOREIGN KEY(id_transfusion) REFERENCES dbo.clin_transfusion(id) ON DELETE CASCADE);
 CREATE INDEX IX_clin_transfusion_monitoreo_transfusion ON dbo.clin_transfusion_monitoreo(id_transfusion, fecha_hora);
END;";
        await _db.ExecuteAsync(sql);
        _inicializado = true;
    }

    private async Task ValidarPacienteAsync(int idOrden, int nhcl)
    {
        await InicializarAsync();
        var existe = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM vt_ordenAtencion WHERE ordenAten_id=@IdOrden AND ordenAten_nhcl=@Nhcl",
            new { IdOrden = idOrden, Nhcl = nhcl });
        if (existe == 0) throw new InvalidOperationException("La orden no corresponde al paciente seleccionado.");
    }

    public async Task<List<PasoTurnoDto>> ObtenerPasosTurnoAsync(int idOrden, int nhcl)
    {
        await ValidarPacienteAsync(idOrden, nhcl);
        return (await _db.QueryAsync<PasoTurnoDto>(@"SELECT id Id,id_orden IdOrden,nhcl Nhcl,fecha Fecha,turno Turno,
subjetivo Subjetivo,objetivo Objetivo,analisis Analisis,plan_clinico [Plan],examenes_realizados ExamenesRealizados,
pendientes Pendientes,alertas Alertas,entrega Entrega,recibe Recibe,id_usuario IdUsuario
FROM clin_paso_turno WHERE id_orden=@IdOrden AND nhcl=@Nhcl ORDER BY fecha DESC", new { IdOrden = idOrden, Nhcl = nhcl })).ToList();
    }

    public async Task GuardarPasoTurnoAsync(PasoTurnoDto d, int idUsuario)
    {
        await ValidarPacienteAsync(d.IdOrden, d.Nhcl);
        if (string.IsNullOrWhiteSpace(d.Entrega) || string.IsNullOrWhiteSpace(d.Recibe))
            throw new InvalidOperationException("Registre quién entrega y quién recibe el turno.");
        if (d.Id == 0)
            await _db.ExecuteAsync(@"INSERT INTO clin_paso_turno(id_orden,nhcl,fecha,turno,subjetivo,objetivo,analisis,plan_clinico,examenes_realizados,pendientes,alertas,entrega,recibe,id_usuario)
VALUES(@IdOrden,@Nhcl,@Fecha,@Turno,@Subjetivo,@Objetivo,@Analisis,@Plan,@ExamenesRealizados,@Pendientes,@Alertas,@Entrega,@Recibe,@IdUsuario)", new { d.IdOrden,d.Nhcl,d.Fecha,d.Turno,d.Subjetivo,d.Objetivo,d.Analisis,d.Plan,d.ExamenesRealizados,d.Pendientes,d.Alertas,d.Entrega,d.Recibe,IdUsuario=idUsuario });
        else
            await _db.ExecuteAsync(@"UPDATE clin_paso_turno SET fecha=@Fecha,turno=@Turno,subjetivo=@Subjetivo,objetivo=@Objetivo,analisis=@Analisis,plan_clinico=@Plan,examenes_realizados=@ExamenesRealizados,pendientes=@Pendientes,alertas=@Alertas,entrega=@Entrega,recibe=@Recibe,id_usuario=@IdUsuario WHERE id=@Id AND id_orden=@IdOrden",
                new { d.Id,d.IdOrden,d.Fecha,d.Turno,d.Subjetivo,d.Objetivo,d.Analisis,d.Plan,d.ExamenesRealizados,d.Pendientes,d.Alertas,d.Entrega,d.Recibe,IdUsuario=idUsuario });
        await _db.RegistrarBitacoraAsync($"Paso de turno guardado Orden #{d.IdOrden}", idUsuario);
    }

    public async Task EliminarPasoTurnoAsync(int id, int idOrden) =>
        _ = await _db.ExecuteAsync("DELETE FROM clin_paso_turno WHERE id=@Id AND id_orden=@IdOrden", new { Id=id, IdOrden=idOrden });

    public async Task<List<InterconsultaHospitalariaDto>> ObtenerInterconsultasAsync(int idOrden, int nhcl)
    {
        await ValidarPacienteAsync(idOrden, nhcl);
        return (await _db.QueryAsync<InterconsultaHospitalariaDto>(@"SELECT id Id,id_orden IdOrden,nhcl Nhcl,fecha_solicitud FechaSolicitud,fecha_agendada FechaAgendada,tipo Tipo,detalle Detalle,equipo_laboratorio EquipoLaboratorio,especialista Especialista,medico_solicitante MedicoSolicitante,estado Estado,resultado_resumen ResultadoResumen,registrado_historia RegistradoHistoria,id_usuario IdUsuario FROM clin_interconsulta WHERE id_orden=@IdOrden AND nhcl=@Nhcl ORDER BY fecha_solicitud DESC", new { IdOrden=idOrden, Nhcl=nhcl })).ToList();
    }

    public async Task GuardarInterconsultaAsync(InterconsultaHospitalariaDto d, int idUsuario)
    {
        await ValidarPacienteAsync(d.IdOrden, d.Nhcl);
        if (string.IsNullOrWhiteSpace(d.Detalle)) throw new InvalidOperationException("Ingrese el examen o interconsulta solicitada.");
        if (d.Id == 0)
            await _db.ExecuteAsync(@"INSERT INTO clin_interconsulta(id_orden,nhcl,fecha_solicitud,fecha_agendada,tipo,detalle,equipo_laboratorio,especialista,medico_solicitante,estado,resultado_resumen,registrado_historia,id_usuario) VALUES(@IdOrden,@Nhcl,@FechaSolicitud,@FechaAgendada,@Tipo,@Detalle,@EquipoLaboratorio,@Especialista,@MedicoSolicitante,@Estado,@ResultadoResumen,@RegistradoHistoria,@IdUsuario)", new { d.IdOrden,d.Nhcl,d.FechaSolicitud,d.FechaAgendada,d.Tipo,d.Detalle,d.EquipoLaboratorio,d.Especialista,d.MedicoSolicitante,d.Estado,d.ResultadoResumen,d.RegistradoHistoria,IdUsuario=idUsuario });
        else
            await _db.ExecuteAsync(@"UPDATE clin_interconsulta SET fecha_agendada=@FechaAgendada,tipo=@Tipo,detalle=@Detalle,equipo_laboratorio=@EquipoLaboratorio,especialista=@Especialista,medico_solicitante=@MedicoSolicitante,estado=@Estado,resultado_resumen=@ResultadoResumen,registrado_historia=@RegistradoHistoria,id_usuario=@IdUsuario WHERE id=@Id AND id_orden=@IdOrden", new { d.Id,d.IdOrden,d.FechaAgendada,d.Tipo,d.Detalle,d.EquipoLaboratorio,d.Especialista,d.MedicoSolicitante,d.Estado,d.ResultadoResumen,d.RegistradoHistoria,IdUsuario=idUsuario });
        await _db.RegistrarBitacoraAsync($"Examen/interconsulta guardado Orden #{d.IdOrden}", idUsuario);
    }

    public async Task EliminarInterconsultaAsync(int id, int idOrden) =>
        _ = await _db.ExecuteAsync("DELETE FROM clin_interconsulta WHERE id=@Id AND id_orden=@IdOrden", new { Id=id, IdOrden=idOrden });

    public async Task<List<TransfusionDto>> ObtenerTransfusionesAsync(int idOrden, int nhcl)
    {
        await ValidarPacienteAsync(idOrden, nhcl);
        var lista = (await _db.QueryAsync<TransfusionDto>(@"SELECT id Id,id_orden IdOrden,nhcl Nhcl,fecha Fecha,producto Producto,identificador_unidad IdentificadorUnidad,fecha_vencimiento FechaVencimiento,grupo_receptor GrupoReceptor,grupo_donante GrupoDonante,tipificacion_verificada TipificacionVerificada,crossmatch_mayor CrossmatchMayor,crossmatch_menor CrossmatchMenor,consentimiento_confirmado ConsentimientoConfirmado,volumen_prescrito_ml VolumenPrescritoMl,volumen_administrado_ml VolumenAdministradoMl,velocidad_ml_hora VelocidadMlHora,hora_inicio HoraInicio,hora_fin HoraFin,diagnostico_indicacion DiagnosticoIndicacion,medico_responsable MedicoResponsable,doble_verificacion DobleVerificacion,estado Estado,observaciones Observaciones,id_usuario IdUsuario FROM clin_transfusion WHERE id_orden=@IdOrden AND nhcl=@Nhcl ORDER BY fecha DESC", new { IdOrden=idOrden, Nhcl=nhcl })).ToList();
        foreach (var t in lista)
            t.Monitoreos = (await _db.QueryAsync<TransfusionMonitoreoDto>(@"SELECT id Id,id_transfusion IdTransfusion,fecha_hora FechaHora,momento Momento,temperatura Temperatura,frecuencia_cardiaca FrecuenciaCardiaca,frecuencia_respiratoria FrecuenciaRespiratoria,pas Pas,pad Pad,pam Pam,mucosas Mucosas,estado_conciencia EstadoConciencia,reacciones_adversas ReaccionesAdversas,accion_tomada AccionTomada,responsable Responsable FROM clin_transfusion_monitoreo WHERE id_transfusion=@Id ORDER BY fecha_hora", new { t.Id })).ToList();
        return lista;
    }

    public async Task<int> GuardarTransfusionAsync(TransfusionDto d, int idUsuario)
    {
        await ValidarPacienteAsync(d.IdOrden, d.Nhcl);
        if (string.IsNullOrWhiteSpace(d.Producto) || string.IsNullOrWhiteSpace(d.IdentificadorUnidad))
            throw new InvalidOperationException("Producto e identificador de la unidad son obligatorios.");
        if (!d.ConsentimientoConfirmado || !d.TipificacionVerificada)
            throw new InvalidOperationException("Confirme consentimiento y tipificación antes de guardar la transfusión.");
        if (d.Id == 0)
            d.Id = await _db.ExecuteScalarAsync<int>(@"INSERT INTO clin_transfusion(id_orden,nhcl,fecha,producto,identificador_unidad,fecha_vencimiento,grupo_receptor,grupo_donante,tipificacion_verificada,crossmatch_mayor,crossmatch_menor,consentimiento_confirmado,volumen_prescrito_ml,volumen_administrado_ml,velocidad_ml_hora,hora_inicio,hora_fin,diagnostico_indicacion,medico_responsable,doble_verificacion,estado,observaciones,id_usuario) OUTPUT INSERTED.id VALUES(@IdOrden,@Nhcl,@Fecha,@Producto,@IdentificadorUnidad,@FechaVencimiento,@GrupoReceptor,@GrupoDonante,@TipificacionVerificada,@CrossmatchMayor,@CrossmatchMenor,@ConsentimientoConfirmado,@VolumenPrescritoMl,@VolumenAdministradoMl,@VelocidadMlHora,@HoraInicio,@HoraFin,@DiagnosticoIndicacion,@MedicoResponsable,@DobleVerificacion,@Estado,@Observaciones,@IdUsuario)", new { d.IdOrden,d.Nhcl,d.Fecha,d.Producto,d.IdentificadorUnidad,d.FechaVencimiento,d.GrupoReceptor,d.GrupoDonante,d.TipificacionVerificada,d.CrossmatchMayor,d.CrossmatchMenor,d.ConsentimientoConfirmado,d.VolumenPrescritoMl,d.VolumenAdministradoMl,d.VelocidadMlHora,d.HoraInicio,d.HoraFin,d.DiagnosticoIndicacion,d.MedicoResponsable,d.DobleVerificacion,d.Estado,d.Observaciones,IdUsuario=idUsuario });
        else
            await _db.ExecuteAsync(@"UPDATE clin_transfusion SET producto=@Producto,identificador_unidad=@IdentificadorUnidad,fecha_vencimiento=@FechaVencimiento,grupo_receptor=@GrupoReceptor,grupo_donante=@GrupoDonante,tipificacion_verificada=@TipificacionVerificada,crossmatch_mayor=@CrossmatchMayor,crossmatch_menor=@CrossmatchMenor,consentimiento_confirmado=@ConsentimientoConfirmado,volumen_prescrito_ml=@VolumenPrescritoMl,volumen_administrado_ml=@VolumenAdministradoMl,velocidad_ml_hora=@VelocidadMlHora,hora_inicio=@HoraInicio,hora_fin=@HoraFin,diagnostico_indicacion=@DiagnosticoIndicacion,medico_responsable=@MedicoResponsable,doble_verificacion=@DobleVerificacion,estado=@Estado,observaciones=@Observaciones,id_usuario=@IdUsuario WHERE id=@Id AND id_orden=@IdOrden", new { d.Id,d.IdOrden,d.Producto,d.IdentificadorUnidad,d.FechaVencimiento,d.GrupoReceptor,d.GrupoDonante,d.TipificacionVerificada,d.CrossmatchMayor,d.CrossmatchMenor,d.ConsentimientoConfirmado,d.VolumenPrescritoMl,d.VolumenAdministradoMl,d.VelocidadMlHora,d.HoraInicio,d.HoraFin,d.DiagnosticoIndicacion,d.MedicoResponsable,d.DobleVerificacion,d.Estado,d.Observaciones,IdUsuario=idUsuario });
        await _db.RegistrarBitacoraAsync($"Transfusión guardada Orden #{d.IdOrden}, ficha #{d.Id}", idUsuario);
        return d.Id;
    }

    public async Task AgregarMonitoreoAsync(TransfusionMonitoreoDto d, int idOrden, int idUsuario)
    {
        var pertenece = await _db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM clin_transfusion WHERE id=@Id AND id_orden=@IdOrden", new { Id=d.IdTransfusion, IdOrden=idOrden });
        if (pertenece == 0) throw new InvalidOperationException("La transfusión no pertenece a la orden activa.");
        await _db.ExecuteAsync(@"INSERT INTO clin_transfusion_monitoreo(id_transfusion,fecha_hora,momento,temperatura,frecuencia_cardiaca,frecuencia_respiratoria,pas,pad,pam,mucosas,estado_conciencia,reacciones_adversas,accion_tomada,responsable) VALUES(@IdTransfusion,@FechaHora,@Momento,@Temperatura,@FrecuenciaCardiaca,@FrecuenciaRespiratoria,@Pas,@Pad,@Pam,@Mucosas,@EstadoConciencia,@ReaccionesAdversas,@AccionTomada,@Responsable)", d);
        await _db.RegistrarBitacoraAsync($"Monitoreo de transfusión #{d.IdTransfusion}", idUsuario);
    }

    public async Task EliminarTransfusionAsync(int id, int idOrden) =>
        _ = await _db.ExecuteAsync("DELETE FROM clin_transfusion WHERE id=@Id AND id_orden=@IdOrden", new { Id=id, IdOrden=idOrden });
}
