<#
    .SYNOPSIS
    Creates alert rules for given resource group hosting Repository-Validator.

    .DESCRIPTION
    Creates generic alert rules for services in specified resource group. If no
    action group parameters are specified, alert rules are created without
    action group.

    .PARAMETER ResourceGroup
    Resource group hosting the Repository Validator solution

    .PARAMETER ActionGroupResourceGroupName
    This is the resource group that has the action group for alerts (email etc.)

    .PARAMETER ActionGroupName
    This is the name tof the action group for alerts (email etc.)
#>
param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter()][string]$ActionGroupResourceGroupName,
    [Parameter()][string]$ActionGroupName
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host 'Retrieving resources and creating criterias'
$alertParameters = @(
    [PSCustomObject]@{
        Name        = 'Bad requests'
        Description = 'Too many bad requests received'
        Criteria    = New-AzMetricAlertRuleV2Criteria -MetricName 'Http4xx' -TimeAggregation Total -Operator GreaterThan -Threshold 5
        Resources   = (Get-AzResource -ResourceType 'Microsoft.Web/Sites' -ResourceGroupName $ResourceGroup)
    }
    [PSCustomObject]@{
        Name        = 'Exceptions'
        Description = 'Exceptions'
        Criteria    = New-AzMetricAlertRuleV2Criteria -MetricName 'exceptions/count' -TimeAggregation Count -Operator GreaterThan -Threshold 1
        Resources   = (Get-AzResource -ResourceType 'Microsoft.Insights/components' -ResourceGroupName $ResourceGroup)
    }
)

$alertRef = $null
if ($ActionGroupResourceGroupName -and $ActionGroupName) {
    Write-Host 'Retrieving alert action group...'
    $alertTargetActual = Get-AzActionGroup -ResourceGroupName $ActionGroupResourceGroupName -Name $ActionGroupName
    $alertRef = New-AzActionGroup -ActionGroupId $alertTargetActual.Id
}
else {
    Write-Host 'No action groups specified.'
}

Write-Host 'Creating alerts'
Foreach ($alertParameter in $alertParameters) {
    Write-Host "Creating alert for $($alertParameter.Name)"

    foreach ($resource in $alertParameter.Resources) {
        if ($alertRef) {
            Add-AzMetricAlertRuleV2 `
                -Name $alertParameter.Name `
                -ResourceGroupName $ResourceGroup `
                -WindowSize 0:5 `
                -Frequency 0:5 `
                -TargetResourceId $resource.ResourceId `
                -Description $alertParameter.Description `
                -Severity 4 `
                -ActionGroup $alertRef `
                -Condition $alertParameter.Criteria
        }
        else {
            Add-AzMetricAlertRuleV2 `
                -Name $alertParameter.Name `
                -ResourceGroupName $ResourceGroup `
                -WindowSize 0:5 `
                -Frequency 0:5 `
                -TargetResourceId $resource.ResourceId `
                -Description $alertParameter.Description `
                -Severity 4 `
                -Condition $alertParameter.Criteria
        }
    }
}
Write-Host 'Alerts created'
