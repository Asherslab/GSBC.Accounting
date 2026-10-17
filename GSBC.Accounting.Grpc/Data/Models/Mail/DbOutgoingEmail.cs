namespace GSBC.Accounting.Grpc.Data.Models.Mail;

/// <summary>
/// One email the app owes somebody, written in the same transaction as the event that owes it.
/// </summary>
/// <remarks>
/// <b>An outbox, not a send-from-the-request.</b> <c>Submit</c> writes these rows beside the submitted
/// claim and returns; <c>OutgoingEmailService</c> sends them. So a Graph outage, an expired client secret
/// or a pod restart between the claim committing and the email leaving cannot lose the email - the row
/// is still here, unsent, and the next pass tries again. Sending inline would make the claimant wait on
/// Microsoft, and would leave a submitted claim nobody was told about whenever the send failed after the
/// commit.
/// <para>
/// The message is built at send time from the submission, not stored here. The claim cannot change once
/// submitted, so building late loses nothing, and a stored copy would be a second home for the claimant's
/// details and the PDF.
/// </para>
/// <para>
/// No <c>Deleted</c> column and no soft-delete filter. This row is a delivery record, not the financial
/// record; the claim and its evidence are what the seven-year rule protects, and they are untouched by
/// anything here. Sent rows are kept anyway - they are what answers "was accounts ever told about
/// RE-2026-0142, and when".
/// </para>
/// </remarks>
public class DbOutgoingEmail
{
    public required Guid Id { get; set; }

    public required Guid SubmissionId { get; set; }

    public required OutgoingEmailKind Kind { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null until Graph accepted it. The only column the sender's "is there work" query reads.</summary>
    public DateTimeOffset? SentAt { get; set; }

    public int Attempts { get; set; }

    /// <summary>Not before this. Pushed out after each failure, so a dead credential is not hammered.</summary>
    public DateTimeOffset NextAttemptAt { get; set; }

    /// <summary>
    /// A replica working on this row holds it until then. Taken with a conditional update, so two
    /// replicas cannot both send the same email; a replica that dies mid-send lets the lease lapse and
    /// another picks the row up.
    /// </summary>
    public DateTimeOffset? LeaseUntil { get; set; }

    /// <summary>The last failure, for whoever reads the table when an email has not arrived.</summary>
    public string? LastError { get; set; }
}

public enum OutgoingEmailKind
{
    /// <summary>The claim, with the combined PDF, to the accounts team.</summary>
    ClaimToAccounts,

    /// <summary>"We have received your claim", to the claimant.</summary>
    ReceiptToClaimant
}
