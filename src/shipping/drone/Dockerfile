#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Drone.csproj", ""]
RUN dotnet restore "./Drone.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Drone.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Drone.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ARG DRONEINGRESSURL
ENV DRONE_INGRESS_URL=${DRONEINGRESSURL}

ARG TRACKINGID
ENV TRACKING_ID=${TRACKINGID}

ENTRYPOINT ["dotnet", "Fabrikam.DroneDelivery.Drone.dll"]
