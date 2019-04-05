FROM microsoft/dotnet:2.2-aspnetcore-runtime as base
WORKDIR /app
EXPOSE 8080

FROM microsoft/dotnet:2.2-sdk AS build

WORKDIR /app
COPY Fabrikam.DroneDelivery.Common/*.csproj ./Fabrikam.DroneDelivery.Common/
COPY Fabrikam.DroneDelivery.DeliveryService/*.csproj ./Fabrikam.DroneDelivery.DeliveryService/
WORKDIR /app
RUN dotnet restore /app/Fabrikam.DroneDelivery.Common/
RUN dotnet restore /app/Fabrikam.DroneDelivery.DeliveryService/

WORKDIR /app
COPY Fabrikam.DroneDelivery.Common/. ./Fabrikam.DroneDelivery.Common/
COPY Fabrikam.DroneDelivery.DeliveryService/. ./Fabrikam.DroneDelivery.DeliveryService/

FROM build AS testrunner

WORKDIR /app/tests
COPY Fabrikam.DroneDelivery.DeliveryService.Tests/*.csproj .
WORKDIR /app/tests
RUN dotnet restore /app/tests/

WORKDIR /app/tests
COPY Fabrikam.DroneDelivery.DeliveryService.Tests/. .
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM build AS publish

MAINTAINER Fernando Antivero (https://github.com/ferantivero)

WORKDIR /app
RUN dotnet publish /app/Fabrikam.DroneDelivery.DeliveryService/ -c Release -o ../out

FROM base AS runtime

MAINTAINER Fernando Antivero (https://github.com/ferantivero)

LABEL Tags="Azure,AKS,DroneDelivery"

ARG user=deliveryuser

RUN useradd -m -s /bin/bash -U $user

WORKDIR /app
COPY --from=publish /app/out ./
COPY scripts/. ./
RUN \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh

RUN chown -R $user.$user /app

# Set it for subsequent commands
USER $user

ENTRYPOINT ["/bin/bash", "/app/run.sh"]