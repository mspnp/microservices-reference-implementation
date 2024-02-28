# Deploying the Reference Implementation

## Prerequisites

- Azure subscription
  > Important: The user initiating the deployment process must have access to the **Microsoft.Authorization/roleAssignments/write** permission. For more information, see [the Container Insights doc](https://docs.microsoft.com/azure/azure-monitor/insights/container-insights-troubleshoot#authorization-error-during-onboarding-or-update-operation)
- [Azure CLI 2.53.1 or newer](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)
- [JQ](https://stedolan.github.io/jq/download/)

> Note: in linux systems, it is possible to run the docker command without prefacing
> with sudo. For more information, please refer to [the Post-installation steps
> for linux](https://docs.docker.com/install/linux/linux-postinstall/)

Clone or download this repo locally.

```bash
git clone --recurse-submodules https://github.com/mspnp/microservices-reference-implementation.git && \
cd microservices-reference-implementation/
```

The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

## Azure Resources Provisioning

Set environment variables.

```bash
export LOCATION=eastus2
```

Log in in to Azure.

```bash
az login

# if you have several subscriptions, select one
# az account set -s <subscription id>
```

## Deployment

> Note: this deployment might take up to 20 minutes

Infrastructure

```bash
# Deploy the managed identities (This takes less than two  minutes.)

export PREREQS_DEPLOYMENT_NAME=workload-stamp-prereqs-main

az deployment sub create --name $PREREQS_DEPLOYMENT_NAME --location $LOCATION --template-file ./workload/workload-stamp-prereqs.bicep --parameters resourceGroupLocation=$LOCATION

# Get the user identities
export DELIVERY_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-delivery --query principalId -o tsv) && \
export DRONESCHEDULER_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-dronescheduler --query principalId -o tsv) && \
export WORKFLOW_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-workflow --query principalId -o tsv) && \
export PACKAGE_ID_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-package --query principalId -o tsv) && \
export INGESTION_ID_PRINCIPAL_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-ingestion --query principalId -o tsv)


# Wait for Microsoft Entra ID propagation
until az ad sp show --id $DELIVERY_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for Microsoft Entra ID propagation" && sleep 5; done
until az ad sp show --id $DRONESCHEDULER_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for Microsoft Entra ID propagation" && sleep 5; done
until az ad sp show --id $WORKFLOW_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for Microsoft Entra ID propagation" && sleep 5; done
until az ad sp show --id $PACKAGE_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for Microsoft Entra ID propagation" && sleep 5; done
until az ad sp show --id $INGESTION_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for Microsoft Entra ID propagation" && sleep 5; done

# Deploy all the workload related resources  (This step takes about 10 minutes)
az deployment group create -f ./workload/workload-stamp.bicep -g rg-shipping-dronedelivery -p droneSchedulerPrincipalId=$DRONESCHEDULER_PRINCIPAL_ID -p workflowPrincipalId=$WORKFLOW_PRINCIPAL_ID -p deliveryPrincipalId=$DELIVERY_PRINCIPAL_ID -p ingestionPrincipalId=$INGESTION_ID_PRINCIPAL_ID -p packagePrincipalId=$PACKAGE_ID_PRINCIPAL_ID

# Get outputs from workload deploy
export ACR_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.acrName.value -o tsv)
export ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv)
```

Deploy the managed cluster and all related resources (This step takes about 15 minutes)

```bash
export RESOURCE_GROUP_ID=$(az group show --name rg-shipping-dronedelivery --query id --output tsv)

export SP_DETAILS=$(az ad sp create-for-rbac --role="Contributor" --scopes $RESOURCE_GROUP_ID -o json) && \
export SP_APP_ID=$(echo $SP_DETAILS | jq ".appId" -r) && \
export SP_CLIENT_SECRET=$(echo $SP_DETAILS | jq ".password" -r)
export TENANT_ID=$(az account show --query tenantId --output tsv)

export DEPLOYMENT_SUFFIX=$(date +%S%N)

export KUBERNETES_VERSION=$(az aks get-versions -l $LOCATION --query "values[?isDefault].version" -o tsv)

export DEPLOYMENT_NAME=azuredeploy-$DEPLOYMENT_SUFFIX
az deployment group create -g rg-shipping-dronedelivery --name $DEPLOYMENT_NAME  --template-file azuredeploy.bicep \
--parameters servicePrincipalClientId=$SP_APP_ID \
            servicePrincipalClientSecret=$SP_CLIENT_SECRET \
            kubernetesVersion=$KUBERNETES_VERSION \
            deliveryIdName=uid-delivery \
            ingestionIdName=uid-ingestion \
            packageIdName=uid-package \
            droneSchedulerIdName=uid-dronescheduler \
            workflowIdName=uid-workflow \
            acrResourceGroupName=rg-shipping-dronedelivery-acr \
            acrName=$ACR_NAME
```

Get the cluster name output from Azure Deploy.

```bash
export CLUSTER_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n $DEPLOYMENT_NAME --query properties.outputs.aksClusterName.value -o tsv)
echo $CLUSTER_NAME
```

Download kubectl and create a Kubernetes namespace.

```bash
#  Install kubectl
az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=rg-shipping-dronedelivery --name=$CLUSTER_NAME

# Create namespaces
kubectl create namespace backend-dev
```

Install and initialize Helm.

```bash
# install helm 3
curl https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3 | bash
```

Integrate Application Insights instance.

```bash
# Acquire Instrumentation Key
export AI_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.appInsightsName.value -o tsv)
echo $AI_NAME

# add RBAC for AppInsights
kubectl apply -f k8s/k8s-rbac-ai.yaml
```

## Verify that the secrets-store pods are running in the kube-system namespace

```bash
kubectl get pods -n kube-system
```

You should see an output similar to this:

```bash
NAME                                     READY   STATUS    RESTARTS   AGE
aks-secrets-store-csi-driver-4bjzx       3/3     Running   2          28m
aks-secrets-store-csi-driver-b22bj       3/3     Running   1          28m
aks-secrets-store-provider-azure-2k5mx   1/1     Running   0          28m
aks-secrets-store-provider-azure-l5w98   1/1     Running   0          28m
```

## Deploy the ingress controller

> :warning: WARNING
>
> Do not use the certificates created by these scripts for production. The
> certificates are provided for demonstration purposes only.
> For your production cluster, use your
> security best practices for digital certificates creation and lifetime management.

> :heavy*exclamation_mark: In the following instructions you will proceed using a public container registry to install NGINX. But please take into account that public registries may be subject to faults such as outages (no SLA) or request throttling. Interruptions like these can be crippling for an application that needs to pull an image \_right now*. To minimize the risks of using public registries, store all applicable container images in a registry that you control, such as the SLA-backed Azure Container Registry.

```bash
# Deploy the ngnix ingress controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.3.0/deploy/static/provider/cloud/deploy.yaml

# Check the Ingress controller pod is running
kubectl get pods --namespace ingress-nginx

# Obtain the load balancer ip address and assign a domain name
until export INGRESS_LOAD_BALANCER_IP=$(kubectl get services ingress-nginx-controller -n ingress-nginx -o jsonpath="{.status.loadBalancer.ingress[0].ip}" 2> /dev/null) && test -n "$INGRESS_LOAD_BALANCER_IP"; do echo "Waiting for load balancer deployment" && sleep 20; done

export INGRESS_LOAD_BALANCER_IP_ID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$INGRESS_LOAD_BALANCER_IP')].[id]" --output tsv) && \
export EXTERNAL_INGEST_DNS_NAME="rg-shipping-dronedelivery-${RANDOM}-ing" && \
export EXTERNAL_INGEST_FQDN=$(az network public-ip update --ids $INGRESS_LOAD_BALANCER_IP_ID --dns-name $EXTERNAL_INGEST_DNS_NAME --query "dnsSettings.fqdn" --output tsv)

# Create a self-signed certificate for TLS
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -out ingestion-ingress-tls.crt \
    -keyout ingestion-ingress-tls.key \
    -subj "/CN=${EXTERNAL_INGEST_FQDN}/O=fabrikam"
```

## Setup cluster resource quota

```bash
kubectl apply -f k8s/k8s-resource-quotas-dev.yaml
```

## Get the OIDC Issuer URL

```bash
export AKS_OIDC_ISSUER="$(az aks show -n $CLUSTER_NAME -g rg-shipping-dronedelivery --query "oidcIssuerProfile.issuerUrl" -otsv)"
```

## Deploy the Delivery service

Extract resource details from deployment.

```bash
export COSMOSDB_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.deliveryCosmosDbName.value -o tsv) && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col" && \
export DELIVERY_KEYVAULT_URI=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.deliveryKeyVaultUri.value -o tsv) && \
export DELIVERY_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.deliveryKeyVaultName.value -o tsv) && \
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-delivery --query clientId -o tsv)
```

Build and publish the Delivery service container image.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/delivery:0.1.0 ./workload/src/shipping/delivery/.
```

Deploy the Delivery service.

```bash
# Create secrets
# Note: Ingress TLS key and certificate secrets cannot be exported as outputs in ARM deployments
# Give current user permissions to import secrets and then it is deleted right after the secret creation command is executed
export SIGNED_IN_OBJECT_ID=$(az ad signed-in-user show --query 'id' -o tsv)

export DELIVERY_KEYVAULT_ID=$(az resource show -g rg-shipping-dronedelivery  -n $DELIVERY_KEYVAULT_NAME --resource-type 'Microsoft.KeyVault/vaults' --query id --output tsv)
az role assignment create --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $DELIVERY_KEYVAULT_ID

az keyvault secret set --name Delivery-Ingress-Tls-Key --vault-name $DELIVERY_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.key)"
az keyvault secret set --name Delivery-Ingress-Tls-Crt --vault-name $DELIVERY_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.crt)"

az role assignment delete --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $DELIVERY_KEYVAULT_ID

#Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-delivery --identity-name uid-delivery --resource-group rg-shipping-dronedelivery --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:delivery-sa-v0.1.0

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
     --set ingress.class=nginx \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set keyvault.uri=$DELIVERY_KEYVAULT_URI \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --dependency-update

# Verify the pod is created
helm status delivery-v0.1.0-dev --namespace backend-dev
```

## Deploy the Package service

Extract resource details from deployment.

```bash
export COSMOSDB_NAME_PACKAGE=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.packageMongoDbName.value -o tsv)
export PACKAGE_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.packageKeyVaultName.value -o tsv)
export PACKAGE_ID_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-package --query clientId -o tsv)
```

Build the Package service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/package:0.1.0 ./workload/src/shipping/package/.
```

