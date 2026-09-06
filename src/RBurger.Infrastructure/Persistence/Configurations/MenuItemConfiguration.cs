using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(mi => mi.Id);
        builder.Property(mi => mi.Id).ValueGeneratedOnAdd();

        // CategoryId / BranchId are configured as relationships in MenuCategoryConfiguration /
        // BranchConfiguration respectively - not repeated here to avoid duplicate relationship
        // definitions for the same FK.

        builder.Property(mi => mi.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(mi => mi.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(mi => mi.DescriptionAr).IsRequired().HasMaxLength(400);
        builder.Property(mi => mi.DescriptionEn).IsRequired().HasMaxLength(400);
        builder.Property(mi => mi.Price).IsRequired().HasPrecision(8, 2);
        builder.Property(mi => mi.ImageUrl).HasMaxLength(300);
        builder.Property(mi => mi.ImageObjectKey).HasMaxLength(300);
        builder.Property(mi => mi.ImageUploadedAt).HasConversion(UtcDateTimeConverters.Nullable);
        // §6.2: "bit | default 1"
        builder.Property(mi => mi.IsAvailable).IsRequired().HasDefaultValue(true);

        // §6.1 relationship (MenuItem (1) -- (∞) OrderItem), OrderItem.MenuItemId is nullable.
        // §7.6.1 explicitly states OrderItems survive a permanent MenuItem deletion with their
        // snapshot intact -> SetNull per approved decision #10 (explicit documentation requirement).
        builder.HasMany(mi => mi.OrderItems)
            .WithOne(oi => oi.MenuItem)
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
