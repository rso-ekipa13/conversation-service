namespace Pigeon.Conversations.Domain;

/// <summary>
/// A command broke a business rule. The API turns this into 400 Bad Request.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
