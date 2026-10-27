using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IQuirofanoService
{
    // 1. Pre-Anestesia (qvt_preanestesia + qvt_productosUtilizados)
    Task<PreAnestesiaDto> ObtenerPreAnestesiaAsync(int idOrden, int nhcl);
    Task<bool> GuardarPreAnestesiaAsync(PreAnestesiaDto dto, int idUsuario, string nombreUsuario);
    Task<List<ProductoQuirofanoDto>> ObtenerProductosQuirofanoAsync(int idOrden, string modulo);
    Task<bool> AgregarProductoQuirofanoAsync(ProductoQuirofanoDto prod, int idUsuario, int idLocalidad, string origReserva = "QUI");
    Task<bool> EliminarProductoQuirofanoAsync(int idProducto, string codigoProd, int idOrden, string origReserva = "QUI");

    // 2. Hoja de Anestesia Intraoperatoria (qvt_RegistroAnestesia + 10 tablas vinculadas)
    Task<HojaAnestesiaCompletaDto> ObtenerHojaAnestesiaAsync(int idOrden, int nhcl, int numFormulario = 1);
    Task<bool> GuardarHojaAnestesiaAsync(HojaAnestesiaCompletaDto dto, int idUsuario, string nombreUsuario);

    // 3. Protocolo Quirúrgico, Equipo y Chequeo de Cirugía (qvt_Protocolo + qvt_ingresocirugia + qvt_team)
    Task<ProtocoloQuirurgicoCompletoDto> ObtenerProtocoloQuirurgicoAsync(int idOrden, int nhcl);
    Task<bool> GuardarProtocoloQuirurgicoAsync(ProtocoloQuirurgicoCompletoDto dto, int idUsuario, string nombreUsuario);
    Task<bool> AgregarMiembroEquipoAsync(MiembroEquipoQuirurgicoDto miembro);
    Task<bool> EliminarMiembroEquipoAsync(int sec);

    // 4. Sala de Recuperación y Post-Operatorio (qvt_recuperacion + qvt_post_operatorio + qvt_ingliqadm + qvt_observaciones_recuperacion)
    Task<List<RecuperacionSignoVitalDto>> ObtenerSignosRecuperacionAsync(int idOrden);
    Task<bool> GuardarSignoRecuperacionAsync(RecuperacionSignoVitalDto signo, int idLocalidad);
    Task<bool> EliminarSignoRecuperacionAsync(int id);
    Task<PostOperatorioChecklistDto> ObtenerPostOperatorioChecklistAsync(int idOrden, int nhcl);
    Task<bool> GuardarPostOperatorioChecklistAsync(PostOperatorioChecklistDto dto, int idUsuario, int idLocalidad);
    Task<List<LiquidoRecuperacionDto>> ObtenerLiquidosRecuperacionAsync(int idOrden, string tipo);
    Task<bool> AgregarLiquidoRecuperacionAsync(LiquidoRecuperacionDto dto, int idUsuario, int idLocalidad);
    Task<bool> EliminarLiquidoRecuperacionAsync(int id, string codigoProd, int idOrden, int nhcl);
    Task<List<ObservacionRecuperacionDto>> ObtenerObservacionesRecuperacionAsync(int idOrden);
    Task<bool> AgregarObservacionRecuperacionAsync(int idOrden, int nhcl, string observacion, int idUsuario, int idLocalidad);
    Task<bool> EliminarObservacionRecuperacionAsync(int id);
}

public class QuirofanoService : IQuirofanoService
{
    private readonly ISqlDataService _db;
    private readonly IInventarioService _inventarioService;
    private readonly ILogger<QuirofanoService> _logger;

    public QuirofanoService(
        ISqlDataService db,
        IInventarioService inventarioService,
        ILogger<QuirofanoService> logger)
    {
        _db = db;
        _inventarioService = inventarioService;
        _logger = logger;
    }

