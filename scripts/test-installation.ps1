[CmdletBinding()]
param([string]$LegacyPackagePath)
# Explicit end-to-end test only: installs a separate test AppId under work.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path $root 'work'
$target = [IO.Path]::GetFullPath((Join-Path $work 'installer-e2e'))
if (-not $target.StartsWith([IO.Path]::GetFullPath($work) + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Diretório de teste fora de work.' }
if (Get-Process -Name 'PW Quest Overlay','PW Borderless','PW Overlay Central' -ErrorAction SilentlyContinue) {
    throw 'Feche as ferramentas antes do teste isolado; ele não deve disputar os mutexes da instalação real.'
}
if (Get-Process -Name elementclient -ErrorAction SilentlyContinue) { throw 'Feche os clientes do jogo antes do teste de restauração do monitor.' }
$version = [IO.File]::ReadAllText((Join-Path $root 'VERSION')).Trim()
$testId = '{26869ECF-12A3-4D1D-B237-9081C6CBDB31}'
$definition = [IO.File]::ReadAllText((Join-Path $root 'installer\PWQuestOverlay.iss'))
$definition = $definition.Replace('AppId={{8D6C663D-85AB-4AD1-A56D-3861323EAC73}', 'AppId={' + $testId)
$definition = $definition.Replace('DefaultDirName={localappdata}\Programs\PWQuestOverlay', 'DefaultDirName=' + $target)
$definition = $definition.Replace('DefaultGroupName={#MyAppName}', 'DefaultGroupName=PW Quest Overlay Installer Test')
$definition = $definition.Replace('OutputBaseFilename=PWQuestOverlay-Setup-v{#MyAppVersion}', 'OutputBaseFilename=PWOverlay-Installer-Test')
$definition = $definition.Replace('#define MyAppName "PW Quest Overlay"', '#define MyAppName "PW Quest Overlay Installer Test"')
$source = Join-Path $work 'installer-test.iss'
[IO.File]::WriteAllText($source, $definition, [Text.UTF8Encoding]::new($false))
$compiler = @('C:\Program Files (x86)\Inno Setup 6\ISCC.exe', (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')) |
    Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Inno Setup 6 não encontrado.' }
& $compiler "/DMyAppVersion=$version" $source | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Instalador de teste não compilou.' }
$setup = Join-Path $root 'dist\PWOverlay-Installer-Test.exe'
$protectedState = Join-Path $env:LOCALAPPDATA 'PWQuestOverlay\state.json'
$stateHash = if (Test-Path -LiteralPath $protectedState) { (Get-FileHash -LiteralPath $protectedState).Hash } else { $null }
$preferencesPath = Join-Path $env:LOCALAPPDATA 'PWQuestOverlay\launcher.json'
$preferences = if (Test-Path -LiteralPath $preferencesPath) { Get-Content -LiteralPath $preferencesPath -Raw | ConvertFrom-Json } else { [pscustomobject]@{ UseOverlay=$true; UseBorderless=$true } }
$outside = Join-Path $work 'installer-outside'
New-Item -ItemType Directory -Path $outside -Force | Out-Null
$fixtureSource = Join-Path $outside 'Fixture.cs'
[IO.File]::WriteAllText($fixtureSource, 'class Fixture { static void Main() { System.Threading.Thread.Sleep(60000); } }')
$fixtureExe = Join-Path $outside 'PW Memory Probe.exe'
& 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' /nologo /target:exe /out:$fixtureExe $fixtureSource
if ($LASTEXITCODE -ne 0) { throw 'Fixture de isolamento não compilou.' }
$foreign = $null
function Run-Installer([string]$file, [string]$log) {
    $process = Start-Process -FilePath $file -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="{0}"' -f $log)) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(45000)) { throw "Tempo excedido: $log" }
    if ($process.ExitCode -ne 0) { throw "Instalador retornou $($process.ExitCode): $log" }
}
function Start-TestTools {
    $ids = @()
    foreach ($name in @('PW Quest Overlay.exe','PW Borderless.exe','PW Overlay Central.exe')) {
        $process = Start-Process -FilePath (Join-Path $target $name) -WorkingDirectory $target -WindowStyle Hidden -PassThru
        $ids += $process.Id
    }
    Start-Sleep -Milliseconds 1500
    foreach ($id in $ids) { if (-not (Get-Process -Id $id -ErrorAction SilentlyContinue)) { throw "Ferramenta de teste encerrou antes da atualização: PID $id" } }
    return $ids
}
function Assert-Stopped($ids) {
    foreach ($id in $ids) { if (Get-Process -Id $id -ErrorAction SilentlyContinue) { throw "Processo de teste ainda ativo: PID $id" } }
}
$installed = $false
try {
    Run-Installer $setup (Join-Path $work 'install-clean-test.log'); $installed = $true
    $foreign = Start-Process -FilePath $fixtureExe -WindowStyle Hidden -PassThru
    if ($LegacyPackagePath) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $LegacyPackagePath).Path)
        try {
            foreach ($name in @('PW Quest Overlay.exe','PW Borderless.exe','PW Overlay Central.exe')) {
                $entry = $archive.GetEntry($name); if (-not $entry) { throw "Arquivo legado ausente: $name" }
                $inputStream = $entry.Open(); $outputStream = [IO.File]::Create((Join-Path $target $name))
                try { $inputStream.CopyTo($outputStream) } finally { $inputStream.Dispose(); $outputStream.Dispose() }
            }
        } finally { $archive.Dispose() }
        $ids = Start-TestTools
        Run-Installer $setup (Join-Path $work 'install-legacy-update-test.log')
        Assert-Stopped $ids
    }
    $ids = Start-TestTools
    Run-Installer $setup (Join-Path $work 'install-current-update-test.log')
    Assert-Stopped $ids
    if ($foreign.HasExited) { throw 'O auxiliar encerrou uma cópia fora da pasta alvo.' }
    $ids = Start-TestTools
    Run-Installer (Join-Path $target 'unins000.exe') (Join-Path $work 'uninstall-test.log')
    Assert-Stopped $ids
    if ($foreign.HasExited) { throw 'A desinstalação encerrou uma cópia fora da pasta alvo.' }
    $installed = $false
    foreach ($view in @([Microsoft.Win32.RegistryView]::Registry32, [Microsoft.Win32.RegistryView]::Registry64)) {
        $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser, $view)
        try { $key = $base.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Uninstall\' + $testId + '_is1'); if ($key) { $key.Dispose(); throw 'Registro de desinstalação de teste não foi removido.' } } finally { $base.Dispose() }
    }
    $after = if (Test-Path -LiteralPath $protectedState) { (Get-FileHash -LiteralPath $protectedState).Hash } else { $null }
    if ($after -ne $stateHash) { throw 'state.json mudou durante o teste. Verifique antes de continuar.' }
    $afterPreferences = Get-Content -LiteralPath $preferencesPath -Raw | ConvertFrom-Json
    if ($afterPreferences.UseOverlay -ne $preferences.UseOverlay -or $afterPreferences.UseBorderless -ne $preferences.UseBorderless) { throw 'As seleções da Central mudaram durante o teste.' }
    Write-Host 'Instalação, atualização legada/atual e desinstalação aprovadas; cópia externa preservada; progresso e seleções reais preservados.'
} finally {
    if ($installed -and (Test-Path -LiteralPath (Join-Path $target 'unins000.exe'))) {
        Run-Installer (Join-Path $target 'unins000.exe') (Join-Path $work 'uninstall-test-cleanup.log')
    }
    if ($foreign) { if (-not $foreign.HasExited) { $foreign.Kill(); $foreign.WaitForExit() }; $foreign.Dispose() }
}
