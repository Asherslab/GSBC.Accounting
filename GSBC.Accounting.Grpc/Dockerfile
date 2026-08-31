FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# QuestPDF renders through SkiaSharp, and SkiaSharp's native library links against fontconfig and
# freetype. The aspnet image does not carry them, and the failure is not a missing-package error at
# startup: the service comes up healthy and the FIRST request to
# `GET /api/submissions/{id}/pdf` throws a DllNotFoundException from deep inside Skia. So this is
# installed here rather than discovered by an accountant asking why the download 500s.
#
# GSBC.ImpactKids' gRPC image has no equivalent line because it renders no documents. Do not "tidy"
# this away by matching that Dockerfile.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 libfreetype6 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Referenced projects are copied too. `dotnet restore` walks ProjectReference, so restoring with only
# the leaf csproj present fails on the missing file and the layer-caching trick buys nothing.
COPY ["GSBC.Accounting.Grpc/GSBC.Accounting.Grpc.csproj", "GSBC.Accounting.Grpc/"]
COPY ["GSBC.Accounting.ServiceDefaults/GSBC.Accounting.ServiceDefaults.csproj", "GSBC.Accounting.ServiceDefaults/"]
COPY ["GSBC.Accounting.Shared.Contracts/GSBC.Accounting.Shared.Contracts.csproj", "GSBC.Accounting.Shared.Contracts/"]
RUN dotnet restore "GSBC.Accounting.Grpc/GSBC.Accounting.Grpc.csproj"

COPY . .
WORKDIR "/src/GSBC.Accounting.Grpc"
RUN dotnet publish "./GSBC.Accounting.Grpc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GSBC.Accounting.Grpc.dll"]
