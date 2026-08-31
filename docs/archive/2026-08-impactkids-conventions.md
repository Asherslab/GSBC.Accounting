---
title: The ImpactKids recipe for one vertical slice
kind: plan
status: folded
opened: 2026-08-31
closed: 2026-08-31
verified: 2026-08-31
code:
  - GSBC.ImpactKids.AppHost/AppHost.cs
  - GSBC.ImpactKids.AppHost/GSBC.ImpactKids.AppHost.csproj
  - GSBC.ImpactKids.ServiceDefaults/Extensions.cs
  - GSBC.ImpactKids.ServiceDefaults/GSBC.ImpactKids.ServiceDefaults.csproj
  - GSBC.ImpactKids.Shared.Contracts/GSBC.ImpactKids.Shared.Contracts.csproj
  - GSBC.ImpactKids.Shared.Contracts/AGENTS.md
  - GSBC.ImpactKids.Shared.Contracts/GlobalUsings.cs
  - GSBC.ImpactKids.Shared.Contracts/Entities/Interfaces/IIdentifiable.cs
  - GSBC.ImpactKids.Shared.Contracts/Entities/DeltaUpdate.cs
  - GSBC.ImpactKids.Shared.Contracts/Entities/Pagination/PaginationRequest.cs
  - GSBC.ImpactKids.Shared.Contracts/Entities/Features/People/Person.cs
  - GSBC.ImpactKids.Shared.Contracts/Entities/Features/People/Allergies/Allergy.cs
  - GSBC.ImpactKids.Shared.Contracts/Messages/Requests/Base/BasicReadMultipleRequest.cs
  - GSBC.ImpactKids.Shared.Contracts/Messages/Requests/Base/Interfaces/IReadRequest.cs
  - GSBC.ImpactKids.Shared.Contracts/Messages/Requests/Features/People/Allergies/CreateAllergyRequest.cs
  - GSBC.ImpactKids.Shared.Contracts/Messages/Requests/Features/People/Allergies/UpdateAllergyRequest.cs
  - GSBC.ImpactKids.Shared.Contracts/Messages/Responses/Base/BasicResponse.cs
  - GSBC.ImpactKids.Shared.Contracts/Messages/Responses/Base/BasicReadResponse.cs
  - GSBC.ImpactKids.Shared.Contracts/Messages/Responses/Base/BasicReadMultipleResponse.cs
  - GSBC.ImpactKids.Shared.Contracts/Services/Base/ICreateService.cs
  - GSBC.ImpactKids.Shared.Contracts/Services/Base/IBasicReadMultipleService.cs
  - GSBC.ImpactKids.Shared.Contracts/Services/Features/People/IAllergyService.cs
  - GSBC.ImpactKids.Grpc/GSBC.ImpactKids.Grpc.csproj
  - GSBC.ImpactKids.Grpc/AGENTS.md
  - GSBC.ImpactKids.Grpc/Program.cs
  - GSBC.ImpactKids.Grpc/createMigrations.sh
  - GSBC.ImpactKids.Grpc/Conversion/IConverter.cs
  - GSBC.ImpactKids.Grpc/Conversion/Converters.cs
  - GSBC.ImpactKids.Grpc/Extensions/ServiceExtensions.cs
  - GSBC.ImpactKids.Grpc/Extensions/QueryableExtensions.cs
  - GSBC.ImpactKids.Grpc/Extensions/DbExtensions.cs
  - GSBC.ImpactKids.Grpc/Data/GsbcDbContext.cs
  - GSBC.ImpactKids.Grpc/Data/GsbcDbContext.PeopleModel.cs
  - GSBC.ImpactKids.Grpc/Data/GsbcDbContext.SyncModel.cs
  - GSBC.ImpactKids.Grpc/Data/GsbcDbContextFactory.cs
  - GSBC.ImpactKids.Grpc/Data/Models/People/DbPerson.cs
  - GSBC.ImpactKids.Grpc/Data/Models/People/DbAllergy.cs
  - GSBC.ImpactKids.Grpc/Data/Models/Attendance/DbAttendanceRecord.cs
  - GSBC.ImpactKids.Grpc/Features/People/AllergyServices/AllergyService.cs
  - GSBC.ImpactKids.Grpc/Features/People/AllergyServices/Create.cs
  - GSBC.ImpactKids.Grpc/Features/People/AllergyServices/Update.cs
  - GSBC.ImpactKids.Grpc/Features/People/AllergyServices/ReadMultiple.cs
  - GSBC.ImpactKids.Grpc/Features/People/AllergyServices/Delete.cs
  - GSBC.ImpactKids.Grpc/Features/Attendance/AttendanceRecordServices/Delete.cs
  - GSBC.ImpactKids.Grpc/Features/People/Photos/PersonPhotoEndpoints.cs
  - GSBC.ImpactKids.Grpc/Features/People/Photos/PhotoStore.cs
  - GSBC.ImpactKids.Grpc/Features/People/Photos/PhotoStoreConfig.cs
  - GSBC.ImpactKids.WASM/GSBC.ImpactKids.WASM.csproj
  - GSBC.ImpactKids.WASM/AGENTS.md
  - GSBC.ImpactKids.WASM/Program.cs
  - GSBC.ImpactKids.WASM/Extensions/GrpcServiceExtensions.cs
  - GSBC.ImpactKids.WASM/Extensions/StateStoreExtensions.cs
  - GSBC.ImpactKids.WASM/Services/RefreshableStore/RefreshableStore.cs
  - GSBC.ImpactKids.WASM/Services/RefreshableStore/EntityListState.cs
  - GSBC.ImpactKids.WASM/Services/RefreshableStore/IRefreshableStore.cs
  - GSBC.ImpactKids.WASM/Components/Base/StoreEntityUtilityComponent.razor.cs
  - GSBC.ImpactKids.WASM/Features/Sync/Pages/Multiple.razor.cs
  - GSBC.ImpactKids.YARP/GSBC.ImpactKids.YARP.csproj
  - GSBC.ImpactKids.YARP/Program.cs
  - GSBC.ImpactKids.YARP/appsettings.json
  - GSBC.ImpactKids.YARP/Extensions/HostExtensions.cs
  - GSBC.ImpactKids.Workers.DbMigrations/GSBC.ImpactKids.Workers.DbMigrations.csproj
  - GSBC.ImpactKids.Workers.DbMigrations/Program.cs
  - GSBC.ImpactKids.Workers.DbMigrations/Worker.cs
  - docs/frontend-store-architecture.md
  - docs/modules/people/photos.md
  - docs/modules/infrastructure/object-store.md
---

> **Archived 2026-08-31.** The recipe was followed; what it describes now exists in this repo, so read the code and the modules/ docs instead. Kept for the "do not copy this" list, which is the reasoning behind several deliberate departures from GSBC.ImpactKids.

# The ImpactKids recipe for one vertical slice

**Every path in the `code:` list above is in `GSBC.ImpactKids`, not in this repo.** They resolve
against the absolute path `/Users/asherp/Documents/Git/GSBC.ImpactKids/`, which is a sibling
checkout and is deliberately *not* referenced from this solution. Nothing here is a path you can
open locally; the excerpts are quoted so you do not have to.

This is the recipe a developer follows to build one vertical slice — contract → DB model →
converter → service interface → service implementation → DI registration → frontend — in the
architecture GSBC.Accounting inherits. It was extracted by reading the code on 2026-08-31, not from
memory, and it records what ImpactKids *does*, including the places where that differs from what
this repo's [scope doc](2026-08-expense-forms-scope.md) assumes.

Read [`docs/work/2026-08-expense-forms-scope.md`](2026-08-expense-forms-scope.md) first for what is
being built. This doc is only the shape to build it in.

---

## 1. Project layout

Seven projects, plus a test project and a one-off worker that this app has no equivalent of. The
scope doc's slice 0 says "six projects", and that is the six below minus `Workers.PhotoBackfill`
and minus `Grpc.Tests` — ImpactKids has nine `.csproj` files in total.

| Project | SDK | What it is |
|---|---|---|
| `*.AppHost` | `Aspire.AppHost.Sdk/13.0.2` | The Aspire orchestrator. Declares Postgres, Redis, RabbitMQ, the SeaweedFS container, and the five project resources with their `WaitFor` ordering. Also the Kubernetes publishing target. |
| `*.ServiceDefaults` | `Microsoft.NET.Sdk` | Shared OpenTelemetry, health checks, service discovery and HTTP resilience. Referenced by every host project. |
| `*.Shared.Contracts` | `Microsoft.NET.Sdk` | protobuf-net contracts: entity records, request/response messages, and the code-first service *interfaces*. Referenced by both the server and the WASM client — this is the only project both ends share. |
| `*.Grpc` | `Microsoft.NET.Sdk.Web` | The server. Service implementations, EF Core `DbContext` + `Db*` models + migrations, Mapperly converters, and the minimal-API endpoints. |
| `*.WASM` | `Microsoft.NET.Sdk.BlazorWebAssembly` | The Blazor client. MudBlazor, the store layer, the gRPC-web client registrations. |
| `*.YARP` | `Microsoft.NET.Sdk.Web` | The BFF. The only externally-exposed endpoint; forwards gRPC-web, `/api/` and the WASM app. |
| `*.Workers.DbMigrations` | `Microsoft.NET.Sdk.Worker` | Applies migrations at startup and seeds reference data, then stops. The AppHost `WaitForCompletion`s it before the gRPC service starts. |

**Every project is `net10.0`, `ImplicitUsings=enable`, `Nullable=enable`.** No `Directory.Build.props`
and no central package management exist — every version is written out in each `.csproj`, which is
why the lists below are exact.

### AppHost

