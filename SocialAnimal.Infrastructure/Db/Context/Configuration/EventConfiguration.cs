using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        
        // Primary key
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.Slug)
            .IsUnique()
            .HasDatabaseName("ix_events_slug");
            
        builder.HasIndex(e => e.DeletedAt)
            .HasDatabaseName("ix_events_deleted_at");
            
        builder.HasIndex(e => e.State)
            .HasDatabaseName("ix_events_state");
            
        builder.HasIndex(e => e.City)
            .HasDatabaseName("ix_events_city");
        
        // Properties
        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(e => e.AddressLine1)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(e => e.AddressLine2)
            .HasMaxLength(200);
            
        builder.Property(e => e.City)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.State)
            .IsRequired()
            .HasMaxLength(2)
            .IsFixedLength();
            
        builder.Property(e => e.Postal)
            .IsRequired()
            .HasMaxLength(20);
            
        // Soft delete query filter
        builder.HasQueryFilter(e => e.DeletedAt == null);
        
        // Concurrency token
        builder.Property(e => e.ConcurrencyToken)
            .IsConcurrencyToken();
            
        // Relationships
        builder.HasMany(e => e.Invitations)
            .WithOne(i => i.Event)
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}