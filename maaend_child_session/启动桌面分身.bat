@echo off
set "EXE=%~dp0ChildSessionHost.exe"
set "DIR=%~dp0"
if "%~1"=="" (
    set "ARG=--start"
) else (
    set "ARG="%~1""
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process $env:EXE -ArgumentList $env:ARG -WorkingDirectory $env:DIR -Verb RunAs"
