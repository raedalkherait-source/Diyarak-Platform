using Diyarak.Api.Authentication;
using Diyarak.Api.Authentication.Phone;
using Diyarak.Api.Market;
using Diyarak.Api.Moderation;
using Diyarak.Api.Payments;
using Diyarak.Api.RateLimiting;
using Diyarak.Api.SavedSearchAlerts;
using Diyarak.Market.Application;
using Diyarak.Market.Application.Photos;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Persistence.PostgreSql;
using Diyarak.Platform.Storage.FileSystem;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;

const string CorsPolicy = "frontend";
const string CorrelationHeader = "X-Correlation-ID";

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Connection string 'Postgres' is not configured.");

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? [];

builder.Services.AddOpenApi();

builder.Services.AddDiyarakAuthentication(
    builder.Configuration);

PhoneAuthenticationOptions phoneAuthentication =
    builder.Services.AddPhoneAuthentication(
        builder.Configuration,
        builder.Environment);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestPropertiesAndHeaders |
        HttpLoggingFields.ResponsePropertiesAndHeaders |
        HttpLoggingFields.Duration;

    options.RequestHeaders.Add(CorrelationHeader);
    options.ResponseHeaders.Add(CorrelationHeader);

    options.CombineLogs = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        CorsPolicy,
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services
   .AddHealthChecks()
   .AddCheck(
       "self",
       () => HealthCheckResult.Healthy(),
       tags: ["ready"])
   .AddDbContextCheck<PlatformDbContext>(
       name: "postgresql",
       failureStatus: HealthStatus.Unhealthy,
       tags: ["ready"]);

builder.Services.AddClientRateLimiting(
    builder.Configuration);

bool trustedProxiesConfigured =
    builder.Services.AddTrustedProxies(builder.Configuration);

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30),
        TimeoutStatusCode =
            StatusCodes.Status504GatewayTimeout
    };
});

builder.Services.AddPostgreSqlPersistence(
    postgresConnectionString);

builder.Services.AddScoped<CreatePropertyUseCase>();
builder.Services.AddScoped<ListOwnedPropertiesUseCase>();
builder.Services.AddScoped<GetPropertyUseCase>();
builder.Services.AddScoped<UpdatePropertyUseCase>();
builder.Services.AddScoped<CreateListingUseCase>();
builder.Services.AddScoped<ListOwnedListingsUseCase>();
builder.Services.AddScoped<GetListingUseCase>();
builder.Services.AddScoped<GetPublishedListingUseCase>();
builder.Services.AddScoped<ListPublishedListingsUseCase>();
builder.Services.AddScoped<UpdateListingUseCase>();
builder.Services.AddScoped<PublishListingUseCase>();
builder.Services.AddScoped<ChangeListingStatusUseCase>();
builder.Services.AddScoped<DeleteDraftListingUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Favorites.AddListingFavoriteUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Favorites.RemoveListingFavoriteUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Favorites.GetListingFavoriteStatusUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Favorites.ListListingFavoritesUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.SavedSearches.CreateSavedSearchUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.SavedSearches.ListSavedSearchesUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.SavedSearches.MarkSavedSearchSeenUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.SavedSearches.DeleteSavedSearchUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.SavedSearches.SetSavedSearchAlertsUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Statistics.RecordListingViewUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Statistics.GetListingStatisticsUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Statistics.ListOwnedListingStatisticsUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.SaveMyAgencyUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.GetMyAgencyUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.RequestAgencyVerificationUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.DecideAgencyVerificationUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.GetPublicAgencyUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.AssignAgencyPackageUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.RevokeAgencyPackageUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.Agencies.GetListingQuotaUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.BulkImport.BulkImportListingsUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.OffPlanProjects.CreateOffPlanProjectUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.OffPlanProjects.UpdateOffPlanProjectUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.OffPlanProjects.PublishOffPlanProjectUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.OffPlanProjects.DeleteOffPlanProjectUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.OffPlanProjects.ListMyOffPlanProjectsUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.OffPlanProjects.ListPublicOffPlanProjectsUseCase>();
builder.Services.AddScoped<Diyarak.Market.Application.OffPlanProjects.GetPublicOffPlanProjectUseCase>();
builder.Services.AddModeration(builder.Configuration);
builder.Services.AddPayments();
// After phone authentication, which registers the SMS gateway the alerts reuse.
builder.Services.AddSavedSearchAlerts(builder.Configuration, builder.Environment);

