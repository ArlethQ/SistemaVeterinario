using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IConsultaExternaService
{
    // 1. Ficha MÃ©dica / Historia ClÃ­nica SOAP (vt_fichamedica)
    Task<FichaMedicaCompletaDto> ObtenerFichaMedicaAsync(int idOrden, int nhcl);
    Task<bool> GuardarFichaMedicaAsync(FichaMedicaCompletaDto dto, int idUsuario);

    // 2. DiagnÃ³sticos CIE-10 (vt_diagnosticos + vt_cie10)
    Task<List<CatalogoCie10ItemDto>> BuscarCatalogoCie10Async(string filtro);
    Task<List<DiagnosticoCie10Dto>> ObtenerDiagnosticosOrdenAsync(int idOrden);
    Task<bool> AgregarDiagnosticoOrdenAsync(DiagnosticoCie10Dto diag);
    Task<bool> EliminarDiagnosticoOrdenAsync(int sec);

    // 3. Antecedentes y Vacunas (vt_antecedentes + vt_vacunas)
    Task<List<AntecedenteClinicoDto>> ObtenerAntecedentesPacienteAsync(int nhcl);
    Task<bool> AgregarAntecedenteAsync(AntecedenteClinicoDto dto, int idUsuario, int idCliente);
    Task<List<VacunaPacienteDto>> ObtenerVacunasPacienteAsync(int nhcl);
    Task<List<CatalogoItemGenericoDto>> ObtenerCatalogoVacunasAsync();
    Task<bool> AgregarVacunaAsync(VacunaPacienteDto dto, int idUsuario, int idCliente);
    Task<bool> EliminarVacunaAsync(int vacunaId, int idUsuario);

    // 4. EvoluciÃ³n MÃ©dica y Ã“rdenes Asociadas (vt_EvolucionMedico + vt_labOrdenes + vt_rxOrdenes)
    Task<List<EvolucionMedicaDto>> ObtenerEvolucionesPacienteAsync(int nhcl, int? idOrdenFiltro = null);
    Task<int> GuardarEvolucionMedicaAsync(EvolucionMedicaDto dto, int idUsuario);
    Task<bool> EliminarEvolucionMedicaAsync(int idEvolucion);
    Task<List<OrdenExamenEvolucionDto>> ObtenerOrdenesExamenAsync(int idOrden, int? idEvolucion = null);
    Task<bool> AgregarOrdenExamenAsync(OrdenExamenEvolucionDto orden);
    Task<bool> EliminarOrdenExamenAsync(int sec, string tipoCategoria);

    // 5. Recetario MÃ©dico e Indicaciones Generales (vt_medicinas + vt_IndicacionesGenerales)
    Task<List<RecetaMedicamentoItemDto>> ObtenerRecetaOrdenAsync(int idOrden);
    Task<string> ObtenerIndicacionesGeneralesAsync(int idOrden);
    Task<bool> AgregarMedicamentoRecetaAsync(RecetaMedicamentoItemDto item, int idUsuario, int idLocalidad);
    Task<bool> EliminarMedicamentoRecetaAsync(int sec);
    Task<bool> GuardarIndicacionesGeneralesAsync(int idOrden, int nhcl, string indicaciones);
    Task<List<RecetaResumenListadoDto>> BuscarRecetasPorFechasAsync(DateTime desde, DateTime hasta, int? nhcl = null);
    Task<RecetaExternaCabeceraDto?> ObtenerRecetaExternaAsync(int idFicha);
    Task<(bool Ok, int IdFicha, string? Mensaje)> GuardarCabeceraRecetaExternaAsync(RecetaExternaCabeceraDto dto, int idUsuario);
    Task<List<DiagnosticoCie10Dto>> ObtenerDiagnosticosRecetaExternaAsync(int idFicha);
    Task<(bool Ok, string? Mensaje)> AgregarDiagnosticoRecetaExternaAsync(int idFicha, DiagnosticoCie10Dto dto, string serial);
    Task<bool> EliminarDiagnosticoRecetaExternaAsync(int sec);
    Task<List<RecetaMedicamentoItemDto>> ObtenerMedicamentosRecetaExternaAsync(int idFicha);
    Task<(bool Ok, string? Mensaje)> AgregarMedicamentoRecetaExternaAsync(int idFicha, RecetaMedicamentoItemDto item, string serial);
    Task<bool> EliminarMedicamentoRecetaExternaAsync(int sec);

    // 6. Autorizaciones / Consentimientos y Alta MÃ©dica (vt_autorizaciones + vt_formatos + qvt_formulario_alta)
    Task<List<FormatoPlantillaDto>> ObtenerPlantillasFormatosAsync(string? tipo = null);
    Task<string> ProcesarPlantillaConDatosPacienteAsync(int formId, int nhcl);
    Task<List<AutorizacionClinicaDto>> ObtenerAutorizacionesPacienteAsync(int nhcl);
    Task<bool> GuardarAutorizacionAsync(AutorizacionClinicaDto dto);
    Task<FormularioAltaMedicaDto> ObtenerFormularioAltaAsync(int idOrden, int nhcl);
    Task<bool> GuardarFormularioAltaAsync(FormularioAltaMedicaDto dto, int idUsuario);

    // 7. Impresión masiva de fichas médicas por lotes (vt_imprimirfichas.aspx)
    Task<List<FichaMedicaResumenLoteDto>> ObtenerAtencionesParaImpresionLoteAsync(int nhcl);

    // 8. Apuntes longitudinales a historias clinicas (vt_apuntesahistorias / vt_notas)
    Task<List<PacienteApunteHistoriaDto>> BuscarPacientesParaApuntesAsync(string filtro, string criterio);
    Task<List<ApunteHistoriaClinicaDto>> ObtenerApuntesHistoriaAsync(int nhcl);
    Task<(bool Ok, string? Mensaje)> AgregarApunteHistoriaAsync(ApunteHistoriaClinicaDto dto, int idUsuario);
    Task<(bool Ok, string? Mensaje)> AnularApunteHistoriaAsync(int id, int idUsuario);
}

