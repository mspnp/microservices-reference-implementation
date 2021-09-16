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
git clone --recurse-submodules https://github.com/mspnp/microservices-reference-implementation.git && \
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
export LOCATION=eastus2
export RESOURCE_GROUP=rg-shipping-microservices
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
export SP_CLIENT_SECRET=$(echo $SP_DETAILS | jq ".password" -r)

export DEPLOYMENT_SUFFIX=$(date +%S%N)
```

## Deployment

> Note: this deployment might take up to 20 minutes

Infrastructure

```bash
# Deploy the managed identities (This takes less than two  minutes.)

export PREREQS_DEPLOYMENT_NAME=workload-stamp-prereqs-main

az deployment sub create --name $PREREQS_DEPLOYMENT_NAME --location eastus2 --template-file ./workload/workload-stamp-prereqs.json --parameters resourceGroupName=$RESOURCE_GROUP resourceGroupLocation=$LOCATION

# Get the Azure Container Registry resource group name and the user identities

export WORKLOAD_PREREQS_DEPLOYMENT_NAME=workload-stamp-prereqs-dep  && \
export DELIVERY_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $WORKLOAD_PREREQS_DEPLOYMENT_NAME --query properties.outputs.deliveryIdName.value -o tsv) && \
export DELIVERY_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query principalId -o tsv) && \
export DRONESCHEDULER_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $WORKLOAD_PREREQS_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerIdName.value -o tsv) && \
export DRONESCHEDULER_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query principalId -o tsv) && \
export WORKFLOW_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $WORKLOAD_PREREQS_DEPLOYMENT_NAME --query properties.outputs.workflowIdName.value -o tsv) && \
export WORKFLOW_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query principalId -o tsv) && \
export PACKAGE_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $WORKLOAD_PREREQS_DEPLOYMENT_NAME --query properties.outputs.packageIdName.value -o tsv) && \
export PACKAGE_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $PACKAGE_ID_NAME --query principalId -o tsv) && \
export INGESTION_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $WORKLOAD_PREREQS_DEPLOYMENT_NAME --query properties.outputs.ingestionIdName.value -o tsv) && \
export INGESTION_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $INGESTION_ID_NAME --query principalId -o tsv) && \
export RESOURCE_GROUP_ACR=$(az deployment sub show -n $PREREQS_DEPLOYMENT_NAME --query properties.outputs.acrResourceGroupName.value -o tsv)

# Wait for AAD propagation
until az ad sp show --id $DELIVERY_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $DRONESCHEDULER_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $WORKFLOW_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $INGESTION_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $PACKAGE_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done

# Export the kubernetes cluster version
export KUBERNETES_VERSION=$(az aks get-versions -l $LOCATION --query "orchestrators[?default!=null].orchestratorVersion" -o tsv)

# Deploy all the workload related resources  (This step takes about 10 minutes)
az deployment group create -f ./workload/workload-stamp.json -g $RESOURCE_GROUP -p droneSchedulerPrincipalId=$DRONESCHEDULER_ID_PRINCIPAL_ID -p workflowPrincipalId=$WORKFLOW_ID_PRINCIPAL_ID -p deliveryPrincipalId=$DELIVERY_ID_PRINCIPAL_ID -p ingestionPrincipalId=$INGESTION_ID_PRINCIPAL_ID -p packagePrincipalId=$PACKAGE_ID_PRINCIPAL_ID -p acrResourceGroupName=$RESOURCE_GROUP_ACR

# Get outputs from workload deploy
export ACR_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.acrName.value -o tsv) && \
export ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv)
```

The Secrets Store CSI Driver for Kubernetes allows for the integration of Azure Key Vault as a secrets store with a Kubernetes cluster via a CSI volume. Before creating a managed AKS cluster that can use the Secrets Store CSI Driver, you must enable the AKS-AzureKeyVaultSecretsProvider feature flag on your subscription.

```bash
# Register the AKS-AzureKeyVaultSecretsProvider feature flag by using the az feature register command

