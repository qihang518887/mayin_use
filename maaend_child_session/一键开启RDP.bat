@echo off
if exist "%~dp0ChildSessionHost.exe" (
    "%~dp0ChildSessionHost.exe" --fix-rdp
    exit /b
)

:: Standalone fallback
setlocal EnableExtensions
fltmc >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)
sc config TermService start= demand >nul 2>&1
net start TermService >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server" /v fDenyTSConnections /t REG_DWORD /d 0 /f >nul
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services" /v fPromptForPassword /t REG_DWORD /d 0 /f >nul
powershell -NoProfile -Command "Add-Type -Namespace W -Name N -MemberDefinition '[DllImport(\"wtsapi32.dll\",SetLastError=true)]public static extern bool WTSEnableChildSessions(int b);'; [void][W.N]::WTSEnableChildSessions(1)" >nul 2>&1
echo RDP fix completed.
pause
