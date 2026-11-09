[CmdletBinding()]
param(
    [string]$Profile = 'the_classic_144',
    [switch]$Watch,
    [ValidateRange(100, 10000)]
    [int]$Interval = 500
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$probe = Join-Path $projectRoot 'dist\PW Memory Probe.exe'
$layout = Join-Path $projectRoot 'dist\memory_offsets.json'
$catalog = Join-Path $projectRoot 'dist\task_catalog_144.tsv'

if (-not (Test-Path -LiteralPath $probe)) {
    & (Join-Path $PSScriptRoot 'build.ps1')
}

$arguments = @('--layout', $layout, '--catalog', $catalog, '--profile', $Profile)
if ($Watch) { $arguments += @('--watch', '--interval', $Interval) }
& $probe $arguments
exit $LASTEXITCODE
