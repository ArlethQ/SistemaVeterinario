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
    Task<AdjuntoTransfusionDto?> ObtenerAdjuntoTransfusionAsync(int id, int idOrden, string tipo);
    Task GuardarAdjuntoTransfusionAsync(int id, int idOrden, string tipo, AdjuntoTransfusionDto adjunto);
    Task GuardarControlCuadriculaAsync(TransfusionMonitoreoDto dto, int idOrden);
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
END;
IF COL_LENGTH(N'dbo.clin_transfusion', N'conservacion') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD conservacion nvarchar(20) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'crossmatching') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD crossmatching nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'numero_historial') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD numero_historial nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'vga_inicio') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD vga_inicio decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'vga_final') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD vga_final decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'proteinas_inicio') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD proteinas_inicio decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'proteinas_final') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD proteinas_final decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'albumina_inicio') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD albumina_inicio decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'albumina_final') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD albumina_final decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'ficha_donante') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD ficha_donante nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'telefono_donante') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD telefono_donante nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'fecha_recoleccion') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD fecha_recoleccion datetime2 NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'ml_recolectados') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD ml_recolectados decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'vga_donante') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD vga_donante decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'pt_donante') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD pt_donante decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'albumina_donante') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD albumina_donante decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion_monitoreo', N'tllc') IS NULL
 ALTER TABLE dbo.clin_transfusion_monitoreo ADD tllc decimal(12,3) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion_monitoreo', N'otros') IS NULL
 ALTER TABLE dbo.clin_transfusion_monitoreo ADD otros nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'ConsentimientoNombre') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD ConsentimientoNombre nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'ConsentimientoContenido') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD ConsentimientoContenido varbinary(max) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'TipificacionNombre') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD TipificacionNombre nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'TipificacionContenido') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD TipificacionContenido varbinary(max) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'PruebaMayorNombre') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD PruebaMayorNombre nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'PruebaMayorContenido') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD PruebaMayorContenido varbinary(max) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'PruebaMenorNombre') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD PruebaMenorNombre nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.clin_transfusion', N'PruebaMenorContenido') IS NULL
 ALTER TABLE dbo.clin_transfusion ADD PruebaMenorContenido varbinary(max) NULL;";
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
        var lista = (await _db.QueryAsync<TransfusionDto>(@"SELECT id Id,id_orden IdOrden,nhcl Nhcl,fecha Fecha,producto Producto,identificador_unidad IdentificadorUnidad,fecha_vencimiento FechaVencimiento,grupo_receptor GrupoReceptor,grupo_donante GrupoDonante,tipificacion_verificada TipificacionVerificada,crossmatch_mayor CrossmatchMayor,crossmatch_menor CrossmatchMenor,consentimiento_confirmado ConsentimientoConfirmado,volumen_prescrito_ml VolumenPrescritoMl,volumen_administrado_ml VolumenAdministradoMl,velocidad_ml_hora VelocidadMlHora,hora_inicio HoraInicio,hora_fin HoraFin,diagnostico_indicacion DiagnosticoIndicacion,medico_responsable MedicoResponsable,doble_verificacion DobleVerificacion,estado Estado,observaciones Observaciones,id_usuario IdUsuario,conservacion Conservacion,crossmatching Crossmatching,numero_historial NumeroHistorial,vga_inicio VgaInicio,vga_final VgaFinal,proteinas_inicio ProteinasInicio,proteinas_final ProteinasFinal,albumina_inicio AlbuminaInicio,albumina_final AlbuminaFinal,ficha_donante FichaDonante,telefono_donante TelefonoDonante,fecha_recoleccion FechaRecoleccion,ml_recolectados MlRecolectados,vga_donante VgaDonante,pt_donante PtDonante,albumina_donante AlbuminaDonante,ConsentimientoNombre,TipificacionNombre,PruebaMayorNombre,PruebaMenorNombre FROM clin_transfusion WHERE id_orden=@IdOrden AND nhcl=@Nhcl ORDER BY fecha DESC", new { IdOrden=idOrden, Nhcl=nhcl })).ToList();
        foreach (var t in lista)
            t.Monitoreos = (await _db.QueryAsync<TransfusionMonitoreoDto>(@"SELECT id Id,id_transfusion IdTransfusion,fecha_hora FechaHora,momento Momento,temperatura Temperatura,frecuencia_cardiaca FrecuenciaCardiaca,frecuencia_respiratoria FrecuenciaRespiratoria,pas Pas,pad Pad,pam Pam,mucosas Mucosas,estado_conciencia EstadoConciencia,reacciones_adversas ReaccionesAdversas,accion_tomada AccionTomada,responsable Responsable,tllc Tllc,otros Otros FROM clin_transfusion_monitoreo WHERE id_transfusion=@Id ORDER BY fecha_hora", new { t.Id })).ToList();
        return lista;
    }

    public async Task<int> GuardarTransfusionAsync(TransfusionDto d, int idUsuario)
    {
        await ValidarPacienteAsync(d.IdOrden, d.Nhcl);
        if (string.IsNullOrWhiteSpace(d.Producto) || string.IsNullOrWhiteSpace(d.IdentificadorUnidad))
            throw new InvalidOperationException("Producto e identificador de la unidad son obligatorios.");
        if ((!d.ConsentimientoConfirmado && string.IsNullOrEmpty(d.ConsentimientoNombre) && !d.AdjuntosPendientes.ContainsKey("Consentimiento")) ||
            (!d.TipificacionVerificada && string.IsNullOrEmpty(d.TipificacionNombre) && !d.AdjuntosPendientes.ContainsKey("Tipificacion")))
            throw new InvalidOperationException("Adjunte el consentimiento y la tipificación antes de guardar.");
        if (d.Id == 0)
            d.Id = await _db.ExecuteScalarAsync<int>(@"INSERT INTO clin_transfusion(id_orden,nhcl,fecha,producto,identificador_unidad,fecha_vencimiento,grupo_receptor,grupo_donante,tipificacion_verificada,crossmatch_mayor,crossmatch_menor,consentimiento_confirmado,volumen_prescrito_ml,volumen_administrado_ml,velocidad_ml_hora,hora_inicio,hora_fin,diagnostico_indicacion,medico_responsable,doble_verificacion,estado,observaciones,id_usuario,conservacion,crossmatching,numero_historial,vga_inicio,vga_final,proteinas_inicio,proteinas_final,albumina_inicio,albumina_final,ficha_donante,telefono_donante,fecha_recoleccion,ml_recolectados,vga_donante,pt_donante,albumina_donante,ConsentimientoNombre,ConsentimientoContenido,TipificacionNombre,TipificacionContenido,PruebaMayorNombre,PruebaMayorContenido,PruebaMenorNombre,PruebaMenorContenido) OUTPUT INSERTED.id VALUES(@IdOrden,@Nhcl,@Fecha,@Producto,@IdentificadorUnidad,@FechaVencimiento,@GrupoReceptor,@GrupoDonante,@TipificacionVerificada,@CrossmatchMayor,@CrossmatchMenor,@ConsentimientoConfirmado,@VolumenPrescritoMl,@VolumenAdministradoMl,@VelocidadMlHora,@HoraInicio,@HoraFin,@DiagnosticoIndicacion,@MedicoResponsable,@DobleVerificacion,@Estado,@Observaciones,@IdUsuario,@Conservacion,@Crossmatching,@NumeroHistorial,@VgaInicio,@VgaFinal,@ProteinasInicio,@ProteinasFinal,@AlbuminaInicio,@AlbuminaFinal,@FichaDonante,@TelefonoDonante,@FechaRecoleccion,@MlRecolectados,@VgaDonante,@PtDonante,@AlbuminaDonante,@ConsentimientoNombre,@ConsentimientoContenido,@TipificacionNombre,@TipificacionContenido,@PruebaMayorNombre,@PruebaMayorContenido,@PruebaMenorNombre,@PruebaMenorContenido)", new { d.IdOrden,d.Nhcl,d.Fecha,d.Producto,d.IdentificadorUnidad,d.FechaVencimiento,d.GrupoReceptor,d.GrupoDonante,d.TipificacionVerificada,d.CrossmatchMayor,d.CrossmatchMenor,d.ConsentimientoConfirmado,d.VolumenPrescritoMl,d.VolumenAdministradoMl,d.VelocidadMlHora,d.HoraInicio,d.HoraFin,d.DiagnosticoIndicacion,d.MedicoResponsable,d.DobleVerificacion,d.Estado,d.Observaciones,d.Conservacion,d.Crossmatching,d.NumeroHistorial,d.VgaInicio,d.VgaFinal,d.ProteinasInicio,d.ProteinasFinal,d.AlbuminaInicio,d.AlbuminaFinal,d.FichaDonante,d.TelefonoDonante,d.FechaRecoleccion,d.MlRecolectados,d.VgaDonante,d.PtDonante,d.AlbuminaDonante,ConsentimientoNombre=d.AdjuntosPendientes.GetValueOrDefault("Consentimiento")?.Nombre,ConsentimientoContenido=d.AdjuntosPendientes.GetValueOrDefault("Consentimiento")?.Contenido,TipificacionNombre=d.AdjuntosPendientes.GetValueOrDefault("Tipificacion")?.Nombre,TipificacionContenido=d.AdjuntosPendientes.GetValueOrDefault("Tipificacion")?.Contenido,PruebaMayorNombre=d.AdjuntosPendientes.GetValueOrDefault("PruebaMayor")?.Nombre,PruebaMayorContenido=d.AdjuntosPendientes.GetValueOrDefault("PruebaMayor")?.Contenido,PruebaMenorNombre=d.AdjuntosPendientes.GetValueOrDefault("PruebaMenor")?.Nombre,PruebaMenorContenido=d.AdjuntosPendientes.GetValueOrDefault("PruebaMenor")?.Contenido,IdUsuario=idUsuario });
        else
            await _db.ExecuteAsync(@"UPDATE clin_transfusion SET fecha=@Fecha,conservacion=@Conservacion,crossmatching=@Crossmatching,numero_historial=@NumeroHistorial,vga_inicio=@VgaInicio,vga_final=@VgaFinal,proteinas_inicio=@ProteinasInicio,proteinas_final=@ProteinasFinal,albumina_inicio=@AlbuminaInicio,albumina_final=@AlbuminaFinal,ficha_donante=@FichaDonante,telefono_donante=@TelefonoDonante,fecha_recoleccion=@FechaRecoleccion,ml_recolectados=@MlRecolectados,vga_donante=@VgaDonante,pt_donante=@PtDonante,albumina_donante=@AlbuminaDonante,ConsentimientoNombre=COALESCE(@ConsentimientoNombre,ConsentimientoNombre),ConsentimientoContenido=COALESCE(@ConsentimientoContenido,ConsentimientoContenido),TipificacionNombre=COALESCE(@TipificacionNombre,TipificacionNombre),TipificacionContenido=COALESCE(@TipificacionContenido,TipificacionContenido),PruebaMayorNombre=COALESCE(@PruebaMayorNombre,PruebaMayorNombre),PruebaMayorContenido=COALESCE(@PruebaMayorContenido,PruebaMayorContenido),PruebaMenorNombre=COALESCE(@PruebaMenorNombre,PruebaMenorNombre),PruebaMenorContenido=COALESCE(@PruebaMenorContenido,PruebaMenorContenido),producto=@Producto,identificador_unidad=@IdentificadorUnidad,fecha_vencimiento=@FechaVencimiento,grupo_receptor=@GrupoReceptor,grupo_donante=@GrupoDonante,tipificacion_verificada=@TipificacionVerificada,crossmatch_mayor=@CrossmatchMayor,crossmatch_menor=@CrossmatchMenor,consentimiento_confirmado=@ConsentimientoConfirmado,volumen_prescrito_ml=@VolumenPrescritoMl,volumen_administrado_ml=@VolumenAdministradoMl,velocidad_ml_hora=@VelocidadMlHora,hora_inicio=@HoraInicio,hora_fin=@HoraFin,diagnostico_indicacion=@DiagnosticoIndicacion,medico_responsable=@MedicoResponsable,doble_verificacion=@DobleVerificacion,estado=@Estado,observaciones=@Observaciones,id_usuario=@IdUsuario WHERE id=@Id AND id_orden=@IdOrden", new { d.Id,d.IdOrden,d.Fecha,d.Producto,d.IdentificadorUnidad,d.FechaVencimiento,d.GrupoReceptor,d.GrupoDonante,d.TipificacionVerificada,d.CrossmatchMayor,d.CrossmatchMenor,d.ConsentimientoConfirmado,d.VolumenPrescritoMl,d.VolumenAdministradoMl,d.VelocidadMlHora,d.HoraInicio,d.HoraFin,d.DiagnosticoIndicacion,d.MedicoResponsable,d.DobleVerificacion,d.Estado,d.Observaciones,d.Conservacion,d.Crossmatching,d.NumeroHistorial,d.VgaInicio,d.VgaFinal,d.ProteinasInicio,d.ProteinasFinal,d.AlbuminaInicio,d.AlbuminaFinal,d.FichaDonante,d.TelefonoDonante,d.FechaRecoleccion,d.MlRecolectados,d.VgaDonante,d.PtDonante,d.AlbuminaDonante,ConsentimientoNombre=d.AdjuntosPendientes.GetValueOrDefault("Consentimiento")?.Nombre,ConsentimientoContenido=d.AdjuntosPendientes.GetValueOrDefault("Consentimiento")?.Contenido,TipificacionNombre=d.AdjuntosPendientes.GetValueOrDefault("Tipificacion")?.Nombre,TipificacionContenido=d.AdjuntosPendientes.GetValueOrDefault("Tipificacion")?.Contenido,PruebaMayorNombre=d.AdjuntosPendientes.GetValueOrDefault("PruebaMayor")?.Nombre,PruebaMayorContenido=d.AdjuntosPendientes.GetValueOrDefault("PruebaMayor")?.Contenido,PruebaMenorNombre=d.AdjuntosPendientes.GetValueOrDefault("PruebaMenor")?.Nombre,PruebaMenorContenido=d.AdjuntosPendientes.GetValueOrDefault("PruebaMenor")?.Contenido,IdUsuario=idUsuario });
        await _db.RegistrarBitacoraAsync($"Transfusión guardada Orden #{d.IdOrden}, ficha #{d.Id}", idUsuario);
        return d.Id;
    }

    public async Task<AdjuntoTransfusionDto?> ObtenerAdjuntoTransfusionAsync(int id, int idOrden, string tipo)
    {
        await InicializarAsync();
        var columna = ColumnaAdjunto(tipo);
        return (await _db.QueryAsync<AdjuntoTransfusionDto>(
            $"SELECT {columna}Nombre Nombre, {columna}Contenido Contenido FROM clin_transfusion WHERE id=@Id AND id_orden=@IdOrden AND {columna}Contenido IS NOT NULL",
            new { Id = id, IdOrden = idOrden })).SingleOrDefault();
    }

    public async Task GuardarAdjuntoTransfusionAsync(int id, int idOrden, string tipo, AdjuntoTransfusionDto adjunto)
    {
        await InicializarAsync();
        var columna = ColumnaAdjunto(tipo);
        var filas = await _db.ExecuteAsync(
            $"UPDATE dbo.clin_transfusion SET {columna}Nombre=@Nombre, {columna}Contenido=@Contenido WHERE id=@Id AND id_orden=@IdOrden",
            new { Id = id, IdOrden = idOrden, adjunto.Nombre, adjunto.Contenido });
        if (filas == 0)
            throw new InvalidOperationException("La transfusión ya no está disponible en esta orden.");
    }

    private static string ColumnaAdjunto(string tipo) => tipo switch
        {
            "Consentimiento" => "Consentimiento",
            "Tipificacion" => "Tipificacion",
            "PruebaMayor" => "PruebaMayor",
            "PruebaMenor" => "PruebaMenor",
            _ => throw new ArgumentException("Tipo de adjunto inválido.", nameof(tipo))
        };

    public async Task GuardarControlCuadriculaAsync(TransfusionMonitoreoDto d, int idOrden)
    {
        if (d.Id == 0)
        {
            d.Id = await _db.ExecuteScalarAsync<int>(@"
INSERT INTO dbo.clin_transfusion_monitoreo(id_transfusion,fecha_hora,momento,temperatura,frecuencia_cardiaca,
 frecuencia_respiratoria,pas,pad,pam,mucosas,estado_conciencia,tllc,otros)
OUTPUT INSERTED.id
SELECT id,@FechaHora,@Momento,@Temperatura,@FrecuenciaCardiaca,@FrecuenciaRespiratoria,
 @Pas,@Pad,@Pam,@Mucosas,@EstadoConciencia,@Tllc,@Otros
FROM dbo.clin_transfusion WHERE id=@IdTransfusion AND id_orden=@IdOrden", new
            {
                d.IdTransfusion, IdOrden = idOrden, d.FechaHora, d.Momento, d.Temperatura, d.FrecuenciaCardiaca,
                d.FrecuenciaRespiratoria, d.Pas, d.Pad, d.Pam, d.Mucosas, d.EstadoConciencia, d.Tllc, d.Otros
            });
            if (d.Id == 0) throw new InvalidOperationException("No se encontró la transfusión en la orden.");
            return;
        }

        var filas = await _db.ExecuteAsync(@"
UPDATE m SET temperatura=@Temperatura, frecuencia_cardiaca=@FrecuenciaCardiaca,
 frecuencia_respiratoria=@FrecuenciaRespiratoria, pas=@Pas, pad=@Pad, pam=@Pam,
 mucosas=@Mucosas, estado_conciencia=@EstadoConciencia, tllc=@Tllc, otros=@Otros
FROM dbo.clin_transfusion_monitoreo m
JOIN dbo.clin_transfusion t ON t.id=m.id_transfusion
WHERE m.id=@Id AND t.id=@IdTransfusion AND t.id_orden=@IdOrden", new
        {
            d.Id, d.IdTransfusion, IdOrden = idOrden, d.Temperatura, d.FrecuenciaCardiaca,
            d.FrecuenciaRespiratoria, d.Pas, d.Pad, d.Pam, d.Mucosas, d.EstadoConciencia, d.Tllc, d.Otros
        });
        if (filas == 0) throw new InvalidOperationException("El control ya no está disponible en esta orden.");
    }

    public async Task AgregarMonitoreoAsync(TransfusionMonitoreoDto d, int idOrden, int idUsuario)
    {
        var pertenece = await _db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM clin_transfusion WHERE id=@Id AND id_orden=@IdOrden", new { Id=d.IdTransfusion, IdOrden=idOrden });
        if (pertenece == 0) throw new InvalidOperationException("La transfusión no pertenece a la orden activa.");
        await _db.ExecuteAsync(@"INSERT INTO clin_transfusion_monitoreo(id_transfusion,fecha_hora,momento,temperatura,frecuencia_cardiaca,frecuencia_respiratoria,pas,pad,pam,mucosas,estado_conciencia,reacciones_adversas,accion_tomada,responsable,tllc,otros) VALUES(@IdTransfusion,@FechaHora,@Momento,@Temperatura,@FrecuenciaCardiaca,@FrecuenciaRespiratoria,@Pas,@Pad,@Pam,@Mucosas,@EstadoConciencia,@ReaccionesAdversas,@AccionTomada,@Responsable,@Tllc,@Otros)", d);
        await _db.RegistrarBitacoraAsync($"Monitoreo de transfusión #{d.IdTransfusion}", idUsuario);
    }

    public async Task EliminarTransfusionAsync(int id, int idOrden) =>
        _ = await _db.ExecuteAsync("DELETE FROM clin_transfusion WHERE id=@Id AND id_orden=@IdOrden", new { Id=id, IdOrden=idOrden });
}
