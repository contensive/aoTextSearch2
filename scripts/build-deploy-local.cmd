@echo off
setlocal enabledelayedexpansion

set defaultsFile=%LOCALAPPDATA%\contensive\local-deploy-appname.txt

rem load saved default app name if it exists
set savedApp=
if exist "%defaultsFile%" for /f "usebackq delims=" %%d in ("%defaultsFile%") do set savedApp=%%d

rem build the prompt string
set "promptStr=Enter local site name: "
if defined savedApp set "promptStr=Enter local site name [!savedApp!]: "

rem prompt for app name
set appName=
set /p appName="!promptStr!"

rem if nothing entered, use saved default
if not defined appName (
    if defined savedApp (
        set appName=!savedApp!
    ) else (
        echo No site name entered.
        pause
        exit /b 1
    )
)

rem save the app name for next time (shared across all addons)
if not exist "%LOCALAPPDATA%\contensive" mkdir "%LOCALAPPDATA%\contensive"
>"%defaultsFile%" echo !appName!

@echo Build project and install on site: !appName!
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "& '%~dp0build.ps1' -LocalDeployTarget '!appName!'"
set deployExit=%errorlevel%
pause
exit /b %deployExit%
