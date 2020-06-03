# Deploying the Reference Implementation

## Prerequisites

- Azure subscription
  > Important: The user initiating the deployment process must have access to the `Microsoft.Authorization/roleAssignments/write` permission. For more information, please refer to [the Container Insights doc](https://docs.microsoft.com/en-us/azure/azure-monitor/insights/container-insights-troubleshoot#authorization-error-during-onboarding-or-update-operation)
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)
- [JQ](https://stedolan.github.io/jq/download/)

> Note: in linux systems, it is possible to run the docker command without prefacing
>       with sudo. For more information, please refer to [the Post-installation steps
>       for linux](https://docs.docker.com/install/linux/linux-postinstall/)

Clone or download this repo locally.

```bash
git clone https://github.com/mspnp/microservices-reference-implementation.git
```

The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

## Generate a SSH rsa public/private key pair

the SSH rsa key pair can be generated using ssh-keygen, among other tools, on Linux, Mac, or Windows. If you already have an ~/.ssh/id_rsa.pub file, you could provide the same later on. If you need to create an SSH key pair, see [How to create and use an SSH key pair](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/mac-create-ssh-keys).
> Note: the SSH rsa public key will be requested when deploying your Kubernetes cluster in Azure.

## Azure Resources Provisioning

Set environment variables.

```bash
export SSH_PUBLIC_KEY_FILE=[YOUR_RECENTLY_GENERATED_SSH_RSA_PUBLIC_KEY_FILE_HERE]

export LOCATION=[YOUR_LOCATION_HERE]

export RESOURCE_GROUP=[YOUR_RESOURCE_GROUP_HERE]

export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
export SUBSCRIPTION_NAME=$(az account show --query name --output tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)

export DEPLOYMENT_SUFFIX=$(date +%S%N)

export PROJECT_ROOT=./microservices-reference-implementation
export K8S=$PROJECT_ROOT/k8s
export HELM_CHARTS=$PROJECT_ROOT/charts
```

Infrastructure Prerequisites

```bash
# Log in to Azure
az login

# Create service principal for AKS
export SP_DETAILS=$(az ad sp create-for-rbac --role="Contributor" -o json) && \
export SP_APP_ID=$(echo $SP_DETAILS | jq ".appId" -r) && \
export SP_CLIENT_SECRET=$(echo $SP_DETAILS | jq ".password" -r) && \
export SP_OBJECT_ID=$(az ad sp show --id $SP_APP_ID -o tsv --query objectId)
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
az deployment create \
   --name $DEV_PREREQ_DEPLOYMENT_NAME \
   --location $LOCATION \
   --template-file ${PROJECT_ROOT}/azuredeploy-prereqs.json \
   --parameters resourceGroupName=$RESOURCE_GROUP \
                resourceGroupLocation=$LOCATION

export IDENTITIES_DEV_DEPLOYMENT_NAME=$(az deployment show -n $DEV_PREREQ_DEPLOYMENT_NAME --query properties.outputs.identitiesDeploymentName.value -o tsv) && \
export DELIVERY_ID_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryIdName.value -o tsv) && \
export DELIVERY_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query principalId -o tsv) && \
export DRONESCHEDULER_ID_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerIdName.value -o tsv) && \
export DRONESCHEDULER_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query principalId -o tsv) && \
export WORKFLOW_ID_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.workflowIdName.value -o tsv) && \
export WORKFLOW_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query principalId -o tsv) && \
export GATEWAY_CONTROLLER_ID_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.appGatewayControllerIdName.value -o tsv) && \
export GATEWAY_CONTROLLER_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $GATEWAY_CONTROLLER_ID_NAME --query principalId -o tsv) && \
export RESOURCE_GROUP_ACR=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.acrResourceGroupName.value -o tsv)

# Wait for AAD propagation
until az ad sp show --id ${DELIVERY_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id ${DRONESCHEDULER_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id ${WORKFLOW_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id ${GATEWAY_CONTROLLER_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done

# Export the kubernetes cluster version
export KUBERNETES_VERSION=$(az aks get-versions -l $LOCATION --query "orchestrators[?default!=null].orchestratorVersion" -o tsv)
export SERVICETAGS_LOCATION=$(az account list-locations --query "[?name=='${LOCATION}'].displayName" -o tsv | sed 's/[[:space:]]//g')

# Deploy cluster and microservices Azure services
export DEV_DEPLOYMENT_NAME=azuredeploy-${DEPLOYMENT_SUFFIX}-dev
az group deployment create -g $RESOURCE_GROUP --name $DEV_DEPLOYMENT_NAME --template-file ${PROJECT_ROOT}/azuredeploy.json \
--parameters servicePrincipalClientId=${SP_APP_ID} \
            servicePrincipalClientSecret=${SP_CLIENT_SECRET} \
            servicePrincipalId=${SP_OBJECT_ID} \
            kubernetesVersion=${KUBERNETES_VERSION} \
            sshRSAPublicKey="$(cat ${SSH_PUBLIC_KEY_FILE})" \
            deliveryIdName=${DELIVERY_ID_NAME} \
            deliveryPrincipalId=${DELIVERY_ID_PRINCIPAL_ID} \
            droneSchedulerIdName=${DRONESCHEDULER_ID_NAME} \
            droneSchedulerPrincipalId=${DRONESCHEDULER_ID_PRINCIPAL_ID} \
            workflowIdName=${WORKFLOW_ID_NAME} \
            workflowPrincipalId=${WORKFLOW_ID_PRINCIPAL_ID} \
            appGatewayControllerIdName=${GATEWAY_CONTROLLER_ID_NAME} \
            appGatewayControllerPrincipalId=${GATEWAY_CONTROLLER_ID_PRINCIPAL_ID} \
            acrResourceGroupName=${RESOURCE_GROUP_ACR} \
            acrResourceGroupLocation=$LOCATION

export VNET_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.aksVNetName.value -o tsv) && \
export CLUSTER_SUBNET_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.aksClusterSubnetName.value -o tsv) && \
export CLUSTER_SUBNET_PREFIX=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.aksClusterSubnetPrefix.value -o tsv) && \
export CLUSTER_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.aksClusterName.value -o tsv) && \
export CLUSTER_SERVER=$(az aks show -n $CLUSTER_NAME -g $RESOURCE_GROUP --query fqdn -o tsv) && \
export FIREWALL_PIP_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.firewallPublicIpName.value -o tsv) && \
export ACR_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.acrName.value -o tsv) && \
export ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv) && \
export DELIVERY_REDIS_HOSTNAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryRedisHostName.value -o tsv)

# Restrict cluster egress traffic
az group deployment create -g $RESOURCE_GROUP --name azuredeploy-firewall-${DEPLOYMENT_SUFFIX} --template-file ${PROJECT_ROOT}/azuredeploy-firewall.json \
--parameters aksVnetName=${VNET_NAME} \
            aksClusterSubnetName=${CLUSTER_SUBNET_NAME} \
            aksClusterSubnetPrefix=${CLUSTER_SUBNET_PREFIX} \
            firewallPublicIpName=${FIREWALL_PIP_NAME} \
            serviceTagsLocation=${SERVICETAGS_LOCATION} \
            aksFqdns="['${CLUSTER_SERVER}']" \
            acrServers="['${ACR_SERVER}']" \
            deliveryRedisHostNames="['${DELIVERY_REDIS_HOSTNAME}']"
```

Get outputs from Azure Deploy
```bash
# Shared
export GATEWAY_SUBNET_PREFIX=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.appGatewaySubnetPrefix.value -o tsv)
```

Download kubectl and create a k8s namespace
```bash
#  Install kubectl
sudo az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME

# Create namespaces
kubectl create namespace backend-dev
```

Setup Helm

```bash
# install helm CLI
curl https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3 | bash
```

Integrate Application Insights instance

```bash
# Acquire Instrumentation Key
export AI_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.appInsightsName.value -o tsv)
export AI_IKEY=$(az resource show \
                    -g $RESOURCE_GROUP \
                    -n $AI_NAME \
                    --resource-type "Microsoft.Insights/components" \
                    --query properties.InstrumentationKey \
                    -o tsv)

# add RBAC for AppInsights
kubectl apply -f $K8S/k8s-rbac-ai.yaml
```

## Setup AAD pod identity and key vault flexvol infrastructure

Complete instructions can be found at https://github.com/Azure/kubernetes-keyvault-flexvol

Note: the tested nmi version was 1.4. It enables namespaced pod identity.

```bash
# setup AAD pod identity
helm repo add aad-pod-identity https://raw.githubusercontent.com/Azure/aad-pod-identity/master/charts && \
helm install aad-pod-identity aad-pod-identity/aad-pod-identity -n kube-system

kubectl create -f https://raw.githubusercontent.com/Azure/kubernetes-keyvault-flexvol/master/deployment/kv-flexvol-installer.yaml
```

## Deploy the ingress controllers

> :warning: WARNING
>
> Do not use the certificates created by these scripts for production. The
> certificates are provided for demonstration purposes only.
> For your production cluster, use your
> security best practices for digital certificates creation and lifetime management.

```bash
# Deploy the AppGateway ingress controller
helm repo add application-gateway-kubernetes-ingress https://appgwingress.blob.core.windows.net/ingress-azure-helm-package/
helm repo update

export GATEWAY_CONTROLLER_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.appGatewayControllerPrincipalResourceId.value -o tsv) && \
export GATEWAY_CONTROLLER_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $GATEWAY_CONTROLLER_ID_NAME --query clientId -o tsv)
export APP_GATEWAY_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.appGatewayName.value -o tsv)
export APP_GATEWAY_PUBLIC_IP_FQDN=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.appGatewayPublicIpFqdn.value -o tsv)

helm install ingress-azure-dev application-gateway-kubernetes-ingress/ingress-azure \
     --namespace kube-system \
     --set appgw.name=$APP_GATEWAY_NAME \
     --set appgw.resourceGroup=$RESOURCE_GROUP \
     --set appgw.subscriptionId=$SUBSCRIPTION_ID \
     --set appgw.shared=false \
     --set kubernetes.watchNamespace=backend-dev \
     --set armAuth.type=aadPodIdentity \
     --set armAuth.identityResourceID=$GATEWAY_CONTROLLER_PRINCIPAL_RESOURCE_ID \
     --set armAuth.identityClientID=$GATEWAY_CONTROLLER_PRINCIPAL_CLIENT_ID \
     --set rbac.enabled=true \
     --set verbosityLevel=3 \
     --set aksClusterConfiguration.apiServerAddress=$CLUSTER_SERVER \
     --set appgw.usePrivateIP=false \
     --version 1.2.0-rc2

# Create a self-signed certificate for TLS
export EXTERNAL_INGEST_FQDN=$APP_GATEWAY_PUBLIC_IP_FQDN
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -out ingestion-ingress-tls.crt \
    -keyout ingestion-ingress-tls.key \
    -subj "/CN=${APP_GATEWAY_PUBLIC_IP_FQDN}/O=fabrikam"
```

## Setup cluster resource quota

```bash
kubectl apply -f $K8S/k8s-resource-quotas-dev.yaml
```

## Deny all ingress and egress traffic

```bash
kubectl apply -f $K8S/k8s-deny-all-non-whitelisted-traffic-dev.yaml
```

## Deploy the Delivery service

Extract resource details from deployment

```bash
export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryCosmosDbName.value -o tsv) && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col" && \
export DELIVERY_KEYVAULT_URI=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryKeyVaultUri.value -o tsv)
```

Build the Delivery service

```bash
export DELIVERY_PATH=$PROJECT_ROOT/src/shipping/delivery
```

Build and publish the container image

```bash
# Build the Docker image
docker build --pull --compress -t $ACR_SERVER/delivery:0.1.0 $DELIVERY_PATH/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/delivery:0.1.0
```

Deploy the Delivery service:

```bash
# Extract pod identity outputs from deployment
export DELIVERY_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryPrincipalResourceId.value -o tsv) && \
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query clientId -o tsv)
export DELIVERY_INGRESS_TLS_SECRET_NAME=delivery-ingress-tls

# Deploy the service
helm package $HELM_CHARTS/delivery/ -u && \
helm install delivery-v0.1.0-dev delivery-v0.1.0.tgz \
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
     --set networkPolicy.egress.external.enabled=true \
     --set networkPolicy.egress.external.clusterSubnetPrefix=$CLUSTER_SUBNET_PREFIX \
     --set networkPolicy.ingress.externalSubnet.enabled=true \
     --set networkPolicy.ingress.externalSubnet.subnetPrefix=$GATEWAY_SUBNET_PREFIX \
     --set identity.clientid=$DELIVERY_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$DELIVERY_PRINCIPAL_RESOURCE_ID \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set keyvault.uri=$DELIVERY_KEYVAULT_URI \
     --set reason="Initial deployment" \
     --set envs.dev=true \
     --namespace backend-dev

# Verify the pod is created
helm status delivery-v0.1.0-dev --namespace backend-dev
```

## Deploy the Package service

Extract resource details from deployment

```bash
export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.packageMongoDbName.value -o tsv)
```

Build the Package service

```bash
export PACKAGE_PATH=$PROJECT_ROOT/src/shipping/package

# Build the docker image
docker build -f $PACKAGE_PATH/Dockerfile -t $ACR_SERVER/package:0.1.0 $PACKAGE_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/package:0.1.0
```

Deploy the Package service

```bash
# Create secret
# Note: Connection strings cannot be exported as outputs in ARM deployments
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g') && \
export COSMOSDB_COL_NAME=packages

# Deploy service
helm package $HELM_CHARTS/package/ -u && \
helm install package-v0.1.0-dev package-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=package \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=package \
     --set ingress.hosts[0].tls=false \
     --set networkPolicy.egress.external.enabled=true \
     --set networkPolicy.egress.external.clusterSubnetPrefix=$CLUSTER_SUBNET_PREFIX \
     --set secrets.appinsights.ikey=$AI_IKEY \
     --set secrets.mongo.pwd=$COSMOSDB_CONNECTION \
     --set cosmosDb.collectionName=$COSMOSDB_COL_NAME \
     --set dockerregistry=$ACR_SERVER \
     --set reason="Initial deployment" \
     --set envs.dev=true \
     --namespace backend-dev

# Verify the pod is created
helm status package-v0.1.0-dev --namespace backend-dev
```

## Deploy the Workflow service

Extract resource details from deployment

```bash
export WORKFLOW_KEYVAULT_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.workflowKeyVaultName.value -o tsv)
```

Build the workflow service

```bash
export WORKFLOW_PATH=$PROJECT_ROOT/src/shipping/workflow

# Build the Docker image
docker build --pull --compress -t $ACR_SERVER/workflow:0.1.0 $WORKFLOW_PATH/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/workflow:0.1.0
```

Create and set up pod identity

```bash
# Extract outputs from deployment
export WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.workflowPrincipalResourceId.value -o tsv) && \
export WORKFLOW_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query clientId -o tsv)
```

Deploy the Workflow service:

```bash
# Deploy the service
helm package $HELM_CHARTS/workflow/ -u && \
helm install workflow-v0.1.0-dev workflow-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=workflow \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$WORKFLOW_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$WORKFLOW_PRINCIPAL_RESOURCE_ID \
     --set networkPolicy.egress.external.enabled=true \
     --set networkPolicy.egress.external.clusterSubnetPrefix=$CLUSTER_SUBNET_PREFIX \
     --set keyvault.name=$WORKFLOW_KEYVAULT_NAME \
     --set keyvault.resourcegroup=$RESOURCE_GROUP \
     --set keyvault.subscriptionid=$SUBSCRIPTION_ID \
     --set keyvault.tenantid=$TENANT_ID \
     --set reason="Initial deployment" \
     --set envs.dev=true \
     --namespace backend-dev

# Verify the pod is created
helm status workflow-v0.1.0-dev --namespace backend-dev
```

## Deploy the Ingestion service

Extract resource details from deployment

```bash
export INGESTION_QUEUE_NAMESPACE=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionQueueNamespace.value -o tsv) && \
export INGESTION_QUEUE_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_ACCESS_KEY_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionServiceAccessKeyName.value -o tsv)
export INGESTION_ACCESS_KEY_VALUE=$(az servicebus namespace authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_QUEUE_NAMESPACE --name $INGESTION_ACCESS_KEY_NAME --query primaryKey -o tsv)
```

Build the Ingestion service

```bash
export INGESTION_PATH=$PROJECT_ROOT/src/shipping/ingestion

# Build the docker image
docker build -f $INGESTION_PATH/Dockerfile -t $ACR_SERVER/ingestion:0.1.0 $INGESTION_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/ingestion:0.1.0
```

Deploy the Ingestion service

```bash
# Set secreat name
export INGRESS_TLS_SECRET_NAME=ingestion-ingress-tls

# Deploy service
helm package $HELM_CHARTS/ingestion/ -u && \
helm install ingestion-v0.1.0-dev ingestion-v0.1.0.tgz \
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
     --set networkPolicy.egress.external.enabled=true \
     --set networkPolicy.egress.external.clusterSubnetPrefix=$CLUSTER_SUBNET_PREFIX \
     --set networkPolicy.ingress.externalSubnet.enabled=true \
     --set networkPolicy.ingress.externalSubnet.subnetPrefix=$GATEWAY_SUBNET_PREFIX \
     --set secrets.appinsights.ikey=${AI_IKEY} \
     --set secrets.queue.keyname=IngestionServiceAccessKey \
     --set secrets.queue.keyvalue=${INGESTION_ACCESS_KEY_VALUE} \
     --set secrets.queue.name=${INGESTION_QUEUE_NAME} \
     --set secrets.queue.namespace=${INGESTION_QUEUE_NAMESPACE} \
     --set reason="Initial deployment" \
     --set envs.dev=true \
     --namespace backend-dev

# Verify the pod is created
helm status ingestion-v0.1.0-dev --namespace backend-dev
```

## Deploy DroneScheduler service

Extract resource details from deployment

```bash
export DRONESCHEDULER_KEYVAULT_URI=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
export DRONESCHEDULER_COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerCosmosDbName.value -o tsv) && \
export ENDPOINT_URL=$(az cosmosdb show -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query documentEndpoint -o tsv) && \
export AUTH_KEY=$(az cosmosdb list-keys -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query primaryMasterKey -o tsv) && \
export DATABASE_NAME="invoicing" && \
export COLLECTION_NAME="utilization"
```

Build the dronescheduler services

```bash
export DRONE_PATH=$PROJECT_ROOT/src/shipping/dronescheduler
```

Create and set up pod identity

```bash
# Extract outputs from deployment
export DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv) && \
export DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query clientId -o tsv)
```

Build and publish the container image

```bash
# Build the Docker image
docker build -f $DRONE_PATH/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 $DRONE_PATH/../

# Push the images to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/dronescheduler:0.1.0
```

Deploy the dronescheduler service:
```bash
# Deploy the service
helm package $HELM_CHARTS/dronescheduler/ -u && \
helm install dronescheduler-v0.1.0-dev dronescheduler-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=dronescheduler \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=dronescheduler \
     --set ingress.hosts[0].tls=false \
     --set identity.clientid=$DRONESCHEDULER_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$DRONESCHEDULER_PRINCIPAL_RESOURCE_ID \
     --set networkPolicy.egress.external.enabled=true \
     --set networkPolicy.egress.external.clusterSubnetPrefix=$CLUSTER_SUBNET_PREFIX \
     --set keyvault.uri=$DRONESCHEDULER_KEYVAULT_URI \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set reason="Initial deployment" \
     --set envs.dev=true \
     --namespace backend-dev

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

## Optional steps

### Fluentd and Elastic Search

Follow these steps to add logging and monitoring capabilities to the solution.

Deploy Elasticsearch. For more information, see https://github.com/kubernetes/examples/tree/master/staging/elasticsearch

```bash
kubectl --namespace kube-system apply -f https://raw.githubusercontent.com/kubernetes/examples/master/staging/elasticsearch/service-account.yaml && \
kubectl --namespace kube-system apply -f https://raw.githubusercontent.com/kubernetes/examples/master/staging/elasticsearch/es-svc.yaml && \
kubectl --namespace kube-system apply -f https://raw.githubusercontent.com/kubernetes/examples/master/staging/elasticsearch/es-rc.yaml
```

Deploy Fluentd. For more information, see https://docs.fluentd.org/v0.12/articles/kubernetes-fluentd

```bash
# The example elasticsearch yaml files deploy a service named "elasticsearch"
wget https://raw.githubusercontent.com/fluent/fluentd-kubernetes-daemonset/master/fluentd-daemonset-elasticsearch.yaml && \
sed -i "s/elasticsearch-logging/elasticsearch/" fluentd-daemonset-elasticsearch.yaml

# Commenting out X-Pack credentials for demo purposes.
# Make sure to configure X-Pack in elasticsearch and provide credentials here for production workloads
sed -i "s/- name: FLUENT_ELASTICSEARCH_USER/#- name: FLUENT_ELASTICSEARCH_USER/" fluentd-daemonset-elasticsearch.yaml && \
sed -i 's/  value: "elastic"/#  value: "elastic"/' fluentd-daemonset-elasticsearch.yaml && \
sed -i "s/- name: FLUENT_ELASTICSEARCH_PASSWORD/#- name: FLUENT_ELASTICSEARCH_PASSWORD/" fluentd-daemonset-elasticsearch.yaml && \
sed -i 's/  value: "changeme"/#  value: "changeme"/' fluentd-daemonset-elasticsearch.yaml && \
kubectl --namespace kube-system apply -f fluentd-daemonset-elasticsearch.yaml
```

## Clean up

Follow the steps below to remove the Fabrikam Drone Delivery app from your cluster

```bash
helm uninstall $(helm ls --all --short -n ingress-controllers)  -n ingress-controllers && \
helm uninstall $(helm ls --all --short -n backend-dev)  -n backend-dev

# if you've choosen the CI/CD path
helm uninstall $(helm ls --all --short -n backend-qa)  -n backend-qa && \
helm uninstall $(helm ls --all --short -n backend-staging)  -n backend-staging && \
helm uninstall $(helm ls --all --short -n backend-prod)  -n backend-prod
```
