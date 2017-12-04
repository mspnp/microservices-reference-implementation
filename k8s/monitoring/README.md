# Monitoring Microservices  with Prometheus: for node-exporter linkerd and Kubernetes statistics
Microsoft patterns & practices

https://docs.microsoft.com/azure/architecture/microservices

---

## Prerequisites

- Azure storage account

## Create the storage account.

The storage account has to be created in the same resource group of prometheus 

Set the account name in storage.yaml under prometheus folder.

storageaccount:  storageaccountName

Set the persistent volume claim name in files persistenVolumeClaim.yaml in monitoring under prometheus 
and grafana folders. Persistent volume claim names have to be different for prometheus and grafana.

  name: PersistentVolumeClaimName
  
Set the persistent volume claim name in files prometheus.yaml and grafana.yaml
  
         persistentVolumeClaim:
            claimName: PersistentVolumeClaimName
			
Create username and password encoded in base64 for grafana by running
below commands:

echo -n grafanausername | base64

echo -n grafanapassword  | base24

Open secret.yaml under grafana folder replace  with above encoded values

grafana-admin-password

grafana-admin-user

## Install Prometheus and grafana

### Prometheus

kubectl apply -f storage.yaml

kubectl apply -f persistentVolumeClaim.yaml

kubectl apply -f kube-state-metrics.yaml

kubectl apply -f node-exporter.yaml

kubectl apply -f prometheusconfigmap.yaml

kubectl apply -f prometheus.yaml


### Grafana


kubectl apply -f persistentVolumeClaim.yaml

kubectl apply -f secret.yaml

kubectl apply -f grafanaconfigmap.yaml

kubectl apply -f grafana.yaml
  

`