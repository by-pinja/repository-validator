<#
    .SYNOPSIS
    This will call status page

    .PARAMETER SettinsFile
    Settings file that contains environment settings.
    Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json
 
$address = ./Deployment/Get-FunctionUri.ps1 `
    -ResourceGroup $settingsJson.ResourceGroupName `
    -WebAppName "$($settingsJson.ResourceGroupName)-monitor" `
    -FunctionName 'GetAlertsAndRules'

Write-Host 'Send status check request'
Write-Host $address
Invoke-RestMethod -Method GET -Uri $address -ContentType 'application/json;charset=UTF-8'
