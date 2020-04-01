# Package service

## Create local development environment

1. Install Docker and docker-compose
2. From a bash CLI navigate to the project root and type `./up.sh`
3. try ```curl -X PUT --header 'Accept: application/json' 'http://localhost:7080/api/packages/42'```

> Known issue:
> ```'failed to connect to server [packagedb:27017] on first connect [MongoError: connect ECONNREFUSED 172.24.0.2:27017]'```
>  First time the package service starts, it might fail connecting to mongo. Package would automatically restart if needed.
