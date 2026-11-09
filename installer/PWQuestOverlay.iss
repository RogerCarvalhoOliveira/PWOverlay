#ifndef MyAppVersion
  #error Compile usando scripts/build.ps1 -Installer para carregar VERSION.
#endif

#define MyAppName "PW Quest Overlay"
#define MyAppPublisher "PWOverlay"
#define MyAppExeName "PW Quest Overlay.exe"

[Setup]
AppId={{8D6C663D-85AB-4AD1-A56D-3861323EAC73}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/RogerCarvalhoOliveira/PWOverlay
AppSupportURL=https://github.com/RogerCarvalhoOliveira/PWOverlay/issues
AppUpdatesURL=https://github.com/RogerCarvalhoOliveira/PWOverlay/releases/latest
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
SetupIconFile=..\assets\branding\PWOverlay.ico
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
Source: "..\dist\PW Maintenance.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\quest_routes.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\memory_offsets.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\task_catalog_144.tsv"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\rota_leveling_pw_144.xlsx"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\LEIA-ME.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\INSTALACAO.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\GUIAS-DEZ-CLASSES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\GUIA-61-105.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\SEM-PW-HELPER.txt"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
Type: filesandordirs; Name: "{app}\ocr"

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
function StopInstalledTools(Installing: Boolean): Boolean;
var
  Helper, MaintenanceLog: String;
  ExitCode: Integer;
  Details: TArrayOfString;
  I: Integer;
begin
  if Installing then begin
    ExtractTemporaryFile('PW Maintenance.exe');
    Helper := ExpandConstant('{tmp}\PW Maintenance.exe');
  end else begin
    Helper := ExpandConstant('{tmp}\PW Maintenance.exe');
    if not FileCopy(ExpandConstant('{app}\PW Maintenance.exe'), Helper, False) then begin
      Log('Não foi possível copiar o auxiliar de manutenção.');
      Result := False;
      Exit;
    end;
  end;
  MaintenanceLog := ExpandConstant('{tmp}\PWOverlay-maintenance.log');
  ExitCode := -1;
  Result := Exec(Helper, '"' + ExpandConstant('{app}\.') + '" "' + MaintenanceLog + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ExitCode);
  if Result then Result := ExitCode = 0;
  Log(Format('Encerramento das ferramentas: resultado=%d; log=%s', [ExitCode, MaintenanceLog]));
  if LoadStringsFromFile(MaintenanceLog, Details) then
    for I := 0 to GetArrayLength(Details) - 1 do Log('Manutenção: ' + Details[I]);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not StopInstalledTools(True) then
    Result := 'Não foi possível liberar os arquivos do PW Overlay. Feche a Central, o overlay e o monitor sem bordas desta instalação e tente novamente. O jogo e o PW Helper não precisam ser fechados.';
end;

function InitializeUninstall: Boolean;
begin
  Result := StopInstalledTools(False);
  if not Result then
    MsgBox('Não foi possível encerrar as ferramentas desta instalação. Feche-as e execute a desinstalação novamente. Seus perfis foram preservados.', mbError, MB_OK);
end;

function InitializeSetup(): Boolean;
begin
  Result := IsDotNetInstalled(net48, 0);
  if not Result then
    MsgBox('O PW Quest Overlay precisa do Microsoft .NET Framework 4.x. Instale o .NET Framework 4.8 e execute este instalador novamente.', mbError, MB_OK);
end;