public class ConsultaExternaService : IConsultaExternaService
{
    private readonly ISqlDataService _db;
    private readonly ILogger<ConsultaExternaService> _logger;

    public ConsultaExternaService(
        ISqlDataService db,
        ILogger<ConsultaExternaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ========================================================================
    // 1. FICHA MÃ‰DICA / HISTORIA CLÃNICA SOAP (Reemplaza vt_hisDatMedicos.aspx)
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
                    ISNULL(temperamento, 'TRANQUILO') AS Temperamento,
                    ISNULL(pronosticotueri, 'Bueno') AS MedPronostico,
                    TRY_CONVERT(DATETIME, contmtiempo) AS MedFechaControl,
                    ISNULL(med_pregunta, 'N') AS RequiereCirugia,
                    ISNULL(estadoparte, 'PENDIENTE') AS EstadoParte,
                    RTRIM(ISNULL(u.user_nombre, '')) AS UserNombre,
                    RTRIM(ISNULL(u.user_nombre, '')) AS NombreMedico,
                    RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad
                FROM vt_fichamedica
                LEFT JOIN vt_usuarios u ON u.user_numero = med_iduser
                LEFT JOIN vt_especialidades e ON e.es_id = med_especialidad
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
            _logger.LogWarning(ex, "Aviso al obtener Ficha MÃ©dica para Orden {IdOrden}", idOrden);
        }

