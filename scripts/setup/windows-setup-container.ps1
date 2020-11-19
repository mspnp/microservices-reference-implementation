Param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup
)

docker run -t -d --name $ResourceGroup --privileged  replyvalorem/aksdemodeployment:1.1 bash

$imageId=$(docker ps --format "{{.Names}}\t{{.ID}}\t{{.Status}}\t{{.Ports}}" |
     ConvertFrom-CSV -Delimiter "`t" -Header ("Names","Id","Status","Ports") |
     Sort-Object Names |
     Where-Object { $_.Names -eq $ResourceGroup } |
     Select-Object Id).Id

docker cp ${env:USERPROFILE}/.ssh/id_rsa ${imageid}:/id_rsa
docker cp ${env:USERPROFILE}/.ssh/id_rsa.pub ${imageid}:/id_rsa.pub
docker cp install-drone-demo.sh ${imageid}:/install-drone-demo.sh

docker exec -it $imageid bash
