using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IConsultaExternaService
{
    // 1. Ficha Médica / Historia Clínica SOAP (vt_fichamedica)
    Task<FichaMedicaCompletaDto> ObtenerFichaMedicaAsync(int idOrden, int nhcl);
    Task<bool> GuardarFichaMedicaAsync(FichaMedicaCompletaDto dto, int idUsuario);

    // 2. Diagnósticos CIE-10 (vt_diagnosticos + vt_cie10)
    Task<List<CatalogoCie10ItemDto>> BuscarCatalogoCie10Async(string filtro);
    Task<List<DiagnosticoCie10Dto>> ObtenerDiagnosticosOrdenAsync(int idOrden);
    Task<bool> AgregarDiagnosticoOrdenAsync(DiagnosticoCie10Dto diag);
    Task<bool> EliminarDiagnosticoOrdenAsync(int sec);

    // 3. Antecedentes y Vacunas (vt_antecedentes + vt_vacunas)
    Task<List<AntecedenteClinicoDto>> ObtenerAntecedentesPacienteAsync(int nhcl);
    Task<bool> AgregarAntecedenteAsync(AntecedenteClinicoDto dto, int idUsuario, int idCliente);
    Task<List<VacunaPacienteDto>> ObtenerVacunasPacienteAsync(int nhcl);
    Task<bool> AgregarVacunaAsync(VacunaPacienteDto dto, int idUsuario, int idCliente);
    Task<bool> EliminarVacunaAsync(int vacunaId, int idUsuario);

    // 4. Evolución Médica y Órdenes Asociadas (vt_EvolucionMedico + vt_labOrdenes + vt_rxOrdenes)
    Task<List<EvolucionMedicaDto>> ObtenerEvolucionesPacienteAsync(int nhcl, int? idOrdenFiltro = null);
    Task<int> GuardarEvolucionMedicaAsync(EvolucionMedicaDto dto, int idUsuario);
    Task<bool> EliminarEvolucionMedicaAsync(int idEvolucion);
    Task<List<OrdenExamenEvolucionDto>> ObtenerOrdenesExamenAsync(int idOrden, int? idEvolucion = null);
    Task<bool> AgregarOrdenExamenAsync(OrdenExamenEvolucionDto orden);
    Task<bool> EliminarOrdenExamenAsync(int sec, string tipoCategoria);

    // 5. Recetario Médico e Indicaciones Generales (vt_medicinas + vt_IndicacionesGenerales)
    Task<List<RecetaMedicamentoItemDto>> ObtenerRecetaOrdenAsync(int idOrden);
    Task<string> ObtenerIndicacionesGeneralesAsync(int idOrden);
    Task<bool> AgregarMedicamentoRecetaAsync(RecetaMedicamentoItemDto item, int idUsuario, int idLocalidad);
    Task<bool> EliminarMedicamentoRecetaAsync(int sec);
    Task<bool> GuardarIndicacionesGeneralesAsync(int idOrden, int nhcl, string indicaciones);
    Task<List<RecetaResumenListadoDto>> BuscarRecetasPorFechasAsync(DateTime desde, DateTime hasta, int? nhcl = null);

    // 6. Autorizaciones / Consentimientos y Alta Médica (vt_autorizaciones + vt_formatos + qvt_formulario_alta)
    Task<List<FormatoPlantillaDto>> ObtenerPlantillasFormatosAsync(string? tipo = null);
    Task<string> ProcesarPlantillaConDatosPacienteAsync(int formId, int nhcl);
    Task<List<AutorizacionClinicaDto>> ObtenerAutorizacionesPacienteAsync(int nhcl);
    Task<bool> GuardarAutorizacionAsync(AutorizacionClinicaDto dto);
    Task<FormularioAltaMedicaDto> ObtenerFormularioAltaAsync(int idOrden, int nhcl);
    Task<bool> GuardarFormularioAltaAsync(FormularioAltaMedicaDto dto, int idUsuario);
}

public class ConsultaExternaService : IConsultaExternaService
{
    private readonly ISqlDataService _db;
    private readonly IBusinessCentralService _bcService;
    private readonly ILogger<ConsultaExternaService> _logger;

    public ConsultaExternaService(
        ISqlDataService db,
        IBusinessCentralService bcService,
        ILogger<ConsultaExternaService> logger)
    {
        _db = db;
        _bcService = bcService;
        _logger = logger;
    }

