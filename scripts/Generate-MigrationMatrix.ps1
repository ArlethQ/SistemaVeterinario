param(
    [string]$LegacyRoot = 'C:\Users\numer_w2oslz\Downloads\HosvetKardex\HosvetKardex',
    [string]$BlazorRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\matriz-migracion-webforms-blazor.md')
)

$ErrorActionPreference = 'Stop'

function Get-Matches([string]$Text, [string]$Pattern, [int]$Limit = 30) {
    if ([string]::IsNullOrWhiteSpace($Text)) { return @() }
    return [regex]::Matches($Text, $Pattern, 'IgnoreCase') |
        ForEach-Object { $_.Groups[1].Value.Trim() } |
        Where-Object { $_ } | Sort-Object -Unique | Select-Object -First $Limit
}

function Join-Values($Values, [string]$Empty = 'No detectado') {
    $items = @($Values | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($items.Count -eq 0) { return $Empty }
    return ($items -join ', ') -replace '\|', '\|'
}

function Get-Module([string]$Name) {
    $n = $Name.ToLowerInvariant()
    if ($n -match 'hosp_|hospital|monitoreo') { return 'Hospitalización' }
    if ($n -match 'quir|anest|operatorio|recuperacion|protocolo') { return 'Quirófano y anestesia' }
    if ($n -match 'enfermer|kardex|signos') { return 'Kardex y enfermería' }
    if ($n -match 'lab|rx|imagen') { return 'Laboratorio e imagenología' }
    if ($n -match 'cita|calendar|horario') { return 'Agenda y horarios' }
    if ($n -match 'orden|despacho|principal|default') { return 'Órdenes y recepción' }
    if ($n -match 'his|ficha|evol|nota|anteced|vacuna|receta') { return 'Historia clínica' }
    if ($n -match 'proforma|factura') { return 'Proformas y facturación' }
    if ($n -match 'usuario|permiso|localidad|especialidad|catalog|vitacora') { return 'Administración' }
    if ($n -match 'personal|cliente|paciente|especie|raza|genero|clase|categoria|obtension|crecimiento') { return 'Clientes y pacientes' }
    if ($n -match 'recordatorio|alerta|fallecido|i_stat|controlingreso') { return 'Seguimiento clínico' }
    return 'Operaciones clínicas'
}

$explicit = @{
    'vt_personales'='ClientesPacientes'; 'vt_tipo_cliente'='ClientesPacientes'; 'vt_tipo_paciente'='ClientesPacientes'
    'vt_especies'='CatalogosClinicos'; 'vt_razas'='CatalogosClinicos'; 'vt_genero'='CatalogosClinicos'; 'vt_clase'='CatalogosClinicos'; 'vt_categoria_ingreso'='CatalogosClinicos'; 'vt_obtension'='CatalogosClinicos'; 'curvaCrecimiento'='HistorialPaciente'
    'vt_administracionCitasnew'='AgendaCitas'; 'vt_vercitas'='AgendaCitas'; 'vt_Calendarizacion'='ProgramacionClinica'; 'vt_calendarizarhorariosmedicos'='ProgramacionClinica'
    'vt_OrdenesMenu'='RecepcionOrdenes'; 'vt_OrdenesDespacho'='RecepcionOrdenes'; 'vt_verOrdenes'='RecepcionOrdenes'; 'vt_principal'='Home'; 'Default'='Home'
    'vt_EnfermeriaKardex'='KardexEnfermeria'; 'vt_EnfermeriaSignos'='EnfermeriaSignos'; 'vt_preenfermeria'='BandejaOrdenes'; 'vt_quirenfermeria'='KardexEnfermeria'
    'Hosp_FichaHospitalizacion'='FichaHospitalizacion'; 'Hosp_Monitoreo'='MonitoreoHospitalario'; 'Hosp_PreFicha'='FichaHospitalizacion'; 'Hosp_PreMonitoreo'='MonitoreoHospitalario'; 'Hosp_PreProformas'='ProformasFacturacion'
    'vt_despacholab'='OrdenesLaboratorio'; 'vt_despachorx'='OrdenesImagenologia'; 'vt_labmedicospre'='BandejaResultadosDiagnosticos'
    'vt_QuirofanoPrincipal'='ProgramacionClinica'; 'vt_QuirofanoIngreso'='OperacionesClinicas'; 'vt_prequirurgico'='PreAnestesia'; 'vt_PreAnestesia'='PreAnestesia'; 'vt_hojaanestesia'='HojaAnestesia'; 'vt_protocolo'='ProtocoloQuirurgico'; 'vt_postquirurgico'='SalaRecuperacion'; 'vt_RecuperacionPrincipal'='SalaRecuperacion'; 'vt_Recuperacion'='SalaRecuperacion'; 'vt_Plan'='SalaRecuperacion'; 'vt_parteoperatorio'='ProtocoloQuirurgico'; 'vt_QuirProformas'='ProformasFacturacion'
    'vt_UniProformas'='ProformasFacturacion'; 'vt_usuarios'='UsuariosAdministracion'; 'vt_permisos'='RolesPermisos'; 'vt_localidades'='LocalidadesAdministracion'; 'vt_especialidades'='CatalogosOperativos'; 'vt_especialidades_localidades'='CatalogosOperativos'; 'vt_vitacora'='BitacoraAuditoria'
    'vt_baseRecordatorios'='SeguimientoClinico'; 'vt_listaalertas'='SeguimientoClinico'; 'vt_fallecidos'='SeguimientoClinico'; 'vt_i_stat'='OperacionesClinicas'; 'vt_ControlIngreso'='OperacionesClinicas'; 'vt_RegistroIngreso'='OperacionesClinicas'
}

$clinical = @('vt_hisDatMedicos','vt_hisdatmedicospre','vt_hisInfoMed','vt_hisInfoMedPre','vt_notas','vt_evoluciones','vt_antecedentes','vt_vacunas','vt_vacunasfinal','vt_imprimirfichas','vt_historialPaciente','vt_fichasanteriores')
foreach ($key in $clinical) { $explicit[$key] = if ($key -match 'evol') { 'EvolucionMedica' } elseif ($key -match 'historial|fichas') { 'HistorialPaciente' } else { 'FichaMedica' } }

$razorFiles = Get-ChildItem (Join-Path $BlazorRoot 'Components\Pages') -Filter '*.razor'
$serviceFiles = Get-ChildItem (Join-Path $BlazorRoot 'Services') -Filter '*.cs'
$blazorText = @{}
foreach ($file in @($razorFiles) + @($serviceFiles)) { $blazorText[$file.FullName] = Get-Content $file.FullName -Raw }

$pages = Get-ChildItem $LegacyRoot -Recurse -Filter '*.aspx' | Sort-Object FullName
$rows = [System.Collections.Generic.List[string]]::new()
$details = [System.Collections.Generic.List[string]]::new()

foreach ($page in $pages) {
    $relative = $page.FullName.Substring($LegacyRoot.Length).TrimStart('\')
    $base = [IO.Path]::GetFileNameWithoutExtension($page.Name)
    $markup = Get-Content $page.FullName -Raw
    $codeFile = @("$($page.FullName).vb", "$($page.FullName).cs") | Where-Object { Test-Path $_ } | Select-Object -First 1
    $code = if ($codeFile) { Get-Content $codeFile -Raw } else { '' }
    $all = "$markup`n$code"

    $fields = Get-Matches $markup '(?:ID|id)\s*=\s*["'']([^"'']+)["'']' 25
    $events = @(Get-Matches $markup '(?:OnClick|OnCommand|OnSelectedIndexChanged|OnRowCommand|OnTextChanged)\s*=\s*["'']([^"'']+)["'']' 20) +
        @(Get-Matches $code '(?:Sub|Function)\s+([A-Za-z0-9_]+)\s*\(' 30) | Where-Object { $_ -match 'Click|Load|Guardar|Actualizar|Eliminar|Buscar|Imprimir|Enviar|Selected|Command' }
    $sql = Get-Matches $all '\b((?:vt|qvt|Hosp)_[A-Za-z0-9_]+)\b' 50
    $files = Get-Matches $all '([A-Za-z0-9_./\\-]+\.(?:pdf|jpg|jpeg|png|xlsx|xls|docx|rpt))' 25
    $reports = Get-Matches $all '([A-Za-z0-9_./\\-]+\.rpt)' 20
    $mail = if ($all -match '(?i)SmtpClient|MailMessage|SendMail|correo|email') { 'Sí; revisar implementación y destinatarios en code-behind' } else { 'No detectado' }

    $candidate = $explicit[$base]
    if (-not $candidate) {
        $normalized = ($base -replace '^(vt_|Hosp_)','' -replace '[^A-Za-z0-9]','').ToLowerInvariant()
        $candidateFile = $razorFiles | Where-Object {
            $r = ($_.BaseName -replace '[^A-Za-z0-9]','').ToLowerInvariant()
            ($normalized.Length -ge 5 -and ($r.Contains($normalized) -or $normalized.Contains($r)))
        } | Select-Object -First 1
        if ($candidateFile) { $candidate = $candidateFile.BaseName }
    }

    $pageEvidence = if ($candidate) { $razorFiles | Where-Object BaseName -eq $candidate | Select-Object -First 1 } else { $null }
    $serviceEvidence = $serviceFiles | Where-Object {
        $text = $blazorText[$_.FullName]
        @($sql | Where-Object { $text -match [regex]::Escape($_) }).Count -gt 0
    }
    $status = if (-not $pageEvidence) { 'faltante' } else { 'parcial' }
    $evidence = if ($pageEvidence) { "Página `Components/Pages/$($pageEvidence.Name)` localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios." } else { 'Sin página Razor equivalente demostrable por nombre o mapeo funcional.' }
    $work = if ($status -eq 'faltante') { 'Definir equivalencia vigente e implementar o justificar obsolescencia.' } else { 'Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.' }
    $serviceNames = @($serviceEvidence.BaseName)

    $candidateDisplay = if ($candidate) { $candidate + '.razor' } else { '—' }
    $rows.Add("| $relative | $(Get-Module $base) | $status | $candidateDisplay | $(Join-Values $serviceNames '—') |")
    $details.Add("### ``$relative``")
    $details.Add('')
    $details.Add("- **Code-behind:** $(if($codeFile){'`' + [IO.Path]::GetFileName($codeFile) + '`'}else{'No localizado'}).")
    $details.Add("- **Propósito funcional:** Módulo $(Get-Module $base); confirmar el propósito exacto durante la validación funcional.")
    $details.Add("- **Campos principales:** $(Join-Values $fields).")
    $details.Add("- **Eventos y operaciones:** $(Join-Values ($events | Sort-Object -Unique)).")
    $details.Add("- **Tablas/vistas/procedimientos:** $(Join-Values $sql).")
    $details.Add("- **Archivos físicos:** $(Join-Values $files).")
    $details.Add("- **Reportes Crystal:** $(Join-Values $reports).")
    $details.Add("- **Correo:** $mail.")
    $details.Add("- **Blazor equivalente:** $(if($candidate){'`Components/Pages/' + $candidate + '.razor`'}else{'No identificado'}).")
    $details.Add("- **Servicio equivalente:** $(Join-Values ($serviceNames | ForEach-Object { '`Services/' + $_ + '.cs`' })).")
    $details.Add("- **Estado:** **$status**.")
    $details.Add("- **Evidencia:** $evidence")
    $details.Add("- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.")
    $details.Add("- **Trabajo requerido:** $work")
    $details.Add('')
}

$header = @(
    '# Matriz de migración WebForms → Blazor', '',
    '> Inventario generado desde código fuente. Estado conservador: ninguna página se marca completa sin prueba funcional contra la misma base de datos.', '',
    "- WebForms inventariadas: **$($pages.Count)**.",
    "- Con code-behind localizado: **$(($pages | Where-Object { (Test-Path "$($_.FullName).vb") -or (Test-Path "$($_.FullName).cs") }).Count)**.",
    "- Crystal Reports encontrados en el legado: **$((Get-ChildItem $LegacyRoot -Recurse -Filter '*.rpt').Count)**.",
    "- Generado: **$(Get-Date -Format 'yyyy-MM-dd HH:mm')**.", '',
    '## Resumen', '',
    '| Página ASPX | Módulo | Estado | Página Blazor candidata | Servicios con objetos SQL coincidentes |',
    '|---|---|---|---|---|'
)

$output = @($header) + @($rows) + @('', '## Evidencia por página', '') + @($details)
$directory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $directory -Force | Out-Null
Set-Content -LiteralPath $OutputPath -Value $output -Encoding utf8
Write-Output "Matriz generada: $OutputPath ($($pages.Count) páginas)"
