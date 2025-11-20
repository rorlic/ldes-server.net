FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get upgrade -y
RUN apt-get install curl -y
WORKDIR /ldes-server

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ./src .
RUN dotnet restore ldes-server/LdesServer.csproj
RUN dotnet build ldes-server/LdesServer.csproj -c $BUILD_CONFIGURATION

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish ldes-server/LdesServer.csproj -c $BUILD_CONFIGURATION -o /ldes-server/publish /p:UseAppHost=false

FROM base AS final
ARG ASPNETCORE_HTTP_PORTS
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE $ASPNETCORE_HTTP_PORTS

WORKDIR /ldes-server
COPY --from=publish --exclude=*.pdb --exclude=*.deps.json --exclude=*.Development.json /ldes-server/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "LdesServer.dll"]
