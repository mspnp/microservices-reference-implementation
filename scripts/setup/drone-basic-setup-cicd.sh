#!/bin/bash


#########################################################################################
#  App Settings
#########################################################################################
az account set --subscription=018bf144-3a6d-4c13-b1d3-d100a03adc6b

export LOCATION=eastus
export RESOURCE_GROUP=russdronebasic7

export AZURE_DEVOPS_USER=v-rusmit@microsoft.com
export AZURE_DEVOPS_EXT_PAT=xv2dlu56mywalvhvord3y35hs4txj3olwduxuxjztsb7sanmn3va
export AZURE_DEVOPS_ORG_NAME=v-rusmit
export AZURE_DEVOPS_PROJECT_NAME="Test Squall 7" 
export AZURE_DEVOPS_REPOS_NAME=microservices-reference-implementation
export AZURE_DEVOPS_RBAC_NAME=v-rusmit-test7

export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
export SUBSCRIPTION_NAME=$(az account show --query name --output tsv)
export TENANT_ID=$(az account show --query tenantId --output tsv)

export SSH_PUBLIC_KEY_FILE=~/.ssh/id_rsa.pub 

#########################################################################################

export DEPLOYMENT_SUFFIX=29926087414
#export DEPLOYMENT_SUFFIX=$(date +%S%N)

export PROJECT_ROOT=../../../microservices-reference-implementation
export K8S=$PROJECT_ROOT/k8s
export HELM_CHARTS=$PROJECT_ROOT/charts

declare environments="dev qa staging prod"

# Create service principal for AKS
# export SP_DETAILS=$(az ad sp create-for-rbac --role="Contributor" -o json) && \
# export SP_APP_ID=$(echo $SP_DETAILS | jq ".appId" -r) && \
# export SP_CLIENT_SECRET=$(echo $SP_DETAILS | jq ".password" -r) && \
export SP_APP_ID=07798b66-44df-438e-bdca-859e6a3b4257 && \
export SP_CLIENT_SECRET=Cet-IumBvF-z~wEsdEX2BqkIzFX7TyW~DG && \
export SP_OBJECT_ID=$(az ad sp show --id $SP_APP_ID -o tsv --query objectId)

###########################################################################################

# Export the kubernetes cluster version and deploy
export KUBERNETES_VERSION=$(az aks get-versions -l $LOCATION --query "orchestrators[?default!=null].orchestratorVersion" -o tsv) && \
export PREREQ_DEPLOYMENT_NAME=azuredeploy-prereqs-${DEPLOYMENT_SUFFIX}
export DEPLOYMENT_NAME=azuredeploy-${DEPLOYMENT_SUFFIX}

for env in $environments; 
do

echo "Deploying azuredeploy-prereqs.json for Environment (${env}): Running..."  

ENV=${env^^}
export DEPLOY_RESOURCES1=$(az deployment sub create \
   --name $PREREQ_DEPLOYMENT_NAME-${env} \
   --location $LOCATION \
   --template-file ${PROJECT_ROOT}/azuredeploy-prereqs.json \
   --parameters resourceGroupName=$RESOURCE_GROUP \
                resourceGroupLocation=$LOCATION \
                environmentName=${env})

echo "Deploying azuredeploy-prereqs.json for Environment (${env}): Complete"  

export {${ENV}_IDENTITIES_DEPLOYMENT_NAME,IDENTITIES_DEPLOYMENT_NAME}=$(az deployment sub show -n $PREREQ_DEPLOYMENT_NAME-${env} --query properties.outputs.identitiesDeploymentName.value -o tsv) && \
export {${ENV}_DELIVERY_ID_NAME,DELIVERY_ID_NAME}=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.deliveryIdName.value -o tsv)
export DELIVERY_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query principalId -o tsv)
export {${ENV}_DRONESCHEDULER_ID_NAME,DRONESCHEDULER_ID_NAME}=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerIdName.value -o tsv)
export DRONESCHEDULER_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query principalId -o tsv)
export {${ENV}_WORKFLOW_ID_NAME,WORKFLOW_ID_NAME}=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.workflowIdName.value -o tsv)
export WORKFLOW_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query principalId -o tsv)
export RESOURCE_GROUP_ACR=$(az deployment group show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.acrResourceGroupName.value -o tsv)

# Wait for AAD propagation
until az ad sp show --id $DELIVERY_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $DRONESCHEDULER_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $WORKFLOW_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done

echo "Deploying azuredeploy.json for Environment (${env}): Running..."  

export DEPLOY_RESOURCES2=$(az deployment group create -g $RESOURCE_GROUP --name $DEPLOYMENT_NAME-${env} --template-file ${PROJECT_ROOT}/azuredeploy.json \
   --parameters servicePrincipalId=${SP_OBJECT_ID} \
               kubernetesVersion=${KUBERNETES_VERSION} \
               sshRSAPublicKey="$(cat ${SSH_PUBLIC_KEY_FILE})" \
               deliveryIdName="$DELIVERY_ID_NAME" \
               deliveryPrincipalId=$DELIVERY_ID_PRINCIPAL_ID \
               droneSchedulerIdName=$DRONESCHEDULER_ID_NAME \
               droneSchedulerPrincipalId=$DRONESCHEDULER_ID_PRINCIPAL_ID \
               workflowIdName=$WORKFLOW_ID_NAME \
               workflowPrincipalId=$WORKFLOW_ID_PRINCIPAL_ID \
               acrResourceGroupName=${RESOURCE_GROUP_ACR} \
               environmentName=${env})

echo "Deploying azuredeploy.json for Environment (${env}): Complete..."  

export {${ENV}_AI_NAME,AI_NAME}=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.appInsightsName.value -o tsv)
export ${ENV}_AI_IKEY=$(az resource show -g $RESOURCE_GROUP -n $AI_NAME --resource-type "Microsoft.Insights/components" --query properties.InstrumentationKey -o tsv)
export {${ENV}_ACR_NAME,ACR_NAME}=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.acrName.value -o tsv)
export ${ENV}_ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv)

done

# Shared
export CLUSTER_NAME=$(az deployment group  show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-dev --query properties.outputs.aksClusterName.value -o tsv) && \

echo "Cluster Name - ${CLUSTER_NAME}"

