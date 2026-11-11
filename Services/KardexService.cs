using System.Collections.Concurrent;
using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IKardexService
{
    Task<List<OrdenActivaResumenDto>> ObtenerOrdenesActivasAsync(string? filtro = null, int top = 40);
    void InvalidarCacheOrdenesActivas();
    Task<PacienteHeaderDto?> ObtenerCabeceraPacienteAsync(int ordenId, int nhcl);
    Task<PacienteHeaderDto?> ObtenerEncabezadoPacienteAsync(int ordenId, int nhcl) => ObtenerCabeceraPacienteAsync(ordenId, nhcl);
    Task<List<ViaAdministracionDto>> ObtenerViasAdministracionAsync();
    Task<List<FuncionarioDto>> ObtenerFuncionariosEnfermeriaAsync();
    Task<List<FuncionarioDto>> ObtenerProfesionalesQuirurgicosAsync();
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
    private readonly IInventarioService _inventarioService;
    private readonly ILogger<KardexService> _logger;

    // Caché en memoria para acelerar la navegación entre pestañas sin repetir consultas pesadas a SQL Server
    private static List<OrdenActivaResumenDto>? _cacheOrdenesActivas;
    private static DateTime _cacheOrdenesExpira = DateTime.MinValue;
    private static List<ViaAdministracionDto>? _cacheVias;
    private static List<FuncionarioDto>? _cacheFuncionarios;
    private static List<FuncionEnfermeriaDto>? _cacheFunciones;
    private static readonly ConcurrentDictionary<string, (DateTime Expira, PacienteHeaderDto Dto)> _cacheCabeceras = new();

    public KardexService(ISqlDataService sql, IInventarioService inventarioService, ILogger<KardexService> logger)
    {
        _sql = sql;
        _inventarioService = inventarioService;
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
                    ISNULL(o.ordenAten_idMedico, 0) AS IdMedico,
                    CAST(ISNULL(o.ordenAten_triajeMedico,0) AS BIT) AS TriajeMedico,
                    CAST(ISNULL(o.ordenAten_esProcedimiento,0) AS BIT) AS EsProcedimiento,
                    o.ordenAten_idEnfermero AS IdEnfermero,
                    CASE WHEN ISNULL(o.ordenAten_esProcedimiento,0) = 1 THEN 'Procedimiento' ELSE RTRIM(ISNULL(esp.es_descripcion, '')) END AS Especialidad,
                    ISNULL(o.ordenAten_fecha, GETDATE()) AS FechaOrden,
                    RTRIM(ISNULL(o.ordenAten_estado, 'A')) AS Estado,
                    RTRIM(ISNULL(o.ordenAten_etapa, 'Consulta')) AS Etapa
                FROM vt_ordenAtencion o
                INNER JOIN vt_pacientes p ON o.ordenAten_nhcl = p.mas_nhcl
                INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
                LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                LEFT JOIN vt_especialidades esp ON o.ordenAten_esp_numero = esp.es_id
                WHERE 1=1 {whereClause}
                ORDER BY o.ordenAten_id DESC";

            var lista = (await _sql.QueryAsync<OrdenActivaResumenDto>(sql, new { Filtro = $"%{filtro?.Trim()}%" })).ToList();
            if (sinFiltro)
            {
                _cacheOrdenesActivas = lista;
                _cacheOrdenesExpira = DateTime.UtcNow.AddSeconds(90);
            }
            return lista;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando las órdenes activas en SQL Server.");
            throw;
        }
    }

    public void InvalidarCacheOrdenesActivas()
    {
        _cacheOrdenesActivas = null;
        _cacheOrdenesExpira = DateTime.MinValue;
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
                    ISNULL(p.mas_nhcl, ISNULL(o.ordenAten_nhcl, @Nhcl)) AS Nhcl,
                    ISNULL(o.ordenAten_id, @OrdenId) AS OrdenAtencionId,
                    RTRIM(ISNULL(NULLIF(p.mas_nombre, ''), ISNULL(NULLIF(o.ordenAten_nombre, ''), 'PACIENTE'))) AS PacienteNombre,
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
                    RTRIM(ISNULL(r.des_raza, 'Mestizo')) AS Raza,
                    CASE WHEN RTRIM(ISNULL(p.mas_origen, 'M')) = 'M' THEN 'Doméstico' ELSE 'Silvestre' END AS Origen,
                    p.mas_fechanace AS FechaNacimiento,
                    RTRIM(ISNULL(u.user_nombre, 'Médico de Turno')) AS MedicoResponsable,
                    RTRIM(ISNULL(u.user_cedula, '')) AS MedicoCedula,
                    RTRIM(ISNULL(esp_m.es_descripcion, '')) AS MedicoEspecialidad,
                    RTRIM(ISNULL(NULLIF(LTRIM(RTRIM(u.user_celular)), ''), ISNULL(u.user_convensional, ''))) AS MedicoTelefono,
                    RTRIM(ISNULL(u.user_email, '')) AS MedicoEmail,
                    RTRIM(ISNULL(CONVERT(nvarchar(500), u.user_observaciones), '')) AS MedicoObservaciones,
                    RTRIM(ISNULL(o.cierre, 'N')) AS Cierre,
                    CAST(CASE WHEN ISNULL(p.fallecido, 'False') = 'True' OR ISNULL(p.fallecido, '0') = '1' THEN 1 ELSE 0 END AS BIT) AS Fallecido,
                    p.fechafallecido AS FechaFallecido,
                    p.motivofallece AS MotivoFallece
                FROM vt_pacientes p
                LEFT JOIN vt_ordenAtencion o ON p.mas_nhcl = o.ordenAten_nhcl AND (@OrdenId <= 0 OR o.ordenAten_id = @OrdenId)
                LEFT JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo OR o.ordenAten_idcliente = c.pa_codigo
                LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
                LEFT JOIN vt_razas r ON p.mas_raza = r.id_raza
                LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
                LEFT JOIN vt_especialidades esp_m ON u.user_esp_numero = esp_m.es_id
                WHERE (@OrdenId > 0 AND o.ordenAten_id = @OrdenId)
                   OR (@OrdenId <= 0 AND @Nhcl > 0 AND p.mas_nhcl = @Nhcl)
                ORDER BY ISNULL(o.ordenAten_id, 0) DESC";

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
            _logger.LogError(ex, "Error en ObtenerCabeceraPacienteAsync Orden={OrdenId} NHCL={Nhcl}", ordenId, nhcl);
            throw;
        }
        return null;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudieron consultar las vías de administración.");
            throw;
        }

        return [];
    }

    public async Task<List<FuncionarioDto>> ObtenerProfesionalesQuirurgicosAsync()
    {
        const string sql = """
            SELECT user_numero AS UserNumero, RTRIM(user_nombre) AS UserNombre,
                   CAST(CASE WHEN user_tipouser_numero = 2 AND user_tipomedico = 'E' THEN 1 ELSE 0 END AS BIT) AS EsExterno
            FROM vt_usuarios
            WHERE user_tipouser_numero IN (2, 3) AND user_estado = 'A'
            ORDER BY user_nombre
            """;
        return (await _sql.QueryAsync<FuncionarioDto>(sql)).ToList();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo consultar el personal de enfermería.");
            throw;
        }

        return [];
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudieron consultar las funciones de enfermería.");
            throw;
        }

        return [];
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
                    RTRIM(CONVERT(VARCHAR(50), ISNULL(codigoprod, ''))) AS Codigo,
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

            return (await _sql.QueryAsync<ProductoUtilizadoDto>(sql, new { OrdenId = ordenId, Modulo = modulo })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ObtenerProductosUtilizadosAsync de la orden {OrdenId}", ordenId);
            throw;
        }
    }

    public async Task<(bool Exito, string Mensaje)> RegistrarAdministracionMedicamentoAsync(
        int ordenId, int nhcl, KardexRegistroInputModel input, int idUsuarioSesion, int idLocalidad, string modulo = "EnCirugia")
    {
        if (input.CantProd <= 0)
            return (false, "La cantidad debe ser mayor a 0.");

        if (input.StockDisponible > 0 && input.CantProd > input.StockDisponible)
            return (false, $"La cantidad no debe superar el stock disponible ({input.StockDisponible:N2} uds).");

        if (ordenId <= 0 || nhcl <= 0 || idUsuarioSesion <= 0 || input.FuncionarioId <= 0 ||
            string.IsNullOrWhiteSpace(input.CodigoProd) || string.IsNullOrWhiteSpace(input.ProdDescripcion) ||
            string.IsNullOrWhiteSpace(input.IndicacionesProd) || string.IsNullOrWhiteSpace(input.Via))
            return (false, "Faltan datos reales de la orden, el producto o el responsable.");

        if (string.IsNullOrWhiteSpace(input.CodigoProd) && string.IsNullOrWhiteSpace(input.IdDinamics))
            return (false, "Seleccione un producto valido del catalogo.");

        string horaStr = input.Hora.ToString("HH:mm");

        try
        {
            var func = await _sql.QueryFirstOrDefaultAsync<FuncionarioDto>(
                "SELECT user_numero AS UserNumero, RTRIM(user_nombre) AS UserNombre, RTRIM(ISNULL(user_iniciales, 'HV')) AS UserIniciales FROM vt_usuarios WHERE user_numero = @Id",
                new { Id = input.FuncionarioId });

            if (func is null)
                return (false, "El funcionario responsable no existe en la base de datos.");

            string iniciales = func.UserIniciales;
            string nombreFuncionario = func.UserNombre;

            const string insertSql = @"
                INSERT INTO qvt_productosUtilizados (
                    ordenId, nhcl, hora, fecha, codigoprod, precioprod, stockprod, indicacionesprod,
                    cantprod, usuario, localidad, proddescripcion, via, origen, iniciales, funcion,
                    funcionario, estado, grupo, modulo, tipo
                ) VALUES (
                    @OrdenId, @Nhcl, @Hora, @Fecha, @CodigoProd, @PrecioProd, 0, @IndicacionesProd,
                    @CantProd, @Usuario, @Localidad, @ProdDescripcion, @Via, 'E', @Iniciales, @Funcion,
                    @Funcionario, 'Vigente', 1, @Modulo, 'M'
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int? idCreado = await _sql.ExecuteScalarAsync<int?>(insertSql, new
            {
                OrdenId = ordenId,
                Nhcl = nhcl,
                Hora = horaStr,
                Fecha = input.Fecha.Date,
                CodigoProd = !string.IsNullOrWhiteSpace(input.CodigoProd) ? input.CodigoProd.Trim() : (input.IdDinamics ?? "0"),
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
                Modulo = modulo
            });

            if (idCreado.HasValue && idCreado.Value > 0)
            {
                await _inventarioService.GuardarReservaStockAsync(
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
            _logger.LogError(ex, "Error al registrar medicamento en Kardex para la orden {OrdenId}", ordenId);
        }

        return (false, "No se pudo guardar la administración del medicamento en la base de datos.");
    }

    public async Task<(bool Exito, string Mensaje)> EliminarAdministracionMedicamentoAsync(int idRegistro, string codigoProd, int ordenId)
    {
        try
        {
            const string deleteSql = "DELETE FROM qvt_productosUtilizados WHERE id = @Id";
            int res = await _sql.ExecuteAsync(deleteSql, new { Id = idRegistro });
            if (res > 0)
            {
                await _inventarioService.EliminarReservaStockAsync(codigoProd, ordenId, idRegistro, "QUI");
                return (true, "Registro eliminado y reserva de stock liberada con éxito.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando el registro de Kardex {IdRegistro}", idRegistro);
        }

        return (false, "No se pudo eliminar el registro del Kardex en la base de datos.");
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
