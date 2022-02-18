param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter(Mandatory)][string]$AppName,
    [Parameter(Mandatory)][string]$GitHubToken,
    [Parameter(Mandatory)][string]$GitHubOrganization,
    [Parameter(Mandatory)][string]$Environment
)

New-AzResourceGroupDeployment `
    -Name 'github-validator' `
    -TemplateFile 'Deployment/azuredeploy.bicep' `
    -ResourceGroupName $ResourceGroup `
    -TemplateParameterObject @{
        appName = $AppName
        gitHubToken = $GitHubToken
        gitHubOrganization = $GitHubOrganization
        environment = $Environment
    }
