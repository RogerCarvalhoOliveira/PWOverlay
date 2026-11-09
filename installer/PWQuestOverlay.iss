#ifndef MyAppVersion
  #define MyAppVersion "0.8.0"
#endif

#define MyAppName "PW Quest Overlay"
#define MyAppPublisher "PWOverlay"
#define MyAppExeName "PW Quest Overlay.exe"

[Setup]
AppId={{8D6C663D-85AB-4AD1-A56D-3861323EAC73}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\PWQuestOverlay
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
MinVersion=6.1sp1
OutputDir=..\dist
OutputBaseFilename=PWQuestOverlay-Setup-v{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
AppMutex=Local\PWQuestOverlay
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho do PW Overlay Central na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "desktopborderless"; Description: "Criar atalho do PW Borderless na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "startupcentral"; Description: "Abrir o PW Overlay Central ao entrar no Windows"; GroupDescription: "Inicialização automática:"; Flags: unchecked
Name: "startupoverlay"; Description: "Iniciar o overlay ao entrar no Windows"; GroupDescription: "Inicialização automática:"; Flags: unchecked
Name: "startupborderless"; Description: "Iniciar o PW Borderless ao entrar no Windows"; GroupDescription: "Inicialização automática:"; Flags: unchecked

[Files]
Source: "..\dist\PW Quest Overlay.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\PW Borderless.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\PW Overlay Central.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\quest_routes.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\memory_offsets.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\task_catalog_144.tsv"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\rota_leveling_pw_144.xlsx"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\LEIA-ME.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\INSTALACAO.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\ocr\*"; DestDir: "{app}\ocr"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\PW Overlay Central"; Filename: "{app}\PW Overlay Central.exe"; WorkingDir: "{app}"
Name: "{group}\PW Quest Overlay (direto)"; Filename: "{app}\PW Quest Overlay.exe"; WorkingDir: "{app}"
Name: "{group}\PW Borderless"; Filename: "{app}\PW Borderless.exe"; WorkingDir: "{app}"
Name: "{group}\Guia de instalação e configuração"; Filename: "{app}\INSTALACAO.txt"
Name: "{group}\Desinstalar PW Quest Overlay"; Filename: "{uninstallexe}"
Name: "{autodesktop}\PW Overlay Central"; Filename: "{app}\PW Overlay Central.exe"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{autodesktop}\PW Borderless"; Filename: "{app}\PW Borderless.exe"; WorkingDir: "{app}"; Tasks: desktopborderless
Name: "{userstartup}\PW Overlay Central"; Filename: "{app}\PW Overlay Central.exe"; WorkingDir: "{app}"; Tasks: startupcentral
Name: "{userstartup}\PW Quest Overlay"; Filename: "{app}\PW Quest Overlay.exe"; WorkingDir: "{app}"; Tasks: startupoverlay
Name: "{userstartup}\PW Borderless"; Filename: "{app}\PW Borderless.exe"; WorkingDir: "{app}"; Tasks: startupborderless

[Run]
Filename: "{app}\INSTALACAO.txt"; Description: "Abrir o guia de configuração"; Flags: postinstall shellexec skipifsilent
Filename: "{app}\PW Overlay Central.exe"; Description: "Abrir o PW Overlay Central"; Flags: postinstall nowait skipifsilent unchecked

[Code]
function HasDotNetFramework4(): Boolean;
begin
  Result := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full') or
    (IsWin64 and RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full'));
end;

function InitializeSetup(): Boolean;
begin
  Result := HasDotNetFramework4();
  if not Result then
    MsgBox('O PW Quest Overlay precisa do Microsoft .NET Framework 4.x. Instale o .NET Framework 4.8 e execute este instalador novamente.', mbError, MB_OK);
end;
