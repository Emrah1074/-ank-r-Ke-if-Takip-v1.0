[Setup]
AppName=Cankiri Kesif Takip
AppVersion=1.0.0
DefaultDirName={autopf}\CankiriKesifTakip
DefaultGroupName=Cankiri Kesif Takip
OutputDir=installer
OutputBaseFilename=CankiriKesifTakip_Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "Masaustune kisayol olustur"; GroupDescription: "Ek gorevler:"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Cankiri Kesif Takip"; Filename: "{app}\CankiriKesifTakip.exe"
Name: "{autodesktop}\Cankiri Kesif Takip"; Filename: "{app}\CankiriKesifTakip.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Cankiri Kesif Takip.exe"; Description: "Uygulamayi simdi baslat"; Flags: nowait postinstall skipifsilent