    // ========================================================================
    // 1. FICHA MÉDICA / HISTORIA CLÍNICA SOAP (Reemplaza vt_hisDatMedicos.aspx)
    // ========================================================================
    public async Task<FichaMedicaCompletaDto> ObtenerFichaMedicaAsync(int idOrden, int nhcl)
    {
        var dto = new FichaMedicaCompletaDto { MedOrden = idOrden, MedNhcl = nhcl };

        try
        {
            const string sql = @"
                SELECT TOP 1
                    sec AS Sec,
                    ISNULL(med_orden, @IdOrden) AS MedOrden,
                    ISNULL(med_nhcl, @Nhcl) AS MedNhcl,
                    ISNULL(med_iduser, 0) AS MedIdUser,
                    ISNULL(med_cliente, 0) AS MedCliente,
                    ISNULL(med_especialidad, 1) AS MedEspecialidad,
                    ISNULL(med_edadanios, 0) AS MedEdadAnios,
                    ISNULL(med_edadmeses, 0) AS MedEdadMeses,
                    ISNULL(med_localidad, 1) AS MedLocalidad,
                    ISNULL(med_fecha, GETDATE()) AS MedFecha,
                    ISNULL(med_tipo, 'HDEV') AS MedTipo,
                    ISNULL(med_especie, '') AS MedEspecie,
                    ISNULL(med_tipoconsulta, 'Consulta Externa') AS MedTipoConsulta,
                    ISNULL(med_motivo, '') AS MedMotivo,
                    ISNULL(med_anamnesis, '') AS MedAnamnesis,
                    ISNULL(med_antecedente, '') AS MedAntecedente,
                    ISNULL(med_objetivo, '') AS MedObjetivo,
                    ISNULL(med_analisis, '') AS MedAnalisis,
                    ISNULL(med_plan, '') AS MedPlan,
                    ISNULL(tratamiento, '') AS Tratamiento,
                    ISNULL(observacionesyrecomendaciones, '') AS ObservacionesYRecomendaciones,
                    ISNULL(med_apuntes, '') AS MedApuntes,
                    ISNULL(peso, 0) AS Peso,
                    ISNULL(temperatura, 38.5) AS Temperatura,
                    ISNULL(frecuenciacardiaca, 90) AS FrecuenciaCardiaca,
                    ISNULL(frecuenciarespiratoria, 24) AS FrecuenciaRespiratoria,
                    ISNULL(mucosas, 'Rosadas') AS Mucosas,
                    ISNULL(tiempollenadocapilar, 2) AS TiempoLlenadoCapilar,
                    ISNULL(deshidratacion, 0) AS Deshidratacion,
                    ISNULL(reflejotusigeno, 'Negativo') AS ReflejoTusigeno,
                    ISNULL(auscultacion, '') AS Auscultacion,
                    ISNULL(palpaciontiroides, '') AS PalpacionTiroides,
                    ISNULL(indicecondicioncorporal, 5) AS IndiceCondicionCorporal,
                    ISNULL(cp, 'Normal') AS Cp,
                    ISNULL(linfonodos, '') AS Linfonodos,
                    ISNULL(palpacionabdominal, '') AS PalpacionAbdominal,
                    ISNULL(pielyanexos, '') AS PielYAnexos,
                    ISNULL(cavidadoral, '') AS CavidadOral,
                    ISNULL(otros, '') AS Otros,
                    ISNULL(temperamento, 'TRANQUILO') AS Temperamento
                FROM vt_fichamedica
                WHERE med_orden = @IdOrden
                ORDER BY sec DESC";

            var existente = await _db.QueryFirstOrDefaultAsync<FichaMedicaCompletaDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl });
            if (existente != null)
            {
                dto = existente;
            }

            // Cargar triaje de temperamento y castrado desde vt_pacientes
            var pac = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT mas_colortriaje, mas_castrado FROM vt_pacientes WHERE mas_nhcl = @Nhcl", new { Nhcl = nhcl });
            if (pac != null)
            {
                dto.ColorTriaje = pac.mas_colortriaje?.ToString()?.Trim() ?? "imagenes/temperamento/blanco.png";
                dto.Esterilizado = pac.mas_castrado?.ToString()?.Trim() ?? "N";
            }

