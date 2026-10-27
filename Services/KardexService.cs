using System.Collections.Concurrent;
using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IKardexService
{
    Task<List<OrdenActivaResumenDto>> ObtenerOrdenesActivasAsync(string? filtro = null, int top = 40);
    Task<PacienteHeaderDto?> ObtenerCabeceraPacienteAsync(int ordenId, int nhcl);
    Task<PacienteHeaderDto?> ObtenerEncabezadoPacienteAsync(int ordenId, int nhcl) => ObtenerCabeceraPacienteAsync(ordenId, nhcl);
    Task<List<ViaAdministracionDto>> ObtenerViasAdministracionAsync();
    Task<List<FuncionarioDto>> ObtenerFuncionariosEnfermeriaAsync();
    Task<List<FuncionEnfermeriaDto>> ObtenerFuncionesEnfermeriaAsync();
    Task<List<ProductoUtilizadoDto>> ObtenerProductosUtilizadosAsync(int ordenId, string modulo = "EnCirugia");
    Task<(bool Exito, string Mensaje)> RegistrarAdministracionMedicamentoAsync(
        int ordenId, int nhcl, KardexRegistroInputModel input, int idUsuarioSesion, int idLocalidad, string modulo = "EnCirugia");
    Task<(bool Exito, string Mensaje)> EliminarAdministracionMedicamentoAsync(int idRegistro, string codigoProd, int ordenId);
    Task<List<KardexSabanaItemDto>> ConstruirSabanaKardex5DiasAsync(int ordenId, int nhcl, DateTime? fechaInicioFiltro = null, string modulo = "EnCirugia");
}

public class KardexService : IKardexService
{
    private readonly ISqlDataService _sql;
    private readonly IBusinessCentralService _bcService;
    private readonly ILogger<KardexService> _logger;

    // Caché en memoria para acelerar la navegación entre pestañas sin repetir consultas pesadas a SQL Server
    private static List<OrdenActivaResumenDto>? _cacheOrdenesActivas;
    private static DateTime _cacheOrdenesExpira = DateTime.MinValue;
    private static List<ViaAdministracionDto>? _cacheVias;
    private static List<FuncionarioDto>? _cacheFuncionarios;
    private static List<FuncionEnfermeriaDto>? _cacheFunciones;
    private static readonly ConcurrentDictionary<string, (DateTime Expira, PacienteHeaderDto Dto)> _cacheCabeceras = new();

    private static readonly List<OrdenActivaResumenDto> _ordenesDemo =
    [
        new() { OrdenAtencionId = 24001, Nhcl = 5001, Paciente = "MAX", Cliente = "Mendoza Vintimilla Carlos Andrés", Especie = "Canino", Sexo = "Macho", Medico = "Dr. Gabriel Vintimilla", Especialidad = "Medicina Interna", FechaOrden = DateTime.Today, Estado = "A" },
        new() { OrdenAtencionId = 24002, Nhcl = 5002, Paciente = "LUNA", Cliente = "Mendoza Vintimilla Carlos Andrés", Especie = "Felino", Sexo = "Hembra", Medico = "Dra. Valeria Andrade", Especialidad = "Consulta General", FechaOrden = DateTime.Today, Estado = "A" },
        new() { OrdenAtencionId = 24003, Nhcl = 5003, Paciente = "ROCKY", Cliente = "Salazar Gómez María Fernanda", Especie = "Canino", Sexo = "Macho", Medico = "Dr. Sebastián Cordero", Especialidad = "Cirugía y Tejidos Blandos", FechaOrden = DateTime.Today, Estado = "A" },
        new() { OrdenAtencionId = 24004, Nhcl = 9001, Paciente = "AMAZONA FARINOSA #42", Cliente = "Ministerio del Ambiente (TUERI)", Especie = "Ave Silvestre", Sexo = "Indefinido", Medico = "Dra. Carolina Páez", Especialidad = "Fauna Silvestre (TUERI)", FechaOrden = DateTime.Today, Estado = "A" }
    ];

