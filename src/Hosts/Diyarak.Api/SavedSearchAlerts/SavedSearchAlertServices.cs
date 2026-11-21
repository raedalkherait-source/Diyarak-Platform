using Diyarak.Api.Market;
using Diyarak.Market.Application;
using Diyarak.Market.Application.SavedSearches;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Messaging.Http;

namespace Diyarak.Api.SavedSearchAlerts;

/// <summary>Configuration section <c>SavedSearchAlerts</c> (ADR-0070).</summary>
internal sealed class SavedSearchAlertOptions
{
    public const string SectionName = "SavedSearchAlerts";

    /// <summary>Off by default: every alert is a paid SMS.</summary>
    public bool Enabled { get; init; }

    /// <summary>Local (Damascus) hours in which alerts may be sent, [start, end).</summary>
    public int WindowStartHour { get; init; } = 10;

    public int WindowEndHour { get; init; } = 20;

    public int IntervalMinutes { get; init; } = 60;

    /// <summary>Upper bound of messages per run, capping cost if something goes wrong.</summary>
    public int MaximumUsersPerRun { get; init; } = 200;

    /// <summary>Public portal URL used in the message, e.g. https://diyarak.app.</summary>
    public Uri? PortalUrl { get; init; }

    public void Validate()
    {
        if (!Enabled)
            return;

        if (WindowStartHour is < 0 or > 23 || WindowEndHour is < 1 or > 24 || WindowStartHour >= WindowEndHour)
            throw new InvalidOperationException($"{SectionName}: the sending window must satisfy 0 <= start < end <= 24.");

        if (IntervalMinutes is < 5 or > 1440 || MaximumUsersPerRun is < 1 or > 10_000)
            throw new InvalidOperationException($"{SectionName}: IntervalMinutes must be 5-1440 and MaximumUsersPerRun 1-10000.");

        if (PortalUrl is not { IsAbsoluteUri: true })
            throw new InvalidOperationException($"{SectionName}:PortalUrl must be an absolute URL.");
    }

    internal string Message(int count) =>
        $"ديارك: {(count >= SavedSearchErrors.NewListingCountCap ? "+99" : count.ToString(System.Globalization.CultureInfo.InvariantCulture))} إعلاناً جديداً يطابق عمليات بحثك المحفوظة.\n{new Uri(PortalUrl!, "/my/searches")}\nلإيقاف التنبيهات افتح الرابط.";
}

/// <summary>Sends alerts as SMS through the configured HTTP gateway to the user's verified phone.</summary>
internal sealed partial class SmsSavedSearchAlertNotifier(
    IPublisherPhoneLookup phones,
    HttpSmsGatewayOtpSender gateway,
    SavedSearchAlertOptions options,
    ILogger<SmsSavedSearchAlertNotifier> logger) : ISavedSearchAlertNotifier
{
    public async Task<bool> NotifyAsync(Guid userId, int newListingCount, CancellationToken cancellationToken = default)
    {
        PhoneNumber? phone = await phones.FindVerifiedPhoneAsync(userId, cancellationToken);
        if (phone is null)
            return false;

        await gateway.SendTextAsync(phone, options.Message(newListingCount), cancellationToken);
        LogSent(logger, userId, newListingCount);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Saved-search alert sent to user {UserId} for {Count} listings.")]
    private static partial void LogSent(ILogger logger, Guid userId, int count);
}

/// <summary>Development stand-in: writes the alert to the log instead of sending an SMS.</summary>
internal sealed partial class LogSavedSearchAlertNotifier(
    IPublisherPhoneLookup phones,
    SavedSearchAlertOptions options,
    ILogger<LogSavedSearchAlertNotifier> logger) : ISavedSearchAlertNotifier
{
    public async Task<bool> NotifyAsync(Guid userId, int newListingCount, CancellationToken cancellationToken = default)
    {
        PhoneNumber? phone = await phones.FindVerifiedPhoneAsync(userId, cancellationToken);
        if (phone is null)
            return false;

        LogAlert(logger, phone.Value, options.Message(newListingCount));
        return true;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "DEVELOPMENT ONLY: saved-search alert for {Phone}: {Message}")]
    private static partial void LogAlert(ILogger logger, string phone, string message);
}

/// <summary>Runs alert batches on an interval inside the local sending window.</summary>
internal sealed partial class SavedSearchAlertWorker(
    IServiceScopeFactory scopes,
    SavedSearchAlertOptions options,
    IClock clock,
    ILogger<SavedSearchAlertWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.IntervalMinutes));

        do
        {
            int hour = Diyarak.Market.Application.Statistics.StatisticsCalendar.LocalHour(clock.UtcNow);
            if (hour >= options.WindowStartHour && hour < options.WindowEndHour)
                await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            SendSavedSearchAlertsUseCase useCase = scope.ServiceProvider.GetRequiredService<SendSavedSearchAlertsUseCase>();
            SavedSearchAlertRun run = await useCase.ExecuteAsync(
                MarketSavedSearchEndpointRouteBuilderExtensions.Parse,
                options.MaximumUsersPerRun,
                cancellationToken);

            if (run.UsersChecked > 0)
                LogRun(logger, run.UsersChecked, run.AlertsSent);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A failed run (gateway or database outage) is retried on the next tick.
            LogFailure(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Saved-search alerts: {Checked} users checked, {Sent} alerts sent.")]
    private static partial void LogRun(ILogger logger, int @checked, int sent);

    [LoggerMessage(Level = LogLevel.Error, Message = "Saved-search alert run failed.")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}

internal static class SavedSearchAlertServiceCollectionExtensions
{
    internal static IServiceCollection AddSavedSearchAlerts(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        SavedSearchAlertOptions options =
            configuration.GetSection(SavedSearchAlertOptions.SectionName).Get<SavedSearchAlertOptions>() ?? new SavedSearchAlertOptions();
        options.Validate();
        services.AddSingleton(options);

        if (!options.Enabled)
            return services;

        bool gatewayConfigured = services.Any(descriptor => descriptor.ServiceType == typeof(HttpSmsGatewayOtpSender));
        if (gatewayConfigured)
            services.AddScoped<ISavedSearchAlertNotifier, SmsSavedSearchAlertNotifier>();
        else if (environment.IsDevelopment())
            services.AddScoped<ISavedSearchAlertNotifier, LogSavedSearchAlertNotifier>();
        else
            throw new InvalidOperationException($"{SavedSearchAlertOptions.SectionName} requires the HTTP SMS gateway (Authentication:Phone:OtpSender = HttpGateway).");

        services.AddScoped<SendSavedSearchAlertsUseCase>();
        services.AddSingleton<SavedSearchAlertWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<SavedSearchAlertWorker>());
        return services;
    }
}
