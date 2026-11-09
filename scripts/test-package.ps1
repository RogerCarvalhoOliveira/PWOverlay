[CmdletBinding()]
param([switch]$Installer)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $projectRoot 'dist'
$version = [IO.File]::ReadAllText((Join-Path $projectRoot 'VERSION')).Trim()
$executables = @('PW Quest Overlay.exe', 'PW Borderless.exe', 'PW Overlay Central.exe', 'PW Memory Probe.exe')
$required = $executables + @('quest_routes.json', 'memory_offsets.json', 'task_catalog_144.tsv',
    'rota_leveling_pw_144.xlsx', 'LEIA-ME.txt', 'INSTALACAO.txt', 'GUIAS-DEZ-CLASSES.txt', 'GUIA-61-105.txt', 'SEM-PW-HELPER.txt')
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing
$zipPath = Join-Path $dist "PWQuestOverlay-Portatil-v$version.zip"
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $names = @($archive.Entries | ForEach-Object FullName)
    if (@(Compare-Object $required $names).Count -ne 0) { throw 'Conteúdo do ZIP diferente da lista de distribuição.' }
    foreach ($entry in $archive.Entries) {
        $stream = $entry.Open()
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $actual = [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-', '') }
        finally { $hash.Dispose(); $stream.Dispose() }
        if ($actual -ne (Get-FileHash -LiteralPath (Join-Path $dist $entry.FullName)).Hash) {
            throw "Arquivo divergente no ZIP: $($entry.FullName)"
        }
    }
} finally { $archive.Dispose() }
foreach ($name in $executables) {
    $file = Join-Path $dist $name
    $info = [Diagnostics.FileVersionInfo]::GetVersionInfo($file)
    if ($info.ProductVersion -ne $version -or $info.FileVersion -ne "$version.0") {
        throw "Metadados de versão incorretos: $name"
    }
    $icon = [Drawing.Icon]::ExtractAssociatedIcon($file)
    if ($null -eq $icon) { throw "Executável sem ícone: $name" }
    $icon.Dispose()
}
foreach ($name in @('quest_routes.json', 'memory_offsets.json', 'task_catalog_144.tsv', 'rota_leveling_pw_144.xlsx')) {
    if ((Get-FileHash -LiteralPath (Join-Path $projectRoot "data\$name")).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $dist $name)).Hash) { throw "Dados desatualizados no build: $name" }
}
$expectedDownloads = @("PWQuestOverlay-Portatil-v$version.zip")
if ($Installer) {
    $setup = Join-Path $dist "PWQuestOverlay-Setup-v$version.exe"
    $info = [Diagnostics.FileVersionInfo]::GetVersionInfo($setup)
    if ($info.ProductVersion.Trim() -ne $version) { throw 'Versão do instalador divergente.' }
    $expectedDownloads += "PWQuestOverlay-Setup-v$version.exe"
}
$hashLines = @(Get-Content -LiteralPath (Join-Path $dist "SHA256SUMS-v$version.txt"))
foreach ($name in $expectedDownloads) {
    $expected = (Get-FileHash -LiteralPath (Join-Path $dist $name) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $name
    if ($hashLines -notcontains $expected) { throw "Hash de download ausente/incorreto: $name" }
}
Write-Host "Pacote v$version aprovado: arquivos, versões, ícones, dados e hashes. Sem arquivos pessoais ou dependências OCR."