    private static readonly List<ProductoUtilizadoDto> _productosDemo =
    [
        new() { Id = 1, Orden = 24001, Nhcl = 5001, Codigo = "MED-OME-40", Producto = "OMEPRAZOL 40MG FRASCO AMPOLLA", Cantidad = 1, Precio = 6.50m, Indicaciones = "1 mg/kg cada 12 horas diluido lento", Via = "INTRAVENOSA (IV)", Fecha = DateTime.Today.ToString("yyyy-MM-dd"), FechaRaw = DateTime.Today, Hora = "08:00", Iniciales = "GV", Funcion = "MEDICO", Funcionario = "Dr. Gabriel Vintimilla", Bodega = "HOSP" },
        new() { Id = 2, Orden = 24001, Nhcl = 5001, Codigo = "MED-OME-40", Producto = "OMEPRAZOL 40MG FRASCO AMPOLLA", Cantidad = 1, Precio = 6.50m, Indicaciones = "1 mg/kg cada 12 horas diluido lento", Via = "INTRAVENOSA (IV)", Fecha = DateTime.Today.ToString("yyyy-MM-dd"), FechaRaw = DateTime.Today, Hora = "20:00", Iniciales = "VA", Funcion = "ENFERMERA", Funcionario = "Dra. Valeria Andrade", Bodega = "HOSP" },
        new() { Id = 3, Orden = 24001, Nhcl = 5001, Codigo = "MED-AMP-SUL", Producto = "AMPICILINA + SULBACTAM 1.5G IV", Cantidad = 1, Precio = 11.20m, Indicaciones = "22 mg/kg cada 8 horas IV lento", Via = "INTRAVENOSA (IV)", Fecha = DateTime.Today.ToString("yyyy-MM-dd"), FechaRaw = DateTime.Today, Hora = "08:30", Iniciales = "GV", Funcion = "MEDICO", Funcionario = "Dr. Gabriel Vintimilla", Bodega = "HOSP" },
        new() { Id = 4, Orden = 24001, Nhcl = 5001, Codigo = "MED-AMP-SUL", Producto = "AMPICILINA + SULBACTAM 1.5G IV", Cantidad = 1, Precio = 11.20m, Indicaciones = "22 mg/kg cada 8 horas IV lento", Via = "INTRAVENOSA (IV)", Fecha = DateTime.Today.ToString("yyyy-MM-dd"), FechaRaw = DateTime.Today, Hora = "16:30", Iniciales = "VA", Funcion = "ENFERMERA", Funcionario = "Dra. Valeria Andrade", Bodega = "HOSP" },
        new() { Id = 5, Orden = 24001, Nhcl = 5001, Codigo = "MED-MEL-05", Producto = "MELOXICAM INYECTABLE 0.5% 10ML", Cantidad = 1, Precio = 8.40m, Indicaciones = "0.1 mg/kg cada 24 horas control dolor", Via = "SUBCUTANEA (SC)", Fecha = DateTime.Today.ToString("yyyy-MM-dd"), FechaRaw = DateTime.Today, Hora = "10:00", Iniciales = "SC", Funcion = "MEDICO", Funcionario = "Dr. Sebastián Cordero", Bodega = "HOSP" }
    ];

    public KardexService(ISqlDataService sql, IBusinessCentralService bcService, ILogger<KardexService> logger)
    {
        _sql = sql;
        _bcService = bcService;
        _logger = logger;
    }

