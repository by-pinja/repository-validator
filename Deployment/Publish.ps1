<#
    .SYNOPSIS
    Packs and publishes ValidationLibrary.AzureFunctions to Azure.
    This is meant to be an from repository root.

    .DESCRIPTION
    Packs and publishes ValidationLibrary.AzureFunctions to Azure.
    Scripts expects that the web app is already created.

    This assumes that the user has already logged in to the Azure Powershell Module.

    .PARAMETER ResourceGroup
    Name of the resource group that has the web app deployed

    .PARAMETER WebAppName
    Name of the target web app

    .EXAMPLE
    .\Publish.ps1 -ResourceGroup "github-test" -WebAppName "hjni-test"
#>
param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter()][string]$WebAppName = $ResourceGroup,
    [Parameter()][string]$VersionSuffx = "DEV")

$ErrorActionPreference = "Stop"
	
$publishFolder = "publish"
$validatorAzureFunctionProject = 'ValidationLibrary.AzureFunctions';
$monitorAzureFunctionsProject = 'StatusFunction';

# delete any previous publish
if (Test-path $publishFolder) { Remove-Item -Recurse -Force $publishFolder }

dotnet publish -c Release -o "$publishFolder/$validatorAzureFunctionProject" $validatorAzureFunctionProject --version-suffix $VersionSuffx
dotnet publish -c Release -o "$publishFolder/$monitorAzureFunctionsProject" $monitorAzureFunctionsProject --version-suffix $VersionSuffx

$validatorOutputZip = "validator-app.zip"
$fullSourcePath = (Resolve-Path "$publishFolder/$validatorAzureFunctionProject").Path
$fullTargetPath = (Resolve-Path ".\").Path
$validatorFullZipTarget = Join-Path -Path $fullTargetPath -ChildPath $validatorOutputZip
Compress-Archive -DestinationPath $validatorFullZipTarget -Path "$fullSourcePath/*" -Force

$monitorOutputZip = "monitor-app.zip"
$fullSourcePath = (Resolve-Path "$publishFolder/$monitorAzureFunctionsProject").Path
$fullTargetPath = (Resolve-Path ".\").Path
$monitorFullZipTarget = Join-Path -Path $fullTargetPath -ChildPath $monitorOutputZip
Compress-Archive -DestinationPath $monitorFullZipTarget -Path "$fullSourcePath/*" -Force

Write-Host "Deploying new version."
Publish-AzWebApp -ResourceGroupName $ResourceGroup -Name $WebAppName -ArchivePath $validatorFullZipTarget -Force
Publish-AzWebApp -ResourceGroupName $ResourceGroup -Name "$WebAppName-monitor" -ArchivePath $monitorFullZipTarget -Force
Write-Host 'Version deployed'
