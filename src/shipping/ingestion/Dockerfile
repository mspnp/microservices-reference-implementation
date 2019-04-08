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

ADD https://github.com/Microsoft/ApplicationInsights-Java/releases/download/2.3.0/applicationinsights-agent-2.3.0.jar ./appinsights/applicationinsights-agent-2.3.0.jar
ADD /etc/AI-Agent.xml ./appinsights/AI-Agent.xml

COPY --from=build /usr/src/app/target/ingestion-0.1.0.jar ./

ENTRYPOINT ["java","-Djava.security.egdfile=file:/dev/./urandom","-javaagent:/app/appinsights/applicationinsights-agent-2.3.0.jar","-jar","ingestion-0.1.0.jar"]
