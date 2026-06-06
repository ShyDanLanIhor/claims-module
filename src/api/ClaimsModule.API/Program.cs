using System.Text.Json.Serialization;
using ClaimsModule.API.Auth;
using ClaimsModule.API.Filters;
using ClaimsModule.API.Middleware;
using ClaimsModule.Application;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Infrastructure;
using ClaimsModule.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Aspire: service discovery, resilience, health checks, and OpenTelemetry.
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("ClaimsDb")
    ?? "Server=(localdb)\\mssqllocaldb;Database=ClaimsModule;Trusted_Connection=True;MultipleActiveResultSets=true";

// --- Application composition ------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(connectionString);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --- Web ------------------------------------------------------------------------------------
// IdempotencyFilter implements the FRS §10 Idempotency-Key contract globally (write methods only).
builder.Services.AddControllers(o => o.Filters.Add<IdempotencyFilter>()).AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// Model-binding / shape errors (bad enum value, malformed or missing body) must use the SAME §10.4
// 422 envelope as FluentValidation — not the framework's default 400 ProblemDetails.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors
                    .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "The value is invalid." : x.ErrorMessage)
                    .ToArray());

        return new Microsoft.AspNetCore.Mvc.ObjectResult(new
        {
            type = "ValidationError",
            title = "One or more validation errors occurred.",
            status = StatusCodes.Status422UnprocessableEntity,
            errors,
            traceId = ctx.HttpContext.TraceIdentifier
        })
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity
        };
    });

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 60_000_000); // ~50 MB + headroom

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
    o.SwaggerDoc("v1", new() { Title = "Claims Module API", Version = "v1" }));

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddPolicy("AllowAngular", policy =>
    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

// --- Hangfire --------------------------------------------------------------------------------
builder.Services.AddHangfire((sp, cfg) => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString)
    // Terminal GL-posting failure handling (FRS §12.1): write Failed + GL_POSTING_FAILED once retries exhaust.
    .UseFilter(sp.GetRequiredService<ClaimsModule.Infrastructure.Jobs.GlPostingFailureStateFilter>()));

// The processing server is toggleable so tests can host the API without a database.
if (builder.Configuration.GetValue("Hangfire:EnableServer", true))
    builder.Services.AddHangfireServer();

var app = builder.Build();

// Apply EF migrations FIRST. This creates the database (if missing) and its schema BEFORE anything
// else touches the data tier — critically, before the Hangfire dashboard/server construct the Hangfire
// SQL storage, whose constructor installs the Hangfire schema. If Hangfire's installer runs against a
// not-yet-created database it gives up, leaving its tables missing ("Invalid object name 'HangFire.Job'").
await ApplyMigrationsIfConfiguredAsync(app);

// Aspire health endpoints (/health, /alive).
app.MapDefaultEndpoints();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger/OpenAPI is served in ALL environments: the deployed app must expose it (Assessment §4.3),
// and the Azure App Service runs as Production (ASPNETCORE_ENVIRONMENT=Production).
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");

// Serve locally-stored documents (LocalFileSystem storage provider fallback) at /uploads.
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthorization();
app.MapControllers();

// Map the dashboard only when the processing server is enabled. UseHangfireDashboard eagerly resolves
// JobStorage, which constructs the SQL Server storage and OPENS a connection — so mapping it
// unconditionally breaks infrastructure-free hosting (e.g. the integration tests / any host without SQL),
// even though the server itself is disabled. Gated like the server + PrepareHangfireStorage below.
if (app.Configuration.GetValue("Hangfire:EnableServer", true))
    app.UseHangfireDashboard("/hangfire");

// Force Hangfire's SQL storage to materialise now (its constructor installs the schema) so the first
// reserve enqueue cannot hit missing Hangfire tables. The database already exists (migrated above).
PrepareHangfireStorage(app);

// Recurring SLA monitor every 15 minutes (FRS §12.2).
RegisterRecurringJobs(app.Services, app.Logger);

app.Run();

static void PrepareHangfireStorage(WebApplication app)
{
    if (!app.Configuration.GetValue("Hangfire:EnableServer", true))
        return;

    try
    {
        _ = app.Services.GetRequiredService<JobStorage>();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Hangfire storage initialization was skipped (is the database available?).");
    }
}

static void RegisterRecurringJobs(IServiceProvider services, ILogger logger)
{
    try
    {
        var scheduler = services.GetRequiredService<IBackgroundJobScheduler>();
        scheduler.AddOrUpdateRecurring<ISlaMonitoringJob>(
            "sla-monitoring", job => job.ScanAsync(default), "*/15 * * * *");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not register recurring jobs (is the database available?).");
    }
}

static async Task ApplyMigrationsIfConfiguredAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:ApplyMigrationsAtStartup", app.Environment.IsDevelopment()))
        return;

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database migration on startup was skipped (is the database available?).");
    }
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
