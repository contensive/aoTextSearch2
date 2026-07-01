#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]   $LocalDeployTarget  = '',
    [hashtable]$RemoteDeployTarget = $null
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '..\..\Contensive5\scripts\build-addon-collection.psm1') -Force

$projectRoot = (Resolve-Path "$PSScriptRoot\..").Path

Invoke-ContensiveBuild `
    -CollectionName    'aoTextSearch' `
    -CollectionPath    "$projectRoot\collections\aoTextSearch" `
    -SolutionPath      "$projectRoot\source\TextSearch.sln" `
    -BinPath           "$projectRoot\source\bin\Release\netstandard2.0" `
    -DeploymentRoot    'C:\Deployments\aoTextSearch2' `
    -CleanFolders      @(
                           "$projectRoot\source\bin"
                           "$projectRoot\source\obj"
                       ) `
    -UiPath            "$projectRoot\ui" `
    -LocalDeployTarget  $LocalDeployTarget `
    -RemoteDeployTarget $RemoteDeployTarget
