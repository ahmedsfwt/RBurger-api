using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever(); // approved decision #6

        builder.Property(o => o.OrderNumber).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        // BranchId relationship configured in BranchConfiguration.
        // CustomerId relationship configured in CustomerConfiguration.
        // DriverId relationship configured in DriverConfiguration.

        // §1.3 / §6.2 / §12.1: OrderStage enum mapped to tinyint, values 0-3 unchanged.
        builder.Property(o => o.Stage)
            .IsRequired()
            .HasConversion<byte>();

        // Approved Day 2 decision #4: snapshot field lengths mirror their Customer source columns
        // (§6.2 gives no explicit length for these three columns - see discrepancy report).
        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.Phone).IsRequired().HasMaxLength(20);
        builder.Property(o => o.Address).IsRequired().HasMaxLength(300);

        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.Property(o => o.Subtotal).IsRequired().HasPrecision(8, 2);
        builder.Property(o => o.DeliveryFee).IsRequired().HasPrecision(8, 2);
        builder.Property(o => o.Total).IsRequired().HasPrecision(8, 2);
        builder.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(10);
        builder.Property(o => o.CustomerReceivedAt).HasConversion(UtcDateTimeConverters.Nullable);
        // §6.2: "datetime2 | default GETUTCDATE()"
        builder.Property(o => o.CreatedAt).IsRequired()
            .HasConversion(UtcDateTimeConverters.NonNullable)
            .HasDefaultValueSql("GETUTCDATE()");

        // Day 13 addition (approved schema change) - see Order.cs's XML comment.
        builder.Property(o => o.IsCancelled).IsRequired().HasDefaultValue(false);
        builder.Property(o => o.CancelledAt).HasConversion(UtcDateTimeConverters.Nullable);

        // §6.1: Order (1) -- (0..1) Payment / Review, both documented as unique 1:1 FKs
        // (Payments.OrderId / Reviews.OrderId). Configured from the Order side since Order
        // holds the reference navigation for both.
        builder.HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Review)
            .WithOne(r => r.Order)
            .HasForeignKey<Review>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // §6.1: Order (1) -- (∞) OrderItem / OrderStatusEvent. No delete behavior documented
        // for an Order itself being deleted -> default restrictive per approved decision #10;
        // see discrepancy report.
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OrderStatusEvents)
            .WithOne(e => e.Order)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
