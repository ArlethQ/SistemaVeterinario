using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Globalization;

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
    Task<bool> ActualizarMedicamentoMonitoreoAsync(int idRegistro, int idOrden, int nhcl, int cantidad, string indicaciones, string via);
    Task<bool> EliminarMedicamentoMonitoreoAsync(int idRegistro, int idOrden, int nhcl);

    // Notas importantes y Observaciones por hora compartidas
    Task AgregarNotaImportanteAsync(int idOrden, int nhcl, string observacion, string tipo, int idUsuario, int idLocalidad);
    Task EliminarNotaImportanteAsync(int idNota);
    Task AgregarObservacionHoraAsync(int idOrden, int nhcl, string hora, string observacion, int responsable, int idLocalidad);
    Task EliminarObservacionHoraAsync(int idObs);

    // 4. Bandeja e Ingreso a Hospitalización
    Task<List<OrdenHospitalizacionColaDto>> ObtenerColaHospitalizacionAsync(string? filtro = null, int? idLocalidad = null);
    Task<IngresoHospitalizacionDto> ObtenerIngresoHospitalizacionAsync(int idOrden, int nhcl);
    Task GuardarIngresoHospitalizacionAsync(IngresoHospitalizacionDto dto, int idUsuario);
    Task<(bool TieneDatosCirugia, bool TieneDatosHospitalizacion)> VerificarDatosModulosOrdenAsync(int idOrden, int nhcl);
    Task<bool> ActualizarIndicacionesModalAsync(int idOrden, int nhcl, bool requiereCirugia, bool requiereHospitalizacion, int idUsuario);
    Task<bool> OrdenRequiereCirugiaAsync(int idOrden);
}

public class HospitalizacionService : IHospitalizacionService
{
    private readonly ISqlDataService _db;
    private readonly IInventarioService _inventarioService;
    private readonly ILogger<HospitalizacionService>? _logger;

    private static List<CondicionHospitalDto>? _cacheCondiciones;
    private static List<RitmoHorarioDto>? _cacheRitmos;

    private static readonly ConcurrentDictionary<string, FormularioSignosEnfermeriaDto> _cacheSignosMemoria = new();
    private static readonly ConcurrentDictionary<int, SortedSet<int>> _hojasPorOrden = new();

    public HospitalizacionService(ISqlDataService db, IInventarioService inventarioService, ILogger<HospitalizacionService>? logger = null)
    {
        _db = db;
        _inventarioService = inventarioService;
        _logger = logger;
    }

    private static FormularioSignosEnfermeriaDto ClonarSignos(FormularioSignosEnfermeriaDto src)
    {
        return new FormularioSignosEnfermeriaDto
        {
            IdOrdenAtencion = src.IdOrdenAtencion,
            Nhcl = src.Nhcl,
            NumFormulario = src.NumFormulario,
            FormulariosDisponibles = new List<int>(src.FormulariosDisponibles ?? [1]),
            Fechas = src.Fechas?.Select(f => new EnfFechaDiaDto
            {
                Id = f.Id,
                Orden = f.Orden,
                Fecha = f.Fecha,
                DiasInternacion = f.DiasInternacion,
                DiasPostQuirurgico = f.DiasPostQuirurgico
            }).ToList() ?? [],
            SignosVitales = src.SignosVitales?.Select(s => new SignoVitalSlotDto
            {
                Id = s.Id,
                Orden = s.Orden,
                Pulso = s.Pulso,
                Temperatura = s.Temperatura,
                FrecuenciaRespiratoria = s.FrecuenciaRespiratoria,
                PresionSistolica = s.PresionSistolica,
                PresionDiastolica = s.PresionDiastolica
            }).ToList() ?? [],
            BalanceHidrico = src.BalanceHidrico?.Select(b => new EnfBalanceHidricoDiaDto
            {
                Id = b.Id,
                Orden = b.Orden,
                Parenteral = b.Parenteral,
                ViaOral = b.ViaOral,
                Orina = b.Orina,
                Drenaje = b.Drenaje,
                Otros = b.Otros
            }).ToList() ?? [],
            Mediciones = src.Mediciones?.Select(m => new EnfMedicionDiaDto
            {
                Id = m.Id,
                Orden = m.Orden,
                Peso = m.Peso,
                Talla = m.Talla,
                PerimetroAbdominal = m.PerimetroAbdominal,
                DietaIndicada = m.DietaIndicada,
                NumeroComidas = m.NumeroComidas,
                Aseo = m.Aseo ?? m.Banio,
                Banio = m.Banio ?? m.Aseo,
                Movilizacion = m.Movilizacion,
                Vendajes = m.Vendajes,
                CambioSonda = m.CambioSonda,
                Responsable = m.Responsable
            }).ToList() ?? []
        };
    }

    public async Task<List<CondicionHospitalDto>> ObtenerCondicionesAsync()
    {
        if (_cacheCondiciones is not null && _cacheCondiciones.Count > 0)
            return _cacheCondiciones;

        var res = (await _db.QueryAsync<CondicionHospitalDto>(
            "SELECT id AS Id, RTRIM(condicion) AS Condicion FROM hvt_condicion WHERE ISNULL(estado, 'A')='A' ORDER BY id")).ToList();
        _cacheCondiciones = res;
        return res;
    }

    public async Task<List<RitmoHorarioDto>> ObtenerRitmosHorariosAsync()
    {
        if (_cacheRitmos is not null && _cacheRitmos.Count > 0)
            return _cacheRitmos;

        var res = (await _db.QueryAsync<RitmoHorarioDto>(
            "SELECT id AS Id, RTRIM(des_ritmo) AS Des_Ritmo FROM hvt_ritmo WHERE ISNULL(estado, 'A')='A' ORDER BY id")).ToList();
        _cacheRitmos = res;
        return res;
    }