`GSBC.ImpactKids.AppHost/GSBC.ImpactKids.AppHost.csproj:1-25` — note the SDK version is pinned in
the `Sdk` attribute itself, and that `OutputType` is `Exe`:

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.0.2">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <UserSecretsId>88705c3c-8603-4e41-afd3-b947d2a3ac4e</UserSecretsId>
    </PropertyGroup>
```

| PackageReference | Version |
|---|---|
| `Aspire.Hosting.Kubernetes` | `13.0.2-preview.1.25603.5` |
| `Aspire.Hosting.PostgreSQL` | `13.0.2` |
| `Aspire.Hosting.RabbitMQ` | `13.0.2` |
| `Aspire.Hosting.Redis` | `13.0.2` |
| `Aspire4Wasm.AppHost` | `6.0.0` |

ProjectReferences: `Grpc`, `WASM`, `Workers.DbMigrations`, `Workers.PhotoBackfill`, `YARP`. The
`UserSecretsId` is load-bearing — see §4 and the AppHost comment at `AppHost.cs:7-19`.

### ServiceDefaults

`GSBC.ImpactKids.ServiceDefaults/GSBC.ImpactKids.ServiceDefaults.csproj:1-20`. The
`IsAspireSharedProject` property and the `FrameworkReference` are both required:

```xml
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsAspireSharedProject>true</IsAspireSharedProject>
    </PropertyGroup>

    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App" />
```

| PackageReference | Version |
|---|---|
| `Microsoft.Extensions.Http.Resilience` | `10.1.0` |
| `Microsoft.Extensions.ServiceDiscovery` | `10.1.0` |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | `1.14.0` |
| `OpenTelemetry.Extensions.Hosting` | `1.14.0` |
| `OpenTelemetry.Instrumentation.AspNetCore` | `1.14.0` |
| `OpenTelemetry.Instrumentation.Http` | `1.14.0` |
| `OpenTelemetry.Instrumentation.Runtime` | `1.14.0` |

No ProjectReferences — it is the leaf.

### Shared.Contracts

`GSBC.ImpactKids.Shared.Contracts/GSBC.ImpactKids.Shared.Contracts.csproj:9-12`. Two packages, no
project references at all:

| PackageReference | Version |
|---|---|
| `protobuf-net` | `3.2.56` |
| `protobuf-net.Grpc` | `1.2.2` |

### Grpc

`GSBC.ImpactKids.Grpc/GSBC.ImpactKids.Grpc.csproj:1-33`. `ContainerUser=root` is set for the
published container image.

| PackageReference | Version | Notes |
|---|---|---|
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | `13.1.0` | |
| `Aspire.RabbitMQ.Client.v7` | `9.5.2` | eventing only — GSBC.Accounting may not need it |
| `Aspire.StackExchange.Redis.DistributedCaching` | `13.1.0` | |
| `Aspire4Wasm.WebApi` | `6.0.1` | |
| `AWSSDK.S3` | `4.0.102.4` | the object-store client |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.1` | not needed for an anonymous app |
| `Grpc.AspNetCore` | `2.76.0` | |
| `Grpc.AspNetCore.HealthChecks` | `2.76.0` | |
| `Grpc.AspNetCore.Web` | `2.76.0` | gRPC-web, required behind YARP |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.1` | `PrivateAssets=all`; needed for `dotnet ef` |
| `Microsoft.Extensions.Caching.Hybrid` | `10.1.0` | |
| `protobuf-net.Grpc.AspNetCore` | `1.2.2` | |
| `protobuf-net.Grpc.ClientFactory` | `1.2.2` | |
| `Riok.Mapperly` | `4.3.1` | the converters |

ProjectReferences: `ServiceDefaults`, `Shared.Contracts`.

### WASM

`GSBC.ImpactKids.WASM/GSBC.ImpactKids.WASM.csproj:1-46`.

| PackageReference | Version | Notes |
|---|---|---|
| `Aspire4Wasm.WebAssembly` | `6.0.1` | gives the client `AddServiceDefaults()` and `https://yarp` discovery |
| `Blazor.WhyDidYouRender` | `3.0.0` | currently commented out in `Program.cs` |
| `Blazor.WhyDidYouRender.Aspire` | `3.0.0` | |
| `EasyAppDev.Blazor.Store` | `2.0.10` | the store library |
| `FuzzySharp` | `2.0.2` | |
| `Microsoft.AspNetCore.Components.WebAssembly` | `10.0.0` | |
| `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | `10.0.0` | |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | `10.0.0` | `PrivateAssets=all` |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | `3.3.4` | reads `BannedSymbols.txt` as an `AdditionalFiles` item |
| `Microsoft.Extensions.Http` | `10.0.0` | |
| `Microsoft.Extensions.Logging.Configuration` | `10.0.1` | |
| `MudBlazor` | `9.2.0` | |
| `Grpc.Net.Client` | `2.71.0` | |
| `Grpc.Net.Client.Web` | `2.71.0` | |
| `Grpc.Net.ClientFactory` | `2.71.0` | |
| `protobuf-net.Grpc.ClientFactory` | `1.2.2` | |

Note the version skew: the client's `Grpc.Net.*` are `2.71.0` while the server's `Grpc.AspNetCore.*`
are `2.76.0`. It works; do not assume they must match.

Properties: `<ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>`
plus a `ServiceWorker` item pairing `wwwroot/service-worker.js` with
`wwwroot/service-worker.published.js`. ProjectReference: `Shared.Contracts` only — **the client
never references the server project.**

### YARP

`GSBC.ImpactKids.YARP/GSBC.ImpactKids.YARP.csproj:20-26`.

| PackageReference | Version |
|---|---|
| `Aspire.StackExchange.Redis.DistributedCaching` | `13.1.0` |
| `Duende.AccessTokenManagement.OpenIdConnect` | `4.1.1` |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | `10.0.2` |
| `Microsoft.Extensions.ServiceDiscovery.Yarp` | `10.2.0` |
| `Yarp.ReverseProxy` | `2.3.0` |

Only `Yarp.ReverseProxy` and `Microsoft.Extensions.ServiceDiscovery.Yarp` are needed for an
anonymous app; the Duende and OIDC packages exist purely for the leader sign-in this app does not
have. ProjectReference: `ServiceDefaults`.

### Workers.DbMigrations

`GSBC.ImpactKids.Workers.DbMigrations/GSBC.ImpactKids.Workers.DbMigrations.csproj:1-14`.
`ErrorOnDuplicatePublishOutputFiles=false` is set because it references the web project.

| PackageReference | Version |
|---|---|
| `CsvHelper` | `33.1.0` (seed data only) |
| `Microsoft.Extensions.Hosting` | `10.0.1` |

ProjectReferences: **`Grpc`** (that is how it gets the `DbContext`) and `ServiceDefaults`.

---

## 2. Shared.Contracts

### There is no `[DataContract]` anywhere

This is the first thing to get right, and it is not what the brief assumed. ImpactKids uses
protobuf-net's own attributes with **implicit field numbering**, so there are no `Order`s to assign
and no field numbers to keep stable. From
`GSBC.ImpactKids.Shared.Contracts/Entities/Features/People/Allergies/Allergy.cs:3-15`:

```csharp
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record Allergy : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid PersonId { get; init; }

    public required Guid? AllergenId { get; init; }

    public string? Notes  { get; init; }
    public bool    Severe { get; init; }
}
```

The rule, stated at `GSBC.ImpactKids.Shared.Contracts/AGENTS.md:6-7`:

> All serialised types carry `[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]`. Always
> include `ImplicitFields` — without it the type serialises as nothing and the failure is silent.

Consequences you must design around:

- **Every public property is on the wire.** A computed property is serialised too, and gets a field
  number, unless it carries `[ProtoIgnore]`.
- Because numbering is positional over the public surface, **reordering properties is a wire
  change.** Both ends are rebuilt together (`AGENTS.md:3-4`), so this is survivable, but do not
  assume you can insert a property in the middle and deploy one side.
- One type opts into a different mode: `Entities/DeltaUpdate.cs:5` uses
  `ImplicitFields.AllFields` with the comment `// changed to all fields to include _updateValue;`,
  because its state lives in a private backing field.
- `BasicResponse` is a `class`, not a `record`, and carries a comment about `[ProtoInclude]` tags
  (`Messages/Responses/Base/BasicResponse.cs:5-11`) — protobuf-net only carries a base type's
  members into a derived contract when the base declares the subtype. Tag 100 is documented as free
  and reusable there. Prefer not to derive from response bases at all.

### Folder tree

From `Shared.Contracts/AGENTS.md:30-39`:

```
Entities/Features/<Feature>/          grouped by domain feature: People, Games, Attendance
Messages/Requests/Features/<Feature>/
Messages/Responses/Features/<Feature>/
Services/Features/<Feature>/
```

Entity records live at `Entities/Features/<Feature>/`, nesting further for sub-features —
`Entities/Features/People/Allergies/Allergy.cs`,
`Entities/Features/Scheduling/School/SchoolTerm.cs`. Cross-cutting entities sit at
`Entities/` directly (`Entities/DeltaUpdate.cs`, `Entities/User.cs`), pagination at
`Entities/Pagination/`, and the marker interface at `Entities/Interfaces/IIdentifiable.cs:3-6`:

```csharp
public interface IIdentifiable
{
    public Guid Id { get; }
}
```

For this app the scope doc already fixes the location:
`Shared.Contracts/Entities/Features/Expenses/`.

`GlobalUsings.cs:3-11` is what makes the bare `[ProtoContract]` and `CallContext` work everywhere
without a `using`:

