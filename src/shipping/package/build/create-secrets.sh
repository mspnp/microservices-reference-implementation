#! /bin/bash

CosmosDB=$1
ResourceGroup=$2

# Get the CosmosDB connection string
ConnectionString=`az cosmosdb list-connection-strings --name $CosmosDB --resource-group $ResourceGroup --query "connectionStrings[0].connectionString"` 

# Trim quotes
ConnectionString="${ConnectionString%\"}"
ConnectionString="${ConnectionString#\"}"

# Create the app secret
echo 'Creating k8s secrets'
kubectl create secret generic package-secrets --from-literal=mongodb-pwd=$ConnectionString