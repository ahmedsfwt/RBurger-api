using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class OrderStatusEventConfiguration : IEntityTypeConfiguration<OrderStatusEvent>
{
    public void Configure(EntityTypeBuilder<OrderStatusEvent> builder)
    {
        builder.ToTable("OrderStatusEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // OrderId relationship configured in OrderConfiguration.

        // Per your Day 1 approval, reuses the single OrderStage enum (mapped to tinyint,
        // values 0-3) rather than a raw byte, consistent with §12.1's rule against
        // re-declaring the stage enum locally.
        builder.Property(e => e.Stage)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(e => e.TriggeredBy).IsRequired().HasMaxLength(10);

        // §6.2: "DriverId or CustomerId, nullable for system" - polymorphic, no FK constraint
        // configured (matches the Day 1 Domain decision of no navigation property for it).
        builder.Property(e => e.ActorId);

        // §6.2: "datetime2 | default GETUTCDATE()"
        builder.Property(e => e.Timestamp).IsRequired()
            .HasConversion(UtcDateTimeConverters.NonNullable)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
