using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Configurations;

public class BuilderOptionConfiguration : IEntityTypeConfiguration<BuilderOption>
{
    public void Configure(EntityTypeBuilder<BuilderOption> builder)
    {
        builder.ToTable("BuilderOptions");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        // BuilderOptionGroupId relationship configured in BuilderOptionGroupConfiguration.

        builder.Property(o => o.NameAr).IsRequired().HasMaxLength(80);
        builder.Property(o => o.NameEn).IsRequired().HasMaxLength(80);
        builder.Property(o => o.ExtraPrice).IsRequired().HasPrecision(6, 2);
    }
}
