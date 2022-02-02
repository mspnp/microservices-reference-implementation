#!/bin/bash

function print_help { echo $'Usage\n\n' \
                           $'-s Subscription\n' \
                           $'-l Location\n' \
                           $'-r Resource Group\n' \
                           $'-b Bing API KEY\n' \
                           $'-a AppId\n' \
                           $'-p Password\n' \
                           $'-t TenantId\n' \
                           $'-u CurrentUser\n' \
                           $'-? Show Usage' \
                           >&2;
                    }

while getopts s:l:r:b:g:a:p:t:u:? option
do
case "${option}"
in
s) SUBSCRIPTION=${OPTARG};;
l) LOCATION=${OPTARG};;
r) RESOURCEGROUP=${OPTARG};;
b) BINGKEY=${OPTARG};;
a) APPID=${OPTARG};;
p) PASSWORD=${OPTARG};;
t) TENANTID=${OPTARG};;
u) CURRENTUSER=${OPTARG};;
?) print_help; exit 0;;
esac
done

if [[ -z "$SUBSCRIPTION" || -z "$LOCATION" || -z "$RESOURCEGROUP" || -z "$RESOURCEGROUP" || -z "$BINGKEY" || -z "$APPID" || -z "$PASSWORD" || -z "$TENANTID"  || -z "$CURRENTUSER" ]]
then
print_help;
exit 2
fi

export SUBSCRIPTIONID=$SUBSCRIPTION
export LOCATION=$LOCATION
export RESOURCE_GROUP=$RESOURCEGROUP
export BING_MAP_API_KEY=$BINGKEY
export CURRENT_USE_OBJECT_ID=$CURRENTUSER
export SP_APP_ID=$APPID
export SP_CLIENT_SECRET=$PASSWORD

az login --service-principal --username $APPID --password $PASSWORD --tenant $TENANTID
az account set --subscription=$SUBSCRIPTIONID

export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
export SUBSCRIPTION_NAME=$(az account show --query name --output tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)

git clone https://github.com/mspnp/microservices-reference-implementation.git && \
pushd ./microservices-reference-implementation && \
git checkout basic-valorem && \
popd

export SSH_PUBLIC_KEY_FILE=~/.ssh/id_rsa.pub

if [ -f "$SSH_PUBLIC_KEY_FILE" ]; then
    export TEST=SSH_PUBLIC_KEY_FILE
else
    mkdir ~/.ssh
    cp /id_rsa.pub ~/.ssh/id_rsa.pub
    cp /id_rsa ~/.ssh/id_rsa
fi

#########################################################################################

export DEPLOYMENT_SUFFIX=$(date +%s%N)
export PROJECT_ROOT=../../../microservices-reference-implementation
export K8S=$PROJECT_ROOT/k8s
export HELM_CHARTS=$PROJECT_ROOT/charts

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

#########################################################################################

# Deploy the resource groups and managed identities
# These are deployed first in a separate template to avoid propagation delays with AAD
echo "Deploying prereqs..."
export DEV_PREREQ_DEPLOYMENT_NAME=azuredeploy-prereqs-${DEPLOYMENT_SUFFIX}-dev

for i in 1 2 3; 
do 
     az deployment sub create \
     --name $DEV_PREREQ_DEPLOYMENT_NAME \
     --location $LOCATION \
     --template-file ${PROJECT_ROOT}/azuredeploy-prereqs.json \
     --parameters resourceGroupName=$RESOURCE_GROUP \
                    resourceGroupLocation=$LOCATION &> /dev/null
     if [[ $? = 0 ]] 
     then
        break;
     fi
done

export IDENTITIES_DEPLOYMENT_NAME=$(az deployment sub show -n $DEV_PREREQ_DEPLOYMENT_NAME --query properties.outputs.identitiesDeploymentName.value -o tsv)
export DELIVERY_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.deliveryIdName.value -o tsv)
export DELIVERY_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query principalId -o tsv)
export DRONESCHEDULER_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerIdName.value -o tsv)
export DRONESCHEDULER_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query principalId -o tsv)
export WORKFLOW_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.workflowIdName.value -o tsv)
export WORKFLOW_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query principalId -o tsv)
export WEBSITE_ID_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.websiteIdName.value -o tsv)
export WEBSITE_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $WEBSITE_ID_NAME --query principalId -o tsv)
export RESOURCE_GROUP_ACR=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.acrResourceGroupName.value -o tsv)