```csharp
global using ProtoBuf;
global using ProtoBuf.Grpc;
global using ProtoBuf.Grpc.Configuration;

global using GSBC.ImpactKids.Shared.Contracts.Messages.Responses.Base;
global using GSBC.ImpactKids.Shared.Contracts.Messages.Requests.Base;
global using GSBC.ImpactKids.Shared.Contracts.Entities.Pagination;
global using GSBC.ImpactKids.Shared.Contracts.Messages.Requests.Base.Interfaces;
global using GSBC.ImpactKids.Shared.Contracts.Entities.Interfaces;
```

Copy this file first. Add `ErrorConstants` to it — `Grpc/AGENTS.md:151-152` records that the error
strings are globally imported so services write `WithError(PersonNotFound)` bare.

### Enums

Declared in the same file as the entity that uses them, with a bare `[ProtoContract]` and **no
`ImplicitFields`** — `Entities/Features/People/Person.cs:110-124`:

```csharp
[ProtoContract]
public enum Gender
{
    Male,
    Female
}

[ProtoContract]
public enum MediaConsent
{
    NotRequested,
    Yes,
    No,
    StrictlyNo
}
```

Two conventions worth carrying:

- **Nullable enum rather than an `Unknown` member.** `Person.Gender` is `Gender?` and the XML doc at
  `Person.cs:19-27` explains why: an `Unknown` member reads as a value, and a value is something code
  eventually writes somewhere as though a human had chosen it. This is exactly the argument the scope
  doc makes for the six compliance answers being `bool?`.
- A `*Helper` static class beside the enum holds the display/parse mapping
  (`Person.cs:126-151`, `MediaConsentHelper.ToDisplay`), so the two ends cannot disagree.
  `Shared.Contracts/AGENTS.md:69-76`: a rule both ends must agree on goes in this project.

### The DateTime-UTC-over-the-wire rule, and the code that enforces it

The rule is stated at `Shared.Contracts/AGENTS.md:51-67`:

> Contracts use `DateTime`, never `DateTimeOffset` — protobuf-net has no surrogate for it here, and
> there are currently zero `DateTimeOffset` properties in this project. The database models use
> `DateTimeOffset` (`timestamptz`), so the conversion happens in the converters, and the entity
> exposes a `Local*` projection for the UI.

Three pieces of real code hold it up.

**1. The contract carries UTC `DateTime` plus a `[ProtoIgnore]` local projection.**
`Entities/Features/People/Person.cs:16-17,42-46`:

```csharp
    public required DateTime?    DateOfBirth   { get; init; }
    public required DateTime?    FirstTime     { get; init; }

    [ProtoIgnore]
    public DateTime? LocalDateOfBirth => DateOfBirth?.ToLocalTime();

    [ProtoIgnore]
    public DateTime? LocalFirstTime => FirstTime?.ToLocalTime();
```

`[ProtoIgnore]` is not cosmetic. `ImplicitFields.AllPublic` would otherwise serialise the computed
property and assign it a field number.

**2. The DB model is `DateTimeOffset`.** `Grpc/Data/Models/People/DbPerson.cs:48-49`:

```csharp
    public required DateTimeOffset? DateOfBirth { get; set; }
    public required DateTimeOffset? FirstTime   { get; set; }
```

**3. A dedicated converter bridges them, injected into every converter that maps a date.**
`Grpc/Conversion/Converters.cs:29-35`:

```csharp
public class DateTimeConverter : IConverter<DateTimeOffset, DateTime>
{
    public DateTime Convert(DateTimeOffset input)
    {
        return input.UtcDateTime;
    }
}
```

and `Converters.cs:44-55` shows how a Mapperly converter picks it up — as a `[UseMapper]` field,
with the `Local*` projections explicitly ignored as mapping *targets*:

```csharp
[Mapper]
public partial class PersonConverter(
    IConverter<DateTimeOffset, DateTime> dateTimeConverter
) : IConverter<DbPerson, Person>
{
    [UseMapper]
    private readonly IConverter<DateTimeOffset, DateTime> _dateTimeConverter = dateTimeConverter;

    [MapperIgnoreTarget(nameof(Person.LocalDateOfBirth))]
    [MapperIgnoreTarget(nameof(Person.LocalFirstTime))]
    public partial Person Convert(DbPerson person);
}
```

**The trap the scope doc mentions**, verbatim from `Grpc/AGENTS.md:220-243`: a `DateTimeOffset` you
compare against *in a query* must have offset zero. Npgsql refuses anything else against a
`timestamptz`:

```
Cannot write DateTimeOffset with Offset=10:00:00 to PostgreSQL type
'timestamp with time zone', only offset 0 (UTC) is supported.
```

It builds fine and throws at execution. The two idioms, both from that section:

```csharp
// value is already UTC:
new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))

// you deliberately started from a local wall-clock day:
DateTime       localToday = DateTime.Today;
DateTimeOffset dayStart   = new DateTimeOffset(localToday, TimeZoneInfo.Local.GetUtcOffset(localToday))
    .ToUniversalTime();          // same instant, offset 0 - without this it throws
```

Keep new columns `DateTimeOffset`; `Grpc/AGENTS.md:217-218` notes it maps to `timestamptz`
regardless of Npgsql's legacy-timestamp switch, which ImpactKids deliberately never sets.

### Requests and responses

Read the bases before writing a message. `Shared.Contracts/AGENTS.md:18-26`: **do not create a
custom message with no additional properties** — use the base directly.

- `BasicReadRequest` / `ReadRequestBase` — the id is a `string` on the wire with a `Guid` façade.
  `Messages/Requests/Base/Interfaces/IReadRequest.cs:8-17`:

  ```csharp
  public abstract class ReadRequestBase : IReadRequest
  {
      public abstract string Id { get; set; }

      public Guid Guid
      {
          get => Guid.Parse(Id);
          set => Id = value.ToString();
      }
  }
  ```

- `BasicReadMultipleRequest` — `Pagination` + `SearchString`, with a static `All()`
  (`Messages/Requests/Base/BasicReadMultipleRequest.cs:9-15`).
- `BasicResponse` — `{ bool Success; string? Error; }` with `static WithError(string)`.
- `BasicReadResponse<T>` — adds `T? Entity`. **`Create` returns `BasicReadResponse<Guid?>`.**
- `BasicReadMultipleResponse<T>` — `ImmutableList<T> Entities` + `PaginationResponse Pagination`.
- Update requests wrap each field in `DeltaUpdate<T>` so "not supplied" and "set to null" are
  different (`Entities/DeltaUpdate.cs:6-23`), and implement
  `IUpdateRequest<TEntity, TRequest>` with a `static abstract FromEntity`
  (`Messages/Requests/Features/People/Allergies/UpdateAllergyRequest.cs:7-28`). The server reads
  `request.Notes.IsUpdated` before assigning.

**`PaginationRequest` silently truncates to 10.** `PerPage` defaults to 10 and
`QueryableExtensions.Paginate` applies that default when `Pagination` is null
(`Grpc/Extensions/QueryableExtensions.cs:10`), so a `BasicReadMultipleRequest` built without
pagination returns the first ten rows with no indication there are more. Use
`BasicReadMultipleRequest.All()`.

---

## 3. The service contract, end to end

### Declaring the interface (code-first)

`Shared.Contracts/Services/Base/ICreateService.cs:3-9` — the base interfaces carry `[SubService]`,
take a `CallContext context = default` as the last parameter, and use unprefixed method names:

```csharp
[SubService]
public interface ICreateService<in TCreateRequest>
{
    Task<BasicReadResponse<Guid?>> Create(
        TCreateRequest request,
        CallContext    context = default
    );
};
```

`IBasicReadMultipleService<TEntity>` is the streaming one — it returns
`IAsyncEnumerable<BasicReadMultipleResponse<TEntity>>`
(`Services/Base/IBasicReadMultipleService.cs:3-10`). The five bases are
`IBasicReadMultipleService<T>`, `ICreateService<TReq>`, `IUpdateService<TReq>`,
`IBasicDeleteService<T>`, `IBasicMultipleRelationshipService<T1,T2>`.

A feature service is `[Service("...")]` plus composition of the bases, and often has no body at all.
`Shared.Contracts/Services/Features/People/IAllergyService.cs:7-12`:

```csharp
[Service("gRPC/GSBC.ImpactKids.Person.Allergies")]
public interface IAllergyService
    : IBasicReadMultipleService<Allergy>,
        ICreateService<CreateAllergyRequest>,
        IUpdateService<UpdateAllergyRequest>,
        IBasicDeleteService<Allergy>;
```

**The `[Service]` string is the wire path, and it must match the YARP route pattern** (§6) — see
`IPersonService.cs:7`, `[Service("gRPC/GSBC.ImpactKids.Person")]`, against YARP's
`/gRPC/GSBC.ImpactKids.{service}/{**catch-all}`. For this repo that becomes
`gRPC/GSBC.Accounting.<Name>` and a matching YARP path. Extra methods beyond the bases go in the
interface body (`IPersonService.cs:14-17` adds `Read`).

### Implementing it in the Grpc project

The house idiom is a **partial class: a folder per service, a file per operation**, with a primary
constructor in the root file whose parameters are in scope in every part
(`Grpc/AGENTS.md:9-43`). `Grpc/Features/People/AllergyServices/`:

```
AllergyService.cs      <- attributes, primary constructor, nothing else
Create.cs
Update.cs
ReadMultiple.cs
Delete.cs
```

`Features/People/AllergyServices/AllergyService.cs:10-14`:

```csharp
public partial class AllergyService(
    GsbcDbContext                  db,
    IEventService<Allergy>         eventService,
    IConverter<DbAllergy, Allergy> converter
) : IAllergyService;
```

`Create.cs:10-48` — validate, return a named error constant, build the `Db*` model with
`Id = Guid.Empty` (Postgres generates the real one), save, raise the event, return the id:

