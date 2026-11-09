using Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class UserSessionConfiguration
    : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .ValueGeneratedNever();

        builder.Property(session => session.RefreshTokenHash)
            .HasMaxLength(64)
            .IsRequired();

        // Exactly one session per issued refresh token.
        builder.HasIndex(session => session.RefreshTokenHash)
            .IsUnique();

        builder.HasIndex(session => session.TokenFamilyId);

        builder.HasIndex(session => new
        {
            session.UserId,
            session.RevokedAtUtc
        });

        builder.Property(session => session.RevocationReason)
            .HasMaxLength(64);

        builder.Property(session => session.CreatedAtUtc)
            .IsRequired();

        builder.Property(session => session.ExpiresAtUtc)
            .IsRequired();

        builder.Property(session => session.LastUsedAtUtc)
            .IsRequired();

        builder.HasOne(session => session.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
