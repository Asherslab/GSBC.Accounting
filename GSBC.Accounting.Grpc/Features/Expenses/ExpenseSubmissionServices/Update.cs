using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Responses.Base;
using Microsoft.EntityFrameworkCore;
using static GSBC.Accounting.Grpc.Features.Expenses.ErrorConstants;

namespace GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;

public partial class ExpenseSubmissionService
{
    /// <summary>
    /// Rewrites a draft with the form as it now stands.
    /// </summary>
    /// <remarks>
    /// The children are <b>replaced</b>, not merged. A line the claimant deleted has to disappear, and
    /// matching rows up by position across an edit that inserted one in the middle is a way to silently
    /// move somebody's money between lines.
    /// <para>
    /// The rows are soft-deleted rather than removed, like everything else here: seven-year retention
    /// applies to a draft too, and a superseded line is part of how a submission came to say what it
    /// says.
    /// </para>
    /// </remarks>
    public async Task<BasicResponse> Update(UpdateExpenseSubmissionRequest request, CallContext context = default)
    {
        CancellationToken token = context.CancellationToken;

        List<string> errors = ValidateForCreate(request.Form);

        if (errors.Count > 0)
            return BasicResponse.WithErrors(errors);

        // No session, no draft to rewrite. Answered with the same "could not be found" as a genuinely
        // missing row, because the two must be indistinguishable from outside.
        if (await sessions.CurrentAsync(token) is not { } sessionId)
            return BasicResponse.WithError(SubmissionNotFound);

        DbExpenseSubmission? submission = await db.ExpenseSubmissions
            .Include(x => x.Lines)
            .Include(x => x.Attendees)
            .Include(x => x.Trips)
            .Include(x => x.MissingReceipt)
            .FirstOrDefaultAsync(
                x => x.Id == request.SubmissionId && x.OwnerSessionId == sessionId, token);

        if (submission is null)
            return BasicResponse.WithError(SubmissionNotFound);

        // A submitted claim is evidence. It does not change afterwards.
        if (submission.Status != SubmissionStatus.Draft)
            return BasicResponse.WithError(AlreadySubmitted);

        CreateExpenseSubmissionRequest form = request.Form;

        // The kind is fixed at creation. A page cannot turn a debit card claim into a reimbursement, and
        // a client that tried would be reinterpreting every per-kind field already stored.
        (decimal gross, decimal gst) = ExpenseTotals.SumLines(form.Lines);
        decimal lessPersonal = ExpenseTotals.Money(form.LessPersonalAmount);

        submission.SubmitterName = form.SubmitterName;
        submission.FormDate = ToOffset(form.FormDate);
        submission.Role = form.Role;
        submission.RoleOther = form.RoleOther;
        submission.MinistryDepartment = form.MinistryDepartment;

        submission.CardLastFourDigits = form.CardLastFourDigits;
        submission.TransactionDate = ToOffset(form.TransactionDate);
        submission.TransactionTime = form.TransactionTime;
        submission.SupplierMerchant = form.SupplierMerchant;
        submission.AmountCharged = form.AmountCharged is { } charged ? ExpenseTotals.Money(charged) : null;
        submission.BankReference = form.BankReference;

        submission.ContactPhoneEmail = form.ContactPhoneEmail;
        submission.ExpensePeriodFrom = ToOffset(form.ExpensePeriodFrom);
        submission.ExpensePeriodTo = ToOffset(form.ExpensePeriodTo);
        submission.PaymentMethod = form.PaymentMethod;
        submission.PaymentMethodOther = form.PaymentMethodOther;
        submission.BankDetailsOnFile = form.BankDetailsOnFile;

        submission.PurposeActivity = form.PurposeActivity;
        submission.EventProject = form.EventProject;
        submission.PriorApprovalBy = form.PriorApprovalBy;
        submission.ApprovalDate = ToOffset(form.ApprovalDate);
        submission.PurposeNarrative = form.PurposeNarrative;

        submission.GrossTotal = gross;
        submission.GstTotal = gst;
        submission.LessPersonalAmount = lessPersonal;
        submission.NetTotal = ExpenseTotals.Net(gross, lessPersonal);

        submission.ComplianceQ1 = form.ComplianceQ1;
        submission.ComplianceQ2 = form.ComplianceQ2;
        submission.ComplianceQ3 = form.ComplianceQ3;
        submission.ComplianceQ4 = form.ComplianceQ4;
        submission.ComplianceQ5 = form.ComplianceQ5;
        submission.ComplianceQ6 = form.ComplianceQ6;
        submission.ComplianceDetails = form.ComplianceDetails;

        submission.Declaration1 = form.Declaration1;
        submission.Declaration2 = form.Declaration2;
        submission.Declaration3 = form.Declaration3;
        submission.Declaration4 = form.Declaration4;
        submission.Declaration5 = form.Declaration5;

        submission.SignatureName = form.SignatureName;

        // What the abandoned-draft purge counts from. A claimant who edits a form every few weeks
        // keeps it indefinitely; one who never comes back loses it ninety days after their last edit,
        // not ninety days after they started.
        submission.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (DbExpenseLine line in submission.Lines)
            line.Deleted = true;

        foreach (DbExpenseAttendee attendee in submission.Attendees)
            attendee.Deleted = true;

        foreach (DbExpenseTrip trip in submission.Trips)
            trip.Deleted = true;

        int ordinal = 0;

        foreach (ExpenseLine line in form.Lines)
        {
            submission.Lines.Add(new DbExpenseLine
            {
                Id = Guid.Empty,
                SubmissionId = submission.Id,
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

        if (submission.Kind == SubmissionKind.DebitCardPurchase)
        {
            int attendeeOrdinal = 0;

            foreach (ExpenseAttendee attendee in form.Attendees)
            {
                submission.Attendees.Add(new DbExpenseAttendee
                {
                    Id = Guid.Empty,
                    SubmissionId = submission.Id,
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

            foreach (ExpenseTrip trip in form.Trips)
            {
                submission.Trips.Add(new DbExpenseTrip
                {
                    Id = Guid.Empty,
                    SubmissionId = submission.Id,
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

        bool hasMissingLine = form.Lines.Any(x => x.Evidence == EvidenceStatus.Missing);

        if (hasMissingLine && form.MissingReceipt is { } missing)
        {
            // 0..1, so it is updated in place rather than replaced - a second row would violate the
            // one-per-submission relationship.
            submission.MissingReceipt ??= new DbMissingReceiptDeclaration
            {
                Id = Guid.Empty,
                SubmissionId = submission.Id
            };

            submission.MissingReceipt.Supplier = missing.Supplier;
            submission.MissingReceipt.Date = ToOffset(missing.Date);
            submission.MissingReceipt.Amount = missing.Amount is { } m ? ExpenseTotals.Money(m) : null;
            submission.MissingReceipt.Reason = missing.Reason;
            submission.MissingReceipt.Declared = missing.Declared;
            submission.MissingReceipt.Deleted = false;
        }
        else if (submission.MissingReceipt is not null)
        {
            // No line says Missing any more, so the declaration no longer belongs to this submission.
            submission.MissingReceipt.Deleted = true;
        }

        await db.SaveChangesAsync(token);

        return new BasicResponse { Success = true };
    }
}
