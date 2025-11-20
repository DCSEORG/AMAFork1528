targetScope = 'subscription'

@description('Resource group name')
param resourceGroupName string = 'rg-expensemgmt-dev'

@description('Location for all resources')
param location string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Environment suffix')
param environment string = 'dev'

@description('Deploy Gen AI resources')
param deployGenAI bool = false

@description('Timestamp for unique naming')
param timestamp string = utcNow('ddHHmm')

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

// App Service and Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  scope: rg
  params: {
    location: location
    baseName: baseName
    environment: environment
    timestamp: timestamp
  }
}

// Gen AI Resources (conditional)
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  scope: rg
  params: {
    location: 'swedencentral'
    rgLocation: location
    baseName: baseName
    environment: environment
    timestamp: timestamp
    managedIdentityId: appService.outputs.managedIdentityId
    managedIdentityPrincipalId: reference(appService.outputs.managedIdentityId, '2023-01-31', 'Full').properties.principalId
  }
  dependsOn: [
    appService
  ]
}

output resourceGroupName string = rg.name
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityName string = appService.outputs.managedIdentityName

output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output searchServiceName string = deployGenAI ? genai.outputs.searchServiceName : ''
