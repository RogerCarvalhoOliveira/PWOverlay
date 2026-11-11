[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testDirectory = Join-Path $projectRoot 'dist\tests'
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
$library = Join-Path $testDirectory 'OverlayCandidate.dll'
$executable = Join-Path $testDirectory 'OverlayInteractionTests.exe'
$sources = @('AppBranding','AppShutdown','Models','QuestProgression','ClientIdentity','ClientProfilesForm','NativeMethods','OverlayForm','CultivationChoiceForm','MarkerCalibrationForm','RouteEditorForm',
    'ProcessMemoryReader','MemoryLayout','PlayerIdentityMemory',
    'TaskCatalog','RttiResolver','QuestMemoryReader','QuestStateTracker','QuestMemoryMonitor') |
    ForEach-Object { Join-Path $projectRoot ('src\' + $_ + '.cs') }
& $compiler /nologo /target:library /optimize+ /platform:x86 /out:$library `
    "/resource:$(Join-Path $projectRoot 'assets\branding\PWOverlay.ico'),PWOverlay.App.ico" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar a interface para teste.' }
& $compiler /nologo /target:exe /platform:x86 /out:$executable `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:$library (Join-Path $projectRoot 'tests\OverlayInteractionTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar testes da interface.' }
& $executable $projectRoot
if ($LASTEXITCODE -ne 0) { throw 'Testes da interface falharam.' }
