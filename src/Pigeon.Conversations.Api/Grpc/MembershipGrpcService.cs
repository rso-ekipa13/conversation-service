using Grpc.Core;
using Marten;
using Pigeon.Conversations.Domain;
using Pigeon.Conversations.Grpc.V1;

namespace Pigeon.Conversations.Api.Grpc;

/// <summary>
/// gRPC adapter for the Membership service defined in protos/pigeon/conversations/v1/membership.proto.
/// Membership.MembershipBase is generated from the .proto at build time; we override its methods.
/// Generated types (GetMembersRequest, Member, ...) and RpcException stay in this class and never reach the domain.
/// </summary>
public sealed class MembershipGrpcService(IQuerySession session) : Membership.MembershipBase
{
    public override async Task<GetMembersResponse> GetMembers(GetMembersRequest request, ServerCallContext context)
    {
        var tenantId = ParseUuid(request.TenantId, "tenant_id");
        var conversationId = ParseUuid(request.ConversationId, "conversation_id");

        // context.CancellationToken fires when the caller cancels or its deadline expires,
        // so we stop working on an answer nobody is waiting for.
        var conversation = await session.LoadAsync<Conversation>(conversationId, context.CancellationToken);

        // A conversation of another tenant is reported as missing, so a caller cannot probe other tenants.
        if (conversation is null || conversation.TenantId != tenantId)
            throw new RpcException(new Status(StatusCode.NotFound, "Conversation not found."));

        var response = new GetMembersResponse();
        response.Members.AddRange(conversation.MemberIds.Select(id => new Member { UserId = id.ToString() }));
        return response;
    }

    private static Guid ParseUuid(string value, string field) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"{field} must be a non-empty UUID."));
}
