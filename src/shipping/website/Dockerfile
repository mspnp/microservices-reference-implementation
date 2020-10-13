#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true  
ENV ASPNETCORE_URLS=http://+:8080  
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src

COPY ./*.sln  ./
WORKDIR /src
COPY Fabrikam.DroneDelivery.ApiClient/*.* ./Fabrikam.DroneDelivery.ApiClient/
COPY Fabrikam.DroneDelivery.WebSite/*.* ./Fabrikam.DroneDelivery.WebSite/

# Setup NodeJs
RUN apt-get update && \
    apt-get install -y wget && \
    apt-get install -y gnupg2 && \
    wget -qO- https://deb.nodesource.com/setup_12.x | bash - && \
    apt-get install -y build-essential nodejs

WORKDIR /src
RUN dotnet restore "/src/Fabrikam.DroneDelivery.ApiClient/Fabrikam.DroneDelivery.ApiClient.csproj"
RUN dotnet restore "/src/Fabrikam.DroneDelivery.WebSite/Fabrikam.DroneDelivery.WebSite.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "/src/Fabrikam.DroneDelivery.ApiClient/Fabrikam.DroneDelivery.ApiClient.csproj" -c Release -o /src/build
RUN dotnet build "/src/Fabrikam.DroneDelivery.WebSite/Fabrikam.DroneDelivery.WebSite.csproj" -c Release -o /src/build

WORKDIR /src/Fabrikam.DroneDelivery.WebSite/ClientApp
RUN npm install
RUN npm build

WORKDIR "/src/Fabrikam.DroneDelivery.WebSite"

FROM build AS publish
RUN dotnet publish "Fabrikam.DroneDelivery.WebSite.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fabrikam.DroneDelivery.WebSite.dll"]