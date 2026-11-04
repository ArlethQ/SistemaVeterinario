SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.vt_autorizaciones', N'U') IS NULL
        THROW 51000, 'No existe la tabla dbo.vt_autorizaciones.', 1;

    IF COL_LENGTH(N'dbo.vt_autorizaciones', N'autorizacion_archivo_url') IS NULL
        ALTER TABLE dbo.vt_autorizaciones
            ADD autorizacion_archivo_url nvarchar(1000) NULL;

    IF COL_LENGTH(N'dbo.vt_autorizaciones', N'autorizacion_archivo_nombre') IS NULL
        ALTER TABLE dbo.vt_autorizaciones
            ADD autorizacion_archivo_nombre nvarchar(260) NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.vt_autorizaciones')
          AND name = N'IX_vt_autorizaciones_nhcl_fecha')
        CREATE INDEX IX_vt_autorizaciones_nhcl_fecha
            ON dbo.vt_autorizaciones (autorizacion_nhcl, autorizacion_fecha DESC);

    IF OBJECT_ID(N'dbo.vt_formatos', N'U') IS NULL
        THROW 51001, 'No existe la tabla dbo.vt_formatos.', 1;

    IF COL_LENGTH(N'dbo.vt_formatos', N'form_es_sistema') IS NULL
        ALTER TABLE dbo.vt_formatos
            ADD form_es_sistema bit NULL;

    UPDATE dbo.vt_formatos
    SET form_es_sistema = CASE WHEN form_id <= 8 THEN 1 ELSE 0 END
    WHERE form_es_sistema IS NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    COL_LENGTH(N'dbo.vt_autorizaciones', N'autorizacion_archivo_url') AS LongitudUrl,
    COL_LENGTH(N'dbo.vt_autorizaciones', N'autorizacion_archivo_nombre') AS LongitudNombre,
    COUNT(*) AS FormatosDisponibles,
    SUM(CASE WHEN ISNULL(form_es_sistema, 0) = 1 THEN 1 ELSE 0 END) AS FormatosSistema
FROM dbo.vt_formatos;
