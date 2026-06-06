// Provisions the Claims Module Azure environment: App Service (API), Static Web App (frontend),
// Azure SQL, Blob Storage, Key Vault and Log Analytics/App Insights. azd-compatible (subscription scope).
targetScope = 'subscription'

@minLength(1)
@description('Name of the environment (azd) — used to name the resource group and tag resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Region for the Static Web App (a SWA-supported region).')
param staticWebAppLocation string = 'eastus2'

@description('Azure SQL administrator login.')
param sqlAdministratorLogin string = 'claimsadmin'

@secure()
@description('Azure SQL administrator password (provide via azd/CI secret).')
param sqlAdministratorPassword string

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }
var storageContainerName = 'claim-documents'
var sqlSecretName = 'ClaimsDbConnectionString'
var storageSecretName = 'StorageConnectionString'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module monitoring 'modules/monitoring.bicep' = {
  scope: rg
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: 'log-${resourceToken}'
    appInsightsName: 'appi-${resourceToken}'
  }
}

module keyVault 'modules/keyvault.bicep' = {
  scope: rg
  name: 'keyvault'
  params: {
    location: location
    tags: tags
    name: 'kv-${resourceToken}'
  }
}

module storage 'modules/storage.bicep' = {
  scope: rg
  name: 'storage'
  params: {
    location: location
    tags: tags
    name: 'st${resourceToken}'
    containerName: storageContainerName
    keyVaultName: keyVault.outputs.name
    secretName: storageSecretName
  }
}

module sql 'modules/sql.bicep' = {
  scope: rg
  name: 'sql'
  params: {
    location: location
    tags: tags
    serverName: 'sql-${resourceToken}'
    databaseName: 'claimsdb'
    administratorLogin: sqlAdministratorLogin
    administratorPassword: sqlAdministratorPassword
    keyVaultName: keyVault.outputs.name
    secretName: sqlSecretName
  }
}

module web 'modules/web.bicep' = {
  scope: rg
  name: 'web'
  params: {
    location: staticWebAppLocation
    tags: tags
    name: 'swa-${resourceToken}'
  }
}

module api 'modules/api.bicep' = {
  scope: rg
  name: 'api'
  params: {
    location: location
    tags: tags
    appServicePlanName: 'plan-${resourceToken}'
    appServiceName: 'app-api-${resourceToken}'
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultName: keyVault.outputs.name
    sqlConnectionStringSecretUri: sql.outputs.secretUri
    storageConnectionStringSecretUri: storage.outputs.secretUri
    storageContainerName: storageContainerName
    allowedCorsOrigin: web.outputs.uri
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = subscription().tenantId
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output API_BASE_URL string = api.outputs.uri
output API_APP_SERVICE_NAME string = api.outputs.name
output WEB_BASE_URL string = web.outputs.uri
output WEB_STATIC_WEB_APP_NAME string = web.outputs.name
output SQL_SERVER_FQDN string = sql.outputs.serverFqdn
output SQL_DATABASE_NAME string = sql.outputs.databaseName