```csharp
    public async Task<BasicReadResponse<Guid?>> Create(CreateAllergyRequest request, CallContext context = default)
    {
        CancellationToken token = context.CancellationToken;

        if (request.AllergenId == null && string.IsNullOrWhiteSpace(request.Notes))
            return BasicReadResponse<Guid?>.WithError(AllergiesMustHaveTypeOrNotes);
        ...
        DbAllergy allergy = new()
        {
            Id = Guid.Empty,
            PersonId = request.PersonId,
            ...
        };

        await db.Allergies.AddAsync(allergy, token);
        await db.SaveChangesAsync(token);
        await eventService.SendUpdatedEvent(token);

        return new BasicReadResponse<Guid?> { Entity = allergy.Id, Success = true };
    }
```

`ReadMultiple.cs:11-38` — the streaming read. Build `IQueryable`, apply search, order, `Paginate`,
then hand the query and the converter to `ReturnInBatches`:

```csharp
    public async IAsyncEnumerable<BasicReadMultipleResponse<Allergy>> BasicReadMultiple(
        BasicReadMultipleRequest request,
        CallContext              context = default
    )
    {
        CancellationToken token = context.CancellationToken;

        IQueryable<DbAllergy> query = db.Allergies;
        ...
        query = query.Paginate(request);

        await foreach (BasicReadMultipleResponse<Allergy> response in query.ReturnInBatches(converter, token: token))
        {
            yield return response;
        }
    }
```

`Grpc/Extensions/DbExtensions.cs:11-41` is `ReturnInBatches`: `AsNoTracking().ToListAsync()`, map
through the converter, yield in 5000-row batches, and — importantly —
**yield one empty success response when the total is zero**, because sending no responses at all
would read as an error to the client (`DbExtensions.cs:36`).

Two rules from `Grpc/AGENTS.md` that bite on write paths:

- **`db.Update(entity)` writes every column** (`AGENTS.md:157-181`), silently reverting anything
  another writer committed since your read. Mark the properties you own instead:

  ```csharp
  record.Deleted = true;

  db.Entry(record).Property(x => x.Deleted).IsModified = true;
  await db.SaveChangesAsync(token);   // UPDATE ... SET deleted = @p WHERE id = @id
  ```

  Live at `Features/Attendance/AttendanceRecordServices/Delete.cs:24`.
- **Every mutating operation ends with `await eventService.SendUpdatedEvent(token)`**
  (`AGENTS.md:252-256`). That is a RabbitMQ invalidation other clients refresh off. GSBC.Accounting
  has no second client watching a submission, so this is the one part of the recipe that is
  reasonable to drop — but drop it deliberately, and then drop RabbitMQ from the AppHost too.

### Registering it on the server

`Grpc/Program.cs:134-135`, both calls, in this order:

```csharp
builder.Services.AddCodeFirstGrpc();
builder.Services.AddGrpc();
```

`Program.cs:136` registers the converters (§4), and the mapping happens after `builder.Build()`:

```csharp
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });   // Program.cs:223

app.MapGrpcService<AllergyService>();                            // Program.cs:233
```

`Grpc/AGENTS.md:6-7`: **a service that compiles and is not mapped fails at the client as an
unimplemented method.** `DefaultEnabled = true` on `UseGrpcWeb` is what lets the browser talk to it
at all.

### Getting a proxy in the WASM client

One helper does the whole registration, including re-registering the concrete client under each
base interface it implements — which is how `IRefreshableStore<T>` can resolve
`IBasicReadMultipleService<T>` without knowing the feature service exists.
`WASM/Extensions/GrpcServiceExtensions.cs:12-46`:

```csharp
        public IServiceCollection AddAuthenticatedGrpcClient<T>() where T : class
        {
            return services.AddAuthenticatedGrpcClient<T>(new Uri("https://yarp"));
        }

        private IServiceCollection AddAuthenticatedGrpcClient<T>(Uri serviceUri) where T : class
        {
            Type serviceType = typeof(T);
            services
                .AddCodeFirstGrpcClient<T>(serviceType.FullName!, x => { x.Address = serviceUri; })
                .ConfigureChannel(x => { x.UnsafeUseInsecureChannelCallCredentials = true; })
                .ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()))
                .AddInterceptor<ExceptionInterceptor>();

            Type? readMultipleServiceBase = serviceType.IsAssignableToGenericType(typeof(IBasicReadMultipleService<>));
            if (readMultipleServiceBase != null)
                services.AddScoped(readMultipleServiceBase, sp => sp.GetRequiredService<T>());
            // ... same for ICreateService<>, IUpdateService<>, IBasicDeleteService<>,
            //     IBasicMultipleRelationshipService<,>
```

Key points:

- **`new Uri("https://yarp")`** is an Aspire service-discovery name, resolved by
  `Aspire4Wasm.WebAssembly` + `builder.AddServiceDefaults()` (`WASM/Program.cs:44`). The client
  never talks to the gRPC service directly — everything goes through the BFF.
- `GrpcWebHandler` is mandatory: the browser cannot speak HTTP/2 gRPC.
- Call sites are one line each, `WASM/Program.cs:86-114`:

  ```csharp
  builder.Services.AddAuthenticatedGrpcClient<IAllergyService>();
  ```

- `ExceptionInterceptor` is registered `AddScoped` at `WASM/Program.cs:46` and **only wraps unary
  calls** — see the comment at `RefreshableStore.cs:38-41`; a failing server-streaming
  `BasicReadMultiple` throws into the calling component.

`WASM/AGENTS.md:7-8`: a page injecting an unregistered service fails at *runtime*, not at build.

---

## 4. EF Core

### The DbContext

One `DbContext`, `GsbcDbContext`, split by subject area with a dotted suffix in the same folder
(`Grpc/AGENTS.md:56-65`) — this is "flavour three" of the partial idiom: a partial that is *a
section of one type*, not one operation of a service.

```
Data/GsbcDbContext.cs                 <- DbSets and OnModelCreating, calling into each area
Data/GsbcDbContext.PeopleModel.cs
Data/GsbcDbContext.SyncModel.cs
...
```

`Data/GsbcDbContext.cs:6-26`:

```csharp
public partial class GsbcDbContext(
    DbContextOptions options
) : DbContext(options)
{
    public required DbSet<DbUser> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DbUser>()
            .HasIndex(x => x.GoogleSub)
            .IsUnique();

        BuildPeopleModel(modelBuilder);
        BuildScheduleModel(modelBuilder);
        ...
    }
}
```

Each area file holds its own `DbSet`s (all `public required`) and a `private static void
Build<Area>Model(ModelBuilder)` — `Data/GsbcDbContext.PeopleModel.cs:10-77`:

```csharp
    public required DbSet<DbPerson>      People       { get; set; }
    public required DbSet<DbAllergy>     Allergies    { get; set; }

    private static void BuildPeopleModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbPerson>()
            .HasIndex(x => x.ElvantoId)
            .IsUnique();

        modelBuilder.Entity<DbPerson>()
            .HasMany(x => x.Allergies)
            .WithOne(x => x.Person)
            .HasForeignKey(x => x.PersonId);
```

Registration is **`AddPooledDbContextFactory`**, not `AddDbContext` (`Grpc/Program.cs:150-156`);
the scoped `GsbcDbContext` that services inject comes from the pool:

```csharp
builder.Services.AddPooledDbContextFactory<GsbcDbContext>((sp, o) =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("impact-kids"));
    o.AddInterceptors(sp.GetRequiredService<FieldChangeTrackingInterceptor>());
    o.AddInterceptors(sp.GetRequiredService<DisplayReadOnlyInterceptor>());
});
```

The connection-string name (`"impact-kids"`) is the Aspire database resource name from
`AppHost.cs:43`. For this repo use the accounting database name consistently in all three places:
`AddDatabase(...)`, `GetConnectionString(...)`, and the migrations worker's
`AddNpgsqlDbContext<T>("...")`.

There is also an `IDesignTimeDbContextFactory` at `Data/GsbcDbContextFactory.cs:7-54` so `dotnet ef`
can construct the context without the Aspire host. It has to initialise every `required` DbSet to
`null!`, which is the price of `public required DbSet<>`. See §9 for the part of it not to copy.

### The `Db`-prefixed model convention

Every persisted class is `Db<ContractName>` in `Grpc/Data/Models/<Area>/`, mirroring the contract
folder tree. Differences from the contract record, all visible in
`Data/Models/People/DbAllergy.cs:5-21`:

```csharp
public class DbAllergy
{
    public required Guid Id { get; set; }

    public required Guid? AllergenId { get; set; }

    [MapperIgnore]
    public DbAllergen? Allergen { get; set; }

    public string? Notes  { get; set; }
    public bool    Severe { get; set; }

    public required Guid PersonId { get; set; }

    [MapperIgnore]
    public DbPerson? Person { get; set; }
}
```

- `class`, not `record`; `{ get; set; }`, not `{ get; init; }`.
- No `[ProtoContract]` — a `Db*` model never crosses the wire.
- **Every navigation property carries `[MapperIgnore]`** (`Riok.Mapperly.Abstractions`). This is
  load-bearing: `Grpc/AGENTS.md:247-249` — without it the mapper walks the graph and either
  serialises half the database or fails on a cycle. New navigation properties on a `Db*` model need
  it. Collections too (`DbPerson.cs:51-55`).
- Enums are stored as strings, configured in the model builder:
  `Data/GsbcDbContext.SyncModel.cs:37-38`

  ```csharp
          modelBuilder.Entity<DbSyncOperation>()
              .Property(x => x.Status).HasConversion<string>();
  ```

  Some older columns hold a raw `string` in the model instead (`DbPerson.MediaConsent`,
  `DbPerson.Gender`) with a `HasDefaultValue` in the model builder
  (`GsbcDbContext.PeopleModel.cs:21-23`). **Prefer the `HasConversion<string>()` form** for the new
  app — it keeps the enum type on the model and still gives readable rows in `psql`.

