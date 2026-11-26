IF OBJECT_ID(N'dbo.hvt_ficha_hospitalizacion_detalle', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.hvt_ficha_hospitalizacion_detalle (
        idorden int NOT NULL,
        nhcl int NOT NULL,
        hoja int NOT NULL,
        datos nvarchar(max) NOT NULL,
        CONSTRAINT PK_hvt_ficha_hospitalizacion_detalle PRIMARY KEY (idorden, nhcl, hoja),
        CONSTRAINT CK_hvt_ficha_hospitalizacion_detalle_datos CHECK (ISJSON(datos) = 1)
    );
END;
