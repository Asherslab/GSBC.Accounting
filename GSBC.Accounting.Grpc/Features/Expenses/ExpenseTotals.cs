using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.Grpc.Features.Expenses;

/// <summary>
/// The server's own arithmetic over a submission's details. <b>The only place totals are produced.</b>
/// </summary>
/// <remarks>
/// A client's totals are a display convenience and are never persisted. The page computes the same
/// figures so they update as somebody types - which is exactly why this exists and why every value here
/// is <c>decimal</c>.
/// <para>
/// This catches a broken client. It does not form a view on whether an expense is legitimate: that is a
/// human's job, and nothing in this app second-guesses it.
/// </para>
/// </remarks>
public static class ExpenseTotals
{
    /// <summary>
    /// Sum of every detail's receipt total, the GST shown on them, and the part of each the church is
    /// not being asked to bear.
    /// </summary>
    /// <remarks>
    /// Rounded to cents at each step rather than only at the end - the columns are <c>decimal(12,2)</c>,
    /// so a sum carrying more scale would be silently truncated on write and the stored total would not
    /// equal the stored details.
    /// <para>
    /// <b><c>NonReimbursed</c> is summed here rather than taken from the client.</b> It used to be a
    /// single figure the claimant typed at the foot of section 3 and the server accepted as given. Now
    /// each detail states its own, so there is one place the number comes from and no second figure for
    /// it to disagree with.
    /// </para>
    /// </remarks>
    public static (decimal Gross, decimal Gst, decimal NonReimbursed) SumDetails(
        IEnumerable<ExpenseDetail> details
    )
    {
        decimal gross = 0m;
        decimal gst = 0m;
        decimal nonReimbursed = 0m;

        foreach (ExpenseDetail detail in details)
        {
            gross += Money(detail.TotalIncGst);
            gst += Money(detail.GstAmount ?? 0m);
            nonReimbursed += Money(detail.NonReimbursedAmount);
        }

        return (Money(gross), Money(gst), Money(nonReimbursed));
    }

    /// <summary>
    /// `NET AUTHORISED CHURCH EXPENSE` / `TOTAL REIMBURSEMENT CLAIMED` - the gross less the portion the
    /// claimant said the church is not paying for.
    /// </summary>
    /// <remarks>
    /// Not clamped at zero. A negative net means the claimant said more was personal than was spent,
    /// which is a mistake worth showing a reviewer rather than quietly rounding away.
    /// </remarks>
    public static decimal Net(decimal gross, decimal lessPersonal) => Money(gross - Money(lessPersonal));

    /// <summary>
    /// The itemised lines of a detail that are not the church's. <b>The floor under its
    /// <see cref="ExpenseDetail.NonReimbursedAmount"/></b>, recomputed here rather than trusted from the
    /// contract's computed property, which a hand-built request could have set to anything.
    /// </summary>
    public static decimal PersonalItemsTotal(ExpenseDetail detail) =>
        Money(detail.Items.Where(x => !x.IsChurchUse).Sum(x => Money(x.Amount)));

    // There is deliberately no ItemisedTotal here, and its absence is the rule rather than an omission:
    // the server never compares what was itemised against the receipt total. Where the evidence does not
    // itemise, best effort is what was asked for, and a submission refused until the cents reconcile is
    // one that gets a made-up line added to close the gap. The page shows the difference as a warning -
    // see ExpenseDetailModel.ItemisationDiffers - and a reviewer can ask.

    /// <summary>
    /// <c>MidpointRounding.ToEven</c> - banker's rounding - so a long run of half-cents does not drift
    /// upward the way <c>AwayFromZero</c> does.
    /// </summary>
    public static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);
}