    private async Task ValidarOrdenPacienteAsync(int idOrden, int nhcl)
    {
        if (idOrden <= 0 || nhcl <= 0)
            throw new ArgumentException("La orden y el NHCL deben corresponder a un paciente registrado.");

        int existe = await _db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1)
            FROM vt_ordenAtencion
            WHERE ordenAten_id=@IdOrden AND ordenAten_nhcl=@Nhcl",
            new { IdOrden = idOrden, Nhcl = nhcl });

        if (existe == 0)
            throw new InvalidOperationException("La orden y el NHCL indicados no existen o no corresponden al mismo paciente.");
    }

    // ========================================================================
    // 1. ENFERMERÍA SIGNOS VITALES Y BALANCE HÍDRICO
    // ========================================================================

    public async Task<FormularioSignosEnfermeriaDto> CargarSignosEnfermeriaAsync(int idOrdenAtencion, int nhcl, int numFormulario = 1)
    {
        await ValidarOrdenPacienteAsync(idOrdenAtencion, nhcl);
        if (numFormulario <= 0) numFormulario = 1;

        string cacheKey = $"{idOrdenAtencion}_{numFormulario}";
        if (_cacheSignosMemoria.TryGetValue(cacheKey, out var cache))
        {
            var dtoCache = ClonarSignos(cache);
            if (_hojasPorOrden.TryGetValue(idOrdenAtencion, out var hojasCache))
            {
                lock (hojasCache)
                    dtoCache.FormulariosDisponibles = hojasCache.ToList();
            }
            return dtoCache;
        }

        var forms = new List<int>();
        const string sqlForms = @"
                SELECT DISTINCT numFormulario 
                FROM qvt_enfSignosVitales 
                WHERE idOrdenAtencion = @IdOrden AND tipo = 'SIGNOS' AND numFormulario IS NOT NULL
                ORDER BY numFormulario";

        forms = (await _db.QueryAsync<int>(sqlForms, new { IdOrden = idOrdenAtencion })).ToList();

        // Incluir las hojas registradas en el almacén en memoria
        if (_hojasPorOrden.TryGetValue(idOrdenAtencion, out var setHojas))
        {
            lock (setHojas)
            {
                foreach (var h in setHojas)
                {
                    if (!forms.Contains(h)) forms.Add(h);
                }
            }
        }

        if (forms.Count == 0)
            forms.Add(1);

        if (!forms.Contains(numFormulario))
            forms.Add(numFormulario);

        forms.Sort();
        _hojasPorOrden.AddOrUpdate(
            idOrdenAtencion,
            _ => new SortedSet<int>(forms),
            (_, existentes) =>
            {
                lock (existentes)
                    foreach (var form in forms) existentes.Add(form);
                return existentes;
            });

        // 1. Intentar cargar desde base de datos
        bool encontradoEnDb = false;
        var fechasDb = new Dictionary<int, EnfFechaDiaDto>();
        var signosDb = new Dictionary<int, SignoVitalSlotDto>();
        var balanceDb = new Dictionary<int, EnfBalanceHidricoDiaDto>();
        var medicionesDb = new Dictionary<int, EnfMedicionDiaDto>();

        try
        {
            var param = new { IdOrden = idOrdenAtencion, NumForm = numFormulario };

            var tFechas = _db.QueryAsync<EnfFechaDiaDto>(@"
                SELECT sec AS Id, ISNULL(orden, 1) AS Orden, fecha AS Fecha,
                       TRY_CAST(diaInternacion AS INT) AS DiasInternacion,
                       TRY_CAST(diaPostquirurgico AS INT) AS DiasPostQuirurgico
                FROM qvt_enfFechas
                WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'
                ORDER BY orden", param);

            var tSignos = _db.QueryAsync<SignoVitalSlotDto>(@"
                SELECT sec AS Id, ISNULL(orden, 1) AS Orden,
                       pulso AS Pulso,
                       temp AS Temperatura,
                       TRY_CAST(resp AS INT) AS FrecuenciaRespiratoria,
                       TRY_CAST(sistolica AS INT) AS PresionSistolica,
                       TRY_CAST(diastolica AS INT) AS PresionDiastolica
                FROM qvt_enfSignosVitales
                WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'
                ORDER BY orden", param);

            var tBalance = _db.QueryAsync<EnfBalanceHidricoDiaDto>(@"
                SELECT sec AS Id, ISNULL(orden, 1) AS Orden,
                       TRY_CAST(parental AS decimal(10,2)) AS Parenteral,
                       TRY_CAST(oral AS decimal(10,2)) AS ViaOral,
                       TRY_CAST(orina AS decimal(10,2)) AS Orina,
                       TRY_CAST(drenaje AS decimal(10,2)) AS Drenaje,
                       TRY_CAST(otros AS decimal(10,2)) AS Otros
                FROM qvt_enfBalanceHidrico
                WHERE idOrdenAtencion = @IdOrden AND numFormulario = @NumForm AND tipo = 'SIGNOS'
                ORDER BY orden", param);

            var tMediciones = _db.QueryAsync<EnfMedicionDiaDto>(@"
                SELECT sec AS Id, ISNULL(orden, 1) AS Orden,
                       TRY_CAST(REPLACE(peso, ',', '.') AS decimal(10,2)) AS Peso,
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

            var listFechas = (await tFechas).ToList();
            var listSignos = (await tSignos).ToList();
            var listBalance = (await tBalance).ToList();
            var listMediciones = (await tMediciones).ToList();

            if (listFechas.Count > 0 || listSignos.Count > 0 || listBalance.Count > 0 || listMediciones.Count > 0)
            {
                encontradoEnDb = true;
                fechasDb = listFechas.GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
                signosDb = listSignos.GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
                balanceDb = listBalance.GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
                medicionesDb = listMediciones.GroupBy(x => x.Orden).ToDictionary(g => g.Key, g => g.First());
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("No se pudieron cargar los signos vitales desde SQL Server.", ex);
        }

        if (encontradoEnDb)
        {
            var dto = new FormularioSignosEnfermeriaDto
            {
                IdOrdenAtencion = idOrdenAtencion,
                Nhcl = nhcl,
                NumFormulario = numFormulario,
                FormulariosDisponibles = forms
            };

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

            _cacheSignosMemoria[cacheKey] = ClonarSignos(dto);
            return dto;
        }

        // 2. Si no está en BD, verificar si ya fue guardado en memoria
        if (_cacheSignosMemoria.TryGetValue(cacheKey, out var guardadoEnMemoria))
        {
            var dtoMem = ClonarSignos(guardadoEnMemoria);
            dtoMem.FormulariosDisponibles = forms;
            return dtoMem;
        }

        // 3. Si no existe ni en BD ni en memoria, inicializar plantilla limpia de 9 días y 18 turnos
        var nuevo = new FormularioSignosEnfermeriaDto
        {
            IdOrdenAtencion = idOrdenAtencion,
            Nhcl = nhcl,
            NumFormulario = numFormulario,
            FormulariosDisponibles = forms
        };

        for (int dia = 1; dia <= 9; dia++)
        {
            nuevo.Fechas.Add(new EnfFechaDiaDto { Orden = dia, Fecha = dia == 1 ? DateTime.Today : null, DiasInternacion = dia });
            nuevo.BalanceHidrico.Add(new EnfBalanceHidricoDiaDto { Orden = dia });
        }

        for (int slot = 1; slot <= 18; slot++)
        {
            nuevo.SignosVitales.Add(new SignoVitalSlotDto { Orden = slot });
            nuevo.Mediciones.Add(new EnfMedicionDiaDto { Orden = slot });
        }

        _cacheSignosMemoria[cacheKey] = ClonarSignos(nuevo);
        return nuevo;
    }

    private static string? Inv(decimal? valor, string formato) =>
        valor?.ToString(formato, System.Globalization.CultureInfo.InvariantCulture);

    public async Task GuardarSignosEnfermeriaAsync(FormularioSignosEnfermeriaDto dto, int idUsuario)
    {
        await ValidarOrdenPacienteAsync(dto.IdOrdenAtencion, dto.Nhcl);
        if (dto.NumFormulario <= 0) dto.NumFormulario = 1;

        // Persistir todo el formulario en un único viaje a SQL Server.
        var sql = new StringBuilder(@"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            DELETE FROM qvt_enfFechas WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS';
            DELETE FROM qvt_enfSignosVitales WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS';
            DELETE FROM qvt_enfBalanceHidrico WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS';
            DELETE FROM qvt_enfMediciones WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS';");
        var parametros = new DynamicParameters(new
        {
            IdOrden = dto.IdOrdenAtencion,
            NumForm = dto.NumFormulario,
            ExpectedFechas = dto.Fechas.Count,
            ExpectedSignos = dto.SignosVitales.Count,
            ExpectedBalance = dto.BalanceHidrico.Count,
            ExpectedMediciones = dto.Mediciones.Count
        });

        if (dto.Fechas.Count > 0)
        {
            sql.Append(" INSERT INTO qvt_enfFechas (idOrdenAtencion,fecha,diaInternacion,diaPostquirurgico,orden,numFormulario,tipo) VALUES ");
            for (int i = 0; i < dto.Fechas.Count; i++)
            {
                if (i > 0) sql.Append(',');
                sql.Append($"(@IdOrden,@F{i}Fecha,@F{i}Internacion,@F{i}Post,@F{i}Orden,@NumForm,'SIGNOS')");
                var f = dto.Fechas[i];
                parametros.Add($"F{i}Fecha", f.Fecha);
                parametros.Add($"F{i}Internacion", f.DiasInternacion?.ToString());
                parametros.Add($"F{i}Post", f.DiasPostQuirurgico?.ToString());
                parametros.Add($"F{i}Orden", f.Orden);
            }
            sql.Append(';');
        }

        if (dto.SignosVitales.Count > 0)
        {
            sql.Append(" INSERT INTO qvt_enfSignosVitales (idOrdenAtencion,pulso,temp,resp,sistolica,diastolica,orden,numFormulario,tipo) VALUES ");
            for (int i = 0; i < dto.SignosVitales.Count; i++)
            {
                if (i > 0) sql.Append(',');
                sql.Append($"(@IdOrden,@S{i}Pulso,@S{i}Temp,@S{i}Resp,@S{i}Sis,@S{i}Dia,@S{i}Orden,@NumForm,'SIGNOS')");
                var s = dto.SignosVitales[i];
                parametros.Add($"S{i}Pulso", s.Pulso);
                parametros.Add($"S{i}Temp", s.Temperatura);
                parametros.Add($"S{i}Resp", s.FrecuenciaRespiratoria?.ToString());
                parametros.Add($"S{i}Sis", s.PresionSistolica?.ToString());
                parametros.Add($"S{i}Dia", s.PresionDiastolica?.ToString());
                parametros.Add($"S{i}Orden", s.Orden);
            }
            sql.Append(';');
        }

        if (dto.BalanceHidrico.Count > 0)
        {
            sql.Append(" INSERT INTO qvt_enfBalanceHidrico (idOrdenAtencion,parental,oral,totalIng,orina,drenaje,otros,totalElm,orden,numFormulario,tipo) VALUES ");
            for (int i = 0; i < dto.BalanceHidrico.Count; i++)
            {
                if (i > 0) sql.Append(',');
                sql.Append($"(@IdOrden,@B{i}Par,@B{i}Oral,@B{i}Ing,@B{i}Orina,@B{i}Dren,@B{i}Otros,@B{i}Elm,@B{i}Orden,@NumForm,'SIGNOS')");
                var b = dto.BalanceHidrico[i];
                parametros.Add($"B{i}Par", Inv(b.Parenteral, "0.0"));
                parametros.Add($"B{i}Oral", Inv(b.ViaOral, "0.0"));
                parametros.Add($"B{i}Ing", Inv(b.TotalIngresos, "0.0"));
                parametros.Add($"B{i}Orina", Inv(b.Orina, "0.0"));
                parametros.Add($"B{i}Dren", Inv(b.Drenaje, "0.0"));
                parametros.Add($"B{i}Otros", Inv(b.Otros, "0.0"));
                parametros.Add($"B{i}Elm", Inv(b.TotalEliminaciones, "0.0"));
                parametros.Add($"B{i}Orden", b.Orden);
            }
            sql.Append(';');
        }

        if (dto.Mediciones.Count > 0)
        {
            sql.Append(" INSERT INTO qvt_enfMediciones (idOrdenAtencion,peso,dieta,numComidas,aseo,actividadFisica,recanalizacion,cambioSonda,responsable,orden,numFormulario,tipo) VALUES ");
            for (int i = 0; i < dto.Mediciones.Count; i++)
            {
                if (i > 0) sql.Append(',');
                sql.Append($"(@IdOrden,@M{i}Peso,@M{i}Dieta,@M{i}Comidas,@M{i}Aseo,@M{i}Mov,@M{i}Vend,@M{i}Sonda,@M{i}Resp,@M{i}Orden,@NumForm,'SIGNOS')");
                var m = dto.Mediciones[i];
                parametros.Add($"M{i}Peso", Inv(m.Peso, "0.00"));
                parametros.Add($"M{i}Dieta", m.DietaIndicada);
                parametros.Add($"M{i}Comidas", m.NumeroComidas);
                parametros.Add($"M{i}Aseo", string.IsNullOrWhiteSpace(m.Banio) ? m.Aseo : m.Banio);
                parametros.Add($"M{i}Mov", m.Movilizacion);
                parametros.Add($"M{i}Vend", m.Vendajes);
                parametros.Add($"M{i}Sonda", m.CambioSonda);
                parametros.Add($"M{i}Resp", m.Responsable);
                parametros.Add($"M{i}Orden", m.Orden);
            }
            sql.Append(';');
        }

        sql.Append(@"
            IF (SELECT COUNT(*) FROM qvt_enfFechas WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS') <> @ExpectedFechas
               OR (SELECT COUNT(*) FROM qvt_enfSignosVitales WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS') <> @ExpectedSignos
               OR (SELECT COUNT(*) FROM qvt_enfBalanceHidrico WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS') <> @ExpectedBalance
               OR (SELECT COUNT(*) FROM qvt_enfMediciones WHERE idOrdenAtencion=@IdOrden AND numFormulario=@NumForm AND tipo='SIGNOS') <> @ExpectedMediciones
            BEGIN
                ROLLBACK TRANSACTION;
                THROW 51000, 'La hoja no se guardó completa en la base de datos.', 1;
            END;
            COMMIT TRANSACTION;");
        await _db.ExecuteAsync(sql.ToString(), parametros);

        _hojasPorOrden.AddOrUpdate(
            dto.IdOrdenAtencion,
            _ => new SortedSet<int> { dto.NumFormulario },
            (_, set) => { lock (set) { set.Add(dto.NumFormulario); } return set; });
        string key = $"{dto.IdOrdenAtencion}_{dto.NumFormulario}";
        _cacheSignosMemoria[key] = ClonarSignos(dto);

        try
        {
            await _db.RegistrarBitacoraAsync(
                $"Guardó Signos Vitales y Balance Hídrico Orden={dto.IdOrdenAtencion} Form={dto.NumFormulario}",
                idUsuario, "vt_EnfermeriaSignos");
        }
        catch { }
    }

    public async Task<int> CrearNuevoFormularioSignosAsync(int idOrdenAtencion, int nhcl, int idUsuario)
    {
        await ValidarOrdenPacienteAsync(idOrdenAtencion, nhcl);

        int maxForm = 0;
        maxForm = await _db.ExecuteScalarAsync<int?>(@"
                SELECT MAX(numFormulario) FROM qvt_enfSignosVitales 
                WHERE idOrdenAtencion = @IdOrden AND tipo = 'SIGNOS'",
                new { IdOrden = idOrdenAtencion }) ?? 0;

        if (_hojasPorOrden.TryGetValue(idOrdenAtencion, out var setHojas))
        {
            lock (setHojas)
            {
                if (setHojas.Count > 0)
                {
                    int maxMem = setHojas.Max();
                    if (maxMem > maxForm) maxForm = maxMem;
                }
            }
        }

        int nuevoNum = Math.Max(1, maxForm + 1);
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
        await ValidarOrdenPacienteAsync(idOrden, nhcl);
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
        await ValidarOrdenPacienteAsync(idOrden, nhcl);

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
        await ValidarOrdenPacienteAsync(sheet.IdOrden, sheet.Nhcl);
        if (sheet.DiasHospitalizacion <= 0)
            throw new ArgumentException("El día de hospitalización debe ser mayor que cero.");
        if (sheet.Peso is < 0)
            throw new ArgumentException("El peso no puede ser negativo.");
        if (sheet.Bloque1Medicacion.Any(x => x.MgKg is < 0 || x.Ml is < 0) ||
            sheet.Bloque2Alimentacion.Any(x => x.Cald is < 0 || x.Calv is < 0 || EsNumeroNegativo(x.Gr)) ||
            sheet.Bloque3Fluidoterapia.Any(x => EsNumeroNegativo(x.Mpd) || EsNumeroNegativo(x.MlDia) || EsNumeroNegativo(x.MlH) || EsNumeroNegativo(x.Gotas)))
            throw new ArgumentException("La ficha de hospitalización no admite cantidades negativas.");

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

    private static bool EsNumeroNegativo(string? valor) =>
        !string.IsNullOrWhiteSpace(valor) &&
        (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.CurrentCulture, out var numero) ||
         decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out numero)) &&
        numero < 0;

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
        await ValidarOrdenPacienteAsync(idOrden, nhcl);
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
                       ISNULL(RTRIM(codigoprod), '') AS Codigo,
                       iniciales AS Iniciales, funcion AS Funcion
                FROM hvt_cabMonitoreoMedBC
                WHERE ordenId = @IdOrden AND ISNULL(nhcl, 0) IN (0, @Nhcl) AND modulo = 'Monitoreo'
                ORDER BY fecha, proddescripcion", new { IdOrden = idOrden, Nhcl = nhcl });

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
        catch (Exception ex)
        {
            throw new InvalidOperationException("No se pudo cargar el monitoreo hospitalario desde SQL Server.", ex);
        }

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
        await _db.ExecuteAsync(@"
            INSERT INTO hvt_bloqueMonitoreo (fila, idorden, nhcl, hoja, fecha, responsable)
            SELECT v.fila, @IdOrden, @Nhcl, @Hoja, CAST(GETDATE() AS date), @Resp
            FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),
                         (13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24)) v(fila)
            WHERE NOT EXISTS (
                SELECT 1 FROM hvt_bloqueMonitoreo x
                WHERE x.idorden=@IdOrden AND x.nhcl=@Nhcl AND ISNULL(x.hoja,1)=@Hoja AND x.fila=v.fila);",
            new { IdOrden = idOrden, Nhcl = nhcl, Hoja = hoja, Resp = idUsuario });
    }

    public async Task<int> CrearNuevaHojaMonitoreoAsync(int idOrden, int nhcl, int idUsuario)
    {
        await ValidarOrdenPacienteAsync(idOrden, nhcl);

        var maxHoja = await _db.ExecuteScalarAsync<int?>(
            "SELECT MAX(hoja) FROM hvt_bloqueMonitoreo WHERE idorden = @IdOrden AND nhcl = @Nhcl",
            new { IdOrden = idOrden, Nhcl = nhcl }) ?? 0;

        int nuevaHoja = maxHoja + 1;
        await InicializarHojaMonitoreoAsync(idOrden, nhcl, nuevaHoja, idUsuario);
        return nuevaHoja;
    }

    public async Task GuardarMonitoreoHospitalarioAsync(MonitoreoHospitalarioSheetDto sheet, int idUsuario)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        await ValidarOrdenPacienteAsync(sheet.IdOrden, sheet.Nhcl);
        ValidarFilas(sheet.FilasMonitoreo, 24, "monitoreo");
        ValidarValoresMonitoreo(sheet.FilasMonitoreo);

        var sql = new StringBuilder("SET XACT_ABORT ON; BEGIN TRANSACTION;");
        var parametros = new DynamicParameters(new
        {
            sheet.IdOrden,
            sheet.Nhcl,
            sheet.Hoja,
            sheet.Fecha,
            Resp = idUsuario
        });
        AgregarActualizacionMonitoreo(sql, parametros, sheet.FilasMonitoreo);
        sql.Append("COMMIT TRANSACTION;");

        await using var connection = new SqlConnection(_db.GetConnectionString());
        await connection.OpenAsync();
        var filasActualizadas = await connection.ExecuteAsync(sql.ToString(), parametros, commandTimeout: 15);
        if (filasActualizadas != 24)
            throw new InvalidOperationException($"Se esperaban 24 filas y SQL actualizó {filasActualizadas}.");

        await _db.RegistrarBitacoraAsync(
            $"Guardó Monitoreo Hospitalario Orden={sheet.IdOrden} Hoja={sheet.Hoja}",
            idUsuario, "Hosp_Monitoreo");
    }

    private static void AgregarActualizacionMonitoreo(
        StringBuilder sql,
        DynamicParameters parametros,
        IReadOnlyList<MonitoreoFilaDto> filas)
    {
        var columnas = new[] { (Columna: "item", Propiedad: "Item") }.Concat(
            Matriz24HorasBaseDto.HorasOrden.Select(h => ($"txt{h}", $"Txt{h}")));

        sql.Append(" UPDATE hvt_bloqueMonitoreo SET ");
        bool primera = true;
        foreach (var (columna, propiedad) in columnas)
        {
            if (!primera) sql.Append(',');
            primera = false;
            sql.Append(columna).Append(" = CASE fila ");
            var propertyInfo = typeof(MonitoreoFilaDto).GetProperty(propiedad)
                ?? throw new InvalidOperationException($"No existe la propiedad {propiedad}.");
            foreach (var fila in filas)
            {
                string nombre = $"MON_{fila.Fila}_{propiedad}";
                sql.Append("WHEN ").Append(fila.Fila).Append(" THEN @").Append(nombre).Append(' ');
                parametros.Add(nombre, propertyInfo.GetValue(fila));
            }
            sql.Append("ELSE ").Append(columna).Append(" END ");
        }

        sql.Append(@", fecha=@Fecha, responsable=@Resp
            WHERE idorden=@IdOrden AND nhcl=@Nhcl AND ISNULL(hoja,1)=@Hoja;")
            .Append(" IF @@ROWCOUNT <> 24 THROW 50001, 'La hoja de monitoreo está incompleta en SQL Server.', 1;");
    }

    private static void ValidarValoresMonitoreo(IEnumerable<MonitoreoFilaDto> filas)
    {
        foreach (var fila in filas)
        {
            var regla = MonitoreoHospitalarioSheetDto.ObtenerRegla(fila.Fila);
            foreach (var hora in Matriz24HorasBaseDto.HorasOrden)
            {
                var valor = fila.GetValorHora(hora);
                if (string.IsNullOrWhiteSpace(valor)) continue;

                if (regla.EsNumerico)
                {
                    if (!decimal.TryParse(valor, NumberStyles.Number, CultureInfo.CurrentCulture, out var numero) &&
                        !decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out numero))
                        throw new ArgumentException($"{fila.ParametroLabel}, hora {hora}: debe ingresar un número.");
                    if (numero < regla.Minimo || numero > regla.Maximo)
                        throw new ArgumentException($"{fila.ParametroLabel}, hora {hora}: el valor permitido es {regla.Ayuda}.");
                }
                else if (regla.ValoresPermitidos is { Length: > 0 } &&
                         !regla.ValoresPermitidos.Contains(valor.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"{fila.ParametroLabel}, hora {hora}: use {regla.Ayuda}.");
                }
            }
        }
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
        if (input.OrdenId <= 0 || input.Nhcl <= 0 || string.IsNullOrWhiteSpace(input.CodigoProducto) || string.IsNullOrWhiteSpace(input.NombreProducto))
            return (false, "Seleccione un paciente y un producto válido.");
        if (input.Cantidad <= 0)
            return (false, "La cantidad debe ser mayor que cero.");
        if (input.Cantidad > input.StockDisponible && input.StockDisponible > 0)
            return (false, $"La cantidad ({input.Cantidad}) supera el stock disponible ({input.StockDisponible}).");

        int idInserted = await _db.ExecuteScalarAsync<int>(@"
            INSERT INTO hvt_cabMonitoreoMedBC (
                ordenId, nhcl, hora, fecha, codigoprod, precioprod, stockprod,
                indicacionesprod, cantprod, usuario, localidad, proddescripcion,
                via, origen, iniciales, funcion, funcionario, estado, modulo,
                med_iva, med_iddinamics, med_medida, med_bodega
            )
            OUTPUT INSERTED.id
            SELECT
                @OrdenId, @Nhcl, @Hora, @Fecha, @CodigoProducto, @Precio, @Stock,
                @Indicaciones, @Cantidad, @Usuario, @Localidad, @Producto,
                @Via, 'K', @Iniciales, @Funcion, @Funcionario, 'A', 'Monitoreo',
                @Iva, @IdBC, @Medida, @Bodega
            WHERE EXISTS (SELECT 1 FROM vt_ordenAtencion WHERE ordenAten_id = @OrdenId AND ordenAten_nhcl = @Nhcl)
              AND NOT EXISTS (SELECT 1 FROM hvt_cabMonitoreo WHERE idOrden = @OrdenId AND cierre = 'S')",
            new
            {
                input.OrdenId,
                input.Nhcl,
                Hora = input.HoraAdministracion.ToString(@"hh\:mm"),
                Fecha = input.FechaAdministracion.Date,
                input.CodigoProducto,
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
                input.Bodega
            });

        return idInserted > 0
            ? (true, "Medicamento de monitoreo registrado correctamente.")
            : (false, "La orden no está disponible o el monitoreo está cerrado.");
    }

    public async Task<bool> ActualizarMedicamentoMonitoreoAsync(
        int idRegistro, int idOrden, int nhcl, int cantidad, string indicaciones, string via)
    {
        if (cantidad <= 0) return false;
        await ValidarOrdenPacienteAsync(idOrden, nhcl);

        return await _db.ExecuteAsync(@"
            UPDATE hvt_cabMonitoreoMedBC
            SET cantprod = @Cantidad, indicacionesprod = @Indicaciones, via = @Via
            WHERE id = @Id AND ordenId = @IdOrden AND ISNULL(nhcl, 0) IN (0, @Nhcl) AND modulo = 'Monitoreo'
              AND (ISNULL(stockprod, 0) <= 0 OR @Cantidad <= stockprod)
              AND NOT EXISTS (SELECT 1 FROM hvt_cabMonitoreo WHERE idOrden = @IdOrden AND cierre = 'S')",
            new { Id = idRegistro, IdOrden = idOrden, Nhcl = nhcl, Cantidad = cantidad,
                Indicaciones = indicaciones.Trim().ToUpperInvariant(), Via = via.Trim() }) > 0;
    }

    public async Task<bool> EliminarMedicamentoMonitoreoAsync(int idRegistro, int idOrden, int nhcl)
    {
        await ValidarOrdenPacienteAsync(idOrden, nhcl);
        return await _db.ExecuteAsync(@"
            DELETE FROM hvt_cabMonitoreoMedBC
            WHERE id = @Id AND ordenId = @IdOrden AND ISNULL(nhcl, 0) IN (0, @Nhcl) AND modulo = 'Monitoreo'
              AND NOT EXISTS (SELECT 1 FROM hvt_cabMonitoreo WHERE idOrden = @IdOrden AND cierre = 'S')",
            new { Id = idRegistro, IdOrden = idOrden, Nhcl = nhcl }) > 0;
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

    public async Task AgregarObservacionHoraAsync(int idOrden, int nhcl, string hora, string observacion, int responsable, int idLocalidad)
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

    // ========================================================================
    // 4. BANDEJA E INGRESO A HOSPITALIZACIÓN
    // ========================================================================

    private async Task AsegurarTablaIngresoHospitalizacionAsync()
    {
        await _db.ExecuteAsync(@"
            IF OBJECT_ID('dbo.hvt_ingresoHospitalizacion', 'U') IS NULL
            CREATE TABLE dbo.hvt_ingresoHospitalizacion (
                id INT IDENTITY(1,1) PRIMARY KEY,
                idOrden INT NOT NULL,
                nhcl INT NOT NULL,
                idJaula INT NULL,
                ubicacionJaula NVARCHAR(150) NOT NULL CONSTRAINT DF_hvt_ingHosp_ubic DEFAULT(''),
                indicaciones NVARCHAR(MAX) NOT NULL CONSTRAINT DF_hvt_ingHosp_ind DEFAULT(''),
                hidratacionFluidoterapia NVARCHAR(MAX) NOT NULL CONSTRAINT DF_hvt_ingHosp_hidr DEFAULT(''),
                dietaCuidados NVARCHAR(MAX) NOT NULL CONSTRAINT DF_hvt_ingHosp_dieta DEFAULT(''),
                fechaIngreso DATETIME NOT NULL CONSTRAINT DF_hvt_ingHosp_fecha DEFAULT(GETDATE()),
                idUsuario INT NOT NULL DEFAULT(0),
                estado CHAR(1) NOT NULL CONSTRAINT DF_hvt_ingHosp_est DEFAULT('A'),
                CONSTRAINT UQ_hvt_ingHosp_orden UNIQUE (idOrden)
            );");
    }

    public async Task<List<OrdenHospitalizacionColaDto>> ObtenerColaHospitalizacionAsync(string? filtro = null, int? idLocalidad = null)
    {
        await AsegurarTablaIngresoHospitalizacionAsync();
        var tieneFiltro = !string.IsNullOrWhiteSpace(filtro);
        var filtroWhere = tieneFiltro
            ? "AND (p.mas_nombre LIKE @Filtro OR c.pa_apellidos LIKE @Filtro OR c.pa_nombres LIKE @Filtro OR CAST(o.ordenAten_id AS VARCHAR(20)) LIKE @Filtro OR CAST(p.mas_nhcl AS VARCHAR(20)) LIKE @Filtro)"
            : string.Empty;

        var sql = $@"
            SELECT TOP (80)
                o.ordenAten_id AS OrdenAtencionId,
                p.mas_nhcl AS Nhcl,
                RTRIM(ISNULL(p.mas_nombre, '')) AS Paciente,
                RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                RTRIM(ISNULL(e.especie_descripcion, 'No Definida')) AS Especie,
                CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 'Hembra'
                     WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 'Macho'
                     ELSE RTRIM(ISNULL(p.mas_sexo, 'Indefinido')) END AS Sexo,
                RTRIM(ISNULL(u.user_nombre, '')) AS Medico,
                CASE WHEN ISNULL(o.ordenAten_esProcedimiento, 0) = 1 THEN 'Procedimiento'
                     ELSE RTRIM(ISNULL(esp.es_descripcion, '')) END AS Especialidad,
                ISNULL(o.ordenAten_fecha, GETDATE()) AS FechaOrden,
                RTRIM(ISNULL(o.ordenAten_estado, 'A')) AS Estado,
                RTRIM(ISNULL(o.ordenAten_etapa, 'Consulta')) AS Etapa,
                ing.idJaula AS JaulaId,
                ISNULL(ing.ubicacionJaula, '') AS JaulaCodigo,
                CAST(CASE WHEN ing.id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS YaIngresado,
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM vt_fichamedica fm1
                    WHERE fm1.med_orden = o.ordenAten_id
                      AND ISNULL(fm1.med_requiereHospitalizacion, 0) = 1
                ) THEN 1 ELSE 0 END AS BIT) AS RequiereHospitalizacion,
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM vt_fichamedica fm2
                    WHERE fm2.med_orden = o.ordenAten_id
                      AND UPPER(RTRIM(ISNULL(fm2.med_pregunta, 'N'))) = 'S'
                ) THEN 1 ELSE 0 END AS BIT) AS RequiereCirugia
            FROM vt_ordenAtencion o
            INNER JOIN vt_pacientes p ON o.ordenAten_nhcl = p.mas_nhcl
            INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
            LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
            LEFT JOIN vt_especialidades esp ON o.ordenAten_esp_numero = esp.es_id
            LEFT JOIN hvt_ingresoHospitalizacion ing ON ing.idOrden = o.ordenAten_id AND ing.estado = 'A'
            WHERE EXISTS (
                SELECT 1 FROM vt_fichamedica fm
                WHERE fm.med_orden = o.ordenAten_id
                  AND (ISNULL(fm.med_requiereHospitalizacion, 0) = 1
                       OR UPPER(RTRIM(ISNULL(fm.med_pregunta, 'N'))) = 'S')
            )
              AND ISNULL(o.ordenAten_estado, 'A') <> 'I'
              AND ISNULL(o.cierrehospi, 'N') <> 'S'
              AND (@IdLocalidad IS NULL OR @IdLocalidad <= 0 OR o.ordenAten_idlocalidad = @IdLocalidad)
              {filtroWhere}
            ORDER BY CASE WHEN ing.id IS NULL THEN 0 ELSE 1 END, o.ordenAten_id DESC";

        return (await _db.QueryAsync<OrdenHospitalizacionColaDto>(sql, new
        {
            Filtro = $"%{filtro?.Trim()}%",
            IdLocalidad = idLocalidad
        })).ToList();
    }

    public async Task<IngresoHospitalizacionDto> ObtenerIngresoHospitalizacionAsync(int idOrden, int nhcl)
    {
        await ValidarOrdenPacienteAsync(idOrden, nhcl);
        await AsegurarTablaIngresoHospitalizacionAsync();

        var existente = await _db.QueryFirstOrDefaultAsync<IngresoHospitalizacionDto>(@"
            SELECT id AS Id, idOrden AS IdOrden, nhcl AS Nhcl, idJaula AS IdJaula,
                   ISNULL(ubicacionJaula, '') AS UbicacionJaula,
                   ISNULL(indicaciones, '') AS Indicaciones,
                   ISNULL(hidratacionFluidoterapia, '') AS HidratacionFluidoterapia,
                   ISNULL(dietaCuidados, '') AS DietaCuidados,
                   fechaIngreso AS FechaIngreso, idUsuario AS IdUsuario, estado AS Estado
            FROM hvt_ingresoHospitalizacion
            WHERE idOrden = @IdOrden AND nhcl = @Nhcl",
            new { IdOrden = idOrden, Nhcl = nhcl });

        return existente ?? new IngresoHospitalizacionDto { IdOrden = idOrden, Nhcl = nhcl };
    }

    public async Task GuardarIngresoHospitalizacionAsync(IngresoHospitalizacionDto dto, int idUsuario)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.IdOrden <= 0 || dto.Nhcl <= 0)
            throw new ArgumentException("La orden y NHCL son obligatorios para iniciar la hospitalización.");

        await ValidarOrdenPacienteAsync(dto.IdOrden, dto.Nhcl);
        await AsegurarTablaIngresoHospitalizacionAsync();

        var jaulaAnterior = await _db.ExecuteScalarAsync<int?>(
            "SELECT idJaula FROM hvt_ingresoHospitalizacion WHERE idOrden = @IdOrden",
            new { dto.IdOrden });

        var ubicacion = (dto.UbicacionJaula ?? string.Empty).Trim();
        var indicaciones = (dto.Indicaciones ?? string.Empty).Trim();
        var hidratacion = (dto.HidratacionFluidoterapia ?? string.Empty).Trim();
        var dieta = (dto.DietaCuidados ?? string.Empty).Trim();

        await _db.ExecuteAsync(@"
            IF EXISTS (SELECT 1 FROM hvt_ingresoHospitalizacion WHERE idOrden = @IdOrden)
            BEGIN
                UPDATE hvt_ingresoHospitalizacion
                SET idJaula = @IdJaula,
                    ubicacionJaula = @UbicacionJaula,
                    indicaciones = @Indicaciones,
                    hidratacionFluidoterapia = @HidratacionFluidoterapia,
                    dietaCuidados = @DietaCuidados,
                    idUsuario = @IdUsuario,
                    estado = 'A'
                WHERE idOrden = @IdOrden;
            END
            ELSE
            BEGIN
                INSERT INTO hvt_ingresoHospitalizacion
                    (idOrden, nhcl, idJaula, ubicacionJaula, indicaciones, hidratacionFluidoterapia, dietaCuidados, idUsuario, estado)
                VALUES
                    (@IdOrden, @Nhcl, @IdJaula, @UbicacionJaula, @Indicaciones, @HidratacionFluidoterapia, @DietaCuidados, @IdUsuario, 'A');
            END

            UPDATE vt_ordenAtencion
            SET ordenAten_etapa = 'Hospitalización'
            WHERE ordenAten_id = @IdOrden AND ISNULL(cierrehospi, 'N') <> 'S';

            IF OBJECT_ID('dbo.vt_jaulas', 'U') IS NOT NULL
            BEGIN
                IF @JaulaAnterior IS NOT NULL AND @JaulaAnterior > 0 AND (@IdJaula IS NULL OR @IdJaula <> @JaulaAnterior)
                    UPDATE vt_jaulas SET jau_ocupada = 0, jau_nhcl_actual = NULL, jau_orden_actual = NULL WHERE jau_id = @JaulaAnterior;

                IF @IdJaula IS NOT NULL AND @IdJaula > 0
                    UPDATE vt_jaulas SET jau_ocupada = 1, jau_nhcl_actual = @Nhcl, jau_orden_actual = @IdOrden WHERE jau_id = @IdJaula;
            END",
            new
            {
                dto.IdOrden,
                dto.Nhcl,
                IdJaula = dto.IdJaula is > 0 ? dto.IdJaula : null,
                UbicacionJaula = ubicacion,
                Indicaciones = indicaciones,
                HidratacionFluidoterapia = hidratacion,
                DietaCuidados = dieta,
                IdUsuario = idUsuario,
                JaulaAnterior = jaulaAnterior
            });

        await _db.RegistrarBitacoraAsync(
            $"Ingreso a hospitalización Orden={dto.IdOrden} Jaula={(string.IsNullOrWhiteSpace(ubicacion) ? "Sin asignar" : ubicacion)}",
            idUsuario, "Hosp_BandejaOrdenes");
    }

    public async Task<(bool TieneDatosCirugia, bool TieneDatosHospitalizacion)> VerificarDatosModulosOrdenAsync(int idOrden, int nhcl)
    {
        await AsegurarTablaIngresoHospitalizacionAsync();
        const string sql = @"
            SELECT
                CAST(CASE WHEN
                    (OBJECT_ID('dbo.qvt_calendario_cirugias', 'U') IS NOT NULL AND EXISTS (
                        SELECT 1 FROM qvt_calendario_cirugias WHERE nhcl = @Nhcl AND estado = 'A' AND fechacirugia >= CAST(GETDATE() AS date)))
                    OR EXISTS (SELECT 1 FROM qvt_preanestesia WHERE orden = @IdOrden)
                    OR EXISTS (SELECT 1 FROM qvt_RegistroAnestesia WHERE idOrden = @IdOrden)
                    OR EXISTS (SELECT 1 FROM qvt_Protocolo WHERE idOrden = @IdOrden)
                    OR EXISTS (SELECT 1 FROM qvt_observaciones_recuperacion WHERE idOrden = @IdOrden AND estado = 'A')
                    OR EXISTS (SELECT 1 FROM qvt_enfFechas WHERE idOrdenAtencion = @IdOrden AND diaPostquirurgico IS NOT NULL AND diaPostquirurgico > 0)
                THEN 1 ELSE 0 END AS BIT) AS TieneCirugia,
                CAST(CASE WHEN
                    EXISTS (SELECT 1 FROM hvt_ingresoHospitalizacion WHERE idOrden = @IdOrden AND estado = 'A')
                    OR EXISTS (SELECT 1 FROM qvt_enfSignosVitales WHERE idOrdenAtencion = @IdOrden)
                    OR EXISTS (SELECT 1 FROM hvt_bloque1ficha WHERE idorden = @IdOrden AND NULLIF(LTRIM(RTRIM(ISNULL(medicacion, ''))), '') IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM hvt_bloque2ficha WHERE idorden = @IdOrden AND NULLIF(LTRIM(RTRIM(ISNULL(alimentacion, ''))), '') IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM hvt_bloque3ficha WHERE idorden = @IdOrden AND NULLIF(LTRIM(RTRIM(ISNULL(fluidoterapia, ''))), '') IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM hvt_bloqueMonitoreo WHERE idorden = @IdOrden AND NULLIF(LTRIM(RTRIM(
                        ISNULL(txt1,'') + ISNULL(txt2,'') + ISNULL(txt3,'') + ISNULL(txt4,'') +
                        ISNULL(txt5,'') + ISNULL(txt6,'') + ISNULL(txt7,'') + ISNULL(txt8,'') +
                        ISNULL(txt9,'') + ISNULL(txt10,'') + ISNULL(txt11,'') + ISNULL(txt12,'') +
                        ISNULL(txt13,'') + ISNULL(txt14,'') + ISNULL(txt15,'') + ISNULL(txt16,'') +
                        ISNULL(txt17,'') + ISNULL(txt18,'') + ISNULL(txt19,'') + ISNULL(txt20,'') +
                        ISNULL(txt21,'') + ISNULL(txt22,'') + ISNULL(txt23,'') + ISNULL(txt24,''))), '') IS NOT NULL)
                THEN 1 ELSE 0 END AS BIT) AS TieneHospitalizacion;";

        var res = await _db.QueryFirstOrDefaultAsync<(bool TieneCirugia, bool TieneHospitalizacion)>(sql, new { IdOrden = idOrden, Nhcl = nhcl });
        return (res.TieneCirugia, res.TieneHospitalizacion);
    }

    public async Task<bool> ActualizarIndicacionesModalAsync(int idOrden, int nhcl, bool requiereCirugia, bool requiereHospitalizacion, int idUsuario)
    {
        if (idOrden <= 0 || nhcl <= 0) return false;
        await ValidarOrdenPacienteAsync(idOrden, nhcl);

        var actuales = await VerificarDatosModulosOrdenAsync(idOrden, nhcl);
        if (!requiereCirugia && actuales.TieneDatosCirugia)
            throw new InvalidOperationException("No se puede deshabilitar Cirugía porque ya existen registros en sus módulos.");
        if (!requiereHospitalizacion && actuales.TieneDatosHospitalizacion)
            throw new InvalidOperationException("No se puede deshabilitar Hospitalización porque ya existen registros en sus módulos.");

        var filas = await _db.ExecuteAsync(@"
            UPDATE vt_fichamedica
            SET med_pregunta = @Pregunta,
                med_requiereHospitalizacion = @ReqHosp,
                med_idmodifica = @IdUsuario,
                med_fechamodifica = GETDATE()
            WHERE med_orden = @IdOrden AND med_nhcl = @Nhcl;",
            new
            {
                IdOrden = idOrden,
                Nhcl = nhcl,
                Pregunta = requiereCirugia ? "S" : "N",
                ReqHosp = requiereHospitalizacion ? 1 : 0,
                IdUsuario = idUsuario
            });

        if (filas > 0)
        {
            await _db.RegistrarBitacoraAsync(
                $"Actualizó indicaciones desde bandeja Orden={idOrden} Cirugía={requiereCirugia} Hosp={requiereHospitalizacion}",
                idUsuario, "Hosp_BandejaOrdenes");
        }
        return filas > 0;
    }

    public async Task<bool> OrdenRequiereCirugiaAsync(int idOrden)
    {
        if (idOrden <= 0) return false;
        var count = await _db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM vt_fichamedica
            WHERE med_orden = @IdOrden
              AND UPPER(RTRIM(ISNULL(med_pregunta, 'N'))) = 'S'",
            new { IdOrden = idOrden });
        return count > 0;
    }
}
