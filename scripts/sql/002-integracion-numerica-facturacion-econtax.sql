/*
    Integracion Sistema Veterinario + Numerica (Facturacion / EContax)

    REQUISITOS
    1. Cargar scriptveterinaria.sql (estructura y datos) en BaseDestino.
       El archivo exportado crea originalmente db_a8a74a_hosvet: antes de
       ejecutarlo debe adaptarse para CREAR/USAR VeterinariaFacturacion.
    2. Cargar script.sql de Numerica con su nombre original db_aab800_numerica.
       Si esa base ya existe y contiene las 143 tablas, no volver a ejecutarlo.
    3. Ejecutar este archivo en SQLCMD Mode desde SSMS o mediante sqlcmd.
    4. BaseDestino y BaseNumerica deben quedar en la misma instancia SQL Server.
    5. Completar IdEmpresaNumerica, IdSucursalNumerica y CodigoEmisorNumerica.

    SEGURIDAD
    - No ejecutar directamente sobre db_a8a74a_hosvet.
    - No modifica las bases de origen.
    - No renumera vt_usuarios ni altera referencias clinicas historicas.
    - No copia usuarios de Numerica.
    - dbo.Usuarios se genera desde vt_usuarios conservando el mismo ID.
    - El inicio de sesion continua validandose con la seguridad veterinaria.
*/

:setvar BaseDestino "VeterinariaFacturacion"
:setvar BaseNumerica "db_aab800_numerica"
:setvar IdEmpresaNumerica "0"
:setvar IdSucursalNumerica "0"
:setvar CodigoEmisorNumerica "0"
:setvar UserNumeroAdministradorVet "0"
:setvar IdPerfilEContaxAdministrador "1"

:on error exit

USE [$(BaseDestino)];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() = N'db_a8a74a_hosvet'
    THROW 51000, 'Por seguridad, restaure una copia de Veterinaria y cambie BaseDestino.', 1;

IF DB_ID(N'$(BaseNumerica)') IS NULL
    THROW 51001, 'No existe la base de origen Numerica indicada.', 1;

IF OBJECT_ID(N'dbo.vt_usuarios', N'U') IS NULL
    THROW 51002, 'La base destino no contiene la estructura completa de Veterinaria.', 1;
GO

DECLARE @IdEmpresaNumerica INT = TRY_CONVERT(INT, N'$(IdEmpresaNumerica)');
DECLARE @IdSucursalNumerica INT = TRY_CONVERT(INT, N'$(IdSucursalNumerica)');
DECLARE @CodigoEmisorNumerica INT = TRY_CONVERT(INT, N'$(CodigoEmisorNumerica)');
DECLARE @UserNumeroAdministradorVet INT = TRY_CONVERT(INT, N'$(UserNumeroAdministradorVet)');
DECLARE @IdPerfilEContaxAdministrador INT = TRY_CONVERT(INT, N'$(IdPerfilEContaxAdministrador)');

IF COALESCE(@IdEmpresaNumerica, 0) < 0
   OR COALESCE(@IdSucursalNumerica, 0) < 0
   OR COALESCE(@CodigoEmisorNumerica, 0) < 0
    THROW 51003, 'Los parametros empresariales no pueden ser negativos.', 1;

CREATE TABLE #TablasMigracion
(
    Orden INT IDENTITY(1,1) PRIMARY KEY,
    Tabla SYSNAME NOT NULL UNIQUE,
    CopiarDatos BIT NOT NULL,
    Grupo VARCHAR(30) NOT NULL
);