        return dto;
    }

    public async Task<bool> GuardarFichaMedicaAsync(FichaMedicaCompletaDto dto, int idUsuario)
    {
        try
        {
            dto.MedIdUser = idUsuario;

            // Asegurar que MedNhcl, MedCliente y MedEspecie esten resueltos
            if (dto.MedNhcl > 0 && (dto.MedCliente <= 0 || string.IsNullOrEmpty(dto.MedEspecie)))
            {
                var pacInfo = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT p.mas_cli_id, p.mas_especie, p.mas_anios, p.mas_meses
                    FROM vt_pacientes p
                    WHERE p.mas_nhcl = @Nhcl", new { Nhcl = dto.MedNhcl });

                if (pacInfo != null)
                {
                    if (dto.MedCliente <= 0 && pacInfo.mas_cli_id != null)
                        dto.MedCliente = (int)pacInfo.mas_cli_id;
                    if (string.IsNullOrEmpty(dto.MedEspecie) && pacInfo.mas_especie != null)
                        dto.MedEspecie = pacInfo.mas_especie.ToString();
                    if (dto.MedEdadAnios <= 0 && pacInfo.mas_anios != null)
                        dto.MedEdadAnios = (int)pacInfo.mas_anios;
                    if (dto.MedEdadMeses <= 0 && pacInfo.mas_meses != null)
                        dto.MedEdadMeses = (int)pacInfo.mas_meses;
                }
            }

            var existeSec = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 sec FROM vt_fichamedica WHERE med_orden = @MedOrden",
                new { dto.MedOrden });

            if (existeSec.HasValue && existeSec.Value > 0)
            {
                dto.Sec = existeSec.Value;
                const string sqlUpdate = @"
                    UPDATE vt_fichamedica SET
                        med_tipo = @MedTipo,
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
                        pronosticotueri = @MedPronostico,
                        contmtiempo = @MedFechaControlStr,
                        contpsi = @ContpSi,
                        contpno = @ContpNo,
                        med_pregunta = @RequiereCirugia,
                        estadoparte = @EstadoParte,
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
                        tratamiento, observacionesyrecomendaciones, med_apuntes, temperamento, pronosticotueri, contmtiempo, contpsi, contpno, med_estado, med_pregunta, estadoparte
                    ) VALUES (
                        @MedNhcl, @MedOrden, GETDATE(), @MedIdUser, @MedTipo, @MedCliente,
                        @MedEspecialidad, @MedEdadAnios, @MedEdadMeses, @MedLocalidad, @MedEspecie,
                        @MedTipoConsulta, @MedMotivo, @MedAnamnesis, @MedAntecedente,
                        @MedObjetivo, @MedAnalisis, @MedPlan,
                        @Peso, @Temperatura, @FrecuenciaCardiaca, @FrecuenciaRespiratoria,
                        @Mucosas, @TiempoLlenadoCapilar, @Deshidratacion, @ReflejoTusigeno,
                        @Auscultacion, @PalpacionTiroides, @IndiceCondicionCorporal, @Cp,
                        @Linfonodos, @PalpacionAbdominal, @PielYAnexos, @CavidadOral, @Otros,
                        @Tratamiento, @ObservacionesYRecomendaciones, @MedApuntes, @Temperamento, @MedPronostico, @MedFechaControlStr, @ContpSi, @ContpNo, 'A', @RequiereCirugia, @EstadoParte
                    )";

                await _db.ExecuteAsync(sqlInsert, dto);
            }

            // Actualizar color de triaje y esterilizado en vt_pacientes
            if (dto.MedNhcl > 0)
            {
                await _db.ExecuteAsync(
                    "UPDATE vt_pacientes SET mas_castrado = @Esterilizado, mas_colortriaje = @ColorTriaje WHERE mas_nhcl = @MedNhcl",
                    new { dto.Esterilizado, dto.ColorTriaje, dto.MedNhcl });
            }

            // Actualizar indicaciones generales de receta siempre
            await GuardarIndicacionesGeneralesAsync(dto.MedOrden, dto.MedNhcl, dto.IndicacionesGeneralesReceta ?? "");

            await _db.RegistrarBitacoraAsync($"Ficha MÃ©dica SOAP guardada Orden #{dto.MedOrden} NHCL #{dto.MedNhcl}", idUsuario);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar Ficha MÃ©dica para Orden {MedOrden}", dto.MedOrden);
            return false;
        }
    }

    // ========================================================================
    // 2. DIAGNÃ“STICOS CIE-10 (vt_diagnosticos + vt_cie10)
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
                CASE WHEN RTRIM(ISNULL(d.dig_tipo, 'D')) = 'P' THEN 'Presuntivo' ELSE 'Definitivo' END AS TipoDiag
            FROM vt_diagnosticos d
            LEFT JOIN vt_cie10 c ON CAST(c.cie_codigo AS VARCHAR(20)) = RTRIM(d.dig_codigoDiag)
            WHERE d.dig_id_orden = @IdOrden
            ORDER BY d.sec ASC";

        var lista = await _db.QueryAsync<DiagnosticoCie10Dto>(sql, new { IdOrden = idOrden });
        return lista.ToList();
    }

    public async Task<bool> AgregarDiagnosticoOrdenAsync(DiagnosticoCie10Dto diag)
    {
        string tipoChar = (diag.TipoDiag?.Trim().ToUpper().StartsWith("P") == true) ? "P" : "D";
        string cod = diag.CodigoDiag?.Trim() ?? "";
        if (cod.Length > 10) cod = cod.Substring(0, 10);

        const string sql = @"
            INSERT INTO vt_diagnosticos (dig_nhcl, dig_codigoDiag, dig_id_orden, dig_descripcion, dig_tipo)
            VALUES (@Nhcl, @CodigoDiag, @IdOrden, @DescripcionObservacion, @TipoChar)";

        await _db.ExecuteAsync(sql, new
        {
            diag.Nhcl,
            CodigoDiag = cod,
            diag.IdOrden,
            DescripcionObservacion = diag.DescripcionObservacion?.Trim() ?? "",
            TipoChar = tipoChar
        });
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
                ISNULL(u.user_nombre, 'MÃ©dico') AS NombreMedico
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
                ISNULL(v.vacuna_idprod, 0) AS IdProducto,
                ISNULL(p.prod_descripcion, '') AS NombreVacuna,
                ISNULL(v.vacuna_observacion, '') AS ObservacionVacuna,
                ISNULL(v.vacuna_tipo, 'VacunaciÃ³n') AS Tipo,
                ISNULL(u.user_nombre, 'MÃ©dico') AS NombreMedico
            FROM vt_vacunas v
            LEFT JOIN vt_productos p ON v.vacuna_idprod = p.prod_id
            LEFT JOIN vt_usuarios u ON v.vacuna_iduser = u.user_numero
            WHERE v.vacuna_nhcl = @Nhcl AND ISNULL(v.vacuna_estado, 'A') = 'A'
            ORDER BY v.vacuna_id DESC";

        var lista = await _db.QueryAsync<VacunaPacienteDto>(sql, new { Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerCatalogoVacunasAsync()
    {
        const string sql = @"
            SELECT prod_id AS Id, RTRIM(ISNULL(prod_descripcion, '')) AS Descripcion
            FROM vt_productos
            WHERE prod_estado = 'A' AND prod_grupo = 10
            ORDER BY prod_descripcion";
        return (await _db.QueryAsync<CatalogoItemGenericoDto>(sql)).ToList();
    }

    public async Task<bool> AgregarVacunaAsync(VacunaPacienteDto dto, int idUsuario, int idCliente)
    {
        const string sql = @"
            INSERT INTO vt_vacunas (
                vacuna_nhcl, vacuna_fecha, vacuna_revacunacion, vacuna_iduser,
                vacuna_cliente, vacuna_estado, vacuna_observacion, vacuna_orden, vacuna_tipo
                , vacuna_idprod
            ) VALUES (
                @Nhcl, @Fecha, @FechaRevacunacion, @IdUser,
                @IdCliente, 'A', @ObservacionVacuna, @Orden, @Tipo
                , @IdProducto
            )";

        // Truncar cadenas para evitar "String or binary data would be truncated"
        var observacion = string.IsNullOrEmpty(dto.ObservacionVacuna)
            ? string.Empty
            : dto.ObservacionVacuna.Length > 500 ? dto.ObservacionVacuna.Substring(0, 500) : dto.ObservacionVacuna;
        var tipo = string.IsNullOrEmpty(dto.Tipo)
            ? "Vacunación"
            : dto.Tipo.Length > 50 ? dto.Tipo.Substring(0, 50) : dto.Tipo;

        await _db.ExecuteAsync(sql, new
        {
            dto.Nhcl,
            dto.Fecha,
            dto.FechaRevacunacion,
            IdUser = idUsuario,
            IdCliente = idCliente,
            ObservacionVacuna = observacion,
            dto.Orden,
            Tipo = tipo
            , dto.IdProducto
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
    // 4. EVOLUCIONES MÃ‰DICAS Y Ã“RDENES ASOCIADAS (vt_EvolucionMedico)
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
                ISNULL(u.user_nombre, 'MÃ©dico Tratante') AS NombreMedico,
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
            int filas = await _db.ExecuteAsync(sqlUpdate, dto);
            if (filas > 0)
            {
                await _db.RegistrarBitacoraAsync($"EvoluciÃ³n MÃ©dica #{dto.IdEvolucion} actualizada Orden #{dto.IdOrden}", idUsuario);
                return dto.IdEvolucion;
            }
            return 0;
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
            await _db.RegistrarBitacoraAsync($"Nueva EvoluciÃ³n MÃ©dica #{idCreado} Orden #{dto.IdOrden}", idUsuario);
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

        var labs = await _db.QueryAsync<OrdenExamenEvolucionDto>(sqlLab, new { IdOrden = idOrden, IdEvolucion = idEvolucion });
        resultado.AddRange(labs);

        var rxs = await _db.QueryAsync<OrdenExamenEvolucionDto>(sqlRx, new { IdOrden = idOrden, IdEvolucion = idEvolucion });
        resultado.AddRange(rxs);

        return resultado;
    }

    public async Task<bool> AgregarOrdenExamenAsync(OrdenExamenEvolucionDto orden)
    {
        if (orden.IdOrden <= 0 || orden.Nhcl <= 0 || orden.Cantidad <= 0 ||
            !int.TryParse(orden.CodigoProd, out int codigoProd) || codigoProd <= 0 ||
            orden.TipoCategoria is not ("LAB" or "RX")) return false;

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
            return await _db.ExecuteAsync(sql, new
            {
                orden.Nhcl,
                CodigoProd = codigoProd,
                Observaciones = $"{orden.DescripcionExamen} - {orden.Observaciones}".Trim(' ', '-'),
                orden.Cantidad,
                orden.IdOrden,
                orden.IdEvolucion
            }) > 0;
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
            return await _db.ExecuteAsync(sql, new
            {
                orden.Nhcl,
                CodigoProd = codigoProd,
                Observaciones = $"{orden.DescripcionExamen} - {orden.Observaciones}".Trim(' ', '-'),
                orden.Cantidad,
                orden.IdOrden,
                orden.IdEvolucion
            }) > 0;
        }
    }

    public async Task<bool> EliminarOrdenExamenAsync(int sec, string tipoCategoria)
    {
        string tabla = tipoCategoria == "LAB" ? "vt_labOrdenes" : "vt_rxOrdenes";
        await _db.ExecuteAsync($"DELETE FROM {tabla} WHERE sec = @Sec", new { Sec = sec });
        return true;
    }

    // ========================================================================
    // 5. RECETARIO MÃ‰DICO E INDICACIONES GENERALES (vt_medicinas + vt_IndicacionesGenerales)
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
        if (item.IdOrden <= 0 || item.Nhcl <= 0 || item.Cantidad <= 0 ||
            string.IsNullOrWhiteSpace(item.Descripcionamento) || string.IsNullOrWhiteSpace(item.Indicaciones))
            return false;

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

        var filasAfectadas = await _db.ExecuteAsync(sql, new
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

        return filasAfectadas > 0;
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

    public async Task<RecetaExternaCabeceraDto?> ObtenerRecetaExternaAsync(int idFicha)
    {
        const string sql = @"
            SELECT TOP 1
                h.idficha AS IdFicha,
                ISNULL(h.nhcl, 0) AS Nhcl,
                ISNULL(p.mas_idcliente, 0) AS IdCliente,
                RTRIM(ISNULL(h.paciente, ISNULL(p.mas_nombre, ''))) AS Paciente,
                RTRIM(ISNULL(h.cliente, LTRIM(RTRIM(ISNULL(c.pa_nombres, '') + ' ' + ISNULL(c.pa_apellidos, ''))))) AS Cliente,
                RTRIM(ISNULL(h.cedula, ISNULL(c.pa_cedula, ''))) AS Cedula,
                RTRIM(ISNULL(h.telefonos, ISNULL(c.pa_telefono, '') + '/' + ISNULL(c.pa_movil, ''))) AS Telefonos,
                RTRIM(ISNULL(h.correo, ISNULL(c.pa_email, ''))) AS Correo,
                RTRIM(ISNULL(h.motivo, '')) AS Motivo,
                ISNULL(h.userid, 0) AS IdUsuario,
                RTRIM(ISNULL(u.user_nombre, '')) AS Medico,
                ISNULL(CAST(h.indicag_detalle AS VARCHAR(MAX)), '') AS IndicacionesGenerales,
                ISNULL((SELECT MIN(m.med_fecha) FROM vt_medicinasexterno m WHERE m.med_id_ficha = h.idficha), GETDATE()) AS Fecha,
                ISNULL((SELECT TOP 1 CAST(m.med_idorden AS VARCHAR(50)) FROM vt_medicinasexterno m WHERE m.med_id_ficha = h.idficha AND ISNULL(m.med_idorden, '') <> '' ORDER BY m.sec), '') AS Serial
            FROM vt_cabecerareceta h
            LEFT JOIN vt_pacientes p ON h.nhcl = p.mas_nhcl
            LEFT JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            LEFT JOIN vt_usuarios u ON h.userid = u.user_numero
            WHERE h.idficha = @IdFicha";

        return await _db.QueryFirstOrDefaultAsync<RecetaExternaCabeceraDto>(sql, new { IdFicha = idFicha });
    }

    public async Task<(bool Ok, int IdFicha, string? Mensaje)> GuardarCabeceraRecetaExternaAsync(
        RecetaExternaCabeceraDto dto, int idUsuario)
    {
        if (dto.Nhcl <= 0 || string.IsNullOrWhiteSpace(dto.Paciente) || string.IsNullOrWhiteSpace(dto.Cliente))
            return (false, 0, "Seleccione un paciente válido.");
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            return (false, 0, "El motivo de la receta es obligatorio.");

        try
        {
            if (dto.IdFicha <= 0)
            {
                dto.IdFicha = await _db.ExecuteScalarAsync<int>(@"
                    INSERT INTO vt_cabecerareceta (
                        nhcl, paciente, cliente, cedula, telefonos, correo, motivo, userid, indicag_detalle
                    ) VALUES (
                        @Nhcl, @Paciente, @Cliente, @Cedula, @Telefonos, @Correo, @Motivo, @IdUsuario, @IndicacionesGenerales
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", new
                {
                    dto.Nhcl,
                    Paciente = dto.Paciente.Trim(),
                    Cliente = dto.Cliente.Trim(),
                    Cedula = dto.Cedula.Trim(),
                    Telefonos = dto.Telefonos.Trim(),
                    Correo = dto.Correo.Trim(),
                    Motivo = dto.Motivo.Trim(),
                    IdUsuario = idUsuario,
                    IndicacionesGenerales = dto.IndicacionesGenerales.Trim()
                });
            }
            else
            {
                var filas = await _db.ExecuteAsync(@"
                    UPDATE vt_cabecerareceta SET
                        paciente=@Paciente, cliente=@Cliente, cedula=@Cedula, telefonos=@Telefonos,
                        correo=@Correo, motivo=@Motivo, userid=@IdUsuario, indicag_detalle=@IndicacionesGenerales
                    WHERE idficha=@IdFicha AND nhcl=@Nhcl", new
                {
                    dto.IdFicha,
                    dto.Nhcl,
                    Paciente = dto.Paciente.Trim(),
                    Cliente = dto.Cliente.Trim(),
                    Cedula = dto.Cedula.Trim(),
                    Telefonos = dto.Telefonos.Trim(),
                    Correo = dto.Correo.Trim(),
                    Motivo = dto.Motivo.Trim(),
                    IdUsuario = idUsuario,
                    IndicacionesGenerales = dto.IndicacionesGenerales.Trim()
                });
                if (filas <= 0) return (false, 0, "La receta externa no existe o pertenece a otro paciente.");
            }

            try
            {
                await _db.RegistrarBitacoraAsync($"Receta externa guardada #{dto.IdFicha} NHCL #{dto.Nhcl}", idUsuario);
            }
            catch
            {
                // La bitacora no debe impedir el registro clinico.
            }
            return (true, dto.IdFicha, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando receta externa NHCL {Nhcl}", dto.Nhcl);
            return (false, 0, "No se pudo guardar la cabecera de la receta.");
        }
    }

    public async Task<List<DiagnosticoCie10Dto>> ObtenerDiagnosticosRecetaExternaAsync(int idFicha)
    {
        const string sql = @"
            SELECT
                d.sec AS Sec,
                CAST(ISNULL(d.dig_nhcl, 0) AS INT) AS Nhcl,
                ISNULL(d.dig_id_ficha, @IdFicha) AS IdOrden,
                RTRIM(ISNULL(d.dig_codigoDiag, '')) AS CodigoDiag,
                RTRIM(ISNULL(c.cie_diagnostico, ISNULL(CAST(d.dig_descripcion AS VARCHAR(MAX)), ''))) AS NombreCie10,
                RTRIM(ISNULL(CAST(d.dig_descripcion AS VARCHAR(MAX)), '')) AS DescripcionObservacion,
                CASE WHEN RTRIM(ISNULL(d.dig_tipo, 'P')) = 'D' THEN 'Definitivo' ELSE 'Presuntivo' END AS TipoDiag
            FROM vt_diagnosticosexternos d
            LEFT JOIN vt_cie10 c ON CAST(c.cie_codigo AS VARCHAR(20)) = RTRIM(d.dig_codigoDiag)
            WHERE d.dig_id_ficha = @IdFicha
            ORDER BY d.sec";
        return (await _db.QueryAsync<DiagnosticoCie10Dto>(sql, new { IdFicha = idFicha })).ToList();
    }

    public async Task<(bool Ok, string? Mensaje)> AgregarDiagnosticoRecetaExternaAsync(
        int idFicha, DiagnosticoCie10Dto dto, string serial)
    {
        if (idFicha <= 0 || dto.Nhcl <= 0) return (false, "Guarde primero la cabecera de la receta.");
        var descripcion = string.IsNullOrWhiteSpace(dto.DescripcionObservacion) ? dto.NombreCie10 : dto.DescripcionObservacion;
        if (string.IsNullOrWhiteSpace(descripcion)) return (false, "Ingrese o seleccione un diagnóstico.");
        var codigo = string.IsNullOrWhiteSpace(dto.CodigoDiag) ? "0" : dto.CodigoDiag.Trim();
        if (codigo.Length > 10) codigo = codigo[..10];
        var tipo = dto.TipoDiag.StartsWith("D", StringComparison.OrdinalIgnoreCase) ? "D" : "P";

        try
        {
            await _db.ExecuteAsync(@"
                INSERT INTO vt_diagnosticosexternos (
                    dig_nhcl, dig_codigoDiag, dig_id_ficha, dig_descripcion, dig_tipo, dig_orden
                ) VALUES (
                    @Nhcl, @Codigo, @IdFicha, @Descripcion, @Tipo, @Serial
                )", new { dto.Nhcl, Codigo = codigo, IdFicha = idFicha, Descripcion = descripcion.Trim(), Tipo = tipo, Serial = serial });
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error agregando diagnostico a receta externa {IdFicha}", idFicha);
            return (false, "No se pudo agregar el diagnóstico.");
        }
    }

    public async Task<bool> EliminarDiagnosticoRecetaExternaAsync(int sec)
        => await _db.ExecuteAsync("DELETE FROM vt_diagnosticosexternos WHERE sec=@Sec", new { Sec = sec }) > 0;

    public async Task<List<RecetaMedicamentoItemDto>> ObtenerMedicamentosRecetaExternaAsync(int idFicha)
    {
        const string sql = @"
            SELECT
                m.sec AS Sec,
                ISNULL(m.med_id_ficha, @IdFicha) AS IdOrden,
                ISNULL(m.med_nhcl, 0) AS Nhcl,
                CAST(ISNULL(m.med_idProdu, 0) AS VARCHAR(50)) AS CodigoProd,
                RTRIM(COALESCE(NULLIF(m.med_descripcionprodu, ''), NULLIF(m.med_otroMedicamento, ''), '')) AS Descripcionamento,
                CASE WHEN RTRIM(ISNULL(m.med_origen, 'F')) = 'O' THEN ISNULL(m.med_cantidad1, 0) ELSE ISNULL(m.med_cantidad, 0) END AS Cantidad,
                RTRIM(ISNULL(m.med_indicaciones, '')) AS Indicaciones,
                ISNULL(m.med_fecha, GETDATE()) AS Fecha,
                RTRIM(ISNULL(m.med_tipo, 'M')) AS Tipo,
                RTRIM(ISNULL(m.med_origen, 'F')) AS Origen
            FROM vt_medicinasexterno m
            WHERE m.med_id_ficha = @IdFicha AND ISNULL(m.med_tipo, 'M') = 'M'
            ORDER BY m.sec";
        return (await _db.QueryAsync<RecetaMedicamentoItemDto>(sql, new { IdFicha = idFicha })).ToList();
    }

    public async Task<(bool Ok, string? Mensaje)> AgregarMedicamentoRecetaExternaAsync(
        int idFicha, RecetaMedicamentoItemDto item, string serial)
    {
        if (idFicha <= 0 || item.Nhcl <= 0) return (false, "Guarde primero la cabecera de la receta.");
        if (item.Cantidad <= 0) return (false, "La cantidad debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(item.Descripcionamento)) return (false, "Ingrese un medicamento.");
        if (string.IsNullOrWhiteSpace(item.Indicaciones)) return (false, "Ingrese las indicaciones del medicamento.");

        var esCatalogo = !string.IsNullOrWhiteSpace(item.CodigoProd) && item.CodigoProd != "0";
        var codigoProducto = esCatalogo && int.TryParse(item.CodigoProd, out var codigoNumerico) ? codigoNumerico : 0;
        try
        {
            await _db.ExecuteAsync(@"
                INSERT INTO vt_medicinasexterno (
                    med_nhcl, med_idProdu, med_indicaciones, med_cantidad, med_id_ficha,
                    med_otroMedicamento, med_cantidad1, med_estado, med_fecha, med_tipo,
                    med_origen, med_descripcionprodu, med_idorden
                ) VALUES (
                    @Nhcl, @CodigoProd, @Indicaciones, @CantidadCatalogo, @IdFicha,
                    @OtroMedicamento, @CantidadOtro, 'Por Entregar', @Fecha, 'M',
                    @Origen, @DescripcionCatalogo, @Serial
                )", new
            {
                item.Nhcl,
                CodigoProd = codigoProducto,
                Indicaciones = item.Indicaciones.Trim(),
                CantidadCatalogo = esCatalogo ? item.Cantidad : 0,
                IdFicha = idFicha,
                OtroMedicamento = esCatalogo ? string.Empty : item.Descripcionamento.Trim().ToUpperInvariant(),
                CantidadOtro = esCatalogo ? 0 : item.Cantidad,
                item.Fecha,
                Origen = esCatalogo ? "F" : "O",
                DescripcionCatalogo = esCatalogo ? item.Descripcionamento.Trim().ToUpperInvariant() : string.Empty,
                Serial = serial
            });
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error agregando medicamento a receta externa {IdFicha}", idFicha);
            return (false, "No se pudo agregar el medicamento.");
        }
    }

    public async Task<bool> EliminarMedicamentoRecetaExternaAsync(int sec)
        => await _db.ExecuteAsync("DELETE FROM vt_medicinasexterno WHERE sec=@Sec", new { Sec = sec }) > 0;

    // ========================================================================
    // 6. AUTORIZACIONES / CONSENTIMIENTOS Y ALTA MÃ‰DICA (vt_autorizaciones + qvt_formulario_alta)
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
<p>Yo, <strong>[NOMBRECLIENTE]</strong>, con cÃ©dula de identidad NÂ° <strong>[CEDULA]</strong>, en calidad de propietario/responsable del paciente <strong>[MASCOTA]</strong> (NHCL #[NHCL]), autorizo al equipo mÃ©dico del Hospital de Especialidades Veterinarias la realizaciÃ³n de los procedimientos diagnÃ³sticos, anestÃ©sicos, quirÃºrgicos o terapÃ©uticos explicados.</p>
<p>Fecha: [FECHAPIE] | TelÃ©fono: [TELEFONOPIE]</p>";
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
                ISNULL(u.user_nombre, 'MÃ©dico') AS NombreMedico
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

        string tipo = string.IsNullOrWhiteSpace(dto.Tipo) ? "HDEV" : dto.Tipo.Trim();
        if (tipo.Length > 50) tipo = tipo.Substring(0, 50);

        string doc = string.IsNullOrWhiteSpace(dto.Documento) ? "Consentimiento Informado" : dto.Documento.Trim();
        if (doc.Length > 150) doc = doc.Substring(0, 150);

        await _db.ExecuteAsync(sql, new
        {
            dto.Nhcl,
            dto.IdUser,
            dto.Cliente,
            dto.DetalleHtml,
            dto.Orden,
            Tipo = tipo,
            Documento = doc
        });
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

    // ========================================================================
    // 7. APUNTES LONGITUDINALES A HISTORIAS CLINICAS (vt_apuntesahistorias)
    // ========================================================================
    public async Task<List<PacienteApunteHistoriaDto>> BuscarPacientesParaApuntesAsync(
        string filtro, string criterio)
    {
        filtro = filtro?.Trim() ?? string.Empty;
        criterio = criterio?.Trim().ToLowerInvariant() ?? "apellidos";
        if (filtro.Length < 2 && criterio != "nhcl") return [];

        const string sql = @"
            SELECT TOP 100
                ISNULL(p.mas_nhcl, 0) AS Nhcl,
                ISNULL(p.mas_idcliente, 0) AS IdCliente,
                RTRIM(ISNULL(p.mas_nombre, '')) AS Paciente,
                LTRIM(RTRIM(ISNULL(c.pa_nombres, '') + ' ' + ISNULL(c.pa_apellidos, ''))) AS Propietario,
                RTRIM(ISNULL(c.pa_cedula, '')) AS Cedula,
                RTRIM(ISNULL(c.pa_movil, '')) AS Celular,
                RTRIM(ISNULL(c.pa_email, '')) AS Email,
                RTRIM(ISNULL(s.sexo, ISNULL(p.mas_sexo, ''))) AS Sexo,
                RTRIM(COALESCE(NULLIF(e.especie_descripcion, ''), NULLIF(cl.des_clas, ''), '')) AS Especie,
                RTRIM(COALESCE(NULLIF(r.des_raza, ''), NULLIF(nc.nombre_cientifico, ''), NULLIF(p.mas_otros, ''), '')) AS Raza,
                CASE WHEN RTRIM(ISNULL(p.mas_origen, 'M')) = 'M' THEN 'HDEV' ELSE 'TUERI' END AS Tipo,
                p.mas_fechanace AS FechaNacimiento
            FROM vt_pacientes p
            INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            LEFT JOIN vt_sexo s ON RTRIM(p.mas_sexo) = RTRIM(s.id)
            LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
            LEFT JOIN vt_razas r ON p.mas_raza = r.id_raza
            LEFT JOIN vt_clase cl ON p.tue_clase = cl.id_clas
            LEFT JOIN vt_nombres_cientificos nc ON p.tue_cientifico = nc.id
            WHERE ISNULL(p.mas_estado, 'A') = 'A'
              AND (
                   (@Criterio = 'nombres' AND c.pa_nombres LIKE '%' + @Filtro + '%')
                OR (@Criterio = 'apellidos' AND c.pa_apellidos LIKE '%' + @Filtro + '%')
                OR (@Criterio = 'cedula' AND c.pa_cedula LIKE '%' + @Filtro + '%')
                OR (@Criterio = 'paciente' AND p.mas_nombre LIKE '%' + @Filtro + '%')
                OR (@Criterio = 'nhcl' AND p.mas_nhcl = TRY_CONVERT(INT, @Filtro))
              )
            ORDER BY p.mas_nombre, c.pa_apellidos, c.pa_nombres";

        try
        {
            var lista = await _db.QueryAsync<PacienteApunteHistoriaDto>(sql, new
            {
                Filtro = filtro,
                Criterio = criterio
            });
            return lista.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buscando pacientes para apuntes de historia");
            throw;
        }
    }

    public async Task<List<ApunteHistoriaClinicaDto>> ObtenerApuntesHistoriaAsync(int nhcl)
    {
        const string sql = @"
            SELECT
                n.not_id AS Id,
                ISNULL(n.not_nhcl, @Nhcl) AS Nhcl,
                ISNULL(n.not_cliente, 0) AS IdCliente,
                ISNULL(n.not_iduser, 0) AS IdUsuario,
                ISNULL(n.not_fecha, GETDATE()) AS Fecha,
                ISNULL(CAST(n.not_observacion AS VARCHAR(MAX)), '') AS Observacion,
                RTRIM(ISNULL(u.user_nombre, 'Usuario no disponible')) AS RegistradoPor,
                RTRIM(ISNULL(n.not_tipo, 'HDEV')) AS Tipo
            FROM vt_notas n
            LEFT JOIN vt_usuarios u ON n.not_iduser = u.user_numero
            WHERE n.not_nhcl = @Nhcl AND ISNULL(n.not_estado, 'A') = 'A'
            ORDER BY n.not_fecha DESC, n.not_id DESC";

        var lista = await _db.QueryAsync<ApunteHistoriaClinicaDto>(sql, new { Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<(bool Ok, string? Mensaje)> AgregarApunteHistoriaAsync(
        ApunteHistoriaClinicaDto dto, int idUsuario)
    {
        if (dto.Nhcl <= 0 || dto.IdCliente <= 0)
            return (false, "Seleccione un paciente válido.");
        if (dto.Fecha == default)
            return (false, "Ingrese una fecha válida.");
        if (string.IsNullOrWhiteSpace(dto.Observacion))
            return (false, "No se puede ingresar un apunte en blanco.");

        var observacion = dto.Observacion.Trim().ToUpperInvariant();
        var tipo = string.IsNullOrWhiteSpace(dto.Tipo) ? "HDEV" : dto.Tipo.Trim().ToUpperInvariant();
        if (tipo.Length > 10) tipo = tipo[..10];

        try
        {
            await _db.ExecuteAsync(@"
                INSERT INTO vt_notas (
                    not_nhcl, not_fecha, not_iduser, not_cliente, not_estado, not_observacion, not_tipo
                ) VALUES (
                    @Nhcl, @Fecha, @IdUsuario, @IdCliente, 'A', @Observacion, @Tipo
                )", new
            {
                dto.Nhcl,
                Fecha = dto.Fecha,
                IdUsuario = idUsuario,
                dto.IdCliente,
                Observacion = observacion,
                Tipo = tipo
            });

            try
            {
                await _db.RegistrarBitacoraAsync($"Apunte agregado a historia NHCL #{dto.Nhcl}", idUsuario);
            }
            catch
            {
                // La bitacora no debe impedir el registro clinico.
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando apunte para NHCL {Nhcl}", dto.Nhcl);
            return (false, "No se pudo guardar el apunte clínico.");
        }
    }

    public async Task<(bool Ok, string? Mensaje)> AnularApunteHistoriaAsync(int id, int idUsuario)
    {
        if (id <= 0) return (false, "El apunte seleccionado no es válido.");
        try
        {
            var filas = await _db.ExecuteAsync(@"
                UPDATE vt_notas
                SET not_estado = 'I', not_elimina = @IdUsuario, not_fechadelete = GETDATE()
                WHERE not_id = @Id AND ISNULL(not_estado, 'A') = 'A'",
                new { Id = id, IdUsuario = idUsuario });
            if (filas <= 0) return (false, "El apunte ya fue anulado o no existe.");

            try
            {
                await _db.RegistrarBitacoraAsync($"Apunte de historia anulado #{id}", idUsuario);
            }
            catch
            {
                // La bitacora no debe impedir la anulacion.
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error anulando apunte de historia {Id}", id);
            return (false, "No se pudo anular el apunte clínico.");
        }
    }

    public async Task<List<FichaMedicaResumenLoteDto>> ObtenerAtencionesParaImpresionLoteAsync(int nhcl)
    {
        if (nhcl <= 0) return [];
        const string sql = @"
            SELECT
                f.sec AS Sec,
                f.med_orden AS Orden,
                f.med_nhcl AS Nhcl,
                f.med_fecha AS Fecha,
                RTRIM(ISNULL(p.mas_nombre, '')) AS Paciente,
                RTRIM(ISNULL(c.pa_nombres, '') + ' ' + ISNULL(c.pa_apellidos, '')) AS Cliente,
                RTRIM(ISNULL(u.user_nombre, '')) AS Medico,
                RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad,
                RTRIM(ISNULL(f.med_motivo, '')) AS Motivo,
                ISNULL((SELECT STUFF((SELECT ', ' + RTRIM(CAST(dig_descripcion AS nvarchar(max)))
                                      FROM vt_diagnosticos
                                      WHERE dig_id_orden = f.med_orden
                                      FOR XML PATH ('')), 1, 2, '')), '') AS Diagnosticos,
                TRY_CONVERT(decimal(10,2), f.peso) AS Peso,
                TRY_CONVERT(decimal(10,2), f.temperatura) AS Temperatura,
                TRY_CONVERT(int, f.frecuenciacardiaca) AS FrecuenciaCardiaca
            FROM vt_fichamedica f
            INNER JOIN vt_pacientes p ON f.med_nhcl = p.mas_nhcl
            LEFT JOIN vt_clientes c ON f.med_cliente = c.pa_codigo
            LEFT JOIN vt_usuarios u ON f.med_iduser = u.user_numero
            LEFT JOIN vt_especialidades e ON f.med_especialidad = e.es_id
            WHERE f.med_nhcl = @Nhcl
            ORDER BY f.med_fecha DESC";

        try
        {
            return (await _db.QueryAsync<FichaMedicaResumenLoteDto>(sql, new { Nhcl = nhcl })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando atenciones para impresión por lotes NHCL {Nhcl}", nhcl);
            return [];
        }
    }
}
