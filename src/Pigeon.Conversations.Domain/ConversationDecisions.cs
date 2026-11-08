namespace Pigeon.Conversations.Domain;

/// <summary>
/// Business rules for commands ("decide"): check the command and return the new event(s).
/// Nothing here changes state or touches storage; the caller appends the returned events.
/// </summary>
public static class ConversationDecisions
{
    public static DirectConversationStarted StartDirect(
        Guid conversationId,
        Guid tenantId,
        Guid initiatorId,
        Guid peerId,
        DateTimeOffset now)
    {
        if (initiatorId == Guid.Empty || peerId == Guid.Empty)
            throw new DomainException("Both participants are required.");

        if (initiatorId == peerId)
            throw new DomainException("A direct conversation needs two different users.");

        return new DirectConversationStarted(conversationId, tenantId, initiatorId, peerId, now);
    }
}
