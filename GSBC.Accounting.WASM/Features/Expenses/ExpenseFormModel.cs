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
    /// <summary>
    /// Which of the two forms this is, or null until the claimant has answered question zero.
    /// </summary>
    /// <remarks>
    /// <b>Nullable, and settable.</b> The kind used to come from the URL, so a page could state it at
    /// construction; it is now the form's first question, and a question has no answer until somebody
    /// gives one. A non-nullable property would not do: <c>DebitCardPurchase</c> is the zero value of
    /// <see cref="SubmissionKind"/>, so an unanswered form would silently claim to be a card purchase
    /// and print the card form's wording at somebody who has not chosen it.
    /// <para>
    /// Nothing below section 0 renders while this is null - not because of layout, but because every
    /// caption, question and declaration on the page comes from <see cref="ExpenseFormWording"/> keyed
    /// by kind, and before the answer there is no correct text to show.
    /// </para>
    /// </remarks>
    public SubmissionKind? Kind { get; set; }

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

    /// <summary>
    /// One entry per purchase. <b>Starts empty, unlike the line table it replaced, which always had a
    /// blank row.</b> A detail is created by attaching a receipt, so a form with no attachments has
    /// nothing to show and an empty panel would be a set of questions about a receipt nobody has picked.
    /// </summary>
    public List<ExpenseDetailModel> Details { get; set; } = [];

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
    public bool DetailTableOpen => Kind switch
    {
        SubmissionKind.DebitCardPurchase => Compliance[0] == true || Compliance[1] == true,
        SubmissionKind.ExpenseReimbursement => Compliance[0] == true,
        // Unanswered. Section 4 is not on screen yet, so neither is its table.
        _ => false
    };

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

    public decimal GrossTotal => Round(Details.Sum(x => Round(x.TotalIncGst ?? 0m)));

    public decimal GstTotal => Round(Details.Sum(x => Round(x.GstAmount ?? 0m)));

    /// <summary>
    /// Summed from the details rather than typed once at the foot of the form, which is what it used to
    /// be. Each receipt states its own personal portion, so there is one place the number comes from.
    /// </summary>
    public decimal LessPersonalAmount => Round(Details.Sum(x => Round(x.NonReimbursedAmount ?? 0m)));

    public decimal NetTotal => Round(GrossTotal - LessPersonalAmount);

    /// <summary>
    /// True when the debit card form's receipts do not add up to what the card was charged. The
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
        && Details.Any(x => x.TotalIncGst is not null)
        && Round(charged) != GrossTotal;

    /// <summary>
    /// True when some purchase is evidenced only by a bank line or a screenshot - nothing from the place
    /// it was bought. That is what unlocks section 5's declaration.
    /// </summary>
    /// <remarks>
    /// <b>A function of the attachments, so the page has to pass them in.</b> The claimant already said
    /// what each file is when they attached it, and asking a second time - a "did you have a receipt?"
    /// tickbox - is how two answers end up disagreeing about the same claim.
    /// <para>
    /// A detail with no files at all is not this. It is a form somebody is midway through, and it opens
    /// nothing; Submit refuses it separately.
    /// </para>
    /// </remarks>
    public bool HasMissingEvidence(IReadOnlyList<ExpenseAttachment> attachments) =>
        Details.Any(detail =>
        {
            List<ExpenseAttachment> files =
                attachments.Where(x => x.DetailKey == detail.Key).ToList();

            return files.Count > 0 && files.All(x => x.Kind != AttachmentKind.SupplierReceipt);
        });

    /// <summary>
    /// True when re-answering question zero as <paramref name="kind"/> would throw away something the
    /// claimant has already said. False on an untouched form, where the switch costs nothing and does
    /// not need confirming.
    /// </summary>
    public bool KindChangeWouldClear(SubmissionKind kind)
    {
        if (Kind is null || Kind == kind)
            return false;

        if (Compliance.Any(x => x is not null) || Declarations.Any(x => x))
            return true;

        if (!string.IsNullOrWhiteSpace(ComplianceDetails) || Attendees.Count > 0 || Trips.Count > 0)
            return true;

        return Kind == SubmissionKind.DebitCardPurchase
            ? !string.IsNullOrWhiteSpace(CardLastFourDigits) || TransactionDate is not null
              || TransactionTime is not null || !string.IsNullOrWhiteSpace(SupplierMerchant)
              || AmountCharged is not null || !string.IsNullOrWhiteSpace(BankReference)
            : !string.IsNullOrWhiteSpace(ContactPhoneEmail) || ExpensePeriodFrom is not null
              || ExpensePeriodTo is not null || PaymentMethod is not null
              || !string.IsNullOrWhiteSpace(PaymentMethodOther) || BankDetailsOnFile is not null;
    }

    /// <summary>
    /// Re-answers question zero, dropping everything that belonged to the form this no longer is.
    /// </summary>
    /// <remarks>
    /// <b>The compliance answers and the declarations go, all of them.</b> Four of the six questions and
    /// four of the five declarations are different text on the two documents, so a tick carried across
    /// would record the claimant as having agreed to wording they were never shown - which is the whole
    /// failure this app exists to avoid. Q4, Q6 and D4 do happen to match word for word and could in
    /// principle survive, but "everything in section 4 and 6 is asked again" is a rule that can be
    /// stated in one line to the person it happens to, and cannot be got subtly wrong later when
    /// somebody edits the wording.
    /// <para>
    /// What stays is everything that means the same thing on both forms: the claimant, the ministry,
    /// section 2, every purchase in section 3 and its receipts, the missing-receipt declaration and the
    /// signature - a purchase is a purchase whichever card paid for it. A
    /// claimant correcting one question should not lose the afternoon's typing.
    /// </para>
    /// <para>
    /// The server does the same clearing for the header fields when it sees the kind change, because a
    /// client is not the place a compliance rule finally lives - see <c>ClearFieldsForOtherKind</c>.
    /// </para>
    /// </remarks>
    public void SwitchKind(SubmissionKind kind)
    {
        if (Kind == kind)
            return;

        Kind = kind;

        if (kind == SubmissionKind.DebitCardPurchase)
        {
            ContactPhoneEmail = null;
            ExpensePeriodFrom = null;
            ExpensePeriodTo = null;
            PaymentMethod = null;
            PaymentMethodOther = null;
            BankDetailsOnFile = null;
        }
        else
        {
            CardLastFourDigits = null;
            TransactionDate = null;
            TransactionTime = null;
            SupplierMerchant = null;
            AmountCharged = null;
            BankReference = null;
        }

        Attendees.Clear();
        Trips.Clear();

        Compliance = new bool?[6];
        ComplianceDetails = null;
        Declarations = new bool[5];
    }

    /// <summary>
    /// Starts a new purchase and returns it, so the caller can attach the files that created it.
    /// </summary>
    public ExpenseDetailModel AddDetail()
    {
        ExpenseDetailModel detail = new();

        Details.Add(detail);

        return detail;
    }

    /// <summary>
    /// Removes a purchase, all of them if asked - unlike the line table this replaced, which always kept
    /// one row.
    /// </summary>
    /// <remarks>
    /// The old rule existed because an empty table has no visible "add" affordance and reads as a broken
    /// page. It does not apply here: a purchase is created by attaching a receipt, so the dropzone is
    /// always on screen and an empty section 3 says "no receipts yet", which is the truth.
    /// <para>
    /// <b>The files are not removed with it.</b> Detaching a file is its own deliberate act, with a
    /// server call behind it; <c>Update</c> clears the link instead, and the file stays on the claim
    /// listed as unfiled. Callers that want the files gone remove them first.
    /// </para>
    /// </remarks>
    public void RemoveDetail(ExpenseDetailModel detail) => Details.Remove(detail);

    /// <summary>
    /// Fills a form back in from a saved draft. The inverse of <see cref="ToCreateRequest"/>.
    /// </summary>
    /// <remarks>
    /// <b>Every field this does not copy is a field a claimant silently loses on resuming.</b> That is
    /// the failure mode to watch for here: nothing errors, the form simply comes back with an empty box
    /// where an answer was, and the person fills it in again without ever knowing. When a field is
    /// added to the form, it needs a line in three places - here, in <see cref="ToCreateRequest"/>, and
    /// in the service's write path.
    /// <para>
    /// <see cref="ExpenseFormModel.Kind"/> comes from the stored submission rather than from the page.
    /// The two agree in every normal case, but the stored one is authoritative: it is what the server
    /// validated and what decides which half of section 1 the row's columns mean.
    /// </para>
    /// </remarks>
    public static ExpenseFormModel FromSubmission(ExpenseSubmission submission)
    {
        ExpenseFormModel model = new()
        {
            Kind = submission.Kind,

            SubmitterName = submission.SubmitterName,
            FormDate = FromUtc(submission.FormDate),
            Role = submission.Role,
            RoleOther = submission.RoleOther,
            MinistryDepartment = submission.MinistryDepartment,

            CardLastFourDigits = submission.CardLastFourDigits,
            TransactionDate = FromUtc(submission.TransactionDate),
            // Stored as free text because the paper form prints a bare rule and a claimant may well
            // have written "about 4pm". Anything the time picker cannot represent is dropped rather
            // than guessed at - a picker showing 00:00 would be a claim nobody made.
            TransactionTime = TimeOnly.TryParse(submission.TransactionTime, out TimeOnly time) ? time : null,
            SupplierMerchant = submission.SupplierMerchant,
            AmountCharged = submission.AmountCharged,
            BankReference = submission.BankReference,

            ContactPhoneEmail = submission.ContactPhoneEmail,
            ExpensePeriodFrom = FromUtc(submission.ExpensePeriodFrom),
            ExpensePeriodTo = FromUtc(submission.ExpensePeriodTo),
            PaymentMethod = submission.PaymentMethod,
            PaymentMethodOther = submission.PaymentMethodOther,
            BankDetailsOnFile = submission.BankDetailsOnFile,

            PurposeActivity = submission.PurposeActivity,
            EventProject = submission.EventProject,
            PriorApprovalBy = submission.PriorApprovalBy,
            ApprovalDate = FromUtc(submission.ApprovalDate),
            PurposeNarrative = submission.PurposeNarrative,

            Compliance =
            [
                submission.ComplianceQ1, submission.ComplianceQ2, submission.ComplianceQ3,
                submission.ComplianceQ4, submission.ComplianceQ5, submission.ComplianceQ6
            ],
            ComplianceDetails = submission.ComplianceDetails,

            Declarations =
            [
                submission.Declaration1 == true, submission.Declaration2 == true,
                submission.Declaration3 == true, submission.Declaration4 == true,
                submission.Declaration5 == true
            ],

            SignatureName = submission.SignatureName,
            IsMockData = submission.IsMockData,

            // No blank-row fallback, unlike the line table this replaced. A draft with no purchases is a
            // draft where nothing has been attached yet, and the dropzone is what it should show.
            //
            // KEY IS COPIED THROUGH, and it is the one field here that cannot be allowed to regenerate:
            // it is what every uploaded file on this draft is filed against, so a fresh Guid would orphan
            // all of them the moment somebody reopened the form.
            Details = submission.Details.Select(detail => new ExpenseDetailModel
            {
                Key = detail.Key,
                Supplier = detail.Supplier,
                PurchaseDate = FromUtc(detail.PurchaseDate),
                Purpose = detail.Purpose,
                ContainsPersonalItems = detail.ContainsPersonalItems,
                ReceiptIsItemised = detail.ReceiptIsItemised,
                TotalIncGst = detail.TotalIncGst,
                GstAmount = detail.GstAmount,
                NonReimbursedAmount = detail.NonReimbursedAmount,
                Items = detail.Items.Select(item => new ExpenseDetailItemModel
                {
                    Description = item.Description,
                    Amount = item.Amount,
                    IsChurchUse = item.IsChurchUse
                }).ToList()
            }).ToList(),

            Attendees = submission.Attendees.Select(attendee => new AttendeeModel
            {
                Date = FromUtc(attendee.Date),
                Person = attendee.Person,
                Relationship = attendee.Relationship,
                Amount = attendee.Amount,
                PrivateShare = attendee.PrivateShare,
                Reason = attendee.Reason
            }).ToList(),

            Trips = submission.Trips.Select(trip => new TripModel
            {
                Date = FromUtc(trip.Date),
                From = trip.From,
                To = trip.To,
                BusinessKm = trip.BusinessKm,
                ApprovedRate = trip.ApprovedRate,
                Purpose = trip.Purpose
            }).ToList()
        };

        if (submission.MissingReceipt is { } missing)
        {
            model.MissingSupplier = missing.Supplier;
            model.MissingDate = FromUtc(missing.Date);
            model.MissingAmount = missing.Amount;
            model.MissingReason = missing.Reason;
            model.MissingDeclared = missing.Declared;
        }

        return model;
    }

    /// <summary>
    /// Builds the write request. Only reachable once question zero has been answered - a form with no
    /// kind has no section 1 on screen, nothing to save, and no wording to save it under.
    /// </summary>
    /// <summary>
    /// Builds the write request. <paramref name="attachments"/> decides only whether section 5's
    /// declaration is sent - the files themselves went up their own endpoint and are not in here.
    /// </summary>
    public CreateExpenseSubmissionRequest ToCreateRequest(IReadOnlyList<ExpenseAttachment> attachments) => new()
    {
        Kind = Kind ?? throw new InvalidOperationException(
            "The form has no kind. Question zero has to be answered before a draft can be written, "
            + "because the kind decides what every stored field means."),
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

        // No LessPersonalAmount: the server sums the details' own non-reimbursed amounts, so sending a
        // total as well would be sending the same figure twice and inviting the two to disagree.

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
        MissingReceipt = HasMissingEvidence(attachments)
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

        Details = Details.Select((detail, index) => new ExpenseDetail
        {
            SubmissionId = Guid.Empty,
            // The claimant's key, sent as-is. Everything else the server mints; this one it writes back
            // untouched, because it is what the uploaded files point at.
            Key = detail.Key,
            Ordinal = index,
            Supplier = Trim(detail.Supplier),
            PurchaseDate = ToUtc(detail.PurchaseDate),
            Purpose = Trim(detail.Purpose),
            ContainsPersonalItems = detail.ContainsPersonalItems,
            ReceiptIsItemised = detail.ReceiptIsItemised,
            TotalIncGst = detail.TotalIncGst ?? 0m,
            GstAmount = detail.GstAmount,
            NonReimbursedAmount = detail.NonReimbursedAmount ?? 0m,
            Items = detail.Items.Select((item, itemIndex) => new ExpenseDetailItem
            {
                DetailId = Guid.Empty,
                Ordinal = itemIndex,
                Description = Trim(item.Description),
                Amount = item.Amount ?? 0m,
                // Through the model's own reading of it, not the raw flag: in personal-items-only mode
                // every listed item is personal whatever the flag says, and a stray true there would
                // drop a line out of the total the non-reimbursed amount is floored on.
                IsChurchUse = detail.EffectiveChurchUse(item)
            }).ToList()
        }).ToList()
    };

    /// <summary>
    /// A calendar day becomes midnight UTC. <c>DateTimeKind.Utc</c> is stated explicitly rather than
    /// left to default: the server rejects anything it cannot treat as offset zero, and a
    /// <c>Kind.Unspecified</c> value silently acquires the machine's local offset on the way in.
    /// </summary>
    private static DateTime? ToUtc(DateOnly? date) =>
        date is null ? null : DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    /// <summary>
    /// Midnight UTC becomes the calendar day it was. The inverse of <see cref="ToUtc"/>.
    /// </summary>
    /// <remarks>
    /// <b><c>DateOnly.FromDateTime</c> on the value as it arrives, with no <c>ToLocalTime</c>.</b> The
    /// stored instant is midnight UTC of the day the claimant picked; converting it to Brisbane time
    /// first would move it to 10am the same day, which is harmless - and to the previous day for
    /// anywhere west of UTC, which is not. A form date that shifts by one day every time somebody
    /// reopens a draft is the sort of thing an auditor notices and nobody can explain.
    /// </remarks>
    private static DateOnly? FromUtc(DateTime? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value);

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

