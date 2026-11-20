using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity.Application;

namespace Diyarak.Api.Authentication.Phone;

/// <summary>
/// Writes one-time codes to the application log. Startup refuses this adapter outside the
/// Development environment; production requires a real SMS/WhatsApp adapter.
/// </summary>
internal sealed partial class DevelopmentLogOtpSender(
    ILogger<DevelopmentLogOtpSender> logger)
    : IOtpSender
{
    public Task SendAsync(
        PhoneNumber phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        LogCode(logger, phoneNumber.Value, code);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "DEVELOPMENT ONLY: verification code for {PhoneNumber} is {Code}")]
    private static partial void LogCode(
        ILogger logger,
        string phoneNumber,
        string code);
}