az feature register --namespace "Microsoft.ContainerService" --name "AKS-AzureKeyVaultSecretsProvider"

# It may take almost 30 minutes for the status to show Registered. Verify the registration status by using the az feature list command:

az feature list -o table --query "[?contains(name, 'Microsoft.ContainerService/AKS-AzureKeyVaultSecretsProvider')].{Name:name,State:properties.state}"

# When ready, refresh the registration of the Microsoft.ContainerService resource provider by using the az provider register command:

az provider register --namespace Microsoft.ContainerService
```

Deploy the managed cluster and all related resources (This step takes about 15 minutes)

```bash
export DEPLOYMENT_NAME=azuredeploy-$DEPLOYMENT_SUFFIX
az deployment group create -g $RESOURCE_GROUP --name $DEPLOYMENT_NAME --template-file azuredeploy.json \
--parameters servicePrincipalClientId=$SP_APP_ID \
            servicePrincipalClientSecret=$SP_CLIENT_SECRET \
            kubernetesVersion=$KUBERNETES_VERSION \
            sshRSAPublicKey="$(cat $SSH_PUBLIC_KEY_FILE)" \
            deliveryIdName=$DELIVERY_ID_NAME \
            ingestionIdName=$INGESTION_ID_NAME \
            packageIdName=$PACKAGE_ID_NAME \
            droneSchedulerIdName=$DRONESCHEDULER_ID_NAME \
            workflowIdName=$WORKFLOW_ID_NAME \
            acrResourceGroupName=$RESOURCE_GROUP_ACR \
            acrName=$ACR_NAME
```

Get the cluster name output from Azure Deploy.

```bash
export CLUSTER_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME --query properties.outputs.aksClusterName.value -o tsv)
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
# install helm 3
curl https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3 | bash
```

Integrate Application Insights instance.

```bash
# Acquire Instrumentation Key
export AI_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.appInsightsName.value -o tsv)
export AI_IKEY=$(az resource show -g $RESOURCE_GROUP -n $AI_NAME --resource-type "Microsoft.Insights/components" --query properties.InstrumentationKey -o tsv)

# add RBAC for AppInsights
kubectl apply -f k8s/k8s-rbac-ai.yaml
```

## Create a SecretProviderClass custom resource

Verify that the secrets-store pods are running in the kube-system namespace

```bash
kubectl get pods -n kube-system
```

You should see an output similar to this:

```bash
NAME                                     READY   STATUS    RESTARTS   AGE
aks-secrets-store-csi-driver-4bjzx       3/3     Running   2          28m
aks-secrets-store-csi-driver-b22bj       3/3     Running   1          28m
aks-secrets-store-provider-azure-2k5mx   1/1     Running   0          28m
aks-secrets-store-provider-azure-l5w98   1/1     Running   0          28m
```

To use and configure the Secrets Store CSI driver for your AKS cluster, create a SecretProviderClass custom resource for the Workflow and the Ingestion service.

```bash
export WORKFLOW_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.workflowKeyVaultName.value -o tsv)
export INGESTION_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.ingestionKeyVaultName.value -o tsv)
export PACKAGE_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.packageKeyVaultName.value -o tsv)
export DELIVERY_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.deliveryKeyVaultName.value -o tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)
export INGRESS_TLS_SECRET_NAME=ingestion-ingress-tls
export DELIVERY_INGRESS_TLS_SECRET_NAME=delivery-ingress-tls

# Create secrets
# Note: Ingress TLS key and certificate secrets cannot be exported as outputs in ARM deployments
# So we create an access policy to allow these secrets to be created imperatively.
# The policy is deleted right after the secret creation commands are executed
export SIGNED_IN_OBJECT_ID=$(az ad signed-in-user show --query 'objectId' -o tsv)

