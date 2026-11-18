using Microsoft.EntityFrameworkCore;
using Simetric.Data;

namespace Simetric.Services.EContax;

public sealed class EContaxErpSchemaService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static volatile bool _ensured;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EContaxErpSchemaService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task EnsureSchemaAsync()
    {
        if (_ensured)
            return;

        await Gate.WaitAsync();
        try
        {
            if (_ensured)
                return;

            await using var context = await _dbFactory.CreateDbContextAsync();
            await context.Database.ExecuteSqlRawAsync("""
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

""");
            _ensured = true;
        }
        finally
        {
            Gate.Release();
        }
    }
}