/* Catalogos y seguridad requeridos con sus datos actuales. */
INSERT INTO #TablasMigracion (Tabla, CopiarDatos, Grupo)
VALUES
    (N'TipoIdentificacion', 1, 'CATALOGO'),
    (N'TipoUsuario', 1, 'CATALOGO'),
    (N'IDENTIFICACION', 1, 'CATALOGO'),
    (N'TIPODOCUMENTO', 1, 'CATALOGO'),
    (N'TIPOCLIENTE', 1, 'CATALOGO'),
    (N'PAIS', 1, 'CATALOGO'),
    (N'PROVINCIA', 1, 'CATALOGO'),
    (N'CIUDAD', 1, 'CATALOGO'),
    (N'CODIGOSIMPUESTOS', 1, 'CATALOGO'),
    (N'PORCENTAJEIVA', 1, 'CATALOGO'),
    (N'FORMASDEPAGO', 1, 'CATALOGO'),
    (N'PRODUCTOTIPO', 1, 'CATALOGO'),
    (N'PRODUCTOSUBTIPO', 1, 'CATALOGO'),
    (N'ABONOTIPO', 1, 'CATALOGO'),
    (N'Usuarios', 0, 'SEGURIDAD'),
    (N'ROLES', 0, 'SEGURIDAD'),
    (N'MENUS', 0, 'SEGURIDAD'),
    (N'ROL_MENU', 0, 'SEGURIDAD'),
    (N'rol', 1, 'SEGURIDAD'),
    (N'menu', 1, 'SEGURIDAD'),
    (N'ECONTAX_PERFIL', 0, 'SEGURIDAD'),
    (N'ECONTAX_ROL_MENU', 1, 'SEGURIDAD'),
    (N'APP_SERVICIOS', 1, 'SEGURIDAD');

/* Configuracion empresarial: se copia posteriormente con filtro. */
INSERT INTO #TablasMigracion (Tabla, CopiarDatos, Grupo)
VALUES
    (N'EMPRESA', 0, 'CONFIGURACION'),
    (N'SUCURSAL', 0, 'CONFIGURACION'),
    (N'EMISOR', 0, 'CONFIGURACION'),
    (N'CAJA', 0, 'CONFIGURACION'),
    (N'CAJA_SECUENCIA', 0, 'CONFIGURACION'),
    (N'CAJA_SECUENCIA_PREFERENCIA', 0, 'CONFIGURACION'),
    (N'ECONTAX_USUARIO_CONTEXTO', 0, 'CONFIGURACION'),
    (N'ECONTAX_USUARIO_ROL', 0, 'CONFIGURACION');

/* Maestros y movimientos: se crea la estructura, sin historial de Numerica. */
INSERT INTO #TablasMigracion (Tabla, CopiarDatos, Grupo)
VALUES
    (N'CLIENTES', 0, 'FACTURACION'),
    (N'ClientesCorreos', 0, 'FACTURACION'),
    (N'PRODUCTO', 0, 'FACTURACION'),
    (N'PROVEEDORES', 0, 'FACTURACION'),
    (N'TRANSPORTISTAS', 0, 'FACTURACION'),
    (N'FACTURA', 0, 'FACTURACION'),
    (N'DETALLEFACTURA', 0, 'FACTURACION'),
    (N'FACTURA_IDEMPOTENCIA', 0, 'FACTURACION'),
    (N'COMPROBANTECORREOESTADO', 0, 'FACTURACION'),
    (N'NOTASCREDITO', 0, 'FACTURACION'),
    (N'DETALLENOTACREDITO', 0, 'FACTURACION'),
    (N'NOTASDEBITO', 0, 'FACTURACION'),
    (N'DETALLESNOTADEBITO', 0, 'FACTURACION'),
    (N'GUIAREMISION', 0, 'FACTURACION'),
    (N'GUIADESTINATARIO', 0, 'FACTURACION'),
    (N'DETALLESGUIAREMISION', 0, 'FACTURACION'),
    (N'RETENCION', 0, 'FACTURACION'),
    (N'RETENCIONIVA', 0, 'FACTURACION'),
    (N'RETENCIONRENTA', 0, 'FACTURACION'),
    (N'RETENCIONISD', 0, 'FACTURACION'),
    (N'RETENCIONINFO', 0, 'FACTURACION'),
    (N'COMPRASRETVALOR', 0, 'FACTURACION'),
    (N'COMPRASFACTURA', 0, 'COMPRAS'),
    (N'COMPRASDETALLEFAC', 0, 'COMPRAS'),
    (N'CXP_PAGOS', 0, 'COMPRAS'),
    (N'COTIZACION', 0, 'FACTURACION'),
    (N'COTIZACION_DETALLE', 0, 'FACTURACION'),
    (N'ABONOS', 0, 'CARTERA'),
    (N'ABONOMULTIPLE', 0, 'CARTERA'),
    (N'ECONTAX_BODEGA', 0, 'ECONTAX'),
    (N'ECONTAX_PRODUCTO_SUCURSAL', 0, 'ECONTAX'),
    (N'ECONTAX_PRODUCTO_PROVEEDOR', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_CONFIGURACION', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_EXISTENCIA', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_LOTE', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_MOVIMIENTO', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_MOVIMIENTO_LOTE', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_TRANSFERENCIA', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_TRANSFERENCIA_DET', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_DEVOLUCION', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_DEVOLUCION_DET', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_TOMA_FISICA', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_TOMA_FISICA_DET', 0, 'ECONTAX'),
    (N'ECONTAX_INVENTARIO_VALORACION_HIST', 0, 'ECONTAX'),
    (N'ECONTAX_ORDEN_COMPRA', 0, 'ECONTAX'),
    (N'ECONTAX_ORDEN_COMPRA_DETALLE', 0, 'ECONTAX'),
    (N'ECONTAX_ORDEN_COMPRA_HISTORIAL', 0, 'ECONTAX'),
    (N'ECONTAX_RECEPCION_COMPRA', 0, 'ECONTAX'),
    (N'ECONTAX_RECEPCION_COMPRA_DETALLE', 0, 'ECONTAX'),
    (N'ECONTAX_RECEPCION_COMPRA_HISTORIAL', 0, 'ECONTAX');

IF EXISTS
(
    SELECT 1
    FROM #TablasMigracion m
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [$(BaseNumerica)].sys.tables t
        JOIN [$(BaseNumerica)].sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = N'dbo' AND t.name = m.Tabla
    )
)
BEGIN
    SELECT Tabla AS TablaFaltante
    FROM #TablasMigracion m
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [$(BaseNumerica)].sys.tables t
        JOIN [$(BaseNumerica)].sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = N'dbo' AND t.name = m.Tabla
    );

    THROW 51004, 'Numerica no contiene una o mas tablas requeridas.', 1;
