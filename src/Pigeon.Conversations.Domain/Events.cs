namespace Pigeon.Conversations.Domain;

// Events are facts in the past tense. Once stored they never change, so they are immutable records.
// Renaming a property later would break reading old events, so choose names carefully.

/// <summary>
/// A user started a one-to-one conversation with another user.
/// </summary>
public sealed record DirectConversationStarted(
    Guid ConversationId,
    Guid TenantId,
    Guid InitiatorId,
    Guid PeerId,
    DateTimeOffset StartedAt
);
