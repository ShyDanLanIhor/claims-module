using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimPartyConfiguration : IEntityTypeConfiguration<ClaimParty>
{
    public void Configure(EntityTypeBuilder<ClaimParty> builder)
    {
        builder.ToTable("ClaimParties");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganisationId).IsRequired();
        builder.Property(p => p.PartyRole).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.PartyType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Property(p => p.CompanyName).HasMaxLength(255);
        builder.Property(p => p.Email).HasMaxLength(255);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Ignore(p => p.DisplayName);

        builder.HasIndex(p => p.ClaimId);
    }
}