###########################################################################################

echo "Creating Kubernetes Namespaces..."

#  Install kubectl
sudo az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME

# Create namespaces
kubectl create namespace backend-dev && \
kubectl create namespace backend-qa && \
kubectl create namespace backend-staging && \
kubectl create namespace backend

###########################################################################################

echo "Installing Helm..."

# install helm client side
DESIRED_VERSION=v2.14.2;curl -L https://git.io/get_helm.sh | bash

# setup tiller in your cluster
kubectl apply -f $K8S/tiller-rbac.yaml
helm init --service-account tiller

###########################################################################################

echo "Adding RBAC for AppInsights..."

# add RBAC for AppInsights
kubectl apply -f $K8S/k8s-rbac-ai.yaml

###########################################################################################

echo "Setup AAD pod identity..."

# setup AAD pod identity
kubectl create -f https://raw.githubusercontent.com/Azure/aad-pod-identity/master/deploy/infra/deployment-rbac.yaml

kubectl create -f https://raw.githubusercontent.com/Azure/kubernetes-keyvault-flexvol/master/deployment/kv-flexvol-installer.yaml

###########################################################################################

kubectl apply -f $K8S/k8s-resource-quotas-dev.yaml -f $K8S/k8s-resource-quotas-qa-stg-prod.yaml

###########################################################################################

echo "Azure Devops setup..."

echo "Logging into Azure Devops..."
echo $AZURE_DEVOPS_EXT_PAT > token.txt
cat token.txt | sudo az devops login

echo "Adding Azure Devops Extensions..."
# add extensions
az extension add --name azure-devops --only-show-errors

# export

export AZURE_DEVOPS_ORG=https://dev.azure.com/$AZURE_DEVOPS_ORG_NAME
export AZURE_DEVOPS_VSRM_ORG=https://vsrm.dev.azure.com/$AZURE_DEVOPS_ORG_NAME
export AZURE_PIPELINES_SERVICE_CONN_NAME=default_cicd_service-connection

echo -e "Azure Devops Project Create..."
# create project or skip this step if you are using an existent Azure DevOps project
sudo az devops project create \
   --name "$AZURE_DEVOPS_PROJECT_NAME" \
   --organization "$AZURE_DEVOPS_ORG"

echo -e "Azure Devops Repo Create..."
# create repo
sudo az repos create \
   --name "$AZURE_DEVOPS_REPOS_NAME" \
   --organization "$AZURE_DEVOPS_ORG" \
   --project "$AZURE_DEVOPS_PROJECT_NAME"

# create service principal for Azure Pipelines
export AKS_SP_APP_ID=$(az ad sp list --display-name $AZURE_DEVOPS_RBAC_NAME --query [0].appId -o tsv)
if [ -z "$AKS_SP_APP_ID" ]; then
    export AKS_SP_APP_ID=$(az ad sp create-for-rbac --name $AZURE_DEVOPS_RBAC_NAME --skip-assignment --query appId -o tsv)
    echo "Creating RBAC Credentials ($AZURE_DEVOPS_RBAC_NAME)..."
fi

if [ -f "~/.ssh/$AKS_SP_APP_ID.txt" ]; then
    export AKS_SP_SECRET=$(cat ~/.ssh/$AKS_SP_APP_ID.txt)
else 
    echo "Resetting RBAC Credentials ($AZURE_DEVOPS_RBAC_NAME)..."
    export AKS_SP_SECRET=$(az ad sp credential reset --name $AZURE_DEVOPS_RBAC_NAME --query "password" -o tsv)
    echo $AKS_SP_SECRET > ~/.ssh/$AKS_SP_APP_ID.txt
fi

export SERVICE_CONNECTION_ID=$(az devops service-endpoint list \
                              --org $AZURE_DEVOPS_ORG \
                              --project "$AZURE_DEVOPS_PROJECT_NAME" \
                              --query "[?name=='default_cicd_service-connection'].id" -o tsv)

if [ -n "$SERVICE_CONNECTION_ID" ]; then
    echo "Deleting Existing Service Connection..."
    export DELETE_SERVICE_CONNECTION_ID=$(az devops service-endpoint delete \
                                        --id $SERVICE_CONNECTION_ID \
                                        --org $AZURE_DEVOPS_ORG \
                                        --project "$AZURE_DEVOPS_PROJECT_NAME" \
                                        --yes)
    export SERVICE_CONNECTION_ID=""
fi

if [ -z "$SERVICE_CONNECTION_ID" ]; then
      export AZURE_DEVOPS_EXT_AZURE_RM_SERVICE_PRINCIPAL_KEY=$AKS_SP_SECRET

      echo "Creating Service Connection..."
      export CREATE_SERVICE_CONNECTION_ID=$(sudo --preserve-env az devops service-endpoint azurerm create  \
                --name $AZURE_PIPELINES_SERVICE_CONN_NAME \
                --azure-rm-tenant-id $TENANT_ID \
                --azure-rm-subscription-id $SUBSCRIPTION_ID \
                --azure-rm-subscription-name "$SUBSCRIPTION_NAME" \
                --organization $AZURE_DEVOPS_ORG \
                --project "$AZURE_DEVOPS_PROJECT_NAME" \
                --azure-rm-service-principal-id $AKS_SP_APP_ID --query id -o tsv)
      export SERVICE_CONNECTION_ID=$CREATE_SERVICE_CONNECTION_ID 

      echo "Updating Service Connection..."
      export UPDATE_SERVICE_CONNECTION=$(az devops service-endpoint update \
                    --id $SERVICE_CONNECTION_ID \
                    --organization $AZURE_DEVOPS_ORG \
                    --project "$AZURE_DEVOPS_PROJECT_NAME" \
                    --enable-for-all true)
fi

# navigate to the repo and add ssh following links below or just skip this step for https
sudo open $AZURE_DEVOPS_ORG/$AZURE_DEVOPS_PROJECT_NAME/_git/$AZURE_DEVOPS_REPOS_NAME

###########################################################################################

echo "Adding New Remote"

# get the ssh url. For https just replace sshUrl with remoteUrl below
export NEW_REMOTE=$(sudo az repos show -r $AZURE_DEVOPS_REPOS_NAME --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query sshUrl -o tsv)

