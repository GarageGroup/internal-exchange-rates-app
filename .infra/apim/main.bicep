targetScope = 'resourceGroup'

param apimServiceName string
param apiId string
param apiPath string
param productId string
param backendServiceUrl string
@secure()
param functionKey string

resource apim 'Microsoft.ApiManagement/service@2024-05-01' existing = {
  name: apimServiceName
}

resource api 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: apiId
  properties: {
    displayName: 'Exchange Rates'
    description: 'Current and historical exchange rates.'
    path: apiPath
    protocols: ['https']
    serviceUrl: backendServiceUrl
    subscriptionRequired: true
  }
}

resource key 'Microsoft.ApiManagement/service/namedValues@2024-05-01' = {
  parent: apim
  name: '${apiId}-function-key'
  properties: {
    displayName: '${apiId}-function-key'
    secret: true
    value: functionKey
  }
}

resource policy 'Microsoft.ApiManagement/service/apis/policies@2024-05-01' = {
  parent: api
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><set-backend-service base-url="${backendServiceUrl}" /><set-header name="x-functions-key" exists-action="override"><value>{{${key.name}}}</value></set-header><set-query-parameter name="code" exists-action="delete" /><set-header name="Ocp-Apim-Subscription-Key" exists-action="delete" /><set-query-parameter name="subscription-key" exists-action="delete" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
}

resource product 'Microsoft.ApiManagement/service/products@2024-05-01' = {
  parent: apim
  name: productId
  properties: {
    displayName: 'Garage Exchange Rates'
    description: 'Current and historical exchange rates.'
    subscriptionRequired: true
    approvalRequired: true
    state: 'published'
  }
}

resource productApi 'Microsoft.ApiManagement/service/products/apis@2024-05-01' = {
  parent: product
  name: apiId
}

resource developers 'Microsoft.ApiManagement/service/products/groups@2024-05-01' = {
  parent: product
  name: 'developers'
}

output apiUrlPath string = apiPath
