param names namingType
param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: names.keyVault
  location: location
  properties: {
    sku: {
      name: 'standard'
      family: 'A'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
  }
}
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: names.appServicePlan
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
    size: 'F1'
    capacity: 1
  }
  properties: {
    reserved: false
    perSiteScaling: false
    maximumElasticWorkerCount: 1
  }
}

resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: names.appService
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  location: location
  properties: {
    httpsOnly: true
    serverFarmId: appServicePlan.id
    siteConfig: {
      use32BitWorkerProcess: true
      netFrameworkVersion: 'v8.0'
    }
    keyVaultReferenceIdentity: 'SystemAssigned'
  }
}

var appServiceOutboundAddresses = split(appService.properties.outboundIpAddresses, ',')

module storageAccount 'storageAccount.bicep' = {
  params: {
    allowedIpAddresses: appServiceOutboundAddresses
    storageAccountName: names.storageAccount
  }
}

resource keyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, resourceGroup().id)
  properties: {
    principalId: appService.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource storageAccountRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(names.storageAccount, resourceGroup().id)
  properties: {
    principalId: appService.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
  }
}

type  namingType = {
  keyVault: string
  storageAccount: string
  appServicePlan: string
  appService: string
}
