# Task 5: Update Database Context and Configurations

## Objective
Update the ApplicationContext to include new DbSets for Invitation and Reservation entities, update the Event DbSet, and ensure all Entity Framework configurations are properly registered.

## Requirements
- Add DbSets for new entities (Invitation, Reservation)
- Update existing DbSets if needed
- Register all entity configurations
- Configure global query filters for soft delete
- Ensure NodaTime and snake_case naming conventions are applied

## Implementation Steps

### Step 1: Update ApplicationContext
Location: `/SocialAnimal.Infrastructure/Db/Context/ApplicationContext.cs`

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
        
        // Global query filters for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Event>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Invitation>().HasQueryFilter(i => i.DeletedAt == null);
        // Note: Reservation does not have soft delete
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
```

### Step 2: Create Configuration Registration Helper
Location: `/SocialAnimal.Infrastructure/Db/Context/Configuration/ConfigurationExtensions.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public static class ConfigurationExtensions
{
    public static void ApplyConfigurations(this ModelBuilder modelBuilder)
    {
        // Apply configurations in order of dependencies
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyBarrierConfiguration());
    }
    
    public static void ConfigureConventions(this ModelBuilder modelBuilder)
    {
        // Configure default string length if not specified
        modelBuilder.Properties<string>()
            .HaveMaxLength(500);
            
        // Configure decimal precision
        modelBuilder.Properties<decimal>()
            .HavePrecision(18, 2);
            
        // Configure DateTime to use UTC
        modelBuilder.Properties<DateTime>()
            .HaveConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
```

### Step 3: Create IdempotencyBarrierConfiguration (if not exists)
Location: `/SocialAnimal.Infrastructure/Db/Context/Configuration/IdempotencyBarrierConfiguration.cs`

```csharp
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
            
        builder.HasIndex(i => i.ExpiresAt)
            .HasDatabaseName("ix_idempotency_barriers_expires_at");
        
        builder.Property(i => i.Key)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(i => i.Value)
            .HasMaxLength(4000);
            
        builder.Property(i => i.ExpiresAt)
            .IsRequired();
            
        builder.Property(i => i.ConcurrencyToken)
            .IsConcurrencyToken();
    }
}
```

### Step 4: Update ApplicationContextFactory for Migrations
Location: `/SocialAnimal.Infrastructure/Db/Context/ApplicationContextFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Npgsql;

namespace SocialAnimal.Infrastructure.Db.Context;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        // Configure DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
        
        // Configure Npgsql with NodaTime
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseNodaTime();
        var dataSource = dataSourceBuilder.Build();
        
        optionsBuilder.UseNpgsql(dataSource, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(ApplicationContext).Assembly.FullName);
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
        });
        
        // Use snake_case naming
        optionsBuilder.UseSnakeCaseNamingConvention();
        
        // Create instance with SystemClock for design-time
        return new ApplicationContext(optionsBuilder.Options, SystemClock.Instance);
    }
}
```

### Step 5: Update Service Registration for Database Context
Location: `/SocialAnimal.Web/Configuration/Modules/InfrastructureServicesModule.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Repositories;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Web.Configuration.Modules;

public class InfrastructureServicesModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register clock
        services.AddSingleton<IClock>(SystemClock.Instance);
        
        // Configure database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        // Configure Npgsql data source with NodaTime
        services.AddSingleton<NpgsqlDataSource>(serviceProvider =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseNodaTime();
            return dataSourceBuilder.Build();
        });
        
        // Configure DbContext
        services.AddDbContext<ApplicationContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
            });
            
            options.UseSnakeCaseNamingConvention();
            
            if (configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
            
            if (configuration.GetValue<bool>("Database:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }
        });
        
        // Register Unit of Work
        services.AddScoped<Func<ApplicationContext>>(serviceProvider => 
            () => serviceProvider.GetRequiredService<ApplicationContext>());
        
        // Register repositories
        services.AddScoped<ICrudRepo, CrudRepo>();
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IEventRepo, EventRepo>();
        services.AddScoped<IInvitationRepo, InvitationRepo>();
        services.AddScoped<IReservationRepo, ReservationRepo>();
        
        // Register CrudQueries for each entity type
        services.AddScoped(typeof(ICrudQueries<>), typeof(CrudQueries<,,>));
    }
}
```

### Step 6: Add Database Configuration Settings
Location: `/SocialAnimal.Web/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=socialanimal;Username=socialanimal;Password=your_password"
  },
  "Database": {
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "CommandTimeout": 30,
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "MaxRetryDelay": "00:00:30"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

Location: `/SocialAnimal.Web/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=socialanimal_dev;Username=socialanimal;Password=dev_password"
  },
  "Database": {
    "EnableSensitiveDataLogging": true,
    "EnableDetailedErrors": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

## Testing Checklist

- [ ] DbContext can be created successfully
- [ ] All DbSets are accessible and queryable
- [ ] Soft delete query filters work correctly
- [ ] Snake_case naming is applied to all tables and columns
- [ ] NodaTime types are properly configured
- [ ] Timestamp updates work on SaveChanges
- [ ] Concurrency tokens are updated correctly
- [ ] Design-time factory works for migrations
- [ ] Connection resilience with retry logic works
- [ ] All entity configurations are applied

## Verification Steps

1. **Test DbContext Creation**:
```csharp
using var context = new ApplicationContext(options, clock);
Assert.NotNull(context.Users);
Assert.NotNull(context.Events);
Assert.NotNull(context.Invitations);
Assert.NotNull(context.Reservations);
```

2. **Test Query Filters**:
```csharp
// Soft-deleted entities should not appear
var activeUsers = await context.Users.ToListAsync();
var allUsers = await context.Users.IgnoreQueryFilters().ToListAsync();
Assert.True(allUsers.Count >= activeUsers.Count);
```

3. **Test Timestamp Updates**:
```csharp
var user = new User { /* ... */ };
context.Users.Add(user);
await context.SaveChangesAsync();

Assert.NotNull(user.CreatedOn);
Assert.Null(user.UpdatedOn);

user.FirstName = "Updated";
await context.SaveChangesAsync();

Assert.NotNull(user.UpdatedOn);
```

## Migration Commands

After completing this task, you'll need to generate and apply migrations:

```bash
# Generate migration
dotnet ef migrations add InitialSchema --project SocialAnimal.Infrastructure --startup-project SocialAnimal.Web

# Review the generated migration
# Located in: SocialAnimal.Infrastructure/Db/Migrations/

# Apply migration to database
dotnet ef database update --project SocialAnimal.Infrastructure --startup-project SocialAnimal.Web

# Generate SQL script (optional, for review)
dotnet ef migrations script --project SocialAnimal.Infrastructure --startup-project SocialAnimal.Web --output schema.sql
```

## Dependencies

This task depends on:
- Tasks 1-4 (All entity definitions must be complete)

This task must be completed before:
- Task 6 (Database migration generation)
- Task 7 (Repository implementations need DbContext)

## Notes

- Global query filters automatically exclude soft-deleted records
- Use `IgnoreQueryFilters()` to include soft-deleted records when needed
- The design-time factory is crucial for EF Core tools to work
- Consider adding database health checks for production
- Connection resilience helps with transient database failures
- Sensitive data logging should only be enabled in development