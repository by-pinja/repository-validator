<#
    .SYNOPSIS
    Creates environment in Azure from given settings file

    .DESCRIPTION
    Creates and prepares and environment for development and testing.
    SettingsFile (default developer-settings.json) should contain all
    relevant information.

    This assumes that the user has already logged in to the Azure Powershell Module.

    .PARAMETER SettinsFile
    Settings file that contains environment settings. Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$tagsHashtable = @{ }
if ($settingsJson.Tags) {
    $settingsJson.Tags.psobject.properties | ForEach-Object { $tagsHashtable[$_.Name] = $_.Value }
}

Write-Host "Creating resource group $($settingsJson.ResourceGroupName) to location $($settingsJson.Location)..."
New-AzResourceGroup -Name $settingsJson.ResourceGroupName -Location $settingsJson.Location -Tag $tagsHashtable -Force

Write-Host 'Creating environment...'
$output = New-AzResourceGroupDeployment `
    -Name 'test-deployment' `
    -TemplateFile 'Deployment/azuredeploy.json' `
    -ResourceGroupName $settingsJson.ResourceGroupName `
    -appName $settingsJson.ResourceGroupName `
    -gitHubToken (ConvertTo-SecureString -String $settingsJson.GitHubToken -AsPlainText -Force) `
    -gitHubOrganization $settingsJson.GitHubOrganization `
    -environment "Development"

Write-Host 'Publishing...'
.\Deployment\Publish.ps1 -ResourceGroup $settingsJson.ResourceGroupName

$statusCheckUrl = .\Deployment\Get-FunctionUri.ps1 `
    -FunctionName 'StatusCheck' `
    -ResourceGroup $settingsJson.ResourceGroupName

$testConfig = @{
    "name"                = "Status Check"
    "url"                 = $statusCheckUrl 
    "expected"            = 200
    "frequency_secs"      = 900
    "timeout_secs"        = 30
    "failedLocationCount" = 1
    "description"         = "Checking that status returns 200"
    "locations"           = @(
        @{
            "Id" = "emea-nl-ams-azr"
        },
        @{
            "Id" = "emea-gb-db3-azr"
        },
        @{
            "Id" = "emea-fr-pra-edge"
        },
        @{
            "Id" = "emea-se-sto-edge"
        },
        @{
            "Id" = "emea-ru-msa-edge"
        }
    ) 
}

New-AzResourceGroupDeployment `
    -Name 'test-deployment' `
    -TemplateFile 'Deployment/availability-test.json' `
    -ResourceGroupName $settingsJson.ResourceGroupName `
    -appInsightsResource $output.Outputs["application-insights-reference"].value `
    -tests $testConfig

if (![string]::IsNullOrEmpty($settingsJson.AzureAlarmHandlerUrl)) {
    Write-Host 'Creating action group'
    .\Deployment\Set-ActionGroup.ps1 `
        -AlertUrl $settingsJson.AzureAlarmHandlerUrl `
        -ActionGroupResourceGroup $settingsJson.ResourceGroupName `
        -ActionGroupName 'repo-alerts'

    .\Deployment\Add-Alerts.ps1 `
        -ResourceGroup $settingsJson.ResourceGroupName `
        -AlertTargetResourceGroup $settingsJson.ResourceGroupName `
        -AlertTargetGroupName 'repo-alerts'
}
else {
    Write-Host 'No Azure Alarm Webhook specified, creating only alarms'
    .\Deployment\Add-Alerts.ps1 `
        -ResourceGroup $settingsJson.ResourceGroupName
}
