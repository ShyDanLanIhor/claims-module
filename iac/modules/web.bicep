// Azure Static Web App hosting the Angular frontend.
param location string
param tags object
param name string

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // Deployment is driven by the CI pipeline (SWA deploy action / azd), not a linked Git repo.
    allowConfigFileUpdates: true
  }
}

output name string = staticWebApp.name
output uri string = 'https://${staticWebApp.properties.defaultHostname}'
