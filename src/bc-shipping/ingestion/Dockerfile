FROM openjdk:8-jre-alpine

ENTRYPOINT ["/usr/bin/java", "-jar", "/usr/share/dronedelivery/Ingestion-1.0.0.jar"]

# Add Maven dependencies (not shaded into the artifact; Docker-cached)
COPY target /usr/share/dronedelivery
