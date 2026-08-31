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

## Restarting after a change — read this before debugging a blank page

**`execute_run_configuration` on an already-running AppHost does not restart it.** It returns success
and a fresh log path, so it looks like a restart, and nothing about the result says otherwise. On
2026-08-31 this cost an hour: the reported "new" run's processes still had the *original* start time.

Check the process, not the tool's answer:

```bash
ps -o pid,lstart,command -p "$(pgrep -f 'GSBC.Accounting.AppHost/bin')" | cut -c1-110
```

To actually restart, stop it first — `kill <apphost pid>` takes the whole DCP tree with it — then run
the configuration again.

### The symptom when you skip that

**The page hangs on the boot spinner at 0%, with no error anywhere.** The Blazor dev server holds the
static-web-asset manifest it started with, and a rebuild refingerprints the app assembly, so the
manifest names a file that no longer exists. It is a 404 for one asset and a silent stall for the user.

Confirm it in one request — the fingerprint comes from the build output:

```bash
f=$(grep -o 'GSBC.Accounting.WASM\.[a-z0-9]*\.wasm' \
      GSBC.Accounting.WASM/bin/Debug/net10.0/GSBC.Accounting.WASM.staticwebassets.endpoints.json |
    sort -u | head -1)
curl -sk -o /dev/null -w "$f -> %{http_code}\n" "https://localhost:7273/_framework/$f"
```

404 means the running dev server is stale — restart, do not go looking for a bug. **Check it against
the dev server directly as well as through YARP** (`lsof -nP -iTCP -sTCP:LISTEN -a -p <devserver pid>`
gives its port): identical answers from both prove the proxy is innocent, which is worth thirty seconds
before suspecting the routing.

A related red herring: `_framework/blazor.boot.json` 404s, and `_framework/dotnet.js` answers 200 with
**0 bytes**. Both are normal on .NET 10 — the boot manifest is inlined into `blazor.webassembly.js` and
the real runtime files are fingerprinted. Neither is evidence of anything.

## Auth

**Nobody signs in, but there IS authentication now — of the browser, not of a person.** The
`__gsbc_anon` cookie is authenticated as an "anonymous session" and the `AnonymousSession` policy gates
almost every call (see `docs/modules/expenses/drafts.md`). The sign-in section of the global skill still
does not apply: there is no identity provider, no `/bff/dev-login`, and no authed page to return to.

What this means when driving the app:

- **A first-time browser is refused by almost everything, and that is correct.** Only `Create` (and the
  read-by-id PDF/attachment download) is reachable without a session. `/drafts` showing "No saved
  drafts" on a fresh node is the policy working, not a failure.
- **The session is minted by saving a draft, and nothing else.** Type enough into a form for `Create` to
  pass validation and the cookie appears. Loading a page never mints one, so a node that has only
  browsed has no session at all.
- Validation gates that mint: the reimbursement form needs a line **with a date**, the debit-card form a
  line **with an item** — and the autosave deliberately swallows refusals, so a half-filled form saving
  nothing is normal rather than a bug.
- Check a session exists with
  `select * from "AnonymousSessions"` — not by looking for an error on screen.

The BFF and YARP layers are still in place so a real sign-in can be added later without rearranging
anything — it would get its own scheme and policy beside `AnonymousSession`, not extend it. When that
lands, replace this section rather than the skill's.

`/bff/user` returning 401 is the expected answer, not a fault.

### Driving the forms: two traps found on 2026-09-01

- **`date` and `time` inputs do not accept typed text** through the nodeterm node — neither
  `2026-09-01` nor `09012026` lands, and the field reads back empty. The reimbursement form cannot be
  completed this way because `Create` requires a line date; **use the debit-card form**, whose line
  needs a text item instead.
- **Chrome's network panel reports these 401s as `503`.** The gRPC call to a gated method really
  answers 401; the extension's network view showed `503` for it, which reads as "YARP can't reach the
  destination" and sent me chasing a dead service that was healthy. **Trust the console, not the
  network panel** — it logs the truth: `Status code: 'Unauthenticated', Message: 'Bad gRPC response.
  HTTP status code: 401'`. (A real transient 503 does happen for a few seconds right after a restart,
  while YARP's destination health check comes up, which is what made this convincing.)
- **Chrome coordinate clicks reach the fixed `.actionbar`; nodeterm cannot.** Save draft, Submit and
  Fill with mock data all work via `computer left_click` at the button's screenshot coordinates, and
  the in-page Discard confirm opens too. When a test needs those buttons, ask for Chrome rather than
  fighting the nodeterm refusal.
- **A new nodeterm browser node is NOT a new visitor — nodes share one cookie jar.** Opening a second
  node to check "what a stranger sees" showed the first node's draft, because it presented the same
  `__gsbc_anon` cookie. To test the unauthenticated path, use `curl` (its own jar) or ask the user for a
  private window; do not trust a fresh node to be cookie-free.
- **The `.actionbar` footer is `position: fixed`, and every click on it is refused as "off-screen"** —
  Save draft, Submit, Fill with mock data. Scrolling does not help; the refusal is the tool
  mis-measuring a fixed element. Drive the form by typing and let the 2-second autosave do the write,
  and verify in Postgres rather than by pressing Save.

## Ports

ImpactKids holds fixed host ports 60535 (redis), 60536 (postgres), 60537 (S3), 63001 (rabbit
management), and its YARP on 7263. **Every fixed host port in this repo is a different number** —
both stacks are `ContainerLifetime.Persistent` and will be running at the same time.

The full allocation, as pinned and **verified listening on 2026-08-31**. Anything added later takes the
next free number in its series and gets a row here.

For the two containers these are the ports **Aspire's DCP proxy** listens on, not what Docker publishes:
`WithHostPort` / `WithHttpEndpoint` pin the proxy, and the container itself gets a random high port. So
`localhost:60546` reaches Postgres, but `docker ps` shows something else entirely — which matters when
looking a container up, see below.

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
on the name picks the wrong one about half the time.

**Do not match on the port either.** The ports in the table above are DCP *proxies* — Aspire publishes
the container on a random high port and listens on the pinned one itself. `docker ps` on 2026-08-31
showed this app's Postgres published on 64556 while 60546 was held by the proxy, so
`docker ps | grep 60546` finds nothing.

Match on the **data volume**, which is named deliberately and is unique to this stack:

```bash
c=$(docker ps -q --filter 'label=com.microsoft.developer.usvc-dev.mountsLabel=type=volume,src=gsbc-accounting-sql-data')
docker exec -e PGPASSWORD="$(docker exec $c printenv POSTGRES_PASSWORD)" $c \
  psql -U postgres -d accounting -c 'select "Id","Kind","Status","TotalGross" from "ExpenseSubmissions" order by "CreatedAt" desc limit 5'
```

A form that says it submitted and a row that exists are different claims. Make the second one.
