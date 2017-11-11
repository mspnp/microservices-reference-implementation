#!/bin/bash
#  ------------------------------------------------------------
#   Copyright (c) Microsoft Corporation.  All rights reserved.
#   Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
#  ------------------------------------------------------------

# get script path
SCRIPTDIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

# only ask if in interactive mode
if [[ -t 0 ]];then
   echo -n "namespace ? [default] "
   read NAMESPACE
fi

if [[ -z ${NAMESPACE} ]];then
   NAMESPACE=default
fi

echo "NAMESPACE: ${NAMESPACE}"

export OUTPUT=$(mktemp)
echo "Fabrikam Drone Delivery Reference Implementation cleanup..."
kubectl delete secret delivery-storageconf  > ${OUTPUT} 2>&1
kubectl delete -n ${NAMESPACE} -f $SCRIPTDIR/dronedelivery.yaml >> ${OUTPUT} 2>&1
ret=$?
function cleanup() {
  rm -f ${OUTPUT}
}

trap cleanup EXIT

if [[ ${ret} -eq 0 ]];then
  cat ${OUTPUT}
else
# ignore NotFound errors
  OUT2=$(grep -v NotFound ${OUTPUT})
  if [[ ! -z ${OUT2} ]];then
    cat ${OUTPUT}
    exit ${ret}
  fi
fi

echo "Fabrikam Drone Delivery Reference Implementation cleanup successful"
