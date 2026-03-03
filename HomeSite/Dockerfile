FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

EXPOSE 8080

USER root
RUN mkdir -p /app/Config /app/Share /app/servers && \
    chown -R app:app /app/Config /app/Share /app/servers

USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HomeSite/HomeSite.csproj", "HomeSite/"]
RUN dotnet restore "./HomeSite/HomeSite.csproj"
COPY . .
WORKDIR "/src/HomeSite"
RUN dotnet build "./HomeSite.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "./HomeSite.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "HomeSite.dll"]