@description('Name prefix for all resources')
param appName string

@description('Azure region')
param location string = resourceGroup().location

@description('App Service plan SKU')
@allowed(['B1', 'B2', 'B3', 'S1', 'S2', 'P1v3'])
param sku string = 'B1'

var planName = '${appName}-plan'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: {
    name: sku
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: sku != 'B1'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ASPNETCORE_URLS'
          value: 'http://+:8080'
        }
      ]
    }
  }
}

output appUrl string = 'https://${webApp.properties.defaultHostName}'
output appName string = webApp.name
