/*
  Columnas presentes en vt_personales.aspx.vb y ausentes en algunos ambientes.
  Script idempotente. Ejecutar únicamente después de respaldo y validación en pruebas.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.vt_clientes', 'pa_emailfactura') IS NULL
    ALTER TABLE dbo.vt_clientes ADD pa_emailfactura VARCHAR(150) NULL;

IF COL_LENGTH('dbo.vt_clientes', 'pa_grupocliente') IS NULL
    ALTER TABLE dbo.vt_clientes ADD pa_grupocliente VARCHAR(50) NULL;

IF COL_LENGTH('dbo.vt_clientes', 'pa_terminospago') IS NULL
    ALTER TABLE dbo.vt_clientes ADD pa_terminospago VARCHAR(50) NULL;

IF COL_LENGTH('dbo.vt_clientes', 'pa_tipopersona') IS NULL
    ALTER TABLE dbo.vt_clientes ADD pa_tipopersona VARCHAR(50) NULL;

IF COL_LENGTH('dbo.vt_clientes', 'pa_genero') IS NULL
    ALTER TABLE dbo.vt_clientes ADD pa_genero INT NULL;

IF COL_LENGTH('dbo.vt_clientes', 'pa_formapago') IS NULL
    ALTER TABLE dbo.vt_clientes ADD pa_formapago VARCHAR(50) NULL;

IF COL_LENGTH('dbo.vt_clientes', 'pa_estadocivil') IS NULL
    ALTER TABLE dbo.vt_clientes ADD pa_estadocivil INT NULL;

COMMIT TRANSACTION;
