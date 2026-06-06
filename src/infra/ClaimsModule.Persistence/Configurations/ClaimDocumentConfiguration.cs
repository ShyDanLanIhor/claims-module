using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimDocumentConfiguration : IEntityTypeConfiguration<ClaimDocument>
{
    public void Configure(EntityTypeBuilder<ClaimDocument> builder)
    {
        builder.ToTable("ClaimDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.OrganisationId).IsRequired();
        builder.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.DocumentName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.BlobPath).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.HasIndex(d => d.ClaimId);
    }
}
