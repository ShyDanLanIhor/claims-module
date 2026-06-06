using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimRiskObjectConfiguration : IEntityTypeConfiguration<ClaimRiskObject>
{
    public void Configure(EntityTypeBuilder<ClaimRiskObject> builder)
    {
        builder.ToTable("ClaimRiskObjects");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrganisationId).IsRequired();
        builder.Property(r => r.AssetType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.AssetDescription).HasMaxLength(500).IsRequired();
        builder.Property(r => r.AssetReference).HasMaxLength(255);
        builder.Property(r => r.IsPrimary).HasDefaultValue(false); // §15.1 BIT NOT NULL DEFAULT 0

        builder.HasIndex(r => r.ClaimId);
    }
}
