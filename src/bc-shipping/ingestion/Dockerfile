FROM openjdk:8-jre-alpine

ENTRYPOINT ["/usr/bin/java", "-jar", "/usr/share/dronedelivery/DeliveryScheduler-1.0.0-SNAPSHOT.jar"]

# Add Maven dependencies (not shaded into the artifact; Docker-cached)
COPY target /usr/share/dronedelivery