### Converters: Mapperly, not extension methods

The converters are a **single file of small partial classes**, `Grpc/Conversion/Converters.cs`
(290 lines, ~25 converters), each implementing the project's own two-parameter interface,
`Grpc/Conversion/IConverter.cs:3-10`:

```csharp
public interface IConverter<in TIn, out TOut> : IConverter
{
    public TOut Convert(TIn input);
}

public interface IConverter
{
}
```

The simple case is three lines, `Converters.cs:75-79`:

```csharp
[Mapper]
public partial class AllergenConverter : IConverter<DbAllergen, Allergen>
{
    public partial Allergen Convert(DbAllergen person);
}
```

Mapperly generates the body at build time. The date-carrying case injects `DateTimeConverter` as a
`[UseMapper]` field and ignores the `Local*` targets — quoted in §2.

**Registration is by reflection over the marker interface**, so a new converter needs no wiring at
all. `Grpc/Extensions/ServiceExtensions.cs:10-33`:

```csharp
    public static IServiceCollection AddConverters(this IServiceCollection services)
    {
        List<Type> converters = [];
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            converters.AddRange(
                assembly.GetTypes()
                    .Where(x =>
                        x.IsAssignableTo(typeof(IConverter)) &&
                        x is { IsClass: true, IsAbstract: false }
                    )
            );
        }

        foreach (Type converter in converters)
        {
            foreach (Type interfaceType in converter.GetInterfaces())
            {
                services.AddScoped(interfaceType, converter);
            }
        }

        return services;
    }
```

Called once at `Grpc/Program.cs:136`. Services then inject
`IConverter<DbAllergy, Allergy>` (`AllergyService.cs:13`) and never name the concrete converter.

**Note the direction: every converter is `Db* → contract`, one way.** There is no
contract → `Db*` mapper anywhere. The write path constructs the `Db*` model by hand from the
request (`Create.cs:30-37`), and the update path assigns field by field from `DeltaUpdate`s
(`Update.cs:19-44`). Do the same here — a submission's server-recomputed totals must not come out of
a mapper.

### Decimal precision

**ImpactKids has no `decimal` columns at all** — a grep for `HasPrecision`, `HasColumnType` and
`decimal` across `GSBC.ImpactKids.Grpc/Data` (excluding `Migrations/`) returns nothing. So there is
no precedent to copy and **this is a convention GSBC.Accounting has to establish**, not inherit. The
scope doc fixes the requirement (`decimal(12,2)`, never `double`); implement it in the area's
`Build*Model` beside the other configuration:

```csharp
modelBuilder.Entity<DbExpenseLine>()
    .Property(x => x.GrossAmount)
    .HasPrecision(12, 2);
```

Without it Npgsql maps `decimal` to unconstrained `numeric`, which stores fine but lets a computed
value carry more scale than the form ever displays.

### Soft delete

Two mechanisms exist, and they differ in one important way.

**By hand, the older and more common one.** A `bool Deleted` column
(`Data/Models/Attendance/DbAttendanceRecord.cs:21`) with **no query filter**, so every read filters
it explicitly. `Grpc/AGENTS.md:186-191`:

> `DbAttendanceRecord` and `DbGamePointRecord` carry a `Deleted` flag rather than being removed, and
> every read filters it. […] filter `!x.Deleted` in every query, including counts.

e.g. `Features/Games/GameDisplayServices/GameDisplayService.cs:190`:

```csharp
            .Where(x => x.ServiceId == service.Id && !x.Deleted);
```

**By query filter, once.** `DbPerson` uses a nullable timestamp plus a global filter —
`Data/GsbcDbContext.SyncModel.cs:19-21`:

```csharp
        // soft-delete filter on DbPerson
        modelBuilder.Entity<DbPerson>()
            .HasQueryFilter(x => x.DeletedAtUtc == null);
```

with `Data/Models/People/DbPerson.cs:61-62`:

```csharp
    [MapperIgnore]
    public DateTimeOffset? DeletedAtUtc { get; set; }
```

The `[MapperIgnore]` keeps the deletion timestamp off the contract.

**Recommendation for this repo:** the scope doc and this repo's `AGENTS.md` both say "filter
`!x.Deleted` in every query, including counts", which is the by-hand form. `HasQueryFilter` is
strictly safer — a forgotten `Where` is the whole risk, and seven-year retention means a
hard-deleted row is unrecoverable. Use `HasQueryFilter` and know that `IgnoreQueryFilters()` is how
an audit view would ever see the deleted rows. Either way, note the `DbAllergy` delete path
(`AllergyServices/Delete.cs:20`) is a real `db.Allergies.Remove(...)` — ImpactKids hard-deletes
where the row is not evidence. Nothing in GSBC.Accounting is in that category.

### Migrations, and the worker that applies them

**Generation.** `dotnet ef` run directly is the one CLI exception (`Grpc/AGENTS.md:199-203`):

```bash
dotnet ef migrations add <Name> --project GSBC.ImpactKids.Grpc
```

Migrations land in `Grpc/Data/Migrations/`. The rules (`Grpc/AGENTS.md:205-212`): additive is free
and you run it yourself; destructive is proposed first; and **never suppress
`PendingModelChangesWarning`** — it is the only signal a model has drifted from its migrations.

A migration may carry data alongside DDL and comment why —
`Data/Migrations/20260808114058_1786189256.cs:13-25`:

```csharp
            migrationBuilder.AddColumn<int>(
                name: "BehaviourPointsMultiplier",
                table: "GameBoards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Behaviour points used to follow the night's multiplier, so an existing board
            // keeps showing what it showed before the two came apart.
            migrationBuilder.Sql(
                """UPDATE "GameBoards" SET "BehaviourPointsMultiplier" = "PointsMultiplier";"""
            );
```

**Application.** `GSBC.ImpactKids.Workers.DbMigrations` is a `Microsoft.NET.Sdk.Worker` host that
migrates, seeds and then stops itself. `Workers.DbMigrations/Program.cs:5-16` is the whole host:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<GsbcDbContext>("impact-kids");
```

Note `AddNpgsqlDbContext` here versus `AddPooledDbContextFactory` in the service — the worker takes
the plain Aspire registration. `Workers.DbMigrations/Worker.cs:20-54`:

```csharp
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = SActivitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope     = serviceProvider.CreateScope();
            var       dbContext = scope.ServiceProvider.GetRequiredService<GsbcDbContext>();

            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedMedicalAsync(dbContext, cancellationToken);
            ...
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(GsbcDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }
```

Seeders follow one shape: execution strategy → explicit transaction → `AnyAsync` guard per row →
`SaveChangesAsync` → commit (`Worker.cs:64-89`). They are idempotent by construction.

**The AppHost makes this a hard gate**, `AppHost.cs:83-95`:

```csharp
IResourceBuilder<ProjectResource> migrations =
    builder.AddProject<Projects.GSBC_ImpactKids_Workers_DbMigrations>("migrations")
        .WithReference(db)
        .WaitFor(db);

IResourceBuilder<ProjectResource> grpcService = builder.AddProject<Projects.GSBC_ImpactKids_Grpc>("grpc")
    ...
    .WithReference(migrations)
    .WaitForCompletion(migrations)
```

`Grpc/AGENTS.md:195-197`: a missing migration surfaces as the whole app failing to start, not as a
runtime error.

---

## 5. The frontend store pattern

Read `GSBC.ImpactKids/docs/frontend-store-architecture.md` (50 lines) alongside this; it is the
authority and it is short.

### The pattern

State runs on `EasyAppDev.Blazor.Store` (a Zustand-style store), wrapped by the project's own
`RefreshableStore<T>` in `WASM/Services/RefreshableStore/`. The flow
(`docs/frontend-store-architecture.md:7-13`):

```
gRPC service
  → IBasicReadMultipleService<T> adapter
  → RefreshableStore<T>.RefreshAll()
  → EntityListState<T>.Entities            (AsyncData<ImmutableList<T>>)
  → component
