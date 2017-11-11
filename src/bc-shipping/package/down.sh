docker-compose -p drone-package -f ./build/docker-compose.yml down

# cleanup the network if there are not containers using it
if [ "$(docker network inspect dronedelivery --format "{{range .Containers}}T{{end}}")" == "" ]; then
docker network rm dronedelivery
fi