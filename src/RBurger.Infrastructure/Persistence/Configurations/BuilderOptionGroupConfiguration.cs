using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class BuilderOptionGroupConfiguration : IEntityTypeConfiguration<BuilderOptionGroup>
{
    public void Configure(EntityTypeBuilder<BuilderOptionGroup> builder)
    {
        builder.ToTable("BuilderOptionGroups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedOnAdd();

        builder.Property(g => g.GroupKey).IsRequired().HasMaxLength(20);
        builder.Property(g => g.IsSingleSelect).IsRequired();

        // §6.1 relationship (BuilderOptionGroup (1) -- (∞) BuilderOption).
        // BuilderOptionGroupId is an approved Day 2 FK-name decision (see discrepancy report).
        // No delete behavior documented -> default restrictive per approved decision #10.
        builder.HasMany(g => g.Options)
            .WithOne(o => o.Group)
            .HasForeignKey(o => o.BuilderOptionGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
