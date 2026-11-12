using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IAdmisionFacturacionService
{
    // 1. Clientes y Pacientes (vt_personales)
    Task<List<ClienteDto>> BuscarClientesAsync(string filtro);
    Task<ClienteDto?> ObtenerClientePorIdAsync(int idCliente);
    Task<int> GuardarClienteAsync(ClienteDto cliente, int idUsuario);
    Task<int> BuscarDuplicadoClienteAsync(string campo, string valor, int codigoExcluir);
    Task<List<PacienteMascotaDto>> ObtenerMascotasPorClienteAsync(int idCliente);
    Task<List<PacienteMascotaDto>> BuscarMascotasAsync(string filtro, string origen = "");
    Task<int> GuardarMascotaAsync(PacienteMascotaDto mascota, int idUsuario);
    Task<List<CatalogoItemGenericoDto>> ObtenerEspeciesAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerRazasPorEspecieAsync(int idEspecie);
    Task<List<CatalogoItemGenericoDto>> ObtenerColoresAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerSexosAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerClasesTueriAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerCategoriasIngresoTueriAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerFormasObtencionTueriAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerNombresCientificosAsync(int? idClase = null);
    Task<List<CatalogoItemGenericoDto>> ObtenerMotivosMuerteAsync();

    // 2. Recepción y Órdenes de Atención (vt_OrdenesMenu)
    Task<List<CatalogoItemGenericoDto>> ObtenerEspecialidadesAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerMedicosPorEspecialidadAsync(int idEspecialidad, int? idLocalidad = null);
    Task<List<CatalogoItemGenericoDto>> ObtenerEnfermerosAsync(int? idLocalidad = null);
    Task<List<ConteoMedicoEsperaDto>> ObtenerConteoMedicosEsperaAsync(DateTime fecha, int idLocalidad);
    Task<bool> TieneOrdenActivaHoyAsync(int nhcl, int idEspecialidad, DateTime fecha);
    Task<int> CrearOrdenAtencionAsync(NuevaOrdenAtencionDto orden, int idUsuario, string ipOrigen);
    Task<List<OrdenAtencionResumenDto>> ObtenerOrdenesDelDiaAsync(DateTime fecha, string filtro = "", string etapa = "");
    Task<List<OrdenPacienteResumenDto>> ObtenerOrdenesPacienteAsync(int nhcl);
    Task<bool> PuedeConsultarOrdenAsync(int idOrden, int nhcl);
    Task<List<ProcedimientoTriajeDto>> ObtenerProcedimientosTriajeAsync(int idOrden, int nhcl);
    Task<List<CatalogoOperativoDto>> ObtenerCatalogoProcedimientosTriajeAsync(int idOrden, int nhcl, int? idProducto = null);
    Task<bool> AgregarProcedimientoTriajeAsync(int idOrden, int nhcl, int idProducto, int cantidad, string observaciones, bool desdeHistorial, bool desdeSoap);
    Task<List<SelectorClinicoDto>> BuscarMascotasConOrdenesVisiblesAsync(string filtro);
    Task<List<OrdenAtencionResumenDto>> ObtenerColaClinicaAsync(bool triaje, int? idOrden = null);
    Task<bool> CambiarEstadoOrdenAsync(int idOrden, string nuevoEstado, string nuevaEtapa);
    Task<bool> CambiarAsignacionOrdenAsync(int idOrden, int idMedico, int idEspecialidad, int idUsuario);
    Task<bool> AnularOrdenAsync(int idOrden, int idUsuario);

    // 3. Agenda de Citas Médicas (vt_administracionCitasnew)
    Task<bool> PuedeVerAgendaCompletaAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerSucursalesAgendaAsync();
    Task<List<CatalogoItemGenericoDto>> ObtenerMedicosAgendaAsync(int? idLocalidad = null);
    Task<Dictionary<DateTime, List<SlotHorarioMedicoDto>>> ObtenerSlotsHorarioMedicoPorRangoAsync(int idMedico, DateTime desde, DateTime hasta, int idLocalidad);
    Task<List<SlotHorarioMedicoDto>> ObtenerSlotsHorarioMedicoAsync(int idMedico, DateTime fecha, int idLocalidad);
    Task<List<CitaMedicaDto>> ObtenerCitasPorFechaAsync(DateTime fechaDesde, DateTime fechaHasta, int idMedico = 0, int? idLocalidad = null);
    Task<int> AgendarCitaAsync(CitaMedicaDto cita, int idUsuario);
    Task<bool> ActualizarEstadoCitaAsync(int idCita, string nuevoEstado);
    Task<bool> ReagendarCitaAsync(int idCita, DateTime fecha, string hora, int idMedico, int idLocalidad, int idUsuario);
    Task<bool> CancelarCitaAsync(int idCita, string motivo, int idUsuario);
    Task<bool> BloquearHorarioMedicoAsync(int idMedico, DateTime fecha, string horaDesde, string horaHasta, int idLocalidad);

    // 4. Facturación consolidada / proformas
    Task<List<OrdenProformaResumenDto>> BuscarOrdenesParaProformaAsync(DateTime desde, DateTime hasta, string filtro = "");
    Task<OrdenProformaResumenDto?> ObtenerDetalleProformaOrdenAsync(int idOrden);
    Task<bool> AgregarCargoProformaManualAsync(ItemFacturableProformaDto cargo, int idUsuario);
    Task<(bool Exito, string NumeroProforma, string Mensaje)> EmitirProformaAsync(int idOrden, List<ItemFacturableProformaDto> itemsSeleccionados, int idUsuario);

    // 5. Usuarios y Bitácora (vt_usuarios, vt_vitacora)
    Task<List<UsuarioSistemaDto>> ObtenerUsuariosAsync(string filtro = "");
    Task<bool> GuardarUsuarioAsync(UsuarioSistemaDto usuario);
    Task<List<BitacoraAuditoriaDto>> ObtenerBitacoraAsync(DateTime desde, DateTime hasta, string filtro = "");
}

