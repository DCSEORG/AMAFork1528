@description('Location for Gen AI resources')
param location string = 'swedencentral'

@description('Location for the resource group')
param rgLocation string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Environment suffix')
param environment string = 'dev'

@description('Timestamp for unique naming')
param timestamp string = utcNow('ddHHmm')

@description('Managed Identity ID to grant access')
param managedIdentityId string

@description('Managed Identity Principal ID')
param managedIdentityPrincipalId string

var openAIAccountName = 'aoai-${baseName}-${environment}-${timestamp}'
var openAIModelDeploymentName = 'gpt-4o'
var searchServiceName = 'search-${baseName}-${environment}-${timestamp}'

// Azure OpenAI Service
resource openAIAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: openAIAccountName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIAccountName
    publicNetworkAccess: 'Enabled'
  }
}

// Deploy GPT-4o model
resource openAIDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAIAccount
  name: openAIModelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

// Cognitive Services OpenAI User role definition
var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

// Grant Managed Identity access to OpenAI
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAIAccount.id, managedIdentityId, cognitiveServicesOpenAIUserRoleId)
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Azure AI Search for RAG
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchServiceName
  location: rgLocation
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Search Service Contributor role for Managed Identity
var searchServiceContributorRoleId = '7ca78c08-252a-4471-8644-bb5ff32d4ba0'

resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityId, searchServiceContributorRoleId)
  scope: searchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchServiceContributorRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output openAIEndpoint string = openAIAccount.properties.endpoint
output openAIName string = openAIAccount.name
output openAIModelName string = openAIModelDeploymentName
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'
output searchServiceName string = searchService.name
