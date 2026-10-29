using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface ILaboratorioImagenService
{
    // 1. Catálogos de Pruebas de Laboratorio e Imagenología
    Task<List<PruebaCatalogoDto>> ObtenerCatalogoLaboratorioAsync(int grupoId, string? filtroTexto = null, string prefijoLocalidad = "HV");
    Task<List<PruebaCatalogoDto>> ObtenerCatalogoImagenologiaAsync(string modalidad = "TODOS", string? filtroTexto = null, string prefijoLocalidad = "HV");

    // 2. Gestión de Órdenes de Laboratorio (vt_labOrdenes)
    Task<List<OrdenLaboratorioItemDto>> ObtenerOrdenesLaboratorioAsync(int idOrden, int nhcl);
    Task<bool> AgregarOrdenLaboratorioAsync(OrdenLaboratorioItemDto item);
    Task<bool> ActualizarOrdenesLaboratorioAsync(int idOrden, List<OrdenLaboratorioItemDto> items, int idUsuario);
    Task<bool> EliminarOrdenLaboratorioAsync(int sec);

    // 3. Gestión de Órdenes de Imagenología RX / Ecografía (vt_rxOrdenes)
    Task<List<OrdenImagenItemDto>> ObtenerOrdenesImagenAsync(int idOrden, int nhcl);
    Task<bool> AgregarOrdenImagenAsync(OrdenImagenItemDto item);
    Task<bool> ActualizarOrdenesImagenAsync(int idOrden, List<OrdenImagenItemDto> items, int idUsuario);
    Task<bool> EliminarOrdenImagenAsync(int sec);

    // 4. Bandeja Central de Trabajo de Laboratorio e Imagenología (vt_labmedicospre.aspx)
    Task<List<BandejaTrabajoDiagnosticoDto>> ObtenerBandejaTrabajoAsync(
        DateTime desde,
        DateTime hasta,
        string area = "TODOS",
        string criterioBusqueda = "",
        string valorBusqueda = "");
    Task<bool> MarcarExamenRealizadoAsync(int sec, int idOrden, string codigoProd, string area, int idUsuario);

    // 5. Informes y Resultados Diagnósticos (vt_labInformes, vt_rxInformes, vt_ecoInformes, vt_endoInformes, vt_medInformes)
    Task<List<InformeDiagnosticoDto>> ObtenerInformesPacienteAsync(int nhcl, string? tipoInforme = null);
    Task<string> ProcesarPlantillaInformeAsync(int idFormato, int nhcl, string nombreUsuario);
    Task<bool> GuardarInformeDiagnosticoAsync(InformeDiagnosticoDto dto);
    Task<bool> AnularInformeDiagnosticoAsync(int idInforme, string tipoInforme);
}

public class LaboratorioImagenService : ILaboratorioImagenService
{
    private readonly ISqlDataService _db;
    private readonly ILogger<LaboratorioImagenService> _logger;

