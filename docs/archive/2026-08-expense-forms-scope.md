---
title: Debit card purchase and expense reimbursement forms
kind: plan
status: folded
opened: 2026-08-31
closed: 2026-08-31
verified: 2026-08-31
folded_into:
  - docs/modules/expenses/submission-model.md
  - docs/modules/expenses/attachments.md
  - docs/modules/frontend/theme.md
  - docs/examples/README.md
code:
  - Good Shepherd Baptist Church Debit Card Purchase Form.docx
  - Good Shepherd Baptist Church Expense Reimbursement Form.docx
  - mockups/debit-card-purchase-form.html
---

# Debit card purchase and expense reimbursement forms

> **Archived.** All eleven slices landed on 2026-08-31. The durable facts are in the `modules/` docs
> listed in `folded_into:` above; read those for how the app works now. What is kept below is the
> reasoning and the intent — including the places where building it proved this document wrong.
>
> **Where this document is wrong, and it matters:** it says the six compliance questions are "five of
> them word-for-word identical" and the five declarations "4 shared". Reading the `.docx` files found
> **two of six** and **one of five**, with declaration 3 being a *different* declaration on each form
> rather than an extra on one. It says the forms "differ in about fifteen fields"; the real count is
> **19**. Sections 2, 3 and 5, all called identical, differ too. The one-aggregate decision survives all
> of that — the structure really is shared — but the *wording* is per-kind almost everywhere, which is
> why `ExpenseFormWording` holds two copies of every string and never one.
>
> It also asked for one thing to be verified rather than assumed: whether SeaweedFS still stores
> `aws-chunked` framing verbatim. **It does**, on 3.98, measured on this stack. See
> `modules/expenses/attachments.md`.

The scope for GSBC.Accounting's first two pages: web versions of the two paper forms finance
currently circulates as `.docx`. **This doc is the handover — it is written to be executed by
someone who has not been part of the conversation that produced it.** Read it before writing any
code here.

The architecture is lifted from `GSBC.ImpactKids` (Aspire + code-first gRPC + Blazor WASM behind a
YARP BFF). The mechanics of building a vertical slice in that shape are in this repo's `AGENTS.md`
files, which are copies of ImpactKids' own. This doc records only what is **different** or
**decided**.

## What this app is for

It captures two forms from ordinary church members and volunteers, and hands the result to a human.
**The finance destination is Xero** — a submission here is not a ledger entry and must not pretend to
be one. This is the end-user-facing capture side: it collects the claim, its evidence and its
declarations, produces something a person can read and file, and stops there.

That framing decides two things that would otherwise look like gaps. There is no double-entry, no
GL posting, no chart of accounts. And there is no enforcement of the compliance rules the form
recites — the form's job is to *ask the questions and record the answers* so a human reviewer sees
them. Where this doc says "refused", it means the submission is incomplete, not that the app has
formed a view on whether an expense is legitimate.

A Xero export (bill or spend-money transaction, with the receipt attached) is the obvious next thing
after this scope. Nothing here should make that harder, which is most of the reason the submission is
stored as structured data rather than as a rendered document.

## What is being built

Two pages, both anonymous, both submit-only:

| Route | Form |
|---|---|
| `/forms/debit-card-purchase` | Church Debit Card Purchase Form |
| `/forms/expense-reimbursement` | Expense Reimbursement Form |

Plus a PDF rendering of a completed submission, and a development-only button that fills a form with
plausible mock data. Nothing else. No list page, no approval queue, no finance screen, no login.

The visual target is `mockups/debit-card-purchase-form.html`, which was built from the `.docx` and
approved as the shape to aim at. Open it before designing any screen here.

## The two forms are one form with two faces

This is the load-bearing observation, and it decides the whole data model. Both `.docx` files have
the same eight sections, the same five-item declaration block, the same missing-receipt declaration,
the same six compliance questions (five of them word-for-word identical), and the same seven-column
line-item table. **They differ in about fifteen fields.**

