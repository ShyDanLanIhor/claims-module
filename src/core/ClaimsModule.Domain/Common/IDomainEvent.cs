namespace ClaimsModule.Domain.Common;

/// <summary>
/// Marker for a domain event. Kept framework-agnostic (no MediatR reference) so the Domain
/// layer depends on nothing. Events are wrapped into MediatR notifications at the Application
/// boundary and dispatched by the persistence layer on save.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

/// <summary>Convenience base record stamping <see cref="OccurredOn"/> at creation time.</summary>
public abstract record DomainEventBase : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