END;

DECLARE
    @Tabla SYSNAME,
    @CopiarDatos BIT,
    @Sql NVARCHAR(MAX),
    @ColumnasInsercion NVARCHAR(MAX),
    @TieneIdentity BIT,
    @DestinoTieneDatos BIT;

DECLARE tablas_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT Tabla, CopiarDatos
    FROM #TablasMigracion
    ORDER BY Orden;

OPEN tablas_cursor;
FETCH NEXT FROM tablas_cursor INTO @Tabla, @CopiarDatos;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla), N'U') IS NULL
    BEGIN
        SET @Sql = N'SELECT TOP (0) * INTO dbo.' + QUOTENAME(@Tabla)
                 + N' FROM [$(BaseNumerica)].dbo.' + QUOTENAME(@Tabla) + N';';
        EXEC sys.sp_executesql @Sql;
    END;

    IF @CopiarDatos = 1
    BEGIN
        SET @Sql = N'SELECT @Resultado = CASE WHEN EXISTS '
                 + N'(SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N') THEN 1 ELSE 0 END;';
        EXEC sys.sp_executesql @Sql, N'@Resultado BIT OUTPUT', @Resultado = @DestinoTieneDatos OUTPUT;

        IF @DestinoTieneDatos = 0
        BEGIN
            SELECT @ColumnasInsercion = STRING_AGG(
                       CONVERT(NVARCHAR(MAX), QUOTENAME(c.name)), N',')
                       WITHIN GROUP (ORDER BY c.column_id)
            FROM [$(BaseNumerica)].sys.columns c
            JOIN [$(BaseNumerica)].sys.tables t ON t.object_id = c.object_id
            JOIN [$(BaseNumerica)].sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'dbo'
              AND t.name = @Tabla;

            IF NULLIF(@ColumnasInsercion, N'') IS NULL
                THROW 51009, 'No fue posible obtener las columnas para copiar una tabla.', 1;

            SELECT @TieneIdentity = CASE WHEN EXISTS
            (
                SELECT 1
                FROM [$(BaseNumerica)].sys.identity_columns ic
                JOIN [$(BaseNumerica)].sys.tables t ON t.object_id = ic.object_id
                JOIN [$(BaseNumerica)].sys.schemas s ON s.schema_id = t.schema_id
                WHERE s.name = N'dbo' AND t.name = @Tabla
            ) THEN 1 ELSE 0 END;

            SET @Sql = CASE WHEN @TieneIdentity = 1
                       THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@Tabla) + N' ON; '
                       ELSE N'' END
                     + N'INSERT INTO dbo.' + QUOTENAME(@Tabla)
                     + N' (' + @ColumnasInsercion + N') SELECT ' + @ColumnasInsercion
                     + N' FROM [$(BaseNumerica)].dbo.' + QUOTENAME(@Tabla) + N'; '
                     + CASE WHEN @TieneIdentity = 1
                       THEN N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@Tabla) + N' OFF;'
                       ELSE N'' END;
            EXEC sys.sp_executesql @Sql;
        END;
    END;

    FETCH NEXT FROM tablas_cursor INTO @Tabla, @CopiarDatos;
