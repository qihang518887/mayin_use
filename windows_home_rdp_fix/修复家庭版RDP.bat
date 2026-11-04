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
echo %EDITION% | findstr /i /c:"Core" >nul
if errorlevel 1 (
    echo This script only upgrades Windows Home editions, nothing to do.
    pause
    exit /b 1
)

fltmc >nul 2>&1
if errorlevel 1 (
    echo Requesting administrator privileges...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

dism /Online /English /Get-TargetEditions | findstr /i /c:"Professional" >nul
if errorlevel 1 (
    echo Windows did not report Professional as a target edition.
    echo Try Settings ^> System ^> Activation ^> Change product key instead.
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

set "CHANGEPK=%SystemRoot%\System32\changepk.exe"
if exist "%SystemRoot%\Sysnative\changepk.exe" set "CHANGEPK=%SystemRoot%\Sysnative\changepk.exe"
echo Starting the edition upgrade...
"%CHANGEPK%" /ProductKey "%PROKEY%"
set "RC=%ERRORLEVEL%"
set "PROKEY="
echo changepk exit code: %RC%
echo Restart Windows, then run this script with /check.
pause
exit /b %RC%

:check
echo Current edition: %EDITION%
echo %EDITION% | findstr /i /c:"Core" >nul
if errorlevel 1 (
    echo Not a Windows Home edition, RDP host is available on this edition.
) else (
    echo Windows Home detected, this script can switch it to Professional.
)
exit /b 0

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
