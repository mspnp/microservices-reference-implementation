# Deploying the Reference Implementation



## Prerequisites

- Azure suscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/install/)

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

export SP_CLIENT_SECRET=$(uuidgen)
```

Infrastructure Prerequisites

```bash
# Log in to Azure
az login

# Create a resource group and service principal for ACS
az group create --name $RESOURCE_GROUP --location $LOCATION && \
export SP_APP_ID=$(az ad sp create-for-rbac --role="Contributor" -p $SP_CLIENT_SECRET | grep -oP '(?<="appId": ")[^"]*') && \
export BEARER_TOKEN=$(az account get-access-token --query accessToken | sed -e 's/^"//' -e 's/"$//') && \
export SUBS_ID=$(az account show --query id | sed -e 's/^"//' -e 's/"$//')
```

Deployment

> Note: this deployment might take up to 20 minutes

* using Azure CLI 2.0

  ```bash
  az group deployment create -g $RESOURCE_GROUP --name azuredeploy --template-file azuredeploy.json \
  --parameters servicePrincipalClientId=${SP_APP_ID} \
             servicePrincipalClientSecret=${SP_CLIENT_SECRET} \
             sshRSAPublicKey="$(cat ${SSH_PUBLIC_KEY_FILE})"
  ```

* from Azure Portal

  [![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmspnp%2Fmicroservices-reference-implementation%2Fmaster%2Fazuredeploy.json)
  > Note:
  > 1. paste the $RESOURCE_GROUP value in the resource group field. Important: choose use existing resource group
  > 2. paste the content of your ssh-rsa public key file in the Ssh RSA Plublic Key field.
  > 3. paste the $SP_APP_ID and $SP_CLIENT_SECRET in the Client Id and Secret fields.

Get outputs from Azure Deploy
```bash
# Shared
export ACR_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.acrName.value | sed -e 's/^"//' -e 's/"$//') && \
export ACR_SERVER=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.acrLoginServer.value | sed -e 's/^"//' -e 's/"$//') && \
export CLUSTER_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.acsK8sClusterName.value | sed -e 's/^"//' -e 's/"$//')
```

Download kubectl and create a k8s namespace
```bash
#  Install kubectl
sudo az acs kubernetes install-cli

# Get the Kubernetes cluster credentials
az acs kubernetes get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME \
    --ssh-key-file $SSH_PRIVATE_KEY_FILE

# Create the BC namespaces
kubectl create namespace shipping && \
kubectl create namespace accounts && \
kubectl create namespace dronemgmt && \
kubectl create namespace 3rdparty
```

## Deploy the Delivery service

Build the Delivery service

```bash
export DELIVERY_PATH=./microservices-reference-implementation/src/shipping/delivery
docker-compose -f $DELIVERY_PATH/docker-compose.ci.build.yml up
```

Build and publish the container image

```bash
# Build the Docker image
docker build --pull --compress -t $ACR_SERVER/fabrikam.dronedelivery.deliveryservice:0.1.0 $DELIVERY_PATH/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/fabrikam.dronedelivery.deliveryservice:0.1.0

```

Create Kubernetes secrets

```bash
export REDIS_ENDPOINT=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query hostName -o tsv)
export REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey -o tsv)

export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.deliveryCosmosDbName.value | sed -e 's/^"//' -e 's/"$//') && \
export COSMOSDB_KEY=$(az cosmosdb list-keys --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query primaryMasterKey) && \
export COSMOSDB_ENDPOINT=$(az cosmosdb show --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query documentEndpoint)

