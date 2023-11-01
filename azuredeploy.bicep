param acrResourceGroupName string
param acrName string

param location string = resourceGroup().location

@description('Name of the delivery managed identity')
param deliveryIdName string

@description('Name of the drone scheduler managed identity')
param droneSchedulerIdName string

@description('Name of the workflow managed identity')
param workflowIdName string

@description('Name of the ingestion managed identity')
param ingestionIdName string

@description('Name of the package managed identity')
param packageIdName string

@description('Configure all linux machines with the SSH RSA public key string.  Your key should include three parts, for example \'ssh-rsa AAAAB...snip...UcyupgH azureuser@linuxvm\'')
param sshRSAPublicKey string

@description('Client ID (used by cloudprovider)')
param servicePrincipalClientId string

@description('The Service Principal Client Secret.')
@secure()
param servicePrincipalClientSecret string

@description('The type of operating system.')
@allowed([
  'Linux'
])
param osType string = 'Linux'

@description('Disk size (in GB) to provision for each of the agent pool nodes. This value ranges from 0 to 1023. Specifying 0 will apply the default disk size for that agentVMSize.')
@minValue(0)
@maxValue(1023)
param osDiskSizeGB int = 0

@description('User name for the Linux Virtual Machines.')
param adminUsername string = 'azureuser'

@description('The version of Kubernetes. It must be supported in the target location.')
param kubernetesVersion string

@description('Type of the storage account that will store Redis Cache.')
@allowed([
  'Standard_LRS'
  'Standard_ZRS'
  'Standard_GRS'
])
param deliveryRedisStorageType string = 'Standard_LRS'

var clusterNamePrefix = 'aks'
var managedIdentityOperatorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f1a07417-d97a-45cb-824c-7a7467783830')
var deliveryRedisStorageName = 'rsto${uniqueString(resourceGroup().id)}'
var nestedACRDeploymentName = 'azuredeploy-acr-${acrResourceGroupName}'
var aksLogAnalyticsNamePrefix = 'logsAnalytics'
var monitoringMetricsPublisherRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '3913510d-42f4-4e42-8a64-420c390055eb')
var nodeResourceGroupName = 'rg-${aksClusterName}-nodepools'
var aksClusterName = uniqueString(clusterNamePrefix, resourceGroup().id)
var agentCount = 2
var agentVMSize = 'Standard_D2_v2'
var workspaceName = 'la-${uniqueString(aksLogAnalyticsNamePrefix, resourceGroup().id)}'
var workspaceSku = 'pergb2018'
var workspaceRetentionInDays = 0

module nestedACRDeployment './azuredeploy_nested_nestedACRDeployment.bicep' = {
  name: nestedACRDeploymentName
  scope: resourceGroup(acrResourceGroupName)
  params: {
    clusterIdentity: aksCluster.properties.identityProfile.kubeletidentity.objectId
    acrName: acrName
  }
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: workspaceName
  location: location
  properties: {
    retentionInDays: workspaceRetentionInDays
    sku: {
      name: workspaceSku
    }
    features: {
      searchVersion: 1
    }
  }
}

resource aksCluster 'Microsoft.ContainerService/managedClusters@2023-07-02-preview' = {
  name: aksClusterName
  location: location
  tags: {
    environment: 'shared cluster'
  }
  properties: {
    kubernetesVersion: kubernetesVersion
    nodeResourceGroup: nodeResourceGroupName
    dnsPrefix: aksClusterName
    agentPoolProfiles: [
      {
        name: 'agentpool'
        osDiskSizeGB: osDiskSizeGB
        count: agentCount
        vmSize: agentVMSize
        osType: osType
        mode: 'System'
      }
    ]
    linuxProfile: {
      adminUsername: adminUsername
      ssh: {
        publicKeys: [
          {
            keyData: sshRSAPublicKey
          }
        ]
      }
    }
    servicePrincipalProfile: {
      clientId: servicePrincipalClientId
      secret: servicePrincipalClientSecret
    }
    addonProfiles: {
      omsagent: {
        config: {
          logAnalyticsWorkspaceResourceID: workspace.id
        }
        enabled: true
      }
      azureKeyvaultSecretsProvider: {
        enabled: true
        config: {
          enableSecretRotation: 'false'
        }
      }
    }
    oidcIssuerProfile: {
      enabled: true
    }
    podIdentityProfile: {
      enabled: false
    }
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

resource deliveryRedisStorage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: deliveryRedisStorageName
  sku: {
    name: deliveryRedisStorageType
  }
  kind: 'Storage'
  location: location
  tags: {
    displayName: 'Storage account for inflight deliveries'
    app: 'fabrikam-delivery'
  }
}

resource aksClusterName_Microsoft_Authorization_id_monitoringMetricsPublisherRole 'Microsoft.Authorization/roleAssignments@2022-04-01'  = {
  name: guid(concat(resourceGroup().id), monitoringMetricsPublisherRole)
  scope: aksCluster
  properties: {
    roleDefinitionId: monitoringMetricsPublisherRole
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

resource deliveryId 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: deliveryIdName
}

resource deliveryIdName_Microsoft_Authorization_msi_delivery_id 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('msi-delivery', resourceGroup().id)
  scope: deliveryId
  properties: {
    roleDefinitionId: managedIdentityOperatorRoleId
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

resource workflowId 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: workflowIdName
}

resource workflowIdName_Microsoft_Authorization_msi_workflow_id 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('msi-workflow', resourceGroup().id)
  scope: workflowId
  properties: {
    roleDefinitionId: managedIdentityOperatorRoleId
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

resource droneSchedulerId 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: droneSchedulerIdName
}

resource droneSchedulerIdName_Microsoft_Authorization_msi_dronescheduler_id 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('msi-dronescheduler', resourceGroup().id)
  scope: droneSchedulerId
  properties: {
    roleDefinitionId: managedIdentityOperatorRoleId
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

resource ingestionId 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: ingestionIdName
}

resource ingestionIdName_Microsoft_Authorization_msi_ingestion_id 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('msi-ingestion', resourceGroup().id)
  scope: ingestionId
  properties: {
    roleDefinitionId: managedIdentityOperatorRoleId
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

resource packageId 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: packageIdName
}

resource packageIdName_Microsoft_Authorization_msi_package_id 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('msi-package', resourceGroup().id)
  scope: packageId
  properties: {
    roleDefinitionId: managedIdentityOperatorRoleId
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

module EnsureClusterUserAssignedHasRbacToManageVMSS './azuredeploy_nested_EnsureClusterUserAssignedHasRbacToManageVMSS.bicep' = {
  name: 'EnsureClusterUserAssignedHasRbacToManageVMSS'
  scope: resourceGroup(nodeResourceGroupName)
  params: {
    clusterIdentity: aksCluster.properties.identityProfile.kubeletidentity.objectId
  }
}

output aksClusterName string = aksClusterName
output acrDeploymentName string = nestedACRDeploymentName
output deliveryPrincipalResourceId string = deliveryId.id
output workflowPrincipalResourceId string = workflowId.id
output ingestionPrincipalResourceId string = ingestionId.id
output packagePrincipalResourceId string = packageId.id
output droneSchedulerPrincipalResourceId string = droneSchedulerId.id
