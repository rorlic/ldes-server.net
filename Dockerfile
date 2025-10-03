FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get upgrade -y
RUN apt-get install curl -y
WORKDIR /ldes-server.net
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /sources
COPY ./sources .
RUN dotnet restore ldes-server.sln
RUN dotnet build ldes-server.sln -c $BUILD_CONFIGURATION -o /ldes-server.net/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish ldes-server.sln -c $BUILD_CONFIGURATION -o /ldes-server.net/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /ldes-server.net
COPY --from=publish /ldes-server.net/publish .

RUN rm -rf *.deps.json
RUN rm -rf xunit.*
RUN rm -rf *Test*
RUN rm -rf *Coverage*
RUN rm -rf *coverlet*

USER $APP_UID
ENTRYPOINT ["dotnet", "AquilaSolutions.LdesServer.dll"]
