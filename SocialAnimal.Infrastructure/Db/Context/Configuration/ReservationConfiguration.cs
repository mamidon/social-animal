using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        
        // Primary key
        builder.HasKey(r => r.Id);
        
        // Unique constraint on invitation + user combination
        builder.HasIndex(r => new { r.InvitationId, r.UserId })
            .IsUnique()
            .HasDatabaseName("ix_reservations_invitation_user");
        
        // Additional indexes
        builder.HasIndex(r => r.InvitationId)
            .HasDatabaseName("ix_reservations_invitation_id");
            
        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_reservations_user_id");
            
        builder.HasIndex(r => r.PartySize)
            .HasDatabaseName("ix_reservations_party_size");
        
        // Properties
        builder.Property(r => r.InvitationId)
            .IsRequired();
            
        builder.Property(r => r.UserId)
            .IsRequired();
            
        builder.Property(r => r.PartySize)
            .IsRequired();
            
        // Concurrency token
        builder.Property(r => r.ConcurrencyToken)
            .IsConcurrencyToken();
            
        // Relationships
        builder.HasOne(r => r.Invitation)
            .WithMany(i => i.Reservations)
            .HasForeignKey(r => r.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent user deletion if reservations exist
    }
}