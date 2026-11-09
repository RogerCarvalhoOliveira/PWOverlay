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
$version = '0.7.6'

$compilerCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw 'Compilador csc.exe do .NET Framework 4.x não encontrado.'
}

New-Item -ItemType Directory -Path $distributionDirectory -Force | Out-Null
$overlayExecutable = Join-Path $distributionDirectory 'PW Quest Overlay.exe'
$borderlessExecutable = Join-Path $distributionDirectory 'PW Borderless.exe'
$launcherExecutable = Join-Path $distributionDirectory 'PW Overlay Central.exe'
$memoryProbeExecutable = Join-Path $distributionDirectory 'PW Memory Probe.exe'
$routeDestination = Join-Path $distributionDirectory 'quest_routes.json'
$memoryOffsetsDestination = Join-Path $distributionDirectory 'memory_offsets.json'
$taskCatalogDestination = Join-Path $distributionDirectory 'task_catalog_144.tsv'
$workbookDestination = Join-Path $distributionDirectory 'rota_leveling_pw_144.xlsx'
$readmeDestination = Join-Path $distributionDirectory 'LEIA-ME.txt'
$installationGuideDestination = Join-Path $distributionDirectory 'INSTALACAO.txt'
$ocrDestination = Join-Path $distributionDirectory 'ocr'

$overlaySources = @(
    (Join-Path $sourceDirectory 'Program.cs'),
    (Join-Path $sourceDirectory 'Models.cs'),
    (Join-Path $sourceDirectory 'NativeMethods.cs'),
    (Join-Path $sourceDirectory 'OverlayForm.cs'),
    (Join-Path $sourceDirectory 'MarkerCalibrationForm.cs'),
    (Join-Path $sourceDirectory 'RouteEditorForm.cs'),
    (Join-Path $sourceDirectory 'QuestOcrService.cs'),
    (Join-Path $sourceDirectory 'OcrRegionSelectionForm.cs'),
    (Join-Path $sourceDirectory 'OcrResultForm.cs'),
    (Join-Path $sourceDirectory 'ProcessMemoryReader.cs'),
    (Join-Path $sourceDirectory 'MemoryLayout.cs'),
    (Join-Path $sourceDirectory 'TaskCatalog.cs'),
    (Join-Path $sourceDirectory 'RttiResolver.cs'),
    (Join-Path $sourceDirectory 'QuestMemoryReader.cs'),
    (Join-Path $sourceDirectory 'QuestStateTracker.cs'),
    (Join-Path $sourceDirectory 'QuestMemoryMonitor.cs')
)

& $compiler /nologo /target:winexe /optimize+ /platform:x86 `
    /out:$overlayExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    /reference:Microsoft.CSharp.dll `
    $overlaySources
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Quest Overlay.' }

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /out:$borderlessExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    (Join-Path $sourceDirectory 'PWBorderlessMonitor.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Borderless Monitor.' }

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /out:$launcherExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    (Join-Path $sourceDirectory 'PWLauncher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Overlay Central.' }

& $compiler /nologo /target:exe /optimize+ /platform:x86 `
    /out:$memoryProbeExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Web.Extensions.dll `
    (Join-Path $sourceDirectory 'ProcessMemoryReader.cs') `
    (Join-Path $sourceDirectory 'MemoryLayout.cs') `
    (Join-Path $sourceDirectory 'TaskCatalog.cs') `
    (Join-Path $sourceDirectory 'RttiResolver.cs') `
    (Join-Path $sourceDirectory 'QuestMemoryReader.cs') `
    (Join-Path $sourceDirectory 'QuestStateTracker.cs') `
    (Join-Path $sourceDirectory 'PWMemoryProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o diagnóstico de memória.' }

Copy-Item -LiteralPath (Join-Path $dataDirectory 'quest_routes.json') -Destination $routeDestination -Force
Copy-Item -LiteralPath (Join-Path $dataDirectory 'memory_offsets.json') -Destination $memoryOffsetsDestination -Force
Copy-Item -LiteralPath (Join-Path $dataDirectory 'task_catalog_144.tsv') -Destination $taskCatalogDestination -Force
Copy-Item -LiteralPath (Join-Path $dataDirectory 'rota_leveling_pw_144.xlsx') -Destination $workbookDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\MANUAL-USUARIO.md') -Destination $readmeDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\INSTALACAO.md') -Destination $installationGuideDestination -Force

function Copy-OcrRuntime {
    $tesseractCandidates = @(
        (Join-Path $env:ProgramFiles 'Tesseract-OCR'),
        (Join-Path ${env:ProgramFiles(x86)} 'Tesseract-OCR')
    )
    $tesseractSource = $tesseractCandidates |
        Where-Object { $_ -and (Test-Path -LiteralPath (Join-Path $_ 'tesseract.exe')) } |
        Select-Object -First 1
    if (-not $tesseractSource) {
        throw 'Tesseract OCR não encontrado. Instale a versão 5.x com o idioma por antes de empacotar.'
    }

    if (Test-Path -LiteralPath $ocrDestination) {
        Remove-Item -LiteralPath $ocrDestination -Recurse -Force
    }
    New-Item -ItemType Directory -Path (Join-Path $ocrDestination 'tessdata') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $ocrDestination 'doc') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $tesseractSource 'tesseract.exe') -Destination $ocrDestination
    Get-ChildItem -LiteralPath $tesseractSource -File -Filter '*.dll' |
        Copy-Item -Destination $ocrDestination
    Copy-Item -LiteralPath (Join-Path $tesseractSource 'tessdata\por.traineddata') `
        -Destination (Join-Path $ocrDestination 'tessdata')
    foreach ($notice in @('AUTHORS', 'LICENSE', 'README.md')) {
        $noticePath = Join-Path $tesseractSource ('doc\' + $notice)
        if (Test-Path -LiteralPath $noticePath) {
            Copy-Item -LiteralPath $noticePath -Destination (Join-Path $ocrDestination 'doc')
        }
    }
}

Write-Host "Compilação concluída em: $distributionDirectory"
Get-ChildItem -LiteralPath $distributionDirectory -File |
    Where-Object { $_.Extension -notin @('.zip') -and $_.Name -notlike '*-Setup-*.exe' } |
    Select-Object Name, Length

if ($Package) {
    Copy-OcrRuntime
    $packagePath = Join-Path $distributionDirectory "PWQuestOverlay-Portatil-v$version.zip"
    if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
    Compress-Archive -LiteralPath @(
        $overlayExecutable,
        $borderlessExecutable,
        $launcherExecutable,
        $memoryProbeExecutable,
        $routeDestination,
        $memoryOffsetsDestination,
        $taskCatalogDestination,
        $workbookDestination,
        $readmeDestination,
        $installationGuideDestination,
        $ocrDestination
    ) -DestinationPath $packagePath -CompressionLevel Optimal
    Write-Host "Pacote criado: $packagePath"
}

if ($Installer) {
    if (-not (Test-Path -LiteralPath (Join-Path $ocrDestination 'tesseract.exe'))) {
        Copy-OcrRuntime
    }
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
