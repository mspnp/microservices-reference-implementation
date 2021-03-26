Param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    [Parameter(Mandatory=$true)]
    [string]$Subscription,
    [Parameter(Mandatory=$true)]
    [string]$Location,
    [Parameter(Mandatory=$true)]
    [string]$BingMapId
)

$userObjectId=$(az ad signed-in-user show --query objectId -o tsv)
if (!$userObjectId) { 
    az login | Out-Null 
    $userObjectId=$(az ad signed-in-user show --query objectId -o tsv)
}

Write-Host "Starting Setup in Subscription: $Subscription..."
az account set --subscription $Subscription | Out-Null
az group create -l $Location -n $ResourceGroup | Out-Null

$tenantId=$(az account show --query tenantId -o tsv)

$sb = [System.Text.StringBuilder]::new()
$(new-object System.Security.Cryptography.SHA256Managed | 
ForEach-Object {$_.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("$Subscription-$ResourceGroup"))} | 
ForEach-Object { $sb.Append($_.ToString("x2")) | out-null } )
$key = $sb.ToString().SubString(0, 13)
$keyVaultName = "Setup-$key"

$keyVaultList = $(az keyvault list --query [].name | ConvertFrom-Json)
if ($keyVaultList.Contains($keyVaultName)) 
{
   $keyvaultSecrets = $(az keyvault secret list --vault-name $keyVaultName --query [].name | ConvertFrom-Json)
   if ($keyvaultSecrets.Contains("DroneAppId")) {
      $appId = $(az keyvault secret show --name "DroneAppId" --vault-name $keyVaultName --query value -o tsv)
   }
   if ($keyvaultSecrets.Contains("DronePassword")) {
      $password = $(az keyvault secret show --name "DronePassword" --vault-name $keyVaultName --query value -o tsv)
   }
}

if ((!$appId) -or (!$password)) {
    az keyvault create --name $keyVaultName --resource-group $ResourceGroup --location $Location | out-null
    az keyvault set-policy -n $keyVaultName --secret-permissions get list set --object-id $userObjectId | Out-null
    $details=$(az ad sp create-for-rbac -n "Drone-Demo-${ResourceGroup}" --role Owner --scopes /subscriptions/$Subscription) | ConvertFrom-Json
    $appId = $details.appId;
    $password = $details.password
    az keyvault secret set --name "DroneAppId" --vault-name $keyVaultName --value $appId | Out-Null
    az keyvault secret set --name "DronePassword" --vault-name $keyVaultName --value $password | Out-Null
}

docker run -t -d --name $ResourceGroup --privileged  replyvalorem/aksdemodeployment:1.1

$imageId=$(docker ps --format "{{.Names}}\t{{.ID}}\t{{.Status}}\t{{.Ports}}" |
     ConvertFrom-CSV -Delimiter "`t" -Header ("Names","Id","Status","Ports") |
     Sort-Object Names |
     Where-Object { $_.Names -eq $ResourceGroup } |
     Select-Object Id)

$imageId = $imageId.id
     
docker cp ${env:USERPROFILE}/.ssh/id_rsa ${imageid}:/id_rsa
docker cp ${env:USERPROFILE}/.ssh/id_rsa.pub ${imageid}:/id_rsa.pub
docker cp install-drone-demo.sh ${imageid}:/install-drone-demo.sh

$pwdLength = $password.length

Write-Host "Subscription: $Subscription"
Write-Host "Location: $Location"
Write-Host "ResourceGroup: $ResourceGroup"
Write-Host "BingMapId: $BingMapId"
Write-Host "AppId: $appId"
Write-Host "Password Length: $pwdLength"
Write-Host "TenantId: $tenantId"
Write-Host "UserObjectId: $userObjectId"
Write-Host
Write-Host "Starting Deployment Script..."
Write-Host

docker exec -it $imageid /install-drone-demo.sh -s $Subscription -l $Location -r $ResourceGroup -b $BingMapId -a $appId -p $password -t $tenantId -u $userObjectId

docker exec -it $imageid bash
