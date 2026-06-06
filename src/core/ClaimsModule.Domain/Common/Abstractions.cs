namespace ClaimsModule.Domain.Common;

/// <summary>Audit columns required on every business table (see FRS §15.1).</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UserCreated { get; set; }
    Guid? UserModified { get; set; }
}

/// <summary>Soft-delete columns; enforced via a global query filter in EF Core.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Multi-tenant isolation. A single fixed OrganisationId is seeded for the assessment.</summary>
public interface ITenantScoped
{
    Guid OrganisationId { get; set; }
}

/// <summary>
/// Marks an aggregate root — the only entities a repository hands out and the boundary of a
/// transactional consistency unit. Aggregate roots also carry a <c>RowVer</c> for optimistic concurrency.
/// </summary>
public interface IAggregateRoot
{
    byte[] RowVersion { get; set; }
}