Deploy the Package service.

```bash
# Create secret
# Note: Connection strings cannot be exported as outputs in ARM deployments
# Give current user permissions to import secrets and then it is deleted right after the secret creation command is executed
export COSMOSDB_CONNECTION_PACKAGE=$(az cosmosdb keys list --type connection-strings --name $COSMOSDB_NAME_PACKAGE --resource-group rg-shipping-dronedelivery --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g') && \
export COSMOSDB_COL_NAME_PACKAGE=packages

export PACKAGE_KEYVAULT_ID=$(az resource show -g rg-shipping-dronedelivery  -n $PACKAGE_KEYVAULT_NAME --resource-type 'Microsoft.KeyVault/vaults' --query id --output tsv)
az role assignment create --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $PACKAGE_KEYVAULT_ID

az keyvault secret set --name CosmosDb--ConnectionString --vault-name $PACKAGE_KEYVAULT_NAME --value $COSMOSDB_CONNECTION_PACKAGE

az role assignment delete --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $PACKAGE_KEYVAULT_ID

# Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-package --identity-name uid-package --resource-group rg-shipping-dronedelivery --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:package-sa-v0.1.0

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
     --set ingress.class=nginx \
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
export WORKFLOW_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.workflowKeyVaultName.value -o tsv)
export WORKFLOW_ID_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery  -n uid-workflow --query clientId -o tsv)
```