# push master from cloned repo to the new remote
git remote add newremote $NEW_REMOTE

###########################################################################################

# navigate to the organization tokens and create a new Personal Access Token
sudo open $AZURE_DEVOPS_ORG/_usersSettings/tokens

# export token for making REST API calls
export AZURE_DEVOPS_AUTHN_BASIC_TOKEN=$(echo -n ${AZURE_DEVOPS_USER}:${AZURE_DEVOPS_EXT_PAT} | base64 | sed -e ':a' -e 'N' -e '$!ba' -e 's/\n//g')

export AZURE_DEVOPS_SERVICE_CONN_ID=$(sudo az devops service-endpoint list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='${AZURE_PIPELINES_SERVICE_CONN_NAME}'].id" -o tsv) && \
export AZURE_DEVOPS_REPOS_ID=$(sudo az repos show --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --repository $AZURE_DEVOPS_REPOS_NAME --query id -o tsv) && \
export AZURE_DEVOPS_PROJECT_ID=$(sudo az devops project show --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query id -o tsv) && \
export AZURE_DEVOPS_USER_ID=$(sudo az devops user show --user ${AZURE_DEVOPS_USER} --organization ${AZURE_DEVOPS_ORG} --query id -o tsv)

###########################################################################################

# Create a self-signed certificate for TLS and public ip addresses
export RESOURCE_GROUP_NODE=$(az aks show -g $RESOURCE_GROUP -n $CLUSTER_NAME --query "nodeResourceGroup" -o tsv) && \
export EXTERNAL_INGEST_DNS_NAME="${RESOURCE_GROUP}-ingest"

for env in $environments;do
ENV=${env^^}

export PUBLIC_IP_CREATE=$(az network public-ip create --name ${EXTERNAL_INGEST_DNS_NAME}-${env}-pip --dns-name ${EXTERNAL_INGEST_DNS_NAME}-${env} --allocation-method static -g $RESOURCE_GROUP_NODE)
EXTERNAL_INGEST_FQDN=$(az network public-ip show --name ${EXTERNAL_INGEST_DNS_NAME}-${env}-pip --query "dnsSettings.fqdn" -g $RESOURCE_GROUP_NODE --output tsv)
export ${ENV}_EXTERNAL_INGEST_FQDN=${EXTERNAL_INGEST_FQDN}
export ${ENV}_INGRESS_LOAD_BALANCER_IP=$(az network public-ip show --name ${EXTERNAL_INGEST_DNS_NAME}-${env}-pip --query "ipAddress" -g $RESOURCE_GROUP_NODE --output tsv)

echo "Deploying ingress controllers..."
# Deploy the ngnix ingress controller
export HELM_INGRESS_INSTALL=$(helm install stable/nginx-ingress --name nginx-ingress-${env} --namespace ingress-controllers --set rbac.create=true --set controller.ingressClass=nginx-${env} --set controller.service.loadBalancerIP=${ENV}_INGRESS_LOAD_BALANCER_IP --version 1.24.7)

CRTFILE=ingestion-ingress-tls-${env}.crt
if [ -f "$CRTFILE" ]; then
    echo "$CRTFILE exists."
else 
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -out CRTFILE \
    -keyout ingestion-ingress-tls-${env}.key \
    -subj "/CN=${EXTERNAL_INGEST_FQDN}/O=fabrikam"
fi

export "${ENV}_INGRESS_TLS_SECRET_CERT=$(echo $(cat ingestion-ingress-tls-${env}.crt) | tr '\n' "\\n")"
export "${ENV}_INGRESS_TLS_SECRET_KEY=$(echo $(cat ingestion-ingress-tls-${env}.key) | tr '\n' "\\n")"
done

# export app paths
export DELIVERY_PATH=$PROJECT_ROOT/src/shipping/delivery && \
export PACKAGE_PATH=$PROJECT_ROOT/src/shipping/package && \
export WORKFLOW_PATH=$PROJECT_ROOT/src/shipping/workflow && \
export INGESTION_PATH=$PROJECT_ROOT/src/shipping/ingestion && \
export DRONE_PATH=$PROJECT_ROOT/src/shipping/dronescheduler

# configure build YAML definitions
for pipelinePath in $DELIVERY_PATH $PACKAGE_PATH $WORKFLOW_PATH $INGESTION_PATH $DRONE_PATH; do
echo pipelinePath

sed -i \
    -e "s#ACR_SERVER_VAR_VAL#$ACR_SERVER#g" \
    -e "s#ACR_NAME_VAR_VAL#$ACR_NAME#g" \
    -e "s#AZURE_PIPELINES_SERVICE_CONN_NAME_VAR_VAL#$AZURE_PIPELINES_SERVICE_CONN_NAME#g" \
    ${pipelinePath}/azure-pipelines.yml
done

ssh-keyscan -H vs-ssh.visualstudio.com >> ~/.ssh/known_hosts

# push changes to the repo
cd $PROJECT_ROOT && \
git add -u && \
git commit -m "set build pipelines variables"
git push newremote && \
cd -

###########################################################################################

echo "Creating Deployment Pipeline..."

# add build definition
export CREATE_DEPLOYEMENT_PIPELINE=$(az pipelines create \
   --organization "$AZURE_DEVOPS_ORG" \
   --project "$AZURE_DEVOPS_PROJECT_NAME" \
   --name "delivery-ci" \
   --yml-path "src/shipping/delivery/azure-pipelines.yml" \
   --repository-type tfsgit \
   --repository "$AZURE_DEVOPS_REPOS_NAME" \
   --branch master)

# query build definition details and resources
export AZURE_DEVOPS_DELIVERY_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='delivery-ci'].id" -o tsv) && \
export AZURE_DEVOPS_DELIVERY_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='delivery-ci'].queue.id" -o tsv) && \
for env in $environments;do
ENV=${env^^}
export ${ENV}_DATABASE_NAME="deliveries-db"
export ${ENV}_COLLECTION_NAME="deliveries-col"
envIdentitiesDeploymentName="${ENV}_IDENTITIES_DEPLOYMENT_NAME"
export ${ENV}_DELIVERY_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.deliveryKeyVaultUri.value -o tsv)
export ${ENV}_DELIVERY_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n ${!envIdentitiesDeploymentName} --query properties.outputs.deliveryPrincipalResourceId.value -o tsv)
envDeliveryIdName="${ENV}_DELIVERY_ID_NAME"
export ${ENV}_DELIVERY_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n ${!envDeliveryIdName} --query clientId -o tsv)
done && \
export INGRESS_TLS_SECRET_NAME=delivery-ingress-tls

