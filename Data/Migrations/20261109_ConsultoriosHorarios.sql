IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_piso') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_piso NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_consultorio') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_consultorio NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_vigencia_desde') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_vigencia_desde DATE NULL;
IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_vigencia_hasta') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_vigencia_hasta DATE NULL;

IF OBJECT_ID('dbo.vt_pisos','U') IS NULL
BEGIN
    CREATE TABLE dbo.vt_pisos(
        piso_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        piso_nombre NVARCHAR(100) NOT NULL,
        piso_localidad INT NOT NULL,
        piso_estado CHAR(1) NOT NULL CONSTRAINT DF_vt_pisos_estado DEFAULT('A'),
        CONSTRAINT UQ_vt_pisos_localidad_nombre UNIQUE(piso_localidad,piso_nombre)
    );
END;

IF OBJECT_ID('dbo.vt_consultorios','U') IS NULL
BEGIN
    CREATE TABLE dbo.vt_consultorios(
        cons_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        cons_nombre NVARCHAR(100) NOT NULL,
        cons_piso INT NOT NULL,
        cons_estado CHAR(1) NOT NULL CONSTRAINT DF_vt_consultorios_estado DEFAULT('A'),
        CONSTRAINT FK_vt_consultorios_piso FOREIGN KEY(cons_piso) REFERENCES dbo.vt_pisos(piso_id),
        CONSTRAINT UQ_vt_consultorios_piso_nombre UNIQUE(cons_piso,cons_nombre)
    );
END;
