---
title: How the app sends email
kind: reference
status: current
module: infrastructure
verified: 2026-10-17
code:
  - GSBC.Accounting.Grpc/Features/Mail
  - GSBC.Accounting.Grpc/Features/Pdf/ClaimPdf.cs
  - GSBC.Accounting.Grpc/Data/Models/Mail/DbOutgoingEmail.cs
  - GSBC.Accounting.Grpc/Features/Expenses/ExpenseSubmissionServices/Submit.cs
  - Charts/accounting/templates/grpc
  - GSBC.Accounting.AppHost/AppHost.cs
---

# How the app sends email

A submitted claim produces two emails, both sent as `expenses@baptist.com.au` through Microsoft Graph.
This covers what they are, why delivery goes through a table rather than happening in the request, and
the Microsoft 365 setup it needs — written for whoever holds the tenant's admin account.

**Do not grant `Mail.Send` to the app registration in Entra.** That consent covers every mailbox in the
tenant, and Exchange's scoping is added to it, never subtracted from it: an app holding both can still
send as the senior pastor. The permission is granted in Exchange instead, scoped to the one mailbox — see
[step 3](#3-let-the-app-send-as-that-mailbox-and-nothing-else).

## The two emails

| | To | Reply-To | Attachment |
|---|---|---|---|
| The claim | `Mail:AccountsAddress` — `accounts@baptist.com.au` | the claimant | the combined PDF, unless it is over `Mail:MaxAttachmentBytes` |
| The receipt | the claimant's `SubmitterEmail` | `accounts@baptist.com.au` | none |

The accounts team decides what goes on to Xero; the app does not send there. That was the accounts
team's call on 2026-10-17, and it is why the address is configuration rather than code.

**The claim's PDF is the form with every receipt bound in after it** (`ClaimPdf`), because Xero's bills
inbox reportedly makes a separate draft bill of every attachment on an email. Photos — HEIC included —
are re-encoded to JPEG at 2000 px on the long edge with location data stripped; PDF receipts are merged as
uploaded. A file that cannot be read gets a placeholder page naming its hash rather than failing the
claim. The PDF download endpoint serves the same file, so the link in the email and the attachment agree.

The receipt deliberately carries no PDF and no link. It goes to an address the claimant typed, and a typo
should cost them a missing confirmation, not hand a stranger their card digits.

A claim over `Mail:MaxAttachmentBytes` (24 MB) goes with the link alone and says so. The default is set by
Exchange Online, not by Graph: the 35 MB send and 36 MB receive limits apply to the *encoded* message,
and base64 adds a third. To raise it, raise both mailboxes first ([step 5](#5-optional-allow-larger-claims)).

## Delivery goes through a table

**`Submit` never talks to Microsoft.** It writes two `OutgoingEmails` rows in the same `SaveChanges` that
marks the claim submitted, then wakes `OutgoingEmailService`, which sends them. So:

- a submitted claim always has its emails owed, and nothing is owed for a claim that failed to submit;
- a Graph outage, an expired secret or a pod restart delays the email and never loses it;
- the claimant is not kept waiting on Graph after pressing Submit.

The sender retries with exponential backoff — 1, 2, 4 … 256 minutes, then every six hours — and gives up
after 40 attempts, about eight days, with a `Critical` log naming the row. Every failure writes Graph's own
error text to `LastError`.

**Scaling out is still safe.** A row is only sent by the replica whose conditional update took its
`LeaseUntil`, so two replicas cannot both send it. The one duplicate this cannot prevent is Graph accepting
a message and the response being lost; the row stays unsent and is sent again. A unique index on
`(SubmissionId, Kind)` stops a claim ever being queued twice.

Checking on it:

```sql
select "Kind", "CreatedAt", "SentAt", "Attempts", "NextAttemptAt", "LastError"
from "OutgoingEmails" order by "CreatedAt" desc limit 20;
```

To resend after fixing the cause, set `"Attempts" = 0, "NextAttemptAt" = now()` on the row. To send one
again that already went, also set `"SentAt" = null`.

### Missing configuration does not stop the pod

`mail-secret` is mounted `optional: true`, the opposite of `attachments-secret`. Without it the service
starts, logs one `Error` saying so, accepts claims and queues their emails. Create the secret, restart the
pod, and the backlog goes out. A pod that refused to start would stop anybody submitting at all.

## Locally

The AppHost sets `Mail:Transport=Directory`. Every message is written to
`$TMPDIR/gsbc-accounting-mail/` as an `.html` file with the envelope printed at the top, and the PDF
beside it — nothing reaches the real accounts team. Mock claims use `@example.com` addresses and carry
`[MOCK]` in the subject.

To try real sending from the laptop, put `Mail:Transport` = `Graph`, `Mail:TenantId`, `Mail:ClientId`,
`Mail:ClientSecret` and — strongly advised — `Mail:AccountsAddress` = your own address in the
**AppHost's** user secrets. The AppHost passes them through.

## Microsoft 365 setup

Done once, by a tenant admin. Steps 1 and 2 are in browser portals; 3 and 5 are Exchange Online
PowerShell (`Install-Module ExchangeOnlineManagement`, then `Connect-ExchangeOnline`).

### 1. Create the shared mailbox

Microsoft 365 admin centre (`admin.cloud.microsoft`) → left menu **Show all** if the menu is short →
**Teams & groups → Shared mailboxes → Add a shared mailbox**. Name `Expenses`, email
`expenses@baptist.com.au`. (The Exchange admin centre, `admin.exchange.microsoft.com`, can do the same
under Recipients → Mailboxes; either works.)

A shared mailbox needs no licence. Its Sent Items keep a copy of every email the app sends, which is the
record of what the accounts team was sent; give the treasurer Read access to it if that is useful.

Then in the Microsoft 365 admin centre → **Users → Active users → Expenses → Block sign-in**. Nobody signs
in to this mailbox; the app does not use a password, so blocking sign-in costs nothing.

### 2. Register the app

Entra admin centre (`entra.microsoft.com`) → **App registrations → New registration**. Name
`GSBC Expenses mailer`, single tenant, no redirect URI.

Record, from its **Overview**:

| Overview field | Becomes |
|---|---|
| Application (client) ID | `Mail__ClientId` |
| Directory (tenant) ID | `Mail__TenantId` |

**Certificates & secrets → New client secret.** Copy the **Value** immediately — it is shown once — into
`Mail__ClientSecret`. Not the **Secret ID** beside it, which looks more like a credential and does not work.
Put the expiry date in a calendar: when it lapses, emails queue with `AADSTS7000222` in `LastError` until
a new secret is in `mail-secret` and the pod has restarted.

**API permissions: add nothing**, and remove `User.Read` if you like (it is delegated and unused). See the
warning at the top of this page.

Finally, **Enterprise applications** (not App registrations) → `GSBC Expenses mailer` → copy its
**Object ID**. That is the service principal's id, which step 3 needs, and it is *not* the Object ID shown
on the app registration.

### 3. Let the app send as that mailbox and nothing else

```powershell
Connect-ExchangeOnline

# Exchange's pointer to the Entra app. -ObjectId is the ENTERPRISE APPLICATION's Object ID.
New-ServicePrincipal -AppId <client id> -ObjectId <enterprise app object id> -DisplayName "GSBC Expenses mailer"

# The one mailbox.
New-ManagementScope -Name "GSBC Expenses mailbox" `
  -RecipientRestrictionFilter "PrimarySmtpAddress -eq 'expenses@baptist.com.au'"

New-ManagementRoleAssignment -App <client id> -Role "Application Mail.Send" `
  -CustomResourceScope "GSBC Expenses mailbox"

# Both must agree: True for the shared mailbox, False for anybody else's.
Test-ServicePrincipalAuthorization -Identity <client id> -Resource expenses@baptist.com.au
Test-ServicePrincipalAuthorization -Identity <client id> -Resource <any staff address>
```

This is Exchange's *RBAC for Applications*, which replaces Application Access Policies. Changes can take
up to two hours to take effect; until they do, Graph answers `ErrorAccessDenied` and the emails wait.

### 4. Give the cluster the credentials

```bash
kubectl -n accounting create secret generic mail-secret \
  --from-literal=Mail__TenantId='<tenant id>' \
  --from-literal=Mail__ClientId='<client id>' \
  --from-literal=Mail__ClientSecret='<secret value>'

kubectl -n accounting rollout restart deployment/grpc-deployment
```

Rotating the secret is the same two commands with `kubectl create ... --dry-run=client -o yaml | kubectl
apply -f -`. The pod reads it once, at start.

### 5. Optional: allow larger claims

Only if claims over 24 MB turn up — a dozen multi-page scanned invoices, say:

```powershell
Set-Mailbox expenses@baptist.com.au -MaxSendSize 150MB
Set-Mailbox accounts@baptist.com.au -MaxReceiveSize 150MB
```

then raise `config.mail.max_attachment_bytes` in the chart. About 100 MB of PDF fits under 150 MB once
encoded; Graph's own ceiling for an uploaded attachment is 150 MB.

## When an email has not arrived

Read `LastError` on its row first. The usual ones:

| `LastError` contains | Means |
|---|---|
| `ErrorAccessDenied` | Step 3 not done, not yet propagated, or scoped to a different address than `Mail:Sender` |
| `AADSTS7000215` | The secret is wrong — usually the Secret ID was pasted instead of the Value |
| `AADSTS7000222` | The secret has expired |
| `AADSTS700016` | The client id or tenant id is wrong |
| `ErrorInvalidUser` | `Mail:Sender` names an address that does not exist in the tenant |
| `MailboxNotEnabledForRESTAPI` | `Mail:Sender` is a user with no Exchange Online mailbox |

No row at all for a submitted claim means it was submitted before this existed. No `LastError` and no
`SentAt` means the sender is not running — the pod log says why at startup.
