# Deploying the Reference Implementation

## Prerequisites

- Azure suscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/)
- [Helm 2.12.3 or later](https://docs.helm.sh/using_helm/#installing-helm)
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
export SSH_PRIVATE_KEY_FILE=[YOUR_RECENTLY_GENERATED_SSH_RSA_PRIVAYE_KEY_FILE_HERE]

export LOCATION=[YOUR_LOCATION_HERE]

export RESOURCE_GROUP=[YOUR_RESOURCE_GROUP_HERE]

export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)

export PROJECT_ROOT=./microservices-reference-implementation
export K8S=$PROJECT_ROOT/k8s
export HELM_CHARTS=$PROJECT_ROOT/charts
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

```bash
# Deploy the managed identities
# These are deployed first in a separate template to avoid propagation delays with AAD
az group deployment create -g $RESOURCE_GROUP --name azuredeploy-identities --template-file azuredeploy-identities.json
export DELIVERY_ID_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.deliveryIdName.value -o tsv)
export DELIVERY_ID_PRINCIPAL_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.deliveryPrincipalId.value -o tsv)
export DRONESCHEDULER_ID_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.droneSchedulerIdName.value -o tsv)
export DRONESCHEDULER_ID_PRINCIPAL_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.droneSchedulerPrincipalId.value -o tsv)
export WORKFLOW_ID_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.workflowIdName.value -o tsv)
export WORKFLOW_ID_PRINCIPAL_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.workflowPrincipalId.value -o tsv)

# Wait for AAD propagation
until az ad sp show --id ${DELIVERY_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id ${DRONESCHEDULER_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id ${WORKFLOW_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done

# Deploy all other resources
# The version of kubernetes must be supported in the target region
export KUBERNETES_VERSION='1.12.6'
az group deployment create -g $RESOURCE_GROUP --name azuredeploy --template-file azuredeploy.json \
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
            workflowPrincipalId=${WORKFLOW_ID_PRINCIPAL_ID}
```

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

Setup Helm in the container

```bash
kubectl apply -f $K8S/tiller-rbac.yaml
helm init --service-account tiller
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

Note: the tested nmi version was 1.4. It enables namespaced pod identity.

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
export DELIVERY_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.deliveryPrincipalResourceId.value -o tsv) && \
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.deliveryPrincipalClientId.value -o tsv)

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

Extract resource details from deployment

```bash
export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.packageMongoDbName.value -o tsv)
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
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g')

# Deploy service
helm install $HELM_CHARTS/package/ \
     --set image.tag=0.1.0 \
     --set image.repository=package \
     --set secrets.appinsights.ikey=$AI_IKEY \
     --set secrets.mongo.pwd=$COSMOSDB_CONNECTION \
     --set dockerregistry=$ACR_SERVER \
     --namespace backend \
     --name package-v0.1.0

# Verify the pod is created
helm status package-v0.1.0
```

## Deploy the Workflow service

Extract resource details from deployment

```bash
export WORKFLOW_KEYVAULT_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.workflowKeyVaultName.value -o tsv)
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
export WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.workflowPrincipalResourceId.value -o tsv) && \
export WORKFLOW_PRINCIPAL_CLIENT_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-identities --query properties.outputs.workflowPrincipalClientId.value -o tsv)
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

Extract resource details from deployment

```bash
export INGESTION_QUEUE_NAMESPACE=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionQueueNamespace.value -o tsv) && \
export INGESTION_QUEUE_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_ACCESS_KEY_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.ingestionServiceAccessKeyName.value -o tsv)
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
    --from-literal=queue_keyname=${INGESTION_ACCESS_KEY_NAME} \
    --from-literal=queue_keyvalue=${INGESTION_ACCESS_KEY_VALUE} \
    --from-literal=appinsights-ikey=${AI_IKEY}

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
export DRONE_PATH=$PROJECT_ROOT/src/shipping/dronescheduler
```

Create and set up pod identity

```bash
# Extract outputs from deployment
export DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv) && \
export DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy --query properties.outputs.droneSchedulerPrincipalClientId.value -o tsv)
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
