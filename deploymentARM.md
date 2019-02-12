# Deploying the Reference Implementation



## Prerequisites

- Azure suscription
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

> The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

## Generate a SSH rsa public/private key pair

the SSH rsa key pair can be generated using ssh-keygen, among other tools, on Linux, Mac, or Windows. If you already have an ~/.ssh/id_rsa.pub file, you could provide the same later on. If you need to create an SSH key pair, see [How to create and use an SSH key pair](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/mac-create-ssh-keys).
> Note: the SSH rsa public key will be requested when deploying your Kubernetes cluster in Azure.

## Azure Resources Provisioning

Set environment variables.

```bash
export SSH_PUBLIC_KEY_FILE=[YOUR_RECENTLY_GENERATED_SSH_RSA_PUBLIC_KEY_FILE_HERE]
export SSH_PRIVATE_KEY_FILE=[YOUR_RECENTLY_GENERATED_SSH_RSA_PRIVAYE_KEY_FILE_HERE]

export LOCATION=[YOUR_LOCATION_HERE]

export RESOURCE_GROUP=[YOUR_RESOURCE_GROUP_HERE]

export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)

export K8S=./microservices-reference-implementation/k8s
```

Infrastructure Prerequisites

```bash
# Log in to Azure
az login

# Create a resource group and service principal for AKS
az group create --name $RESOURCE_GROUP --location $LOCATION && \
export SP_DETAILS=$(az ad sp create-for-rbac --role="Contributor") && \
export SP_APP_ID=$(echo $SP_DETAILS | jq ".appId" -r) && \
export SP_CLIENT_SECRET=$(echo $SP_DETAILS | jq ".password" -r) && \
export SP_OBJECT_ID=$(az ad sp show --id $SP_APP_ID -o tsv --query objectId)
```

Deployment

> Note: this deployment might take up to 20 minutes

* using Azure CLI 2.0

  ```bash
  az group deployment create -g $RESOURCE_GROUP --name azuredeploy --template-file azuredeploy.json \
    --parameters servicePrincipalClientId=${SP_APP_ID} \
             servicePrincipalClientSecret=${SP_CLIENT_SECRET} \
             servicePrincipalId=${SP_OBJECT_ID} \
             sshRSAPublicKey="$(cat ${SSH_PUBLIC_KEY_FILE})"
  ```

* from Azure Portal

  [![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmspnp%2Fmicroservices-reference-implementation%2Fmaster%2Fazuredeploy.json)
  > Note:
  > 1. paste the $RESOURCE_GROUP value in the resource group field. Important: choose use existing resource group
  > 2. paste the content of your ssh-rsa public key file in the Ssh RSA Plublic Key field.
  > 3. paste the $SP_APP_ID, $SP_CLIENT_SECRET, and $SP_OBJECT_ID in the corresponding fields.

Get outputs from Azure Deploy
```bash
# Shared
export ACR_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.acrName.value -o tsv) && \
export ACR_SERVER=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.acrLoginServer.value -o tsv) && \
export CLUSTER_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.aksClusterName.value -o tsv)
```

Enable Azure Monitoring for the AKS cluster
```bash
az aks enable-addons -a monitoring --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME
```

Download kubectl and create a k8s namespace
```bash
#  Install kubectl
sudo az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME

# Create namespaces
kubectl create namespace backend && \
kubectl create namespace frontend
```


Integrate Application Insights instance

```bash
# Acquire Instrumentation Key
export AI_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.appInsightsName.value -o tsv)
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

```bash
# setup AAD pod identity
kubectl create -f https://raw.githubusercontent.com/Azure/aad-pod-identity/master/deploy/infra/deployment-rbac.yaml

kubectl create -f https://raw.githubusercontent.com/Azure/kubernetes-keyvault-flexvol/master/deployment/kv-flexvol-installer.yaml
```

## Deploy the Delivery service

Extract resource details from deployment

```bash
export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.deliveryCosmosDbName.value -o tsv) && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col" && \
export DELIVERY_KEYVAULT_URI=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.deliveryKeyVaultUri.value -o tsv)
```

Build the Delivery service

```bash
export DELIVERY_PATH=./microservices-reference-implementation/src/shipping/delivery
```

Build and publish the container image

```bash
# Build the Docker image
docker build --pull --compress -t $ACR_SERVER/delivery:0.1.0 $DELIVERY_PATH/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/delivery:0.1.0
```

Set up pod identity

```bash
# Extract outputs from deployment
export DELIVERY_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.deliveryPrincipalResourceId.value -o tsv) && \
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.deliveryPrincipalClientId.value -o tsv)

