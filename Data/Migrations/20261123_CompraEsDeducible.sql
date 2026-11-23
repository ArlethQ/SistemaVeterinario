USE [VeterinariaFacturacion];
GO

IF COL_LENGTH(N'dbo.COMPRASFACTURA', N'esDeducible') IS NULL
    ALTER TABLE dbo.COMPRASFACTURA ADD esDeducible bit NULL;
