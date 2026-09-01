---
title: The attachment store
kind: reference
status: current
module: expenses
verified: 2026-09-01
code:
  - GSBC.Accounting.Grpc/Features/Attachments
  - GSBC.Accounting.Grpc/Data/Models/Expenses/DbExpenseAttachment.cs
  - GSBC.Accounting.AppHost/AppHost.cs
---

# The attachment store

Read this before touching an upload path. It leads with the trap, because that one is not theoretical:
it was measured on this stack and it fails silently.

## `UseChunkEncoding` must stay false — SeaweedFS stores the framing as the file

**Measured on this stack's SeaweedFS 3.98 on 2026-08-31. The failure reproduces.** GSBC.ImpactKids hit
it first, and the scope doc asked for it to be re-checked rather than assumed, because a version bump
could have fixed it. It has not.

With chunk encoding on, the AWS SDK frames the body as `aws-chunked` and SeaweedFS writes that framing
*as the object's content*. A 701-byte PDF was stored as **996 bytes**, beginning:

```
2BD;chunk-signature=045ae7206eb90949a68c2edcd619b43a…
%PDF-1.4
```

and ending `x-amz-trailer-signature:408f2d0c9a4720b4…`, with the real PDF in between.

**Nothing errors.** The PUT succeeds, the row is valid, the recorded byte size and content type are
right, and the object is not the file. There is no log line and no failed request. Six years later that
is an auditor's problem and nobody's explanation.

Two things guard it, and both are needed:

- `AttachmentStoreConfig.UseChunkEncoding` is `false`, which stops it happening.
- The magic-byte check **on read** is how anyone would find out if it started again. It is what caught
  this: the download answered 500 instead of handing over a corrupt receipt.

## Magic bytes on write and on read

`FileSignature` decides what a file is from its first 16 bytes and refuses anything else: PDF, JPEG,
PNG, HEIC and WebP. That is what a receipt legitimately is — a document, a photo or a scan.

The declared content type must **agree** with the detected one. A Mach-O binary renamed `notreally.pdf`
and declared `application/pdf` is refused; so is a real PDF declared as `image/jpeg`. Only the JPEG
spellings (`image/jpg`, `image/pjpeg`) and the HEIC ones are folded together, because browsers
genuinely disagree about those for identical bytes.

HEIC matters more than it looks: an iPhone photo is HEIC, and it is the format people most often do not
realise they are uploading. Its brand sits at offset 8, which is why the sniff buffer is 16 bytes and
why `ReadAtLeastAsync` loops — one `ReadAsync` on a network stream returns short, and sniffing whatever
came back would reject valid HEIC files at random.

## Limits, because the endpoint is anonymous

There is still no sign-in. The write endpoints now sit behind the `AnonymousSession` policy, so an
upload has to carry the `__gsbc_anon` cookie and the draft has to belong to that session — see
[drafts.md](drafts.md) — but that authenticates a browser, not a person: it stops a stranger attaching
files to somebody else's form, and does nothing to stop a stranger creating a draft of their own and
uploading to that. So every ceiling below is still doing the work it was doing.

The download is deliberately **not** behind the policy, because a submitted claim's evidence has to be
readable by whoever is handed its id — there is no approval screen in this scope to hand it to them any
other way. A draft's receipts stay owner-only, enforced by the query predicate rather than by the
policy.

| Limit | Value | Why |
|---|---|---|
| Bytes per file | 20 MB | A scanned multi-page invoice or a phone photo. ImpactKids' 1 MB was set for 30 KB face JPEGs. |
| Bytes per submission | 100 MB | Without it, "create a draft, upload forever" makes the store a free file host. |
| Files per submission | 25 | Same reason. |
| Submission must exist | — | No id, no write. |
| Caller must own the draft | — | The id alone used to be enough to attach a file to anyone's form. |
| Submission must be `Draft` | — | Submitted evidence does not gain new pages afterwards. |

Download is the one asymmetry: **the owner, or anyone holding the id of a `Submitted` claim.** A
submitted claim's evidence is what a reviewer is handed a link to, and there is no approval screen to
hand it to them any other way. A draft's receipts are private to the person still filling the form in.