az keyvault set-policy --secret-permissions set --object-id $SIGNED_IN_OBJECT_ID -n $INGESTION_KEYVAULT_NAME
az keyvault secret set --name Ingestion-Ingress-Tls-Key --vault-name $INGESTION_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.key)"
az keyvault secret set --name Ingestion-Ingress-Tls-Crt --vault-name $INGESTION_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.crt)"
az keyvault delete-policy --object-id $SIGNED_IN_OBJECT_ID -n $INGESTION_KEYVAULT_NAME

cat <<EOF | kubectl apply -f -
apiVersion: secrets-store.csi.x-k8s.io/v1alpha1
kind: SecretProviderClass
metadata:
  name: workflow-secrets-csi-akv
  namespace: backend-dev
spec:
  provider: azure
  parameters:
    usePodIdentity: "true"
    keyvaultName: "${WORKFLOW_KEYVAULT_NAME}"
    objects:  |
      array:
        - |
          objectName: QueueName
          objectAlias: QueueName
          objectType: secret
        - |
          objectName: QueueEndpoint
          objectAlias: QueueEndpoint
          objectType: secret
        - |
          objectName: QueueAccessPolicyName
          objectAlias: QueueAccessPolicyName
          objectType: secret
        - |
          objectName: QueueAccessPolicyKey
          objectAlias: QueueAccessPolicyKey
          objectType: secret
        - |
          objectName: ApplicationInsights--InstrumentationKey
          objectAlias: ApplicationInsights--InstrumentationKey
          objectType: secret
    tenantId: "${TENANT_ID}"
EOF

cat <<EOF | kubectl apply -f -
apiVersion: secrets-store.csi.x-k8s.io/v1alpha1
kind: SecretProviderClass
metadata:
  name: ingestion-secrets-csi-akv
  namespace: backend-dev
spec:
  provider: azure
  secretObjects:
  - secretName: "${INGRESS_TLS_SECRET_NAME}"
    type: Opaque
    data: 
    - objectName: Ingestion-Ingress-Tls-Key
      key: tls.key
    - objectName: Ingestion-Ingress-Tls-Crt
      key: tls.crt
  - secretName: ingestion-secrets
    type: Opaque
    data: 
    - objectName: Queue--Key
      key: queue-keyvalue
    - objectName: ApplicationInsights--InstrumentationKey
      key: appinsights-ikey
  parameters:
    usePodIdentity: "true"
    keyvaultName: "${INGESTION_KEYVAULT_NAME}"
    objects:  |
      array:
        - |
          objectName: Queue--Key
          objectAlias: Queue--Key
          objectType: secret
        - |
          objectName: ApplicationInsights--InstrumentationKey
          objectAlias: ApplicationInsights--InstrumentationKey
          objectType: secret
        - |
          objectName: Ingestion-Ingress-Tls-Key
          objectAlias: Ingestion-Ingress-Tls-Key
          objectType: secret
        - |
          objectName: Ingestion-Ingress-Tls-Crt
          objectAlias: Ingestion-Ingress-Tls-Crt
          objectType: secret
    tenantId: "${TENANT_ID}"
EOF

cat <<EOF | kubectl apply -f -
apiVersion: secrets-store.csi.x-k8s.io/v1alpha1
kind: SecretProviderClass
metadata:
  name: package-secrets-csi-akv
  namespace: backend-dev
spec:
  provider: azure
  secretObjects:
  - secretName: package-secrets
    type: Opaque
    data: 
    - objectName: CosmosDb--ConnectionString
      key: cosmosdb-connstr
    - objectName: ApplicationInsights--InstrumentationKey
      key: appinsights-ikey
  parameters:
    usePodIdentity: "true"
    keyvaultName: "${PACKAGE_KEYVAULT_NAME}"
    objects:  |
      array:
        - |
          objectName: CosmosDb--ConnectionString
          objectAlias: CosmosDb--ConnectionString
          objectType: secret
        - |
          objectName: ApplicationInsights--InstrumentationKey
          objectAlias: ApplicationInsights--InstrumentationKey
          objectType: secret
    tenantId: "${TENANT_ID}"
