[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$GameDirectory = 'C:\The Classic Games\The Classic PW 1.4.4',
    [string]$TaskName = 'PW Borderless Monitor'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceExecutable = Join-Path $projectRoot 'dist\PW Borderless.exe'
if (-not (Test-Path -LiteralPath $sourceExecutable)) {
    throw 'Compile o projeto antes de instalar: scripts\build.ps1'
}
if (-not (Test-Path -LiteralPath $GameDirectory -PathType Container)) {
    throw "Diretório do jogo não encontrado: $GameDirectory"
}

$destinationExecutable = Join-Path $GameDirectory 'PW Borderless.exe'
$currentUser = "$env:USERDOMAIN\$env:USERNAME"
if ($PSCmdlet.ShouldProcess($destinationExecutable, 'Copiar monitor e registrar tarefa de inicialização')) {
    Copy-Item -LiteralPath $sourceExecutable -Destination $destinationExecutable -Force
    $action = New-ScheduledTaskAction -Execute $destinationExecutable -WorkingDirectory $GameDirectory
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
    $principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -ExecutionTimeLimit ([TimeSpan]::Zero)
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger `
        -Principal $principal -Settings $settings -Force | Out-Null
    Write-Host "Instalado em: $destinationExecutable"
    Write-Host "Tarefa registrada: $TaskName"
}
