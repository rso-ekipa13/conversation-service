using Marten;
using Microsoft.AspNetCore.Mvc;
using Pigeon.Conversations.Domain;

namespace Pigeon.Conversations.Api.Endpoints;

public static class ConversationEndpoints
{
    public sealed record StartDirectConversationRequest(Guid PeerId);

    public sealed record StartDirectConversationResponse(Guid ConversationId);

    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations");
        group.MapPost("/direct", StartDirect);
        group.MapGet("/{id:guid}", GetById);
        return app;
    }

    // Write side (command). The gateway validates the JWT and passes the caller as headers (ADR-0006),
    // so the service never trusts a user id from the request body.
    private static async Task<IResult> StartDirect(
        StartDirectConversationRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        IDocumentSession session,
        TimeProvider clock,
        CancellationToken ct)
    {
        DirectConversationStarted started;
        try
        {
            // Version 7 GUIDs are ordered by time, which keeps database indexes compact.
            started = ConversationDecisions.StartDirect(
                Guid.CreateVersion7(), tenantId, userId, request.PeerId, clock.GetUtcNow());
        }
        catch (DomainException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        // Appends the event as the first one of a new stream. Nothing is written until SaveChangesAsync,
        // which stores the event and the inline snapshot in one transaction.
        session.Events.StartStream<Conversation>(started.ConversationId, started);
        await session.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/conversations/{started.ConversationId}",
            new StartDirectConversationResponse(started.ConversationId));
    }

    // Read side (query). Loads the snapshot document; no events are replayed.
    private static async Task<IResult> GetById(Guid id, IQuerySession session, CancellationToken ct)
    {
        var conversation = await session.LoadAsync<Conversation>(id, ct);
        return conversation is null ? TypedResults.NotFound() : TypedResults.Ok(conversation);
    }
}
