namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One row of section 3's seven-column table. Ordered, because the paper form is.
/// </summary>
/// <remarks>
/// Columns 4 to 7 are identical on both forms. Columns 1 to 3 differ, and column 1 differs in
/// <b>type</b>, not merely in label: the debit card form's `Item` is text - one card transaction
/// itemised into its parts - while the reimbursement form's `Date` is a date, one row per receipt. So
/// both <see cref="ItemDescription"/> and <see cref="LineDate"/> exist and are nullable, and each page
/// requires whichever one its own form prints.
/// <para>
/// Neither table's printed row count is a limit. The debit card form has four blank rows and the
/// reimbursement form five; that is what fits on the page. Do not cap the web form at either.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ExpenseLine : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid SubmissionId { get; init; }

    /// <summary>Position in the table, zero-based. The form is a list and its order is meaningful.</summary>
    public required int Ordinal { get; init; }

    /// <summary>Column 1 on the debit card form: `Item`. Null on a reimbursement line.</summary>
    public string? ItemDescription { get; init; }

    /// <summary>Column 1 on the reimbursement form: `Date`. Null on a debit card line.</summary>
    public DateTime? LineDate { get; init; }

    /// <summary>Column 2: `Qty / details` / `Supplier &amp; item / service`.</summary>
    public string? Details { get; init; }

    /// <summary>Column 3: `Church purpose / user` / `Purpose / ministry`.</summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// Column 4, printed `☐ Attached / ☐ Missing`. An enum rather than two bools because the paper
    /// checkboxes are mutually exclusive. <see cref="EvidenceStatus.Missing"/> on any line is what
    /// unlocks section 5.
    /// </summary>
    public required EvidenceStatus Evidence { get; init; }

    /// <summary>Column 5: `Gross incl. GST`.</summary>
    public decimal GrossAmount { get; init; }

    /// <summary>Column 6: `GST shown`. Optional on the form - a supplier that is not registered shows none.</summary>
    public decimal? GstAmount { get; init; }

    /// <summary>
    /// Column 7, printed `100% /       %` - the paper form pre-prints 100 and the writer overrides it,
    /// so 100 is the default rather than a value someone chose.
    /// </summary>
    public decimal ChurchUsePercent { get; init; } = 100m;
}

/// <summary>Section 3, column 4. `☐ Attached / ☐ Missing`.</summary>
[ProtoContract]
public enum EvidenceStatus
{
    Attached,
    Missing
}