# add relese definition
cat $DELIVERY_PATH/azure-pipelines-cd.json | \
     sed "s#AZURE_DEVOPS_SERVICE_CONN_ID_VAR_VAL#$AZURE_DEVOPS_SERVICE_CONN_ID#g" | \
     sed "s#AZURE_DEVOPS_DELIVERY_BUILD_ID_VAR_VAL#$AZURE_DEVOPS_DELIVERY_BUILD_ID#g" | \
     sed "s#AZURE_DEVOPS_REPOS_ID_VAR_VAL#$AZURE_DEVOPS_REPOS_ID#g" | \
     sed "s#AZURE_DEVOPS_PROJECT_ID_VAR_VAL#$AZURE_DEVOPS_PROJECT_ID#g" | \
     sed "s#AZURE_DEVOPS_QUEUE_ID_VAR_VAL#$AZURE_DEVOPS_DELIVERY_QUEUE_ID#g" | \
     sed "s#AZURE_DEVOPS_USER_ID_VAR_VAL#$AZURE_DEVOPS_USER_ID#g" | \
     sed "s#CLUSTER_NAME_VAR_VAL#$CLUSTER_NAME#g" | \
     sed "s#RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     # development resources
     sed "s#DEV_ACR_SERVER_VAR_VAL#$DEV_ACR_SERVER#g" | \
     sed "s#DEV_ACR_NAME_VAR_VAL#$DEV_ACR_NAME#g" | \
     sed "s#DEV_DELIVERY_PRINCIPAL_CLIENT_ID_VAR_VAL#$DEV_DELIVERY_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#DEV_DELIVERY_PRINCIPAL_RESOURCE_ID_VAR_VAL#$DEV_DELIVERY_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#DEV_DATABASE_NAME_VAR_VAL#$DEV_DATABASE_NAME#g" | \
     sed "s#DEV_COLLECTION_NAME_VAR_VAL#$DEV_COLLECTION_NAME#g" | \
     sed "s#DEV_DELIVERY_KEYVAULT_URI_VAR_VAL#$DEV_DELIVERY_KEYVAULT_URI#g" | \
     sed "s#DEV_EXTERNAL_INGEST_FQDN_VAR_VAL#$DEV_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#DEV_INGRESS_TLS_SECRET_CERT_VAR_VAL#$DEV_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#DEV_INGRESS_TLS_SECRET_KEY_VAR_VAL#$DEV_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#DEV_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" | \
     # qa resources
     sed "s#QA_ACR_SERVER_VAR_VAL#$QA_ACR_SERVER#g" | \
     sed "s#QA_ACR_NAME_VAR_VAL#$QA_ACR_NAME#g" | \
     sed "s#QA_DELIVERY_PRINCIPAL_CLIENT_ID_VAR_VAL#$QA_DELIVERY_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#QA_DELIVERY_PRINCIPAL_RESOURCE_ID_VAR_VAL#$QA_DELIVERY_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#QA_DATABASE_NAME_VAR_VAL#$QA_DATABASE_NAME#g" | \
     sed "s#QA_COLLECTION_NAME_VAR_VAL#$QA_COLLECTION_NAME#g" | \
     sed "s#QA_DELIVERY_KEYVAULT_URI_VAR_VAL#$QA_DELIVERY_KEYVAULT_URI#g" | \
     sed "s#QA_EXTERNAL_INGEST_FQDN_VAR_VAL#$QA_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#QA_INGRESS_TLS_SECRET_CERT_VAR_VAL#$QA_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#QA_INGRESS_TLS_SECRET_KEY_VAR_VAL#$QA_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#QA_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" | \
     # staging resources
     sed "s#STAGING_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#STAGING_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#STAGING_DELIVERY_PRINCIPAL_CLIENT_ID_VAR_VAL#$STAGING_DELIVERY_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#STAGING_DELIVERY_PRINCIPAL_RESOURCE_ID_VAR_VAL#$STAGING_DELIVERY_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#STAGING_DATABASE_NAME_VAR_VAL#$STAGING_DATABASE_NAME#g" | \
     sed "s#STAGING_COLLECTION_NAME_VAR_VAL#$STAGING_COLLECTION_NAME#g" | \
     sed "s#STAGING_DELIVERY_KEYVAULT_URI_VAR_VAL#$STAGING_DELIVERY_KEYVAULT_URI#g" | \
     sed "s#STAGING_EXTERNAL_INGEST_FQDN_VAR_VAL#$STAGING_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#STAGING_INGRESS_TLS_SECRET_CERT_VAR_VAL#$STAGING_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#STAGING_INGRESS_TLS_SECRET_KEY_VAR_VAL#$STAGING_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#STAGING_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" | \
     # production resources
     sed "s#SOURCE_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#SOURCE_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#PROD_ACR_SERVER_VAR_VAL#$PROD_ACR_SERVER#g" | \
     sed "s#PROD_ACR_NAME_VAR_VAL#$PROD_ACR_NAME#g" | \
     sed "s#PROD_DELIVERY_PRINCIPAL_CLIENT_ID_VAR_VAL#$PROD_DELIVERY_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#PROD_DELIVERY_PRINCIPAL_RESOURCE_ID_VAR_VAL#$PROD_DELIVERY_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#PROD_DATABASE_NAME_VAR_VAL#$PROD_DATABASE_NAME#g" | \
     sed "s#PROD_COLLECTION_NAME_VAR_VAL#$PROD_COLLECTION_NAME#g" | \
     sed "s#PROD_DELIVERY_KEYVAULT_URI_VAR_VAL#$PROD_DELIVERY_KEYVAULT_URI#g" | \
     sed "s#PROD_EXTERNAL_INGEST_FQDN_VAR_VAL#$PROD_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#PROD_INGRESS_TLS_SECRET_CERT_VAR_VAL#$PROD_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#PROD_INGRESS_TLS_SECRET_KEY_VAR_VAL#$PROD_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#PROD_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" \
     > $DELIVERY_PATH/azure-pipelines-cd-0.json

