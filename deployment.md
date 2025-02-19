# Deploying the Reference Implementation

## Prerequisites

- Azure subscription
  > Important: The user initiating the deployment process must have access to the **Microsoft.Authorization/roleAssignments/write** permission. For more information, see [the Container Insights doc](https://docs.microsoft.com/azure/azure-monitor/insights/container-insights-troubleshoot#authorization-error-during-onboarding-or-update-operation)
- [Azure CLI 2.53.1 or newer](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [JQ](https://stedolan.github.io/jq/download/)
- Kubectl 
(az aks install-cli)
- Helm
(curl https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3 | bash)

## Clone or download this repo locally.

```bash
git clone --recurse-submodules https://github.com/mspnp/microservices-reference-implementation.git && \
cd microservices-reference-implementation/
```

The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

Set environment variables.

```bash
export LOCATION=eastus2
```

### Log in to Azure CLI

```bash
az login
```

### Deploy the workload's prerequisites

```bash
az deployment sub create --name workload-stamp-prereqs --location ${LOCATION} --template-file ./workload/workload-stamp-prereqs.bicep --parameters resourceGroupLocation=${LOCATION}
```

:book: This pre-flight Bicep file creates two resource groups. Additionally five User identities are provisioned that will be later associated to every containerized microservice. This is because they will need Azure RBAC roles over the Azure Key Vault to read secrets at runtime. The resources will be created in the resource group's location, and each resource group will contain the region name as part of its name.

### Get the workload user assigned identities

```bash
DELIVERY_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-delivery --query principalId -o tsv) && \
DRONESCHEDULER_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-dronescheduler --query principalId -o tsv) && \
WORKFLOW_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-workflow --query principalId -o tsv) && \
PACKAGE_ID_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-package --query principalId -o tsv) && \
INGESTION_ID_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-ingestion --query principalId -o tsv)
```

### Deploy the workload related resources

```bash
az deployment group create -f ./workload/workload-stamp.bicep -g rg-shipping-dronedelivery-${LOCATION} -p droneSchedulerPrincipalId=$DRONESCHEDULER_PRINCIPAL_ID -p workflowPrincipalId=$WORKFLOW_PRINCIPAL_ID -p deliveryPrincipalId=$DELIVERY_PRINCIPAL_ID -p ingestionPrincipalId=$INGESTION_ID_PRINCIPAL_ID -p packagePrincipalId=$PACKAGE_ID_PRINCIPAL_ID
```

### Assign Azure Container Registry variables

```bash
ACR_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.acrName.value -o tsv)
ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv)
```

## Build the microservice images

### Steps

1. Build and push the Delivery service container image to the container registry.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/delivery:0.1.0 ./workload/src/shipping/delivery/.
```

2. Build and push the Ingestion service container image to the container registry.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/ingestion:0.1.0 ./workload/src/shipping/ingestion/.
```

3. Build and push the Workflow service container image to the container registry.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/workflow:0.1.0 ./workload/src/shipping/workflow/.
```

4. Build and push the DroneScheduler service container image to the container registry.

```bash
az acr build -r $ACR_NAME -f ./workload/src/shipping/dronescheduler/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 ./workload/src/shipping/.
```

5. Build and push the Package service container image to the container registry.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/package:0.1.0 ./workload/src/shipping/package/.
```

## Deploy the managed cluster and related resources

```bash
export LOG_ANALYTICS_WORKSPACE_ID=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.laWorkspace.value -o tsv)

az deployment group create -g rg-shipping-dronedelivery-${LOCATION} --name managed-cluster-deployment  --template-file azuredeploy.bicep \
--parameters deliveryIdName=uid-delivery \
            ingestionIdName=uid-ingestion \
            packageIdName=uid-package \
            droneSchedulerIdName=uid-dronescheduler \
            workflowIdName=uid-workflow \
            acrResourceGroupName=rg-shipping-dronedelivery-${LOCATION}-acr \
            acrName=$ACR_NAME \
            logAnalyticsWorkspaceID=$LOG_ANALYTICS_WORKSPACE_ID
```

### Get the cluster name output from the Azure deployment

```bash
export CLUSTER_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n managed-cluster-deployment --query properties.outputs.aksClusterName.value -o tsv)
```

### Get the AKS cluster credentials and create a Kubernetes namespace

```bash

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=rg-shipping-dronedelivery-${LOCATION} --name=$CLUSTER_NAME

# Create namespaces
kubectl create namespace backend-dev
```

### Configure RBAC permissions for Azure Application Insights

```bash

# add RBAC for AppInsights
kubectl apply -f k8s/k8s-rbac-ai.yaml
```

### Set resource quotas for the namespace

```bash
kubectl apply -f k8s/k8s-resource-quotas-dev.yaml
```

### Get the cluster OpenID Connect issuer URL, and the tenant ID and object ID of the signed-in user

```bash
export AKS_OIDC_ISSUER="$(az aks show -n $CLUSTER_NAME -g rg-shipping-dronedelivery-${LOCATION} --query "oidcIssuerProfile.issuerUrl" -otsv)"
export TENANT_ID=$(az account show --query tenantId --output tsv)
export SIGNED_IN_OBJECT_ID=$(az ad signed-in-user show --query 'id' -o tsv)
```

### Create managed identity federations for microservices
```
# Setup managed identity for delivery microservice to trust your Kubernetes service account
az identity federated-credential create --name credential-for-delivery --identity-name uid-delivery --resource-group rg-shipping-dronedelivery-${LOCATION} --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:delivery-sa-v0.1.0

# Setup managed identity for package microservice to trust your Kubernetes service account
az identity federated-credential create --name credential-for-package --identity-name uid-package --resource-group rg-shipping-dronedelivery-${LOCATION} --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:package-sa-v0.1.0

# Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-workflow --identity-name uid-workflow --resource-group rg-shipping-dronedelivery-${LOCATION} --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:workflow-sa-v0.1.0

# Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-ingestion --identity-name uid-ingestion --resource-group rg-shipping-dronedelivery-${LOCATION} --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:ingestion-sa-v0.1.0

# Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-dronescheduler --identity-name uid-dronescheduler --resource-group rg-shipping-dronedelivery-${LOCATION} --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:dronescheduler-sa-v0.1.0

```

### Collect details of the managed ingress controller

```bash

# Obtain the load balancer ip address of managed ingress and assign a domain name
export INGRESS_LOAD_BALANCER_IP=$(kubectl get service -n app-routing-system nginx -o jsonpath="{.status.loadBalancer.ingress[0].ip}" 2> /dev/null) 
export INGRESS_LOAD_BALANCER_IP_ID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$INGRESS_LOAD_BALANCER_IP')].[id]" --output tsv) && \
export EXTERNAL_INGEST_DNS_NAME="dronedelivery-${LOCATION}-${RANDOM}-ing" && \
export EXTERNAL_INGEST_FQDN=$(az network public-ip update --ids $INGRESS_LOAD_BALANCER_IP_ID --dns-name $EXTERNAL_INGEST_DNS_NAME --query "dnsSettings.fqdn" --output tsv)

```

### Create self-signed certificate for TLS

> :warning: WARNING
>
> Do not use the certificates created by these scripts for production. The
> certificates are provided for demonstration purposes only.
> For your production cluster, use your
> security best practices for digital certificates creation and lifetime management.

```bash

# Create a self-signed certificate for TLS
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -out ingestion-ingress-tls.crt \
    -keyout ingestion-ingress-tls.key \
    -subj "/CN=${EXTERNAL_INGEST_FQDN}/O=fabrikam"
```

## Deploy the Delivery service

Extract resource details from deployment.

```bash
export COSMOSDB_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.deliveryCosmosDbName.value -o tsv) && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col" && \
export DELIVERY_KEYVAULT_URI=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.deliveryKeyVaultUri.value -o tsv) && \
export DELIVERY_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.deliveryKeyVaultName.value -o tsv) && \
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-delivery --query clientId -o tsv)
export DELIVERY_KEYVAULT_ID=$(az resource show -g rg-shipping-dronedelivery-${LOCATION}  -n $DELIVERY_KEYVAULT_NAME --resource-type 'Microsoft.KeyVault/vaults' --query id --output tsv)

# Create secrets
# The current user is given permission to import secrets and then it is deleted right after the secret creation command is executed

az role assignment create --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $DELIVERY_KEYVAULT_ID

# Wait for the role assignment to finish propagating.
sleep 30
az keyvault secret set --name Delivery-Ingress-Tls-Key --vault-name $DELIVERY_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.key)"
az keyvault secret set --name Delivery-Ingress-Tls-Crt --vault-name $DELIVERY_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.crt)"
az role assignment delete --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $DELIVERY_KEYVAULT_ID

# Deploy the service
helm package charts/delivery/ -u && \
helm install delivery-v0.1.0-dev delivery-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=delivery \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=delivery \
     --set ingress.hosts[0].tls=true \
     --set ingress.hosts[0].tlsSecretName=delivery-ingress-tls \
     --set identity.clientid=$DELIVERY_PRINCIPAL_CLIENT_ID \
     --set identity.serviceAccountName=delivery-sa-v0.1.0 \
     --set identity.tenantId=$TENANT_ID \
     --set keyVaultName=$DELIVERY_KEYVAULT_NAME \
     --set ingress.tls=true \
     --set ingress.class=webapprouting.kubernetes.azure.com \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set keyvault.uri=$DELIVERY_KEYVAULT_URI \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --dependency-update

# Verify the Helm deployment status. 
helm status delivery-v0.1.0-dev --namespace backend-dev
```

## Deploy the Package service

Extract resource details from deployment.

```bash
export PACKAGE_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.packageKeyVaultName.value -o tsv)
export PACKAGE_ID_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-package --query clientId -o tsv)
export COSMOSDB_COL_NAME_PACKAGE=packages

# Deploy service
helm package charts/package/ -u && \
helm install package-v0.1.0-dev package-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=package \
     --set identity.clientid=$PACKAGE_ID_CLIENT_ID \
     --set identity.serviceAccountName=package-sa-v0.1.0 \
     --set identity.tenantId=$TENANT_ID \
     --set keyVaultName=$PACKAGE_KEYVAULT_NAME \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=package \
     --set ingress.hosts[0].tls=false \
     --set ingress.class=webapprouting.kubernetes.azure.com \
     --set cosmosDb.collectionName=$COSMOSDB_COL_NAME_PACKAGE \
     --set dockerregistry=$ACR_SERVER \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --dependency-update

# Verify the pod is created
helm status package-v0.1.0-dev --namespace backend-dev
```

## Deploy the Workflow service

Extract resource details from deployment.

```bash
export WORKFLOW_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.workflowKeyVaultName.value -o tsv)
export WORKFLOW_ID_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION}  -n uid-workflow --query clientId -o tsv)
export WORKFLOW_QUEUE_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.ingestionQueueName.value -o tsv)
export WORKFLOW_NAMESPACE_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.ingestionQueueNamespace.value -o tsv)
export WORKFLOW_NAMESPACE_ENDPOINT=$(az servicebus namespace show -g rg-shipping-dronedelivery-${LOCATION} -n $WORKFLOW_NAMESPACE_NAME --query serviceBusEndpoint -o tsv)
export WORKFLOW_NAMESPACE_SAS_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.workflowServiceAccessKeyName.value -o tsv)

# Deploy the service
helm package charts/workflow/ -u && \
helm install workflow-v0.1.0-dev workflow-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=workflow \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$WORKFLOW_ID_CLIENT_ID \
     --set identity.serviceAccountName=workflow-sa-v0.1.0 \
     --set identity.tenantId=$TENANT_ID \
     --set secrets.queue.name=${WORKFLOW_QUEUE_NAME} \
     --set secrets.queue.endpoint=${WORKFLOW_NAMESPACE_ENDPOINT} \
     --set secrets.queue.policyname=${WORKFLOW_NAMESPACE_SAS_NAME} \
     --set keyvault.name=$WORKFLOW_KEYVAULT_NAME \
     --set keyvault.resourcegroup=rg-shipping-dronedelivery-${LOCATION} \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --set serviceuri.delivery="http://delivery-v010/api/Deliveries/" \
     --set serviceuri.drone="http://dronescheduler-v010/api/DroneDeliveries/" \
     --set serviceuri.package="http://package-v010/api/packages/" \
     --namespace backend-dev \
     --dependency-update

# Verify the pod is created
helm status workflow-v0.1.0-dev --namespace backend-dev
```

## Deploy the Ingestion service

Extract resource details and pod identity outputs from deployment.

```bash
export INGESTION_QUEUE_NAMESPACE=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.ingestionQueueNamespace.value -o tsv) && \
export INGESTION_QUEUE_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.ingestionKeyVaultName.value -o tsv)
export INGESTION_ID_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-ingestion --query clientId -o tsv)
export INGESTION_KEYVAULT_ID=$(az resource show -g rg-shipping-dronedelivery-${LOCATION}  -n $INGESTION_KEYVAULT_NAME --resource-type 'Microsoft.KeyVault/vaults' --query id --output tsv)

# The current user is given permission to import secrets and then it is deleted right after the secret creation command is executed
az role assignment create --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $INGESTION_KEYVAULT_ID

# Wait for the role assignment to finish propagating.
sleep 30
az keyvault secret set --name Ingestion-Ingress-Tls-Key --vault-name $INGESTION_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.key)"
az keyvault secret set --name Ingestion-Ingress-Tls-Crt --vault-name $INGESTION_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.crt)"

az role assignment delete --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $INGESTION_KEYVAULT_ID

# Deploy service
helm package charts/ingestion/ -u && \
helm install ingestion-v0.1.0-dev ingestion-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=ingestion \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$INGESTION_ID_CLIENT_ID \
     --set identity.serviceAccountName=ingestion-sa-v0.1.0 \
     --set identity.tenantId=$TENANT_ID \
     --set keyVaultName=$INGESTION_KEYVAULT_NAME \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=ingestion \
     --set ingress.hosts[0].tls=true \
     --set ingress.hosts[0].tlsSecretName=ingestion-ingress-tls \
     --set ingress.tls=true \
     --set ingress.class=webapprouting.kubernetes.azure.com \
     --set secrets.queue.keyname=IngestionServiceAccessKey \
     --set secrets.queue.name=${INGESTION_QUEUE_NAME} \
     --set secrets.queue.namespace=${INGESTION_QUEUE_NAMESPACE} \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --dependency-update

# Verify the pod is created
helm status ingestion-v0.1.0-dev --namespace backend-dev
```

## Deploy DroneScheduler service

Extract resource details from deployment.

```bash
export DRONESCHEDULER_KEYVAULT_URI=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
export DRONESCHEDULER_COSMOSDB_NAME=$(az deployment group show -g rg-shipping-dronedelivery-${LOCATION} -n workload-stamp --query properties.outputs.droneSchedulerCosmosDbName.value -o tsv) && \
export ENDPOINT_URL=$(az cosmosdb show -n $DRONESCHEDULER_COSMOSDB_NAME -g rg-shipping-dronedelivery-${LOCATION} --query documentEndpoint -o tsv) && \
export AUTH_KEY=$(az cosmosdb keys list -n $DRONESCHEDULER_COSMOSDB_NAME -g rg-shipping-dronedelivery-${LOCATION} --query primaryMasterKey -o tsv) && \
export DRONESCHEDULER_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery-${LOCATION} -n uid-dronescheduler --query clientId -o tsv)  && \
export DATABASE_NAME="invoicing" && \
export COLLECTION_NAME="utilization"

# Deploy the service
helm package charts/dronescheduler/ -u && \
helm install dronescheduler-v0.1.0-dev dronescheduler-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=dronescheduler \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=dronescheduler \
     --set ingress.hosts[0].tls=false \
     --set ingress.class=webapprouting.kubernetes.azure.com \
     --set identity.clientid=$DRONESCHEDULER_CLIENT_ID \
     --set identity.serviceAccountName=dronescheduler-sa-v0.1.0 \
     --set keyvault.uri=$DRONESCHEDULER_KEYVAULT_URI \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set cosmosdb.endpoint=$ENDPOINT_URL \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --dependency-update

# Verify the pod is created
helm status dronescheduler-v0.1.0-dev --namespace backend-dev
```

## Verify that all the microservice pods are ready and are in the running state.
```
kubectl get pods -n backend-dev --watch
```

## Validate the application is running

You can send delivery requests and check their statuses using curl.

### Send a request

Since the certificate used for TLS is self-signed, the request disables TLS validation using the '-k' option.

```bash
curl -v -k -X POST "https://$EXTERNAL_INGEST_FQDN/v0.1.0/api/deliveryrequests" --header 'Content-Type: application/json' --header 'Accept: application/json' -d '{
   "confirmationRequired": "None",
   "deadline": "",
   "dropOffLocation": "drop off",
   "expedited": true,
   "ownerId": "myowner",
   "packageInfo": {
     "packageId": "mypackage",
     "size": "Small",
     "tag": "mytag",
     "weight": 10
   },
   "pickupLocation": "my pickup",
   "pickupTime": "2019-05-08T20:00:00.000Z"
 }' > deliveryresponse.json
```

### Check the request status

```bash
DELIVERY_ID=$(cat deliveryresponse.json | jq -r .deliveryId)

curl -v -k "https://$EXTERNAL_INGEST_FQDN/v0.1.0/api/deliveries/$DELIVERY_ID" --header 'Accept: application/json' 
```
