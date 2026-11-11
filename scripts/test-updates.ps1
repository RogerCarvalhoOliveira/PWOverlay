[CmdletBinding()]
param([switch]$DownloadPublished)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $arguments = @('-NoProfile', '-File', $PSCommandPath)
    if ($DownloadPublished) { $arguments += '-DownloadPublished' }
    & powershell.exe @arguments
    exit $LASTEXITCODE
}

$root = Split-Path -Parent $PSScriptRoot
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root 'dist\PW Overlay Central.exe'))
$type = $assembly.GetType('PWOverlayCentral.ReleaseUpdate', $true)
$static = [Reflection.BindingFlags]'NonPublic, Static'
$instance = [Reflection.BindingFlags]'NonPublic, Instance'
$parse = $type.GetMethod('ReadRelease', $static)
$version = [version]'0.13.5'
$asset = @{
    name = 'PWQuestOverlay-Setup-v0.14.0.exe'
    browser_download_url = 'https://github.com/RogerCarvalhoOliveira/PWOverlay/releases/download/v0.14.0/PWQuestOverlay-Setup-v0.14.0.exe'
    digest = 'sha256:' + ('a' * 64)
    size = 100
}
$fixture = @{ tag_name = 'v0.14.0'; draft = $false; prerelease = $false; assets = @($asset) }
$release = $parse.Invoke($null, @([string](ConvertTo-Json $fixture -Depth 4), $version))
if ($null -eq $release) { throw 'O analisador perdeu uma atualização disponível.' }
if ($null -ne $parse.Invoke($null, @([string](ConvertTo-Json $fixture -Depth 4), [version]'0.14.0'))) { throw 'O analisador ofereceu a versão atual.' }
if ($null -ne $parse.Invoke($null, @([string](ConvertTo-Json $fixture -Depth 4), [version]'1.0.0'))) { throw 'O analisador ofereceu downgrade.' }
$fixture.prerelease = $true
if ($null -ne $parse.Invoke($null, @([string](ConvertTo-Json $fixture -Depth 4), $version))) { throw 'Uma versão de teste foi oferecida.' }
$fixture.prerelease = $false
$asset.browser_download_url = 'https://example.invalid/setup.exe'
$rejected = $false
try { $null = $parse.Invoke($null, @([string](ConvertTo-Json $fixture -Depth 4), $version)) }
catch { $rejected = $_.Exception.InnerException -is [IO.InvalidDataException] }
if (!$rejected) { throw 'O analisador aceitou um instalador fora do repositório.' }
Write-Host 'Atualizador: versão, canal estável e URL oficial verificados.'

if ($DownloadPublished) {
    $token = [Threading.CancellationToken]::None
    $check = $type.GetMethod('Check', $static)
    $latest = $check.Invoke($null, @([version]'0.0.0', $token))
    if ($null -eq $latest) { throw 'Nenhuma release estável disponível.' }
    $setup = [string]$type.GetMethod('Download', $instance).Invoke($latest, @($token))
    try {
        $type.GetMethod('Verify', $instance).Invoke($latest, @($setup))
        Write-Host ('Instalador publicado conferido por SHA-256: ' + (Split-Path -Leaf $setup))
        $file = [IO.File]::Open($setup, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite)
        try {
            $first = $file.ReadByte()
            $file.Position = 0
            $file.WriteByte([byte]($first -bxor 1))
        } finally { $file.Dispose() }
        $rejected = $false
        try { $type.GetMethod('Verify', $instance).Invoke($latest, @($setup)) }
        catch { $rejected = $_.Exception.InnerException -is [IO.InvalidDataException] }
        if (!$rejected) { throw 'O atualizador aceitou um arquivo alterado.' }
        Write-Host 'Arquivo com conteúdo alterado foi rejeitado.'
    } finally {
        Remove-Item -LiteralPath $setup
        Remove-Item -LiteralPath (Split-Path -Parent $setup)
    }
}
