FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["GSBC.Accounting.YARP/GSBC.Accounting.YARP.csproj", "GSBC.Accounting.YARP/"]
COPY ["GSBC.Accounting.ServiceDefaults/GSBC.Accounting.ServiceDefaults.csproj", "GSBC.Accounting.ServiceDefaults/"]
RUN dotnet restore "GSBC.Accounting.YARP/GSBC.Accounting.YARP.csproj"

COPY . .
WORKDIR "/src/GSBC.Accounting.YARP"
RUN dotnet publish "./GSBC.Accounting.YARP.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GSBC.Accounting.YARP.dll"]
