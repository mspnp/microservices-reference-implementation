# Deploying the Reference Implementation

## Prerequisites

- Azure subscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)
- [Helm 2.12.3 or later](https://docs.helm.sh/using_helm/#installing-helm)

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
export CLUSTER_NAME="${UNIQUE_APP_NAME_PREFIX}-cluster" && \
export AI_NAME="${UNIQUE_APP_NAME_PREFIX}-appinsights"

export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)

export PROJECT_ROOT=./microservices-reference-implementation
export K8S=$PROJECT_ROOT/k8s
export HELM_CHARTS=$PROJECT_ROOT/charts
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

Setup Helm in the container

```bash
kubectl apply -f $K8S/tiller-rbac.yaml
helm init --service-account tiller
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
export ACR_ID=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "id" --output tsv)

# Grant the cluster read access to the registry
az role assignment create --assignee $CLUSTER_SERVICE_PRINCIPAL --role Reader --scope $ACR_ID
```

Create an Application Insights instance

```bash
# Create AppInsights instance
az resource create \
   --resource-group $RESOURCE_GROUP \
   --resource-type "Microsoft.Insights/components" \
   --name $AI_NAME \
   --location $LOCATION \
   --properties '{"Application_Type":"other"}'

# Acquire Instrumentation Key
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

Create KeyVault and secrets

```bash
# Create the vault
export DELIVERY_KEYVAULT_NAME="${UNIQUE_APP_NAME_PREFIX}-delivery-kv"
az keyvault create --name $DELIVERY_KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION

export DELIVERY_KEYVAULT_ID=$(az keyvault show --resource-group $RESOURCE_GROUP --name $DELIVERY_KEYVAULT_NAME --query "id" --output tsv)
export DELIVERY_KEYVAULT_URI=$(az keyvault show --resource-group $RESOURCE_GROUP --name $DELIVERY_KEYVAULT_NAME --query "properties.vaultUri" --output tsv)

# Retrieve resource details
export REDIS_ENDPOINT=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query hostName -o tsv)
export REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey -o tsv)
export COSMOSDB_KEY=$(az cosmosdb list-keys --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query primaryMasterKey -o tsv)
export COSMOSDB_ENDPOINT=$(az cosmosdb show --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query documentEndpoint -o tsv)

# Create secrets
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name CosmosDB-Key --value ${COSMOSDB_KEY} # (consider using encoding base64 to keep the actual values)
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name CosmosDB-Endpoint --value ${COSMOSDB_ENDPOINT}
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name Redis-Endpoint --value ${REDIS_ENDPOINT}
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name Redis-AccessKey --value ${REDIS_KEY}
az keyvault secret set --vault-name $DELIVERY_KEYVAULT_NAME --name ApplicationInsights--InstrumentationKey --value ${AI_IKEY}
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
az keyvault set-policy --name $DELIVERY_KEYVAULT_NAME --secret-permissions get list --spn $DELIVERY_PRINCIPAL_CLIENT_ID

# Allow the cluster to manage the identity to assign to pods
az role assignment create --role "Managed Identity Operator" --assignee $CLUSTER_SERVICE_PRINCIPAL --scope $DELIVERY_PRINCIPAL_RESOURCE_ID
```

Deploy the Delivery service:

```bash
# Deploy the service
helm install $HELM_CHARTS/delivery/ \
     --set image.tag=0.1.0 \
     --set image.repository=delivery \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$DELIVERY_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$DELIVERY_PRINCIPAL_RESOURCE_ID \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set keyvault.uri=$DELIVERY_KEYVAULT_URI \
     --namespace backend \
     --name delivery-v0.1.0

