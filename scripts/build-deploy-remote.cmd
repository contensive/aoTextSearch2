@echo off
setlocal enabledelayedexpansion

set defaultsFile=%LOCALAPPDATA%\contensive\remote-deploy-domain.txt

rem load saved default domain if it exists
set savedDomain=
if exist "%defaultsFile%" for /f "usebackq delims=" %%d in ("%defaultsFile%") do set savedDomain=%%d

rem build the prompt string
set "promptStr=Enter remote domain: "
if defined savedDomain set "promptStr=Enter remote domain [!savedDomain!]: "

rem prompt for domain
set domain=
set /p domain="!promptStr!"

rem if nothing entered, use saved default
if not defined domain (
    if defined savedDomain (
        set domain=!savedDomain!
    ) else (
        echo No domain entered.
        pause
        exit /b 1
    )
)

rem save the domain for next time (shared across all addons)
if not exist "%LOCALAPPDATA%\contensive" mkdir "%LOCALAPPDATA%\contensive"
>"%defaultsFile%" echo !domain!

rem derive site name from first segment of domain
for /f "tokens=1 delims=." %%a in ("!domain!") do set siteName=%%a

@echo Build project and deploy to !domain!
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "& '%~dp0build.ps1' -RemoteDeployTarget @{Url='https://!domain!/installCollection'; SiteName='!siteName!'}"
set deployExit=%errorlevel%
pause
exit /b %deployExit%
