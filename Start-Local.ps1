<#
    .SYNOPSIS
    This script builds and start local environment with docker.

    .DESCRIPTION
    This script requires docker. This script also overrides .env-file

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

@"
# Dont change manually. This is generated by Start-Local.ps1

GITHUBTOKEN=$($settingsJson.GitHubToken)
GITHUBORGANIZATION=$($settingsJson.GitHubOrganization)
"@ | Out-File -FilePath '.env' -Encoding 'utf8'

docker-compose build
if ($LASTEXITCODE) {
    throw "Build failed with exit code $LASTEXITCODE"
}

docker-compose up
if ($LASTEXITCODE) {
    throw "Build failed with exit code $LASTEXITCODE"
}