/// <summary>
/// One purchase being filled in - a receipt, its two questions, its itemisation and its money.
/// </summary>
/// <remarks>
/// Every money field is nullable so an empty box stays empty rather than showing a 0 somebody has to
/// select and delete before typing.
/// </remarks>
public class ExpenseDetailModel
{
    /// <summary>
    /// Stable for the life of this purchase - across re-renders, across autosaves, and across the
    /// claimant closing the draft and coming back to it.
    /// </summary>
    /// <remarks>
    /// Doing double duty, and both jobs need the same property. It is Blazor's <c>@key</c>, so a removed
    /// panel's DOM is not reused for the panel that shifted up; and it is what every uploaded file is
    /// filed against on the server, because <c>Update</c> gives each detail a new row id on every
    /// autosave and a file holding one would come unlinked two seconds later.
    /// <para>
    /// <b>Settable, unlike the row keys elsewhere in this file</b> - a resumed draft has to come back
    /// with the keys it was saved under, or every file on it is orphaned the moment the page loads.
    /// </para>
    /// </remarks>
    public Guid Key { get; set; } = Guid.NewGuid();

    /// <summary>Where it was bought. One purchase location per detail.</summary>
    public string? Supplier { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    /// <summary>What it was for - the church purpose, in the claimant's own words.</summary>
    public string? Purpose { get; set; }

    /// <summary>Question one. Null until answered, and null is not No.</summary>
    public bool? ContainsPersonalItems { get; set; }

    /// <summary>Question two. Null until answered.</summary>
    public bool? ReceiptIsItemised { get; set; }

    public decimal? TotalIncGst { get; set; }
    public decimal? GstAmount { get; set; }

    /// <summary>
    /// What the church is not being asked to pay for. Never below <see cref="PersonalItemsTotal"/> -
    /// see <see cref="ClampNonReimbursed"/> - and free to be above it, which is a gift.
    /// </summary>
    public decimal? NonReimbursedAmount { get; set; }

    public List<ExpenseDetailItemModel> Items { get; set; } = [];

    /// <summary>Both questions answered. Nothing below them renders until they are.</summary>
    public bool Answered => ContainsPersonalItems is not null && ReceiptIsItemised is not null;

    /// <summary>
    /// What this receipt needs typed out, from its two answers. The same derivation the contract and the
    /// server use - see <see cref="ExpenseDetail.Itemisation"/>, which is where it is explained.
    /// </summary>
    public ItemisationRequirement Itemisation => (ContainsPersonalItems, ReceiptIsItemised) switch
    {
        (false, true) => ItemisationRequirement.None,
        (_, false) => ItemisationRequirement.Everything,
        (true, true) => ItemisationRequirement.PersonalItemsOnly,
        _ => ItemisationRequirement.None
    };

    /// <summary>
    /// Whether the church-use toggle appears on each item. Only where the claimant is typing out the
    /// whole receipt AND has said some of it is personal - otherwise every listed item is already known
    /// to be one or the other, and a control whose answer is fixed is a control that invites a wrong one.
    /// </summary>
    public bool ItemsAreMixed =>
        Itemisation == ItemisationRequirement.Everything && ContainsPersonalItems == true;

    /// <summary>The itemised lines that are not the church's. The floor under the non-reimbursed field.</summary>
    public decimal PersonalItemsTotal =>
        Round(Items.Where(x => !EffectiveChurchUse(x)).Sum(x => Round(x.Amount ?? 0m)));

    /// <summary>Everything typed out, church items included. Shown against the receipt total.</summary>
    public decimal ItemisedTotal => Round(Items.Sum(x => Round(x.Amount ?? 0m)));

    /// <summary>
    /// True when a full itemisation does not come to what the receipt says. <b>A warning, never a
    /// block</b> - somebody reading a faded thermal docket is asked for best effort, and a form that
    /// refuses to submit until the cents reconcile is a form that gets a made-up line added to close the
    /// gap.
    /// </summary>
    public bool ItemisationDiffers =>
        Itemisation == ItemisationRequirement.Everything
        && Items.Count > 0
        && TotalIncGst is { } total
        && Round(total) != ItemisedTotal;

    /// <summary>
    /// Whether an item counts as the church's, given the mode. In personal-items-only mode every line
    /// listed IS a personal item, whatever the flag happens to hold - the toggle is not on screen there.
    /// </summary>
    public bool EffectiveChurchUse(ExpenseDetailItemModel item) =>
        Itemisation != ItemisationRequirement.PersonalItemsOnly && item.IsChurchUse;

    /// <summary>
    /// Raises the non-reimbursed amount to the personal items total when itemising has pushed it above
    /// what is currently typed there.
    /// </summary>
    /// <remarks>
    /// <b>Raises, never lowers.</b> A claimant who chose to absorb $50 of a $30-personal receipt is
    /// making a gift, and deleting a personal line must not quietly take $20 of that gift back. Lowering
    /// it is something they do by typing, in a field whose <c>min</c> is this floor.
    /// </remarks>
    public void ClampNonReimbursed()
    {
        decimal floor = Itemisation == ItemisationRequirement.None ? 0m : PersonalItemsTotal;

        if ((NonReimbursedAmount ?? 0m) < floor)
            NonReimbursedAmount = floor;
    }

    /// <summary>
    /// Drops what the questions have made irrelevant, so a stale answer cannot ride along on a claim.
    /// </summary>
    /// <remarks>
    /// Called whenever either question is re-answered. The items go when nothing needs itemising - they
    /// would otherwise be printed on the PDF as an itemisation nobody was asked for, and worse, they
    /// would still be flooring the non-reimbursed amount. The <b>church-use flags</b> go when the mode
    /// stops mixing, for the same reason: an item silently marked "church use" in a list that is meant to
    /// be entirely personal drops straight out of the floor.
    /// </remarks>
    public void ReconcileToAnswers()
    {
        if (Itemisation == ItemisationRequirement.None)
            Items.Clear();

        if (!ItemsAreMixed)
        {
            foreach (ExpenseDetailItemModel item in Items)
                item.IsChurchUse = false;
        }

        ClampNonReimbursed();
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);
}

/// <summary>One line the claimant is typing out of a receipt.</summary>
public class ExpenseDetailItemModel
{
    /// <summary>Stable across re-renders, so Blazor's @key does not reuse a row's DOM for another row.</summary>
    public Guid Key { get; } = Guid.NewGuid();

    public string? Description { get; set; }

    public decimal? Amount { get; set; }

    /// <summary>
    /// Church use or not - a yes/no, not the paper form's percentage. Only meaningful where
    /// <see cref="ExpenseDetailModel.ItemsAreMixed"/>; read it through
    /// <see cref="ExpenseDetailModel.EffectiveChurchUse"/> rather than directly.
    /// </summary>
    public bool IsChurchUse { get; set; }
}
