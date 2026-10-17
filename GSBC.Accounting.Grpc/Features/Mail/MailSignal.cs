namespace GSBC.Accounting.Grpc.Features.Mail;

/// <summary>
/// Wakes the sender as soon as a claim is submitted, rather than at its next poll.
/// </summary>
/// <remarks>
/// Only a nudge. The rows in <c>OutgoingEmails</c> are the source of truth, and the poll would find them
/// anyway - this just means a claimant's receipt arrives while they are still looking at the
/// confirmation, instead of up to a minute later. A wake on another replica's submit is missed, and
/// that replica's own sender handles it.
/// </remarks>
public sealed class MailSignal
{
    private readonly SemaphoreSlim _wake = new(0, 1);

    public void Wake()
    {
        // Already signalled is as good as signalled twice.
        try
        {
            _wake.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken token) => _wake.WaitAsync(timeout, token);
}