# Verify the pod is created
helm status delivery-v0.1.0
```

## Deploy the Package service

Provision Azure resources

```bash
export COSMOSDB_NAME="${UNIQUE_APP_NAME_PREFIX}-package-service-cosmosdb"
az cosmosdb create --name $COSMOSDB_NAME --kind MongoDB --resource-group $RESOURCE_GROUP
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
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g')
kubectl -n backend create \
                   secret generic package-secrets \
                   --from-literal=mongodb-pwd=$COSMOSDB_CONNECTION \
                   --from-literal=appinsights-ikey=$AI_IKEY

# Deploy service
helm install $HELM_CHARTS/package/ \
     --set image.tag=0.1.0 \
     --set image.repository=package \
     --set dockerregistry=$ACR_SERVER \
     --namespace backend \
     --name package-v0.1.0

# Verify the pod is created
helm status package-v0.1.0
```

## Deploy the Workflow service

Provision Azure resources

```bash
export INGESTION_QUEUE_NAMESPACE="${UNIQUE_APP_NAME_PREFIX}-ingestion-ns"
export INGESTION_QUEUE_NAME="${UNIQUE_APP_NAME_PREFIX}-ingestion"

az servicebus namespace create --location $LOCATION \
            --name $INGESTION_QUEUE_NAMESPACE \
            --resource-group $RESOURCE_GROUP \
            --sku Standard

az servicebus queue create --name $INGESTION_QUEUE_NAME \
                           --namespace-name $INGESTION_QUEUE_NAMESPACE \
                           --resource-group $RESOURCE_GROUP

export INGESTION_QUEUE_ID=$(az servicebus queue show --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_QUEUE_NAMESPACE --name $INGESTION_QUEUE_NAME --query "id" --output tsv)
export INGESTION_QUEUE_NAMESPACE_ID=$(az servicebus namespace show --resource-group $RESOURCE_GROUP --name $INGESTION_QUEUE_NAMESPACE --query "id" --output tsv)
export INGESTION_QUEUE_NAMESPACE_ENDPOINT=$(az servicebus namespace show --resource-group $RESOURCE_GROUP --name $INGESTION_QUEUE_NAMESPACE --query "serviceBusEndpoint" --output tsv)
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


Create KeyVault and secrets

```bash
export WORKFLOW_KEYVAULT_NAME="${UNIQUE_APP_NAME_PREFIX}-workflow-kv"
az keyvault create --name $WORKFLOW_KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION
az keyvault secret set --vault-name $WORKFLOW_KEYVAULT_NAME --name QueueName --value ${INGESTION_QUEUE_NAME}
az keyvault secret set --vault-name $WORKFLOW_KEYVAULT_NAME --name QueueEndpoint --value ${INGESTION_QUEUE_NAMESPACE_ENDPOINT}
az keyvault secret set --vault-name $WORKFLOW_KEYVAULT_NAME --name ApplicationInsights-InstrumentationKey --value ${AI_IKEY}

export WORKFLOW_KEYVAULT_ID=$(az keyvault show --resource-group $RESOURCE_GROUP --name $WORKFLOW_KEYVAULT_NAME --query "id" --output tsv)
export WORKFLOW_KEYVAULT_URI=$(az keyvault show --resource-group $RESOURCE_GROUP --name $WORKFLOW_KEYVAULT_NAME --query "properties.vaultUri" --output tsv)
```

Create and set up pod identity


> Note: after creating the identity, please wait for some time before assigning Reader role to the Workflow Principal Id. Otherwise it's possible to experience the following error: ```No matches in graph database for 'your-principal-id'```

