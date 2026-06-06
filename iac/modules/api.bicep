// Linux App Service hosting the .NET 9 API. Uses a system-assigned managed identity to read
// connection strings from Key Vault (via @Microsoft.KeyVault references) — no secrets in config.
param location string
param tags object
param appServicePlanName string
param appServiceName string
param appInsightsConnectionString string
param keyVaultName string
param sqlConnectionStringSecretUri string
param storageConnectionStringSecretUri string
param storageContainerName string
param allowedCorsOrigin string

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource api 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'Storage__Provider', value: 'AzureBlob' }
        { name: 'Storage__ContainerName', value: storageContainerName }
        { name: 'Hangfire__EnableServer', value: 'true' }
        { name: 'Database__ApplyMigrationsAtStartup', value: 'false' }
        { name: 'Cors__AllowedOrigins__0', value: allowedCorsOrigin }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'ConnectionStrings__ClaimsDb', value: '@Microsoft.KeyVault(SecretUri=${sqlConnectionStringSecretUri})' }
        { name: 'Storage__ConnectionString', value: '@Microsoft.KeyVault(SecretUri=${storageConnectionStringSecretUri})' }
      ]
    }
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Key Vault Secrets User → lets the App Service identity resolve the @Microsoft.KeyVault references.
resource kvSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, api.id, 'key-vault-secrets-user')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: api.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output name string = api.name
output uri string = 'https://${api.properties.defaultHostName}'
output principalId string = api.identity.principalId