URL_AZURE_DEVOPS_PROJECT_NAME="${AZURE_DEVOPS_PROJECT_NAME// /%20}"
UPDATE_PIPELINE_URL=${AZURE_DEVOPS_VSRM_ORG}/${URL_AZURE_DEVOPS_PROJECT_NAME}/_apis/release/definitions?api-version=6.0

echo "Deploying Delivery Pipeline..."

#export UPDATE_DEPLOYMENT_PIPELINE=$(
curl -sL -w "%{http_code}" -X POST $UPDATE_PIPELINE_URL \
     -d@${DELIVERY_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" 

###########################################################################################

echo "Preparing Package Pipeline..."

# add build definitions
export CREATE_PACKAGE_PIPELINE=$(az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project "$AZURE_DEVOPS_PROJECT_NAME" \
   --name package-ci \
   --yml-path src/shipping/package/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master)

# query build definition details and resources
export AZURE_DEVOPS_PACKAGE_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='package-ci'].id" -o tsv) && \
export AZURE_DEVOPS_PACKAGE_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='package-ci'].queue.id" -o tsv) && \
for env in $environments;do
ENV=${env^^}
export COSMOSDB_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.packageMongoDbName.value -o tsv)
export ${ENV}_COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g')
export ${ENV}_COSMOSDB_COL_NAME=packages
done

# add release definition
cat $PACKAGE_PATH/azure-pipelines-cd.json | \
     sed "s#AZURE_DEVOPS_SERVICE_CONN_ID_VAR_VAL#$AZURE_DEVOPS_SERVICE_CONN_ID#g" | \
     sed "s#AZURE_DEVOPS_PACKAGE_BUILD_ID_VAR_VAL#$AZURE_DEVOPS_PACKAGE_BUILD_ID#g" | \
     sed "s#AZURE_DEVOPS_REPOS_ID_VAR_VAL#$AZURE_DEVOPS_REPOS_ID#g" | \
     sed "s#AZURE_DEVOPS_PROJECT_ID_VAR_VAL#$AZURE_DEVOPS_PROJECT_ID#g" | \
     sed "s#AZURE_DEVOPS_PACKAGE_QUEUE_ID_VAR_VAL#$AZURE_DEVOPS_PACKAGE_QUEUE_ID#g" | \
     sed "s#AZURE_DEVOPS_USER_ID_VAR_VAL#$AZURE_DEVOPS_USER_ID#g" | \
     sed "s#CLUSTER_NAME_VAR_VAL#$CLUSTER_NAME#g" | \
     sed "s#RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     # development resources
     sed "s#DEV_AI_IKEY_VAR_VAL#$DEV_AI_IKEY#g" | \
     sed "s#DEV_ACR_SERVER_VAR_VAL#$DEV_ACR_SERVER#g" | \
     sed "s#DEV_ACR_NAME_VAR_VAL#$DEV_ACR_NAME#g" | \
     sed "s#DEV_COSMOSDB_COL_NAME_VAR_VAL#$DEV_COSMOSDB_COL_NAME#g" | \
     sed "s#DEV_COSMOSDB_CONNECTION_VAR_VAL#${DEV_COSMOSDB_CONNECTION//&/\\&}#g" | \
     # qa resources
     sed "s#QA_AI_IKEY_VAR_VAL#$QA_AI_IKEY#g" | \
     sed "s#QA_ACR_SERVER_VAR_VAL#$QA_ACR_SERVER#g" | \
     sed "s#QA_ACR_NAME_VAR_VAL#$QA_ACR_NAME#g" | \
     sed "s#QA_COSMOSDB_COL_NAME_VAR_VAL#$QA_COSMOSDB_COL_NAME#g" | \
     sed "s#QA_COSMOSDB_CONNECTION_VAR_VAL#${QA_COSMOSDB_CONNECTION//&/\\&}#g" | \
     # staging resources
     sed "s#STAGING_AI_IKEY_VAR_VAL#$STAGING_AI_IKEY#g" | \
     sed "s#STAGING_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#STAGING_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#STAGING_COSMOSDB_COL_NAME_VAR_VAL#$STAGING_COSMOSDB_COL_NAME#g" | \
     sed "s#STAGING_COSMOSDB_CONNECTION_VAR_VAL#${STAGING_COSMOSDB_CONNECTION//&/\\&}#g" | \
     # production resources
     sed "s#PROD_AI_IKEY_VAR_VAL#$PROD_AI_IKEY#g" | \
     sed "s#SOURCE_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#SOURCE_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#PROD_ACR_SERVER_VAR_VAL#$PROD_ACR_SERVER#g" | \
     sed "s#PROD_ACR_NAME_VAR_VAL#$PROD_ACR_NAME#g" | \
     sed "s#PROD_COSMOSDB_COL_NAME_VAR_VAL#$PROD_COSMOSDB_COL_NAME#g" | \
     sed "s#PROD_COSMOSDB_CONNECTION_VAR_VAL#${PROD_COSMOSDB_CONNECTION//&/\\&}#g" \
     > $PACKAGE_PATH/azure-pipelines-cd-0.json

echo "Creating Package Pipeline..."

curl -sL -w "%{http_code}" -X POST $UPDATE_PIPELINE_URL \
     -d@${PACKAGE_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null

###########################################################################################

echo "Preparing Workflow Pipeline..."

# add build definitions
export CREATE_WORKFLOW_PIPELINE=$(az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project "$AZURE_DEVOPS_PROJECT_NAME" \
   --name workflow-ci \
   --yml-path src/shipping/workflow/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master)

# query build definition details and resources
export AZURE_DEVOPS_WORKFLOW_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='workflow-ci'].id" -o tsv) && \
export AZURE_DEVOPS_WORKFLOW_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='workflow-ci'].queue.id" -o tsv) && \
for env in $environments;do
ENV=${env^^}
envIdentitiesDeploymentName="${ENV}_IDENTITIES_DEPLOYMENT_NAME"
export ${ENV}_WORKFLOW_KEYVAULT_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.workflowKeyVaultName.value -o tsv)
export ${ENV}_WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n ${!envIdentitiesDeploymentName} --query properties.outputs.workflowPrincipalResourceId.value -o tsv)
envWorkflowIdName="${ENV}_WORKFLOW_ID_NAME"
export ${ENV}_WORKFLOW_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n ${!envWorkflowIdName} --query clientId -o tsv)
done

