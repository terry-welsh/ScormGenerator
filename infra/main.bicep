@description('Name prefix for all resources')
param appName string

@description('Azure region')
param location string = resourceGroup().location

@description('Container image to deploy')
param containerImage string = 'mcr.microsoft.com/dotnet/aspnet:10.0'

@description('GitHub username (repository owner) for pulling from ghcr.io')
param registryUsername string

@description('GitHub PAT with read:packages scope for pulling from ghcr.io')
@secure()
param registryPassword string

var envName = '${appName}-env'

// Consumption plan Container Apps Environment — free tier: 180,000 vCPU-seconds/month
// Scale-to-zero means no charges when idle.
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {}
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      secrets: [
        {
          name: 'ghcr-password'
          value: registryPassword
        }
      ]
      registries: [
        {
          server: 'ghcr.io'
          username: registryUsername
          passwordSecretRef: 'ghcr-password'
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        transport: 'auto' // supports WebSockets (required for Blazor Server / SignalR)
      }
    }
    template: {
      containers: [
        {
          name: appName
          image: containerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
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
      ]
      scale: {
        minReplicas: 0 // scale to zero when no traffic
        maxReplicas: 1
      }
    }
  }
}

output appUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output appName string = containerApp.name
