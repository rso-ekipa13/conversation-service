using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Health checks. Each check we register later (e.g. PostgreSQL) gets the tag "ready".
builder.Services.AddHealthChecks();

var app = builder.Build();

// Liveness: is the process alive and able to answer HTTP at all?
// Runs no checks on purpose. If the database is down, restarting this pod would not help,
// so a dependency failure must never make liveness fail.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: can this instance serve traffic right now?
// Runs only checks tagged "ready". When one fails, Kubernetes stops routing traffic
// to the pod but does not restart it.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapGet("/", () => "conversation-service");

app.Run();

// Makes Program visible to integration tests (WebApplicationFactory<Program>).
public partial class Program;