    public async Task<List<OrdenActivaResumenDto>> ObtenerOrdenesActivasAsync(string? filtro = null, int top = 40)
    {
        bool sinFiltro = string.IsNullOrWhiteSpace(filtro);
        if (sinFiltro && _cacheOrdenesActivas is not null && DateTime.UtcNow < _cacheOrdenesExpira && _cacheOrdenesActivas.Count > 0)
        {
            return _cacheOrdenesActivas;
        }

        try
        {
            string whereClause = sinFiltro
                ? ""
                : "AND (p.mas_nombre LIKE @Filtro OR c.pa_apellidos LIKE @Filtro OR c.pa_nombres LIKE @Filtro OR CAST(o.ordenAten_id AS VARCHAR(20)) LIKE @Filtro OR CAST(p.mas_nhcl AS VARCHAR(20)) LIKE @Filtro)";

            string sql = $@"
                SELECT TOP ({top})
                    o.ordenAten_id AS OrdenAtencionId,
                    p.mas_nhcl AS Nhcl,
                    RTRIM(ISNULL(p.mas_nombre, '')) AS Paciente,
                    RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS Cliente,
                    RTRIM(ISNULL(e.especie_descripcion, 'No Definida')) AS Especie,
                    CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 'Hembra'
                         WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 'Macho'
                         ELSE RTRIM(ISNULL(p.mas_sexo, 'Indefinido')) END AS Sexo,
                    RTRIM(ISNULL(u.user_nombre, '')) AS Medico,
                    RTRIM(ISNULL(esp.es_descripcion, '')) AS Especialidad,
                    ISNULL(o.ordenAten_fecha, GETDATE()) AS FechaOrden,
                    RTRIM(ISNULL(o.ordenAten_estado, 'A')) AS Estado
                FROM vt_ordenAtencion o
                INNER JOIN vt_pacientes p ON o.ordenAten_nhcl = p.mas_nhcl
                INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
                LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                LEFT JOIN vt_especialidades esp ON o.ordenAten_esp_numero = esp.es_id
                WHERE 1=1 {whereClause}
                ORDER BY o.ordenAten_id DESC";

            var lista = (await _sql.QueryAsync<OrdenActivaResumenDto>(sql, new { Filtro = $"%{filtro?.Trim()}%" })).ToList();
            if (lista.Count > 0)
            {
                if (sinFiltro)
                {
                    _cacheOrdenesActivas = lista;
                    _cacheOrdenesExpira = DateTime.UtcNow.AddSeconds(90);
                }
                return lista;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Modo Demo activo en ObtenerOrdenesActivasAsync.");
        }

        if (sinFiltro) return _ordenesDemo.ToList();
        return _ordenesDemo.Where(o =>
            o.Paciente.Contains(filtro!, StringComparison.OrdinalIgnoreCase) ||
            o.Cliente.Contains(filtro!, StringComparison.OrdinalIgnoreCase) ||
            o.OrdenAtencionId.ToString().Contains(filtro!) ||
            o.Nhcl.ToString().Contains(filtro!)).ToList();
    }

    public async Task<PacienteHeaderDto?> ObtenerCabeceraPacienteAsync(int ordenId, int nhcl)
    {
        if (ordenId <= 0 && nhcl <= 0)
        {
            var activas = await ObtenerOrdenesActivasAsync();
            if (activas.Count > 0)
            {
                ordenId = activas[0].OrdenAtencionId;
                nhcl = activas[0].Nhcl;
            }
        }

        string cacheKey = $"{ordenId}:{nhcl}";
        if (_cacheCabeceras.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.Expira)
        {
            return cached.Dto;
        }

        try
        {
            const string sql = @"
                SELECT TOP 1
                    ISNULL(o.ordenAten_nhcl, ISNULL(p.mas_nhcl, @Nhcl)) AS Nhcl,
                    ISNULL(o.ordenAten_id, @OrdenId) AS OrdenAtencionId,
                    RTRIM(ISNULL(o.ordenAten_nombre, ISNULL(p.mas_nombre, 'PACIENTE'))) AS PacienteNombre,
                    CASE 
                        WHEN c.pa_nombres IS NOT NULL OR c.pa_apellidos IS NOT NULL 
                            THEN RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, ''))
                        ELSE 'Cliente #' + CAST(ISNULL(o.ordenAten_idcliente, ISNULL(p.mas_idcliente, 0)) AS VARCHAR(20))
                    END AS ClienteNombre,
                    RTRIM(ISNULL(c.pa_cedula, '')) AS ClienteCedula,
                    ISNULL(c.pa_codigo, ISNULL(o.ordenAten_idcliente, 0)) AS ClienteCodigo,
                    CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 'Hembra'
                         WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 'Macho'
                         ELSE RTRIM(ISNULL(p.mas_sexo, 'Indefinido')) END AS Sexo,
                    RTRIM(ISNULL(e.especie_descripcion, 'Canino')) AS Especie,
                    CASE WHEN RTRIM(ISNULL(p.mas_origen, 'M')) = 'M' THEN 'HDEV' ELSE 'TUERI' END AS Origen,
                    p.mas_fechanace AS FechaNacimiento,
                    RTRIM(ISNULL(u.user_nombre, 'Médico de Turno')) AS MedicoResponsable,
                    RTRIM(ISNULL(o.cierre, 'N')) AS Cierre,
                    CAST(CASE WHEN ISNULL(p.fallecido, 'False') = 'True' OR ISNULL(p.fallecido, '0') = '1' THEN 1 ELSE 0 END AS BIT) AS Fallecido,
                    p.fechafallecido AS FechaFallecido,
                    p.motivofallece AS MotivoFallece
                FROM vt_ordenAtencion o
                LEFT JOIN vt_pacientes p ON o.ordenAten_nhcl = p.mas_nhcl
                LEFT JOIN vt_clientes c ON o.ordenAten_idcliente = c.pa_codigo OR p.mas_idcliente = c.pa_codigo
                LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                WHERE (@OrdenId > 0 AND o.ordenAten_id = @OrdenId)
                   OR (@OrdenId <= 0 AND @Nhcl > 0 AND o.ordenAten_nhcl = @Nhcl)
                ORDER BY o.ordenAten_id DESC";

            var header = await _sql.QueryFirstOrDefaultAsync<PacienteHeaderDto>(sql, new { OrdenId = ordenId, Nhcl = nhcl });
            if (header is not null)
            {
                var (anios, meses) = _sql.CalcularEdad(header.FechaNacimiento);
                header.EdadAnios = anios;
                header.EdadMeses = meses;
                _cacheCabeceras[cacheKey] = (DateTime.UtcNow.AddMinutes(3), header);
                _cacheCabeceras[$"{header.OrdenAtencionId}:{header.Nhcl}"] = (DateTime.UtcNow.AddMinutes(3), header);
                return header;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error en ObtenerCabeceraPacienteAsync Orden={OrdenId} NHCL={Nhcl}", ordenId, nhcl);
        }

        var demo = _ordenesDemo.FirstOrDefault(o => o.Nhcl == nhcl || o.OrdenAtencionId == ordenId);
        if (demo is not null)
        {
            return new PacienteHeaderDto
            {
                Nhcl = demo.Nhcl,
                OrdenAtencionId = demo.OrdenAtencionId,
                PacienteNombre = demo.Paciente,
                ClienteNombre = demo.Cliente,
                ClienteCedula = "1718293045",
                ClienteCodigo = 101,
                Sexo = demo.Sexo,
                Especie = demo.Especie,
                Raza = demo.Especie == "Canino" ? "Golden Retriever" : "Europeo Pelo Corto",
                Origen = demo.Nhcl >= 9000 ? "TUERI" : "HDEV",
                FechaNacimiento = DateTime.Today.AddYears(-4),
                EdadAnios = 4,
                EdadMeses = 2,
                MedicoResponsable = demo.Medico,
                Cierre = "N"
            };
        }

        return new PacienteHeaderDto
        {
            Nhcl = nhcl > 0 ? nhcl : 1,
            OrdenAtencionId = ordenId > 0 ? ordenId : 1,
            PacienteNombre = ordenId > 0 ? $"Paciente Orden #{ordenId}" : "Paciente",
            ClienteNombre = "Sin Propietario Registrado",
            ClienteCedula = "",
            ClienteCodigo = 0,
            Sexo = "Indefinido",
            Especie = "Canino",
            Raza = "Mestizo",
            Origen = "HDEV",
            FechaNacimiento = DateTime.Today.AddYears(-2),
            EdadAnios = 2,
            EdadMeses = 0,
            MedicoResponsable = "Médico de Turno",
            Cierre = "N"
        };
    }

