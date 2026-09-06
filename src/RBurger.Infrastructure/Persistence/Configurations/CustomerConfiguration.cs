using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        // Approved Day 2 decision #6: client/application-generated GUIDs, not DB-generated.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.FullName).IsRequired().HasMaxLength(150);

        builder.Property(c => c.Phone).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => c.Phone).IsUnique();

        builder.Property(c => c.PasswordHash).IsRequired().HasMaxLength(300);
        builder.Property(c => c.DefaultAddress).HasMaxLength(300);
        builder.Property(c => c.PreferredLanguage).IsRequired().HasMaxLength(5);
        // §6.2: "datetime2 | default GETUTCDATE()"
        builder.Property(c => c.CreatedAt).IsRequired()
            .HasConversion(UtcDateTimeConverters.NonNullable)
            .HasDefaultValueSql("GETUTCDATE()");

        // §6.1 relationship (Customer (1) -- (∞) Order). CustomerId is nullable per approved
        // decision #3 (§7.6.4 nulls it on customer deletion) -> SetNull, explicitly required.
        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
