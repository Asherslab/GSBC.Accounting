using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// What a form page binds to while it is being filled in. Mutable, unlike the contract records, and
/// tolerant of every half-finished state a person types their way through.
/// </summary>
/// <remarks>
/// One model serves both forms, for the same reason there is one aggregate: the structure is shared.
/// The <b>wording</b> is not, and this class holds none of it - every label, question and declaration
/// comes from <see cref="ExpenseFormWording"/>, keyed by kind.
/// <para>
/// Dates are <see cref="DateOnly"/> here because the form asks for a day, not an instant. They become
/// UTC <c>DateTime</c> at midnight when the request is built - see <see cref="ToUtc"/>.
/// </para>
/// <para>
/// The totals computed here are <b>for display only</b>. The server recomputes all of them and writes
/// its own; if the two ever disagree the server is right. They are duplicated rather than fetched
/// because a total that only updates after a round trip makes a form feel broken.
/// </para>
/// </remarks>
public class ExpenseFormModel
{
    public required SubmissionKind Kind { get; init; }

    // ---- Section 1, shared ----
    public string? SubmitterName { get; set; }
    public DateOnly? FormDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public ClaimantRole? Role { get; set; }
    public string? RoleOther { get; set; }
    public string? MinistryDepartment { get; set; }

    // ---- Section 1, debit card only ----
    public string? CardLastFourDigits { get; set; }
    public DateOnly? TransactionDate { get; set; }
    /// <summary>
    /// A TimeOnly here so the page can use &lt;input type="time"&gt;, which binds TimeOnly and refuses a
    /// string. The contract keeps it as text because the paper form prints a bare `Time: ________` rule
    /// rather than a picker, and a claimant may well write "about 4pm".
    /// </summary>
    public TimeOnly? TransactionTime { get; set; }
    public string? SupplierMerchant { get; set; }
    public decimal? AmountCharged { get; set; }
    public string? BankReference { get; set; }

