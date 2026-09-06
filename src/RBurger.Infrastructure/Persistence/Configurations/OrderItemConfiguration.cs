using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id).ValueGeneratedOnAdd();

        // OrderId relationship configured in OrderConfiguration.
        // MenuItemId relationship configured in MenuItemConfiguration.

        builder.Property(oi => oi.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(oi => oi.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(oi => oi.Quantity).IsRequired();
        builder.Property(oi => oi.UnitPrice).IsRequired().HasPrecision(8, 2);
    }
}
