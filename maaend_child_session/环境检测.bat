@echo off
chcp 65001 >nul
"%~dp0ChildSessionHost.exe" --check
pause
