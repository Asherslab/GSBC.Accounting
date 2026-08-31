using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Responses.Base;
using GSBC.Accounting.Grpc.Features.Sessions;
using static GSBC.Accounting.Grpc.Features.Expenses.ErrorConstants;

namespace GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;

public partial class ExpenseSubmissionService
{
    /// <summary>
    /// Writes a submission in <see cref="SubmissionStatus.Draft"/> and returns its id.
    /// </summary>
    /// <remarks>
    /// <b>Draft is phase one of a two-phase write.</b> The browser calls this to get an id, uploads each
    /// receipt against that id, then submits. The phases exist because the page is anonymous: an upload
    /// endpoint that accepted files with no submission id would be an open write endpoint to the object
    /// store.
    /// <para>
    /// <b>This is the one place a draft session is minted</b>, and creating a draft is the first moment
    /// a browser has anything worth remembering. Everything else calls <c>CurrentAsync</c>, which never
    /// issues a cookie - so somebody who reads the landing page, or a crawler that walks the site,
    /// leaves neither a row nor a cookie behind.
    /// </para>
    /// <para>
    /// The id is no longer a credential on its own: the row records which session created it, and every
    /// later read and write checks that as well. Both still matter - the id stays a database-generated
    /// <c>Guid</c> rather than a sequence, because the PDF endpoint accepts it alone for a submitted
    /// claim.
    /// </para>
    /// <para>
    /// Validation here is only what must hold for the row to be coherent - the arithmetic has to add up
    /// and the card number must not be recorded. The completeness rules (a receipt attached, the
    /// declarations ticked, the lines reconciling against the amount charged) belong to submit, in slice
    /// 7, because a draft is allowed to be half-finished.
    /// </para>
    /// </remarks>
    // THE ONE METHOD THAT OPTS OUT OF Policies.AnonymousSession, and it has to. This is the sole caller
    // of EnsureAsync, so a browser reaching it by definition has no session yet; requiring one here
    // would mean nobody could ever obtain one and no draft could ever be created. Every other method on
    // this service, and every attachment write, keeps the policy.
    //
    // THE EXEMPTION IS IN Program.cs, NOT HERE. [AllowAnonymous] on this method is a no-op -
    // protobuf-net.Grpc does not carry method-level attributes onto the endpoint it builds - so the
    // attribute is deliberately absent rather than sitting here reading as though it works. Adding it
    // back would be worse than useless: it would look like the exemption and silently not be it.
    public async Task<BasicReadResponse<Guid?>> Create(
        CreateExpenseSubmissionRequest request,
        CallContext context = default
    )
    {
        CancellationToken token = context.CancellationToken;

        List<string> errors = ValidateForCreate(request);

        if (errors.Count > 0)
            return BasicReadResponse<Guid?>.WithErrors(errors);

        // AFTER validation, deliberately. A refused create must not leave a session behind, or every
        // keystroke that fails the "needs a line" check would mint one - the autosave in the form pages
        // calls this speculatively and expects to be told no.
        Guid sessionId = await sessions.EnsureAsync(token);

        // The server's own arithmetic. Whatever the client computed is discarded - it is a display
        // convenience, and the mockup computes it in JavaScript floats.
        (decimal gross, decimal gst) = ExpenseTotals.SumLines(request.Lines);
        decimal lessPersonal = ExpenseTotals.Money(request.LessPersonalAmount);

        DbExpenseSubmission submission = new()
        {
            // Guid.Empty, not Guid.NewGuid(): Postgres generates the real one, so there is one authority
            // for identity rather than two that can disagree.
            Id = Guid.Empty,
            Kind = request.Kind,
            Status = SubmissionStatus.Draft,

            SubmitterName = request.SubmitterName,
            FormDate = ToOffset(request.FormDate),
            Role = request.Role,
            RoleOther = request.RoleOther,
            MinistryDepartment = request.MinistryDepartment,

            CardLastFourDigits = request.CardLastFourDigits,
            TransactionDate = ToOffset(request.TransactionDate),
            TransactionTime = request.TransactionTime,
            SupplierMerchant = request.SupplierMerchant,
            AmountCharged = request.AmountCharged is { } charged ? ExpenseTotals.Money(charged) : null,
            BankReference = request.BankReference,

            ContactPhoneEmail = request.ContactPhoneEmail,
            ExpensePeriodFrom = ToOffset(request.ExpensePeriodFrom),
            ExpensePeriodTo = ToOffset(request.ExpensePeriodTo),
            PaymentMethod = request.PaymentMethod,
            PaymentMethodOther = request.PaymentMethodOther,
            BankDetailsOnFile = request.BankDetailsOnFile,

            PurposeActivity = request.PurposeActivity,
            EventProject = request.EventProject,
            PriorApprovalBy = request.PriorApprovalBy,
            ApprovalDate = ToOffset(request.ApprovalDate),
            PurposeNarrative = request.PurposeNarrative,

            GrossTotal = gross,
            GstTotal = gst,
            LessPersonalAmount = lessPersonal,
            NetTotal = ExpenseTotals.Net(gross, lessPersonal),

            ComplianceQ1 = request.ComplianceQ1,
            ComplianceQ2 = request.ComplianceQ2,
            ComplianceQ3 = request.ComplianceQ3,
            ComplianceQ4 = request.ComplianceQ4,
            ComplianceQ5 = request.ComplianceQ5,
            ComplianceQ6 = request.ComplianceQ6,
            ComplianceDetails = request.ComplianceDetails,

            Declaration1 = request.Declaration1,
            Declaration2 = request.Declaration2,
            Declaration3 = request.Declaration3,
            Declaration4 = request.Declaration4,
            Declaration5 = request.Declaration5,

            SignatureName = request.SignatureName,
            SignedAt = null,

            OwnerSessionId = sessionId,

            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            SubmittedAt = null,
            IsMockData = request.IsMockData
        };

        // Ordinal is assigned here rather than trusted from the request: the order of section 3 is the
        // order the claimant typed, and a client that sent duplicate or sparse ordinals would silently
        // reorder the printed form.
        int ordinal = 0;

        foreach (ExpenseLine line in request.Lines)
        {
            submission.Lines.Add(new DbExpenseLine
            {
                Id = Guid.Empty,
                SubmissionId = Guid.Empty,
                Ordinal = ordinal++,
                ItemDescription = line.ItemDescription,
                LineDate = ToOffset(line.LineDate),
                Details = line.Details,
                Purpose = line.Purpose,
                Evidence = line.Evidence,
                GrossAmount = ExpenseTotals.Money(line.GrossAmount),
                GstAmount = line.GstAmount is { } g ? ExpenseTotals.Money(g) : null,
                ChurchUsePercent = line.ChurchUsePercent
            });
        }

        // Section 4's detail tables. Only the one belonging to this kind is written - a debit card
        // submission has no trip record and a reimbursement has no attendee table, and a client sending
        // the wrong one is ignored rather than trusted.
        if (request.Kind == SubmissionKind.DebitCardPurchase)
        {
            int attendeeOrdinal = 0;

            foreach (ExpenseAttendee attendee in request.Attendees)
            {
                submission.Attendees.Add(new DbExpenseAttendee
                {
                    Id = Guid.Empty,
                    SubmissionId = Guid.Empty,
                    Ordinal = attendeeOrdinal++,
                    Date = ToOffset(attendee.Date),
                    Person = attendee.Person,
                    Relationship = attendee.Relationship,
                    Amount = attendee.Amount is { } a ? ExpenseTotals.Money(a) : null,
                    PrivateShare = attendee.PrivateShare is { } p ? ExpenseTotals.Money(p) : null,
                    Reason = attendee.Reason
                });
            }
        }
        else
        {
            int tripOrdinal = 0;

            foreach (ExpenseTrip trip in request.Trips)
            {
                submission.Trips.Add(new DbExpenseTrip
                {
                    Id = Guid.Empty,
                    SubmissionId = Guid.Empty,
                    Ordinal = tripOrdinal++,
                    Date = ToOffset(trip.Date),
                    From = trip.From,
                    To = trip.To,
                    BusinessKm = trip.BusinessKm,
                    ApprovedRate = trip.ApprovedRate,
                    Purpose = trip.Purpose
                });
            }
        }

        // Section 5, written only when some line actually says Missing. A declaration attached to a
        // submission with full evidence would read to a reviewer as a statement somebody made, and
        // nobody made it.
        if (request.MissingReceipt is { } missing && request.Lines.Any(x => x.Evidence == EvidenceStatus.Missing))
        {
            submission.MissingReceipt = new DbMissingReceiptDeclaration
            {
                Id = Guid.Empty,
                SubmissionId = Guid.Empty,
                Supplier = missing.Supplier,
                Date = ToOffset(missing.Date),
                Amount = missing.Amount is { } m ? ExpenseTotals.Money(m) : null,
                Reason = missing.Reason,
                Declared = missing.Declared
            };
        }

        await db.ExpenseSubmissions.AddAsync(submission, token);
        await db.SaveChangesAsync(token);

        return new BasicReadResponse<Guid?> { Entity = submission.Id, Success = true };
    }

