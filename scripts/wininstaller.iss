; Define a default if not provided
#ifndef MyAppVersion
#define MyAppVersion "0.0.0"
#pragma message "using default version"
#endif

#pragma message "Compiling version " + MyAppVersion

[Setup]
AppName=InkscapeTileMaker
AppVersion={#MyAppVersion}
AppPublisherURL=https://github.com/AidenBradley24/InkscapeTileMaker
AppSupportURL=https://github.com/AidenBradley24/InkscapeTileMaker/issues
AppUpdatesURL=https://github.com/AidenBradley24/InkscapeTileMaker/releases

DefaultDirName={autopf}\InkscapeTileMaker
DefaultGroupName=InkscapeTileMaker
OutputBaseFilename=InkscapeTileMakerInstaller
OutputDir=..\Output
Compression=lzma
SolidCompression=yes
DisableProgramGroupPage=yes

LicenseFile=..\LICENSE.txt
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=Aiden Bradley
VersionInfoDescription=
VersionInfoCopyright=© 2026 Aiden Bradley

[Files]
Source: "..\InkscapeTileMaker\bin\Release\net10.0-windows10.0.19041.0\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\InkscapeTileMaker"; Filename: "{app}\InkscapeTileMaker.exe"

[Run]
; Run your app after install
Filename: "{app}\InkscapeTileMaker.exe"; \
  Parameters: "serve"; \
  Description: "Run InkscapeTileMaker"; \
  Flags: nowait postinstall skipifsilent
