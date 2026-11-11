[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$routePath = Join-Path $projectRoot 'data\quest_routes.json'
$payload = Get-Content -LiteralPath $routePath -Raw | ConvertFrom-Json
$missions = @($payload.missions)
$errors = New-Object System.Collections.Generic.List[string]
if ($payload.schemaVersion -ge 9 -and
    (@($payload.xpToNextLevel).Count -ne 104 -or @($payload.xpToNextLevel | Where-Object { $_ -le 0 }).Count -gt 0)) {
    $errors.Add('Curva de XP inválida para os níveis 1 a 105.')
}
$requiredFields = @('id', 'routeKey', 'order', 'classScope', 'minLevel', 'expectedLevel', 'name', 'npc', 'location')

foreach ($mission in $missions) {
    if ($mission.xpOptional -and
        ($mission.priority -notin @('Alta','Opcional') -or $mission.repeatable -or
         -not $mission.historyTaskGroups -or $mission.xp3 -le 0)) {
        $errors.Add("Missão de XP dispensável sem metadados válidos: $($mission.id)")
    }
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
        if ([string]::IsNullOrWhiteSpace($step.id) -or [string]::IsNullOrWhiteSpace($step.title) -or [string]::IsNullOrWhiteSpace($step.action)) {
            $errors.Add("Etapa sem id/title/action em $($mission.id)")
        }
        if ($null -eq $step.routeOrder -or [int]$step.routeOrder -le 0) {
            $errors.Add("Etapa sem ordem global em $($mission.id): $($step.id)")
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

foreach ($routeKey in @('WR','WBWF','EAEP','MC','SKMS','COMMON')) {
    $routeSteps = @($missions | Where-Object routeKey -eq $routeKey | ForEach-Object { $_.steps })
    foreach ($duplicateOrder in ($routeSteps | Group-Object routeOrder | Where-Object Count -gt 1)) {
        $errors.Add("Ordem global duplicada na rota ${routeKey}: $($duplicateOrder.Name)")
    }
}

$duplicateIds = $missions | Group-Object id | Where-Object Count -gt 1
foreach ($duplicate in $duplicateIds) {
    $errors.Add("ID duplicado: $($duplicate.Name) ($($duplicate.Count)x)")
}

$validClasses = @('WR/MG', 'WB/WF', 'WF', 'EA/EP', 'MC/ES', 'SK/MS', 'SK', 'MS', 'ALL')
foreach ($mission in $missions | Where-Object { $_.classScope -notin $validClasses }) {
    $errors.Add("classScope inválido: $($mission.classScope) em $($mission.id)")
}
foreach ($mission in $missions) {
    if ($mission.cultivationPath -and $mission.cultivationPath -notin @('GOD','EVIL')) {
        $errors.Add("Cultivo inválido: $($mission.cultivationPath) em $($mission.id)")
    }
    if ($mission.cultivationChoice -and ($mission.expectedLevel -ne 89 -or $mission.cultivationPath)) {
        $errors.Add("Escolha de cultivo inválida em $($mission.id)")
    }
    if ($mission.expectedLevel -gt 105 -or $mission.expectedLevel -lt $mission.minLevel) {
        $errors.Add("Nível previsto inválido em $($mission.id)")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    throw "Rotas inválidas: $($errors.Count) erro(s)."
}

$counts = [ordered]@{
    WR = @($missions | Where-Object { $_.routeKey -in @('WR','COMMON') }).Count
    WB = @($missions | Where-Object { $_.routeKey -eq 'COMMON' -or ($_.routeKey -eq 'WBWF' -and $_.classScope -eq 'WB/WF') }).Count
    WF = @($missions | Where-Object { $_.routeKey -in @('WBWF','COMMON') }).Count
    EA = @($missions | Where-Object { $_.routeKey -in @('EAEP','COMMON') }).Count
    EP = @($missions | Where-Object { $_.routeKey -in @('EAEP','COMMON') }).Count
    MC = @($missions | Where-Object { $_.routeKey -in @('MC','COMMON') }).Count
    MG = @($missions | Where-Object { $_.routeKey -in @('WR','COMMON') }).Count
    ES = @($missions | Where-Object { $_.routeKey -in @('MC','COMMON') }).Count
    SK = @($missions | Where-Object { $_.routeKey -eq 'COMMON' -or ($_.routeKey -eq 'SKMS' -and $_.classScope -in @('SK/MS','SK')) }).Count
    MS = @($missions | Where-Object { $_.routeKey -eq 'COMMON' -or ($_.routeKey -eq 'SKMS' -and $_.classScope -in @('SK/MS','MS')) }).Count
}

Write-Host "Rotas válidas. Missões únicas: $($missions.Count)"
Write-Host "Etapas: $(@($missions | ForEach-Object { @($_.steps) }).Count)"
$counts.GetEnumerator() | Format-Table Name, Value -AutoSize
Write-Host 'Totais da fonte acima incluem as alternativas God e Evil; o overlay filtra pelo personagem.'
