using System.Text.Json;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Microsoft.JSInterop;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Keeps an in-progress form in the browser's <c>localStorage</c>, one slot per kind.
/// </summary>
/// <remarks>
/// <b>There are deliberately no server-side drafts.</b> The pages are anonymous, so a draft row would
/// belong to nobody: either it is unrecoverable, or it is enumerable by anyone who guesses a URL. Since
/// a half-filled reimbursement form contains a claimant's name and contact details, that is a real
/// leak for no gain. Keeping it in the browser costs one less table and one less way to lose it.
/// <para>
/// The consequence to be honest about: a draft lives on <b>one</b> browser on <b>one</b> device. Clear
/// the site data, switch to a phone, or open a private window, and it is gone. That is the trade the
/// scope makes, not an oversight.
/// </para>
/// <para>
/// Every call is wrapped: <c>localStorage</c> throws outright in some contexts (private windows,
/// browsers set to block site data, thumbnail capture) rather than returning empty. A form that will not
/// render because it could not read a draft would be a much worse failure than a form that lost one.
/// </para>
/// </remarks>
public class DraftStore(IJSRuntime js)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private static string Key(SubmissionKind kind) => $"gsbc.accounting.draft.{kind}";

    public async Task SaveAsync(ExpenseFormModel model)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Key(model.Kind), JsonSerializer.Serialize(model, Options));
        }
        catch
        {
            // Storage unavailable or full. The form still works; it just will not survive a reload.
        }
    }

    public async Task<ExpenseFormModel?> LoadAsync(SubmissionKind kind)
    {
        try
        {
            string? json = await js.InvokeAsync<string?>("localStorage.getItem", Key(kind));

            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<ExpenseFormModel>(json, Options);
        }
        catch
        {
            // Unreadable or written by an older shape of the model. Start clean rather than fail - a
            // draft is a convenience, and no draft is a perfectly good state.
            return null;
        }
    }

    public async Task ClearAsync(SubmissionKind kind)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.removeItem", Key(kind));
        }
        catch
        {
            // Nothing to do. Worst case a stale draft reappears on the next visit.
        }
    }
}