            dto.IndicacionesGeneralesReceta = await ObtenerIndicacionesGeneralesAsync(idOrden);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Aviso al obtener Ficha Médica para Orden {IdOrden}", idOrden);
        }

        return dto;
    }

    public async Task<bool> GuardarFichaMedicaAsync(FichaMedicaCompletaDto dto, int idUsuario)
    {
        try
        {
            dto.MedIdUser = idUsuario;

            var existeSec = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 sec FROM vt_fichamedica WHERE med_orden = @MedOrden",
                new { dto.MedOrden });

            if (existeSec.HasValue && existeSec.Value > 0)
            {
                dto.Sec = existeSec.Value;
                const string sqlUpdate = @"
                    UPDATE vt_fichamedica SET
                        med_tipoconsulta = @MedTipoConsulta,
                        med_motivo = @MedMotivo,
                        med_anamnesis = @MedAnamnesis,
                        med_antecedente = @MedAntecedente,
                        med_objetivo = @MedObjetivo,
                        med_analisis = @MedAnalisis,
                        med_plan = @MedPlan,
                        peso = @Peso,
                        temperatura = @Temperatura,
                        frecuenciacardiaca = @FrecuenciaCardiaca,
                        frecuenciarespiratoria = @FrecuenciaRespiratoria,
                        mucosas = @Mucosas,
                        tiempollenadocapilar = @TiempoLlenadoCapilar,
                        deshidratacion = @Deshidratacion,
                        reflejotusigeno = @ReflejoTusigeno,
                        auscultacion = @Auscultacion,
                        palpaciontiroides = @PalpacionTiroides,
                        indicecondicioncorporal = @IndiceCondicionCorporal,
                        cp = @Cp,
                        linfonodos = @Linfonodos,
                        palpacionabdominal = @PalpacionAbdominal,
                        pielyanexos = @PielYAnexos,
                        cavidadoral = @CavidadOral,
                        otros = @Otros,
                        tratamiento = @Tratamiento,
                        observacionesyrecomendaciones = @ObservacionesYRecomendaciones,
                        med_apuntes = @MedApuntes,
                        temperamento = @Temperamento,
                        med_idmodifica = @MedIdUser,
                        med_fechamodifica = GETDATE()
                    WHERE sec = @Sec";

                await _db.ExecuteAsync(sqlUpdate, dto);
            }
            else
            {
                const string sqlInsert = @"
                    INSERT INTO vt_fichamedica (
                        med_nhcl, med_orden, med_fecha, med_iduser, med_tipo, med_cliente,
                        med_especialidad, med_edadanios, med_edadmeses, med_localidad, med_especie,
                        med_tipoconsulta, med_motivo, med_anamnesis, med_antecedente,
                        med_objetivo, med_analisis, med_plan,
                        peso, temperatura, frecuenciacardiaca, frecuenciarespiratoria,
                        mucosas, tiempollenadocapilar, deshidratacion, reflejotusigeno,
                        auscultacion, palpaciontiroides, indicecondicioncorporal, cp,
                        linfonodos, palpacionabdominal, pielyanexos, cavidadoral, otros,
                        tratamiento, observacionesyrecomendaciones, med_apuntes, temperamento, med_estado
                    ) VALUES (
                        @MedNhcl, @MedOrden, GETDATE(), @MedIdUser, @MedTipo, @MedCliente,
                        @MedEspecialidad, @MedEdadAnios, @MedEdadMeses, @MedLocalidad, @MedEspecie,
                        @MedTipoConsulta, @MedMotivo, @MedAnamnesis, @MedAntecedente,
                        @MedObjetivo, @MedAnalisis, @MedPlan,
                        @Peso, @Temperatura, @FrecuenciaCardiaca, @FrecuenciaRespiratoria,
                        @Mucosas, @TiempoLlenadoCapilar, @Deshidratacion, @ReflejoTusigeno,
                        @Auscultacion, @PalpacionTiroides, @IndiceCondicionCorporal, @Cp,
                        @Linfonodos, @PalpacionAbdominal, @PielYAnexos, @CavidadOral, @Otros,
                        @Tratamiento, @ObservacionesYRecomendaciones, @MedApuntes, @Temperamento, 'A'
                    )";

                await _db.ExecuteAsync(sqlInsert, dto);
            }

            // Actualizar color de triaje y esterilizado en vt_pacientes
            await _db.ExecuteAsync(
                "UPDATE vt_pacientes SET mas_castrado = @Esterilizado, mas_colortriaje = @ColorTriaje WHERE mas_nhcl = @MedNhcl",
                new { dto.Esterilizado, dto.ColorTriaje, dto.MedNhcl });

            // Actualizar indicaciones generales de receta si se ingresaron
            if (!string.IsNullOrWhiteSpace(dto.IndicacionesGeneralesReceta))
            {
                await GuardarIndicacionesGeneralesAsync(dto.MedOrden, dto.MedNhcl, dto.IndicacionesGeneralesReceta);
            }

            await _db.RegistrarBitacoraAsync($"Ficha Médica SOAP guardada Orden #{dto.MedOrden} NHCL #{dto.MedNhcl}", idUsuario);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar Ficha Médica para Orden {MedOrden}", dto.MedOrden);
            return false;
        }
    }

    // ========================================================================
    // 2. DIAGNÓSTICOS CIE-10 (vt_diagnosticos + vt_cie10)
    // ========================================================================
    public async Task<List<CatalogoCie10ItemDto>> BuscarCatalogoCie10Async(string filtro)
    {
        const string sql = @"
            SELECT TOP 35
                c.cie_codigo AS CieCodigo,
                RTRIM(ISNULL(c.cie_diagnostico, '')) AS CieDiagnostico,
                ISNULL(c.cie_grupodiag, 0) AS CieGrupoDiag,
                RTRIM(ISNULL(g.vt_grupodes, 'General')) AS GrupoDescripcion
            FROM vt_cie10 c
            LEFT JOIN vt_grupos_diagnosticos g ON c.cie_grupodiag = g.vt_grupodiag
            WHERE (@Filtro = '' OR c.cie_diagnostico LIKE '%' + @Filtro + '%' OR CAST(c.cie_codigo AS VARCHAR(20)) LIKE '%' + @Filtro + '%')
            ORDER BY c.cie_diagnostico ASC";

        var lista = await _db.QueryAsync<CatalogoCie10ItemDto>(sql, new { Filtro = filtro?.Trim() ?? "" });
        return lista.ToList();
    }

    public async Task<List<DiagnosticoCie10Dto>> ObtenerDiagnosticosOrdenAsync(int idOrden)
    {
        const string sql = @"
            SELECT
                d.sec AS Sec,
                CAST(ISNULL(d.dig_nhcl, 0) AS INT) AS Nhcl,
                ISNULL(d.dig_id_orden, 0) AS IdOrden,
                RTRIM(ISNULL(d.dig_codigoDiag, '')) AS CodigoDiag,
                RTRIM(ISNULL(c.cie_diagnostico, ISNULL(CAST(d.dig_descripcion AS VARCHAR(MAX)), ''))) AS NombreCie10,
                RTRIM(ISNULL(CAST(d.dig_descripcion AS VARCHAR(MAX)), '')) AS DescripcionObservacion,
                RTRIM(ISNULL(d.dig_tipo, 'Definitivo')) AS TipoDiag
            FROM vt_diagnosticos d
            LEFT JOIN vt_cie10 c ON CAST(c.cie_codigo AS VARCHAR(20)) = RTRIM(d.dig_codigoDiag)
            WHERE d.dig_id_orden = @IdOrden
            ORDER BY d.sec ASC";

        var lista = await _db.QueryAsync<DiagnosticoCie10Dto>(sql, new { IdOrden = idOrden });
        return lista.ToList();
    }

    public async Task<bool> AgregarDiagnosticoOrdenAsync(DiagnosticoCie10Dto diag)
    {
        const string sql = @"
            INSERT INTO vt_diagnosticos (dig_nhcl, dig_codigoDiag, dig_id_orden, dig_descripcion, dig_tipo)
            VALUES (@Nhcl, @CodigoDiag, @IdOrden, @DescripcionObservacion, @TipoDiag)";

        await _db.ExecuteAsync(sql, diag);
        return true;
    }

    public async Task<bool> EliminarDiagnosticoOrdenAsync(int sec)
    {
        await _db.ExecuteAsync("DELETE FROM vt_diagnosticos WHERE sec = @Sec", new { Sec = sec });
        return true;
    }

    // ========================================================================
    // 3. ANTECEDENTES Y VACUNAS (vt_antecedentes + vt_vacunas)
    // ========================================================================
    public async Task<List<AntecedenteClinicoDto>> ObtenerAntecedentesPacienteAsync(int nhcl)
    {
        const string sql = @"
            SELECT
                a.antecedente_id AS AntecedenteId,
                ISNULL(a.antecedente_nhcl, 0) AS Nhcl,
                ISNULL(a.antecedente_orden, 0) AS Orden,
                ISNULL(a.antecedente_fecha, GETDATE()) AS Fecha,
                ISNULL(a.antecedente_descripcion, '') AS Descripcion,
                ISNULL(a.alimentacion, '') AS Alimentacion,
                ISNULL(a.cuales_mascotas, '') AS ContactoMascotas,
                ISNULL(u.user_nombre, 'Médico') AS NombreMedico
            FROM vt_antecedentes a
            LEFT JOIN vt_usuarios u ON a.antecedente_iduser = u.user_numero
            WHERE a.antecedente_nhcl = @Nhcl AND ISNULL(a.antecedente_estado, 'A') = 'A'
            ORDER BY a.antecedente_id DESC";

        var lista = await _db.QueryAsync<AntecedenteClinicoDto>(sql, new { Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<bool> AgregarAntecedenteAsync(AntecedenteClinicoDto dto, int idUsuario, int idCliente)
    {
        const string sql = @"
            INSERT INTO vt_antecedentes (
                antecedente_nhcl, antecedente_fecha, antecedente_iduser, antecedente_cliente,
                antecedente_estado, antecedente_descripcion, antecedente_orden, alimentacion, cuales_mascotas
            ) VALUES (
                @Nhcl, GETDATE(), @IdUser, @IdCliente,
                'A', @Descripcion, @Orden, @Alimentacion, @ContactoMascotas
            )";

        await _db.ExecuteAsync(sql, new
        {
            dto.Nhcl,
            IdUser = idUsuario,
            IdCliente = idCliente,
            dto.Descripcion,
            dto.Orden,
            dto.Alimentacion,
            dto.ContactoMascotas
        });
        return true;
    }

    public async Task<List<VacunaPacienteDto>> ObtenerVacunasPacienteAsync(int nhcl)
    {
        const string sql = @"
            SELECT
                v.vacuna_id AS VacunaId,
                ISNULL(v.vacuna_nhcl, 0) AS Nhcl,
                ISNULL(v.vacuna_orden, 0) AS Orden,
                ISNULL(v.vacuna_fecha, GETDATE()) AS Fecha,
                v.vacuna_revacunacion AS FechaRevacunacion,
                ISNULL(v.vacuna_observacion, '') AS ObservacionVacuna,
                ISNULL(v.vacuna_tipo, 'Vacunación') AS Tipo,
                ISNULL(u.user_nombre, 'Médico') AS NombreMedico
            FROM vt_vacunas v
            LEFT JOIN vt_usuarios u ON v.vacuna_iduser = u.user_numero
            WHERE v.vacuna_nhcl = @Nhcl AND ISNULL(v.vacuna_estado, 'A') = 'A'
            ORDER BY v.vacuna_id DESC";

        var lista = await _db.QueryAsync<VacunaPacienteDto>(sql, new { Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<bool> AgregarVacunaAsync(VacunaPacienteDto dto, int idUsuario, int idCliente)
    {
        const string sql = @"
            INSERT INTO vt_vacunas (
                vacuna_nhcl, vacuna_fecha, vacuna_revacunacion, vacuna_iduser,
                vacuna_cliente, vacuna_estado, vacuna_observacion, vacuna_orden, vacuna_tipo
            ) VALUES (
                @Nhcl, @Fecha, @FechaRevacunacion, @IdUser,
                @IdCliente, 'A', @ObservacionVacuna, @Orden, @Tipo
            )";

        await _db.ExecuteAsync(sql, new
        {
            dto.Nhcl,
            dto.Fecha,
            dto.FechaRevacunacion,
            IdUser = idUsuario,
            IdCliente = idCliente,
            dto.ObservacionVacuna,
            dto.Orden,
            dto.Tipo
        });
        return true;
    }

    public async Task<bool> EliminarVacunaAsync(int vacunaId, int idUsuario)
    {
        await _db.ExecuteAsync(
            "UPDATE vt_vacunas SET vacuna_estado = 'I', vacuna_elimina = @IdUser WHERE vacuna_id = @Id",
            new { Id = vacunaId, IdUser = idUsuario });
        return true;
    }

    // ========================================================================
    // 4. EVOLUCIONES MÉDICAS Y ÓRDENES ASOCIADAS (vt_EvolucionMedico)
    // ========================================================================
    public async Task<List<EvolucionMedicaDto>> ObtenerEvolucionesPacienteAsync(int nhcl, int? idOrdenFiltro = null)
    {
        const string sql = @"
            SELECT
                e.idEvolucion AS IdEvolucion,
                ISNULL(e.idOrden, 0) AS IdOrden,
                ISNULL(e.nhcl, 0) AS Nhcl,
                ISNULL(e.fechaRegistro, GETDATE()) AS FechaRegistro,
                ISNULL(e.fecha, GETDATE()) AS Fecha,
                ISNULL(e.hora, '') AS Hora,
                ISNULL(e.descripcion, '') AS Descripcion,
                ISNULL(e.idEscogido, 0) AS IdEscogido,
                ISNULL(e.idUser, 0) AS IdUser,
                ISNULL(u.user_nombre, 'Médico Tratante') AS NombreMedico,
                ISNULL(e.estadoEvolucion, 'A') AS EstadoEvolucion
            FROM vt_EvolucionMedico e
            LEFT JOIN vt_usuarios u ON ISNULL(e.idEscogido, e.idUser) = u.user_numero
            WHERE e.nhcl = @Nhcl
              AND ISNULL(e.estadoEvolucion, 'A') = 'A'
              AND (@IdOrden IS NULL OR e.idOrden = @IdOrden)
            ORDER BY e.idEvolucion DESC";

        var evoluciones = (await _db.QueryAsync<EvolucionMedicaDto>(sql, new { Nhcl = nhcl, IdOrden = idOrdenFiltro })).ToList();

        foreach (var ev in evoluciones)
        {
            var examenes = await ObtenerOrdenesExamenAsync(ev.IdOrden, ev.IdEvolucion);
            ev.ExamenesLabAsociados = examenes.Where(x => x.TipoCategoria == "LAB").ToList();
            ev.ExamenesImagenAsociados = examenes.Where(x => x.TipoCategoria == "RX").ToList();
        }

        return evoluciones;
    }

    public async Task<int> GuardarEvolucionMedicaAsync(EvolucionMedicaDto dto, int idUsuario)
    {
        dto.IdUser = idUsuario;
        if (dto.IdEscogido <= 0) dto.IdEscogido = idUsuario;

        if (dto.IdEvolucion > 0)
        {
            const string sqlUpdate = @"
                UPDATE vt_EvolucionMedico SET
                    fecha = @Fecha, hora = @Hora, descripcion = @Descripcion,
                    idEscogido = @IdEscogido, idUser = @IdUser
                WHERE idEvolucion = @IdEvolucion";
            await _db.ExecuteAsync(sqlUpdate, dto);
            return dto.IdEvolucion;
        }
        else
        {
            const string sqlInsert = @"
                INSERT INTO vt_EvolucionMedico (
                    idOrden, nhcl, fechaRegistro, fecha, hora, descripcion, idEscogido, idUser, estadoEvolucion
                ) VALUES (
                    @IdOrden, @Nhcl, GETDATE(), @Fecha, @Hora, @Descripcion, @IdEscogido, @IdUser, 'A'
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int idCreado = await _db.ExecuteScalarAsync<int>(sqlInsert, dto);
            await _db.RegistrarBitacoraAsync($"Nueva Evolución Médica #{idCreado} Orden #{dto.IdOrden}", idUsuario);
            return idCreado;
        }
    }

    public async Task<bool> EliminarEvolucionMedicaAsync(int idEvolucion)
    {
        await _db.ExecuteAsync(
            "UPDATE vt_EvolucionMedico SET estadoEvolucion = 'I' WHERE idEvolucion = @Id",
            new { Id = idEvolucion });
        return true;
    }

    public async Task<List<OrdenExamenEvolucionDto>> ObtenerOrdenesExamenAsync(int idOrden, int? idEvolucion = null)
    {
        var resultado = new List<OrdenExamenEvolucionDto>();

        const string sqlLab = @"
            SELECT
                l.sec AS Sec,
                ISNULL(l.labO_id_orden, 0) AS IdOrden,
                ISNULL(l.labO_nhcl, 0) AS Nhcl,
                ISNULL(l.labO_evolucion, 0) AS IdEvolucion,
                CAST(ISNULL(l.labO_idProdu, 0) AS VARCHAR(50)) AS CodigoProd,
                ISNULL(pl.lab_descripcion, ISNULL(l.labO_observaciones, 'Examen Laboratorio')) AS DescripcionExamen,
                ISNULL(l.labO_observaciones, '') AS Observaciones,
                ISNULL(l.labO_cantidad, 1) AS Cantidad,
                CAST(15.00 AS DECIMAL(18,2)) AS Precio,
                ISNULL(l.labO_estado, 'Por Facturar') AS Estado,
                ISNULL(l.labO_fecha, GETDATE()) AS Fecha,
                'LAB' AS TipoCategoria
            FROM vt_labOrdenes l
            LEFT JOIN vt_pruebas_laboratorio pl ON l.labO_idProdu = pl.lab_id
            WHERE l.labO_id_orden = @IdOrden
              AND (@IdEvolucion IS NULL OR ISNULL(l.labO_evolucion, 0) = @IdEvolucion)";

        const string sqlRx = @"
            SELECT
                r.sec AS Sec,
                ISNULL(r.rxO_id_orden, 0) AS IdOrden,
                CAST(ISNULL(r.rxO_nhcl, 0) AS INT) AS Nhcl,
                ISNULL(r.rxO_evolucion, 0) AS IdEvolucion,
                CAST(CAST(ISNULL(r.rxO_idProdu, 0) AS INT) AS VARCHAR(50)) AS CodigoProd,
                ISNULL(pr.prod_descripcion, ISNULL(r.rxO_observaciones, 'Estudio Imagen')) AS DescripcionExamen,
                ISNULL(r.rxO_observaciones, '') AS Observaciones,
                ISNULL(r.rxO_cantidad, 1) AS Cantidad,
                CAST(35.00 AS DECIMAL(18,2)) AS Precio,
                ISNULL(r.rxO_estado, 'Por Facturar') AS Estado,
                ISNULL(r.rxO_fecha, GETDATE()) AS Fecha,
                'RX' AS TipoCategoria
            FROM vt_rxOrdenes r
            LEFT JOIN vt_productos pr ON r.rxO_idProdu = pr.prod_id
            WHERE r.rxO_id_orden = @IdOrden
              AND (@IdEvolucion IS NULL OR ISNULL(r.rxO_evolucion, 0) = @IdEvolucion)";

        try
        {
            var labs = await _db.QueryAsync<OrdenExamenEvolucionDto>(sqlLab, new { IdOrden = idOrden, IdEvolucion = idEvolucion });
            resultado.AddRange(labs);
        }
        catch { }

        try
        {
            var rxs = await _db.QueryAsync<OrdenExamenEvolucionDto>(sqlRx, new { IdOrden = idOrden, IdEvolucion = idEvolucion });
            resultado.AddRange(rxs);
        }
        catch { }

        return resultado;
    }

    public async Task<bool> AgregarOrdenExamenAsync(OrdenExamenEvolucionDto orden)
    {
        if (orden.TipoCategoria == "LAB")
        {
            const string sql = @"
                INSERT INTO vt_labOrdenes (
                    labO_nhcl, labO_idProdu, labO_observaciones, labO_cantidad,
                    labO_id_orden, labO_estado, labO_fecha, labO_evolucion
                ) VALUES (
                    @Nhcl, @CodigoProd, @Observaciones, @Cantidad,
                    @IdOrden, 'Por Facturar', GETDATE(), @IdEvolucion
                )";
            await _db.ExecuteAsync(sql, new
            {
                orden.Nhcl,
                CodigoProd = int.TryParse(orden.CodigoProd, out var c) ? c : 1,
                Observaciones = $"{orden.DescripcionExamen} - {orden.Observaciones}".Trim(' ', '-'),
                orden.Cantidad,
                orden.IdOrden,
                orden.IdEvolucion
            });
        }
        else
        {
            const string sql = @"
                INSERT INTO vt_rxOrdenes (
                    rxO_nhcl, rxO_idProdu, rxO_observaciones, rxO_cantidad,
                    rxO_id_orden, rxO_estado, rxO_fecha, rxO_evolucion
                ) VALUES (
                    @Nhcl, @CodigoProd, @Observaciones, @Cantidad,
                    @IdOrden, 'Por Facturar', GETDATE(), @IdEvolucion
                )";
            await _db.ExecuteAsync(sql, new
            {
                orden.Nhcl,
                CodigoProd = int.TryParse(orden.CodigoProd, out var c) ? c : 1,
                Observaciones = $"{orden.DescripcionExamen} - {orden.Observaciones}".Trim(' ', '-'),
                orden.Cantidad,
                orden.IdOrden,
                orden.IdEvolucion
            });
        }
        return true;
    }

    public async Task<bool> EliminarOrdenExamenAsync(int sec, string tipoCategoria)
    {
        string tabla = tipoCategoria == "LAB" ? "vt_labOrdenes" : "vt_rxOrdenes";
        await _db.ExecuteAsync($"DELETE FROM {tabla} WHERE sec = @Sec", new { Sec = sec });
        return true;
    }

    // ========================================================================
    // 5. RECETARIO MÉDICO E INDICACIONES GENERALES (vt_medicinas + vt_IndicacionesGenerales)
    // ========================================================================
    public async Task<List<RecetaMedicamentoItemDto>> ObtenerRecetaOrdenAsync(int idOrden)
    {
        const string sql = @"
            SELECT
                sec AS Sec,
                ISNULL(med_id_orden, 0) AS IdOrden,
                ISNULL(med_nhcl, 0) AS Nhcl,
                CAST(ISNULL(med_idProdu, '') AS VARCHAR(50)) AS CodigoProd,
                ISNULL(med_otroMedicamento, '') AS Descripcionamento,
                ISNULL(med_cantidad, 1) AS Cantidad,
                ISNULL(med_cantidadLetras, '') AS CantidadLetras,
                ISNULL(med_indicaciones, '') AS Indicaciones,
                ISNULL(med_observaciones, '') AS Observaciones,
                ISNULL(med_fecha, GETDATE()) AS Fecha,
                ISNULL(med_tipo, 'M') AS Tipo
            FROM vt_medicinas
            WHERE med_id_orden = @IdOrden
            ORDER BY sec ASC";

        var lista = await _db.QueryAsync<RecetaMedicamentoItemDto>(sql, new { IdOrden = idOrden });
        return lista.ToList();
    }

    public async Task<string> ObtenerIndicacionesGeneralesAsync(int idOrden)
    {
        var detalle = await _db.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 CAST(indicag_detalle AS VARCHAR(MAX)) FROM vt_IndicacionesGenerales WHERE indicag_id_orden = @IdOrden ORDER BY sec DESC",
            new { IdOrden = idOrden });
        return detalle ?? string.Empty;
    }

    public async Task<bool> AgregarMedicamentoRecetaAsync(RecetaMedicamentoItemDto item, int idUsuario, int idLocalidad)
    {
        const string sql = @"
            INSERT INTO vt_medicinas (
                med_nhcl, med_idProdu, med_indicaciones, med_cantidad, med_id_orden,
                med_otroMedicamento, med_estado, med_cantidad1, med_cantidadLetras,
                med_fecha, med_tipo, med_observaciones
            ) VALUES (
                @Nhcl, @CodigoNum, @Indicaciones, @Cantidad, @IdOrden,
                @Descripcionamento, 'Por Entregar', @Cantidad, @CantidadLetras,
                GETDATE(), @Tipo, @Observaciones
            )";

        await _db.ExecuteAsync(sql, new
        {
            item.Nhcl,
            CodigoNum = int.TryParse(item.CodigoProd, out var cod) ? cod : 0,
            item.Indicaciones,
            item.Cantidad,
            item.IdOrden,
            item.Descripcionamento,
            item.CantidadLetras,
            item.Tipo,
            item.Observaciones
        });

        return true;
    }

    public async Task<bool> EliminarMedicamentoRecetaAsync(int sec)
    {
        await _db.ExecuteAsync("DELETE FROM vt_medicinas WHERE sec = @Sec", new { Sec = sec });
        return true;
    }

    public async Task<bool> GuardarIndicacionesGeneralesAsync(int idOrden, int nhcl, string indicaciones)
    {
        await _db.ExecuteAsync("DELETE FROM vt_IndicacionesGenerales WHERE indicag_id_orden = @IdOrden", new { IdOrden = idOrden });
        if (!string.IsNullOrWhiteSpace(indicaciones))
        {
            await _db.ExecuteAsync(@"
                INSERT INTO vt_IndicacionesGenerales (indicag_nhcl, indicag_detalle, indicag_id_orden)
                VALUES (@Nhcl, @Detalle, @IdOrden)",
                new { Nhcl = nhcl, Detalle = indicaciones.Trim(), IdOrden = idOrden });
        }
        return true;
    }

    public async Task<List<RecetaResumenListadoDto>> BuscarRecetasPorFechasAsync(DateTime desde, DateTime hasta, int? nhcl = null)
    {
        const string sql = @"
            SELECT DISTINCT TOP 50
                f.med_orden AS IdOrdenOFicha,
                ISNULL(f.med_nhcl, 0) AS Nhcl,
                CONVERT(VARCHAR(10), f.med_fecha, 120) AS FechaIngreso,
                RTRIM(ISNULL(o.ordenAten_nombre, '')) AS Paciente,
                RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                RTRIM(ISNULL(u.user_nombre, '')) AS Medico,
                RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad,
                RTRIM(ISNULL(c.pa_movil, '')) AS Telefono,
                RTRIM(ISNULL(c.pa_email, '')) AS Email,
                CAST(0 AS BIT) AS EsRecetaExterna
            FROM vt_ordenAtencion o
            INNER JOIN vt_fichamedica f ON o.ordenAten_id = f.med_orden
            INNER JOIN vt_medicinas m ON o.ordenAten_id = m.med_id_orden
            LEFT JOIN vt_especialidades e ON o.ordenAten_esp_numero = e.es_id
            LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
            LEFT JOIN vt_clientes c ON o.ordenAten_idcliente = c.pa_codigo
            WHERE f.med_fecha >= @Desde AND f.med_fecha <= @Hasta
              AND (@Nhcl IS NULL OR f.med_nhcl = @Nhcl)
            ORDER BY FechaIngreso DESC";

        var lista = await _db.QueryAsync<RecetaResumenListadoDto>(sql, new
        {
            Desde = desde.Date,
            Hasta = hasta.Date.AddDays(1).AddSeconds(-1),
            Nhcl = nhcl
        });
        return lista.ToList();
    }

    // ========================================================================
    // 6. AUTORIZACIONES / CONSENTIMIENTOS Y ALTA MÉDICA (vt_autorizaciones + qvt_formulario_alta)
    // ========================================================================
    public async Task<List<FormatoPlantillaDto>> ObtenerPlantillasFormatosAsync(string? tipo = null)
    {
        const string sql = @"
            SELECT
                CAST(form_id AS INT) AS FormId,
                RTRIM(ISNULL(form_nombre, '')) AS FormNombre,
                RTRIM(ISNULL(form_tipo, '')) AS FormTipo,
                ISNULL(CAST(form_archivoHtml AS VARCHAR(MAX)), '') AS FormArchivoHtml
            FROM vt_formatos
            WHERE (@Tipo IS NULL OR form_tipo = @Tipo)
            ORDER BY form_nombre ASC";

        var lista = await _db.QueryAsync<FormatoPlantillaDto>(sql, new { Tipo = tipo });
        return lista.ToList();
    }

    public async Task<string> ProcesarPlantillaConDatosPacienteAsync(int formId, int nhcl)
    {
        var plantilla = await _db.ExecuteScalarAsync<string?>(
            "SELECT CAST(form_archivoHtml AS VARCHAR(MAX)) FROM vt_formatos WHERE form_id = @FormId", new { FormId = formId });

        if (string.IsNullOrWhiteSpace(plantilla))
        {
            plantilla = @"<h4>CONSENTIMIENTO INFORMADO e-ForVets</h4>
<p>Yo, <strong>[NOMBRECLIENTE]</strong>, con cédula de identidad N° <strong>[CEDULA]</strong>, en calidad de propietario/responsable del paciente <strong>[MASCOTA]</strong> (NHCL #[NHCL]), autorizo al equipo médico del Hospital de Especialidades Veterinarias USFQ la realización de los procedimientos diagnósticos, anestésicos, quirúrgicos o terapéuticos explicados.</p>
<p>Fecha: [FECHAPIE] | Teléfono: [TELEFONOPIE]</p>";
        }

        var datos = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT TOP 1
                RTRIM(ISNULL(p.mas_nombre, '')) AS Mascota,
                RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                RTRIM(ISNULL(c.pa_cedula, '')) AS Cedula,
                RTRIM(ISNULL(c.pa_movil, '')) AS Celular,
                RTRIM(ISNULL(c.pa_telefono, '')) AS Telefono,
                RTRIM(ISNULL(c.pa_direccion, '')) AS Direccion
            FROM vt_pacientes p
            INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            WHERE p.mas_nhcl = @Nhcl", new { Nhcl = nhcl });

        string cliente = datos?.Cliente ?? "";
        string cedula = datos?.Cedula ?? "";
        string mascota = datos?.Mascota ?? "";
        string celular = datos?.Celular ?? "";
        string telefono = datos?.Telefono ?? "";
        string direccion = datos?.Direccion ?? "";
        string hoy = DateTime.Today.ToString("yyyy-MM-dd");

        return plantilla
            .Replace("[NOMBRECLIENTE]", cliente)
            .Replace("[CEDULA]", cedula)
            .Replace("[MASCOTA]", mascota)
            .Replace("[NOMBREMASCOTA]", mascota)
            .Replace("[NOMBREPIE]", cliente)
            .Replace("[CEDULAPIE]", cedula)
            .Replace("[TELEFONOPIE]", celular)
            .Replace("[CONVENSIONALPIE]", telefono)
            .Replace("[FECHAPIE]", hoy)
            .Replace("[FECHADIA]", hoy)
            .Replace("[NHCL]", nhcl.ToString())
            .Replace("[DIRECCION]", direccion);
    }

    public async Task<List<AutorizacionClinicaDto>> ObtenerAutorizacionesPacienteAsync(int nhcl)
    {
        const string sql = @"
            SELECT
                a.autorizacion_id AS AutorizacionId,
                ISNULL(a.autorizacion_nhcl, 0) AS Nhcl,
                ISNULL(a.autorizacion_orden, 0) AS Orden,
                ISNULL(a.autorizacion_cliente, 0) AS Cliente,
                ISNULL(a.autorizacion_iduser, 0) AS IdUser,
                ISNULL(a.autorizacion_fecha, GETDATE()) AS Fecha,
                ISNULL(a.autorizacion_documento, 'Consentimiento Informado') AS Documento,
                ISNULL(a.autorizacion_tipo, 'HDEV') AS Tipo,
                ISNULL(a.autorizacion_detalle, '') AS DetalleHtml,
                ISNULL(u.user_nombre, 'Médico') AS NombreMedico
            FROM vt_autorizaciones a
            LEFT JOIN vt_usuarios u ON a.autorizacion_iduser = u.user_numero
            WHERE a.autorizacion_nhcl = @Nhcl
            ORDER BY a.autorizacion_id DESC";

        var lista = await _db.QueryAsync<AutorizacionClinicaDto>(sql, new { Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<bool> GuardarAutorizacionAsync(AutorizacionClinicaDto dto)
    {
        const string sql = @"
            INSERT INTO vt_autorizaciones (
                autorizacion_nhcl, autorizacion_fecha, autorizacion_iduser, autorizacion_cliente,
                autorizacion_detalle, autorizacion_orden, autorizacion_tipo, autorizacion_documento
            ) VALUES (
                @Nhcl, GETDATE(), @IdUser, @Cliente,
                @DetalleHtml, @Orden, @Tipo, @Documento
            )";

        await _db.ExecuteAsync(sql, dto);
        return true;
    }

    public async Task<FormularioAltaMedicaDto> ObtenerFormularioAltaAsync(int idOrden, int nhcl)
    {
        const string sql = @"
            SELECT TOP 1
                id AS Id,
                ISNULL(idOrden, @IdOrden) AS IdOrden,
                ISNULL(nhcl, @Nhcl) AS Nhcl,
                ISNULL(fecha_ing, GETDATE()) AS FechaIng,
                ISNULL(usering, 0) AS UserIng,
                ISNULL(cuidadosgenerales, '') AS CuidadosGenerales,
                ISNULL(permitido, '') AS Permitido,
                ISNULL(nopermitido, '') AS NoPermitido,
                ISNULL(dieta, '') AS Dieta,
                ISNULL(medicacion, '') AS Medicacion
            FROM qvt_formulario_alta
            WHERE idOrden = @IdOrden
            ORDER BY id DESC";

        var dto = await _db.QueryFirstOrDefaultAsync<FormularioAltaMedicaDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl });
        return dto ?? new FormularioAltaMedicaDto { IdOrden = idOrden, Nhcl = nhcl };
    }

    public async Task<bool> GuardarFormularioAltaAsync(FormularioAltaMedicaDto dto, int idUsuario)
    {
        dto.UserIng = idUsuario;
        var existeId = await _db.ExecuteScalarAsync<int?>(
            "SELECT TOP 1 id FROM qvt_formulario_alta WHERE idOrden = @IdOrden", new { dto.IdOrden });

        if (existeId.HasValue && existeId.Value > 0)
        {
            dto.Id = existeId.Value;
            await _db.ExecuteAsync(@"
                UPDATE qvt_formulario_alta SET
                    cuidadosgenerales = @CuidadosGenerales, permitido = @Permitido,
                    nopermitido = @NoPermitido, dieta = @Dieta, medicacion = @Medicacion,
                    usering = @UserIng
                WHERE id = @Id", dto);
        }
        else
        {
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_formulario_alta (
                    idOrden, nhcl, fecha_ing, usering, cuidadosgenerales, permitido, nopermitido, dieta, medicacion
                ) VALUES (
                    @IdOrden, @Nhcl, GETDATE(), @UserIng, @CuidadosGenerales, @Permitido, @NoPermitido, @Dieta, @Medicacion
                )", dto);
        }
        return true;
    }
}
