-- Ejecutar en la base clínica que contiene dbo.vt_medicinas.
IF OBJECT_ID(N'dbo.vt_medicinas', N'U') IS NULL
    THROW 50001, 'Ejecute el script en la base de datos clínica.', 1;

IF OBJECT_ID(N'dbo.vt_advertencias_medicamento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.vt_advertencias_medicamento
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nhcl int NOT NULL,
        CodigoProducto varchar(50) NOT NULL,
        Medicamento nvarchar(300) NOT NULL,
        Motivo nvarchar(500) NOT NULL,
        Inicio date NOT NULL,
        Fin date NULL,
        UsuarioRegistro int NOT NULL,
        FechaRegistroUtc datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_vt_advertencias_medicamento_fechas CHECK (Fin IS NULL OR Fin >= Inicio)
    );

    CREATE INDEX IX_vt_advertencias_medicamento_nhcl
        ON dbo.vt_advertencias_medicamento (Nhcl, Inicio DESC);
END;
