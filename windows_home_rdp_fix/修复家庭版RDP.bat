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

echo.
echo This changes the Windows edition and needs a restart.
echo Save your work first. A valid Windows Pro product key is required.
set "PROKEY="
if exist "%~dp0key.txt" for /f "usebackq delims=" %%K in ("%~dp0key.txt") do if not defined PROKEY set "PROKEY=%%K"
if not defined PROKEY set /p "PROKEY=Enter Windows Pro product key: "
if not defined PROKEY (
    echo No key entered.
    pause
    exit /b 1
)

set "CHANGEPK=%SystemRoot%\System32\changepk.exe"
if exist "%SystemRoot%\Sysnative\changepk.exe" set "CHANGEPK=%SystemRoot%\Sysnative\changepk.exe"
echo Starting the official Windows edition upgrade...
"%CHANGEPK%" /ProductKey "%PROKEY%"
set "RC=%ERRORLEVEL%"
set "PROKEY="
echo changepk exit code: %RC%
echo Restart Windows after the upgrade, then run this script with /check.
pause
exit /b %RC%

:check
echo Current edition: %EDITION%
echo %EDITION% | findstr /i /c:"Core" >nul
if errorlevel 1 (
    echo Not a Windows Home edition, RDP host is available on this edition.
) else (
    echo Windows Home detected. RDP host needs a valid Windows Pro product key.
)
exit /b 0
