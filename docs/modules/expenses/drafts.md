---
title: Drafts and the draft session cookie
kind: reference
status: current
module: expenses
verified: 2026-09-01
code:
  - GSBC.Accounting.Grpc/Features/Sessions
  - GSBC.Accounting.Grpc/Data/Models/Sessions/DbDraftSession.cs
  - GSBC.Accounting.Grpc/Features/Expenses/ExpenseSubmissionServices
  - GSBC.Accounting.WASM/Features/Expenses/DraftAutosave.cs
  - GSBC.Accounting.WASM/Features/Expenses/Pages/DraftsPage.razor
  - GSBC.Accounting.WASM/Features/Expenses/Pages/ExpenseForm.razor
---

# Drafts and the anonymous session cookie

How an unsubmitted form is kept, who is allowed to read it back, and what a claimant is promised about
it. Read this before touching anything that reads or writes an `ExpenseSubmission`, because **the
submission id is no longer sufficient authority on its own** and several endpoints used to assume it
was.

## The cookie authenticates a browser, never a person

`__gsbc_anon` answers exactly one question: *is this the browser that saved that draft?*

It **is** an authentication scheme, and that is new — `AnonymousSessionHandler` turns the cookie into a
principal and the `AnonymousSession` policy gates almost everything on it (see
[Authentication and the AnonymousSession policy](#authentication-and-the-anonymoussession-policy)).
What it authenticates is a browser. It identifies nobody, nobody chose it, and anyone holding the
cookie is its owner.

So the old rule stands unchanged and matters more now that `HttpContext.User` is populated: **no
approval step, no finance step, no audit trail naming somebody, and nothing else that needs to know who
a person is may ever be hung off it** — sections 7 and 8 of both paper forms are completed by a human
who is not the claimant, and that stays true. When real sign-in arrives it gets its own scheme and its
own policy beside this one; it does not extend this one.

The pages remain anonymous in the sense that matters: nobody signs in, anybody can start a form, and
`Create` is reachable by strangers by necessity. So the per-IP rate limits and upload ceilings in
[attachments.md](attachments.md) are still not optional.

## Authentication and the AnonymousSession policy

Ownership used to be enforced by every method remembering to call `CurrentAsync` and fold
`OwnerSessionId` into its predicate. That was correct everywhere and **structurally unenforced** — a new
method that forgot the call silently fell back to treating a submission id as authority, which is the
regression the cookie was introduced to close.

Now the cookie is a real scheme with a deny-by-default `FallbackPolicy`, so forgetting is a 401 rather
than a hole:

| Endpoint | Gate |
|---|---|
| `Create` | **Exempt** — the only minter, so requiring a session would mean nobody could ever get one |
| `Read`, `Update`, `Submit`, `DiscardDraft`, `ListDrafts` | `AnonymousSession` policy |
| Attachment `POST` / `DELETE` / `PATCH` | `AnonymousSession` policy, on the route group |
| Attachment `GET`, `GET .../pdf` | **Exempt** — a submitted claim is readable by whoever holds its id |
| Health probes, `/` signpost | **Exempt**, explicitly, because of the fallback policy |

Two things that are easy to get wrong here:

- **The policy is a floor, not the check.** Satisfying it proves a session exists, never that the
  session owns the submission in the request. Every `x.OwnerSessionId == sessionId` predicate stays
  exactly where it was; the policy replaces none of them.
- **The two exempt reads are not open.** They widen *who may ask*, never *what comes back*: a draft is
  still owner-only, enforced by the predicate, and a stranger asking for one gets 404. Verified — a
  draft's PDF answers 404 with no cookie and 200 once the claim is submitted.

### `[AllowAnonymous]` on a gRPC method does not work

protobuf-net.Grpc 1.2.2 propagates the service *type*'s attributes onto the endpoint it builds and drops
method-level ones. So `[AllowAnonymous]` on `Create` compiles, reads correctly, and is silently ignored
— measured, with every method including `Create` answering 401, which is a service nobody can ever
obtain a session from.

The exemption is applied in `Program.cs` with `AllowAnonymousGrpcMethods<T>("Create")`, an endpoint
convention that adds the metadata after the endpoint is built. **Do not "tidy" that back into an
attribute on the method.**

## Why a session table rather than a signed cookie

The cookie carries 256 random bits and nothing else. The server SHA-256s it and looks the hash up in
`AnonymousSessions`; only the hash is stored, so a database backup or a `select *` cannot be replayed as
somebody's session.

A self-contained signed cookie was the obvious alternative and is the wrong choice here:

| | Signed cookie | Session row |
|---|---|---|
| Survives a pod restart | **No** — the data-protection key ring lives in the container filesystem unless persisted | Yes |
| Can be renewed, expired or revoked | No | Yes |
| Can be claimed by a user account later | No | Yes — `UserId` |
| Needs key management | Yes | No |

The first row is the decisive one. These cookies are meant to last a year; a key ring that is lost on
every redeploy would invalidate every draft in the church on every deploy, silently.

## Minted on the first draft write, never on a page view

`AnonymousSessions.EnsureAsync` is called by `Create` and **by nothing else**. Every other caller uses
`CurrentAsync`, which returns null rather than issuing anything.

That is deliberate and easy to break. Minting in `ListDrafts` — which runs on every visit to `/drafts` —
would hand a session and a row to every visitor and every crawler. It also keeps the cookie defensible
as strictly necessary to a service the claimant actually asked for, which is what keeps it out of
consent-banner territory.

`Create` mints **after** validation, so a refused create leaves no session behind — the autosave calls
`Create` speculatively as the claimant types, and a refusal that had already minted a session would hand
a cookie to a browser that stored nothing.

## Lifetimes

| Number | Value | Why |
|---|---|---|
| Cookie lifetime | 365 days | "Indefinite" is not available — Chrome caps cookie expiry at 400 days and other browsers have their own ceilings. A longer `Max-Age` is silently truncated, leaving the server believing in a session the browser threw away. |
| Renewed after | 180 days | Half. Most claimants file a form once or twice a year, so a returning visitor effectively never expires, while a browser never seen again lapses on schedule. Renewing on every request would write to Postgres on every autosave keystroke. |
| Abandoned draft | 90 days from last edit | A privacy limit, not a storage one — see below. |

Renewal re-sends the cookie as well as updating the row. Without that the row outlives the cookie and
the claimant loses their drafts on a schedule the server believed it had already extended.

## The cookie attributes are security controls

Set in `AnonymousSessions.Write`. Each one is load-bearing:

- **`SameSite=Lax`** is what refuses the cookie on a cross-site POST. The attachment upload carries
  `DisableAntiforgery()` — it must, because its body is a raw stream that cannot be buffered for model
  binding — so `Lax` stands in for the antiforgery token that endpoint cannot have. Weakening it to
  `None` reopens CSRF on an endpoint that writes to the object store.
- **`HttpOnly`** keeps it out of `document.cookie`, which also keeps it out of Safari's seven-day cap on
  script-set cookies. A client-set cookie would quietly become a one-week session.
- **`Secure`** follows `Request.IsHttps` rather than being hard-coded true. The gRPC service is plain
  HTTP behind YARP, so that is only correct because `UseForwardedHeaders` runs first and honours
  `X-Forwarded-Proto`. Hard-coding it true drops the cookie entirely on the local HTTP profile (5242).

## Minted by the gRPC service, carried by the proxy

YARP holds no signing key and no database connection, so it can neither mint this nor check it. It
forwards `Cookie` in and `Set-Cookie` back out, and the browser stores the result against the proxy's
origin because the WASM app, `/gRPC/` and `/api/` are all served from there.

This is the shape `GSBC.ImpactKids` uses for its pickup-display token (`DisplayAuth/`): the proxy
carries a sealed envelope it cannot open. Read that code before adding a second scheme here — its
`AddBearerTokenToHeadersTransform` carries a production incident from 2026-08-28 where two schemes in
one browser resolved to the wrong identity and put every affected leader into a sign-in loop. An
anonymous session and a signed-in user in one browser is that same collision.

## What every read and write now filters on

`ExpenseSubmission.OwnerSessionId`. The pattern is the same everywhere: resolve the session, then put
`x.OwnerSessionId == sessionId` **in the query predicate**, not in a check after the fetch. Filtering in
the query is what makes "not yours" and "does not exist" the same answer — anything else is a way to ask
the server which submission ids are real.

| Operation | Rule |
|---|---|
| `Create` | Mints a session, stamps `OwnerSessionId` |
| `Update`, `Submit`, `Read`, `DiscardDraft` | Owner only; `Read` and `DiscardDraft` are drafts only |
| `ListDrafts` | The session's own drafts; no session answers an empty list, not an error |
| Attachment upload, detach | Owner only, and the submission must be a `Draft` |
| Attachment download, PDF render | Owner **or** the submission is `Submitted` |

**The last row is the one to understand before changing it.** A submitted claim stays readable by id
because that is the only review path this scope has: there is no approval queue and no finance screen,
so somebody is handed a submission id and reads the PDF. Locking that to the claimant's own browser
leaves the reviewer with `psql`. A *draft* is a different document — half-finished, unreviewed, and
already carrying a name, contact details and card last-four from the moment section 1 is typed.

Rows written before this column existed have `OwnerSessionId` null. **Null is unreachable, not
unowned**: the comparison is against a session id that is never null once a session has resolved, so
those rows match no caller. They stay reachable exactly where they were before — the database, and the
PDF link if they were submitted.

## What this replaced, and what it fixed

Before this, drafts lived in `localStorage` (one slot per form kind) and the server-side draft could not
be read back at all. Two things were wrong with that:

1. **A claimant could not see or list what they had saved**, and the reference shown after "Save draft"
   was a receipt rather than a resume link.
2. **`Update`, `Submit`, attachment upload and the PDF authorised on the submission id alone.** Anyone
   holding one could overwrite a draft, attach files to it, or download a PDF carrying the claimant's
   name and contact details. v4 guids are not guessable, so this was never brute-forceable — but the id
   is printed on screen after a save and baked into the PDF's filename, so it leaked by being shared.

`DraftStore.cs` is gone. **There is deliberately no browser-local copy any more**: two places to look
for the same draft is how a claimant ends up resuming the older one.

## Resuming, and where the kind comes from

Both kinds resume at `/forms/expense/{id}` — `DraftsPage.ResumeUrl` builds nothing else. The id is in the
path rather than the query so the back button and a bookmark land on the same form, and the server checks
it against the caller's session, so pasting somebody else's id gets "could not be found" rather than
their claim.

**The kind comes off the stored row, never off the URL.** `ExpenseFormModel.FromSubmission` takes
`submission.Kind`, so a resumed draft arrives with section 0 already answered — by the row that was
validated, not by the link that was clicked. That mattered more when there were two routes and they could
disagree with the row; it still matters, because the kind decides what every per-kind column in that row
means.

A draft row is **not** created merely by answering section 0. Nothing is written until the claimant types
something or attaches a receipt, so somebody who opens the form, answers one question and walks away
leaves nothing behind — which is the point of creating the draft lazily, and easy to undo by accident:
bumping the autosave from the kind handler put an "Unnamed draft" on this page for every abandoned
visit, observed on 2026-09-01.

## Autosave, and what it costs

`DraftAutosave` saves 2 seconds after the claimant stops typing. `localStorage` was free to write on
every keystroke; a gRPC round trip and a database write are not.

**Up to two seconds of typing is not yet on the server.** Close the tab mid-sentence and that sentence
is gone. That is the trade for having drafts that a list page can show.

Autosave is **silent about refusals**, and that matters. A half-finished form is no longer refused —
the completeness rules moved to `Submit` on 2026-09-01 — so what reaches the autosave is a genuine
problem with what was typed, and even that is dropped: showing the banner mid-keystroke would mean a
form that scolds while it is being filled in. Errors surface only on an explicit Save or Submit.
Both cancel the pending autosave first: otherwise the stale save lands a second later as an `Update`
against a now-`Submitted` row and puts a refusal on screen underneath the success message.

## Removing a receipt is a server call now

It used to drop the file from the page only. That stopped being tenable when drafts became resumable —
the file came straight back on the next open. `DELETE /api/submissions/{id}/attachments/{attachmentId}`
soft-deletes the row; nothing hard-deletes evidence.

**`IgnoreQueryFilters()` in the upload's duplicate check is load-bearing.** The unique index on
`(SubmissionId, ContentHash)` does not know about soft deletes, so a claimant who removes a receipt and
re-attaches the same file would sail past a filtered duplicate check into a unique-constraint violation
— a 500 for an ordinary change of mind. The upload finds the flagged row and un-flags it instead.

## The abandoned-draft purge

`DraftPurgeService`, daily, soft-deletes drafts whose `UpdatedAt` is more than 90 days old, and hard-
deletes expired session rows.

**This is a privacy obligation, not housekeeping.** A draft carries a claimant's name and contact
details, and an abandoned one is a form that will never be submitted — keeping it for the seven years
the ACNC asks of *submitted* records would be retaining personal data for a purpose that has ended.

Two conditions guard it and both matter: `Status == Draft` keeps submitted evidence out, and `UpdatedAt`
rather than `CreatedAt` means the window runs from the last edit, so a form somebody is slowly working
through is never taken from under them. It loads and flags rather than using a bulk `ExecuteUpdate`, so
the predicate is visible next to the thing it protects.

**Attachment objects are deliberately left in the store.** Soft-deleting rows is reversible; deleting
bytes is not, and reclaiming them is a decision about destroying uploaded files that nobody has taken.
Every pass logs the reclaimable byte count so the size of that question stays visible.

`UpdatedAt` was backfilled from `CreatedAt` in the migration. Without that line every pre-existing row
would have defaulted to `0001-01-01` and the first purge after deploy would have thrown away every
draft in the database.

## What a claimant is told

Said on the landing page, on the form and on `/drafts`, because the limits are real:

- Drafts are kept **for this browser**. Clearing cookies or browsing data loses them, and so does a
  private window.
- They are **not shared between devices**.
- A draft nobody edits is **deleted after 90 days**, and the list shows the date.
- **Nobody else can see a draft** — not an approver, not finance — until it is submitted.

Shared church machines are configured to clear cookies on close, which is what handles two people using
one browser. That is an operational control, not something the app enforces.

## When sign-in arrives

`DbDraftSession.UserId` is the whole upgrade path, and it is why drafts are owned by a *session* rather
than by a cookie value. Setting that column on the sessions a person signs in from makes every draft
they ever saved on any of those browsers theirs — no data migration, and no ambiguity about which
anonymous draft belonged to whom. Anonymous access stays; a signed-in claimant additionally reaches
their drafts across browsers.
