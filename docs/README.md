---
title: Documentation map
kind: reference
status: current
verified: 2026-08-31
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
    one aggregate, what that does *not* let you share, and the money and retention rules.
  - [The attachment store](modules/expenses/attachments.md) — the SeaweedFS chunk-encoding trap that
    silently stores the wrong bytes, the magic-byte checks, and why the anonymous upload needs ceilings.

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

- [Debit card purchase and expense reimbursement forms](work/2026-08-expense-forms-scope.md) —
  `accepted`, and written as a handover for whoever builds it. Why the two paper forms are one
  aggregate, why anonymous is the design rather than a phase, the attachment store and which of
  ImpactKids' object-store lessons actually transfer, the QuestPDF output, and the slice order. Read
  it before writing any code here.

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
