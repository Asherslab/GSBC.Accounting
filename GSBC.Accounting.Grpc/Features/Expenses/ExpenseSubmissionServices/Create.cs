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

        // AFTER validation, deliberately. A refused create must not leave a session behind - the autosave
        // in the form pages calls this speculatively as the claimant types, and a refusal that had
        // already minted a session would hand a cookie to a browser that stored nothing.
        Guid sessionId = await sessions.EnsureAsync(token);

        // The server's own arithmetic. Whatever the client computed is discarded - it is a display
        // convenience. That now includes the personal portion, which is summed from the details rather
        // than taken as a figure of its own.
        (decimal gross, decimal gst, decimal lessPersonal) = ExpenseTotals.SumDetails(request.Details);

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

        WriteDetails(submission, request.Details);

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

        // Section 5, written only when there is genuinely a purchase with no supplier receipt behind it.
        // A declaration attached to a submission with full evidence would read to a reviewer as a
        // statement somebody made, and nobody made it.
        //
        // On CREATE the attachments do not exist yet - this is phase one, and the files go up against
        // the id it returns - so the trigger cannot be evaluated here at all. Update is where it is
        // decided, and Submit is where it is enforced; carrying the declaration through on create simply
        // means a claimant who filled it in before their first autosave does not lose it.
        if (request.MissingReceipt is { } missing)
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
    /// Adds the form's section 3 to a submission, as new rows. Shared by <c>Create</c> and by
    /// <c>Update</c>, which soft-deletes the previous set first.
    /// </summary>
    /// <remarks>
    /// <b><see cref="ExpenseDetail.Key"/> is carried through from the client and every other identifier
    /// is not.</b> The key is what the uploaded files point at, so it has to survive a rewrite; the row
    /// ids are the database's to mint. A detail arriving with an empty key gets one here rather than
    /// being refused - it means an older client, and the cost is only that its files stay unfiled.
    /// <para>
    /// <c>Ordinal</c> is assigned here rather than trusted from the request: the order of section 3 is
    /// the order the claimant attached the receipts, and a client sending duplicate or sparse ordinals
    /// would silently reorder the printed form.
    /// </para>
    /// <para>
    /// Items are written only where the detail's own two questions say itemisation is required. A client
    /// that sends them anyway is ignored: leaving them stored would mean a receipt that needs no
    /// itemisation printing one on the PDF, and a personal-items total floored on lines nobody was asked
    /// for.
    /// </para>
    /// </remarks>
    private static void WriteDetails(DbExpenseSubmission submission, IReadOnlyList<ExpenseDetail> details)
    {
        int ordinal = 0;

        foreach (ExpenseDetail detail in details)
        {
            bool itemised = detail.Itemisation != ItemisationRequirement.None;

            DbExpenseDetail row = new()
            {
                // Guid.Empty for the id, because Postgres generates it. NOT for the key - that one
                // belongs to the client and is the only link the uploaded files have.
                Id = Guid.Empty,
                SubmissionId = submission.Id,
                Key = detail.Key == Guid.Empty ? Guid.NewGuid() : detail.Key,
                Ordinal = ordinal++,
                Supplier = detail.Supplier,
                PurchaseDate = ToOffset(detail.PurchaseDate),
                Purpose = detail.Purpose,
                ContainsPersonalItems = detail.ContainsPersonalItems,
                ReceiptIsItemised = detail.ReceiptIsItemised,
                TotalIncGst = ExpenseTotals.Money(detail.TotalIncGst),
                GstAmount = detail.GstAmount is { } gst ? ExpenseTotals.Money(gst) : null,
                NonReimbursedAmount = ExpenseTotals.Money(detail.NonReimbursedAmount)
            };

            if (itemised)
            {
                int itemOrdinal = 0;

                foreach (ExpenseDetailItem item in detail.Items)
                {
                    row.Items.Add(new DbExpenseDetailItem
                    {
                        Id = Guid.Empty,
                        DetailId = Guid.Empty,
                        Ordinal = itemOrdinal++,
                        Description = item.Description,
                        Amount = ExpenseTotals.Money(item.Amount),
                        // On a receipt whose personal lines alone were asked for, every line listed IS a
                        // personal one. The page does not show the toggle in that mode, so a client
                        // sending IsChurchUse = true there is sending an answer to a question it was not
                        // asked - and a "church use" item in that list would silently drop out of the
                        // personal total the non-reimbursed amount is floored on.
                        IsChurchUse = detail.Itemisation != ItemisationRequirement.PersonalItemsOnly
                                      && item.IsChurchUse
                    });
                }
            }

            submission.Details.Add(row);
        }
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

        // The form's own instruction - "Never record the full card number, PIN or security code" -
        // enforced rather than merely printed. Deliberately NOT "exactly four" here: a claimant halfway
        // through typing has "12" on screen, and refusing that would refuse to save their draft over a
        // value that is on its way to being right. What must never be stored is something that could be
        // a card number, and four digits is the ceiling for that. Submit requires all four.
        if (!string.IsNullOrWhiteSpace(request.CardLastFourDigits)
            && (request.CardLastFourDigits.Length > 4 || !request.CardLastFourDigits.All(char.IsAsciiDigit)))
        {
            errors.Add(CardLastFourDigitsMustBeDigitsOnly);
        }

        foreach (ExpenseDetail detail in request.Details)
        {
            if (detail.TotalIncGst < 0)
                errors.Add(DetailTotalCannotBeNegative);

            if (detail.GstAmount is { } gst && gst > detail.TotalIncGst)
                errors.Add(GstCannotExceedTotal);

            if (detail.NonReimbursedAmount < 0)
                errors.Add(NonReimbursedCannotBeNegative);

            if (detail.NonReimbursedAmount > detail.TotalIncGst)
                errors.Add(NonReimbursedCannotExceedTotal);

            if (detail.Items.Any(x => x.Amount < 0))
                errors.Add(ItemAmountCannotBeNegative);
        }

        // NOTHING HERE ASKS WHETHER THE FORM IS FINISHED - not "at least one receipt", not the two
        // questions, not the itemisation those questions require, and NOT the floor under the
        // non-reimbursed amount. Those are completeness rules and they live in ValidateForSubmit.
        //
        // The floor is the one worth naming, because it looks like arithmetic and is not: a claimant
        // halfway through typing the personal lines has itemised $12 of an eventual $40, and a draft
        // refused for that is a draft that goes unsaved while somebody is still working on it. The
        // ceiling above IS checked here, because no amount of further typing makes "I am not claiming
        // more than the receipt was for" become coherent.
        return errors.Distinct().ToList();
    }
}
