using HosvetKardex.Web.Data;
using HosvetKardex.Web.Models;

namespace HosvetKardex.Web.Services;

public interface IFarmaciaService
{
    Task<List<FarmaciaOrdenColaDto>> ObtenerColaDespachoAsync(int idLocalidad, string? filtro = null, string estadoFiltro = "PENDIENTE", string? moduloFiltro = null);
    Task<(bool Exito, string Mensaje)> DespacharItemColaAsync(string origenTabla, int idRegistro, int idLocalidad, int idUsuarioFarmacia, string nombreUsuarioFarmacia);
    Task<(bool Exito, string Mensaje)> DespacharOrdenCompletaAsync(int idOrden, int idLocalidad, int idUsuarioFarmacia, string nombreUsuarioFarmacia);
    Task<(bool Exito, string Mensaje)> CancelarItemColaAsync(string origenTabla, int idRegistro, int idLocalidad, int idUsuarioFarmacia, string nombreUsuarioFarmacia);
    Task<bool> ReservarStockDirectoAsync(string idProductoStr, string codigoProducto, decimal cantidad, int idLocalidad, int idOrden, int nhcl, int idUsuario, string modulo, string bodega);
    Task<bool> LiberarStockDirectoAsync(string codigoProducto, decimal cantidad, int idLocalidad, int idOrden, int idUsuario, string modulo);
    Task<(bool Exito, string Mensaje)> RegistrarVentaMostradorAsync(FarmaciaVentaMostradorInputDto venta, int idLocalidad, int idUsuario, string nombreUsuario, string inicialesUsuario);
    Task<List<FarmaciaVentaHistorialDto>> ObtenerHistorialVentasAsync(int idLocalidad, string? filtro = null);
    Task<List<FarmaciaStockSedeItemDto>> ObtenerStockSedeDetalladoAsync(int idLocalidad, string? filtro = null);
    Task<List<FarmaciaRecetaSoapOrdenDto>> ObtenerRecetasSoapColaAsync(int idLocalidad, string? filtro = null, string estadoFiltro = "PENDIENTE");
    Task<(bool Exito, string Mensaje)> ActualizarReservaRecetaSoapAsync(FarmaciaRecetaSoapOrdenDto receta, int idLocalidad, int idUsuario);
    Task<(bool Exito, string Mensaje)> CompletarVentaRecetaSoapAsync(FarmaciaRecetaSoapOrdenDto receta, int idLocalidad, int idUsuario, string nombreUsuario, string inicialesUsuario);
}

public class FarmaciaService : IFarmaciaService
{
    private readonly ISqlDataService _db;
    private readonly IInventarioService _inventarioService;
    private readonly ILogger<FarmaciaService> _logger;

    public FarmaciaService(ISqlDataService db, IInventarioService inventarioService, ILogger<FarmaciaService> logger)
    {
        _db = db;
        _inventarioService = inventarioService;
        _logger = logger;
    }

    public async Task<List<FarmaciaOrdenColaDto>> ObtenerColaDespachoAsync(
        int idLocalidad,
        string? filtro = null,
        string estadoFiltro = "PENDIENTE",
        string? moduloFiltro = null)
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;

        const string sql = @"
            WITH ExistenciasSede AS (
                SELECT
                    e.codigo_producto,
                    SUM(e.stock_actual) AS StockFisico,
                    SUM(ISNULL(e.stock_reservado, 0)) AS StockReservado,
                    MAX(CASE WHEN b.nombre IS NOT NULL THEN b.nombre + CASE WHEN ISNULL(b.codigo, '') <> '' THEN ' (' + b.codigo + ')' ELSE '' END ELSE '' END) AS BodegaExistencia
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e
                LEFT JOIN dbo.ECONTAX_BODEGA b ON b.id_bodega = e.id_bodega
                WHERE e.id_sucursal = @IdLocalidad
                GROUP BY e.codigo_producto
            ),
            ColaUnificada AS (
                SELECT
                    'MON' AS OrigenTabla,
                    m.id AS IdRegistro,
                    ISNULL(m.ordenId, 0) AS IdOrden,
                    ISNULL(m.nhcl, 0) AS Nhcl,
                    ISNULL(mas.mas_nombre, 'Paciente #' + CAST(ISNULL(m.nhcl, 0) AS VARCHAR(20))) AS Paciente,
                    ISNULL(mas.mas_especie, '') AS Especie,
                    LTRIM(RTRIM(ISNULL(cli.cli_apellidos, '') + ' ' + ISNULL(cli.cli_nombres, ''))) AS Propietario,
                    ISNULL(NULLIF(LTRIM(RTRIM(m.modulo)), ''), 'Monitoreo') AS Modulo,
                    ISNULL(RTRIM(m.codigoprod), '') AS CodigoProducto,
                    COALESCE(TRY_CAST(m.med_iddinamics AS INT), p.CODIGO, 0) AS CodigoProductoInt,
                    ISNULL(m.proddescripcion, '') AS NombreProducto,
                    CAST(ISNULL(m.cantprod, 0) AS DECIMAL(18,4)) AS Cantidad,
                    CAST(ISNULL(m.precioprod, 0) AS DECIMAL(18,4)) AS PrecioUnitario,
                    COALESCE(NULLIF(LTRIM(RTRIM(m.med_medida)), ''), NULLIF(LTRIM(RTRIM(p.UNIDAD_MEDIDA)), ''), 'UNIDAD') AS UnidadMedida,
                    COALESCE(NULLIF(LTRIM(RTRIM(m.med_bodega)), ''), NULLIF(ex.BodegaExistencia, ''), 'Bodega de la sucursal') AS Bodega,
                    ISNULL(m.via, '') AS Via,
                    ISNULL(m.indicacionesprod, '') AS Indicaciones,
                    COALESCE(NULLIF(LTRIM(RTRIM(m.funcionario)), ''), NULLIF(LTRIM(RTRIM(m.iniciales)), ''), 'Veterinario') AS Solicitante,
                    ISNULL(m.fecha, CAST(GETDATE() AS date)) AS Fecha,
                    ISNULL(m.hora, '') AS Hora,
                    CASE WHEN ISNULL(m.grupo, '') = 'ENTREGADO' THEN 'ENTREGADO' ELSE 'PENDIENTE' END AS EstadoFarmacia,
                    ISNULL(m.observaciones, '') AS DetalleEntrega,
                    ISNULL(ex.StockFisico, 0) AS StockFisico,
                    ISNULL(ex.StockReservado, 0) AS StockReservado
                FROM dbo.hvt_cabMonitoreoMedBC m
                LEFT JOIN dbo.vt_mascotas mas ON mas.mas_nhcl = m.nhcl
                LEFT JOIN dbo.vt_clientes cli ON cli.cli_codigo = mas.mas_idcliente
                LEFT JOIN dbo.PRODUCTO p ON p.CODIGO = TRY_CAST(m.med_iddinamics AS INT)
                    OR (TRY_CAST(m.med_iddinamics AS INT) IS NULL AND (p.CODIGO_PRINCIPAL = m.codigoprod OR CAST(p.CODIGO AS VARCHAR(50)) = m.codigoprod))
                LEFT JOIN ExistenciasSede ex ON ex.codigo_producto = COALESCE(TRY_CAST(m.med_iddinamics AS INT), p.CODIGO, 0)
                WHERE ISNULL(m.localidad, @IdLocalidad) = @IdLocalidad
                  AND ISNULL(m.estado, 'A') = 'A'
                  AND ISNULL(m.modulo, 'Monitoreo') <> 'VentaFarmacia'
                  AND ISNULL(m.cantprod, 0) > 0

                UNION ALL

                SELECT
                    'QUI' AS OrigenTabla,
                    q.id AS IdRegistro,
                    ISNULL(q.ordenId, 0) AS IdOrden,
                    ISNULL(q.nhcl, 0) AS Nhcl,
                    ISNULL(mas.mas_nombre, 'Paciente #' + CAST(ISNULL(q.nhcl, 0) AS VARCHAR(20))) AS Paciente,
                    ISNULL(mas.mas_especie, '') AS Especie,
                    LTRIM(RTRIM(ISNULL(cli.cli_apellidos, '') + ' ' + ISNULL(cli.cli_nombres, ''))) AS Propietario,
                    ISNULL(NULLIF(LTRIM(RTRIM(q.modulo)), ''), 'Quirófano') AS Modulo,
                    ISNULL(RTRIM(CONVERT(VARCHAR(50), q.codigoprod)), '') AS CodigoProducto,
                    ISNULL(p.CODIGO, 0) AS CodigoProductoInt,
                    ISNULL(q.proddescripcion, '') AS NombreProducto,
                    CAST(ISNULL(q.cantprod, 0) AS DECIMAL(18,4)) AS Cantidad,
                    CAST(ISNULL(q.precioprod, 0) AS DECIMAL(18,4)) AS PrecioUnitario,
                    ISNULL(NULLIF(LTRIM(RTRIM(p.UNIDAD_MEDIDA)), ''), 'UNIDAD') AS UnidadMedida,
                    COALESCE(NULLIF(ex.BodegaExistencia, ''), 'Bodega de la sucursal') AS Bodega,
                    ISNULL(q.via, '') AS Via,
                    ISNULL(q.indicacionesprod, '') AS Indicaciones,
                    COALESCE(NULLIF(LTRIM(RTRIM(q.funcionario)), ''), NULLIF(LTRIM(RTRIM(q.iniciales)), ''), 'Anestesiólogo') AS Solicitante,
                    ISNULL(q.fecha, CAST(GETDATE() AS date)) AS Fecha,
                    ISNULL(q.hora, '') AS Hora,
                    CASE WHEN ISNULL(q.observaciones, '') LIKE '%[[]FARMACIA:ENTREGADO]%' THEN 'ENTREGADO' ELSE 'PENDIENTE' END AS EstadoFarmacia,
                    REPLACE(ISNULL(q.observaciones, ''), '[FARMACIA:ENTREGADO]', '') AS DetalleEntrega,
                    ISNULL(ex.StockFisico, 0) AS StockFisico,
                    ISNULL(ex.StockReservado, 0) AS StockReservado
                FROM dbo.qvt_productosUtilizados q
                LEFT JOIN dbo.vt_mascotas mas ON mas.mas_nhcl = q.nhcl
                LEFT JOIN dbo.vt_clientes cli ON cli.cli_codigo = mas.mas_idcliente
                LEFT JOIN dbo.PRODUCTO p ON p.CODIGO_PRINCIPAL = CONVERT(VARCHAR(50), q.codigoprod)
                    OR CAST(p.CODIGO AS VARCHAR(50)) = CONVERT(VARCHAR(50), q.codigoprod)
                LEFT JOIN ExistenciasSede ex ON ex.codigo_producto = ISNULL(p.CODIGO, 0)
                WHERE ISNULL(q.localidad, @IdLocalidad) = @IdLocalidad
                  AND ISNULL(q.estado, 'Vigente') IN ('Vigente', 'A')
                  AND ISNULL(q.cantprod, 0) > 0
            )
            SELECT TOP 250 *
            FROM ColaUnificada
            ORDER BY
                CASE WHEN EstadoFarmacia = 'PENDIENTE' THEN 0 ELSE 1 END,
                Fecha DESC,
                IdRegistro DESC;";

