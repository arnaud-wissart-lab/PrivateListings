# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0.301 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["PrivateListings.sln", "./"]
COPY ["src/PrivateListings.Application/PrivateListings.Application.csproj", "src/PrivateListings.Application/"]
COPY ["src/PrivateListings.Domain/PrivateListings.Domain.csproj", "src/PrivateListings.Domain/"]
COPY ["src/PrivateListings.Infrastructure/PrivateListings.Infrastructure.csproj", "src/PrivateListings.Infrastructure/"]
COPY ["src/PrivateListings.ServiceDefaults/PrivateListings.ServiceDefaults.csproj", "src/PrivateListings.ServiceDefaults/"]
COPY ["src/PrivateListings.Web/PrivateListings.Web.csproj", "src/PrivateListings.Web/"]
RUN dotnet restore "src/PrivateListings.Web/PrivateListings.Web.csproj"

COPY ["src/PrivateListings.Application/", "src/PrivateListings.Application/"]
COPY ["src/PrivateListings.Domain/", "src/PrivateListings.Domain/"]
COPY ["src/PrivateListings.Infrastructure/", "src/PrivateListings.Infrastructure/"]
COPY ["src/PrivateListings.ServiceDefaults/", "src/PrivateListings.ServiceDefaults/"]
COPY ["src/PrivateListings.Web/", "src/PrivateListings.Web/"]
RUN dotnet publish "src/PrivateListings.Web/PrivateListings.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "PrivateListings.Web.dll"]