public class AdmisionFacturacionService : IAdmisionFacturacionService
{
    private readonly ISqlDataService _db;
    private readonly SessionStateService _session;
    private static bool EsRolSinRestriccionSucursal(int rol) => rol is 1 or 4 or 12;
    private const string PrefijoAsignacion = "Asignación de orden #";
    private const string SucursalesAsignadas = @"SELECT ul_idLocalidad
        FROM vt_usuarioLocalidades
        WHERE ul_idUsuario = @UsuarioSesion AND ISNULL(ul_estado, 'A') = 'A'";
    private const string AlcanceOrden = @"(@VeTodasSucursales = 1 OR (
        o.ordenAten_idlocalidad = @LocalidadSesion
        AND o.ordenAten_idlocalidad IN (" + SucursalesAsignadas + @")
        AND (@Rol <> 2 OR o.ordenAten_idMedico = @UsuarioSesion)))";
    private const string PermisoEditarCita = @" AND EXISTS (
        SELECT 1 FROM vt_usuarios actor
        WHERE actor.user_numero = @UsuarioSesion AND actor.user_estado = 'A'
          AND (@AgendaCompleta = 1 OR (vt_citas.ci_usuario = @UsuarioSesion
               AND (@MedicoDestino IS NULL OR @MedicoDestino = @UsuarioSesion))))
        AND (@EsAdmin = 1 OR ISNULL(vt_citas.ci_localidad, 1) IN (" + SucursalesAsignadas + "))";
    private readonly ILogger<AdmisionFacturacionService> _logger;
    private readonly Dictionary<string, HashSet<string>> _columnCache = new(StringComparer.OrdinalIgnoreCase);

    public AdmisionFacturacionService(ISqlDataService db, ILogger<AdmisionFacturacionService> logger, SessionStateService session)
    {
        _db = db;
        _logger = logger;
        _session = session;
    }

    private async Task<HashSet<string>> ObtenerColumnasTablaAsync(string tableName)
    {
        if (_columnCache.TryGetValue(tableName, out var cached)) return cached;

        var columns = (await _db.QueryAsync<string>(@"
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(@TableName)", new { TableName = tableName }))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _columnCache[tableName] = columns;
        return columns;
    }

    // =========================================================================
    // 1. CLIENTES Y PACIENTES (vt_personales)
    // =========================================================================

    public async Task<List<ClienteDto>> BuscarClientesAsync(string filtro)
    {
        var columns = await ObtenerColumnasTablaAsync("vt_clientes");
        string[] expectedOptionalColumns =
        [
            "pa_emailfactura", "pa_grupocliente", "pa_terminospago", "pa_tipopersona",
            "pa_genero", "pa_formapago", "pa_estadocivil"
        ];
        var missingColumns = expectedOptionalColumns.Where(x => !columns.Contains(x)).ToArray();
        if (missingColumns.Length > 0)
        {
            _logger.LogWarning("La tabla vt_clientes no contiene columnas opcionales de paridad: {MissingColumns}", string.Join(", ", missingColumns));
        }
        string Optional(string column, string expression, string fallback) => columns.Contains(column) ? expression : fallback;
        string sql = $@"
            SELECT TOP 100
                pa_codigo AS PaCodigo,
                RTRIM(ISNULL(pa_nombres, '')) AS PaNombres,
                RTRIM(ISNULL(pa_apellidos, '')) AS PaApellidos,
                RTRIM(ISNULL(pa_cedula, '')) AS PaCedula,
                RTRIM(ISNULL(pa_telefono, '')) AS PaTelefono,
                RTRIM(ISNULL(pa_movil, '')) AS PaMovil,
                RTRIM(ISNULL(pa_email, '')) AS PaEmail,
                {Optional("pa_emailfactura", "RTRIM(ISNULL(pa_emailfactura, ''))", "CAST('' AS VARCHAR(150))")} AS PaEmailFactura,
                RTRIM(ISNULL(pa_direccion, '')) AS PaDireccion,
                RTRIM(ISNULL(pa_observaciones, '')) AS PaObservaciones,
                ISNULL(pa_fechaingreso, GETDATE()) AS PaFechaIngreso,
                RTRIM(ISNULL(pa_estado, 'A')) AS PaEstado,
                RTRIM(ISNULL(pa_tipocli, '1')) AS PaTipoCli,
                CASE RTRIM(ISNULL(pa_documento, '05'))
                    WHEN '04' THEN 'RUC'
                    WHEN 'R' THEN 'RUC'
                    WHEN '06' THEN 'PASAPORTE'
                    WHEN 'P' THEN 'PASAPORTE'
                    ELSE 'CEDULA'
                END AS PaDocumento,
                RTRIM(ISNULL(pa_notas, '')) AS PaNotas,
                {Optional("pa_grupocliente", "RTRIM(ISNULL(pa_grupocliente, ''))", "CAST('' AS VARCHAR(50))")} AS PaGrupoCliente,
                {Optional("pa_terminospago", "RTRIM(ISNULL(pa_terminospago, ''))", "CAST('' AS VARCHAR(50))")} AS PaTerminosPago,
                {Optional("pa_tipopersona", "RTRIM(ISNULL(pa_tipopersona, ''))", "CAST('' AS VARCHAR(50))")} AS PaTipoPersona,
                {Optional("pa_genero", "CONVERT(VARCHAR(10), ISNULL(pa_genero, 1))", "CAST('1' AS VARCHAR(10))")} AS PaGenero,
                {Optional("pa_formapago", "RTRIM(ISNULL(pa_formapago, ''))", "CAST('' AS VARCHAR(50))")} AS PaFormaPago,
                {Optional("pa_estadocivil", "CONVERT(VARCHAR(10), ISNULL(pa_estadocivil, 1))", "CAST('1' AS VARCHAR(10))")} AS PaEstadoCivil
            FROM vt_clientes
            WHERE pa_estado = 'A'
              AND (@Filtro = '' OR pa_apellidos LIKE '%' + @Filtro + '%' OR pa_nombres LIKE '%' + @Filtro + '%' OR pa_cedula LIKE '%' + @Filtro + '%')
            ORDER BY pa_apellidos, pa_nombres;";

        try
        {
            var result = (await _db.QueryAsync<ClienteDto>(sql, new { Filtro = filtro?.Trim() ?? string.Empty })).ToList();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando clientes en SQL Server");
            throw;
        }
    }

    public async Task<ClienteDto?> ObtenerClientePorIdAsync(int idCliente)
    {
        var lista = await BuscarClientesAsync(string.Empty);
        return lista.FirstOrDefault(c => c.PaCodigo == idCliente);
    }

    public async Task<int> BuscarDuplicadoClienteAsync(string campo, string valor, int codigoExcluir)
    {
        valor = valor?.Trim() ?? string.Empty;
        if (valor.Length == 0) return 0;

        string sql = campo switch
        {
            "DOCUMENTO" => "SELECT TOP 1 pa_codigo FROM vt_clientes WHERE RTRIM(pa_cedula) = @Valor AND pa_codigo <> @Codigo;",
            "MOVIL" => "SELECT TOP 1 pa_codigo FROM vt_clientes WHERE LEFT(RTRIM(pa_movil), 10) = @Valor AND pa_codigo <> @Codigo;",
            "EMAIL" => "SELECT TOP 1 pa_codigo FROM vt_clientes WHERE LOWER(RTRIM(pa_email)) = LOWER(@Valor) AND pa_codigo <> @Codigo;",
            _ => string.Empty
        };
        if (sql.Length == 0) return 0;

        try
        {
            return await _db.ExecuteScalarAsync<int>(sql, new { Valor = valor, Codigo = codigoExcluir });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo verificar duplicado ({Campo})", campo);
            return 0;
        }
    }

    public async Task<int> GuardarClienteAsync(ClienteDto cliente, int idUsuario)
    {
        var valDoc = EcuadorDocumentoValidador.Validar(cliente.PaDocumento, cliente.PaCedula);
        if (valDoc.EsValido)
        {
            cliente.PaCedula = valDoc.ValorLimpio;
        }
        cliente.PaApellidos = NombreValidador.Sanitizar(cliente.PaApellidos);
        cliente.PaNombres = NombreValidador.Sanitizar(cliente.PaNombres);

        string documentoCodigo = cliente.PaDocumento?.Trim().ToUpperInvariant() switch
        {
            "RUC" or "R" or "04" => "04",
            "PASAPORTE" or "P" or "06" => "06",
            _ => "05"
        };

        try
        {
            const string dupSql = @"
                SELECT TOP 1 pa_codigo FROM vt_clientes
                WHERE RTRIM(pa_cedula) = @Cedula AND pa_codigo <> @Codigo;";
            int existente = await _db.ExecuteScalarAsync<int>(dupSql, new { Cedula = cliente.PaCedula?.Trim() ?? string.Empty, Codigo = cliente.PaCodigo });
            if (existente > 0)
                throw new DocumentoDuplicadoException($"El documento {cliente.PaCedula} ya está registrado en el propietario #{existente}.", "DOCUMENTO");

            string movilDup = cliente.PaMovil?.Trim() ?? string.Empty;
            if (movilDup.Length > 0)
            {
                int idMovil = await _db.ExecuteScalarAsync<int>(
                    "SELECT TOP 1 pa_codigo FROM vt_clientes WHERE LEFT(RTRIM(pa_movil), 10) = @Movil AND pa_codigo <> @Codigo;",
                    new { Movil = movilDup, Codigo = cliente.PaCodigo });
                if (idMovil > 0)
                    throw new DocumentoDuplicadoException($"El celular {movilDup} ya está registrado en el propietario #{idMovil}.", "MOVIL");
            }

            string emailDup = cliente.PaEmail?.Trim() ?? string.Empty;
            if (emailDup.Length > 0)
            {
                int idEmail = await _db.ExecuteScalarAsync<int>(
                    "SELECT TOP 1 pa_codigo FROM vt_clientes WHERE LOWER(RTRIM(pa_email)) = LOWER(@Email) AND pa_codigo <> @Codigo;",
                    new { Email = emailDup, Codigo = cliente.PaCodigo });
                if (idEmail > 0)
                    throw new DocumentoDuplicadoException($"El correo {emailDup} ya está registrado en el propietario #{idEmail}.", "EMAIL");
            }

            var columns = await ObtenerColumnasTablaAsync("vt_clientes");
            var optionalValues = new (string Column, string Parameter)[]
            {
                ("pa_emailfactura", "@PaEmailFactura"),
                ("pa_grupocliente", "@PaGrupoCliente"),
                ("pa_terminospago", "@PaTerminosPago"),
                ("pa_tipopersona", "@PaTipoPersona"),
                ("pa_genero", "@PaGenero"),
                ("pa_formapago", "@PaFormaPago"),
                ("pa_estadocivil", "@PaEstadoCivil")
            };

            if (cliente.PaCodigo == 0)
            {
                var insertColumns = new List<string>
                {
                    "pa_nombres", "pa_apellidos", "pa_cedula", "pa_telefono", "pa_movil",
                    "pa_email", "pa_direccion", "pa_observaciones", "pa_fechaingreso", "pa_fechaMod",
                    "pa_estado", "pa_usuarioModifico", "pa_tipocli", "pa_documento", "pa_notas"
                };
                var insertValues = new List<string>
                {
                    "@PaNombres", "@PaApellidos", "@PaCedula", "@PaTelefono", "@PaMovil",
                    "@PaEmail", "@PaDireccion", "@PaObservaciones", "GETDATE()", "GETDATE()",
                    "'A'", "@IdUsuario", "@PaTipoCli", "@DocumentoCodigo", "@PaNotas"
                };
                foreach (var optional in optionalValues.Where(x => columns.Contains(x.Column)))
                {
                    insertColumns.Add(optional.Column);
                    insertValues.Add(optional.Parameter);
                }
                string insertSql = $"INSERT INTO vt_clientes ({string.Join(",", insertColumns)}) VALUES ({string.Join(",", insertValues)}); SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int nuevoId = await _db.ExecuteScalarAsync<int>(insertSql, new
                {
                    cliente.PaNombres,
                    cliente.PaApellidos,
                    cliente.PaCedula,
                    cliente.PaTelefono,
                    cliente.PaMovil,
                    cliente.PaEmail,
                    cliente.PaDireccion,
                    cliente.PaObservaciones,
                    IdUsuario = idUsuario.ToString(),
                    PaTipoCli = string.IsNullOrWhiteSpace(cliente.PaTipoCli) ? "1" : (cliente.PaTipoCli.Length > 3 ? cliente.PaTipoCli[..3] : cliente.PaTipoCli),
                    DocumentoCodigo = documentoCodigo,
                    PaNotas = string.IsNullOrWhiteSpace(cliente.PaNotas) ? "1" : (cliente.PaNotas.Length > 1 ? cliente.PaNotas[..1] : cliente.PaNotas),
                    PaGrupoCliente = cliente.PaGrupoCliente?.Trim() ?? string.Empty,
                    PaTerminosPago = cliente.PaTerminosPago?.Trim() ?? string.Empty,
                    PaTipoPersona = cliente.PaTipoPersona?.Trim() ?? string.Empty,
                    PaGenero = int.TryParse(cliente.PaGenero, out int genero) ? genero : 1,
                    PaFormaPago = cliente.PaFormaPago?.Trim() ?? string.Empty,
                    PaEstadoCivil = int.TryParse(cliente.PaEstadoCivil, out int estadoCivil) ? estadoCivil : 1,
                    PaEmailFactura = cliente.PaEmailFactura?.Trim() ?? string.Empty
                });
                if (nuevoId > 0) return nuevoId;
            }
            else
            {
                var updates = new List<string>
                {
                    "pa_nombres=@PaNombres", "pa_apellidos=@PaApellidos", "pa_cedula=@PaCedula",
                    "pa_telefono=@PaTelefono", "pa_movil=@PaMovil", "pa_email=@PaEmail",
                    "pa_direccion=@PaDireccion", "pa_observaciones=@PaObservaciones", "pa_fechaMod=GETDATE()",
                    "pa_usuarioModifico=@IdUsuario", "pa_tipocli=@PaTipoCli", "pa_documento=@DocumentoCodigo",
                    "pa_notas=@PaNotas", "pa_estado=@PaEstado"
                };
                updates.AddRange(optionalValues.Where(x => columns.Contains(x.Column)).Select(x => $"{x.Column}={x.Parameter}"));
                string updateSql = $"UPDATE vt_clientes SET {string.Join(",", updates)} WHERE pa_codigo=@PaCodigo;";

                int rows = await _db.ExecuteAsync(updateSql, new
                {
                    cliente.PaCodigo,
                    cliente.PaNombres,
                    cliente.PaApellidos,
                    cliente.PaCedula,
                    cliente.PaTelefono,
                    cliente.PaMovil,
                    cliente.PaEmail,
                    PaEmailFactura = cliente.PaEmailFactura?.Trim() ?? string.Empty,
                    cliente.PaDireccion,
                    cliente.PaObservaciones,
                    IdUsuario = idUsuario.ToString(),
                    PaTipoCli = string.IsNullOrWhiteSpace(cliente.PaTipoCli) ? "1" : (cliente.PaTipoCli.Length > 3 ? cliente.PaTipoCli[..3] : cliente.PaTipoCli),
                    DocumentoCodigo = documentoCodigo,
                    PaNotas = string.IsNullOrWhiteSpace(cliente.PaNotas) ? "1" : (cliente.PaNotas.Length > 1 ? cliente.PaNotas[..1] : cliente.PaNotas),
                    PaGrupoCliente = cliente.PaGrupoCliente?.Trim() ?? string.Empty,
                    PaTerminosPago = cliente.PaTerminosPago?.Trim() ?? string.Empty,
                    PaTipoPersona = cliente.PaTipoPersona?.Trim() ?? string.Empty,
                    PaGenero = int.TryParse(cliente.PaGenero, out int genero) ? genero : 1,
                    PaFormaPago = cliente.PaFormaPago?.Trim() ?? string.Empty,
                    PaEstadoCivil = int.TryParse(cliente.PaEstadoCivil, out int estadoCivil) ? estadoCivil : 1,
                    PaEstado = string.Equals(cliente.PaEstado, "I", StringComparison.OrdinalIgnoreCase) ? "I" : "A"
                });
                if (rows > 0) return cliente.PaCodigo;
            }
        }
        catch (DocumentoDuplicadoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar cliente en SQL Server");
            throw;
        }

        throw new InvalidOperationException("SQL Server no confirmó el guardado del propietario.");
    }

    public async Task<List<PacienteMascotaDto>> ObtenerMascotasPorClienteAsync(int idCliente)
    {
        const string sql = @"
            SELECT
                p.mas_nhcl AS MasNhcl,
                ISNULL(p.mas_idcliente, 0) AS MasIdCliente,
                RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS ClienteNombre,
                RTRIM(ISNULL(c.pa_cedula, '')) AS ClienteCedula,
                RTRIM(ISNULL(c.pa_movil, '')) AS ClienteTelefono,
                RTRIM(ISNULL(p.mas_nombre, '')) AS MasNombre,
                RTRIM(ISNULL(p.mas_origen, 'M')) AS MasOrigen,
                CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 2 WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 1 ELSE 3 END AS MasSexo,
                RTRIM(ISNULL(s.sexo, CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 'Hembra' WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 'Macho' ELSE 'Indeterminado' END)) AS SexoDescripcion,
                CASE WHEN RTRIM(ISNULL(p.mas_castrado, 'N')) = 'S' THEN 'SI' ELSE 'NO' END AS MasCastrado,
                ISNULL(p.mas_especie, 1) AS MasEspecie,
                RTRIM(ISNULL(e.especie_descripcion, '')) AS EspecieDescripcion,
                ISNULL(p.mas_raza, 1) AS MasRaza,
                RTRIM(ISNULL(r.des_raza, '')) AS RazaDescripcion,
                RTRIM(ISNULL(p.mas_otros, '')) AS MasOtros,
                RTRIM(ISNULL(p.mas_microchip, '')) AS MasMicrochip,
                ISNULL(p.mas_fechanace, GETDATE()) AS MasFechaNace,
                RTRIM(ISNULL(p.mas_observacion, '')) AS MasObservacion,
                RTRIM(ISNULL(p.mas_estado, 'A')) AS MasEstado,
                RTRIM(ISNULL(p.mas_colortriaje, 'green')) AS MasColorTriaje,
                CAST(CASE WHEN CONVERT(VARCHAR(10), ISNULL(p.fallecido, 0)) IN ('1','True','TRUE') THEN 1 ELSE 0 END AS BIT) AS Fallecido,
                p.fechafallecido AS FechaFallecido,
                p.motivofallece AS MotivoFallece,
                p.tue_clase AS TueClase,
                RTRIM(ISNULL(p.tue_otraclase, '')) AS TueOtraClase,
                CAST(ISNULL(p.tue_peso, 0) AS VARCHAR(30)) AS TuePeso,
                CAST(ISNULL(p.tue_cientifico, 0) AS VARCHAR(50)) AS TueCientifico,
                p.tue_categoria AS TueCategoria,
                RTRIM(ISNULL(p.tue_otracategoria, '')) AS TueOtraCategoria,
                p.tue_formaobtension AS TueFormaObtencion,
                RTRIM(ISNULL(p.tue_otraobtension, '')) AS TueOtraObtencion,
                RTRIM(ISNULL(p.tue_region, '')) AS TueRegion,
                p.tue_provincia AS TueProvincia,
                p.tue_ciudad AS TueCiudad,
                RTRIM(ISNULL(p.tue_sectorbarrio, '')) AS TueSectorBarrio
            FROM vt_pacientes p
            LEFT JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            LEFT JOIN vt_sexo s ON RTRIM(p.mas_sexo) = RTRIM(s.id)
            LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
            LEFT JOIN vt_razas r ON p.mas_raza = r.id_raza
            WHERE p.mas_idcliente = @IdCliente AND p.mas_estado = 'A'
            ORDER BY p.mas_nhcl DESC;";

        try
        {
            var result = (await _db.QueryAsync<PacienteMascotaDto>(sql, new { IdCliente = idCliente })).ToList();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando pacientes del propietario en SQL Server");
            throw;
        }
    }

    public async Task<List<PacienteMascotaDto>> BuscarMascotasAsync(string filtro, string origen = "")
    {
        const string sql = @"
            SELECT TOP 100
                p.mas_nhcl AS MasNhcl,
                ISNULL(p.mas_idcliente, 0) AS MasIdCliente,
                RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS ClienteNombre,
                RTRIM(ISNULL(c.pa_cedula, '')) AS ClienteCedula,
                RTRIM(ISNULL(c.pa_movil, '')) AS ClienteTelefono,
                RTRIM(ISNULL(p.mas_nombre, '')) AS MasNombre,
                RTRIM(ISNULL(p.mas_origen, 'M')) AS MasOrigen,
                CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 2 WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 1 ELSE 3 END AS MasSexo,
                RTRIM(ISNULL(s.sexo, CASE WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'H' THEN 'Hembra' WHEN RTRIM(ISNULL(p.mas_sexo, '')) = 'M' THEN 'Macho' ELSE 'Indeterminado' END)) AS SexoDescripcion,
                CASE WHEN RTRIM(ISNULL(p.mas_castrado, 'N')) = 'S' THEN 'SI' ELSE 'NO' END AS MasCastrado,
                ISNULL(p.mas_especie, 1) AS MasEspecie,
                RTRIM(ISNULL(e.especie_descripcion, '')) AS EspecieDescripcion,
                ISNULL(p.mas_raza, 1) AS MasRaza,
                RTRIM(ISNULL(r.des_raza, '')) AS RazaDescripcion,
                RTRIM(ISNULL(p.mas_otros, '')) AS MasOtros,
                RTRIM(ISNULL(p.mas_microchip, '')) AS MasMicrochip,
                ISNULL(p.mas_fechanace, GETDATE()) AS MasFechaNace,
                RTRIM(ISNULL(p.mas_observacion, '')) AS MasObservacion,
                RTRIM(ISNULL(p.mas_estado, 'A')) AS MasEstado,
                RTRIM(ISNULL(p.mas_colortriaje, 'green')) AS MasColorTriaje,
                CAST(CASE WHEN CONVERT(VARCHAR(10), ISNULL(p.fallecido, 0)) IN ('1','True','TRUE') THEN 1 ELSE 0 END AS BIT) AS Fallecido,
                p.fechafallecido AS FechaFallecido,
                p.motivofallece AS MotivoFallece,
                p.tue_clase AS TueClase,
                RTRIM(ISNULL(p.tue_otraclase, '')) AS TueOtraClase,
                CAST(ISNULL(p.tue_peso, 0) AS VARCHAR(30)) AS TuePeso,
                CAST(ISNULL(p.tue_cientifico, 0) AS VARCHAR(50)) AS TueCientifico,
                p.tue_categoria AS TueCategoria,
                RTRIM(ISNULL(p.tue_otracategoria, '')) AS TueOtraCategoria,
                p.tue_formaobtension AS TueFormaObtencion,
                RTRIM(ISNULL(p.tue_otraobtension, '')) AS TueOtraObtencion,
                RTRIM(ISNULL(p.tue_region, '')) AS TueRegion,
                p.tue_provincia AS TueProvincia,
                p.tue_ciudad AS TueCiudad,
                RTRIM(ISNULL(p.tue_sectorbarrio, '')) AS TueSectorBarrio
            FROM vt_pacientes p
            INNER JOIN vt_clientes c ON p.mas_idcliente = c.pa_codigo
            LEFT JOIN vt_sexo s ON RTRIM(p.mas_sexo) = RTRIM(s.id)
            LEFT JOIN vt_especie e ON p.mas_especie = e.especie_id
            LEFT JOIN vt_razas r ON p.mas_raza = r.id_raza
            WHERE p.mas_estado = 'A'
              AND (@Origen = '' OR p.mas_origen = @Origen)
              AND (@Filtro = '' OR p.mas_nombre LIKE '%' + @Filtro + '%' OR c.pa_apellidos LIKE '%' + @Filtro + '%' OR c.pa_cedula LIKE '%' + @Filtro + '%' OR CAST(p.mas_nhcl AS VARCHAR) = @Filtro)
            ORDER BY p.mas_nhcl DESC;";

        try
        {
            var result = (await _db.QueryAsync<PacienteMascotaDto>(sql, new
            {
                Filtro = filtro?.Trim() ?? string.Empty,
                Origen = origen?.Trim() ?? string.Empty
            })).ToList();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando pacientes en SQL Server");
            throw;
        }
    }

    public async Task<int> GuardarMascotaAsync(PacienteMascotaDto mascota, int idUsuario)
    {
        try
        {
            if (mascota.Fallecido && mascota.MotivoFallece == -1)
            {
                string motivoOtro = mascota.MotivoFalleceOtro.Trim();
                if (string.IsNullOrWhiteSpace(motivoOtro))
                    throw new InvalidOperationException("Debe ingresar el motivo de muerte.");

                mascota.MotivoFallece = await _db.ExecuteScalarAsync<int>(@"
                    SET XACT_ABORT ON;
                    BEGIN TRANSACTION;

                    DECLARE @Sec INT;
                    SELECT TOP (1) @Sec=sec
                    FROM vt_motivomuerte WITH (TABLOCKX, HOLDLOCK)
                    WHERE UPPER(LTRIM(RTRIM(motivo)))=UPPER(@Motivo);

                    IF @Sec IS NULL
                    BEGIN
                        SELECT @Sec=ISNULL(MAX(sec), 0) + 1 FROM vt_motivomuerte;
                        INSERT INTO vt_motivomuerte (sec, motivo, estado)
                        VALUES (@Sec, @Motivo, 'A');
                    END
                    ELSE
                        UPDATE vt_motivomuerte SET estado='A' WHERE sec=@Sec;

                    COMMIT TRANSACTION;
                    SELECT @Sec;", new { Motivo = motivoOtro });
            }

            if (!string.IsNullOrWhiteSpace(mascota.MasNombre))
            {
                mascota.MasNombre = mascota.MasNombre.Trim().ToUpperInvariant();
            }
            if (mascota.MasFechaNace.Date > DateTime.Today)
            {
                mascota.MasFechaNace = DateTime.Today;
            }
            string sexoChar = mascota.MasSexo == 2 ? "H" : (mascota.MasSexo == 1 ? "M" : "I");
            string castradoChar = mascota.MasCastrado?.Trim().ToUpperInvariant().StartsWith("S") == true ? "S" : "N";
            decimal? tuePesoVal = decimal.TryParse(mascota.TuePeso, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : null;
            int? tueCientificoVal = int.TryParse(mascota.TueCientifico, out var c) ? c : null;

            if (mascota.MasNhcl == 0)
            {
                const string insertSql = @"
                    INSERT INTO vt_pacientes (
                        mas_idcliente, mas_nombre, mas_sexo, mas_castrado, mas_especie,
                        mas_raza, mas_otros, mas_fechanace, mas_observacion, mas_estado,
                        mas_fechaingreso, mas_ultimamodi, mas_usuariomodi, mas_origen, mas_colortriaje,
                        mas_microchip, fallecido, fechafallecido, motivofallece,
                        tue_clase, tue_otraclase, tue_peso, tue_cientifico, tue_categoria,
                        tue_otracategoria, tue_formaobtension, tue_otraobtension, tue_region,
                        tue_sectorbarrio, tue_provincia, tue_ciudad
                    )
                    VALUES (
                        @MasIdCliente, @MasNombre, @MasSexo, @MasCastrado, @MasEspecie,
                        @MasRaza, @MasOtros, @MasFechaNace, @MasObservacion, 'A',
                        GETDATE(), GETDATE(), @IdUsuario, @MasOrigen, @MasColorTriaje,
                        @MasMicrochip, @Fallecido, @FechaFallecido, @MotivoFallece,
                        @TueClase, @TueOtraClase, @TuePeso, @TueCientifico, @TueCategoria,
                        @TueOtraCategoria, @TueFormaObtencion, @TueOtraObtencion, @TueRegion,
                        @TueSectorBarrio, @TueProvincia, @TueCiudad
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int nuevoNhcl = await _db.ExecuteScalarAsync<int>(insertSql, new
                {
                    mascota.MasIdCliente,
                    mascota.MasNombre,
                    MasSexo = sexoChar,
                    MasCastrado = castradoChar,
                    mascota.MasEspecie,
                    mascota.MasRaza,
                    mascota.MasOtros,
                    mascota.MasMicrochip,
                    mascota.MasFechaNace,
                    mascota.MasObservacion,
                    IdUsuario = idUsuario.ToString(),
                    mascota.MasOrigen,
                    mascota.MasColorTriaje,
                    mascota.Fallecido,
                    FechaFallecido = mascota.Fallecido ? mascota.FechaFallecido : null,
                    MotivoFallece = mascota.Fallecido ? mascota.MotivoFallece : null,
                    mascota.TueClase,
                    mascota.TueOtraClase,
                    TuePeso = tuePesoVal,
                    TueCientifico = tueCientificoVal,
                    mascota.TueCategoria,
                    mascota.TueOtraCategoria,
                    mascota.TueFormaObtencion,
                    mascota.TueOtraObtencion,
                    mascota.TueRegion,
                    mascota.TueSectorBarrio,
                    mascota.TueProvincia,
                    mascota.TueCiudad
                });
                if (nuevoNhcl > 0) return nuevoNhcl;
            }
            else
            {
                const string updateSql = @"
                    UPDATE vt_pacientes SET
                        mas_nombre = @MasNombre,
                        mas_sexo = @MasSexo,
                        mas_castrado = @MasCastrado,
                        mas_especie = @MasEspecie,
                        mas_raza = @MasRaza,
                        mas_otros = @MasOtros,
                        mas_fechanace = @MasFechaNace,
                        mas_observacion = @MasObservacion,
                        mas_ultimamodi = GETDATE(),
                        mas_usuariomodi = @IdUsuario,
                        mas_origen = @MasOrigen,
                        mas_colortriaje = @MasColorTriaje,
                        mas_microchip = @MasMicrochip,
                        fallecido = @Fallecido,
                        fechafallecido = @FechaFallecido,
                        motivofallece = @MotivoFallece,
                        tue_clase = @TueClase,
                        tue_otraclase = @TueOtraClase,
                        tue_peso = @TuePeso,
                        tue_cientifico = @TueCientifico,
                        tue_categoria = @TueCategoria,
                        tue_otracategoria = @TueOtraCategoria,
                        tue_formaobtension = @TueFormaObtencion,
                        tue_otraobtension = @TueOtraObtencion,
                        tue_region = @TueRegion,
                        tue_sectorbarrio = @TueSectorBarrio,
                        tue_provincia = @TueProvincia,
                        tue_ciudad = @TueCiudad
                    WHERE mas_nhcl = @MasNhcl;";

                int rows = await _db.ExecuteAsync(updateSql, new
                {
                    mascota.MasNhcl,
                    mascota.MasNombre,
                    MasSexo = sexoChar,
                    MasCastrado = castradoChar,
                    mascota.MasEspecie,
                    mascota.MasRaza,
                    mascota.MasOtros,
                    mascota.MasMicrochip,
                    mascota.MasFechaNace,
                    mascota.MasObservacion,
                    IdUsuario = idUsuario.ToString(),
                    mascota.MasOrigen,
                    mascota.MasColorTriaje,
                    mascota.Fallecido,
                    FechaFallecido = mascota.Fallecido ? mascota.FechaFallecido : null,
                    MotivoFallece = mascota.Fallecido ? mascota.MotivoFallece : null,
                    mascota.TueClase,
                    mascota.TueOtraClase,
                    TuePeso = tuePesoVal,
                    TueCientifico = tueCientificoVal,
                    mascota.TueCategoria,
                    mascota.TueOtraCategoria,
                    mascota.TueFormaObtencion,
                    mascota.TueOtraObtencion,
                    mascota.TueRegion,
                    mascota.TueSectorBarrio,
                    mascota.TueProvincia,
                    mascota.TueCiudad
                });
                if (rows > 0) return mascota.MasNhcl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar paciente/mascota en SQL Server");
            throw;
        }

        throw new InvalidOperationException("SQL Server no confirmó el guardado del paciente.");
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerEspeciesAsync()
    {
        try
        {
            return (await _db.QueryAsync<CatalogoItemGenericoDto>(
                "SELECT especie_id AS Id, especie_descripcion AS Descripcion FROM vt_especie WHERE especie_estado = 'A' ORDER BY especie_descripcion")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando especies en SQL Server");
            throw;
        }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerRazasPorEspecieAsync(int idEspecie)
    {
        try
        {
            return (await _db.QueryAsync<CatalogoItemGenericoDto>(
                "SELECT id_raza AS Id, des_raza AS Descripcion, id_especie AS IdPadre FROM vt_razas WHERE id_especie = @IdEspecie AND estado = 'A' ORDER BY des_raza",
                new { IdEspecie = idEspecie })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando razas en SQL Server");
            throw;
        }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerColoresAsync()
    {
        try
        {
            return (await _db.QueryAsync<CatalogoItemGenericoDto>(
                "SELECT id AS Id, color AS Descripcion FROM vt_color ORDER BY color")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando colores en SQL Server");
            throw;
        }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerSexosAsync()
    {
        try
        {
            return (await _db.QueryAsync<CatalogoItemGenericoDto>(
                "SELECT CASE WHEN RTRIM(id) = 'M' THEN 1 WHEN RTRIM(id) = 'H' THEN 2 ELSE 3 END AS Id, RTRIM(sexo) AS Descripcion FROM vt_sexo ORDER BY id DESC")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando sexos en SQL Server");
            throw;
        }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerClasesTueriAsync()
    {
        try
        {
            return (await _db.QueryAsync<CatalogoItemGenericoDto>(
                "SELECT id_clas AS Id, des_clas AS Descripcion FROM vt_clase ORDER BY des_clas")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando clases TUERI en SQL Server");
            throw;
        }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerCategoriasIngresoTueriAsync() =>
        (await _db.QueryAsync<CatalogoItemGenericoDto>(
            "SELECT id AS Id, RTRIM(ingreso) AS Descripcion FROM vt_ingreso WHERE estado='A' ORDER BY ingreso")).ToList();

    public async Task<List<CatalogoItemGenericoDto>> ObtenerFormasObtencionTueriAsync() =>
        (await _db.QueryAsync<CatalogoItemGenericoDto>(
            "SELECT id AS Id, RTRIM(obtension) AS Descripcion FROM vt_obtension WHERE estado='A' ORDER BY obtension")).ToList();

    public async Task<List<CatalogoItemGenericoDto>> ObtenerNombresCientificosAsync(int? idClase = null) =>
        (await _db.QueryAsync<CatalogoItemGenericoDto>(@"
            SELECT id AS Id, RTRIM(nombre_cientifico) +
                CASE WHEN NULLIF(RTRIM(ISNULL(nombre_comun,'')), '') IS NULL THEN '' ELSE ' / ' + RTRIM(nombre_comun) END AS Descripcion
            FROM vt_nombres_cientificos
            WHERE estado_cientifico='A' AND (@IdClase IS NULL OR clas_id=@IdClase)
            ORDER BY nombre_cientifico", new { IdClase = idClase })).ToList();

    public async Task<List<CatalogoItemGenericoDto>> ObtenerMotivosMuerteAsync()
    {
        const string sql = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @Motivos TABLE (motivo VARCHAR(200));
            INSERT INTO @Motivos (motivo) VALUES
                ('Accidente'),
                ('Causa desconocida'),
                ('Complicaciones posoperatorias'),
                ('Enfermedad crónica'),
                ('Enfermedad infecciosa'),
                ('Enfermedad terminal'),
                ('Envenenamiento o intoxicación'),
                ('Eutanasia humanitaria'),
                ('Falla multiorgánica'),
                ('Muerte natural por edad avanzada'),
                ('Paro cardiorrespiratorio'),
                ('Traumatismo grave');

            UPDATE existente
            SET estado='A'
            FROM vt_motivomuerte existente
            INNER JOIN @Motivos requerido
                ON UPPER(LTRIM(RTRIM(existente.motivo)))=UPPER(requerido.motivo)
            WHERE ISNULL(existente.estado, 'I')<>'A';

            DECLARE @MaxSec INT;
            SELECT @MaxSec=ISNULL(MAX(sec), 0)
            FROM vt_motivomuerte WITH (TABLOCKX, HOLDLOCK);

            ;WITH Faltantes AS
            (
                SELECT requerido.motivo
                FROM @Motivos requerido
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM vt_motivomuerte existente
                    WHERE UPPER(LTRIM(RTRIM(existente.motivo)))=UPPER(requerido.motivo))
            )
            INSERT INTO vt_motivomuerte (sec, motivo, estado)
            SELECT @MaxSec + ROW_NUMBER() OVER (ORDER BY motivo), motivo, 'A'
            FROM Faltantes;

            COMMIT TRANSACTION;

            SELECT sec AS Id, RTRIM(motivo) AS Descripcion
            FROM vt_motivomuerte
            WHERE estado='A'
            ORDER BY motivo;";

        return (await _db.QueryAsync<CatalogoItemGenericoDto>(sql)).ToList();
    }

    // =========================================================================
    // 2. RECEPCIÓN Y ÓRDENES DE ATENCIÓN (vt_OrdenesMenu)
    // =========================================================================

    public async Task<List<CatalogoItemGenericoDto>> ObtenerEspecialidadesAsync()
    {
        try
        {
            return (await _db.QueryAsync<CatalogoItemGenericoDto>(@"
                SELECT 
                    es_id AS Id, 
                    RTRIM(ISNULL(es_codigo, '')) AS Codigo,
                    RTRIM(es_descripcion) AS Descripcion,
                    CASE WHEN UPPER(RTRIM(ISNULL(es_servicio, ''))) = 'S' THEN 1 ELSE 0 END AS RequiereTriaje
                FROM vt_especialidades 
                WHERE ISNULL(es_estado, 'A') = 'A' 
                ORDER BY es_descripcion")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando especialidades en SQL Server");
            throw;
        }
    }

    private int ResolverLocalidadActiva(int? idLocalidad = null) =>
        idLocalidad.GetValueOrDefault() > 0
            ? idLocalidad!.Value
            : (_session.Localidad > 0 ? _session.Localidad : 1);

    private async Task<bool> MedicoAtiendeEnLocalidadAsync(int idMedico, int idLocalidad)
    {
        if (idMedico <= 0 || idLocalidad <= 0) return false;

        return await _db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1)
            FROM vt_usuarios u
            WHERE u.user_numero = @IdMedico
              AND u.user_estado = 'A'
              AND (
                  EXISTS (
                      SELECT 1 FROM vt_horariosMedicos h
                      WHERE h.hr_iduser = u.user_numero
                        AND h.hr_localidad = @IdLocalidad
                  )
                  OR (
                      NOT EXISTS (
                          SELECT 1 FROM vt_horariosMedicos programado
                          WHERE programado.hr_iduser = u.user_numero
                            AND programado.hr_localidad > 0
                      )
                      AND EXISTS (
                          SELECT 1 FROM vt_usuarioLocalidades ul
                          WHERE ul.ul_idUsuario = u.user_numero
                            AND ul.ul_idLocalidad = @IdLocalidad
                            AND ISNULL(ul.ul_estado, 'A') = 'A'
                      )
                  )
              )", new { IdMedico = idMedico, IdLocalidad = idLocalidad }) > 0;
    }

    private async Task<bool> EnfermeroAtiendeEnLocalidadAsync(int idEnfermero, int idLocalidad)
    {
        if (idEnfermero <= 0 || idLocalidad <= 0) return false;

        return await _db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1)
            FROM vt_usuarios u
            WHERE u.user_numero = @IdEnfermero
              AND u.user_estado = 'A'
              AND EXISTS (
                  SELECT 1 FROM vt_usuarioLocalidades ul
                  WHERE ul.ul_idUsuario = u.user_numero
                    AND ul.ul_idLocalidad = @IdLocalidad
                    AND ISNULL(ul.ul_estado, 'A') = 'A'
              )", new { IdEnfermero = idEnfermero, IdLocalidad = idLocalidad }) > 0;
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerMedicosPorEspecialidadAsync(int idEspecialidad, int? idLocalidad = null)
    {
        int localidadObjetivo = ResolverLocalidadActiva(idLocalidad);
        try
        {
            return (await _db.QueryAsync<CatalogoItemGenericoDto>(@"
                SELECT DISTINCT u.user_numero AS Id, u.user_nombre AS Descripcion, ISNULL(u.user_esp_numero, 0) AS IdPadre
                FROM vt_usuarios u
                LEFT JOIN vt_usuarioEspecialidades ue ON ue.ue_idUsuario = u.user_numero AND ISNULL(ue.ue_estado, 'A') = 'A'
                WHERE u.user_estado = 'A'
                  AND u.user_tipouser_numero = 2
                  AND UPPER(LTRIM(RTRIM(ISNULL(u.user_tipomedico, '')))) IN ('P', 'L', 'E')
                  AND (
                      @IdEsp = 0 
                      OR ue.ue_idEspecialidad = @IdEsp 
                      OR (ue.ue_idEspecialidad IS NULL AND u.user_esp_numero = @IdEsp)
                  )
                  AND (
                      EXISTS (
                          SELECT 1 FROM vt_horariosMedicos h
                          WHERE h.hr_iduser = u.user_numero
                            AND h.hr_localidad = @IdLocalidad
                      )
                      OR (
                          NOT EXISTS (
                              SELECT 1 FROM vt_horariosMedicos programado
                              WHERE programado.hr_iduser = u.user_numero
                                AND programado.hr_localidad > 0
                          )
                          AND EXISTS (
                              SELECT 1 FROM vt_usuarioLocalidades ul
                              WHERE ul.ul_idUsuario = u.user_numero
                                AND ul.ul_idLocalidad = @IdLocalidad
                                AND ISNULL(ul.ul_estado, 'A') = 'A'
                          )
                      )
                  )
                ORDER BY u.user_nombre", new { IdEsp = idEspecialidad, IdLocalidad = localidadObjetivo })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando médicos en SQL Server");
            throw;
        }
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerEnfermerosAsync(int? idLocalidad = null)
    {
        int localidadObjetivo = ResolverLocalidadActiva(idLocalidad);
        return (await _db.QueryAsync<CatalogoItemGenericoDto>(@"
            SELECT u.user_numero AS Id, RTRIM(u.user_nombre) AS Descripcion
            FROM vt_usuarios u
            INNER JOIN vt_tipos_usuarios t ON t.tipuser_numero = u.user_tipouser_numero
            WHERE u.user_estado = 'A'
              AND UPPER(t.tipuser_descripcion) LIKE '%ENFERMER%'
              AND EXISTS (
                  SELECT 1 FROM vt_usuarioLocalidades ul
                  WHERE ul.ul_idUsuario = u.user_numero
                    AND ul.ul_idLocalidad = @IdLocalidad
                    AND ISNULL(ul.ul_estado, 'A') = 'A'
              )
            ORDER BY u.user_nombre", new { IdLocalidad = localidadObjetivo })).ToList();
    }

    public async Task<List<ConteoMedicoEsperaDto>> ObtenerConteoMedicosEsperaAsync(DateTime fecha, int idLocalidad)
    {
        const string sql = @"
            SELECT
                u.user_numero AS IdMedico,
                u.user_nombre AS Medico,
                ISNULL(e.es_descripcion, 'General') AS Especialidad,
                COUNT(o.ordenAten_id) AS EnEspera
            FROM vt_ordenAtencion o
            INNER JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
            LEFT JOIN vt_especialidades e ON o.ordenAten_esp_numero = e.es_id
            WHERE o.ordenAten_fecha = @Fecha
              AND o.ordenAten_estado = 'A'
              AND (@IdLocalidad = 0 OR o.ordenAten_idlocalidad = @IdLocalidad)
            GROUP BY u.user_numero, u.user_nombre, e.es_descripcion
            ORDER BY EnEspera DESC;";

        try
        {
            return (await _db.QueryAsync<ConteoMedicoEsperaDto>(sql, new { Fecha = fecha.Date, IdLocalidad = idLocalidad })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando la espera por médico en SQL Server");
            throw;
        }
    }

    public async Task<bool> TieneOrdenActivaHoyAsync(int nhcl, int idEspecialidad, DateTime fecha)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM vt_ordenAtencion
            WHERE ordenAten_nhcl = @Nhcl
              AND ordenAten_esp_numero = @IdEsp
              AND ordenAten_fecha = @Fecha
              AND ordenAten_estado IN ('A', 'T');";

        try
        {
            int count = await _db.ExecuteScalarAsync<int>(sql, new { Nhcl = nhcl, IdEsp = idEspecialidad, Fecha = fecha.Date });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando órdenes activas en SQL Server");
            throw;
        }
    }

    public async Task<int> CrearOrdenAtencionAsync(NuevaOrdenAtencionDto orden, int idUsuario, string ipOrigen)
    {
        if (_session.Localidad > 0)
        {
            orden.IdLocalidad = _session.Localidad;
        }
        if (orden.IdLocalidad <= 0) throw new InvalidOperationException("La localidad de la orden es obligatoria.");
        if (orden.TipoDespacho is < 0 or > 4) throw new InvalidOperationException("El tipo de despacho seleccionado no es válido.");
        if (orden.EsProcedimiento)
        {
            if (!orden.IdEnfermero.HasValue || orden.IdEnfermero.Value <= 0)
                throw new InvalidOperationException("El procedimiento requiere un enfermero responsable.");
            if (!await EnfermeroAtiendeEnLocalidadAsync(orden.IdEnfermero.Value, orden.IdLocalidad))
                throw new InvalidOperationException("El enfermero seleccionado no pertenece a la sucursal conectada.");

            orden.IdMedico = 0;
            orden.TriajeMedico = false;
            orden.IdEspecialidad = 0;
            orden.TipoDespacho = 0;
        }
        else if (orden.IdMedico <= 0)
        {
            throw new InvalidOperationException("La consulta requiere un médico tratante.");
        }
        else if (!await MedicoAtiendeEnLocalidadAsync(orden.IdMedico, orden.IdLocalidad))
        {
            throw new InvalidOperationException("El médico seleccionado no atiende en la sucursal conectada.");
        }


        string etapaInicial = orden.EsProcedimiento ? "Consulta" : orden.TipoDespacho switch
        {
            1 => "Imagenología",
            2 => "Laboratorio",
            3 => "Hospitalización",
            4 => "Quirófano",
            _ => "Consulta"
        };

        try
        {
            const string insertSql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                IF @ColorTriaje IS NOT NULL AND RTRIM(@ColorTriaje) <> ''
                    UPDATE vt_pacientes SET mas_colortriaje = @ColorTriaje WHERE mas_nhcl = @Nhcl;

                DECLARE @Prefijo VARCHAR(10);
                DECLARE @TurnoCodigo VARCHAR(20);
                DECLARE @SiguienteNum INT;

                IF @EsProcedimiento = 1
                BEGIN
                    SET @Prefijo = 'PROC';
                    SELECT @SiguienteNum = COUNT(1) + 1
                    FROM vt_ordenAtencion
                    WHERE CAST(ordenAten_fecha AS DATE) = CAST(@FechaOrden AS DATE)
                      AND ISNULL(ordenAten_esProcedimiento, 0) = 1;

                    SET @TurnoCodigo = @Prefijo + '-' + RIGHT('00' + CAST(@SiguienteNum AS VARCHAR(10)), CASE WHEN @SiguienteNum < 100 THEN 2 ELSE LEN(CAST(@SiguienteNum AS VARCHAR(10))) END);
                END
                ELSE
                BEGIN
                    SELECT @Prefijo = ISNULL(NULLIF(RTRIM(es_codigo), ''), LEFT(UPPER(LTRIM(RTRIM(ISNULL(es_descripcion, 'GEN')))), 3))
                    FROM vt_especialidades WHERE es_id = @IdEspecialidad;
                    IF @Prefijo IS NULL OR @Prefijo = '' SET @Prefijo = 'GEN';

                    SELECT @SiguienteNum = COUNT(1) + 1
                    FROM vt_ordenAtencion
                    WHERE CAST(ordenAten_fecha AS DATE) = CAST(@FechaOrden AS DATE)
                      AND ordenAten_esp_numero = @IdEspecialidad;

                    SET @TurnoCodigo = @Prefijo + '-' + RIGHT('00' + CAST(@SiguienteNum AS VARCHAR(10)), CASE WHEN @SiguienteNum < 100 THEN 2 ELSE LEN(CAST(@SiguienteNum AS VARCHAR(10))) END);
                END

                DECLARE @Consultorio NVARCHAR(100);
                IF @EsProcedimiento = 0
                BEGIN
                    SELECT TOP 1 @Consultorio = CONCAT('Piso ', NULLIF(RTRIM(hr_piso), ''), ' · ', RTRIM(hr_consultorio))
                    FROM vt_horariosMedicos
                    WHERE hr_iduser=@IdMedico AND hr_localidad=@IdLocalidad
                      AND UPPER(RTRIM(hr_dia2)) IN (UPPER(@DiaIngles), UPPER(@DiaEspanol))
                      AND CAST(hr_desde AS time)<=CAST(@HoraOrden AS time) AND CAST(hr_hasta AS time)>CAST(@HoraOrden AS time)
                      AND (hr_vigencia_desde IS NULL OR hr_vigencia_desde<=@FechaOrden)
                      AND (hr_vigencia_hasta IS NULL OR hr_vigencia_hasta>=@FechaOrden);
                END

                INSERT INTO vt_ordenAtencion (
                    ordenAten_nombre, ordenAten_idcliente, ordenAten_nhcl, ordenAten_idMedico,
                    ordenAten_estado, ordenAten_fecha, ordenAten_esp_numero, ordenAten_idlocalidad,
                    ordenAten_hora, ordenAten_IdUsuario, ordenaten_IPOrigen, ordenAten_etapa,
                    ordenAten_Tipo, ordenAten_Despacho, ordenAten_turno, ordenAten_consultorio,
                    ordenAten_esProcedimiento, ordenAten_idEnfermero, ordenAten_triajeMedico,
                    ordenAten_tipoConsulta
                )
                VALUES (
                    @NombrePropietario, @IdCliente, @Nhcl, @IdMedico,
                    'A', @FechaOrden, @IdEspecialidad, @IdLocalidad,
                    @HoraOrden, @IdUsuario, @IpOrigen, @Etapa,
                    @TipoOrigen, @TipoDespacho, @TurnoCodigo, @Consultorio,
                    @EsProcedimiento, @IdEnfermero, @TriajeMedico,
                    @TipoConsulta
                );
                DECLARE @NuevaOrden INT = CAST(SCOPE_IDENTITY() AS INT);
                INSERT INTO vt_vitacora (vit_detalle, vit_idUser, vit_idFechaMod, ip, hostname)
                VALUES (CONCAT(@PrefijoAsignacion, @NuevaOrden), @IdUsuario, DATEADD(hour, -5, SYSUTCDATETIME()), @IpOrigen, 'BlazorClient');
                COMMIT TRANSACTION;
                SELECT @NuevaOrden;";

            int idNueva = await _db.ExecuteScalarAsync<int>(insertSql, new
            {
                PrefijoAsignacion,
                orden.ColorTriaje,
                orden.Nhcl,
                orden.NombrePropietario,
                orden.IdCliente,
                orden.IdMedico,
                FechaOrden = orden.FechaOrden.Date,
                orden.IdEspecialidad,
                orden.IdLocalidad,
                orden.HoraOrden,
                DiaIngles = orden.FechaOrden.DayOfWeek.ToString(),
                DiaEspanol = DiaSemanaEspanol(orden.FechaOrden.DayOfWeek),
                IdUsuario = idUsuario,
                IpOrigen = ipOrigen,
                Etapa = etapaInicial,
                orden.TipoOrigen,
                orden.TipoDespacho,
                orden.EsProcedimiento,
                orden.IdEnfermero,
                orden.TriajeMedico,
                orden.TipoConsulta
            });

            if (idNueva > 0) return idNueva;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando orden de atención en SQL Server");
            throw;
        }
        throw new InvalidOperationException("SQL Server no confirmó la creación de la orden de atención.");
    }

    public async Task<List<OrdenAtencionResumenDto>> ObtenerOrdenesDelDiaAsync(DateTime fecha, string filtro = "", string etapa = "")
    {
        if (!_session.IsAuthenticated) return [];

        string sql = $@"
            SELECT TOP 150
                o.ordenAten_id AS IdOrden,
                ISNULL(NULLIF(RTRIM(o.ordenAten_turno), ''), ISNULL(NULLIF(RTRIM(es.es_codigo), ''), 'GEN') + '-01') AS Turno,
                ISNULL(RTRIM(o.ordenAten_consultorio), '') AS Consultorio,
                o.ordenAten_nhcl AS Nhcl,
                o.ordenAten_idcliente AS IdCliente,
                ISNULL(o.ordenAten_nombre, '') AS Propietario,
                ISNULL(c.pa_cedula, '') AS Cedula,
                ISNULL(p.mas_nombre, '') AS Mascota,
                ISNULL(esp.especie_descripcion, '') AS Especie,
                ISNULL(o.ordenAten_esp_numero, 0) AS IdEspecialidad,
                CASE WHEN ISNULL(o.ordenAten_esProcedimiento,0) = 1 THEN 'PROC' ELSE ISNULL(NULLIF(RTRIM(es.es_codigo), ''), 'GEN') END AS CodigoEspecialidad,
                CASE WHEN ISNULL(o.ordenAten_esProcedimiento,0) = 1 THEN 'Procedimiento' ELSE ISNULL(es.es_descripcion, '') END AS Especialidad,
                o.ordenAten_idMedico AS IdMedico,
                ISNULL(u.user_nombre, '') AS Medico,
                CAST(ISNULL(o.ordenAten_triajeMedico, CASE WHEN o.ordenAten_esp_numero > 0 THEN 1 ELSE 0 END) AS BIT) AS TriajeMedico,
                CAST(ISNULL(o.ordenAten_esProcedimiento,0) AS BIT) AS EsProcedimiento,
                o.ordenAten_idEnfermero AS IdEnfermero,
                ISNULL(enf.user_nombre,'') AS Enfermero,
                o.ordenAten_fecha AS Fecha,
                ISNULL(o.ordenAten_hora, '') AS Hora,
                ISNULL(o.ordenAten_estado, 'A') AS Estado,
                ISNULL(o.ordenAten_etapa, 'Consulta') AS Etapa,
                ISNULL(o.ordenAten_Tipo, 'M') AS TipoOrigen,
                ISNULL(o.ordenAten_Despacho, 0) AS TipoDespacho,
                ISNULL(p.mas_colortriaje, 'green') AS ColorTriaje,
                'N' AS Facturado,
                '' AS NumFacturaBc
            FROM vt_ordenAtencion o
            LEFT JOIN vt_pacientes p ON o.ordenAten_nhcl = p.mas_nhcl
            LEFT JOIN vt_clientes c ON o.ordenAten_idcliente = c.pa_codigo
            LEFT JOIN vt_especie esp ON p.mas_especie = esp.especie_id
            LEFT JOIN vt_especialidades es ON o.ordenAten_esp_numero = es.es_id
            LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
            LEFT JOIN vt_usuarios enf ON o.ordenAten_idEnfermero = enf.user_numero
            WHERE o.ordenAten_fecha = @Fecha
              AND {AlcanceOrden}
              AND (@LocalidadSesion <= 0 OR o.ordenAten_idlocalidad = @LocalidadSesion)
              AND ISNULL(o.ordenAten_estado, 'A') <> 'I'
              AND (@Etapa = '' OR o.ordenAten_etapa = @Etapa)
              AND (@Filtro = '' OR o.ordenAten_nombre LIKE '%' + @Filtro + '%' OR p.mas_nombre LIKE '%' + @Filtro + '%' OR ISNULL(o.ordenAten_turno, '') LIKE '%' + @Filtro + '%' OR CAST(o.ordenAten_id AS VARCHAR) = @Filtro)
            ORDER BY o.ordenAten_id DESC;";

        try
        {
            var res = (await _db.QueryAsync<OrdenAtencionResumenDto>(sql, new
            {
                Fecha = fecha.Date,
                Filtro = filtro?.Trim() ?? string.Empty,
                Etapa = etapa?.Trim() ?? string.Empty,
                Rol = _session.TipoUser,
                VeTodasSucursales = EsRolSinRestriccionSucursal(_session.TipoUser) ? 1 : 0,
                LocalidadSesion = _session.Localidad,
                UsuarioSesion = _session.IdUser
            })).ToList();
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando órdenes de atención en SQL Server");
            throw;
        }
    }

    public async Task<List<OrdenPacienteResumenDto>> ObtenerOrdenesPacienteAsync(int nhcl)
    {
        if (!_session.IsAuthenticated || nhcl <= 0) return [];
        var ordenes = await _db.QueryAsync<OrdenPacienteResumenDto>($@"
            SELECT o.ordenAten_id AS IdOrden,
                   ISNULL(o.ordenAten_fecha, GETDATE()) AS Fecha,
                   ISNULL(o.ordenAten_hora, '') AS Hora,
                   ISNULL(esp.es_descripcion, 'Consulta General') AS Especialidad,
                   ISNULL(u.user_nombre, 'Médico') AS Medico,
                   ISNULL(o.ordenAten_estado, 'A') AS Estado,
                   CAST(ISNULL(o.ordenAten_esProcedimiento, 0) AS bit) AS EsProcedimiento
            FROM vt_ordenAtencion o
            LEFT JOIN vt_usuarios u ON o.ordenAten_idMedico = u.user_numero
            LEFT JOIN vt_especialidades esp ON o.ordenAten_esp_numero = esp.es_id
            WHERE o.ordenAten_nhcl = @Nhcl AND {AlcanceOrden}
            ORDER BY o.ordenAten_id DESC", new
            {
                Nhcl = nhcl,
                Rol = _session.TipoUser,
                VeTodasSucursales = EsRolSinRestriccionSucursal(_session.TipoUser) ? 1 : 0,
                LocalidadSesion = _session.Localidad,
                UsuarioSesion = _session.IdUser
            });
        return ordenes.ToList();
    }

    public async Task<List<SelectorClinicoDto>> BuscarMascotasConOrdenesVisiblesAsync(string filtro)
    {
        if (!_session.IsAuthenticated || string.IsNullOrWhiteSpace(filtro))
            return [];

        var pacientes = await _db.QueryAsync<SelectorClinicoDto>($@"
            SELECT TOP 100 p.mas_nhcl AS Id,
                   RTRIM(p.mas_nombre) + ' · NHCL ' + CAST(p.mas_nhcl AS varchar(20)) AS Descripcion
            FROM vt_pacientes p
            WHERE p.mas_estado = 'A' AND p.mas_nombre LIKE '%' + @Filtro + '%'
              AND (@VeTodasSucursales = 1 OR EXISTS (
                    SELECT 1 FROM vt_ordenAtencion o
                    WHERE o.ordenAten_nhcl = p.mas_nhcl AND {AlcanceOrden}))
            ORDER BY p.mas_nombre, p.mas_nhcl",
            new
            {
                Filtro = filtro.Trim(),
                Rol = _session.TipoUser,
                VeTodasSucursales = EsRolSinRestriccionSucursal(_session.TipoUser) ? 1 : 0,
                LocalidadSesion = _session.Localidad,
                UsuarioSesion = _session.IdUser
            });
        return pacientes.ToList();
    }

    public async Task<bool> PuedeConsultarOrdenAsync(int idOrden, int nhcl)
    {
        if (!_session.IsAuthenticated || idOrden <= 0)
            return false;

        return await _db.ExecuteScalarAsync<int>($@"
            SELECT COUNT(*) FROM vt_ordenAtencion o
            WHERE o.ordenAten_id = @IdOrden AND (@Nhcl <= 0 OR o.ordenAten_nhcl = @Nhcl)
              AND {AlcanceOrden}",
            new
            {
                IdOrden = idOrden,
                Nhcl = nhcl,
                Rol = _session.TipoUser,
                VeTodasSucursales = EsRolSinRestriccionSucursal(_session.TipoUser) ? 1 : 0,
                LocalidadSesion = _session.Localidad,
                UsuarioSesion = _session.IdUser
            }) > 0;
    }

    public async Task<List<CatalogoOperativoDto>> ObtenerCatalogoProcedimientosTriajeAsync(int idOrden, int nhcl, int? idProducto = null)
    {
        if (nhcl <= 0 || !await PuedeConsultarOrdenAsync(idOrden, nhcl))
            return [];

        return (await _db.QueryAsync<CatalogoOperativoDto>("""
            SELECT p.CODIGO AS Id, COALESCE(NULLIF(RTRIM(p.CODIGO_PRINCIPAL), ''), CAST(p.CODIGO AS varchar(30))) AS Codigo,
                RTRIM(ISNULL(p.NOMBRE, '')) AS Descripcion, p.IDSUBTIPO AS IdSubgrupo,
                RTRIM(ISNULL(s.DESCRIPCION, '')) AS Subgrupo, p.VALOR_UNITARIO AS Precio
            FROM VeterinariaFacturacion.dbo.PRODUCTO p
            JOIN VeterinariaFacturacion.dbo.PRODUCTOTIPO t ON t.IDTIPOPRODUCTO = p.TIPO_PRODUCTO
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTOSUBTIPO s ON s.IDSUBTIPO = p.IDSUBTIPO
            WHERE p.IDUSUARIO = @IdUsuario AND ISNULL(p.ESTADO, 1) = 1
                AND UPPER(LTRIM(RTRIM(t.DESCRIPCION))) IN ('PROCEDIMIENTO', 'PROCEDIMIENTOS', 'SERVICIOS')
                AND (@IdProducto IS NULL OR p.CODIGO = @IdProducto)
            ORDER BY p.NOMBRE
            """, new { IdProducto = idProducto, IdUsuario = 1_000_000 + Math.Max(1, _session.Localidad) })).ToList();
    }

    public async Task<List<ProcedimientoTriajeDto>> ObtenerProcedimientosTriajeAsync(int idOrden, int nhcl)
    {
        if (nhcl <= 0 || !await PuedeConsultarOrdenAsync(idOrden, nhcl))
            return [];

        var columnas = await ObtenerColumnasTablaAsync("vt_procedimientos");
        var cantidad = columnas.Contains("proc_cantidad") ? "ISNULL(p.proc_cantidad, 1)" : "1";
        var observaciones = columnas.Contains("proc_observaciones") ? "ISNULL(CONVERT(nvarchar(250), p.proc_observaciones), '')" : "''";
        var estado = columnas.Contains("proc_estado") ? "ISNULL(CONVERT(nvarchar(30), p.proc_estado), '')" : "''";
        var precio = columnas.Contains("proc_precio") ? "p.proc_precio" : "NULL";
        return (await _db.QueryAsync<ProcedimientoTriajeDto>($"""
            SELECT p.proc_idProdu AS Id, ISNULL(producto.CODIGO_PRINCIPAL, '') AS Codigo,
                COALESCE(producto.NOMBRE, CONCAT('Procedimiento #', p.proc_idProdu)) AS Descripcion,
                ISNULL(subtipo.DESCRIPCION, '') AS Subgrupo, {cantidad} AS Cantidad,
                {observaciones} AS Observaciones, {estado} AS Estado, {precio} AS Precio
            FROM vt_procedimientos p
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTO producto ON producto.CODIGO = p.proc_idProdu
            LEFT JOIN VeterinariaFacturacion.dbo.PRODUCTOSUBTIPO subtipo ON subtipo.IDSUBTIPO = producto.IDSUBTIPO
            WHERE p.proc_idOrden = @IdOrden AND p.proc_nhcl = @Nhcl
            ORDER BY p.proc_fecha, p.proc_idProdu
            """, new { IdOrden = idOrden, Nhcl = nhcl })).ToList();
    }

    public async Task<bool> AgregarProcedimientoTriajeAsync(int idOrden, int nhcl, int idProducto, int cantidad, string observaciones, bool desdeHistorial, bool desdeSoap)
    {
        if (nhcl <= 0 || idProducto <= 0 || cantidad <= 0 || observaciones.Length > 250)
            return false;

        var procedimiento = (await ObtenerCatalogoProcedimientosTriajeAsync(idOrden, nhcl, idProducto)).SingleOrDefault();
        if (procedimiento is null)
            return false;

        var columnas = await ObtenerColumnasTablaAsync("vt_procedimientos");
        if ((cantidad != 1 && !columnas.Contains("proc_cantidad")) ||
            (observaciones.Length > 0 && !columnas.Contains("proc_observaciones")))
            return false;
        (string Columna, string Valor)[] opcionales =
        [
            ("proc_descripcion", "LEFT(@Descripcion, 250)"),
            ("proc_cantidad", "@Cantidad"),
            ("proc_estado", "'Por Entregar'"),
            ("proc_observaciones", "@Observaciones"),
            ("proc_precio", "@Precio")
        ];
        var camposDisponibles = opcionales.Where(campo => columnas.Contains(campo.Columna)).ToArray();
        var columnasOpcionales = string.Concat(camposDisponibles.Select(campo => ", " + campo.Columna));
        var valoresOpcionales = string.Concat(camposDisponibles.Select(campo => ", " + campo.Valor));

        return await _db.ExecuteAsync($"""
            INSERT INTO vt_procedimientos (proc_idOrden, proc_nhcl, proc_idProdu, proc_fecha{columnasOpcionales})
            SELECT o.ordenAten_id, o.ordenAten_nhcl, @IdProducto, GETDATE(){valoresOpcionales}
            FROM vt_ordenAtencion o WITH (UPDLOCK, HOLDLOCK)
            WHERE o.ordenAten_id = @IdOrden AND o.ordenAten_nhcl = @Nhcl
              AND {AlcanceOrden}
              AND (
                  (@DesdeSoap = 1 AND (ISNULL(o.ordenAten_estado, 'A') IN ('A', 'E') OR (@Rol = 1 AND o.ordenAten_estado = 'T')))
                  OR (@DesdeSoap = 0 AND ISNULL(o.ordenAten_esProcedimiento, 0) = 1
                      AND (@Rol = 1 OR o.ordenAten_idEnfermero = @UsuarioSesion)
                      AND EXISTS (
                  SELECT 1 FROM vt_usuarios u
                  JOIN vt_tipos_usuarios t ON t.tipuser_numero = u.user_tipouser_numero
                  WHERE u.user_numero = @UsuarioSesion AND u.user_estado = 'A'
                      AND (t.tipuser_numero = 1 OR UPPER(t.tipuser_descripcion) LIKE '%ENFERMER%')
              )
              AND (
                  (@DesdeHistorial = 0
                      AND ISNULL(o.ordenAten_estado, 'A') IN ('A', 'E')
                      AND NOT EXISTS (SELECT 1 FROM vt_vitacora v
                          WHERE v.vit_detalle = CONCAT('Triaje de ingreso completado Orden #', o.ordenAten_id)))
                  OR (@DesdeHistorial = 1
                      AND (ISNULL(o.ordenAten_estado, 'A') IN ('A', 'E') OR (@Rol = 1 AND o.ordenAten_estado = 'T'))
                      AND EXISTS (SELECT 1 FROM vt_fichamedica f
                          WHERE f.med_orden = o.ordenAten_id AND f.med_nhcl = o.ordenAten_nhcl
                              AND f.sec = (SELECT MAX(actual.sec) FROM vt_fichamedica actual
                                  WHERE actual.med_orden = f.med_orden AND actual.med_nhcl = f.med_nhcl)
                              AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), f.med_anamnesis))), '') IS NULL))
              )
                  ))
              AND NOT EXISTS (SELECT 1 FROM vt_procedimientos p WITH (UPDLOCK, HOLDLOCK)
                  WHERE p.proc_idOrden = @IdOrden AND p.proc_nhcl = @Nhcl AND p.proc_idProdu = @IdProducto);
            """, new
        {
            IdOrden = idOrden, Nhcl = nhcl, IdProducto = idProducto, DesdeHistorial = desdeHistorial, DesdeSoap = desdeSoap,
            procedimiento.Descripcion, Precio = procedimiento.Precio ?? 0m, Cantidad = cantidad, Observaciones = observaciones,
            Rol = _session.TipoUser, UsuarioSesion = _session.IdUser,
            VeTodasSucursales = EsRolSinRestriccionSucursal(_session.TipoUser) ? 1 : 0
        }) > 0;
    }

    public async Task<List<OrdenAtencionResumenDto>> ObtenerColaClinicaAsync(bool triaje, int? idOrden = null)
    {
        if (!_session.IsAuthenticated) return [];

        string sql = $@"
            SELECT o.ordenAten_id AS IdOrden,
                   o.ordenAten_nhcl AS Nhcl,
                   ISNULL(o.ordenAten_turno, '') AS Turno,
                   ISNULL(p.mas_nombre, '') AS Mascota,
                   o.ordenAten_esp_numero AS IdEspecialidad,
                   ISNULL(e.es_descripcion, '') AS Especialidad,
                   ISNULL(es.especie_descripcion, '') AS Especie,
                   ISNULL(u.user_nombre, '') AS Medico,
                   ISNULL(enf.user_nombre, '') AS Enfermero,
                   ISNULL(o.ordenAten_estado, 'A') AS Estado,
                   CAST(ISNULL(o.ordenAten_esProcedimiento, 0) AS bit) AS EsProcedimiento,
                   COALESCE(asignacion.Fecha, historica.Fecha,
                       DATEADD(second, DATEDIFF(second, CAST('00:00' AS time),
                           COALESCE(TRY_CONVERT(time, o.ordenAten_hora), CAST('00:00' AS time))),
                           CAST(CAST(o.ordenAten_fecha AS date) AS datetime))) AS FechaAsignacion
            FROM vt_ordenAtencion o
            LEFT JOIN vt_pacientes p ON p.mas_nhcl = o.ordenAten_nhcl
            LEFT JOIN vt_especialidades e ON e.es_id = o.ordenAten_esp_numero
            LEFT JOIN vt_especie es ON es.especie_id = p.mas_especie
            LEFT JOIN vt_usuarios u ON u.user_numero = o.ordenAten_idMedico
            LEFT JOIN vt_usuarios enf ON enf.user_numero = o.ordenAten_idEnfermero
            OUTER APPLY (
                SELECT MAX(v.vit_idFechaMod) AS Fecha
                FROM vt_vitacora v
                WHERE v.vit_detalle LIKE CONCAT(@PrefijoAsignacion, o.ordenAten_id)
            ) asignacion
            OUTER APPLY (
                SELECT MAX(v.vit_idFechaMod) AS Fecha
                FROM vt_vitacora v
                WHERE asignacion.Fecha IS NULL
                  AND v.vit_detalle LIKE CONCAT('Cambio de médico/especialidad en orden #', o.ordenAten_id, ': médico %, especialidad %')
            ) historica
            WHERE ((@IdOrden IS NOT NULL AND o.ordenAten_id = @IdOrden)
                OR (@IdOrden IS NULL AND o.ordenAten_fecha >= @Hoy AND o.ordenAten_fecha < DATEADD(day, 1, @Hoy)
                    AND (@LocalidadSesion <= 0 OR o.ordenAten_idlocalidad = @LocalidadSesion)
                    AND ISNULL(o.ordenAten_estado, 'A') IN ('A', 'E')))
              AND {AlcanceOrden}
              AND (
                   (@Triaje = 0 AND (@Rol <> 2 OR o.ordenAten_idMedico = @Usuario))
                   OR (@Triaje = 1 AND (
                        (@Rol = 1 OR EXISTS (SELECT 1 FROM vt_usuarios enfermero
                            JOIN vt_tipos_usuarios tipo ON tipo.tipuser_numero = enfermero.user_tipouser_numero
                            WHERE enfermero.user_numero = @Usuario AND enfermero.user_estado = 'A'
                                AND UPPER(tipo.tipuser_descripcion) LIKE '%ENFERMER%'))
                        AND (ISNULL(o.ordenAten_triajeMedico, CASE WHEN o.ordenAten_esp_numero > 0 THEN 1 ELSE 0 END) = 1
                            OR (ISNULL(o.ordenAten_esProcedimiento, 0) = 1 AND (@Rol = 1 OR o.ordenAten_idEnfermero = @Usuario)))
                        AND ISNULL(o.ordenAten_estado, 'A') IN ('A', 'E')
                        AND NOT EXISTS (SELECT 1 FROM vt_vitacora v
                            WHERE v.vit_detalle = CONCAT('Triaje de ingreso completado Orden #', o.ordenAten_id))
                   ))
              )
            ORDER BY FechaAsignacion, o.ordenAten_id;";

        return (await _db.QueryAsync<OrdenAtencionResumenDto>(sql, new
        {
            Hoy = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).Date,
            Triaje = triaje,
            IdOrden = idOrden,
            Rol = _session.TipoUser,
            VeTodasSucursales = EsRolSinRestriccionSucursal(_session.TipoUser) ? 1 : 0,
            LocalidadSesion = _session.Localidad,
            Usuario = _session.IdUser,
            UsuarioSesion = _session.IdUser,
            PrefijoAsignacion
        })).ToList();
    }

    public async Task<bool> CambiarEstadoOrdenAsync(int idOrden, string nuevoEstado, string nuevaEtapa)
    {
        if (!await PuedeConsultarOrdenAsync(idOrden, 0)) return false;

        try
        {
            const string sql = @"
                UPDATE vt_ordenAtencion 
                SET ordenAten_estado = @Estado, 
                    ordenAten_etapa = @Etapa,
                    ordenAten_fechaenAtencion = CASE WHEN @Estado = 'E' THEN GETDATE() ELSE ordenAten_fechaenAtencion END,
                    ordenAten_fechaProcesado = CASE WHEN @Estado = 'T' THEN GETDATE() ELSE ordenAten_fechaProcesado END
                WHERE ordenAten_id = @IdOrden";
            int rows = await _db.ExecuteAsync(sql, new { IdOrden = idOrden, Estado = nuevoEstado, Etapa = nuevaEtapa });
            if (rows > 0) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cambiando el estado de la orden en SQL Server");
            throw;
        }
        throw new InvalidOperationException("SQL Server no confirmó la actualización de la orden.");
    }

    public async Task<bool> CambiarAsignacionOrdenAsync(int idOrden, int idMedico, int idEspecialidad, int idUsuario)
    {
        if (idMedico <= 0 || idEspecialidad <= 0 || !await PuedeConsultarOrdenAsync(idOrden, 0)) return false;
        int localidadOrden = await _db.ExecuteScalarAsync<int>(
            "SELECT ISNULL(ordenAten_idlocalidad, 0) FROM vt_ordenAtencion WHERE ordenAten_id = @IdOrden",
            new { IdOrden = idOrden });
        int localidadValidar = localidadOrden > 0 ? localidadOrden : ResolverLocalidadActiva();
        if (!await MedicoAtiendeEnLocalidadAsync(idMedico, localidadValidar)) return false;
        var columnas = await ObtenerColumnasTablaAsync("vt_ordenAtencion");
        string anterior = columnas.Contains("ordenaten_anteriorIdMed") ? "ordenaten_anteriorIdMed=ordenAten_idMedico," : string.Empty;
        string updateTurnoSql = @"
            DECLARE @EspActual INT, @FechaOrd DATE, @NuevoTurno VARCHAR(20);
            SELECT @EspActual = ordenAten_esp_numero, @FechaOrd = CAST(ordenAten_fecha AS DATE), @NuevoTurno = ordenAten_turno
            FROM vt_ordenAtencion WHERE ordenAten_id = @IdOrden;

            IF @EspActual IS NULL OR @EspActual <> @IdEspecialidad OR @NuevoTurno IS NULL OR RTRIM(@NuevoTurno) = ''
            BEGIN
                DECLARE @Pref VARCHAR(10);
                SELECT @Pref = ISNULL(NULLIF(RTRIM(es_codigo), ''), LEFT(UPPER(LTRIM(RTRIM(ISNULL(es_descripcion, 'GEN')))), 3))
                FROM vt_especialidades WHERE es_id = @IdEspecialidad;
                IF @Pref IS NULL OR @Pref = '' SET @Pref = 'GEN';

                DECLARE @Sig INT;
                SELECT @Sig = COUNT(1) + 1
                FROM vt_ordenAtencion
                WHERE CAST(ordenAten_fecha AS DATE) = @FechaOrd
                  AND ordenAten_esp_numero = @IdEspecialidad
                  AND ordenAten_id <> @IdOrden;

                SET @NuevoTurno = @Pref + '-' + RIGHT('00' + CAST(@Sig AS VARCHAR(10)), CASE WHEN @Sig < 100 THEN 2 ELSE LEN(CAST(@Sig AS VARCHAR(10))) END);
            END
        ";
        int rows = await _db.ExecuteScalarAsync<int>($@"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            {updateTurnoSql}
            DECLARE @Cambios TABLE (MedicoAnterior INT);
            UPDATE vt_ordenAtencion SET {anterior}
                ordenAten_idMedico=@IdMedico, ordenAten_esp_numero=@IdEspecialidad, ordenAten_turno=@NuevoTurno
            OUTPUT deleted.ordenAten_idMedico INTO @Cambios
            WHERE ordenAten_id=@IdOrden;

            INSERT INTO vt_vitacora (vit_detalle, vit_idUser, vit_idFechaMod, ip, hostname)
            SELECT CONCAT(@PrefijoAsignacion, @IdOrden), @IdUsuario, DATEADD(hour, -5, SYSUTCDATETIME()), '127.0.0.1', 'BlazorClient'
            FROM @Cambios WHERE ISNULL(MedicoAnterior, 0) <> @IdMedico;
            COMMIT TRANSACTION;
            SELECT COUNT(*) FROM @Cambios;", new { IdOrden = idOrden, IdMedico = idMedico, IdEspecialidad = idEspecialidad, IdUsuario = idUsuario, PrefijoAsignacion });
        if (rows > 0) await _db.RegistrarBitacoraAsync($"Cambio de médico/especialidad en orden #{idOrden}: médico {idMedico}, especialidad {idEspecialidad}", idUsuario);
        return rows > 0;
    }

    public async Task<bool> AnularOrdenAsync(int idOrden, int idUsuario)
    {
        if (!await PuedeConsultarOrdenAsync(idOrden, 0)) return false;

        var columnas = await ObtenerColumnasTablaAsync("vt_ordenAtencion");
        string auditoria = columnas.Contains("ordenAten_borra") ? ", ordenAten_borra=@IdUsuario" : string.Empty;
        int rows = await _db.ExecuteAsync($"UPDATE vt_ordenAtencion SET ordenAten_estado='I'{auditoria} WHERE ordenAten_id=@IdOrden AND ISNULL(ordenAten_estado,'A')<>'I'", new { IdOrden = idOrden, IdUsuario = idUsuario });
        if (rows > 0) await _db.RegistrarBitacoraAsync($"Anulación de orden #{idOrden}", idUsuario);
        return rows > 0;
    }

    // =========================================================================
    // 3. AGENDA DE CITAS MÉDICAS (vt_administracionCitasnew)
    // =========================================================================

    public async Task<bool> PuedeVerAgendaCompletaAsync()
    {
        if (!_session.IsAuthenticated || _session.TipoUser == 2)
            return false;
        if (EsRolSinRestriccionSucursal(_session.TipoUser))
            return true;

        var rol = await _db.ExecuteScalarAsync<string>(@"
            SELECT tipuser_descripcion
            FROM vt_tipos_usuarios
            WHERE tipuser_numero = @Rol", new { Rol = _session.TipoUser });

        return rol?.Contains("recepci", StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerSucursalesAgendaAsync()
    {
        if (!_session.IsAuthenticated)
            return [];

        var sucursales = await _db.QueryAsync<CatalogoItemGenericoDto>($@"
            SELECT loc_id AS Id, RTRIM(ISNULL(loc_descripcion, '')) AS Descripcion
            FROM vt_localidades
            WHERE ISNULL(loc_estado, 'A') = 'A'
              AND (@EsAdmin = 1 OR loc_id IN ({SucursalesAsignadas}))
            ORDER BY loc_descripcion", new { EsAdmin = EsRolSinRestriccionSucursal(_session.TipoUser), UsuarioSesion = _session.IdUser });
        return sucursales.ToList();
    }

    public async Task<List<CatalogoItemGenericoDto>> ObtenerMedicosAgendaAsync(int? idLocalidad = null)
    {
        if (!_session.IsAuthenticated ||
            (idLocalidad.HasValue && !await PuedeUsarSucursalAgendaAsync(idLocalidad.Value)))
            return [];

        var medicos = await _db.QueryAsync<CatalogoItemGenericoDto>($@"
            SELECT u.user_numero AS Id, RTRIM(u.user_nombre) AS Descripcion,
                   ISNULL(u.user_esp_numero, 0) AS IdPadre
            FROM vt_usuarios u
            WHERE u.user_estado = 'A'
              AND (@AgendaCompleta = 1 OR u.user_numero = @UsuarioSesion)
              AND (
                    ((u.user_tipouser_numero = 2 OR UPPER(LTRIM(RTRIM(ISNULL(u.user_tipomedico, '')))) IN ('P', 'L', 'E'))
                     AND (@IdLocalidad IS NULL OR NOT EXISTS (
                         SELECT 1 FROM vt_horariosMedicos programado
                         WHERE programado.hr_iduser = u.user_numero
                           AND programado.hr_localidad > 0))
                     AND ((@IdLocalidad IS NULL AND @EsAdmin = 1) OR EXISTS (
                         SELECT 1 FROM vt_usuarioLocalidades ul
                         WHERE ul.ul_idUsuario = u.user_numero AND ISNULL(ul.ul_estado, 'A') = 'A'
                           AND (@IdLocalidad IS NULL OR ul.ul_idLocalidad = @IdLocalidad)
                           AND (@EsAdmin = 1 OR ul.ul_idLocalidad IN ({SucursalesAsignadas})))))
                    OR EXISTS (
                         SELECT 1 FROM vt_horariosMedicos h
                         WHERE h.hr_iduser = u.user_numero
                           AND (@IdLocalidad IS NULL OR h.hr_localidad = @IdLocalidad)
                           AND (@EsAdmin = 1 OR h.hr_localidad IN ({SucursalesAsignadas})))
                  )
            ORDER BY u.user_nombre", new
        {
            IdLocalidad = idLocalidad,
            AgendaCompleta = await PuedeVerAgendaCompletaAsync(),
            EsAdmin = EsRolSinRestriccionSucursal(_session.TipoUser),
            UsuarioSesion = _session.IdUser
        });
        var lista = medicos.ToList();
        if (lista.Count > 0)
        {
            var esps = (await _db.QueryAsync<(int IdUsuario, int IdEspecialidad)>(@"
                SELECT ue_idUsuario AS IdUsuario, ue_idEspecialidad AS IdEspecialidad
                FROM vt_usuarioEspecialidades
                WHERE ISNULL(ue_estado, 'A') = 'A'
                  AND ue_idEspecialidad > 0")).ToLookup(x => x.IdUsuario, x => x.IdEspecialidad);

            foreach (var m in lista)
            {
                var ids = new HashSet<int>();
                if (m.IdPadre.GetValueOrDefault() > 0)
                    ids.Add(m.IdPadre!.Value);
                foreach (var espId in esps[m.Id])
                    ids.Add(espId);

                if (m.IdPadre.GetValueOrDefault() <= 0 && ids.Count > 0)
                    m.IdPadre = ids.First();

                m.Codigo = string.Join(",", ids);
            }
        }
        return lista;
    }

    private async Task<bool> PuedeUsarSucursalAgendaAsync(int idLocalidad)
    {
        if (!_session.IsAuthenticated || idLocalidad <= 0)
            return false;

        return await _db.ExecuteScalarAsync<int>($@"
            SELECT COUNT(*) FROM vt_localidades
            WHERE loc_id = @IdLocalidad AND ISNULL(loc_estado, 'A') = 'A'
              AND (@EsAdmin = 1 OR loc_id IN ({SucursalesAsignadas}))",
            new { IdLocalidad = idLocalidad, EsAdmin = EsRolSinRestriccionSucursal(_session.TipoUser), UsuarioSesion = _session.IdUser }) > 0;
    }

    public async Task<List<SlotHorarioMedicoDto>> ObtenerSlotsHorarioMedicoAsync(int idMedico, DateTime fecha, int idLocalidad)
    {
        var dias = await ObtenerSlotsHorarioMedicoPorRangoAsync(idMedico, fecha, fecha, idLocalidad);
        return dias.GetValueOrDefault(fecha.Date) ?? [];
    }

    public async Task<Dictionary<DateTime, List<SlotHorarioMedicoDto>>> ObtenerSlotsHorarioMedicoPorRangoAsync(
        int idMedico, DateTime desde, DateTime hasta, int idLocalidad)
    {
        Dictionary<DateTime, List<SlotHorarioMedicoDto>> dias = [];
        if (!_session.IsAuthenticated || idMedico <= 0 || (idMedico != _session.IdUser && !await PuedeVerAgendaCompletaAsync()) ||
            hasta.Date < desde.Date || (hasta.Date - desde.Date).Days >= 42 || !await PuedeUsarSucursalAgendaAsync(idLocalidad))
            return dias;

        var columnas = await ObtenerColumnasTablaAsync("vt_horariosMedicos");
        string[] requeridas = ["hr_iduser", "hr_localidad", "hr_dia", "hr_desde", "hr_hasta", "hr_intervalo"];
        if (requeridas.Any(c => !columnas.Contains(c)))
            return dias;

        var parametros = new { IdMedico = idMedico, IdLocalidad = idLocalidad, Desde = desde.Date, Hasta = hasta.Date };
        string dia = columnas.Contains("hr_dia2") ? "COALESCE(NULLIF(LTRIM(RTRIM(hr_dia2)), ''), hr_dia)" : "hr_dia";
        string inicio = columnas.Contains("hr_vigencia_desde") ? "hr_vigencia_desde" : "CAST(NULL AS date)";
        string fin = columnas.Contains("hr_vigencia_hasta") ? "hr_vigencia_hasta" : "CAST(NULL AS date)";
        var bloques = (await _db.QueryAsync<(string Dia, string Desde, string Hasta, int Intervalo, DateTime? Inicio, DateTime? Fin)>($@"
            SELECT RTRIM({dia}), RTRIM(hr_desde), RTRIM(hr_hasta), ISNULL(hr_intervalo, 30), {inicio}, {fin}
            FROM vt_horariosMedicos
            WHERE hr_iduser=@IdMedico AND hr_localidad=@IdLocalidad", parametros)).ToList();

        var excepciones = await ObtenerColumnasTablaAsync("vt_calendarizacionHorariosMedicos");
        string[] requeridasExcepcion = ["cal_idmedico", "cal_fecha", "cal_desde", "cal_hasta", "cal_localidad"];
        var bloqueos = new List<(DateTime Fecha, string Desde, string Hasta)>();
        if (requeridasExcepcion.All(excepciones.Contains))
        {
            string estado = excepciones.Contains("cal_estado") ? "AND ISNULL(cal_estado, 'A')='A'" : "";
            bloqueos = (await _db.QueryAsync<(DateTime Fecha, string Desde, string Hasta)>($@"
                SELECT CAST(cal_fecha AS date), RTRIM(cal_desde), RTRIM(cal_hasta)
                FROM vt_calendarizacionHorariosMedicos
                WHERE cal_idmedico=@IdMedico AND cal_localidad=@IdLocalidad
                  AND cal_fecha>=@Desde AND cal_fecha<DATEADD(day, 1, @Hasta) {estado}", parametros)).ToList();
        }

        var citas = (await ObtenerCitasPorFechaAsync(desde, hasta, idMedico, idLocalidad))
            .Where(c => c.CiLocalidad == idLocalidad && c.CiEstado != "I" && c.CiEstadoCita != "X")
            .ToLookup(c => c.CiFecha.Date);
        var bloqueosPorDia = bloqueos.ToLookup(b => b.Fecha);
        for (var fecha = desde.Date; fecha <= hasta.Date; fecha = fecha.AddDays(1))
        {
            var slots = new SortedDictionary<string, SlotHorarioMedicoDto>(StringComparer.Ordinal);
            foreach (var bloque in bloques.Where(b => (b.Inicio == null || b.Inicio.Value.Date <= fecha) &&
                (b.Fin == null || b.Fin.Value.Date >= fecha) &&
                (string.Equals(b.Dia, fecha.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(b.Dia, DiaSemanaEspanol(fecha.DayOfWeek), StringComparison.OrdinalIgnoreCase))))
            {
                if (!TimeSpan.TryParse(bloque.Desde, out var hora) ||
                    !(TimeSpan.TryParse(bloque.Hasta, out var limite) || bloque.Hasta == "24:00"))
                    continue;

                if (bloque.Hasta == "24:00")
                    limite = TimeSpan.FromDays(1);

                var intervalo = TimeSpan.FromMinutes(bloque.Intervalo is >= 5 and <= 240 ? bloque.Intervalo : 30);
                for (; hora < limite; hora += intervalo)
                {
                    string textoHora = $"{hora.Hours:D2}:{hora.Minutes:D2}";
                    var cita = citas[fecha].FirstOrDefault(c => TimeSpan.TryParse(c.CiHora, out var horaCita) &&
                        horaCita.Hours == hora.Hours && horaCita.Minutes == hora.Minutes);
                    bool bloqueado = bloqueosPorDia[fecha].Any(b => EstaEnRango(hora, b.Desde, b.Hasta));
                    slots.TryAdd(textoHora, new SlotHorarioMedicoDto
                    {
                        Hora = textoHora,
                        Bloqueado = bloqueado,
                        Disponible = !bloqueado && cita == null,
                        CitaExistente = cita,
                        MotivoEstado = bloqueado ? "Bloqueo / excepción" : cita != null ? "Reservado" : "Disponible"
                    });
                }
            }
            dias[fecha] = slots.Values.ToList();
        }

        return dias;
    }

    private static string DiaSemanaEspanol(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "Lunes", DayOfWeek.Tuesday => "Martes", DayOfWeek.Wednesday => "Miércoles",
        DayOfWeek.Thursday => "Jueves", DayOfWeek.Friday => "Viernes", DayOfWeek.Saturday => "Sábado",
        _ => "Domingo"
    };

    private static bool EstaEnRango(TimeSpan hora, string desde, string hasta)
    {
        if (!TimeSpan.TryParse(desde, out var inicio)) return false;
        if (hasta == "24:00") return hora >= inicio;
        return TimeSpan.TryParse(hasta, out var fin) && hora >= inicio && hora < fin;
    }

    private async Task<HashSet<string>> AsegurarColumnaEspecialidadCitasAsync()
    {
        var columnas = await ObtenerColumnasTablaAsync("vt_citas");
        if (!columnas.Contains("ci_especialidad"))
        {
            try
            {
                await _db.ExecuteAsync(@"
                    IF COL_LENGTH('vt_citas', 'ci_especialidad') IS NULL
                        ALTER TABLE vt_citas ADD ci_especialidad INT NULL;");
                columnas.Add("ci_especialidad");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo agregar automáticamente la columna ci_especialidad en vt_citas");
            }
        }
        return columnas;
    }

    public async Task<List<CitaMedicaDto>> ObtenerCitasPorFechaAsync(DateTime fechaDesde, DateTime fechaHasta, int idMedico = 0, int? idLocalidad = null)
    {
        if (!_session.IsAuthenticated)
            return [];
        if (!await PuedeVerAgendaCompletaAsync())
        {
            if (idMedico != 0 && idMedico != _session.IdUser)
                return [];
            idMedico = _session.IdUser;
        }

        var columnas = await AsegurarColumnaEspecialidadCitasAsync();
        string motivoCancelacion = columnas.Contains("ci_refeliminacion") ? "ISNULL(c.ci_refeliminacion, '')" : "CAST('' AS VARCHAR(250))";
        string colEspExpr = columnas.Contains("ci_especialidad")
            ? "ISNULL(NULLIF(c.ci_especialidad, 0), ISNULL(u.user_esp_numero, 0))"
            : "ISNULL(u.user_esp_numero, 0)";
        string sql = $@"
            SELECT
                c.ci_id AS CiId,
                ISNULL(c.ci_nombre, '') AS CiNombre,
                ISNULL(c.ci_nombremas, '') AS CiNombreMas,
                ISNULL(c.ci_nhcl, 0) AS CiNhcl,
                c.ci_fecha AS CiFecha,
                ISNULL(c.ci_hora, '08:00') AS CiHora,
                CASE WHEN ISNULL(c.ci_estado,'A')='I' THEN 'X'
                     WHEN UPPER(ISNULL(c.ci_estadocita,'')) LIKE 'CONFIRM%' THEN 'C'
                     WHEN UPPER(ISNULL(c.ci_estadocita,'')) LIKE 'ATEND%' THEN 'A'
                     WHEN UPPER(ISNULL(c.ci_estadocita,'')) LIKE 'CANCEL%' THEN 'X'
                     ELSE ISNULL(c.ci_estadocita, 'N') END AS CiEstadoCita,
                ISNULL(c.ci_telefono, '') AS CiTelefono,
                ISNULL(c.ci_email, '') AS CiEmail,
                ISNULL(c.ci_observacion, '') AS CiObservacion,
                {motivoCancelacion} AS CiMotivoCancelacion,
                ISNULL(c.ci_localidad, 1) AS CiLocalidad,
                ISNULL(c.ci_usuario, 0) AS CiUsuario,
                ISNULL(u.user_nombre, '') AS NombreMedico,
                {colEspExpr} AS CiEspecialidad,
                ISNULL(RTRIM(e.es_codigo), '') AS CodigoEspecialidad,
                ISNULL(RTRIM(e.es_descripcion), '') AS Especialidad,
                ISNULL(c.ci_estado, 'A') AS CiEstado,
                ISNULL(c.ci_tipoConsulta, 1) AS CiTipoConsulta,
                ISNULL(c.ci_tipo, 'M') AS CiTipo
            FROM vt_citas c
            LEFT JOIN vt_usuarios u ON c.ci_usuario = u.user_numero
            LEFT JOIN vt_especialidades e ON {colEspExpr} = e.es_id
            WHERE c.ci_fecha >= @Desde AND c.ci_fecha < DATEADD(day, 1, @Hasta)
              AND (@IdMedico = 0 OR c.ci_usuario = @IdMedico)
              AND (@IdLocalidad IS NULL OR ISNULL(c.ci_localidad, 1) = @IdLocalidad)
              AND (@EsAdmin = 1 OR ISNULL(c.ci_localidad, 1) IN ({SucursalesAsignadas}))
            ORDER BY c.ci_fecha, c.ci_hora;";

        try
        {
            var res = (await _db.QueryAsync<CitaMedicaDto>(sql, new
            {
                Desde = fechaDesde.Date,
                Hasta = fechaHasta.Date,
                IdMedico = idMedico, IdLocalidad = idLocalidad, EsAdmin = EsRolSinRestriccionSucursal(_session.TipoUser), UsuarioSesion = _session.IdUser
            })).ToList();
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando citas en SQL Server");
            throw;
        }
    }

    public async Task<int> AgendarCitaAsync(CitaMedicaDto cita, int idUsuario)
    {
        if (!_session.IsAuthenticated || (cita.CiUsuario != _session.IdUser && !await PuedeVerAgendaCompletaAsync()))
            throw new InvalidOperationException("No tiene permiso para agendar una cita para ese médico.");
        if (!await PuedeUsarSucursalAgendaAsync(cita.CiLocalidad))
            throw new InvalidOperationException("No tiene acceso a la sucursal seleccionada.");

        try
        {
            if (TimeSpan.TryParse(cita.CiHora, out var tsHora))
            {
                cita.CiHora = $"{tsHora.Hours:D2}:{tsHora.Minutes:D2}";
            }

            var ahoraClinica = DateTime.UtcNow.AddHours(-5);
            if (cita.CiFecha.Date >= ahoraClinica.Date)
            {
                var slots = await ObtenerSlotsHorarioMedicoAsync(cita.CiUsuario, cita.CiFecha, cita.CiLocalidad);
                if (!slots.Any(slot => slot.Disponible && slot.Hora == cita.CiHora &&
                    TimeSpan.TryParse(slot.Hora, out var hora) && cita.CiFecha.Date.Add(hora) >= ahoraClinica))
                    throw new InvalidOperationException("Ese turno ya no está disponible para el médico y la fecha seleccionados.");
            }

            int superpuestas = await _db.ExecuteScalarAsync<int>(@"
                SELECT COUNT(1) FROM vt_citas
                WHERE ci_usuario=@CiUsuario AND ci_localidad=@CiLocalidad
                  AND CAST(ci_fecha AS date)=@CiFecha 
                  AND ISNULL(ci_estado,'A')='A'
                  AND ISNULL(ci_estadocita,'N') NOT IN ('X', 'CANCELADA')
                  AND (LEFT(RTRIM(ci_hora),5)=@CiHora 
                       OR RTRIM(ci_hora)=@CiHora 
                       OR TRY_CONVERT(time, RTRIM(ci_hora))=TRY_CONVERT(time, @CiHora))", new
            {
                cita.CiUsuario,
                cita.CiLocalidad,
                CiFecha = cita.CiFecha.Date,
                cita.CiHora
            });
            if (superpuestas > 0)
                throw new InvalidOperationException($"El médico ya tiene una cita activa agendada a las {cita.CiHora} en esta fecha.");

            var columnas = await AsegurarColumnaEspecialidadCitasAsync();
            string colEspInsert = columnas.Contains("ci_especialidad") ? ", ci_especialidad" : "";
            string valEspInsert = columnas.Contains("ci_especialidad") ? ", @CiEspecialidad" : "";

            string insertSql = $@"
                INSERT INTO vt_citas (
                    ci_nombre, ci_nombremas, ci_fecha, ci_hora, ci_estadocita,
                    ci_telefono, ci_observacion, ci_localidad, ci_usuario,
                    ci_usuarioEjecuta, ci_nhcl, ci_estado, ci_tipoConsulta, ci_email, ci_tipo{colEspInsert}
                )
                VALUES (
                    @CiNombre, @CiNombreMas, @CiFecha, @CiHora, @CiEstadoCita,
                    @CiTelefono, @CiObservacion, @CiLocalidad, @CiUsuario,
                    @IdUsuario, @CiNhcl, 'A', @CiTipoConsulta, @CiEmail, @CiTipo{valEspInsert}
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int idNueva = await _db.ExecuteScalarAsync<int>(insertSql, new
            {
                cita.CiNombre,
                cita.CiNombreMas,
                CiFecha = cita.CiFecha.Date,
                cita.CiHora,
                cita.CiEstadoCita,
                cita.CiTelefono,
                cita.CiObservacion,
                cita.CiLocalidad,
                cita.CiUsuario,
                IdUsuario = idUsuario,
                cita.CiNhcl,
                cita.CiTipoConsulta,
                cita.CiEmail,
                cita.CiTipo,
                CiEspecialidad = cita.CiEspecialidad > 0 ? (int?)cita.CiEspecialidad : null
            });

            if (idNueva > 0) return idNueva;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando cita en SQL Server");
            throw;
        }
        throw new InvalidOperationException("SQL Server no confirmó el registro de la cita.");
    }

    public async Task<bool> ActualizarEstadoCitaAsync(int idCita, string nuevoEstado)
    {
        if (!_session.IsAuthenticated) return false;
        try
        {
            var columnas = await ObtenerColumnasTablaAsync("vt_citas");
            var cambios = new List<string> { "ci_estadocita = @Estado" };
            if (nuevoEstado == "X")
            {
                cambios.Add("ci_estado = 'I'");
            }
            else if (nuevoEstado == "C" || nuevoEstado == "N" || nuevoEstado == "A")
            {
                cambios.Add("ci_estado = 'A'");
            }
            if (columnas.Contains("ci_HoraModificacion"))
            {
                cambios.Add("ci_HoraModificacion = GETDATE()");
            }

            int rows = await _db.ExecuteAsync(
                $"UPDATE vt_citas SET {string.Join(", ", cambios)} WHERE ci_id = @IdCita{PermisoEditarCita}",
                new { IdCita = idCita, Estado = nuevoEstado, MedicoDestino = (int?)null, UsuarioSesion = _session.IdUser, AgendaCompleta = await PuedeVerAgendaCompletaAsync(), EsAdmin = EsRolSinRestriccionSucursal(_session.TipoUser) });
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando el estado de la cita #{IdCita} a {Estado} en SQL Server", idCita, nuevoEstado);
            throw;
        }
    }

    public async Task<bool> ReagendarCitaAsync(int idCita, DateTime fecha, string hora, int idMedico, int idLocalidad, int idUsuario)
    {
        if (!await PuedeUsarSucursalAgendaAsync(idLocalidad) ||
            (idMedico != _session.IdUser && !await PuedeVerAgendaCompletaAsync()))
            return false;
        if (idCita <= 0 || idMedico <= 0 || idLocalidad <= 0 || !TimeSpan.TryParse(hora, out var horaNueva)) return false;
        string horaNormalizada = $"{horaNueva.Hours:D2}:{horaNueva.Minutes:D2}";
        int cruces = await _db.ExecuteScalarAsync<int>(@"SELECT COUNT(1) FROM vt_citas
            WHERE ci_id<>@IdCita AND ci_usuario=@IdMedico AND ci_localidad=@IdLocalidad
              AND CAST(ci_fecha AS date)=@Fecha AND LEFT(RTRIM(ci_hora),5)=LEFT(@Hora,5) AND ISNULL(ci_estado,'A')='A'",
            new { IdCita = idCita, IdMedico = idMedico, IdLocalidad = idLocalidad, Fecha = fecha.Date, Hora = horaNormalizada });
        if (cruces > 0) return false;
        var columnas = await ObtenerColumnasTablaAsync("vt_citas");
        var adicionales = new List<string> { "ci_estadocita='N'" };
        if (columnas.Contains("ci_HoraModificacion")) adicionales.Add("ci_HoraModificacion=GETDATE()");
        if (columnas.Contains("ci_usuarioPrincipal")) adicionales.Add("ci_usuarioPrincipal=@IdUsuario");
        string extra = "," + string.Join(',', adicionales);
        int rows = await _db.ExecuteAsync($@"UPDATE vt_citas SET ci_fecha=@Fecha,ci_hora=@Hora,ci_usuario=@IdMedico,
            ci_localidad=@IdLocalidad,ci_estado='A'{extra} WHERE ci_id=@IdCita AND ISNULL(ci_estado,'A')='A'{PermisoEditarCita}",
            new { IdCita = idCita, Fecha = fecha.Date, Hora = horaNormalizada, IdMedico = idMedico, IdLocalidad = idLocalidad, IdUsuario = idUsuario, MedicoDestino = idMedico, UsuarioSesion = _session.IdUser, AgendaCompleta = await PuedeVerAgendaCompletaAsync(), EsAdmin = EsRolSinRestriccionSucursal(_session.TipoUser) });
        if (rows <= 0) return false;

        var confirmacion = await _db.QueryFirstOrDefaultAsync<CitaMedicaDto>(@"
            SELECT ci_id AS CiId, ci_fecha AS CiFecha, LEFT(RTRIM(ci_hora), 5) AS CiHora,
                   ci_usuario AS CiUsuario, ci_localidad AS CiLocalidad, ci_estado AS CiEstado
            FROM vt_citas
            WHERE ci_id = @IdCita;", new { IdCita = idCita });
        bool confirmado = confirmacion is not null &&
                         confirmacion.CiFecha.Date == fecha.Date &&
                         string.Equals(confirmacion.CiHora, horaNormalizada, StringComparison.Ordinal) &&
                         confirmacion.CiUsuario == idMedico &&
                         confirmacion.CiLocalidad == idLocalidad &&
                         string.Equals(confirmacion.CiEstado, "A", StringComparison.OrdinalIgnoreCase);
        if (!confirmado)
        {
            _logger.LogError("La actualización de reagendamiento de la cita {IdCita} no pudo verificarse.", idCita);
            return false;
        }

        await _db.RegistrarBitacoraAsync($"Reagendamiento de cita #{idCita} para {fecha:yyyy-MM-dd} {horaNormalizada}", idUsuario);
        return true;
    }

    public async Task<bool> CancelarCitaAsync(int idCita, string motivo, int idUsuario)
    {
        if (!_session.IsAuthenticated || idCita <= 0) return false;
        string motivoNormalizado = string.IsNullOrWhiteSpace(motivo) ? "Cancelada por el usuario" : motivo.Trim();
        int usuarioAuditoria = idUsuario > 0 ? idUsuario : 1;
        try
        {
            var columnas = await ObtenerColumnasTablaAsync("vt_citas");
            var cambios = new List<string> { "ci_estado='I'", "ci_estadocita='X'" };
            if (columnas.Contains("ci_refeliminacion")) cambios.Add("ci_refeliminacion=@Motivo");
            if (columnas.Contains("ci_delete")) cambios.Add("ci_delete=@IdUsuario");
            if (columnas.Contains("ci_HoraModificacion")) cambios.Add("ci_HoraModificacion=GETDATE()");
            int rows = await _db.ExecuteAsync($"UPDATE vt_citas SET {string.Join(',', cambios)} WHERE ci_id=@IdCita{PermisoEditarCita}", new { IdCita = idCita, Motivo = motivoNormalizado, IdUsuario = usuarioAuditoria, MedicoDestino = (int?)null, UsuarioSesion = _session.IdUser, AgendaCompleta = await PuedeVerAgendaCompletaAsync(), EsAdmin = EsRolSinRestriccionSucursal(_session.TipoUser) });
            if (rows > 0)
            {
                try
                {
                    await _db.RegistrarBitacoraAsync($"Cancelación de cita #{idCita}: {motivoNormalizado}", usuarioAuditoria);
                }
                catch
                {
                    // No bloquear la cancelación si falla la bitácora
                }
            }
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelando cita #{IdCita} en SQL Server", idCita);
            throw;
        }
    }

    public async Task<bool> BloquearHorarioMedicoAsync(int idMedico, DateTime fecha, string horaDesde, string horaHasta, int idLocalidad)
    {
        try
        {
            int rows = await _db.ExecuteAsync(@"
                INSERT INTO vt_calendarizacionHorariosMedicos (cal_idmedico, cal_fecha, cal_desde, cal_hasta, cal_localidad, cal_estado)
                VALUES (@IdMedico, @Fecha, @Desde, @Hasta, @IdLocalidad, 'A')",
                new { IdMedico = idMedico, Fecha = fecha.Date, Desde = horaDesde, Hasta = horaHasta, IdLocalidad = idLocalidad });
            if (rows > 0) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bloqueando el horario médico en SQL Server");
            throw;
        }
        throw new InvalidOperationException("SQL Server no confirmó el bloqueo del horario.");
    }

    // =========================================================================
    // 4. FACTURACIÓN CONSOLIDADA / PROFORMAS
    // =========================================================================

    public async Task<List<OrdenProformaResumenDto>> BuscarOrdenesParaProformaAsync(DateTime desde, DateTime hasta, string filtro = "")
    {
        var ordenes = await ObtenerOrdenesDelDiaAsync(hasta, filtro);
        var resultado = new List<OrdenProformaResumenDto>();

        foreach (var o in ordenes)
        {
            var detalle = await ObtenerDetalleProformaOrdenAsync(o.IdOrden);
            if (detalle != null)
                resultado.Add(detalle);
        }

        return resultado;
    }

    public async Task<OrdenProformaResumenDto?> ObtenerDetalleProformaOrdenAsync(int idOrden)
    {
        var cols = await ObtenerColumnasTablaAsync("vt_ordenAtencion");
        string colFactura = cols.Contains("ordenaten_factura") ? "RTRIM(ISNULL(o.ordenaten_factura, 'N'))" : "'N'";
        string colNumFac = cols.Contains("ordenaten_numfacBC") ? "RTRIM(ISNULL(o.ordenaten_numfacBC, ''))" : "''";
        string sqlOrden = $@"
            SELECT TOP 1
                o.ordenAten_id AS IdOrden,
                o.ordenAten_nhcl AS Nhcl,
                o.ordenAten_idcliente AS IdCliente,
                RTRIM(ISNULL(c.pa_apellidos, '')) + ' ' + RTRIM(ISNULL(c.pa_nombres, '')) AS ClienteNombre,
                RTRIM(ISNULL(c.pa_cedula, '')) AS ClienteCedula,
                RTRIM(ISNULL(c.pa_email, '')) AS ClienteEmail,
                RTRIM(ISNULL(c.pa_movil, ISNULL(c.pa_telefono, ''))) AS ClienteTelefono,
                RTRIM(ISNULL(p.mas_nombre, '')) AS MascotaNombre,
                RTRIM(ISNULL(e.especie_descripcion, '')) AS Especie,
                RTRIM(ISNULL(u.user_nombre, '')) AS MedicoTratante,
                ISNULL(o.ordenAten_fecha, GETDATE()) AS FechaOrden,
                RTRIM(ISNULL(o.ordenAten_etapa, '')) AS EtapaActual,
                {colFactura} AS FacturadoBc,
                {colNumFac} AS NumeroFacturaBc
            FROM vt_ordenAtencion o
            LEFT JOIN vt_pacientes p ON p.mas_nhcl = o.ordenAten_nhcl
            LEFT JOIN vt_clientes c ON c.pa_codigo = o.ordenAten_idcliente
            LEFT JOIN vt_especie e ON e.especie_id = p.mas_especie
            LEFT JOIN vt_usuarios u ON u.user_numero = o.ordenAten_idMedico
            WHERE o.ordenAten_id = @IdOrden;";

        var orden = await _db.QueryFirstOrDefaultAsync<OrdenProformaResumenDto>(sqlOrden, new { IdOrden = idOrden });
        if (orden is null)
            return null;

        var items = new List<ItemFacturableProformaDto>();

        try
        {
            // Intentar cargar cargos reales desde vt_facturasBC / vt_facturasBChospi / qvt_productosUtilizados
            const string sqlItems = @"
                SELECT
                    fac_id AS IdRegistro,
                    fac_idorden AS IdOrden,
                    'HOSPITALIZACION' AS OrigenModulo,
                    ISNULL(fac_itemBC, 'SER-HOSP-01') AS CodigoItemBc,
                    ISNULL(fac_descripcion, 'Cargo Hospitalario') AS DescripcionItem,
                    CAST(ISNULL(fac_cantidad, 1) AS DECIMAL(18,2)) AS Cantidad,
                    CAST(ISNULL(fac_precio, 15.00) AS DECIMAL(18,2)) AS PrecioUnitario,
                    ISNULL(fac_fecha, GETDATE()) AS FechaCargo
                FROM vt_facturasBChospi
                WHERE fac_idorden = @IdOrden;";

            var dbItems = (await _db.QueryAsync<ItemFacturableProformaDto>(sqlItems, new { IdOrden = idOrden })).ToList();
            if (dbItems.Count > 0) items.AddRange(dbItems);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron obtener cargos de vt_facturasBChospi para la orden {IdOrden}", idOrden);
            // Cargos omitidos

        }

        orden.Items = items;
        return orden;
    }

    public Task<bool> AgregarCargoProformaManualAsync(ItemFacturableProformaDto cargo, int idUsuario)
    {
        _logger.LogWarning("No se agregó el cargo manual porque esa operación todavía no tiene persistencia configurada.");
        return Task.FromResult(false);
    }

    public async Task<(bool Exito, string NumeroProforma, string Mensaje)> EmitirProformaAsync(
        int idOrden, List<ItemFacturableProformaDto> itemsSeleccionados, int idUsuario)
    {
        string numeroGenerado = $"PROF-{DateTime.Now:yyyyMMdd}-{idOrden}";

        try
        {
            var cols = await ObtenerColumnasTablaAsync("vt_ordenAtencion");
            if (cols.Contains("ordenaten_factura") && cols.Contains("ordenaten_numfacBC"))
            {
                int actualizados = await _db.ExecuteAsync(@"
                    UPDATE vt_ordenAtencion
                    SET ordenaten_factura = 'S',
                        ordenaten_numfacBC = @NumFac
                    WHERE ordenAten_id = @IdOrden",
                    new { IdOrden = idOrden, NumFac = numeroGenerado });
                if (actualizados == 0)
                    return (false, string.Empty, "No se encontró la orden para emitir la proforma.");
            }
            else
            {
                await _db.ExecuteAsync(@"
                    UPDATE vt_ordenAtencion
                    SET ordenAten_etapa = 'PROFORMA'
                    WHERE ordenAten_id = @IdOrden",
                    new { IdOrden = idOrden });
            }




        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando la proforma de la orden {IdOrden}", idOrden);
            return (false, string.Empty, "No se pudo emitir la proforma en la base de datos.");
        }

        decimal total = itemsSeleccionados.Where(i => i.Seleccionado).Sum(i => i.Subtotal);
        return (true, numeroGenerado, $"Proforma consolidada {numeroGenerado} emitida por ${total:F2}.");
    }

    // =========================================================================
    // 5. USUARIOS Y BITÁCORA (vt_usuarios, vt_vitacora)
    // =========================================================================

    public async Task<List<UsuarioSistemaDto>> ObtenerUsuariosAsync(string filtro = "")
    {
        const string sql = @"
            SELECT
                u.user_numero AS UserNumero,
                ISNULL(u.user_nombre, '') AS UserNombre,
                ISNULL(u.user_login, '') AS UserLogin,
                ISNULL(u.user_estado, 'A') AS UserEstado,
                ISNULL(u.user_tipouser_numero, 2) AS UserTipoUserNumero,
                ISNULL(tu.tipuser_descripcion, 'Médico') AS TipoDescripcion,
                ISNULL(u.user_esp_numero, 1) AS UserEspNumero,
                ISNULL(e.es_descripcion, 'General') AS EspecialidadDescripcion,
                ISNULL(u.user_observaciones, '') AS UserObservaciones,
                ISNULL(u.user_tipomedico, 'P') AS UserTipoMedico,
                ISNULL(u.user_cedula, '') AS UserCedula,
                ISNULL(u.user_email, '') AS UserEmail,
                ISNULL(u.user_celular, '') AS UserCelular,
                ISNULL(u.user_iniciales, '') AS UserIniciales
            FROM vt_usuarios u
            LEFT JOIN vt_tipos_usuarios tu ON u.user_tipouser_numero = tu.tipuser_numero
            LEFT JOIN vt_especialidades e ON u.user_esp_numero = e.es_id
            WHERE (@Filtro = '' OR u.user_nombre LIKE '%' + @Filtro + '%' OR u.user_login LIKE '%' + @Filtro + '%')
            ORDER BY u.user_nombre;";

        try
        {
            return (await _db.QueryAsync<UsuarioSistemaDto>(sql, new { Filtro = filtro?.Trim() ?? string.Empty })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudieron consultar los usuarios del sistema.");
            throw;
        }
    }

    public async Task<bool> GuardarUsuarioAsync(UsuarioSistemaDto usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario.UserNombre) ||
            string.IsNullOrWhiteSpace(usuario.UserLogin) ||
            usuario.UserTipoUserNumero <= 0 ||
            usuario.UserEspNumero <= 0)
        {
            return false;
        }

        string login = usuario.UserLogin.Trim();
        int duplicados = await _db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1)
            FROM vt_usuarios
            WHERE user_login = @Login
              AND user_numero <> @IdUsuario;", new { Login = login, IdUsuario = usuario.UserNumero });
        if (duplicados > 0)
        {
            return false;
        }

        bool esNuevo = usuario.UserNumero <= 0;
        if (esNuevo && string.IsNullOrWhiteSpace(usuario.UserClave))
        {
            return false;
        }

        var parametros = new
        {
            IdUsuario = usuario.UserNumero,
            Nombre = usuario.UserNombre.Trim(),
            Login = login,
            Iniciales = usuario.UserIniciales.Trim(),
            Email = usuario.UserEmail.Trim(),
            Celular = usuario.UserCelular.Trim(),
            Convencional = usuario.UserConvencional.Trim(),
            Cedula = usuario.UserCedula.Trim(),
            TipoUsuario = usuario.UserTipoUserNumero,
            Especialidad = usuario.UserEspNumero,
            TipoMedico = usuario.UserTipoMedico.Trim(),
            Observaciones = usuario.UserObservaciones.Trim(),
            Estado = usuario.UserEstado == "I" ? "I" : "A",
            Clave = _db.HashPasswordSha1(usuario.UserClave)
        };

        int filas;
        if (esNuevo)
        {
            filas = await _db.ExecuteAsync(@"
                INSERT INTO vt_usuarios
                    (user_nombre, user_login, user_clave, user_iniciales, user_email,
                     user_celular, user_convensional, user_cedula, user_tipouser_numero,
                     user_esp_numero, user_tipomedico, user_observaciones, user_estado,
                     user_fechacambio)
                VALUES
                    (@Nombre, @Login, @Clave, @Iniciales, @Email,
                     @Celular, @Convencional, @Cedula, @TipoUsuario,
                     @Especialidad, @TipoMedico, @Observaciones, @Estado,
                     GETDATE());", parametros);

            if (filas <= 0) return false;
            usuario.UserNumero = await _db.ExecuteScalarAsync<int>(
                "SELECT user_numero FROM vt_usuarios WHERE user_login = @Login;", new { Login = login });
        }
        else
        {
            string sql = string.IsNullOrWhiteSpace(usuario.UserClave)
                ? @"
                    UPDATE vt_usuarios
                    SET user_nombre = @Nombre, user_login = @Login, user_iniciales = @Iniciales,
                        user_email = @Email, user_celular = @Celular, user_convensional = @Convencional,
                        user_cedula = @Cedula, user_tipouser_numero = @TipoUsuario,
                        user_esp_numero = @Especialidad, user_tipomedico = @TipoMedico,
                        user_observaciones = @Observaciones, user_estado = @Estado,
                        user_fechacambio = GETDATE()
                    WHERE user_numero = @IdUsuario;"
                : @"
                    UPDATE vt_usuarios
                    SET user_nombre = @Nombre, user_login = @Login, user_clave = @Clave,
                        user_iniciales = @Iniciales, user_email = @Email, user_celular = @Celular,
                        user_convensional = @Convencional, user_cedula = @Cedula,
                        user_tipouser_numero = @TipoUsuario, user_esp_numero = @Especialidad,
                        user_tipomedico = @TipoMedico, user_observaciones = @Observaciones,
                        user_estado = @Estado, user_fechacambio = GETDATE()
                    WHERE user_numero = @IdUsuario;";
            filas = await _db.ExecuteAsync(sql, parametros);
            if (filas <= 0) return false;
        }

        await _db.RegistrarBitacoraAsync(
            $"{(esNuevo ? "Crea" : "Actualiza")} usuario: {login}",
            usuario.UserNumero);
        return true;
    }

    public async Task<List<BitacoraAuditoriaDto>> ObtenerBitacoraAsync(DateTime desde, DateTime hasta, string filtro = "")
    {
        const string sql = @"
            SELECT TOP 200
                v.vit_id AS VitId,
                ISNULL(v.vit_detalle, '') AS VitDetalle,
                ISNULL(u.user_login, 'sistema') AS UserLogin,
                ISNULL(v.vit_idFechaMod, GETDATE()) AS VitFechaMod,
                ISNULL(v.ip, '127.0.0.1') AS Ip,
                ISNULL(v.hostname, 'HDEV-SRV') AS Hostname
            FROM vt_vitacora v
            LEFT JOIN vt_usuarios u ON v.vit_idUser = u.user_numero
            WHERE v.vit_idFechaMod BETWEEN @Desde AND @Hasta
            ORDER BY v.vit_idFechaMod DESC;";

        try
        {
            return (await _db.QueryAsync<BitacoraAuditoriaDto>(sql, new { Desde = desde.Date, Hasta = hasta.Date.AddDays(1).AddSeconds(-1) })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo consultar la bitácora de auditoría.");
            throw;
        }
    }
}

public class DocumentoDuplicadoException : Exception
{
    public string Campo { get; }
    public DocumentoDuplicadoException(string message, string campo = "DOCUMENTO") : base(message) { Campo = campo; }
}
