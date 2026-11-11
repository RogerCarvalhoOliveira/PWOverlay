#ifndef MyAppVersion
  #error Compile usando scripts/build.ps1 -Installer para carregar VERSION.
#endif

#define MyAppName "PW Quest Overlay"
#define MyAppPublisher "PWOverlay"
#define MyAppExeName "PW Quest Overlay.exe"
#define MyAppGuid "8D6C663D-85AB-4AD1-A56D-3861323EAC73"

[Setup]
AppId={{{#MyAppGuid}}
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
Source: "..\dist\PW Identity Reader.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\PW Borderless.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\PW Overlay Central.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\PW Maintenance.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\quest_routes.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\memory_offsets.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\task_catalog_144.tsv"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\task_history_126.tsv"; DestDir: "{app}"; Flags: ignoreversion
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
var
  UninstallButton, UpdatesButton: TNewButton;
  InstalledUninstaller: String;
  ClosingForExternalAction, CheckingUpdate: Boolean;

function UninstallKey(): String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{{#MyAppGuid}}_is1';
end;

function FindInstalledUninstaller(): String;
var
  Key, FileName, InstallLocation: String;
begin
  Result := '';
  Key := UninstallKey();
  if not RegQueryStringValue(HKCU, Key, 'UninstallString', FileName) then Exit;
  if not RegQueryStringValue(HKCU, Key, 'InstallLocation', InstallLocation) then Exit;
  FileName := RemoveQuotes(Trim(FileName));
  if (CompareText(AddBackslash(ExtractFileDir(FileName)), AddBackslash(InstallLocation)) = 0)
    and (CompareText(Copy(ExtractFileName(FileName), 1, 5), 'unins') = 0)
    and (CompareText(ExtractFileExt(FileName), '.exe') = 0) and FileExists(FileName) then
    Result := FileName;
end;

procedure OpenInstalledUninstaller(Sender: TObject);
var
  ErrorCode: Integer;
begin
  if Exec(InstalledUninstaller, '', ExtractFileDir(InstalledUninstaller), SW_SHOWNORMAL, ewNoWait, ErrorCode) then begin
    ClosingForExternalAction := True;
    WizardForm.Close;
  end else
    MsgBox('Não foi possível abrir o desinstalador. ' + SysErrorMessage(ErrorCode), mbError, MB_OK);
end;

procedure CheckUpdates(Sender: TObject);
var
  Helper, ResultFile, InstalledVersion, DownloadedSetup, CacheDir: String;
  ExitCode: Integer;
  Lines: TArrayOfString;
  NextEnabled, BackEnabled: Boolean;
begin
  CheckingUpdate := True;
  NextEnabled := WizardForm.NextButton.Enabled;
  BackEnabled := WizardForm.BackButton.Enabled;
  UpdatesButton.Enabled := False;
  UninstallButton.Enabled := False;
  WizardForm.NextButton.Enabled := False;
  WizardForm.BackButton.Enabled := False;
  try
    try
      Helper := ExpandConstant('{tmp}\PW Overlay Central.exe');
      ResultFile := ExpandConstant('{tmp}\PWOverlay-update-result.txt');
      if not FileExists(Helper) then ExtractTemporaryFile('PW Overlay Central.exe');
      DeleteFile(ResultFile);
      InstalledVersion := '';
      RegQueryStringValue(HKCU, UninstallKey(), 'DisplayVersion', InstalledVersion);
      if (Pos('"', InstalledVersion) > 0) or (Length(InstalledVersion) > 32) then InstalledVersion := '';
      if not Exec(Helper, '--setup-update "' + ResultFile + '" "' + InstalledVersion + '"',
        ExtractFileDir(Helper), SW_SHOWNORMAL, ewWaitUntilTerminated, ExitCode) then
        RaiseException('Não foi possível abrir a consulta de atualizações. ' + SysErrorMessage(ExitCode));
      if (ExitCode = 0) or (ExitCode = 1) then Exit;
      if ExitCode <> 10 then RaiseException('A consulta de atualizações foi interrompida.');
      if not LoadStringsFromFile(ResultFile, Lines) or (GetArrayLength(Lines) <> 2) then
        RaiseException('A consulta de atualizações não retornou o instalador.');
      DownloadedSetup := Lines[0];
      CacheDir := AddBackslash(ExpandConstant('{localappdata}\PWQuestOverlay\updates'));
      if (CompareText(Copy(DownloadedSetup, 1, Length(CacheDir)), CacheDir) <> 0) or
        (CompareText(ExtractFileExt(DownloadedSetup), '.exe') <> 0) or
        (CompareText(GetSHA256OfFile(DownloadedSetup), Lines[1]) <> 0) then
        RaiseException('Não foi possível verificar o arquivo baixado.');
      if not Exec(DownloadedSetup, '', ExtractFileDir(DownloadedSetup), SW_SHOWNORMAL, ewNoWait, ExitCode) then
        RaiseException('Não foi possível abrir o novo instalador. ' + SysErrorMessage(ExitCode));
      ClosingForExternalAction := True;
    except
      MsgBox(GetExceptionMessage, mbError, MB_OK);
    end;
  finally
    CheckingUpdate := False;
    UpdatesButton.Enabled := True;
    UninstallButton.Enabled := True;
    WizardForm.NextButton.Enabled := NextEnabled;
    WizardForm.BackButton.Enabled := BackEnabled;
  end;
  if ClosingForExternalAction then WizardForm.Close;
end;

procedure InitializeWizard();
begin
  InstalledUninstaller := FindInstalledUninstaller();
  UninstallButton := TNewButton.Create(WizardForm);
  UninstallButton.Parent := WizardForm;
  UninstallButton.SetBounds(ScaleX(12), WizardForm.CancelButton.Top, ScaleX(100), WizardForm.CancelButton.Height);
  UninstallButton.Anchors := [akLeft, akBottom];
  UninstallButton.Caption := 'Desinstalar';
  UninstallButton.OnClick := @OpenInstalledUninstaller;
  UninstallButton.Visible := False;
  UpdatesButton := TNewButton.Create(WizardForm);
  UpdatesButton.Parent := WizardForm;
  UpdatesButton.SetBounds(ScaleX(12), WizardForm.CancelButton.Top, ScaleX(152), WizardForm.CancelButton.Height);
  UpdatesButton.Anchors := [akLeft, akBottom];
  UpdatesButton.Caption := 'Procurar atualizações';
  UpdatesButton.OnClick := @CheckUpdates;
  UpdatesButton.Visible := False;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  UninstallButton.Visible := not WizardSilent and (CurPageID = wpSelectTasks) and (InstalledUninstaller <> '');
  if UninstallButton.Visible then UpdatesButton.Left := ScaleX(120) else UpdatesButton.Left := ScaleX(12);
  UpdatesButton.Visible := not WizardSilent and
    ((CurPageID = wpWelcome) or (CurPageID = wpSelectDir) or (CurPageID = wpSelectTasks) or (CurPageID = wpReady));
end;

procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  if CheckingUpdate then begin
    Cancel := False;
    Confirm := False;
  end else if ClosingForExternalAction then begin
    Cancel := True;
    Confirm := False;
  end;
end;

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