END;

CLOSE tablas_cursor;
DEALLOCATE tablas_cursor;

/*
   Limpia solamente perfiles EContax huerfanos que pudieron quedar de una
   ejecucion anterior. Los perfiles validos de empresas ya configuradas se
   conservan. En el modelo multi-veterinaria no se copian perfiles de Numerica.
*/
DELETE p
FROM dbo.ECONTAX_PERFIL p
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.EMPRESA e
    WHERE e.idEmpresa = p.IdEmpresa
);

/*
   Los usuarios de facturacion nacen exclusivamente de vt_usuarios.
   Se conserva el mismo numero para que el actor clinico y el actor de
   facturacion sean la misma persona. PasswordHash no se usa para iniciar
   sesion: el acceso continua validandose en vt_usuarios.
*/
SET IDENTITY_INSERT dbo.Usuarios ON;

INSERT INTO dbo.Usuarios
(
    IdUsuario,
    Nombres,
    Apellidos,
    Email,
    PasswordHash,
    CuentaBloqueada,
    FechaCreacion,
    Estado,
    ClaveTemporal,
    Celular,
    Identificacion,
    SaldoDocumentos
)
SELECT
    CONVERT(INT, v.user_numero),
    COALESCE(NULLIF(LTRIM(RTRIM(v.user_nombre)), ''),
             NULLIF(LTRIM(RTRIM(v.user_login)), ''),
             CONCAT('Usuario veterinario ', v.user_numero)),
    N'',
    COALESCE(NULLIF(LTRIM(RTRIM(v.user_email)), ''),
             CONCAT('vet-', CONVERT(INT, v.user_numero), '@local.invalid')),
    CONVERT(NVARCHAR(128), HASHBYTES('SHA2_512', CONVERT(NVARCHAR(36), NEWID())), 2),
    1,
    COALESCE(v.user_fechacreacion, GETDATE()),
    CASE WHEN UPPER(LTRIM(RTRIM(COALESCE(v.user_estado, 'A')))) IN ('A', 'S', '1')
         THEN 1 ELSE 0 END,
    1,
    NULLIF(LTRIM(RTRIM(v.user_celular)), ''),
    NULLIF(LTRIM(RTRIM(v.user_cedula)), ''),
    0
FROM dbo.vt_usuarios v
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Usuarios n
    WHERE n.IdUsuario = CONVERT(INT, v.user_numero)
);

/*
   Usuario fiscal tecnico por localidad. Numerica separa documentos por
   IdUsuario; compartir este ID entre los usuarios de una misma localidad
   permite que vean la facturacion de su veterinaria, sin mezclarla con otras.
*/
INSERT INTO dbo.Usuarios
(
    IdUsuario,
    Nombres,
    Apellidos,
    NombreEmpresa,
    Email,
    PasswordHash,
    CuentaBloqueada,
    FechaCreacion,
    Estado,
    ClaveTemporal,
    Identificacion,
    SaldoDocumentos
)
SELECT
    1000000 + CONVERT(INT, l.loc_id),
    COALESCE(NULLIF(LTRIM(RTRIM(l.loc_nom_empresa)), ''),
             NULLIF(LTRIM(RTRIM(l.loc_descripcion)), ''),
             CONCAT('Veterinaria ', CONVERT(INT, l.loc_id))),
    N'',
    COALESCE(NULLIF(LTRIM(RTRIM(l.loc_nom_empresa)), ''),
             NULLIF(LTRIM(RTRIM(l.loc_descripcion)), '')),
    CONCAT('facturacion-localidad-', CONVERT(INT, l.loc_id), '@local.invalid'),
    CONVERT(NVARCHAR(128), HASHBYTES('SHA2_512', CONVERT(NVARCHAR(36), NEWID())), 2),
    1,
    GETDATE(),
    CASE WHEN UPPER(LTRIM(RTRIM(COALESCE(l.loc_estado, 'A')))) = 'A' THEN 1 ELSE 0 END,
    1,
    NULLIF(LTRIM(RTRIM(l.loc_ruc)), ''),
    0
