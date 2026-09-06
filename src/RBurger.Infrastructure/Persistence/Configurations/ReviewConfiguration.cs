using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever(); // approved decision #6

        // OrderId (unique 1:1) relationship configured in OrderConfiguration.
        // §6.3 explicitly requires: "unique index on OrderId" - reinforced here.
        builder.HasIndex(r => r.OrderId).IsUnique();

        // §6.2 documents an explicit CustomerId FK on Reviews; §6.1's relationship summary
        // does not enumerate Customer--Review, so no reverse collection exists on Customer
        // (per Day 1 decision) - configured here as a reference-only relationship.
        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(500);
        // §6.2: "datetime2 | default GETUTCDATE()"
        builder.Property(r => r.CreatedAt).IsRequired()
            .HasConversion(UtcDateTimeConverters.NonNullable)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
