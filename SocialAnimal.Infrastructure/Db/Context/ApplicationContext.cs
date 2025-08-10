using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context;

public class ApplicationContext : DbContext
{
    private readonly IClock _clock;
    
    public ApplicationContext(DbContextOptions<ApplicationContext> options, IClock clock) 
        : base(options)
    {
        _clock = clock;
    }
    
    // Entity DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<IdempotencyBarrier> IdempotencyBarriers => Set<IdempotencyBarrier>();
    
    // Remove obsolete DbSet
    // public DbSet<EventAttendance> EventAttendances => Set<EventAttendance>(); // REMOVED
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);
        
        // Global query filters for soft delete are configured in individual entity configurations
        // This ensures they are applied consistently and can be easily managed per entity
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configure snake_case naming convention
        optionsBuilder.UseSnakeCaseNamingConvention();
        
        // Note: NodaTime configuration is handled in the service registration
        // where UseNpgsql and UseNodaTime are called together
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }
    
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));
        
        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedOn = _clock.GetCurrentInstant();
                // Ensure UpdatedOn is null for new entities
                entity.UpdatedOn = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedOn = _clock.GetCurrentInstant();
                // Don't modify CreatedOn for updates
                entry.Property(nameof(BaseEntity.CreatedOn)).IsModified = false;
            }
            
            // Always update concurrency token
            entity.ConcurrencyToken = DateTime.UtcNow;
        }
    }
}