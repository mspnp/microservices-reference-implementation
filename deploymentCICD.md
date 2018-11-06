# Deploying the Reference Implementation

## Prerequisites
TODO: use azure shell
- Azure suscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Helm 2.11.1](#)
- Azure Pipelines CLI: See the [install steps](https://docs.microsoft.com/en-us/cli/vsts/install?view=vsts-cli-latest) for instructions on installing the VSTS CLI on Windows, Linux, or Mac.
1. [create Azure DevOps account](https://azure.microsoft.com/en-us/services/devops)
2. create a new Azure DevOps organization and a [personal access token](https://docs.microsoft.com/vsts/accounts/use-personal-access-tokens-to-authenticate)
2. [add Azure subscription as service connection](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure?view=vsts#create-an-azure-resource-manager-service-connection-with-an-existing-service-principal)
3. [optionally, assign service connection application to role, so it is allowed to create new azure resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal#assign-application-to-role)

Create a new Azure Repo

```bash
# login in Azure DevOps
vsts login --token <token> # use the token created in prerequisite step <number>th

# configure as default the new organization create in prerequisite step <number>th
vsts configure --defaults instance=https://dev.azure.com/<organization name>/

# create repo
vsts code repo create <name>
```

Clone or download this repo locally

```bash
git clone https://github.com/mspnp/<repo>.git && \
cd <repo> && \
git remote add <remote-name> <remote-url> # this remote url corresponds to the prerequisite step 4th
```
Export the following environment variables

```
export SERVICECONNECTION=<service-connection-name> # use the name configured in the 2nd prerequisite step
export LOCATION=<location>
export UNIQUE_APP_NAME_PREFIX=[YOUR_UNIQUE_APPLICATION_NAME_HERE]

export RESOURCE_GROUP="${UNIQUE_APP_NAME_PREFIX}-rg" && \
export ACR_NAME="${UNIQUE_APP_NAME_PREFIX}-acr" && \
export AKS_NAME="${UNIQUE_APP_NAME_PREFIX}-cluster"
```

The deployment steps shown here use Bash shell commands. On Windows, you can use the [Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/about) to run Bash.

## Create the Kubernetes cluster

Provision a Kubernetes cluster in AKS
TODO: use arm and deploy aks and other infrastructure needed
```bash
# Log in to Azure
az login

# Create a resource group for AKS
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create the AKS cluster
az aks create --resource-group $RESOURCE_GROUP --name $AKS_NAME --node-count 4 --enable-addons monitoring --generate-ssh-keys

# Install kubectl
sudo az aks install-cli

# Get the Kubernetes cluster credentials
az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$AKS_NAME

# Create the ACR instance
az acr create --name $ACR_NAME --resource-group $RESOURCE_GROUP --sku Basic

# Add a repository to Helm client
az acr helm repo add -n $ACR_NAME

# Install Helm
helm init --upgrade
```

## Setup CICD Pipeline using Azure Pipelines

Replace azure pipeline place holders

```
sed -i "s#AzureSubscription: <azure resource manager>#AzureSubscription: $SERVICECONNECTION#g" azure-pipelines.yml && \
sed -i "s#Location: <location>#Location: $LOCATION#g"  azure-pipelines.yml && \
sed -i "s#ResourceGroup: <resource group>#ResourceGroup: $RESOURCEGROUP#g" azure-pipelines.yml && \
sed -i "s#AzureContainerRegistry: <container registry name>#AzureContainerRegistry: $ACR_NAME#g" azure-pipelines.yml && \
sed -i "s#AzureKubernetesService: <aks name>#AzureKubernetesService: $AKS_NAME#g" azure-pipelines.yml
```

Push changes to azure repos or github

```bash
git push <remote-name> master
```

Follow instructions below to configure your first Azure Pipeline

[Get your first build with Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/get-started-yaml?view=vsts#get-your-first-build)

> Note: this first build will attemp to execute the azurepipeline.yml against master

Trigger the CICD pipeline by pushing to staging

```
git checkout -b staging && \
git push <remote-name> staging
```

> Note: also feature branches are going through the CI pipeline.

Follow CICD from Azure Pipelines

```
open https://dev.azure.com/<organization-name>/<project-name>/_build
```
