using JasperFx.Events.Projections;
using Marten;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Pigeon.Conversations.Api;
using Pigeon.Conversations.Api.Endpoints;
using Pigeon.Conversations.Api.Grpc;
using Pigeon.Conversations.Api.Projections;

var builder = WebApplication.CreateBuilder(args);

// Services (dependency injection)

// Locally from appsettings.Development.json; in Kubernetes from the env var ConnectionStrings__Conversations.
var connectionString = builder.Configuration.GetConnectionString("Conversations")
    ?? throw new InvalidOperationException("Connection string 'Conversations' is not configured.");

// Registers IDocumentStore (singleton) and IDocumentSession / IQuerySession (scoped, one per request).
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.DatabaseSchemaName = "conversations";

    // Inline projection: in the same transaction as the events, Marten runs ConversationProjection
    // and stores the resulting Conversation as a document. Reads then load one row instead of replaying events.
    options.Projections.Add<ConversationProjection>(ProjectionLifecycle.Inline);
});

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: [HealthTags.Ready]);

// The current time comes from DI, so tests can replace it with a fake clock.
builder.Services.AddSingleton(TimeProvider.System);

// gRPC server. Reflection lets tools such as grpcurl discover the services; only in Development.
builder.Services.AddGrpc();
if (builder.Environment.IsDevelopment())
    builder.Services.AddGrpcReflection();

var app = builder.Build();

// Endpoints

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthTags.Ready)
});

app.MapGet("/", () => "conversation-service");
app.MapConversationEndpoints();

app.MapGrpcService<MembershipGrpcService>();
if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

app.Run();

// Makes Program visible to integration tests (WebApplicationFactory<Program>).
public partial class Program;
