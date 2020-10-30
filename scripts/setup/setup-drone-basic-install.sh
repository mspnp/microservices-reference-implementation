function print_help { echo $'Usage\n\n -r Resource Group\n -? Show Usage' >&2; }

while getopts r:? option
do
case "${option}"
in
r) RESOURCEGROUP=${OPTARG};;
?) print_help; exit 0;;
esac
done

if [ -z "$RESOURCEGROUP" ]
then
print_help;
exit 2
fi

docker run -t -d --privileged --name $RESOURCEGROUP replyvalorem/aksdemodeployment:1.1
export imageid=$(docker ps | grep $RESOURCEGROUP | awk '{ print $1 }')

docker cp ~/.ssh/id_rsa $imageid:/id_rsa
docker cp ~/.ssh/id_rsa.pub $imageid:/id_rsa.pub
docker cp run-drone-basic-install.sh $imageid:/run-drone-basic-install.sh

docker exec -it $imageid bash