```

State is one generic record per entity type,
`WASM/Services/RefreshableStore/EntityListState.cs:7-11`:

```csharp
public record EntityListState<T>(
    AsyncData<ImmutableList<T>> Entities
) : IInitialisableState<EntityListState<T>>
{
    public static EntityListState<T> Initial => new(AsyncData<ImmutableList<T>>.NotAsked());
```

`AsyncData<T>` is a status plus a nullable payload — `NotAsked` / `Loading` / `Success` /
`Failure` — never a bare value. `WASM/AGENTS.md:24-26`: **check `HasData` before `Data!`.**
`Data == null` with no error means still loading, and rendering that as "none" is the most common
bug on these pages.

Every entity store is registered **singleton, app-wide**, one line per entity, in
`WASM/Extensions/StateStoreExtensions.cs:103-113`:

```csharp
        private IServiceCollection AddEntityStore<T>() where T : notnull
        {
            return services
                .AddStore(
                    EntityListState<T>.Initial,
                    (store, sp) => store
                        .WithDefaults(sp, typeof(T).Name)
                )
                .AddSingleton<IAsyncActionExecutor<EntityListState<T>>, AsyncActionExecutor<EntityListState<T>>>()
                .AddSingleton<IRefreshableStore<T>, RefreshableStore<T>>();
        }
```

with call sites at `StateStoreExtensions.cs:56-78` (`.AddEntityStore<Person>()` …) and page-scoped
stores registered separately via `AddPageStore<T>()` for types implementing
`IInitialisableState<T>`. The comment at `StateStoreExtensions.cs:83-84` is worth heeding: **stores
are not scoped to a component — every component updates when a single store updates**, so do not
make a reusable component's store.

`RefreshableStore<T>.RetrieveEntities` (`RefreshableStore.cs:120-145`) is what actually calls the
service: it resolves `IBasicReadMultipleService<T>` from a fresh scope, drains the server stream
with `BasicReadMultipleRequest.All()`, and concatenates the batches.

### The `RefreshAll()` seeding trap

This is the bug the doc exists to prevent, and it is subtle enough to hit twice.

Two facts collide (`docs/frontend-store-architecture.md:24-25`):

1. **`Store.Subscribe` is change-only.** A new subscriber is *not* replayed the current value.
   Subscribing gives you future changes, never the present.
2. **`RefreshAll()` goes through `ExecuteCachedAsync`, and on a cache hit it skips the state
   write.** `RefreshableStore.cs:44-53` uses `cacheFor: TimeSpan.FromMinutes(30)` under key
   `$"{typeof(T).Name}-list"`. A cache hit returns early with **no `UpdateAsync`**, so it produces
   no change and notifies nobody. Only the first (or expired) caller writes state.

So a page that copies store data into a local field inside a subscription callback — which is what
every `RefreshableStore`-based page in ImpactKids does — gets *nothing* when the store was already
warm.

**The mandatory pattern is both halves** (`docs/frontend-store-architecture.md:27-32`,
`WASM/AGENTS.md:11-20`):

```csharp
HandleSubscriptionDisposal(PeopleStore, RetrievePeople);   // re-run on store change, auto-unsubscribe
RetrievePeople();                                          // seed from whatever is cached
await Task.WhenAll(PeopleStore.RefreshAll());              // then fetch
```

Live, at `WASM/Features/Sync/Pages/Multiple.razor.cs:27-41`:

```csharp
    protected override async Task OnInitializedAsync()
    {
        _syncSub    = SyncStore.Subscribe(_ => RefreshOperations());
        _reviewsSub = PendingReviewsStore.Subscribe(_ => RefreshPendingReviews());

        RefreshOperations();
        RefreshPendingReviews();

        await Task.WhenAll(
            SyncStore.RefreshAll(),
            PendingReviewsStore.RefreshAll()
        );
    }

    private void RefreshPendingReviews()
    {
        _pendingReviews = PendingReviewsStore.GetState().Entities;
        InvokeAsync(StateHasChanged);
    }
```

`HandleSubscriptionDisposal` comes from `StoreEntityUtilityComponent`
(`WASM/Components/Base/StoreEntityUtilityComponent.razor.cs:22-28`); it just adds the subscription
to a list disposed in `Dispose()` (`:77-86`).

**The worked failure** (`docs/frontend-store-architecture.md:34`): `Individual.razor.cs` had the
subscription but omitted the explicit seed. When the store was already warm from another page,
`RefreshAll()` hit the cache → no state change → the change-only subscription never fired → the
local field stayed at its `NotAsked` default → the tab silently never appeared.

Two more rules that come with it:

- **After a write, call `RefreshEvent()`, never `RefreshAll()`** (`WASM/AGENTS.md:29-37`).
  `RefreshAll` hands back the response from *before* the mutation for up to 30 minutes;
  `RefreshEvent` invalidates the key first. This one hides, because the event-bus invalidation
  usually lands a moment later and updates the page anyway. `RefreshAll` is for arriving on a page;
  `RefreshEvent` is for having just changed something. Caveat at
  `docs/frontend-store-architecture.md:38`: `RefreshEvent` early-returns if `Entities.IsNotAsked`,
  so it will not fetch a store no component has loaded yet.
- **The library-native alternative is immune.** `StoreComponent<T>` reads
  `State => Store.GetState()` live at render, and the subscription only triggers `StateHasChanged`
  (`docs/frontend-store-architecture.md:19`). If a page in this repo can use that shape, the trap
  does not apply to it.

**Relevance to GSBC.Accounting.** The two form pages are submit-only and keep in-progress state in
`localStorage`, so they may need no entity store at all — in which case skip this whole layer rather
than adopting it decoratively. Where it *will* matter is any reference list the forms read (ministry
names, evidence types) and the attachment list on a submission being built. If you register a store
for those, the seed line is not optional.

---

## 6. YARP

The whole proxy configuration is `GSBC.ImpactKids.YARP/appsettings.json:14-55`. Three routes, two
clusters, and **order matters** — the catch-all is last:

```json
  "ReverseProxy": {
    "Routes": {
      "grpc": {
        "ClusterId": "grpc",
        "AuthorizationPolicy": "LeaderOrDisplay",
        "Match": {
          "Path": "/gRPC/GSBC.ImpactKids.{service}/{**catch-all}"
        }
      },
      "api": {
        "ClusterId": "grpc",
        "AuthorizationPolicy": "LeaderOrDisplay",
        "Match": {
          "Path": "/api/{**catch-all}"
        }
      },
      "wasm": {
        "ClusterId": "wasm",
        "Match": {
          "Path": "{**catch-all}"
        }
      }
    },
    "Clusters": {
      "grpc": {
        "Destinations": {
          "grpc": { "Address": "http://grpc" }
        }
      },
      "wasm": {
        "Destinations": {
          "wasm": { "Address": "http://wasm" }
        }
      }
    }
  }
```

Reading it:

- **gRPC-web.** The `grpc` route's path is the `[Service("gRPC/GSBC.ImpactKids.…")]` attribute value
  with a `{service}` placeholder — the two must agree, and this is the coupling to remember when
  renaming a service. YARP forwards it as an ordinary HTTP/1.1 POST; the gRPC-web unwrapping is done
  at the far end by `app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true })`
  (`Grpc/Program.cs:223`). YARP itself needs no gRPC knowledge.
- **`/api/` minimal APIs** go to the same `grpc` cluster — the minimal-API endpoints are hosted
  *inside* the gRPC project (§8). This is one destination, two protocols.
- **The WASM app** is served by the third route, a bare catch-all to a separate `wasm` project
  resource. Everything not claimed by the first two routes — `index.html`, the `.wasm` payload,
  static assets, deep links — falls through to it.
- **`http://grpc` and `http://wasm` are Aspire service-discovery names**, not hostnames. They
  resolve because of `.AddServiceDiscoveryDestinationResolver()` at
  `YARP/Extensions/HostExtensions.cs:55`, in the block that loads the config
  (`HostExtensions.cs:30-56`):

  ```csharp
              builder.Services
                  .AddReverseProxy()
                  .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                  .AddTransforms(builderContext =>
                  {
                      builderContext.RequestTransforms.Add(new RequestHeaderRemoveTransform("Cookie"));

                      if (!string.IsNullOrEmpty(builderContext.Route.AuthorizationPolicy))
                      {
                          builderContext.RequestTransforms.Add(builderContext.Services
                              .GetRequiredService<AddBearerTokenToHeadersTransform>());
                      }
                  })
                  .AddServiceDiscoveryDestinationResolver();
  ```

  The cookie is **stripped** on the way through and swapped for a bearer token — that is the BFF
  pattern. GSBC.Accounting is anonymous, so it needs neither the transform nor the
  `AuthorizationPolicy` lines, but keep `RequestHeaderRemoveTransform("Cookie")`: there is no reason
  to forward browser cookies to a service that does not read them.

`YARP/Program.cs:11-13,33-57` is the host:

```csharp
builder.AddServiceDefaults();
builder.AddReverseProxy();
builder.AddAuthenticationSchemes();
...
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

RouteGroupBuilder bff = app.MapGroup("bff");
bff.MapUserEndpoints();
...
app.MapDefaultEndpoints();

app.MapReverseProxy();
```

`app.MapReverseProxy()` is **last**, after every locally-mapped route, because the `wasm` catch-all
would otherwise swallow them.

One failure worth knowing even without auth (`HostExtensions.cs:74-84`): a 302 redirect from this
proxy falls through to the wasm catch-all and comes back as `index.html` with a **200**, which
grpc-web reports as `Bad gRPC response. Invalid content-type value: text/html`. Any middleware you
add here that redirects will produce exactly that confusing error.

**In the AppHost**, YARP is the only externally-reachable resource, `AppHost.cs:116-131`:

```csharp
IResourceBuilder<ProjectResource> wasm =
    builder.AddStandaloneBlazorWebAssemblyProject<Projects.GSBC_ImpactKids_WASM>("wasm");

IResourceBuilder<ProjectResource> yarp =
    builder.AddProject<Projects.GSBC_ImpactKids_YARP>("yarp");

yarp = yarp
    .WithReference(grpcService)
    .WaitFor(grpcService)
    .WithReference(wasm)
    .WaitFor(wasm)
    .WithExternalHttpEndpoints();

wasm = wasm.WithReference(yarp);

grpcService.WithReference(wasm);
```

`AddStandaloneBlazorWebAssemblyProject` is from `Aspire4Wasm.AppHost`. The `wasm.WithReference(yarp)`
line is what makes `new Uri("https://yarp")` resolvable from inside the browser app. Only YARP calls
`WithExternalHttpEndpoints()` — that is the single door.

---

## 7. ServiceDefaults

`GSBC.ImpactKids.ServiceDefaults/Extensions.cs` is close to the Aspire template, with three
deliberate edits. `AddServiceDefaults` (`Extensions.cs:21-45`) wires four things:

```csharp
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }
```

1. **OpenTelemetry** (`Extensions.cs:47-80`) — logging with `IncludeFormattedMessage` and
   `IncludeScopes`; metrics from ASP.NET Core, HttpClient and the runtime; tracing from
   `builder.Environment.ApplicationName` plus ASP.NET Core and HttpClient, with health-check paths
   filtered out of traces. gRPC client instrumentation is present but commented out
   (`Extensions.cs:72-73`) — it needs `OpenTelemetry.Instrumentation.GrpcNetClient`.
