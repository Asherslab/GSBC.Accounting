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

## Out-of-repo work, for whoever deploys this

The production object store is one SeaweedFS with two buckets, so the deployed config differs from
local only in the endpoint. Three things live in the cluster, not here:

- The **`accounting` bucket** has to exist and the app's credential has to reach it.
- The **Backblaze backup identity is bucket-scoped** (`Read:photos`, `List:photos`) and needs
  `accounting` added, plus a second `rclone copy` in the CronJob. Without it the receipts are not
  backed up, and nothing will say so.
- **SeaweedFS reads its identities once at startup.** A changed Secret updates nothing until the pod
  restarts, with no error either way — so an identity change must be followed by a rollout restart.

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