for name in $(az deployment group list -g $RESOURCE_GROUP --query "[].name" -o tsv)
do
   if grep -q "azuredeploy-" <<< "$name"; then
      if [[ ${name} != *"prereqs"* ]];then
         export MAIN_DEPLOYMENT_NAME=$name
      fi
   fi
done

echo $MAIN_DEPLOYMENT_NAME

if [ ! -z "$MAIN_DEPLOYMENT_NAME" ]; then
  export WORKFLOW_TO_KV_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.workFlowToKvNameGuid.value -o tsv)
  export DELIVERY_TO_KV_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.deliveryToKvNameGuid.value -o tsv)
  export DRONESCHED_TO_KV_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.droneschedulerToKvNameGuid.value -o tsv)

  export MON_METRICS_PUBROLE_ASSIGN_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.monMetricsPubRoleAssignNameGuid.value -o tsv)
  export DELIVERYID_NAME_ROLE_ASSIGN_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.deliveryIdNameRoleAssignNameGuid.value -o tsv)
  export MSI_WEBSITE_ROLE_ASSIGN_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.msiwebsiteRoleAssignNameGuid.value -o tsv)
  export MSI_WORKFLOW_ROLE_ASSIGN_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.msiworkflowRoleAssignNameGuid.value -o tsv)
  export MSI_DRONESCHEDULER_ROLE_ASSIGN_NAME_GUID=$(az deployment group show -g $RESOURCE_GROUP -n $MAIN_DEPLOYMENT_NAME --query properties.parameters.msidroneschedulerRoleAssignNameGuid.value -o tsv)
fi

# Wait for AAD propagation
sleep 60s

# until az ad sp show --id ${DELIVERY_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 30; done
# until az ad sp show --id ${DRONESCHEDULER_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 30; done
# until az ad sp show --id ${WORKFLOW_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 30; done
# until az ad sp show --id ${WEBSITE_ID_PRINCIPAL_ID} &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 30; done

# Export the kubernetes cluster version
export KUBERNETES_VERSION=$(az aks get-versions -l $LOCATION --query "orchestrators[?default!=null].orchestratorVersion" -o tsv)

# Deploy all other resources
export DEV_DEPLOYMENT_NAME=azuredeploy-${DEPLOYMENT_SUFFIX}-dev

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

export EXIST_SSH_PUBLIC_KEY=$(az aks list --resource-group $RESOURCE_GROUP --query [0].linuxProfile.ssh.publicKeys[0].keyData -o tsv)

if [ ! -z "$EXIST_SSH_PUBLIC_KEY" ]; then
   echo "SSH Key Already Exists..."
   export SSH_PUBLIC_KEY_FILE=~/.ssh/id_rsa_$RESOURCE_GROUP.pub
   echo $SSH_PUBLIC_KEY_FILE
   echo $EXIST_SSH_PUBLIC_KEY > $SSH_PUBLIC_KEY_FILE
fi

for name in $(az keyvault list -g $RESOURCE_GROUP --query "[].name" -o tsv)
do
   if grep -q "\-d-" <<< "$name"; then
      export DEPLOYMENT_KV_NAME=$name
   fi
done

