[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$statePath = Join-Path $env:LOCALAPPDATA 'PWQuestOverlay\state.json'
if (-not (Test-Path -LiteralPath $statePath)) {
    Write-Host "Estado ainda não existe: $statePath"
    exit 0
}

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
Write-Host "Arquivo: $statePath"
Write-Host "Bloqueado: $($state.PositionLocked) | Compacto: $($state.CompactMode) | Oculto: $($state.UserHidden) | Opacidade: $($state.Opacity)"
Write-Host ''
Write-Host 'Associações manuais:'
if ($state.ClassAssociations) {
    $state.ClassAssociations.PSObject.Properties |
        Select-Object @{Name='Título';Expression={$_.Name}}, @{Name='Classe';Expression={$_.Value}} |
        Format-Table -AutoSize
} else {
    Write-Host '  nenhuma'
}

Write-Host 'Perfis:'
$state.Characters.PSObject.Properties | ForEach-Object {
    $profile = $_.Value
    [pscustomobject]@{
        Chave = $_.Name
        Personagem = $profile.CharacterName
        Classe = $profile.ClassCode
        Manual = $profile.ManualClass
        Marcadas = @($profile.MissionStates.PSObject.Properties).Count
        MarcadoresOverlay = $profile.OverlayMarkerCount
    }
} | Format-Table -AutoSize
