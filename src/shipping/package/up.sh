#!/bin/bash

DOCKER_VER= docker version --format '{{.Client.Version}}'
echo "Docker version ${DOCKER_VER}"

# create a network if this does not exist
if ! docker network ls | grep -q dronedelivery; then
docker network create dronedelivery
fi

docker-compose -p drone-package -f ./docker-compose.dev.yaml up -d

docker attach dronepackage_app_1