    // ---- Section 1, reimbursement only ----
    public string? ContactPhoneEmail { get; set; }
    public DateOnly? ExpensePeriodFrom { get; set; }
    public DateOnly? ExpensePeriodTo { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentMethodOther { get; set; }
    public bool? BankDetailsOnFile { get; set; }

    // ---- Section 2 ----
    public string? PurposeActivity { get; set; }
    public string? EventProject { get; set; }
    public string? PriorApprovalBy { get; set; }
    public DateOnly? ApprovalDate { get; set; }
    public string? PurposeNarrative { get; set; }

    // ---- Section 3 ----
    public List<ExpenseLineModel> Lines { get; set; } = [new()];

    public decimal LessPersonalAmount { get; set; }

    // ---- Section 4 ----

    /// <summary>
    /// The six answers, indexed 0..5. null is UNANSWERED and stays null until somebody chooses - it is a
    /// different fact from "No", and the one a reviewer needs to see.
    /// </summary>
    public bool?[] Compliance { get; set; } = new bool?[6];

    public string? ComplianceDetails { get; set; }

    /// <summary>Rows of the detail table a Yes on question 1 or 2 opens. Debit card form.</summary>
    public List<AttendeeModel> Attendees { get; set; } = [];

    /// <summary>Rows of the trip record a Yes on question 1 opens. Reimbursement form.</summary>
    public List<TripModel> Trips { get; set; } = [];

    /// <summary>
    /// True when the form's own detail table should be shown. The debit card form opens it on question
    /// 1 (travel) or 2 (meals); the reimbursement form only on question 1 (motor vehicle), because its
    /// question 2 asks for attendees in the free-text block instead.
    /// </summary>
    public bool DetailTableOpen =>
        Kind == SubmissionKind.DebitCardPurchase
            ? Compliance[0] == true || Compliance[1] == true
            : Compliance[0] == true;

    /// <summary>Any Yes opens the shared free-text block.</summary>
    public bool ComplianceDetailsOpen => Compliance.Any(x => x == true);

    // ---- Section 5 ----

    public string? MissingSupplier { get; set; }
    public DateOnly? MissingDate { get; set; }
    public decimal? MissingAmount { get; set; }
    public string? MissingReason { get; set; }
    public bool MissingDeclared { get; set; }

    // ---- Section 6 ----

    /// <summary>The five declarations, indexed 0..4.</summary>
    public bool[] Declarations { get; set; } = new bool[5];

    public string? SignatureName { get; set; }

    public bool IsMockData { get; set; }

    // ---- Display totals. The server's are authoritative. ----

    public decimal GrossTotal => Round(Lines.Sum(x => Round(x.GrossAmount ?? 0m)));

    public decimal GstTotal => Round(Lines.Sum(x => Round(x.GstAmount ?? 0m)));

    public decimal NetTotal => Round(GrossTotal - Round(LessPersonalAmount));

    /// <summary>
    /// True when the debit card form's lines do not add up to what the card was charged. The
    /// reimbursement form has no equivalent, because nothing external states its total.
    /// </summary>
    /// <remarks>
    /// Shown as a warning while typing rather than a block - a half-entered form is nearly always
    /// mismatched, and a page that shouts at every keystroke is a page people stop reading. The server
    /// refuses the mismatch at submit; this is only the early warning.
    /// </remarks>
    public bool ChargeMismatch =>
        Kind == SubmissionKind.DebitCardPurchase
        && AmountCharged is { } charged
        && Lines.Any(x => x.GrossAmount is not null)
        && Round(charged) != GrossTotal;

    /// <summary>Any line marked Missing is what unlocks section 5's declaration.</summary>
    public bool HasMissingEvidence => Lines.Any(x => x.Evidence == EvidenceStatus.Missing);

    public void AddLine() => Lines.Add(new ExpenseLineModel());

    /// <summary>
    /// Removes a line, but never the last one. An empty table has no "add" affordance in the row area
    /// and reads as a broken page rather than an empty one.
    /// </summary>
    public void RemoveLine(ExpenseLineModel line)
    {
        if (Lines.Count > 1)
            Lines.Remove(line);
    }

    public CreateExpenseSubmissionRequest ToCreateRequest() => new()
    {
        Kind = Kind,
        SubmitterName = Trim(SubmitterName),
        FormDate = ToUtc(FormDate),
        Role = Role,
        RoleOther = Trim(RoleOther),
        MinistryDepartment = Trim(MinistryDepartment),

        CardLastFourDigits = Trim(CardLastFourDigits),
        TransactionDate = ToUtc(TransactionDate),
        TransactionTime = TransactionTime?.ToString("HH:mm"),
        SupplierMerchant = Trim(SupplierMerchant),
        AmountCharged = AmountCharged,
        BankReference = Trim(BankReference),

        ContactPhoneEmail = Trim(ContactPhoneEmail),
        ExpensePeriodFrom = ToUtc(ExpensePeriodFrom),
        ExpensePeriodTo = ToUtc(ExpensePeriodTo),
        PaymentMethod = PaymentMethod,
        PaymentMethodOther = Trim(PaymentMethodOther),
        BankDetailsOnFile = BankDetailsOnFile,

        PurposeActivity = Trim(PurposeActivity),
        EventProject = Trim(EventProject),
        PriorApprovalBy = Trim(PriorApprovalBy),
        ApprovalDate = ToUtc(ApprovalDate),
        PurposeNarrative = Trim(PurposeNarrative),

        LessPersonalAmount = LessPersonalAmount,

        ComplianceQ1 = Compliance[0],
        ComplianceQ2 = Compliance[1],
        ComplianceQ3 = Compliance[2],
        ComplianceQ4 = Compliance[3],
        ComplianceQ5 = Compliance[4],
        ComplianceQ6 = Compliance[5],
        ComplianceDetails = Trim(ComplianceDetails),

        Declaration1 = Declarations[0],
        Declaration2 = Declarations[1],
        Declaration3 = Declarations[2],
        Declaration4 = Declarations[3],
        Declaration5 = Declarations[4],

        SignatureName = Trim(SignatureName),
        IsMockData = IsMockData,

        // Only sent when the section is actually open - a submission with no missing evidence must not
        // carry an empty declaration that a reviewer would read as one somebody made.
        MissingReceipt = HasMissingEvidence
            ? new MissingReceiptDeclaration
            {
                SubmissionId = Guid.Empty,
                Supplier = Trim(MissingSupplier),
                Date = ToUtc(MissingDate),
                Amount = MissingAmount,
                Reason = Trim(MissingReason),
                Declared = MissingDeclared
            }
            : null,

        Attendees = Kind == SubmissionKind.DebitCardPurchase && DetailTableOpen
            ? Attendees.Select((a, i) => new ExpenseAttendee
            {
                SubmissionId = Guid.Empty,
                Ordinal = i,
                Date = ToUtc(a.Date),
                Person = Trim(a.Person),
                Relationship = Trim(a.Relationship),
                Amount = a.Amount,
                PrivateShare = a.PrivateShare,
                Reason = Trim(a.Reason)
            }).ToList()
            : [],

        Trips = Kind == SubmissionKind.ExpenseReimbursement && DetailTableOpen
            ? Trips.Select((t, i) => new ExpenseTrip
            {
                SubmissionId = Guid.Empty,
                Ordinal = i,
                Date = ToUtc(t.Date),
                From = Trim(t.From),
                To = Trim(t.To),
                BusinessKm = t.BusinessKm,
                ApprovedRate = t.ApprovedRate,
                Purpose = Trim(t.Purpose)
            }).ToList()
            : [],

        Lines = Lines.Select((line, index) => new ExpenseLine
        {
            SubmissionId = Guid.Empty,
            Ordinal = index,
            ItemDescription = Trim(line.ItemDescription),
            LineDate = ToUtc(line.LineDate),
            Details = Trim(line.Details),
            Purpose = Trim(line.Purpose),
            Evidence = line.Evidence,
            GrossAmount = line.GrossAmount ?? 0m,
            GstAmount = line.GstAmount,
            ChurchUsePercent = line.ChurchUsePercent ?? 100m
        }).ToList()
    };

    /// <summary>
    /// A calendar day becomes midnight UTC. <c>DateTimeKind.Utc</c> is stated explicitly rather than
    /// left to default: the server rejects anything it cannot treat as offset zero, and a
    /// <c>Kind.Unspecified</c> value silently acquires the machine's local offset on the way in.
    /// </summary>
    private static DateTime? ToUtc(DateOnly? date) =>
        date is null ? null : DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    /// <summary>Whitespace-only becomes null, so an untouched field is absent rather than blank.</summary>
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);
}

