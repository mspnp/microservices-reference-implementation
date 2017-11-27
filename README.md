## Provisioning

this section is meant to provide guidance on how to get provisioned Drone Delivery Reference Implementation
into your ACS Kubernetes cluster.

The Drone Delivery Reference Implementation is composed by 7 microservices:

* *account*
* *delivery*
* *scheduler*
* *ingestion*
* *dronescheduler*
* *package*
* *thirdparty*

### Prerequisites

a. install [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

b. install kubeclt
   ```bash
    sudo az acs kubernetes install-cli --client-version=1.7.7
   ```
   *Important*: Please note that you might need to use a higher client version depending on the cluster version

c. set the following environment variables:
   ```bash
    export LOCATION=your_location_here && \
    export RESOURCE_GROUP=your_resource_group_here && \
    export CLUSTER_NAME=your_cluster_name_here && \
   ```
   Note: the creation of your cluster might take some time

d. login in your azure subscription:
   ```bash
   az login
   ```

e. create the resource group:
   ```bash
   az group create --name=$RESOURCE_GROUP --location=$LOCATION
   ```

f. create your ACS Cluster:
   ```bash
   az acs create --orchestrator-type=kubernetes \
                 --resource-group $RESOURCE_GROUP \
                 --name=$CLUSTER_NAME \
                 --generate-ssh-keys
   ```

g. check your ACS cluster has been created:
   ```bash
   az acs show -g $RESOURCE_GROUP -n $CLUSTER_NAME
   ```

h. download the credentails:
   ```bash
   az acs kubernetes get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME
   ```

i. test the cluster is up and running:
   ```bash
   kubectl get nodes
   ```

j. Deploy the following Azure Resources:

   1. delivery microservice:
   [TBD] azure templates or az steps

   2. package microservice:
   [TBD] azure templates or az steps

   3. ingestion/scheduler:
   [TBD] azure templates or az steps

k. (Optional) add linkerd to your cluster:
   ```bash
   wget https://raw.githubusercontent.com/linkerd/linkerd-examples/master/k8s-daemonset/k8s/linkerd.yml && \
   sed -i "s/default/bc-shipping/g" linkerd.yml && \
   kubectl apply -f linkerd.yml
   ```

### Deploying the application

1. Clone the repository
   ```bash
   git clone https://github.com/mspnp/microservices-reference-implementation.git
   ```

2. Exectute the first time provisioning script and follow the instructions
   ```bash
   ./microservices-reference-implementation/k8s/provisioning.sh
   ```

3. Check Drone Delivery is up and running:
   ```bash
   Fabrikam Drone Delivery Reference Implementation provisioning started...

   NAMESPACE: default
   secret "delivery-storageconf" created
   service "account" created
   deployment "account" created
   service "delivery" created
   deployment "delivery" created
   service "deliveryscheduler" created
   deployment "deliveryscheduler" created
   service "dronescheduler" created
   deployment "dronescheduler" created
   service "thirdparty" created
   deployment "thirdparty" created

   NAME                                    READY     STATUS              RESTARTS   AGE
   po/account-1032534693-9gsdb             1/1       Running             0          6s
   po/delivery-1092888057-76wdt            1/1       Running             0          5s
   po/deliveryscheduler-1372145804-zgq2m   1/1       Running             0          5s
   po/dronescheduler-1287209820-v4hc0      1/1       Running             0          4s
   po/thirdparty-3105374237-g4520          1/1       Running             0          3s

   NAME                    CLUSTER-IP   EXTERNAL-IP   PORT(S)        AGE
   svc/account             None         <none>        80/TCP         7s
   svc/delivery            None         <none>        80/TCP         6s
   svc/deliveryscheduler   10.0.1.134   <pending>     80:30734/TCP   5s
   svc/dronescheduler      None         <none>        80/TCP         4s
   svc/thirdparty          None         <none>        80/TCP         3s

   NAME                       DESIRED   CURRENT   UP-TO-DATE   AVAILABLE   AGE
   deploy/account             1         1         1            1           6s
   deploy/delivery            1         1         1            1           5s
   deploy/deliveryscheduler   1         1         1            1           5s
   deploy/dronescheduler      1         1         1            1           4s
   deploy/thirdparty          1         1         1            1           3s

   NAME                              DESIRED   CURRENT   READY     AGE
   rs/account-1032534693             1         1         1         6s
   rs/delivery-1092888057            1         1         1         5s
   rs/deliveryscheduler-1372145804   1         1         1         5s
   rs/dronescheduler-1287209820      1         1         1         4s
   rs/thirdparty-3105374237          1         1         1         3s

   Fabrikam Drone Delivery Reference Implementation provisioning done!
   ```

### Deploying the application (the hard way)
this section is just walkthrough of the steps being executed in the provisioning script:

1. clone the repository
   ```bash
   git clone https://github.com/mspnp/microservices-reference-implementation.git
   ```

2. Create the bc-shipping namespace:
   ```bash
   kubeclt create namespace bc-shipping
   ```

2. Create the required secrets in your cluster:
   ```bash
   kubectl --namespace bc-shipping create --save-config=true secret generic delivery-storageconf | \
                  --from-literal=CosmosDB_Key=your_cosmosdb_key | \
                  --from-literal=CosmosDB_Endpoint=your_cosmosdb_endpoint | \
                  --from-literal=Redis_HostName=your_redis_hostname | \
                  --from-literal=Redis_PrimaryKey=your_redis_primarykey | \
                  --from-literal=EH_ConnectionString=your_eventhub_connstr | \
                  --from-literal=Redis_SecondaryKey=your_redis_secondarykey

   kubectl create secret generic package-secrets --from-literal=mongodb-pwd=your_mongodb_connection_string
   ```

3. Deploy the Drone Delivery microservices in your cluster:
   ```bash
   kubectl --namespace bc-shipping apply -f ./microservices-reference-implementation/k8s/
   ```

4. Check Drone Delivery is up and running:
   ```bash
   kubectl get all -l bc=shipping
   ```

### Testing Drone Delivery in your ACS Kubernetes cluster

  [TBD]
