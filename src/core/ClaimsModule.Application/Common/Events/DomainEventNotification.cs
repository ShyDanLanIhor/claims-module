using ClaimsModule.Domain.Common;
using MediatR;

namespace ClaimsModule.Application.Common.Events;

/// <summary>
/// MediatR wrapper around a framework-agnostic <see cref="IDomainEvent"/>. The persistence layer
/// drains domain events from tracked aggregates and publishes them as these notifications, so the
/// Domain stays free of any MediatR dependency while Application handlers can subscribe.
/// </summary>
public sealed class DomainEventNotification<TDomainEvent>(TDomainEvent domainEvent) : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; } = domainEvent;
}
