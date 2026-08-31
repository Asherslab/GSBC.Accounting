---
title: The expense submission model
kind: reference
status: current
module: expenses
verified: 2026-09-01
code:
  - GSBC.Accounting.Shared.Contracts/Entities/Features/Expenses
  - GSBC.Accounting.Grpc/Data/Models/Expenses
  - GSBC.Accounting.Grpc/Data/AccountingDbContext.ExpensesModel.cs
  - GSBC.Accounting.Grpc/Features/Expenses
---

# The expense submission model

Read this before adding a field, a question or a declaration. It says why the two paper forms are one
type, what that does and does not let you share between them, and the two rules that exist because this
is a financial record.

## One aggregate, two kinds

`ExpenseSubmission` carries a `SubmissionKind` discriminator — `DebitCardPurchase` or
`ExpenseReimbursement` — and the 19 fields that exist on only one of the two forms are nullable columns
on it. Ten are debit-card-only, nine reimbursement-only.

Two aggregates would mean every shared section is written, migrated, validated and rendered twice, and
the second copy drifts.

**But sharing the structure is not sharing the words.** Measured against the `.docx` files:

| | Identical between the two forms |
|---|---|
| Compliance questions (§4) | **2 of 6** |
| Declarations (§6) | **1 of 5** |
| Missing-receipt declaration (§5) | closing GST sentence only |
| Line-table headers (§3) | 4 of 7 |
| Finance checklist (§8) | 4 of 6 |

So the *answers* live in shared columns and every question, declaration and label is a per-kind string
in the UI and the PDF. A form that does not say what the paper form says is, on a compliance document,
the failure that matters. Never hoist a question's text into a shared constant because two forms happen
to ask something similar.

Section 3's first column is the sharpest case: it is a **type** difference, not a label difference. The
debit card form prints `Item` (text — one card transaction itemised into its parts); the reimbursement
form prints `Date` (one row per receipt). `ExpenseLine` therefore carries both `ItemDescription` and
`LineDate`, both nullable, and `Create` requires whichever one the kind implies.

## The compliance answers and declarations are columns, the questions are not

Six `bool?` columns for section 4 and five for section 6, on the header rather than in a table. There
are exactly six and five, fixed by the paper forms; a table would make "was question 4 answered" a join.

**`null` means *not answered*, which is a different fact from "No"** — and it is the one a reviewer
needs to see. The page therefore renders a No/Yes radio pair, never a checkbox: an unticked checkbox
cannot be told apart from an unanswered question.

Nothing here enforces the rules the questions recite. The form asks them and records what was said.

The *text* of every question and declaration lives in `ExpenseFormWording`, keyed by kind. Where the two
forms genuinely agree the same literal appears twice, deliberately, so changing one never changes the
other by accident.

Section 4's Yes reveals a detail table, and which table depends on the kind because question 1 is a
different question on each form: `ExpenseAttendee` (date, person, relationship, amount, private share,
reason) for the debit card form's meals and hospitality, `ExpenseTrip` (date, from, to, business km,
approved rate, purpose) for the reimbursement form's motor vehicle record. `ApprovedRate` is *recorded,
not applied* — this app holds no ATO rate table and checks nothing against one.

Section 5 (`MissingReceiptDeclaration`, 0..1) exists only when some line is marked `Missing`, matching
the form's own "complete only when evidence is unavailable". `Create` writes it only when a line
actually says Missing: a declaration attached to a submission with full evidence would read to a
reviewer as a statement somebody made, and nobody made it.

## Sections 7 and 8 are captured but never filled in here

The columns exist on the aggregate and the page renders both sections read-only and disabled, exactly
as the mockup shows them, so the form still reads as the whole document to the person filling it in.
Nothing in this scope can complete them: both pages are anonymous, and "the approver must not be the
claimant" is a compliance constraint that needs an identity to enforce.

## The server computes the totals

`ExpenseTotals` is the only place a total is produced. `Create` discards whatever the client sent for
`GrossTotal`, `GstTotal` and `NetTotal` and writes its own. The client's numbers are a display
convenience — and the approved mockup computes them in JavaScript floats, so they can genuinely differ
in the last cent.

`LessPersonalAmount` is the exception: only the claimant knows what part of a purchase was personal, so
it is taken as given.

`AmountCharged` (debit card only) is what the claimant says the card was charged, and is deliberately
*not* the same field as `GrossTotal`, which is the sum of the lines. Reconciling those two is the point
of the debit card form.

Rounding is `MidpointRounding.ToEven` at every step, not only at the end: the column is
`decimal(12,2)`, so a sum carrying more scale is silently truncated on write and the stored total then
disagrees with the stored lines.

## Two rules that are not style preferences

**Money is `decimal(12,2)` and the precision is configured explicitly.** An unconfigured `decimal` maps
to bare `numeric`, which stores whatever scale it is handed — which is how a client that computed in
floats gets its rounding error into a financial record instead of being stopped at the column.
`ChurchUsePercent` is `decimal(5,2)`.

