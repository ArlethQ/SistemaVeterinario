using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;

namespace HosvetKardex.Web.Services;

public interface IHospitalizacionService
{
    // Catálogos Hospitalización
    Task<List<CondicionHospitalDto>> ObtenerCondicionesAsync();
    Task<List<RitmoHorarioDto>> ObtenerRitmosHorariosAsync();

    // 1. Enfermería Signos Vitales y Balance Hídrico (vt_EnfermeriaSignos.aspx)
    Task<FormularioSignosEnfermeriaDto> CargarSignosEnfermeriaAsync(int idOrdenAtencion, int nhcl, int numFormulario = 1);
    Task GuardarSignosEnfermeriaAsync(FormularioSignosEnfermeriaDto dto, int idUsuario);
    Task<int> CrearNuevoFormularioSignosAsync(int idOrdenAtencion, int nhcl, int idUsuario);

    // 2. Ficha de Hospitalización 24h (Hosp_FichaHospitalizacion.aspx)
    Task<FichaHospitalizacionSheetDto> CargarFichaHospitalizacionAsync(int idOrden, int nhcl, int hoja = 1, int idUsuario = 0);
    Task GuardarFichaHospitalizacionAsync(FichaHospitalizacionSheetDto sheet, int idUsuario);
    Task<int> CrearNuevaHojaFichaAsync(int idOrden, int nhcl, int idUsuario);
    Task CerrarFichaHospitalizacionAsync(int idOrden);

    // 3. Monitoreo Hospitalario 24h (Hosp_Monitoreo.aspx)
    Task<MonitoreoHospitalarioSheetDto> CargarMonitoreoHospitalarioAsync(int idOrden, int nhcl, int hoja = 1, int idUsuario = 0);
    Task GuardarMonitoreoHospitalarioAsync(MonitoreoHospitalarioSheetDto sheet, int idUsuario);
    Task<int> CrearNuevaHojaMonitoreoAsync(int idOrden, int nhcl, int idUsuario);
    Task CerrarMonitoreoHospitalarioAsync(int idOrden);
    Task<(bool Exito, string Mensaje)> RegistrarMedicamentoMonitoreoAsync(MonitoreoMedInputModel input, int idUsuario, int idLocalidad);
    Task EliminarMedicamentoMonitoreoAsync(int idRegistro, string codigoProd, int idOrden);

    // Notas importantes y Observaciones por hora compartidas
    Task AgregarNotaImportanteAsync(int idOrden, int nhcl, string observacion, string tipo, int idUsuario, int idLocalidad);
    Task EliminarNotaImportanteAsync(int idNota);
    Task AgregarObservacionHoraAsync(int idOrden, int nhcl, string hora, string observacion, string responsable, int idLocalidad);
    Task EliminarObservacionHoraAsync(int idObs);
}

public class HospitalizacionService : IHospitalizacionService
{
    private readonly ISqlDataService _db;
    private readonly IInventarioService _inventarioService;

    private static List<CondicionHospitalDto>? _cacheCondiciones;
    private static List<RitmoHorarioDto>? _cacheRitmos;

    public HospitalizacionService(ISqlDataService db, IInventarioService inventarioService)
    {
        _db = db;
        _inventarioService = inventarioService;
    }

    public async Task<List<CondicionHospitalDto>> ObtenerCondicionesAsync()
    {
        if (_cacheCondiciones is not null && _cacheCondiciones.Count > 0)
            return _cacheCondiciones;

        try
        {
            const string sql = "SELECT id AS Id, condicion AS Condicion FROM hvt_condicion WHERE estado = 'A' ORDER BY condicion";
            var res = (await _db.QueryAsync<CondicionHospitalDto>(sql)).ToList();
            if (res.Count > 0)
            {
                _cacheCondiciones = res;
                return res;
            }
        }
        catch { }

        return
        [
            new() { Id = 1, Condicion = "ESTABLE" },
            new() { Id = 2, Condicion = "RESERVADO" },
            new() { Id = 3, Condicion = "CRÍTICO / UCI" }
        ];
    }

    public async Task<List<RitmoHorarioDto>> ObtenerRitmosHorariosAsync()
    {
        if (_cacheRitmos is not null && _cacheRitmos.Count > 0)
            return _cacheRitmos;

        try
        {
            const string sql = "SELECT id AS Id, des_ritmo AS Des_Ritmo FROM hvt_ritmo WHERE estado = 'A' ORDER BY des_ritmo";
            var res = (await _db.QueryAsync<RitmoHorarioDto>(sql)).ToList();
            if (res.Count > 0)
            {
                _cacheRitmos = res;
                return res;
            }
        }
        catch { }

        return
        [
            new() { Id = 1, Des_Ritmo = "Q6H (Cada 6h)" },
            new() { Id = 2, Des_Ritmo = "Q8H (Cada 8h)" },
            new() { Id = 3, Des_Ritmo = "Q12H (Cada 12h)" },
            new() { Id = 4, Des_Ritmo = "Q24H (Cada 24h)" }
        ];
    }

    // ========================================================================
    // 1. ENFERMERÍA SIGNOS VITALES Y BALANCE HÍDRICO
    // ========================================================================

