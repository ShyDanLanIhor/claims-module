using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class CauseOfLossCodeConfiguration : IEntityTypeConfiguration<CauseOfLossCode>
{
    private static readonly (string Code, string Name, PerilCategory Peril)[] Seed =
    [
        ("COL-FIRE", "Fire", PerilCategory.Property),
        ("COL-FLOOD", "Flood", PerilCategory.Weather),
        ("COL-THEFT", "Theft", PerilCategory.Crime),
        ("COL-VEH-COL", "Vehicle Collision", PerilCategory.Auto),
        ("COL-VEH-COMP", "Vehicle Comprehensive", PerilCategory.Auto),
        ("COL-LIAB", "Third Party Liability", PerilCategory.Liability),
        ("COL-EQUIP", "Equipment Breakdown", PerilCategory.Equipment),
        ("COL-WIND", "Wind / Storm", PerilCategory.Weather),
        ("COL-INJURY", "Bodily Injury", PerilCategory.Liability),
        ("COL-OTHER", "Other / Unknown", PerilCategory.General)
    ];

    public void Configure(EntityTypeBuilder<CauseOfLossCode> builder)
    {
        builder.ToTable("CauseOfLossCodes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(255).IsRequired();
        builder.Property(c => c.PerilCategory).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.IsActive).HasDefaultValue(true); // §15.1: every BIT NOT NULL carries a default
        builder.HasIndex(c => c.Code).IsUnique();

        // FRS §5.6 / §15.4: seed active reference data via HasData.
        builder.HasData(Seed.Select((s, i) => new CauseOfLossCode
        {
            Id = SeedConstants.DeterministicGuid($"cause:{s.Code}"),
            Code = s.Code,
            Name = s.Name,
            PerilCategory = s.Peril,
            IsActive = true,
            SortOrder = (i + 1) * 10
        }));
    }
}
