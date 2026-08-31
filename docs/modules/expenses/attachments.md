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

## What a claimant can change after a file is stored

Two things, both on a draft they own, and both leave the bytes alone:

- **Remove** soft-deletes the row. Nothing in this app destroys uploaded bytes — the global query
  filter hides the row and the submission stops referencing it, which is exactly what the claimant was
  promised. Seven-year retention applies to the evidence, not to the row that mentions it.
- **Refile** (`PATCH .../attachments/{id}/kind`) changes only `AttachmentKind`. The kind is picked in a
  dropdown *before* the file is chosen, so getting it wrong is the ordinary case rather than the
  exception; without this the only remedy was removing the receipt and pushing the same bytes back up a
  phone connection.

The kind is not cosmetic: `Submit` refuses a claim carrying no `ItemisedReceipt` or `TaxInvoice`, so a
mislabelled file is the difference between a form that submits and one that does not.

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

Production is **one SeaweedFS with two buckets** — ImpactKids' `photos` and this app's `accounting` —
so the deployed config differs from local only in the endpoint. Two follow-ons belong to whoever
deploys it:

- The Backblaze backup identity is bucket-scoped (`Read:photos`, `List:photos`) and needs `accounting`
  added, plus a second `rclone copy` in the CronJob.
- **SeaweedFS reads its identities once at startup.** A changed Secret updates nothing until the pod
  restarts, with no error either way — so an identity change must be followed by a rollout restart.