kubectl --namespace shipping create --save-config=true secret generic delivery-storageconf \
    --from-literal=CosmosDB_Key=${COSMOSDB_KEY[@]//\"/} \
    --from-literal=CosmosDB_Endpoint=${COSMOSDB_ENDPOINT[@]//\"/} \
    --from-literal=Redis_Endpoint=${REDIS_ENDPOINT} \
    --from-literal=Redis_AccessKey=${REDIS_KEY} \
    --from-literal=EH_ConnectionString=
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
kubectl --namespace shipping apply -f ./microservices-reference-implementation/k8s/delivery.yaml
```

## Deploy the Package service

Build the Package service

```bash
export PACKAGE_PATH=microservices-reference-implementation/src/shipping/package

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
export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.packageMongoDbName.value | sed -e 's/^"//' -e 's/"$//') && \
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString")
kubectl -n shipping create secret generic package-secrets --from-literal=mongodb-pwd=${COSMOSDB_CONNECTION[@]//\"/}

# Deploy service
kubectl --namespace shipping apply -f ./microservices-reference-implementation/k8s/package.yml
```

## Deploy the Ingestion service

Build the Ingestion service

```bash
export INGESTION_PATH=./microservices-reference-implementation/src/shipping/ingestion

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
export INGESTION_EH_NS=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionEHNamespace.value | sed -e 's/^"//' -e 's/"$//') && \
export INGESTION_EH_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionEHName.value | sed -e 's/^"//' -e 's/"$//') && \
export EH_KEYS=$(curl -X POST "https://management.azure.com/subscriptions/${SUBS_ID}/resourceGroups/${RESOURCE_GROUP}/providers/Microsoft.EventHub/namespaces/${INGESTION_EH_NS}/AuthorizationRules/RootManageSharedAccessKey/ListKeys?api-version=2017-04-01" -H "Content-Type: application/json" -H "Authorization: Bearer ${BEARER_TOKEN}" -H "Content-Length: 0" --stderr - --silent) && \
export EH_ACCESS_KEY_NAME=$(echo $EH_KEYS | grep -oP '(?<="keyName":")[^"]*') && \
export EH_ACCESS_KEY_VALUE=$(echo $EH_KEYS | grep -oP '(?<="primaryKey":")[^"]*') && \
export EH_CONNECTION_STRING=$(echo $EH_KEYS | grep -oP '(?<="primaryConnectionString":")[^"]*')

# Create secret
kubectl -n shipping create secret generic ingestion-secrets --from-literal=eventhub_namespace=${INGESTION_EH_NS} \
--from-literal=eventhub_name=${INGESTION_EH_NAME} \
--from-literal=eventhub_keyname=${EH_ACCESS_KEY_NAME} \
--from-literal=eventhub_keyvalue=${EH_ACCESS_KEY_VALUE}

# Deploy service
kubectl --namespace shipping apply -f ./microservices-reference-implementation/k8s/ingestion.yaml
```

## Deploy the Scheduler service

Build the Scheduler service

```bash
export SCHEDULER_PATH=./microservices-reference-implementation/src/shipping/scheduler

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
export SCHEDULER_STORAGE_ACCOUNT_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.schedulerStorageAccountName.value | sed -e 's/^"//' -e 's/"$//') && \
export EH_KEYS=$(curl -X POST "https://management.azure.com/subscriptions/${SUBS_ID}/resourceGroups/${RESOURCE_GROUP}/providers/Microsoft.EventHub/namespaces/${INGESTION_EH_NS}/AuthorizationRules/RootManageSharedAccessKey/ListKeys?api-version=2017-04-01" -H "Content-Type: application/json" -H "Authorization: Bearer ${BEARER_TOKEN}" -H "Content-Length: 0" --stderr - --silent) && \
export EH_CONNECTION_STRING=$(echo $EH_KEYS | grep -oP '(?<="primaryConnectionString":")[^"]*')

export STORAGE_ACCOUNT_ACCESS_KEY=[YOUR_STORAGE_ACCOUNT_ACCESS_KEY_HERE]
export STORAGE_ACCOUNT_CONNECTION_STRING="[YOUR_STORAGE_ACCOUNT_CONNECTION_STRING_HERE]"

# Create secrets
kubectl -n shipping create secret generic scheduler-secrets --from-literal=eventhub_name=${INGESTION_EH_NAME} \
--from-literal=eventhub_sas_connection_string=${EH_CONNECTION_STRING} \
--from-literal=storageaccount_name=${SCHEDULER_STORAGE_ACCOUNT_NAME} \
--from-literal=storageaccount_key=${STORAGE_ACCOUNT_ACCESS_KEY} \
--from-literal=queueconstring=${STORAGE_ACCOUNT_CONNECTION_STRING}

