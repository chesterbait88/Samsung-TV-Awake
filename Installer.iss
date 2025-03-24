#define MyAppName "TV Monitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TVMonitorApp"
#define MyAppExeName "TVMonitorApp.exe"
#define MyAppSourceDir "bin\Release\net8.0-windows"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{EDA6E64B-3F8A-4FA8-B9C7-9F3ED2D6D5A0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputBaseFilename=TVMonitorSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=Resources\app.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start automatically with Windows"; GroupDescription: "Startup options"

[Files]
; Main application files
Source: "{#MyAppSourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSourceDir}\TVMonitorApp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSourceDir}\TVMonitorApp.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSourceDir}\TVMonitorApp.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; Dependencies
Source: "{#MyAppSourceDir}\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSourceDir}\runtimes\win\lib\net8.0\System.Management.dll"; DestDir: "{app}\runtimes\win\lib\net8.0"; Flags: ignoreversion

; Resources
Source: "{#MyAppSourceDir}\Resources\app.ico"; DestDir: "{app}\Resources"; Flags: ignoreversion

; Create empty directories
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}\Logs"; Flags: ignoreversion recursesubdirs createallsubdirs; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKA; Subkey: "Software\{#MyAppName}"; Flags: uninsdeletekey

[Code]
var
  ResultCode: Integer;

// Create an empty config file and set permissions during installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not FileExists(ExpandConstant('{app}\config.ini')) then
    begin
      SaveStringToFile(ExpandConstant('{app}\config.ini'), '', False);
      Exec('icacls.exe', ExpandConstant('"{app}\config.ini" /grant Users:(M)'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end;
  end;
end;