    public async Task<FormularioSignosEnfermeriaDto> CargarSignosEnfermeriaAsync(int idOrdenAtencion, int nhcl, int numFormulario = 1)
    {
        if (idOrdenAtencion <= 0) idOrdenAtencion = 1;
        if (nhcl <= 0) nhcl = 1;
        if (numFormulario <= 0) numFormulario = 1;

        var forms = new List<int>();
        try
        {
            const string sqlForms = @"
                SELECT DISTINCT numFormulario 
                FROM qvt_enfSignosVitales 
                WHERE idOrdenAtencion = @IdOrden AND tipo = 'SIGNOS' AND numFormulario IS NOT NULL
                ORDER BY numFormulario";

            forms = (await _db.QueryAsync<int>(sqlForms, new { IdOrden = idOrdenAtencion })).ToList();
        }
        catch { }

        if (forms.Count == 0)
            forms.Add(1);

        if (!forms.Contains(numFormulario))
            numFormulario = forms.First();

        var dto = new FormularioSignosEnfermeriaDto
        {
            IdOrdenAtencion = idOrdenAtencion,
            Nhcl = nhcl,
            NumFormulario = numFormulario,
            FormulariosDisponibles = forms
        };

        var param = new { IdOrden = idOrdenAtencion, NumForm = numFormulario };

        var fechasDb = new Dictionary<int, EnfFechaDiaDto>();
        var signosDb = new Dictionary<int, SignoVitalSlotDto>();
        var balanceDb = new Dictionary<int, EnfBalanceHidricoDiaDto>();
        var medicionesDb = new Dictionary<int, EnfMedicionDiaDto>();

        try
        {
            var tFechas = _db.QueryAsync<EnfFechaDiaDto>(@"
                SELECT id AS Id, ISNULL(orden, 1) AS Orden, fecha AS Fecha,
                       TRY_CAST(diaInternacion AS INT) AS DiasInternacion,
                       TRY_CAST(diaPostquirurgico AS INT) AS DiasPostQuirurgico
                FROM qvt_enfFechas
                WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'
                ORDER BY orden", param);

            var tSignos = _db.QueryAsync<SignoVitalSlotDto>(@"
                SELECT id AS Id, ISNULL(orden, 1) AS Orden,
                       TRY_CAST(pulso AS INT) AS Pulso,
                       TRY_CAST(temp AS decimal(10,2)) AS Temperatura,
                       TRY_CAST(resp AS INT) AS FrecuenciaRespiratoria,
                       TRY_CAST(sistolica AS INT) AS PresionSistolica,
                       TRY_CAST(diastolica AS INT) AS PresionDiastolica
                FROM qvt_enfSignosVitales
                WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'
                ORDER BY orden", param);

            var tBalance = _db.QueryAsync<EnfBalanceHidricoDiaDto>(@"
                SELECT id AS Id, ISNULL(orden, 1) AS Orden,
                       TRY_CAST(parental AS decimal(10,2)) AS Parenteral,
                       TRY_CAST(oral AS decimal(10,2)) AS ViaOral,
                       TRY_CAST(orina AS decimal(10,2)) AS Orina,
                       TRY_CAST(drenaje AS decimal(10,2)) AS Drenaje,
                       TRY_CAST(otros AS decimal(10,2)) AS Otros
                FROM qvt_enfBalanceHidrico
                WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'
                ORDER BY orden", param);

            var tMediciones = _db.QueryAsync<EnfMedicionDiaDto>(@"
                SELECT id AS Id, ISNULL(orden, 1) AS Orden,
                       TRY_CAST(peso AS decimal(10,2)) AS Peso,
                       dieta AS DietaIndicada,
                       numComidas AS NumeroComidas,
                       aseo AS Aseo,
                       aseo AS Banio,
                       actividadFisica AS Movilizacion,
                       recanalizacion AS Vendajes,
                       cambioSonda AS CambioSonda,
                       responsable AS Responsable
                FROM qvt_enfMediciones
                WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'
                ORDER BY orden", param);

            await Task.WhenAll(tFechas, tSignos, tBalance, tMediciones);

            fechasDb = (await tFechas).GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
            signosDb = (await tSignos).GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
            balanceDb = (await tBalance).GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
            medicionesDb = (await tMediciones).GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
        }
        catch { }

        for (int dia = 1; dia <= 9; dia++)
        {
            dto.Fechas.Add(fechasDb.TryGetValue(dia, out var f)
                ? f
                : new EnfFechaDiaDto { Orden = dia, Fecha = dia == 1 ? DateTime.Today : null, DiasInternacion = dia });
        }

        for (int slot = 1; slot <= 18; slot++)
        {
            dto.SignosVitales.Add(signosDb.TryGetValue(slot, out var s)
                ? s
                : new SignoVitalSlotDto { Orden = slot });
        }

        for (int dia = 1; dia <= 9; dia++)
        {
            dto.BalanceHidrico.Add(balanceDb.TryGetValue(dia, out var b)
                ? b
                : new EnfBalanceHidricoDiaDto { Orden = dia });
        }

        for (int idx = 1; idx <= 18; idx++)
        {
            dto.Mediciones.Add(medicionesDb.TryGetValue(idx, out var m)
                ? m
                : new EnfMedicionDiaDto { Orden = idx });
        }

        return dto;
    }

    private static string? Inv(decimal? valor, string formato) =>
        valor?.ToString(formato, System.Globalization.CultureInfo.InvariantCulture);

    public async Task GuardarSignosEnfermeriaAsync(FormularioSignosEnfermeriaDto dto, int idUsuario)
    {
        var paramForm = new { IdOrden = dto.IdOrdenAtencion, NumForm = dto.NumFormulario };
        var comandos = new List<(string Sql, object? Parameters)>
        {
            ("DELETE FROM qvt_enfFechas WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'", paramForm),
            ("DELETE FROM qvt_enfSignosVitales WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'", paramForm),
            ("DELETE FROM qvt_enfBalanceHidrico WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'", paramForm),
            ("DELETE FROM qvt_enfMediciones WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'", paramForm)
        };

        foreach (var f in dto.Fechas)
        {
            comandos.Add((@"
                INSERT INTO qvt_enfFechas (idOrdenAtencion, fecha, diaInternacion, diaPostquirurgico, orden, numFormulario, tipo)
                VALUES (@IdOrden, @Fecha, @DiasInternacion, @DiasPostQuirurgico, @Orden, @NumForm, 'SIGNOS')",
                new
                {
                    IdOrden = dto.IdOrdenAtencion,
                    f.Fecha,
                    DiasInternacion = f.DiasInternacion?.ToString(),
                    DiasPostQuirurgico = f.DiasPostQuirurgico?.ToString(),
                    f.Orden,
                    NumForm = dto.NumFormulario
                }));
        }

        foreach (var s in dto.SignosVitales)
        {
            comandos.Add((@"
                INSERT INTO qvt_enfSignosVitales (idOrdenAtencion, pulso, temp, resp, sistolica, diastolica, orden, numFormulario, tipo)
                VALUES (@IdOrden, @Pulso, @Temperatura, @FrecuenciaRespiratoria, @PresionSistolica, @PresionDiastolica, @Orden, @NumForm, 'SIGNOS')",
                new
                {
                    IdOrden = dto.IdOrdenAtencion,
                    Pulso = s.Pulso?.ToString(),
                    Temperatura = Inv(s.Temperatura, "0.0"),
                    FrecuenciaRespiratoria = s.FrecuenciaRespiratoria?.ToString(),
                    PresionSistolica = s.PresionSistolica?.ToString(),
                    PresionDiastolica = s.PresionDiastolica?.ToString(),
                    s.Orden,
                    NumForm = dto.NumFormulario
                }));
        }

        foreach (var b in dto.BalanceHidrico)
        {
            comandos.Add((@"
                INSERT INTO qvt_enfBalanceHidrico (idOrdenAtencion, parental, oral, totalIng, orina, drenaje, otros, totalElm, orden, numFormulario, tipo)
                VALUES (@IdOrden, @Parenteral, @ViaOral, @TotalIngresos, @Orina, @Drenaje, @Otros, @TotalEliminaciones, @Orden, @NumForm, 'SIGNOS')",
                new
                {
                    IdOrden = dto.IdOrdenAtencion,
                    Parenteral = Inv(b.Parenteral, "0.0"),
                    ViaOral = Inv(b.ViaOral, "0.0"),
                    TotalIngresos = Inv(b.TotalIngresos, "0.0"),
                    Orina = Inv(b.Orina, "0.0"),
                    Drenaje = Inv(b.Drenaje, "0.0"),
                    Otros = Inv(b.Otros, "0.0"),
                    TotalEliminaciones = Inv(b.TotalEliminaciones, "0.0"),
                    b.Orden,
                    NumForm = dto.NumFormulario
                }));
        }

        foreach (var m in dto.Mediciones)
        {
            comandos.Add((@"
                INSERT INTO qvt_enfMediciones (idOrdenAtencion, peso, dieta, numComidas, aseo, actividadFisica, recanalizacion, cambioSonda, responsable, orden, numFormulario, tipo)
                VALUES (@IdOrden, @Peso, @DietaIndicada, @NumeroComidas, @Aseo, @Movilizacion, @Vendajes, @CambioSonda, @Responsable, @Orden, @NumForm, 'SIGNOS')",
                new
                {
                    IdOrden = dto.IdOrdenAtencion,
                    Peso = Inv(m.Peso, "0.00"),
                    m.DietaIndicada,
                    m.NumeroComidas,
                    // La pantalla edita 'Baño / Aseo' en Banio; se persiste en la columna aseo.
                    Aseo = string.IsNullOrWhiteSpace(m.Banio) ? m.Aseo : m.Banio,
                    m.Movilizacion,
                    m.Vendajes,
                    m.CambioSonda,
                    m.Responsable,
                    m.Orden,
                    NumForm = dto.NumFormulario
                }));
        }

        // Todo el formulario se guarda en una sola transacción; si algo falla se revierte y se informa el error.
        await _db.ExecuteBatchAsync(comandos);

        try
        {
            await _db.RegistrarBitacoraAsync(
                $"Guardó Signos Vitales y Balance Hídrico Orden={dto.IdOrdenAtencion} Form={dto.NumFormulario}",
                idUsuario, "vt_EnfermeriaSignos");
        }
        catch
        {
            // La bitácora es complementaria: un fallo allí no debe invalidar el guardado clínico.
        }
    }

    public async Task<int> CrearNuevoFormularioSignosAsync(int idOrdenAtencion, int nhcl, int idUsuario)
    {
        int maxForm = 1;
        try
        {
            maxForm = await _db.ExecuteScalarAsync<int?>(@"
                SELECT MAX(numFormulario) FROM qvt_enfSignosVitales 
                WHERE idOrdenAtencion = @IdOrden AND tipo = 'SIGNOS'",
                new { IdOrden = idOrdenAtencion }) ?? 0;
        }
        catch { }

        int nuevoNum = maxForm + 1;
        var nuevoDto = new FormularioSignosEnfermeriaDto
        {
            IdOrdenAtencion = idOrdenAtencion,
            Nhcl = nhcl,
            NumFormulario = nuevoNum
        };

        for (int i = 1; i <= 9; i++)
        {
            nuevoDto.Fechas.Add(new EnfFechaDiaDto { Orden = i, Fecha = i == 1 ? DateTime.Today : null, DiasInternacion = i });
            nuevoDto.BalanceHidrico.Add(new EnfBalanceHidricoDiaDto { Orden = i });
        }
        for (int i = 1; i <= 18; i++)
        {
            nuevoDto.SignosVitales.Add(new SignoVitalSlotDto { Orden = i });
            nuevoDto.Mediciones.Add(new EnfMedicionDiaDto { Orden = i });
        }

        await GuardarSignosEnfermeriaAsync(nuevoDto, idUsuario);
        return nuevoNum;
    }

    // ========================================================================
    // 2. FICHA DE HOSPITALIZACIÓN 24 HORAS (Hosp_FichaHospitalizacion.aspx)
    // ========================================================================

    public async Task<FichaHospitalizacionSheetDto> CargarFichaHospitalizacionAsync(int idOrden, int nhcl, int hoja = 1, int idUsuario = 0)
    {
        if (idOrden <= 0) idOrden = 1;
        if (nhcl <= 0) nhcl = 1;
        if (hoja <= 0) hoja = 1;

        var hojas = new List<int> { 1 };
        string medico = string.Empty;
        string cierre = "N";
        var b1 = new List<FichaBloque1MedicacionDto>();
        var b2 = new List<FichaBloque2AlimentacionDto>();
        var b3 = new List<FichaBloque3FluidoterapiaDto>();
        var notas = new List<NotaImportanteDto>();

        try
        {
            var countExist = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM hvt_bloque1ficha WHERE idorden = @IdOrden AND nhcl = @Nhcl",
                new { IdOrden = idOrden, Nhcl = nhcl });

            if (countExist == 0)
            {
                await InicializarHojaFichaAsync(idOrden, nhcl, 1, idUsuario);
            }

            hojas = (await _db.QueryAsync<int>(@"
                SELECT DISTINCT ISNULL(hoja, 1) FROM hvt_bloque1ficha 
                WHERE idorden = @IdOrden AND nhcl = @Nhcl ORDER BY 1",
                new { IdOrden = idOrden, Nhcl = nhcl })).ToList();

            if (hojas.Count == 0) hojas.Add(1);
            if (!hojas.Contains(hoja)) hoja = hojas.First();

            var param = new { IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja };

            var tCab = _db.QueryFirstOrDefaultAsync<(string Medico, string Cierre)>(@"
                SELECT ISNULL(u.user_nombre, '') AS Medico, ISNULL(o.cierrehospi, 'N') AS Cierre
                FROM vt_ordenAtencion o
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                WHERE o.ordenAten_id = @IdOrden", new { IdOrden = idOrden });

            var tB1 = _db.QueryAsync<FichaBloque1MedicacionDto>(@"
                SELECT id AS Id, ISNULL(fila, 1) AS Fila, idorden AS IdOrden, nhcl AS Nhcl, ISNULL(hoja, 1) AS Hoja,
                       fecha AS Fecha, responsable AS Responsable, medicacion AS Medicacion,
                       TRY_CAST(mgkg AS decimal(18,2)) AS MgKg, TRY_CAST(ml AS decimal(18,2)) AS Ml,
                       via AS Via, ritmo AS Ritmo, condicion AS Condicion,
                       TRY_CAST(peso AS decimal(18,2)) AS Peso, dia AS Dia,
                       txt7 AS Txt7, txt8 AS Txt8, txt9 AS Txt9, txt10 AS Txt10, txt11 AS Txt11, txt12 AS Txt12,
                       txt13 AS Txt13, txt14 AS Txt14, txt15 AS Txt15, txt16 AS Txt16, txt17 AS Txt17, txt18 AS Txt18,
                       txt19 AS Txt19, txt20 AS Txt20, txt21 AS Txt21, txt22 AS Txt22, txt23 AS Txt23, txt24 AS Txt24,
                       txt1 AS Txt1, txt2 AS Txt2, txt3 AS Txt3, txt4 AS Txt4, txt5 AS Txt5, txt6 AS Txt6
                FROM hvt_bloque1ficha
                WHERE idorden = @IdOrden AND nhcl = @Nhcl AND hoja = @Hoja
                ORDER BY fila", param);

            var tB2 = _db.QueryAsync<FichaBloque2AlimentacionDto>(@"
                SELECT id AS Id, ISNULL(fila, 1) AS Fila, idorden AS IdOrden, nhcl AS Nhcl, ISNULL(hoja, 1) AS Hoja,
                       fecha AS Fecha, responsable AS Responsable, alimentacion AS Alimentacion,
                       TRY_CAST(cald AS decimal(18,2)) AS Cald, TRY_CAST(calv AS decimal(18,2)) AS Calv,
                       gr AS Gr, ritmo AS Ritmo, condicion AS Condicion,
                       TRY_CAST(peso AS decimal(18,2)) AS Peso, dia AS Dia,
                       txt7 AS Txt7, txt8 AS Txt8, txt9 AS Txt9, txt10 AS Txt10, txt11 AS Txt11, txt12 AS Txt12,
                       txt13 AS Txt13, txt14 AS Txt14, txt15 AS Txt15, txt16 AS Txt16, txt17 AS Txt17, txt18 AS Txt18,
                       txt19 AS Txt19, txt20 AS Txt20, txt21 AS Txt21, txt22 AS Txt22, txt23 AS Txt23, txt24 AS Txt24,
                       txt1 AS Txt1, txt2 AS Txt2, txt3 AS Txt3, txt4 AS Txt4, txt5 AS Txt5, txt6 AS Txt6
                FROM hvt_bloque2ficha
                WHERE idorden = @IdOrden AND nhcl = @Nhcl AND hoja = @Hoja
                ORDER BY fila", param);

            var tB3 = _db.QueryAsync<FichaBloque3FluidoterapiaDto>(@"
                SELECT id AS Id, ISNULL(fila, 1) AS Fila, idorden AS IdOrden, nhcl AS Nhcl, ISNULL(hoja, 1) AS Hoja,
                       fecha AS Fecha, responsable AS Responsable, fluidoterapia AS Fluidoterapia,
                       mpd AS Mpd, mldia AS MlDia, mlh AS MlH, gotas AS Gotas,
                       condicion AS Condicion, TRY_CAST(peso AS decimal(18,2)) AS Peso, dia AS Dia,
                       txt7 AS Txt7, txt8 AS Txt8, txt9 AS Txt9, txt10 AS Txt10, txt11 AS Txt11, txt12 AS Txt12,
                       txt13 AS Txt13, txt14 AS Txt14, txt15 AS Txt15, txt16 AS Txt16, txt17 AS Txt17, txt18 AS Txt18,
                       txt19 AS Txt19, txt20 AS Txt20, txt21 AS Txt21, txt22 AS Txt22, txt23 AS Txt23, txt24 AS Txt24,
                       txt1 AS Txt1, txt2 AS Txt2, txt3 AS Txt3, txt4 AS Txt4, txt5 AS Txt5, txt6 AS Txt6
                FROM hvt_bloque3ficha
                WHERE idorden = @IdOrden AND nhcl = @Nhcl AND hoja = @Hoja
                ORDER BY fila", param);

            var tNotas = _db.QueryAsync<NotaImportanteDto>(@"
                SELECT id AS Id, idOrden AS IdOrden, ISNULL(nhcl, 0) AS Nhcl, ISNULL(hora, '') AS Hora,
                       ISNULL(fecha, GETDATE()) AS Fecha, ISNULL(observacion, '') AS Observacion, ISNULL(tipo, 'I') AS Tipo
                FROM hvt_importante
                WHERE estado = 'A' AND idOrden = @IdOrden AND ISNULL(nhcl, 0) = @Nhcl AND tipo IN ('I', 'O')
                ORDER BY id DESC", new { IdOrden = idOrden, Nhcl = nhcl });

            await Task.WhenAll(tCab, tB1, tB2, tB3, tNotas);

            var cabOrden = await tCab;
            medico = cabOrden.Medico ?? string.Empty;
            cierre = cabOrden.Cierre ?? "N";
            b1 = (await tB1).ToList();
            b2 = (await tB2).ToList();
            b3 = (await tB3).ToList();
            notas = (await tNotas).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("No se pudo cargar la ficha de hospitalización desde SQL Server.", ex);
        }

        if (b1.Count == 0)
            b1 = Enumerable.Range(1, 12).Select(i => new FichaBloque1MedicacionDto { Fila = i, IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja }).ToList();
        if (b2.Count == 0)
            b2 = Enumerable.Range(1, 5).Select(i => new FichaBloque2AlimentacionDto { Fila = i, IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja }).ToList();
        if (b3.Count == 0)
            b3 = Enumerable.Range(1, 7).Select(i => new FichaBloque3FluidoterapiaDto { Fila = i, IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja }).ToList();

        var primera = b1.FirstOrDefault();

        return new FichaHospitalizacionSheetDto
        {
            IdOrden = idOrden,
            Nhcl = nhcl,
            Hoja = hoja,
            Fecha = primera?.Fecha ?? DateTime.Today,
            CondicionId = primera?.Condicion,
            Peso = primera?.Peso,
            DiasHospitalizacion = primera?.Dia ?? hoja,
            MedicoResponsable = medico,
            Cerrada = string.Equals(cierre.Trim(), "S", StringComparison.OrdinalIgnoreCase),
            HojasDisponibles = hojas,
            Bloque1Medicacion = b1,
            Bloque2Alimentacion = b2,
            Bloque3Fluidoterapia = b3,
            NotasImportantes = notas.Where(n => n.Tipo == "I").ToList(),
            ObservacionesGenerales = notas.Where(n => n.Tipo == "O").ToList()
        };
    }

    private async Task InicializarHojaFichaAsync(int idOrden, int nhcl, int hoja, int idUsuario)
    {
        const string sql = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            INSERT INTO hvt_bloque1ficha (fila, idorden, nhcl, hoja, fecha, dia, responsable)
            SELECT v.fila, @IdOrden, @Nhcl, @Hoja, CAST(GETDATE() AS date), @Hoja, @Resp
            FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) v(fila)
            WHERE NOT EXISTS (
                SELECT 1 FROM hvt_bloque1ficha x
                WHERE x.idorden=@IdOrden AND x.nhcl=@Nhcl AND ISNULL(x.hoja,1)=@Hoja AND x.fila=v.fila);

            INSERT INTO hvt_bloque2ficha (fila, idorden, nhcl, hoja, fecha, dia, responsable)
            SELECT v.fila, @IdOrden, @Nhcl, @Hoja, CAST(GETDATE() AS date), @Hoja, @Resp
            FROM (VALUES (1),(2),(3),(4),(5)) v(fila)
            WHERE NOT EXISTS (
                SELECT 1 FROM hvt_bloque2ficha x
                WHERE x.idorden=@IdOrden AND x.nhcl=@Nhcl AND ISNULL(x.hoja,1)=@Hoja AND x.fila=v.fila);

            INSERT INTO hvt_bloque3ficha (fila, idorden, nhcl, hoja, fecha, dia, responsable)
            SELECT v.fila, @IdOrden, @Nhcl, @Hoja, CAST(GETDATE() AS date), @Hoja, @Resp
            FROM (VALUES (1),(2),(3),(4),(5),(6),(7)) v(fila)
            WHERE NOT EXISTS (
                SELECT 1 FROM hvt_bloque3ficha x
                WHERE x.idorden=@IdOrden AND x.nhcl=@Nhcl AND ISNULL(x.hoja,1)=@Hoja AND x.fila=v.fila);

            COMMIT TRANSACTION;";

        await _db.ExecuteAsync(sql, new { IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja, Resp = idUsuario });
    }

    public async Task<int> CrearNuevaHojaFichaAsync(int idOrden, int nhcl, int idUsuario)
    {
        var maxHoja = await _db.ExecuteScalarAsync<int?>(
            "SELECT MAX(hoja) FROM hvt_bloque1ficha WHERE idorden = @IdOrden AND nhcl = @Nhcl",
            new { IdOrden = idOrden, Nhcl = nhcl }) ?? 0;

        int nuevaHoja = maxHoja + 1;
        await InicializarHojaFichaAsync(idOrden, nhcl, nuevaHoja, idUsuario);
        return nuevaHoja;
    }

    public async Task GuardarFichaHospitalizacionAsync(FichaHospitalizacionSheetDto sheet, int idUsuario)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (sheet.IdOrden <= 0 || sheet.Nhcl <= 0 || sheet.Hoja <= 0)
            throw new ArgumentException("La orden, NHCL y hoja son obligatorios para guardar la ficha.");
        if (sheet.DiasHospitalizacion <= 0)
            throw new ArgumentException("El día de hospitalización debe ser mayor que cero.");
        if (sheet.Peso is < 0)
            throw new ArgumentException("El peso no puede ser negativo.");

        ValidarFilas(sheet.Bloque1Medicacion, 12, "medicación");
        ValidarFilas(sheet.Bloque2Alimentacion, 5, "alimentación");
        ValidarFilas(sheet.Bloque3Fluidoterapia, 7, "fluidoterapia");

        var sql = new StringBuilder("SET XACT_ABORT ON; BEGIN TRANSACTION;");
        var parametros = new DynamicParameters(new
        {
            sheet.IdOrden,
            sheet.Nhcl,
            sheet.Hoja,
            sheet.Fecha,
            sheet.CondicionId,
            sheet.Peso,
            sheet.DiasHospitalizacion,
            Resp = idUsuario
        });

        AgregarActualizacionBloque(sql, parametros, "hvt_bloque1ficha", "B1", sheet.Bloque1Medicacion,
        [
            ("medicacion", "Medicacion"), ("mgkg", "MgKg"), ("ml", "Ml"),
            ("via", "Via"), ("ritmo", "Ritmo")
        ]);
        AgregarActualizacionBloque(sql, parametros, "hvt_bloque2ficha", "B2", sheet.Bloque2Alimentacion,
        [
            ("alimentacion", "Alimentacion"), ("cald", "Cald"), ("calv", "Calv"),
            ("gr", "Gr"), ("ritmo", "Ritmo")
        ]);
        AgregarActualizacionBloque(sql, parametros, "hvt_bloque3ficha", "B3", sheet.Bloque3Fluidoterapia,
        [
            ("fluidoterapia", "Fluidoterapia"), ("mpd", "Mpd"), ("mldia", "MlDia"),
            ("mlh", "MlH"), ("gotas", "Gotas")
        ]);
        sql.Append("COMMIT TRANSACTION;");

        await using var connection = new SqlConnection(_db.GetConnectionString());
        await connection.OpenAsync();
        var filasActualizadas = await connection.ExecuteAsync(sql.ToString(), parametros, commandTimeout: 15);
        if (filasActualizadas != 24)
            throw new InvalidOperationException($"Se esperaban 24 filas y SQL actualizó {filasActualizadas}. No se confirmó el guardado.");

        await _db.RegistrarBitacoraAsync(
            $"Guardó Ficha Hospitalización Orden={sheet.IdOrden} Hoja={sheet.Hoja}",
            idUsuario, "Hosp_FichaHospitalizacion");
    }

    private static void ValidarFilas<T>(IReadOnlyCollection<T> filas, int cantidadEsperada, string bloque)
        where T : Matriz24HorasBaseDto
    {
        if (filas.Count != cantidadEsperada || filas.Select(x => x.Fila).Distinct().Count() != cantidadEsperada)
            throw new InvalidOperationException($"El bloque de {bloque} no contiene sus {cantidadEsperada} filas válidas.");
    }

    private static void AgregarActualizacionBloque<T>(
        StringBuilder sql,
        DynamicParameters parametros,
        string tabla,
        string prefijo,
        IReadOnlyList<T> filas,
        IReadOnlyList<(string Columna, string Propiedad)> columnasEspecificas)
        where T : Matriz24HorasBaseDto
    {
        var columnas = columnasEspecificas.Concat(
            Matriz24HorasBaseDto.HorasOrden.Select(h => ($"txt{h}", $"Txt{h}")));

        sql.Append(" UPDATE ").Append(tabla).Append(" SET ");
        bool primeraColumna = true;
        foreach (var (columna, propiedad) in columnas)
        {
            if (!primeraColumna) sql.Append(',');
            primeraColumna = false;
            sql.Append(columna).Append(" = CASE fila ");

            var propertyInfo = typeof(T).GetProperty(propiedad)
                ?? typeof(Matriz24HorasBaseDto).GetProperty(propiedad)
                ?? throw new InvalidOperationException($"No existe la propiedad {propiedad}.");

            foreach (var fila in filas)
            {
                string nombreParametro = $"{prefijo}_{fila.Fila}_{propiedad}";
                sql.Append("WHEN ").Append(fila.Fila).Append(" THEN @").Append(nombreParametro).Append(' ');
                parametros.Add(nombreParametro, propertyInfo.GetValue(fila));
            }
            sql.Append("ELSE ").Append(columna).Append(" END ");
        }

        sql.Append(@", fecha=@Fecha, condicion=@CondicionId, peso=@Peso, dia=@DiasHospitalizacion, responsable=@Resp
            WHERE idorden=@IdOrden AND nhcl=@Nhcl AND ISNULL(hoja,1)=@Hoja;")
            .Append(" IF @@ROWCOUNT <> ").Append(filas.Count)
            .Append(" THROW 50001, 'La ficha tiene filas incompletas en SQL Server.', 1;");
    }

    public async Task CerrarFichaHospitalizacionAsync(int idOrden)
    {
        await _db.ExecuteAsync(
            "UPDATE vt_ordenAtencion SET cierrehospi = 'S' WHERE ordenAten_id = @IdOrden",
            new { IdOrden = idOrden });
    }

    // ========================================================================
    // 3. MONITOREO HOSPITALARIO 24 HORAS (Hosp_Monitoreo.aspx)
    // ========================================================================

    public async Task<MonitoreoHospitalarioSheetDto> CargarMonitoreoHospitalarioAsync(int idOrden, int nhcl, int hoja = 1, int idUsuario = 0)
    {
        if (idOrden <= 0) idOrden = 1;
        if (nhcl <= 0) nhcl = 1;
        if (hoja <= 0) hoja = 1;

        var hojas = new List<int> { 1 };
        string? cabMedico = string.Empty;
        string? cierre = "N";
        var filas = new List<MonitoreoFilaDto>();
        var meds = new List<MonitoreoMedicamentoDto>();
        var notasChequeo = new List<NotaImportanteDto>();
        var obsHoras = new List<ObservacionHoraDto>();

        try
        {
            var countExist = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM hvt_bloqueMonitoreo WHERE idorden = @IdOrden AND nhcl = @Nhcl",
                new { IdOrden = idOrden, Nhcl = nhcl });

            if (countExist == 0)
            {
                await InicializarHojaMonitoreoAsync(idOrden, nhcl, 1, idUsuario);
            }

            hojas = (await _db.QueryAsync<int>(@"
                SELECT DISTINCT ISNULL(hoja, 1) FROM hvt_bloqueMonitoreo 
                WHERE idorden = @IdOrden AND nhcl = @Nhcl ORDER BY 1",
                new { IdOrden = idOrden, Nhcl = nhcl })).ToList();

            if (hojas.Count == 0) hojas.Add(1);
            if (!hojas.Contains(hoja)) hoja = hojas.First();

            var tMedico = _db.ExecuteScalarAsync<string?>(@"
                SELECT ISNULL(u.user_nombre, '')
                FROM vt_ordenAtencion o
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                WHERE o.ordenAten_id = @IdOrden", new { IdOrden = idOrden });

            var tCierre = _db.ExecuteScalarAsync<string?>(
                "SELECT TOP 1 cierre FROM hvt_cabMonitoreo WHERE idOrden = @IdOrden",
                new { IdOrden = idOrden });

            var tFilas = _db.QueryAsync<MonitoreoFilaDto>(@"
                SELECT id AS Id, ISNULL(fila, 1) AS Fila, idorden AS IdOrden, nhcl AS Nhcl, ISNULL(hoja, 1) AS Hoja,
                       fecha AS Fecha, responsable AS Responsable, item AS Item,
                       txt7 AS Txt7, txt8 AS Txt8, txt9 AS Txt9, txt10 AS Txt10, txt11 AS Txt11, txt12 AS Txt12,
                       txt13 AS Txt13, txt14 AS Txt14, txt15 AS Txt15, txt16 AS Txt16, txt17 AS Txt17, txt18 AS Txt18,
                       txt19 AS Txt19, txt20 AS Txt20, txt21 AS Txt21, txt22 AS Txt22, txt23 AS Txt23, txt24 AS Txt24,
                       txt1 AS Txt1, txt2 AS Txt2, txt3 AS Txt3, txt4 AS Txt4, txt5 AS Txt5, txt6 AS Txt6
                FROM hvt_bloqueMonitoreo
                WHERE idorden = @IdOrden AND nhcl = @Nhcl AND hoja = @Hoja
                ORDER BY fila", new { IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja });

            var tMeds = _db.QueryAsync<MonitoreoMedicamentoDto>(@"
                SELECT id AS Id, ISNULL(ordenId, 0) AS OrdenId, ISNULL(nhcl, 0) AS Nhcl,
                       ISNULL(proddescripcion, '') AS Producto, ISNULL(cantprod, 0) AS Cantidad,
                       ISNULL(indicacionesprod, '') AS Indicaciones, ISNULL(via, '') AS Via,
                       CONVERT(varchar(10), fecha, 120) AS Fecha, ISNULL(hora, '') AS Hora,
                       CAST(ISNULL(codigoprod, 0) AS varchar(25)) AS Codigo,
                       iniciales AS Iniciales, funcion AS Funcion
                FROM hvt_cabMonitoreoMedBC
                WHERE ordenId = @IdOrden AND modulo = 'Monitoreo'
                ORDER BY fecha, proddescripcion", new { IdOrden = idOrden });

            var tNotas = _db.QueryAsync<NotaImportanteDto>(@"
                SELECT id AS Id, idOrden AS IdOrden, ISNULL(nhcl, 0) AS Nhcl, ISNULL(hora, '') AS Hora,
                       ISNULL(fecha, GETDATE()) AS Fecha, ISNULL(observacion, '') AS Observacion, 'N' AS Tipo
                FROM hvt_importante
                WHERE estado = 'A' AND tipo = 'N' AND idOrden = @IdOrden
                ORDER BY id DESC", new { IdOrden = idOrden });

            var tObs = _db.QueryAsync<ObservacionHoraDto>(@"
                SELECT id AS Id, ISNULL(idOrden, 0) AS IdOrden, ISNULL(nhcl, 0) AS Nhcl,
                       ISNULL(hora, '') AS Hora, ISNULL(observacion, '') AS Observacion, responsable AS Responsable
                FROM hvt_observacioneshoras
                WHERE estado = 'A' AND idOrden = @IdOrden
                ORDER BY id DESC", new { IdOrden = idOrden });

            await Task.WhenAll(tMedico, tCierre, tFilas, tMeds, tNotas, tObs);

            cabMedico = await tMedico;
            cierre = await tCierre;
            filas = (await tFilas).ToList();
            meds = (await tMeds).ToList();
            notasChequeo = (await tNotas).ToList();
            obsHoras = (await tObs).ToList();
        }
        catch { }

        if (filas.Count == 0)
            filas = Enumerable.Range(1, 24).Select(i => new MonitoreoFilaDto { Fila = i, IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja }).ToList();

        var catalogoMap = MonitoreoHospitalarioSheetDto.CatalogoParametros.ToDictionary(c => c.Fila);
        foreach (var f in filas)
        {
            if (catalogoMap.TryGetValue(f.Fila, out var meta))
            {
                f.ParametroLabel = meta.Parametro;
                f.UnidadLabel = meta.Unidad;
            }
        }

        return new MonitoreoHospitalarioSheetDto
        {
            IdOrden = idOrden,
            Nhcl = nhcl,
            Hoja = hoja,
            Fecha = filas.FirstOrDefault()?.Fecha ?? DateTime.Today,
            MedicoResponsable = cabMedico ?? string.Empty,
            Cerrada = string.Equals(cierre?.Trim(), "S", StringComparison.OrdinalIgnoreCase),
            HojasDisponibles = hojas,
            FilasMonitoreo = filas,
            MedicamentosRegistrados = meds,
            NotasChequeoQuirurgico = notasChequeo,
            ObservacionesPorHora = obsHoras
        };
    }

    private async Task InicializarHojaMonitoreoAsync(int idOrden, int nhcl, int hoja, int idUsuario)
    {
        for (int i = 1; i <= 24; i++)
        {
            await _db.ExecuteAsync(@"
                INSERT INTO hvt_bloqueMonitoreo (fila, idorden, nhcl, hoja, fecha, responsable)
                VALUES (@Fila, @IdOrden, @Nhcl, @Hoja, CAST(GETDATE() AS date), @Resp)",
                new { Fila = i, IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja, Resp = idUsuario });
        }
    }

    public async Task<int> CrearNuevaHojaMonitoreoAsync(int idOrden, int nhcl, int idUsuario)
    {
        var maxHoja = await _db.ExecuteScalarAsync<int?>(
            "SELECT MAX(hoja) FROM hvt_bloqueMonitoreo WHERE idorden = @IdOrden AND nhcl = @Nhcl",
            new { IdOrden = idOrden, Nhcl = nhcl }) ?? 0;

        int nuevaHoja = maxHoja + 1;
        await InicializarHojaMonitoreoAsync(idOrden, nhcl, nuevaHoja, idUsuario);
        return nuevaHoja;
    }

    public async Task GuardarMonitoreoHospitalarioAsync(MonitoreoHospitalarioSheetDto sheet, int idUsuario)
    {
        foreach (var r in sheet.FilasMonitoreo)
        {
            await _db.ExecuteAsync(@"
                UPDATE hvt_bloqueMonitoreo SET
                    item = @Item, fecha = @Fecha, responsable = @Resp,
                    txt7 = @Txt7, txt8 = @Txt8, txt9 = @Txt9, txt10 = @Txt10, txt11 = @Txt11, txt12 = @Txt12,
                    txt13 = @Txt13, txt14 = @Txt14, txt15 = @Txt15, txt16 = @Txt16, txt17 = @Txt17, txt18 = @Txt18,
                    txt19 = @Txt19, txt20 = @Txt20, txt21 = @Txt21, txt22 = @Txt22, txt23 = @Txt23, txt24 = @Txt24,
                    txt1 = @Txt1, txt2 = @Txt2, txt3 = @Txt3, txt4 = @Txt4, txt5 = @Txt5, txt6 = @Txt6
                WHERE id = @Id",
                new
                {
                    r.Id, r.Item, sheet.Fecha, Resp = idUsuario,
                    r.Txt7, r.Txt8, r.Txt9, r.Txt10, r.Txt11, r.Txt12,
                    r.Txt13, r.Txt14, r.Txt15, r.Txt16, r.Txt17, r.Txt18,
                    r.Txt19, r.Txt20, r.Txt21, r.Txt22, r.Txt23, r.Txt24,
                    r.Txt1, r.Txt2, r.Txt3, r.Txt4, r.Txt5, r.Txt6
                });
        }

        await _db.RegistrarBitacoraAsync(
            $"Guardó Monitoreo Hospitalario Orden={sheet.IdOrden} Hoja={sheet.Hoja}",
            idUsuario, "Hosp_Monitoreo");
    }

    public async Task CerrarMonitoreoHospitalarioAsync(int idOrden)
    {
        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM hvt_cabMonitoreo WHERE idOrden = @IdOrden",
            new { IdOrden = idOrden });

        if (count > 0)
        {
            await _db.ExecuteAsync(
                "UPDATE hvt_cabMonitoreo SET cierre = 'S' WHERE idOrden = @IdOrden",
                new { IdOrden = idOrden });
        }
        else
        {
            await _db.ExecuteAsync(
                "INSERT INTO hvt_cabMonitoreo (idOrden, fecha, estado, cierre) VALUES (@IdOrden, CAST(GETDATE() AS date), 'A', 'S')",
                new { IdOrden = idOrden });
        }
    }

    public async Task<(bool Exito, string Mensaje)> RegistrarMedicamentoMonitoreoAsync(
        MonitoreoMedInputModel input, int idUsuario, int idLocalidad)
    {
        if (input.Cantidad > input.StockDisponible && input.StockDisponible > 0)
            return (false, $"La cantidad ({input.Cantidad}) supera el stock disponible ({input.StockDisponible}).");

        int.TryParse(input.CodigoProducto, out int codigoNum);

        int idInserted = await _db.ExecuteScalarAsync<int>(@"
            INSERT INTO hvt_cabMonitoreoMedBC (
                ordenId, nhcl, hora, fecha, codigoprod, precioprod, stockprod,
                indicacionesprod, cantprod, usuario, localidad, proddescripcion,
                via, origen, iniciales, funcion, funcionario, estado, modulo,
                med_iva, med_iddinamics, med_medida, med_bodega, med_idProdu
            )
            OUTPUT INSERTED.id
            VALUES (
                @OrdenId, @Nhcl, @Hora, @Fecha, @CodigoNum, @Precio, @Stock,
                @Indicaciones, @Cantidad, @Usuario, @Localidad, @Producto,
                @Via, 'K', @Iniciales, @Funcion, @Funcionario, 'A', 'Monitoreo',
                @Iva, @IdBC, @Medida, @Bodega, @CodigoStr
            )",
            new
            {
                input.OrdenId,
                input.Nhcl,
                Hora = input.HoraAdministracion.ToString(@"hh\:mm"),
                Fecha = input.FechaAdministracion.Date,
                CodigoNum = codigoNum,
                Precio = input.PrecioUnitario,
                Stock = (int)input.StockDisponible,
                Indicaciones = input.Indicaciones.Trim().ToUpperInvariant(),
                input.Cantidad,
                Usuario = idUsuario,
                Localidad = idLocalidad,
                Producto = input.NombreProducto.Trim().ToUpperInvariant(),
                input.Via,
                input.Iniciales,
                input.Funcion,
                input.Funcionario,
                input.Iva,
                input.IdBC,
                input.Medida,
                input.Bodega,
                CodigoStr = input.CodigoProducto
            });

        await _inventarioService.GuardarReservaStockAsync(
            input.CodigoProducto, idUsuario, input.Cantidad, idLocalidad, input.Bodega,
            input.OrdenId, input.Nhcl, idInserted, "MON", null, 1);

        return (true, "Medicamento de monitoreo registrado correctamente.");
    }

    public async Task EliminarMedicamentoMonitoreoAsync(int idRegistro, string codigoProd, int idOrden)
    {
        await _db.ExecuteAsync("DELETE FROM hvt_cabMonitoreoMedBC WHERE id = @Id", new { Id = idRegistro });
        await _inventarioService.EliminarReservaStockAsync(codigoProd, idOrden, idRegistro, "MON");
    }

    // ========================================================================
    // NOTAS IMPORTANTES Y OBSERVACIONES POR HORA
    // ========================================================================

    public async Task AgregarNotaImportanteAsync(int idOrden, int nhcl, string observacion, string tipo, int idUsuario, int idLocalidad)
    {
        if (string.IsNullOrWhiteSpace(observacion)) return;

        await _db.ExecuteAsync(@"
            INSERT INTO hvt_importante (idOrden, nhcl, hora, fecha, observacion, estado, usuario, localidad, tipo)
            VALUES (@IdOrden, @Nhcl, @Hora, CAST(GETDATE() AS date), @Obs, 'A', @Usuario, @Localidad, @Tipo)",
            new
            {
                IdOrden = idOrden,
                Nhcl = nhcl,
                Hora = DateTime.Now.ToString("HH:mm"),
                Obs = observacion.Trim().ToUpperInvariant(),
                Usuario = idUsuario,
                Localidad = idLocalidad,
                Tipo = tipo
            });
    }

    public async Task EliminarNotaImportanteAsync(int idNota)
    {
        await _db.ExecuteAsync("UPDATE hvt_importante SET estado = 'I' WHERE id = @Id", new { Id = idNota });
    }

    public async Task AgregarObservacionHoraAsync(int idOrden, int nhcl, string hora, string observacion, string responsable, int idLocalidad)
    {
        if (string.IsNullOrWhiteSpace(observacion)) return;

        await _db.ExecuteAsync(@"
            INSERT INTO hvt_observacioneshoras (idOrden, nhcl, hora, observacion, responsable, estado, localidad)
            VALUES (@IdOrden, @Nhcl, @Hora, @Obs, @Resp, 'A', @Loc)",
            new
            {
                IdOrden = idOrden,
                Nhcl = nhcl,
                Hora = hora,
                Obs = observacion.Trim().ToUpperInvariant(),
                Resp = responsable,
                Loc = idLocalidad
            });
    }

    public async Task EliminarObservacionHoraAsync(int idObs)
    {
        await _db.ExecuteAsync("UPDATE hvt_observacioneshoras SET estado = 'I' WHERE id = @Id", new { Id = idObs });
    }
}
