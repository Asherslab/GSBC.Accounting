# GSBC.Accounting — local run specifics

Resolves the `{{PLACEHOLDER}}` values in the global `run-and-inspect-app` skill
(`~/.claude/skills/run-and-inspect-app/SKILL.md`). Read the skill for the procedure; read this for
the values. Where the two disagree, **this file wins** — it is written against the running app.

| Placeholder | Value |
|---|---|
| `{{REPO_ROOT}}` | `/Users/asherp/Documents/Git/GSBC.Accounting` |
| `{{SOLUTION_PREFIX}}` | `GSBC.Accounting` |
| `{{APPHOST_PROJECT}}` | `GSBC.Accounting.AppHost` |
| `{{API_PROJECT}}` | `GSBC.Accounting.Grpc` |
| `{{WASM_PROJECT}}` | `GSBC.Accounting.WASM` |
| `{{YARP_PROJECT}}` | `GSBC.Accounting.YARP` |
| `{{TESTS_PROJECT}}` | `GSBC.Accounting.Grpc.Tests` — **does not exist yet**, no test project has been created |
| `{{RUN_CONFIG}}` | `GSBC.Accounting.AppHost: https` |
| `{{APP_PORT}}` | `7273` (https) / `5242` (http) — the YARP proxy, and the only port to open in a browser |
| `{{DASHBOARD_MCP_PORT}}` | `16046` |
| `{{ASPIRE_MCP_NAME}}` | `gsbc-accounting-aspire` |
| `{{POSTGRES_PORT}}` | `60546` |
| `{{DB_NAME}}` | `accounting` |
| `{{AUTHED_PAGE}}` | none — see Auth below |

## Auth

**There is none, by design.** Both form pages are anonymous, and the whole sign-in section of the
global skill does not apply here yet: there is no identity provider, no `/bff/dev-login`, and no
authed page to return to. A page that fails is failing for some other reason — do not go looking for
a session.

The BFF and YARP layers are still in place so auth can be added later without rearranging anything.
When it is, replace this section rather than the skill's.

`/bff/user` returning 401 is the expected answer, not a fault.

## Ports

ImpactKids holds fixed host ports 60535 (redis), 60536 (postgres), 60537 (S3), 63001 (rabbit
management), and its YARP on 7263. **Every fixed host port in this repo is a different number** —
both stacks are `ContainerLifetime.Persistent` and will be running at the same time.

The full allocation, as pinned on 2026-08-31. Anything added later takes the next free number in its
series and gets a row here.

| Port | What | Pinned in |
|---|---|---|
| 60546 | Postgres | `GSBC.Accounting.AppHost/AppHost.cs`, `WithHostPort` |
| 60547 | SeaweedFS S3 | `GSBC.Accounting.AppHost/AppHost.cs`, `WithHttpEndpoint` |
| 7273 / 5242 | YARP — **the app's address** | `GSBC.Accounting.YARP/Properties/launchSettings.json` |
| 5260 | gRPC service (ImpactKids holds 5250) | `GSBC.Accounting.Grpc/Properties/launchSettings.json` |
| 17269 / 15024 | Aspire dashboard | AppHost `launchSettings.json` |
| 21201, 22307, 16046 | Aspire OTLP, resource service, dashboard MCP | AppHost `launchSettings.json`, both profiles |

The WASM project keeps the template's own `launchSettings.json` ports. They are never used: Aspire
assigns that resource its port, and nothing should be opened there directly — the app is served
through YARP, which is what makes `/gRPC/…` and `/api/…` resolve.

## Object store

**Locally this stack runs its own SeaweedFS container**, own port, own volume, so it and ImpactKids
run side by side without interfering. In production there is one SeaweedFS with two buckets —
ImpactKids' `photos` and this app's `accounting` — so the only difference between the two configs is
the endpoint.

Sized for **1–20 MB PDFs**, not for JPEGs. ImpactKids runs `-master.volumeSizeLimitMB=128
-volume.max=8`, a hard 1 GB ceiling sized for a decade of photos; copying those numbers here would
hit the ceiling quickly, and the failure is `400 InvalidRequest` on PUT with the real cause
(`No more free space left`) only in the container log. This stack runs
`-master.volumeSizeLimitMB=1024 -volume.max=30`, a 30 GB ceiling.

`-master.volumePreallocate=false` **is** carried across and is load-bearing — left at its default,
`weed server` allocates 1 GB volume files seven at a time, and ImpactKids measured three small
objects taking 7 GB of disk on 2026-08-29.

The chunk-encoding question — whether SeaweedFS still stores `aws-chunked` framing verbatim on this
version — is answered in slice 3, not assumed. See the scope doc under Attachments.

## Verifying a submission actually landed

Once slice 1 is in, the check is `psql`, not the UI:

Both stacks name their Postgres resource `sql`, so both containers are called `sql-<hash>` and matching
on the name picks the wrong one about half the time. **Match on the published port instead** — 60546 is
this app's, 60536 is ImpactKids'.

```bash
c=$(docker ps --format '{{.Names}}\t{{.Ports}}' | grep ':60546->' | cut -f1)
docker exec -e PGPASSWORD="$(docker exec $c printenv POSTGRES_PASSWORD)" $c \
  psql -U postgres -d accounting -c 'select "Id","Kind","Status","TotalGross" from "ExpenseSubmissions" order by "CreatedAt" desc limit 5'
```

A form that says it submitted and a row that exists are different claims. Make the second one.
