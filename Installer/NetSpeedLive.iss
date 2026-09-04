; NetSpeed Live — Inno Setup installer (Stage 14)
;
; Public branding only: end users see "NetSpeed Live" / KSA AI STUDIO.
; The internal assembly stays NetPulseOverlay.exe (do not rename).
;
; Version note: <Version> in NetPulseOverlay.csproj is the authoritative
; version source. Keep the #define below in sync (1.1.0).
;
; Compile:  ISCC.exe Installer\NetSpeedLive.iss
; Output:   Release\NetSpeedLive-Setup-<version>.exe

#define MyAppName "NetSpeed Live"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "KSA AI STUDIO"
#define MyAppExeName "NetPulseOverlay.exe"
#define MyAppPublishDir "..\Release\NetSpeedLive-" + MyAppVersion + "-win-x64"

[Setup]
AppId={{9A6E2B7C-4D3F-4E8A-9B21-C5D7E1F0A832}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=© 2026 KSA AI STUDIO
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
; Per-user installation: no administrator privileges required.
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\Release
OutputBaseFilename=NetSpeedLive-Setup-{#MyAppVersion}
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Let the app's own single-instance-free lifecycle handle running instances;
; offer to close NetPulseOverlay.exe if it is running during install/upgrade.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; \
    GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppPublishDir}\*"; DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcut:  Start Menu -> NetSpeed Live
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
; Optional desktop shortcut (user selects in the installer).
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; \
    Tasks: desktopicon

[Run]
; Optional launch after installation (skipped in silent installs).
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; \
    Flags: nowait postinstall skipifsilent

; NOTES
; - User settings under %APPDATA%\NetPulseOverlay\ are intentionally NOT
;   touched by install or uninstall, so a reinstall keeps the user's
;   preferences. There is no UninstallDelete/UninstallDeleteType entry.
; - Startup ("Start with Windows") is managed exclusively by the application
;   (HKCU Run via StartupService). The installer never writes a Run entry.