FROM dbo.vt_localidades l
WHERE CONVERT(BIGINT, l.loc_id) BETWEEN 1 AND 1000000000
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Usuarios n
      WHERE n.IdUsuario = 1000000 + CONVERT(INT, l.loc_id)
  );

SET IDENTITY_INSERT dbo.Usuarios OFF;

/*
   EMPRESA, SUCURSAL, EMISOR, CAJA y sus secuenciales quedan sin datos de
   Numerica. Cada localidad veterinaria los registra desde la aplicacion.
*/

/* Replica defaults, claves e indices despues de cargar los datos. */
DECLARE
    @NombreObjeto SYSNAME,
    @Columna SYSNAME,
    @Definicion NVARCHAR(MAX),
    @EsPk BIT,
    @EsUniqueConstraint BIT,
    @EsUnique BIT,
    @TipoIndice TINYINT,
    @Columnas NVARCHAR(MAX),
    @Incluidas NVARCHAR(MAX);

DECLARE defaults_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.name, c.name, dc.name, dc.definition
    FROM [$(BaseNumerica)].sys.default_constraints dc
    JOIN [$(BaseNumerica)].sys.tables t ON t.object_id = dc.parent_object_id
    JOIN [$(BaseNumerica)].sys.schemas s ON s.schema_id = t.schema_id
    JOIN [$(BaseNumerica)].sys.columns c
      ON c.object_id = dc.parent_object_id
     AND c.column_id = dc.parent_column_id
    JOIN #TablasMigracion m ON m.Tabla = t.name
    WHERE s.name = N'dbo';

OPEN defaults_cursor;
FETCH NEXT FROM defaults_cursor INTO @Tabla, @Columna, @NombreObjeto, @Definicion;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c
          ON c.object_id = dc.parent_object_id
         AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla))
          AND c.name = @Columna
    )
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla)
                 + N' ADD CONSTRAINT ' + QUOTENAME(@NombreObjeto)
                 + N' DEFAULT ' + @Definicion + N' FOR ' + QUOTENAME(@Columna) + N';';
        EXEC sys.sp_executesql @Sql;
    END;

    FETCH NEXT FROM defaults_cursor INTO @Tabla, @Columna, @NombreObjeto, @Definicion;
END;

CLOSE defaults_cursor;
DEALLOCATE defaults_cursor;

CREATE TABLE #IndicesOrigen
(
    Tabla SYSNAME NOT NULL,
    Nombre SYSNAME NOT NULL,
    EsPk BIT NOT NULL,
    EsUniqueConstraint BIT NOT NULL,
    EsUnique BIT NOT NULL,
    TipoIndice TINYINT NOT NULL,
    Columnas NVARCHAR(MAX) NOT NULL,
    Incluidas NVARCHAR(MAX) NULL
);

INSERT INTO #IndicesOrigen
(
    Tabla, Nombre, EsPk, EsUniqueConstraint, EsUnique,
    TipoIndice, Columnas, Incluidas
)
SELECT
    t.name,
    i.name,
    i.is_primary_key,
    i.is_unique_constraint,
    i.is_unique,
    i.type,
    STRING_AGG
    (
        CASE WHEN ic.is_included_column = 0
             THEN QUOTENAME(c.name)
                  + CASE WHEN ic.is_descending_key = 1 THEN N' DESC' ELSE N' ASC' END
        END,
        N','
    ) WITHIN GROUP (ORDER BY ic.index_column_id),
    STRING_AGG
    (
        CASE WHEN ic.is_included_column = 1 THEN QUOTENAME(c.name) END,
        N','
    ) WITHIN GROUP (ORDER BY ic.index_column_id)
FROM [$(BaseNumerica)].sys.indexes i
JOIN [$(BaseNumerica)].sys.tables t ON t.object_id = i.object_id
JOIN [$(BaseNumerica)].sys.schemas s ON s.schema_id = t.schema_id
JOIN [$(BaseNumerica)].sys.index_columns ic
  ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN [$(BaseNumerica)].sys.columns c
  ON c.object_id = ic.object_id AND c.column_id = ic.column_id
JOIN #TablasMigracion m ON m.Tabla = t.name
WHERE s.name = N'dbo'
  AND i.index_id > 0
  AND i.is_hypothetical = 0
  AND i.type IN (1, 2)