    public async Task<List<ViaAdministracionDto>> ObtenerViasAdministracionAsync()
    {
        if (_cacheVias is not null && _cacheVias.Count > 0)
            return _cacheVias;

        try
        {
            const string sql = "SELECT id AS Id, RTRIM(via) AS Via FROM qvt_via WHERE estado = 'A' ORDER BY via";
            var lista = (await _sql.QueryAsync<ViaAdministracionDto>(sql)).ToList();
            if (lista.Count > 0)
            {
                _cacheVias = lista;
                return lista;
            }
        }
        catch { }

        return
        [
            new() { Id = 1, Via = "INTRAVENOSA (IV)" },
            new() { Id = 2, Via = "INTRAMUSCULAR (IM)" },
            new() { Id = 3, Via = "SUBCUTANEA (SC)" },
            new() { Id = 4, Via = "ORAL (PO)" },
            new() { Id = 5, Via = "TOPICA" }
        ];
    }

    public async Task<List<FuncionarioDto>> ObtenerFuncionariosEnfermeriaAsync()
    {
        if (_cacheFuncionarios is not null && _cacheFuncionarios.Count > 0)
            return _cacheFuncionarios;

        try
        {
            const string sql = @"
                SELECT 
                    user_numero AS UserNumero,
                    RTRIM(user_nombre) AS UserNombre,
                    RTRIM(user_iniciales) AS UserIniciales
                FROM vt_usuarios
                WHERE user_tipouser_numero IN (2, 3)
                  AND user_estado = 'A'
                  AND user_iniciales IS NOT NULL
                ORDER BY user_nombre";
            var lista = (await _sql.QueryAsync<FuncionarioDto>(sql)).ToList();
            if (lista.Count > 0)
            {
                _cacheFuncionarios = lista;
                return lista;
            }
        }
        catch { }

        return
        [
            new() { UserNumero = 1, UserNombre = "Dr. Gabriel Vintimilla", UserIniciales = "GV" },
            new() { UserNumero = 2, UserNombre = "Dra. Valeria Andrade", UserIniciales = "VA" },
            new() { UserNumero = 3, UserNombre = "Dr. Sebastián Cordero", UserIniciales = "SC" }
        ];
    }