# Deploy the identity resources
cat $K8S/delivery-identity.yaml | \
    sed "s#ResourceID: \"identityResourceId\"#ResourceID: $DELIVERY_PRINCIPAL_RESOURCE_ID#g" | \
    sed "s#ClientID: \"identityClientid\"#ClientID: $DELIVERY_PRINCIPAL_CLIENT_ID#g" > $K8S/delivery-identity-0.yaml
kubectl apply -f $K8S/delivery-identity-0.yaml
```

Deploy the Delivery service:

```bash
# Update the image tag and config values in the deployment YAML
sed "s#image:#image: $ACR_SERVER/delivery:0.1.0#g" $K8S/delivery.yaml | \
    sed "s/value: \"CosmosDB_DatabaseId\"/value: $DATABASE_NAME/g" | \
    sed "s/value: \"CosmosDB_CollectionId\"/value: $COLLECTION_NAME/g" | \
    sed "s#value: \"KeyVault_Name\"#value: $DELIVERY_KEYVAULT_URI#g" > $K8S/delivery-0.yaml

# Deploy the service
kubectl --namespace backend apply -f $K8S/delivery-0.yaml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy the Package service

Extract resource details from deployment

```bash
export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.packageMongoDbName.value -o tsv)
```

Build the Package service

```bash
export PACKAGE_PATH=microservices-reference-implementation/src/shipping/package

# Build the docker image
docker build -f $PACKAGE_PATH/Dockerfile -t $ACR_SERVER/package:0.1.0 $PACKAGE_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/package:0.1.0
```

Deploy the Package service

```bash
# Update deployment YAML with image tage
sed "s#image:#image: $ACR_SERVER/package:0.1.0#g" $K8S/package.yml > $K8S/package-0.yml

# Create secret
# Note: Connection strings cannot be exported as outputs in ARM deployments
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g')
kubectl -n backend create \
                   secret generic package-secrets \
                   --from-literal=mongodb-pwd=$COSMOSDB_CONNECTION \
                   --from-literal=appinsights-ikey=$AI_IKEY

# Deploy service
kubectl --namespace backend apply -f $K8S/package-0.yml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy the Workflow service

Extract resource details from deployment

```bash
export WORKFLOW_KEYVAULT_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.workflowKeyVaultName.value -o tsv)
```

Build the workflow service

```bash
export WORKFLOW_PATH=./microservices-reference-implementation/src/shipping/workflow

# Build the Docker image
docker build --pull --compress -t $ACR_SERVER/workflow:0.1.0 $WORKFLOW_PATH/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/workflow:0.1.0
```

Create and set up pod identity

```bash
# Extract outputs from deployment
export WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.workflowPrincipalResourceId.value -o tsv) && \
export WORKFLOW_PRINCIPAL_CLIENT_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.workflowPrincipalClientId.value -o tsv)

# Deploy the identity resources
cat $K8S/workflow-identity.yaml | \
    sed "s#ResourceID: \"identityResourceId\"#ResourceID: $WORKFLOW_PRINCIPAL_RESOURCE_ID#g" | \
    sed "s#ClientID: \"identityClientid\"#ClientID: $WORKFLOW_PRINCIPAL_CLIENT_ID#g" > $K8S/workflow-identity-0.yaml
kubectl apply -f $K8S/workflow-identity-0.yaml
```

Deploy the Workflow service:

```bash
# Update the image tag and config values in the deployment YAML
sed "s#image:#image: $ACR_SERVER/workflow:0.1.0#g" $K8S/workflow.yaml | \
    sed "s#resourcegroup: \"keyVaultResourceGroup\"#resourcegroup: $RESOURCE_GROUP#g" | \
    sed "s#subscriptionid: \"keyVaultSubscriptionId\"#subscriptionid: $SUBSCRIPTION_ID#g" | \
    sed "s#tenantid: \"keyVaultTenantId\"#tenantid: $TENANT_ID#g" | \
    sed "s#keyvaultname: \"keyVaultName\"#keyvaultname: $WORKFLOW_KEYVAULT_NAME#g" > $K8S/workflow-0.yaml

# Deploy the service
kubectl --namespace backend apply -f $K8S/workflow-0.yaml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy the Ingestion service

