/*
  Unifica el contexto empresarial de Veterinaria, Contabilidad y Facturacion.

  Uso recomendado:
  1. Ejecutar primero en una copia de la base.
  2. Revisar los SELECT finales (usuarios/localidades sin empresa y conflictos).
  3. Completar los mapeos manuales de la seccion "MIGRACION DE DATOS".

  El script es idempotente. No elimina usuarios ni datos clinicos.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.EMPRESA', N'U') IS NULL
        THROW 51000, 'No existe dbo.EMPRESA. Ejecute primero el bootstrap de E-Contax.', 1;

    IF OBJECT_ID(N'dbo.SUCURSAL', N'U') IS NULL
        THROW 51001, 'No existe dbo.SUCURSAL. Ejecute primero el bootstrap de E-Contax.', 1;

    IF OBJECT_ID(N'dbo.vt_usuarios', N'U') IS NULL
        THROW 51002, 'No existe dbo.vt_usuarios.', 1;

    IF OBJECT_ID(N'dbo.vt_localidades', N'U') IS NULL
        THROW 51003, 'No existe dbo.vt_localidades.', 1;

    /* La empresa es el tenant comun. */
    IF COL_LENGTH(N'dbo.vt_usuarios', N'idEmpresa') IS NULL
        ALTER TABLE dbo.vt_usuarios ADD idEmpresa INT NULL;

    /* Puente temporal entre la identidad veterinaria y la identidad contable. */
    IF COL_LENGTH(N'dbo.vt_usuarios', N'idUsuarioFacturacion') IS NULL
        ALTER TABLE dbo.vt_usuarios ADD idUsuarioFacturacion INT NULL;

    IF COL_LENGTH(N'dbo.vt_localidades', N'idEmpresa') IS NULL
        ALTER TABLE dbo.vt_localidades ADD idEmpresa INT NULL;

    IF COL_LENGTH(N'dbo.vt_localidades', N'idSucursal') IS NULL
        ALTER TABLE dbo.vt_localidades ADD idSucursal INT NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* SQL Server debe iniciar un lote nuevo para reconocer las columnas agregadas. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.FK_vt_usuarios_EMPRESA', N'F') IS NULL
        ALTER TABLE dbo.vt_usuarios WITH CHECK
        ADD CONSTRAINT FK_vt_usuarios_EMPRESA
            FOREIGN KEY (idEmpresa) REFERENCES dbo.EMPRESA(idEmpresa);

    IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.FK_vt_usuarios_Usuarios', N'F') IS NULL
        ALTER TABLE dbo.vt_usuarios WITH CHECK
        ADD CONSTRAINT FK_vt_usuarios_Usuarios
            FOREIGN KEY (idUsuarioFacturacion) REFERENCES dbo.Usuarios(IdUsuario);

    IF OBJECT_ID(N'dbo.FK_vt_localidades_EMPRESA', N'F') IS NULL
        ALTER TABLE dbo.vt_localidades WITH CHECK
        ADD CONSTRAINT FK_vt_localidades_EMPRESA
            FOREIGN KEY (idEmpresa) REFERENCES dbo.EMPRESA(idEmpresa);

    IF OBJECT_ID(N'dbo.FK_vt_localidades_SUCURSAL', N'F') IS NULL
        ALTER TABLE dbo.vt_localidades WITH CHECK
        ADD CONSTRAINT FK_vt_localidades_SUCURSAL
            FOREIGN KEY (idEmpresa, idSucursal)
            REFERENCES dbo.SUCURSAL(idEmpresa, idSucursal);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.vt_usuarios') AND name = N'IX_vt_usuarios_idEmpresa_estado')
        CREATE INDEX IX_vt_usuarios_idEmpresa_estado
            ON dbo.vt_usuarios(idEmpresa, user_estado)
            INCLUDE (user_numero, user_login, user_nombre);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.vt_usuarios') AND name = N'UX_vt_usuarios_idUsuarioFacturacion')
        CREATE UNIQUE INDEX UX_vt_usuarios_idUsuarioFacturacion
            ON dbo.vt_usuarios(idUsuarioFacturacion)
            WHERE idUsuarioFacturacion IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.vt_localidades') AND name = N'IX_vt_localidades_empresa_sucursal')
        CREATE INDEX IX_vt_localidades_empresa_sucursal
            ON dbo.vt_localidades(idEmpresa, idSucursal, loc_estado)
            INCLUDE (loc_id, loc_descripcion);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* MIGRACION DE DATOS
   Si solo existe una empresa activa, se asigna como tenant inicial sin sobrescribir
   asignaciones existentes. En una base multiempresa, use los UPDATE manuales de abajo.
*/
DECLARE @EmpresaUnica INT = NULL;

