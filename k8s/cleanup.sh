#!/bin/bash
#  ------------------------------------------------------------
#   Copyright (c) Microsoft Corporation.  All rights reserved.
#   Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
#  ------------------------------------------------------------

# get script path
SCRIPTDIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

# only ask if in interactive mode
if [[ -t 0 ]];then
   echo -n "namespace ? [bc-shipping] "
   read NAMESPACE
fi

if [[ -z ${NAMESPACE} ]];then
   NAMESPACE=bc-shipping
fi

echo "NAMESPACE: ${NAMESPACE}"

OUTPUT=$(mktemp)
echo "Fabrikam Drone Delivery Reference Implementation cleanup..."
{
  kubectl delete -n "${NAMESPACE}" secret delivery-storageconf 
  kubectl delete -n "${NAMESPACE}" -f "$SCRIPTDIR"/ 
  if [[ "${NAMESPACE}" != default ]];then
      kubectl delete namespace  "${NAMESPACE}"
  fi
} >> "${OUTPUT}" 2>&1

ret=$?
function cleanup() {
  rm -f "${OUTPUT}"
}

trap cleanup EXIT

if [[ "${ret}" -eq 0 ]];then
  cat "${OUTPUT}"
else
# ignore NotFound errors
  OUT2=$(grep -v NotFound "${OUTPUT}")
  if [[ ! -z "${OUT2}" ]];then
    cat "${OUTPUT}"
    exit "${ret}"
  fi
fi

echo "Fabrikam Drone Delivery Reference Implementation cleanup successful"
