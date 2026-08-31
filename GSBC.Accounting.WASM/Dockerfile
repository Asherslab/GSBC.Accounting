# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GSBC.Accounting.WASM/GSBC.Accounting.WASM.csproj", "GSBC.Accounting.WASM/"]
COPY ["GSBC.Accounting.Shared.Contracts/GSBC.Accounting.Shared.Contracts.csproj", "GSBC.Accounting.Shared.Contracts/"]
RUN dotnet restore "GSBC.Accounting.WASM/GSBC.Accounting.WASM.csproj"

COPY . .

# InvariantGlobalization trims the ICU data. Safe here: every date and money value this app prints is
# formatted with an explicit culture or an invariant format, and the two paper forms are Australian
# English only.
RUN dotnet publish ./GSBC.Accounting.WASM/GSBC.Accounting.WASM.csproj \
    -c Release -o /app/publish \
    /p:InvariantGlobalization=true \
    /p:DebuggerSupport=false \
    /p:StartupTracing=false \
    /p:LinkerDumpDependencies=false

# ---------- Runtime stage ----------
FROM nginx:latest AS runtime
COPY ./GSBC.Accounting.WASM/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Fail the build rather than shipping an image that serves 404 at the root.
RUN test -f /usr/share/nginx/html/index.html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
