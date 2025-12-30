; MineTray Installer Script for Inno Setup
; Run with Inno Setup Compiler: iscc installer.iss

#define MyAppName "MineTray"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "MineTray"
#define MyAppURL "https://github.com/kihi78/MineTray"
#define MyAppExeName "MineTray.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{8B8363F2-1ED5-4786-A6ED-C1FD5BD5B4A0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output settings
OutputDir=installer
OutputBaseFilename=MineTray_Setup_{#MyAppVersion}
SetupIconFile=MineTray\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
; Windows version requirements
MinVersion=10.0
; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Windows起動時に自動起動"; GroupDescription: "その他:"; Flags: unchecked

[Files]
Source: "publish\MineTray.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Close running instance before uninstall
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Try to close running instance gracefully
  Exec('taskkill', '/IM MineTray.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;
