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
export LOCATION=your_location_here && \
export UNIQUE_APP_NAME_PREFIX=you_unique_application_name_here && \
export RESOURCE_GROUP="${UNIQUE_APP_NAME_PREFIX}-rg" && \
export CLUSTER_NAME="${UNIQUE_APP_NAME_PREFIX}-cluster" && \
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
export ACR_NAME=your_container_registry_name_here

# Create the ACR instance
az acr create --name $ACR_NAME --resource-group $RESOURCE_GROUP --sku Basic

# Log in to ACR
az acr login --name $ACR_NAME

# Get the ACR login server name
export ACR_SERVER=$(az acr show -g $RESOURCE_GROUP -n $ACR_NAME --query "loginServer")

# Strip quotes
export ACR_SERVER=("${ACR_SERVER[@]//\"/}")
```

Deploy Prometheus, Grafana [TBD]

Deploy Elasticsearch [TBD]

Deploy linkerd. For more information, see https://linkerd.io/getting-started/k8s/


## Deploy the Delivery service

Provision Azure resources

```bash
export REDIS_NAME="${UNIQUE_APP_NAME_PREFIX}-delivery-service-redis" && \
export COSMOSDB_NAME="${UNIQUE_APP_NAME_PREFIX}-delivery-service-cosmosdb" && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col"

# Create a resource group
az group create --name $RESOURCE_GROUP_DELIVERY --location $LOCATION

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

```
docker-compose -f ./microservices-reference-implementation/src/bc-shipping/delivery/docker-compose.ci.build.yml up
```

Build and publish the container image 

```bash
# Build the Docker image
docker build -t $ACR_SERVER/fabrikam.dronedelivery.deliveryservice:0.1.0 ./microservices-reference-implementation/src/bc-shipping/delivery/Fabrikam.DroneDelivery.DeliveryService/.

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
sed -i "s/value: \"CosmosDB_DatabaseId\"/value: $DATABASE_NAME/g" "./microservices-reference-implementation/k8s/delivery.yaml" && \
sed -i "s/value: \"CosmosDB_CollectionId\"/value: $COLLECTION_NAME/g"  "./microservices-reference-implementation/k8s/delivery.yaml" && \
sed -i "s/value: \"EH_EntityPath\"/value:/g"                               "./microservices-reference-implementation/k8s/delivery.yaml"

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
docker-compose -f $PACKAGE_PATH/docker-compose.ci.build.yml up

# Build the docker image
docker build -f $PACKAGE_PATH/build/prod.dockerfile -t $ACR_SERVER/package-service:0.1.0 $PACKAGE_PATH

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
export INGESTION_EH_NS=ingestion_event_hub_namespace_here && \
export INGESTION_EH_NAME=ingestion_event_hub_name_here && \
export INGESTION_EH_CONSUMERGROUP_NAME=ingestion_event_hub_consumerGroup_name_here
wget https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/201-event-hubs-create-event-hub-and-consumer-group/azuredeploy.json && \
sed -i 's#"partitionCount": "4"#"partitionCount": "32"#g' azuredeploy.json && \
az group deployment create -g $RESOURCE_GROUP_SVC --template-file azuredeploy.json  --parameters \
'{ \
  "namespaceName": {"value": "'${INGESTION_EH_NS}'"}, \
  "eventHubName": {"value": "'${INGESTION_EH_NAME}'"}, \
  "consumerGroupName": {"value": "'${INGESTION_EH_CONSUMERGROUP_NAME}'"} \
}'
```
Note: you could also create this from [the Azure Portal](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-create)

Build the Ingestion service

```bash
export INGESTION_PATH=microservices-reference-implementation/src/bc-shipping/ingestion
export SOURCE=folderName_on_maven_container_for_ingestion
export FULLPATHSOURCE=fullpath_for_ingestion_folder
export JAR_BUILD_IMAGE=imageName_to_build_java_jar

note:
SOURCE is a folder name of your choice without back or forward slashes
FULLPATHSOURCE is the full path of ingestion folder
JAR_BUILD_IMAGE is the image name of your choice, that will run maven and compile java source into jar binaries

# Build the image that compiles java source code and generates the java jar binaries
cd FULLPATHSOURCE

docker  build -t $JAR_BUILD_IMAGE:1 -f  microservices-reference-implementation/src/bc-shipping/ingestion/Dockerfilemaven .

# Build the app. this will gerate jars under target folder
docker run -it --rm -e  SOURCEPATH=/$SOURCE -v $FULLPATHSOURCE:/$SOURCE -v $FULLPATHSOURCE/target:/$SOURCE/target $JAR_BUILD_IMAGE:1

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

# Create secret
kubectl -n bc-shipping create secret generic ingestion-secrets --from-literal=eventhub_namespace=${INGESTION_EH_NS} \
--from-literal=eventhub_name=${INGESTION_EH_NAME} \
--from-literal=eventhub_keyname=your_sas_token_name_here \ # Azure Portal --> Event Hubs --> select your event hub namespace --> shared keys
--from-literal=eventhub_keyvalue=your_sas_token_key_here # Azure Portal --> Event Hubs --> select your event hub namespace --> shared keys

# Deploy service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/ingestion.yaml
```

