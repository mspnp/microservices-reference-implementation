# Deploying the Reference Implementation

## Prerequisites

- Azure subscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)

> Note: in linux systems, it is possible to run the docker command without prefacing
>       with sudo. For more information, please refer to [the Post-installation steps
>       for linux](https://docs.docker.com/install/linux/linux-postinstall/)

Clone or download this repo locally.

```bash
git clone https://github.com/mspnp/microservices-reference-implementation.git
```

The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

## Create the Kubernetes cluster

Set environment variables.

```bash
export LOCATION=[YOUR_LOCATION_HERE]

export UNIQUE_APP_NAME_PREFIX=[YOUR_UNIQUE_APPLICATION_NAME_HERE]

export RESOURCE_GROUP="${UNIQUE_APP_NAME_PREFIX}-rg" && \
export CLUSTER_NAME="${UNIQUE_APP_NAME_PREFIX}-cluster"

export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)

export K8S=./microservices-reference-implementation/k8s
```

Provision a Kubernetes cluster in AKS

```bash
# Log in to Azure
az login

# Create a resource group for AKS
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create the AKS cluster
az aks create --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --node-count 4 --enable-addons monitoring --generate-ssh-keys

# Install kubectl
sudo az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME

# Get the cluster's principal
export CLUSTER_SERVICE_PRINCIPAL=$(az aks show --name $CLUSTER_NAME --resource-group $RESOURCE_GROUP --query servicePrincipalProfile.clientId --output tsv)

# Create namespaces
kubectl create namespace backend && \
kubectl create namespace frontend
```

Create an Azure Container Registry instance.

> Note: Azure Container Registory is not required. If you prefer, you can store the Docker images for this solution in another container registry.

```bash
export ACR_NAME=[YOUR_CONTAINER_REGISTRY_NAME_HERE]

# Create the ACR instance
az acr create --name $ACR_NAME --resource-group $RESOURCE_GROUP --sku Basic

# Log in to ACR
az acr login --name $ACR_NAME

# Get the ACR login server name
export ACR_SERVER=$(az acr show -g $RESOURCE_GROUP -n $ACR_NAME --query "loginServer" -o tsv)
```

Grant the cluster access to the registry.

```bash
# Acquire the necessary IDs
export CLUSTER_CLIENT_ID=$(az aks show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --query "servicePrincipalProfile.clientId" --output tsv)
export ACR_ID=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "id" --output tsv)

# Grant the cluster read access to the registry
az role assignment create --assignee $CLUSTER_CLIENT_ID --role Reader --scope $ACR_ID
```

## Setup AAD pod identity and key vault flexvol infrastructure

Complete instructions can be found at https://github.com/Azure/kubernetes-keyvault-flexvol

```bash
# setup AAD pod identity
kubectl create -f https://raw.githubusercontent.com/Azure/aad-pod-identity/master/deploy/infra/deployment-rbac.yaml

kubectl create -f https://raw.githubusercontent.com/Azure/kubernetes-keyvault-flexvol/master/deployment/kv-flexvol-installer.yaml
```

## Deploy the Delivery service

Provision Azure resources

```bash
export REDIS_NAME="${UNIQUE_APP_NAME_PREFIX}-delivery-service-redis" && \
export COSMOSDB_NAME="${UNIQUE_APP_NAME_PREFIX}-delivery-service-cosmosdb" && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col"

# Create Azure Redis Cache
az redis create --location $LOCATION \
            --name $REDIS_NAME \
            --resource-group $RESOURCE_GROUP \
            --sku Premium \
            --vm-size P4

# Create Cosmos DB account with DocumentDB API
az cosmosdb create \
    --name $COSMOSDB_NAME \
    --kind GlobalDocumentDB \
    --resource-group $RESOURCE_GROUP \
    --max-interval 10 \
    --max-staleness-prefix 200
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

Create Kubernetes secrets

```bash
export REDIS_ENDPOINT=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query hostName -o tsv)
export REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey -o tsv)

export COSMOSDB_KEY=$(az cosmosdb list-keys --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query primaryMasterKey -o tsv)
export COSMOSDB_ENDPOINT=$(az cosmosdb show --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query documentEndpoint -o tsv)

kubectl --namespace backend create --save-config=true secret generic delivery-storageconf \
    --from-literal=CosmosDB_Key=${COSMOSDB_KEY} \
    --from-literal=CosmosDB_Endpoint=${COSMOSDB_ENDPOINT} \
    --from-literal=Redis_Endpoint=${REDIS_ENDPOINT} \
    --from-literal=Redis_AccessKey=${REDIS_KEY} \
    --from-literal=EH_ConnectionString=
