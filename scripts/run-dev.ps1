[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'build.ps1')

Get-Process -Name 'PW Quest Overlay' -ErrorAction SilentlyContinue |
    Stop-Process -Force

$executable = Join-Path $projectRoot 'dist\PW Quest Overlay.exe'
Start-Process -FilePath $executable -WorkingDirectory (Split-Path -Parent $executable)
Write-Host 'PW Quest Overlay iniciado.'
