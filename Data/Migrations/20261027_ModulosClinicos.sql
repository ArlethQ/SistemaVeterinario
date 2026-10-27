IF OBJECT_ID('dbo.vt_especialidad_localidad','U') IS NULL
BEGIN
    CREATE TABLE dbo.vt_especialidad_localidad (
        id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        esp_id int NOT NULL,
        id_localidad int NOT NULL,
        estado varchar(1) NOT NULL CONSTRAINT DF_esploc_estado DEFAULT 'A',
        CONSTRAINT UQ_especialidad_localidad UNIQUE (esp_id,id_localidad)
    );
END;

IF OBJECT_ID('dbo.vt_istat','U') IS NULL
BEGIN
    CREATE TABLE dbo.vt_istat (
        sec int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        is_nhcl int NOT NULL,
        is_fecha datetime NOT NULL,
        is_iduser int NOT NULL,
        is_cliente int NOT NULL CONSTRAINT DF_istat_cliente DEFAULT 0,
        is_estado char(1) NOT NULL CONSTRAINT DF_istat_estado DEFAULT 'A',
        is_tipo varchar(10) NULL,
        hematocrito varchar(150) NULL, hemoglobina varchar(150) NULL,
        urea varchar(150) NULL, creatinina varchar(150) NULL, ionized varchar(150) NULL,
        glucosa varchar(150) NULL, chloride varchar(150) NULL, sodio varchar(150) NULL,
        potasio varchar(150) NULL, ph varchar(150) NULL, pressure varchar(150) NULL,
        bicarbonato varchar(150) NULL, carbon varchar(150) NULL, gap varchar(150) NULL,
        base varchar(150) NULL, oxygen varchar(150) NULL, saturation varchar(150) NULL,
        lactato varchar(150) NULL, is_elimina int NULL, is_fechadelete datetime NULL
    );
    CREATE INDEX IX_vt_istat_nhcl_fecha ON dbo.vt_istat(is_nhcl,is_fecha DESC);
END;

IF COL_LENGTH('dbo.vt_fichamedica','med_pregunta') IS NULL
    ALTER TABLE dbo.vt_fichamedica ADD med_pregunta char(1) NOT NULL CONSTRAINT DF_fichamedica_alerta DEFAULT 'N';
IF COL_LENGTH('dbo.vt_fichamedica','estadoparte') IS NULL
    ALTER TABLE dbo.vt_fichamedica ADD estadoparte varchar(30) NULL;