GROUP BY t.name, i.name, i.is_primary_key, i.is_unique_constraint,
         i.is_unique, i.type;

DECLARE indices_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT Tabla, Nombre, EsPk, EsUniqueConstraint, EsUnique,
           TipoIndice, Columnas, Incluidas
    FROM #IndicesOrigen
    ORDER BY EsPk DESC, EsUniqueConstraint DESC, Tabla, Nombre;

OPEN indices_cursor;
FETCH NEXT FROM indices_cursor
INTO @Tabla, @NombreObjeto, @EsPk, @EsUniqueConstraint, @EsUnique,
     @TipoIndice, @Columnas, @Incluidas;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla))
          AND name = @NombreObjeto
    )
    BEGIN
        IF @EsPk = 1 OR @EsUniqueConstraint = 1
            SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla)
                     + N' ADD CONSTRAINT ' + QUOTENAME(@NombreObjeto)
                     + CASE WHEN @EsPk = 1 THEN N' PRIMARY KEY ' ELSE N' UNIQUE ' END
                     + CASE WHEN @TipoIndice = 1 THEN N'CLUSTERED ' ELSE N'NONCLUSTERED ' END
                     + N'(' + @Columnas + N');';
        ELSE
            SET @Sql = N'CREATE ' + CASE WHEN @EsUnique = 1 THEN N'UNIQUE ' ELSE N'' END
                     + CASE WHEN @TipoIndice = 1 THEN N'CLUSTERED ' ELSE N'NONCLUSTERED ' END
                     + N'INDEX ' + QUOTENAME(@NombreObjeto)
                     + N' ON dbo.' + QUOTENAME(@Tabla) + N'(' + @Columnas + N')'
                     + CASE WHEN NULLIF(@Incluidas, N'') IS NOT NULL
                            THEN N' INCLUDE (' + @Incluidas + N')' ELSE N'' END + N';';

        EXEC sys.sp_executesql @Sql;
    END;

    FETCH NEXT FROM indices_cursor
    INTO @Tabla, @NombreObjeto, @EsPk, @EsUniqueConstraint, @EsUnique,
         @TipoIndice, @Columnas, @Incluidas;
END;

CLOSE indices_cursor;
DEALLOCATE indices_cursor;

/*
   EContax referencia SUCURSAL por la clave compuesta empresa/sucursal.
   En bases creadas mediante SELECT INTO esa clave puede no conservarse aunque
   la tabla ya exista de un intento anterior. Se garantiza antes de crear FKs.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.SUCURSAL')
      AND i.is_unique = 1
      AND 2 =
      (
          SELECT COUNT(*)
          FROM sys.index_columns ic
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.is_included_column = 0
      )
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns ic
          JOIN sys.columns c
            ON c.object_id = ic.object_id AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 1
            AND c.name = N'idEmpresa'
      )
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns ic
          JOIN sys.columns c
            ON c.object_id = ic.object_id AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 2
            AND c.name = N'idSucursal'
      )
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Integracion_Sucursal_EmpresaSucursal
        ON dbo.SUCURSAL(idEmpresa, idSucursal);
END;

/* Replica solamente FKs cuyas dos tablas fueron incluidas. */
CREATE TABLE #FkOrigen
(
    Nombre SYSNAME NOT NULL,
    TablaPadre SYSNAME NOT NULL,
    TablaReferencia SYSNAME NOT NULL,
    ColumnasPadre NVARCHAR(MAX) NOT NULL,
    ColumnasReferencia NVARCHAR(MAX) NOT NULL,
    AccionDelete NVARCHAR(60) NOT NULL,
    AccionUpdate NVARCHAR(60) NOT NULL
);

INSERT INTO #FkOrigen
SELECT
    fk.name,
    tp.name,
    tr.name,
    STRING_AGG(QUOTENAME(cp.name), N',')
        WITHIN GROUP (ORDER BY fkc.constraint_column_id),
    STRING_AGG(QUOTENAME(cr.name), N',')
        WITHIN GROUP (ORDER BY fkc.constraint_column_id),
    fk.delete_referential_action_desc,
    fk.update_referential_action_desc
FROM [$(BaseNumerica)].sys.foreign_keys fk
JOIN [$(BaseNumerica)].sys.foreign_key_columns fkc
  ON fkc.constraint_object_id = fk.object_id
