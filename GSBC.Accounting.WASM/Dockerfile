# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Referenced projects are copied too, so this restore layer is complete. `dotnet restore` does not
# fail without them - it prints "Skipping project ... because it was not found" and restores only the
# leaf, which means the cached layer is partial and the publish below silently restores the rest again.
# GSBC.ImpactKids' Dockerfiles do exactly that; this is the same practice with the gap closed.
COPY ["GSBC.Accounting.WASM/GSBC.Accounting.WASM.csproj", "GSBC.Accounting.WASM/"]
COPY ["GSBC.Accounting.Shared.Contracts/GSBC.Accounting.Shared.Contracts.csproj", "GSBC.Accounting.Shared.Contracts/"]
RUN dotnet restore "GSBC.Accounting.WASM/GSBC.Accounting.WASM.csproj"

COPY . .

# NO /p:InvariantGlobalization here, unlike GSBC.ImpactKids' otherwise-identical Dockerfile. Program.cs
# pins en-AU and `new CultureInfo("en-AU")` throws in invariant mode - copying that flag over loaded the
# runtime and then failed with Argument_CultureNotSupportedInInvariantMode, rendering nothing. See the
# csproj, which now states InvariantGlobalization=false so a hand-run publish cannot reintroduce it.
RUN dotnet publish ./GSBC.Accounting.WASM/GSBC.Accounting.WASM.csproj \
    -c Release -o /app/publish \
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