# Deploy service
kubectl --namespace shipping apply -f ./microservices-reference-implementation/k8s/scheduler.yaml
```

## Deploy mock services

Build the mock services

```bash
export MOCKS_PATH=microservices-reference-implementation/src/shipping/delivery
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
kubectl --namespace accounts apply -f ./microservices-reference-implementation/k8s/account.yaml && \
kubectl --namespace dronemgmt apply -f ./microservices-reference-implementation/k8s/dronescheduler.yaml && \
kubectl --namespace 3rdparty apply -f ./microservices-reference-implementation/k8s/thirdparty.yaml
```

## Verify all services are running:

```bash
kubectl get all --all-namespaces -l co=fabrikam
```

Deploy Elasticsearch. For more information, see https://github.com/kubernetes/examples/tree/master/staging/elasticsearch

```bash
kubectl --namespace kube-system apply -f https://raw.githubusercontent.com/kubernetes/examples/master/staging/elasticsearch/service-account.yaml && \
kubectl --namespace kube-system apply -f https://raw.githubusercontent.com/kubernetes/examples/master/staging/elasticsearch/es-svc.yaml && \
kubectl --namespace kube-system apply -f https://raw.githubusercontent.com/kubernetes/examples/master/staging/elasticsearch/es-rc.yaml
```

Deploy Fluend. For more information, see https://docs.fluentd.org/v0.12/articles/kubernetes-fluentd

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

#### Deploy linkerd

For more information, see [https://linkerd.io/getting-started/k8s/](https://linkerd.io/getting-started/k8s/)

> Note:
> the service mesh configuration linked above is defaulting the namespace to "default" for service discovery.
> Since Drone Delivery microservices are getting deployed into several custom namespaces, this config needs to be modified. This will consist of a small change in the dtab rules.

Deploy linkerd defaulting the namespace to shipping instead:

```bash
wget https://raw.githubusercontent.com/linkerd/linkerd-examples/master/k8s-daemonset/k8s/servicemesh.yml && \
sed -i "s#/default#/shipping#g" servicemesh.yml && \
sed -i "149i \ \ \ \ \ \ \ \ /svc/account => /svc/account.accounts ;" servicemesh.yml && \
sed -i "149i \ \ \ \ \ \ \ \ /svc/dronescheduler => /svc/dronescheduler.dronemgmt ;" servicemesh.yml && \
sed -i "149i \ \ \ \ \ \ \ \ /svc/thirdparty => /svc/thirdparty.3rdparty ;" servicemesh.yml && \
sed -i "176i \ \ \ \ \ \ \ \ /svc/account => /svc/account.accounts ;" servicemesh.yml && \
sed -i "176i \ \ \ \ \ \ \ \ /svc/dronescheduler => /svc/dronescheduler.dronemgmt ;" servicemesh.yml && \
sed -i "176i \ \ \ \ \ \ \ \ /svc/thirdparty => /svc/thirdparty.3rdparty ;" servicemesh.yml && \
kubectl apply -f servicemesh.yml
```

Deploy Prometheus and Grafana. For more information, see https://github.com/linkerd/linkerd-viz#kubernetes-deploy

It is recommended to put an API Gateway in front of all APIs you want exposed to the public,
however for convenience, we exposed the Ingestion service with a public IP address.

You can send delivery requests to the ingestion service using the swagger ui.

```bash
export INGESTION_SERVICE_EXTERNAL_IP_ADDRESS=$(kubectl get --namespace shipping svc ingestion -o jsonpath="{.status.loadBalancer.ingress[0].*}")
curl "http://${INGESTION_SERVICE_EXTERNAL_IP_ADDRESS}"/swagger-ui.html#/ingestion45controller/scheduleDeliveryAsyncUsingPOST
```
