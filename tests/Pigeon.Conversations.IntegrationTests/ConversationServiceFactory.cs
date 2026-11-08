using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Pigeon.Conversations.Grpc.V1;
using Testcontainers.PostgreSql;

namespace Pigeon.Conversations.IntegrationTests;

/// <summary>
/// Runs the whole service in memory (WebApplicationFactory) against a real PostgreSQL in Docker (Testcontainers).
/// xUnit creates one instance per test class (IClassFixture), so the database starts once for all its tests.
/// </summary>
public sealed class ConversationServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same image as compose.yaml, so tests run against the database version we develop with.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    // Runs before the first test: start the container (a fresh, empty database on a random free port).
    public Task InitializeAsync() => _postgres.StartAsync();

    // Called while the service starts, before Program.cs reads its configuration.
    // UseSetting overrides the connection string from appsettings.Development.json.
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Conversations", _postgres.GetConnectionString());

    /// <summary>The generated gRPC client, talking to the in-memory server instead of a network port.</summary>
    public Membership.MembershipClient CreateMembershipClient()
    {
        var http = CreateDefaultClient(new ResponseVersionHandler());
        var channel = GrpcChannel.ForAddress(http.BaseAddress!, new GrpcChannelOptions { HttpClient = http });
        return new Membership.MembershipClient(channel);
    }

    // Explicit, because WebApplicationFactory already has a DisposeAsync with a different return type.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // The in-memory test server answers with HTTP/1.1 as the response version; the gRPC client
    // insists on HTTP/2, so copy the request version onto the response.
    private sealed class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Version = request.Version;
            return response;
        }
    }
}
