[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$routePath = Join-Path $projectRoot 'data\quest_routes.json'
$payload = Get-Content -LiteralPath $routePath -Raw | ConvertFrom-Json
$missions = @($payload.missions)
$errors = New-Object System.Collections.Generic.List[string]
$requiredFields = @('id', 'routeKey', 'order', 'classScope', 'minLevel', 'name', 'npc', 'location')

foreach ($mission in $missions) {
    foreach ($field in $requiredFields) {
        $value = $mission.$field
        if ($null -eq $value -or ($value -is [string] -and [string]::IsNullOrWhiteSpace($value))) {
            $errors.Add("Campo obrigatório ausente: $field em $($mission.id)")
        }
    }
    $steps = @($mission.steps)
    if ($steps.Count -eq 0) {
        $errors.Add("Missão sem etapas: $($mission.id)")
        continue
    }
    foreach ($step in $steps) {
        if ([string]::IsNullOrWhiteSpace($step.id) -or [string]::IsNullOrWhiteSpace($step.action)) {
            $errors.Add("Etapa sem id/action em $($mission.id)")
        }
        $hasX = $null -ne $step.markerX
        $hasY = $null -ne $step.markerY
        if ($hasX -ne $hasY) {
            $errors.Add("Par de coordenadas incompleto na etapa $($step.id) de $($mission.id)")
        }
    }
    foreach ($duplicateStep in ($steps | Group-Object id | Where-Object Count -gt 1)) {
        $errors.Add("ID de etapa duplicado em $($mission.id): $($duplicateStep.Name)")
    }
}

$duplicateIds = $missions | Group-Object id | Where-Object Count -gt 1
foreach ($duplicate in $duplicateIds) {
    $errors.Add("ID duplicado: $($duplicate.Name) ($($duplicate.Count)x)")
}

$validClasses = @('WR', 'WB/WF', 'WF', 'EA/EP', 'MC')
foreach ($mission in $missions | Where-Object { $_.classScope -notin $validClasses }) {
    $errors.Add("classScope inválido: $($mission.classScope) em $($mission.id)")
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    throw "Rotas inválidas: $($errors.Count) erro(s)."
}

$counts = [ordered]@{
    WR = @($missions | Where-Object routeKey -eq 'WR').Count
    WB = @($missions | Where-Object { $_.routeKey -eq 'WBWF' -and $_.classScope -eq 'WB/WF' }).Count
    WF = @($missions | Where-Object { $_.routeKey -eq 'WBWF' }).Count
    EA = @($missions | Where-Object routeKey -eq 'EAEP').Count
    EP = @($missions | Where-Object routeKey -eq 'EAEP').Count
    MC = @($missions | Where-Object routeKey -eq 'MC').Count
}

Write-Host "Rotas válidas. Missões únicas: $($missions.Count)"
Write-Host "Etapas: $(@($missions | ForEach-Object { @($_.steps) }).Count)"
$counts.GetEnumerator() | Format-Table Name, Value -AutoSize