## Deploy the Scheduler service 

Provision Azure resources
```bash
export SCHEDULER_STORAGE_ACCOUNT_NAME=ingestion_storage_account_name_here 
az storage account create --resource-group $RESOURCE_GROUP_SVC --name $SCHEDULER_STORAGE_ACCOUNT_NAME --sku Standard_LRS
```

Build the Scheduler service

```bash
export SCHEDULER_PATH=microservices-reference-implementation/src/bc-shipping/scheduler
export SOURCE=folderName_on_maven_container_for_scheduler
export FULLPATHSOURCE=fullpath_for_scheduler_folder
export JAR_BUILD_IMAGE=imageName_to_build_java_jar. you can reuse the same image create in ingestiion

note:
SOURCE is a folder name of your choice without back or forward slashes
FULLPATHSOURCE is the full path of scheduler folder
JAR_BUILD_IMAGE is the image name of your choice, that will run maven and compile java source into jar binaries
this image can be reused from the ingestion service step

# Build the app. this will gerate jars under target folder
docker run -it --rm -e  SOURCEPATH=/$SOURCE -v $FULLPATHSOURCE:/$SOURCE -v $FULLPATHSOURCE/target:/$SOURCE/target $JAR_BUILD_IMAGE:1

# Build the docker image
docker build -f $SCHEDULER_PATH/Dockerfile -t $ACR_SERVER/scheduler:0.1.0 $SCHEDULER_PATH

Deploy the Scheduler service

# Update deployment YAML with image tage
sed -i "s#image:#image: $ACR_SERVER/scheduler:0.1.0#g" ./microservices-reference-implementation/k8s/scheduler.yaml

# Create secret
kubectl -n bc-shipping create secret generic scheduler-secrets --from-literal=eventhub_name=${INGESTION_EH_NAME} \
--from-literal=eventhub_sas_connection_string=your_sas_connection_string_here \ # Azure Portal --> Event Hubs --> select your event hub namespace --> shared keys
--from-literal=storageaccount_name=${SCHEDULER_STORAGE_ACCOUNT_NAME} \  
--from-literal=storageaccount_key=your_storageaccount_accesskeys_here # Azure Portal --> Storage Account --> select your storage account --> access keys 

# Deploy service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/scheduler.yaml
```

## Deploy mock services

Build the mock services

```
docker-compose -f ./microservices-reference-implementation/src/bc-shipping/delivery/docker-compose.ci.build.yml up
```

Build and publish the container image 

```bash
# Build the Docker image
docker build -t $ACR_SERVER/account:0.1.0 ./microservices-reference-implementation/src/bc-shipping/delivery/MockAccountService/. && \
docker build -t $ACR_SERVER/dronescheduler:0.1.0 ./microservices-reference-implementation/src/bc-shipping/delivery/MockDroneScheduler/. && \
docker build -t $ACR_SERVER/thirdparty:0.1.0 ./microservices-reference-implementation/src/bc-shipping/delivery/MockThirdPartyService/. 

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/account:0.1.0 && \
docker push $ACR_SERVER/dronescheduler:0.1.0 && \
docker push $ACR_SERVER/thridparty:0.1.0
```

Deploy the mock services:

```bash
# Update the image tag in the deployment YAML
sed -i "s#image:#image: $ACR_SERVER/account:0.1.0#g" ./microservices-reference-implementation/k8s/account.yaml && \
sed -i "s#image:#image: $ACR_SERVER/dronescheduler:0.1.0#g" ./microservices-reference-implementation/k8s/dronescheduler.yaml && \
sed -i "s#image:#image: $ACR_SERVER/thirdparty:0.1.0#g" ./microservices-reference-implementation/k8s/thirdparty.yaml 

# Deploy the service
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/account.yaml ./microservices-reference-implementation/k8s/dronescheduler.yaml ./microservices-reference-implementation/k8s/thirdparty.yaml
```

## Verify all services are running:

```bash
kubectl get all -n bc-shipping
```

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
