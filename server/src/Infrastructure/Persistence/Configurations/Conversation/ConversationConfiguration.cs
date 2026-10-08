using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.StorageFolder)
            .HasMaxLength(255);

        builder.HasIndex(c => c.LastMessageAt);

        builder.HasOne(c => c.LastMessage)
            .WithMany()
            .HasForeignKey(c => c.LastMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(c => c.Participants)
            .HasField("_participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(c => c.Messages)
            .HasField("_messages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}