    /// <summary>
    /// A UTC <c>DateTime</c> from the wire becomes a <c>DateTimeOffset</c> at offset zero.
    /// </summary>
    /// <remarks>
    /// <c>SpecifyKind</c> is the load-bearing part. A <c>DateTime</c> deserialised from protobuf has
    /// <c>Kind.Unspecified</c>, and constructing a <c>DateTimeOffset</c> from one of those applies the
    /// machine's local offset - so a value written in Brisbane would land ten hours out, and it would
    /// throw on any later query comparison with "only offset 0 (UTC) is supported".
    /// </remarks>
    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    private static List<string> ValidateForCreate(CreateExpenseSubmissionRequest request)
    {
        List<string> errors = [];

        if (request.Lines.Count == 0)
            errors.Add(SubmissionNeedsALine);

        if (request.LessPersonalAmount < 0)
            errors.Add(LessPersonalCannotBeNegative);

        // Four digits, and the check exists so the form's own instruction - "Never record the full card
        // number, PIN or security code" - is enforced rather than merely printed.
        if (!string.IsNullOrWhiteSpace(request.CardLastFourDigits)
            && (request.CardLastFourDigits.Length != 4 || !request.CardLastFourDigits.All(char.IsAsciiDigit)))
        {
            errors.Add(CardLastFourDigitsMustBeFourDigits);
        }

        foreach (ExpenseLine line in request.Lines)
        {
            if (line.GrossAmount < 0)
                errors.Add(GrossAmountCannotBeNegative);

            if (line.GstAmount is { } gst && gst > line.GrossAmount)
                errors.Add(GstCannotExceedGross);

            if (line.ChurchUsePercent is < 0 or > 100)
                errors.Add(ChurchUsePercentOutOfRange);
        }

        // Section 3's first column is a different field on each form, so which one is required depends
        // on the kind. This is the smallest example of the rule that runs through the whole app: the two
        // forms share a structure, not their contents.
        switch (request.Kind)
        {
            case SubmissionKind.DebitCardPurchase
                when request.Lines.Any(x => string.IsNullOrWhiteSpace(x.ItemDescription)):
                errors.Add(DebitCardLineNeedsAnItem);
                break;

            case SubmissionKind.ExpenseReimbursement when request.Lines.Any(x => x.LineDate is null):
                errors.Add(ReimbursementLineNeedsADate);
                break;
        }

        return errors.Distinct().ToList();
    }
}
