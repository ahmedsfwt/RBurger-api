using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence.Converters;

namespace RBurger.Infrastructure.Persistence.Configurations;

// Day 13 addition (approved schema change - see RefreshToken.cs's XML comment).
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).ValueGeneratedNever(); // mirrors every other Guid PK (approved decision #6)

        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(64); // SHA-256 hex digest = 64 chars
        // Unique: a hash collision/duplicate would mean two live tokens hash identically,
        // which must never be treated as valid - also the primary lookup index for §7.1's
        // POST /api/v1/auth/refresh.
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        builder.Property(rt => rt.UserType).IsRequired().HasMaxLength(10); // Customer|Driver|Admin

        // Lookup/concurrency index: RevokeAllActiveForUserAsync (reuse-detection defense) and
        // any future "log out everywhere" flow scan by (UserType, UserId).
        builder.HasIndex(rt => new { rt.UserType, rt.UserId });

        builder.Property(rt => rt.ExpiresAt).IsRequired().HasConversion(UtcDateTimeConverters.NonNullable);
        builder.Property(rt => rt.CreatedAt).IsRequired().HasConversion(UtcDateTimeConverters.NonNullable);
        builder.Property(rt => rt.RevokedAt).HasConversion(UtcDateTimeConverters.Nullable);
    }
}