| Section | Debit card | Reimbursement |
|---|---|---|
| 1 — who | Cardholder, card last 4, transaction date + time, amount charged, bank reference | Claimant, phone/email, expense period (from → to), payment method, bank details on file |
| 2 — purpose | identical | identical |
| 3 — lines | first column is **Item** (one card transaction, itemised) | first column is **Date** (one line per receipt) |
| 3 — totals | Total card transaction → less personal → **net authorised church expense** | Subtotal of receipts → less non-reimbursable → **total reimbursement claimed** |
| 4 — checks | Q1 is parking/toll/fuel/taxi; detail table is **meal attendees** | Q1 is motor vehicle km; detail table is a **trip record** (from/to/km/rate) |
| 4 — Q2–Q6 | identical | identical |
| 5 — missing receipt | identical | identical, plus "not reimbursed from another source" |
| 6 — declarations | 5 items | 5 items, 4 shared; the fifth is the no-double-claim one |
| 7 — approval | identical, plus a "repayment required $" decision | identical, plus "returned for information" |
| 8 — finance | Transaction reference, statement date, personal repayment | Claim reference, payment date, payment reference |

**Model this as one aggregate with a `SubmissionKind` discriminator, not two parallel aggregates.**
Two aggregates means every shared section gets written, migrated, validated and rendered twice, and
the second copy drifts — the reimbursement form already carries a declaration the card form should
probably have and doesn't.

The two Razor pages stay separate. They are genuinely different screens with different wording; they
just submit the same contract.

## Domain model

One aggregate, five child collections. Contract records in
`Shared.Contracts/Entities/Features/Expenses/`, EF models `Db`-prefixed in `Grpc/Data/Models/`.

```
ExpenseSubmission                  the header: kind, status, submitter, purpose narrative,
                                   totals, the six compliance answers, the five declarations,
                                   the typed signature
  └ ExpenseLine                    section 3, ordered; gross, GST, church-use %, evidence flag
  └ ExpenseAttachment              an uploaded file; may point at a line or at the submission
  └ ExpenseAttendee                section 4 meals/hospitality table   (debit card)
  └ ExpenseTrip                    section 4 motor vehicle trip record (reimbursement)
  └ MissingReceiptDeclaration      section 5, 0..1, present only when a line is marked Missing
```

The kind-specific header fields are nullable columns on the aggregate, not a side table. There are
about fifteen of them and they are fixed by the paper forms.

Decisions inside that:

- **The six compliance answers are columns on the header, not a table.** There are exactly six and
  they are fixed by the form; a table would make "was Q4 answered" a join. Each is a `bool?` — null
  means *not answered*, which is a different fact from "No" and the one a reviewer needs to see.
- **Money is `decimal(12,2)`, never `double`.** Worth writing down because the mockup computes in
  JavaScript floats.
- **The server recomputes every total** — line sum, GST sum, less-personal, net. The client's numbers
  are a display convenience. A submission whose claimed total disagrees with its lines is refused with
  both figures named. This is about catching a broken client, not about judging the claim.
- **Dates cross the wire as UTC `DateTime` and land as `DateTimeOffset`**, per the contracts rule
  carried from ImpactKids. The trap that comes with it: a `DateTimeOffset` compared against in a query
  must have offset zero or Npgsql throws at execution, not at compile time.
- **Status is `Draft | Submitted | Approved | Declined | Paid`,** but only the first two are reachable
  in this scope. The rest exist so later work is additive rather than a migration of live rows.
- **Soft delete, not hard.** Seven-year ACNC retention. Nothing here hard-deletes a submission or an
  attachment; every query filters `!x.Deleted` by hand.

## Anonymous is the design, not a phase

There is no sign-in and nothing in this scope can enforce a rule that needs one. Every submission is
verified by a human before it goes anywhere. Build accordingly:

- **Sections 7 and 8 are captured in the model and rendered read-only and disabled on the page**,
  exactly as the mockup shows them, so the form still reads as the whole document to the person
  filling it in. They are filled in by a person, off-screen, for now.
- **No server-side drafts.** A draft belonging to nobody is either unrecoverable or enumerable by
  anyone with the URL. The page keeps in-progress state in `localStorage` and only reaches the server
  on submit. One less table, and one less way to leak a half-filled form containing bank details.
- **The submit and upload endpoints are open, so they need limits on day one**: per-IP rate limits on
  both, a cap on attachments per submission, a cap on total bytes per submission, and a content-type
  allow-list. Without them the object store is an anonymous file host. This is the one piece of
  hardening that is not optional.

Write the model as though auth exists — it costs nothing now and the BFF/YARP layer is already there
to hang it on later.

## Attachments