    // ========================================================================
    // 1. EVALUACIÓN PRE-ANESTÉSICA (Reemplaza vt_PreAnestesia.aspx.vb)
    // ========================================================================
    public async Task<PreAnestesiaDto> ObtenerPreAnestesiaAsync(int idOrden, int nhcl)
    {
        const string sql = @"
            SELECT TOP 1
                idPreAnestesico AS IdPreAnestesico,
                ISNULL(orden, @IdOrden) AS Orden,
                ISNULL(nhcl, @Nhcl) AS Nhcl,
                ISNULL(iduser, 0) AS IdUser,
                ISNULL(fechaingreso, GETDATE()) AS FechaIngreso,
                ISNULL(riesgo, 1) AS Riesgo,
                ISNULL(Ebueno, 0) AS Ebueno,
                ISNULL(Edeprimido, 0) AS Edeprimido,
                ISNULL(Einsuficiente, 0) AS Einsuficiente,
                ISNULL(InformacionEspecial, '') AS InformacionEspecial,
                ISNULL(GrupoSanguineo, '') AS GrupoSanguineo,
                ISNULL(uroanalisis, '') AS Uroanalisis,
                ISNULL(hematologico, '') AS Hematologico,
                ISNULL(quimicasanguinea, '') AS QuimicaSanguinea,
                ISNULL(sistemarespiratorio, '') AS SistemaRespiratorio,
                ISNULL(sistemacirculatorio, '') AS SistemaCirculatorio,
                ISNULL(sistemanervioso, '') AS SistemaNervioso,
                ISNULL(sistemasotros, '') AS SistemasOtros,
                ISNULL(anestesiasanteriores, '') AS AnestesiasAnteriores,
                ISNULL(terapiaanterior, '') AS TerapiaAnterior,
                ultimaingestion AS UltimaIngestion,
                horadolor AS HoraDolor,
                ISNULL(rcabeza, 0) AS Rcabeza,
                ISNULL(rsentidos, 0) AS Rsentidos,
                ISNULL(rendoscopias, 0) AS Rendoscopias,
                ISNULL(rintradural, 0) AS Rintradural,
                ISNULL(rextradural, 0) AS Rextradural,
                ISNULL(rcuello, 0) AS Rcuello,
                ISNULL(rextratoraxica, 0) AS Rextratoraxica,
                ISNULL(rintratoraxica, 0) AS Rintratoraxica,
                ISNULL(rabdomen, 0) AS Rabdomen,
                ISNULL(rextremidades, 0) AS Rextremidades,
                ISNULL(robstetricas, 0) AS Robstetricas,
                ISNULL(rperiniales, 0) AS Rperiniales,
                ISNULL(firma, '') AS Firma
            FROM qvt_preanestesia
            WHERE orden = @IdOrden
            ORDER BY idPreAnestesico DESC";

        try
        {
            var dto = await _db.QueryFirstOrDefaultAsync<PreAnestesiaDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl });
            return dto ?? new PreAnestesiaDto { Orden = idOrden, Nhcl = nhcl };
        }
        catch
        {
            return new PreAnestesiaDto { Orden = idOrden, Nhcl = nhcl };
        }
    }

    public async Task<bool> GuardarPreAnestesiaAsync(PreAnestesiaDto dto, int idUsuario, string nombreUsuario)
    {
        try
        {
            dto.IdUser = idUsuario;
            if (string.IsNullOrWhiteSpace(dto.Firma))
                dto.Firma = nombreUsuario;

            var existeId = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 idPreAnestesico FROM qvt_preanestesia WHERE orden = @Orden",
                new { dto.Orden });

            if (existeId.HasValue && existeId.Value > 0)
            {
                const string sqlUpdate = @"
                    UPDATE qvt_preanestesia SET
                        riesgo = @Riesgo, Ebueno = @Ebueno, Edeprimido = @Edeprimido, Einsuficiente = @Einsuficiente,
                        InformacionEspecial = @InformacionEspecial, GrupoSanguineo = @GrupoSanguineo,
                        uroanalisis = @Uroanalisis, hematologico = @Hematologico, quimicasanguinea = @QuimicaSanguinea,
                        sistemarespiratorio = @SistemaRespiratorio, sistemacirculatorio = @SistemaCirculatorio,
                        sistemanervioso = @SistemaNervioso, sistemasotros = @SistemasOtros,
                        anestesiasanteriores = @AnestesiasAnteriores, terapiaanterior = @TerapiaAnterior,
                        ultimaingestion = @UltimaIngestion, horadolor = @HoraDolor,
                        rcabeza = @Rcabeza, rsentidos = @Rsentidos, rendoscopias = @Rendoscopias,
                        rintradural = @Rintradural, rextradural = @Rextradural, rcuello = @Rcuello,
                        rextratoraxica = @Rextratoraxica, rintratoraxica = @Rintratoraxica, rabdomen = @Rabdomen,
                        rextremidades = @Rextremidades, robstetricas = @Robstetricas, rperiniales = @Rperiniales,
                        firma = @Firma, iduser = @IdUser
                    WHERE idPreAnestesico = @IdPreAnestesico";

                dto.IdPreAnestesico = existeId.Value;
                await _db.ExecuteAsync(sqlUpdate, dto);
            }
            else
            {
                const string sqlInsert = @"
                    INSERT INTO qvt_preanestesia (
                        orden, nhcl, iduser, fechaingreso, riesgo, Ebueno, Edeprimido, Einsuficiente,
                        InformacionEspecial, GrupoSanguineo, uroanalisis, hematologico, quimicasanguinea,
                        sistemarespiratorio, sistemacirculatorio, sistemanervioso, sistemasotros,
                        anestesiasanteriores, terapiaanterior, ultimaingestion, horadolor,
                        rcabeza, rsentidos, rendoscopias, rintradural, rextradural, rcuello,
                        rextratoraxica, rintratoraxica, rabdomen, rextremidades, robstetricas, rperiniales, firma
                    ) VALUES (
                        @Orden, @Nhcl, @IdUser, GETDATE(), @Riesgo, @Ebueno, @Edeprimido, @Einsuficiente,
                        @InformacionEspecial, @GrupoSanguineo, @Uroanalisis, @Hematologico, @QuimicaSanguinea,
                        @SistemaRespiratorio, @SistemaCirculatorio, @SistemaNervioso, @SistemasOtros,
                        @AnestesiasAnteriores, @TerapiaAnterior, @UltimaIngestion, @HoraDolor,
                        @Rcabeza, @Rsentidos, @Rendoscopias, @Rintradural, @Rextradural, @Rcuello,
                        @Rextratoraxica, @Rintratoraxica, @Rabdomen, @Rextremidades, @Robstetricas, @Rperiniales, @Firma
                    )";
                await _db.ExecuteAsync(sqlInsert, dto);
            }

            await _db.RegistrarBitacoraAsync(
                $"Pre-Anestesia guardada Orden {dto.Orden} (ASA {dto.Riesgo})",
                idUsuario);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar Pre-Anestesia Orden {Orden}", dto.Orden);
            return false;
        }
    }

    public async Task<List<ProductoQuirofanoDto>> ObtenerProductosQuirofanoAsync(int idOrden, string modulo)
    {
        const string sql = @"
            SELECT
                id AS Id,
                ISNULL(ordenId, 0) AS OrdenId,
                ISNULL(nhcl, 0) AS Nhcl,
                ISNULL(hora, '') AS Hora,
                ISNULL(fecha, GETDATE()) AS Fecha,
                CAST(ISNULL(codigoprod, 0) AS VARCHAR(50)) AS CodigoProd,
                ISNULL(proddescripcion, '') AS ProdDescripcion,
                ISNULL(precioprod, 0) AS PrecioProd,
                ISNULL(stockprod, 0) AS StockProd,
                ISNULL(indicacionesprod, '') AS IndicacionesProd,
                CAST(ISNULL(cantprod, 1) AS DECIMAL(18,2)) AS CantProd,
                ISNULL(via, '') AS Via,
                ISNULL(iniciales, '') AS Iniciales,
                ISNULL(funcion, '') AS Funcion,
                ISNULL(funcionario, '') AS Funcionario,
                ISNULL(estado, 'Vigente') AS Estado,
                ISNULL(modulo, '') AS Modulo,
                'FARMA-HDEV' AS MedBodega,
                'UND' AS MedMedida
            FROM qvt_productosUtilizados
            WHERE ordenId = @IdOrden
              AND (@Modulo = '' OR modulo = @Modulo)
            ORDER BY id DESC";

        try
        {
            var lista = await _db.QueryAsync<ProductoQuirofanoDto>(sql, new { IdOrden = idOrden, Modulo = modulo });
            return lista.ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> AgregarProductoQuirofanoAsync(ProductoQuirofanoDto prod, int idUsuario, int idLocalidad, string origReserva = "QUI")
    {
        try
        {
            const string sqlInsert = @"
                INSERT INTO qvt_productosUtilizados (
                    ordenId, nhcl, hora, fecha, codigoprod, precioprod, stockprod, indicacionesprod,
                    cantprod, usuario, localidad, proddescripcion, via, origen, iniciales, funcion,
                    funcionario, estado, grupo, modulo, tipo, med_iva, med_iddinamics, med_medida, med_bodega
                ) VALUES (
                    @OrdenId, @Nhcl, @Hora, @Fecha, @CodigoProd, @PrecioProd, @StockProd, @IndicacionesProd,
                    @CantProd, @Usuario, @Localidad, @ProdDescripcion, @Via, 'E', @Iniciales, @Funcion,
                    @Funcionario, 'Vigente', 'FARMACO', @Modulo, 'M', @MedIva, @MedIdDinamics, @MedMedida, @MedBodega
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int idCreado = await _db.ExecuteScalarAsync<int>(sqlInsert, new
            {
                prod.OrdenId,
                prod.Nhcl,
                prod.Hora,
                prod.Fecha,
                prod.CodigoProd,
                prod.PrecioProd,
                prod.StockProd,
                prod.IndicacionesProd,
                prod.CantProd,
                Usuario = idUsuario,
                Localidad = idLocalidad,
                prod.ProdDescripcion,
                prod.Via,
                prod.Iniciales,
                prod.Funcion,
                prod.Funcionario,
                prod.Modulo,
                prod.MedIva,
                prod.MedIdDinamics,
                prod.MedMedida,
                prod.MedBodega
            });

            if (idCreado > 0 && !string.IsNullOrWhiteSpace(prod.CodigoProd))
            {
                await _inventarioService.GuardarReservaStockAsync(
                    prod.CodigoProd,
                    idUsuario,
                    prod.CantProd,
                    idLocalidad,
                    prod.MedBodega,
                    prod.OrdenId,
                    prod.Nhcl,
                    idCreado,
                    origReserva);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar producto de quirófano en Orden {OrdenId}", prod.OrdenId);
            return false;
        }
    }

    public async Task<bool> EliminarProductoQuirofanoAsync(int idProducto, string codigoProd, int idOrden, string origReserva = "QUI")
    {
        try
        {
            await _db.ExecuteAsync("DELETE FROM qvt_productosUtilizados WHERE id = @Id", new { Id = idProducto });
            await _inventarioService.EliminarReservaStockAsync(codigoProd, idOrden, idProducto, origReserva);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar producto de quirófano {Id}", idProducto);
            return false;
        }
    }

    // ========================================================================
    // 2. HOJA DE ANESTESIA INTRAOPERATORIA (Reemplaza vt_hojaanestesia.aspx.vb)
    // ========================================================================
    public async Task<HojaAnestesiaCompletaDto> ObtenerHojaAnestesiaAsync(int idOrden, int nhcl, int numFormulario = 1)
    {
        var hoja = new HojaAnestesiaCompletaDto
        {
            IdOrden = idOrden,
            Nhcl = nhcl,
            NumFormulario = numFormulario
        };

        // Asegurar los 16 slots de tiempo (cada 15 minutos por defecto)
        if (hoja.SlotsTiempo.Count < 16)
        {
            hoja.SlotsTiempo.Clear();
            for (int i = 1; i <= 16; i++)
            {
                hoja.SlotsTiempo.Add(new SlotGraficaAnestesiaDto
                {
                    Orden = i,
                    HoraLabel = DateTime.Today.AddHours(8).AddMinutes((i - 1) * 15).ToString("HH:mm")
                });
            }
        }

        try
        {
            // 1. Cabecera qvt_RegistroAnestesia
            var reg = await _db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT TOP 1 * FROM qvt_RegistroAnestesia WHERE idOrden = @IdOrden ORDER BY idRegistro DESC",
                new { IdOrden = idOrden });

            if (reg != null)
            {
                hoja.IdRegistro = (int)reg.idRegistro;
                hoja.Fecha = reg.fecha != null ? (DateTime)reg.fecha : DateTime.Today;
                hoja.Sexo = reg.sexo?.ToString() ?? "";
                hoja.Asa = reg.asa?.ToString() ?? "I";
                hoja.HoraInicio = reg.horaInicio?.ToString() ?? "08:00";
                hoja.HoraFin = reg.horaFin?.ToString() ?? "";
                hoja.TipoAnestesia = reg.tipoAnestesia != null ? (int)reg.tipoAnestesia : 1;
                hoja.Firma = reg.firma?.ToString() ?? "";

                int idReg = hoja.IdRegistro;

                // 2. Manejo de Vía Aérea
                var via = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM qvt_ManejoViaAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
                if (via != null)
                {
                    hoja.MascaraFacial = via.mascaraFacial == true;
                    hoja.CateterNasal = via.cateterNasal == true;
                    hoja.MascaraLaringea = via.M_laringea == true;
                    hoja.NumMascaraLaringea = via.numMascaralaringea?.ToString() ?? "";
                    hoja.Traqueotomo = via.Traqueotomo == true;
                    hoja.IntubOral = via.intubOral == true;
                    hoja.IntubNasal = via.intubNasal == true;
                    hoja.NumTubo = via.NumTubo?.ToString() ?? "";
                    hoja.IntubBalon = via.intubBalon == true;
                    hoja.DifCormack = via.difCormack != null ? (int)via.difCormack : 1;
                    hoja.NIntentos = via.nIntentos != null ? (int)via.nIntentos : 1;
                    hoja.VolTidal = via.volTidal != null ? (int)via.volTidal : 0;
                    hoja.FR = via.FR?.ToString() ?? "";
                    hoja.O2 = via.o2 != null ? (int)via.o2 : 100;
                }

                // 3. Monitoreo Anestésico
                var mon = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM qvt_MonitoreoAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
                if (mon != null)
                {
                    hoja.EKG = mon.EKG == true;
                    hoja.SPO2 = mon.SPO2 == true;
                    hoja.CO2ET = mon.CO2ET == true;
                    hoja.PANI = mon.PANI == true;
                    hoja.TempMonitoreo = mon.T == true;
                    hoja.PVC = mon.PVC == true;
                    hoja.SondaVesical = mon.S_Vesical == true;
                    hoja.SNG = mon.SNG == true;
                    hoja.AnalizadorGases = mon.Analizador_gases == true;
                    hoja.Gasometria = mon.gasometria == true;
                    hoja.OtraMonitorizacion = mon.OTRA?.ToString() ?? "";
                }

                // 4. Técnica y Ventilación
                var tec = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM qvt_tecnicaAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
                if (tec != null)
                {
                    hoja.TecnicaGeneral = tec.general == true;
                    hoja.TecnicaRegional = tec.regional == true;
                    hoja.TecnicaEpidural = tec.epidural == true;
                    hoja.TecnicaSedacion = tec.sedacion == true;
                    hoja.TecnicaLocal = tec.local == true;
                    hoja.Induccion = tec.induccion?.ToString() ?? "";
                    hoja.Mantenimiento = tec.mantenimiento?.ToString() ?? "";
                    hoja.BloqueoRegional = tec.bloqueo_nervio?.ToString() ?? "";
                }

                var vent = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM qvt_ventilacionAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
                if (vent != null)
                {
                    hoja.VentEspontanea = vent.espontanea == true;
                    hoja.VentMecanica = vent.mecanica == true;
                    hoja.VentAsistida = vent.asistida == true;
                    hoja.PEEP = vent.peep?.ToString() ?? "";
                }

                // 5. Ingresos y Egresos Anestesia
                var ing = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM qvt_IngresosAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
                if (ing != null)
                {
                    hoja.IngDextrosa = ing.dextrosa != null ? Convert.ToDouble(ing.dextrosa) : 0;
                    hoja.IngSalina09 = ing.s_salina_09 != null ? Convert.ToDouble(ing.s_salina_09) : 0;
                    hoja.IngLactatoRinger = ing.l_ringer != null ? Convert.ToDouble(ing.l_ringer) : 0;
                    hoja.IngSangre = ing.sangre != null ? Convert.ToDouble(ing.sangre) : 0;
                    hoja.IngPlasma = ing.plasma != null ? Convert.ToDouble(ing.plasma) : 0;
                    hoja.IngExpansor = ing.expansor != null ? Convert.ToDouble(ing.expansor) : 0;
                    hoja.IngManitol = ing.manitol != null ? Convert.ToDouble(ing.manitol) : 0;
                    hoja.IngOtros = ing.otros != null ? Convert.ToDouble(ing.otros) : 0;
                }

                var egr = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM qvt_egresosAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
                if (egr != null)
                {
                    hoja.EgrSangrado = egr.sangrado != null ? Convert.ToDouble(egr.sangrado) : 0;
                    hoja.EgrDiuresis = egr.diuresis != null ? Convert.ToDouble(egr.diuresis) : 0;
                    hoja.EgrPerdidasInsensibles = egr.p_insensibles != null ? Convert.ToDouble(egr.p_insensibles) : 0;
                    hoja.EgrOtros = egr.otros != null ? Convert.ToDouble(egr.otros) : 0;
                }

                var dur = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM qvt_duracionAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
                if (dur != null)
                {
                    hoja.DuracionAnestesiaHoras = dur.duracionAnestesiaHoras != null ? (int)dur.duracionAnestesiaHoras : 0;
                    hoja.DuracionAnestesiaMinutos = dur.duracionAnestesiaMinutos != null ? (int)dur.duracionAnestesiaMinutos : 0;
                    hoja.DuracionCirugiaHoras = dur.duracionCirugiaHoras != null ? (int)dur.duracionCirugiaHoras : 0;
                    hoja.DuracionCirugiaMin = dur.duracionCirugiaMin != null ? (int)dur.duracionCirugiaMin : 0;
                    hoja.Comentarios = dur.comentarios?.ToString() ?? "";
                }
            }

            // 6. Descripciones de los 15 agentes (qvt_graficaDescripcion)
            var desc = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 * FROM qvt_graficaDescripcion WHERE idOrden = @IdOrden AND ISNULL(numFormulario, 1) = @NumForm",
                new { IdOrden = idOrden, NumForm = numFormulario });
            if (desc != null)
            {
                string[] dbDescs = new string[]
                {
                    desc.descripcion1?.ToString() ?? "", desc.descripcion2?.ToString() ?? "", desc.descripcion3?.ToString() ?? "",
                    desc.descripcion4?.ToString() ?? "", desc.descripcion5?.ToString() ?? "", desc.descripcion6?.ToString() ?? "",
                    desc.descripcion7?.ToString() ?? "", desc.descripcion8?.ToString() ?? "", desc.descripcion9?.ToString() ?? "",
                    desc.descripcion10?.ToString() ?? "", desc.descripcion11?.ToString() ?? "", desc.descripcion12?.ToString() ?? "",
                    desc.descripcion13?.ToString() ?? "", desc.descripcion14?.ToString() ?? "", desc.descripcion15?.ToString() ?? ""
                };
                for (int i = 0; i < 15; i++)
                {
                    if (!string.IsNullOrWhiteSpace(dbDescs[i]))
                        hoja.NombresAgentes[i] = dbDescs[i];
                }
            }

            // 7. Etiquetas de Tiempo (qvt_graficaAnestesiaTiempo)
            var tiempos = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 * FROM qvt_graficaAnestesiaTiempo WHERE idOrden = @IdOrden ORDER BY sec DESC",
                new { IdOrden = idOrden });
            if (tiempos != null)
            {
                string?[] tLabels = new string?[]
                {
                    tiempos.tiempo1?.ToString(), tiempos.tiempo2?.ToString(), tiempos.tiempo3?.ToString(), tiempos.tiempo4?.ToString(),
                    tiempos.tiempo5?.ToString(), tiempos.tiempo6?.ToString(), tiempos.tiempo7?.ToString(), tiempos.tiempo8?.ToString(),
                    tiempos.tiempo9?.ToString(), tiempos.tiempo10?.ToString(), tiempos.tiempo11?.ToString(), tiempos.tiempo12?.ToString(),
                    tiempos.tiempo13?.ToString(), tiempos.tiempo14?.ToString(), tiempos.tiempo15?.ToString(), tiempos.tiempo16?.ToString()
                };
                for (int i = 0; i < 16; i++)
                {
                    if (!string.IsNullOrWhiteSpace(tLabels[i]))
                        hoja.SlotsTiempo[i].HoraLabel = tLabels[i]!;
                }
            }

            // 8. 16 Puntos de la Gráfica de Anestesia (qvt_graficaAnestesia)
            if (hoja.IdRegistro > 0)
            {
                var puntos = await _db.QueryAsync<dynamic>(
                    @"SELECT * FROM qvt_graficaAnestesia
                      WHERE idRegistro = @IdReg AND ISNULL(numFormulario, 1) = @NumForm
                      ORDER BY orden ASC",
                    new { IdReg = hoja.IdRegistro, NumForm = numFormulario });

                foreach (var p in puntos)
                {
                    int ord = p.orden != null ? (int)p.orden : 0;
                    if (ord >= 1 && ord <= 16)
                    {
                        var slot = hoja.SlotsTiempo[ord - 1];
                        slot.Id = (int)p.id;
                        slot.PresionSistolica = p.presionSistolica != null ? (int?)p.presionSistolica : null;
                        slot.PresionDiastolica = p.presionDiastolica != null ? (int?)p.presionDiastolica : null;
                        slot.TensionArterialMedia = p.tesionArterialMedia?.ToString() ?? "";
                        slot.FrecuenciaCardiaca = p.frecuenciaCardiaca != null ? (int?)p.frecuenciaCardiaca : null;
                        slot.Temperatura = p.temperatura != null ? (int?)p.temperatura : null;
                        slot.O2L = p.O2L != null ? (int?)p.O2L : null;
                        slot.EtCO2 = p.etco2 != null ? (int?)p.etco2 : null;
                        slot.Induccion = p.induccion?.ToString() ?? "";
                        slot.FinAnestesia = p.finAnestesia?.ToString() ?? "";
                        slot.RespEspontanea = p.respEspontanea?.ToString() ?? "";
                        slot.RespAsistida = p.respAsistida?.ToString() ?? "";
                        slot.RespControlada = p.respControlada?.ToString() ?? "";

                        slot.ValoresAgentes[0] = p.valorDescripcion1?.ToString() ?? "";
                        slot.ValoresAgentes[1] = p.valorDescripcion2?.ToString() ?? "";
                        slot.ValoresAgentes[2] = p.valorDescripcion3?.ToString() ?? "";
                        slot.ValoresAgentes[3] = p.valorDescripcion4?.ToString() ?? "";
                        slot.ValoresAgentes[4] = p.valorDescripcion5?.ToString() ?? "";
                        slot.ValoresAgentes[5] = p.valorDescripcion6?.ToString() ?? "";
                        slot.ValoresAgentes[6] = p.valorDescripcion7?.ToString() ?? "";
                        slot.ValoresAgentes[7] = p.valorDescripcion8?.ToString() ?? "";
                        slot.ValoresAgentes[8] = p.valorDescripcion9?.ToString() ?? "";
                        slot.ValoresAgentes[9] = p.valorDescripcion10?.ToString() ?? "";
                        slot.ValoresAgentes[10] = p.valorDescripcion11?.ToString() ?? "";
                        slot.ValoresAgentes[11] = p.valorDescripcion12?.ToString() ?? "";
                        slot.ValoresAgentes[12] = p.valorDescripcion13?.ToString() ?? "";
                        slot.ValoresAgentes[13] = p.valorDescripcion14?.ToString() ?? "";
                        slot.ValoresAgentes[14] = p.valorDescripcion15?.ToString() ?? "";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Aviso al cargar Hoja de Anestesia para Orden {IdOrden}", idOrden);
        }

        return hoja;
    }

    public async Task<bool> GuardarHojaAnestesiaAsync(HojaAnestesiaCompletaDto dto, int idUsuario, string nombreUsuario)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Firma))
                dto.Firma = nombreUsuario;

            // 1. Upsert en qvt_RegistroAnestesia
            var regId = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 idRegistro FROM qvt_RegistroAnestesia WHERE idOrden = @IdOrden",
                new { dto.IdOrden });

            if (regId.HasValue && regId.Value > 0)
            {
                dto.IdRegistro = regId.Value;
                await _db.ExecuteAsync(@"
                    UPDATE qvt_RegistroAnestesia SET
                        fecha = @Fecha, asa = @Asa, horaInicio = @HoraInicio, horaFin = @HoraFin,
                        tipoAnestesia = @TipoAnestesia, firma = @Firma, atendido = 1
                    WHERE idRegistro = @IdRegistro", dto);
            }
            else
            {
                dto.IdRegistro = await _db.ExecuteScalarAsync<int>(@"
                    INSERT INTO qvt_RegistroAnestesia (
                        fecha, sexo, asa, idOrden, nhcl, atendido, fechaRegistro, horaInicio, horaFin, tipoAnestesia, firma
                    ) VALUES (
                        @Fecha, @Sexo, @Asa, @IdOrden, @Nhcl, 1, GETDATE(), @HoraInicio, @HoraFin, @TipoAnestesia, @Firma
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", dto);
            }

            int idReg = dto.IdRegistro;

            // 2. Upsert Manejo Vía Aérea
            await _db.ExecuteAsync("DELETE FROM qvt_ManejoViaAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_ManejoViaAnestesia (
                    idRegistro, mascaraFacial, cateterNasal, M_laringea, numMascaralaringea, Traqueotomo,
                    intubOral, intubNasal, NumTubo, intubBalon, difCormack, nIntentos, volTidal, FR, o2
                ) VALUES (
                    @IdRegistro, @MascaraFacial, @CateterNasal, @MascaraLaringea, @NumMascaraLaringea, @Traqueotomo,
                    @IntubOral, @IntubNasal, @NumTubo, @IntubBalon, @DifCormack, @NIntentos, @VolTidal, @FR, @O2
                )", dto);

            // 3. Upsert Monitoreo Anestesia
            await _db.ExecuteAsync("DELETE FROM qvt_MonitoreoAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_MonitoreoAnestesia (
                    idRegistro, EKG, SPO2, CO2ET, PANI, T, PVC, S_Vesical, SNG, Analizador_gases, gasometria, OTRA
                ) VALUES (
                    @IdRegistro, @EKG, @SPO2, @CO2ET, @PANI, @TempMonitoreo, @PVC, @SondaVesical, @SNG, @AnalizadorGases, @Gasometria, @OtraMonitorizacion
                )", dto);

            // 4. Upsert Técnica y Ventilación
            await _db.ExecuteAsync("DELETE FROM qvt_tecnicaAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_tecnicaAnestesia (
                    idRegistro, general, regional, epidural, sedacion, local, induccion, mantenimiento, bloqueo_nervio
                ) VALUES (
                    @IdRegistro, @TecnicaGeneral, @TecnicaRegional, @TecnicaEpidural, @TecnicaSedacion, @TecnicaLocal, @Induccion, @Mantenimiento, @BloqueoRegional
                )", dto);

            await _db.ExecuteAsync("DELETE FROM qvt_ventilacionAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_ventilacionAnestesia (idRegistro, espontanea, mecanica, asistida, peep)
                VALUES (@IdRegistro, @VentEspontanea, @VentMecanica, @VentAsistida, @PEEP)", dto);

            // 5. Upsert Ingresos, Egresos y Duración
            await _db.ExecuteAsync("DELETE FROM qvt_IngresosAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_IngresosAnestesia (
                    idRegistro, dextrosa, s_salina_09, l_ringer, sangre, plasma, expansor, manitol, otros, total
                ) VALUES (
                    @IdRegistro, @IngDextrosa, @IngSalina09, @IngLactatoRinger, @IngSangre, @IngPlasma, @IngExpansor, @IngManitol, @IngOtros, @TotalIngresos
                )", dto);

            await _db.ExecuteAsync("DELETE FROM qvt_egresosAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_egresosAnestesia (
                    idRegistro, sangrado, diuresis, p_insensibles, otros, total, balance
                ) VALUES (
                    @IdRegistro, @EgrSangrado, @EgrDiuresis, @EgrPerdidasInsensibles, @EgrOtros, @TotalEgresos, @BalanceTotal
                )", dto);

            await _db.ExecuteAsync("DELETE FROM qvt_duracionAnestesia WHERE idRegistro = @IdReg", new { IdReg = idReg });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_duracionAnestesia (
                    idRegistro, duracionAnestesiaHoras, duracionAnestesiaMinutos, duracionCirugiaHoras, duracionCirugiaMin,
                    comentarios, fecha, horaInicio, horaFinal, tipoAnestesia
                ) VALUES (
                    @IdRegistro, @DuracionAnestesiaHoras, @DuracionAnestesiaMinutos, @DuracionCirugiaHoras, @DuracionCirugiaMin,
                    @Comentarios, @Fecha, @HoraInicio, @HoraFin, @TipoAnestesia
                )", dto);

            // 6. Guardar Nombres de los 15 Agentes (qvt_graficaDescripcion)
            await _db.ExecuteAsync(
                "DELETE FROM qvt_graficaDescripcion WHERE idOrden = @IdOrden AND ISNULL(numFormulario, 1) = @NumFormulario",
                new { dto.IdOrden, dto.NumFormulario });

            await _db.ExecuteAsync(@"
                INSERT INTO qvt_graficaDescripcion (
                    idOrden, numFormulario,
                    descripcion1, descripcion2, descripcion3, descripcion4, descripcion5,
                    descripcion6, descripcion7, descripcion8, descripcion9, descripcion10,
                    descripcion11, descripcion12, descripcion13, descripcion14, descripcion15
                ) VALUES (
                    @IdOrden, @NumFormulario,
                    @D1, @D2, @D3, @D4, @D5, @D6, @D7, @D8, @D9, @D10, @D11, @D12, @D13, @D14, @D15
                )", new
            {
                dto.IdOrden,
                dto.NumFormulario,
                D1 = dto.NombresAgentes[0], D2 = dto.NombresAgentes[1], D3 = dto.NombresAgentes[2],
                D4 = dto.NombresAgentes[3], D5 = dto.NombresAgentes[4], D6 = dto.NombresAgentes[5],
                D7 = dto.NombresAgentes[6], D8 = dto.NombresAgentes[7], D9 = dto.NombresAgentes[8],
                D10 = dto.NombresAgentes[9], D11 = dto.NombresAgentes[10], D12 = dto.NombresAgentes[11],
                D13 = dto.NombresAgentes[12], D14 = dto.NombresAgentes[13], D15 = dto.NombresAgentes[14]
            });

            // 7. Guardar Etiquetas de Tiempo (qvt_graficaAnestesiaTiempo)
            await _db.ExecuteAsync("DELETE FROM qvt_graficaAnestesiaTiempo WHERE idOrden = @IdOrden", new { dto.IdOrden });
            await _db.ExecuteAsync(@"
                INSERT INTO qvt_graficaAnestesiaTiempo (
                    idOrden, nhcl,
                    tiempo1, tiempo2, tiempo3, tiempo4, tiempo5, tiempo6, tiempo7, tiempo8,
                    tiempo9, tiempo10, tiempo11, tiempo12, tiempo13, tiempo14, tiempo15, tiempo16
                ) VALUES (
                    @IdOrden, @Nhcl,
                    @T1, @T2, @T3, @T4, @T5, @T6, @T7, @T8, @T9, @T10, @T11, @T12, @T13, @T14, @T15, @T16
                )", new
            {
                dto.IdOrden,
                dto.Nhcl,
                T1 = dto.SlotsTiempo[0].HoraLabel, T2 = dto.SlotsTiempo[1].HoraLabel,
                T3 = dto.SlotsTiempo[2].HoraLabel, T4 = dto.SlotsTiempo[3].HoraLabel,
                T5 = dto.SlotsTiempo[4].HoraLabel, T6 = dto.SlotsTiempo[5].HoraLabel,
                T7 = dto.SlotsTiempo[6].HoraLabel, T8 = dto.SlotsTiempo[7].HoraLabel,
                T9 = dto.SlotsTiempo[8].HoraLabel, T10 = dto.SlotsTiempo[9].HoraLabel,
                T11 = dto.SlotsTiempo[10].HoraLabel, T12 = dto.SlotsTiempo[11].HoraLabel,
                T13 = dto.SlotsTiempo[12].HoraLabel, T14 = dto.SlotsTiempo[13].HoraLabel,
                T15 = dto.SlotsTiempo[14].HoraLabel, T16 = dto.SlotsTiempo[15].HoraLabel
            });

            // 8. Guardar los 16 Puntos de la Gráfica de Anestesia (qvt_graficaAnestesia)
            await _db.ExecuteAsync(
                "DELETE FROM qvt_graficaAnestesia WHERE idRegistro = @IdReg AND ISNULL(numFormulario, 1) = @NumForm",
                new { IdReg = idReg, NumForm = dto.NumFormulario });

            const string sqlSlot = @"
                INSERT INTO qvt_graficaAnestesia (
                    idRegistro, numFormulario, orden,
                    presionSistolica, presionDiastolica, tesionArterialMedia,
                    frecuenciaCardiaca, temperatura, O2L, etco2,
                    induccion, finAnestesia, respEspontanea, respAsistida, respControlada,
                    valorDescripcion1, valorDescripcion2, valorDescripcion3, valorDescripcion4, valorDescripcion5,
                    valorDescripcion6, valorDescripcion7, valorDescripcion8, valorDescripcion9, valorDescripcion10,
                    valorDescripcion11, valorDescripcion12, valorDescripcion13, valorDescripcion14, valorDescripcion15
                ) VALUES (
                    @IdReg, @NumForm, @Orden,
                    @PAS, @PAD, @TAM, @FC, @Temp, @O2L, @EtCO2,
                    @Induccion, @FinAnestesia, @RespEsp, @RespAsist, @RespCtrl,
                    @V1, @V2, @V3, @V4, @V5, @V6, @V7, @V8, @V9, @V10, @V11, @V12, @V13, @V14, @V15
                )";

            foreach (var slot in dto.SlotsTiempo)
            {
                if (slot.PresionSistolica.HasValue && slot.PresionDiastolica.HasValue && string.IsNullOrWhiteSpace(slot.TensionArterialMedia))
                {
                    int tam = (slot.PresionSistolica.Value + (2 * slot.PresionDiastolica.Value)) / 3;
                    slot.TensionArterialMedia = tam.ToString();
                }

                await _db.ExecuteAsync(sqlSlot, new
                {
                    IdReg = idReg,
                    NumForm = dto.NumFormulario,
                    slot.Orden,
                    PAS = slot.PresionSistolica,
                    PAD = slot.PresionDiastolica,
                    TAM = slot.TensionArterialMedia,
                    FC = slot.FrecuenciaCardiaca,
                    Temp = slot.Temperatura,
                    slot.O2L,
                    slot.EtCO2,
                    slot.Induccion,
                    slot.FinAnestesia,
                    RespEsp = slot.RespEspontanea,
                    RespAsist = slot.RespAsistida,
                    RespCtrl = slot.RespControlada,
                    V1 = slot.ValoresAgentes[0], V2 = slot.ValoresAgentes[1], V3 = slot.ValoresAgentes[2],
                    V4 = slot.ValoresAgentes[3], V5 = slot.ValoresAgentes[4], V6 = slot.ValoresAgentes[5],
                    V7 = slot.ValoresAgentes[6], V8 = slot.ValoresAgentes[7], V9 = slot.ValoresAgentes[8],
                    V10 = slot.ValoresAgentes[9], V11 = slot.ValoresAgentes[10], V12 = slot.ValoresAgentes[11],
                    V13 = slot.ValoresAgentes[12], V14 = slot.ValoresAgentes[13], V15 = slot.ValoresAgentes[14]
                });
            }

            await _db.RegistrarBitacoraAsync(
                $"Hoja de Anestesia guardada Orden {dto.IdOrden} (Registro #{dto.IdRegistro})",
                idUsuario);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar Hoja de Anestesia para Orden {IdOrden}", dto.IdOrden);
            return false;
        }
    }

    // ========================================================================
    // 3. PROTOCOLO OPERATORIO, EQUIPO Y CHEQUEO (vt_protocolo + vt_quirTeam + vt_QuirofanoIngreso)
    // ========================================================================
    public async Task<ProtocoloQuirurgicoCompletoDto> ObtenerProtocoloQuirurgicoAsync(int idOrden, int nhcl)
    {
        var dto = new ProtocoloQuirurgicoCompletoDto { IdOrden = idOrden, Nhcl = nhcl };

        try
        {
            // 1. qvt_Protocolo
            var prot = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 * FROM qvt_Protocolo WHERE idOrden = @IdOrden ORDER BY id DESC",
                new { IdOrden = idOrden });

            if (prot != null)
            {
                dto.IdProtocolo = (int)prot.id;
                dto.Fecha = prot.fecha != null ? (DateTime)prot.fecha : DateTime.Today;
                dto.TipoCirugia = prot.tipoCirugia?.ToString() ?? "Electiva";
                dto.Principal = prot.principal?.ToString() ?? "";
                dto.Incision = prot.incision?.ToString() ?? "";
                dto.Hallazgos = prot.hallazgos?.ToString() ?? "";
                dto.ProcedimientoQuir = prot.procedimientoQuir?.ToString() ?? "";
                dto.Sintesis = prot.sintesis?.ToString() ?? "";
                dto.Complicaciones = prot.complicaciones?.ToString() ?? "Ninguna";
                dto.Sangrado = prot.sangrado != null ? (int)prot.sangrado : 0;
                dto.Disposicion = prot.disposicion != null ? (int)prot.disposicion : 1;
                dto.Drenaje = prot.drenaje != null ? (int)prot.drenaje : 0;
                dto.Muestra = prot.muestra?.ToString() ?? "NO";
                dto.FechaMuestra = prot.fechaMuestra != null ? (DateTime?)prot.fechaMuestra : null;
                dto.Congelacion = prot.congelacion?.ToString() ?? "NO";
                dto.FechaConge = prot.fechaConge != null ? (DateTime?)prot.fechaConge : null;
            }

            // 2. qvt_ingresocirugia (Chequeo de Enfermería / Conteo de Gasas)
            var ing = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 * FROM qvt_ingresocirugia WHERE id_orden = @IdOrden ORDER BY sec DESC",
                new { IdOrden = idOrden });

            if (ing != null)
            {
                dto.SecIngresoCirugia = (int)ing.sec;
                dto.Sala = ing.sala != null ? (int)ing.sala : 1;
                dto.Posicion = ing.posicion?.ToString() ?? "Decúbito Dorsal";
                dto.CondicionPiel = ing.condicionpiel?.ToString() ?? "Íntegra";
                dto.Lavado = ing.lavado?.ToString() ?? "Clorhexidina";
                dto.UnidadElectro = ing.unidadelectro?.ToString() ?? "Bipolar / Monopolar";
                dto.LocalizacionPlaca = ing.localizacionplaca?.ToString() ?? "";
                dto.GasasIniciales = ing.gasasl?.ToString() ?? "10";
                dto.GasasAdicionales = ing.gasas?.ToString() ?? "0";
                dto.GasasRecuentoFinal = ing.gasasr?.ToString() ?? "10 (Completo)";
                dto.Compresas = ing.compresas?.ToString() ?? "Completas";
                dto.MaterialDejado = ing.materialdejado?.ToString() ?? "Ninguno";
                dto.CasoContaminado = ing.casocontaminado?.ToString() ?? "Limpia";
                dto.TransferidoA = ing.transferidoa?.ToString() ?? "Sala de Recuperación";
                dto.ObservacionesEnfermeria = ing.observaciones1?.ToString() ?? "";
            }

            // 3. qvt_team (Equipo Quirúrgico)
            const string sqlTeam = @"
                SELECT
                    t.sec AS Sec,
                    ISNULL(t.idOrden, 0) AS IdOrden,
                    ISNULL(t.idUsuario, 0) AS IdUsuario,
                    ISNULL(u.user_nombre, 'Profesional') AS NombreProfesional,
                    ISNULL(t.observacion, ISNULL(t.tipo, 'Cirujano')) AS RolFuncion,
                    ISNULL(t.principal, 0) AS Principal,
                    ISNULL(t.fecha, GETDATE()) AS Fecha
                FROM qvt_team t
                LEFT JOIN vt_usuarios u ON t.idUsuario = u.user_numero
                WHERE t.idOrden = @IdOrden AND ISNULL(t.estado, 'A') = 'A'
                ORDER BY t.principal DESC, t.sec ASC";

            var team = await _db.QueryAsync<MiembroEquipoQuirurgicoDto>(sqlTeam, new { IdOrden = idOrden });
            dto.Equipo = team.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Aviso al cargar Protocolo Quirúrgico para Orden {IdOrden}", idOrden);
        }

        return dto;
    }

    public async Task<bool> GuardarProtocoloQuirurgicoAsync(ProtocoloQuirurgicoCompletoDto dto, int idUsuario, string nombreUsuario)
    {
        try
        {
            // 1. Upsert qvt_Protocolo
            var existeProt = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 id FROM qvt_Protocolo WHERE idOrden = @IdOrden", new { dto.IdOrden });

            if (existeProt.HasValue && existeProt.Value > 0)
            {
                dto.IdProtocolo = existeProt.Value;
                await _db.ExecuteAsync(@"
                    UPDATE qvt_Protocolo SET
                        fecha = @Fecha, tipoCirugia = @TipoCirugia, principal = @Principal,
                        incision = @Incision, hallazgos = @Hallazgos, procedimientoQuir = @ProcedimientoQuir,
                        sintesis = @Sintesis, complicaciones = @Complicaciones, sangrado = @Sangrado,
                        disposicion = @Disposicion, drenaje = @Drenaje, muestra = @Muestra,
                        fechaMuestra = @FechaMuestra, congelacion = @Congelacion, fechaConge = @FechaConge
                    WHERE id = @IdProtocolo", dto);
            }
            else
            {
                await _db.ExecuteAsync(@"
                    INSERT INTO qvt_Protocolo (
                        idOrden, nhcl, fecha, tipoCirugia, principal, incision, hallazgos,
                        procedimientoQuir, sintesis, complicaciones, sangrado, disposicion,
                        drenaje, muestra, fechaMuestra, congelacion, fechaConge
                    ) VALUES (
                        @IdOrden, @Nhcl, @Fecha, @TipoCirugia, @Principal, @Incision, @Hallazgos,
                        @ProcedimientoQuir, @Sintesis, @Complicaciones, @Sangrado, @Disposicion,
                        @Drenaje, @Muestra, @FechaMuestra, @Congelacion, @FechaConge
                    )", dto);
            }

            // 2. Upsert qvt_ingresocirugia
            var existeIng = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 sec FROM qvt_ingresocirugia WHERE id_orden = @IdOrden", new { dto.IdOrden });

            if (existeIng.HasValue && existeIng.Value > 0)
            {
                await _db.ExecuteAsync(@"
                    UPDATE qvt_ingresocirugia SET
                        sala = @Sala, posicion = @Posicion, condicionpiel = @CondicionPiel,
                        lavado = @Lavado, unidadelectro = @UnidadElectro, localizacionplaca = @LocalizacionPlaca,
                        gasasl = @GasasIniciales, gasas = @GasasAdicionales, gasasr = @GasasRecuentoFinal,
                        compresas = @Compresas, materialdejado = @MaterialDejado, casocontaminado = @CasoContaminado,
                        transferidoa = @TransferidoA, observaciones1 = @ObservacionesEnfermeria, iduser = @IdUser
                    WHERE sec = @Sec",
                    new
                    {
                        dto.Sala, dto.Posicion, dto.CondicionPiel, dto.Lavado, dto.UnidadElectro,
                        dto.LocalizacionPlaca, dto.GasasIniciales, dto.GasasAdicionales, dto.GasasRecuentoFinal,
                        dto.Compresas, dto.MaterialDejado, dto.CasoContaminado, dto.TransferidoA,
                        dto.ObservacionesEnfermeria, IdUser = idUsuario, Sec = existeIng.Value
                    });
            }
            else
            {
                await _db.ExecuteAsync(@"
                    INSERT INTO qvt_ingresocirugia (
                        nhcl, id_orden, fecha, sala, posicion, condicionpiel, lavado,
                        unidadelectro, localizacionplaca, gasasl, gasas, gasasr, compresas,
                        materialdejado, casocontaminado, transferidoa, observaciones1, iduser
                    ) VALUES (
                        @Nhcl, @IdOrden, GETDATE(), @Sala, @Posicion, @CondicionPiel, @Lavado,
                        @UnidadElectro, @LocalizacionPlaca, @GasasIniciales, @GasasAdicionales, @GasasRecuentoFinal, @Compresas,
                        @MaterialDejado, @CasoContaminado, @TransferidoA, @ObservacionesEnfermeria, @IdUser
                    )",
                    new
                    {
                        dto.Nhcl, dto.IdOrden, dto.Sala, dto.Posicion, dto.CondicionPiel, dto.Lavado,
                        dto.UnidadElectro, dto.LocalizacionPlaca, dto.GasasIniciales, dto.GasasAdicionales,
                        dto.GasasRecuentoFinal, dto.Compresas, dto.MaterialDejado, dto.CasoContaminado,
                        dto.TransferidoA, dto.ObservacionesEnfermeria, IdUser = idUsuario
                    });
            }

            await _db.RegistrarBitacoraAsync(
                $"Protocolo Quirúrgico guardado Orden {dto.IdOrden}",
                idUsuario);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar Protocolo Quirúrgico Orden {IdOrden}", dto.IdOrden);
            return false;
        }
    }

    public async Task<bool> AgregarMiembroEquipoAsync(MiembroEquipoQuirurgicoDto miembro)
    {
        try
        {
            const string sql = @"
                INSERT INTO qvt_team (idOrden, idUsuario, fecha, estado, principal, observacion, tipo)
                VALUES (@IdOrden, @IdUsuario, GETDATE(), 'A', @Principal, @RolFuncion, @RolFuncion)";
            await _db.ExecuteAsync(sql, miembro);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar miembro al equipo quirúrgico en Orden {IdOrden}", miembro.IdOrden);
            return false;
        }
    }

    public async Task<bool> EliminarMiembroEquipoAsync(int sec)
    {
        await _db.ExecuteAsync("UPDATE qvt_team SET estado = 'I' WHERE sec = @Sec", new { Sec = sec });
        return true;
    }

    // ========================================================================
    // 4. SALA DE RECUPERACIÓN Y POST-OPERATORIO (Reemplaza vt_Recuperacion.aspx.vb)
    // ========================================================================
    public async Task<List<RecuperacionSignoVitalDto>> ObtenerSignosRecuperacionAsync(int idOrden)
    {
        const string sql = @"
            SELECT
                r.id AS Id,
                ISNULL(r.idOrden, 0) AS IdOrden,
                ISNULL(r.nhclr, 0) AS Nhcl,
                ISNULL(r.horar, '') AS Hora,
                ISNULL(r.fechar, GETDATE()) AS Fecha,
                ISNULL(r.pulso, 0) AS Pulso,
                ISNULL(r.sistolica, 0) AS Sistolica,
                ISNULL(r.diastolica, 0) AS Diastolica,
                ISNULL(r.presionm, 0) AS PresionMedia,
                ISNULL(r.respiracion, 0) AS Respiracion,
                ISNULL(r.temperatura, 0) AS Temperatura,
                ISNULL(r.oxigeno, 0) AS Oxigeno,
                ISNULL(r.saturacion, 0) AS Saturacion,
                ISNULL(r.usuario, 0) AS Usuario,
                ISNULL(u.user_nombre, 'Enfermería') AS NombreUsuario
            FROM qvt_recuperacion r
            LEFT JOIN vt_usuarios u ON r.usuario = u.user_numero
            WHERE r.idOrden = @IdOrden AND ISNULL(r.estado, 'A') = 'A'
            ORDER BY r.id ASC";

        try
        {
            var lista = await _db.QueryAsync<RecuperacionSignoVitalDto>(sql, new { IdOrden = idOrden });
            return lista.ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> GuardarSignoRecuperacionAsync(RecuperacionSignoVitalDto signo, int idLocalidad)
    {
        try
        {
            if (signo.PresionMedia <= 0 && signo.Sistolica > 0 && signo.Diastolica > 0)
            {
                signo.PresionMedia = (signo.Sistolica + (2 * signo.Diastolica)) / 3;
            }

            if (signo.Id > 0)
            {
                const string sqlUpdate = @"
                    UPDATE qvt_recuperacion SET
                        pulso = @Pulso, sistolica = @Sistolica, diastolica = @Diastolica, presionm = @PresionMedia,
                        respiracion = @Respiracion, temperatura = @Temperatura, oxigeno = @Oxigeno,
                        saturacion = @Saturacion, usuario = @Usuario
                    WHERE id = @Id";
                await _db.ExecuteAsync(sqlUpdate, signo);
            }
            else
            {
                const string sqlInsert = @"
                    INSERT INTO qvt_recuperacion (
                        idOrden, nhclr, horar, fechar, pulso, sistolica, diastolica, presionm,
                        respiracion, temperatura, oxigeno, saturacion, usuario, estado, dia, mes, anio, localidadr
                    ) VALUES (
                        @IdOrden, @Nhcl, @Hora, @Fecha, @Pulso, @Sistolica, @Diastolica, @PresionMedia,
                        @Respiracion, @Temperatura, @Oxigeno, @Saturacion, @Usuario, 'A',
                        DAY(@Fecha), MONTH(@Fecha), YEAR(@Fecha), @Localidad
                    )";
                await _db.ExecuteAsync(sqlInsert, new
                {
                    signo.IdOrden,
                    signo.Nhcl,
                    signo.Hora,
                    signo.Fecha,
                    signo.Pulso,
                    signo.Sistolica,
                    signo.Diastolica,
                    signo.PresionMedia,
                    signo.Respiracion,
                    signo.Temperatura,
                    signo.Oxigeno,
                    signo.Saturacion,
                    signo.Usuario,
                    Localidad = idLocalidad
                });
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar signo vital en recuperación Orden {IdOrden}", signo.IdOrden);
            return false;
        }
    }

    public async Task<bool> EliminarSignoRecuperacionAsync(int id)
    {
        await _db.ExecuteAsync("UPDATE qvt_recuperacion SET estado = 'I' WHERE id = @Id", new { Id = id });
        return true;
    }

    public async Task<PostOperatorioChecklistDto> ObtenerPostOperatorioChecklistAsync(int idOrden, int nhcl)
    {
        const string sql = @"
            SELECT TOP 1
                id AS Id,
                ISNULL(idOrden, @IdOrden) AS IdOrden,
                ISNULL(nhcl, @Nhcl) AS Nhcl,
                fechahorasal AS FechaHoraSalida,
                ISNULL(destino, 'Hospitalización') AS Destino,
                ISNULL(ind, 'SI') AS Ind, ISNULL(obsind, '') AS ObsInd,
                ISNULL(sig, 'SI') AS Sig, ISNULL(obssig, '') AS ObsSig,
                ISNULL(apo, 'SI') AS Apo, ISNULL(obsapo, '') AS ObsApo,
                ISNULL(liq, 'SI') AS Liq, ISNULL(obsliq, '') AS ObsLiq,
                ISNULL(son, 'NO') AS Son, ISNULL(obsson, '') AS ObsSon,
                ISNULL(drenes, 'NO') AS Drenes, ISNULL(obsdrenes, '') AS ObsDrenes,
                ISNULL(drena, 'NO') AS Drena, ISNULL(obsdrena, '') AS ObsDrena,
                ISNULL(sng, 'NO') AS Sng, ISNULL(obssng, '') AS ObsSng,
                ISNULL(otros, 'NO') AS Otros, ISNULL(obsotros, '') AS ObsOtros
            FROM qvt_post_operatorio
            WHERE idOrden = @IdOrden
            ORDER BY id DESC";

        try
        {
            var dto = await _db.QueryFirstOrDefaultAsync<PostOperatorioChecklistDto>(sql, new { IdOrden = idOrden, Nhcl = nhcl });
            return dto ?? new PostOperatorioChecklistDto { IdOrden = idOrden, Nhcl = nhcl };
        }
        catch
        {
            return new PostOperatorioChecklistDto { IdOrden = idOrden, Nhcl = nhcl };
        }
    }

    public async Task<bool> GuardarPostOperatorioChecklistAsync(PostOperatorioChecklistDto dto, int idUsuario, int idLocalidad)
    {
        try
        {
            var existeId = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 id FROM qvt_post_operatorio WHERE idOrden = @IdOrden", new { dto.IdOrden });

            if (existeId.HasValue && existeId.Value > 0)
            {
                await _db.ExecuteAsync(@"
                    UPDATE qvt_post_operatorio SET
                        fechahorasal = @FechaHoraSalida, destino = @Destino,
                        ind = @Ind, obsind = @ObsInd, sig = @Sig, obssig = @ObsSig,
                        apo = @Apo, obsapo = @ObsApo, liq = @Liq, obsliq = @ObsLiq,
                        son = @Son, obsson = @ObsSon, drenes = @Drenes, obsdrenes = @ObsDrenes,
                        drena = @Drena, obsdrena = @ObsDrena, sng = @Sng, obssng = @ObsSng,
                        otros = @Otros, obsotros = @ObsOtros, usuario = @Usuario, localidad = @Localidad
                    WHERE id = @Id",
                    new
                    {
                        dto.FechaHoraSalida, dto.Destino,
                        dto.Ind, dto.ObsInd, dto.Sig, dto.ObsSig,
                        dto.Apo, dto.ObsApo, dto.Liq, dto.ObsLiq,
                        dto.Son, dto.ObsSon, dto.Drenes, dto.ObsDrenes,
                        dto.Drena, dto.ObsDrena, dto.Sng, dto.ObsSng,
                        dto.Otros, dto.ObsOtros,
                        Usuario = idUsuario, Localidad = idLocalidad, Id = existeId.Value
                    });
            }
            else
            {
                await _db.ExecuteAsync(@"
                    INSERT INTO qvt_post_operatorio (
                        idOrden, nhcl, hora, fecha, fechahorasal, destino,
                        ind, obsind, sig, obssig, apo, obsapo, liq, obsliq,
                        son, obsson, drenes, obsdrenes, drena, obsdrena, sng, obssng,
                        otros, obsotros, usuario, localidad
                    ) VALUES (
                        @IdOrden, @Nhcl, CONVERT(VARCHAR(5), GETDATE(), 108), GETDATE(), @FechaHoraSalida, @Destino,
                        @Ind, @ObsInd, @Sig, @ObsSig, @Apo, @ObsApo, @Liq, @ObsLiq,
                        @Son, @ObsSon, @Drenes, @ObsDrenes, @Drena, @ObsDrena, @Sng, @ObsSng,
                        @Otros, @ObsOtros, @Usuario, @Localidad
                    )",
                    new
                    {
                        dto.IdOrden, dto.Nhcl, dto.FechaHoraSalida, dto.Destino,
                        dto.Ind, dto.ObsInd, dto.Sig, dto.ObsSig,
                        dto.Apo, dto.ObsApo, dto.Liq, dto.ObsLiq,
                        dto.Son, dto.ObsSon, dto.Drenes, dto.ObsDrenes,
                        dto.Drena, dto.ObsDrena, dto.Sng, dto.ObsSng,
                        dto.Otros, dto.ObsOtros,
                        Usuario = idUsuario, Localidad = idLocalidad
                    });
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar checklist post-operatorio Orden {IdOrden}", dto.IdOrden);
            return false;
        }
    }

    public async Task<List<LiquidoRecuperacionDto>> ObtenerLiquidosRecuperacionAsync(int idOrden, string tipo)
    {
        const string sql = @"
            SELECT
                id AS Id,
                ISNULL(idOrden, 0) AS IdOrden,
                ISNULL(nhcl, 0) AS Nhcl,
                ISNULL(hora, '') AS Hora,
                ISNULL(fecha, GETDATE()) AS Fecha,
                ISNULL(descripcion, '') AS Descripcion,
                ISNULL(observaciones, '') AS Observaciones,
                ISNULL(tipo, 'I') AS Tipo,
                ISNULL(med_idProdu, '') AS MedIdProdu,
                ISNULL(med_cantidad, 1) AS MedCantidad,
                ISNULL(med_bodega, '') AS MedBodega
            FROM qvt_ingliqadm
            WHERE idOrden = @IdOrden AND tipo = @Tipo AND ISNULL(estado, 'A') = 'A'
            ORDER BY id DESC";

        try
        {
            var lista = await _db.QueryAsync<LiquidoRecuperacionDto>(sql, new { IdOrden = idOrden, Tipo = tipo });
            return lista.ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> AgregarLiquidoRecuperacionAsync(LiquidoRecuperacionDto dto, int idUsuario, int idLocalidad)
    {
        try
        {
            const string sqlInsert = @"
                INSERT INTO qvt_ingliqadm (
                    idOrden, nhcl, hora, fecha, descripcion, observaciones, estado, usuario, localidad, tipo,
                    med_idProdu, med_origen, med_stock, med_precio, med_iva, med_iddinamics, med_medida, med_bodega, med_cantidad, etapa
                ) VALUES (
                    @IdOrden, @Nhcl, @Hora, @Fecha, @Descripcion, @Observaciones, 'A', @Usuario, @Localidad, @Tipo,
                    @MedIdProdu, 'F', 0, @MedPrecio, @MedIva, @MedIdDinamics, @MedMedida, @MedBodega, @MedCantidad, 'Recuperacion Quirofanos'
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int idCreado = await _db.ExecuteScalarAsync<int>(sqlInsert, new
            {
                dto.IdOrden,
                dto.Nhcl,
                dto.Hora,
                dto.Fecha,
                dto.Descripcion,
                dto.Observaciones,
                Usuario = idUsuario,
                Localidad = idLocalidad,
                dto.Tipo,
                dto.MedIdProdu,
                dto.MedPrecio,
                dto.MedIva,
                dto.MedIdDinamics,
                dto.MedMedida,
                dto.MedBodega,
                dto.MedCantidad
            });

            if (dto.Tipo == "I" && idCreado > 0 && !string.IsNullOrWhiteSpace(dto.MedIdProdu))
            {
                await _inventarioService.GuardarReservaStockAsync(
                    dto.MedIdProdu,
                    idUsuario,
                    dto.MedCantidad,
                    idLocalidad,
                    dto.MedBodega,
                    dto.IdOrden,
                    dto.Nhcl,
                    idCreado,
                    "LIQ");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar líquido de recuperación en Orden {IdOrden}", dto.IdOrden);
            return false;
        }
    }

    public async Task<bool> EliminarLiquidoRecuperacionAsync(int id, string codigoProd, int idOrden, int nhcl)
    {
        await _db.ExecuteAsync("UPDATE qvt_ingliqadm SET estado = 'I' WHERE id = @Id", new { Id = id });
        if (!string.IsNullOrWhiteSpace(codigoProd))
        {
            await _inventarioService.EliminarReservaStockAsync(codigoProd, idOrden, id, "LIQ");
        }
        return true;
    }

    public async Task<List<ObservacionRecuperacionDto>> ObtenerObservacionesRecuperacionAsync(int idOrden)
    {
        const string sql = @"
            SELECT
                o.id AS Id,
                o.idOrden AS IdOrden,
                ISNULL(o.nhcl, 0) AS Nhcl,
                ISNULL(o.hora, '') AS Hora,
                ISNULL(o.fecha, GETDATE()) AS Fecha,
                ISNULL(o.observacion, '') AS Observacion,
                ISNULL(o.usuario, 0) AS Usuario,
                ISNULL(u.user_nombre, 'Enfermería') AS NombreUsuario
            FROM qvt_observaciones_recuperacion o
            LEFT JOIN vt_usuarios u ON o.usuario = u.user_numero
            WHERE o.idOrden = @IdOrden AND ISNULL(o.estado, 'A') = 'A'
            ORDER BY o.id DESC";

        try
        {
            var lista = await _db.QueryAsync<ObservacionRecuperacionDto>(sql, new { IdOrden = idOrden });
            return lista.ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> AgregarObservacionRecuperacionAsync(int idOrden, int nhcl, string observacion, int idUsuario, int idLocalidad)
    {
        if (string.IsNullOrWhiteSpace(observacion)) return false;

        const string sql = @"
            INSERT INTO qvt_observaciones_recuperacion (idOrden, nhcl, hora, fecha, observacion, estado, usuario, localidad)
            VALUES (@IdOrden, @Nhcl, CONVERT(VARCHAR(5), GETDATE(), 108), GETDATE(), @Observacion, 'A', @Usuario, @Localidad)";

        await _db.ExecuteAsync(sql, new
        {
            IdOrden = idOrden,
            Nhcl = nhcl,
            Observacion = observacion.Trim(),
            Usuario = idUsuario,
            Localidad = idLocalidad
        });
        return true;
    }

    public async Task<bool> EliminarObservacionRecuperacionAsync(int id)
    {
        await _db.ExecuteAsync("UPDATE qvt_observaciones_recuperacion SET estado = 'I' WHERE id = @Id", new { Id = id });
        return true;
    }
}
