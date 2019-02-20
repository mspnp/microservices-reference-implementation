FROM openjdk:8-jre-alpine as base
WORKDIR /usr/src/app
EXPOSE 80

FROM maven:3.5-jdk-8-slim as build
WORKDIR /usr/src/app

COPY pom.xml ./
COPY settings-docker.xml /usr/share/maven/ref/
RUN /usr/local/bin/mvn-entrypoint.sh \
    mvn package -Dmaven.test.skip=true -Dcheckstyle.skip=true -Dmaven.javadoc.skip=true --fail-never
COPY . .
RUN mvn package -Dmaven.test.skip=true -Dcheckstyle.skip=true -Dmaven.javadoc.skip=true

FROM base as final

WORKDIR /app
COPY --from=build /usr/src/app/target ./

ENTRYPOINT ["java","-jar","ingestion-0.1.0.jar"]
