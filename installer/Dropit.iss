; Inno Setup script – per-user install, no admin rights needed.
; Build: iscc /DAppVersion=1.2.3 installer\Dropit.iss  (expects the published app in ..\publish)

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{5B7E3C1A-8F2D-4C6B-9E1A-7D3F2B9C4A10}
AppName=Dropit
AppVersion={#AppVersion}
AppVerName=Dropit {#AppVersion}
AppPublisher=Metamorfolog
DefaultDirName={localappdata}\Programs\Dropit
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=lowest
OutputDir=..\artifacts
OutputBaseFilename=Dropit-Setup-{#AppVersion}
SetupIconFile=..\src\Dropit.App\Assets\Dropit.ico
UninstallDisplayIcon={app}\Dropit.exe
UninstallDisplayName=Dropit
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=force
RestartApplications=no

[Languages]
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{userprograms}\Dropit"; Filename: "{app}\Dropit.exe"

[Run]
Filename: "{app}\Dropit.exe"; Description: "{cm:LaunchProgram,Dropit}"; Flags: nowait postinstall skipifsilent
; Silent install = in-app update: start the new version again.
Filename: "{app}\Dropit.exe"; Parameters: "--updated"; Flags: nowait; Check: WizardSilent

[UninstallDelete]
; FFmpeg and Pandoc downloaded on demand by the app.
Type: filesandordirs; Name: "{localappdata}\Dropit\tools"

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c taskkill /im Dropit.exe /f"; Flags: runhidden; RunOnceId: "StopApp"
Filename: "{app}\Dropit.exe"; Parameters: "--unregister"; Flags: runhidden waituntilterminated; RunOnceId: "Unregister"