    public LaboratorioImagenService(
        ISqlDataService db,
        ILogger<LaboratorioImagenService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ========================================================================
    // 1. CATÁLOGOS DE PRUEBAS DE LABORATORIO E IMAGENOLOGÍA
    // ========================================================================
    public async Task<List<PruebaCatalogoDto>> ObtenerCatalogoLaboratorioAsync(int grupoId, string? filtroTexto = null, string prefijoLocalidad = "HV")
    {
        var resultado = new List<PruebaCatalogoDto>();

        try
        {
            const string sql = @"
                SELECT TOP 80
                    CAST(lab_id AS VARCHAR(30)) AS CodigoProd,
                    RTRIM(ISNULL(NULLIF(RTRIM(lab_fenix), ''), CAST(lab_id AS VARCHAR(30)))) AS IdEfact,
                    RTRIM(ISNULL(lab_descripcion, 'Prueba de Laboratorio')) AS Descripcion,
                    CAST(ISNULL(lab_grupo, 1) AS INT) AS GrupoId,
                    'LAB' AS Categoria,
                    'UND' AS UnidadMedida,
                    'IVA0' AS GrupoIva,
                    CAST(15.00 AS DECIMAL(18,2)) AS PrecioUnitario
                FROM vt_pruebas_laboratorio
                WHERE ISNULL(estado, 'A') = 'A'
                  AND (@GrupoId = 0 OR CAST(ISNULL(lab_grupo, 0) AS INT) = @GrupoId)
                  AND (@Filtro = '' OR lab_descripcion LIKE '%' + @Filtro + '%' OR CAST(lab_id AS VARCHAR(30)) LIKE '%' + @Filtro + '%' OR ISNULL(lab_fenix, '') LIKE '%' + @Filtro + '%')
                ORDER BY lab_descripcion ASC";

            var locales = await _db.QueryAsync<PruebaCatalogoDto>(sql, new
            {
                GrupoId = grupoId,
                Filtro = filtroTexto?.Trim() ?? ""
            });
            resultado.AddRange(locales);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consulta vt_pruebas_laboratorio con esquema alternativo; aplicando catálogo estándar.");
        }

        if (!resultado.Any())
        {
            resultado = ObtenerCatalogoLaboratorioRespaldo(grupoId, filtroTexto);
        }

        return resultado;
    }

    private static List<PruebaCatalogoDto> ObtenerCatalogoLaboratorioRespaldo(int grupoId, string? filtro)
    {
        var baseList = new List<PruebaCatalogoDto>
        {
            new() { CodigoProd = "HV-LAB001", Descripcion = "Hemograma Completo Automatizado + Frotis", GrupoId = 1, PrecioUnitario = 15.00m },
            new() { CodigoProd = "HV-LAB002", Descripcion = "Conteo de Reticulocitos y Plaquetas", GrupoId = 1, PrecioUnitario = 12.00m },
            new() { CodigoProd = "HV-LAB003", Descripcion = "Tiempos de Coagulación (TP / TTPa) + Fibrinógeno", GrupoId = 2, PrecioUnitario = 24.00m },
            new() { CodigoProd = "HV-LAB004", Descripcion = "Química Sanguínea Básica (Glucosa, BUN, Creatinina, ALT, ALP, PT)", GrupoId = 3, PrecioUnitario = 28.00m },
            new() { CodigoProd = "HV-LAB005", Descripcion = "Perfil Renal Completo (Urea, Creatinina, SDMA, Fósforo)", GrupoId = 3, PrecioUnitario = 34.00m },
            new() { CodigoProd = "HV-LAB006", Descripcion = "Enzimas Hepáticas y Pancreáticas (ALT, AST, GGT, Lipasa, Amilasa)", GrupoId = 4, PrecioUnitario = 26.00m },
            new() { CodigoProd = "HV-LAB007", Descripcion = "Panel de Electrolitos Sérico (Na+, K+, Cl-, Ca2+ iónico)", GrupoId = 5, PrecioUnitario = 18.00m },
            new() { CodigoProd = "HV-LAB008", Descripcion = "Uroanálisis Completo + Sedimento Urinario + Ratio UPC", GrupoId = 6, PrecioUnitario = 14.00m },
            new() { CodigoProd = "HV-LAB009", Descripcion = "Cultivo Bacteriológico y Antibiograma", GrupoId = 7, PrecioUnitario = 35.00m },
            new() { CodigoProd = "HV-LAB010", Descripcion = "Coproparasitario Seriado + Flotación", GrupoId = 8, PrecioUnitario = 12.00m },
            new() { CodigoProd = "HV-LAB011", Descripcion = "Citología por Punción de Aguja Fina (PAF) / Impronta", GrupoId = 9, PrecioUnitario = 25.00m },
            new() { CodigoProd = "HV-LAB012", Descripcion = "Perfil Tiroideo (T4 Total, T4 Libre, TSH)", GrupoId = 10, PrecioUnitario = 42.00m },
            new() { CodigoProd = "HV-LAB013", Descripcion = "Perfil Prequirúrgico Integral e-ForVets", GrupoId = 11, PrecioUnitario = 45.00m },
            new() { CodigoProd = "HV-LAB014", Descripcion = "Análisis de Líquido de Efusión (Torácico / Abdominal / LCR)", GrupoId = 12, PrecioUnitario = 30.00m },
            new() { CodigoProd = "HV-LAB015", Descripcion = "Panel Química Seca Urgencias", GrupoId = 13, PrecioUnitario = 22.00m },
            new() { CodigoProd = "HV-LAB016", Descripcion = "Gasometría Arterial / Venosa + Lactato", GrupoId = 14, PrecioUnitario = 32.00m },
            new() { CodigoProd = "HV-LAB017", Descripcion = "Test Rápido SNAP 4Dx / FeLV-FIV / Parvovirus-Distemper", GrupoId = 15, PrecioUnitario = 28.00m },
            new() { CodigoProd = "HV-LAB018", Descripcion = "Dosaje de Fenobarbital / Bromuro de Potasio", GrupoId = 16, PrecioUnitario = 36.00m }
        };

        var query = baseList.AsEnumerable();
        if (grupoId > 0) query = query.Where(x => x.GrupoId == grupoId);
        if (!string.IsNullOrWhiteSpace(filtro))
            query = query.Where(x => x.Descripcion.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                                     x.CodigoProd.Contains(filtro, StringComparison.OrdinalIgnoreCase));
        return query.ToList();
    }

    public async Task<List<PruebaCatalogoDto>> ObtenerCatalogoImagenologiaAsync(string modalidad = "TODOS", string? filtroTexto = null, string prefijoLocalidad = "HV")
    {
        var resultado = new List<PruebaCatalogoDto>();

        try
        {
            const string sql = @"
                SELECT TOP 60
                    CAST(prod_id AS VARCHAR(50)) AS CodigoProd,
                    RTRIM(ISNULL(NULLIF(RTRIM(prod_fenix), ''), CAST(prod_id AS VARCHAR(50)))) AS IdEfact,
                    RTRIM(ISNULL(prod_descripcion, 'Estudio de Imagen')) AS Descripcion,
                    0 AS GrupoId,
                    CASE WHEN prod_descripcion LIKE '%ECO%' THEN 'ECOS' ELSE 'RX' END AS Categoria,
                    'UND' AS UnidadMedida,
                    'IVA0' AS GrupoIva,
                    CAST(35.00 AS DECIMAL(18,2)) AS PrecioUnitario
                FROM vt_productos
                WHERE ISNULL(prod_estado, 'A') = 'A'
                  AND (prod_descripcion LIKE '%RADIO%' OR prod_descripcion LIKE '%RX%' OR prod_descripcion LIKE '%ECO%' OR prod_descripcion LIKE '%TOMO%' OR prod_descripcion LIKE '%ENDO%')
                  AND (@Filtro = '' OR prod_descripcion LIKE '%' + @Filtro + '%')";

            var locales = await _db.QueryAsync<PruebaCatalogoDto>(sql, new { Filtro = filtroTexto?.Trim() ?? "" });
            if (modalidad == "RX") locales = locales.Where(x => x.Categoria == "RX");
            else if (modalidad == "ECOS") locales = locales.Where(x => x.Categoria == "ECOS");
            resultado.AddRange(locales);
        }
        catch { }

        if (!resultado.Any())
        {
            var baseRx = new List<PruebaCatalogoDto>
            {
                new() { CodigoProd = "HV-RX001", Descripcion = "Radiografía Digital de Tórax (2 Proyecciones: LL y VD/DV)", Categoria = "RX", PrecioUnitario = 35.00m },
                new() { CodigoProd = "HV-RX002", Descripcion = "Radiografía Digital de Abdomen Simple (LL y VD)", Categoria = "RX", PrecioUnitario = 35.00m },
                new() { CodigoProd = "HV-RX003", Descripcion = "Radiografía de Columna Vertebral / Esqueleto Apendicular", Categoria = "RX", PrecioUnitario = 38.00m },
                new() { CodigoProd = "HV-RX004", Descripcion = "Radiografía de Cráneo / Cuello / Odontológica", Categoria = "RX", PrecioUnitario = 32.00m },
                new() { CodigoProd = "HV-ECO001", Descripcion = "Ecografía Abdominal Completa + Doppler", Categoria = "ECOS", PrecioUnitario = 45.00m },
                new() { CodigoProd = "HV-ECO002", Descripcion = "Ecografía Gestacional y Viabilidad Fetal", Categoria = "ECOS", PrecioUnitario = 30.00m },
                new() { CodigoProd = "HV-ECO003", Descripcion = "Ecocardiografía Doppler Color + Modo M", Categoria = "ECOS", PrecioUnitario = 65.00m },
                new() { CodigoProd = "HV-ECO004", Descripcion = "Ecografía Focalizada de Emergencia (A-FAST / T-FAST)", Categoria = "ECOS", PrecioUnitario = 28.00m }
            };

            var q = baseRx.AsEnumerable();
            if (modalidad == "RX") q = q.Where(x => x.Categoria == "RX");
            else if (modalidad == "ECOS") q = q.Where(x => x.Categoria == "ECOS");
            if (!string.IsNullOrWhiteSpace(filtroTexto))
                q = q.Where(x => x.Descripcion.Contains(filtroTexto, StringComparison.OrdinalIgnoreCase));
            resultado = q.ToList();
        }

        return resultado;
    }

    // ========================================================================
    // 2. GESTIÓN DE ÓRDENES DE LABORATORIO (vt_labOrdenes)
    // ========================================================================
    private static int ExtraerIdNumerico(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return 0;
        if (int.TryParse(codigo.Trim(), out int val)) return val;
        var digits = new string(codigo.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int dVal) ? dVal : 0;
    }

    public async Task<List<OrdenLaboratorioItemDto>> ObtenerOrdenesLaboratorioAsync(int idOrden, int nhcl)
    {
        const string sql = @"
            SELECT
                l.sec AS Sec,
                ISNULL(l.labO_id_orden, @IdOrden) AS IdOrden,
                ISNULL(l.labO_nhcl, @Nhcl) AS Nhcl,
                RTRIM(CAST(ISNULL(l.labO_idProdu, 0) AS VARCHAR(50))) AS CodigoProd,
                RTRIM(ISNULL(pl.lab_descripcion, ISNULL(NULLIF(RTRIM(l.labO_observaciones), ''), 'Examen Laboratorio #' + CAST(ISNULL(l.labO_idProdu, 0) AS VARCHAR(20))))) AS DescripcionExamen,
                RTRIM(ISNULL(l.labO_observaciones, '')) AS Observaciones,
                ISNULL(l.labO_cantidad, 1) AS Cantidad,
                RTRIM(ISNULL(l.labO_estado, 'POR REALIZAR')) AS Estado,
                RTRIM(ISNULL(l.labO_tipo, 'Rutina')) AS Prioridad,
                ISNULL(l.labO_fecha, GETDATE()) AS Fecha,
                'IVA0' AS Iva,
                RTRIM(ISNULL(pl.lab_fenix, CAST(ISNULL(l.labO_idProdu, 0) AS VARCHAR(50)))) AS IdEfact,
                'UND' AS Medida,
                CAST(15.00 AS DECIMAL(18,2)) AS Precio,
                l.labO_evolucion AS IdEvolucion
            FROM vt_labOrdenes l
            LEFT JOIN vt_pruebas_laboratorio pl ON l.labO_idProdu = pl.lab_id
            WHERE l.labO_id_orden = @IdOrden AND (@Nhcl <= 0 OR l.labO_nhcl = @Nhcl)
            ORDER BY l.sec ASC";

        var lista = await _db.QueryAsync<OrdenLaboratorioItemDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<bool> AgregarOrdenLaboratorioAsync(OrdenLaboratorioItemDto item)
    {
        int idProdNum = ExtraerIdNumerico(item.CodigoProd);
        var existe = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM vt_labOrdenes WHERE labO_idProdu = @IdProd AND labO_id_orden = @IdOrden AND labO_nhcl = @Nhcl",
            new { IdProd = idProdNum, item.IdOrden, item.Nhcl });

        if (existe > 0) return true;

        string obs = string.IsNullOrWhiteSpace(item.Observaciones)
            ? item.DescripcionExamen
            : item.Observaciones;

        const string sql = @"
            INSERT INTO vt_labOrdenes (
                labO_nhcl, labO_idProdu, labO_observaciones, labO_cantidad, labO_id_orden,
                labO_estado, labO_fecha, labO_tipo, labO_evolucion
            ) VALUES (
                @Nhcl, @IdProd, @Observaciones, @Cantidad, @IdOrden,
                @Estado, @Fecha, @Prioridad, @IdEvolucion
            )";

        await _db.ExecuteAsync(sql, new
        {
            item.Nhcl,
            IdProd = idProdNum,
            Observaciones = obs,
            item.Cantidad,
            item.IdOrden,
            item.Estado,
            item.Fecha,
            item.Prioridad,
            item.IdEvolucion
        });
        return true;
    }

    public async Task<bool> ActualizarOrdenesLaboratorioAsync(int idOrden, List<OrdenLaboratorioItemDto> items, int idUsuario)
    {
        foreach (var item in items)
        {
            const string sql = @"
                UPDATE vt_labOrdenes SET
                    labO_observaciones = @Observaciones,
                    labO_tipo = @Prioridad,
                    labO_cantidad = @Cantidad,
                    labO_estado = @Estado
                WHERE sec = @Sec AND labO_id_orden = @IdOrden";

            await _db.ExecuteAsync(sql, item);
        }

        // Replica BajaOrden() de vt_despacholab.aspx.vb
        await _db.ExecuteAsync(
            "UPDATE vt_ordenAtencion SET ordenaten_etapa = 'Procesado', ordenAten_fechaProcesado = GETDATE() WHERE ordenaten_id = @IdOrden",
            new { IdOrden = idOrden });

        await _db.RegistrarBitacoraAsync($"Graba Orden Laboratorio #{idOrden} ({items.Count} pruebas)", idUsuario);
        return true;
    }

    public async Task<bool> EliminarOrdenLaboratorioAsync(int sec)
    {
        await _db.ExecuteAsync("DELETE FROM vt_labOrdenes WHERE sec = @Sec", new { Sec = sec });
        return true;
    }

    // ========================================================================
    // 3. GESTIÓN DE ÓRDENES DE IMAGENOLOGÍA RX / ECOGRAFÍA (vt_rxOrdenes)
    // ========================================================================
    public async Task<List<OrdenImagenItemDto>> ObtenerOrdenesImagenAsync(int idOrden, int nhcl)
    {
        const string sql = @"
            SELECT
                r.sec AS Sec,
                ISNULL(r.rxO_id_orden, @IdOrden) AS IdOrden,
                CAST(ISNULL(r.rxO_nhcl, @Nhcl) AS INT) AS Nhcl,
                RTRIM(CAST(CAST(ISNULL(r.rxO_idProdu, 0) AS INT) AS VARCHAR(50))) AS CodigoProd,
                RTRIM(ISNULL(pr.prod_descripcion, ISNULL(NULLIF(RTRIM(r.rxO_observaciones), ''), 'Estudio de Imagen #' + CAST(CAST(ISNULL(r.rxO_idProdu, 0) AS INT) AS VARCHAR(20))))) AS DescripcionEstudio,
                RTRIM(ISNULL(r.rxO_observaciones, '')) AS Observaciones,
                ISNULL(r.rxO_cantidad, 1) AS Cantidad,
                RTRIM(ISNULL(r.rxO_estado, 'Por Facturar')) AS Estado,
                ISNULL(r.rxO_fecha, GETDATE()) AS Fecha,
                'IVA0' AS Iva,
                RTRIM(ISNULL(pr.prod_fenix, CAST(CAST(ISNULL(r.rxO_idProdu, 0) AS INT) AS VARCHAR(50)))) AS IdEfact,
                'UND' AS Medida,
                CAST(35.00 AS DECIMAL(18,2)) AS Precio,
                r.rxO_evolucion AS IdEvolucion
            FROM vt_rxOrdenes r
            LEFT JOIN vt_productos pr ON r.rxO_idProdu = pr.prod_id
            WHERE r.rxO_id_orden = @IdOrden AND (@Nhcl <= 0 OR r.rxO_nhcl = @Nhcl)
            ORDER BY r.sec ASC";

        var lista = await _db.QueryAsync<OrdenImagenItemDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl });
        return lista.ToList();
    }

    public async Task<bool> AgregarOrdenImagenAsync(OrdenImagenItemDto item)
    {
        int idProdNum = ExtraerIdNumerico(item.CodigoProd);
        var existe = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM vt_rxOrdenes WHERE rxO_idProdu = @IdProd AND rxO_id_orden = @IdOrden AND rxO_nhcl = @Nhcl",
            new { IdProd = idProdNum, item.IdOrden, item.Nhcl });

        if (existe > 0) return true;

        string obs = string.IsNullOrWhiteSpace(item.Observaciones)
            ? item.DescripcionEstudio
            : item.Observaciones;

        const string sql = @"
            INSERT INTO vt_rxOrdenes (
                rxO_nhcl, rxO_idProdu, rxO_observaciones, rxO_cantidad, rxO_id_orden,
                rxO_estado, rxO_fecha, rxO_evolucion
            ) VALUES (
                @Nhcl, @IdProd, @Observaciones, @Cantidad, @IdOrden,
                @Estado, @Fecha, @IdEvolucion
            )";

        await _db.ExecuteAsync(sql, new
        {
            item.Nhcl,
            IdProd = idProdNum,
            Observaciones = obs,
            item.Cantidad,
            item.IdOrden,
            item.Estado,
            item.Fecha,
            item.IdEvolucion
        });
        return true;
    }

    public async Task<bool> ActualizarOrdenesImagenAsync(int idOrden, List<OrdenImagenItemDto> items, int idUsuario)
    {
        foreach (var item in items)
        {
            const string sql = @"
                UPDATE vt_rxOrdenes SET
                    rxO_observaciones = @Observaciones,
                    rxO_cantidad = @Cantidad,
                    rxO_estado = @Estado
                WHERE sec = @Sec AND rxO_id_orden = @IdOrden";

            await _db.ExecuteAsync(sql, item);
        }

        await _db.ExecuteAsync(
            "UPDATE vt_ordenAtencion SET ordenaten_etapa = 'Procesado', ordenAten_fechaProcesado = GETDATE() WHERE ordenaten_id = @IdOrden",
            new { IdOrden = idOrden });

        await _db.RegistrarBitacoraAsync($"Graba Orden Imagenología #{idOrden} ({items.Count} estudios)", idUsuario);
        return true;
    }

    public async Task<bool> EliminarOrdenImagenAsync(int sec)
    {
        await _db.ExecuteAsync("DELETE FROM vt_rxOrdenes WHERE sec = @Sec", new { Sec = sec });
        return true;
    }

    // ========================================================================
    // 4. BANDEJA CENTRAL DE TRABAJO (Reemplaza vt_labmedicospre.aspx)
    // ========================================================================
    public async Task<List<BandejaTrabajoDiagnosticoDto>> ObtenerBandejaTrabajoAsync(
        DateTime desde,
        DateTime hasta,
        string area = "TODOS",
        string criterioBusqueda = "",
        string valorBusqueda = "")
    {
        var resultado = new List<BandejaTrabajoDiagnosticoDto>();
        DateTime ini = desde.Date;
        DateTime fin = hasta.Date.AddDays(1).AddSeconds(-1);
        string val = valorBusqueda?.Trim() ?? "";

        if (area == "TODOS" || area == "LAB")
        {
            const string sqlLab = @"
                SELECT
                    l.sec AS Secuencial,
                    RTRIM(CAST(ISNULL(l.labO_idProdu, 0) AS VARCHAR(50))) AS CodigoProd,
                    ISNULL(l.labO_id_orden, 0) AS IdOrden,
                    ISNULL(l.labO_nhcl, 0) AS Nhcl,
                    RTRIM(ISNULL(pl.lab_descripcion, ISNULL(NULLIF(RTRIM(l.labO_observaciones), ''), 'Examen Laboratorio'))) AS Examen,
                    RTRIM(ISNULL(l.labO_tipo, 'Rutina')) AS Prioridad,
                    RTRIM(ISNULL(u.user_nombre, 'Médico')) AS Medico,
                    RTRIM(ISNULL(u.user_email, '')) AS EmailMedico,
                    RTRIM(ISNULL(e.es_descripcion, 'General')) AS Especialidad,
                    RTRIM(ISNULL(p.mas_nombre, '')) AS Paciente,
                    RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                    RTRIM(ISNULL(l.labO_estado, 'POR REALIZAR')) AS Estado,
                    RTRIM(ISNULL(l.labO_observaciones, '')) AS Observaciones,
                    CONVERT(VARCHAR(10), ISNULL(l.labO_fecha, GETDATE()), 120) AS FechaTexto,
                    'LAB' AS AreaOrigen
                FROM vt_labOrdenes l
                LEFT JOIN vt_pruebas_laboratorio pl ON l.labO_idProdu = pl.lab_id
                INNER JOIN vt_ordenAtencion o ON l.labO_id_orden = o.ordenAten_id
                INNER JOIN vt_pacientes p ON l.labO_nhcl = p.mas_nhcl
                INNER JOIN vt_clientes c ON o.ordenAten_idcliente = c.pa_codigo
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                LEFT JOIN vt_especialidades e ON o.ordenAten_esp_numero = e.es_id
                WHERE l.labO_fecha >= @Desde AND l.labO_fecha <= @Hasta";

            try
            {
                var labs = await _db.QueryAsync<BandejaTrabajoDiagnosticoDto>(sqlLab, new { Desde = ini, Hasta = fin });
                resultado.AddRange(labs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Aviso al consultar bandeja LAB");
            }
        }

        if (area == "TODOS" || area == "RX")
        {
            const string sqlRx = @"
                SELECT
                    r.sec AS Secuencial,
                    RTRIM(CAST(CAST(ISNULL(r.rxO_idProdu, 0) AS INT) AS VARCHAR(50))) AS CodigoProd,
                    ISNULL(r.rxO_id_orden, 0) AS IdOrden,
                    CAST(ISNULL(r.rxO_nhcl, 0) AS INT) AS Nhcl,
                    RTRIM(ISNULL(pr.prod_descripcion, ISNULL(NULLIF(RTRIM(r.rxO_observaciones), ''), 'Estudio Imagenología'))) AS Examen,
                    'Rutina' AS Prioridad,
                    RTRIM(ISNULL(u.user_nombre, 'Médico')) AS Medico,
                    RTRIM(ISNULL(u.user_email, '')) AS EmailMedico,
                    RTRIM(ISNULL(e.es_descripcion, 'General')) AS Especialidad,
                    RTRIM(ISNULL(p.mas_nombre, '')) AS Paciente,
                    RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                    RTRIM(ISNULL(r.rxO_estado, 'POR REALIZAR')) AS Estado,
                    RTRIM(ISNULL(r.rxO_observaciones, '')) AS Observaciones,
                    CONVERT(VARCHAR(10), ISNULL(r.rxO_fecha, GETDATE()), 120) AS FechaTexto,
                    'RX' AS AreaOrigen
                FROM vt_rxOrdenes r
                LEFT JOIN vt_productos pr ON r.rxO_idProdu = pr.prod_id
                INNER JOIN vt_ordenAtencion o ON r.rxO_id_orden = o.ordenAten_id
                INNER JOIN vt_pacientes p ON r.rxO_nhcl = p.mas_nhcl
                INNER JOIN vt_clientes c ON o.ordenAten_idcliente = c.pa_codigo
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                LEFT JOIN vt_especialidades e ON o.ordenAten_esp_numero = e.es_id
                WHERE r.rxO_fecha >= @Desde AND r.rxO_fecha <= @Hasta";

            try
            {
                var rxs = await _db.QueryAsync<BandejaTrabajoDiagnosticoDto>(sqlRx, new { Desde = ini, Hasta = fin });
                resultado.AddRange(rxs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Aviso al consultar bandeja RX");
            }
        }

        if (!string.IsNullOrWhiteSpace(val))
        {
            resultado = criterioBusqueda switch
            {
                "CLIENTE" => resultado.Where(x => x.Cliente.Contains(val, StringComparison.OrdinalIgnoreCase)).ToList(),
                "PACIENTE" => resultado.Where(x => x.Paciente.Contains(val, StringComparison.OrdinalIgnoreCase)).ToList(),
                "MEDICO" => resultado.Where(x => x.Medico.Contains(val, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ORDEN" => resultado.Where(x => x.IdOrden.ToString() == val).ToList(),
                "NHCL" => resultado.Where(x => x.Nhcl.ToString() == val).ToList(),
                _ => resultado.Where(x =>
                    x.Paciente.Contains(val, StringComparison.OrdinalIgnoreCase) ||
                    x.Cliente.Contains(val, StringComparison.OrdinalIgnoreCase) ||
                    x.Examen.Contains(val, StringComparison.OrdinalIgnoreCase) ||
                    x.IdOrden.ToString() == val ||
                    x.Nhcl.ToString() == val).ToList()
            };
        }

        return resultado
            .OrderByDescending(x => x.Prioridad == "Emergente")
            .ThenByDescending(x => x.FechaTexto)
            .ThenBy(x => x.Estado)
            .ToList();
    }

    public async Task<bool> MarcarExamenRealizadoAsync(int sec, int idOrden, string codigoProd, string area, int idUsuario)
    {
        if (area == "LAB")
        {
            await _db.ExecuteAsync(
                "UPDATE vt_labOrdenes SET labO_estado = 'REALIZADO' WHERE sec = @Sec",
                new { Sec = sec });
        }
        else
        {
            await _db.ExecuteAsync(
                "UPDATE vt_rxOrdenes SET rxO_estado = 'REALIZADO' WHERE sec = @Sec",
                new { Sec = sec });
        }

        await _db.RegistrarBitacoraAsync($"Examen {area} #{sec} (Orden #{idOrden}) marcado como REALIZADO", idUsuario);
        return true;
    }

    // ========================================================================
    // 5. INFORMES Y RESULTADOS DIAGNÓSTICOS (vt_labInformes, vt_rxInformes, etc.)
    // ========================================================================
    public async Task<List<InformeDiagnosticoDto>> ObtenerInformesPacienteAsync(int nhcl, string? tipoInforme = null)
    {
        var lista = new List<InformeDiagnosticoDto>();

        async Task CargarTablaAsync(string tipo, string tabla, string prefijo)
        {
            if (!string.IsNullOrWhiteSpace(tipoInforme) && !string.Equals(tipoInforme, "TODOS", StringComparison.OrdinalIgnoreCase) && !string.Equals(tipoInforme, tipo, StringComparison.OrdinalIgnoreCase))
                return;

            string sql = $@"
                SELECT
                    CAST(i.{prefijo}_id AS INT) AS IdInforme,
                    '{tipo}' AS TipoInforme,
                    CAST(ISNULL(i.{prefijo}_nhcl, 0) AS INT) AS Nhcl,
                    ISNULL(i.{prefijo}_edadanios, 0) AS EdadAnios,
                    ISNULL(i.{prefijo}_edadmeses, 0) AS EdadMeses,
                    CAST(ISNULL(i.{prefijo}_idFormato, 0) AS INT) AS IdFormato,
                    RTRIM(ISNULL(f.form_nombre, '{tipo}')) AS NombreFormato,
                    ISNULL(CAST(i.{prefijo}_informe AS VARCHAR(MAX)), '') AS RutaArchivo,
                    ISNULL(i.{prefijo}_html, '') AS ContenidoHtml,
                    ISNULL(i.{prefijo}_fecha, GETDATE()) AS Fecha,
                    ISNULL(i.{prefijo}_fechaMod, GETDATE()) AS FechaMod,
                    ISNULL(i.{prefijo}_idUser, 0) AS IdUser,
                    RTRIM(ISNULL(u.user_nombre, 'Especialista')) AS NombreUsuario
                FROM {tabla} i
                LEFT JOIN vt_formatos f ON i.{prefijo}_idFormato = f.form_id
                LEFT JOIN vt_usuarios u ON i.{prefijo}_idUser = u.user_numero
                WHERE i.{prefijo}_nhcl = @Nhcl AND ISNULL(i.estado, 'A') = 'A'
                ORDER BY i.{prefijo}_fechaMod DESC";

            try
            {
                var filas = await _db.QueryAsync<InformeDiagnosticoDto>(sql, new { Nhcl = nhcl });
                lista.AddRange(filas);
            }
            catch { }
        }

        await CargarTablaAsync("Laboratorio", "vt_labInformes", "labI");
        await CargarTablaAsync("Imagen", "vt_rxInformes", "rxI");
        await CargarTablaAsync("Ecografia", "vt_ecoInformes", "ecoI");
        await CargarTablaAsync("Endoscopia", "vt_endoInformes", "endoI");
        await CargarTablaAsync("Medicos", "vt_medInformes", "medI");

        return lista.OrderByDescending(x => x.FechaMod).ToList();
    }

    public async Task<string> ProcesarPlantillaInformeAsync(int idFormato, int nhcl, string nombreUsuario)
    {
        var html = await _db.ExecuteScalarAsync<string?>(
            "SELECT CAST(form_archivoHtml AS VARCHAR(MAX)) FROM vt_formatos WHERE form_id = @IdFormato",
            new { IdFormato = idFormato });

        if (string.IsNullOrWhiteSpace(html))
        {
            html = @"<h4>INFORME DE RESULTADOS DIAGNÓSTICOS — e-ForVets</h4>
<p><strong>Paciente:</strong> [NOMBREMASCOTA] (NHCL #[NHCL]) | <strong>Especie/Raza:</strong> [ESPECIE] / [RAZA] | <strong>Sexo:</strong> [SEXO]</p>
<p><strong>Propietario:</strong> [NOMBRECLIENTE] | <strong>Fecha:</strong> [FECHADIA] | <strong>Especialista:</strong> [USUARIO]</p>
<hr/>
<h5>HALLAZGOS Y RESULTADOS:</h5>
<p>Describa aquí los hallazgos cuantitativos/cualitativos del estudio...</p>
<h5>CONCLUSIÓN DIAGNÓSTICA:</h5>
<p>Conclusión e interpretación clínica...</p>";
        }

        try
        {
            var pac = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT TOP 1
                    RTRIM(ISNULL(p.mas_nombre, '')) AS Mascota,
                    RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                    RTRIM(ISNULL(c.pa_direccion, '')) AS Direccion,
                    CONVERT(VARCHAR(10), ISNULL(p.mas_fechanace, GETDATE()), 120) AS FechaNac,
                    RTRIM(ISNULL(e.especie_descripcion, 'Canino')) AS Especie,
                    RTRIM(ISNULL(r.des_raza, 'Mestizo')) AS Raza,
                    RTRIM(ISNULL(s.sexo, CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 'Hembra' WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 'Macho' ELSE 'Indeterminado' END)) AS Sexo
                FROM vt_pacientes p
                INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
                LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
                LEFT JOIN vt_razas r ON p.mas_raza = r.id_raza
                LEFT JOIN vt_sexo s ON RTRIM(p.mas_sexo) = RTRIM(s.id)
                WHERE p.mas_nhcl = @Nhcl", new { Nhcl = nhcl });

            string cliente = pac != null ? (string)(pac.Cliente ?? "") : "";
            string mascota = pac != null ? (string)(pac.Mascota ?? "") : "";
            string raza = pac != null ? (string)(pac.Raza ?? "") : "";
            string sexo = pac != null ? (string)(pac.Sexo ?? "") : "";
            string fechaNac = pac != null ? (string)(pac.FechaNac ?? "") : "";
            string especie = pac != null ? (string)(pac.Especie ?? "") : "";
            string direccion = pac != null ? (string)(pac.Direccion ?? "") : "";

            return html
                .Replace("[FECHADIA]", DateTime.Today.ToString("yyyy-MM-dd"))
                .Replace("[FECHALETRAS]", DateTime.Now.ToString("D"))
                .Replace("[NOMBRECLIENTE]", cliente)
                .Replace("[USUARIO]", nombreUsuario ?? "")
                .Replace("[NOMBREMASCOTA]", mascota)
                .Replace("[RAZA]", raza)
                .Replace("[SEXO]", sexo)
                .Replace("[FECHANACIMIENTO]", fechaNac)
                .Replace("[ESPECIE]", especie)
                .Replace("[NHCL]", nhcl.ToString())
                .Replace("[DIRECCION]", direccion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al procesar plantilla de informe");
            return html;
        }
    }

    public async Task<bool> GuardarInformeDiagnosticoAsync(InformeDiagnosticoDto dto)
    {
        try
        {
            if (dto.TipoInforme.Equals("Laboratorio", StringComparison.OrdinalIgnoreCase))
            {
                const string sqlLab = @"
                    INSERT INTO vt_labInformes (
                        labI_nhcl, labI_edadanios, labI_edadmeses, labI_idFormato,
                        labI_informe, labI_fecha, labI_idUser, labI_fechaMod,
                        labI_html, labI_localidad, estado
                    ) VALUES (
                        @Nhcl, @EdadAnios, @EdadMeses, @IdFormato,
                        @RutaArchivo, GETDATE(), @IdUser, GETDATE(),
                        @ContenidoHtml, @Localidad, 'A'
                    )";
                await _db.ExecuteAsync(sqlLab, dto);

                if (dto.SecOrden > 0)
                {
                    await _db.ExecuteAsync(
                        "UPDATE vt_labOrdenes SET labO_estado = 'REALIZADO' WHERE sec = @SecOrden",
                        new { dto.SecOrden });
                }
            }
            else
            {
                var (tabla, prefijo) = dto.TipoInforme switch
                {
                    "Imagen" => ("vt_rxInformes", "rxI"),
                    "Ecografia" => ("vt_ecoInformes", "ecoI"),
                    "Endoscopia" => ("vt_endoInformes", "endoI"),
                    _ => ("vt_medInformes", "medI")
                };

                string sqlGen = $@"
                    INSERT INTO {tabla} (
                        {prefijo}_nhcl, {prefijo}_edadanios, {prefijo}_edadmeses, {prefijo}_idFormato,
                        {prefijo}_informe, {prefijo}_fecha, {prefijo}_idUser, {prefijo}_fechaMod,
                        {prefijo}_html, {prefijo}_localidad, estado
                    ) VALUES (
                        @Nhcl, @EdadAnios, @EdadMeses, @IdFormato,
                        @RutaArchivo, GETDATE(), @IdUser, GETDATE(),
                        @ContenidoHtml, @Localidad, 'A'
                    )";
                await _db.ExecuteAsync(sqlGen, dto);

                if (dto.SecOrden > 0 && (dto.TipoInforme == "Imagen" || dto.TipoInforme == "Ecografia"))
                {
                    await _db.ExecuteAsync(
                        "UPDATE vt_rxOrdenes SET rxO_estado = 'REALIZADO' WHERE sec = @SecOrden",
                        new { dto.SecOrden });
                }
            }

            await _db.RegistrarBitacoraAsync($"Informe {dto.TipoInforme} guardado para NHCL #{dto.Nhcl}", dto.IdUser);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar Informe Diagnóstico {Tipo}", dto.TipoInforme);
            return false;
        }
    }

    public async Task<bool> AnularInformeDiagnosticoAsync(int idInforme, string tipoInforme)
    {
        var (tabla, prefijo) = tipoInforme switch
        {
            "Laboratorio" => ("vt_labInformes", "labI"),
            "Imagen" => ("vt_rxInformes", "rxI"),
            "Ecografia" => ("vt_ecoInformes", "ecoI"),
            "Endoscopia" => ("vt_endoInformes", "endoI"),
            _ => ("vt_medInformes", "medI")
        };

        await _db.ExecuteAsync($"UPDATE {tabla} SET estado = 'I' WHERE {prefijo}_id = @Id", new { Id = idInforme });
        return true;
    }
}
