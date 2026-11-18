-- Esquema requerido por las nuevas pantallas contables de e-Contax.
-- Ejecutar el archivo completo en SQL Server sobre la base integrada existente.
-- El sistema inicializa el plan de cuentas por empresa al ingresar al módulo.
-- Las tablas existentes se conservan. Este script no importa datos históricos.

USE [VeterinariaFacturacion];

IF DB_NAME() <> N'VeterinariaFacturacion'
    THROW 50000, N'Ejecute el script en VeterinariaFacturacion.', 1;

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock @Resource = N'ECONTAX_AF_SCHEMA',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 15000;
    IF @lockResult < 0
        THROW 50001, N'No se pudo obtener el bloqueo del esquema de activos fijos.', 1;

    EXEC @lockResult = sys.sp_getapplock @Resource = N'ECONTAX_AM_SCHEMA',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 15000;
    IF @lockResult < 0
        THROW 50002, N'No se pudo obtener el bloqueo del esquema de intangibles.', 1;

    -- Plan de cuentas, asientos, bancos y anticipos
    IF OBJECT_ID(N'[dbo].[ECONTAX_CUENTA_CONTABLE]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_CUENTA_CONTABLE] (
            [IdCuenta] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdEmpresa] INT NOT NULL,
            [Codigo] NVARCHAR(40) NOT NULL,
            [Nombre] NVARCHAR(250) NOT NULL,
            [CodigoPadre] NVARCHAR(40) NULL,
            [Nivel] INT NOT NULL DEFAULT(1),
            [TipoCuenta] NVARCHAR(20) NOT NULL,
            [Naturaleza] NVARCHAR(15) NOT NULL,
            [EsMovimiento] BIT NOT NULL DEFAULT(1),
            [AceptaAuxiliar] BIT NOT NULL DEFAULT(1),
            [ClasificacionFlujo] NVARCHAR(20) NOT NULL DEFAULT('OPERACION'),
            [Activa] BIT NOT NULL DEFAULT(1),
            [FechaCreacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
            [FechaActualizacion] DATETIME2 NULL,
            CONSTRAINT [UX_ECONTAX_CUENTA_CONTABLE_EMP_COD] UNIQUE ([IdEmpresa], [Codigo])
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_CONFIG_CUENTA_CONTABLE]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_CONFIG_CUENTA_CONTABLE] (
            [IdConfig] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdEmpresa] INT NOT NULL,
            [Clave] NVARCHAR(60) NOT NULL,
            [Descripcion] NVARCHAR(200) NOT NULL,
            [CodigoCuenta] NVARCHAR(40) NOT NULL,
            [FechaActualizacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
            CONSTRAINT [UX_ECONTAX_CONFIG_CUENTA_EMP_CLAVE] UNIQUE ([IdEmpresa], [Clave])
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_ASIENTO_CONTABLE]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_ASIENTO_CONTABLE] (
            [IdAsiento] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdEmpresa] INT NOT NULL,
            [IdSucursal] INT NULL,
            [NumeroAsiento] NVARCHAR(30) NOT NULL,
            [Fecha] DATETIME2 NOT NULL,
            [TipoAsiento] NVARCHAR(25) NOT NULL DEFAULT('DIARIO'),
            [ModuloOrigen] NVARCHAR(40) NOT NULL DEFAULT('MANUAL'),
            [IdDocumentoOrigen] INT NULL,
            [ReferenciaDocumento] NVARCHAR(80) NULL,
            [Glosa] NVARCHAR(500) NOT NULL,
            [TotalDebe] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [TotalHaber] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [Estado] NVARCHAR(20) NOT NULL DEFAULT('CONTABILIZADO'),
            [IdAsientoReverso] INT NULL,
            [CreadoPor] INT NOT NULL,
            [FechaCreacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
            [ActualizadoPor] INT NULL,
            [FechaActualizacion] DATETIME2 NULL,
            CONSTRAINT [UX_ECONTAX_ASIENTO_EMP_NUM] UNIQUE ([IdEmpresa], [NumeroAsiento])
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_ASIENTO_DETALLE]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_ASIENTO_DETALLE] (
            [IdDetalle] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdAsiento] INT NOT NULL,
            [IdCuenta] INT NOT NULL,
            [CodigoCuenta] NVARCHAR(40) NOT NULL,
            [NombreCuenta] NVARCHAR(250) NOT NULL,
            [Linea] INT NOT NULL,
            [Detalle] NVARCHAR(300) NULL,
            [Debe] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [Haber] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [TipoTercero] NVARCHAR(30) NULL,
            [IdTercero] INT NULL,
            [IdentificacionTercero] NVARCHAR(30) NULL,
            [NombreTercero] NVARCHAR(200) NULL,
            [CentroCosto] NVARCHAR(60) NULL,
            CONSTRAINT [FK_ECONTAX_ASIENTO_DETALLE_CAB] FOREIGN KEY ([IdAsiento])
                REFERENCES [dbo].[ECONTAX_ASIENTO_CONTABLE]([IdAsiento]) ON DELETE CASCADE
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_CUENTA_BANCO]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_CUENTA_BANCO] (
            [IdCuentaBanco] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdEmpresa] INT NOT NULL,
            [IdSucursal] INT NULL,
            [Banco] NVARCHAR(120) NOT NULL,
            [NumeroCuenta] NVARCHAR(60) NOT NULL,
            [TipoCuenta] NVARCHAR(30) NOT NULL DEFAULT('CORRIENTE'),
            [Moneda] NVARCHAR(10) NOT NULL DEFAULT('USD'),
            [CodigoCuentaContable] NVARCHAR(40) NOT NULL DEFAULT('1.1.1.3.1'),
            [SaldoInicial] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [SaldoActual] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [Activa] BIT NOT NULL DEFAULT(1),
            [FechaCreacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_MOVIMIENTO_BANCO]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_MOVIMIENTO_BANCO] (
            [IdMovimientoBanco] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdEmpresa] INT NOT NULL,
            [IdCuentaBanco] INT NOT NULL,
            [Fecha] DATETIME2 NOT NULL,
            [Tipo] NVARCHAR(30) NOT NULL,
            [NumeroReferencia] NVARCHAR(60) NULL,
            [Beneficiario] NVARCHAR(200) NULL,
            [Concepto] NVARCHAR(300) NOT NULL,
            [Monto] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [Conciliado] BIT NOT NULL DEFAULT(0),
            [IdConciliacion] INT NULL,
            [ModuloOrigen] NVARCHAR(40) NULL,
            [IdDocumentoOrigen] INT NULL,
            [IdAsiento] INT NULL,
            [CreadoPor] INT NOT NULL,
            [FechaCreacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_CONCILIACION_BANCARIA]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_CONCILIACION_BANCARIA] (
            [IdConciliacion] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdEmpresa] INT NOT NULL,
            [IdCuentaBanco] INT NOT NULL,
            [Periodo] NVARCHAR(20) NOT NULL,
            [FechaCorte] DATETIME2 NOT NULL,
            [SaldoLibro] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [SaldoBanco] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [Diferencia] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [Estado] NVARCHAR(20) NOT NULL DEFAULT('BORRADOR'),
            [Observacion] NVARCHAR(400) NULL,
            [CreadoPor] INT NOT NULL,
            [FechaCreacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_ANTICIPO]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_ANTICIPO] (
            [IdAnticipo] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdEmpresa] INT NOT NULL,
            [IdSucursal] INT NULL,
            [NumeroAnticipo] NVARCHAR(30) NOT NULL,
            [Tipo] NVARCHAR(20) NOT NULL,
            [IdTercero] INT NULL,
            [IdentificacionTercero] NVARCHAR(30) NOT NULL,
            [NombreTercero] NVARCHAR(200) NOT NULL,
            [Fecha] DATETIME2 NOT NULL,
            [MontoTotal] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [SaldoDisponible] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [FormaPago] NVARCHAR(30) NOT NULL DEFAULT('TRANSFERENCIA'),
            [IdCuentaBanco] INT NULL,
            [ReferenciaPago] NVARCHAR(80) NULL,
            [Observacion] NVARCHAR(400) NULL,
            [Estado] NVARCHAR(20) NOT NULL DEFAULT('ACTIVO'),
            [IdAsiento] INT NULL,
            [CreadoPor] INT NOT NULL,
            [FechaCreacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
            CONSTRAINT [UX_ECONTAX_ANTICIPO_EMP_NUM] UNIQUE ([IdEmpresa], [NumeroAnticipo])
        );
    END;

    IF OBJECT_ID(N'[dbo].[ECONTAX_ANTICIPO_APLICACION]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_ANTICIPO_APLICACION] (
            [IdAplicacion] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [IdAnticipo] INT NOT NULL,
            [IdEmpresa] INT NOT NULL,
            [TipoDocumentoDestino] NVARCHAR(30) NOT NULL,
            [IdDocumentoDestino] INT NULL,
            [ReferenciaDestino] NVARCHAR(80) NOT NULL,
            [Fecha] DATETIME2 NOT NULL,
            [MontoAplicado] DECIMAL(18,2) NOT NULL DEFAULT(0),
            [Observacion] NVARCHAR(300) NULL,
            [IdAsiento] INT NULL,
            [CreadoPor] INT NOT NULL,
            [FechaCreacion] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
        );
    END;

    -- Activos fijos y depreciación
    IF OBJECT_ID(N'dbo.ECONTAX_AF_CATEGORIA', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AF_CATEGORIA] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [Nombre] nvarchar(120) NOT NULL,
            [VidaUtilMeses] int NOT NULL,
            [IdCuentaActivo] int NOT NULL,
            [IdCuentaGasto] int NOT NULL,
            [IdCuentaAcumulada] int NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AF_CATEGORIA_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AF_CATEGORIA] ON [dbo].[ECONTAX_AF_CATEGORIA] ([IdEmpresa], [Nombre]);
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AF_ACTIVO', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AF_ACTIVO] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [IdCategoria] int NOT NULL,
            [Codigo] nvarchar(40) NOT NULL,
            [Nombre] nvarchar(180) NOT NULL,
            [PuestaEnServicio] date NOT NULL,
            [ValorResidual] decimal(18,2) NOT NULL,
            [VidaUtilMeses] int NOT NULL,
            [Referencia] nvarchar(160) NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AF_ACTIVO_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AF_ACTIVO_IdCategoria] FOREIGN KEY ([IdEmpresa], [IdCategoria]) REFERENCES [dbo].[ECONTAX_AF_CATEGORIA] ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AF_ACTIVO] ON [dbo].[ECONTAX_AF_ACTIVO] ([IdEmpresa], [Codigo]);
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AF_MOVIMIENTO', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AF_MOVIMIENTO] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [IdActivo] int NOT NULL,
            [Fecha] date NOT NULL,
            [Tipo] nvarchar(20) NOT NULL,
            [CambioCosto] decimal(18,2) NOT NULL,
            [CambioDepreciacion] decimal(18,2) NOT NULL,
            [Ubicacion] nvarchar(120) NOT NULL,
            [Responsable] nvarchar(120) NOT NULL,
            [Motivo] nvarchar(250) NOT NULL,
            [IdUsuario] int NOT NULL,
            [RegistradoUtc] datetime2 NOT NULL,
            [IdOperacion] uniqueidentifier NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AF_MOVIMIENTO_OPERACION] UNIQUE ([IdEmpresa], [IdOperacion]),
            CONSTRAINT [UQ_ECONTAX_AF_MOVIMIENTO_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AF_MOVIMIENTO_IdActivo] FOREIGN KEY ([IdEmpresa], [IdActivo]) REFERENCES [dbo].[ECONTAX_AF_ACTIVO] ([IdEmpresa], [Id])
        );
        CREATE INDEX [IX_ECONTAX_AF_MOVIMIENTO] ON [dbo].[ECONTAX_AF_MOVIMIENTO] ([IdEmpresa], [IdActivo], [Fecha]);
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AF_DEPRECIACION_LOTE', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AF_DEPRECIACION_LOTE] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [Periodo] date NOT NULL,
            [Estado] nvarchar(20) NOT NULL,
            [IdAsiento] int NULL,
            [IdAsientoReverso] int NULL,
            [CreadoPor] int NOT NULL,
            [AprobadoPor] int NULL,
            [ContabilizadoPor] int NULL,
            [ReversadoPor] int NULL,
            [CreadoUtc] datetime2 NOT NULL,
            [AprobadoUtc] datetime2 NULL,
            [ContabilizadoUtc] datetime2 NULL,
            [ReversadoUtc] datetime2 NULL,
            CONSTRAINT [UQ_ECONTAX_AF_DEPRECIACION_LOTE_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AF_DEPRECIACION_LOTE] ON [dbo].[ECONTAX_AF_DEPRECIACION_LOTE] ([IdEmpresa], [Periodo]) WHERE [Estado] <> N'REVERSADO' AND [Estado] <> N'DESCARTADO';
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AF_DEPRECIACION_DETALLE', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AF_DEPRECIACION_DETALLE] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [IdLote] int NOT NULL,
            [IdActivo] int NOT NULL,
            [Importe] decimal(18,2) NOT NULL,
            [IdCuentaGasto] int NOT NULL,
            [IdCuentaAcumulada] int NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AF_DEPRECIACION_DETALLE_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AF_DEPRECIACION_DETALLE_IdActivo] FOREIGN KEY ([IdEmpresa], [IdActivo]) REFERENCES [dbo].[ECONTAX_AF_ACTIVO] ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AF_DEPRECIACION_DETALLE_IdLote] FOREIGN KEY ([IdEmpresa], [IdLote]) REFERENCES [dbo].[ECONTAX_AF_DEPRECIACION_LOTE] ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AF_DEPRECIACION_DETALLE] ON [dbo].[ECONTAX_AF_DEPRECIACION_DETALLE] ([IdEmpresa], [IdLote], [IdActivo]);
    END;

    -- Intangibles y amortización
    IF OBJECT_ID(N'dbo.ECONTAX_AM_CATEGORIA', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AM_CATEGORIA] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [Nombre] nvarchar(120) NOT NULL,
            [VidaUtilMeses] int NOT NULL,
            [IdCuentaActivo] int NOT NULL,
            [IdCuentaGasto] int NOT NULL,
            [IdCuentaAcumulada] int NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AM_CATEGORIA_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AM_CATEGORIA] ON [dbo].[ECONTAX_AM_CATEGORIA] ([IdEmpresa], [Nombre]);
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AM_INTANGIBLE', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AM_INTANGIBLE] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [IdCategoria] int NOT NULL,
            [Codigo] nvarchar(40) NOT NULL,
            [Nombre] nvarchar(180) NOT NULL,
            [PuestaEnServicio] date NOT NULL,
            [ValorResidual] decimal(18,2) NOT NULL,
            [VidaUtilMeses] int NOT NULL,
            [Referencia] nvarchar(160) NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AM_INTANGIBLE_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AM_INTANGIBLE_IdCategoria] FOREIGN KEY ([IdEmpresa], [IdCategoria]) REFERENCES [dbo].[ECONTAX_AM_CATEGORIA] ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AM_INTANGIBLE] ON [dbo].[ECONTAX_AM_INTANGIBLE] ([IdEmpresa], [Codigo]);
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AM_MOVIMIENTO', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AM_MOVIMIENTO] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [IdActivo] int NOT NULL,
            [Fecha] date NOT NULL,
            [Tipo] nvarchar(20) NOT NULL,
            [CambioCosto] decimal(18,2) NOT NULL,
            [CambioDepreciacion] decimal(18,2) NOT NULL,
            [Ubicacion] nvarchar(120) NOT NULL,
            [Responsable] nvarchar(120) NOT NULL,
            [Motivo] nvarchar(250) NOT NULL,
            [IdUsuario] int NOT NULL,
            [RegistradoUtc] datetime2 NOT NULL,
            [IdOperacion] uniqueidentifier NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AM_MOVIMIENTO_OPERACION] UNIQUE ([IdEmpresa], [IdOperacion]),
            CONSTRAINT [UQ_ECONTAX_AM_MOVIMIENTO_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AM_MOVIMIENTO_IdActivo] FOREIGN KEY ([IdEmpresa], [IdActivo]) REFERENCES [dbo].[ECONTAX_AM_INTANGIBLE] ([IdEmpresa], [Id])
        );
        CREATE INDEX [IX_ECONTAX_AM_MOVIMIENTO] ON [dbo].[ECONTAX_AM_MOVIMIENTO] ([IdEmpresa], [IdActivo], [Fecha]);
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AM_AMORTIZACION_LOTE', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AM_AMORTIZACION_LOTE] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [Periodo] date NOT NULL,
            [Estado] nvarchar(20) NOT NULL,
            [IdAsiento] int NULL,
            [IdAsientoReverso] int NULL,
            [CreadoPor] int NOT NULL,
            [AprobadoPor] int NULL,
            [ContabilizadoPor] int NULL,
            [ReversadoPor] int NULL,
            [CreadoUtc] datetime2 NOT NULL,
            [AprobadoUtc] datetime2 NULL,
            [ContabilizadoUtc] datetime2 NULL,
            [ReversadoUtc] datetime2 NULL,
            CONSTRAINT [UQ_ECONTAX_AM_AMORTIZACION_LOTE_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AM_AMORTIZACION_LOTE] ON [dbo].[ECONTAX_AM_AMORTIZACION_LOTE] ([IdEmpresa], [Periodo]) WHERE [Estado] <> N'REVERSADO' AND [Estado] <> N'DESCARTADO';
    END;

    IF OBJECT_ID(N'dbo.ECONTAX_AM_AMORTIZACION_DETALLE', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ECONTAX_AM_AMORTIZACION_DETALLE] (
            [Id] int IDENTITY(1,1) PRIMARY KEY,
            [IdEmpresa] int NOT NULL,
            [IdLote] int NOT NULL,
            [IdActivo] int NOT NULL,
            [Importe] decimal(18,2) NOT NULL,
            [IdCuentaGasto] int NOT NULL,
            [IdCuentaAcumulada] int NOT NULL,
            CONSTRAINT [UQ_ECONTAX_AM_AMORTIZACION_DETALLE_EMPRESA_ID] UNIQUE ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AM_AMORTIZACION_DETALLE_IdActivo] FOREIGN KEY ([IdEmpresa], [IdActivo]) REFERENCES [dbo].[ECONTAX_AM_INTANGIBLE] ([IdEmpresa], [Id]),
            CONSTRAINT [FK_ECONTAX_AM_AMORTIZACION_DETALLE_IdLote] FOREIGN KEY ([IdEmpresa], [IdLote]) REFERENCES [dbo].[ECONTAX_AM_AMORTIZACION_LOTE] ([IdEmpresa], [Id])
        );
        CREATE UNIQUE INDEX [IX_ECONTAX_AM_AMORTIZACION_DETALLE] ON [dbo].[ECONTAX_AM_AMORTIZACION_DETALLE] ([IdEmpresa], [IdLote], [IdActivo]);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT N'Esquema contable de e-Contax preparado en VeterinariaFacturacion.';
