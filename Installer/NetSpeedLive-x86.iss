; NetSpeed Live — Inno Setup installer, 32-bit (x86) target (Stage 16A)
;
; Public branding only: end users see "NetSpeed Live" / KSA AI STUDIO.
; The internal assembly stays NetPulseOverlay.exe (do not rename).
; This installer packages the self-contained win-x86 publish so it installs
; on 32-bit Windows. The x64 installer (NetSpeedLive.iss) is unchanged and both
; architectures are offered separately.
;
; Version note: <Version> in NetPulseOverlay.csproj is the authoritative
; version source. Keep the #define below in sync (1.1.0).
;
; Compile:  ISCC.exe Installer\NetSpeedLive-x86.iss
; Output:   Release\NetSpeedLive-Setup-<version>-x86.exe

#define MyAppName "NetSpeed Live"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "KSA AI STUDIO"
#define MyAppExeName "NetPulseOverlay.exe"
#define MyAppPublishDir "..\Release\NetSpeedLive-" + MyAppVersion + "-win-x86"

[Setup]
; Distinct AppId from the x64 installer so the two architectures have their own
; uninstall identities and never collide if both are installed.
AppId={{9A6E2B7C-4D3F-4E8A-9B21-C5D7E1F0A833}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=© 2026 KSA AI STUDIO
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} (x86) installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
; 32-bit installer. x86compatible is the default for a 32-bit Setup program: it
; runs on 32-bit Windows AND x64 Windows via WOW64 (the payload is x86).
ArchitecturesAllowed=x86compatible
; Per-user installation: no administrator privileges required.
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\Release
OutputBaseFilename=NetSpeedLive-Setup-{#MyAppVersion}-x86
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} (x86)
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
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