```bash
# Create the identity and extract properties
export WORKFLOW_PRINCIPAL_NAME="workflow"
az identity create --resource-group $RESOURCE_GROUP --name $WORKFLOW_PRINCIPAL_NAME
export WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $WORKFLOW_PRINCIPAL_NAME --query "id" --output tsv)
export WORKFLOW_PRINCIPAL_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $WORKFLOW_PRINCIPAL_NAME --query "principalId" --output tsv)
export WORKFLOW_PRINCIPAL_CLIENT_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $WORKFLOW_PRINCIPAL_NAME --query "clientId" --output tsv)

# Grant the identity access to the KeyVault
az role assignment create --role Reader --assignee $WORKFLOW_PRINCIPAL_ID --scope $WORKFLOW_KEYVAULT_ID
az keyvault set-policy --name $WORKFLOW_KEYVAULT_NAME --secret-permissions get list --spn $WORKFLOW_PRINCIPAL_CLIENT_ID

# Grant the identity access to the ingestion queue
az role assignment create --role Contributor --assignee $WORKFLOW_PRINCIPAL_ID --scope $INGESTION_QUEUE_NAMESPACE_ID

# Allow the cluster to manage the identity to assign to pods
az role assignment create --role "Managed Identity Operator" --assignee $CLUSTER_SERVICE_PRINCIPAL --scope $WORKFLOW_PRINCIPAL_RESOURCE_ID
```

Deploy the Workflow service:

```bash
# Deploy the service
helm install $HELM_CHARTS/workflow/ \
     --set image.tag=0.1.0 \
     --set image.repository=workflow \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$WORKFLOW_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$WORKFLOW_PRINCIPAL_RESOURCE_ID \
     --set keyvault.name=$WORKFLOW_KEYVAULT_NAME \
     --set keyvault.resourcegroup=$RESOURCE_GROUP \
     --set keyvault.subscriptionid=$SUBSCRIPTION_ID \
     --set keyvault.tenantid=$TENANT_ID \
     --namespace backend \
     --name workflow-v0.1.0

# Verify the pod is created
helm status workflow-v0.1.0
```

## Deploy the Ingestion service

Provision Azure resources

```bash
# Create authorization rule to the ingestion queue
az servicebus namespace authorization-rule create --namespace-name $INGESTION_QUEUE_NAMESPACE \
                                                --name IngestionServiceAccessKey \
                                                --resource-group $RESOURCE_GROUP \
                                                --rights Send

# Get access key
export INGESTION_ACCESS_KEY_VALUE=$(az servicebus namespace authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_QUEUE_NAMESPACE --name IngestionServiceAccessKey --query primaryKey -o tsv)
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
# Deploy the ngnix ingress controller
helm install stable/nginx-ingress --name nginx-ingress --namespace ingress-controllers --set rbac.create=true

# Obtain the load balancer ip address and assign a domain name
until export INGRESS_LOAD_BALANCER_IP=$(kubectl get services/nginx-ingress-controller -n ingress-controllers -o jsonpath="{.status.loadBalancer.ingress[0].ip}" 2> /dev/null) && test -n "$INGRESS_LOAD_BALANCER_IP"; do echo "Waiting for load balancer deployment" && sleep 20; done
export INGRESS_LOAD_BALANCER_IP_ID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$INGRESS_LOAD_BALANCER_IP')].[id]" --output tsv)
export EXTERNAL_INGEST_DNS_NAME="${UNIQUE_APP_NAME_PREFIX}-ingest"
export EXTERNAL_INGEST_FQDN=$(az network public-ip update --ids $INGRESS_LOAD_BALANCER_IP_ID --dns-name $EXTERNAL_INGEST_DNS_NAME --query "dnsSettings.fqdn" --output tsv)

# Create a self-signed certificate for TLS
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -out ingestion-ingress-tls.crt \
    -keyout ingestion-ingress-tls.key \
    -subj "/CN=${EXTERNAL_INGEST_FQDN}/O=fabrikam"

kubectl create secret tls ingestion-ingress-tls \
    --namespace backend \
    --key ingestion-ingress-tls.key \
    --cert ingestion-ingress-tls.crt

# Update deployment YAML with image tag and the fqdn
cat $K8S/ingestion.yaml | \
    sed "s#image:#image: $ACR_SERVER/ingestion:0.1.0#g" | \
    sed "s#ingestion-host-name#$EXTERNAL_INGEST_FQDN#g" \
    > $K8S/ingestion-0.yaml

# Create secret
kubectl -n backend create secret generic ingestion-secrets \
        --from-literal=queue_namespace=${INGESTION_QUEUE_NAMESPACE} \
        --from-literal=queue_name=${INGESTION_QUEUE_NAME} \
        --from-literal=queue_keyname=IngestionServiceAccessKey \
        --from-literal=queue_keyvalue=${INGESTION_ACCESS_KEY_VALUE} \
        --from-literal=appinsights-ikey=${AI_IKEY}

# Deploy service
kubectl --namespace backend apply -f $K8S/ingestion-0.yaml

# Verify the pod is created
kubectl get pods -n backend
```