EOF

cat <<EOF | kubectl apply -f -
apiVersion: secrets-store.csi.x-k8s.io/v1alpha1
kind: SecretProviderClass
metadata:
  name: delivery-secrets-csi-akv
  namespace: backend-dev
spec:
  provider: azure
  secretObjects:
  - secretName: "${INGRESS_TLS_SECRET_NAME}"
    type: Opaque
    data: 
    - objectName: Delivery-Ingress-Tls-Key
      key: tls.key
    - objectName: Delivery-Ingress-Tls-Crt
      key: tls.crt
  parameters:
    usePodIdentity: "true"
    keyvaultName: "${DELIVERY_KEYVAULT_NAME}"
    objects:  |
      array:
        - |
          objectName: Delivery-Ingress-Tls-Key
          objectAlias: Delivery-Ingress-Tls-Key
          objectType: secret
        - |
          objectName: Delivery-Ingress-Tls-Crt
          objectAlias: Delivery-Ingress-Tls-Crt
          objectType: secret
    tenantId: "${TENANT_ID}"
EOF

```

```bash
# setup AAD pod identity
helm repo add aad-pod-identity https://raw.githubusercontent.com/Azure/aad-pod-identity/master/charts

helm repo update

helm install aad-pod-identity aad-pod-identity/aad-pod-identity --set installCRDs=true --set nmi.allowNetworkPluginKubenet=true  --namespace kube-system --version 4.0.0
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
export EXTERNAL_INGEST_DNS_NAME="${RESOURCE_GROUP}-${RANDOM}-ing" && \
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
export COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.deliveryCosmosDbName.value -o tsv) && \
export DATABASE_NAME="${COSMOSDB_NAME}-db" && \
export COLLECTION_NAME="${DATABASE_NAME}-col" && \
export DELIVERY_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.deliveryKeyVaultUri.value -o tsv)
```

Build and publish the Delivery service container image.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/delivery:0.1.0 ./workload/src/shipping/delivery/.
```

Deploy the Delivery service.

```bash
# Extract pod identity outputs from deployment
export DELIVERY_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME --query properties.outputs.deliveryPrincipalResourceId.value -o tsv) && \
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query clientId -o tsv)

# Create secrets
# Note: Ingress TLS key and certificate secrets cannot be exported as outputs in ARM deployments
# So we create an access policy to allow these secrets to be created imperatively.
# The policy is deleted right after the secret creation commands are executed
export SIGNED_IN_OBJECT_ID=$(az ad signed-in-user show --query 'objectId' -o tsv)

az keyvault set-policy --secret-permissions set --object-id $SIGNED_IN_OBJECT_ID -n $DELIVERY_KEYVAULT_NAME
az keyvault secret set --name Delivery-Ingress-Tls-Key --vault-name $DELIVERY_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.key)"
az keyvault secret set --name Delivery-Ingress-Tls-Crt --vault-name $DELIVERY_KEYVAULT_NAME --value "$(cat ingestion-ingress-tls.crt)"
az keyvault delete-policy --object-id $SIGNED_IN_OBJECT_ID -n $DELIVERY_KEYVAULT_NAME

# Deploy the service
helm package charts/delivery/ -u && \
helm install delivery-v0.1.0-dev delivery-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=delivery \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=delivery \
     --set ingress.hosts[0].tls=true \
     --set ingress.hosts[0].tlsSecretName=$INGRESS_TLS_SECRET_NAME \
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
export COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.packageMongoDbName.value -o tsv)
export PACKAGE_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME --query properties.outputs.packagePrincipalResourceId.value -o tsv) && \
export PACKAGE_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $PACKAGE_ID_NAME --query clientId -o tsv)
```

