# GSBC.Accounting

Church finance forms. Two anonymous submission pages backed by Aspire + code-first gRPC + Blazor
WASM behind a YARP BFF — the same shape as `GSBC.ImpactKids`, from which most of this repo's
conventions and a good deal of its code are inherited.

**Read [docs/work/2026-08-expense-forms-scope.md](docs/work/2026-08-expense-forms-scope.md) before
writing any code here.** It fixes the domain model (the two paper forms are one aggregate, not two),
the attachment design and the object-store traps that come with it, what being anonymous costs, and
the slice order.

# Documentation

`docs/` carries the reasoning behind decisions that are not obvious from the code.
**[docs/README.md](docs/README.md) is the map** — read it before hunting through the tree.

- `docs/modules/<module>/` — how things work now. The **only** place to read from when answering
  "how does X work". Always `status: current`.
- `docs/work/YYYY-MM-<slug>.md` — plans, handovers and design discussions. Write new ones here,
  never in `modules/`.
- `docs/open-questions/` — one unresolved question per file.
- `docs/archive/` — finished, rejected or superseded work. Read for *why*; never cite it as current
  behaviour, and never edit it.

Every doc has front-matter with `kind`, `status`, `code:` and `verified:`. **Writing, moving or
folding a doc — or changing code a doc describes — follow [docs/AGENTS.md](docs/AGENTS.md).**

## Where to read about the inherited architecture

Do not copy `GSBC.ImpactKids`' docs into this repo. Read them there and record only the difference:

- `GSBC.ImpactKids/docs/modules/infrastructure/object-store.md` — the SeaweedFS volume-flag trap, why
  there are no presigned URLs, and why the backup is `copy` and never `sync`.
- `GSBC.ImpactKids/docs/modules/infrastructure/generated-passwords.md` — why running a Production
  profile locally locks the database out.
- `GSBC.ImpactKids/docs/modules/people/photos.md` — the S3 client settings that fail *silently*.
- `GSBC.ImpactKids/docs/frontend-store-architecture.md` — the store pattern, and why a page must seed
  its own state after `RefreshAll()` rather than relying on the subscription.

# Local tooling

**Never `dotnet run`.** Run configurations only — `mcp__rider__execute_run_configuration`, per the
global `run-and-inspect-app` skill plus [`.claude/app-local.md`](.claude/app-local.md), which
resolves that skill's placeholders for this repo. A CLI-launched app duplicates Rider's processes and
fights over the pinned ports, and it dies when the session that started it goes away.

This includes one-off side experiments: starting one project on a spare port to test a config gate is
still `dotnet run`. Use a run configuration, or ask for one.

Build through Rider rather than the CLI:

- build — `mcp__rider__build_solution`, which also works while the app is running
- per-file analysis — `mcp__rider__get_file_problems`, Rider's own inspections, so it catches more
  than the compiler

`dotnet ef` is fine to run directly — there is no MCP equivalent.

**Edit files with the Edit/Write tools, never with `sed` or a `python` heredoc.** A shell replace that
matches nothing fails silently and hides the diff — you get a green exit code and an unchanged file.

**ImpactKids' ports are taken.** Both stacks are `ContainerLifetime.Persistent` and will run at the
same time. Every fixed host port here must differ; the numbers are in `.claude/app-local.md`.

# Implementation Rule — Vertical Slices

Work in vertical slices, not in layers. A slice is one operation carried end-to-end: contract → DB
model → converter → service interface → service implementation → DI registration → frontend.

One operation per slice — "read multiple", "read one", "create", "update", "delete". Do not batch a
layer across operations. Do not stop for approval at layer boundaries.

## One slice, one reviewable change

Each slice is finished when it:

- builds — `mcp__rider__build_solution`
- registers what it added — `Program.cs` service mapping on the server, DI and client registration in
  the WASM app
- **has been seen working in the running app.** Start it through Rider, drive the page, and read the
  rows it actually wrote. This is a hard gate, not a nicety — it is where integration bugs surface,
  and until there is a test project it is the only gate there is.
- updates any `docs/modules/` doc whose documented behaviour it changed, per [docs/AGENTS.md](docs/AGENTS.md)

The only thing allowed through the gate unfinished is something a later slice in the same feature
will complete — a stubbed empty state, a disabled control, a placeholder count. Say so.

## Money and evidence

Two rules specific to this repo, both because it holds financial records:

- **The server recomputes every total.** Line sums, GST, less-personal, net. The client's arithmetic
  is a display convenience. A submission whose claimed total disagrees with its lines is refused,
  with both figures named in the error.
- **Nothing hard-deletes a submission or an attachment.** ACNC retention is seven years. Soft-delete
  and filter `!x.Deleted` in every query, including counts.

## Migrations and contracts

- **Migrations** — additive is free (new nullable-or-defaulted columns, new tables, widening types,
  new indexes) and you run `dotnet ef` yourself. Name a migration for what it does, not a timestamp.
  Never suppress `PendingModelChangesWarning` — it is the only signal a model has drifted.
- **Contracts** — change freely. Every consumer is in this repo. Both ends must be rebuilt together.

## When to stop and ask

Only for what cannot be undone by editing code:

- a migration that drops or rewrites existing data. Propose it; do not run it.
- deleting or overwriting data outside a migration
- anything with an out-of-repo prerequisite — a cluster change, the object-store volume ceiling, a
  deploy-order dependency
- an ambiguity where two readings lead to materially different designs

Otherwise report at slice boundaries and keep going.

# Git

`master` is the trunk.

Never:

- **commit on `master`** unless the user explicitly asks for it *and* you have told them plainly that
  the current branch is `master`. Both halves — the request and the acknowledgement — every time.
- **create branches.** The user makes them. When work needs one, say so and wait.
- **push anything, anywhere.** No `git push`, no `--force`, no branch or tag pushes.

Reading is always fine: `git log`, `git diff`, `git merge-base`, `git status`.

On a branch the user has made — `feature/*` and the like — commit per slice without asking.