**Nothing hard-deletes.** ACNC retention is seven years. Both entities carry `Deleted` behind a global
`HasQueryFilter`, and the submission→lines relationship is `DeleteBehavior.Restrict` so a cascade cannot
take the evidence with it. `IgnoreQueryFilters()` is the only way to see deleted rows.

## What the model deliberately does not hold

- **More of a card number than four digits.** The form prints "Never record the full card number, PIN or
  security code on this form". `CardLastFourDigits` is `HasMaxLength(4)` and validated as four digits, so
  the column could not hold more even if something tried.
- **Bank account details.** `BankDetailsOnFile` is a `bool?` and that is the whole banking data model.
  The paper form collects no BSB and no account number, and says so: "Do not email bank details in an
  unsecured message." Adding an account-number field would be a new class of data at rest, not an
  implementation detail.

## Identity, status and dates

The id is a `Guid` generated on insert, never a sequence. `Create` passes `Guid.Empty` and EF's
`ValueGeneratedOnAdd` replaces it.

**The id is no longer sufficient authority on its own.** `OwnerSessionId` records the browser session
that created the submission, and every read and write of a draft filters on it — see
[drafts.md](drafts.md). The id still carries weight for one case: a **submitted** claim's PDF and
attachments render for anyone holding it, because that is the only review path this scope has. A
guessable id would make every claim in the church readable by counting, which is why it is a `Guid`.

`UpdatedAt` is the last write of any kind, autosaves included. It is what the drafts list sorts on and
what the abandoned-draft purge counts 90 days from, so `Update` has to bump it — counting from
`CreatedAt` would delete a draft somebody was still working on.

`SubmissionStatus` is `Draft | Submitted | Approved | Declined | Paid`. Only the first two are reachable
in this scope; the rest exist so the approval work is additive rather than a migration of live rows.

Contracts carry UTC `DateTime` (protobuf-net has no `DateTimeOffset` surrogate); the database carries
`DateTimeOffset` / `timestamptz`. `DateTimeConverter` bridges them on read, and `Create.ToOffset` on
write. **`DateTime.SpecifyKind(…, Utc)` in `ToOffset` is load-bearing**: a `DateTime` deserialised from
protobuf has `Kind.Unspecified`, and building a `DateTimeOffset` from one applies the machine's local
offset — so a value written in Brisbane lands ten hours out, and any later query comparison throws
`only offset 0 (UTC) is supported` at execution rather than at compile time.

## Create is phase one of two

`Create` writes a `Draft` and returns its id. The browser then uploads each receipt against that id and
submits. The phases exist because the page is anonymous: an upload endpoint that accepted files with no
submission id would be an open write endpoint to the object store.

So `Create` validates only what must hold for the row to be coherent — the arithmetic, and the card
number. Completeness rules (a receipt attached, declarations ticked, lines reconciling against the
amount charged) belong to submit: a draft is allowed to be half-finished.

`BasicResponse` and `BasicReadResponse<T>` carry an `Errors` **list**, not one string. A form that
reveals its problems one at a time is the one people give up on.

## Update exists because the draft is created early

The draft appears as soon as the claimant has typed enough for `Create` to accept — long before they
have finished. Without `Update`, everything typed afterwards never reaches the server, and submit then
checks a row that no longer matches the screen. That was live on 2026-08-31: correcting the amount
charged on the page left the stored row at the old figure, and the reconciliation error could not be
cleared.

So `EnsureSubmissionAsync` creates on first call and **updates on every call after**. Re-sending the
whole form is safe because attachments are keyed to the submission *id*, not to its contents.

The form pages call it on a 2-second debounce as the claimant types, as well as on Save and Submit —
see [drafts.md](drafts.md).

`Update` replaces the children rather than merging them: a deleted line has to disappear, and matching
rows by position across an edit that inserted one in the middle is a way to silently move somebody's
money between lines. The superseded rows are soft-deleted like everything else.

## Submit is where a draft stops being allowed to be half-finished

`Create` checks only what must hold for the row to be coherent. Every completeness rule is in `Submit`,
because somebody filling in a long form needs to save it and come back.

`Submit` recomputes the totals from the **stored** lines and checks the **stored** submission — never a
re-sent form, which would let a client submit something different from what it attached receipts to. It
returns every problem at once.

The headline check is the reconciliation, and it **names both figures**: "The itemised lines total
$156.25 but section 1 says the card was charged $99.00." Anything less sends somebody hunting through a
table for a number the server already knows. Only the debit card form has it — it is the one whose total
is stated twice, once by the bank and once by the claimant's itemisation.

Writes use `db.Entry(x).Property(...).IsModified` rather than `db.Update`, which writes every column and
would silently revert anything another writer committed since the read.
