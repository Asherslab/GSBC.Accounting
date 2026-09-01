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
  - GSBC.Accounting.WASM/Features/Expenses
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
the second copy drifts. There is one page too, for the same reason and with the same evidence behind it:
until 2026-09-01 there were two, they were 90% the same file, and the parts that had drifted were the
comments explaining why the code looked like that.

**The kind is an answer, not a route.** Section 0 of the form asks how the expense was paid for — "I paid
for it myself" or "I used a church debit card" — and everything below it renders in that document's own
words. `ExpenseFormModel.Kind` is therefore `SubmissionKind?` and settable, and nothing below section 0
renders while it is null: `DebitCardPurchase` is the enum's zero value, so a non-nullable kind would make
an unanswered form silently claim to be a card purchase, and there is no correct wording to show before
the answer.

**But sharing the structure is not sharing the words.** Measured against the `.docx` files:

| | Identical between the two forms |
|---|---|
| Compliance questions (§4) | **2 of 6** |
| Declarations (§6) | **1 of 5** |
| Missing-receipt declaration (§5) | closing GST sentence only |
| Line-table headers (§3) | 4 of 7 — *and the app no longer prints either set; see below* |
| Finance checklist (§8) | 4 of 6 |

So the *answers* live in shared columns and every question, declaration and label is a per-kind string
in the UI and the PDF. A form that does not say what the paper form says is, on a compliance document,
the failure that matters. Never hoist a question's text into a shared constant because two forms happen
to ask something similar.