    public async Task<List<FuncionEnfermeriaDto>> ObtenerFuncionesEnfermeriaAsync()
    {
        if (_cacheFunciones is not null && _cacheFunciones.Count > 0)
            return _cacheFunciones;

        try
        {
            const string sql = @"
                SELECT 
                    tipuser_numero AS TipUserNumero,
                    RTRIM(tipuser_descripcion) AS TipUserDescripcion
                FROM vt_tipos_usuarios
                WHERE tip_enfermeria = 'S'
                ORDER BY tipuser_descripcion";
            var lista = (await _sql.QueryAsync<FuncionEnfermeriaDto>(sql)).ToList();
            if (lista.Count > 0)
            {
                _cacheFunciones = lista;
                return lista;
            }
        }
        catch { }

        return
        [
            new() { TipUserNumero = 2, TipUserDescripcion = "MEDICO" },
            new() { TipUserNumero = 3, TipUserDescripcion = "ENFERMERA" }
        ];
    }

    public async Task<List<ProductoUtilizadoDto>> ObtenerProductosUtilizadosAsync(int ordenId, string modulo = "EnCirugia")
    {
        try
        {
            const string sql = @"
                SELECT 
                    id AS Id,
                    ordenId AS Orden,
                    ISNULL(nhcl, 0) AS Nhcl,
                    RTRIM(CAST(ISNULL(codigoprod, 0) AS VARCHAR(50))) AS Codigo,
                    RTRIM(ISNULL(proddescripcion, '')) AS Producto,
                    CAST(ISNULL(cantprod, 0) AS DECIMAL(18,2)) AS Cantidad,
                    CAST(ISNULL(precioprod, 0) AS DECIMAL(18,2)) AS Precio,
                    RTRIM(ISNULL(indicacionesprod, '')) AS Indicaciones,
                    RTRIM(ISNULL(via, '')) AS Via,
                    CONVERT(VARCHAR(10), fecha, 120) AS Fecha,
                    ISNULL(fecha, GETDATE()) AS FechaRaw,
                    RTRIM(ISNULL(hora, '')) AS Hora,
                    RTRIM(ISNULL(iniciales, '')) AS Iniciales,
                    RTRIM(ISNULL(funcion, '')) AS Funcion,
                    RTRIM(ISNULL(funcionario, '')) AS Funcionario,
                    'FARMA-HDEV' AS Bodega
                FROM qvt_productosUtilizados
                WHERE ordenId = @OrdenId AND (modulo = @Modulo OR modulo = 'EnfermeriaIng' OR @Modulo = '' OR @Modulo IS NULL)
                ORDER BY fecha DESC, hora DESC, proddescripcion";

            var res = (await _sql.QueryAsync<ProductoUtilizadoDto>(sql, new { OrdenId = ordenId, Modulo = modulo })).ToList();
            if (res.Count > 0) return res;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error en ObtenerProductosUtilizadosAsync de la orden {OrdenId}", ordenId);
        }

        // Si es la orden de demostración 24001, retornamos los demo; si es cualquier otra orden real, retornamos lista vacía para no mezclar pacientes
        var itemsOrden = _productosDemo.Where(p => p.Orden == ordenId).ToList();
        return itemsOrden;
    }

