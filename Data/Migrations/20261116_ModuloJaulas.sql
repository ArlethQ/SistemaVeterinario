IF OBJECT_ID('dbo.vt_jaulas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.vt_jaulas (
        jau_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        jau_codigo NVARCHAR(50) NOT NULL,
        jau_localidad INT NOT NULL,
        jau_piso INT NOT NULL,
        jau_consultorio INT NOT NULL,
        jau_observacion NVARCHAR(200) NULL,
        jau_ocupada BIT NOT NULL CONSTRAINT DF_vt_jaulas_ocupada DEFAULT(0),
        jau_nhcl_actual INT NULL,
        jau_orden_actual INT NULL,
        jau_estado CHAR(1) NOT NULL CONSTRAINT DF_vt_jaulas_estado DEFAULT('A'),
        CONSTRAINT FK_vt_jaulas_piso FOREIGN KEY (jau_piso) REFERENCES dbo.vt_pisos(piso_id),
        CONSTRAINT FK_vt_jaulas_consultorio FOREIGN KEY (jau_consultorio) REFERENCES dbo.vt_consultorios(cons_id),
        CONSTRAINT UQ_vt_jaulas_consultorio_codigo UNIQUE (jau_consultorio, jau_codigo)
    );
END;
