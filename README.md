# Microservices  Reference Implementation
Microsoft patterns & practices

https://docs.microsoft.com/azure/architecture/microservices

---

## Prerequisites

- Azure suscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- Docker
- [Docker Compose](https://docs.docker.com/compose/install/)

Clone or download this repo locally.

```bash
git clone https://github.com/mspnp/microservices-reference-implementation.git
```

## Create the Kubernetes cluster

Set environment variables.

```bash
export LOCATION=your_location_here && \
export RESOURCE_GROUP=your_resource_group_here && \
export CLUSTER_NAME=your_cluster_name_here && 
export RESOURCE_GROUP_SVC=services_resource_group_here
```

Provision a Kubernetes cluster in ACS

```bash
# Log in to Azure
az login

# Create a resource group for ACS
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create a resource group for other Azure services
az group create --name $RESOURCE_GROUP_SVC --location $LOCATION

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
export DELIVERY_SERVICE_PREFIX=delivery_service_prefix_here && \
export REDIS_NAME="${DELIVERY_SERVICE_PREFIX}-redis" && \
export COSMOSDB_NAME="${DELIVERY_SERVICE_PREFIX}-cosmosdb" && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col"

# Create a resource group
az group create --name $RESOURCE_GROUP_DELIVERY --location $LOCATION

# Create Azure Redis Cache
az redis create --location $LOCATION \
            --name $REDIS_NAME \
            --resource-group $RESOURCE_GROUP_SVC \
            --sku Premium \
            --vm-size P4

# Create Cosmos DB account with DocumentDB API
az cosmosdb create \
    --name $COSMOSDB_NAME \
    --kind GlobalDocumentDB \
    --resource-group $RESOURCE_GROUP_SVC \
    --max-interval 10 \
    --max-staleness-prefix 200 

# Create a Cosmos DB database 
az cosmosdb database create \
    --name $COSMOSDB_NAME \
    --db-name=$DATABASE_NAME \
    --resource-group $RESOURCE_GROUP_SVC

# Create a Cosmos DB collection
az cosmosdb collection create \
    --collection-name $COLLECTION_NAME \
    --name $COSMOSDB_NAME \
    --db-name $DATABASE_NAME \
    --resource-group $RESOURCE_GROUP_SVC
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

# Update the deployment YAML with the image tag.
sed -i "s#image:#image: $ACR_SERVER/fabrikam.dronedelivery.deliveryservice:0.1.0#g" ./microservices-reference-implementation/k8s/delivery.yaml
```

Create Kubernetes secrets

```bash
export COSMOSDB_KEY=$(az cosmosdb list-keys --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP_SVC --query primaryMasterKey) && \
export COSMOSDB_ENDPOINT=$(az cosmosdb show --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP_SVC --query documentEndpoint) && \
export REDIS_HOSTNAME=$(az redis show -n $REDIS_NAME -g $RESOURCE_GROUP_SVC --query hostName) && \
export REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP_SVC --query primaryKey)

kubectl --namespace bc-shipping create --save-config=true secret generic delivery-storageconf \
    --from-literal=CosmosDB_Key=${COSMOSDB_KEY[@]//\"/} \
    --from-literal=CosmosDB_Endpoint=${COSMOSDB_ENDPOINT[@]//\"/} \
    --from-literal=Redis_HostName=${REDIS_HOSTNAME[@]//\"/} \
    --from-literal=Redis_PrimaryKey=${REDIS_KEY[@]//\"/} \
    --from-literal=EH_ConnectionString= \
    --from-literal=Redis_SecondaryKey=
```

Update Delivery service YAML with environment variables for database and collection name.

```bash
sed -i "s/value: \"CosmosDB_DatabaseId\"/value: $DATABASE_NAME/g" "./microservices-reference-implementation/k8s/delivery.yaml" && \
sed -i "s/value: \"CosmosDB_CollectionId\"/value: $COLLECTION_NAME/g"  "./microservices-reference-implementation/k8s/delivery.yaml" && \
sed -i "s/value: \"EH_EntityPath\"/value:/g"                               "./microservices-reference-implementation/k8s/delivery.yaml"
```

Deploy the Delivery service:

```bash
kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/
```

Verify the service is running:

```bash
kubectl get all -n bc-shipping
```

## Deploy the Package service

TBD

### Deploy the Ingestion and Scheduler services 

TBD

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
