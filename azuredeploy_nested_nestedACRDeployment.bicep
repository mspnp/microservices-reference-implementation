param clusterIdentity string
param acrName string

var acrPullRole =  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') 

resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: acrName
}

resource variables_acrName_Microsoft_Authorization_id_variables_acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01'  = {
  name: guid(concat(resourceGroup().id), acrPullRole)
  scope: acr
  properties: {
    roleDefinitionId: acrPullRole
    principalId: clusterIdentity
    principalType: 'ServicePrincipal'
  }
  dependsOn: []
}
