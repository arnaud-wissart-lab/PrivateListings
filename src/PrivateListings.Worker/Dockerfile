# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS base
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true

FROM mcr.microsoft.com/dotnet/sdk:10.0.301 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["PrivateListings.sln", "./"]
COPY ["src/PrivateListings.Application/PrivateListings.Application.csproj", "src/PrivateListings.Application/"]
COPY ["src/PrivateListings.Domain/PrivateListings.Domain.csproj", "src/PrivateListings.Domain/"]
COPY ["src/PrivateListings.Infrastructure/PrivateListings.Infrastructure.csproj", "src/PrivateListings.Infrastructure/"]
COPY ["src/PrivateListings.ServiceDefaults/PrivateListings.ServiceDefaults.csproj", "src/PrivateListings.ServiceDefaults/"]
COPY ["src/PrivateListings.Worker/PrivateListings.Worker.csproj", "src/PrivateListings.Worker/"]
RUN dotnet restore "src/PrivateListings.Worker/PrivateListings.Worker.csproj"

COPY ["src/PrivateListings.Application/", "src/PrivateListings.Application/"]
COPY ["src/PrivateListings.Domain/", "src/PrivateListings.Domain/"]
COPY ["src/PrivateListings.Infrastructure/", "src/PrivateListings.Infrastructure/"]
COPY ["src/PrivateListings.ServiceDefaults/", "src/PrivateListings.ServiceDefaults/"]
COPY ["src/PrivateListings.Worker/", "src/PrivateListings.Worker/"]
RUN dotnet publish "src/PrivateListings.Worker/PrivateListings.Worker.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "PrivateListings.Worker.dll"]