Build the workflow service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/workflow:0.1.0 ./workload/src/shipping/workflow/.
```

Deploy the Workflow service.

```bash
# Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-workflow --identity-name uid-workflow --resource-group rg-shipping-dronedelivery --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:workflow-sa-v0.1.0

# Deploy the service
helm package charts/workflow/ -u && \
helm install workflow-v0.1.0-dev workflow-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=workflow \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$WORKFLOW_ID_CLIENT_ID \
     --set identity.serviceAccountName=workflow-sa-v0.1.0 \
     --set identity.tenantId=$TENANT_ID \
     --set keyvault.name=$WORKFLOW_KEYVAULT_NAME \
     --set keyvault.resourcegroup=rg-shipping-dronedelivery \
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
export INGESTION_QUEUE_NAMESPACE=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.ingestionQueueNamespace.value -o tsv) && \
export INGESTION_QUEUE_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_KEYVAULT_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.ingestionKeyVaultName.value -o tsv)
export INGESTION_ID_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-ingestion --query clientId -o tsv)

# Give current user permissions to import secrets and then it is deleted right after the secret creation command is executed
export INGESTION_KEYVAULT_ID=$(az resource show -g rg-shipping-dronedelivery  -n $INGESTION_KEYVAULT_NAME --resource-type 'Microsoft.KeyVault/vaults' --query id --output tsv)
az role assignment create --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $INGESTION_KEYVAULT_ID

