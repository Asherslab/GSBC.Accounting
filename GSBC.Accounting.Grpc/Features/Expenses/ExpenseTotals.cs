using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.Grpc.Features.Expenses;

/// <summary>
/// The server's own arithmetic over a submission's lines. <b>The only place totals are produced.</b>
/// </summary>
/// <remarks>
/// A client's totals are a display convenience and are never persisted. The mockup computes in
/// JavaScript floats, so its numbers can differ from these in the last cent - which is exactly why this
/// exists and why every value here is <c>decimal</c>.
/// <para>
/// This catches a broken client. It does not form a view on whether an expense is legitimate: that is a
/// human's job, and nothing in this app second-guesses it.
/// </para>
/// </remarks>
public static class ExpenseTotals
{
    /// <summary>
    /// Sum of every line's gross, and of the GST shown. Rounded to cents at each step rather than only
    /// at the end - the column is <c>decimal(12,2)</c>, so a sum carrying more scale would be silently
    /// truncated on write and the stored total would not equal the stored lines.
    /// </summary>
    public static (decimal Gross, decimal Gst) SumLines(IEnumerable<ExpenseLine> lines)
    {
        decimal gross = 0m;
        decimal gst = 0m;

        foreach (ExpenseLine line in lines)
        {
            gross += Money(line.GrossAmount);
            gst += Money(line.GstAmount ?? 0m);
        }

        return (Money(gross), Money(gst));
    }

    /// <summary>
    /// `NET AUTHORISED CHURCH EXPENSE` / `TOTAL REIMBURSEMENT CLAIMED` - the gross less the personal
    /// portion the claimant declared.
    /// </summary>
    /// <remarks>
    /// Not clamped at zero. A negative net means the claimant said more was personal than was spent,
    /// which is a mistake worth showing a reviewer rather than quietly rounding away.
    /// </remarks>
    public static decimal Net(decimal gross, decimal lessPersonal) => Money(gross - Money(lessPersonal));

    /// <summary>
    /// <c>MidpointRounding.ToEven</c> - banker's rounding - so a long run of half-cents does not drift
    /// upward the way <c>AwayFromZero</c> does.
    /// </summary>
    public static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);
}