# add relese definition
cat $WORKFLOW_PATH/azure-pipelines-cd.json | \
     sed "s#AZURE_DEVOPS_SERVICE_CONN_ID_VAR_VAL#$AZURE_DEVOPS_SERVICE_CONN_ID#g" | \
     sed "s#AZURE_DEVOPS_WORKFLOW_BUILD_ID_VAR_VAL#$AZURE_DEVOPS_WORKFLOW_BUILD_ID#g" | \
     sed "s#AZURE_DEVOPS_REPOS_ID_VAR_VAL#$AZURE_DEVOPS_REPOS_ID#g" | \
     sed "s#AZURE_DEVOPS_PROJECT_ID_VAR_VAL#$AZURE_DEVOPS_PROJECT_ID#g" | \
     sed "s#AZURE_DEVOPS_WORKFLOW_QUEUE_ID_VAR_VAL#$AZURE_DEVOPS_WORKFLOW_QUEUE_ID#g" | \
     sed "s#AZURE_DEVOPS_USER_ID_VAR_VAL#$AZURE_DEVOPS_USER_ID#g" | \
     sed "s#CLUSTER_NAME_VAR_VAL#$CLUSTER_NAME#g" | \
     sed "s#CLUSTER_RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     # development resources
     sed "s#DEV_ACR_SERVER_VAR_VAL#$DEV_ACR_SERVER#g" | \
     sed "s#DEV_ACR_NAME_VAR_VAL#$DEV_ACR_NAME#g" | \
     sed "s#DEV_WORKFLOW_KEYVAULT_RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     sed "s#DEV_WORKFLOW_PRINCIPAL_CLIENT_ID_VAR_VAL#$DEV_WORKFLOW_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#DEV_WORKFLOW_PRINCIPAL_RESOURCE_ID_VAR_VAL#$DEV_WORKFLOW_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#DEV_WORKFLOW_KEYVAULT_NAME_VAR_VAL#$DEV_WORKFLOW_KEYVAULT_NAME#g" | \
     sed "s#DEV_SUBSCRIPTION_ID_VAR_VAL#$SUBSCRIPTION_ID#g" | \
     sed "s#DEV_TENANT_ID_VAR_VAL#$TENANT_ID#g" | \
     # qa resources
     sed "s#QA_ACR_SERVER_VAR_VAL#$QA_ACR_SERVER#g" | \
     sed "s#QA_ACR_NAME_VAR_VAL#$QA_ACR_NAME#g" | \
     sed "s#QA_WORKFLOW_KEYVAULT_RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     sed "s#QA_WORKFLOW_PRINCIPAL_CLIENT_ID_VAR_VAL#$QA_WORKFLOW_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#QA_WORKFLOW_PRINCIPAL_RESOURCE_ID_VAR_VAL#$QA_WORKFLOW_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#QA_WORKFLOW_KEYVAULT_NAME_VAR_VAL#$QA_WORKFLOW_KEYVAULT_NAME#g" | \
     sed "s#QA_SUBSCRIPTION_ID_VAR_VAL#$SUBSCRIPTION_ID#g" | \
     sed "s#QA_TENANT_ID_VAR_VAL#$TENANT_ID#g" | \
     # staging resources
     sed "s#STAGING_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#STAGING_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#STAGING_WORKFLOW_KEYVAULT_RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     sed "s#STAGING_WORKFLOW_PRINCIPAL_CLIENT_ID_VAR_VAL#$STAGING_WORKFLOW_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#STAGING_WORKFLOW_PRINCIPAL_RESOURCE_ID_VAR_VAL#$STAGING_WORKFLOW_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#STAGING_WORKFLOW_KEYVAULT_NAME_VAR_VAL#$STAGING_WORKFLOW_KEYVAULT_NAME#g" | \
     sed "s#STAGING_SUBSCRIPTION_ID_VAR_VAL#$SUBSCRIPTION_ID#g" | \
     sed "s#STAGING_TENANT_ID_VAR_VAL#$TENANT_ID#g" | \
     # production resources
     sed "s#SOURCE_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#SOURCE_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#PROD_ACR_SERVER_VAR_VAL#$PROD_ACR_SERVER#g" | \
     sed "s#PROD_ACR_NAME_VAR_VAL#$PROD_ACR_NAME#g" | \
     sed "s#PROD_WORKFLOW_KEYVAULT_RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     sed "s#PROD_WORKFLOW_PRINCIPAL_CLIENT_ID_VAR_VAL#$PROD_WORKFLOW_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#PROD_WORKFLOW_PRINCIPAL_RESOURCE_ID_VAR_VAL#$PROD_WORKFLOW_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#PROD_WORKFLOW_KEYVAULT_NAME_VAR_VAL#$PROD_WORKFLOW_KEYVAULT_NAME#g" | \
     sed "s#PROD_SUBSCRIPTION_ID_VAR_VAL#$SUBSCRIPTION_ID#g" | \
     sed "s#PROD_TENANT_ID_VAR_VAL#$TENANT_ID#g" \
    > $WORKFLOW_PATH/azure-pipelines-cd-0.json

echo "Creating Workflow Pipeline..."

curl -sL -w "%{http_code}" -X POST $UPDATE_PIPELINE_URL \
     -d@${WORKFLOW_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null

###########################################################################################

echo "Preparing Ingestion Pipeline..."

# add build definitions
export CREATE_INGESTION_PIPELINE=$(az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project "$AZURE_DEVOPS_PROJECT_NAME" \
   --name ingestion-ci \
   --yml-path src/shipping/ingestion/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master)

# query build definition details and resources
export AZURE_DEVOPS_INGESTION_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='ingestion-ci'].id" -o tsv) && \
export AZURE_DEVOPS_INGESTION_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='ingestion-ci'].queue.id" -o tsv) && \
for env in $environments;do
ENV=${env^^}
export {${ENV}_INGESTION_QUEUE_NAMESPACE,INGESTION_QUEUE_NAMESPACE}=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.ingestionQueueNamespace.value -o tsv)
export ${ENV}_INGESTION_QUEUE_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_ACCESS_KEY_NAME=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.ingestionServiceAccessKeyName.value -o tsv)
export ${ENV}_INGESTION_ACCESS_KEY_VALUE=$(az servicebus namespace authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $INGESTION_QUEUE_NAMESPACE --name $INGESTION_ACCESS_KEY_NAME --query primaryKey -o tsv)
done && \
export INGRESS_TLS_SECRET_NAME=ingestion-ingress-tls

