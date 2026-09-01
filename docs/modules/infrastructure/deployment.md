---
title: How this deploys
kind: reference
status: current
module: infrastructure
verified: 2026-09-01
code:
  - .github/workflows
  - Charts/accounting
  - GSBC.Accounting.Grpc/Dockerfile
  - GSBC.Accounting.WASM/nginx.conf
---

# How this deploys

Production is `expenses.baptist.com.au`, on the single-node k3s cluster on the Mac mini. Nothing is
deployed by hand and nothing in this repo holds a cluster credential: a push to `master` produces
images and a chart, then writes a version number into the `gsbc.argo` repo, and Argo CD's automated
sync does the rest.

The same shape as `GSBC.ImpactKids`, deliberately — one pipeline to understand rather than two. Where
this differs from that one, the difference is recorded here and only here.

## The pipeline

`.github/workflows/docker-publish.yml` on a push to `master`:

1. **version** — `paulhatch/semantic-version` reads the tag history, and the job pushes `v<version>`.
   The clone must be `fetch-depth: 0` or every run believes it is the first.
2. **four image builds, in parallel** — migrations, grpc, wasm, yarp, each `linux/amd64,linux/arm64`.
   arm64 is not optional; the cluster is a Mac mini.
3. **chart** — `chart-publish.yml` rewrites `Chart.yaml`'s `version` and `values.yaml`'s `image_tag`
   from the semantic version, then `chart-releaser` publishes to this repo's `gh-pages`, which Argo
   reads as `https://asherslab.github.io/GSBC.Accounting`.
4. **argo-update** — `argo-repo-update.yml` seds the chart version into
   `gsbc.argo/clusters/mini/app-definitions/accounting.yaml` and pushes.

Step 4 waits on step 3 and on all four builds. Pointing Argo at a chart that is not published yet
fails the sync with "chart not found"; pointing it at images that are still building is an
`ImagePullBackOff`.

**The `#helm` comment in `accounting.yaml` is load-bearing.** That Application has three sources and
three `targetRevision` keys — the other two are `HEAD` for the git sources — so the sed matches the
trailing comment rather than the key. Delete the comment and deployments stop *silently*: the sed
matches nothing, the "no changes" branch reports success, and the cluster keeps running the old
version.

### Repository settings this needs

| Name | Kind | Why |
|---|---|---|
| `DOCKERHUB_USERNAME` | variable | Docker Hub login. |
| `DOCKERHUB_TOKEN` | secret | Docker Hub login. |
| `ARGO_REPO_TOKEN` | secret | Push access to `asherslab/gsbc.argo`. `GITHUB_TOKEN` is scoped to this repository and cannot push there. |

`gh-pages` must exist and be served by GitHub Pages, or the chart publishes to a branch nobody reads.

## The chart

`Charts/accounting`, chart name `accounting-app`. Six components: sql, s3, migrations, grpc, wasm,
yarp. No RabbitMQ and no Redis — this app has no queue and no cache, which is four workloads to
ImpactKids' six.

Only `yarp-service` is reachable from outside the namespace. Everything — the WASM app, `/gRPC/` and
`/api/` — is served from that one origin, which is what lets the browser hold the `__gsbc_anon` cookie
against it (see [drafts.md](../expenses/drafts.md)).

`migrations` is a Helm `post-install,post-upgrade` hook with `hook-delete-policy: before-hook-creation`.
Without that policy the *second* upgrade fails: a completed Job's pod template is immutable, so the
apply is rejected rather than re-run.

### Scaling out is safe, and the reason is not obvious

The anonymous session token is hashed into a Postgres row, not signed with ASP.NET Data Protection. So
there is no key ring to share, no sticky sessions to configure, and a rolling restart does not
invalidate anybody's drafts. Adding replicas to `grpc-deployment` needs no other change.

### SeaweedFS numbers differ from ImpactKids' on purpose

