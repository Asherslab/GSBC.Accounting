---
title: Documentation map
kind: reference
status: current
verified: 2026-09-01
---

# Documentation

Start here. Every doc carries front-matter saying what it is (`kind`) and whether it is still true
(`status`), so you can tell reference from a plan without reading it.

Writing or moving a doc rather than reading one? [AGENTS.md](AGENTS.md) is the procedure — which
directory, what front-matter, and how a doc leaves `work/`.

Operating the repo — running the app, driving the browser, reading the database — is not here. That
is the global `run-and-inspect-app` skill plus this repo's [`.claude/app-local.md`](../.claude/app-local.md),
and the repo-wide rules are in [../AGENTS.md](../AGENTS.md).

## For how something works, read `modules/` only

Present tense, kept current, no dates.

- **expenses**
  - [The expense submission model](modules/expenses/submission-model.md) — why the two paper forms are
    one aggregate and one page, what that does *not* let you share, what happens when a claimant
    changes their answer to the form's first question, and the money and retention rules.
  - [What the two paper forms actually say](modules/expenses/paper-form-fields.md) — the ground truth
    for every string the app prints, read out of the `.docx` files.
  - [The attachment store](modules/expenses/attachments.md) — the SeaweedFS chunk-encoding trap that
    silently stores the wrong bytes, the magic-byte checks, and why the anonymous upload needs ceilings.
  - [Drafts and the draft session cookie](modules/expenses/drafts.md) — who owns an unsubmitted form,
    why the submission id stopped being sufficient authority, the cookie's attributes and lifetimes,
    the 90-day purge, and the upgrade path to real accounts. **Read before touching any read or write
    of a submission.**
  - [Rendered examples](examples/README.md) — the two PDFs `GET /api/submissions/{id}/pdf` produces,
    checked in for the parked output-format conversation.

- **infrastructure**
  - [How this deploys](modules/infrastructure/deployment.md) — the push-to-`master` pipeline, the Helm
    chart, the six out-of-band secrets, why this app has its own SeaweedFS with different volume
    numbers, and the two container details (Skia's native libraries, `/_framework/` never falling back
    to `index.html`) that fail in confusing ways when missed.

- **frontend**
  - [The theme, and why there is no component library](modules/frontend/theme.md) — `app.css` is a
    lift of the approved mockup, the three theme states, and the duplicated dark palette that must
    stay in sync.

## The other three directories

| Directory | What is in it | Trust it for |
|---|---|---|
| `work/` | plans and discussions for changes not yet finished | what someone intends to do |
| `open-questions/` | one unresolved question per file | knowing something is *not* settled |
| `archive/` | finished, rejected or superseded work | history and reasoning only — **never** current behaviour |

### In flight now

- [What is outstanding](work/2026-08-outstanding.md) — the short list of what is *not* done now the
  expense forms scope has landed: the decisions parked with the accountant and with finance, the
  cluster work outside this repo, and the known gaps. **Start here.**

The scope itself is [archived](archive/2026-08-expense-forms-scope.md) — read it for why the app looks
like this, and for the places where building it proved the plan wrong. Never cite it as current
behaviour.

### Open

Nothing. Add an entry when you open an `open-questions/` doc.

## Lifecycle

A doc has one `status`, and one way out of it.

```
proposed ──accepted──> in-progress ──landed──> folded ──> archive/
    │                       │
    └──rejected─────────────┴──superseded────> archive/
```

- `modules/` docs are always `status: current`. They are rewritten in place, never dated, never
  superseded.
- `work/` docs are `proposed`, `accepted`, `in-progress` or `landed`. When the change lands, the
  durable facts move into `modules/` and the file moves to `archive/` with `folded_into:`.
- `archive/` docs are `folded`, `rejected` or `superseded`. Never edited — supersede instead.
- `open-questions/` docs are `open`. Closing one means deleting it and updating the module doc.

`verified:` is the last date someone checked a doc against the code. If the code in `code:` has
changed since, treat the doc as suspect and fix it while you are there.

## Naming

- `modules/<module>/<topic>.md` — no dates, no version words.
- `work/YYYY-MM-<slug>.md` and `archive/YYYY-MM-<slug>.md` — dated by when the work opened.
- `open-questions/<slug>.md` — no dates.

## Where this came from

The convention, and most of the architecture it documents, is inherited from `GSBC.ImpactKids`.
Where that repo's `docs/modules/` describes something this repo reuses — the object store, the
generated passwords and persistent volumes — read it there rather than copying it here, and record
only the difference.
