@echo off
setlocal

rem Uploads the built collection to the Addon Collection Library on contensive.com.
rem TODO: verify CollectionName, CollectionPath, and DeploymentPath are correct for this repo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy-to-addon-library.ps1" ^
    -CollectionName "aoTextSearch" ^
    -CollectionPath "%~dp0..\collections\aoTextSearch" ^
    -DeploymentPath "C:\Deployments\aoTextSearch2" ^
    -UiPath "%~dp0..\ui"

set exitCode=%errorlevel%
pause
exit /b %exitCode%