This deployment has **its own SeaweedFS**, in its own namespace, sharing nothing with ImpactKids'.
Each instance is one small container, so sharing saves nothing worth the coupling, and separate
identities mean a credential or capacity problem on one side cannot reach the other's objects.

| | ImpactKids | Here |
|---|---|---|
| Typical object | 30 KB face JPEG | 1–20 MB receipt PDF or phone photo |
| `volumeSizeLimitMB` | 128 | 1024 |
| `volume.max` | 8 | 30 |
| PVC | 2Gi | 20Gi |

`-master.volumePreallocate=false` is carried across unchanged and is load-bearing. Left at its
default, `weed server` allocates 1 GB volume files and grows them seven at a time; ImpactKids measured
three small objects consuming 7 GB of disk on 2026-08-29. Against these PVCs that is an immediate
out-of-space, and the S3 error is a bare `400 InvalidRequest` with the real cause ("No more free space
left") only in the container log.

The PVC is deliberately smaller than `volumeSizeLimitMB × volume.max`. That product is a ceiling on
what SeaweedFS *may* allocate, not a reservation. If the store approaches 20 GB, grow the PVC first
and only then raise `volume.max`.

### The bucket creates itself

`GSBC.Accounting.Grpc/Program.cs` calls `EnsureBucketAsync()` at startup, best-effort, and `PutAsync`
creates the bucket on demand if that did not manage it. So there is no bucket-creation step in the
runbook — but the app's SeaweedFS identity needs `Admin` for it, which is why the identities document
grants it.

## Secrets

Five always, plus a sixth once the offsite backup is turned on. All created out of band with
`kubectl`, all in the `accounting` namespace. **The chart renders
no Secret and no values file holds a credential**, because values files live in `gsbc.argo` and that is
git.

| Secret | Keys | Consumer |
|---|---|---|
| `sql-secrets` | `POSTGRES_PASSWORD` | sql |
| `migrations-secrets` | `ConnectionStrings__accounting` | migrations Job |
| `grpc-secrets` | `ConnectionStrings__accounting` | grpc |
| `s3-identities-secret` | `s3.json` | s3 |
| `attachments-secret` | `Attachments__AccessKey`, `Attachments__SecretKey` | grpc |
| `s3-backup-secret` | four `RCLONE_CONFIG_*` keys | backup CronJob, only when enabled |

**The exact commands live in `gsbc.argo/clusters/mini/README.md`, under "accounting".** They are not
duplicated here: one copy, in the repo where the cluster is described.

Three things about them are worth knowing before touching one:

- **`attachments-secret` is not optional.** Its absence stops the gRPC pod starting. That is the
  opposite of ImpactKids' `photos-secret`, which is mounted `optional: true` so a missing photo
  credential degrades to coloured initials instead of taking sign-in down. Here the receipt is the
  point of the form, so a deployment that accepts claims it cannot attach evidence to is worse than
  one that will not start.
- **Postgres only applies `POSTGRES_PASSWORD` when it initialises an empty data directory.** Rotating
  that Secret changes what the containers *present*, not what the database *accepts*: the volume keeps
  the original and every caller is locked out with `28P01: password authentication failed for user
  postgres`. Rotating means `ALTER USER` inside the database first, then the Secret, then a restart.
  ImpactKids did exactly this to itself on 2026-08-24.
- **SeaweedFS reads its identities once at startup**, and the chart renders no checksum to hang a
  rollout annotation on. An identity change updates nothing, with no error either way, until
  `kubectl -n accounting rollout restart statefulset/s3-statefulset`.

## The offsite backup, and why it is off

`backup.s3.enabled` is `false` until the Backblaze bucket and application key exist.

The receipts are the one thing in this deployment that cannot be regenerated. They are held outside
the database on purpose, so the Postgres dump does not cover them, and the object store is a single
replica on one PVC — fine for serving, and not a backup. A lost receipt is a hole in a seven-year
audit trail.

`rclone copy`, never `sync`: copy does not delete at the destination, so a bad migration or a
fat-fingered bulk delete on our side cannot erase the offsite copy. `--immutable` fails the run loudly
if an object's content ever changes under a name that should be a content hash — that is a bug in the
app, and this is where it would surface. The full reasoning, including why SeaweedFS' own
`filer.backup` was rejected, is in `GSBC.ImpactKids/Charts/impact-kids/templates/s3/backup-cronjob.yaml`
and applies unchanged.

Because the two SeaweedFS instances are separate, **this needs its own B2 bucket and its own
application key.** Adding `Read:accounting` to ImpactKids' backup identity would do nothing: that
identity lives in the other SeaweedFS.

Do not set `enabled: true` with `bucket` or `endpoint` blank. Both are `required` in the template, so
the render fails — and a failed render fails the sync for the *whole* application, not just the
backup.

## Two container details that are easy to lose

**The gRPC image installs `libfontconfig1` and `libfreetype6`.** QuestPDF renders through SkiaSharp,
whose native library links against them, and the `aspnet` image does not carry them. The failure is
not a startup error: the service comes up healthy and the first `GET /api/submissions/{id}/pdf` throws
a `DllNotFoundException` from inside Skia. ImpactKids' gRPC Dockerfile has no equivalent line because
it renders no documents — do not "tidy" this away by matching it.

**`/_framework/` must never fall back to `index.html`.** `nginx.conf` gives it its own `location` with
`try_files $uri =404`. Under the SPA fallback a missing hash returns `200 text/html`, the runtime fails
its integrity check, and the user gets an opaque "Load failed" instead of an obvious 404. `index.html`
is `no-cache` for the same family of reason: there is no service worker in this app, so a fresh
`index.html` on navigation is the entire update story.

## Caching, and the Cloudflare setting it depends on

There are **two** caches between a publish and a user, and they fail differently. Fixing one and
assuming the other followed is how this has now gone wrong twice.

The rule the origin follows: **a file may be cached forever if and only if its name changes when its
bytes change.** Never key on the directory. Under .NET 10 the files with stable names are `index.html`,
`_framework/blazor.webassembly.js`, `_framework/dotnet.js`, and everything in `wwwroot` the SDK does
not fingerprint (`css/app.css`, `favicon.png`). Everything else under `_framework/` carries ten base-36
characters before its extension. The loader cannot be fingerprinted out of this problem — see the
`OverrideHtmlAssetPlaceholders` comment in the csproj, which records what happens when you try.

`dotnet.js` is the whole game. `index.html` names only `blazor.webassembly.js`, which loads
`dotnet.js`, which is the manifest naming every hashed asset. Pin those two and a deploy is invisible
however fresh `index.html` is, because nothing the browser re-reads ever mentions a changed byte.

### The zone rewrites Cache-Control, and only on what it caches

Measured against the deployed origin on 2026-09-01, `expenses.baptist.com.au` on default cache settings:

| Path | Origin sends | Browser receives | `cf-cache-status` |
|---|---|---|---|
| `/index.html` | `no-cache` | `no-cache` | DYNAMIC |
| `*.<hash>.wasm` | `max-age=31536000, immutable` | unchanged | DYNAMIC |
| `/_framework/dotnet.js` | `no-cache` | **`max-age=14400`** | MISS |
| `/_framework/blazor.webassembly.js` | `no-cache` | **`max-age=14400`** | EXPIRED |
| `/css/app.css`, `/favicon.png` | `no-cache` | **`max-age=14400`** | MISS |

`14400` appears in no file in this repo. It is the zone's **Browser Cache TTL, set to 4 hours rather
than "Respect Existing Headers"**, and it rewrites `Cache-Control` on the way to the browser — but only
on responses the edge actually stored. The split is Cloudflare's default static-extension list, not
anything we send: `.js`, `.css` and `.png` are on it; `.html` and `.wasm` are not.

So the rewrite landed on exactly the two stable-named boot files that must revalidate, and on nothing
that would have benefited — while the 4.85 MB of fingerprinted `immutable` payload was not edge-cached
at all. The edge was doing precisely the wrong half of the job.

**Browser Cache TTL must be "Respect Existing Headers".** No origin configuration can outrank an edge
that rewrites the header, so that setting is the fix and everything below is defence in depth.

### Why the map default is `private, no-cache`

`private` marks a response as belonging to one user's cache, so a shared cache should decline to store
it — and a response the edge never stored is one whose `Cache-Control` it never rewrites. The browser
still caches and still revalidates, so the 304s are kept. It makes correctness a property of
`nginx.conf` rather than of a dashboard toggle invisible from this repo.

Treat it as belt-and-braces, not as a substitute for the setting above: the measurement proves the zone
ignored `no-cache` on `.js`, so it may ignore `private` there too. Confirm after the next deploy —
`curl -sSI https://expenses.baptist.com.au/_framework/dotnet.js` must **not** show a `max-age` above 0.

### Bypassing the cache is not the fix, and it is not what saves ImpactKids

`kids.baptist.com.au` runs with the Cloudflare cache bypassed, and its `nginx.conf` still matches the
whole of `~^/_framework/` as immutable — the bug this app fixed on 2026-09-01. Measured the same day:
every path is `DYNAMIC`, so the bypass does hold at the edge, and `dotnet.js` reaches the browser as
`public, max-age=31536000, immutable`. A bypass suppresses the edge cache and does nothing whatever
about the browser.

**What keeps that app deployable is its service worker, not the bypass.** Its install pass builds every
request as `new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' })`, and `cache:
'no-cache'` forces revalidation against the origin, bypassing the HTTP cache's freshness — `immutable`
included. `_framework/` is then served from the SW cache at runtime, so the poisoned HTTP-cache entry
is never consulted. Registration passes `updateViaCache: 'none'` and nginx marks
`service-worker-assets.js` `no-cache`, so the chain that triggers a reinstall stays fresh too.

So ImpactKids' header is a **latent** trap rather than an active one. It is still worth fixing: the
protection lasts exactly as long as the service worker does, a hard reload bypasses the SW and lands on
the year-long entry, and any client where registration fails has no cover at all.

**This app has no service worker.** That is precisely why the same latent bug bit hard here and merely
lurked there — and why the ordering above matters. `index.html` on navigation is the entire update
story, so the headers have to be right on their own.

### What the zone should hold instead

1. Browser Cache TTL → **Respect Existing Headers**.
2. A Cache Rule making `/_framework/` eligible, with Edge TTL taken from the origin header. Safe
   *because* of the rule above: the fingerprinted assets are `public, immutable` and get stored, while
   the two stable-named loaders are `private` and do not. This is the cold-load win — 4.85 MB from the
   Brisbane edge rather than from the Mac mini through the tunnel.
3. One purge after the change, to evict the `max-age=14400` copies already at the edge.

None of this is in git: the zone is configured in the Cloudflare dashboard, and this repo holds no
Cloudflare credential.

## What the cluster holds that this repo does not

In `gsbc.argo`, under `clusters/mini/`:

- `app-definitions/accounting.yaml` — the Argo Application, and the `#helm` marker CI rewrites.
- `../../apps/accounting/values.yaml` — the shared Helm overlay. Currently only the backup block.
- `app/accounting/ingress.yaml` — the LAN path. Split-horizon DNS points on-site clients at the node;
  cert-manager solves DNS-01, so the certificate is valid on both paths.
- `app/accounting/tunnel-bindings.yaml` — the public path, via `baptist-tunnel-mini`. The operator
  creates and deletes the Cloudflare record; do not also create it by hand.
- `app/accounting/limitrange.yaml` — namespace-wide default requests and limits, because the chart
  exposes no resources knobs and on a single node an unconstrained leak takes out everything.
