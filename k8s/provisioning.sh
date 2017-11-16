#!/bin/bash
#  ------------------------------------------------------------
#   Copyright (c) Microsoft Corporation.  All rights reserved.
#   Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
#  ------------------------------------------------------------


echo "______                       ______     _ _                      "
echo "|  _  \                      |  _  \   | (_)                     "
echo "| | | |_ __ ___  _ __   ___  | | | |___| |___   _____ _ __ _   _ "
echo "| | | | '__/ _ \| '_ \ / _ \ | | | / _ \ | \ \ / / _ \ '__| | | |"
echo "| |/ /| | | (_) | | | |  __/ | |/ /  __/ | |\ V /  __/ |  | |_| |"
echo "|___/ |_|  \___/|_| |_|\___| |___/ \___|_|_| \_/ \___|_|   \__, |"
echo "                                                            __/ |"
echo "                                                           |___/ "

# get script path
SCRIPTDIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
echo
echo "Welcome to the Microsoft Fabrikam Drone Delivery Reference Implementation provisioning!"
echo 
# only ask if in interactive mode
if [[ -t 0 ]];then

   echo -n "namespace ? [bc-shipping] "
   read NAMESPACE

   echo "Please enter the following data/secrets:"
   echo "Delivery Service"

   stty -echo
   printf "  Azure CosmosDB Endpoint (secret) ? "
   read CosmosDB_Endpoint
   stty echo
   printf "\n"

   echo -n "  Azure CosmosDB DatabaseId ? "
   read CosmosDB_DatabaseId

   echo -n "  Azure CosmosDB CollectionId ? "
   read CosmosDB_CollectionId
   
   stty -echo
   printf "  Azure CosmosDB Key (secret) ? "
   read CosmosDB_Key
   stty echo
   printf "\n"

   printf "  Azure Redis Host Name (secret) ? "
   stty -echo
   read Redis_HostName
   stty echo
   printf "\n"
   
   printf "  Azure Redis PrimaryKey (secret) ? "
   stty -echo
   read Redis_PrimaryKey
   stty echo
   printf "\n"

   printf "  Azure Event Hub Connection Stringe (secret)? "
   stty -echo
   read EH_ConnectionString
   stty echo
   printf "\n"

   echo -n "  Azure Event Hub Entity Path ? "
   read EH_EntityPath 

   echo "Package Service"

   stty -echo
   printf "  MongoDB connection string ? "
   read MongoDB_ConnectionString
   stty echo
   printf "\n"
fi

if [[ -z ${NAMESPACE} ]];then
  NAMESPACE=bc-shipping
fi

MISSINGINFO=0
if [[ -z ${CosmosDB_Key} ]];then
  echo >&2 'error: missing Azure Cosmos Db key secret value'
  MISSINGINFO=1;
fi

if [[ -z ${CosmosDB_Endpoint} ]];then
  echo >&2 'error: missing Azure Cosmos Db Endpoint secret value'
  MISSINGINFO=1;
fi

if [[ -z ${CosmosDB_DatabaseId} ]];then
  echo >&2 'error: missing Azure Cosmos Db DatabaseId value'
  MISSINGINFO=1;
fi

if [[ -z ${CosmosDB_CollectionId} ]];then
  echo >&2 'error: missing Azure Cosmos Db CollectionId value'
  MISSINGINFO=1;
fi

if [[ -z ${Redis_HostName} ]];then
  echo >&2 'error: missing Azure Redis Host Name secret value'
  MISSINGINFO=1;
fi

if [[ -z ${Redis_PrimaryKey} ]];then
  echo >&2 'error: missing Azure Redis Primary Key secret value'
  MISSINGINFO=1;
fi

if [[ -z ${EH_ConnectionString} ]];then
  echo >&2 'error: missing Azure Event Hub Connection String secret value'
  MISSINGINFO=1;
fi

if [[ -z ${EH_EntityPath} ]];then
  echo >&2 'error: missing Azure Event Hub Entity Path value' 
  MISSINGINFO=1;
fi

if [[ -z ${MongoDB_ConnectionString} ]];then
  echo >&2 'error: missing MongoDB connection string'
  MISSINGINFO=1;
fi

if [[ ${MISSINGINFO} !=  0 ]];then
    exit ${MISSINGINFO}
fi

OUTPUT=$(mktemp)

function err_handler()
{
  echo  >> "${OUTPUT}"
  echo "Fabrikam Drone Delivery Reference Implementation provisioning finished with errors!" >> "${OUTPUT}"
  echo  >> "${OUTPUT}"
  exit 1
}

function cleanup() {
  cat "${OUTPUT}"
  rm -f "${OUTPUT}"
}

trap "err_handler" ERR
trap "cleanup" EXIT

echo 
echo "Fabrikam Drone Delivery Reference Implementation provisioning started..."
echo 
echo "NAMESPACE: ${NAMESPACE}"
echo 

sed -i "s/value: \"CosmosDB_DatabaseId\"/value: \"${CosmosDB_DatabaseId}\"/g"      "$SCRIPTDIR/delivery.yaml"
sed -i "s/value: \"CosmosDB_CollectionId\"/value: \"${CosmosDB_CollectionId}\"/g"  "$SCRIPTDIR/delivery.yaml"
sed -i "s/value: \"EH_EntityPath\"/value: \"${EH_EntityPath}\"/g"                  "$SCRIPTDIR/delivery.yaml"

echo
# Create namespace
if [[ "${NAMESPACE}" != default ]];then
    kubectl create namespace "${NAMESPACE}" >> "${OUTPUT}" 2>&1
fi
# Create Secrets
kubectl create -n "${NAMESPACE}" --save-config=true secret generic delivery-storageconf --from-literal=CosmosDB_Key="${CosmosDB_Key}" --from-literal=CosmosDB_Endpoint="${CosmosDB_Endpoint}" --from-literal=Redis_HostName="${Redis_HostName}" --from-literal=Redis_PrimaryKey="${Redis_PrimaryKey}" --from-literal=EH_ConnectionString="${EH_ConnectionString}" --from-literal=Redis_SecondaryKey= >> "${OUTPUT}" 2>&1

kubectl create -n ${NAMESPACE} secret generic package-secrets --from-literal=mongodb-pwd=${MongoDB_ConnectionString} > ${OUTPUT} 2>&1

# Deploy Services
kubectl apply -n "${NAMESPACE}" -f "$SCRIPTDIR"/ >> "${OUTPUT}" 2>&1
echo  >> "${OUTPUT}" 2>&1
# Print summary
kubectl get all -n "${NAMESPACE}" -l bc=shipping >> "${OUTPUT}" 2>&1
echo >> "${OUTPUT}" 2>&1

echo 
echo "Fabrikam Drone Delivery Reference Implementation provisioning done!"
echo
