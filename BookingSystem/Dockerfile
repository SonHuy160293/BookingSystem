FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY BookingSystem.slnx ./
COPY src/BuildingBlocks/BookingSystem.AspNetCore/BookingSystem.AspNetCore.csproj src/BuildingBlocks/BookingSystem.AspNetCore/
COPY src/BuildingBlocks/BookingSystem.EntityFrameworkCore/BookingSystem.EntityFrameworkCore.csproj src/BuildingBlocks/BookingSystem.EntityFrameworkCore/
COPY src/BuildingBlocks/BookingSystem.SharedKernel/BookingSystem.SharedKernel.csproj src/BuildingBlocks/BookingSystem.SharedKernel/
COPY src/Modules/BookingSystem.Identity/BookingSystem.Identity.API/BookingSystem.Identity.API.csproj src/Modules/BookingSystem.Identity/BookingSystem.Identity.API/
COPY src/Modules/BookingSystem.Identity/BookingSystem.Identity.Application/BookingSystem.Identity.Application.csproj src/Modules/BookingSystem.Identity/BookingSystem.Identity.Application/
COPY src/Modules/BookingSystem.Identity/BookingSystem.Identity.Domain/BookingSystem.Identity.Domain.csproj src/Modules/BookingSystem.Identity/BookingSystem.Identity.Domain/
COPY src/Modules/BookingSystem.Identity/BookingSystem.Identity.Infrastructure/BookingSystem.Identity.Infrastructure.csproj src/Modules/BookingSystem.Identity/BookingSystem.Identity.Infrastructure/
COPY src/Modules/BookingSystem.Identity/BookingSystem.Identity.MigrationRunner/BookingSystem.Identity.MigrationRunner.csproj src/Modules/BookingSystem.Identity/BookingSystem.Identity.MigrationRunner/

RUN dotnet restore src/Modules/BookingSystem.Identity/BookingSystem.Identity.API/BookingSystem.Identity.API.csproj
RUN dotnet restore src/Modules/BookingSystem.Identity/BookingSystem.Identity.MigrationRunner/BookingSystem.Identity.MigrationRunner.csproj
COPY . .
RUN dotnet publish src/Modules/BookingSystem.Identity/BookingSystem.Identity.API/BookingSystem.Identity.API.csproj -c Release -o /app/api --no-restore
RUN dotnet publish src/Modules/BookingSystem.Identity/BookingSystem.Identity.MigrationRunner/BookingSystem.Identity.MigrationRunner.csproj -c Release -o /app/migration --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final-api
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/api .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "BookingSystem.Identity.API.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final-migration
WORKDIR /app
COPY --from=build /app/migration .
ENTRYPOINT ["dotnet", "BookingSystem.Identity.MigrationRunner.dll"]