for i in 1 2 3; 
do
     if [ ! -z "$WORKFLOW_TO_KV_NAME_GUID" -a ! -z "$DELIVERY_TO_KV_NAME_GUID" -a ! -z "$DRONESCHED_TO_KV_NAME_GUID" -a ! -z "$MON_METRICS_PUBROLE_ASSIGN_NAME_GUID" -a ! -z "$DELIVERYID_NAME_ROLE_ASSIGN_NAME_GUID" -a ! -z "$MSI_WEBSITE_ROLE_ASSIGN_NAME_GUID" -a ! -z "$MSI_WORKFLOW_ROLE_ASSIGN_NAME_GUID" -a ! -z "$MSI_DRONESCHEDULER_ROLE_ASSIGN_NAME_GUID" ]; then
          echo "ReDeploying resources..."
          az deployment group create -g $RESOURCE_GROUP --name $DEV_DEPLOYMENT_NAME --template-file ${PROJECT_ROOT}/azuredeploy.json \
          --parameters kubernetesVersion=${KUBERNETES_VERSION} \
                    sshRSAPublicKey="$(cat ${SSH_PUBLIC_KEY_FILE})" \
                    servicePrincipalClientId=${SP_APP_ID} \
                    servicePrincipalClientSecret=${SP_CLIENT_SECRET} \
                    userObjectId=${CURRENT_USE_OBJECT_ID} \
                    deliveryIdName=${DELIVERY_ID_NAME} \
                    deliveryPrincipalId=${DELIVERY_ID_PRINCIPAL_ID} \
                    droneSchedulerIdName=${DRONESCHEDULER_ID_NAME} \
                    droneSchedulerPrincipalId=${DRONESCHEDULER_ID_PRINCIPAL_ID} \
                    workflowIdName=${WORKFLOW_ID_NAME} \
                    workflowPrincipalId=${WORKFLOW_ID_PRINCIPAL_ID} \
                    websiteIdName=${WEBSITE_ID_NAME} \
                    websitePrincipalId=${WEBSITE_ID_PRINCIPAL_ID} \
                    workFlowToKvNameGuid=${WORKFLOW_TO_KV_NAME_GUID} \
                    deliveryToKvNameGuid=${DELIVERY_TO_KV_NAME_GUID} \
                    droneschedulerToKvNameGuid=${DRONESCHED_TO_KV_NAME_GUID} \
                    monMetricsPubRoleAssignNameGuid=${MON_METRICS_PUBROLE_ASSIGN_NAME_GUID} \
                    deliveryIdNameRoleAssignNameGuid=${DELIVERYID_NAME_ROLE_ASSIGN_NAME_GUID} \
                    msiwebsiteRoleAssignNameGuid=${MSI_WEBSITE_ROLE_ASSIGN_NAME_GUID} \
                    msiworkflowRoleAssignNameGuid=${MSI_WORKFLOW_ROLE_ASSIGN_NAME_GUID} \
                    msidroneschedulerRoleAssignNameGuid=${MSI_DRONESCHEDULER_ROLE_ASSIGN_NAME_GUID} \
                    acrResourceGroupName=${RESOURCE_GROUP_ACR} 2>&1 1>/dev/null 
     else
          echo "Deploying resources..."
          az deployment group create -g $RESOURCE_GROUP --name $DEV_DEPLOYMENT_NAME --template-file ${PROJECT_ROOT}/azuredeploy.json \
          --parameters kubernetesVersion=${KUBERNETES_VERSION} \
                    sshRSAPublicKey="$(cat ${SSH_PUBLIC_KEY_FILE})" \
                    servicePrincipalClientId=${SP_APP_ID} \
                    servicePrincipalClientSecret=${SP_CLIENT_SECRET} \
                    userObjectId=${CURRENT_USE_OBJECT_ID} \
                    deliveryIdName=${DELIVERY_ID_NAME} \
                    deliveryPrincipalId=${DELIVERY_ID_PRINCIPAL_ID} \
                    droneSchedulerIdName=${DRONESCHEDULER_ID_NAME} \
                    droneSchedulerPrincipalId=${DRONESCHEDULER_ID_PRINCIPAL_ID} \
                    workflowIdName=${WORKFLOW_ID_NAME} \
                    workflowPrincipalId=${WORKFLOW_ID_PRINCIPAL_ID} \
                    websiteIdName=${WEBSITE_ID_NAME} \
                    websitePrincipalId=${WEBSITE_ID_PRINCIPAL_ID} \
                    acrResourceGroupName=${RESOURCE_GROUP_ACR} 2>&1 1>/dev/null 
     fi

     if [[ $? = 0 ]] 
     then
       az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.acrName.value -o tsv 2>&1 1>/dev/null
       
       if [[ $? = 0 ]] 
       then
         break
       else 
         if [[ $i -ge 3 ]]; then exit 1; fi
         sleep 15
       fi
     else 
       if [[ $i -ge 3 ]]; then exit 1; fi
       sleep 15
     fi
done

#########################################################################################

# Shared
export ACR_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.acrName.value -o tsv)
export ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv)
export CLUSTER_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.aksClusterName.value -o tsv)

#########################################################################################

echo "Installing kubectl..."

#  Install kubectl
sudo az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME --overwrite-existing --admin

# Create namespaces
kubectl create namespace backend-dev

#########################################################################################

echo "Installing Helm..."

# install helm client side
curl -L https://git.io/get_helm.sh | bash -s -- -v v2.14.2
helm init --stable-repo-url https://charts.helm.sh/stable --wait
helm repo update

#helm repo add stable https://charts.helm.sh/stable
#helm repo update

# setup tiller in your cluster
kubectl apply -f $K8S/tiller-rbac.yaml

#echo "Installing Tiller..."

sleep 60s

