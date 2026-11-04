[CmdletBinding()]
param(
    [string]$Servidor = "MSI",
    [string]$Usuario = "sa",
    [switch]$AutenticacionWindows,
    [string]$BaseDestino = "VeterinariaFacturacion",
    [string]$BaseNumerica = "db_aab800_numerica",
    [string]$ScriptVeterinaria = "C:\Users\Arleth\OneDrive\Documentos\scriptveterinaria.sql",
    [string]$ScriptNumerica = "C:\Users\Arleth\OneDrive\Documentos\script.sql",
    [switch]$RecrearDestino,
    [int]$IdEmpresaNumerica = 0,
    [int]$IdSucursalNumerica = 0,
    [int]$CodigoEmisorNumerica = 0,
    [int]$UserNumeroAdministradorVet = 0,
    [int]$IdPerfilEContaxAdministrador = 1
)

$ErrorActionPreference = "Stop"

function Assert-SafeSqlName([string]$Value, [string]$ParameterName) {
    if ($Value -notmatch '^[A-Za-z0-9_]+$') {
        throw "$ParameterName contiene caracteres no permitidos: $Value"
    }
}

function Invoke-SqlCmdChecked {
    param([string[]]$Arguments)

    & sqlcmd @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd termino con codigo $LASTEXITCODE. Revise el mensaje anterior."
    }
}

function Invoke-ScalarQuery([string]$Query) {
    $arguments = @($script:ConnectionArguments) + @("-h", "-1", "-W", "-Q", $Query)
    $result = & sqlcmd @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo consultar SQL Server."
    }

    return (($result | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join "").Trim()
}

function New-AdaptedDatabaseBodyScript {
    param(
        [string]$SourcePath,
        [string]$OriginalDatabase,
        [string]$TargetDatabase
    )

    $outputPath = Join-Path ([IO.Path]::GetTempPath()) "carga-$TargetDatabase-$([guid]::NewGuid().ToString('N')).sql"
    $reader = [IO.StreamReader]::new($SourcePath, [Text.Encoding]::UTF8, $true, 1048576)
    $writer = [IO.StreamWriter]::new($outputPath, $false, [Text.UTF8Encoding]::new($true), 1048576)
    try {
        $foundFirstDatabaseUse = $false
        while (($line = $reader.ReadLine()) -ne $null) {
            if (-not $foundFirstDatabaseUse) {
                if ($line.Trim() -ieq "USE [$OriginalDatabase]") {
                    $foundFirstDatabaseUse = $true
                    $writer.WriteLine(":on error exit")
                    $writer.WriteLine("USE [$TargetDatabase]")
                }
                continue
            }

            $adaptedLine = $line.Replace($OriginalDatabase, $TargetDatabase)
            if ($OriginalDatabase -eq "db_a8a74a_hosvet") {
                # Conserva indicaciones clinicas extensas; no se permite truncarlas.
                $adaptedLine = $adaptedLine.Replace(
                    "[med_indicaciones] [varchar](250)",
                    "[med_indicaciones] [varchar](max)")
            }
            $writer.WriteLine($adaptedLine)
        }

        if (-not $foundFirstDatabaseUse) {
            throw "No se encontro USE [$OriginalDatabase] en $SourcePath."
        }
    }
    finally {
        $reader.Dispose()
        $writer.Dispose()
    }

    return $outputPath
}

function Read-PositiveInteger([string]$Prompt) {
    while ($true) {
        $value = 0
        $inputValue = Read-Host $Prompt
        if ([int]::TryParse($inputValue, [ref]$value) -and $value -gt 0) {
            return $value
        }
        Write-Host "Ingrese un numero entero mayor que cero." -ForegroundColor Yellow
    }
}

Assert-SafeSqlName $BaseDestino "BaseDestino"
Assert-SafeSqlName $BaseNumerica "BaseNumerica"

if (-not (Test-Path -LiteralPath $ScriptVeterinaria -PathType Leaf)) {
    throw "No se encontro el script veterinario: $ScriptVeterinaria"
}

if (-not (Test-Path -LiteralPath $ScriptNumerica -PathType Leaf)) {
    throw "No se encontro el script de Numerica: $ScriptNumerica"
}