Extract resource details from deployment

```bash
export INGESTION_QUEUE_NAMESPACE=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionQueueNamespace.value -o tsv) && \
export INGESTION_QUEUE_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_ACCESS_KEY_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionServiceAccessKeyName.value -o tsv)
export INGESTION_ACCESS_KEY_VALUE=$(az servicebus namespace authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_QUEUE_NAMESPACE --name $INGESTION_ACCESS_KEY_NAME --query primaryKey -o tsv)
```

Build the Ingestion service

```bash
export INGESTION_PATH=./microservices-reference-implementation/src/shipping/ingestion

# Build the app
docker build -t openjdk_and_mvn-build:8-jdk -f $INGESTION_PATH/Dockerfilemaven $INGESTION_PATH
docker run -it --rm -v $( cd "${INGESTION_PATH}" && pwd )/:/sln openjdk_and_mvn-build:8-jdk

# Build the docker image
docker build -f $INGESTION_PATH/Dockerfile -t $ACR_SERVER/ingestion:0.1.0 $INGESTION_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/ingestion:0.1.0
```

Deploy the Ingestion service

```bash
# Update deployment YAML with image tage
sed "s#image:#image: $ACR_SERVER/ingestion:0.1.0#g" $K8S/ingestion.yaml > $K8S/ingestion-0.yaml

# Create secret
kubectl -n backend create secret generic ingestion-secrets \
    --from-literal=queue_namespace=${INGESTION_QUEUE_NAMESPACE} \
    --from-literal=queue_name=${INGESTION_QUEUE_NAME} \
    --from-literal=queue_keyname=${INGESTION_ACCESS_KEY_NAME} \
    --from-literal=queue_keyvalue=${INGESTION_ACCESS_KEY_VALUE}

# Deploy service
kubectl --namespace backend apply -f $K8S/ingestion-0.yaml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy DroneScheduler service

Extract resource details from deployment

```bash
export DRONESCHEDULER_KEYVAULT_URI=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
```

Build the dronescheduler services

```bash
export DRONE_PATH=microservices-reference-implementation/src/shipping/dronescheduler
```

Create and set up pod identity

```bash
# Extract outputs from deployment
export DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv) && \
export DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.droneSchedulerPrincipalClientId.value -o tsv)

# Deploy the identity resources
cat $K8S/dronescheduler-identity.yaml | \
    sed "s#ResourceID: \"identityResourceId\"#ResourceID: $DRONESCHEDULER_PRINCIPAL_RESOURCE_ID#g" | \
    sed "s#ClientID: \"identityClientid\"#ClientID: $DRONESCHEDULER_PRINCIPAL_CLIENT_ID#g" > $K8S/dronescheduler-identity-0.yaml
kubectl apply -f $K8S/dronescheduler-identity-0.yaml
```

Build and publish the container image

```bash
# Build the Docker image
docker build -f $DRONE_PATH/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 $DRONE_PATH/../

# Push the images to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/dronescheduler:0.1.0
```

Deploy the dronescheduler services:

```bash
# Update the image tag in the deployment YAML
cat $K8S/dronescheduler.yaml | \
    sed "s#image:#image: $ACR_SERVER/dronescheduler:0.1.0#g"  | \
    sed "s#value: \"KeyVault_Name\"#value: $DRONESCHEDULER_KEYVAULT_URI#g" > $K8S/dronescheduler-0.yaml

# Deploy the service
kubectl --namespace backend apply -f $K8S/dronescheduler-0.yaml

## Verify all services are running:
kubectl get pods -n backend
```

## Validate the application is running

You can send delivery requests to the ingestion service using the Swagger UI.

Get the public IP address of the Ingestion Service:

```bash
export EXTERNAL_IP_ADDRESS=$(kubectl get --namespace backend svc ingestion -o jsonpath="{.status.loadBalancer.ingress[0].*}")
```

Use a web browser to navigate to `http://[EXTERNAL_IP_ADDRESS]/swagger-ui.html#/ingestion45controller/scheduleDeliveryAsyncUsingPOST` and use the **Try it out** button to submit a delivery request.

```bash
open "http://$EXTERNAL_IP_ADDRESS/swagger-ui.html#/ingestion45controller/scheduleDeliveryAsyncUsingPOST"
```

> We recommended putting an API Gateway in front of all public APIs. For convenience, the Ingestion service is directly exposed with a public IP address.

## Optional steps

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
