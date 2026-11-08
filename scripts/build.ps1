[CmdletBinding()]
param(
    [switch]$Package
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceDirectory = Join-Path $projectRoot 'src'
$dataDirectory = Join-Path $projectRoot 'data'
$distributionDirectory = Join-Path $projectRoot 'dist'

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
$routeDestination = Join-Path $distributionDirectory 'quest_routes.json'
$workbookDestination = Join-Path $distributionDirectory 'rota_leveling_pw_144.xlsx'
$readmeDestination = Join-Path $distributionDirectory 'LEIA-ME.txt'

$overlaySources = @(
    (Join-Path $sourceDirectory 'Program.cs'),
    (Join-Path $sourceDirectory 'Models.cs'),
    (Join-Path $sourceDirectory 'NativeMethods.cs'),
    (Join-Path $sourceDirectory 'OverlayForm.cs')
)

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /out:$overlayExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    $overlaySources
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Quest Overlay.' }

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /out:$borderlessExecutable `
    /reference:System.dll `
    /reference:System.Core.dll `
    (Join-Path $sourceDirectory 'PWBorderlessMonitor.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o PW Borderless Monitor.' }

Copy-Item -LiteralPath (Join-Path $dataDirectory 'quest_routes.json') -Destination $routeDestination -Force
Copy-Item -LiteralPath (Join-Path $dataDirectory 'rota_leveling_pw_144.xlsx') -Destination $workbookDestination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\MANUAL-USUARIO.md') -Destination $readmeDestination -Force

Write-Host "Compilação concluída em: $distributionDirectory"
Get-ChildItem -LiteralPath $distributionDirectory -File |
    Where-Object { $_.Name -ne 'PW Quest Overlay Prototipo.zip' } |
    Select-Object Name, Length

if ($Package) {
    $packagePath = Join-Path $distributionDirectory 'PW Quest Overlay Prototipo.zip'
    if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
    Compress-Archive -LiteralPath @(
        $overlayExecutable,
        $borderlessExecutable,
        $routeDestination,
        $workbookDestination,
        $readmeDestination
    ) -DestinationPath $packagePath -CompressionLevel Optimal
    Write-Host "Pacote criado: $packagePath"
}
