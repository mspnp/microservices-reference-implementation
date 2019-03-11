docker-compose -p drone-package -f ./docker-compose.dev.yaml down

# cleanup the network if there are not containers using it
if [ "$(docker network inspect dronedelivery --format "{{range .Containers}}T{{end}}")" == "" ]; then
docker network rm dronedelivery
fi
