[CmdletBinding()]
param(
    [string]$Profile = 'the_classic_144'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$probe = Join-Path $projectRoot 'dist\PW Memory Probe.exe'
$layout = Join-Path $projectRoot 'dist\memory_offsets.json'

if (-not (Test-Path -LiteralPath $probe)) {
    & (Join-Path $PSScriptRoot 'build.ps1')
}

& $probe --layout $layout --profile $Profile
exit $LASTEXITCODE
