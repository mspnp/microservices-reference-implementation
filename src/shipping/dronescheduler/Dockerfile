FROM microsoft/dotnet:2.2-aspnetcore-runtime as base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build

MAINTAINER Fernando Antivero (https://github.com/ferantivero)

WORKDIR /app
COPY delivery/Fabrikam.DroneDelivery.Common/*.csproj ./Fabrikam.DroneDelivery.Common/
COPY dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/*.csproj ./Fabrikam.DroneDelivery.DroneScheduler/
WORKDIR /app
RUN dotnet restore /app/Fabrikam.DroneDelivery.Common/
RUN dotnet restore /app/Fabrikam.DroneDelivery.DroneScheduler/

WORKDIR /app
COPY delivery/Fabrikam.DroneDelivery.Common/. ./Fabrikam.DroneDelivery.Common/
COPY dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/. ./Fabrikam.DroneDelivery.DroneScheduler/

FROM build AS publish

MAINTAINER Fernando Antivero (https://github.com/ferantivero)

WORKDIR /app
RUN dotnet publish /app/Fabrikam.DroneDelivery.DroneScheduler/ -c Release -o ../out

FROM base AS runtime

MAINTAINER Fernando Antivero (https://github.com/ferantivero)

LABEL Tags="Azure,AKS,DroneDelivery"

ARG user=deliveryuser

RUN useradd -m -s /bin/bash -U $user

WORKDIR /app
COPY --from=publish /app/out ./
COPY dronescheduler/scripts/. ./
RUN \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh

RUN chown -R $user.$user /app

# Set it for subsequent commands
USER $user

ENTRYPOINT ["/bin/bash", "/app/run.sh"]
