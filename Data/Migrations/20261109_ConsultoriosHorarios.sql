IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_piso') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_piso NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_consultorio') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_consultorio NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_vigencia_desde') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_vigencia_desde DATE NULL;
IF COL_LENGTH('dbo.vt_horariosMedicos', 'hr_vigencia_hasta') IS NULL
    ALTER TABLE dbo.vt_horariosMedicos ADD hr_vigencia_hasta DATE NULL;
