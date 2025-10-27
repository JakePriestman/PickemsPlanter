param storageAccountName string

param location string = resourceGroup().location

param allowedIpAddresses array

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name:  storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    networkAcls: {
      defaultAction: 'Deny'
      ipRules: [
        for ipAddress in allowedIpAddresses : {
          value: ipAddress
          action: 'Allow'
        }
      ]
    }
  }
}

output id string = storageAccount.id
