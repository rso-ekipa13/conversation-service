using Marten.Events.Aggregation;
using Pigeon.Conversations.Domain;

namespace Pigeon.Conversations.Api.Projections;

/// <summary>
/// Adapter between Marten and the domain: tells Marten how to build a Conversation document from its stream.
/// Each method only delegates to the domain, so the rules stay in Pigeon.Conversations.Domain.
/// Must be partial: Marten's source generator (JasperFx.Events.SourceGenerator, brought in by the Marten package)
/// adds the code that dispatches each event type to the matching method at compile time.
/// </summary>
public sealed partial class ConversationProjection : SingleStreamProjection<Conversation, Guid>
{
    public Conversation Create(DirectConversationStarted e) => Conversation.Create(e);
}