IF (SELECT COUNT(*) FROM dbo.EMPRESA WHERE estado = 1) = 1
    SELECT @EmpresaUnica = idEmpresa FROM dbo.EMPRESA WHERE estado = 1;

IF @EmpresaUnica IS NOT NULL
BEGIN
    UPDATE dbo.vt_usuarios
       SET idEmpresa = @EmpresaUnica
     WHERE idEmpresa IS NULL;

    UPDATE l
       SET l.idEmpresa = @EmpresaUnica,
           l.idSucursal = COALESCE(
               (SELECT TOP (1) s.idSucursal
                  FROM dbo.SUCURSAL s
                 WHERE s.idEmpresa = @EmpresaUnica AND s.estado = 1
                 ORDER BY CASE WHEN s.idSucursal = 1 THEN 0 ELSE 1 END, s.idSucursal),
               l.idSucursal)
      FROM dbo.vt_localidades l
     WHERE l.idEmpresa IS NULL;
END;
GO

/*
  Ejemplos para bases con varias empresas (reemplazar valores y ejecutar manualmente):

  UPDATE dbo.vt_usuarios
     SET idEmpresa = 2, idUsuarioFacturacion = 25
   WHERE user_numero = 10;

  UPDATE dbo.vt_localidades
     SET idEmpresa = 2, idSucursal = 1
   WHERE loc_id = 3;

  La asignacion usuario-localidad debe permanecer dentro de la misma empresa.
*/

CREATE OR ALTER VIEW dbo.VW_USUARIO_EMPRESA_UNIFICADO
AS
    SELECT
        vu.user_numero AS IdUsuarioVeterinaria,
        vu.idUsuarioFacturacion,
        vu.idEmpresa,
        e.nombre AS Empresa,
        vu.user_login AS LoginVeterinaria,
        vu.user_nombre AS Nombre,
        vu.user_estado AS EstadoVeterinaria
    FROM dbo.vt_usuarios vu
    LEFT JOIN dbo.EMPRESA e ON e.idEmpresa = vu.idEmpresa;
GO

CREATE OR ALTER VIEW dbo.VW_LOCALIDAD_EMPRESA_UNIFICADA
AS
    SELECT
        l.loc_id AS IdLocalidad,
        l.idEmpresa,
        l.idSucursal,
        e.nombre AS Empresa,
        s.nombre AS Sucursal,
        l.loc_descripcion AS Localidad,
        l.loc_estado AS EstadoLocalidad
    FROM dbo.vt_localidades l
    LEFT JOIN dbo.EMPRESA e ON e.idEmpresa = l.idEmpresa
    LEFT JOIN dbo.SUCURSAL s
      ON s.idEmpresa = l.idEmpresa
     AND s.idSucursal = l.idSucursal;
GO

/* Impide que un usuario veterinario reciba acceso a una localidad de otro tenant. */
CREATE OR ALTER TRIGGER dbo.TR_vt_usuarioLocalidades_ValidarEmpresa
ON dbo.vt_usuarioLocalidades
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.vt_usuarios u ON u.user_numero = i.ul_idUsuario
        INNER JOIN dbo.vt_localidades l ON l.loc_id = i.ul_idLocalidad
        WHERE ISNULL(i.ul_estado, 'A') = 'A'
          AND u.idEmpresa IS NOT NULL
          AND l.idEmpresa IS NOT NULL
          AND u.idEmpresa <> l.idEmpresa
    )
    BEGIN
        THROW 51004, 'No se puede asignar al usuario una localidad de otra empresa.', 1;
    END;
END;
GO

/* VALIDACIONES: todos estos resultados deben quedar vacios antes de activar filtros obligatorios. */
SELECT user_numero, user_login, user_nombre
FROM dbo.vt_usuarios
WHERE user_estado = 'A' AND idEmpresa IS NULL;

SELECT loc_id, loc_descripcion
FROM dbo.vt_localidades
WHERE loc_estado = 'A' AND (idEmpresa IS NULL OR idSucursal IS NULL);

SELECT ul.ul_idUsuario, ul.ul_idLocalidad, u.idEmpresa AS EmpresaUsuario, l.idEmpresa AS EmpresaLocalidad
FROM dbo.vt_usuarioLocalidades ul
INNER JOIN dbo.vt_usuarios u ON u.user_numero = ul.ul_idUsuario
INNER JOIN dbo.vt_localidades l ON l.loc_id = ul.ul_idLocalidad
WHERE ISNULL(ul.ul_estado, 'A') = 'A'
  AND (u.idEmpresa IS NULL OR l.idEmpresa IS NULL OR u.idEmpresa <> l.idEmpresa);

SELECT idUsuarioFacturacion, COUNT(*) AS Repeticiones
FROM dbo.vt_usuarios
WHERE idUsuarioFacturacion IS NOT NULL
GROUP BY idUsuarioFacturacion
HAVING COUNT(*) > 1;
GO
