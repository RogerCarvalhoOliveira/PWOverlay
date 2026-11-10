[CmdletBinding()]
param(
    [switch]$Package,
    [switch]$Installer
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceDirectory = Join-Path $projectRoot 'src'
$dataDirectory = Join-Path $projectRoot 'data'
$distributionDirectory = Join-Path $projectRoot 'dist'
$version = [IO.File]::ReadAllText((Join-Path $projectRoot 'VERSION')).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION deve conter X.Y.Z.' }
$iconPath = Join-Path $projectRoot 'assets\branding\PWOverlay.ico'
if (-not (Test-Path -LiteralPath $iconPath)) { throw 'Ícone versionado não encontrado.' }

$compilerCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw 'Compilador csc.exe do .NET Framework 4.x não encontrado.'
}

New-Item -ItemType Directory -Path $distributionDirectory -Force | Out-Null
$generatedDirectory = Join-Path $distributionDirectory 'generated'
New-Item -ItemType Directory -Path $generatedDirectory -Force | Out-Null
$assemblyInfo = Join-Path $generatedDirectory 'AssemblyInfo.cs'
@"
using System.Reflection;
[assembly: AssemblyTitle("PW Quest Overlay")]
[assembly: AssemblyProduct("PW Quest Overlay")]
[assembly: AssemblyDescription("Guia de missões do Perfect World em multiclient")]
[assembly: AssemblyVersion("$version.0")]
[assembly: AssemblyFileVersion("$version.0")]
[assembly: AssemblyInformationalVersion("$version")]
"@ | Set-Content -LiteralPath $assemblyInfo -Encoding UTF8
$brandingSource = Join-Path $sourceDirectory 'AppBranding.cs'
$shutdownSource = Join-Path $sourceDirectory 'AppShutdown.cs'
$maintenanceSource = Join-Path $sourceDirectory 'ToolMaintenance.cs'
$overlayExecutable = Join-Path $distributionDirectory 'PW Quest Overlay.exe'
$borderlessExecutable = Join-Path $distributionDirectory 'PW Borderless.exe'
$launcherExecutable = Join-Path $distributionDirectory 'PW Overlay Central.exe'
$memoryProbeExecutable = Join-Path $distributionDirectory 'PW Memory Probe.exe'
$maintenanceExecutable = Join-Path $distributionDirectory 'PW Maintenance.exe'
$routeDestination = Join-Path $distributionDirectory 'quest_routes.json'
$memoryOffsetsDestination = Join-Path $distributionDirectory 'memory_offsets.json'
$taskCatalogDestination = Join-Path $distributionDirectory 'task_catalog_144.tsv'
$workbookDestination = Join-Path $distributionDirectory 'rota_leveling_pw_144.xlsx'
$readmeDestination = Join-Path $distributionDirectory 'LEIA-ME.txt'
$installationGuideDestination = Join-Path $distributionDirectory 'INSTALACAO.txt'
$classGuideDestination = Join-Path $distributionDirectory 'GUIAS-DEZ-CLASSES.txt'
$endgameGuideDestination = Join-Path $distributionDirectory 'GUIA-61-105.txt'
$standaloneGuideDestination = Join-Path $distributionDirectory 'SEM-PW-HELPER.txt'

$overlaySources = @(
    (Join-Path $sourceDirectory 'Program.cs'),
    (Join-Path $sourceDirectory 'Models.cs'),
    (Join-Path $sourceDirectory 'ClientIdentity.cs'),
    (Join-Path $sourceDirectory 'ClientProfilesForm.cs'),
    (Join-Path $sourceDirectory 'NativeMethods.cs'),
    (Join-Path $sourceDirectory 'OverlayForm.cs'),
    (Join-Path $sourceDirectory 'CultivationChoiceForm.cs'),
    (Join-Path $sourceDirectory 'MarkerCalibrationForm.cs'),
    (Join-Path $sourceDirectory 'RouteEditorForm.cs'),
    (Join-Path $sourceDirectory 'ProcessMemoryReader.cs'),
    (Join-Path $sourceDirectory 'PlayerIdentityMemory.cs'),
    (Join-Path $sourceDirectory 'MemoryLayout.cs'),
    (Join-Path $sourceDirectory 'TaskCatalog.cs'),
    (Join-Path $sourceDirectory 'RttiResolver.cs'),
    (Join-Path $sourceDirectory 'QuestMemoryReader.cs'),
    (Join-Path $sourceDirectory 'QuestStateTracker.cs'),
    (Join-Path $sourceDirectory 'QuestMemoryMonitor.cs')
)

