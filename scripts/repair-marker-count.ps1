[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CharacterKey,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 20)]
    [int]$Count
)

$ErrorActionPreference = 'Stop'
$directory = Join-Path $env:LOCALAPPDATA 'PWQuestOverlay'
$statePath = Join-Path $directory 'state.json'
if (-not (Test-Path -LiteralPath $statePath)) { throw "Estado não encontrado: $statePath" }

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$property = $state.Characters.PSObject.Properties[$CharacterKey]
if ($null -eq $property) { throw "Personagem não encontrado: $CharacterKey" }

$backupPath = $statePath + '.before-marker-repair.bak'
Copy-Item -LiteralPath $statePath -Destination $backupPath -Force
foreach ($item in $state.Characters.PSObject.Properties) {
    $existingCount = $item.Value.OverlayMarkerCount
    if ($item.Value.HasOverlayMarker -and ($null -eq $existingCount -or [int]$existingCount -le 0)) {
        $item.Value | Add-Member -NotePropertyName OverlayMarkerCount -NotePropertyValue 1 -Force
    }
}
$progress = $property.Value
$progress | Add-Member -NotePropertyName OverlayMarkerCount -NotePropertyValue $Count -Force
$progress.HasOverlayMarker = $true
if ($null -ne $state.Marker.MarkerList -and [int]$state.Marker.MarkerList.X -eq 230 -and [int]$state.Marker.MarkerList.Y -eq 200) {
    $state.Marker.MarkerList.Y = 70
}
$state.SchemaVersion = [Math]::Max(6, [int]$state.SchemaVersion)
$json = $state | ConvertTo-Json -Depth 20 -Compress
[System.IO.File]::WriteAllText($statePath, $json, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "Contagem preparada para ${CharacterKey}: $Count. Backup: $backupPath"