helm init --stable-repo-url https://charts.helm.sh/stable --service-account tiller --override spec.selector.matchLabels.'name'='tiller',spec.selector.matchLabels.'app'='helm' --output yaml | sed 's@apiVersion: extensions/v1beta1@apiVersion: apps/v1@' | sed 's@image: gcr.io/kubernetes-helm/tiller:v2.14.2@image: ghcr.io/helm/tiller:v2.14.2@' | kubectl apply -f -

sleep 60s

#########################################################################################

# Acquire Instrumentation Key
export AI_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.appInsightsName.value -o tsv)
export AI_IKEY=$(az resource show \
                    -g $RESOURCE_GROUP \
                    -n $AI_NAME \
                    --resource-type "Microsoft.Insights/components" \
                    --query properties.InstrumentationKey \
                    -o tsv)

# add RBAC for AppInsights
kubectl apply -f $K8S/k8s-rbac-ai.yaml

#########################################################################################

echo "Configuring AAD POD Identity..."

# setup AAD pod identity
kubectl create -f $PROJECT_ROOT/k8s/deployment-rbac.yaml

kubectl create -f https://raw.githubusercontent.com/Azure/kubernetes-keyvault-flexvol/master/deployment/kv-flexvol-installer.yaml

#########################################################################################

kubectl create namespace ingress-controllers

for i in 1 2 3; 
do
     echo "Deploy the ngnix ingress controller..."

     # Deploy the ngnix ingress controller
     helm install stable/nginx-ingress --name nginx-ingress-dev --namespace ingress-controllers --set rbac.create=true --set controller.ingressClass=nginx-dev --version 1.41.3 &> nginx.txt
     err=$?
     cmdoutput=$(cat nginx.txt)

     if [[ $err = 0 ]] 
     then
         break;
     else 
         if grep -q "already exists" <<< "$cmdoutput"; then
            break;
         else 
            echo $cmdoutput
         fi
         if [[ $i -ge 3 ]]; then exit 1; fi
         sleep 30s
     fi
done

# Obtain the load balancer ip address and assign a domain name
until export INGRESS_LOAD_BALANCER_IP=$(kubectl get services/nginx-ingress-dev-controller -n ingress-controllers -o jsonpath="{.status.loadBalancer.ingress[0].ip}" 2> /dev/null) && test -n "$INGRESS_LOAD_BALANCER_IP"; do echo "Waiting for load balancer deployment" && sleep 20; done
export INGRESS_LOAD_BALANCER_IP_ID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$INGRESS_LOAD_BALANCER_IP')].[id]" --output tsv)
export EXTERNAL_INGEST_DNS_NAME="${RESOURCE_GROUP}-ingest-dev"
export EXTERNAL_INGEST_FQDN=$(az network public-ip update --ids $INGRESS_LOAD_BALANCER_IP_ID --dns-name $EXTERNAL_INGEST_DNS_NAME --query "dnsSettings.fqdn" --output tsv)

if [ -f "ingestion-ingress-tls.crt" ]; then
    echo "ingestion-ingress-tls.crt exists."
else
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -out ingestion-ingress-tls.crt \
    -keyout ingestion-ingress-tls.key \
    -subj "/CN=${EXTERNAL_INGEST_FQDN}/O=fabrikam"
fi

#########################################################################################

kubectl apply -f $K8S/k8s-resource-quotas-dev.yaml

#########################################################################################

for i in 1 2 3; 
do

echo "Deploying Delivery Service..."

export COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryCosmosDbName.value -o tsv)
export DATABASE_NAME="${COSMOSDB_NAME}-db"
export COLLECTION_NAME="${DATABASE_NAME}-col"
export DELIVERY_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.deliveryKeyVaultUri.value -o tsv)

export DELIVERY_PATH=$PROJECT_ROOT/src/shipping/delivery

# Build the Docker image
docker build --pull --compress -t $ACR_SERVER/delivery:0.1.0 $DELIVERY_PATH/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/delivery:0.1.0

# Extract pod identity outputs from deployment
export DELIVERY_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.deliveryPrincipalResourceId.value -o tsv)
export DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query clientId -o tsv)
export DELIVERY_INGRESS_TLS_SECRET_NAME=delivery-ingress-tls

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

# Deploy the service
helm install $HELM_CHARTS/delivery/ \
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
     --set identity.clientid=$DELIVERY_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$DELIVERY_PRINCIPAL_RESOURCE_ID \
     --set cosmosdb.id=$DATABASE_NAME \
     --set cosmosdb.collectionid=$COLLECTION_NAME \
     --set keyvault.uri=$DELIVERY_KEYVAULT_URI \
     --set secrets.appinsights.ikey=$AI_IKEY \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --name delivery-v0.1.0-dev \
     --dep-up