2. **The OTLP exporter**, only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (`Extensions.cs:82-100`),
   which Aspire sets for you.
3. **Health checks** — a single `"self"` liveness check tagged `live` (`Extensions.cs:102-110`).
4. **Service discovery** and **standard HTTP resilience** on every `HttpClient`.

`MapDefaultEndpoints` (`Extensions.cs:112-135`) maps `/health` (all checks) and `/alive` (checks
tagged `live`) — **in Development only**, and both explicitly `.AllowAnonymous()`:

```csharp
        if (app.Environment.IsDevelopment())
        {
            // Anonymous explicitly. A host that sets a deny-by-default FallbackPolicy - the
            // gRPC service does - would otherwise close these to Aspire, which probes them
            // with no credential and reports the resource unhealthy forever.
            app.MapHealthChecks(HealthEndpointPath)
                .AllowAnonymous();
```

Keep the `.AllowAnonymous()` even in an app with no auth — the moment anything sets a fallback
policy, its absence shows up as a permanently unhealthy resource in the Aspire dashboard rather than
as an error.

Every host calls `builder.AddServiceDefaults()` first (`Grpc/Program.cs:50`, `YARP/Program.cs:11`,
`Workers.DbMigrations/Program.cs:7`, `WASM/Program.cs:44`) and every web host calls
`app.MapDefaultEndpoints()` (`Grpc/Program.cs:221`, `YARP/Program.cs:54`).

---

## 8. The minimal-API upload endpoint, and the S3 client

### The endpoint pattern

File bytes never go through gRPC. They go to a minimal API mounted **inside the gRPC project**,
under `/api/…`, which YARP's `api` route already forwards (§6). The file is
`Grpc/Features/People/Photos/PersonPhotoEndpoints.cs`, and the pattern is a static class exposing
one `IEndpointRouteBuilder` extension that maps a `RouteGroupBuilder`
(`PersonPhotoEndpoints.cs:23-27`):

```csharp
public static class PersonPhotoEndpoints
{
    public static IEndpointRouteBuilder AddPersonPhotoEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder group = builder.MapGroup("api/people/{id:guid}/photo");
```

called from `Program.cs:282` after `builder.Build()`, and — importantly — **only when the store is
configured** (`Program.cs:280`), because leaving the routes unmapped makes a store-less deployment a
404 rather than a 500 from a handler that cannot resolve `PhotoStore`.

**The POST takes the raw body, not a multipart form** (`PersonPhotoEndpoints.cs:72-120`):

```csharp
        // Taking a photo. The body is the encoded image itself rather than a multipart form: the
        // capture view already has the exact bytes it wants to store, from a canvas it cropped and
        // downscaled, and wrapping them in a form only to unwrap them again buys nothing.
        group.MapPost("", async (
            Guid                id,
            HttpRequest         request,
            GsbcDbContext       db,
            PhotoStore          store,
            PhotoStoreConfig    config,
            IEventService<ContractPerson> events,
            CancellationToken   token
        ) =>
        {
            ...
            string contentType = request.ContentType ?? "";
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("Expected an image/* body.");

            using MemoryStream buffer = new();
            await request.Body.CopyToAsync(buffer, token);
            if (buffer.Length == 0)
                return Results.BadRequest("Empty body.");
            if (buffer.Length > MaxPhotoBytes)
                return Results.BadRequest($"Photo is larger than {MaxPhotoBytes / 1024} KB.");

            string version = await store.PutAsync(buffer.ToArray(), contentType, token);

            person.PhotoVersion = version;
            person.PhotoNeedsUpdate = false;

            await db.SaveChangesAsync(token);
            await events.SendUpdatedEvent(token);

            return Results.Ok(new PhotoUploadedResponse(version));
        });
```

with `private const long MaxPhotoBytes = 1024 * 1024;` at `:125` and the response record declared
`public sealed record PhotoUploadedResponse(string PhotoVersion);` at `:128` — public because
minimal-API result serialisation has to see it.

**Copy the shape; change the sizing.** ImpactKids buffers the entire body into a `MemoryStream`
before checking its length — fine for a 1 MB ceiling, wrong for a 20 MB receipt, and the scope doc
already says so: check `Content-Length` before buffering, and stream rather than materialising a
`byte[]`.

The GET (`PersonPhotoEndpoints.cs:29-70`) is worth reading for two decisions:

- **Cache-Control on the success path only** (`:67`):
  `http.Response.Headers.CacheControl = "private, max-age=31536000, immutable";`
  Safe only because the content version is in the URL. Every 404 above it is a state that can
  change, so caching one immutably would hide the fix behind the old absence.
- A version mismatch (`?v=` that is not current) is a **404, not a redirect to the current one**
  (`:52-53`), so an old URL is never poisoned in the browser cache.

### The store client

`Grpc/Features/People/Photos/PhotoStore.cs` is the whole S3 surface — two operations plus a bucket
ensure. Objects are keyed by the hash of their own bytes (`PhotoStore.cs:30-33`):

```csharp
    public static string VersionOf(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes))[..12];

    private string KeyFor(string version) => $"people/{version}.jpg";
```

Twelve hex characters — 48 bits. The scope doc asks for "enough hash to make a collision between two
different receipts implausible"; 48 bits is not that, and the key hard-codes `.jpg`. Both need
changing here.

**The write, and the two settings that fail silently.** `PhotoStore.cs:74-105`:

```csharp
        using MemoryStream stream = new(bytes);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName  = config.BucketName,
            Key         = KeyFor(version),
            InputStream = stream,
            ContentType = contentType,

            // Both of these are load-bearing, and the second one silently corrupted every photo
            // before it was set.
            //
            // DisablePayloadSigning cannot be used here: the SDK refuses it over plain HTTP - "When
            // DisablePayloadSigning is true, the request must be sent over HTTPS" - and the store is
            // reached over HTTP inside the cluster, so it turned every upload into a 500.
            //
            // With signing on, the SDK defaults to aws-chunked streaming
            // (STREAMING-AWS4-HMAC-SHA256-PAYLOAD), and SeaweedFS 3.98 stores that framing verbatim
            // instead of decoding it. The object is then the right size and the right content type
            // and is NOT a JPEG - it begins "<hex length>;chunk-signature=..." - so nothing fails,
            // the row is written, and the face simply never renders. Verified by reading an object
            // back with xxd. UseChunkEncoding = false sends the body plainly, still signed.
            UseChunkEncoding = false
        }, token);
```

Stated as a rule at `docs/modules/people/photos.md:76-86`:

> - **`DisablePayloadSigning` cannot be used.** The AWS SDK refuses it over plain HTTP, and the
>   store is reached over HTTP inside the cluster. Setting it turns every upload into a 500.
> - **`UseChunkEncoding = false` is required.** With signing on, the SDK defaults to `aws-chunked`
>   streaming, and SeaweedFS 3.98 stores that framing verbatim instead of decoding it. The object is
>   then the right size, has the right content type and a valid database row, and is *not* an
>   image — it begins `<hex length>;chunk-signature=…` rather than `FF D8 FF`. Nothing fails; the
>   face just never renders.

The scope doc asks you to **verify rather than inherit** this against whatever SeaweedFS version
this stack runs, in slice 3. The magic-byte check is how you find out —
`PhotoStore.cs:131-139,155-169`:

```csharp
            if (!LooksLikeAnImage(bytes))
            {
                logger.LogError(
                    "Photo {Version} is stored but is not image data ({Bytes} bytes, starts {Head}). "
                    + "Refusing to serve it. This is a storage bug, not a missing photo.",
                    version, bytes.Length,
                    Convert.ToHexString(bytes.AsSpan(0, Math.Min(8, bytes.Length))));
                return null;
            }
...
    private static bool LooksLikeAnImage(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12) return false;

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;

        // PNG: 89 50 4E 47
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;

        // WebP: "RIFF" .... "WEBP"
        if (bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return true;

        return false;
    }
```

This app needs PDF (`25 50 44 46`) and HEIC (`ftyp` box at offset 4, brand `heic`/`heix`/`mif1`)
added and WebP probably kept — the list is in the scope doc.

Also carried across: **`PutAsync` recreates the bucket on a `NoSuchBucket` error and retries once**
(`PhotoStore.cs:54-72`), because the startup `EnsureBucketAsync` (`:40-48`) is best-effort — there
is no ordering guarantee that the store is up when the service starts.

### Client settings and config keys

`Grpc/Features/People/Photos/PhotoStoreConfig.cs:10-20` is bound from the `Photos` section:

```csharp
public class PhotoStoreConfig
{
    public const string SectionName = "Photos";

    /// <summary>e.g. <c>http://localhost:60537</c>. Cluster-internal in production — no ingress.</summary>
    public required string ServiceUrl { get; set; }

    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }

    public string BucketName { get; set; } = "photos";
```

so the environment-variable keys are `Photos__ServiceUrl`, `Photos__AccessKey`, `Photos__SecretKey`,
`Photos__BucketName`. The AppHost sets the first three (`AppHost.cs:99-101`).

The client registration, `Grpc/Program.cs:161-185` — note that **all three of URL, access key and
secret key are checked**, not just the URL, and that the whole feature is simply absent when they
are missing:

