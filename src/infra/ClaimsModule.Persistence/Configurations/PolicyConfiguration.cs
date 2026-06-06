using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganisationId).IsRequired();
        builder.Property(p => p.PolicyNumber).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ClientName).HasMaxLength(255).IsRequired();
        builder.Property(p => p.EffectiveDate).HasColumnType("date");
        builder.Property(p => p.ExpirationDate).HasColumnType("date");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.CoverageTypes).HasMaxLength(500).IsRequired();

        builder.HasIndex(p => new { p.OrganisationId, p.PolicyNumber }).IsUnique();

        // FRS §5.5: minimum required simulated policy dataset.
        builder.HasData(
            Policy("POL-2024-001001", "Meridian Transport LLC", new(2024, 1, 1), new(2026, 12, 31), PolicyStatus.Active, "Vehicle,Cargo"),
            Policy("POL-2024-001002", "Harborview Properties Inc", new(2024, 6, 1), new(2026, 5, 31), PolicyStatus.Active, "Property,Liability"),
            Policy("POL-2025-002001", "Coastal Builders Group", new(2025, 3, 1), new(2027, 2, 28), PolicyStatus.Active, "Property,Equipment"),
            Policy("POL-2025-002002", "Stanton Medical Group", new(2025, 1, 1), new(2026, 12, 31), PolicyStatus.Active, "Liability,Vehicle"),
            Policy("POL-2023-000099", "Archived Corp", new(2020, 1, 1), new(2021, 12, 31), PolicyStatus.Expired, "Property"));
    }

    private static Policy Policy(
        string number, string client, DateOnly effective, DateOnly expiration, PolicyStatus status, string coverage) => new()
    {
        Id = SeedConstants.DeterministicGuid($"policy:{number}"),
        OrganisationId = SeedConstants.OrganisationId,
        PolicyNumber = number,
        ClientName = client,
        EffectiveDate = effective,
        ExpirationDate = expiration,
        Status = status,
        CoverageTypes = coverage
    };
}
