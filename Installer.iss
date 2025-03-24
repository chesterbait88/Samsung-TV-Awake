#define MyAppName "TV Monitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TVMonitorApp"
#define MyAppExeName "TVMonitorApp.exe"
#define MyAppSourceDir "bin\Release\net8.0-windows"

[Setup]
AppId={{EDA6E64B-3F8A-4FA8-B9C7-9F3ED2D6D5A0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={commonpf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputBaseFilename=TVMonitorSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
SetupIconFile=Resources\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Output
Uninstallable=yes
UsePreviousAppDir=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupentry"; Description: "Start automatically with Windows"; GroupDescription: "Startup options"

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

; Create empty directories with proper permissions
Source: "{#MyAppSourceDir}\*"; DestDir: "{commonappdata}\{#MyAppName}"; Flags: ignoreversion recursesubdirs createallsubdirs; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKLM; Subkey: "Software\{#MyAppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupentry
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}_is1"; ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\{#MyAppExeName}"

[Code]
var
  ResultCode: Integer;

// Create an empty config file and set permissions during installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create and set permissions for config file in CommonAppData
    if not FileExists(ExpandConstant('{commonappdata}\{#MyAppName}\config.ini')) then
    begin
      SaveStringToFile(ExpandConstant('{commonappdata}\{#MyAppName}\config.ini'), '', False);
      Exec('icacls.exe', ExpandConstant('"{commonappdata}\{#MyAppName}\config.ini" /grant Users:(M)'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end;
    
    // Create and set permissions for Logs directory
    if not DirExists(ExpandConstant('{commonappdata}\{#MyAppName}\Logs')) then
    begin
      CreateDir(ExpandConstant('{commonappdata}\{#MyAppName}\Logs'));
      Exec('icacls.exe', ExpandConstant('"{commonappdata}\{#MyAppName}\Logs" /grant Users:(M)'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end;
  end;
end;