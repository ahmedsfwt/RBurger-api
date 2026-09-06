using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever(); // approved decision #6

        builder.Property(a => a.Username).IsRequired().HasMaxLength(50);
        builder.HasIndex(a => a.Username).IsUnique();

        builder.Property(a => a.FullName).IsRequired().HasMaxLength(150);
        builder.Property(a => a.PasswordHash).IsRequired().HasMaxLength(300);
        // §6.2: "bit | default 1"
        builder.Property(a => a.IsActive).IsRequired().HasDefaultValue(true);

        // §6.2: "datetime2 | default GETUTCDATE()"
        builder.Property(a => a.CreatedAt).IsRequired()
            .HasConversion(UtcDateTimeConverters.NonNullable)
            .HasDefaultValueSql("GETUTCDATE()");

        // §6.1 relationship (Admin (1) -- (∞) Driver), Driver.CreatedByAdminId is required (non-nullable).
        // §7 documents no Admin-deletion endpoint at all, so this delete behavior is not
        // exercised by any documented flow; default restrictive per approved decision #10.
        builder.HasMany(a => a.Drivers)
            .WithOne(d => d.CreatedByAdmin)
            .HasForeignKey(d => d.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
