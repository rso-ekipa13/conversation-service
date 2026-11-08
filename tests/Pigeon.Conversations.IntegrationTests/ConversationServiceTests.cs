using System.Net;
using System.Net.Http.Json;
using Grpc.Core;
using Pigeon.Conversations.Grpc.V1;

namespace Pigeon.Conversations.IntegrationTests;

public sealed class ConversationServiceTests(ConversationServiceFactory factory)
    : IClassFixture<ConversationServiceFactory>
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // The JSON shape the app relies on. Declared here instead of reusing the API's DTO,
    // so renaming a property in the API breaks this test instead of silently breaking clients.
    private sealed record StartedResponse(Guid ConversationId);

    [Fact]
    public async Task Service_starts_and_is_ready_when_database_is_reachable()
    {
        // Also proves that Marten accepts the projection at startup (the error we hit by hand).
        var response = await factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Started_direct_conversation_has_both_users_as_members()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var conversationId = await StartDirectConversation(alice, bob);

        var response = await factory.CreateMembershipClient().GetMembersAsync(
            new GetMembersRequest { TenantId = Tenant.ToString(), ConversationId = conversationId.ToString() },
            deadline: DateTime.UtcNow.AddSeconds(5));

        var members = response.Members.Select(m => Guid.Parse(m.UserId)).Order();
        Assert.Equal(new[] { alice, bob }.Order(), members);
    }

    [Fact]
    public async Task Conversation_of_another_tenant_is_not_found()
    {
        var conversationId = await StartDirectConversation(Guid.NewGuid(), Guid.NewGuid());
        var otherTenant = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await factory.CreateMembershipClient().GetMembersAsync(
                new GetMembersRequest { TenantId = otherTenant.ToString(), ConversationId = conversationId.ToString() }));

        Assert.Equal(StatusCode.NotFound, error.StatusCode);
    }

    [Fact]
    public async Task Invalid_conversation_id_is_an_invalid_argument()
    {
        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await factory.CreateMembershipClient().GetMembersAsync(
                new GetMembersRequest { TenantId = Tenant.ToString(), ConversationId = "abc" }));

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Fact]
    public async Task Direct_conversation_with_yourself_is_rejected()
    {
        var alice = Guid.NewGuid();

        var response = await PostStartDirect(alice, peer: alice);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> StartDirectConversation(Guid initiator, Guid peer)
    {
        var response = await PostStartDirect(initiator, peer);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<StartedResponse>();
        return body!.ConversationId;
    }

    // Sends the headers the gateway would add after validating the JWT.
    private Task<HttpResponseMessage> PostStartDirect(Guid initiator, Guid peer)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/conversations/direct")
        {
            Content = JsonContent.Create(new { peerId = peer })
        };
        request.Headers.Add("X-User-Id", initiator.ToString());
        request.Headers.Add("X-Tenant-Id", Tenant.ToString());
        return factory.CreateClient().SendAsync(request);
    }
}
