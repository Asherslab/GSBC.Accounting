using System.Text.Json;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Microsoft.JSInterop;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Who filled the last claim in on this browser, so a new form starts with section 1 already answered.
/// </summary>
/// <remarks>
/// The same twenty or thirty people use this form repeatedly. Name, contact, role and ministry are
/// identical every time and are four of the six required fields in section 1, so a routine morning-tea
/// claim was fifteen fields where it could be six.
/// <para>
/// <b>In <c>localStorage</c>, deliberately, and it is the one thing in this app that belongs there.</b>
/// The draft itself lives on the server owned by the session cookie - the comment in Program.cs about
/// there being no localStorage copy of a draft still holds, and for the same reason: two places to look
/// for one draft is how somebody resumes the older one. This is not a draft. It is a convenience about
/// the person at this keyboard, it is theirs alone, and it must survive the anonymous session expiring,
/// which a server-side answer keyed on that session cannot.
/// </para>
/// <para>
/// It carries no bank details, no card digits and nothing about any purchase. Section 1's identity
/// fields and nothing else - a phone left on a kitchen table shows the next person a name that was
/// already typed into the form in front of them.
/// </para>
/// <para>
/// <b>Every access is wrapped.</b> localStorage throws outright in some contexts - private windows,
/// third-party-cookie blocking - and this is a convenience: a form that fails to open because it could
/// not remember a name is far worse than a form that asks for the name again. Same reasoning as the
/// theme script in index.html.
/// </para>
/// </remarks>
public sealed class ClaimantMemory(IJSRuntime js)
{
    private const string Key = "gsbc.claimant";

    /// <summary>What is remembered. Section 1's identity fields, and nothing else.</summary>
    /// <remarks>
    /// A record of its own rather than a trimmed <c>ExpenseFormModel</c>: what this stores is a
    /// deliberate short list, and reusing the form model would mean every field added to the form
    /// silently joining it.
    /// </remarks>
    public sealed record Claimant
    {
        public string? Name { get; init; }

        /// <summary>Only on the reimbursement form. The card form asks for card digits instead.</summary>
        public string? ContactPhoneEmail { get; init; }

        public ClaimantRole? Role { get; init; }

        public string? RoleOther { get; init; }

        public string? Ministry { get; init; }

        /// <summary>
        /// Whether there is enough here to be worth offering. A stored blank would otherwise announce
        /// "pre-filled from your last claim" over an empty section 1.
        /// </summary>
        public bool IsUseful => !string.IsNullOrWhiteSpace(Name);
    }

    /// <summary>
    /// What the last claim on this browser said, or null when there is nothing worth using - including
    /// when the browser refuses to answer at all.
    /// </summary>
    public async Task<Claimant?> ReadAsync()
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("localStorage.getItem", Key);

            if (string.IsNullOrWhiteSpace(stored))
                return null;

            Claimant? claimant = JsonSerializer.Deserialize<Claimant>(stored);

            return claimant is { IsUseful: true } ? claimant : null;
        }
        catch
        {
            // Unreadable storage, or a stored shape this version no longer understands. Either way the
            // form asks for the name, which is what it did before this existed.
            return null;
        }
    }

    /// <summary>
    /// Remembers the person who just submitted, so their next claim starts filled in.
    /// </summary>
    /// <remarks>
    /// Written on SUBMIT rather than on every autosave. A draft is a form somebody is still deciding
    /// about - half-typed names and all - and remembering one would hand the next claim whatever was on
    /// screen when they gave up.
    /// </remarks>
    public async Task RememberAsync(Claimant claimant)
    {
        if (!claimant.IsUseful)
            return;

        try
        {
            await js.InvokeVoidAsync(
                "localStorage.setItem", Key, JsonSerializer.Serialize(claimant));
        }
        catch
        {
            // See the class remarks: never worth breaking a submitted claim over.
        }
    }
}
