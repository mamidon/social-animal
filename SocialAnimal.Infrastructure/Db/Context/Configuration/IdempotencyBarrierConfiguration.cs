using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public class IdempotencyBarrierConfiguration : IEntityTypeConfiguration<IdempotencyBarrier>
{
    public void Configure(EntityTypeBuilder<IdempotencyBarrier> builder)
    {
        builder.ToTable("idempotency_barriers");
        
        builder.HasKey(i => i.Id);
        
        builder.HasIndex(i => i.Key)
            .IsUnique()
            .HasDatabaseName("ix_idempotency_barriers_key");
            
        builder.HasIndex(i => i.ExpiresOn)
            .HasDatabaseName("ix_idempotency_barriers_expires_on");
        
        builder.Property(i => i.Key)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(i => i.Operation)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(i => i.ExpiresOn)
            .IsRequired();
            
        builder.Property(i => i.Result)
            .HasColumnType("text");
            
        builder.Property(i => i.ConcurrencyToken)
            .IsConcurrencyToken();
    }
}