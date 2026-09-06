using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // approved decision #6

        // OrderId (unique 1:1) relationship configured in OrderConfiguration.

        builder.Property(p => p.Method).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(15);
        builder.Property(p => p.GatewayProvider).HasMaxLength(20);
        builder.Property(p => p.GatewayTransactionId).HasMaxLength(100);
        builder.Property(p => p.Amount).IsRequired().HasPrecision(8, 2);
        builder.Property(p => p.PaidAt).HasConversion(UtcDateTimeConverters.Nullable);

        // Day 13 addition (approved schema change) - see Payment.cs's XML comment.
        builder.Property(p => p.CashRefundNote).HasMaxLength(500); // mirrors Orders.Notes' length
        builder.Property(p => p.CashRefundNotedAt).HasConversion(UtcDateTimeConverters.Nullable);
    }
}
