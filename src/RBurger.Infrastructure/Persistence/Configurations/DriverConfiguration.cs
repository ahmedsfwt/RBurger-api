using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever(); // approved decision #6

        builder.Property(d => d.FullName).IsRequired().HasMaxLength(150);

        builder.Property(d => d.Phone).IsRequired().HasMaxLength(20);
        builder.HasIndex(d => d.Phone).IsUnique();

        builder.Property(d => d.PasswordHash).IsRequired().HasMaxLength(300);
        builder.Property(d => d.Vehicle).IsRequired().HasMaxLength(20);

        // BranchId relationship configured in BranchConfiguration.
        // CreatedByAdminId relationship configured in AdminConfiguration.

        // §6.2: "bit | default 1"
        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);

        // §6.2: "datetime2 | default GETUTCDATE()"
        builder.Property(d => d.CreatedAt).IsRequired()
            .HasConversion(UtcDateTimeConverters.NonNullable)
            .HasDefaultValueSql("GETUTCDATE()");

        // §6.1 relationship (Driver (1) -- (∞) Order), Order.DriverId is nullable until Received.
        // No delete behavior documented for a driver row itself being deleted while historical
        // orders reference it (§7.6.3 only blocks deletion for non-terminal orders) -> default
        // restrictive per approved decision #10; see discrepancy report.
        builder.HasMany(d => d.Orders)
            .WithOne(o => o.Driver)
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
