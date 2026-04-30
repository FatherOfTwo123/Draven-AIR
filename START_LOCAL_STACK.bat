@echo off
setlocal
pushd "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0START_LOCAL_STACK.ps1" %*
set "EXIT_CODE=%ERRORLEVEL%"
popd
if not "%EXIT_CODE%"=="0" (
    echo.
    echo Script failed with exit code %EXIT_CODE%.
    pause
)
exit /b %EXIT_CODE%
