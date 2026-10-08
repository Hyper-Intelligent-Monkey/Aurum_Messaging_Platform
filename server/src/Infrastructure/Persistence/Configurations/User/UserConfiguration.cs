using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.NormalizedUsername)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.Email)
            .IsRequired();

        builder.Property(u => u.HashedPassword)
            .IsRequired(false);

        builder.HasIndex(u => u.NormalizedUsername)
            .IsUnique();
        
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Navigation(u => u.Conversations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.IsEmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.EmailConfirmationToken)
            .HasMaxLength(256);

        builder.Property(u => u.PasswordResetOtp)
            .HasMaxLength(10);
    }
}