At least one itemised receipt is mandatory; it is the point of the form.

**Local development: this stack declares its own SeaweedFS container**, on its own port, with its own
volume, so the two stacks run side by side without interfering. Both are
`ContainerLifetime.Persistent`. ImpactKids holds 60535 (redis), 60536 (postgres), 60537 (S3), 63001
(rabbit management) and 7263 (DCP proxy); pick different numbers here and record them in
`.claude/app-local.md`.

**Production: one SeaweedFS instance, two buckets** — ImpactKids' existing `photos`, and `accounting`
for this app. That means the production config differs from local only in the endpoint it points at,
and the deployment work is a bucket plus a credential, not a second object store. Two follow-ons in
the cluster, both outside this repo:

- The Backblaze backup identity is bucket-scoped (`Read:photos`, `List:photos`) and needs `accounting`
  added, plus a second `rclone copy` in the CronJob.
- SeaweedFS reads its identities **once at startup**. A changed Secret updates nothing until the pod
  restarts, with no error either way — so the identity change must be followed by a rollout restart.

**Do not cargo-cult ImpactKids' photo store.** It was designed for 30 KB JPEGs of children's faces
and most of its choices do not transfer. Design the attachment store for what this app actually
stores — 1–20 MB PDFs and phone photos of receipts — and let the following be the only things carried
across, each because it is independently justified here:

- **Magic-byte validation on read and on write.** Not because ImpactKids does it, but because a
  receipt that is silently not the file it claims to be is discovered by an auditor in year six. Check
  the bytes against the declared content type and refuse the mismatch: JPEG `FF D8 FF`, PNG
  `89 50 4E 47`, PDF `25 50 44 46`, HEIC (`ftyp` box at offset 4, brand `heic`/`heix`/`mif1`).
- **A broken store answers 500, not 404.** 404 means "no such attachment"; a broken store must not
  hide behind it.
- **Serve as `Content-Disposition: attachment` with `X-Content-Type-Options: nosniff`.** Serving
  user-supplied PDFs same-origin is a content-injection shape that serving your own re-encoded JPEGs
  is not.

One warning to *verify* rather than inherit: ImpactKids found that with payload signing on, the AWS
SDK's default `aws-chunked` framing was **stored verbatim** by SeaweedFS 3.98 — right size, right
content type, valid database row, and the object was not the file, with no error anywhere. They fixed
it with `UseChunkEncoding = false`. Check whether that still reproduces on whatever SeaweedFS version
this stack runs before deciding how to write. The magic-byte check above is what tells you; a
round-trip test of a PDF and a phone photo in slice 3 is where you find out.

Beyond that, size the design for this app: check `Content-Length` before buffering a body, stream
reads rather than materialising them into a `byte[]`, key objects with enough hash to make a
collision between two different receipts implausible, and store the real metadata a receipt needs —
original filename, content type, byte size, hash, uploaded-at. ImpactKids stores none of that because
a photo does not need it.

**Uploads are a plain HTTP endpoint, not gRPC.** ImpactKids posts a raw body to a minimal API under
`/api/…` and lets YARP forward it; the gRPC channel never carries file bytes. Do the same. Note that
Blazor WASM cannot stream a request body — the browser materialises it — so the whole file sits in the
WASM heap during upload regardless of what the server does. That is the practical client-side ceiling
and it arrives well before the server's.

**Two-phase, because the page is anonymous.** The browser POSTs the form to get a submission id in
`Draft`, uploads each file against that id, then POSTs the submit. An attachment endpoint that accepts
files with no submission id is an open write endpoint to the object store.

Sizing is this stack's own: ImpactKids' 1 GB volume ceiling was set for a decade of JPEGs and is not
a constraint here. Size the container and PVC for multi-megabyte PDFs from the start.

## PDF output

**In scope, and build it early** — the accountant has not chosen the final output format, and a
rendered example is what that conversation needs.

**QuestPDF, rendering server-side from the submission model.** MIT-licensed for organisations under
$1M revenue, which the church is. Deterministic, no external process, no headless browser.

- Render from the aggregate, not from the HTML page. The screen layout and the printed layout are
  different problems and should be allowed to diverge.