`Content-Length` is checked first as a cheap rejection, but it is only a claim by the caller: the real
enforcement is in `StageAsync`, which stops reading the moment the running total passes the ceiling.

The body is staged to a **temp file**, not memory. Twenty megabytes per request in the managed heap is
a denial-of-service invitation on an unauthenticated endpoint, and S3 needs a seekable stream to sign
the payload anyway. Hashing is `IncrementalHash` over the same single pass.

## Serving a file somebody else uploaded

Both headers on every download, and both are load-bearing:

- `X-Content-Type-Options: nosniff`
- `Content-Disposition: attachment`

This serves user-supplied files from the app's own origin. Without those, a file uploaded as a
"receipt" can render in place and become same-origin content. Serving your own re-encoded JPEGs, as
ImpactKids does, is not this shape of problem; serving whatever a stranger uploaded is.

**A broken store answers 500, never 404.** A 404 means "no such attachment". A row that exists with no
object behind it is evidence that has gone missing, and somebody has to notice.

## Keys and metadata

`submissions/{submissionId}/{sha256}.{ext}` — the **whole** SHA-256, not a prefix. ImpactKids keeps 12
hex characters (48 bits), fine for photos of a few hundred children and not fine for financial
evidence. The same file uploaded twice to one submission is one object and one row, enforced by a
unique index on `(SubmissionId, ContentHash)`.

The claimant's filename never appears in the key — it is attacker-controlled text, kept as metadata for
the reviewer instead, sanitised for the `Content-Disposition` header.

`ObjectKey` is stored rather than recomputed, so a future change to the key layout does not orphan
every object written before it.

Original filename, content type, byte size, hash and uploaded-at are all kept, because a receipt is
evidence under seven-year retention: "is this the file that was uploaded" needs an answer that does not
depend on the object store still being trustworthy. ImpactKids stores none of this, because a photo
does not need it.

## Every file belongs to one purchase, and the link is a key rather than an id

`DbExpenseAttachment.DetailKey` holds `DbExpenseDetail.Key` — **the claimant's own stable handle for a
purchase, not the detail's row id.** The upload carries it as `?detailKey=`.

It cannot be the row id. `Update` replaces a draft's details rather than merging them, so every autosave
gives each detail a fresh `Id`, and a file holding one would come unlinked about two seconds after it was
uploaded. The key is minted in the browser when the purchase is created, sent with the form, and written
back untouched by `WriteDetails`.

A `null` `DetailKey` reads as "evidence for this claim, purchase unstated". It is reachable when somebody
deletes a purchase whose files are still attached: `Update` clears the link rather than throwing evidence
away, because detaching a file is a deliberate act with its own endpoint. The form shows those files under
a warning and the PDF lists them under `—` rather than against purchase 1.

**This link is why the PDF is readable at all.** The files are emailed to finance *beside* the document,
not inside it, so section 3 prints each purchase's filenames and the evidence manifest prints the purchase
number against every file. Without it a reviewer holding four photos called `IMG_4471.jpeg` has to work
out which receipt each one is from the amounts.

## `?inline=1` on the download, for images only

The download endpoint serves `Content-Disposition: attachment` by default. `?inline=1` switches that to
`inline` **for `image/jpeg`, `image/png` and `image/webp` and nothing else** — the allowlist is
`AttachmentEndpoints.PreviewableInline`, and it is checked against the *detected* content type, which is
what the bytes were verified to be at upload rather than what the upload claimed.

That exists so the form's preview modal can render a receipt in an `<img>`, which it cannot do against a
response marked `attachment`. Somebody photographing four dockets on a phone gets four files whose names
say nothing, and is then asked which supplier and which date each one is.

**Widening the allowlist would undo the reason the header is there.** This origin serves whatever a
stranger uploaded; a PDF rendered in place is a scripting host running same-origin. `X-Content-Type-Options:
nosniff` is unconditional and is what makes even the image case safe — it forbids the browser from
re-interpreting a PNG as anything else. SVG is not accepted at upload and must not be added to either list.

## What a claimant can change after a file is stored

Two things, both on a draft they own, and both leave the bytes alone:

- **Remove** soft-deletes the row. Nothing in this app destroys uploaded bytes — the global query
  filter hides the row and the submission stops referencing it, which is exactly what the claimant was
  promised. Seven-year retention applies to the evidence, not to the row that mentions it.