```

Create KeyVault and secrets

```bash
export DELIVERY_KEYVAULT_NAME="${UNIQUE_APP_NAME_PREFIX}-delivery-kv"
az keyvault create --name $DELIVERY_KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name CosmosDB-Key --value ${COSMOSDB_KEY} # (consider using encoding base64 to keep the actual values)
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name CosmosDB-Endpoint --value ${COSMOSDB_ENDPOINT}
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name Redis-Endpoint --value ${REDIS_ENDPOINT}
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name Redis-AccessKey --value ${REDIS_KEY}
# az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name EH-ConnectionString    # cannot create a secret without a value or with an empty value

export DELIVERY_KEYVAULT_ID=$(az keyvault show --resource-group $RESOURCE_GROUP --name $DELIVERY_KEYVAULT_NAME --query "id" --output tsv)
export DELIVERY_KEYVAULT_URI=$(az keyvault show --resource-group $RESOURCE_GROUP --name $DELIVERY_KEYVAULT_NAME --query "properties.vaultUri" --output tsv)
```

Create and set up pod identity

```bash
# Create the identity and extract properties
export DELIVERY_PRINCIPAL_NAME=delivery
az identity create --resource-group $RESOURCE_GROUP --name $DELIVERY_PRINCIPAL_NAME
export DELIVERY_PRINCIPAL_RESOURCE_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $DELIVERY_PRINCIPAL_NAME --query "id" --output tsv)
export DELIVERY_PRINCIPAL_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $DELIVERY_PRINCIPAL_NAME --query "principalId" --output tsv)
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $DELIVERY_PRINCIPAL_NAME --query "clientId" --output tsv)

# Grant the identity access to the KeyVault
az role assignment create --role Reader --assignee $DELIVERY_PRINCIPAL_ID --scope $DELIVERY_KEYVAULT_ID
az keyvault set-policy --name $DELIVERY_KEYVAULT_NAME --secret-permissions get, list --spn $DELIVERY_PRINCIPAL_CLIENT_ID

# Allow the cluster to manage the identity to assign to pods
az role assignment create --role "Managed Identity Operator" --assignee $CLUSTER_SERVICE_PRINCIPAL --scope $DELIVERY_PRINCIPAL_RESOURCE_ID

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
    sed "s/value: \"EH_EntityPath\"/value:/g" | \
    sed "s#value: \"KeyVault_Name\"#value: $DELIVERY_KEYVAULT_URI#g" | \
    sed "s#resourcegroup: \"keyVaultResourceGroup\"#resourcegroup: $RESOURCE_GROUP#g" | \
    sed "s#subscriptionid: \"keyVaultSubscriptionId\"#subscriptionid: $SUBSCRIPTION_ID#g" | \
    sed "s#tenantid: \"keyVaultTenantId\"#tenantid: $TENANT_ID#g" | \
    sed "s#keyvaultname: \"keyVaultName\"#keyvaultname: $DELIVERY_KEYVAULT_NAME#g" > $K8S/delivery-0.yaml

# Deploy the service
kubectl --namespace backend apply -f $K8S/delivery-0.yaml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy the Package service

Provision Azure resources

```bash
export COSMOSDB_NAME="${UNIQUE_APP_NAME_PREFIX}-package-service-cosmosdb"
az cosmosdb create --name $COSMOSDB_NAME --kind MongoDB --resource-group $RESOURCE_GROUP
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
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv)
kubectl -n backend create secret generic package-secrets --from-literal=mongodb-pwd=$COSMOSDB_CONNECTION

# Create KeyVault secret
export PACKAGE_KEYVAULT_NAME="${UNIQUE_APP_NAME_PREFIX}-package-kv"
az keyvault create --name $PACKAGE_KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION
az keyvault secret set --vault-name $PACKAGE_KEYVAULT_NAME --name mongodb-pwd --value $COSMOSDB_CONNECTION

# Deploy service
kubectl --namespace backend apply -f $K8S/package-0.yml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy the Ingestion service
Provision Azure resources

```bash
export INGESTION_EH_NS=[INGESTION_EVENT_HUB_NAMESPACE_HERE]
export INGESTION_EH_NAME=[INGESTION_EVENT_HUB_NAME_HERE]
export INGESTION_EH_CONSUMERGROUP_NAME=[INGESTION_EVENT_HUB_CONSUMERGROUP_NAME_HERE]

# Create an Event Hubs namespace
az eventhubs namespace create --name $INGESTION_EH_NS \
                              --resource-group $RESOURCE_GROUP \
                              --location $LOCATION

# Create an event hub
az eventhubs eventhub create --name $INGESTION_EH_NAME \
                             --resource-group $RESOURCE_GROUP \
                             --namespace-name $INGESTION_EH_NS \
                             --partition-count 4

# Create consumer group
az eventhubs eventhub consumer-group create --eventhub-name $INGESTION_EH_NAME \
                                            --name $INGESTION_EH_CONSUMERGROUP_NAME \
                                            --namespace-name $INGESTION_EH_NS \
                                            --resource-group $RESOURCE_GROUP

# Create authorization rule
az eventhubs eventhub authorization-rule create --eventhub-name $INGESTION_EH_NAME \
                                                --name IngestionServiceAccessKey \
                                                --namespace-name $INGESTION_EH_NS \
                                                --resource-group $RESOURCE_GROUP \
                                                --rights Listen Send

