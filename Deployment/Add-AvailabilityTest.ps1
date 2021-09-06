<#
    .SYNOPSIS
    Adds availability test
#>
param(
    [Parameter(Mandatory)][string]$ResourceGroupName
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$applicationInsightsId = (Get-AzResource -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Insights/components).ResourceId
$statusCheckUrl = .\Deployment\Get-FunctionUri.ps1 `
    -FunctionName 'StatusCheck' `
    -ResourceGroup $ResourceGroupName

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
    -ResourceGroupName $ResourceGroupName `
    -appInsightsResource $applicationInsightsId `
    -tests $testConfig
