[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compilerCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Compilador csc.exe do .NET Framework 4.x não encontrado.' }

& (Join-Path $PSScriptRoot 'validate-data.ps1')

$testDirectory = Join-Path $projectRoot 'dist\tests'
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$testExecutable = Join-Path $testDirectory 'SmokeTests.exe'
& $compiler /nologo /target:exe /optimize+ /out:$testExecutable `
    /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll `
    (Join-Path $projectRoot 'src\Models.cs') `
    (Join-Path $projectRoot 'tests\SmokeTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os smoke tests.' }

& $testExecutable (Join-Path $projectRoot 'data\quest_routes.json')
if ($LASTEXITCODE -ne 0) { throw 'Smoke tests falharam.' }