Build the Package service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/package:0.1.0 ./workload/src/shipping/package/.
```

Deploy the Package service.

```bash
# Create secret
# Note: Connection strings cannot be exported as outputs in ARM deployments
# So we create an access policy to allow the secret to be created imperatively.
# The policy is deleted right after the secret creation command is executed
export COSMOSDB_CONNECTION=$(az cosmosdb keys list --type connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g') && \
export COSMOSDB_COL_NAME=packages && \

az keyvault set-policy --secret-permissions set --object-id $SIGNED_IN_OBJECT_ID -n $PACKAGE_KEYVAULT_NAME

az keyvault secret set --name CosmosDb--ConnectionString --vault-name $PACKAGE_KEYVAULT_NAME --value $COSMOSDB_CONNECTION

az keyvault delete-policy --object-id $SIGNED_IN_OBJECT_ID -n $PACKAGE_KEYVAULT_NAME

# Deploy service
helm package charts/package/ -u && \
helm install package-v0.1.0-dev package-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=package \
     --set identity.clientid=$PACKAGE_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$PACKAGE_PRINCIPAL_RESOURCE_ID \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=package \
     --set ingress.hosts[0].tls=false \
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
export WORKFLOW_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.workflowKeyVaultName.value -o tsv)
```

Build the workflow service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/workflow:0.1.0 ./workload/src/shipping/workflow/.
```

Create and set up pod identity.

```bash
# Extract outputs from deployment and get Azure account details
export WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME --query properties.outputs.workflowPrincipalResourceId.value -o tsv) && \
export WORKFLOW_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query clientId -o tsv) && \
export SUBSCRIPTION_ID=$(az account show --query id --output tsv) && \
export TENANT_ID=$(az account show --query tenantId --output tsv)
```

Deploy the Workflow service.

```bash
# Deploy the service
helm package charts/workflow/ -u && \
helm install workflow-v0.1.0-dev workflow-v0.1.0.tgz \
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

Extract resource details and pod identity outputs from deployment.

```bash
export INGESTION_QUEUE_NAMESPACE=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.ingestionQueueNamespace.value -o tsv) && \
export INGESTION_QUEUE_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME --query properties.outputs.ingestionPrincipalResourceId.value -o tsv) && \
export INGESTION_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $INGESTION_ID_NAME --query clientId -o tsv)
```

Build the Ingestion service.

```bash
az acr build -r $ACR_NAME -t $ACR_SERVER/ingestion:0.1.0 ./workload/src/shipping/ingestion/.
```

## Deploy the Ingestion service

```bash

# Deploy service
helm package charts/ingestion/ -u && \
helm install ingestion-v0.1.0-dev ingestion-v0.1.0.tgz \
     --set image.tag=0.1.0 \
     --set image.repository=ingestion \
     --set dockerregistry=$ACR_SERVER \
     --set identity.clientid=$INGESTION_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$INGESTION_PRINCIPAL_RESOURCE_ID \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=ingestion \
     --set ingress.hosts[0].tls=true \
     --set ingress.hosts[0].tlsSecretName=$INGRESS_TLS_SECRET_NAME \
     --set secrets.queue.keyname=IngestionServiceAccessKey \
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
export DRONESCHEDULER_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
export DRONESCHEDULER_COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n workload-stamp --query properties.outputs.droneSchedulerCosmosDbName.value -o tsv) && \
export ENDPOINT_URL=$(az cosmosdb show -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query documentEndpoint -o tsv) && \
export AUTH_KEY=$(az cosmosdb keys list -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query primaryMasterKey -o tsv) && \
export DATABASE_NAME="invoicing" && \
export COLLECTION_NAME="utilization"
```

Create and set up pod identity.

```bash
# Extract outputs from deployment
export DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv) && \
export DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query clientId -o tsv)
```

Build and publish the container image.

```bash
az acr build -r $ACR_NAME -f ./workload/src/shipping/dronescheduler/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 ./workload/src/shipping/.
```

Deploy the dronescheduler service.

```bash
# Deploy the service
helm package charts/dronescheduler/ -u && \
helm install dronescheduler-v0.1.0-dev dronescheduler-v0.1.0.tgz \
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