# Get access key
export EH_ACCESS_KEY_VALUE=$(az eventhubs eventhub authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_EH_NS --name IngestionServiceAccessKey --eventhub-name $INGESTION_EH_NAME --query primaryKey -o tsv)
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
--from-literal=eventhub_namespace=${INGESTION_EH_NS} \
--from-literal=eventhub_name=${INGESTION_EH_NAME} \
--from-literal=eventhub_keyname=IngestionServiceAccessKey \
--from-literal=eventhub_keyvalue=${EH_ACCESS_KEY_VALUE}

# Create KeyVault secrets
export INGESTION_KEYVAULT_NAME="${UNIQUE_APP_NAME_PREFIX}-ingestion-kv"
az keyvault create --name $INGESTION_KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION
az keyvault secret set --vault-name $INGESTION_KEYVAULT_NAME --name eventhub-namespace --value ${INGESTION_EH_NS}
az keyvault secret set --vault-name $INGESTION_KEYVAULT_NAME --name eventhub-name --value ${INGESTION_EH_NAME}
az keyvault secret set --vault-name $INGESTION_KEYVAULT_NAME --name eventhub-keyname --value IngestionServiceAccessKey
az keyvault secret set --vault-name $INGESTION_KEYVAULT_NAME --name eventhub-keyvalue --value ${EH_ACCESS_KEY_VALUE}

# Deploy service
kubectl --namespace backend apply -f $K8S/ingestion-0.yaml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy DroneScheduler service

Build the dronescheduler services

```bash
export DRONE_PATH=microservices-reference-implementation/src/shipping/dronescheduler
```

Build and publish the container image

```bash
# Build the Docker images
docker build -f $DRONE_PATH/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 $DRONE_PATH/../

# Push the images to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/dronescheduler:0.1.0
```

Deploy the dronescheduler services:

```bash
# Update the image tag in the deployment YAML
sed "s#image:#image: $ACR_SERVER/dronescheduler:0.1.0#g" $K8S/dronescheduler.yaml > $K8S/dronescheduler-0.yaml

# Deploy the service
kubectl --namespace backend apply -f $K8S/dronescheduler-0.yaml

## Verify all services are running:
kubectl get pods -n backend
```

## Deploy linkerd


```bash
kubectl create ns linkerd
wget https://raw.githubusercontent.com/linkerd/linkerd-examples/master/k8s-daemonset/k8s/servicemesh.yml && \
sed -i "s#/default#/shipping#g" servicemesh.yml && \
sed -i "149i \ \ \ \ \ \ \ \ /svc/account => /svc/account.accounts ;" servicemesh.yml && \
sed -i "149i \ \ \ \ \ \ \ \ /svc/dronescheduler => /svc/dronescheduler.backend ;" servicemesh.yml && \
sed -i "149i \ \ \ \ \ \ \ \ /svc/thirdparty => /svc/thirdparty.3rdparty ;" servicemesh.yml && \
sed -i "176i \ \ \ \ \ \ \ \ /svc/account => /svc/account.accounts ;" servicemesh.yml && \
sed -i "176i \ \ \ \ \ \ \ \ /svc/dronescheduler => /svc/dronescheduler.backend ;" servicemesh.yml && \
sed -i "176i \ \ \ \ \ \ \ \ /svc/thirdparty => /svc/thirdparty.3rdparty ;" servicemesh.yml && \
kubectl apply -f servicemesh.yml
```

For more information, see [https://linkerd.io/getting-started/k8s/](https://linkerd.io/getting-started/k8s/)

> Note:
> The service mesh configuration linked above uses the default namespace for service discovery.
> Since Drone Delivery microservices are getting deployed into several custom namespaces, this config needs to be modified as shown. This change modifies the dtab rules.

The linkerd accounts need to be granted permissions to query the cluster for resources, as RBAC is enabled by default on AKS.

```bash
wget https://raw.githubusercontent.com/linkerd/linkerd-examples/master/k8s-daemonset/k8s/linkerd-rbac.yml && \
sed -i "s#namespace: default#namespace: linkerd#g" linkerd-rbac.yml && \
kubectl apply -f linkerd-rbac.yml
```

For more information on using linkerd with an RBAC-enabled cluster see [https://blog.buoyant.io/2017/07/24/using-linkerd-kubernetes-rbac/](https://blog.buoyant.io/2017/07/24/using-linkerd-kubernetes-rbac/)

## Validate the application is running

You can send delivery requests to the ingestion service using the Swagger UI.

Get the public IP address of the Ingestion Service:

```bash
export EXTERNAL_IP_ADDRESS=$(kubectl get --namespace shipping svc ingestion -o jsonpath="{.status.loadBalancer.ingress[0].*}")
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

Deploy Prometheus and Grafana. For more information, see https://github.com/linkerd/linkerd-viz#kubernetes-deploy