builder.Services.TryAddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddSingleton<IListingPhotoStorage>(
    _ => new FileSystemListingPhotoStorage(
        builder.Configuration["Media:FileSystem:RootPath"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "media")));
builder.Services.AddScoped<UploadListingPhotoUseCase>();
builder.Services.AddScoped<ListListingPhotosUseCase>();
builder.Services.AddScoped<DeleteListingPhotoUseCase>();
builder.Services.AddScoped<GetListingPhotoContentUseCase>();
builder.Services.AddScoped<GetListingContactUseCase>();

var app = builder.Build();

if (trustedProxiesConfigured)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpLogging();

app.Use(async (context, next) =>
{
    string correlationId;

    if (context.Request.Headers.TryGetValue(
            CorrelationHeader,
            out var suppliedCorrelationId) &&
        suppliedCorrelationId.Count == 1 &&
        !string.IsNullOrWhiteSpace(suppliedCorrelationId[0]) &&
        suppliedCorrelationId[0]!.Length <= 64)
    {
        correlationId = suppliedCorrelationId[0]!;
    }
    else
    {
        correlationId = context.TraceIdentifier;
    }

    context.TraceIdentifier = correlationId;

    context.Response.Headers[CorrelationHeader] =
        correlationId;

    await next(context);
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] =
        "nosniff";

    context.Response.Headers["X-Frame-Options"] =
        "DENY";

    context.Response.Headers["Referrer-Policy"] =
        "no-referrer";

    context.Response.Headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=()";

    bool isDevelopmentFrontend =
        app.Environment.IsDevelopment() &&
        context.Request.Path.StartsWithSegments("/app");

    if (isDevelopmentFrontend)
    {
        context.Response.OnStarting(
            () =>
            {
                context.Response.Headers["Cache-Control"] =
                    "no-store";

                return Task.CompletedTask;
            });
    }

    context.Response.Headers["Content-Security-Policy"] =
        isDevelopmentFrontend
            ? "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; base-uri 'self'; form-action 'none'; frame-ancestors 'none'"
            : "default-src 'none'; frame-ancestors 'none'";

    await next(context);
});

app.UseRouting();

app.Use(async (context, next) =>
{
    if (context.GetEndpoint()?.Metadata
            .GetMetadata<MarketManagementEndpointMetadata>() is not null)
    {
        context.Response.OnStarting(
            static state =>
            {
                var response = (HttpResponse)state;
                response.Headers["Cache-Control"] = "no-store";
                return Task.CompletedTask;
            },
            context.Response);
    }

    await next(context);
});

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestTimeouts();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    var frontendPath = Path.GetFullPath(
        Path.Combine(
            app.Environment.ContentRootPath,
            "..",
            "..",
            "Frontend",
            "Diyarak.Web"));

    if (Directory.Exists(frontendPath))
    {
        app.UseDefaultFiles(
            new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(frontendPath),
                RequestPath = "/app"
            });

        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(frontendPath),
                RequestPath = "/app"
            });
    }
}

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check =>
            check.Tags.Contains("ready")
    });

app.MapHealthChecks("/health");

app.MapGet(
        "/",
        () =>
            Results.Ok(new
            {
                service = "Diyarak.Api",
                status = "Running",
            }));

app.MapPublicMarketListingEndpoints();
app.MapPublicMarketListingPhotoEndpoints();
app.MapPublicListingContactEndpoints();
app.MapPublicListingViewEndpoints();
app.MapPublicAgencyEndpoints();
app.MapPublicListingReportEndpoints();

if (phoneAuthentication.Enabled)
{
    app.MapPhoneAuthenticationEndpoints();
}

if (phoneAuthentication.Enabled ||
    builder.Configuration.GetValue<bool>(
        $"{OidcAuthenticationOptions.SectionName}:Enabled"))
{
    app.MapMarketPropertyEndpoints();
    app.MapMarketListingEndpoints();
    app.MapMarketListingPhotoManagementEndpoints();
    app.MapMarketFavoriteEndpoints();
    app.MapMarketSavedSearchEndpoints();
    app.MapMarketListingStatisticsEndpoints();
    app.MapMarketAgencyEndpoints();
    app.MapMarketBulkImportEndpoints();
    app.MapMarketOffPlanProjectEndpoints();
    app.MapPaymentEndpoints();
    app.MapModerationEndpoints();
}

app.Run();

public partial class Program { }
