# Microservices  Reference Implementation
Microsoft patterns & practices

https://docs.microsoft.com/azure/architecture/microservices

---

## Prerequisites

- Azure suscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/install/)

Clone or download this repo locally.

```bash
git clone https://github.com/mspnp/microservices-reference-implementation.git
```

## Create the Kubernetes cluster

Set environment variables.

```bash
export LOCATION=[YOUR_LOCATION_HERE]
export UNIQUE_APP_NAME_PREFIX=[YOUR_UNIQUE_APPLICATION_NAME_HERE]

export RESOURCE_GROUP="${UNIQUE_APP_NAME_PREFIX}-rg" && \
export CLUSTER_NAME="${UNIQUE_APP_NAME_PREFIX}-cluster"
```

Provision a Kubernetes cluster in ACS

```bash
# Log in to Azure
az login

# Create a resource group for ACS
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create the ACS cluster
az acs create --orchestrator-type kubernetes --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --generate-ssh-keys

# Install kubectl
sudo az acs kubernetes install-cli

# Get the Kubernetes cluster credentials
az acs kubernetes get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME

# Create the Shipping BC namespace
kubectl create namespace bc-shipping
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
export ACR_SERVER=$(az acr show -g $RESOURCE_GROUP -n $ACR_NAME --query "loginServer")

# Strip quotes
export ACR_SERVER=("${ACR_SERVER[@]//\"/}")
```

Deploy Elasticsearch. For more information, see https://github.com/kubernetes/examples/tree/master/staging/elasticsearch

Deploy Fluend. For more information, see https://docs.fluentd.org/v0.12/articles/kubernetes-fluentd

Deploy linkerd. For more information, see https://linkerd.io/getting-started/k8s/

Deploy Prometheus and Grafana. For more information, see https://github.com/linkerd/linkerd-viz#kubernetes-deploy

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

# Create a Cosmos DB database 
az cosmosdb database create \
    --name $COSMOSDB_NAME \
    --db-name=$DATABASE_NAME \
    --resource-group $RESOURCE_GROUP

# Create a Cosmos DB collection
az cosmosdb collection create \
    --collection-name $COLLECTION_NAME \
    --name $COSMOSDB_NAME \
    --db-name $DATABASE_NAME \
    --resource-group $RESOURCE_GROUP
```

Build the Delivery service

```bash
export DELIVERY_PATH=./microservices-reference-implementation/src/bc-shipping/delivery
docker-compose -f $DELIVERY_PATH/docker-compose.ci.build.yml up
```

Build and publish the container image 

```bash
# Build the Docker image
docker build -t $ACR_SERVER/fabrikam.dronedelivery.deliveryservice:0.1.0 $DELIVERY_PATH/Fabrikam.DroneDelivery.DeliveryService/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/fabrikam.dronedelivery.deliveryservice:0.1.0

```

Create Kubernetes secrets

```bash
export COSMOSDB_KEY=$(az cosmosdb list-keys --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query primaryMasterKey) && \
export COSMOSDB_ENDPOINT=$(az cosmosdb show --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query documentEndpoint) && \
export REDIS_HOSTNAME=$(az redis show -n $REDIS_NAME -g $RESOURCE_GROUP --query hostName) && \
export REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey)

