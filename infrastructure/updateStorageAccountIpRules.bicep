param appServiceOutboundAddresses array
param existingIpRules array
param storageAccountName string
param sku resourceInput<'Microsoft.Storage/storageAccounts@2025-06-01'>.sku
param kind resourceInput<'Microsoft.Storage/storageAccounts@2025-06-01'>.kind

var newIpRules = [
  for ip in appServiceOutboundAddresses: {
    action: 'Allow'
    value: ip
  }
]

var mergedIpRules = union(existingIpRules, newIpRules)

resource storageUpdate 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: resourceGroup().location
  sku: sku
  kind: kind
  properties: {
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      ipRules: mergedIpRules
    }
  }
}