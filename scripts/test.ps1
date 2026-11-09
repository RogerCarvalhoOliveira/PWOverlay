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
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll `
    (Join-Path $projectRoot 'src\Models.cs') `
    (Join-Path $projectRoot 'src\ClientIdentity.cs') `
    (Join-Path $projectRoot 'src\MemoryLayout.cs') `
    (Join-Path $projectRoot 'src\TaskCatalog.cs') `
    (Join-Path $projectRoot 'src\QuestMemoryReader.cs') `
    (Join-Path $projectRoot 'src\QuestStateTracker.cs') `
    (Join-Path $projectRoot 'src\QuestMemoryMonitor.cs') `
    (Join-Path $projectRoot 'src\ProcessMemoryReader.cs') `
    (Join-Path $projectRoot 'src\RttiResolver.cs') `
    (Join-Path $projectRoot 'tests\SmokeTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os smoke tests.' }

& $testExecutable `
    (Join-Path $projectRoot 'data\quest_routes.json') `
    (Join-Path $projectRoot 'data\memory_offsets.json') `
    (Join-Path $projectRoot 'data\task_catalog_144.tsv')
if ($LASTEXITCODE -ne 0) { throw 'Smoke tests falharam.' }