#########################################################################################

echo "Deploying Packing Service..."

export COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.packageMongoDbName.value -o tsv)

export PACKAGE_PATH=$PROJECT_ROOT/src/shipping/package

# Build the docker image
docker build -f $PACKAGE_PATH/Dockerfile -t $ACR_SERVER/package:0.1.0 $PACKAGE_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/package:0.1.0

# Create secret
# Note: Connection strings cannot be exported as outputs in ARM deployments
export COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g')       
export COSMOSDB_COL_NAME=packages

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

# Deploy service
helm install $HELM_CHARTS/package/ \
     --set image.tag=0.1.0 \
     --set image.repository=package \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=package \
     --set ingress.hosts[0].tls=false \
     --set secrets.appinsights.ikey=$AI_IKEY \
     --set secrets.mongo.pwd=$COSMOSDB_CONNECTION \
     --set cosmosDb.collectionName=$COSMOSDB_COL_NAME \
     --set dockerregistry=$ACR_SERVER \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --name package-v0.1.0-dev \
     --dep-up

#########################################################################################

echo "Deploying Workflow Service..."

export WORKFLOW_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.workflowKeyVaultName.value -o tsv)
export WORKFLOW_PATH=$PROJECT_ROOT/src/shipping/workflow

# Build the Docker image
docker build --pull --compress -t $ACR_SERVER/workflow:0.1.0 $WORKFLOW_PATH/.

# Push the image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/workflow:0.1.0

# Extract outputs from deployment
export WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.workflowPrincipalResourceId.value -o tsv)
export WORKFLOW_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query clientId -o tsv)

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

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
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --name workflow-v0.1.0-dev \
     --dep-up

#########################################################################################

echo "Deploying Ingestion Service..."

export INGESTION_QUEUE_NAMESPACE=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionQueueNamespace.value -o tsv)
export INGESTION_QUEUE_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_ACCESS_KEY_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.ingestionServiceAccessKeyName.value -o tsv)
export INGESTION_ACCESS_KEY_VALUE=$(az servicebus namespace authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_QUEUE_NAMESPACE --name $INGESTION_ACCESS_KEY_NAME --query primaryKey -o tsv)

export INGESTION_PATH=$PROJECT_ROOT/src/shipping/ingestion

# Build the docker image
docker build -f $INGESTION_PATH/Dockerfile -t $ACR_SERVER/ingestion:0.1.0 $INGESTION_PATH

# Push the docker image to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/ingestion:0.1.0

# Set secreat name
export INGRESS_TLS_SECRET_NAME=ingestion-ingress-tls

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

# Deploy service
helm install $HELM_CHARTS/ingestion/ \
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
     --set secrets.appinsights.ikey=${AI_IKEY} \
     --set secrets.queue.keyname=IngestionServiceAccessKey \
     --set secrets.queue.keyvalue=${INGESTION_ACCESS_KEY_VALUE} \
     --set secrets.queue.name=${INGESTION_QUEUE_NAME} \
     --set secrets.queue.namespace=${INGESTION_QUEUE_NAMESPACE} \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --name ingestion-v0.1.0-dev \
     --dep-up

#########################################################################################

echo "Deploying Drone Scheduler Service..."

export DRONESCHEDULER_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
export DRONESCHEDULER_COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEV_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerCosmosDbName.value -o tsv)
export ENDPOINT_URL=$(az cosmosdb show -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query documentEndpoint -o tsv)
export AUTH_KEY=$(az cosmosdb keys list -n $DRONESCHEDULER_COSMOSDB_NAME -g $RESOURCE_GROUP --query primaryMasterKey -o tsv)
export DATABASE_NAME="invoicing"
export COLLECTION_NAME="utilization"

export DRONE_PATH=$PROJECT_ROOT/src/shipping/dronescheduler

export DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv)
export DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query clientId -o tsv)

# Build the Docker image
docker build -f $DRONE_PATH/Dockerfile -t $ACR_SERVER/dronescheduler:0.1.0 $DRONE_PATH/../

# Push the images to ACR
az acr login --name $ACR_NAME
docker push $ACR_SERVER/dronescheduler:0.1.0

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

# Deploy the service
helm install $HELM_CHARTS/dronescheduler/ \
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
     --name dronescheduler-v0.1.0-dev \
     --dep-up

#########################################################################################

echo "Deploying Website Service..."

export WEBSITE_PATH=$PROJECT_ROOT/src/shipping/website

