#define AppVersion "6.1.1"
#define AppBaseName "dw-secret-notes-6.1.1-windows-setup"
[Setup]
AppId=DoMeZos-Ware.dwSecretNotes
AppName=dw Secret Notes
AppVersion={#AppVersion}
AppVerName=dw Secret Notes {#AppVersion}
AppPublisher=DoMeZos-Ware
AppPublisherURL=https://domezos-ware.com
AppSupportURL=https://domezos-ware.com/help.php
AppUpdatesURL=https://domezos-ware.com/install.php
DefaultDirName={localappdata}\Programs\DoMeZos-Ware\dw Secret Notes
DefaultGroupName=DoMeZos-Ware
OutputDir=..\..\Builds\Installer
OutputBaseFilename={#AppBaseName}
UninstallDisplayIcon={app}\dw-secret-notes.exe
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"; GroupDescription: "Weitere Optionen:"
[Files]
Source: "..\..\Builds\Windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\dw Secret Notes"; Filename: "{app}\dw-secret-notes.exe"
Name: "{autodesktop}\dw Secret Notes"; Filename: "{app}\dw-secret-notes.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\dw-secret-notes.exe"; Description: "dw Secret Notes starten"; Flags: nowait postinstall skipifsilent