az keyvault secret set --name Ingestion-Ingress-Tls-Key --vault-name $INGESTION_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.key)"
az keyvault secret set --name Ingestion-Ingress-Tls-Crt --vault-name $INGESTION_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.crt)"

az role assignment delete --role 'Key Vault Secrets Officer' --assignee $SIGNED_IN_OBJECT_ID --scope $INGESTION_KEYVAULT_ID
```

Build the Ingestion service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/ingestion:0.1.0 ./workload/src/shipping/ingestion/.
```

Deploy the Ingestion service

```bash
# Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-ingestion --identity-name uid-ingestion --resource-group rg-shipping-dronedelivery --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:ingestion-sa-v0.1.0

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
     --set ingress.class=nginx \
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
export DRONESCHEDULER_KEYVAULT_URI=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
export DRONESCHEDULER_COSMOSDB_NAME=$(az deployment group show -g rg-shipping-dronedelivery -n workload-stamp --query properties.outputs.droneSchedulerCosmosDbName.value -o tsv) && \
export ENDPOINT_URL=$(az cosmosdb show -n $DRONESCHEDULER_COSMOSDB_NAME -g rg-shipping-dronedelivery --query documentEndpoint -o tsv) && \
export AUTH_KEY=$(az cosmosdb keys list -n $DRONESCHEDULER_COSMOSDB_NAME -g rg-shipping-dronedelivery --query primaryMasterKey -o tsv) && \
export DRONESCHEDULER_CLIENT_ID=$(az identity show -g rg-shipping-dronedelivery -n uid-dronescheduler --query clientId -o tsv)  && \
export DATABASE_NAME="invoicing" && \
export COLLECTION_NAME="utilization"
```

Build and publish the container image.

```bash
az acr build -r $ACR_NAME -f ./workload/src/shipping/dronescheduler/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 ./workload/src/shipping/.
```

Deploy the dronescheduler service.

```bash
# Setup your managed identity to trust your Kubernetes service account
az identity federated-credential create --name credential-for-dronescheduler --identity-name uid-dronescheduler --resource-group rg-shipping-dronedelivery --issuer ${AKS_OIDC_ISSUER} --subject system:serviceaccount:backend-dev:dronescheduler-sa-v0.1.0

# Deploy the service
helm package charts/dronescheduler/ -u && \
helm install dronescheduler-v0.1.0-dev dronescheduler-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=dronescheduler \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=dronescheduler \
     --set ingress.hosts[0].tls=false \
     --set ingress.class=nginx \
     --set identity.clientid=$DRONESCHEDULER_CLIENT_ID \
     --set identity.serviceAccountName=dronescheduler-sa-v0.1.0 \
     --set keyvault.uri=$DRONESCHEDULER_KEYVAULT_URI \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --dependency-update

# Verify the pod is created
helm status dronescheduler-v0.1.0-dev --namespace backend-dev
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
