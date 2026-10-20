using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Api.Endpoints;
using PrescriptionRefill.Api.Errors;
using PrescriptionRefill.Api.Realtime;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application;
using PrescriptionRefill.Infrastructure;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("RefillDb")
    ?? throw new InvalidOperationException("Connection string 'RefillDb' is not configured.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        // Framework-produced problems (malformed JSON, unknown routes) get a contract code too.
        if (!context.ProblemDetails.Extensions.ContainsKey("code"))
        {
            context.ProblemDetails.Extensions["code"] = context.ProblemDetails.Status switch
            {
                StatusCodes.Status400BadRequest => ErrorCodes.ValidationFailed,
                StatusCodes.Status404NotFound => ErrorCodes.NotFound,
                _ => ErrorCodes.UnexpectedError,
            };
        }
    });
builder.Services.AddExceptionHandler<UnexpectedErrorHandler>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRequestNotifier, SignalRRequestNotifier>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (args.Contains("--seed"))
{
    // Applies pending migrations, seeds fictional demo data, and exits (research R6).
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RefillDbContext>();
    await db.Database.MigrateAsync();
    var seeded = await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    app.Logger.LogInformation(seeded ? "Demo data seeded." : "Database already contains data; seeding skipped.");
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapRefillRequestEndpoints();
app.MapHub<RefillRequestsHub>(RefillRequestsHub.Path);

app.Run();

public partial class Program;