- **Append the attachments as pages** so the form and its evidence are one file. This is the main
  reason to generate rather than fill: the Word form cannot do it, and the seven-year retention
  obligation is much easier to meet with one artefact than with a form plus a folder. Images embed
  directly; PDF receipts need page-level merging, so if that turns out to be fiddly, ship an
  attachment *manifest* page (filename, type, size, hash) in the first pass and merge later.
- Sections 7 and 8 render as **empty ruled blocks** for wet-signing, matching the paper form.
- Expose it as `GET /api/submissions/{id}/pdf`. Same anonymous exposure question as everything else
  — the id is the only credential, so it must be a `Guid`, never sequential.

The alternative — filling the original `.docx` via OpenXML and converting with headless LibreOffice —
is the option to avoid. OpenXML cannot render, so it needs the external process, which is slow,
font-fragile and drifts in layout.

Deliverable for the requester conversation: **one PDF of a fully filled debit card submission**,
generated from mock data, checked in or handed over so it can be shown alongside the HTML mockup.

## Mock data

A development-only **"Fill with mock data"** button on both form pages. It exists so the forms can be
demonstrated and the PDF exercised without twenty minutes of typing.

- **Gate it on the environment**, the same way ImpactKids gates its dev sign-in — the control must not
  exist in a published build, not merely be hidden by CSS.
- **Each press produces a visibly different submission.** Two mock submissions side by side must be
  tellable apart at a glance, or the PDF renderer and the review flow can't be checked. Vary the
  supplier, the ministry, the line items, the amounts and the reference — a small set of realistic
  scenarios (hardware for a youth shed, catering for a men's breakfast, fuel and parking for a camp
  run) picked at random, with randomised references and dates, beats one fixture.
- Include at least one scenario that exercises the awkward paths: a line marked **Missing** so section
  5 unlocks, a **Yes** on a compliance question so a reveal opens, and a non-zero personal portion so
  the net differs from the gross.
- The mock data must **not** be indistinguishable from a real submission in the database. Prefix the
  reference or tag the row so a real submission is never confused with one.

## Slice plan

Vertical slices per the inherited rule — contract → DB model → converter → service interface →
implementation → DI → frontend, one operation at a time, each seen working in the running app before
the next starts.

| # | Slice | Done when |
|---|---|---|
| 0 | Solution scaffold: six projects, ServiceDefaults, AppHost (own Postgres + SeaweedFS on free ports), YARP, empty WASM shell, migrations worker | the YARP proxy port serves an empty themed app, alongside a running ImpactKids stack |
| 1 | `ExpenseSubmission` + `ExpenseLine` contract, model, migration, `Create` | a hard-coded submission lands in Postgres, read back with `psql` |
| 2 | Debit card page, sections 1–3, posting slice 1's contract | the real form on screen writes a real row |
| 3 | Attachment store, upload and download endpoints, size/type/magic-byte gates | a PDF and a phone photo both round-trip; a renamed `.exe` is refused; the chunk-encoding question above is answered |
| 4 | Attachments on the page: drop zone, per-file evidence type, delete | mockup parity for the attachments card |
| 5 | Sections 4–6: compliance reveals, attendee table, declarations, typed signature | every conditional path renders and persists |
| 6 | Mock-data button, environment-gated, several scenarios | one press fills the form; two presses give visibly different submissions |
| 7 | Server-side validation and submit: totals recomputed, lines vs charge reconciled, error list | a mismatched submission is refused with both figures named |
| 8 | **QuestPDF renderer** + `GET /api/submissions/{id}/pdf` | a filled mock submission produces a PDF worth showing the accountant |
| 9 | Reimbursement page: same contract, trip record, kind-specific wording | both forms submit |
| 10 | Rate limits, body-size limits, `nosniff` + `Content-Disposition` | verified by hand against the running app |

Slices 0–3 carry the risk; everything after is additive. Slice 6 lands before 8 deliberately — the PDF
needs something to render, and hand-typing a full submission for every iteration is the thing that
makes people stop iterating.

## Still open

- **Output format** — parked with the accountant. Slice 8 exists to give that conversation something
  concrete; nothing else waits on it.
- **Xero export** — the likely next scope after this one. Not designed here, but the reason the
  submission is stored structured.
- **Cluster follow-ons** for the shared production SeaweedFS: the `accounting` bucket on the backup
  identity, the second `rclone copy`, and the rollout restart that makes an identity change take
  effect. Outside this repo, but they belong to whoever deploys this.
