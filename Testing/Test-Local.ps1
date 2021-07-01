<#
    .SYNOPSIS
    Send json to validation endpoint to test validation

    .DESCRIPTION
    This functions can be used the test Azure Functions validation endpoint
    without making changes in GitHub.

    .PARAMETER Organization
    Organization/user containing the repository (default by-pinja)

    .PARAMETER Repository
    Name of the repository to be validated  (default repository-validator)
#>
[CmdLetBinding()]
param(
    [Parameter()][string]$Organization = 'by-pinja',
    [Parameter()][string]$Repository = 'repository-validator'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$hostKeyLocation = 'ValidationLibrary.AzureFunctions/dev_secrets/host.json'
Write-Host "Reading keys from file $hostKeyLocation"
$keys = Get-Content -Raw -Path $hostKeyLocation | ConvertFrom-Json
$key = $keys.masterKey.value

Write-Host $key


$validationAddress = "http://localhost:8080/api/v1/github-endpoint?code=$key"
# This should match the webhook content sent by github, but we are only using
# required properties
$params = @{
    'repository' = @{
        'name'  = $Repository
        'owner' = @{
            'login' = $Organization
        }
    }
} | ConvertTo-Json

Write-Host 'Send validation request'
Invoke-RestMethod -Method POST -Uri $validationAddress -Body $params -ContentType 'application/json;charset=UTF-8'

$statusCheckAddress = "http://localhost:8080/api/v1/status?code=$key"

Write-Host 'Send status check request'
Invoke-RestMethod -Method GET -Uri $statusCheckAddress -ContentType 'application/json;charset=UTF-8'
