using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");
        
        // Primary key
        builder.HasKey(i => i.Id);
        
        // Indexes
        builder.HasIndex(i => i.Slug)
            .IsUnique()
            .HasDatabaseName("ix_invitations_slug");
            
        builder.HasIndex(i => i.EventId)
            .HasDatabaseName("ix_invitations_event_id");
            
        builder.HasIndex(i => i.DeletedAt)
            .HasDatabaseName("ix_invitations_deleted_at");
        
        // Properties
        builder.Property(i => i.Slug)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(i => i.EventId)
            .IsRequired();
            
        // Soft delete query filter
        builder.HasQueryFilter(i => i.DeletedAt == null);
        
        // Concurrency token
        builder.Property(i => i.ConcurrencyToken)
            .IsConcurrencyToken();
            
        // Relationships
        builder.HasOne(i => i.Event)
            .WithMany(e => e.Invitations)
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
            
        builder.HasMany(i => i.Reservations)
            .WithOne(r => r.Invitation)
            .HasForeignKey(r => r.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}