@echo off
setlocal
cd /d "%~dp0"

set "GAME_CLIENT_ROOT=%~1"
if "%GAME_CLIENT_ROOT%"=="" set "GAME_CLIENT_ROOT=%DRAVEN_GAME_CLIENT_ROOT%"
if "%GAME_CLIENT_ROOT%"=="" if exist "%~dp0..\League of Legends\RADS\projects\lol_air_client\releases\0.0.1.88\deploy\LolClient.exe" set "GAME_CLIENT_ROOT=%~dp0..\League of Legends"
if "%GAME_CLIENT_ROOT%"=="" if exist "%~dp0RADS\projects\lol_air_client\releases\0.0.1.88\deploy\LolClient.exe" set "GAME_CLIENT_ROOT=%~dp0"

if "%GAME_CLIENT_ROOT%"=="" (
    echo Usage: RunDirectAirWithMaestro.bat "C:\Path\To\GameClient420"
    echo.
    echo Or set DRAVEN_GAME_CLIENT_ROOT before running this script.
    echo If the repo sits next to your 4.20 client folder, that path is auto-detected.
    pause
    exit /b 1
)

if not exist "%GAME_CLIENT_ROOT%\RADS\projects\lol_air_client\releases\0.0.1.88\deploy\LolClient.exe" (
    echo Could not find the 4.20 game client under:
    echo   %GAME_CLIENT_ROOT%
    echo.
    echo Pass the client root folder that contains RADS.
    pause
    exit /b 1
)

set "DRAVEN_GAME_CLIENT_ROOT=%GAME_CLIENT_ROOT%"

set "DRAVEN_EXE=%~dp0Draven\bin\Release2\Draven.exe"
set "DRAVEN_AUTO_STOP_SERVER="
if exist "%DRAVEN_EXE%" (
    tasklist /FI "IMAGENAME eq Draven.exe" | find /I "Draven.exe" >nul
    if errorlevel 1 (
        set "DRAVEN_AUTO_STOP_SERVER=1"
        start "Draven" /min "%DRAVEN_EXE%"
    )
) else (
    echo Draven.exe not found. Build first or run run_sql_and_draven.bat.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -like '*direct_air_maestro.js*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >nul 2>&1
taskkill /IM LolClient.exe /F >nul 2>&1

start "direct_air_maestro" /min node "%~dp0direct_air_maestro.js" "%GAME_CLIENT_ROOT%"