docker build --pull --compress -t $ACR_SERVER/website:0.1.0 $WEBSITE_PATH/.

az acr login --name $ACR_NAME
docker push $ACR_SERVER/website:0.1.0

export WEBSITE_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.websitePrincipalResourceId.value -o tsv)
export WEBSITE_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n $WEBSITE_ID_NAME --query clientId -o tsv)
export WEBSITE_INGRESS_TLS_SECRET_NAME=website-ingress-tls

printenv > import-$RESOURCE_GROUP-envs.sh; sed -i -e 's/^/export /' import-$RESOURCE_GROUP-envs.sh

# Deploy the service
helm install $HELM_CHARTS/website/ \
     --set image.tag=0.1.0 \
     --set image.repository=website \
     --set dockerregistry=$ACR_SERVER \
     --set ingress.hosts[0].name=$EXTERNAL_INGEST_FQDN \
     --set ingress.hosts[0].serviceName=website \
     --set ingress.hosts[0].tls=true \
     --set ingress.hosts[0].tlsSecretName=$DELIVERY_INGRESS_TLS_SECRET_NAME \
     --set ingress.tls.secrets[0].name=$DELIVERY_INGRESS_TLS_SECRET_NAME \
     --set ingress.tls.secrets[0].key="$(cat ingestion-ingress-tls.key)" \
     --set ingress.tls.secrets[0].certificate="$(cat ingestion-ingress-tls.crt)" \
     --set identity.clientid=$WEBSITE_PRINCIPAL_CLIENT_ID \
     --set identity.resourceid=$WEBSITE_PRINCIPAL_RESOURCE_ID \
     --set bingmap.key=$BING_MAP_API_KEY \
     --set api.url=https://$EXTERNAL_INGEST_FQDN \
     --set keyvault.uri=$DELIVERY_KEYVAULT_URI \
     --set secrets.appinsights.ikey=$AI_IKEY \
     --set reason="Initial deployment" \
     --set tags.dev=true \
     --namespace backend-dev \
     --name website-v0.1.0-dev \
     --dep-up

     echo -n "Waiting for Service Deployments to complete"

     for i in {0..20}
     do
         echo -n "."
         sleep 6s
     done

     echo 
     echo

     declare -a StringArray=("delivery" "package" "workflow" "ingestion" "dronescheduler" "website" )
     l=${#StringArray[@]}
     cnt=0

     for svc in ${StringArray[@]}; do
        echo -n "Checking $svc Service: "

        kubectl wait --namespace backend-dev --for=condition=ready pod --selector=app.kubernetes.io/instance=$svc-v0.1.0-dev --timeout=30s &> status.log
        err=$?
        cmdoutput=$(cat status.log)
        rm status.log

        if [[ $err = 0 ]] 
        then
           echo "GOOD"
           cnt=$((cnt+1))
        else 
          if grep -q "already exists" <<< "$cmdoutput"; then
             echo "ALREADY EXISTS"
             cnt=$((cnt+1))
          else 
             echo "ERROR"
             echo
             echo $cmdoutput
             break;
          fi
        fi

        sleep 1s
     done

     if [[ $cnt = $l ]] 
     then
        break;
     fi

     if [[ $i -ge 3 ]]; then exit 1; fi
     sleep 30s
done

echo

#Make Request to prime system
export requestData="{\"confirmationRequired\":\"None\",\"deadline\":\"\",\"dropOffLocation\":\"555 110th Ave NE, Bellevue, WA 98004\",\"expedited\":true,\"ownerId\":\"myowner\",\"packageInfo\":{\"packageId\":\"mypackage\",\"size\":\"Large\",\"tag\":\"mytag\",\"weight\":10},\"pickupLocation\":\"1 Microsoft Way, Redmond, WA 98052\",\"pickupTime\":\"2019-05-08T20:00:00.000Z\"}"
curl -X POST -H "Content-Type: application/json" -d "$requestData" --insecure  "https://$EXTERNAL_INGEST_FQDN/api/DroneSite/deliveryrequest"

echo

sleep 15s

kubectl get pod -n backend-dev

echo
echo "az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME --admin" >> import-$RESOURCE_GROUP-envs.sh
echo 
echo "##############################################################################"
echo "To Access the Drone Demo Site run the following URL from your browser"
echo
echo "https://$EXTERNAL_INGEST_FQDN"
echo
echo "Run the command below to configure the session to manage the environment"
echo 
echo ". import-$RESOURCE_GROUP-envs.sh" 
echo
echo "##############################################################################"
