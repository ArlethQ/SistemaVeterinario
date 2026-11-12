using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IConsultaExternaService
{
    // 1. Ficha Médica / Historia Clínica SOAP (vt_fichamedica)
    Task<FichaMedicaCompletaDto> ObtenerFichaMedicaAsync(int idOrden, int nhcl, bool historica = false);
    Task<List<string>> ObtenerTiposConsultaCatalogoAsync(int idLocalidad = 0);
    Task<bool> GuardarFichaMedicaAsync(FichaMedicaCompletaDto dto, int idUsuario, bool triaje = false, bool editarTriaje = false);

    Task<List<TriajeHistorialDto>> ObtenerHistorialTriajeAsync(int nhcl);

    // 2. Diagnósticos CIE-10 (vt_diagnosticos + vt_cie10)
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

    // 4. Evolución Médica y Órdenes Asociadas (vt_EvolucionMedico + vt_labOrdenes + vt_rxOrdenes)
    Task<List<EvolucionMedicaDto>> ObtenerEvolucionesPacienteAsync(int nhcl, int? idOrdenFiltro = null);
    Task<int> GuardarEvolucionMedicaAsync(EvolucionMedicaDto dto, int idUsuario);
    Task<bool> EliminarEvolucionMedicaAsync(int idEvolucion);
    Task<List<OrdenExamenEvolucionDto>> ObtenerOrdenesExamenAsync(int idOrden, int? idEvolucion = null);
    Task<bool> AgregarOrdenExamenAsync(OrdenExamenEvolucionDto orden);
    Task<bool> EliminarOrdenExamenAsync(int sec, string tipoCategoria);

    // 5. Recetario Médico e Indicaciones Generales (vt_medicinas + vt_IndicacionesGenerales)
    Task<List<RecetaMedicamentoItemDto>> ObtenerRecetaOrdenAsync(int idOrden);
    Task<List<AdvertenciaMedicamentoDto>> ObtenerAdvertenciasMedicamentoAsync(int nhcl);
    Task<bool> RegistrarAdvertenciaMedicamentoAsync(int nhcl, int orden, int sec, string motivo, DateTime inicio, DateTime? fin, int usuario);
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

    // 6. Autorizaciones / Consentimientos y Alta Médica (vt_autorizaciones + vt_formatos + qvt_formulario_alta)
    Task<List<FormatoPlantillaDto>> ObtenerPlantillasFormatosAsync(string? tipo = null);
    Task<string> ProcesarPlantillaConDatosPacienteAsync(int formId, int nhcl);
    Task<bool> GuardarPlantillaFormatoAsync(int formId, string nombre, string html);
    Task<(bool Ok, string? Mensaje)> EliminarPlantillaFormatoAsync(int formId);
    Task<List<AutorizacionClinicaDto>> ObtenerAutorizacionesPacienteAsync(int nhcl);
    Task<bool> GuardarAutorizacionAsync(AutorizacionClinicaDto dto);
    Task<bool> ActualizarArchivoAutorizacionAsync(int autorizacionId, string? archivoUrl, string? archivoNombre);
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
    private const string FiltroRegistroTriaje = """
        (marca.Fecha IS NOT NULL OR (
            LTRIM(RTRIM(CONVERT(nvarchar(max), f.med_motivo))) = 'Triaje y signos vitales'
            AND EXISTS (
                SELECT 1 FROM vt_usuarios autor
                JOIN vt_tipos_usuarios tipo ON tipo.tipuser_numero = autor.user_tipouser_numero
                WHERE autor.user_numero = f.med_iduser AND UPPER(tipo.tipuser_descripcion) LIKE '%ENFERMER%'
            )
        ))
        """;

    private const string MarcaRegistroTriaje = """
        OUTER APPLY (
            SELECT TOP (1) v.vit_idFechaMod AS Fecha, v.vit_idUser AS Usuario
            FROM vt_vitacora v
            WHERE v.vit_detalle = CONCAT('Triaje de ingreso completado Orden #', f.med_orden)
            ORDER BY v.vit_idFechaMod DESC
        ) marca
        """;

    private readonly ISqlDataService _db;
    private readonly SessionStateService _session;
    private readonly ILogger<ConsultaExternaService> _logger;

    private int UsuarioFacturacion => 1_000_000 + (_session.Localidad > 0 ? _session.Localidad : 1);

    public ConsultaExternaService(
        ISqlDataService db,
        SessionStateService session,
        ILogger<ConsultaExternaService> logger)
    {
        _db = db;
        _session = session;
        _logger = logger;
    }

    // ========================================================================
    // 1. FICHA MÉDICA / HISTORIA CLÍNICA SOAP (Reemplaza vt_hisDatMedicos.aspx)
    // ========================================================================
    public async Task<FichaMedicaCompletaDto> ObtenerFichaMedicaAsync(int idOrden, int nhcl, bool historica = false)
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
                    ISNULL(med_tipoconsulta, '') AS MedTipoConsulta,
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
                    CAST(ISNULL(med_requiereHospitalizacion, 0) AS BIT) AS RequiereHospitalizacion,
                    ISNULL(estadoparte, 'PENDIENTE') AS EstadoParte,
                    RTRIM(ISNULL(u.user_nombre, '')) AS UserNombre,
                    RTRIM(ISNULL(u.user_nombre, '')) AS NombreMedico,
                    RTRIM(ISNULL(e.es_descripcion, '')) AS Especialidad
                FROM vt_fichamedica
                LEFT JOIN vt_usuarios u ON u.user_numero = med_iduser
                LEFT JOIN vt_especialidades e ON e.es_id = med_especialidad
                WHERE med_orden = @IdOrden AND (@Historica = 0 OR med_nhcl = @Nhcl)
                ORDER BY sec DESC";

            var existente = await _db.QueryFirstOrDefaultAsync<FichaMedicaCompletaDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl, Historica = historica });
            if (existente != null)
            {
                dto = existente;
            }

            if (historica) return dto;

            // Cargar triaje de temperamento y castrado desde vt_pacientes
            var pac = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT mas_colortriaje, mas_castrado FROM vt_pacientes WHERE mas_nhcl = @Nhcl", new { Nhcl = nhcl });
            if (pac != null)
            {
                string colStr = pac.mas_colortriaje?.ToString()?.Trim() ?? "green";
                dto.Esterilizado = pac.mas_castrado?.ToString()?.Trim() ?? "N";
                dto.ColorTriaje = colStr;

                // Si no hay temperamento en vt_fichamedica o está en blanco/tranquilo, sincronizar con el semáforo de vt_pacientes
                if (string.IsNullOrWhiteSpace(dto.Temperamento) || dto.Temperamento == "TRANQUILO")
                {
                    if (colStr.Equals("red", StringComparison.OrdinalIgnoreCase) || colStr.Contains("rojo"))
                        dto.Temperamento = "AGRESIVO";
                    else if (colStr.Equals("yellow", StringComparison.OrdinalIgnoreCase) || colStr.Contains("amarill"))
                        dto.Temperamento = "A LA DEFENSIVA";
                    else if (colStr.Equals("green", StringComparison.OrdinalIgnoreCase) || colStr.Contains("verd"))
                        dto.Temperamento = "TRANQUILO";
                }
            }

            // Si la consulta actual no tiene examen físico guardado aún (consulta nueva o en blanco),
            // verificar si el paciente ya tuvo una consulta registrada el MISMO DÍA para precargar
            // el examen físico, constantes y temperamento.
            bool sinExamenFisico = existente == null || (existente.Peso <= 0 && string.IsNullOrWhiteSpace(existente.MedMotivo) && string.IsNullOrWhiteSpace(existente.MedObjetivo));

            if (!historica && nhcl > 0 && sinExamenFisico)
            {
                const string sqlMismoDia = @"
                    SELECT TOP 1
                        ISNULL(f.med_tipo, 'HDEV') AS MedTipo,
                        ISNULL(f.peso, 0) AS Peso,
                        ISNULL(f.temperatura, 38.5) AS Temperatura,
                        ISNULL(f.frecuenciacardiaca, 90) AS FrecuenciaCardiaca,
                        ISNULL(f.frecuenciarespiratoria, 24) AS FrecuenciaRespiratoria,
                        ISNULL(f.mucosas, 'Rosadas y húmedas') AS Mucosas,
                        ISNULL(f.tiempollenadocapilar, 2) AS TiempoLlenadoCapilar,
                        ISNULL(f.deshidratacion, 0) AS Deshidratacion,
                        ISNULL(f.reflejotusigeno, 'Negativo') AS ReflejoTusigeno,
                        ISNULL(f.auscultacion, 'Normal sin soplos ni ruidos agregados') AS Auscultacion,
                        ISNULL(f.palpaciontiroides, 'Sin alteraciones') AS PalpacionTiroides,
                        ISNULL(f.indicecondicioncorporal, 5) AS IndiceCondicionCorporal,
                        ISNULL(f.cp, 'Normal') AS Cp,
                        ISNULL(f.linfonodos, 'Normales / Simétricos') AS Linfonodos,
                        ISNULL(f.palpacionabdominal, 'Blando, no doloroso') AS PalpacionAbdominal,
                        ISNULL(f.pielyanexos, 'Íntegra, pelaje normal') AS PielYAnexos,
                        ISNULL(f.cavidadoral, 'Sin alteraciones') AS CavidadOral,
                        ISNULL(f.otros, '') AS Otros,
                        ISNULL(f.temperamento, 'TRANQUILO') AS Temperamento,
                        ISNULL(f.med_apuntes, '') AS MedApuntes
                    FROM vt_fichamedica f
                    LEFT JOIN vt_ordenAtencion o ON o.ordenAten_id = f.med_orden
                    WHERE f.med_nhcl = @Nhcl
                      AND f.med_orden <> @IdOrden
                      AND (
                          CONVERT(date, f.med_fecha) = @Hoy
                          OR CONVERT(date, f.med_fecha) = CONVERT(date, GETDATE())
                          OR CONVERT(date, o.ordenAten_fecha) = @Hoy
                          OR CONVERT(date, o.ordenAten_fecha) = CONVERT(date, GETDATE())
                      )
                      AND (ISNULL(f.peso, 0) > 0 OR ISNULL(f.temperamento, '') <> '' OR ISNULL(f.auscultacion, '') <> '' OR ISNULL(f.palpacionabdominal, '') <> '' OR ISNULL(f.med_apuntes, '') <> '')
                    ORDER BY f.sec DESC";

                var previaMismoDia = await _db.QueryFirstOrDefaultAsync<dynamic>(sqlMismoDia, new { IdOrden = idOrden, Nhcl = nhcl, Hoy = DateTime.Today });
                if (previaMismoDia != null)
                {
                    dto.MedTipo = previaMismoDia.MedTipo?.ToString() ?? dto.MedTipo;
                    dto.Peso = Convert.ToDecimal(previaMismoDia.Peso ?? 0);
                    dto.Temperatura = Convert.ToDecimal(previaMismoDia.Temperatura ?? 38.5m);
                    dto.FrecuenciaCardiaca = Convert.ToDecimal(previaMismoDia.FrecuenciaCardiaca ?? 90);
                    dto.FrecuenciaRespiratoria = Convert.ToDecimal(previaMismoDia.FrecuenciaRespiratoria ?? 24);
                    dto.Mucosas = previaMismoDia.Mucosas?.ToString() ?? dto.Mucosas;
                    dto.TiempoLlenadoCapilar = Convert.ToInt32(previaMismoDia.TiempoLlenadoCapilar ?? 2);
                    dto.Deshidratacion = Convert.ToInt32(previaMismoDia.Deshidratacion ?? 0);
                    dto.ReflejoTusigeno = previaMismoDia.ReflejoTusigeno?.ToString() ?? dto.ReflejoTusigeno;
                    dto.Auscultacion = previaMismoDia.Auscultacion?.ToString() ?? dto.Auscultacion;
                    dto.PalpacionTiroides = previaMismoDia.PalpacionTiroides?.ToString() ?? dto.PalpacionTiroides;
                    dto.IndiceCondicionCorporal = Convert.ToInt32(previaMismoDia.IndiceCondicionCorporal ?? 5);
                    dto.Cp = previaMismoDia.Cp?.ToString() ?? dto.Cp;
                    dto.Linfonodos = previaMismoDia.Linfonodos?.ToString() ?? dto.Linfonodos;
                    dto.PalpacionAbdominal = previaMismoDia.PalpacionAbdominal?.ToString() ?? dto.PalpacionAbdominal;
                    dto.PielYAnexos = previaMismoDia.PielYAnexos?.ToString() ?? dto.PielYAnexos;
                    dto.CavidadOral = previaMismoDia.CavidadOral?.ToString() ?? dto.CavidadOral;
                    dto.Otros = previaMismoDia.Otros?.ToString() ?? dto.Otros;
                    dto.Temperamento = previaMismoDia.Temperamento?.ToString() ?? dto.Temperamento;
                    dto.MedApuntes = previaMismoDia.MedApuntes?.ToString() ?? dto.MedApuntes;
                    dto.EsExamenFisicoPrecargadoDelDia = true;

                    if (dto.Temperamento == "AGRESIVO")
                        dto.ColorTriaje = "imagenes/temperamento/rojo.png";
                    else if (dto.Temperamento == "A LA DEFENSIVA")
                        dto.ColorTriaje = "imagenes/temperamento/amarillo.png";
                    else if (dto.Temperamento == "NERVIOSO")
                        dto.ColorTriaje = "imagenes/temperamento/verde.png";
                    else
                        dto.ColorTriaje = "imagenes/temperamento/blanco.png";
                }
            }

            dto.IndicacionesGeneralesReceta = await ObtenerIndicacionesGeneralesAsync(idOrden);

            if (string.IsNullOrWhiteSpace(dto.MedTipoConsulta) ||
                string.Equals(dto.MedTipoConsulta.Trim(), "Consulta Externa", StringComparison.OrdinalIgnoreCase))
            {
                dto.MedTipoConsulta = await ResolverTipoConsultaPredeterminadoOrdenAsync(idOrden);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Aviso al obtener Ficha Médica para Orden {IdOrden}", idOrden);
        }

        return dto;
    }

    public async Task<List<string>> ObtenerTiposConsultaCatalogoAsync(int idLocalidad = 0)
    {
        int idUsuarioFact = idLocalidad > 0 ? 1_000_000 + idLocalidad : 0;

        const string sqlPorSucursal = @"
            SELECT DISTINCT LTRIM(RTRIM(p.NOMBRE)) AS Nombre
            FROM VeterinariaFacturacion.dbo.PRODUCTO p
            INNER JOIN VeterinariaFacturacion.dbo.PRODUCTOTIPO t ON t.IDTIPOPRODUCTO = p.TIPO_PRODUCTO
            INNER JOIN VeterinariaFacturacion.dbo.PRODUCTOSUBTIPO s ON s.IDSUBTIPO = p.IDSUBTIPO
            WHERE ISNULL(p.ESTADO, 1) = 1
              AND ISNULL(t.ESTADO, 1) = 1
              AND ISNULL(s.ESTADO, 'A') <> 'I'
              AND p.IDUSUARIO = @IdUsuarioFact
              AND UPPER(LTRIM(RTRIM(ISNULL(t.DESCRIPCION, '')))) LIKE 'SERVICIO%'
              AND UPPER(LTRIM(RTRIM(ISNULL(s.DESCRIPCION, '')))) LIKE 'CONSULTA%'
              AND NULLIF(LTRIM(RTRIM(ISNULL(p.NOMBRE, ''))), '') IS NOT NULL
            ORDER BY LTRIM(RTRIM(p.NOMBRE)) ASC";

        const string sqlGeneral = @"
            SELECT DISTINCT LTRIM(RTRIM(p.NOMBRE)) AS Nombre
            FROM VeterinariaFacturacion.dbo.PRODUCTO p
            INNER JOIN VeterinariaFacturacion.dbo.PRODUCTOTIPO t ON t.IDTIPOPRODUCTO = p.TIPO_PRODUCTO
            INNER JOIN VeterinariaFacturacion.dbo.PRODUCTOSUBTIPO s ON s.IDSUBTIPO = p.IDSUBTIPO
            WHERE ISNULL(p.ESTADO, 1) = 1
              AND ISNULL(t.ESTADO, 1) = 1
              AND ISNULL(s.ESTADO, 'A') <> 'I'
              AND p.IDUSUARIO >= 1000000
              AND UPPER(LTRIM(RTRIM(ISNULL(t.DESCRIPCION, '')))) LIKE 'SERVICIO%'
              AND UPPER(LTRIM(RTRIM(ISNULL(s.DESCRIPCION, '')))) LIKE 'CONSULTA%'
              AND NULLIF(LTRIM(RTRIM(ISNULL(p.NOMBRE, ''))), '') IS NOT NULL
            ORDER BY LTRIM(RTRIM(p.NOMBRE)) ASC";

        try
        {
            if (idUsuarioFact > 0)
            {
                var locales = (await _db.QueryAsync<string>(sqlPorSucursal, new { IdUsuarioFact = idUsuarioFact })).ToList();
                if (locales.Count > 0) return locales;
            }

            return (await _db.QueryAsync<string>(sqlGeneral)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar el catálogo de consultas desde PRODUCTO.");
            return [];
        }
    }

    private sealed class OrdenTipoConsultaInfo
    {
        public int? TipoConsultaOrden { get; set; }
        public int IdEspecialidad { get; set; }
        public int IdLocalidad { get; set; }
        public string CodigoEspecialidad { get; set; } = string.Empty;
        public string DescripcionEspecialidad { get; set; } = string.Empty;
    }

    private async Task<string> ResolverTipoConsultaPredeterminadoOrdenAsync(int idOrden)
    {
        OrdenTipoConsultaInfo? ordenInfo = null;
        if (idOrden > 0)
        {
            try
            {
                const string sqlOrden = @"
                    SELECT TOP 1
                        o.ordenAten_tipoConsulta AS TipoConsultaOrden,
                        ISNULL(o.ordenAten_esp_numero, 0) AS IdEspecialidad,
                        ISNULL(o.ordenAten_idlocalidad, 0) AS IdLocalidad,
                        RTRIM(ISNULL(e.es_codigo, '')) AS CodigoEspecialidad,
                        RTRIM(ISNULL(e.es_descripcion, '')) AS DescripcionEspecialidad
                    FROM vt_ordenAtencion o
                    LEFT JOIN vt_especialidades e ON e.es_id = o.ordenAten_esp_numero
                    WHERE o.ordenAten_id = @IdOrden";
                ordenInfo = await _db.QueryFirstOrDefaultAsync<OrdenTipoConsultaInfo>(sqlOrden, new { IdOrden = idOrden });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar el tipo de consulta de la orden {IdOrden}.", idOrden);
            }
        }

        bool esEspecialista = false;
        if (ordenInfo != null)
        {
            if (ordenInfo.TipoConsultaOrden == 2)
            {
                esEspecialista = true;
            }
            else if (ordenInfo.TipoConsultaOrden is 1 or 4)
            {
                esEspecialista = false;
            }
            else if (ordenInfo.IdEspecialidad > 0)
            {
                bool esMedGeneral = string.Equals(ordenInfo.CodigoEspecialidad, "MG", StringComparison.OrdinalIgnoreCase)
                    || ordenInfo.DescripcionEspecialidad.Contains("MEDICINA GENERAL", StringComparison.OrdinalIgnoreCase);
                esEspecialista = !esMedGeneral;
            }
        }

        var opciones = await ObtenerTiposConsultaCatalogoAsync(ordenInfo?.IdLocalidad ?? 0);
        if (esEspecialista)
        {
            return opciones.FirstOrDefault(x => string.Equals(x, "CONSULTA ESPECIALISTA DE LLAMADA", StringComparison.OrdinalIgnoreCase))
                ?? opciones.FirstOrDefault(x => x.Contains("ESPECIALISTA", StringComparison.OrdinalIgnoreCase))
                ?? "CONSULTA ESPECIALISTA DE LLAMADA";
        }

        return opciones.FirstOrDefault(x => string.Equals(x, "CONSULTA DE PLANTA", StringComparison.OrdinalIgnoreCase))
            ?? opciones.FirstOrDefault(x => x.Contains("PLANTA", StringComparison.OrdinalIgnoreCase) || x.Contains("GENERAL", StringComparison.OrdinalIgnoreCase))
            ?? "CONSULTA DE PLANTA";
    }

    public async Task<List<TriajeHistorialDto>> ObtenerHistorialTriajeAsync(int nhcl)
    {
        return (await _db.QueryAsync<TriajeHistorialDto>($"""
            SELECT f.med_orden AS Orden, COALESCE(marca.Fecha, f.med_fecha) AS Fecha,
                RTRIM(ISNULL(u.user_nombre, '')) AS RegistradoPor,
                f.peso AS Peso, f.temperatura AS Temperatura, f.frecuenciacardiaca AS FrecuenciaCardiaca,
                CASE WHEN NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), f.med_anamnesis))), '') IS NULL
                    THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS PuedeEditar
            FROM vt_fichamedica f
            {MarcaRegistroTriaje}
            LEFT JOIN vt_usuarios u ON u.user_numero = COALESCE(marca.Usuario, f.med_iduser)
            WHERE f.med_nhcl = @Nhcl AND f.med_orden > 0 AND {FiltroRegistroTriaje}
                AND f.sec = (SELECT MAX(actual.sec) FROM vt_fichamedica actual
                    WHERE actual.med_nhcl = f.med_nhcl AND actual.med_orden = f.med_orden)
            ORDER BY Fecha DESC, Orden DESC
            """, new { Nhcl = nhcl })).ToList();
    }

    public async Task<bool> GuardarFichaMedicaAsync(FichaMedicaCompletaDto dto, int idUsuario, bool triaje = false, bool editarTriaje = false)
    {
        try
        {
            dto.MedIdUser = idUsuario;

            // Asegurar que MedNhcl, MedCliente y MedEspecie esten resueltos
            if (dto.MedNhcl > 0 && (dto.MedCliente <= 0 || string.IsNullOrEmpty(dto.MedEspecie)))
            {
                var pacInfo = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT p.mas_idcliente, e.especie_descripcion AS mas_especie, p.masyear, p.masmeses
                    FROM vt_pacientes p
                    LEFT JOIN vt_especie e ON e.especie_id = p.mas_especie
                    WHERE p.mas_nhcl = @Nhcl", new { Nhcl = dto.MedNhcl });

                if (pacInfo != null)
                {
                    if (dto.MedCliente <= 0 && pacInfo.mas_idcliente != null)
                        dto.MedCliente = Convert.ToInt32(pacInfo.mas_idcliente);
                    if (string.IsNullOrEmpty(dto.MedEspecie) && pacInfo.mas_especie != null)
                        dto.MedEspecie = pacInfo.mas_especie.ToString();
                    string? strYear = pacInfo.masyear?.ToString();
                    if (dto.MedEdadAnios <= 0 && int.TryParse(strYear, out int anios))
                        dto.MedEdadAnios = anios;

                    string? strMeses = pacInfo.masmeses?.ToString();
                    if (dto.MedEdadMeses <= 0 && int.TryParse(strMeses, out int meses))
                        dto.MedEdadMeses = meses;
                }
            }

            if (triaje && string.IsNullOrWhiteSpace(dto.MedMotivo))
            {
                dto.MedMotivo = "Triaje y signos vitales";
            }

            var comandos = new List<(string Sql, object? Parameters)>();
            if (triaje && editarTriaje)
            {
                comandos.Add(($"""
                    IF NOT EXISTS (
                        SELECT 1 FROM vt_fichamedica f WITH (UPDLOCK, HOLDLOCK)
                        {MarcaRegistroTriaje}
                        WHERE f.sec = @Sec AND f.med_orden = @MedOrden AND f.med_nhcl = @MedNhcl
                            AND {FiltroRegistroTriaje}
                            AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), f.med_anamnesis))), '') IS NULL
                    ) OR NOT EXISTS (
                        SELECT 1 FROM vt_usuarios u
                        JOIN vt_tipos_usuarios t ON t.tipuser_numero = u.user_tipouser_numero
                        WHERE u.user_numero = @MedIdUser AND u.user_estado = 'A'
                            AND (t.tipuser_numero = 1 OR UPPER(t.tipuser_descripcion) LIKE '%ENFERMER%')
                    )
                        THROW 50001, 'El triaje ya no se puede editar.', 1;
                    """, dto));
            }
            else if (triaje)
            {
                comandos.Add((@"
                    IF NOT EXISTS (
                        SELECT 1 FROM vt_ordenAtencion o WITH (UPDLOCK, HOLDLOCK)
                        WHERE o.ordenAten_id = @MedOrden AND o.ordenAten_nhcl = @MedNhcl
                          AND o.ordenAten_esp_numero > 0 AND ISNULL(o.ordenAten_esProcedimiento, 0) = 0
                          AND ISNULL(o.ordenAten_estado, 'A') IN ('A', 'E')
                    ) OR NOT EXISTS (
                        SELECT 1 FROM vt_usuarios u
                        JOIN vt_tipos_usuarios t ON t.tipuser_numero = u.user_tipouser_numero
                        WHERE u.user_numero = @MedIdUser AND u.user_estado = 'A'
                          AND (t.tipuser_numero = 1 OR UPPER(t.tipuser_descripcion) LIKE '%ENFERMER%')
                    ) OR EXISTS (
                        SELECT 1 FROM vt_vitacora
                        WHERE vit_detalle = CONCAT('Triaje de ingreso completado Orden #', @MedOrden)
                    )
                        THROW 50001, 'La orden ya no está disponible para triaje.', 1;

                    UPDATE vt_ordenAtencion SET ordenAten_idEnfermero = @MedIdUser
                    WHERE ordenAten_id = @MedOrden;

                    INSERT INTO vt_vitacora (vit_detalle, vit_idUser, vit_idFechaMod, ip, hostname)
                    VALUES (CONCAT('Triaje de ingreso completado Orden #', @MedOrden),
                        @MedIdUser, GETDATE(), '127.0.0.1', 'BlazorClient');", dto));
            }

            var existeSec = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 sec FROM vt_fichamedica WHERE med_orden = @MedOrden AND (@EditarTriaje = 0 OR med_nhcl = @MedNhcl) ORDER BY sec DESC",
                new { dto.MedOrden, dto.MedNhcl, EditarTriaje = editarTriaje });

            if (editarTriaje && (!existeSec.HasValue || existeSec.Value <= 0)) return false;

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
                        med_requiereHospitalizacion = @RequiereHospitalizacion,
                        estadoparte = @EstadoParte,
                        med_idmodifica = @MedIdUser,
                        med_fechamodifica = GETDATE()
                    WHERE sec = @Sec";

                if (triaje)
                {
                    comandos.Add((@"
                        UPDATE vt_fichamedica SET med_tipo = @MedTipo, med_tipoconsulta = @MedTipoConsulta,
                            peso = @Peso, temperatura = @Temperatura,
                            frecuenciacardiaca = @FrecuenciaCardiaca, frecuenciarespiratoria = @FrecuenciaRespiratoria,
                            mucosas = @Mucosas, tiempollenadocapilar = @TiempoLlenadoCapilar,
                            deshidratacion = @Deshidratacion, indicecondicioncorporal = @IndiceCondicionCorporal,
                            cp = @Cp, med_apuntes = @MedApuntes,
                            temperamento = @Temperamento, med_idmodifica = @MedIdUser, med_fechamodifica = GETDATE()
                        WHERE sec = @Sec", dto));
                }
                else
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
                        tratamiento, observacionesyrecomendaciones, med_apuntes, temperamento, pronosticotueri, contmtiempo, contpsi, contpno, med_estado, med_pregunta, med_requiereHospitalizacion, estadoparte
                    ) VALUES (
                        @MedNhcl, @MedOrden, GETDATE(), @MedIdUser, @MedTipo, @MedCliente,
                        @MedEspecialidad, @MedEdadAnios, @MedEdadMeses, @MedLocalidad, @MedEspecie,
                        @MedTipoConsulta, @MedMotivo, @MedAnamnesis, @MedAntecedente,
                        @MedObjetivo, @MedAnalisis, @MedPlan,
                        @Peso, @Temperatura, @FrecuenciaCardiaca, @FrecuenciaRespiratoria,
                        @Mucosas, @TiempoLlenadoCapilar, @Deshidratacion, @ReflejoTusigeno,
                        @Auscultacion, @PalpacionTiroides, @IndiceCondicionCorporal, @Cp,
                        @Linfonodos, @PalpacionAbdominal, @PielYAnexos, @CavidadOral, @Otros,
                        @Tratamiento, @ObservacionesYRecomendaciones, @MedApuntes, @Temperamento, @MedPronostico, @MedFechaControlStr, @ContpSi, @ContpNo, 'A', @RequiereCirugia, @RequiereHospitalizacion, @EstadoParte
                    )";

                if (triaje)
                    comandos.Add((sqlInsert, dto));
                else
                    await _db.ExecuteAsync(sqlInsert, dto);
            }

            // Actualizar color de triaje y esterilizado en vt_pacientes
            if (dto.MedNhcl > 0)
            {
                string colorCode = "green";
                string temp = (dto.Temperamento ?? "").ToUpperInvariant();
                string col = (dto.ColorTriaje ?? "").ToLowerInvariant();

                if (temp.Contains("AGRESIV") || col.Contains("rojo") || col.Contains("red"))
                    colorCode = "red";
                else if (temp.Contains("DEFENSIV") || col.Contains("amarill") || col.Contains("yellow"))
                    colorCode = "yellow";
                else
                    colorCode = "green";

                if (triaje && !editarTriaje)
                    comandos.Add(("UPDATE vt_pacientes SET mas_castrado = @Esterilizado, mas_colortriaje = @ColorCode WHERE mas_nhcl = @MedNhcl",
                        new { dto.Esterilizado, ColorCode = colorCode, dto.MedNhcl }));
                else if (!triaje)
                    await _db.ExecuteAsync(
                        "UPDATE vt_pacientes SET mas_castrado = @Esterilizado, mas_colortriaje = @ColorCode WHERE mas_nhcl = @MedNhcl",
                        new { dto.Esterilizado, ColorCode = colorCode, dto.MedNhcl });
            }

            if (triaje)
            {
                await _db.ExecuteBatchAsync(comandos);
                return true;
            }

            // Actualizar indicaciones generales de receta siempre
            await GuardarIndicacionesGeneralesAsync(dto.MedOrden, dto.MedNhcl, dto.IndicacionesGeneralesReceta ?? "");

            // Al guardar la ficha SOAP, cerrar la orden para que salga de la cola del día y pase al historial
            if (dto.MedOrden > 0)
            {
                await _db.ExecuteAsync(@"
                    UPDATE vt_ordenAtencion
                    SET ordenAten_estado = 'T',
                        ordenAten_etapa = 'Procesado',
                        ordenAten_fechaProcesado = ISNULL(ordenAten_fechaProcesado, GETDATE())
                    WHERE ordenAten_id = @IdOrden
                      AND ISNULL(ordenAten_estado, 'A') <> 'I';",
                    new { IdOrden = dto.MedOrden });
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

    public async Task<bool> ActualizarArchivoAutorizacionAsync(int autorizacionId, string? archivoUrl, string? archivoNombre)
    {
        try
        {
            const string sql = @"
                UPDATE vt_autorizaciones
                SET autorizacion_archivo_url = @ArchivoUrl,
                    autorizacion_archivo_nombre = @ArchivoNombre
                WHERE autorizacion_id = @AutorizacionId";
            await _db.ExecuteAsync(sql, new { AutorizacionId = autorizacionId, ArchivoUrl = archivoUrl, ArchivoNombre = archivoNombre });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar archivo adjunto de autorizacion {AutorizacionId}", autorizacionId);
            return false;
        }
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
                ISNULL(v.vacuna_idprod, 0) AS IdProducto,
                REPLACE(REPLACE(RTRIM(ISNULL(p.NOMBRE COLLATE DATABASE_DEFAULT, '')), CHAR(13), ''), CHAR(10), '') AS NombreVacuna,
                ISNULL(v.vacuna_observacion, '') AS ObservacionVacuna,
                ISNULL(v.vacuna_tipo, 'Vacunación') AS Tipo,
                ISNULL(u.user_nombre, 'Médico') AS NombreMedico
            FROM vt_vacunas v
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON v.vacuna_idprod = p.CODIGO
            LEFT JOIN vt_usuarios u ON v.vacuna_iduser = u.user_numero
            WHERE v.vacuna_nhcl = @Nhcl AND ISNULL(v.vacuna_estado, 'A') = 'A'
            ORDER BY v.vacuna_id DESC";

        var lista = await _db.QueryAsync<VacunaPacienteDto>(sql, new { Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerCatalogoVacunasAsync()
    {
        const string sql = @"
            SELECT
                p.CODIGO AS Id,
                REPLACE(REPLACE(RTRIM(ISNULL(p.NOMBRE, '')), CHAR(13), ''), CHAR(10), '') AS Descripcion
            FROM VeterinariaFacturacion.dbo.PRODUCTO p
            INNER JOIN VeterinariaFacturacion.dbo.PRODUCTOTIPO t ON t.IDTIPOPRODUCTO = p.TIPO_PRODUCTO
            WHERE ISNULL(p.ESTADO, 1) = 1
              AND ISNULL(t.ESTADO, 1) = 1
              AND p.IDUSUARIO = @IdUsuario
              AND UPPER(LTRIM(RTRIM(ISNULL(t.DESCRIPCION, '')))) LIKE '%VACUN%'
            ORDER BY Descripcion ASC";
        return (await _db.QueryAsync<CatalogoItemGenericoDto>(sql, new { IdUsuario = UsuarioFacturacion })).ToList();
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
            int filas = await _db.ExecuteAsync(sqlUpdate, dto);
            if (filas > 0)
            {
                await _db.RegistrarBitacoraAsync($"Evolución Médica #{dto.IdEvolucion} actualizada Orden #{dto.IdOrden}", idUsuario);
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
                COALESCE(p.NOMBRE COLLATE DATABASE_DEFAULT, NULLIF(RTRIM(l.labO_observaciones), ''), 'Examen Laboratorio') AS DescripcionExamen,
                ISNULL(l.labO_observaciones, '') AS Observaciones,
                ISNULL(l.labO_cantidad, 1) AS Cantidad,
                CAST(ISNULL(p.VALOR_UNITARIO, 0.00) AS DECIMAL(18,2)) AS Precio,
                ISNULL(l.labO_estado, 'Por Facturar') AS Estado,
                ISNULL(l.labO_fecha, GETDATE()) AS Fecha,
                'LAB' AS TipoCategoria
            FROM vt_labOrdenes l
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON p.CODIGO = l.labO_idProdu
            WHERE l.labO_id_orden = @IdOrden
              AND (@IdEvolucion IS NULL OR ISNULL(l.labO_evolucion, 0) = @IdEvolucion)";

        const string sqlRx = @"
            SELECT
                r.sec AS Sec,
                ISNULL(r.rxO_id_orden, 0) AS IdOrden,
                CAST(ISNULL(r.rxO_nhcl, 0) AS INT) AS Nhcl,
                ISNULL(r.rxO_evolucion, 0) AS IdEvolucion,
                CAST(CAST(ISNULL(r.rxO_idProdu, 0) AS INT) AS VARCHAR(50)) AS CodigoProd,
                COALESCE(p.NOMBRE COLLATE DATABASE_DEFAULT, NULLIF(RTRIM(r.rxO_observaciones), ''), 'Estudio Imagen') AS DescripcionExamen,
                ISNULL(r.rxO_observaciones, '') AS Observaciones,
                ISNULL(r.rxO_cantidad, 1) AS Cantidad,
                CAST(ISNULL(p.VALOR_UNITARIO, 0.00) AS DECIMAL(18,2)) AS Precio,
                ISNULL(r.rxO_estado, 'Por Facturar') AS Estado,
                ISNULL(r.rxO_fecha, GETDATE()) AS Fecha,
                'RX' AS TipoCategoria
            FROM vt_rxOrdenes r
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON p.CODIGO = r.rxO_idProdu
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
    // 5. RECETARIO MÉDICO E INDICACIONES GENERALES (vt_medicinas + vt_IndicacionesGenerales)
    // ========================================================================
    public async Task<List<AdvertenciaMedicamentoDto>> ObtenerAdvertenciasMedicamentoAsync(int nhcl)
    {
        if (nhcl <= 0)
            return [];

        var advertencias = await _db.QueryAsync<AdvertenciaMedicamentoDto>(@"
            SELECT Id, CodigoProducto, Medicamento, Motivo, Inicio, Fin
            FROM dbo.vt_advertencias_medicamento
            WHERE Nhcl = @Nhcl
            ORDER BY Inicio DESC, Id DESC", new { Nhcl = nhcl });
        return advertencias.ToList();
    }

    public async Task<bool> RegistrarAdvertenciaMedicamentoAsync(
        int nhcl, int orden, int sec, string motivo, DateTime inicio, DateTime? fin, int usuario)
    {
        motivo = motivo.Trim();
        if (nhcl <= 0 || orden <= 0 || sec <= 0 || usuario <= 0 ||
            motivo.Length is 0 or > 500 || (fin.HasValue && fin.Value.Date < inicio.Date))
            return false;

        var filas = await _db.ExecuteAsync(@"
            INSERT INTO dbo.vt_advertencias_medicamento
                (Nhcl, CodigoProducto, Medicamento, Motivo, Inicio, Fin, UsuarioRegistro)
            SELECT @Nhcl, LTRIM(RTRIM(CAST(ISNULL(m.med_idProdu, '') AS varchar(50)))),
                   LEFT(nombre.Medicamento, 300), @Motivo, @Inicio, @Fin, @Usuario
            FROM vt_medicinas m
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.med_idProdu = p.CODIGO
            CROSS APPLY (SELECT COALESCE(
                NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(m.med_otroMedicamento, CHAR(13), ''), CHAR(10), ''))), ''),
                NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(p.NOMBRE COLLATE DATABASE_DEFAULT, CHAR(13), ''), CHAR(10), ''))), '')
            ) AS Medicamento) nombre
            WHERE m.med_nhcl = @Nhcl AND m.med_id_orden = @Orden AND m.sec = @Sec
              AND ISNULL(m.med_tipo, 'M') = 'M' AND nombre.Medicamento IS NOT NULL",
            new { Nhcl = nhcl, Orden = orden, Sec = sec, Motivo = motivo, Inicio = inicio.Date, Fin = fin?.Date, Usuario = usuario });
        return filas > 0;
    }

    public async Task<List<RecetaMedicamentoItemDto>> ObtenerRecetaOrdenAsync(int idOrden)
    {
        const string sql = @"
            SELECT
                m.sec AS Sec,
                ISNULL(m.med_id_orden, 0) AS IdOrden,
                ISNULL(m.med_nhcl, 0) AS Nhcl,
                CAST(ISNULL(m.med_idProdu, '') AS VARCHAR(50)) AS CodigoProd,
                COALESCE(
                    NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(m.med_otroMedicamento, '')), CHAR(13), ''), CHAR(10), ''), ''),
                    NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(p.NOMBRE COLLATE DATABASE_DEFAULT, '')), CHAR(13), ''), CHAR(10), ''), ''),
                    ''
                ) AS Descripcionamento,
                ISNULL(m.med_cantidad, 1) AS Cantidad,
                ISNULL(m.med_cantidadLetras, '') AS CantidadLetras,
                ISNULL(m.med_indicaciones, '') AS Indicaciones,
                ISNULL(m.med_observaciones, '') AS Observaciones,
                ISNULL(m.med_fecha, GETDATE()) AS Fecha,
                ISNULL(m.med_tipo, 'M') AS Tipo
            FROM vt_medicinas m
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.med_idProdu = p.CODIGO
            WHERE m.med_id_orden = @IdOrden
            ORDER BY m.sec ASC";

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
                '' AS Serial
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
                    dig_nhcl, dig_codigoDiag, dig_id_ficha, dig_descripcion, dig_tipo
                ) VALUES (
                    @Nhcl, @Codigo, @IdFicha, @Descripcion, @Tipo
                )", new { dto.Nhcl, Codigo = codigo, IdFicha = idFicha, Descripcion = descripcion.Trim(), Tipo = tipo });
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
                COALESCE(
                    NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(m.med_otroMedicamento, '')), CHAR(13), ''), CHAR(10), ''), ''),
                    NULLIF(REPLACE(REPLACE(RTRIM(ISNULL(p.NOMBRE COLLATE DATABASE_DEFAULT, '')), CHAR(13), ''), CHAR(10), ''), ''),
                    ''
                ) AS Descripcionamento,
                CASE WHEN ISNULL(m.med_cantidad, 0) > 0 THEN m.med_cantidad ELSE ISNULL(m.med_cantidad1, 0) END AS Cantidad,
                RTRIM(ISNULL(m.med_indicaciones, '')) AS Indicaciones,
                ISNULL(m.med_fecha, GETDATE()) AS Fecha,
                RTRIM(ISNULL(m.med_tipo, 'M')) AS Tipo,
                CASE WHEN ISNULL(m.med_idProdu, 0) > 0 THEN 'F' ELSE 'O' END AS Origen
            FROM vt_medicinasexterno m
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO p ON m.med_idProdu = p.CODIGO
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

        var codigoProducto = !string.IsNullOrWhiteSpace(item.CodigoProd) && item.CodigoProd != "0" &&
                             int.TryParse(item.CodigoProd, out var codigoNumerico)
            ? codigoNumerico
            : 0;
        var esCatalogo = codigoProducto > 0;
        try
        {
            await _db.ExecuteAsync(@"
                INSERT INTO vt_medicinasexterno (
                    med_nhcl, med_idProdu, med_indicaciones, med_cantidad, med_id_ficha,
                    med_otroMedicamento, med_cantidad1, med_estado, med_fecha, med_tipo
                ) VALUES (
                    @Nhcl, @CodigoProd, @Indicaciones, @CantidadCatalogo, @IdFicha,
                    @OtroMedicamento, @CantidadOtro, 'Por Entregar', @Fecha, 'M'
                )", new
            {
                item.Nhcl,
                CodigoProd = codigoProducto,
                Indicaciones = item.Indicaciones.Trim(),
                CantidadCatalogo = esCatalogo ? item.Cantidad : 0,
                IdFicha = idFicha,
                OtroMedicamento = item.Descripcionamento.Trim().ToUpperInvariant(),
                CantidadOtro = esCatalogo ? 0 : item.Cantidad,
                item.Fecha
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
    // 6. AUTORIZACIONES / CONSENTIMIENTOS Y ALTA MÉDICA (vt_autorizaciones + qvt_formulario_alta)
    // ========================================================================
    public async Task<List<FormatoPlantillaDto>> ObtenerPlantillasFormatosAsync(string? tipo = null)
    {
        const string sql = @"
            SELECT
                CAST(form_id AS INT) AS FormId,
                RTRIM(ISNULL(form_nombre, '')) AS FormNombre,
                RTRIM(ISNULL(form_tipo, '')) AS FormTipo,
                ISNULL(CAST(form_archivoHtml AS VARCHAR(MAX)), '') AS FormArchivoHtml,
                CAST(ISNULL(form_es_sistema, CASE WHEN form_id <= 8 THEN 1 ELSE 0 END) AS BIT) AS EsSistema
            FROM vt_formatos
            WHERE (@Tipo IS NULL OR form_tipo = @Tipo)
            ORDER BY form_nombre ASC";

        var lista = await _db.QueryAsync<FormatoPlantillaDto>(sql, new { Tipo = tipo });
        var res = lista.ToList();
        foreach (var item in res)
        {
            if (!string.IsNullOrEmpty(item.FormArchivoHtml))
            {
                item.FormArchivoHtml = CorregirCaracteresEspeciales(
                    item.FormArchivoHtml
                        .Replace("src=\"imagenes/", "src=\"/imagenes/")
                        .Replace("src='imagenes/", "src='/imagenes/"));
            }
        }
        return res;
    }

    public async Task<bool> GuardarPlantillaFormatoAsync(int formId, string nombre, string html)
    {
        try
        {
            if (formId > 0)
            {
                const string sql = @"
                    UPDATE vt_formatos
                    SET form_archivoHtml = @Html,
                        form_nombre = CASE WHEN LEN(ISNULL(@Nombre, '')) > 0 THEN @Nombre ELSE form_nombre END
                    WHERE form_id = @FormId";
                await _db.ExecuteAsync(sql, new { FormId = formId, Nombre = nombre, Html = html });
                return true;
            }
            else
            {
                const string sql = @"
                    INSERT INTO vt_formatos (form_nombre, form_tipo, form_archivoHtml, form_es_sistema)
                    VALUES (@Nombre, 'PERSONALIZADO', @Html, 0)";
                await _db.ExecuteAsync(sql, new { Nombre = string.IsNullOrWhiteSpace(nombre) ? "Nueva Plantilla" : nombre, Html = html });
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar plantilla formato form_id={FormId}", formId);
            return false;
        }
    }

    public async Task<(bool Ok, string? Mensaje)> EliminarPlantillaFormatoAsync(int formId)
    {
        try
        {
            if (formId <= 0)
                return (false, "Identificador de plantilla no válido.");

            var esSistema = await _db.ExecuteScalarAsync<int?>(
                "SELECT ISNULL(form_es_sistema, CASE WHEN form_id <= 8 THEN 1 ELSE 0 END) FROM vt_formatos WHERE form_id = @FormId",
                new { FormId = formId });

            if (esSistema == 1 || formId <= 8)
            {
                return (false, "Esta es una plantilla oficial predeterminada del sistema y está protegida contra eliminación.");
            }

            var filas = await _db.ExecuteAsync("DELETE FROM vt_formatos WHERE form_id = @FormId", new { FormId = formId });
            if (filas > 0)
            {
                return (true, null);
            }
            return (false, "No se encontró la plantilla para eliminar.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar plantilla formato form_id={FormId}", formId);
            return (false, "Ocurrió un error al intentar eliminar la plantilla.");
        }
    }

    public static string CorregirCaracteresEspeciales(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;

        return texto
            .Replace("Numrica", "Numérica")
            .Replace("NUMRICA", "NUMÉRICA");
    }

    public async Task<string> ProcesarPlantillaConDatosPacienteAsync(int formId, int nhcl)
    {
        var plantilla = await _db.ExecuteScalarAsync<string?>(
            "SELECT CAST(form_archivoHtml AS VARCHAR(MAX)) FROM vt_formatos WHERE form_id = @FormId", new { FormId = formId });

        if (string.IsNullOrWhiteSpace(plantilla))
        {
            plantilla = @"<h4>CONSENTIMIENTO INFORMADO e-ForVets</h4>
<p>Yo, <strong>[NOMBRECLIENTE]</strong>, con cédula de identidad N° <strong>[CEDULA]</strong>, en calidad de propietario/responsable del paciente <strong>[MASCOTA]</strong> (NHCL #[NHCL]), autorizo al equipo médico del Hospital de Especialidades Veterinarias la realización de los procedimientos diagnósticos, anestésicos, quirúrgicos o terapéuticos explicados.</p>
<p>Fecha: [FECHAPIE] | Teléfono: [TELEFONOPIE]</p>";
        }

        var datos = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT TOP 1
                RTRIM(ISNULL(p.mas_nombre, '')) AS Mascota,
                RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                RTRIM(ISNULL(c.pa_cedula, '')) AS Cedula,
                RTRIM(ISNULL(c.pa_movil, '')) AS Celular,
                RTRIM(ISNULL(c.pa_telefono, '')) AS Telefono,
                RTRIM(ISNULL(c.pa_direccion, '')) AS Direccion,
                RTRIM(ISNULL(e.especie_descripcion, '')) AS Especie,
                RTRIM(ISNULL(r.des_raza, '')) AS Raza,
                CASE 
                    WHEN UPPER(RTRIM(ISNULL(p.mas_sexo, ''))) = 'M' THEN 'Macho'
                    WHEN UPPER(RTRIM(ISNULL(p.mas_sexo, ''))) = 'H' THEN 'Hembra'
                    ELSE RTRIM(ISNULL(p.mas_sexo, ''))
                END AS Sexo,
                p.mas_fechanace AS FechaNacimiento,
                RTRIM(ISNULL(u.user_nombre, 'Médico Veterinario')) AS Medico
            FROM vt_pacientes p
            INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
            LEFT JOIN vt_razas r ON p.mas_raza = r.id_raza
            LEFT JOIN vt_ordenAtencion o ON p.mas_nhcl = o.ordenAten_nhcl
            LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
            WHERE p.mas_nhcl = @Nhcl
            ORDER BY o.ordenAten_id DESC", new { Nhcl = nhcl });

        string cliente = datos?.Cliente ?? "";
        string cedula = datos?.Cedula ?? "";
        string mascota = datos?.Mascota ?? "";
        string celular = datos?.Celular ?? "";
        string telefono = datos?.Telefono ?? "";
        string direccion = datos?.Direccion ?? "";
        string especie = datos?.Especie ?? "";
        string raza = datos?.Raza ?? "";
        string sexo = datos?.Sexo ?? "";
        string medico = datos?.Medico ?? "Médico Veterinario";
        string hoy = DateTime.Today.ToString("yyyy-MM-dd");

        string edadTexto = "";
        if (datos?.FechaNacimiento != null)
        {
            var fn = (DateTime)datos.FechaNacimiento;
            var hoyDt = DateTime.Today;
            int anios = hoyDt.Year - fn.Year;
            if (hoyDt.Month < fn.Month || (hoyDt.Month == fn.Month && hoyDt.Day < fn.Day)) anios--;
            int meses = (hoyDt.Month - fn.Month + 12) % 12;
            edadTexto = anios > 0 ? $"{anios}a {meses}m" : $"{meses} meses";
        }

        string resultado = CorregirCaracteresEspeciales(plantilla)
            .Replace("src=\"imagenes/", "src=\"/imagenes/")
            .Replace("src='imagenes/", "src='/imagenes/")
            .Replace("[NOMBRECLIENTE]", cliente)
            .Replace("[CLIENTE]", cliente)
            .Replace("[CEDULA]", cedula)
            .Replace("[MASCOTA]", mascota)
            .Replace("[NOMBREMASCOTA]", mascota)
            .Replace("[ESPECIE]", especie)
            .Replace("[RAZA]", raza)
            .Replace("[SEXO]", sexo)
            .Replace("[EDAD]", edadTexto)
            .Replace("[USUARIO]", medico)
            .Replace("[MEDICO]", medico)
            .Replace("[NOMBREPIE]", cliente)
            .Replace("[CEDULAPIE]", cedula)
            .Replace("[TELEFONOPIE]", celular)
            .Replace("[CONVENSIONALPIE]", telefono)
            .Replace("[FECHAPIE]", hoy)
            .Replace("[FECHADIA]", hoy)
            .Replace("[FECHA]", hoy)
            .Replace("[NHCL]", nhcl.ToString())
            .Replace("[DIRECCION]", direccion);

        return resultado;
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
                ISNULL(u.user_nombre, 'Médico') AS NombreMedico,
                a.autorizacion_archivo_url AS ArchivoUrl,
                a.autorizacion_archivo_nombre AS ArchivoNombre
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
        if (filtro.Length < 1) return [];

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
                OR (@Criterio = 'nhcl' AND CAST(p.mas_nhcl AS VARCHAR(20)) LIKE '%' + @Filtro + '%')
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
