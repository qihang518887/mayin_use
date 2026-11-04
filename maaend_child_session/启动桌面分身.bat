@echo off
if not "%~1"=="" (
    start "" "%~dp0ChildSessionHost.exe" "%~1"
) else (
    start "" "%~dp0ChildSessionHost.exe" --start
)
