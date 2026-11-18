using Microsoft.EntityFrameworkCore;
using Simetric.Data;

namespace Simetric.Services.EContax;

internal static class EContaxActivosFijosSchema
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static volatile bool activosInicializados;
    private static volatile bool intangiblesInicializados;

    internal static string TablaIntangible(string nombre) => nombre
        .Replace("ECONTAX_AF_", "ECONTAX_AM_", StringComparison.Ordinal)
        .Replace("ECONTAX_AM_ACTIVO", "ECONTAX_AM_INTANGIBLE", StringComparison.Ordinal)
        .Replace("ECONTAX_AM_DEPRECIACION", "ECONTAX_AM_AMORTIZACION", StringComparison.Ordinal);

    internal static async Task EnsureAsync(AppDbContext db, bool intangibles)
    {
        if (intangibles ? intangiblesInicializados : activosInicializados) return;
        await Gate.WaitAsync();
        try
        {
            if (intangibles ? intangiblesInicializados : activosInicializados) return;
            await db.Database.ExecuteSqlRawAsync(intangibles ? SqlIntangibles : Sql);
            if (intangibles) intangiblesInicializados = true;
            else activosInicializados = true;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static readonly string SqlIntangibles = TablaIntangible(Sql);

    private const string Sql = """
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult int;
EXEC @lockResult = sp_getapplock @Resource = N'ECONTAX_AF_SCHEMA',
    @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 15000;
IF @lockResult < 0 THROW 50001, N'No se pudo inicializar Activos Fijos.', 1;

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
COMMIT TRANSACTION;
""";
}