& $compiler /nologo /target:winexe /optimize+ /platform:x86 `
    /win32icon:$iconPath "/resource:$iconPath,PWOverlay.App.ico" `
    /out:$overlayExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    /reference:Microsoft.CSharp.dll `
    $overlaySources $assemblyInfo $brandingSource $shutdownSource
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Quest Overlay.' }

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /win32icon:$iconPath `
    /out:$borderlessExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    (Join-Path $sourceDirectory 'PWBorderlessMonitor.cs') $assemblyInfo $shutdownSource
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Borderless Monitor.' }

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /win32icon:$iconPath "/resource:$iconPath,PWOverlay.App.ico" `
    /out:$launcherExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    (Join-Path $sourceDirectory 'PWLauncher.cs') $assemblyInfo $brandingSource $shutdownSource $maintenanceSource
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Overlay Central.' }

& $compiler /nologo /target:exe /optimize+ /platform:x86 `
    /win32icon:$iconPath `
    /out:$memoryProbeExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Web.Extensions.dll `
    (Join-Path $sourceDirectory 'ProcessMemoryReader.cs') `
    (Join-Path $sourceDirectory 'PlayerIdentityMemory.cs') `
    (Join-Path $sourceDirectory 'MemoryLayout.cs') `
    (Join-Path $sourceDirectory 'TaskCatalog.cs') `
    (Join-Path $sourceDirectory 'RttiResolver.cs') `
    (Join-Path $sourceDirectory 'QuestMemoryReader.cs') `
    (Join-Path $sourceDirectory 'QuestStateTracker.cs') `
    (Join-Path $sourceDirectory 'PWMemoryProbe.cs') $assemblyInfo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o diagnóstico de memória.' }

& $compiler /nologo /target:exe /optimize+ /platform:anycpu /win32icon:$iconPath `
    /out:$maintenanceExecutable /reference:System.dll /reference:System.Core.dll `
    (Join-Path $sourceDirectory 'MaintenanceProgram.cs') $shutdownSource $maintenanceSource $assemblyInfo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o auxiliar de manutenção.' }

Copy-Item -LiteralPath (Join-Path $dataDirectory 'quest_routes.json') -Destination $routeDestination -Force
Copy-Item -LiteralPath (Join-Path $dataDirectory 'memory_offsets.json') -Destination $memoryOffsetsDestination -Force
Copy-Item -LiteralPath (Join-Path $dataDirectory 'task_catalog_144.tsv') -Destination $taskCatalogDestination -Force
Copy-Item -LiteralPath (Join-Path $dataDirectory 'rota_leveling_pw_144.xlsx') -Destination $workbookDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\MANUAL-USUARIO.md') -Destination $readmeDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\INSTALACAO.md') -Destination $installationGuideDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\GUIAS-DEZ-CLASSES.md') -Destination $classGuideDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\GUIA-61-105.md') -Destination $endgameGuideDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\SEM-PW-HELPER.md') -Destination $standaloneGuideDestination -Force

Write-Host "Compilação concluída em: $distributionDirectory"
Get-ChildItem -LiteralPath $distributionDirectory -File |
    Where-Object { $_.Extension -notin @('.zip') -and $_.Name -notlike '*-Setup-*.exe' } |
    Select-Object Name, Length

if ($Package) {
    $packagePath = Join-Path $distributionDirectory "PWQuestOverlay-Portatil-v$version.zip"
    if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
    Compress-Archive -LiteralPath @(
        $overlayExecutable,
        $borderlessExecutable,
        $launcherExecutable,
        $memoryProbeExecutable,
        $maintenanceExecutable,
        $routeDestination,
        $memoryOffsetsDestination,
        $taskCatalogDestination,
        $workbookDestination,
        $readmeDestination,
        $installationGuideDestination,
        $classGuideDestination,
        $endgameGuideDestination,
        $standaloneGuideDestination
    ) -DestinationPath $packagePath -CompressionLevel Optimal
    Write-Host "Pacote criado: $packagePath"
}

if ($Installer) {
    $compilerCandidates = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe',
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    $innoCompiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $innoCompiler) {
        throw 'Inno Setup 6 não encontrado. Instale com: winget install --id JRSoftware.InnoSetup --exact'
    }
    & $innoCompiler "/DMyAppVersion=$version" (Join-Path $projectRoot 'installer\PWQuestOverlay.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar o instalador.' }
    Write-Host "Instalador criado: $(Join-Path $distributionDirectory "PWQuestOverlay-Setup-v$version.exe")"
}

if ($Package -or $Installer) {
    $releaseFiles = @()
    if ($Package) { $releaseFiles += Join-Path $distributionDirectory "PWQuestOverlay-Portatil-v$version.zip" }
    if ($Installer) { $releaseFiles += Join-Path $distributionDirectory "PWQuestOverlay-Setup-v$version.exe" }
    $hashes = $releaseFiles | ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
        '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), (Split-Path -Leaf $_)
    }
    $hashes | Set-Content -LiteralPath (Join-Path $distributionDirectory "SHA256SUMS-v$version.txt") -Encoding ASCII
}