JOIN [$(BaseNumerica)].sys.tables tp ON tp.object_id = fk.parent_object_id
JOIN [$(BaseNumerica)].sys.tables tr ON tr.object_id = fk.referenced_object_id
JOIN [$(BaseNumerica)].sys.columns cp
  ON cp.object_id = fkc.parent_object_id AND cp.column_id = fkc.parent_column_id
JOIN [$(BaseNumerica)].sys.columns cr
  ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id
JOIN #TablasMigracion mp ON mp.Tabla = tp.name
JOIN #TablasMigracion mr ON mr.Tabla = tr.name
GROUP BY fk.name, tp.name, tr.name,
         fk.delete_referential_action_desc, fk.update_referential_action_desc;

DECLARE @TablaReferencia SYSNAME, @ColumnasReferencia NVARCHAR(MAX),
        @AccionDelete NVARCHAR(60), @AccionUpdate NVARCHAR(60);

DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT Nombre, TablaPadre, TablaReferencia, ColumnasPadre,
           ColumnasReferencia, AccionDelete, AccionUpdate
    FROM #FkOrigen;

OPEN fk_cursor;
FETCH NEXT FROM fk_cursor
INTO @NombreObjeto, @Tabla, @TablaReferencia, @Columnas,
     @ColumnasReferencia, @AccionDelete, @AccionUpdate;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @NombreObjeto)
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla)
                 + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@NombreObjeto)
                 + N' FOREIGN KEY (' + @Columnas + N') REFERENCES dbo.'
                 + QUOTENAME(@TablaReferencia) + N'(' + @ColumnasReferencia + N')'
                 + CASE WHEN @AccionDelete <> N'NO_ACTION'
                        THEN N' ON DELETE ' + REPLACE(@AccionDelete, N'_', N' ') ELSE N'' END
                 + CASE WHEN @AccionUpdate <> N'NO_ACTION'
                        THEN N' ON UPDATE ' + REPLACE(@AccionUpdate, N'_', N' ') ELSE N'' END
                 + N';';
        EXEC sys.sp_executesql @Sql;
    END;

    FETCH NEXT FROM fk_cursor
    INTO @NombreObjeto, @Tabla, @TablaReferencia, @Columnas,
         @ColumnasReferencia, @AccionDelete, @AccionUpdate;
END;

CLOSE fk_cursor;
DEALLOCATE fk_cursor;