        try
        {
            var lista = (await _db.QueryAsync<FarmaciaOrdenColaDto>(sql, new { IdLocalidad = sedeId })).ToList();

            if (!string.IsNullOrWhiteSpace(estadoFiltro) && !string.Equals(estadoFiltro, "TODOS", StringComparison.OrdinalIgnoreCase))
            {
                lista = lista.Where(x => string.Equals(x.EstadoFarmacia, estadoFiltro, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(moduloFiltro) && !string.Equals(moduloFiltro, "TODOS", StringComparison.OrdinalIgnoreCase))
            {
                lista = lista.Where(x => x.Modulo.Contains(moduloFiltro, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var term = filtro.Trim();
                lista = lista.Where(x =>
                    x.NombreProducto.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CodigoProducto.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Paciente.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Propietario.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Solicitante.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Bodega.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.IdOrden.ToString().Contains(term) ||
                    x.Nhcl.ToString().Contains(term)).ToList();
            }

            return lista;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar la cola de despacho de Farmacia en localidad {IdLocalidad}", sedeId);
            return [];
        }
    }

    public async Task<(bool Exito, string Mensaje)> DespacharItemColaAsync(
        string origenTabla,
        int idRegistro,
        int idLocalidad,
        int idUsuarioFarmacia,
        string nombreUsuarioFarmacia)
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;
        string tabla = string.Equals(origenTabla, "QUI", StringComparison.OrdinalIgnoreCase) ? "QUI" : "MON";
        string despachador = string.IsNullOrWhiteSpace(nombreUsuarioFarmacia) ? "Farmacia" : nombreUsuarioFarmacia.Trim();

        const string sql = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @CodProdInt INT = 0;
            DECLARE @CodProdStr VARCHAR(50) = '';
            DECLARE @NombreProd VARCHAR(250) = '';
            DECLARE @Cantidad DECIMAL(18,4) = 0;
            DECLARE @OrdenId INT = 0;
            DECLARE @Nhcl INT = 0;
            DECLARE @Bodega VARCHAR(120) = '';
            DECLARE @Solicitante VARCHAR(120) = '';
            DECLARE @YaEntregado BIT = 0;

            IF @OrigenTabla = 'MON'
            BEGIN
                SELECT TOP 1
                    @CodProdInt = ISNULL(TRY_CAST(med_iddinamics AS INT), 0),
                    @CodProdStr = ISNULL(RTRIM(codigoprod), ''),
                    @NombreProd = ISNULL(proddescripcion, ''),
                    @Cantidad = ISNULL(cantprod, 0),
                    @OrdenId = ISNULL(ordenId, 0),
                    @Nhcl = ISNULL(nhcl, 0),
                    @Bodega = ISNULL(med_bodega, ''),
                    @Solicitante = COALESCE(NULLIF(LTRIM(RTRIM(funcionario)), ''), NULLIF(LTRIM(RTRIM(iniciales)), ''), 'Veterinario'),
                    @YaEntregado = CASE WHEN ISNULL(grupo, '') = 'ENTREGADO' THEN 1 ELSE 0 END
                FROM dbo.hvt_cabMonitoreoMedBC WITH (UPDLOCK, HOLDLOCK)
                WHERE id = @IdRegistro;
            END
            ELSE
            BEGIN
                SELECT TOP 1
                    @CodProdStr = ISNULL(RTRIM(CONVERT(VARCHAR(50), codigoprod)), ''),
                    @NombreProd = ISNULL(proddescripcion, ''),
                    @Cantidad = ISNULL(cantprod, 0),
                    @OrdenId = ISNULL(ordenId, 0),
                    @Nhcl = ISNULL(nhcl, 0),
                    @Solicitante = COALESCE(NULLIF(LTRIM(RTRIM(funcionario)), ''), NULLIF(LTRIM(RTRIM(iniciales)), ''), 'Anestesiólogo'),
                    @YaEntregado = CASE WHEN ISNULL(observaciones, '') LIKE '%[[]FARMACIA:ENTREGADO]%' THEN 1 ELSE 0 END
                FROM dbo.qvt_productosUtilizados WITH (UPDLOCK, HOLDLOCK)
                WHERE id = @IdRegistro;
            END

            IF @Cantidad <= 0
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -1;
                RETURN;
            END

            IF @YaEntregado = 1
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -2;
                RETURN;
            END

            IF @CodProdInt <= 0
            BEGIN
                SELECT TOP 1 @CodProdInt = p.CODIGO
                FROM dbo.PRODUCTO p
                WHERE p.CODIGO_PRINCIPAL = @CodProdStr OR CAST(p.CODIGO AS VARCHAR(50)) = @CodProdStr;
            END

            IF @CodProdInt <= 0
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -3;
                RETURN;
            END

            DECLARE @IdEmpresa INT = NULL;
            DECLARE @StockActual DECIMAL(18,4) = 0;
            DECLARE @StockReservado DECIMAL(18,4) = 0;
            DECLARE @CostoPromedio DECIMAL(18,4) = 0;

            SELECT TOP 1
                @IdEmpresa = e.id_empresa,
                @StockActual = e.stock_actual,
                @StockReservado = ISNULL(e.stock_reservado, 0),
                @CostoPromedio = ISNULL(e.costo_promedio, 0)
            FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e WITH (UPDLOCK, HOLDLOCK)
            WHERE e.codigo_producto = @CodProdInt
              AND e.id_sucursal = @IdLocalidad
            ORDER BY e.stock_actual DESC;

            IF @IdEmpresa IS NULL OR @StockActual < @Cantidad
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -4;
                RETURN;
            END

            DECLARE @NuevoStock DECIMAL(18,4) = @StockActual - @Cantidad;
            DECLARE @NuevoReservado DECIMAL(18,4) = CASE WHEN @StockReservado - @Cantidad < 0 THEN 0 ELSE @StockReservado - @Cantidad END;

            -- Descargar físicamente stock_actual y liberar stock_reservado
            UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
            SET stock_actual = @NuevoStock,
                stock_reservado = @NuevoReservado,
                fecha_actualizacion = SYSUTCDATETIME()
            WHERE codigo_producto = @CodProdInt
              AND id_sucursal = @IdLocalidad
              AND id_empresa = @IdEmpresa;

            -- Descontar de lotes disponibles (FIFO)
            DECLARE @RestanteLote DECIMAL(18,4) = @Cantidad;
            DECLARE @IdLote BIGINT, @DispLote DECIMAL(18,4), @TomarLote DECIMAL(18,4);
            WHILE @RestanteLote > 0
            BEGIN
                SET @IdLote = NULL;
                SELECT TOP 1 @IdLote = l.id_lote, @DispLote = l.cantidad_disponible
                FROM dbo.ECONTAX_INVENTARIO_LOTE l WITH (UPDLOCK, HOLDLOCK)
                WHERE l.codigo_producto = @CodProdInt
                  AND l.id_sucursal = @IdLocalidad
                  AND l.id_empresa = @IdEmpresa
                  AND l.cantidad_disponible > 0
                ORDER BY ISNULL(l.fecha_vencimiento, '9999-12-31'), l.fecha_ingreso, l.id_lote;

                IF @IdLote IS NULL BREAK;

                SET @TomarLote = CASE WHEN @DispLote <= @RestanteLote THEN @DispLote ELSE @RestanteLote END;
                UPDATE dbo.ECONTAX_INVENTARIO_LOTE
                SET cantidad_disponible = cantidad_disponible - @TomarLote
                WHERE id_lote = @IdLote;

                SET @RestanteLote = @RestanteLote - @TomarLote;
            END

            DECLARE @SelloEntrega VARCHAR(300) = 'Entregado por ' + @Despachador + ' (' + CONVERT(VARCHAR(16), GETDATE(), 120) + ')';

            -- Registrar movimiento de SALIDA oficial en el Kardex
            INSERT INTO dbo.ECONTAX_INVENTARIO_MOVIMIENTO (
                codigo_producto, id_empresa, id_sucursal, tipo_movimiento,
                cantidad, costo_unitario, costo_total, saldo_cantidad, saldo_costo,
                metodo_valoracion, referencia_tipo, referencia, id_usuario, fecha_movimiento, observacion
            ) VALUES (
                @CodProdInt, @IdEmpresa, @IdLocalidad, 'SALIDA',
                @Cantidad, @CostoPromedio, ROUND(@Cantidad * @CostoPromedio, 4), @NuevoStock, ROUND(@NuevoStock * @CostoPromedio, 4),
                'PROMEDIO', 'FARMACIA_DESPACHO', 'ORDEN:' + CAST(@OrdenId AS VARCHAR(20)), @IdUsuario, SYSUTCDATETIME(),
                'Despacho de Farmacia a ' + @Solicitante + ' · Orden #' + CAST(@OrdenId AS VARCHAR(20)) + ' NHCL #' + CAST(@Nhcl AS VARCHAR(20)) +
                CASE WHEN @Bodega <> '' THEN ' · Bodega: ' + @Bodega ELSE '' END + ' · ' + @SelloEntrega
            );

            IF @OrigenTabla = 'MON'
            BEGIN
                UPDATE dbo.hvt_cabMonitoreoMedBC
                SET grupo = 'ENTREGADO',
                    observaciones = @SelloEntrega
                WHERE id = @IdRegistro;
            END
            ELSE
            BEGIN
                UPDATE dbo.qvt_productosUtilizados
                SET observaciones = '[FARMACIA:ENTREGADO] ' + @SelloEntrega
                WHERE id = @IdRegistro;
            END

            COMMIT TRANSACTION;
            SELECT 1;";

        try
        {
            int code = await _db.ExecuteScalarAsync<int>(sql, new
            {
                OrigenTabla = tabla,
                IdRegistro = idRegistro,
                IdLocalidad = sedeId,
                IdUsuario = idUsuarioFarmacia > 0 ? idUsuarioFarmacia : 1,
                Despachador = despachador
            });

            if (code == -2)
                return (false, "Esta orden de medicamento ya fue entregada previamente.");
            if (code == -4)
                return (false, "Stock físico insuficiente en la sucursal para completar la entrega.");
            if (code <= 0)
                return (false, "No se encontró el registro solicitado en la cola de Farmacia.");

            _inventarioService.InvalidarCacheCatalogo();
            return (true, "Medicamento entregado al veterinario y descargado definitivamente del inventario/Kardex.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al despachar ítem {IdRegistro} ({OrigenTabla}) en Farmacia", idRegistro, tabla);
            return (false, $"Error al despachar medicamento: {ex.Message}");
        }
    }

    public async Task<(bool Exito, string Mensaje)> DespacharOrdenCompletaAsync(
        int idOrden,
        int idLocalidad,
        int idUsuarioFarmacia,
        string nombreUsuarioFarmacia)
    {
        var pendientes = (await ObtenerColaDespachoAsync(idLocalidad, null, "PENDIENTE", null))
            .Where(x => x.IdOrden == idOrden)
            .ToList();

        if (pendientes.Count == 0)
            return (false, $"La Orden #{idOrden} no tiene medicamentos pendientes en la cola.");

        int entregados = 0;
        string? ultimoError = null;
        foreach (var item in pendientes)
        {
            var res = await DespacharItemColaAsync(item.OrigenTabla, item.IdRegistro, idLocalidad, idUsuarioFarmacia, nombreUsuarioFarmacia);
            if (res.Exito) entregados++;
            else ultimoError = res.Mensaje;
        }

        return entregados > 0
            ? (true, $"Se entregaron y descargaron {entregados} medicamento(s) de la Orden #{idOrden}.")
            : (false, ultimoError ?? "No se pudieron despachar los medicamentos de la orden.");
    }

    public async Task<(bool Exito, string Mensaje)> CancelarItemColaAsync(
        string origenTabla,
        int idRegistro,
        int idLocalidad,
        int idUsuarioFarmacia,
        string nombreUsuarioFarmacia)
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;
        string tabla = string.Equals(origenTabla, "QUI", StringComparison.OrdinalIgnoreCase) ? "QUI" : "MON";

        const string sql = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @CodProdInt INT = 0;
            DECLARE @CodProdStr VARCHAR(50) = '';
            DECLARE @Cantidad DECIMAL(18,4) = 0;
            DECLARE @OrdenId INT = 0;
            DECLARE @YaEntregado BIT = 0;

            IF @OrigenTabla = 'MON'
            BEGIN
                SELECT TOP 1
                    @CodProdInt = ISNULL(TRY_CAST(med_iddinamics AS INT), 0),
                    @CodProdStr = ISNULL(RTRIM(codigoprod), ''),
                    @Cantidad = ISNULL(cantprod, 0),
                    @OrdenId = ISNULL(ordenId, 0),
                    @YaEntregado = CASE WHEN ISNULL(grupo, '') = 'ENTREGADO' THEN 1 ELSE 0 END
                FROM dbo.hvt_cabMonitoreoMedBC WITH (UPDLOCK, HOLDLOCK)
                WHERE id = @IdRegistro;
            END
            ELSE
            BEGIN
                SELECT TOP 1
                    @CodProdStr = ISNULL(RTRIM(CONVERT(VARCHAR(50), codigoprod)), ''),
                    @Cantidad = ISNULL(cantprod, 0),
                    @OrdenId = ISNULL(ordenId, 0),
                    @YaEntregado = CASE WHEN ISNULL(observaciones, '') LIKE '%[[]FARMACIA:ENTREGADO]%' THEN 1 ELSE 0 END
                FROM dbo.qvt_productosUtilizados WITH (UPDLOCK, HOLDLOCK)
                WHERE id = @IdRegistro;
            END

            IF @Cantidad <= 0 OR @YaEntregado = 1
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -1;
                RETURN;
            END

            IF @CodProdInt <= 0
            BEGIN
                SELECT TOP 1 @CodProdInt = p.CODIGO
                FROM dbo.PRODUCTO p
                WHERE p.CODIGO_PRINCIPAL = @CodProdStr OR CAST(p.CODIGO AS VARCHAR(50)) = @CodProdStr;
            END

            IF @CodProdInt > 0
            BEGIN
                DECLARE @IdEmpresa INT, @StockActual DECIMAL(18,4), @StockReservado DECIMAL(18,4), @CostoPromedio DECIMAL(18,4);
                SELECT TOP 1
                    @IdEmpresa = e.id_empresa,
                    @StockActual = e.stock_actual,
                    @StockReservado = ISNULL(e.stock_reservado, 0),
                    @CostoPromedio = ISNULL(e.costo_promedio, 0)
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e WITH (UPDLOCK, HOLDLOCK)
                WHERE e.codigo_producto = @CodProdInt AND e.id_sucursal = @IdLocalidad;

                IF @IdEmpresa IS NOT NULL
                BEGIN
                    DECLARE @NuevoReservado DECIMAL(18,4) = CASE WHEN @StockReservado - @Cantidad < 0 THEN 0 ELSE @StockReservado - @Cantidad END;
                    UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
                    SET stock_reservado = @NuevoReservado,
                        fecha_actualizacion = SYSUTCDATETIME()
                    WHERE codigo_producto = @CodProdInt AND id_sucursal = @IdLocalidad AND id_empresa = @IdEmpresa;

                    INSERT INTO dbo.ECONTAX_INVENTARIO_MOVIMIENTO (
                        codigo_producto, id_empresa, id_sucursal, tipo_movimiento,
                        cantidad, costo_unitario, costo_total, saldo_cantidad, saldo_costo,
                        metodo_valoracion, referencia_tipo, referencia, id_usuario, fecha_movimiento, observacion
                    ) VALUES (
                        @CodProdInt, @IdEmpresa, @IdLocalidad, 'RESERVA_LIBERADA',
                        @Cantidad, @CostoPromedio, ROUND(@Cantidad * @CostoPromedio, 4), @StockActual, ROUND(@StockActual * @CostoPromedio, 4),
                        'PROMEDIO', 'FARMACIA_COLA', 'ORDEN:' + CAST(@OrdenId AS VARCHAR(20)), @IdUsuario, SYSUTCDATETIME(),
                        'Reserva liberada/cancelada en Farmacia por ' + @UsuarioNombre + ' · Orden #' + CAST(@OrdenId AS VARCHAR(20))
                    );
                END
            END

            IF @OrigenTabla = 'MON'
                DELETE FROM dbo.hvt_cabMonitoreoMedBC WHERE id = @IdRegistro;
            ELSE
                DELETE FROM dbo.qvt_productosUtilizados WHERE id = @IdRegistro;

            COMMIT TRANSACTION;
            SELECT 1;";

        try
        {
            int ok = await _db.ExecuteScalarAsync<int>(sql, new
            {
                OrigenTabla = tabla,
                IdRegistro = idRegistro,
                IdLocalidad = sedeId,
                IdUsuario = idUsuarioFarmacia > 0 ? idUsuarioFarmacia : 1,
                UsuarioNombre = string.IsNullOrWhiteSpace(nombreUsuarioFarmacia) ? "Farmacia" : nombreUsuarioFarmacia.Trim()
            });

            if (ok <= 0)
                return (false, "No se pudo liberar la solicitud (verifica que siga en estado pendiente).");

            _inventarioService.InvalidarCacheCatalogo();
            return (true, "Solicitud cancelada y unidades reservadas liberadas nuevamente al stock disponible.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar ítem {IdRegistro} en Farmacia", idRegistro);
            return (false, $"Error al liberar reserva: {ex.Message}");
        }
    }

    public async Task<bool> ReservarStockDirectoAsync(
        string idProductoStr,
        string codigoProducto,
        decimal cantidad,
        int idLocalidad,
        int idOrden,
        int nhcl,
        int idUsuario,
        string modulo,
        string bodega)
    {
        if (cantidad <= 0) return false;
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;

        const string sql = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @CodProdInt INT = TRY_CAST(@IdProductoStr AS INT);
            IF @CodProdInt IS NULL OR @CodProdInt <= 0
            BEGIN
                SELECT TOP 1 @CodProdInt = p.CODIGO
                FROM dbo.PRODUCTO p
                WHERE p.CODIGO_PRINCIPAL = @CodigoProducto OR CAST(p.CODIGO AS VARCHAR(50)) = @CodigoProducto;
            END

            IF @CodProdInt IS NULL OR @CodProdInt <= 0
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0;
                RETURN;
            END

            DECLARE @IdEmpresa INT, @StockActual DECIMAL(18,4), @StockReservado DECIMAL(18,4), @CostoPromedio DECIMAL(18,4);
            SELECT TOP 1
                @IdEmpresa = e.id_empresa,
                @StockActual = e.stock_actual,
                @StockReservado = ISNULL(e.stock_reservado, 0),
                @CostoPromedio = ISNULL(e.costo_promedio, 0)
            FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e WITH (UPDLOCK, HOLDLOCK)
            WHERE e.codigo_producto = @CodProdInt AND e.id_sucursal = @IdLocalidad;

            IF @IdEmpresa IS NULL OR (@StockActual - @StockReservado) < @Cantidad
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0;
                RETURN;
            END

            UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
            SET stock_reservado = @StockReservado + @Cantidad,
                fecha_actualizacion = SYSUTCDATETIME()
            WHERE codigo_producto = @CodProdInt AND id_sucursal = @IdLocalidad AND id_empresa = @IdEmpresa;

            INSERT INTO dbo.ECONTAX_INVENTARIO_MOVIMIENTO (
                codigo_producto, id_empresa, id_sucursal, tipo_movimiento,
                cantidad, costo_unitario, costo_total, saldo_cantidad, saldo_costo,
                metodo_valoracion, referencia_tipo, referencia, id_usuario, fecha_movimiento, observacion
            ) VALUES (
                @CodProdInt, @IdEmpresa, @IdLocalidad, 'RESERVA',
                @Cantidad, @CostoPromedio, ROUND(@Cantidad * @CostoPromedio, 4), @StockActual, ROUND(@StockActual * @CostoPromedio, 4),
                'PROMEDIO', 'FARMACIA_COLA', 'ORDEN:' + CAST(@IdOrden AS VARCHAR(20)), @IdUsuario, SYSUTCDATETIME(),
                'En cola de Farmacia · ' + @Modulo + ' Orden #' + CAST(@IdOrden AS VARCHAR(20)) + ' NHCL #' + CAST(@Nhcl AS VARCHAR(20)) +
                CASE WHEN ISNULL(@Bodega, '') <> '' THEN ' · Bodega: ' + @Bodega ELSE '' END
            );

            COMMIT TRANSACTION;
            SELECT 1;";

        try
        {
            var ok = await _db.ExecuteScalarAsync<int>(sql, new
            {
                IdProductoStr = idProductoStr,
                CodigoProducto = codigoProducto,
                Cantidad = cantidad,
                IdLocalidad = sedeId,
                IdOrden = idOrden,
                Nhcl = nhcl,
                IdUsuario = idUsuario > 0 ? idUsuario : 1,
                Modulo = modulo,
                Bodega = bodega
            }) > 0;
            if (ok) _inventarioService.InvalidarCacheCatalogo();
            return ok;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LiberarStockDirectoAsync(
        string codigoProducto,
        decimal cantidad,
        int idLocalidad,
        int idOrden,
        int idUsuario,
        string modulo)
    {
        if (cantidad <= 0) return false;
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;

        const string sql = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @CodProdInt INT = NULL;
            SELECT TOP 1 @CodProdInt = p.CODIGO
            FROM dbo.PRODUCTO p
            WHERE p.CODIGO_PRINCIPAL = @CodigoProducto OR CAST(p.CODIGO AS VARCHAR(50)) = @CodigoProducto;

            IF @CodProdInt > 0
            BEGIN
                DECLARE @IdEmpresa INT, @StockActual DECIMAL(18,4), @StockReservado DECIMAL(18,4), @CostoPromedio DECIMAL(18,4);
                SELECT TOP 1
                    @IdEmpresa = e.id_empresa,
                    @StockActual = e.stock_actual,
                    @StockReservado = ISNULL(e.stock_reservado, 0),
                    @CostoPromedio = ISNULL(e.costo_promedio, 0)
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e WITH (UPDLOCK, HOLDLOCK)
                WHERE e.codigo_producto = @CodProdInt AND e.id_sucursal = @IdLocalidad;

                IF @IdEmpresa IS NOT NULL
                BEGIN
                    UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
                    SET stock_reservado = CASE WHEN @StockReservado - @Cantidad < 0 THEN 0 ELSE @StockReservado - @Cantidad END,
                        fecha_actualizacion = SYSUTCDATETIME()
                    WHERE codigo_producto = @CodProdInt AND id_sucursal = @IdLocalidad AND id_empresa = @IdEmpresa;

                    INSERT INTO dbo.ECONTAX_INVENTARIO_MOVIMIENTO (
                        codigo_producto, id_empresa, id_sucursal, tipo_movimiento,
                        cantidad, costo_unitario, costo_total, saldo_cantidad, saldo_costo,
                        metodo_valoracion, referencia_tipo, referencia, id_usuario, fecha_movimiento, observacion
                    ) VALUES (
                        @CodProdInt, @IdEmpresa, @IdLocalidad, 'RESERVA_LIBERADA',
                        @Cantidad, @CostoPromedio, ROUND(@Cantidad * @CostoPromedio, 4), @StockActual, ROUND(@StockActual * @CostoPromedio, 4),
                        'PROMEDIO', 'FARMACIA_COLA', 'ORDEN:' + CAST(@IdOrden AS VARCHAR(20)), @IdUsuario, SYSUTCDATETIME(),
                        'Liberación de reserva · ' + @Modulo + ' Orden #' + CAST(@IdOrden AS VARCHAR(20))
                    );
                END
            END

            COMMIT TRANSACTION;
            SELECT 1;";

        try
        {
            var ok = await _db.ExecuteScalarAsync<int>(sql, new
            {
                CodigoProducto = codigoProducto,
                Cantidad = cantidad,
                IdLocalidad = sedeId,
                IdOrden = idOrden,
                IdUsuario = idUsuario > 0 ? idUsuario : 1,
                Modulo = modulo
            }) > 0;
            if (ok) _inventarioService.InvalidarCacheCatalogo();
            return ok;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Exito, string Mensaje)> RegistrarVentaMostradorAsync(
        FarmaciaVentaMostradorInputDto venta,
        int idLocalidad,
        int idUsuario,
        string nombreUsuario,
        string inicialesUsuario)
    {
        if (venta.Lineas.Count == 0 || venta.Lineas.Any(l => l.CantidadBase <= 0))
            return (false, "Agregue al menos un producto con cantidad válida para realizar la venta/despacho.");

        int sedeId = idLocalidad > 0 ? idLocalidad : 1;
        string despachador = string.IsNullOrWhiteSpace(nombreUsuario) ? "Farmacia" : nombreUsuario.Trim();
        string iniciales = string.IsNullOrWhiteSpace(inicialesUsuario) ? "FAR" : inicialesUsuario.Trim();
        if (iniciales.Length > 5) iniciales = iniciales[..5];
        string clienteRef = string.IsNullOrWhiteSpace(venta.ClienteReferencia) ? "Mostrador / Venta directa" : venta.ClienteReferencia.Trim();

        const string sqlLinea = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @CodProdInt INT = TRY_CAST(@IdProducto AS INT);
            IF @CodProdInt IS NULL OR @CodProdInt <= 0
            BEGIN
                SELECT TOP 1 @CodProdInt = p.CODIGO
                FROM dbo.PRODUCTO p
                WHERE p.CODIGO_PRINCIPAL = @CodigoProducto OR CAST(p.CODIGO AS VARCHAR(50)) = @CodigoProducto;
            END

            DECLARE @IdEmpresa INT = NULL;
            DECLARE @StockActual DECIMAL(18,4) = 0;
            DECLARE @StockReservado DECIMAL(18,4) = 0;
            DECLARE @CostoPromedio DECIMAL(18,4) = 0;

            IF @CodProdInt > 0
            BEGIN
                SELECT TOP 1
                    @IdEmpresa = e.id_empresa,
                    @StockActual = e.stock_actual,
                    @StockReservado = ISNULL(e.stock_reservado, 0),
                    @CostoPromedio = ISNULL(e.costo_promedio, 0)
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e WITH (UPDLOCK, HOLDLOCK)
                WHERE e.codigo_producto = @CodProdInt
                  AND e.id_sucursal = @IdLocalidad
                ORDER BY e.stock_actual DESC;
            END

            IF @IdEmpresa IS NULL OR (@StockActual - @StockReservado) < @CantidadBase
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -2;
                RETURN;
            END

            DECLARE @NuevoStock DECIMAL(18,4) = @StockActual - @CantidadBase;

            UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
            SET stock_actual = @NuevoStock,
                fecha_actualizacion = SYSUTCDATETIME()
            WHERE codigo_producto = @CodProdInt
              AND id_sucursal = @IdLocalidad
              AND id_empresa = @IdEmpresa;

            -- Descontar de lotes disponibles (FIFO)
            DECLARE @RestanteLote DECIMAL(18,4) = @CantidadBase;
            DECLARE @IdLote BIGINT, @DispLote DECIMAL(18,4), @TomarLote DECIMAL(18,4);
            WHILE @RestanteLote > 0
            BEGIN
                SET @IdLote = NULL;
                SELECT TOP 1 @IdLote = l.id_lote, @DispLote = l.cantidad_disponible
                FROM dbo.ECONTAX_INVENTARIO_LOTE l WITH (UPDLOCK, HOLDLOCK)
                WHERE l.codigo_producto = @CodProdInt
                  AND l.id_sucursal = @IdLocalidad
                  AND l.id_empresa = @IdEmpresa
                  AND l.cantidad_disponible > 0
                ORDER BY ISNULL(l.fecha_vencimiento, '9999-12-31'), l.fecha_ingreso, l.id_lote;

                IF @IdLote IS NULL BREAK;

                SET @TomarLote = CASE WHEN @DispLote <= @RestanteLote THEN @DispLote ELSE @RestanteLote END;
                UPDATE dbo.ECONTAX_INVENTARIO_LOTE
                SET cantidad_disponible = cantidad_disponible - @TomarLote
                WHERE id_lote = @IdLote;

                SET @RestanteLote = @RestanteLote - @TomarLote;
            END

            INSERT INTO dbo.ECONTAX_INVENTARIO_MOVIMIENTO (
                codigo_producto, id_empresa, id_sucursal, tipo_movimiento,
                cantidad, costo_unitario, costo_total, saldo_cantidad, saldo_costo,
                metodo_valoracion, referencia_tipo, referencia, id_usuario, fecha_movimiento, observacion
            ) VALUES (
                @CodProdInt, @IdEmpresa, @IdLocalidad, 'SALIDA',
                @CantidadBase, @CostoPromedio, ROUND(@CantidadBase * @CostoPromedio, 4), @NuevoStock, ROUND(@NuevoStock * @CostoPromedio, 4),
                'PROMEDIO', 'FARMACIA_VENTA', @Referencia, @IdUsuario, SYSUTCDATETIME(),
                'Venta/Despacho directo en Farmacia · Cliente: ' + @ClienteRef +
                CASE WHEN ISNULL(@Bodega, '') <> '' THEN ' · Bodega: ' + @Bodega ELSE '' END +
                ' · Atendido por: ' + @Despachador
            );

            INSERT INTO dbo.hvt_cabMonitoreoMedBC (
                ordenId, nhcl, hora, fecha, codigoprod, precioprod, stockprod,
                indicacionesprod, cantprod, usuario, localidad, proddescripcion,
                via, origen, iniciales, funcion, funcionario, estado, grupo, modulo,
                med_iva, med_iddinamics, med_medida, med_bodega, observaciones
            ) VALUES (
                @OrdenId, @Nhcl, CONVERT(VARCHAR(5), GETDATE(), 108), CAST(GETDATE() AS date), @CodigoProducto, @PrecioUnitario, CAST(@NuevoStock AS INT),
                @Indicaciones, @CantidadBase, @IdUsuario, @IdLocalidad, @NombreProducto,
                'MOSTRADOR', 'F', @Iniciales, 'Farmacia', @Despachador, 'A', 'ENTREGADO', 'VentaFarmacia',
                @Iva, CAST(@CodProdInt AS VARCHAR(50)), @UnidadMedida, @Bodega, @ClienteRef
            );

            COMMIT TRANSACTION;
            SELECT 1;";

        try
        {
            int procesadas = 0;
            string referencia = venta.IdOrden is > 0 ? $"ORDEN:{venta.IdOrden.Value}" : $"VENTA:{DateTime.Now:yyyyMMddHHmmss}";

            foreach (var linea in venta.Lineas)
            {
                int res = await _db.ExecuteScalarAsync<int>(sqlLinea, new
                {
                    linea.IdProducto,
                    linea.CodigoProducto,
                    linea.NombreProducto,
                    linea.CantidadBase,
                    linea.PrecioUnitario,
                    linea.UnidadMedida,
                    linea.Bodega,
                    linea.Iva,
                    Indicaciones = string.IsNullOrWhiteSpace(linea.Indicaciones) ? venta.Observacion : linea.Indicaciones,
                    OrdenId = venta.IdOrden ?? 0,
                    Nhcl = venta.Nhcl ?? 0,
                    IdLocalidad = sedeId,
                    IdUsuario = idUsuario > 0 ? idUsuario : 1,
                    Despachador = despachador,
                    Iniciales = iniciales,
                    ClienteRef = clienteRef,
                    Referencia = referencia
                });

                if (res == -2)
                    return (false, $"Stock disponible insuficiente para '{linea.NombreProducto}'.");
                if (res > 0)
                    procesadas++;
            }

            _inventarioService.InvalidarCacheCatalogo();
            return (true, $"Venta/despacho de Farmacia registrado correctamente ({procesadas} producto(s) descargados del inventario).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar venta de mostrador en Farmacia");
            return (false, $"Error al registrar la venta de Farmacia: {ex.Message}");
        }
    }

    public async Task<List<FarmaciaVentaHistorialDto>> ObtenerHistorialVentasAsync(int idLocalidad, string? filtro = null)
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;
        const string sql = @"
            SELECT TOP 150
                m.id AS Id,
                ISNULL(m.ordenId, 0) AS IdOrden,
                ISNULL(m.nhcl, 0) AS Nhcl,
                ISNULL(m.fecha, CAST(GETDATE() AS date)) AS Fecha,
                ISNULL(m.hora, '') AS Hora,
                ISNULL(RTRIM(m.codigoprod), '') AS CodigoProducto,
                ISNULL(m.proddescripcion, '') AS NombreProducto,
                CAST(ISNULL(m.cantprod, 0) AS DECIMAL(18,4)) AS Cantidad,
                ISNULL(NULLIF(LTRIM(RTRIM(m.med_medida)), ''), 'UNIDAD') AS UnidadMedida,
                CAST(ISNULL(m.precioprod, 0) AS DECIMAL(18,4)) AS PrecioUnitario,
                ISNULL(m.med_bodega, '') AS Bodega,
                ISNULL(m.observaciones, 'Mostrador') AS ClienteReferencia,
                ISNULL(m.funcionario, 'Farmacia') AS DespachadoPor
            FROM dbo.hvt_cabMonitoreoMedBC m
            WHERE ISNULL(m.localidad, @IdLocalidad) = @IdLocalidad
              AND m.modulo = 'VentaFarmacia'
              AND ISNULL(m.estado, 'A') = 'A'
            ORDER BY m.id DESC;";

        try
        {
            var lista = (await _db.QueryAsync<FarmaciaVentaHistorialDto>(sql, new { IdLocalidad = sedeId })).ToList();
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var term = filtro.Trim();
                lista = lista.Where(x =>
                    x.NombreProducto.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CodigoProducto.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.ClienteReferencia.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Bodega.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return lista;
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<FarmaciaStockSedeItemDto>> ObtenerStockSedeDetalladoAsync(int idLocalidad, string? filtro = null)
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;
        const string sql = @"
            SELECT
                p.CODIGO AS CodigoProducto,
                ISNULL(NULLIF(LTRIM(RTRIM(p.CODIGO_PRINCIPAL)), ''), CAST(p.CODIGO AS VARCHAR(50))) AS CodigoPrincipal,
                ISNULL(p.NOMBRE, '') AS NombreProducto,
                ISNULL(NULLIF(LTRIM(RTRIM(p.UNIDAD_MEDIDA)), ''), 'UNIDAD') AS UnidadMedida,
                CAST(CASE WHEN ISNULL(p.FACTOR_CONVERSION_INVENTARIO, 0) <= 0 THEN 1 ELSE p.FACTOR_CONVERSION_INVENTARIO END AS DECIMAL(18,4)) AS FactorConversion,
                COALESCE(
                    NULLIF(MAX(CASE WHEN b.nombre IS NOT NULL THEN b.nombre + CASE WHEN ISNULL(b.codigo, '') <> '' THEN ' (' + b.codigo + ')' ELSE '' END ELSE '' END), ''),
                    (
                        SELECT STRING_AGG(bs.nombre + CASE WHEN ISNULL(bs.codigo, '') <> '' THEN ' (' + bs.codigo + ')' ELSE '' END, ', ')
                        FROM dbo.ECONTAX_BODEGA bs
                        WHERE bs.id_sucursal = @IdLocalidad AND bs.estado = 1
                    ),
                    'Bodega de la sucursal'
                ) AS Bodega,
                CAST(SUM(e.stock_actual) AS DECIMAL(18,4)) AS StockFisico,
                CAST(SUM(ISNULL(e.stock_reservado, 0)) AS DECIMAL(18,4)) AS StockReservado,
                CAST(MAX(CASE WHEN ISNULL(p.VALOR_UNITARIO, 0) > 0 THEN p.VALOR_UNITARIO ELSE ISNULL(e.costo_promedio, 0) END) AS DECIMAL(18,4)) AS PrecioUnitario,
                CAST(MAX(ISNULL(e.costo_promedio, 0)) AS DECIMAL(18,4)) AS CostoPromedio
            FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e
            INNER JOIN dbo.PRODUCTO p ON p.CODIGO = e.codigo_producto
            LEFT JOIN dbo.ECONTAX_BODEGA b ON b.id_bodega = e.id_bodega
            WHERE e.id_sucursal = @IdLocalidad
              AND ISNULL(p.ESTADO, 1) <> 0
              AND ISNULL(UPPER(p.TIPOCOMPRAVENA), 'B') NOT IN ('SERVICIO', 'S')
            GROUP BY
                p.CODIGO,
                p.CODIGO_PRINCIPAL,
                p.NOMBRE,
                p.UNIDAD_MEDIDA,
                p.FACTOR_CONVERSION_INVENTARIO
            HAVING SUM(e.stock_actual) > 0 OR SUM(ISNULL(e.stock_reservado, 0)) > 0
            ORDER BY p.NOMBRE;";

        try
        {
            var lista = (await _db.QueryAsync<FarmaciaStockSedeItemDto>(sql, new { IdLocalidad = sedeId })).ToList();
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var term = filtro.Trim();
                lista = lista.Where(x =>
                    x.NombreProducto.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CodigoPrincipal.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Bodega.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return lista;
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<FarmaciaRecetaSoapOrdenDto>> ObtenerRecetasSoapColaAsync(
        int idLocalidad,
        string? filtro = null,
        string estadoFiltro = "PENDIENTE")
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;

        const string sql = @"
            WITH ExistenciasSede AS (
                SELECT
                    e.codigo_producto,
                    SUM(e.stock_actual) AS StockFisico,
                    SUM(ISNULL(e.stock_reservado, 0)) AS StockReservado,
                    MAX(ISNULL(e.costo_promedio, 0)) AS CostoPromedio,
                    MAX(CASE WHEN b.nombre IS NOT NULL THEN b.nombre + CASE WHEN ISNULL(b.codigo, '') <> '' THEN ' (' + b.codigo + ')' ELSE '' END ELSE '' END) AS BodegaExistencia
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e
                LEFT JOIN dbo.ECONTAX_BODEGA b ON b.id_bodega = e.id_bodega
                WHERE e.id_sucursal = @IdLocalidad
                GROUP BY e.codigo_producto
            ),
            BodegaPorDefecto AS (
                SELECT STRING_AGG(bs.nombre + CASE WHEN ISNULL(bs.codigo, '') <> '' THEN ' (' + bs.codigo + ')' ELSE '' END, ', ') AS NombreBodegaSede
                FROM dbo.ECONTAX_BODEGA bs
                WHERE bs.id_sucursal = @IdLocalidad AND bs.estado = 1
            )
            SELECT TOP 300
                m.sec AS Sec,
                ISNULL(m.med_id_orden, 0) AS IdOrden,
                ISNULL(m.med_nhcl, 0) AS Nhcl,
                ISNULL(mas.mas_nombre, ISNULL(o.ordenAten_nombre, 'Paciente #' + CAST(ISNULL(m.med_nhcl, 0) AS VARCHAR(20)))) AS Paciente,
                ISNULL(mas.mas_especie, '') AS Especie,
                LTRIM(RTRIM(ISNULL(cli.cli_apellidos, '') + ' ' + ISNULL(cli.cli_nombres, ''))) AS Propietario,
                ISNULL(u.user_nombre, 'Médico Veterinario') AS Medico,
                ISNULL(m.med_fecha, GETDATE()) AS Fecha,
                COALESCE(NULLIF(m.med_idProdu, 0), pMatch.CODIGO, 0) AS CodigoProductoInt,
                ISNULL(COALESCE(p.CODIGO_PRINCIPAL, pMatch.CODIGO_PRINCIPAL), '') AS CodigoPrincipal,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(ISNULL(m.med_otroMedicamento, ''), CHAR(13), ''), CHAR(10), ''))), ''),
                    NULLIF(LTRIM(RTRIM(ISNULL(p.NOMBRE, ''))), ''),
                    'Medicamento recetado'
                ) AS NombreRecetado,
                ISNULL(COALESCE(p.NOMBRE, pMatch.NOMBRE), '') AS NombreProductoInventario,
                ISNULL(m.med_indicaciones, '') AS Indicaciones,
                CAST(ISNULL(m.med_cantidad, 1) AS DECIMAL(18,4)) AS CantidadRecetada,
                ISNULL(m.med_cantidadLetras, '') AS CantidadLetras,
                CAST(ISNULL(m.med_cantidad1, 0) AS DECIMAL(18,4)) AS CantidadReservadaLinea,
                ISNULL(NULLIF(LTRIM(RTRIM(m.med_estado)), ''), 'Por Entregar') AS Estado,
                ISNULL(m.med_observaciones, '') AS Observaciones,
                ISNULL(NULLIF(LTRIM(RTRIM(COALESCE(p.UNIDAD_MEDIDA, pMatch.UNIDAD_MEDIDA))), ''), 'UNIDAD') AS UnidadMedida,
                CAST(CASE WHEN ISNULL(COALESCE(p.FACTOR_CONVERSION_INVENTARIO, pMatch.FACTOR_CONVERSION_INVENTARIO), 0) <= 0 THEN 1 ELSE COALESCE(p.FACTOR_CONVERSION_INVENTARIO, pMatch.FACTOR_CONVERSION_INVENTARIO) END AS DECIMAL(18,4)) AS FactorConversion,
                COALESCE(NULLIF(ex.BodegaExistencia, ''), NULLIF((SELECT TOP 1 NombreBodegaSede FROM BodegaPorDefecto), ''), 'Bodega de la sucursal') AS Bodega,
                CAST(CASE WHEN ISNULL(COALESCE(p.VALOR_UNITARIO, pMatch.VALOR_UNITARIO), 0) > 0 THEN COALESCE(p.VALOR_UNITARIO, pMatch.VALOR_UNITARIO) ELSE ISNULL(ex.CostoPromedio, 0) END AS DECIMAL(18,4)) AS PrecioUnitario,
                CAST(ISNULL(ex.StockFisico, 0) AS DECIMAL(18,4)) AS StockFisicoSede,
                CAST(ISNULL(ex.StockReservado, 0) AS DECIMAL(18,4)) AS StockReservadoTotalSede
            FROM dbo.vt_medicinas m
            LEFT JOIN dbo.vt_ordenAtencion o ON o.ordenAten_id = m.med_id_orden
            LEFT JOIN dbo.vt_mascotas mas ON mas.mas_nhcl = m.med_nhcl
            LEFT JOIN dbo.vt_clientes cli ON cli.cli_codigo = COALESCE(mas.mas_idcliente, o.ordenAten_idcliente)
            LEFT JOIN dbo.vt_usuarios u ON u.user_numero = o.ordenAten_idMedico
            LEFT JOIN dbo.PRODUCTO p ON p.CODIGO = m.med_idProdu AND m.med_idProdu > 0
            OUTER APPLY (
                SELECT TOP 1 pm.CODIGO, pm.CODIGO_PRINCIPAL, pm.NOMBRE, pm.UNIDAD_MEDIDA, pm.FACTOR_CONVERSION_INVENTARIO, pm.VALOR_UNITARIO
                FROM dbo.PRODUCTO pm
                WHERE (m.med_idProdu IS NULL OR m.med_idProdu <= 0)
                  AND NULLIF(LTRIM(RTRIM(ISNULL(m.med_otroMedicamento, ''))), '') IS NOT NULL
                  AND (pm.NOMBRE = LTRIM(RTRIM(m.med_otroMedicamento)) OR pm.CODIGO_PRINCIPAL = LTRIM(RTRIM(m.med_otroMedicamento)))
            ) pMatch
            LEFT JOIN ExistenciasSede ex ON ex.codigo_producto = COALESCE(NULLIF(m.med_idProdu, 0), pMatch.CODIGO, 0)
            WHERE ISNULL(m.med_id_orden, 0) > 0
              AND (o.ordenAten_localidad IS NULL OR o.ordenAten_localidad = 0 OR o.ordenAten_localidad = @IdLocalidad)
            ORDER BY
                CASE WHEN ISNULL(NULLIF(LTRIM(RTRIM(m.med_estado)), ''), 'Por Entregar') = 'Por Entregar' THEN 0 ELSE 1 END,
                m.med_id_orden DESC,
                m.sec ASC;";

        try
        {
            var items = (await _db.QueryAsync<FarmaciaRecetaSoapItemDto>(sql, new { IdLocalidad = sedeId })).ToList();

            foreach (var it in items)
            {
                if (it.EstaEnCola)
                {
                    var maxDisp = it.MaximoDisponibleParaEstaLinea;
                    it.SeVende = it.TieneProductoEnCatalogo && maxDisp > 0;
                    it.CantidadAVender = it.SeVende ? Math.Min(it.CantidadRecetada, maxDisp) : 0m;
                }
                else
                {
                    it.SeVende = it.CantidadReservadaLinea > 0;
                    it.CantidadAVender = it.CantidadReservadaLinea;
                }
            }

            var ordenes = items
                .GroupBy(x => x.IdOrden)
                .Select(g =>
                {
                    var first = g.First();
                    return new FarmaciaRecetaSoapOrdenDto
                    {
                        IdOrden = first.IdOrden,
                        Nhcl = first.Nhcl,
                        Paciente = first.Paciente,
                        Especie = first.Especie,
                        Propietario = first.Propietario,
                        Medico = first.Medico,
                        Fecha = g.Max(x => x.Fecha),
                        Items = g.ToList()
                    };
                })
                .ToList();

            if (string.Equals(estadoFiltro, "PENDIENTE", StringComparison.OrdinalIgnoreCase))
                ordenes = ordenes.Where(o => o.TienePendientes).ToList();
            else if (string.Equals(estadoFiltro, "PROCESADA", StringComparison.OrdinalIgnoreCase))
                ordenes = ordenes.Where(o => !o.TienePendientes).ToList();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var term = filtro.Trim();
                ordenes = ordenes.Where(o =>
                    o.IdOrden.ToString().Contains(term) ||
                    o.Nhcl.ToString().Contains(term) ||
                    o.Paciente.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    o.Propietario.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    o.Medico.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    o.Items.Any(i =>
                        i.NombreRecetado.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        i.CodigoPrincipal.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            return ordenes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar recetas SOAP en cola de Farmacia");
            return [];
        }
    }

    public async Task<(bool Exito, string Mensaje)> ActualizarReservaRecetaSoapAsync(
        FarmaciaRecetaSoapOrdenDto receta,
        int idLocalidad,
        int idUsuario)
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;

        const string sqlLinea = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @CodAnterior INT = 0;
            DECLARE @ReservadoAnterior DECIMAL(18,4) = 0;
            DECLARE @EstadoActual VARCHAR(30) = '';

            SELECT TOP 1
                @CodAnterior = ISNULL(med_idProdu, 0),
                @ReservadoAnterior = CAST(ISNULL(med_cantidad1, 0) AS DECIMAL(18,4)),
                @EstadoActual = ISNULL(NULLIF(LTRIM(RTRIM(med_estado)), ''), 'Por Entregar')
            FROM dbo.vt_medicinas WITH (UPDLOCK, HOLDLOCK)
            WHERE sec = @Sec;

            IF @EstadoActual <> 'Por Entregar'
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0;
                RETURN;
            END

            -- 1. Si tenía reserva previa, liberarla primero
            IF @CodAnterior > 0 AND @ReservadoAnterior > 0
            BEGIN
                UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
                SET stock_reservado = CASE WHEN ISNULL(stock_reservado, 0) - @ReservadoAnterior < 0 THEN 0 ELSE ISNULL(stock_reservado, 0) - @ReservadoAnterior END,
                    fecha_actualizacion = SYSUTCDATETIME()
                WHERE codigo_producto = @CodAnterior AND id_sucursal = @IdLocalidad;
            END

            -- 2. Reservar la nueva cantidad deseada (limitada al disponible real)
            DECLARE @NuevoReservadoLinea DECIMAL(18,4) = 0;
            IF @NuevoCodProd > 0 AND @CantidadDeseada > 0
            BEGIN
                DECLARE @IdEmpresa INT, @StockActual DECIMAL(18,4), @StockReservado DECIMAL(18,4);
                SELECT TOP 1
                    @IdEmpresa = e.id_empresa,
                    @StockActual = e.stock_actual,
                    @StockReservado = ISNULL(e.stock_reservado, 0)
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e WITH (UPDLOCK, HOLDLOCK)
                WHERE e.codigo_producto = @NuevoCodProd AND e.id_sucursal = @IdLocalidad;

                IF @IdEmpresa IS NOT NULL
                BEGIN
                    DECLARE @Disp DECIMAL(18,4) = CASE WHEN @StockActual - @StockReservado > 0 THEN @StockActual - @StockReservado ELSE 0 END;
                    SET @NuevoReservadoLinea = CASE WHEN @Disp >= @CantidadDeseada THEN @CantidadDeseada ELSE @Disp END;

                    IF @NuevoReservadoLinea > 0
                    BEGIN
                        UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
                        SET stock_reservado = @StockReservado + @NuevoReservadoLinea,
                            fecha_actualizacion = SYSUTCDATETIME()
                        WHERE codigo_producto = @NuevoCodProd AND id_sucursal = @IdLocalidad AND id_empresa = @IdEmpresa;
                    END
                END
            END

            UPDATE dbo.vt_medicinas
            SET med_idProdu = @NuevoCodProd,
                med_cantidad1 = @NuevoReservadoLinea
            WHERE sec = @Sec;

            COMMIT TRANSACTION;
            SELECT 1;";

        try
        {
            foreach (var item in receta.Items.Where(x => x.EstaEnCola))
            {
                decimal deseada = item.SeVende && item.CantidadAVender > 0 ? item.CantidadAVender : 0m;
                await _db.ExecuteScalarAsync<int>(sqlLinea, new
                {
                    item.Sec,
                    NuevoCodProd = item.CodigoProductoInt,
                    CantidadDeseada = deseada,
                    IdLocalidad = sedeId
                });
            }

            _inventarioService.InvalidarCacheCatalogo();
            return (true, $"Reserva en cola actualizada para la Receta de la Orden #{receta.IdOrden}. Las unidades no seleccionadas quedaron libres.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar reserva de receta SOAP Orden {IdOrden}", receta.IdOrden);
            return (false, $"No se pudo actualizar la reserva: {ex.Message}");
        }
    }

    public async Task<(bool Exito, string Mensaje)> CompletarVentaRecetaSoapAsync(
        FarmaciaRecetaSoapOrdenDto receta,
        int idLocalidad,
        int idUsuario,
        string nombreUsuario,
        string inicialesUsuario)
    {
        int sedeId = idLocalidad > 0 ? idLocalidad : 1;
        string despachador = string.IsNullOrWhiteSpace(nombreUsuario) ? "Farmacia" : nombreUsuario.Trim();
        string iniciales = string.IsNullOrWhiteSpace(inicialesUsuario) ? "FAR" : inicialesUsuario.Trim();
        if (iniciales.Length > 5) iniciales = iniciales[..5];

        var pendientes = receta.Items.Where(x => x.EstaEnCola).ToList();
        if (pendientes.Count == 0)
            return (false, "Esta receta ya fue procesada previamente.");

        foreach (var item in pendientes.Where(x => x.SeVende && x.CantidadAVender > 0))
        {
            if (item.CodigoProductoInt <= 0)
                return (false, $"El medicamento '{item.NombreRecetado}' no está vinculado a un producto en stock. Vincúlalo o desmárcalo de la compra.");
            if (item.CantidadAVender > item.MaximoDisponibleParaEstaLinea)
                return (false, $"La cantidad a vender de '{item.NombreRecetado}' ({item.CantidadAVender:0.##}) supera el stock disponible ({item.MaximoDisponibleParaEstaLinea:0.##}).");
        }

        const string sqlLinea = @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @CodAnterior INT = 0;
            DECLARE @CantRecetada DECIMAL(18,4) = 0;
            DECLARE @ReservadoAnterior DECIMAL(18,4) = 0;
            DECLARE @EstadoActual VARCHAR(30) = '';

            SELECT TOP 1
                @CodAnterior = ISNULL(med_idProdu, 0),
                @CantRecetada = CAST(ISNULL(med_cantidad, 0) AS DECIMAL(18,4)),
                @ReservadoAnterior = CAST(ISNULL(med_cantidad1, 0) AS DECIMAL(18,4)),
                @EstadoActual = ISNULL(NULLIF(LTRIM(RTRIM(med_estado)), ''), 'Por Entregar')
            FROM dbo.vt_medicinas WITH (UPDLOCK, HOLDLOCK)
            WHERE sec = @Sec;

            IF @EstadoActual <> 'Por Entregar'
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT 0;
                RETURN;
            END

            -- 1. Liberar primero la reserva temporal anterior (@ReservadoAnterior)
            IF @CodAnterior > 0 AND @ReservadoAnterior > 0
            BEGIN
                UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
                SET stock_reservado = CASE WHEN ISNULL(stock_reservado, 0) - @ReservadoAnterior < 0 THEN 0 ELSE ISNULL(stock_reservado, 0) - @ReservadoAnterior END,
                    fecha_actualizacion = SYSUTCDATETIME()
                WHERE codigo_producto = @CodAnterior AND id_sucursal = @IdLocalidad;
            END

            -- 2. Si se compró cantidad > 0, descargar SOLO lo comprado (@CantidadComprada) del stock_actual y Kardex
            IF @CodProdFinal > 0 AND @CantidadComprada > 0
            BEGIN
                DECLARE @IdEmpresa INT = NULL;
                DECLARE @StockActual DECIMAL(18,4) = 0;
                DECLARE @StockReservadoRestante DECIMAL(18,4) = 0;
                DECLARE @CostoPromedio DECIMAL(18,4) = 0;

                SELECT TOP 1
                    @IdEmpresa = e.id_empresa,
                    @StockActual = e.stock_actual,
                    @StockReservadoRestante = ISNULL(e.stock_reservado, 0),
                    @CostoPromedio = ISNULL(e.costo_promedio, 0)
                FROM dbo.ECONTAX_INVENTARIO_EXISTENCIA e WITH (UPDLOCK, HOLDLOCK)
                WHERE e.codigo_producto = @CodProdFinal AND e.id_sucursal = @IdLocalidad;

                IF @IdEmpresa IS NULL OR (@StockActual - @StockReservadoRestante) < @CantidadComprada
                BEGIN
                    ROLLBACK TRANSACTION;
                    SELECT -2;
                    RETURN;
                END

                DECLARE @NuevoStock DECIMAL(18,4) = @StockActual - @CantidadComprada;

                UPDATE dbo.ECONTAX_INVENTARIO_EXISTENCIA
                SET stock_actual = @NuevoStock,
                    fecha_actualizacion = SYSUTCDATETIME()
                WHERE codigo_producto = @CodProdFinal
                  AND id_sucursal = @IdLocalidad
                  AND id_empresa = @IdEmpresa;

                -- Descontar de lotes disponibles (FIFO) SOLO lo comprado
                DECLARE @RestanteLote DECIMAL(18,4) = @CantidadComprada;
                DECLARE @IdLote BIGINT, @DispLote DECIMAL(18,4), @TomarLote DECIMAL(18,4);
                WHILE @RestanteLote > 0
                BEGIN
                    SET @IdLote = NULL;
                    SELECT TOP 1 @IdLote = l.id_lote, @DispLote = l.cantidad_disponible
                    FROM dbo.ECONTAX_INVENTARIO_LOTE l WITH (UPDLOCK, HOLDLOCK)
                    WHERE l.codigo_producto = @CodProdFinal
                      AND l.id_sucursal = @IdLocalidad
                      AND l.id_empresa = @IdEmpresa
                      AND l.cantidad_disponible > 0
                    ORDER BY ISNULL(l.fecha_vencimiento, '9999-12-31'), l.fecha_ingreso, l.id_lote;

                    IF @IdLote IS NULL BREAK;

                    SET @TomarLote = CASE WHEN @DispLote <= @RestanteLote THEN @DispLote ELSE @RestanteLote END;
                    UPDATE dbo.ECONTAX_INVENTARIO_LOTE
                    SET cantidad_disponible = cantidad_disponible - @TomarLote
                    WHERE id_lote = @IdLote;

                    SET @RestanteLote = @RestanteLote - @TomarLote;
                END

                -- Registrar en el Kardex ÚNICAMENTE la cantidad realmente comprada (@CantidadComprada)
                INSERT INTO dbo.ECONTAX_INVENTARIO_MOVIMIENTO (
                    codigo_producto, id_empresa, id_sucursal, tipo_movimiento,
                    cantidad, costo_unitario, costo_total, saldo_cantidad, saldo_costo,
                    metodo_valoracion, referencia_tipo, referencia, id_usuario, fecha_movimiento, observacion
                ) VALUES (
                    @CodProdFinal, @IdEmpresa, @IdLocalidad, 'SALIDA',
                    @CantidadComprada, @CostoPromedio, ROUND(@CantidadComprada * @CostoPromedio, 4), @NuevoStock, ROUND(@NuevoStock * @CostoPromedio, 4),
                    'PROMEDIO', 'FARMACIA_RECETA', 'ORDEN:' + CAST(@IdOrden AS VARCHAR(20)), @IdUsuario, SYSUTCDATETIME(),
                    'Venta de Receta SOAP Orden #' + CAST(@IdOrden AS VARCHAR(20)) + ' NHCL #' + CAST(@Nhcl AS VARCHAR(20)) +
                    ' · Comprado: ' + CAST(@CantidadComprada AS VARCHAR(20)) + ' de ' + CAST(@CantRecetada AS VARCHAR(20)) + ' recetado' +
                    CASE WHEN ISNULL(@Bodega, '') <> '' THEN ' · Bodega: ' + @Bodega ELSE '' END +
                    ' · Despachado por: ' + @Despachador
                );

                -- Registrar en historial de ventas de Farmacia
                INSERT INTO dbo.hvt_cabMonitoreoMedBC (
                    ordenId, nhcl, hora, fecha, codigoprod, precioprod, stockprod,
                    indicacionesprod, cantprod, usuario, localidad, proddescripcion,
                    via, origen, iniciales, funcion, funcionario, estado, grupo, modulo,
                    med_iva, med_iddinamics, med_medida, med_bodega, observaciones
                ) VALUES (
                    @IdOrden, @Nhcl, CONVERT(VARCHAR(5), GETDATE(), 108), CAST(GETDATE() AS date), @CodigoPrincipal, @PrecioUnitario, CAST(@NuevoStock AS INT),
                    @Indicaciones, @CantidadComprada, @IdUsuario, @IdLocalidad, @NombreProducto,
                    'RECETA SOAP', 'F', @Iniciales, 'Farmacia', @Despachador, 'A', 'ENTREGADO', 'VentaFarmacia',
                    'IVA0', CAST(@CodProdFinal AS VARCHAR(50)), @UnidadMedida, @Bodega,
                    'Receta SOAP Orden #' + CAST(@IdOrden AS VARCHAR(20)) + ' · ' + @PacienteRef
                );
            END

            DECLARE @NuevoEstado VARCHAR(15) = CASE
                WHEN @CantidadComprada <= 0 THEN 'No Comprado'
                WHEN @CantidadComprada < @CantRecetada THEN 'Parcial'
                ELSE 'Entregado'
            END;

            DECLARE @ObsFinal VARCHAR(240) = 'Farmacia (' + CONVERT(VARCHAR(16), GETDATE(), 120) + '): comprado ' +
                CAST(@CantidadComprada AS VARCHAR(20)) + ' de ' + CAST(@CantRecetada AS VARCHAR(20)) + ' · ' + @Despachador;

            UPDATE dbo.vt_medicinas
            SET med_idProdu = @CodProdFinal,
                med_cantidad1 = @CantidadComprada,
                med_estado = @NuevoEstado,
                med_utilizado = 1,
                med_observaciones = @ObsFinal
            WHERE sec = @Sec;

            COMMIT TRANSACTION;
            SELECT 1;";

        try
        {
            int comprados = 0;
            int omitidos = 0;

            foreach (var item in pendientes)
            {
                decimal cantComprada = item.SeVende && item.CantidadAVender > 0 ? item.CantidadAVender : 0m;
                int res = await _db.ExecuteScalarAsync<int>(sqlLinea, new
                {
                    item.Sec,
                    CodProdFinal = item.CodigoProductoInt,
                    CantidadComprada = cantComprada,
                    IdOrden = receta.IdOrden,
                    Nhcl = receta.Nhcl,
                    IdLocalidad = sedeId,
                    IdUsuario = idUsuario > 0 ? idUsuario : 1,
                    Despachador = despachador,
                    Iniciales = iniciales,
                    CodigoPrincipal = string.IsNullOrWhiteSpace(item.CodigoPrincipal) ? item.CodigoProductoInt.ToString() : item.CodigoPrincipal,
                    NombreProducto = string.IsNullOrWhiteSpace(item.NombreProductoInventario) ? item.NombreRecetado : item.NombreProductoInventario,
                    item.PrecioUnitario,
                    item.UnidadMedida,
                    item.Bodega,
                    item.Indicaciones,
                    PacienteRef = $"{receta.Paciente} ({receta.Propietario})"
                });

                if (res == -2)
                    return (false, $"Stock insuficiente al procesar '{item.NombreRecetado}'.");
                if (cantComprada > 0) comprados++;
                else omitidos++;
            }

            _inventarioService.InvalidarCacheCatalogo();
            return (true, $"Receta SOAP Orden #{receta.IdOrden} completada: {comprados} medicamento(s) vendidos y descargados del Kardex, {omitidos} no comprados (reserva liberada).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al completar venta de receta SOAP Orden {IdOrden}", receta.IdOrden);
            return (false, $"Error al procesar la receta: {ex.Message}");
        }
    }
}
