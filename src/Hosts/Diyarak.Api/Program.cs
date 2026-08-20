using System.Threading.RateLimiting;
using Diyarak.Platform.Persistence.PostgreSql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

const string ApiRateLimitPolicy = "api";
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(
        ApiRateLimitPolicy,
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
            limiterOptions.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
        });
});

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

var app = builder.Build();

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

    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'none'; frame-ancestors 'none'";

    await next(context);
});

app.UseRouting();

app.UseCors(CorsPolicy);

app.UseRequestTimeouts();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
        (IHostEnvironment environment) =>
            Results.Ok(new
            {
                service = "Diyarak.Api",
                version = "1.0.0",
                status = "Running",
                environment =
                    environment.EnvironmentName
            }))
    .RequireRateLimiting(ApiRateLimitPolicy);

app.Run();
