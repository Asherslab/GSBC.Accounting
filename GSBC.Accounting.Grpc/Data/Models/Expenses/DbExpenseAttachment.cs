using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Data.Models.Expenses;

/// <summary>One uploaded file. The bytes live in the object store; this is what points at them.</summary>
public class DbExpenseAttachment
{
    public required Guid Id { get; set; }

    public required Guid SubmissionId { get; set; }

    [MapperIgnore]
    public DbExpenseSubmission? Submission { get; set; }

    /// <summary>The line this evidences, or null when it belongs to the submission as a whole.</summary>
    public Guid? LineId { get; set; }

    /// <summary>
    /// As the claimant's device named it. Displayed to a reviewer and never used to build an object
    /// key - it is attacker-controlled text, and a key built from it would be a path-traversal question
    /// nobody needs to answer.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>What the bytes were detected to be, not what the upload declared.</summary>
    public required string ContentType { get; set; }

    public required long ByteSize { get; set; }

    /// <summary>Full SHA-256, hex. Also what <see cref="ObjectKey"/> is built from.</summary>
    public required string ContentHash { get; set; }

    /// <summary>
    /// Stored rather than recomputed. Recomputing it would tie every read to today's key scheme, so a
    /// future change to the layout would orphan every object written before it.
    /// </summary>
    public required string ObjectKey { get; set; }

    public required AttachmentKind Kind { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Soft delete, and it means the object stays in the store too. Seven-year retention applies to the
    /// evidence, not only to the row that mentions it.
    /// </summary>
    [MapperIgnore]
    public bool Deleted { get; set; }
}