kubectl --namespace bc-shipping create --save-config=true secret generic delivery-storageconf \
    --from-literal=CosmosDB_Key=${COSMOSDB_KEY[@]//\"/} \
    --from-literal=CosmosDB_Endpoint=${COSMOSDB_ENDPOINT[@]//\"/} \
    --from-literal=Redis_HostName=${REDIS_HOSTNAME[@]//\"/} \
    --from-literal=Redis_PrimaryKey=${REDIS_KEY[@]//\"/} \
    --from-literal=EH_ConnectionString= \
    --from-literal=Redis_SecondaryKey=
```

Deploy the Delivery service:

```bash
# Update the image tag in the deployment YAML
sed -i "s#image:#image: $ACR_SERVER/fabrikam.dronedelivery.deliveryservice:0.1.0#g" ./microservices-reference-implementation/k8s/delivery.yaml

## Update config values in the deployment YAML
sed -i "s/value: \"CosmosDB_DatabaseId\"/value: $DATABASE_NAME/g"      "./microservices-reference-implementation/k8s/delivery.yaml" && \
sed -i "s/value: \"CosmosDB_CollectionId\"/value: $COLLECTION_NAME/g"  "./microservices-reference-implementation/k8s/delivery.yaml" && \
sed -i "s/value: \"EH_EntityPath\"/value:/g"                           "./microservices-reference-implementation/k8s/delivery.yaml"

# Deploy the service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/delivery.yaml
```

## Deploy the Package service

Provision Azure resources

```bash
export COSMOSDB_NAME="${UNIQUE_APP_NAME_PREFIX}-package-service-cosmosdb"
az cosmosdb create --name $COSMOSDB_NAME --kind MongoDB --resource-group $RESOURCE_GROUP
```

Build the Package service

```bash
export PACKAGE_PATH=microservices-reference-implementation/src/bc-shipping/package

# Build the app
docker-compose -f $PACKAGE_PATH/build/docker-compose.ci.build.yml up

# Build the docker image
sudo docker build -f $PACKAGE_PATH/build/prod.dockerfile -t $ACR_SERVER/package-service:0.1.0 $PACKAGE_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/package-service:0.1.0
```

Deploy the Package service

```bash
# Update deployment YAML with image tage
sed -i "s#image:#image: $ACR_SERVER/package-service:0.1.0#g" ./microservices-reference-implementation/k8s/package.yml

# Create secret
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString")
kubectl -n bc-shipping create secret generic package-secrets --from-literal=mongodb-pwd=${COSMOSDB_CONNECTION[@]//\"/}

# Deploy service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/package.yml
```

## Deploy the Ingestion service 
Provision Azure resources

```bash
export INGESTION_EH_NS=[INGESTION_EVENT_HUB_NAMESPACE_HERE]
export INGESTION_EH_NAME=[INGESTION_EVENT_HUB_NAME_HERE]
export INGESTION_EH_CONSUMERGROUP_NAME=[INGESTION_EVENT_HUB_CONSUMERGROUP_NAME_HERE]

wget https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/201-event-hubs-create-event-hub-and-consumer-group/azuredeploy.json && \
sed -i 's#"partitionCount": "4"#"partitionCount": "32"#g' azuredeploy.json && \
az group deployment create -g $RESOURCE_GROUP --template-file azuredeploy.json  --parameters \
'{ \
  "namespaceName": {"value": "'${INGESTION_EH_NS}'"}, \
  "eventHubName": {"value": "'${INGESTION_EH_NAME}'"}, \
  "consumerGroupName": {"value": "'${INGESTION_EH_CONSUMERGROUP_NAME}'"} \
}'
```
Note: you could also create this from [the Azure Portal](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-create)

Build the Ingestion service

```bash
export INGESTION_PATH=./microservices-reference-implementation/src/bc-shipping/ingestion

# Build the app 
docker build -t openjdk_and_mvn-build:8-jdk -f $INGESTION_PATH/Dockerfilemaven $INGESTION_PATH && \
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
sed -i "s#image:#image: $ACR_SERVER/ingestion:0.1.0#g" ./microservices-reference-implementation/k8s/ingestion.yaml

# Get the EventHub shared access policy name and key from the Azure Portal
export EH_ACCESS_KEY_NAME=[YOUR_SHARED_ACCESS_POLICY_NAME_HERE]
export EH_ACCESS_KEY_VALUE=[YOUR_SHARED_ACCESS_POLICY_VALUE_HERE]

# Create secret
kubectl -n bc-shipping create secret generic ingestion-secrets --from-literal=eventhub_namespace=${INGESTION_EH_NS} \
--from-literal=eventhub_name=${INGESTION_EH_NAME} \
--from-literal=eventhub_keyname={$EH_ACCESS_KEY_NAME} \
--from-literal=eventhub_keyvalue={$EH_ACCESS_KEY_VALUE}

# Deploy service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/ingestion.yaml
```

## Deploy the Scheduler service 

Provision Azure resources
```bash
export SCHEDULER_STORAGE_ACCOUNT_NAME=[SCHEDULER_STORAGE_ACCOUNT_NAME_HERE]

az storage account create --resource-group $RESOURCE_GROUP --name $SCHEDULER_STORAGE_ACCOUNT_NAME --sku Standard_LRS
```

Build the Scheduler service

```bash
export SCHEDULER_PATH=./microservices-reference-implementation/src/bc-shipping/scheduler

# Build the app 
docker build -t openjdk_and_mvn-build:8-jdk -f $SCHEDULER_PATH/Dockerfilemaven $SCHEDULER_PATH && \
docker run -it --rm -v $( cd "${SCHEDULER_PATH}" && pwd )/:/sln openjdk_and_mvn-build:8-jdk

# Build the docker image
docker build -f $SCHEDULER_PATH/Dockerfile -t $ACR_SERVER/scheduler:0.1.0 $SCHEDULER_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/scheduler:0.1.0
```

Deploy the Scheduler service

```bash
# Update deployment YAML with image tage
sed -i "s#image:#image: $ACR_SERVER/scheduler:0.1.0#g" ./microservices-reference-implementation/k8s/scheduler.yaml

# Get the following values from the Azure Portal
export EH_CONNECTION_STRING=[YOUR_EVENT_HUB_CONNECTION_STRING_HERE]
export STORAGE_ACCOUNT_ACCESS_KEY=[YOUR_STORAGE_ACCOUNT_ACCESS_KEY_HERE]
export STORAGE_QUEUE_CONNECTION_STRING=[YOUR_STORAGE_QUEUE_CONNECTION_STRING_HERE]

# Create secrets
kubectl -n bc-shipping create secret generic scheduler-secrets --from-literal=eventhub_name=${INGESTION_EH_NAME} \
--from-literal=eventhub_sas_connection_string=${EH_CONNECTION_STRING} \
--from-literal=storageaccount_name=${SCHEDULER_STORAGE_ACCOUNT_NAME} \
--from-literal=storageaccount_key=${STORAGE_ACCOUNT_ACCESS_KEY} \
--from-literal=queueconstring=${STORAGE_QUEUE_CONNECTION_STRING}

# Deploy service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/scheduler.yaml
```

## Deploy mock services

Build the mock services

```bash
export MOCKS_PATH=microservices-reference-implementation/src/bc-shipping/delivery
docker-compose -f $MOCKS_PATH/docker-compose.ci.build.yml up
```

Build and publish the container image 

```bash
# Build the Docker image
docker build -t $ACR_SERVER/account:0.1.0 $MOCKS_PATH/MockAccountService/. && \
docker build -t $ACR_SERVER/dronescheduler:0.1.0 $MOCKS_PATH/MockDroneScheduler/. && \
docker build -t $ACR_SERVER/thirdparty:0.1.0 $MOCKS_PATH/MockThirdPartyService/. 

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/account:0.1.0 && \
docker push $ACR_SERVER/dronescheduler:0.1.0 && \
docker push $ACR_SERVER/thirdparty:0.1.0
```

Deploy the mock services:

```bash
# Update the image tag in the deployment YAML
sed -i "s#image:#image: $ACR_SERVER/account:0.1.0#g" ./microservices-reference-implementation/k8s/account.yaml && \
sed -i "s#image:#image: $ACR_SERVER/dronescheduler:0.1.0#g" ./microservices-reference-implementation/k8s/dronescheduler.yaml && \
sed -i "s#image:#image: $ACR_SERVER/thirdparty:0.1.0#g" ./microservices-reference-implementation/k8s/thirdparty.yaml 

# Deploy the service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/account.yaml && \
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/dronescheduler.yaml && \
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/thirdparty.yaml
```

## Verify all services are running:

```bash
kubectl get all -n bc-shipping
```

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
