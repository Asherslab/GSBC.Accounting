using System.Net.Mail;

namespace GSBC.Accounting.Grpc.Features.Expenses;

/// <summary>
/// Whether a claimant's typed address is one the receipt email can be sent to.
/// </summary>
/// <remarks>
/// <b>A sanity check, not verification.</b> The point is to catch the typo a claimant can fix while the
/// form is still in front of them - a missing <c>@</c>, a <c>gmail</c> with no <c>.com</c> - because the
/// receipt email is the only thing that tells them the claim arrived, and a bad address fails silently
/// from their side. Whether the mailbox exists is something only sending to it answers.
/// <para>
/// <c>MailAddress</c> alone is too forgiving to be the whole test: it accepts a display name and angle
/// brackets (<c>Jo &lt;jo@x.com&gt;</c>) and a dotless domain (<c>jo@localhost</c>). Neither is something
/// a person types into an email box on purpose, so both are refused.
/// </para>
/// </remarks>
public static class EmailAddress
{
    /// <summary>RFC 5321's ceiling on a forward path, and the column's length.</summary>
    public const int MaxLength = 254;

    public static bool IsPlausible(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
            return false;

        string trimmed = value.Trim();

        if (!MailAddress.TryCreate(trimmed, out MailAddress? parsed))
            return false;

        // The address MailAddress parsed has to be the whole of what was typed - otherwise it found an
        // address inside a display name.
        if (!string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase))
            return false;

        string host = parsed.Host;

        return host.Contains('.') && !host.StartsWith('.') && !host.EndsWith('.');
    }
}
