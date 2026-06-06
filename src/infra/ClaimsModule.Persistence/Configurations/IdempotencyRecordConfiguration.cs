using ClaimsModule.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrganisationId).IsRequired();
        builder.Property(r => r.Key).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Method).HasMaxLength(10).IsRequired();
        builder.Property(r => r.Path).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ContentType).HasMaxLength(100);
        // ResponseBody left as nvarchar(max).

        builder.HasIndex(r => new { r.OrganisationId, r.Key }).IsUnique();
    }
}
