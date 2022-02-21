param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter(Mandatory)][string]$AppName,
    [Parameter(Mandatory)][string]$GitHubToken,
    [Parameter(Mandatory)][string]$GitHubOrganization,
    [Parameter(Mandatory)][string]$Environment
)
$ErrorActionPreference = "Stop"

$parameters = @{
    appName            = $AppName
    gitHubToken        = $GitHubToken
    gitHubOrganization = $GitHubOrganization
    environment        = $Environment
}

bicep build 'Deployment/azuredeploy.bicep'
New-AzResourceGroupDeployment -Name 'github-validator' -TemplateFile 'Deployment/azuredeploy.json' -ResourceGroupName $ResourceGroup -TemplateParameterObject $parameters -Verbose
