;
; Script generated by the ASCOM Driver Installer Script Generator 6.2.0.0
; Generated by Lukas Demetz on 6/27/2018 (UTC)
;
[Setup]
AppID={{5dd64651-0423-490e-92a5-ec2fd7f7451e}
AppName=ASCOM HakosRoof Dome Driver
AppVerName=ASCOM HakosRoof Dome Driver 1.0.1
AppVersion=1.0.1
AppPublisher=Lukas Demetz <lukas.demetz@gmail.com>
AppPublisherURL=mailto:lukas.demetz@gmail.com
AppSupportURL=http://tech.groups.yahoo.com/group/ASCOM-Talk/
AppUpdatesURL=http://ascom-standards.org/
VersionInfoVersion=1.0.0
MinVersion=0,6.0.2195sp4
DefaultDirName="{cf}\ASCOM\Dome"
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir="."
OutputBaseFilename="HakosRoof Setup"
Compression=lzma
SolidCompression=yes
; Put there by Platform if Driver Installer Support selected
WizardImageFile="C:\Program Files (x86)\ASCOM\Platform 6 Developer Components\Installer Generator\Resources\WizardImage.bmp"
LicenseFile="C:\Program Files (x86)\ASCOM\Platform 6 Developer Components\Installer Generator\Resources\CreativeCommons.txt"
; {cf}\ASCOM\Uninstall\Dome folder created by Platform, always
UninstallFilesDir="{cf}\ASCOM\Uninstall\Dome\HakosRoof"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{cf}\ASCOM\Uninstall\Dome\HakosRoof"
; TODO: Add subfolders below {app} as needed (e.g. Name: "{app}\MyFolder")

[Files]
;Source: "C:\Users\Lukas\source\repos\HakosRoof\HakosRoof\bin\Release\ASCOM.HakosRoof.Dome.dll"; DestDir: "{app}"
Source: "C:\Users\interski\Source\Repos\HakosRoof\HakosRoof\bin\Release\*"; DestDir:"{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; TODO: Add other files needed by your driver here (add subfolders above)


; Only if driver is .NET
[Run]
; Only for .NET assembly/in-proc drivers
Filename: "{dotnet4032}\regasm.exe"; Parameters: "/codebase ""{app}\ASCOM.HakosRoof.Dome.dll"""; Flags: runhidden 32bit
Filename: "{dotnet4064}\regasm.exe"; Parameters: "/codebase ""{app}\ASCOM.HakosRoof.Dome.dll"""; Flags: runhidden 64bit; Check: IsWin64




; Only if driver is .NET
[UninstallRun]
; Only for .NET assembly/in-proc drivers
Filename: "{dotnet4032}\regasm.exe"; Parameters: "-u ""{app}\ASCOM.HakosRoof.Dome.dll"""; Flags: runhidden 32bit
; This helps to give a clean uninstall
Filename: "{dotnet4064}\regasm.exe"; Parameters: "/codebase ""{app}\ASCOM.HakosRoof.Dome.dll"""; Flags: runhidden 64bit; Check: IsWin64
Filename: "{dotnet4064}\regasm.exe"; Parameters: "-u ""{app}\ASCOM.HakosRoof.Dome.dll"""; Flags: runhidden 64bit; Check: IsWin64




[CODE]
//
// Before the installer UI appears, verify that the (prerequisite)
// ASCOM Platform 6.2 or greater is installed, including both Helper
// components. Utility is required for all types (COM and .NET)!
//
function InitializeSetup(): Boolean;
var
   U : Variant;
   H : Variant;
begin
   Result := TRUE;  // Assume failure
   // check that the DriverHelper and Utilities objects exist, report errors if they don't
   //try
   //   H := CreateOLEObject('DriverHelper.Util');
   //except
   //   MsgBox('The ASCOM DriverHelper object has failed to load, this indicates a serious problem with the ASCOM installation', mbInformation, MB_OK);
   //end;
   //try
    //  U := CreateOLEObject('ASCOM.Utilities.Util');
   //except
    //  MsgBox('The ASCOM Utilities object has failed to load, this indicates that the ASCOM Platform has not been installed correctly', mbInformation, MB_OK);
   //end;
   //try
   ///   if (U.IsMinimumRequiredVersion(6,2)) then	// this will work in all locales
   // /     Result := TRUE;
   //except
   //end;
   //if(not Result) then
    //  MsgBox('The ASCOM Platform 6.2 or greater is required for this driver.', mbInformation, MB_OK);
end;

// Code to enable the installer to uninstall previous versions of itself when a new version is installed
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  UninstallExe: String;
  UninstallRegistry: String;
begin
  if (CurStep = ssInstall) then // Install step has started
	begin
      // Create the correct registry location name, which is based on the AppId
      UninstallRegistry := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}' + '_is1');
      // Check whether an extry exists
      if RegQueryStringValue(HKLM, UninstallRegistry, 'UninstallString', UninstallExe) then
        begin // Entry exists and previous version is installed so run its uninstaller quietly after informing the user
          MsgBox('Setup will now remove the previous version.', mbInformation, MB_OK);
          Exec(RemoveQuotes(UninstallExe), ' /SILENT', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
          sleep(1000);    //Give enough time for the install screen to be repainted before continuing
        end
  end;
end;

