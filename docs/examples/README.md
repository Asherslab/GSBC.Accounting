---
title: Rendered examples
kind: reference
status: current
module: expenses
verified: 2026-08-31
code:
  - GSBC.Accounting.Grpc/Features/Pdf
---

# Rendered examples

What `GET /api/submissions/{id}/pdf` actually produces, checked in so it can be shown alongside the
HTML mockup without running the stack.

## `debit-card-purchase-example.pdf`

A fully filled debit card submission, generated from mock data on 2026-08-31 and rendered by
`SubmissionDocument`. Two A4 pages:

1. Masthead, sections 1 to 6 — including the totals block and all six compliance questions with their
   answers, and the five declarations with their tick boxes.
2. The typed signature block, sections 7 and 8 as **empty ruled blocks** for wet-signing, and the
   evidence manifest.

Three things to look at when the accountant reviews it:

- **The mock-data banner.** Loud and amber, because a demonstration submission must never be mistaken
  for a claim once somebody has printed it. A real submission does not carry it.
- **Unanswered compliance questions print as "Not answered", in red** — never as a blank. A blank on
  paper cannot be told apart from a "No", and the two are different facts.
- **The evidence manifest is a manifest, not the receipts.** Images could be embedded directly, but PDF
  receipts need page-level merging, which QuestPDF does not do. The manifest carries each file's name,
  type, size and SHA-256, so "is this the file that was uploaded" has an answer that does not depend on
  the object store. Merging the files themselves is the obvious next step, and the open question below.

## `expense-reimbursement-example.pdf`

The same submission shape on the other form, so the two can be compared side by side. Worth checking
that the per-kind wording really is per-kind: column 1 of section 3 is `Date` rather than `Item`,
compliance question 1 is the motor vehicle one and opens a **trip record** rather than an attendee
table, the totals read `Subtotal of receipts` / `TOTAL REIMBURSEMENT CLAIMED`, and declaration 3 is the
no-double-claim one rather than the debit card's repayment declaration.

A "Yes" answer prints in orange, so a reviewer's eye lands on the questions that opened something.

## The open question this exists to answer

The output format is parked with the accountant. This PDF is what that conversation needs — whether the
layout is right, whether anything is missing, and whether the receipts have to be bound into the same
file or whether the manifest is enough.

Regenerate it by submitting a mock-filled form and fetching `/api/submissions/{id}/pdf`.
