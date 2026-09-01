---
title: What is outstanding
kind: handover
status: accepted
module: expenses
opened: 2026-08-31
verified: 2026-09-01
code:
  - GSBC.Accounting.Grpc/Features/Pdf
  - GSBC.Accounting.Grpc/Extensions/RateLimiting.cs
  - GSBC.Accounting.WASM/Features/Expenses
---

# What is outstanding

Everything in the [expense forms scope](../archive/2026-08-expense-forms-scope.md) is built and was
seen working. This is the short list of what is *not* done, so nobody has to reconstruct it by reading
eleven commits.

Ordered by what would hurt most to discover late.

## Decisions someone else has to make

**The output format is still parked with the accountant.** That was always the plan; slice 8 exists to
give the conversation something concrete. The two rendered PDFs are in [`docs/examples/`](../examples/README.md).
The specific question to put to them: **is the evidence manifest enough, or do the receipts have to be
bound into the same file?** QuestPDF cannot merge PDF pages, so binding them in needs a separate merge
step — images could embed today, PDFs cannot.

**Two declarations exist on one form and not the other, and closing that gap is finance's call.**
Debit card declaration 3 is the *repayment* declaration; reimbursement declaration 3 is the
*no-double-claim* one. Neither form carries the other's. This build reproduces each form as printed
rather than quietly harmonising them, which is the right default for a compliance document — but
somebody should decide whether both forms want both.

Merging the two pages into one on 2026-09-01 sharpened this rather than settling it. A claimant can now
change their answer to the form's first question, and D3 is the clearest reason the page clears all five
declarations when they do: the declaration they agreed to on the way in does not exist on the form they
are leaving on. If finance decides both forms want both declarations, that clearing gets cheaper to
justify, not harder — but it is still their call, not the build's.

## Out-of-repo work, for whoever deploys this

**Mostly done as of 2026-09-01.** The Dockerfiles, the Helm chart, the three GitHub Actions workflows
and the `gsbc.argo` manifests all exist, and how it fits together is documented in
[How this deploys](../modules/infrastructure/deployment.md). The deployment lands on
`expenses.baptist.com.au`, in its own `accounting` namespace, with **its own SeaweedFS** — the earlier
plan of one shared instance with two buckets was rejected that day.

What is genuinely still outstanding:

- **The six `kubectl create secret` commands have not been run**, and the exact commands are in
  `gsbc.argo/clusters/mini/README.md` under "accounting". `sql-secrets` in particular must exist
  *before* the first sync: Postgres applies `POSTGRES_PASSWORD` only when initialising an empty data
  directory.
- **The repository needs `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` and `ARGO_REPO_TOKEN`**, and
  `gh-pages` served by GitHub Pages. Until the first push to `master` publishes a chart, Argo reports
  "chart not found" — expected, not a fault.
- **The offsite backup is off**, because it needs a Backblaze bucket and an application key that do not
  exist yet. Its own bucket and its own key: adding `Read:accounting` to ImpactKids' backup identity
  would do nothing, since that identity lives in the other SeaweedFS. Until this is on, the receipts —
  the one thing here that cannot be regenerated — exist only on one PVC.
- **Nothing has been deployed or verified in the cluster yet.** Everything above is built and templated,
  not observed running.

## Known gaps in the app

**Rate limiting is a speed bump, not a defence.** Partitioning by remote IP means a shared NAT puts a
whole office in one bucket, and it trusts the ingress to set `X-Forwarded-For`. Real protection needs
authentication or something in front. See [attachments.md](../modules/expenses/attachments.md).

**A draft still lives on one browser on one device** — but it is now a server-side draft owned by a
`__gsbc_anon` session cookie rather than a `localStorage` copy, so a claimant can list, resume and
discard their own drafts, and drafts are no longer readable by anyone holding a submission id. See
[drafts.md](../modules/expenses/drafts.md), which supersedes what this section used to say.

The honest cost is unchanged and still worth saying out loud to whoever writes the claimant
instructions: clearing cookies, switching to a phone or using a private window loses them, and an
unedited draft is deleted after 90 days.

**No approval queue and no finance screen.** A claimant can read their own unsubmitted drafts back, and
nothing else can be read back. Sections 7 and 8 are captured in the model and rendered locked, so the
work that fills them in is additive. A reviewer still needs `psql` or the PDF link, and somebody still
has to hand them the submission id — that link deliberately keeps working for a **submitted** claim
without a cookie, because it is the only review path there is.

**Attachment objects for purged and discarded drafts stay in the object store.** Rows are soft-deleted;
bytes are not touched, because destroying uploaded files is a decision nobody has taken. The daily
purge logs the reclaimable byte count. If that number grows enough to matter, reclaiming it is a
deliberate piece of work, not a tidy-up.

**There is no test project.** Until there is, "seen working in the running app" is the only gate, which
is what `AGENTS.md` says. The arithmetic in `ExpenseTotals` and the signature detection in
`FileSignature` are the two places unit tests would pay for themselves fastest — both are pure
functions with awkward edges (banker's rounding, HEIC brands at offset 8).

## The next scope

**Xero export** — a bill or spend-money transaction with the receipt attached. It is the reason a
submission is stored as structured data rather than as a rendered document, and nothing in this build
should make it harder.

## Things that were checked and are fine

Recorded so nobody re-investigates them:

- **The SeaweedFS `aws-chunked` failure still reproduces** on 3.98. `UseChunkEncoding` is `false` and
  the read-side magic-byte check is what would catch a regression. Do not "tidy up" either.
- **`dotnet build` while the stack is running leaves the Blazor dev server serving a stale asset
  manifest**, and the symptom is the boot spinner stuck at 0% with no error. Stop the AppHost first.
  `execute_run_configuration` on a running AppHost does *not* restart it. See
  [`.claude/app-local.md`](../../.claude/app-local.md).
- **Both forms' wording was read out of the `.docx` files**, not paraphrased. The scope doc
  significantly understated how much the two differ; the measured numbers are in
  [paper-form-fields.md](../modules/expenses/paper-form-fields.md).
