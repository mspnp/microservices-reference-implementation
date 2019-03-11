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
