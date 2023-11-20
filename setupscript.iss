; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "LaserLR50"
#define MyAppVersion "1.0"
#define MyAppPublisher "ISK, Ilc."
#define MyAppExeName "NewLaserProject.exe"
#define AdvantechDriverExec "Advantech Common Motion Driver and Utility_20220316.exe"
#define dotnetruntimeinstaller "windowsdesktop-runtime-7.0.12-win-x64.exe"
#define dotnetruntime6 "windowsdesktop-runtime-6.0.23-win-x64.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
PrivilegesRequired=admin
AppId={{38C9799C-DAB0-41E2-A1D4-AA6EEAF71C7B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=C:\Users\Serj\source\repos\NewLaserProject
OutputBaseFilename=LR50Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Components]
Name: "advantechdrivers"; Description: "���������� �������� Advantech"; 
Name: "lmcboard"; Description: "���������� �������� LMCV4-DIGIT";
Name: "dotnetruntime7"; Description: "���������� .Net7 Runtime";
Name: "dotnetruntime6"; Description: "���������� .Net6 Runtime";

[Files]
Source: "D:\Software\{#AdvantechDriverExec}"; DestDir: "{app}\drivers"; Flags: ignoreversion; Components: advantechdrivers
Source: "NewLaserProject\bin\Debug\net6.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion; Permissions: admins-full
Source: "NewLaserProject\bin\Debug\net6.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Permissions: admins-full
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "D:\Software\LMCV4_20230423\LMCV4_20230423\WIN7_64\Lmcv4u.sys"; DestDir: "{app}\drivers"
Source: "D:\Software\LMCV4_20230423\LMCV4_20230423\WIN7_64\Lmcv4u.inf"; DestDir: "{app}\drivers"
Source: "D:\Software\LMCV4_20230423\LMCV4_20230423\WIN7_64\LMCV4U.cat"; DestDir: "{app}\drivers"
Source: "D:\Software\LMCV4_20230423\LMCV4_20230423\WIN7_64\WdfCoInstaller01009.dll"; DestDir: "{app}\drivers"
Source: "D:\Software\{#dotnetruntimeinstaller}"; DestDir: "{app}\drivers"; Flags: 64bit
Source: "D:\Software\{#dotnetruntime6}"; DestDir: "{app}\drivers"; Flags: 64bit

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\drivers\{#AdvantechDriverExec}"; Flags: skipifsilent nowait; StatusMsg: "Installing Advantech drivers"; Components: advantechdrivers
Filename: "{sys}\rundll32.exe"; Parameters: "SETUPAPI.DLL,InstallHinfSection DefaultInstall 132 {app}\drivers\Lmcv4u.inf"; WorkingDir: "{app}\drivers"; StatusMsg: "Installing LMCV4-DIGIT drivers"; Components: lmcboard
Filename: "{app}\drivers\{#dotnetruntimeinstaller}"; Flags: skipifsilent nowait; StatusMsg: "Installing .Net runtime"; Components: dotnetruntime7
Filename: "{app}\drivers\{#dotnetruntime6}"; Flags: skipifsilent nowait; StatusMsg: "Installing .Net runtime"; Components: dotnetruntime6
Filename: "{app}\{#MyAppExeName}"; Flags: runascurrentuser nowait postinstall skipifsilent; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"