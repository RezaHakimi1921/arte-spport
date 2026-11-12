namespace Arte.Sport.Api.Sms;

public interface ISmsSender
{
    Task SendOtpAsync(string mobile, string code, CancellationToken ct);
}

public sealed class SmsNotConfiguredException : Exception;

/// <summary>Test and development sink. The code stays in memory and is never written in production.</summary>
public sealed class FakeSmsSender : ISmsSender
{
    public Dictionary<string, string> Sent { get; } = new();

    public Task SendOtpAsync(string mobile, string code, CancellationToken ct)
    {
        Sent[mobile] = code;
        return Task.CompletedTask;
    }
}

public sealed class UnavailableSmsSender : ISmsSender
{
    public Task SendOtpAsync(string mobile, string code, CancellationToken ct) =>
        throw new SmsNotConfiguredException();
}