# add relese definition
cat $INGESTION_PATH/azure-pipelines-cd.json | \
     sed "s#AZURE_DEVOPS_SERVICE_CONN_ID_VAR_VAL#$AZURE_DEVOPS_SERVICE_CONN_ID#g" | \
     sed "s#AZURE_DEVOPS_INGESTION_BUILD_ID_VAR_VAL#$AZURE_DEVOPS_INGESTION_BUILD_ID#g" | \
     sed "s#AZURE_DEVOPS_REPOS_ID_VAR_VAL#$AZURE_DEVOPS_REPOS_ID#g" | \
     sed "s#AZURE_DEVOPS_PROJECT_ID_VAR_VAL#$AZURE_DEVOPS_PROJECT_ID#g" | \
     sed "s#AZURE_DEVOPS_INGESTION_QUEUE_ID_VAR_VAL#$AZURE_DEVOPS_INGESTION_QUEUE_ID#g" | \
     sed "s#AZURE_DEVOPS_USER_ID_VAR_VAL#$AZURE_DEVOPS_USER_ID#g" | \
     sed "s#CLUSTER_NAME_VAR_VAL#$CLUSTER_NAME#g" | \
     sed "s#RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     # development resources
     sed "s#DEV_AI_IKEY_VAR_VAL#$DEV_AI_IKEY#g" | \
     sed "s#DEV_ACR_SERVER_VAR_VAL#$DEV_ACR_SERVER#g" | \
     sed "s#DEV_ACR_NAME_VAR_VAL#$DEV_ACR_NAME#g" | \
     sed "s#DEV_EXTERNAL_INGEST_FQDN_VAR_VAL#$DEV_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#DEV_INGESTION_QUEUE_NAMESPACE_VAR_VAL#$DEV_INGESTION_QUEUE_NAMESPACE#g" | \
     sed "s#DEV_INGESTION_QUEUE_NAME_VAR_VAL#$DEV_INGESTION_QUEUE_NAME#g" | \
     sed "s#DEV_INGESTION_ACCESS_KEY_VALUE_VAR_VAL#$DEV_INGESTION_ACCESS_KEY_VALUE#g" | \
     sed "s#DEV_INGRESS_TLS_SECRET_CERT_VAR_VAL#$DEV_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#DEV_INGRESS_TLS_SECRET_KEY_VAR_VAL#$DEV_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#DEV_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" | \
     # qa resources
     sed "s#QA_AI_IKEY_VAR_VAL#$QA_AI_IKEY#g" | \
     sed "s#QA_ACR_SERVER_VAR_VAL#$QA_ACR_SERVER#g" | \
     sed "s#QA_ACR_NAME_VAR_VAL#$QA_ACR_NAME#g" | \
     sed "s#QA_EXTERNAL_INGEST_FQDN_VAR_VAL#$QA_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#QA_INGESTION_QUEUE_NAMESPACE_VAR_VAL#$QA_INGESTION_QUEUE_NAMESPACE#g" | \
     sed "s#QA_INGESTION_QUEUE_NAME_VAR_VAL#$QA_INGESTION_QUEUE_NAME#g" | \
     sed "s#QA_INGESTION_ACCESS_KEY_VALUE_VAR_VAL#$QA_INGESTION_ACCESS_KEY_VALUE#g" | \
     sed "s#QA_INGRESS_TLS_SECRET_CERT_VAR_VAL#$QA_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#QA_INGRESS_TLS_SECRET_KEY_VAR_VAL#$QA_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#QA_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" | \
     # staging resources
     sed "s#STAGING_AI_IKEY_VAR_VAL#$STAGING_AI_IKEY#g" | \
     sed "s#STAGING_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#STAGING_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#STAGING_EXTERNAL_INGEST_FQDN_VAR_VAL#$STAGING_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#STAGING_INGESTION_QUEUE_NAMESPACE_VAR_VAL#$STAGING_INGESTION_QUEUE_NAMESPACE#g" | \
     sed "s#STAGING_INGESTION_QUEUE_NAME_VAR_VAL#$STAGING_INGESTION_QUEUE_NAME#g" | \
     sed "s#STAGING_INGESTION_ACCESS_KEY_VALUE_VAR_VAL#$STAGING_INGESTION_ACCESS_KEY_VALUE#g" | \
     sed "s#STAGING_INGRESS_TLS_SECRET_CERT_VAR_VAL#$STAGING_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#STAGING_INGRESS_TLS_SECRET_KEY_VAR_VAL#$STAGING_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#STAGING_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" | \
     # production resources
     sed "s#PROD_AI_IKEY_VAR_VAL#$PROD_AI_IKEY#g" | \
     sed "s#SOURCE_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#SOURCE_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#PROD_ACR_SERVER_VAR_VAL#$PROD_ACR_SERVER#g" | \
     sed "s#PROD_ACR_NAME_VAR_VAL#$PROD_ACR_NAME#g" | \
     sed "s#PROD_EXTERNAL_INGEST_FQDN_VAR_VAL#$PROD_EXTERNAL_INGEST_FQDN#g" | \
     sed "s#PROD_INGESTION_QUEUE_NAMESPACE_VAR_VAL#$PROD_INGESTION_QUEUE_NAMESPACE#g" | \
     sed "s#PROD_INGESTION_QUEUE_NAME_VAR_VAL#$PROD_INGESTION_QUEUE_NAME#g" | \
     sed "s#PROD_INGESTION_ACCESS_KEY_VALUE_VAR_VAL#$PROD_INGESTION_ACCESS_KEY_VALUE#g" | \
     sed "s#PROD_INGRESS_TLS_SECRET_CERT_VAR_VAL#$PROD_INGRESS_TLS_SECRET_CERT#g" | \
     sed "s#PROD_INGRESS_TLS_SECRET_KEY_VAR_VAL#$PROD_INGRESS_TLS_SECRET_KEY#g" | \
     sed "s#PROD_INGRESS_TLS_SECRET_NAME_VAR_VAL#$INGRESS_TLS_SECRET_NAME#g" \
     > $INGESTION_PATH/azure-pipelines-cd-0.json