Section 3 used to be the sharpest case, and it is now the exception that proves the rule: it is the one
section the app has **deliberately stopped copying from the paper form**, so it is identically shaped on
both kinds. See [Section 3 is per receipt, not per line](#section-3-is-per-receipt-not-per-line).

Section 2's first slot is the trap in the other direction: one column, `PurposeActivity`, printed under
two labels — `Ministry / department` on the debit card form and `Purpose / activity` on the
reimbursement one. On a card form that reads oddly beside section 1's field of the same name, and the
old debit card page "fixed" it by dropping the section 2 field and pointing the label at
`MinistryDepartment` instead. The result was that **`PurposeActivity` was null on every card
submission** — a blank row in the PDF (`SubmissionDocument.cs:195`) and a missing subtitle in the drafts
list, neither of which errors. Where a label looks wrong, check
[paper-form-fields.md](paper-form-fields.md) before changing which column it points at.

## Section 3 is per receipt, not per line

**This is the one place the app deliberately departs from the `.docx` files.** Everywhere else the rule
is "print what the paper prints"; here the paper's shape was a consequence of being paper.

On the forms, section 3 is a grid you write across and the receipts are stapled to the back. Nothing on
the page records which row a receipt belongs to — a reviewer infers it from the amounts. A web form does
not have to work that way, and the whole point of building one was to stop it working that way.

So: **`ExpenseDetail` is one purchase, at one place, evidenced by its own attached files, and it is
created by attaching a receipt.** `ExpenseLine`, `EvidenceStatus` and `ChurchUsePercent` are gone.

Several files may hang off one detail — a long docket photographed in three parts, or a receipt plus the
bank line proving it was paid — but **two purchases are two details**, because the two questions below are
asked of a single receipt and have no answer for two. The dropzone takes a whole selection as *one*
detail for that reason: three photos of one docket is one purchase, and opening three panels asking three
times who the supplier was would be the form misunderstanding what somebody just did.

### The two questions, and the three modes they produce

Each detail asks whether the receipt includes anything that is not a church expense, and whether the
receipt itemises itself. Both are `bool?` — null is unanswered, not No — and together they derive
`ExpenseDetail.Itemisation`, which is **computed on both ends and never stored**:

| Personal items | Receipt itemises | Requirement | What the claimant types |
|---|---|---|---|
| No | Yes | `None` | A total and the GST. Nothing else — the evidence already lists it and all of it is the Church's. |
| Yes | Yes | `PersonalItemsOnly` | The personal lines only. The receipt shows the rest; re-typing it is transcription for its own sake. |
| either | No | `Everything` | Every line, each marked Church use or not. With no itemisation there is otherwise no record of what the money bought. |

Church use is a **yes/no per item**, not the paper form's percentage. "This $80 shop was 60% church" is a
real thing to say about a basket; it is not a real thing to say about a packet of paper plates, and a
claimant asked for a percentage on one invents a number. Where a purchase genuinely splits, it splits
into items.

Where the evidence does not itemise, **best effort is what is asked for and the sums are not required to
reconcile.** The page shows the difference between the items and the receipt total as a warning; nothing
refuses the submission over it. A form that will not submit until the cents come out right is a form that
gets a made-up line added to close the gap.

### The non-reimbursed amount is floored, never capped

`NonReimbursedAmount` is per detail, and it may not be **less** than that detail's itemised personal
items: whatever was listed as personal is not claimable. It may freely be **more** — somebody choosing to
carry part of a legitimate church cost is making a gift, and the form has no business refusing one.

The floor lives in three places, and each is right where it is:

| Where | What it does | Why there |
|---|---|---|
| The `min` on the input | Stops the spinner going below the floor | Somebody must not discover the rule at the end of the form |
| `ExpenseDetailModel.ClampNonReimbursed` | **Raises** to the floor after an item edit, never lowers | Deleting a personal line must not silently take back a gift |
| `Submit` | Refuses below the floor | A rule that only a client enforces is not enforced |

`Create` does **not** check it. A claimant halfway through typing the personal lines has itemised $12 of
an eventual $40, and a draft refused for that is a draft that goes unsaved while somebody is still working
on it. The *ceiling* — not claiming more than the receipt was for — is checked on create, because no
amount of further typing makes that coherent.

### `Key` is what an attachment points at

`ExpenseDetail.Key` is a `Guid` minted in the browser and written back unchanged by `WriteDetails`.
`DbExpenseAttachment.DetailKey` holds it. **Not the row id**: `Update` replaces a draft's details rather
than merging them, so every autosave gives each detail a fresh `Id` and a file holding one would come
unlinked seconds after upload. See [attachments.md](attachments.md).

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

Section 5 (`MissingReceiptDeclaration`, 0..1) exists only when some purchase has **no
`SupplierReceipt` among its files** — nothing from the place it was bought, only a bank line or a
screenshot. That matches the form's own "complete only when evidence is unavailable", read against
evidence the claimant has already classified.

**The trigger is the attachments, not a checkbox.** There used to be an `Attached / Missing` column and
the claimant answered it separately from labelling the file; two answers to the same question is how the
two come to disagree. `Update` evaluates the trigger and `Submit` enforces it. `Create` cannot: it is
phase one, the files go up against the id it returns, so there are no attachments to look at yet — it
carries a declaration through if one is sent so that somebody who filled it in before their first
autosave does not lose it.

## Sections 7 and 8 are captured but never filled in here

The columns exist on the aggregate and the page renders both sections read-only and disabled, exactly
as the mockup shows them, so the form still reads as the whole document to the person filling it in.
Nothing in this scope can complete them: the form is anonymous, and "the approver must not be the
claimant" is a compliance constraint that needs an identity to enforce.

## The server computes the totals

`ExpenseTotals.SumDetails` is the only place a total is produced. `Create` discards whatever the client
sent for `GrossTotal`, `GstTotal`, `LessPersonalAmount` and `NetTotal` and writes its own. The client's
numbers are a display convenience — the page computes the same figures so they move as somebody types.

**`LessPersonalAmount` used to be the exception and is not any more.** It was a single box at the foot of
section 3 that the claimant typed and the server took as given. Each purchase now states its own
`NonReimbursedAmount`, so the submission-level figure is their sum and nothing else — there is no second
number for the first to disagree with, and the request does not carry one.

`AmountCharged` (debit card only) is what the claimant says the card was charged, and is deliberately
*not* the same field as `GrossTotal`, which is the sum of the receipts. Reconciling those two is the
point of the debit card form.

Rounding is `MidpointRounding.ToEven` at every step, not only at the end: the columns are
`decimal(12,2)`, so a sum carrying more scale is silently truncated on write and the stored total then
disagrees with the stored details.

**Every field `ValidateForSubmit` reads has to be in `Submit`'s projection of the stored details.** It
rebuilds `DbExpenseDetail` rows into contract `ExpenseDetail`s and validates those, so a field left out
of the projection reads as absent however full the database is. That landed on the claimant as "every
purchase in section 3 needs the place it was bought" printed over a form that plainly said Woolworths —
observed on 2026-09-01, when `Supplier`, `PurchaseDate` and `Purpose` were all missing from it.

## Two rules that are not style preferences

**Money is `decimal(12,2)` and the precision is configured explicitly.** An unconfigured `decimal` maps
to bare `numeric`, which stores whatever scale it is handed — which is how a client that computed in
floats gets its rounding error into a financial record instead of being stopped at the column. There is
no percentage column left to configure — church use is a per-item `bool`.

**Nothing hard-deletes.** ACNC retention is seven years. Every entity carries `Deleted` behind a global
`HasQueryFilter`, and submission→details, detail→items and submission→attachments are all
`DeleteBehavior.Restrict` so a cascade cannot take the evidence with it. `IgnoreQueryFilters()` is the
only way to see deleted rows.

The soft-delete filter matters more than it used to: `Update` soft-deletes and re-adds every detail and
item on **every autosave**, so an unfiltered count of a draft's details climbs by the size of section 3
every two seconds. `ListDrafts` counts through the filtered sets for that reason.

## What the model deliberately does not hold

- **More of a card number than four digits.** The form prints "Never record the full card number, PIN or
  security code on this form". `CardLastFourDigits` is `HasMaxLength(4)`, and `Create` refuses anything
  that is not digits or is longer than four, so the column could not hold more even if something tried.
  A *draft* may hold fewer than four — "12" is where "1234" passes through on the way in, and refusing it
  would refuse to save somebody's draft. `Submit` requires all four.
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

So `Create` validates only what must hold for the row to be coherent — the arithmetic, and that the card
field cannot be holding a card number. Completeness rules belong to submit: a draft is allowed to be
half-finished. That includes **having any purchases at all**, their supplier, date and purpose, their two
questions, and any itemisation those answers require.

The line the two validators draw through the money is the useful example. `Create` refuses a purchase
that says it is not claiming **more than the receipt was for** — no amount of further typing makes that
coherent. It permits one not claiming **less than its own itemised personal items**, which `Submit`
refuses, because a claimant halfway through typing those lines has itemised $12 of an eventual $40 and a
draft refused for that is a draft that goes unsaved while they are still working on it.

`BasicResponse` and `BasicReadResponse<T>` carry an `Errors` **list**, not one string. A form that
reveals its problems one at a time is the one people give up on.

## Update exists because the draft is created early

The draft appears on the first edit, long before the claimant has finished. Without `Update`,
everything typed afterwards never reaches the server, and submit then checks a row that no longer
matches the screen. That was live on 2026-08-31: correcting the amount
charged on the page left the stored row at the old figure, and the reconciliation error could not be
cleared.

So `EnsureSubmissionAsync` creates on first call and **updates on every call after**. Re-sending the
whole form is safe because attachments are keyed to the submission *id* and to the claimant's own
`ExpenseDetail.Key`, neither of which the form's contents can change.

The form page calls it on a 2-second debounce as the claimant types, as well as on Save and Submit —
see [drafts.md](drafts.md).

`Update` replaces the children rather than merging them: a deleted purchase has to disappear, and
matching rows by position across an edit that inserted one in the middle is a way to silently move
somebody's money between purchases. The superseded rows are soft-deleted like everything else.

**That replacement is why `ExpenseDetail.Key` exists**, and it is also why `Update` has to tidy up after
it: a file whose purchase has just gone is left on the claim with its `DetailKey` cleared rather than
deleted, because throwing away evidence needs a deliberate act with its own endpoint. The page does the
tidier thing when the claimant presses × on a panel — it detaches that purchase's files first — and both
are right for where they sit. The server cannot tell a deletion from a stale client; the × can.

### The kind can change while the submission is a draft

`Update` assigns `form.Kind` over the stored one. It was fixed at creation until 2026-09-01, which was
right when the kind came from the URL and wrong once it became the form's first question — people
mis-answer first questions, and the alternative is retyping the claim.

**What must not survive the change is the other form's content.** Three separate things drop it:

| What | Where | Why there |
|---|---|---|
| The six header fields of the kind being left | `Update.ClearFieldsForOtherKind` | The server is where a compliance rule finally lives; a client that sends them anyway is ignored |
| The attendee or trip rows | falls out of `Update`'s soft-delete-then-re-add | Only the table matching the *stored* kind is re-added |
| All six compliance answers and all five declarations | `ExpenseFormModel.SwitchKind` | The page is the only place that can warn the claimant first |

The declarations and questions clear because **four of the six questions and four of the five
declarations are different text on the two forms**. A tick carried across would record somebody as having
agreed to wording they were never shown, which is the failure this whole app exists to avoid. Q4, Q6 and
D4 are word-for-word identical and could in principle survive; clearing everything instead is a rule that
fits in one sentence on screen and cannot be got subtly wrong by a later edit to the wording.

Everything that means the same thing on both documents stays: the claimant, the ministry, section 2,
every purchase in section 3 and its receipts, the missing-receipt declaration and the signature. Section
3 is now the easiest of those to be sure about — a receipt has a supplier and a date whichever card paid
for it, so there is nothing in it that belongs to one kind.

The page asks before it does any of this, in an inline panel rather than a `confirm()` — a browser dialog
cannot say what is about to be lost and reads as an error. Nothing changes until it is accepted, so
"Keep this form" is genuinely free rather than an undo.

## Submit is where a draft stops being allowed to be half-finished

`Create` checks only what must hold for the row to be coherent. Every completeness rule is in `Submit`,
because somebody filling in a long form needs to save it and come back: a submitter name, the section 2
narrative, at least one purchase, and per purchase a file, a supplier, a date, a purpose, a total, both
questions answered and whatever itemisation those answers require; a missing-receipt declaration where
some purchase has nothing from the supplier; all six compliance answers, all five declarations, a
signature, and — on the debit card form — four card digits, an amount charged, and the reconciliation
below.

**What `Submit` does not check is the itemisation adding up.** Where the evidence does not itemise the
claimant was asked for best effort, and a form that will not submit until the cents reconcile is a form
that gets a made-up line added to close the gap. The page shows the difference as a warning; a reviewer
can ask.

`Submit` recomputes the totals from the **stored** details and checks the **stored** submission — never a
re-sent form, which would let a client submit something different from what it attached receipts to. It
returns every problem at once.

The headline check is the reconciliation, and it **names both figures**: "The receipts in section 3 total
$156.25 but section 1 says the card was charged $99.00." Anything less sends somebody hunting for a
number the server already knows. Only the debit card form has it — it is the one whose total is stated
twice, once by the bank and once by the claimant's own receipts.

Writes use `db.Entry(x).Property(...).IsModified` rather than `db.Update`, which writes every column and
would silently revert anything another writer committed since the read.