$integrationScript = Join-Path $PSScriptRoot "002-integracion-numerica-facturacion-econtax.sql"
if (-not (Test-Path -LiteralPath $integrationScript -PathType Leaf)) {
    throw "No se encontro el script de integracion: $integrationScript"
}

$script:ConnectionArguments = @("-S", $Servidor, "-C", "-b", "-r", "1")
$plainPassword = $null

if ($AutenticacionWindows) {
    $script:ConnectionArguments += "-E"
}
else {
    $securePassword = Read-Host "Contrasena SQL para $Usuario@$Servidor" -AsSecureString
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }

    $script:ConnectionArguments += @("-U", $Usuario, "-P", $plainPassword)
}

$escapedDestination = $BaseDestino.Replace("'", "''")
$escapedNumerica = $BaseNumerica.Replace("'", "''")

try {
    Write-Host "1/4 Verificando la base de Numerica..." -ForegroundColor Cyan
    $numericaExists = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'$escapedNumerica') IS NULL THEN 0 ELSE 1 END;"
    if ($numericaExists -eq "0") {
        Write-Host "La base $BaseNumerica no existe. Se creara usando las rutas predeterminadas del servidor."
        Invoke-SqlCmdChecked (@($script:ConnectionArguments) + @("-Q", "CREATE DATABASE [$BaseNumerica];"))
        $adaptedNumericaScript = New-AdaptedDatabaseBodyScript -SourcePath $ScriptNumerica -OriginalDatabase "db_aab800_numerica" -TargetDatabase $BaseNumerica
        try {
            Write-Host "Cargando las tablas y datos actualizados de Numerica..."
            Invoke-SqlCmdChecked (@($script:ConnectionArguments) + @("-d", $BaseNumerica, "-i", $adaptedNumericaScript))
        }
        finally {
            if (Test-Path -LiteralPath $adaptedNumericaScript) {
                Remove-Item -LiteralPath $adaptedNumericaScript -Force
            }
        }
    }
    else {
        $numericaTables = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT COUNT(*) FROM [$BaseNumerica].sys.tables;"
        if ([int]$numericaTables -lt 100) {
            throw "La base $BaseNumerica existe, pero solo contiene $numericaTables tablas. No se sobrescribira automaticamente."
        }
        Write-Host "$BaseNumerica ya esta cargada ($numericaTables tablas); se conserva sin volver a insertar datos."
    }

    Write-Host "2/4 Preparando la base veterinaria completa..." -ForegroundColor Cyan
    $destinationExists = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'$escapedDestination') IS NULL THEN 0 ELSE 1 END;"
    if ($RecrearDestino -and $destinationExists -eq "1") {
        Write-Host "Eliminando exclusivamente la base destino incompleta $BaseDestino..." -ForegroundColor Yellow
        Invoke-SqlCmdChecked (@($script:ConnectionArguments) + @(
            "-Q", "ALTER DATABASE [$BaseDestino] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$BaseDestino];"
        ))
        $destinationExists = "0"
    }

    if ($destinationExists -eq "0") {
        $sourceCollation = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT collation_name FROM sys.databases WHERE name = N'$escapedNumerica';"
        $createDatabaseSql = "CREATE DATABASE [$BaseDestino]"
        if (-not [string]::IsNullOrWhiteSpace($sourceCollation) -and
            $sourceCollation -ne "NULL" -and
            $sourceCollation -match '^[A-Za-z0-9_]+$') {
            $createDatabaseSql += " COLLATE $sourceCollation"
        }
        else {
            Write-Host "Numerica no informo una intercalacion reutilizable; se usara la predeterminada de MSI."
        }
        $createDatabaseSql += ";"
        Invoke-SqlCmdChecked (@($script:ConnectionArguments) + @("-Q", $createDatabaseSql))
    }

    $destinationTables = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT COUNT(*) FROM [$BaseDestino].sys.tables;"
    $hasVeterinaryUsers = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID(N'[$BaseDestino].dbo.vt_usuarios', N'U') IS NULL THEN 0 ELSE 1 END;"

    if ($hasVeterinaryUsers -eq "0") {
        if ([int]$destinationTables -gt 0) {
            throw "La base $BaseDestino contiene $destinationTables tablas, pero no dbo.vt_usuarios. Por seguridad no se mezclaran datos sobre una base parcialmente cargada."
        }

        Write-Host "Adaptando scriptveterinaria.sql a $BaseDestino. Se requiere espacio temporal aproximado de 1.2 GB..."
        $adaptedVeterinaryScript = New-AdaptedDatabaseBodyScript -SourcePath $ScriptVeterinaria -OriginalDatabase "db_a8a74a_hosvet" -TargetDatabase $BaseDestino

        try {
            Write-Host "3/4 Cargando las 122 tablas y los datos veterinarios..." -ForegroundColor Cyan
            Invoke-SqlCmdChecked (@($script:ConnectionArguments) + @("-d", $BaseDestino, "-i", $adaptedVeterinaryScript))
        }
        finally {
            if (Test-Path -LiteralPath $adaptedVeterinaryScript) {
                Remove-Item -LiteralPath $adaptedVeterinaryScript -Force
            }
        }
    }
    else {
        $veterinaryUserCount = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT COUNT(*) FROM [$BaseDestino].dbo.vt_usuarios;"
        $veterinaryTableCount = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT COUNT(*) FROM [$BaseDestino].sys.tables WHERE name LIKE 'vt[_]%';"
        if ([int]$veterinaryUserCount -eq 0 -or [int]$veterinaryTableCount -lt 100) {
            throw "La base $BaseDestino esta incompleta: $veterinaryTableCount tablas vt_ y $veterinaryUserCount usuarios. Vuelva a ejecutar agregando -RecrearDestino."
        }
        Write-Host "$BaseDestino contiene $veterinaryTableCount tablas veterinarias y $veterinaryUserCount usuarios; se conserva la carga existente."
    }

    $validatedVeterinaryUsers = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT COUNT(*) FROM [$BaseDestino].dbo.vt_usuarios;"
    $validatedVeterinaryClients = Invoke-ScalarQuery "SET NOCOUNT ON; SELECT COUNT(*) FROM [$BaseDestino].dbo.vt_clientes;"
    if ([int]$validatedVeterinaryUsers -eq 0 -or [int]$validatedVeterinaryClients -eq 0) {
        throw "La carga veterinaria no supero la validacion: $validatedVeterinaryUsers usuarios y $validatedVeterinaryClients clientes. No se ejecutara la integracion."
    }
    Write-Host "Validacion veterinaria correcta: $validatedVeterinaryUsers usuarios y $validatedVeterinaryClients clientes."

    Write-Host "4/4 Incorporando estructuras de facturacion y EContax, sin copiar emisores ni usuarios de Numerica..." -ForegroundColor Cyan
    $integrationValues = [ordered]@{
        BaseDestino = $BaseDestino
        BaseNumerica = $BaseNumerica
        IdEmpresaNumerica = $IdEmpresaNumerica
        IdSucursalNumerica = $IdSucursalNumerica
        CodigoEmisorNumerica = $CodigoEmisorNumerica
        UserNumeroAdministradorVet = $UserNumeroAdministradorVet
        IdPerfilEContaxAdministrador = $IdPerfilEContaxAdministrador
    }
    $preparedIntegrationScript = Join-Path ([IO.Path]::GetTempPath()) "integracion-$BaseDestino-$([guid]::NewGuid().ToString('N')).sql"
    $integrationContent = [IO.File]::ReadAllText($integrationScript)
    foreach ($entry in $integrationValues.GetEnumerator()) {
        $pattern = "(?m)^:setvar\s+$([regex]::Escape($entry.Key))\s+.*$"
        $replacement = ":setvar $($entry.Key) `"$($entry.Value)`""
        $integrationContent = [regex]::Replace($integrationContent, $pattern, $replacement)
    }
    [IO.File]::WriteAllText($preparedIntegrationScript, $integrationContent, [Text.UTF8Encoding]::new($true))
    try {
        Invoke-SqlCmdChecked (@($script:ConnectionArguments) + @("-i", $preparedIntegrationScript))
    }
    finally {
        if (Test-Path -LiteralPath $preparedIntegrationScript) {
            Remove-Item -LiteralPath $preparedIntegrationScript -Force
        }
    }

    Write-Host "Base $BaseDestino construida e integrada correctamente." -ForegroundColor Green
    Write-Host "Configure VeterinariaConnection para apuntar a $BaseDestino antes de iniciar la aplicacion."
}
finally {
    $plainPassword = $null
}
