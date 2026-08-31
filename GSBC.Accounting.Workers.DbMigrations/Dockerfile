FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# The migrations worker references the gRPC project - that is where AccountingDbContext lives - so this
# image pulls in the whole graph. Copying every referenced csproj makes the restore layer complete;
# omitting them does not fail (restore prints "Skipping project ... because it was not found") but
# leaves the cache partial and re-restores during publish.
COPY ["GSBC.Accounting.Workers.DbMigrations/GSBC.Accounting.Workers.DbMigrations.csproj", "GSBC.Accounting.Workers.DbMigrations/"]
COPY ["GSBC.Accounting.Grpc/GSBC.Accounting.Grpc.csproj", "GSBC.Accounting.Grpc/"]
COPY ["GSBC.Accounting.ServiceDefaults/GSBC.Accounting.ServiceDefaults.csproj", "GSBC.Accounting.ServiceDefaults/"]
COPY ["GSBC.Accounting.Shared.Contracts/GSBC.Accounting.Shared.Contracts.csproj", "GSBC.Accounting.Shared.Contracts/"]
RUN dotnet restore "GSBC.Accounting.Workers.DbMigrations/GSBC.Accounting.Workers.DbMigrations.csproj"

COPY . .
WORKDIR "/src/GSBC.Accounting.Workers.DbMigrations"
RUN dotnet publish "./GSBC.Accounting.Workers.DbMigrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GSBC.Accounting.Workers.DbMigrations.dll"]
