FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY Fabrikam.Workflow.Service/Fabrikam.Workflow.Service.csproj Fabrikam.Workflow.Service/
RUN dotnet restore Fabrikam.Workflow.Service/Fabrikam.Workflow.Service.csproj
COPY . .
WORKDIR /src/Fabrikam.Workflow.Service
RUN dotnet build Fabrikam.Workflow.Service.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Fabrikam.Workflow.Service.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Fabrikam.Workflow.Service.dll"]
