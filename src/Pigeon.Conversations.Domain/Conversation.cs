namespace Pigeon.Conversations.Domain;

public enum ConversationKind
{
    Direct
}

/// <summary>
/// The conversation state, rebuilt from events ("evolve"): Create handles the first event of a stream,
/// Apply (later) each following event. Business rules for commands live in ConversationDecisions.
/// Plain C#: Marten calls these through ConversationProjection in the Api project.
/// </summary>
public sealed record Conversation(
    Guid Id,
    Guid TenantId,
    ConversationKind Kind,
    IReadOnlyList<Guid> MemberIds,
    DateTimeOffset StartedAt)
{
    public static Conversation Create(DirectConversationStarted e) =>
        new(e.ConversationId, e.TenantId, ConversationKind.Direct, [e.InitiatorId, e.PeerId], e.StartedAt);
}
