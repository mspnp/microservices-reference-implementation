FROM openjdk:8-jdk-alpine
MAINTAINER Kirpa Singh <kirpas@microsoft.com>

ENTRYPOINT ["/usr/bin/java", "-jar", "/usr/share/dronedelivery/dronedelivery.delivery-scheduler-service-0.0.1-SNAPSHOT.jar"]

# Add Maven dependencies (not shaded into the artifact; Docker-cached)
ADD target/lib /usr/share/dronedelivery/lib

# Add the service itself
ADD target/dronedelivery.delivery-scheduler-service-0.0.1-SNAPSHOT.jar /usr/share/dronedelivery/dronedelivery.delivery-scheduler-service-0.0.1-SNAPSHOT.jar


