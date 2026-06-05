using Kursserver.Utils;

namespace Kursserver.Tests.Integration;

public record SentEmail(string To, string Subject, string Body);

/// <summary>
/// Test double for <see cref="IEmailService"/> that records every send instead of
/// hitting Resend. Registered as a singleton in <see cref="CustomWebApplicationFactory"/>
/// so a test can assert on what was sent after a request completes.
/// </summary>
public class FakeEmailService : IEmailService
{
    private readonly List<SentEmail> _sent = new();
    private readonly object _gate = new();

    public IReadOnlyList<SentEmail> Sent
    {
        get { lock (_gate) { return _sent.ToList(); } }
    }

    public void Clear()
    {
        lock (_gate) { _sent.Clear(); }
    }

    public Task ResendEmailAsync(string toEmail, int passcode)
    {
        lock (_gate) { _sent.Add(new SentEmail(toEmail, "Din lösenkod", passcode.ToString())); }
        return Task.CompletedTask;
    }

    public void SendEmailFireAndForget(string toEmail, string subject, string body)
    {
        lock (_gate) { _sent.Add(new SentEmail(toEmail, subject, body)); }
    }
}
