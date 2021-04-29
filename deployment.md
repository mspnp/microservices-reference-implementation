# Deploying the Reference Implementation

## Prerequisites

- Azure subscription
  > Important: The user initiating the deployment process must have access to the **Microsoft.Authorization/roleAssignments/write** permission. For more information, see [the Container Insights doc](https://docs.microsoft.com/azure/azure-monitor/insights/container-insights-troubleshoot#authorization-error-during-onboarding-or-update-operation)
- [Azure CLI 2.0.49 or later](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)
- [JQ](https://stedolan.github.io/jq/download/)

> Note: in linux systems, it is possible to run the docker command without prefacing
> with sudo. For more information, please refer to [the Post-installation steps
> for linux](https://docs.docker.com/install/linux/linux-postinstall/)

Clone or download this repo locally.

```bash
git clone https://github.com/mspnp/microservices-reference-implementation.git && \
cd microservices-reference-implementation/
```

The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

## Generate a SSH rsa public/private key pair

the SSH rsa key pair can be generated using ssh-keygen, among other tools, on Linux, Mac, or Windows. If you already have an ~/.ssh/id_rsa.pub file, you could provide the same later on. If you need to create an SSH key pair, see [How to create and use an SSH key pair](https://docs.microsoft.com/azure/virtual-machines/linux/mac-create-ssh-keys).

> Note: the SSH rsa public key will be requested when deploying your Kubernetes cluster in Azure.

## Azure Resources Provisioning

Set environment variables.

```bash
export SSH_PUBLIC_KEY_FILE=[YOUR_RECENTLY_GENERATED_SSH_RSA_PUBLIC_KEY_FILE_HERE]
export LOCATION=[YOUR_LOCATION_HERE]
export RESOURCE_GROUP=[YOUR_RESOURCE_GROUP_HERE]
```

Gather infrastructure Prerequisites.

```bash
# Log in to Azure
az login

# if you have several subscriptions, select one 
# az account set -s <subscription id>

# Create service principal for AKS
export SP_DETAILS=$(az ad sp create-for-rbac --role="Contributor" -o json) && \
export SP_APP_ID=$(echo $SP_DETAILS | jq ".appId" -r) && \
export SP_CLIENT_SECRET=$(echo $SP_DETAILS | jq ".password" -r) && \
export SP_OBJECT_ID=$(az ad sp show --id $SP_APP_ID -o tsv --query objectId)

# It is needed later on in the two paths
export DEPLOYMENT_SUFFIX=$(date +%S%N)
```

## Optional: Set up automated CI/CD for dev, test, qa and production with Azure DevOps

Add [CI/CD to Drone Delivery using Azure Pipelines with YAML](./deploymentCICD.md).

> Important: If you don't want to set up the CI/CD pipelines, you can manually deploy the application for development as follows.

## Manual deployment for dev

> Note: this deployment might take up to 20 minutes

Infrastructure

```bash
# Deploy the resource groups and managed identities
# These are deployed first in a separate template to avoid propagation delays with AAD
export DEV_PREREQ_DEPLOYMENT_NAME=azuredeploy-prereqs-${DEPLOYMENT_SUFFIX}-dev
az deployment sub create \
   --name $DEV_PREREQ_DEPLOYMENT_NAME \
   --location $LOCATION \
   --template-file azuredeploy-prereqs.json \
   --parameters resourceGroupName=$RESOURCE_GROUP \
                resourceGroupLocation=$LOCATION

export IDENTITIES_DEPLOYMENT_NAME=$(az deployment sub show -n $DEV_PREREQ_DEPLOYMENT_NAME --query properties.outputs.identitiesDeploymentName.value -o tsv) && \
export DELIVERY_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.deliveryIdName.value -o tsv) && \
export DELIVERY_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query principalId -o tsv) && \
export DRONESCHEDULER_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerIdName.value -o tsv) && \
export DRONESCHEDULER_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query principalId -o tsv) && \
export WORKFLOW_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.workflowIdName.value -o tsv) && \
export WORKFLOW_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query principalId -o tsv) && \
export RESOURCE_GROUP_ACR=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.acrResourceGroupName.value -o tsv)

# Wait for AAD propagation
until az ad sp show --id $DELIVERY_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $DRONESCHEDULER_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $WORKFLOW_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done

# Export the kubernetes cluster version
export KUBERNETES_VERSION=$(az aks get-versions -l $LOCATION --query "orchestrators[?default!=null].orchestratorVersion" -o tsv)

# Deploy all other resources
export DEV_DEPLOYMENT_NAME=azuredeploy-$DEPLOYMENT_SUFFIX-dev
az deployment group create -g $RESOURCE_GROUP --name $DEV_DEPLOYMENT_NAME --template-file azuredeploy.json \
--parameters servicePrincipalClientId=$SP_APP_ID \
            servicePrincipalClientSecret=$SP_CLIENT_SECRET \
            servicePrincipalId=$SP_OBJECT_ID \
            kubernetesVersion=$KUBERNETES_VERSION \
            sshRSAPublicKey="$(cat $SSH_PUBLIC_KEY_FILE)" \
            deliveryIdName=$DELIVERY_ID_NAME \
            deliveryPrincipalId=$DELIVERY_ID_PRINCIPAL_ID \
            droneSchedulerIdName=$DRONESCHEDULER_ID_NAME \
            droneSchedulerPrincipalId=$DRONESCHEDULER_ID_PRINCIPAL_ID \
            workflowIdName=$WORKFLOW_ID_NAME \
            workflowPrincipalId=$WORKFLOW_ID_PRINCIPAL_ID \
            acrResourceGroupName=$RESOURCE_GROUP_ACR
```

Get outputs from Azure Deploy.

```bash
export ACR_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.acrName.value -o tsv) && \
export ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv) && \
export CLUSTER_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.aksClusterName.value -o tsv)
```

Download kubectl and create a Kubernetes namespace.

```bash
#  Install kubectl
az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME

# Create namespaces
kubectl create namespace backend-dev
```

Install and initialize Helm.

```bash
# install helm client side
curl -L https://git.io/get_helm.sh | bash -s -- -v v3.5.4
helm repo add stable https://charts.helm.sh/stable
helm repo update
```

Integrate Application Insights instance.

```bash
# Acquire Instrumentation Key
export AI_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.appInsightsName.value -o tsv)
export AI_IKEY=$(az resource show \
                    -g $RESOURCE_GROUP \
                    -n $AI_NAME \
                    --resource-type "Microsoft.Insights/components" \
                    --query properties.InstrumentationKey \
                    -o tsv)

# add RBAC for AppInsights
kubectl apply -f k8s/k8s-rbac-ai.yaml
```

## Setup AAD pod identity and key vault flexvol infrastructure

Complete instructions can be found at https://github.com/Azure/kubernetes-keyvault-flexvol

Note: the tested nmi version was 1.4. It enables namespaced pod identity.

```bash
# setup AAD pod identity
helm repo add aad-pod-identity https://raw.githubusercontent.com/Azure/aad-pod-identity/master/charts

helm repo update

helm install aad-pod-identity aad-pod-identity/aad-pod-identity --set installCRDs=true --set nmi.allowNetworkPluginKubenet=true  --namespace kube-system --version 4.0.0

kubectl create -f https://raw.githubusercontent.com/Azure/kubernetes-keyvault-flexvol/master/deployment/kv-flexvol-installer.yaml
```

## Deploy the ingress controller

> :warning: WARNING
>
> Do not use the certificates created by these scripts for production. The
> certificates are provided for demonstration purposes only.
> For your production cluster, use your
> security best practices for digital certificates creation and lifetime management.

> :heavy_exclamation_mark: In the following instructions you will proceed using a public container registry to install NGINX. But please take into account that public registries may be subject to faults such as outages (no SLA) or request throttling. Interruptions like these can be crippling for an application that needs to pull an image _right now_. To minimize the risks of using public registries, store all applicable container images in a registry that you control, such as the SLA-backed Azure Container Registry.

```bash
# Deploy the ngnix ingress controller
kubectl create namespace ingress-controllers
helm install nginx-ingress-dev stable/nginx-ingress --namespace ingress-controllers --version 1.24.7 --set rbac.create=true --set controller.ingressClass=nginx-dev

# Obtain the load balancer ip address and assign a domain name
until export INGRESS_LOAD_BALANCER_IP=$(kubectl get services/nginx-ingress-dev-controller -n ingress-controllers -o jsonpath="{.status.loadBalancer.ingress[0].ip}" 2> /dev/null) && test -n "$INGRESS_LOAD_BALANCER_IP"; do echo "Waiting for load balancer deployment" && sleep 20; done

export INGRESS_LOAD_BALANCER_IP_ID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$INGRESS_LOAD_BALANCER_IP')].[id]" --output tsv) && \
export EXTERNAL_INGEST_DNS_NAME="${RESOURCE_GROUP}-ingest-dev" && \
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

## Deploy the Delivery service

Extract resource details from deployment.

```bash
export COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryCosmosDbName.value -o tsv) && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col" && \
export DELIVERY_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryKeyVaultUri.value -o tsv)
```

Build and publish the Delivery service container image.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/delivery:0.1.0 ./src/shipping/delivery/.
```

Deploy the Delivery service.

```bash
# Extract pod identity outputs from deployment
export DELIVERY_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.deliveryPrincipalResourceId.value -o tsv) && \
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query clientId -o tsv) && \
export DELIVERY_INGRESS_TLS_SECRET_NAME=delivery-ingress-tls

# Deploy the service
helm install delivery-v0.1.0-dev charts/delivery/ \
     --set image.tag=0.1.0 \
     --set image.repository=delivery \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=delivery \
     --set ingress.hosts[0].tls=true \
     --set ingress.hosts[0].tlsSecretName=$DELIVERY_INGRESS_TLS_SECRET_NAME \
     --set ingress.tls.secrets[0].name=$DELIVERY_INGRESS_TLS_SECRET_NAME \
     --set ingress.tls.secrets[0].key="$(cat ingestion-ingress-tls.key)" \
     --set ingress.tls.secrets[0].certificate="$(cat ingestion-ingress-tls.crt)" \
     --set identity.clientid=$DELIVERY_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$DELIVERY_PRINCIPAL_RESOURCE_ID \
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
export COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.packageMongoDbName.value -o tsv)
```

Build the Package service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/package:0.1.0 ./src/shipping/package/.
```

Deploy the Package service.

```bash
# Create secret
# Note: Connection strings cannot be exported as outputs in ARM deployments
export COSMOSDB_CONNECTION=$(az cosmosdb keys list --type connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g') && \
export COSMOSDB_COL_NAME=packages

# Deploy service
helm install package-v0.1.0-dev charts/package/ \
     --set image.tag=0.1.0 \
     --set image.repository=package \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=package \
     --set ingress.hosts[0].tls=false \
     --set secrets.appinsights.ikey=$AI_IKEY \
     --set secrets.mongo.pwd=$COSMOSDB_CONNECTION \
     --set cosmosDb.collectionName=$COSMOSDB_COL_NAME \
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
export WORKFLOW_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.workflowKeyVaultName.value -o tsv)
```

Build the workflow service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/workflow:0.1.0 ./src/shipping/workflow/.
```

Create and set up pod identity.

```bash
# Extract outputs from deployment and get Azure account details
export WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.workflowPrincipalResourceId.value -o tsv) && \
export WORKFLOW_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query clientId -o tsv) && \
export SUBSCRIPTION_ID=$(az account show --query id --output tsv) && \
export TENANT_ID=$(az account show --query tenantId --output tsv)
```

Deploy the Workflow service.

```bash
# Deploy the service
helm install workflow-v0.1.0-dev charts/workflow/ \
     --set image.tag=0.1.0 \
     --set image.repository=workflow \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$WORKFLOW_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$WORKFLOW_PRINCIPAL_RESOURCE_ID \
     --set keyvault.name=$WORKFLOW_KEYVAULT_NAME \
     --set keyvault.resourcegroup=$RESOURCE_GROUP \
     --set keyvault.subscriptionid=$SUBSCRIPTION_ID \
     --set keyvault.tenantid=$TENANT_ID \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --dependency-update

# Verify the pod is created
helm status workflow-v0.1.0-dev --namespace backend-dev
```

## Deploy the Ingestion service

Extract resource details from deployment.

```bash
export INGESTION_QUEUE_NAMESPACE=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionQueueNamespace.value -o tsv) && \
export INGESTION_QUEUE_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionQueueName.value -o tsv) && \
export INGESTION_ACCESS_KEY_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionServiceAccessKeyName.value -o tsv) && \
export INGESTION_ACCESS_KEY_VALUE=$(az servicebus namespace authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_QUEUE_NAMESPACE --name $INGESTION_ACCESS_KEY_NAME --query primaryKey -o tsv)
```

Build the Ingestion service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/ingestion:0.1.0 ./src/shipping/ingestion/.
```

Deploy the Ingestion service.

```bash
# Set secreat name
export INGRESS_TLS_SECRET_NAME=ingestion-ingress-tls

# Deploy service
helm install ingestion-v0.1.0-dev charts/ingestion/ \
     --set image.tag=0.1.0 \
     --set image.repository=ingestion \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=ingestion \
     --set ingress.hosts[0].tls=true \
     --set ingress.hosts[0].tlsSecretName=$INGRESS_TLS_SECRET_NAME \
     --set ingress.tls.secrets[0].name=$INGRESS_TLS_SECRET_NAME \
     --set ingress.tls.secrets[0].key="$(cat ingestion-ingress-tls.key)" \
     --set ingress.tls.secrets[0].certificate="$(cat ingestion-ingress-tls.crt)" \
     --set secrets.appinsights.ikey=${AI_IKEY} \
     --set secrets.queue.keyname=IngestionServiceAccessKey \
     --set secrets.queue.keyvalue=${INGESTION_ACCESS_KEY_VALUE} \
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
export DRONESCHEDULER_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
export DRONESCHEDULER_COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerCosmosDbName.value -o tsv) && \
export ENDPOINT_URL=$(az cosmosdb show -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query documentEndpoint -o tsv) && \
export AUTH_KEY=$(az cosmosdb keys list -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query primaryMasterKey -o tsv) && \
export DATABASE_NAME="invoicing" && \
export COLLECTION_NAME="utilization"
```

Create and set up pod identity.

```bash
# Extract outputs from deployment
export DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv) && \
export DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query clientId -o tsv)
```

Build and publish the container image.

```bash
az acr build -r $ACR_NAME -f ./src/shipping/dronescheduler/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 ./src/shipping/.
```

Deploy the dronescheduler service.

```bash
# Deploy the service
helm install dronescheduler-v0.1.0-dev charts/dronescheduler/ \
     --set image.tag=0.1.0 \
     --set image.repository=dronescheduler \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=dronescheduler \
     --set ingress.hosts[0].tls=false \
     --set identity.clientid=$DRONESCHEDULER_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$DRONESCHEDULER_PRINCIPAL_RESOURCE_ID \
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
curl -X POST "https://$EXTERNAL_INGEST_FQDN/api/deliveryrequests" --header 'Content-Type: application/json' --header 'Accept: application/json' -k -d '{
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
curl "https://$EXTERNAL_INGEST_FQDN/api/deliveries/$DELIVERY_ID" --header 'Accept: application/json' -k
```
