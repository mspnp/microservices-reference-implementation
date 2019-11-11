# Setup Reference Implementation CI/CD with Azure DevOps

## Prerequisites

- Azure subscription
- [Azure CLI 2.0.49 or later](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure DevOps account](https://azure.microsoft.com/services/devops)
- [Helm 2.12.3 or later](https://docs.helm.sh/using_helm/#installing-helm)
- [Values from deployment instructions](./deployment.md)

## Infrastructure for dev, test, staging and production

```bash
# Export the kubernetes cluster version and deploy
export KUBERNETES_VERSION=$(az aks get-versions -l $LOCATION --query "orchestrators[?default!=null].orchestratorVersion" -o tsv) && \
for env in dev qa staging prod; do
ENV=${env^^}
az deployment create \
   --name azuredeploy-prereqs-${env} \
   --location $LOCATION \
   --template-file ${PROJECT_ROOT}/azuredeploy-prereqs.json \
   --parameters resourceGroupName=$RESOURCE_GROUP \
                resourceGroupLocation=$LOCATION \
                environmentName=${env}

export {${ENV}_IDENTITIES_DEPLOYMENT_NAME,IDENTITIES_DEPLOYMENT_NAME}=$(az deployment show -n azuredeploy-adv-prereqs-adv-${env} --query properties.outputs.identitiesDeploymentName.value -o tsv) && \
export {${ENV}_DELIVERY_ID_NAME,DELIVERY_ID_NAME}=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.deliveryIdName.value -o tsv)
export DELIVERY_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DELIVERY_ID_NAME --query principalId -o tsv)
export {${ENV}_DRONESCHEDULER_ID_NAME,DRONESCHEDULER_ID_NAME}=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.droneSchedulerIdName.value -o tsv)
export DRONESCHEDULER_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $DRONESCHEDULER_ID_NAME --query principalId -o tsv)
export {${ENV}_WORKFLOW_ID_NAME,WORKFLOW_ID_NAME}=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.workflowIdName.value -o tsv)
export WORKFLOW_ID_PRINCIPAL_ID=$(az identity show -g $RESOURCE_GROUP -n $WORKFLOW_ID_NAME --query principalId -o tsv)
export RESOURCE_GROUP_ACR=$(az group deployment show -g $RESOURCE_GROUP -n $IDENTITIES_DEPLOYMENT_NAME --query properties.outputs.acrResourceGroupName.value -o tsv)

# Wait for AAD propagation
until az ad sp show --id $DELIVERY_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $DRONESCHEDULER_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done
until az ad sp show --id $WORKFLOW_ID_PRINCIPAL_ID &> /dev/null ; do echo "Waiting for AAD propagation" && sleep 5; done

az group deployment create -g $RESOURCE_GROUP --name azuredeploy-${env} --template-file ${PROJECT_ROOT}/azuredeploy.json \
   --parameters servicePrincipalClientId=${SP_APP_ID} \
               servicePrincipalClientSecret=${SP_CLIENT_SECRET} \
               servicePrincipalId=${SP_OBJECT_ID} \
               kubernetesVersion=${KUBERNETES_VERSION} \
               sshRSAPublicKey="$(cat ${SSH_PUBLIC_KEY_FILE})" \
               deliveryIdName="$DELIVERY_ID_NAME" \
               deliveryPrincipalId=$DELIVERY_ID_PRINCIPAL_ID \
               droneSchedulerIdName=$DRONESCHEDULER_ID_NAME \
               droneSchedulerPrincipalId=$DRONESCHEDULER_ID_PRINCIPAL_ID \
               workflowIdName=$WORKFLOW_ID_NAME \
               workflowPrincipalId=$WORKFLOW_ID_PRINCIPAL_ID \
               acrResourceGroupName=${RESOURCE_GROUP_ACR} \
               acrResourceGroupLocation=$LOCATION \
               environmentName=${env}

export {${ENV}_AI_NAME,AI_NAME}=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.appInsightsName.value -o tsv)
export ${ENV}_AI_IKEY=$(az resource show -g $RESOURCE_GROUP -n $AI_NAME --resource-type "Microsoft.Insights/components" --query properties.InstrumentationKey -o tsv)
export {${ENV}_ACR_NAME,ACR_NAME}=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.acrName.value -o tsv)
export ${ENV}_ACR_SERVER=$(az acr show -n $ACR_NAME --query loginServer -o tsv)

done
```

Get outputs from Azure Deploy
```bash
# Shared
export CLUSTER_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-dev --query properties.outputs.aksClusterName.value -o tsv) && \
```

Enable Azure Monitoring for Containers in the AKS cluster
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
kubectl create namespace backend-qa && \
kubectl create namespace backend-staging && \
kubectl create namespace backend
```

Setup Helm in the container

```bash
kubectl apply -f $K8S/tiller-rbac.yaml
helm init --service-account tiller
```

## Integrate Application Insights instance

```bash
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

## Setup cluster resource quota

```bash
kubectl apply -f $K8S/k8s-resource-quotas-dev.yaml -f $K8S/k8s-resource-quotas-qa-stg-prod.yaml
```

## Setup Azure DevOps

```
# add extensions
az extension add --name azure-devops

# export
AZURE_DEVOPS_ORG_NAME=<devops-org-name>
AZURE_DEVOPS_ORG=https://dev.azure.com/$AZURE_DEVOPS_ORG_NAME
AZURE_DEVOPS_VSRM_ORG=https://vsrm.dev.azure.com/$AZURE_DEVOPS_ORG_NAME
AZURE_DEVOPS_PROJECT_NAME=<new-or-existent-project-name>
AZURE_DEVOPS_REPOS_NAME=<new-repo-name>
AZURE_PIPELINES_SERVICE_CONN_NAME=default_cicd_service-connection

# create project or skip this step if you are using an existent Azure DevOps project
az devops project create \
   --name $AZURE_DEVOPS_PROJECT_NAME \
   --organization $AZURE_DEVOPS_ORG

# create repo
az repos create \
   --name $AZURE_DEVOPS_REPOS_NAME \
   --organization $AZURE_DEVOPS_ORG \
   --project $AZURE_DEVOPS_PROJECT_NAME

# create service principal for Azure Pipelines
az ad sp create-for-rbac

# create Service Connection
az devops service-endpoint create \
   --service-endpoint-type azurerm \
   --name $AZURE_PIPELINES_SERVICE_CONN_NAME \
   --authorization-scheme ServicePrincipal \
   --azure-rm-tenant-id $TENANT_ID \
   --azure-rm-subscription-id $SUBSCRIPTION_ID \
   --azure-rm-subscription-name "$SUBSCRIPTION_NAME" \
   --organization $AZURE_DEVOPS_ORG \
   --project $AZURE_DEVOPS_PROJECT_NAME \
   --azure-rm-service-principal-id <sp-appId-created-for-rbac> --azure-rm-service-principal-key <sp-password-created-for-rbac>

# navigate to the repo and add ssh following links below or just skip this step for https
open $AZURE_DEVOPS_ORG/$AZURE_DEVOPS_PROJECT_NAME/_git/$AZURE_DEVOPS_REPOS_NAME
```

> Follow instructions at [Use SSH Key authentication](https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops) to add ssh.
For more information on the different authentication types, please take a look at [Authentication comparison](https://docs.microsoft.com/en-us/azure/devops/repos/git/auth-overview?view=azure-devops#authentication-comparison)

![](https://docs.microsoft.com/en-us/azure/devops/repos/git/_img/ssh_add_public_key.gif?view=azure-devops)

## Add new remote for the new Azure Repo
```
# get the ssh url. For https just replace sshUrl with remoteUrl below
export NEW_REMOTE=$(az repos show -r $AZURE_DEVOPS_REPOS_NAME --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query sshUrl -o tsv)

# push master from cloned repo to the new remote
cd <github-repo> && \
git remote add newremote $NEW_REMOTE
```

## Obtain Azure DevOps resources

Extract details from devops, repos and projects

```bash
# navigate to the organization tokens and create a new Personal Access Token
open $AZURE_DEVOPS_ORG/_usersSettings/tokens

# export token for making REST API calls
export AZURE_DEVEOPS_USER=<devops-user-email>
export AZURE_DEVOPS_PAT=<generated-PAT>
export AZURE_DEVOPS_AUTHN_BASIC_TOKEN=$(echo -n ${AZURE_DEVOPS_USER}:${AZURE_DEVOPS_PAT} | base64 | sed -e ':a' -e 'N' -e '$!ba' -e 's/\n//g')

export AZURE_DEVOPS_SERVICE_CONN_ID=$(az devops service-endpoint list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='${AZURE_PIPELINES_SERVICE_CONN_NAME}'].id" -o tsv) && \
export AZURE_DEVOPS_REPOS_ID=$(az repos show --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --repository $AZURE_DEVOPS_REPOS_NAME --query id -o tsv) && \
export AZURE_DEVOPS_PROJECT_ID=$(az devops project show --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query id -o tsv) && \
export AZURE_DEVOPS_USER_ID=$(az devops user show --user ${AZURE_DEVEOPS_USER} --organization ${AZURE_DEVOPS_ORG} --query id -o tsv)
```

### Build pipelines pre-requisites

```bash
# Create a self-signed certificate for TLS and public ip addresses
export RESOURCE_GROUP_NODE=$(az aks show -g $RESOURCE_GROUP -n $CLUSTER_NAME --query "nodeResourceGroup" -o tsv) && \
export EXTERNAL_INGEST_DNS_NAME="${RESOURCE_GROUP}-ingest"

for env in dev qa staging prod;do
ENV=${env^^}

az network public-ip create --name ${EXTERNAL_INGEST_DNS_NAME}-${env}-pip --dns-name ${EXTERNAL_INGEST_DNS_NAME}-${env} --allocation-method static -g $RESOURCE_GROUP_NODE
EXTERNAL_INGEST_FQDN=$(az network public-ip show --name ${EXTERNAL_INGEST_DNS_NAME}-${env}-pip --query "dnsSettings.fqdn" -g $RESOURCE_GROUP_NODE --output tsv)
export ${ENV}_EXTERNAL_INGEST_FQDN=${EXTERNAL_INGEST_FQDN}
export ${ENV}_INGRESS_LOAD_BALANCER_IP=$(az network public-ip show --name ${EXTERNAL_INGEST_DNS_NAME}-${env}-pip --query "ipAddress" -g $RESOURCE_GROUP_NODE --output tsv)

# Deploy the ngnix ingress controller
helm install stable/nginx-ingress --name nginx-ingress-${env} --namespace ingress-controllers --set rbac.create=true --set controller.ingressClass=nginx-${env} --set controller.service.loadBalancerIP=${ENV}_INGRESS_LOAD_BALANCER_IP

openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -out ingestion-ingress-tls-${env}.crt \
    -keyout ingestion-ingress-tls-${env}.key \
    -subj "/CN=${EXTERNAL_INGEST_FQDN}/O=fabrikam"
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
sed -i \
    -e "s#ACR_SERVER_VAR_VAL#$ACR_SERVER#g" \
    -e "s#ACR_NAME_VAR_VAL#$ACR_NAME#g" \
    -e "s#AZURE_PIPELINES_SERVICE_CONN_NAME_VAR_VAL#$AZURE_PIPELINES_SERVICE_CONN_NAME#g" \
    ${pipelinePath}/azure-pipelines.yml
done

# push changes to the repo
cd $PROJECT_ROOT && \
git add -u && \
git commit -m "set build pipelines variables" && \
git push newremote master && \
cd -
```

## Add Delivery CI/CD

```
# add build definition
az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project $AZURE_DEVOPS_PROJECT_NAME \
   --name delivery-ci \
   --service-connection $AZURE_DEVOPS_SERVICE_CONN_ID \
   --yml-path src/shipping/delivery/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master

# query build definition details and resources
export AZURE_DEVOPS_DELIVERY_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='delivery-ci'].id" -o tsv) && \
export AZURE_DEVOPS_DELIVERY_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='delivery-ci'].queue.id" -o tsv) && \
for env in dev qa staging prod;do
ENV=${env^^}
export ${ENV}_DATABASE_NAME="deliveries-db"
export ${ENV}_COLLECTION_NAME="deliveries-col"
envIdentitiesDeploymentName="${ENV}_IDENTITIES_DEPLOYMENT_NAME"
export ${ENV}_DELIVERY_KEYVAULT_URI=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.deliveryKeyVaultUri.value -o tsv)
export ${ENV}_DELIVERY_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n ${!envIdentitiesDeploymentName} --query properties.outputs.deliveryPrincipalResourceId.value -o tsv)
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

curl -sL -w "%{http_code}" -X POST ${AZURE_DEVOPS_VSRM_ORG}/${AZURE_DEVOPS_PROJECT_NAME}/_apis/release/definitions?api-version=5.1-preview.3 \
     -d@${DELIVERY_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null
```

Kick off CI/CD pipelines

```bash
git checkout -b release/delivery/v0.1.0 && \
git push newremote release/delivery/v0.1.0
```

Verify delivery was deployed

```bash
helm status delivery-v0.1.0
```

## Add Package CI/CD

Create build and release pipeline definitions
```
# add build definitions
az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project $AZURE_DEVOPS_PROJECT_NAME \
   --name package-ci \
   --service-connection $AZURE_DEVOPS_SERVICE_CONN_ID \
   --yml-path src/shipping/package/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master

# query build definition details and resources
export AZURE_DEVOPS_PACKAGE_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='package-ci'].id" -o tsv) && \
export AZURE_DEVOPS_PACKAGE_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='package-ci'].queue.id" -o tsv) && \
for env in dev qa staging prod;do
ENV=${env^^}
export COSMOSDB_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.packageMongoDbName.value -o tsv)
export ${ENV}_COSMOSDB_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCE_GROUP --query "connectionStrings[0].connectionString" -o tsv | sed 's/==/%3D%3D/g')
export ${ENV}_COSMOSDB_COL_NAME=packages
done

# add relese definition
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

curl -sL -w "%{http_code}" -X POST ${AZURE_DEVOPS_VSRM_ORG}/${AZURE_DEVOPS_PROJECT_NAME}/_apis/release/definitions?api-version=5.1-preview.3 \
     -d@${PACKAGE_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null
```

Kick off CI/CD pipeline

```bash
git checkout -b release/package/v0.1.0 && \
git push newremote release/package/v0.1.0
```

Verify package was deployed

```bash
helm status package-v0.1.0
```

## Add Workflow CI/CD

```
# add build definitions
az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project $AZURE_DEVOPS_PROJECT_NAME \
   --name workflow-ci \
   --service-connection $AZURE_DEVOPS_SERVICE_CONN_ID \
   --yml-path src/shipping/workflow/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master

# query build definition details and resources
export AZURE_DEVOPS_WORKFLOW_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='workflow-ci'].id" -o tsv) && \
export AZURE_DEVOPS_WORKFLOW_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='workflow-ci'].queue.id" -o tsv) && \
for env in dev qa staging prod;do
ENV=${env^^}
envIdentitiesDeploymentName="${ENV}_IDENTITIES_DEPLOYMENT_NAME"
export ${ENV}_WORKFLOW_KEYVAULT_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.workflowKeyVaultName.value -o tsv)
export ${ENV}_WORKFLOW_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n ${!envIdentitiesDeploymentName} --query properties.outputs.workflowPrincipalResourceId.value -o tsv)
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

curl -sL -w "%{http_code}" -X POST ${AZURE_DEVOPS_VSRM_ORG}/${AZURE_DEVOPS_PROJECT_NAME}/_apis/release/definitions?api-version=5.1-preview.3 \
     -d@${WORKFLOW_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null
```

Kick off CI/CD pipeline

```bash
git checkout -b release/workflow/v0.1.0 && \
git push newremote release/workflow/v0.1.0
```

Verify workflow was deployed

```bash
helm status workflow-v0.1.0
```

## Add Ingestion CI/CD

Ingestion pre-requisites

Create build and release pipeline definitions
```
# add build definitions
az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project $AZURE_DEVOPS_PROJECT_NAME \
   --name ingestion-ci \
   --service-connection $AZURE_DEVOPS_SERVICE_CONN_ID \
   --yml-path src/shipping/ingestion/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master

# query build definition details and resources
export AZURE_DEVOPS_INGESTION_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='ingestion-ci'].id" -o tsv) && \
export AZURE_DEVOPS_INGESTION_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='ingestion-ci'].queue.id" -o tsv) && \
for env in dev qa staging prod;do
ENV=${env^^}
export {${ENV}_INGESTION_QUEUE_NAMESPACE,INGESTION_QUEUE_NAMESPACE}=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.ingestionQueueNamespace.value -o tsv)
export ${ENV}_INGESTION_QUEUE_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.ingestionQueueName.value -o tsv)
export INGESTION_ACCESS_KEY_NAME=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.ingestionServiceAccessKeyName.value -o tsv)
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

curl -sL -w "%{http_code}" -X POST ${AZURE_DEVOPS_VSRM_ORG}/${AZURE_DEVOPS_PROJECT_NAME}/_apis/release/definitions?api-version=5.1-preview.3 \
     -d@${INGESTION_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null
```

Kick off CI/CD pipeline

```bash
git checkout -b release/ingestion/v0.1.0 && \
git push newremote release/ingestion/v0.1.0
```

Verify ingestion was deployed

```bash
helm status ingestion-v0.1.0
```

## Add DroneScheduler CI/CD

Create build and release pipeline definitions
```
# add build definitions
az pipelines create \
   --organization $AZURE_DEVOPS_ORG \
   --project $AZURE_DEVOPS_PROJECT_NAME \
   --name dronescheduler-ci \
   --service-connection $AZURE_DEVOPS_SERVICE_CONN_ID \
   --yml-path src/shipping/dronescheduler/azure-pipelines.yml \
   --repository-type tfsgit \
   --repository $AZURE_DEVOPS_REPOS_NAME \
   --branch master

# query build definition details and resources
export AZURE_DEVOPS_DRONE_BUILD_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='dronescheduler-ci'].id" -o tsv) && \
export AZURE_DEVOPS_DRONE_QUEUE_ID=$(az pipelines build definition list --organization $AZURE_DEVOPS_ORG --project $AZURE_DEVOPS_PROJECT_NAME --query "[?name=='dronescheduler-ci'].queue.id" -o tsv) && \
for env in dev qa staging prod;do
ENV=${env^^}
envIdentitiesDeploymentName="${ENV}_IDENTITIES_DEPLOYMENT_NAME"
export ${ENV}_DRONESCHEDULER_PRINCIPAL_RESOURCE_ID=$(az group deployment show -g $RESOURCE_GROUP -n ${!envIdentitiesDeploymentName} --query properties.outputs.droneSchedulerPrincipalResourceId.value -o tsv)
envDroneSchedulerIdName="${ENV}_DRONESCHEDULER_ID_NAME"
export ${ENV}_DRONESCHEDULER_PRINCIPAL_CLIENT_ID=$(az identity show -g $RESOURCE_GROUP -n ${!envDroneSchedulerIdName} --query clientId -o tsv)
export ${ENV}_DRONESCHEDULER_KEYVAULT_URI=$(az group deployment show -g $RESOURCE_GROUP -n azuredeploy-${env} --query properties.outputs.droneSchedulerKeyVaultUri.value -o tsv)
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

curl -sL -w "%{http_code}" -X POST ${AZURE_DEVOPS_VSRM_ORG}/${AZURE_DEVOPS_PROJECT_NAME}/_apis/release/definitions?api-version=5.1-preview.3 \
     -d@${DRONE_PATH}/azure-pipelines-cd-0.json \
     -H "Authorization: Basic ${AZURE_DEVOPS_AUTHN_BASIC_TOKEN}" \
     -H "Content-Type: application/json" \
     -o /dev/null
```

Kick off CI/CD pipeline

```bash
git checkout -b release/dronescheduler/v0.1.0 && \
git push newremote release/dronescheduler/v0.1.0
```

Verify dronescheduler was deployed

```bash
helm status dronescheduler-v0.1.0
```

## Validate the application is running

You can resume the [the original deployment instructions](./deployment.md#validate-the-application-is-running) to validate the application is running.
