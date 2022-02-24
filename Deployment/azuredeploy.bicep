@description('The name of the function app that you wish to create.')
param appName string

@description('Location for all resources.')
param location string = resourceGroup().location

@description('GitHub token that is used to access repositories')
@secure()
param gitHubToken string

@description('GitHub organization that is to be validated')
param gitHubOrganization string

@allowed([
  'Development'
  'Production'
])
@description('Environment type (Development, Production)')
param environment string

var functionAppName_var = appName
var hostingPlanName_var = appName
var applicationInsightsName_var = appName
var storageAccountName_var = '${replace(appName, '-', '')}func'
var storageAccountid = '${resourceGroup().id}/providers/Microsoft.Storage/storageAccounts/${storageAccountName_var}'

resource storageAccountName 'Microsoft.Storage/storageAccounts@2018-07-01' = {
  name: storageAccountName_var
  location: location
  kind: 'Storage'
  sku: {
    name: 'Standard_LRS'
  }
  tags: {
    displayName: 'Storage for function app'
    environment: environment
  }
}

resource hostingPlanName 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: hostingPlanName_var
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  tags: {
    displayName: 'Server for function app'
    environment: environment
  }
}

resource functionAppName 'Microsoft.Web/sites@2018-02-01' = {
  kind: 'functionapp'
  name: functionAppName_var
  location: location
  tags: {
    displayName: 'Function app'
    environment: environment
  }
  properties: {
    serverFarmId: hostingPlanName.id
    siteConfig: {
      defaultDocuments: []
      phpVersion: ''
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName_var};AccountKey=${listKeys(storageAccountid, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName_var};AccountKey=${listKeys(storageAccountid, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName_var)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: reference(applicationInsights.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'GitHub__Token'
          value: gitHubToken
        }
        {
          name: 'GitHub__Organization'
          value: gitHubOrganization
        }
        {
          name: 'Rules__HasCodeownersRule'
          value: 'disable'
        }
      ]
    }
  }
  dependsOn: [
    storageAccountName
  ]
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: resourceGroup().name
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName_var
  location: location
  kind: 'web'
  tags: {
    'hidden-link:${resourceGroup().id}/providers/Microsoft.Web/sites/${applicationInsightsName_var}': 'Resource'
    displayName: 'Application insights'
    environment: environment
  }
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}
