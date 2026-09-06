using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

// Day 13 addition (approved schema change - see IdempotencyRecord.cs's XML comment).
public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Endpoint).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Key).IsRequired().HasMaxLength(200); // matches typical header-length ceilings
        builder.Property(i => i.RequestHash).IsRequired().HasMaxLength(64);
        builder.Property(i => i.Status).IsRequired().HasMaxLength(15);
        builder.Property(i => i.ResponseJson).HasColumnType("nvarchar(max)");

        // Uniqueness/concurrency safety net: the caller, endpoint, and key together must be
        // unique. A second concurrent request racing to insert the same triple fails at the DB
        // with a unique-constraint violation (mapped by the repository to "already in
        // progress"), which is what makes "concurrent requests using the same key must not
        // execute the operation twice" true even under real concurrency, not just in-process.
        builder.HasIndex(i => new { i.Endpoint, i.ScopeId, i.Key }).IsUnique();

        builder.Property(i => i.CreatedAt).IsRequired().HasConversion(UtcDateTimeConverters.NonNullable);
        builder.Property(i => i.CompletedAt).HasConversion(UtcDateTimeConverters.Nullable);
    }
}
