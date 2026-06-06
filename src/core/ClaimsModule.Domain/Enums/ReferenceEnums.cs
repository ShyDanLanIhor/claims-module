namespace ClaimsModule.Domain.Enums;

/// <summary>Peril grouping for a cause-of-loss code (FRS §9.9).</summary>
public enum PerilCategory
{
    Property,
    Auto,
    Liability,
    Weather,
    Equipment,
    Crime,
    General
}

/// <summary>Status of a (simulated) policy (FRS §9.10).</summary>
public enum PolicyStatus
{
    Active,
    Expired,
    Cancelled
}

/// <summary>User roles and their reserve authority (FRS §3).</summary>
public enum UserRole
{
    Handler,
    Supervisor,
    Manager
}
