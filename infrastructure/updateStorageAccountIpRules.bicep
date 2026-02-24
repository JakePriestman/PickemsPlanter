param appServiceOutboundAddresses array
param existingIpRules array
param storageAccountName string
param sku resourceInput<'Microsoft.Storage/storageAccounts@2025-06-01'>.sku
param kind resourceInput<'Microsoft.Storage/storageAccounts@2025-06-01'>.kind

var existingIps = [ for rule in existingIpRules: rule.value ]

var mergedIps = union(existingIps, appServiceOutboundAddresses)

var newIpRules = [
  for ip in mergedIps: {
    action: 'Allow'
    value: ip
  }
]

resource storageUpdate 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: resourceGroup().location
  sku: sku
  kind: kind
  properties: {
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      ipRules: newIpRules
    }
  }
}