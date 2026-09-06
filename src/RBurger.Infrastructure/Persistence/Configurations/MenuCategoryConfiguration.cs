using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("MenuCategories");

        builder.HasKey(mc => mc.Id);
        builder.Property(mc => mc.Id).ValueGeneratedOnAdd();

        builder.Property(mc => mc.Key).IsRequired().HasMaxLength(30);
        builder.HasIndex(mc => mc.Key).IsUnique();

        builder.Property(mc => mc.LabelAr).IsRequired().HasMaxLength(60);
        builder.Property(mc => mc.LabelEn).IsRequired().HasMaxLength(60);
        builder.Property(mc => mc.SortOrder).IsRequired();

        // §6.1 relationship (MenuCategory (1) -- (∞) MenuItem)
        // No delete behavior documented for this relationship (see discrepancy report);
        // default restrictive behavior per approved decision #10.
        builder.HasMany(mc => mc.MenuItems)
            .WithOne(mi => mi.Category)
            .HasForeignKey(mi => mi.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
