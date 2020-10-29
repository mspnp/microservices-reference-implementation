docker run -t -d --privileged basicinstall:latest
export imageid=$(docker ps | grep 'basicinstall:latest' | awk '{ print $1 }')

docker cp ~/.ssh/id_rsa_console $imageid:/id_rsa
docker cp ~/.ssh/id_rsa_console.pub $imageid:/id_rsa.pub

docker exec -it $imageid bash