```csharp
PhotoStoreConfig? photoConfig = builder.Configuration
    .GetSection(PhotoStoreConfig.SectionName).Get<PhotoStoreConfig>();

if (photoConfig is not null
    && !string.IsNullOrWhiteSpace(photoConfig.ServiceUrl)
    && !string.IsNullOrWhiteSpace(photoConfig.AccessKey)
    && !string.IsNullOrWhiteSpace(photoConfig.SecretKey))
{
    builder.Services.AddSingleton(photoConfig);
    builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
        new BasicAWSCredentials(photoConfig.AccessKey, photoConfig.SecretKey),
        new AmazonS3Config
        {
            ServiceURL = photoConfig.ServiceUrl,
            // Neither SeaweedFS locally nor the in-cluster one has per-bucket DNS, so the bucket has
            // to travel in the path rather than the hostname.
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1"
        }));
    builder.Services.AddScoped<PhotoStore>();
}
```

`ForcePathStyle = true` and `AuthenticationRegion = "us-east-1"` are both required against
SeaweedFS. `docs/modules/infrastructure/object-store.md:33-34`: the .NET side is `AWSSDK.S3` against
a custom endpoint with `ForcePathStyle`, so swapping the store later is an endpoint and two
credentials.

**The local container.** `AppHost.cs:61-81` — parameters for the credentials, the container, the
volume flags (§9), and a fixed host port:

```csharp
IResourceBuilder<ParameterResource> s3AccessKey =
    builder.AddParameter("s3-access-key", "impact-kids", publishValueAsDefault: true);

// No special characters: this value is signed into S3 request headers and pasted into shell and YAML
// by hand often enough that a quoting mistake is the likelier failure than a short alphabet.
IResourceBuilder<ParameterResource> s3SecretKey = builder.AddResource(
    ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, "s3-secret-key", special: false));

IResourceBuilder<ContainerResource> s3 = builder.AddContainer("s3", "chrislusf/seaweedfs", "3.98")
    .WithArgs("server", "-dir=/data", "-s3", "-s3.port=8333",
        "-master.volumeSizeLimitMB=128", "-master.volumePreallocate=false", "-volume.max=8")
    .WithEnvironment("AWS_ACCESS_KEY_ID", s3AccessKey)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", s3SecretKey)
    .WithVolume("impact-kids-s3-data", "/data")
    .WithHttpEndpoint(port: 60537, targetPort: 8333, name: "s3")
    .WithLifetime(ContainerLifetime.Persistent);
```

Locally SeaweedFS seeds a single admin identity from `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`;
in the cluster it reads an `s3.json` identities document instead, because the cluster needs a second
read-only identity for the backup job (`object-store.md:152-166`). **Anonymous access is refused** in
both (`object-store.md:168-170`, verified 2026-08-29).

The one thing SeaweedFS does *not* suffer, and it is worth knowing before you build a persistent
container with a data volume: a regenerated secret cannot lock you out, because SeaweedFS holds
nothing about its S3 identity on the volume and re-reads it from the environment at every start
(`AppHost.cs:52-60`, `object-store.md:171-184`). Postgres and RabbitMQ **do** seal their password
into the data directory on first init — that is the failure documented in
`GSBC.ImpactKids/docs/modules/infrastructure/generated-passwords.md`, and it is why the AppHost
loads user secrets in every environment, not just Development (`AppHost.cs:7-19`).

---

## 9. Do not copy this

Things ImpactKids does that GSBC.Accounting must not inherit. Each is either explicitly ruled out by
[the scope doc](2026-08-expense-forms-scope.md) or is an ImpactKids-specific decision that would be
wrong here.

**The photo store's sizing, everywhere it appears.**
The scope doc: *"Do not cargo-cult ImpactKids' photo store. It was designed for 30 KB JPEGs of
children's faces and most of its choices do not transfer."* Concretely, the things sized for a
20–50 KB JPEG:

| ImpactKids | Why it is wrong here |
|---|---|
| `MaxPhotoBytes = 1024 * 1024` (`PersonPhotoEndpoints.cs:125`) | a receipt is 1–20 MB |
| body buffered whole into a `MemoryStream` before the length check (`:101-106`) | check `Content-Length` first; stream, do not materialise |
| `GetAsync` reads the object into a `byte[]` — *"Read whole rather than streamed — these are 20–50 KB"* (`PhotoStore.cs:112`) | stream reads |
| 12 hex chars of SHA-256, 48 bits (`PhotoStore.cs:30-31`) | use enough hash that two different receipts colliding is implausible |
| key hard-codes `.jpg` (`PhotoStore.cs:33`) | PDFs and HEICs |
| no filename, no size, no hash, no uploaded-at stored — only `DbPerson.PhotoVersion` | store the real metadata a receipt needs |
| magic bytes cover JPEG/PNG/WebP only (`PhotoStore.cs:155-169`) | add PDF and HEIC; keep the check itself |

**The 1 GB volume ceiling.** `-master.volumeSizeLimitMB=128 -master.volumePreallocate=false
-volume.max=8` (`AppHost.cs:75-76`) gives a hard ceiling of `128 MB × 8 = 1 GB`, sized in
`object-store.md:120-140` against *"ten years of photos is about 250 MB"*. The scope doc:
*"ImpactKids' 1 GB volume ceiling was set for a decade of JPEGs and is not a constraint here. Size
the container and PVC for multi-megabyte PDFs from the start."*

But **do keep the two flags that are not the ceiling**. `object-store.md:126-132`: left at their
defaults, `weed server` allocates volume files of 1 GB each and grows them seven at a time, so
three small objects claimed **7 GB of disk** and filled the Docker VM outright; with these flags the
same three objects take 236 KB. So keep `-master.volumePreallocate=false` and a sane
`volumeSizeLimitMB`, and raise `volume.max` to whatever this app's PVC supports. The symptom when
the ceiling is wrong is **not** an out-of-space error: it is `400 InvalidRequest` on every PUT, with
`No more free space left` visible only in the container log (`object-store.md:137-140`).

**The presigned-URL rationale.** `object-store.md:42-47` argues against presigned URLs for
`<img>` tags: they defeat browser caching, require exposing the store publicly, and are a bearer
credential with no tie back to the leader's session. **Two of those three arguments do not apply
here** — this app has no session to tie to, and a receipt is downloaded once rather than rendered
repeatedly. The remaining reason not to expose the store publicly stands on its own. So do not cite
"ImpactKids doesn't use presigned URLs" as a reason; if the question comes up for large downloads,
it is a fresh decision. (The SeaweedFS `SignatureDoesNotMatch` bug with a static `-s3.config` that
doc links is real and would still bite in the cluster.)

**Migration names.** `Grpc/AGENTS.md:204-205` says to name a migration for what it does, and
`Grpc/createMigrations.sh` does the exact opposite:

```bash
dotnet ef migrations add "$(date +%s)" -o Data/Migrations --context GsbcDbContext
```

Every one of the ~30 migrations in `Grpc/Data/Migrations/` is therefore named for a Unix epoch —
`20260808114058_1786189256.cs`, class `_1786189256`. **Follow the AGENTS.md rule, not the script.**
This repo's `AGENTS.md` repeats it: *"Name a migration for what it does, not a timestamp."*

**The hard-coded design-time connection string.** `Data/GsbcDbContextFactory.cs:12` embeds the local
Postgres password in the `IDesignTimeDbContextFactory`, and that file is tracked and present in
`HEAD` (verified with `git ls-files --error-unmatch` and `git show HEAD:…` in the ImpactKids
checkout). It is a local dev container's password, not a production credential, so the exposure is
small — but there is no reason to repeat it. Read the connection string from an environment variable
or user secrets in the new factory.

**Everything that exists only because ImpactKids has leaders and wall displays.** All of it is
inapplicable to two anonymous form pages, and carrying any of it over adds a subsystem with no
caller:

- Auth0 / JWT bearer / `CustomClaimsTransformation` / `Policies.EnabledOnly` and the deny-by-default
  fallback (`Grpc/Program.cs:75-133`, `Grpc/AGENTS.md:97-147`).
- The display authentication scheme, enrolment keys and `DisplayReadOnlyInterceptor`.
- The OIDC/cookie BFF in YARP and the `Duende.AccessTokenManagement.OpenIdConnect` +
  `Microsoft.AspNetCore.Authentication.OpenIdConnect` packages.
- The dev sign-in bypass (`AppHost.cs:137-147`). Note the *shape* though: the scope doc asks the
  mock-data button to be gated the same way — `builder.ExecutionContext.IsRunMode &&
  builder.Environment.IsDevelopment()`, so the control cannot exist in a published build rather than
  merely being hidden.

Write the model as though auth exists (the scope doc says so), but do not build the machinery.

**RabbitMQ and the eventing layer, unless you decide you want it.** `SendUpdatedEvent` on every
mutation (`Grpc/AGENTS.md:252-256`), the SSE client, `RefreshEvent()`, the coalescing counter — all
of it exists so a change on one leader's phone reaches every other phone in the building. Two
anonymous submit-only forms have no second viewer. If you drop it, drop `Aspire.Hosting.RabbitMQ`,
`Aspire.RabbitMQ.Client.v7`, the exchange declarations at `Grpc/Program.cs:307-313` and the
`RabbitWorker`/`HeartbeatService` hosted services with it, rather than leaving a half-wired bus.

**Ports.** ImpactKids holds 60535 (redis), 60536 (postgres), 60537 (S3), 63001 (rabbit management)
and 7263 (DCP proxy), and every container is `ContainerLifetime.Persistent`, so both stacks run at
once and will fight. Pick different numbers and record them in `.claude/app-local.md`.

**`Aspire.Hosting.Kubernetes` and the Helm target.** `AppHost.cs:21-26` adds a Kubernetes
environment with `HelmChartName = "impact-kids-app"`. Also note `object-store.md:280-284`: the chart
that actually gets deployed is hand-written in `Charts/`, and is **not** the output of
`aspire publish` — that writes an untracked, unreferenced `k8s-artifacts/` that has already
diverged. Deployment is out of scope here; do not add the publishing target on the assumption it is
how the app ships.