echo "Creating Ingestion Pipeline..."

curl -sL -w "%{http_code}" -X POST $UPDATE_PIPELINE_URL \
     -d@${INGESTION_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null

###########################################################################################

echo "Preparing Dronescheduler Pipeline..."

# add build definitions
export CREATE_DRONESCHEDULER_PIPELINE=$(az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project "$AZURE_DEVOPS_PROJECT_NAME" \
   --name dronescheduler-ci \
   --yml-path src/shipping/dronescheduler/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master)

# query build definition details and resources
export AZURE_DEVOPS_DRONE_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='dronescheduler-ci'].id" -o tsv) && \
export AZURE_DEVOPS_DRONE_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project "$AZURE_DEVOPS_PROJECT_NAME" --query "[?name=='dronescheduler-ci'].queue.id" -o tsv) && \
for env in $environments;do
ENV=${env^^}
envIdentitiesDeploymentName="${ENV}_IDENTITIES_DEPLOYMENT_NAME"
export ${ENV}_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az deployment group show -g $RESOURCE_GROUP -n ${!envIdentitiesDeploymentName} --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv)
envDroneSchedulerIdName="${ENV}_DRONESCHEDULER_ID_NAME"
export ${ENV}_DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n ${!envDroneSchedulerIdName} --query clientId -o tsv)
export ${ENV}_DRONESCHEDULER_KEYVAULT_URI=$(az deployment group show -g $RESOURCE_GROUP -n $DEPLOYMENT_NAME-${env} --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
done

# add relese definition
cat $DRONE_PATH/azure-pipelines-cd.json | \
     sed "s#AZURE_DEVOPS_SERVICE_CONN_ID_VAR_VAL#$AZURE_DEVOPS_SERVICE_CONN_ID#g" | \
     sed "s#AZURE_DEVOPS_DRONE_BUILD_ID_VAR_VAL#$AZURE_DEVOPS_DRONE_BUILD_ID#g" | \
     sed "s#AZURE_DEVOPS_REPOS_ID_VAR_VAL#$AZURE_DEVOPS_REPOS_ID#g" | \
     sed "s#AZURE_DEVOPS_PROJECT_ID_VAR_VAL#$AZURE_DEVOPS_PROJECT_ID#g" | \
     sed "s#AZURE_DEVOPS_DRONE_QUEUE_ID_VAR_VAL#$AZURE_DEVOPS_DRONE_QUEUE_ID#g" | \
     sed "s#AZURE_DEVOPS_USER_ID_VAR_VAL#$AZURE_DEVOPS_USER_ID#g" | \
     sed "s#CLUSTER_NAME_VAR_VAL#$CLUSTER_NAME#g" | \
     sed "s#RESOURCE_GROUP_VAR_VAL#$RESOURCE_GROUP#g" | \
     # development resources
     sed "s#DEV_ACR_SERVER_VAR_VAL#$DEV_ACR_SERVER#g" | \
     sed "s#DEV_ACR_NAME_VAR_VAL#$DEV_ACR_NAME#g" | \
     sed "s#DEV_DRONESCHEDULER_PRINCIPAL_CLIENT_ID_VAR_VAL#$DEV_DRONESCHEDULER_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#DEV_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID_VAR_VAL#$DEV_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#DEV_DRONESCHEDULER_KEYVAULT_URI_VAR_VAL#$DEV_DRONESCHEDULER_KEYVAULT_URI#g" | \
     # qa resources
     sed "s#QA_ACR_SERVER_VAR_VAL#$QA_ACR_SERVER#g" | \
     sed "s#QA_ACR_NAME_VAR_VAL#$QA_ACR_NAME#g" | \
     sed "s#QA_DRONESCHEDULER_PRINCIPAL_CLIENT_ID_VAR_VAL#$QA_DRONESCHEDULER_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#QA_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID_VAR_VAL#$QA_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#QA_DRONESCHEDULER_KEYVAULT_URI_VAR_VAL#$QA_DRONESCHEDULER_KEYVAULT_URI#g" | \
     # staging resources
     sed "s#STAGING_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#STAGING_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#STAGING_DRONESCHEDULER_PRINCIPAL_CLIENT_ID_VAR_VAL#$STAGING_DRONESCHEDULER_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#STAGING_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID_VAR_VAL#$STAGING_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#STAGING_DRONESCHEDULER_KEYVAULT_URI_VAR_VAL#$STAGING_DRONESCHEDULER_KEYVAULT_URI#g" | \
     # production resources
     sed "s#SOURCE_ACR_SERVER_VAR_VAL#$STAGING_ACR_SERVER#g" | \
     sed "s#SOURCE_ACR_NAME_VAR_VAL#$STAGING_ACR_NAME#g" | \
     sed "s#PROD_ACR_SERVER_VAR_VAL#$PROD_ACR_SERVER#g" | \
     sed "s#PROD_ACR_NAME_VAR_VAL#$PROD_ACR_NAME#g" | \
     sed "s#PROD_DRONESCHEDULER_PRINCIPAL_CLIENT_ID_VAR_VAL#$PROD_DRONESCHEDULER_PRINCIPAL_CLIENT_ID#g" | \
     sed "s#PROD_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID_VAR_VAL#$PROD_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID#g" | \
     sed "s#PROD_DRONESCHEDULER_KEYVAULT_URI_VAR_VAL#$PROD_DRONESCHEDULER_KEYVAULT_URI#g" \
     > $DRONE_PATH/azure-pipelines-cd-0.json

echo "Creating Drone Scheduler Pipeline..."

curl -sL -w "%{http_code}" -X POST $UPDATE_PIPELINE_URL \
     -d@${DRONE_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null