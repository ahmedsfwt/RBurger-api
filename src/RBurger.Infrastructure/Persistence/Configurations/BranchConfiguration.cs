using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();

        builder.Property(b => b.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(b => b.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(b => b.DeliveryFee).IsRequired().HasPrecision(8, 2);
        builder.Property(b => b.EtaMinMinutes).IsRequired();
        builder.Property(b => b.EtaMaxMinutes).IsRequired();

        // Approved Day 2 decision #5: required, max length 200 (not explicitly annotated
        // "required" in §6.2, unlike sibling columns - see discrepancy report).
        builder.Property(b => b.HotlinePhones).IsRequired().HasMaxLength(200);

        // §6.2: "bit | default 1"
        builder.Property(b => b.IsActive).IsRequired().HasDefaultValue(true);

        // Day 14 addition (Backend Parity Spec §1.4). 50 chars: generous free-text ETA label
        // (e.g. "25-35 mins" / "٢٠ دقيقة"), consistent with this project's convention for short
        // configured text columns (comparable to Driver.Vehicle's 20-char column).
        builder.Property(b => b.EstimatedDeliveryTime).HasMaxLength(50);

        // §6.1 relationships (Branch (1) -- (∞) MenuItem / Order / Driver)
        builder.HasMany(b => b.MenuItems)
            .WithOne(mi => mi.Branch)
            .HasForeignKey(mi => mi.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Orders)
            .WithOne(o => o.Branch)
            .HasForeignKey(o => o.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Drivers)
            .WithOne(d => d.Branch)
            .HasForeignKey(d => d.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
