@echo off
setlocal
if not "%~1"=="" set "DRAVEN_GAME_CLIENT_ROOT=%~1"
cd /d "%~dp0Draven\bin\Release2"
start "Draven" "Draven.exe"
