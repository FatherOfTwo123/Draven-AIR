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

set "DRAVEN_AUTO_STOP_SERVER="
tasklist /FI "IMAGENAME eq Draven.exe" | find /I "Draven.exe" >nul
if errorlevel 1 (
    if not exist "%~dp0run_sql_and_draven.bat" (
        echo run_sql_and_draven.bat not found.
        pause
        exit /b 1
    )

    set "DRAVEN_AUTO_STOP_SERVER=1"
    start "DravenBootstrap" /min /wait cmd /c ""%~dp0run_sql_and_draven.bat" "%GAME_CLIENT_ROOT%""

    powershell -NoProfile -ExecutionPolicy Bypass -Command "$deadline = (Get-Date).AddSeconds(20); while ((Get-Date) -lt $deadline) { $client = $null; try { $client = New-Object Net.Sockets.TcpClient; $iar = $client.BeginConnect('127.0.0.1', 8080, $null, $null); if ($iar.AsyncWaitHandle.WaitOne(250)) { exit 0 } } catch { } finally { if ($client) { $client.Close() } }; Start-Sleep -Milliseconds 500 }; exit 1" >nul 2>&1
    if errorlevel 1 (
        echo Draven did not become ready in time. Check draven-live-out.log and draven-live-err.log.
        pause
        exit /b 1
    )

    timeout /t 1 /nobreak >nul
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -like '*direct_air_maestro.js*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >nul 2>&1
taskkill /IM LolClient.exe /F >nul 2>&1

start "direct_air_maestro" /min node "%~dp0direct_air_maestro.js" "%GAME_CLIENT_ROOT%"
