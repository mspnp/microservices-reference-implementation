# Package service

## Create local development environment

1. Install Docker
2. From a bash CLI navigate to the project root and type `./up.sh`
3. From the app environment type `npm install`
4. To run the app in the local dev environment, type `npm start`
5. The app listens on port 80. In the dev environment, this is mapped to localhost:7080

## Build docker image

```
docker build -f ./Dockerfile -t <repo>/package-service .
```

## Provision database and create secrets

1. In Azure, create a Cosmos DB database with MongoDB API
2. Install Azure CLI 2.0
3. Run `az login`
4. Run create-secrets.sh

