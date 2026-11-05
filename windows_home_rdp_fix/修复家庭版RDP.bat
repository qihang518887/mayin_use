@echo off
setlocal EnableExtensions
title Windows Home to Pro (RDP host fix)
set "EDITION="
for /f "tokens=3" %%A in ('reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v EditionID 2^>nul ^| findstr /i "EditionID"') do set "EDITION=%%A"
if not defined EDITION (
    echo Failed to read the Windows edition.
    pause
    exit /b 1
)
if /i "%~1"=="/check" goto :check
if /i "%~1"=="-check" goto :check

echo Current edition: %EDITION%

fltmc >nul 2>&1
if errorlevel 1 (
    echo Requesting administrator privileges...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo %EDITION% | findstr /i /c:"Core" >nul
if errorlevel 1 goto :activate

dism /Online /English /Get-TargetEditions | findstr /i /c:"Professional" >nul
if errorlevel 1 (
    echo Windows did not report Professional as a target edition.
    echo Try Settings ^> System ^> Activation ^> Change product key instead.
    pause
    exit /b 1
)

call :rebootpending
if defined REBOOTREQ (
    echo Pending reboot detected, restart Windows and run this again.
    pause
    exit /b 1
)

call :readkey
if not defined PROKEY (
    echo.
    echo This changes the Windows edition and needs a restart.
    echo Save your work first.
    set /p "PROKEY=Enter Windows Pro product key: "
)
if not defined PROKEY (
    echo No key entered.
    pause
    exit /b 1
)

echo Installing the Professional key through SPP...
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $null=(([WMISEARCHER]'SELECT Version FROM SoftwareLicensingService').Get()).InstallProductKey('%PROKEY%'); exit 0 } catch { if ($_.Exception.InnerException) { exit $_.Exception.InnerException.HResult } else { exit 1 } }"
set "RC=%ERRORLEVEL%"
set "PROKEY="
if not "%RC%"=="0" (
    echo Failed to install the product key. WMI error code: %RC%
    pause
    exit /b %RC%
)
echo.
echo Edition key installed successfully.
echo Restart Windows, then run this script again to activate with HWID.
pause
exit /b 0

:activate
echo %EDITION% | findstr /i /c:"Professional" >nul
if errorlevel 1 (
    echo Not a Windows Home or Professional edition, nothing to do.
    pause
    exit /b 1
)

call :rebootpending
if defined REBOOTREQ (
    echo Pending reboot detected, restart Windows before activating.
    pause
    exit /b 1
)

set "MASFILE="
if exist "%~dp0HWID_Activation.cmd" set "MASFILE=%~dp0HWID_Activation.cmd"
if not defined MASFILE if exist "%~dp0MAS_AIO.cmd" set "MASFILE=%~dp0MAS_AIO.cmd"
if not defined MASFILE (
    echo HWID_Activation.cmd was not found next to this script.
    echo Put the MAS HWID_Activation.cmd in this folder and run again.
    pause
    exit /b 2
)

echo Activating with HWID...
call "%MASFILE%" /HWID
exit /b %ERRORLEVEL%

:check
echo Current edition: %EDITION%
echo %EDITION% | findstr /i /c:"Core" >nul
if errorlevel 1 (
    echo Not a Windows Home edition, RDP host is available on this edition.
    echo Run this script without arguments to activate with HWID.
) else (
    echo Windows Home detected, this script can switch it to Professional.
)
exit /b 0

:rebootpending
set "REBOOTREQ="
reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending" >nul 2>&1 && set "REBOOTREQ=1"
reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired" >nul 2>&1 && set "REBOOTREQ=1"
exit /b

:readkey
set "PROKEY="
if exist "%~dp0key.txt" for /f "usebackq delims=" %%K in ("%~dp0key.txt") do if not defined PROKEY set "PROKEY=%%K"
if defined PROKEY exit /b 0
set "KPS=%TEMP%\rdpfix_key.ps1"
> "%KPS%" echo $ErrorActionPreference="Stop"
>> "%KPS%" echo $cs="[DllImport(""pkeyhelper.dll"",CharSet=CharSet.Unicode)]public static extern int GetEditionIdFromName(string n,out int i);[DllImport(""pkeyhelper.dll"",CharSet=CharSet.Unicode)]public static extern int SkuGetProductKeyForEdition(int e,string ch,out IntPtr k,out IntPtr c);"
>> "%KPS%" echo [void](Add-Type -Namespace P -Name N -MemberDefinition $cs)
>> "%KPS%" echo $i=0
>> "%KPS%" echo [void][P.N]::GetEditionIdFromName("Professional",[ref]$i)
>> "%KPS%" echo $k=[IntPtr]::Zero
>> "%KPS%" echo $c=[IntPtr]::Zero
>> "%KPS%" echo [void][P.N]::SkuGetProductKeyForEdition($i,"Retail",[ref]$k,[ref]$c)
>> "%KPS%" echo if($k -eq [IntPtr]::Zero){exit 1}
>> "%KPS%" echo [Runtime.InteropServices.Marshal]::PtrToStringUni($k)
for /f "usebackq delims=" %%K in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%KPS%" 2^>nul`) do set "PROKEY=%%K"
del "%KPS%" >nul 2>&1
exit /b 0
