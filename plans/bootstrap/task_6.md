Configure Entity Framework database context and entities

This task sets up the Entity Framework Core database context in the Infrastructure project with PostgreSQL configuration, snake_case naming conventions, and NodaTime support following the patterns defined in CLAUDE.md.

## Work to be Done

### Application Database Context
Create `ApplicationContext.cs` in `SocialAnimal.Infrastructure/Db/Context/ApplicationContext.cs`:

```csharp
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
    public DbSet<EventAttendance> EventAttendances => Set<EventAttendance>();
    public DbSet<IdempotencyBarrier> IdempotencyBarriers => Set<IdempotencyBarrier>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);
        
        // Global query filters for soft delete (if needed)
        // modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configure snake_case naming convention
        optionsBuilder.UseSnakeCaseNamingConvention();
        
        // Configure NodaTime
        optionsBuilder.UseNodaTime();
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
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
            }
            
            entity.UpdatedOn = _clock.GetCurrentInstant();
            entity.ConcurrencyToken = DateTime.UtcNow; // For optimistic concurrency
        }
    }
}
```

### Base Entity Class
Create `BaseEntity.cs` in `SocialAnimal.Infrastructure/Db/Entities/BaseEntity.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

public abstract class BaseEntity
{
    [Key]
    public long Id { get; set; }
    
    public required Instant CreatedOn { get; set; }
    
    public Instant? UpdatedOn { get; set; }
    
    [Timestamp]
    public DateTime? ConcurrencyToken { get; set; }
}
```

### User Entity
Create `User.cs` in `SocialAnimal.Infrastructure/Db/Entities/User.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Handle), IsUnique = true)]
[Index(nameof(Reference), IsUnique = true)]
public class User : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public required string Handle { get; set; } // URL-safe username
    
    [Required]
    [MaxLength(255)]
    public required string Email { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string Reference { get; set; } // External ID like "user_abc123"
    
    public string? PasswordHash { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsEmailVerified { get; set; } = false;
    
    // Navigation properties
    public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public virtual ICollection<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();
}
```

### Event Entity
Create `Event.cs` in `SocialAnimal.Infrastructure/Db/Entities/Event.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Handle), IsUnique = true)]
[Index(nameof(Reference), IsUnique = true)]
[Index(nameof(StartsOn))]
public class Event : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public required string Handle { get; set; } // URL slug for event
    
    [Required]
    [MaxLength(50)]
    public required string Reference { get; set; } // External ID like "event_xyz789"
    
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public required Instant StartsOn { get; set; }
    
    public required Instant EndsOn { get; set; }
    
    [MaxLength(500)]
    public string? Location { get; set; }
    
    public int? MaxAttendees { get; set; }
    
    public bool IsPublic { get; set; } = true;
    
    public bool IsCancelled { get; set; } = false;
    
    // Foreign keys
    public required long OrganizerId { get; set; }
    
    // Navigation properties
    public virtual User Organizer { get; set; } = null!;
    public virtual ICollection<EventAttendance> Attendances { get; set; } = new List<EventAttendance>();
}
```

### EventAttendance Entity
Create `EventAttendance.cs` in `SocialAnimal.Infrastructure/Db/Entities/EventAttendance.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(EventId), nameof(UserId), IsUnique = true)]
public class EventAttendance : BaseEntity
{
    public required long EventId { get; set; }
    
    public required long UserId { get; set; }
    
    public required AttendanceStatus Status { get; set; }
    
    public Instant? RespondedOn { get; set; }
    
    public Instant? CheckedInOn { get; set; }
    
    // Navigation properties
    public virtual Event Event { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public enum AttendanceStatus
{
    Invited,
    Accepted,
    Declined,
    Maybe,
    CheckedIn
}
```

### Idempotency Barrier Entity
Create `IdempotencyBarrier.cs` in `SocialAnimal.Infrastructure/Db/Entities/IdempotencyBarrier.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Key), IsUnique = true)]
[Index(nameof(ExpiresOn))]
public class IdempotencyBarrier : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public required string Key { get; set; }
    
    [MaxLength(100)]
    public required string Operation { get; set; }
    
    public required Instant ExpiresOn { get; set; }
    
    public string? Result { get; set; } // JSON serialized result
}
```

### Entity Configuration Example
Create `UserConfiguration.cs` in `SocialAnimal.Infrastructure/Db/Context/Configuration/UserConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name will be snake_cased automatically
        builder.ToTable("users");
        
        // Configure Reference to follow pattern
        builder.Property(u => u.Reference)
            .HasDefaultValueSql("'user_' || gen_random_uuid()::text");
        
        // Configure relationships
        builder.HasMany(u => u.OrganizedEvents)
            .WithOne(e => e.Organizer)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(u => u.EventAttendances)
            .WithOne(ea => ea.User)
            .HasForeignKey(ea => ea.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Database Context Factory for Migrations
Create `ApplicationContextFactory.cs` in `SocialAnimal.Infrastructure/Db/Context/ApplicationContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Context;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
        
        // Use environment variable or default connection string for migrations
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
            ?? "Host=localhost;Database=socialanimal_dev;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.UseNodaTime();
        });
        
        return new ApplicationContext(optionsBuilder.Options, SystemClock.Instance);
    }
}
```

## Relevant Patterns from CLAUDE.md

- **Infrastructure Layer**: Database access in dedicated `Db/` folder
- **Entity-Record Mapping**: Entities in Infrastructure will implement IInto<TRecord>
- **Time and Date Handling**: Use NodaTime `Instant` for timestamps
- **Identifier Patterns**: Prefixed external IDs (user_*, event_*), long database IDs
- **Concurrency Control**: [Timestamp] attribute for optimistic concurrency
- **Naming Conventions**: snake_case for database via UseSnakeCaseNamingConvention()
- **Idempotency Patterns**: IdempotencyBarrier entity for critical operations

## Deliverables

1. `ApplicationContext.cs` - Main database context with entity sets
2. `BaseEntity.cs` - Base entity class with common properties
3. `User.cs` - User entity with proper indexes and constraints
4. `Event.cs` - Event entity with relationships
5. `EventAttendance.cs` - Many-to-many relationship entity
6. `IdempotencyBarrier.cs` - Idempotency control entity
7. `UserConfiguration.cs` - Example entity configuration
8. `ApplicationContextFactory.cs` - Design-time factory for migrations

## Acceptance Criteria

- Database context compiles without errors
- Snake_case naming convention is configured
- NodaTime support is properly configured
- All entities inherit from BaseEntity
- Unique constraints are defined for Handle and Reference fields
- Relationships are properly configured with appropriate cascade behavior
- Timestamp fields automatically update on save
- Optimistic concurrency is configured via ConcurrencyToken
- Design-time factory enables EF Core migrations
- All required indexes are defined for query performance