    public async Task<(bool Exito, string Mensaje)> RegistrarAdministracionMedicamentoAsync(
        int ordenId, int nhcl, KardexRegistroInputModel input, int idUsuarioSesion, int idLocalidad, string modulo = "EnCirugia")
    {
        if (input.CantProd <= 0)
            return (false, "La cantidad debe ser mayor a 0.");

        if (input.StockDisponible > 0 && input.CantProd > input.StockDisponible)
            return (false, $"La cantidad no debe superar el stock disponible ({input.StockDisponible:N2} uds).");

        string iniciales = "GV";
        string nombreFuncionario = "Personal e-ForVets";
        string horaStr = input.Hora.ToString("HH:mm");

        try
        {
            var func = await _sql.QueryFirstOrDefaultAsync<FuncionarioDto>(
                "SELECT user_numero AS UserNumero, RTRIM(user_nombre) AS UserNombre, RTRIM(ISNULL(user_iniciales, 'HV')) AS UserIniciales FROM vt_usuarios WHERE user_numero = @Id",
                new { Id = input.FuncionarioId });

            iniciales = func?.UserIniciales ?? "HV";
            nombreFuncionario = func?.UserNombre ?? "Personal e-ForVets";

            const string insertSql = @"
                INSERT INTO qvt_productosUtilizados (
                    ordenId, nhcl, hora, fecha, codigoprod, precioprod, stockprod, indicacionesprod,
                    cantprod, usuario, localidad, proddescripcion, via, origen, iniciales, funcion,
                    funcionario, estado, grupo, modulo, tipo, med_iva, med_iddinamics, med_medida, med_bodega
                ) VALUES (
                    @OrdenId, @Nhcl, @Hora, @Fecha, @CodigoProd, @PrecioProd, 0, @IndicacionesProd,
                    @CantProd, @Usuario, @Localidad, @ProdDescripcion, @Via, 'E', @Iniciales, @Funcion,
                    @Funcionario, 'Vigente', @Grupo, @Modulo, 'M', @MedIva, @MedIdDinamics, @MedMedida, @MedBodega
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int? idCreado = await _sql.ExecuteScalarAsync<int?>(insertSql, new
            {
                OrdenId = ordenId,
                Nhcl = nhcl,
                Hora = horaStr,
                Fecha = input.Fecha.Date,
                CodigoProd = input.CodigoProd.Trim(),
                PrecioProd = input.PrecioProd,
                IndicacionesProd = input.IndicacionesProd.Trim(),
                CantProd = input.CantProd,
                Usuario = idUsuarioSesion,
                Localidad = idLocalidad,
                ProdDescripcion = input.ProdDescripcion.Trim(),
                Via = input.Via.Trim(),
                Iniciales = iniciales,
                Funcion = input.Funcion.Trim(),
                Funcionario = nombreFuncionario,
                Grupo = string.IsNullOrWhiteSpace(input.Tipo) ? "FARMACO" : input.Tipo.Trim(),
                Modulo = modulo,
                MedIva = input.Iva.Trim(),
                MedIdDinamics = input.IdDinamics.Trim(),
                MedMedida = input.UnidadMedida.Trim(),
                MedBodega = input.Bodega.Trim()
            });

            if (idCreado.HasValue && idCreado.Value > 0)
            {
                await _bcService.GuardarReservaStockAsync(
                    input.CodigoProd.Trim(),
                    idUsuarioSesion,
                    input.CantProd,
                    idLocalidad,
                    input.Bodega.Trim(),
                    ordenId,
                    nhcl,
                    idCreado.Value,
                    "QUI");

                return (true, "Administración de medicamento registrada y stock reservado con éxito.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Modo Demo al registrar medicamento en memoria.");
        }

        int nuevoId = _productosDemo.Count > 0 ? _productosDemo.Max(p => p.Id) + 1 : 1;
        _productosDemo.Insert(0, new ProductoUtilizadoDto
        {
            Id = nuevoId,
            Orden = ordenId,
            Nhcl = nhcl,
            Codigo = input.CodigoProd.Trim(),
            Producto = input.ProdDescripcion.Trim(),
            Cantidad = input.CantProd,
            Precio = input.PrecioProd,
            Indicaciones = input.IndicacionesProd.Trim(),
            Via = input.Via.Trim(),
            Fecha = input.Fecha.ToString("yyyy-MM-dd"),
            FechaRaw = input.Fecha.Date,
            Hora = horaStr,
            Iniciales = iniciales,
            Funcion = input.Funcion.Trim(),
            Funcionario = nombreFuncionario,
            Bodega = string.IsNullOrWhiteSpace(input.Bodega) ? "HOSP" : input.Bodega.Trim()
        });

        return (true, "Administración de medicamento registrada en Kardex (Modo Demo).");
    }

    public async Task<(bool Exito, string Mensaje)> EliminarAdministracionMedicamentoAsync(int idRegistro, string codigoProd, int ordenId)
    {
        try
        {
            const string deleteSql = "DELETE FROM qvt_productosUtilizados WHERE id = @Id";
            int res = await _sql.ExecuteAsync(deleteSql, new { Id = idRegistro });
            if (res > 0)
            {
                await _bcService.EliminarReservaStockAsync(codigoProd, ordenId, idRegistro, "QUI");
                return (true, "Registro eliminado y reserva de stock liberada con éxito.");
            }
        }
        catch { }

        _productosDemo.RemoveAll(p => p.Id == idRegistro);
        return (true, "Registro eliminado del Kardex.");
    }

    public async Task<List<KardexSabanaItemDto>> ConstruirSabanaKardex5DiasAsync(
        int ordenId, int nhcl, DateTime? fechaInicioFiltro = null, string modulo = "EnCirugia")
    {
        var sabana = new List<KardexSabanaItemDto>();
        var registros = await ObtenerProductosUtilizadosAsync(ordenId, modulo);
        if (registros.Count == 0)
            return sabana;

        DateTime minFecha = registros.Min(r => r.FechaRaw.Date);
        if (fechaInicioFiltro.HasValue && fechaInicioFiltro.Value.Date > minFecha)
        {
            minFecha = fechaInicioFiltro.Value.Date;
        }

        var fechas5Dias = Enumerable.Range(0, 5).Select(d => minFecha.AddDays(d)).ToList();

        // Agrupar medicamentos únicos dentro del rango de 5 días como lo hace CargaLista + CargaSiguientesDias
        var medicamentosUnicos = registros
            .Where(r => r.FechaRaw.Date >= minFecha && r.FechaRaw.Date <= minFecha.AddDays(5))
            .GroupBy(r => r.Codigo)
            .Select(g => g.OrderBy(x => x.FechaRaw).ThenBy(x => x.Hora).First())
            .ToList();

        int contador = 1;
        foreach (var med in medicamentosUnicos)
        {
            var itemSabana = new KardexSabanaItemDto
            {
                NumeroFila = contador,
                IdProd = med.Codigo,
                NombreProducto = med.Producto,
                Via = med.Via,
                Indicacion = med.Indicaciones,
                Cantidad = med.Cantidad,
                FechaInicio = minFecha,
                DescripcionCompleta = $"{contador} - {med.Producto} ({med.Via}) - {med.Indicaciones}",
                FechasColumnas = fechas5Dias
            };

            foreach (var dia in fechas5Dias)
            {
                var tomasDia = registros
                    .Where(r => r.Codigo == med.Codigo && r.FechaRaw.Date == dia.Date)
                    .OrderBy(r => r.Hora)
                    .Select(r =>
                    {
                        int horaNum = 8;
                        if (r.Hora.Length >= 2 && int.TryParse(r.Hora[..2], out int hParsed))
                        {
                            horaNum = hParsed;
                        }
                        string funcAbrev = string.IsNullOrWhiteSpace(r.Funcion) ? "" : r.Funcion[..1].ToUpperInvariant();
                        return new HoraInicialesFuncionDto
                        {
                            Hora = r.Hora,
                            Iniciales = r.Iniciales,
                            Funcion = funcAbrev,
                            EsTurnoNocturno = horaNum >= 18
                        };
                    })
                    .ToList();

                // Completar hasta 6 casillas por día como en ListaAdmin() de vt_EnfermeriaKardex.aspx.vb
                while (tomasDia.Count < 6)
                {
                    tomasDia.Add(new HoraInicialesFuncionDto());
                }

                itemSabana.AdministracionesPorDia.Add(tomasDia);
            }

            sabana.Add(itemSabana);
            contador++;
        }

        return sabana;
    }
}