- **Refile** (`PATCH .../attachments/{id}/kind`) changes only `AttachmentKind`. Uploads default to
  `SupplierReceipt` and the claimant corrects the odd one, so getting it wrong is the ordinary case
  rather than the exception; without this the only remedy was removing the receipt and pushing the same
  bytes back up a phone connection.

The kind is not cosmetic. `AttachmentKind` is `SupplierReceipt | BankOrCardStatement | QuoteOrOrder |
Other`, and the distinction that carries weight is **whether the file came from the place the purchase
was made**. A purchase whose files are all bank lines proves the money moved and says nothing about what
it bought, and that is exactly what opens section 5's missing-receipt declaration — so a mislabelled file
is the difference between a form that submits and one that does not.

There is deliberately **no "itemised receipt" kind**. Whether the evidence itemises is now asked outright
per purchase (`ExpenseDetail.ReceiptIsItemised`) rather than inferred from a dropdown somebody picked
before choosing the file.

**Both are drafts-only, owner-only**, for the same reason uploads are. A submitted claim is what a
reviewer is reading, and relabelling or withdrawing its evidence afterwards needs a person rather than
a control on a form.

Re-uploading identical bytes is the third path to the same place: it un-deletes the existing row and
applies the kind that came with the new upload, rather than tripping the unique index on
`(SubmissionId, ContentHash)` — that index does not know about soft deletes, so a filtered duplicate
check would turn an ordinary change of mind into a 500.

## Rate limits and body ceilings

Per-IP fixed windows, because the pages are anonymous and nothing else stands between the open internet
and the object store:

| Policy | Limit | Applies to |
|---|---|---|
| `uploads` | 30/min | attachment upload, download, remove and refile |
| `submissions` | 120/min | the gRPC service (create, update, submit) |
| `renders` | 20/min | the PDF endpoint |

Rejections answer **429** with `Retry-After: 60` and a readable message, not the default 503 — a
throttled client needs to be told that, and a 503 reads as "the server is broken" in a log. Queue length
is zero: holding an over-limit request ties up a connection and delays the one answer the caller needs.

**Be honest about what this is.** Partitioning by remote IP is a speed bump against casual abuse, not a
defence against a determined attacker. `UseForwardedHeaders` runs first so the partition key is the
caller rather than YARP, which means the ingress has to be trusted to set the header, and a shared NAT
puts a whole office in one bucket. Real protection needs authentication or something in front.

### Three ceilings, and they have to be ordered

| Where | Limit | Why |
|---|---|---|
| App (`MaxBytesPerFile`) | 20 MB | The one that produces a readable message |
| gRPC service Kestrel | 24 MB | A margin, so the app's check answers first |
| **YARP Kestrel** | 24 MB | See below |

**The proxy needs its own, and it is not optional.** Without it a 25 MB upload came back **502**: the
service refuses an over-size body early from its `Content-Length` and closes without draining it, YARP is
still writing and sees a broken pipe, and reports a bad gateway. The caller learns nothing and the log
points at the proxy rather than at the file. Measured 2026-08-31 — 22 MB gave a clean 413 with the app's
own message while 25 MB gave 502; with the proxy limit set, both give 413.

## Deployment, outside this repo

[How this deploys](../infrastructure/deployment.md) is the full picture. What matters from here:

Production runs **its own SeaweedFS**, in its own namespace, sharing nothing with ImpactKids' — so the
deployed config differs from local only in the endpoint. Sharing one instance was considered and
rejected on 2026-09-01: each is one small container, so sharing saves nothing worth the coupling.

- The bucket creates itself. `EnsureBucketAsync()` runs at startup and `PutAsync` creates it on demand
  anyway, which is why the app's SeaweedFS identity carries `Admin`.
- The offsite backup needs its **own** Backblaze bucket and application key. Adding `Read:accounting`
  to ImpactKids' backup identity would do nothing — that identity lives in the other SeaweedFS.
- **SeaweedFS reads its identities once at startup.** A changed Secret updates nothing until the pod
  restarts, with no error either way — so an identity change must be followed by a rollout restart.
