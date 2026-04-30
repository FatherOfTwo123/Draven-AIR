@echo off
setlocal
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -like '*direct_air_maestro.js*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >nul 2>&1
taskkill /IM Draven.exe /F >nul 2>&1
taskkill /IM LolClient.exe /F >nul 2>&1

if exist "%~dp0draven.pid" del /q "%~dp0draven.pid" >nul 2>&1

echo Stopped local AIR stack processes.