/* Union logica: conserva ambos identificadores y evita reescribir historia clinica. */
IF OBJECT_ID(N'dbo.UsuarioVeterinariaMap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioVeterinariaMap
    (
        IdUsuarioNumerica INT NOT NULL,
        UserNumeroVeterinaria NUMERIC(9,0) NOT NULL,
        TipoVinculacion VARCHAR(20) NOT NULL,
        Confirmado BIT NOT NULL
            CONSTRAINT DF_UsuarioVeterinariaMap_Confirmado DEFAULT (0),
        FechaVinculacion DATETIME2 NOT NULL
            CONSTRAINT DF_UsuarioVeterinariaMap_Fecha DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_UsuarioVeterinariaMap PRIMARY KEY (IdUsuarioNumerica),
        CONSTRAINT UQ_UsuarioVeterinariaMap_Veterinaria UNIQUE (UserNumeroVeterinaria)
    );
END;

IF OBJECT_ID(N'dbo.VeterinariaFacturacionTenant', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VeterinariaFacturacionTenant
    (
        IdLocalidad NUMERIC(15,0) NOT NULL,
        IdUsuarioFacturacion INT NOT NULL,
        IdEmpresa INT NULL,
        IdSucursal INT NULL,
        CodigoEmisor INT NULL,
        Estado BIT NOT NULL
            CONSTRAINT DF_VeterinariaFacturacionTenant_Estado DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL
            CONSTRAINT DF_VeterinariaFacturacionTenant_Fecha DEFAULT SYSUTCDATETIME(),
        FechaActualizacion DATETIME2 NULL,

        CONSTRAINT PK_VeterinariaFacturacionTenant PRIMARY KEY (IdLocalidad),
        CONSTRAINT UQ_VeterinariaFacturacionTenant_Usuario UNIQUE (IdUsuarioFacturacion),
        CONSTRAINT FK_VeterinariaFacturacionTenant_Localidad
            FOREIGN KEY (IdLocalidad) REFERENCES dbo.vt_localidades(loc_id),
        CONSTRAINT FK_VeterinariaFacturacionTenant_Usuario
            FOREIGN KEY (IdUsuarioFacturacion) REFERENCES dbo.Usuarios(IdUsuario)
    );
END;

INSERT INTO dbo.VeterinariaFacturacionTenant
(
    IdLocalidad,
    IdUsuarioFacturacion,
    Estado
)
SELECT
    l.loc_id,
    1000000 + CONVERT(INT, l.loc_id),
    CASE WHEN UPPER(LTRIM(RTRIM(COALESCE(l.loc_estado, 'A')))) = 'A' THEN 1 ELSE 0 END
FROM dbo.vt_localidades l
WHERE CONVERT(BIGINT, l.loc_id) BETWEEN 1 AND 1000000000
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.VeterinariaFacturacionTenant t
      WHERE t.IdLocalidad = l.loc_id
  );

IF OBJECT_ID(N'dbo.ClienteVeterinariaMap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClienteVeterinariaMap
    (
        PaCodigoVeterinaria INT NOT NULL,
        CodClienteFacturacion INT NOT NULL,
        FechaSincronizacion DATETIME2 NOT NULL
            CONSTRAINT DF_ClienteVeterinariaMap_Fecha DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_ClienteVeterinariaMap PRIMARY KEY (PaCodigoVeterinaria),
        CONSTRAINT UQ_ClienteVeterinariaMap_Facturacion UNIQUE (CodClienteFacturacion),
        CONSTRAINT FK_ClienteVeterinariaMap_Veterinaria
            FOREIGN KEY (PaCodigoVeterinaria) REFERENCES dbo.vt_clientes(pa_codigo),
        CONSTRAINT FK_ClienteVeterinariaMap_Facturacion
            FOREIGN KEY (CodClienteFacturacion) REFERENCES dbo.CLIENTES(CODCLIENTE)
    );
END;

INSERT INTO dbo.UsuarioVeterinariaMap
(
    IdUsuarioNumerica,
    UserNumeroVeterinaria,
    TipoVinculacion,
    Confirmado
)
SELECT n.IdUsuario, v.user_numero, 'MIGRACION_VET', 1
FROM dbo.vt_usuarios v
JOIN dbo.Usuarios n
  ON n.IdUsuario = CONVERT(INT, v.user_numero)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.UsuarioVeterinariaMap m
    WHERE m.IdUsuarioNumerica = n.IdUsuario
       OR m.UserNumeroVeterinaria = v.user_numero
);

GO

CREATE OR ALTER VIEW dbo.vw_UsuariosIntegrados
AS
    SELECT
        n.IdUsuario,
        m.UserNumeroVeterinaria,
        n.Nombres,
        n.Apellidos,
        n.Email,
        n.Identificacion,
        n.IdTipoUsuario,
        n.Estado,
        m.TipoVinculacion,
        m.Confirmado
    FROM dbo.Usuarios n
    LEFT JOIN dbo.UsuarioVeterinariaMap m
      ON m.IdUsuarioNumerica = n.IdUsuario;
GO

/* Resultado de control de los usuarios veterinarios integrados. */
SELECT
    v.user_numero AS UserNumeroVeterinaria,
    v.user_login AS LoginVeterinaria,
    v.user_nombre AS NombreVeterinaria,
    v.user_email AS EmailVeterinaria,
    CASE
        WHEN m.UserNumeroVeterinaria IS NULL THEN 'PENDIENTE_VINCULAR'
        WHEN m.Confirmado = 0 THEN 'PENDIENTE_CONFIRMAR'
        ELSE 'CONFIRMADO'
    END AS EstadoIntegracion
FROM dbo.vt_usuarios v
LEFT JOIN dbo.UsuarioVeterinariaMap m
  ON m.UserNumeroVeterinaria = v.user_numero
ORDER BY v.user_numero;

SELECT Grupo, COUNT(*) AS TablasPreparadas
FROM #TablasMigracion
GROUP BY Grupo
ORDER BY Grupo;

PRINT 'Integracion estructural terminada.';
PRINT 'Revise usuarios pendientes antes de habilitar el inicio de sesion unificado.';
PRINT 'Los hashes SHA-1 de Veterinaria no fueron copiados sobre los hashes BCrypt.';
GO
