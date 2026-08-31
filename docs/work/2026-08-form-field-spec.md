---
title: Field-by-field specification of the two paper forms
kind: plan
status: accepted
module: expenses
opened: 2026-08-31
verified: 2026-08-31
code:
  - Good Shepherd Baptist Church Debit Card Purchase Form.docx
  - Good Shepherd Baptist Church Expense Reimbursement Form.docx
---

# Field-by-field specification of the two paper forms

The ground truth under [2026-08-expense-forms-scope.md](2026-08-expense-forms-scope.md). Every label, question and
declaration below was read out of `word/document.xml` inside the two `.docx` files, not paraphrased from the scope
doc. Read this before writing a contract, a page or a validator; where it disagrees with the scope doc, this
document is the one that was checked against the files.

**The scope doc's central claim about the two forms is wrong in a way that changes the build.** It says the six
compliance questions are "five of them word-for-word identical" and that "Q2–Q6 identical". **Two of six are
identical.** It says the five declarations are "4 shared". **One of five is shared.** Sections 2, 3 and 5, which the
scope doc calls identical, all differ in wording. The one-aggregate decision still holds — the *structure* is
shared throughout — but **the wording is per-kind almost everywhere**, so every question and declaration must be a
per-kind string in the UI, not a shared constant. See [Where the scope doc is wrong](#where-the-scope-doc-is-wrong).

## How to read this

- Column **Type** uses the vocabulary the scope doc set: `string`, `decimal`, `date`, `bool?`, `enum`.
- Column **Req.** is *the form's own instruction*, not a guess. The paper forms carry **no required-field marking
  at all** — no asterisks, no shading, no Word validation. The only "must" statements printed anywhere are the two
  banner lines in the masthead and the section captions ("Complete all applicable fields", "Complete only when
  evidence is unavailable"). So `form` means the form says so in words, `implied` means the field is the point of
  its section and a submission without it is incomplete, `no` means the form is silent. Anything stricter is a
  product decision this document does not make.
- Column **Format / max** records only what the *printed form* constrains. It is nearly always nothing: a Word
  table cell has no length. Where a rule is stated in the form's own words it is quoted.

**There are no interactive controls in either file.** Every checkbox on both forms is the literal character
`☐` (U+2610 BALLOT BOX) typed into a run — 52 of them in the debit card form, 58 in the reimbursement form. There
is not a single `w:checkBox` legacy form field, `w14:checkbox` content control or `w:sdt` in either document. Both
files are print-and-pen documents, not fillable forms. That matters for slice 8: there is no field mapping to
preserve, and it removes the last argument for the OpenXML fill-and-convert route the scope doc already rejects.

Every place a `☐` appears is called out below as **checkbox** with its label.

## Masthead, header and footer

| Element | Debit card form | Reimbursement form |
|---|---|---|
| Running header | `GOOD SHEPHERD BAPTIST CHURCH  |  FINANCE` | identical |
| Title | `CHURCH DEBIT CARD PURCHASE FORM` | `EXPENSE REIMBURSEMENT FORM` |
| Subtitle | `For recording and substantiating every purchase made using a Church debit card` | `For expenses personally paid on behalf of Good Shepherd Baptist Church` |
| Banner | `Complete one form for each transaction. Attach the itemised receipt/tax invoice and submit promptly. A card terminal receipt or bank statement alone does not show what was purchased.` | `Attach itemised receipts/tax invoices. Submit promptly and do not approve your own claim. A card receipt or bank statement alone does not show what was purchased.` |
| Page 2 title | `CHURCH DEBIT CARD PURCHASE FORM` | `EXPENSE REIMBURSEMENT FORM` |
| Page 2 subtitle | `Page 2 - special purchase details, declarations and exceptions` | `Page 2 – declarations, exceptions and approval` |
| Page 3 title | `APPROVAL & FINANCE` | identical |
| Page 3 subtitle | `Independent authorisation and card reconciliation` | `Independent authorisation and payment processing` |
| Footer | `Debit Card Purchase Form  |  Version 1.0 – August 2026  |  Retain with supporting records for at least 7 years` | `Expense Reimbursement Form  |  Version 1.0 – August 2026  |  Retain with supporting records for at least 7 years` |

The banner is the source of the "one card transaction per form" rule and of "do not approve your own claim". Both
are compliance statements a claimant reads on screen, so both belong on the page, verbatim, not summarised.

The debit-card banner constrains the aggregate: **one debit card form is one card transaction**, itemised across
section 3 lines. The reimbursement form has no such rule — its section 3 caption says one line per receipt.

## Section 1 — header fields

Section captions differ:

- Debit card: `1. CARDHOLDER AND TRANSACTION DETAILS` / `Complete all applicable fields`
- Reimbursement: `1. CLAIMANT AND PAYMENT DETAILS` / `Complete all applicable fields`

### Debit card form

| Label (verbatim) | Type | Req. | Format / max implied by the form |
|---|---|---|---|
| `Cardholder name` | string | implied | none |
| `Form date` | date | implied | printed as `/        /` — a day/month/year rule box |
| `Role / relationship` | enum | implied | six checkboxes, below |
| `Card last 4 digits` | string | implied | **exactly 4 digits** — see the card-security line below |
| `Ministry / department` | string | implied | none |
| `Transaction date` | date | implied | `/        /` |
| `Time:` | string | no | printed as `Time: ________` on the same cell as the transaction date; free text, not a picker |
| `Supplier / merchant` | string | implied | none |
| `Amount charged / bank reference` | — | — | one cell holding two fields, split below |
| ↳ `$____________` | decimal | implied | money, 2dp |
| ↳ `Ref: __________________` | string | no | none |

`Role / relationship` is six **checkbox**es in one cell, verbatim:
`☐ Employee`, `☐ Volunteer`, `☐ Pastor`, `☐ Responsible Person`, `☐ Other:`
— the last followed by a free-text run-on for the other value.

Immediately under the table, verbatim, and it is a hard constraint on the model as well as on the page:

> Card security: Record only the last four digits. Never record the full card number, PIN or security code on this
> form.

So `CardLastFourDigits` is `string?` of length 4, validated as four digits, and nothing in the app — contract, DB
column, log line or PDF — ever holds more of the card number than that.

### Reimbursement form

| Label (verbatim) | Type | Req. | Format / max implied by the form |
|---|---|---|---|
| `Claimant name` | string | implied | none |
| `Claim date` | date | implied | `/        /` |
| `Role / relationship` | enum | implied | same six checkboxes as the debit card form, verbatim identical |
| `Phone / email` | string | implied | none |
| `Ministry / department` | string | implied | none |
| `Expense period` | date ×2 | implied | printed `/        /   to          /        /` — a from and a to |
| `Payment method` | enum | implied | `☐ EFT`   `☐ Other:` with free text after `Other:` |
| `Bank details on file` | bool? | implied | `☐ Yes`   `☐ No – provide securely` |

Under the table, verbatim:

> Banking privacy: Do not email bank details in an unsecured message. Use the church-approved secure method.

The form deliberately **does not collect a BSB or account number**. Neither should the app. `Bank details on file`
is the whole of the banking data model: a `bool?`, plus the instruction that a "No" is handled off-channel. Adding
an account-number field would be a new decision and a new class of data at rest, not an implementation detail.

## Section 2 — purpose and authorisation

Section captions:

- Debit card: `2. CHURCH PURPOSE AND AUTHORISATION`
- Reimbursement: `2. BUSINESS PURPOSE AND AUTHORISATION`

**The scope doc calls section 2 "identical". It is not** — the caption differs, the first field's label differs,
and the narrative prompt differs.

| Slot | Debit card label | Reimbursement label | Type | Req. |
|---|---|---|---|---|
| 1 | `Ministry / department` | `Purpose / activity` | string | implied |
| 2 | `Event / project` | `Event / project` | string | no |
| 3 | `Prior approval by` | `Prior approval by` | string | no |
| 4 | `Approval date` | `Approval date` | date | no |

Note that the debit card form repeats `Ministry / department` here, having already asked it in section 1; the
reimbursement form uses the slot for `Purpose / activity` instead. One shared column with a per-kind label.

The narrative prompt, verbatim, followed on both forms by a two-row empty table for a written answer:

- Debit card:
  > What was purchased, who used or benefited from it, and how did it further the Church's charitable/religious
  > purposes?
- Reimbursement:
  > How did the expenditure further the Church’s charitable/religious purposes?

Both are `string`, multi-line, no length on the form. The debit-card prompt asks three questions in one box, which
is worth keeping as three lines of placeholder text rather than compressing to the reimbursement wording.

(The apostrophe differs between the two — the debit card form uses ASCII `'` in `Church's`, the reimbursement form
uses `’` in `Church’s`. Cosmetic, but quote each form's own character when reproducing it.)

## Section 3 — the line-item table

Section captions:

- Debit card: `3. PURCHASE AND EVIDENCE DETAILS` / `Itemise the complete card transaction`
- Reimbursement: `3. EXPENSE DETAILS` / `Use one line per receipt / transaction`

Both tables have **seven columns**. **Three of the seven headers differ**, not one as the scope doc says.

| # | Debit card header | Reimbursement header | Same? | Type | Req. |
|---|---|---|---|---|---|
| 1 | `Item` | `Date` | **differs** | debit `string`, reimb `date` | implied |
| 2 | `Qty / details` | `Supplier & item / service` | **differs** | string | implied |
| 3 | `Church purpose / user` | `Purpose / ministry` | **differs** | string | implied |
| 4 | `Receipt / tax invoice` | `Receipt / tax invoice` | identical | enum | implied |
| 5 | `Gross incl. GST` | `Gross incl. GST` | identical | decimal | implied |
| 6 | `GST shown` | `GST shown` | identical | decimal | no |
| 7 | `Church use %` | `Church use %` | identical | decimal | implied |

Column 1 is a genuine **type** change, not just a label change: the debit card form's `Item` is text (the card
transaction is one purchase, itemised into its parts) while the reimbursement form's `Date` is a date (one row per
receipt, each with its own date). A single `ExpenseLine` needs both — a nullable `LineDate` and a nullable
`ItemDescription` — with the page requiring whichever one its form prints.

Cell contents, identical on both forms:

- Column 4 is two **checkbox**es in one cell, printed `☐ Attached / ☐ Missing`. They are mutually exclusive, so
  model as an enum (`Attached | Missing`), not two bools. `Missing` on any line is what unlocks section 5.
- Columns 5 and 6 are pre-printed with `$`. Money, 2dp.
- Column 7 is pre-printed `100% /       %` — i.e. the default is 100% and the writer overrides it. Percentage,
  0–100.

**Row counts differ**: the debit card table has **4** blank line rows; the reimbursement table has **5**. Neither
is a real limit — it is what fits on the page — so the web form should not cap lines at 4 or 5. Worth stating
because a naive port of the paper layout would.

### Totals block

Three rows on both, immediately below the line table. All three are `decimal`, all pre-printed with `$`, and the
scope doc's rule that the server recomputes them applies to all three.

| Row | Debit card label | Reimbursement label |
|---|---|---|
| 1 | `Total card transaction` | `Subtotal of receipts` |
| 2 | `Less personal portion to be repaid immediately` | `Less personal / non-reimbursable portion` |
| 3 | `NET AUTHORISED CHURCH EXPENSE` | `TOTAL REIMBURSEMENT CLAIMED` |

Row 3 is printed in capitals on both forms. Same three columns, same arithmetic, per-kind labels — one shared
triple of columns, not six.

The debit-card row 2 wording carries an obligation the reimbursement wording does not: `to be repaid immediately`.
That is the same obligation as declaration 3 and the section 8 `Personal repayment` field, and it is the reason
the debit card form has a repayment field where the reimbursement form does not.

## Section 4 — compliance questions

Section captions:

- Debit card: `4. SPECIAL PURCHASE DETAILS AND COMPLIANCE CHECKS`
- Reimbursement: `4. SPECIAL CATEGORIES AND COMPLIANCE CHECKS`

Each question is one paragraph beginning with a leading **checkbox** `☐`, then the question, then a **`☐ No`** and a
**`☐ Yes`** checkbox pair with the Yes carrying its own instruction. Three checkboxes per question, eighteen per
form. The leading `☐` has no label of its own — it is a "considered this" tick — and does not need a model column;
`bool?` on the No/Yes pair is the whole answer, with `null` meaning unanswered, as the scope doc says.

**Verdict: two of six are word-for-word identical (Q4 and Q6). Four differ.** The scope doc's "five of them
word-for-word identical" and "Q2–Q6 identical" are both wrong.

| # | Debit card, verbatim | Reimbursement, verbatim | Same? |
|---|---|---|---|
| 1 | `☐  Parking, toll, fuel, taxi or other travel? ☐ No  ☐ Yes - record the destination/event and Church purpose, and attach the available receipt or trip evidence.` | `☐  Motor vehicle travel claimed? ☐ No  ☐ Yes — complete the trip record below. Fuel is not claimed separately where a per-kilometre rate is used.` | **differs** |
| 2 | `☐  Meal, restaurant, catering, gift or hospitality? ☐ No  ☐ Yes - list every attendee or recipient, their relationship to the Church, the ministry purpose and any private share below.` | `☐  Entertainment, meals, gifts or hospitality? ☐ No  ☐ Yes — identify attendees, Church purpose and any personal component.` | **differs** |
| 3 | `☐  Did a spouse, child, family member or private companion attend, travel, dine or benefit? ☐ No  ☐ Yes - identify them and fully exclude their costs and fair share of every joint expense.` | `☐  Did a spouse, child, family member or other private companion benefit from or accompany the claimant? ☐ No  ☐ Yes — identify and fully exclude their costs and their share of any joint expense.` | **differs** |
| 4 | `☐  Expense incurred outside Australia or for an overseas activity? ☐ No  ☐ Yes — specify country and link to the relevant activity/project records.` | `☐  Expense incurred outside Australia or for an overseas activity? ☐ No  ☐ Yes — specify country and link to the relevant activity/project records.` | **identical** |
| 5 | `☐  Cardholder, supplier or recipient is a Responsible Person, senior manager, close family member, or related entity? ☐ No  ☐ Yes - disclose below and use an independent approver.` | `☐  Claimant is a Responsible Person, senior manager, close family member, or related entity? ☐ No  ☐ Yes — declare below and use an independent approver.` | **differs** |
| 6 | `☐  Actual, potential or perceived conflict of interest? ☐ No  ☐ Yes — disclose below and record/manage it under the Church conflict-of-interest process.` | `☐  Actual, potential or perceived conflict of interest? ☐ No  ☐ Yes — disclose below and record/manage it under the Church conflict-of-interest process.` | **identical** |

Notes on the differences, because some are subtler than they look:

- **Q1 is a different question, not a rephrasing.** The debit card asks about incidental travel costs *paid on the
  card* (parking, tolls, fuel, taxi) and wants a receipt. The reimbursement asks about *the claimant's own vehicle*
  at a per-kilometre rate and explicitly excludes fuel. A "Yes" on each opens a different table (below), and a
  shared question text would be a compliance error, not a cosmetic one.
- **Q2 differs in scope**: the debit-card version includes `restaurant` and `catering` explicitly and demands `every
  attendee or recipient` with `any private share`; the reimbursement version leads with `Entertainment` and asks
  only to `identify attendees`.
- **Q3 and Q5 differ in the subject** (`Cardholder, supplier or recipient` vs `Claimant`) and in the verb
  (`disclose` vs `declare`).
- **Q1, Q2, Q3 and Q5 on the debit card form use a hyphen `-` before the Yes instruction; the reimbursement form
  uses an em dash `—`.** Q4 and Q6 use `—` on both, which is part of why they match exactly. If anyone
  "normalises" the punctuation, Q4 and Q6 stop being the only exact matches and this table stops being checkable.

Both forms answer all six as `bool?` columns on the header, per the scope doc. That decision survives; only the
label strings become per-kind.

### The section 4 detail tables

A "Yes" on Q1 or Q2 opens a table. **Both tables have six columns**, and the scope doc's summary of the trip record
as "from/to/km/rate" omits two of them.

Debit card — captioned `Meals / hospitality / gifts / travel details (if applicable)`, 3 blank rows:

| # | Header (verbatim) | Type | Req. |
|---|---|---|---|
| 1 | `Date` | date | implied when opened |
| 2 | `Person / recipient` | string | implied when opened |
| 3 | `Relationship / role` | string | implied when opened |
| 4 | `Amount` | decimal | implied when opened |
| 5 | `Private share` | decimal | no |
| 6 | `Reason and Church purpose` | string | implied when opened |

Reimbursement — captioned `Motor vehicle trip record (if applicable)`, 3 blank rows:

| # | Header (verbatim) | Type | Req. |
|---|---|---|---|
| 1 | `Date` | date | implied when opened |
| 2 | `From` | string | implied when opened |
| 3 | `To` | string | implied when opened |
| 4 | `Business km` | decimal | implied when opened |
| 5 | `Approved rate` | decimal | implied when opened |
| 6 | `Church purpose` | string | implied when opened |

These are the scope doc's `ExpenseAttendee` and `ExpenseTrip` child collections. Neither table caps at 3 rows for
any reason but page space.

### The section 4 free-text block

A three-row empty table on both, under a caption that **differs**:

- Debit card: `Conflict / related-party / personal repayment / overseas details (if applicable)`
- Reimbursement: `Conflict / related-party / overseas details (if applicable)`

`string`, multi-line, no length. The debit card version additionally collects **personal repayment** detail here —
the third thing the debit card form asks about repayment, alongside the totals row and the section 8 field.

## Section 5 — missing receipt declaration

Caption on both forms, identical: `5. MISSING RECEIPT DECLARATION` / `Complete only when evidence is unavailable`.

The three run-on fields and the free-text reason are **byte-for-byte identical** on both forms:

> `Supplier: ________________________________  Date: ____________  Amount: $____________`
>
> `Reason evidence cannot be supplied and steps taken to obtain a copy: __________________________________________________________`
>
> `________________________________________________________________________________________________`

| Label (verbatim) | Type | Req. | Notes |
|---|---|---|---|
| `Supplier:` | string | implied when section opens | none |
| `Date:` | date | implied when section opens | shorter rule than the `/    /` boxes elsewhere; still a date |
| `Amount:` | decimal | implied when section opens | pre-printed `$` |
| `Reason evidence cannot be supplied and steps taken to obtain a copy:` | string | implied when section opens | two printed rules' worth of space, multi-line |

The declaration itself is a **checkbox** paragraph, and it **differs between the forms**:

- Debit card, verbatim:
  > `☐  I declare that the card charge was made for the stated Church purpose, the details are accurate, and I have
  > supplied all available evidence. I understand GST must not be claimed unless the Church holds the evidence
  > required by law.`
- Reimbursement, verbatim:
  > `☐  I declare that I paid this amount for the stated Church purpose, have not been and will not be reimbursed
  > from another source, and have supplied all available evidence. I understand GST must not be claimed unless the
  > Church holds the evidence required by law.`

**The scope doc says "identical, plus 'not reimbursed from another source'". That is half right.** The added clause
is real, but the opening is also rewritten (`the card charge was made` / `the details are accurate` versus `I paid
this amount`) and the debit card version drops `the details are accurate` from the reimbursement version entirely —
it is the reimbursement version that has no such phrase. Only the final GST sentence is shared verbatim.

## Section 6 — declarations

Captions differ: `6. CARDHOLDER DECLARATION` / `6. CLAIMANT DECLARATION`.

Five **checkbox** paragraphs each. **Verdict: one of five is word-for-word identical (D4). Four differ, and D3 is a
different declaration entirely.** The scope doc's "4 shared; the fifth is the no-double-claim one" is wrong: it is
the other way round — four differ and one is shared.

| # | Debit card, verbatim | Reimbursement, verbatim | Same? |
|---|---|---|---|
| 1 | `☐  STRICTLY CHURCH EXPENSE: Every amount treated as a Church expense was incurred solely for an authorised Church purpose. No personal, private or family expense is included.` | `☐  STRICTLY CHURCH EXPENSE: Every amount claimed was incurred and paid by me solely for an authorised Church purpose. No personal, private or family expense is included.` | **differs** |
| 2 | `☐  Where my spouse, child, family member or another private companion attended, accompanied me or received any benefit, I have separately identified and excluded all of their costs and their fair share of every joint or shared expense (including meals, travel, accommodation, tickets and transport).` | `☐  Where my spouse, child, family member or another private companion accompanied me or received any benefit, I have excluded all of their costs and their fair share of every joint or shared expense (including travel, accommodation, meals, tickets and transport).` | **differs** |
| 3 | `☐  If any personal or unauthorised amount was inadvertently charged, I have disclosed it and repaid or arranged immediate repayment to the Church. I understand the Church debit card must not be used for personal purchases.` | `☐  I have not previously claimed, been reimbursed for, or received an allowance or other payment covering these amounts, and I will notify the Church if that changes.` | **differs — different subject** |
| 4 | `☐  The attached evidence is genuine and itemised. I have disclosed discounts, refunds, credits, loyalty benefits used as payment, insurance recoveries and any private use.` | `☐  The attached evidence is genuine and itemised. I have disclosed discounts, refunds, credits, loyalty benefits used as payment, insurance recoveries and any private use.` | **identical** |
| 5 | `☐  I have identified meal attendees, gift recipients and beneficiaries where applicable, and disclosed any conflict of interest or related-party connection.` | `☐  I have disclosed any actual, potential or perceived conflict of interest and any related-party connection relevant to this claim.` | **differs** |

Where the differences bite:

- **D1** is not a rephrasing: the debit card says `treated as a Church expense`, the reimbursement says `claimed …
  and paid by me`. The debit card version has to work for money the Church has already spent; the reimbursement
  version asserts the claimant spent their own.
- **D2** differs in three places — `attended, accompanied me or received` vs `accompanied me or received`,
  `separately identified and excluded` vs `excluded`, and the parenthetical list is reordered (`meals, travel,
  accommodation` vs `travel, accommodation, meals`). Same obligation, different words. A shared string would
  silently change what a cardholder signed.
- **D3 is a different declaration on each form.** Debit card D3 is the *repayment* declaration; reimbursement D3 is
  the *no-double-claim* declaration. Neither form has the other's. The scope doc's observation that "the
  reimbursement form already carries a declaration the card form should probably have and doesn't" is correct and
  identifies D3 — but it is a swap, not an addition, and the reimbursement form is equally missing the debit card's
  repayment declaration. Both gaps are for the finance team to decide on, not for this build to close.
- **D5** narrows on the debit card form (meal attendees and gift recipients *plus* conflicts) and on the
  reimbursement form covers conflicts only.

Model as five `bool?` columns on the header with per-kind label strings, matching the six compliance answers.

### Signature line

Between section 6 and section 7 on both forms, and it is the thing the five declarations attach to:

- Debit card: `Cardholder signature (confirming all declarations on page 2): ________________________  Date:         /        /`
- Reimbursement: `Claimant signature (confirming all declarations on page 2): ________________________  Date:         /        /`

| Slot | Type | Req. | Notes |
|---|---|---|---|
| signature | string | implied | the scope doc's typed signature; the label differs per kind |
| `Date:` | date | implied | `/        /` |

## Section 7 — independent approval

Captions differ only in the trailing rule:

- Debit card: `7. INDEPENDENT APPROVAL` / `Approver must not be the cardholder`
- Reimbursement: `7. INDEPENDENT APPROVAL` / `Approver must not be the claimant`

That caption is a compliance constraint printed on the form, not a preference. It is unenforceable in this scope —
the pages are anonymous — but it must render, verbatim, on both the page and the PDF.

| Field | Debit card, verbatim | Reimbursement, verbatim | Type |
|---|---|---|---|
| Decision | `Decision:  ☐ Approved in full   ☐ Church expense approved $________   ☐ Repayment required $________   ☐ Declined` | `Decision:  ☐ Approved in full   ☐ Approved for $________________   ☐ Returned for information   ☐ Declined` | enum |
| Confirmation | `I confirm the purchase is reasonable, authorised, supported, consistent with Church purposes and policy, and any private, gift, hospitality, conflict or related-party matter has been appropriately managed.` | `I confirm the claim is reasonable, authorised, supported, consistent with Church purposes and policy, and any conflict or related-party matter has been appropriately managed.` | bool? |
| `Approver name / role:` | `______________________________________________` | identical | string |
| `Signature:` | `________________________________  Date:         /        /` | identical | string + date |

Four **checkbox**es in the decision line on each form. They are mutually exclusive, so one enum:

| Debit card member | Reimbursement member | Carries an amount? |
|---|---|---|
| `Approved in full` | `Approved in full` | no |
| `Church expense approved $________` | `Approved for $________________` | **yes** — same slot, different label; one shared `decimal?` |
| `Repayment required $________` | `Returned for information` | debit card only, **yes**; reimbursement only, no amount |
| `Declined` | `Declined` | no |

So one shared enum with kind-specific members, one shared approved-amount `decimal?`, and one debit-card-only
repayment `decimal?`. The confirmation sentence differs (`the purchase` vs `the claim`, and the debit card adds
`private, gift, hospitality,`), so it is a per-kind string with a shared `bool?`.

All of section 7 renders **read-only and disabled** per the scope doc.

## Section 8 — finance use only

Caption `8. FINANCE USE ONLY` on both, with no sub-caption. Five rows of four cells each; **three cells differ**.

| Row | Debit card labels | Reimbursement labels | Type | Same? |
|---|---|---|---|---|
| 1a | `Transaction reference` | `Claim reference` | string | **differs** |
| 1b | `Statement date` | `Payment date` | date | **differs** |
| 2a | `Account / GL code` | `Account / GL code` | string | identical |
| 2b | `Cost centre / ministry` | `Cost centre / ministry` | string | identical |
| 3a | `GST treatment` | `GST treatment` | enum | identical |
| 3b | `GST credit claimed` | `GST credit claimed` | decimal | identical |
| 4a | `Evidence check` | `Evidence check` | enum | identical |
| 4b | `BAS period` | `BAS period` | string | identical |
| 5a | `Personal repayment` | `Payment reference` | debit `string`, reimb `string` | **differs** |
| 5b | `Entered / checked by` | `Entered / checked by` | string | identical |

Checkbox cells, identical on both forms:

- `GST treatment`: `☐ GST credit   ☐ No GST credit   ☐ Mixed`
  — three **checkbox**es, mutually exclusive, one enum.
- `Evidence check`: `☐ Valid tax invoice(s)   ☐ Other acceptable evidence` — two **checkbox**es, one enum.
- `GST credit claimed` is pre-printed `$`.

`Personal repayment` on the debit card form is an unruled cell with no `$` printed, unlike `GST credit claimed`. It
is the fourth place the debit card form touches repayment. Treat it as `string?` — the form does not commit it to
being an amount — and let finance write "repaid 2026-08-14, ref 4471" if that is what they do.

### Finance checklist

Six **checkbox** paragraphs on each form. Items 1, 2, 5 and 6 are word-for-word identical; **items 3 and 4 differ**.

| # | Debit card, verbatim | Reimbursement, verbatim | Same? |
|---|---|---|---|
| 1 | `☐ Supplier, date, description and amount are evidenced; GST information is sufficient for the credit claimed.` | same | identical |
| 2 | `☐ Expenditure is coded to the correct activity/fund and restricted funds or grant conditions have been checked.` | same | identical |
| 3 | `☐ No personal or family costs remain as a Church expense; joint costs are apportioned and any private amount has been repaid and reconciled.` | `☐ No personal or family costs are included; any joint expense has been properly apportioned and supporting calculations checked.` | **differs** |
| 4 | `☐ Meal attendees, gift recipients and travel purpose are documented where applicable; refunds, credits and duplicate charges have been checked.` | `☐ Refunds, credits and duplicate claims have been excluded; arithmetic and bank details verified.` | **differs** |
| 5 | `☐ Related-party, conflict and overseas-activity records/registers have been updated where required.` | same | identical |
| 6 | `☐ Form, approvals and supporting records filed together and retained under the Church record-retention policy (minimum seven years for ACNC records).` | same | identical |

Checklist item 6 is where the seven-year ACNC retention the scope doc cites is actually printed. Quote it on the
page rather than paraphrasing the obligation.

### Closing note

- Debit card, verbatim:
  > Important: This form supports compliance but does not replace the Church constitution, delegations, debit card
  > policy, grant conditions, employment obligations, or professional advice. Finance should apply the current ATO
  > and ACNC requirements at the payment date.
- Reimbursement, verbatim: identical except `debit card policy` → `reimbursement policy`.

## The kind-specific header fields

**The exact count is 19** — 10 debit-card-only and 9 reimbursement-only. The scope doc's "about fifteen" undercounts.

A field is counted here when it exists on **exactly one** of the two forms. A field printed on both under different
labels is *not* counted — it is one shared column with a per-kind label, and those are listed separately below so
the two lists together account for every header difference.

Child-collection fields (sections 3, 4's tables, 5) are not header fields and are not counted; they are
`ExpenseLine`, `ExpenseAttendee`, `ExpenseTrip` and `MissingReceiptDeclaration`.

### Debit card only — 10

| # | Section | Label (verbatim) | Proposed C# property | Type |
|---|---|---|---|---|
| 1 | 1 | `Card last 4 digits` | `CardLastFourDigits` | `string?` (exactly 4 digits) |
| 2 | 1 | `Transaction date` | `TransactionDate` | `DateTimeOffset?` |
| 3 | 1 | `Time:` | `TransactionTime` | `TimeOnly?` |
| 4 | 1 | `Supplier / merchant` | `SupplierMerchant` | `string?` |
| 5 | 1 | `Amount charged` (`$____________`) | `AmountCharged` | `decimal?` |
| 6 | 1 | `Ref:` (bank reference) | `BankReference` | `string?` |
| 7 | 7 | `Repayment required $________` | `RepaymentRequiredAmount` | `decimal?` |
| 8 | 8 | `Transaction reference` | `TransactionReference` | `string?` |
| 9 | 8 | `Statement date` | `StatementDate` | `DateTimeOffset?` |
| 10 | 8 | `Personal repayment` | `PersonalRepayment` | `string?` |

### Reimbursement only — 9

| # | Section | Label (verbatim) | Proposed C# property | Type |
|---|---|---|---|---|
| 11 | 1 | `Phone / email` | `ContactPhoneEmail` | `string?` |
| 12 | 1 | `Expense period` (from) | `ExpensePeriodFrom` | `DateTimeOffset?` |
| 13 | 1 | `Expense period` (to) | `ExpensePeriodTo` | `DateTimeOffset?` |
| 14 | 1 | `Payment method` (`☐ EFT   ☐ Other:`) | `PaymentMethod` | `PaymentMethod?` (enum) |
| 15 | 1 | `Other:` on payment method | `PaymentMethodOther` | `string?` |
| 16 | 1 | `Bank details on file` (`☐ Yes   ☐ No – provide securely`) | `BankDetailsOnFile` | `bool?` |
| 17 | 8 | `Claim reference` | `ClaimReference` | `string?` |
| 18 | 8 | `Payment date` | `PaymentDate` | `DateTimeOffset?` |
| 19 | 8 | `Payment reference` | `PaymentReference` | `string?` |

Nineteen nullable columns on `ExpenseSubmission`, exactly as the scope doc's shape intends — just five more of them
than it estimated.

`AmountCharged` (field 5) is the debit card form's own statement of what the card was charged, sitting beside
`Total card transaction` in the totals block, which is the sum of the lines. **Those two are the reconciliation the
scope doc's slice 7 describes**: a submission whose lines do not sum to the amount charged is refused with both
figures named. The reimbursement form has no equivalent, because nothing external states its total.

### Shared columns carrying a per-kind label

Not kind-specific fields — one column each — but the page and the PDF must print the right label, so they need a
per-kind string in the UI layer.

| Shared column | Debit card label | Reimbursement label |
|---|---|---|
| `SubmitterName` | `Cardholder name` | `Claimant name` |
| `FormDate` | `Form date` | `Claim date` |
| `PurposeActivity` (§2 slot 1) | `Ministry / department` | `Purpose / activity` |
| `GrossTotal` | `Total card transaction` | `Subtotal of receipts` |
| `LessPersonalAmount` | `Less personal portion to be repaid immediately` | `Less personal / non-reimbursable portion` |
| `NetTotal` | `NET AUTHORISED CHURCH EXPENSE` | `TOTAL REIMBURSEMENT CLAIMED` |
| `ApprovedAmount` (§7) | `Church expense approved $________` | `Approved for $________________` |
| `SignatureName` | `Cardholder signature (confirming all declarations on page 2):` | `Claimant signature (confirming all declarations on page 2):` |
| `ApprovalDecision` (§7) | enum member `Repayment required` | enum member `Returned for information` |
| line column 1 | `Item` (text) | `Date` (date) — **needs both nullable columns on `ExpenseLine`** |
| line column 2 | `Qty / details` | `Supplier & item / service` |
| line column 3 | `Church purpose / user` | `Purpose / ministry` |

Beyond these, **every one of the six compliance questions, the five declarations, the section 5 declaration, the
section 7 confirmation sentence and the six finance-checklist items is a per-kind string**, because at most two of
six, one of five, zero of one, zero of one and four of six respectively are shared. Store the answers in shared
columns; never share the text.

## Where the scope doc is wrong

Every claim below is from [2026-08-expense-forms-scope.md](2026-08-expense-forms-scope.md) and was checked against
`word/document.xml`. None of them changes the one-aggregate decision — the structure really is shared — but several
would produce a form that does not say what the paper form says, which on a compliance document is the failure that
matters.

| Scope doc says | The `.docx` says |
|---|---|
| "the same six compliance questions (five of them word-for-word identical)" and "Q2–Q6 identical" | **Two of six** are identical: Q4 and Q6. Q1, Q2, Q3 and Q5 all differ, and Q1 is a materially different question. |
| §6 "5 items, 4 shared; the fifth is the no-double-claim one" | **One of five** is identical: D4. D3 is a *different declaration* on each form — repayment on the card form, no-double-claim on the reimbursement form — not an extra item on one of them. |
| §2 "identical" | Caption differs (`CHURCH` vs `BUSINESS PURPOSE AND AUTHORISATION`), the first field's label differs (`Ministry / department` vs `Purpose / activity`), and the narrative prompt differs. |
| §3 "first column is **Item**" / "first column is **Date**" — implying only column 1 differs | **Three of seven** headers differ: columns 1, 2 and 3. Columns 4–7 are identical. |
| §5 "identical, plus 'not reimbursed from another source'" | The added clause is real, but the opening clause is also rewritten and `the details are accurate` appears only on the debit card form. Only the closing GST sentence is shared verbatim. |
| §4 detail table is "a **trip record** (from/to/km/rate)" | Six columns: `Date`, `From`, `To`, `Business km`, `Approved rate`, `Church purpose`. |
| "They differ in about fifteen fields" | **19** kind-specific header fields — 10 debit card, 9 reimbursement — plus 12 shared columns that carry a per-kind label. |
| — (not mentioned) | The section 4 free-text caption differs: the debit card form collects `personal repayment` detail there and the reimbursement form does not. |
| — (not mentioned) | Finance checklist items 3 and 4 differ between the forms; 1, 2, 5 and 6 are identical. |
| — (not mentioned) | The debit card line table has 4 blank rows, the reimbursement 5. Page space, not a limit — do not cap the web form at either. |
| — (not mentioned) | Neither `.docx` contains a single form control. All 110 checkboxes across the two files are the literal character `☐` (U+2610). |

One thing the scope doc gets right and is worth restating: it observes that "the reimbursement form already carries
a declaration the card form should probably have and doesn't". That is D3, and it runs both ways — the debit card
form's repayment declaration has no counterpart on the reimbursement form either. Both are questions for finance,
and this scope should reproduce each form as printed rather than quietly harmonising them.
