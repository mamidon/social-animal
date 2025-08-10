using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        // Primary key
        builder.HasKey(u => u.Id);
        
        // Indexes
        builder.HasIndex(u => u.Slug)
            .IsUnique()
            .HasDatabaseName("ix_users_slug");
            
        builder.HasIndex(u => u.Phone)
            .HasDatabaseName("ix_users_phone");
            
        builder.HasIndex(u => u.DeletedAt)
            .HasDatabaseName("ix_users_deleted_at");
        
        // Properties
        builder.Property(u => u.Slug)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.Phone)
            .IsRequired()
            .HasMaxLength(20);
            
        // Soft delete query filter
        builder.HasQueryFilter(u => u.DeletedAt == null);
        
        // Concurrency token
        builder.Property(u => u.ConcurrencyToken)
            .IsConcurrencyToken();
    }
}