/// <summary>One editable row of the debit card form's section 4 table.</summary>
public class AttendeeModel
{
    public Guid Key { get; } = Guid.NewGuid();
    public DateOnly? Date { get; set; }
    public string? Person { get; set; }
    public string? Relationship { get; set; }
    public decimal? Amount { get; set; }
    public decimal? PrivateShare { get; set; }
    public string? Reason { get; set; }
}

/// <summary>One editable row of the reimbursement form's trip record.</summary>
public class TripModel
{
    public Guid Key { get; } = Guid.NewGuid();
    public DateOnly? Date { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public decimal? BusinessKm { get; set; }
    public decimal? ApprovedRate { get; set; }
    public string? Purpose { get; set; }
}

/// <summary>One editable row of section 3. Every money field is nullable so an empty cell stays empty.</summary>
public class ExpenseLineModel
{
    /// <summary>Stable across re-renders, so Blazor's @key does not reuse a row's DOM for another row.</summary>
    public Guid Key { get; } = Guid.NewGuid();

    /// <summary>Column 1 on the debit card form.</summary>
    public string? ItemDescription { get; set; }

    /// <summary>Column 1 on the reimbursement form.</summary>
    public DateOnly? LineDate { get; set; }

    public string? Details { get; set; }
    public string? Purpose { get; set; }

    public EvidenceStatus Evidence { get; set; } = EvidenceStatus.Attached;

    public decimal? GrossAmount { get; set; }
    public decimal? GstAmount { get; set; }

    /// <summary>The paper form pre-prints 100, so 100 is the default rather than a choice someone made.</summary>
    public decimal? ChurchUsePercent { get; set; } = 100m;
}
