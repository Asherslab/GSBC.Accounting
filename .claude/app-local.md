# GSBC.Accounting — local run specifics

Resolves the `{{PLACEHOLDER}}` values in the global `run-and-inspect-app` skill
(`~/.claude/skills/run-and-inspect-app/SKILL.md`). Read the skill for the procedure; read this for
the values. Where the two disagree, **this file wins** — it is written against the running app.

**Status: not yet true.** The solution does not exist yet (see
[docs/work/2026-08-expense-forms-scope.md](../docs/work/2026-08-expense-forms-scope.md), slice 0).
Every value below is the intended one. Verify each against `launchSettings.json` and the Rider run
configuration when slice 0 lands, then delete this paragraph.

| Placeholder | Value |
|---|---|
| `{{REPO_ROOT}}` | `/Users/asherp/Documents/Git/GSBC.Accounting` |
| `{{SOLUTION_PREFIX}}` | `GSBC.Accounting` |
| `{{APPHOST_PROJECT}}` | `GSBC.Accounting.AppHost` |
| `{{API_PROJECT}}` | `GSBC.Accounting.Grpc` |
| `{{WASM_PROJECT}}` | `GSBC.Accounting.WASM` |
| `{{YARP_PROJECT}}` | `GSBC.Accounting.YARP` |
| `{{TESTS_PROJECT}}` | `GSBC.Accounting.Grpc.Tests` |
| `{{RUN_CONFIG}}` | `GSBC.Accounting.AppHost: https` |
| `{{APP_PORT}}` | *pin in `GSBC.Accounting.YARP/Properties/launchSettings.json`* |
| `{{DASHBOARD_MCP_PORT}}` | *pin in the AppHost's `launchSettings.json`, both profiles* |
| `{{ASPIRE_MCP_NAME}}` | `gsbc-accounting-aspire` |
| `{{POSTGRES_PORT}}` | *next free port in the series; ImpactKids holds 60536* |
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
management), and the DCP proxy on 7263. **Every container port in this repo must be a different
number** — both stacks are `ContainerLifetime.Persistent` and will be running at the same time.
Pick the next free number in each series and record it above.

## Object store

**Locally this stack runs its own SeaweedFS container**, own port, own volume, so it and ImpactKids
run side by side without interfering. In production there is one SeaweedFS with two buckets —
ImpactKids' `photos` and this app's `accounting` — so the only difference between the two configs is
the endpoint.

Size the local container and its volume for **1–20 MB PDFs**, not for JPEGs. ImpactKids runs
`-master.volumeSizeLimitMB=128 -volume.max=8`, a hard 1 GB ceiling sized for a decade of photos;
copying those numbers here would hit the ceiling quickly, and the failure is `400 InvalidRequest` on
PUT with the real cause (`No more free space left`) only in the container log. Do keep
`-master.volumePreallocate=false` — left at its default, `weed server` allocates 1 GB volume files
seven at a time and three small objects took 7 GB of disk.

The chunk-encoding question — whether SeaweedFS still stores `aws-chunked` framing verbatim on this
version — is answered in slice 3, not assumed. See the scope doc under Attachments.

## Verifying a submission actually landed

Once slice 1 is in, the check is `psql`, not the UI:

```bash
c=$(docker ps --format '{{.Names}}' | grep '^sql-' | grep -i account)
docker exec -e PGPASSWORD="$(docker exec $c printenv POSTGRES_PASSWORD)" $c \
  psql -U postgres -d accounting -c 'select "Id","Kind","Status","TotalGross" from "ExpenseSubmissions" order by "CreatedAt" desc limit 5'
```

A form that says it submitted and a row that exists are different claims. Make the second one.