## Deploy DroneScheduler service

Build the dronescheduler services

```bash
export DRONE_PATH=$PROJECT_ROOT/src/shipping/dronescheduler
```

Create KeyVault and secrets

```bash
export DRONESCHEDULER_KEYVAULT_NAME="${UNIQUE_APP_NAME_PREFIX}-drone-kv"
az keyvault create --name $DRONESCHEDULER_KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION
az keyvault secret set --vault-name $DRONESCHEDULER_KEYVAULT_NAME --name ApplicationInsights--InstrumentationKey --value ${AI_IKEY}

export DRONESCHEDULER_KEYVAULT_ID=$(az keyvault show --resource-group $RESOURCE_GROUP --name $DRONESCHEDULER_KEYVAULT_NAME --query "id" --output tsv)
export DRONESCHEDULER_KEYVAULT_URI=$(az keyvault show --resource-group $RESOURCE_GROUP --name $DRONESCHEDULER_KEYVAULT_NAME --query "properties.vaultUri" --output tsv)
```

Create and set up pod identity

```bash
# Create the identity and extract properties
export DRONESCHEDULER_PRINCIPAL_NAME=dronescheduler
az identity create --resource-group $RESOURCE_GROUP --name $DRONESCHEDULER_PRINCIPAL_NAME
export DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $DRONESCHEDULER_PRINCIPAL_NAME --query "id" --output tsv)
export DRONESCHEDULER_PRINCIPAL_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $DRONESCHEDULER_PRINCIPAL_NAME --query "principalId" --output tsv)
export DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az identity show --resource-group $RESOURCE_GROUP --name $DRONESCHEDULER_PRINCIPAL_NAME --query "clientId" --output tsv)

# Grant the identity access to the KeyVault
az role assignment create --role Reader --assignee $DRONESCHEDULER_PRINCIPAL_ID --scope $DRONESCHEDULER_KEYVAULT_ID
az keyvault set-policy --name $DRONESCHEDULER_KEYVAULT_NAME --secret-permissions get list --spn $DRONESCHEDULER_PRINCIPAL_CLIENT_ID

# Allow the cluster to manage the identity to assign to pods
az role assignment create --role "Managed Identity Operator" --assignee $CLUSTER_SERVICE_PRINCIPAL --scope $DRONESCHEDULER_PRINCIPAL_RESOURCE_ID
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
helm install $HELM_CHARTS/dronescheduler/ \
     --set image.tag=0.1.0 \
     --set image.repository=dronescheduler \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$DRONESCHEDULER_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$DRONESCHEDULER_PRINCIPAL_RESOURCE_ID \
     --set keyvault.uri=$DRONESCHEDULER_KEYVAULT_URI \
     --namespace backend \
     --name dronescheduler-v0.1.0

# Verify the pod is created
helm status dronescheduler-v0.1.0
```

## Validate the application is running

You can send delivery requests to the ingestion service using the Swagger UI.

Use a web browser to navigate to `https://[EXTERNAL_INGEST_FQDN]/swagger-ui.html#/ingestion45controller/scheduleDeliveryAsyncUsingPOST` and use the **Try it out** button to submit a delivery request.

```bash
open "https://$EXTERNAL_INGEST_FQDN/swagger-ui.html#/ingestion45controller/scheduleDeliveryAsyncUsingPOST"
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
