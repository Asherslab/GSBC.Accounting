namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Saves a form to the server a short while after the claimant stops typing.
/// </summary>
/// <remarks>
/// <b>This is what replaced the browser-local draft, and the debounce is the whole reason it can.</b>
/// The old <c>DraftStore</c> wrote to <c>localStorage</c> on every keystroke, which is free; a server
/// draft is a gRPC round trip and a database write, and doing that per character would be both slow and
/// rude to a claimant on a phone connection. Waiting for a pause turns a paragraph into one save.
/// <para>
/// <b>The cost, stated plainly: up to <see cref="Delay"/> of typing is not yet on the server.</b> Close
/// the tab mid-sentence and that sentence is gone. Two seconds is the trade - short enough that what is
/// lost is a phrase rather than a section, long enough that a fast typist does not generate a save per
/// word.
/// </para>
/// <para>
/// Cancellation rather than a timer. Each keystroke cancels the pending delay and starts another, so
/// only the last one survives to save; in WebAssembly, where there is one thread and the continuation
/// resumes on it, that is enough to make the save calls strictly sequential without a lock.
/// </para>
/// </remarks>
public sealed class DraftAutosave(Func<Task> save) : IDisposable
{
    /// <summary>
    /// How long a claimant has to stop typing before the form is saved.
    /// </summary>
    public static readonly TimeSpan Delay = TimeSpan.FromSeconds(2);

    private CancellationTokenSource? _pending;

    /// <summary>
    /// Restarts the clock. Called on every edit.
    /// </summary>
    public void Bump()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = new CancellationTokenSource();

        _ = SaveAfterDelayAsync(_pending.Token);
    }

    /// <summary>
    /// Drops a pending save without running it.
    /// </summary>
    /// <remarks>
    /// <b>Called before an explicit Save or Submit, both of which push the form themselves.</b> Without
    /// it, a claimant who types their signature and presses submit inside the debounce window gets two
    /// writes: the explicit one, and then the stale pending one landing a second later. The second is
    /// an <c>Update</c> against a submission that has just become <c>Submitted</c>, so it is refused -
    /// harmlessly, but it puts "this form has already been submitted" on screen underneath the success
    /// message.
    /// </remarks>
    public void Cancel()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }

    private async Task SaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(Delay, token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a later keystroke. The newer delay is the one that will save.
            return;
        }

        await save();
    }

    /// <summary>
    /// Drops the pending save rather than running it. A component being torn down is a page the
    /// claimant has navigated away from, and its half-second-old keystroke is not worth a request
    /// against a form nobody is looking at any more.
    /// </summary>
    public void Dispose() => Cancel();
}
