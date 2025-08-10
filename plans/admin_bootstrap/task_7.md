# Task 7: Implement Repository Layer

## Objective
Ensure all repository interfaces and implementations are complete and properly registered in the dependency injection container.

## Requirements
- Complete repository implementations for all entities
- Follow existing CQRS-like patterns with ICrudQueries
- Ensure proper use of Unit of Work pattern
- Register all repositories in DI container

## Implementation Steps

### Step 1: Update ICrudRepo Interface (if needed)
Location: `/SocialAnimal.Core/Repositories/ICrudRepo.cs`

```csharp
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface ICrudRepo
{
    Task<TRecord> CreateAsync<TRecord>(TRecord record) where TRecord : BaseRecord;
    Task<TRecord?> GetAsync<TRecord>(long id) where TRecord : BaseRecord;
    Task<TRecord> UpdateAsync<TRecord>(TRecord record) where TRecord : BaseRecord;
    Task<bool> DeleteAsync<TRecord>(long id) where TRecord : BaseRecord;
    Task<bool> ExistsAsync<TRecord>(long id) where TRecord : BaseRecord;
    Task<int> SaveChangesAsync();
}
```

### Step 2: Update CrudRepo Implementation
Location: `/SocialAnimal.Infrastructure/Repositories/CrudRepo.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class CrudRepo : ICrudRepo
{
    private readonly ApplicationContext _context;
    private readonly IClock _clock;
    
    public CrudRepo(ApplicationContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }
    
    public async Task<TRecord> CreateAsync<TRecord>(TRecord record) where TRecord : BaseRecord
    {
        var entityType = GetEntityType<TRecord>();
        var entity = ConvertToEntity(record, entityType);
        
        _context.Add(entity);
        await _context.SaveChangesAsync();
        
        return ConvertToRecord<TRecord>(entity);
    }
    
    public async Task<TRecord?> GetAsync<TRecord>(long id) where TRecord : BaseRecord
    {
        var entityType = GetEntityType<TRecord>();
        var entity = await _context.FindAsync(entityType, id);
        
        if (entity == null)
            return null;
            
        return ConvertToRecord<TRecord>(entity);
    }
    
    public async Task<TRecord> UpdateAsync<TRecord>(TRecord record) where TRecord : BaseRecord
    {
        var entityType = GetEntityType<TRecord>();
        var entity = ConvertToEntity(record, entityType);
        
        _context.Update(entity);
        await _context.SaveChangesAsync();
        
        return ConvertToRecord<TRecord>(entity);
    }
    
    public async Task<bool> DeleteAsync<TRecord>(long id) where TRecord : BaseRecord
    {
        var entityType = GetEntityType<TRecord>();
        var entity = await _context.FindAsync(entityType, id);
        
        if (entity == null)
            return false;
        
        _context.Remove(entity);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> ExistsAsync<TRecord>(long id) where TRecord : BaseRecord
    {
        var entityType = GetEntityType<TRecord>();
        var entity = await _context.FindAsync(entityType, id);
        return entity != null;
    }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    private Type GetEntityType<TRecord>() where TRecord : BaseRecord
    {
        return typeof(TRecord).Name switch
        {
            nameof(UserRecord) => typeof(User),
            nameof(EventRecord) => typeof(Event),
            nameof(InvitationRecord) => typeof(Invitation),
            nameof(ReservationRecord) => typeof(Reservation),
            _ => throw new NotSupportedException($"Record type {typeof(TRecord).Name} is not supported")
        };
    }
    
    private object ConvertToEntity<TRecord>(TRecord record, Type entityType) where TRecord : BaseRecord
    {
        return entityType.Name switch
        {
            nameof(User) => User.FromRecord((record as UserRecord)!),
            nameof(Event) => Event.FromRecord((record as EventRecord)!),
            nameof(Invitation) => Invitation.FromRecord((record as InvitationRecord)!),
            nameof(Reservation) => Reservation.FromRecord((record as ReservationRecord)!),
            _ => throw new NotSupportedException($"Entity type {entityType.Name} is not supported")
        };
    }
    
    private TRecord ConvertToRecord<TRecord>(object entity) where TRecord : BaseRecord
    {
        return entity switch
        {
            User user => (user.Into() as TRecord)!,
            Event evt => (evt.Into() as TRecord)!,
            Invitation invitation => (invitation.Into() as TRecord)!,
            Reservation reservation => (reservation.Into() as TRecord)!,
            _ => throw new NotSupportedException($"Entity type {entity.GetType().Name} is not supported")
        };
    }
}
```

### Step 3: Create Generic Repository Base Class
Location: `/SocialAnimal.Infrastructure/Repositories/RepositoryBase.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public abstract class RepositoryBase<TEntity, TRecord> 
    where TEntity : BaseEntity, IInto<TRecord>, new()
    where TRecord : BaseRecord
{
    protected readonly Func<ApplicationContext> _unitOfWork;
    
    protected RepositoryBase(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    protected async Task<TRecord?> GetByPredicateAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool ignoreQueryFilters = false)
    {
        using var context = _unitOfWork();
        IQueryable<TEntity> query = context.Set<TEntity>();
        
        if (ignoreQueryFilters)
            query = query.IgnoreQueryFilters();
            
        var entity = await query.FirstOrDefaultAsync(predicate);
        return entity?.Into();
    }
    
    protected async Task<IEnumerable<TRecord>> GetManyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int skip = 0,
        int take = 20,
        bool ignoreQueryFilters = false,
        params Expression<Func<TEntity, object>>[] includes)
    {
        using var context = _unitOfWork();
        IQueryable<TEntity> query = context.Set<TEntity>();
        
        if (ignoreQueryFilters)
            query = query.IgnoreQueryFilters();
            
        if (predicate != null)
            query = query.Where(predicate);
            
        foreach (var include in includes)
            query = query.Include(include);
            
        if (orderBy != null)
            query = orderBy(query);
            
        var entities = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            
        return entities.Select(e => e.Into());
    }
    
    protected async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool ignoreQueryFilters = false)
    {
        using var context = _unitOfWork();
        IQueryable<TEntity> query = context.Set<TEntity>();
        
        if (ignoreQueryFilters)
            query = query.IgnoreQueryFilters();
            
        if (predicate != null)
            query = query.Where(predicate);
            
        return await query.CountAsync();
    }
    
    protected async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool ignoreQueryFilters = false)
    {
        using var context = _unitOfWork();
        IQueryable<TEntity> query = context.Set<TEntity>();
        
        if (ignoreQueryFilters)
            query = query.IgnoreQueryFilters();
            
        return await query.AnyAsync(predicate);
    }
}
```

### Step 4: Refactor Repository Implementations to Use Base Class

Example refactor for EventRepo:
Location: `/SocialAnimal.Infrastructure/Repositories/EventRepo.cs`

```csharp
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class EventRepo : RepositoryBase<Event, EventRecord>, IEventRepo
{
    public EventRepo(Func<ApplicationContext> unitOfWork) : base(unitOfWork)
    {
        Events = new CrudQueries<ApplicationContext, Event, EventRecord>(
            unitOfWork, c => c.Events);
    }
    
    public ICrudQueries<EventRecord> Events { get; }
    
    public Task<EventRecord?> GetBySlugAsync(string slug)
    {
        return GetByPredicateAsync(e => e.Slug == slug, ignoreQueryFilters: true);
    }
    
    public Task<bool> SlugExistsAsync(string slug)
    {
        return ExistsAsync(e => e.Slug == slug, ignoreQueryFilters: true);
    }
    
    public Task<IEnumerable<EventRecord>> GetActiveEventsAsync(int skip = 0, int take = 20)
    {
        return GetManyAsync(
            orderBy: q => q.OrderByDescending(e => e.CreatedOn),
            skip: skip,
            take: take);
    }
    
    public Task<IEnumerable<EventRecord>> GetDeletedEventsAsync(int skip = 0, int take = 20)
    {
        return GetManyAsync(
            predicate: e => e.DeletedAt != null,
            orderBy: q => q.OrderByDescending(e => e.DeletedAt),
            skip: skip,
            take: take,
            ignoreQueryFilters: true);
    }
    
    public Task<IEnumerable<EventRecord>> GetEventsByStateAsync(string state, int skip = 0, int take = 20)
    {
        return GetManyAsync(
            predicate: e => e.State == state.ToUpper(),
            orderBy: q => q.OrderByDescending(e => e.CreatedOn),
            skip: skip,
            take: take);
    }
    
    public Task<IEnumerable<EventRecord>> GetEventsByCityAsync(string city, string state, int skip = 0, int take = 20)
    {
        return GetManyAsync(
            predicate: e => e.City.ToLower() == city.ToLower() && e.State == state.ToUpper(),
            orderBy: q => q.OrderByDescending(e => e.CreatedOn),
            skip: skip,
            take: take);
    }
}
```

### Step 5: Create Repository Module for DI Registration
Location: `/SocialAnimal.Web/Configuration/Modules/RepositoryModule.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Repositories;

namespace SocialAnimal.Web.Configuration.Modules;

public class RepositoryModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register generic CRUD repository
        services.AddScoped<ICrudRepo, CrudRepo>();
        
        // Register specific repositories
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IEventRepo, EventRepo>();
        services.AddScoped<IInvitationRepo, InvitationRepo>();
        services.AddScoped<IReservationRepo, ReservationRepo>();
        
        // Register generic CrudQueries (if needed separately)
        services.AddScoped(typeof(ICrudQueries<>), typeof(CrudQueries<,,>));
        
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
```

### Step 6: Create Integration Tests for Repositories
Location: `/SocialAnimal.Tests/Integration/Repositories/EventRepoTests.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Repositories;
using Xunit;

namespace SocialAnimal.Tests.Integration.Repositories;

public class EventRepoTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly IEventRepo _eventRepo;
    private readonly IClock _clock;
    
    public EventRepoTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _clock = SystemClock.Instance;
        _context = new ApplicationContext(options, _clock);
        _context.Database.EnsureCreated();
        
        _eventRepo = new EventRepo(() => _context);
    }
    
    [Fact]
    public async Task CreateEvent_Should_ReturnCreatedEvent()
    {
        // Arrange
        var eventRecord = new EventRecord
        {
            Id = 0,
            Slug = "summer-bbq-2024",
            Title = "Summer BBQ",
            AddressLine1 = "123 Main St",
            City = "Seattle",
            State = "WA",
            Postal = "98101",
            CreatedOn = _clock.GetCurrentInstant(),
            ConcurrencyToken = DateTime.UtcNow
        };
        
        // Act
        var created = await _eventRepo.Events.CreateAsync(eventRecord);
        
        // Assert
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("summer-bbq-2024", created.Slug);
    }
    
    [Fact]
    public async Task GetBySlug_Should_ReturnEvent_WhenExists()
    {
        // Arrange
        var eventRecord = new EventRecord
        {
            Id = 0,
            Slug = "test-event",
            Title = "Test Event",
            AddressLine1 = "456 Oak Ave",
            City = "Portland",
            State = "OR",
            Postal = "97201",
            CreatedOn = _clock.GetCurrentInstant(),
            ConcurrencyToken = DateTime.UtcNow
        };
        
        await _eventRepo.Events.CreateAsync(eventRecord);
        
        // Act
        var found = await _eventRepo.GetBySlugAsync("test-event");
        
        // Assert
        Assert.NotNull(found);
        Assert.Equal("test-event", found.Slug);
        Assert.Equal("Test Event", found.Title);
    }
    
    [Fact]
    public async Task GetActiveEvents_Should_ExcludeDeletedEvents()
    {
        // Arrange
        var activeEvent = new EventRecord
        {
            Id = 0,
            Slug = "active-event",
            Title = "Active Event",
            AddressLine1 = "789 Pine St",
            City = "San Francisco",
            State = "CA",
            Postal = "94102",
            CreatedOn = _clock.GetCurrentInstant(),
            ConcurrencyToken = DateTime.UtcNow
        };
        
        var deletedEvent = new EventRecord
        {
            Id = 0,
            Slug = "deleted-event",
            Title = "Deleted Event",
            AddressLine1 = "321 Elm St",
            City = "Los Angeles",
            State = "CA",
            Postal = "90001",
            DeletedAt = _clock.GetCurrentInstant(),
            CreatedOn = _clock.GetCurrentInstant(),
            ConcurrencyToken = DateTime.UtcNow
        };
        
        await _eventRepo.Events.CreateAsync(activeEvent);
        await _eventRepo.Events.CreateAsync(deletedEvent);
        
        // Act
        var activeEvents = await _eventRepo.GetActiveEventsAsync();
        
        // Assert
        Assert.Single(activeEvents);
        Assert.Equal("active-event", activeEvents.First().Slug);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}
```

## Testing Checklist

- [ ] All repository interfaces are defined
- [ ] All repository implementations are complete
- [ ] Generic repository base class works correctly
- [ ] CrudQueries implementation handles all entity types
- [ ] Soft delete filters work correctly
- [ ] Pagination works correctly
- [ ] Include/eager loading works when needed
- [ ] Unit of Work pattern is properly implemented
- [ ] All repositories are registered in DI container
- [ ] Integration tests pass for all repositories

## Performance Considerations

1. **Use AsNoTracking for read-only queries**:
```csharp
var events = await context.Events
    .AsNoTracking()
    .Where(e => e.State == "CA")
    .ToListAsync();
```

2. **Implement query result caching where appropriate**:
```csharp
private readonly MemoryCache _cache;

public async Task<EventRecord?> GetBySlugCachedAsync(string slug)
{
    return await _cache.GetOrCreateAsync($"event_{slug}", async entry =>
    {
        entry.SlidingExpiration = TimeSpan.FromMinutes(5);
        return await GetBySlugAsync(slug);
    });
}
```

3. **Use compiled queries for frequently used queries**:
```csharp
private static readonly Func<ApplicationContext, string, Task<Event?>> GetBySlugQuery =
    EF.CompileAsyncQuery((ApplicationContext context, string slug) =>
        context.Events.FirstOrDefault(e => e.Slug == slug));
```

## Common Patterns

### Specification Pattern
Consider implementing specification pattern for complex queries:

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
}

public class ActiveEventsSpecification : ISpecification<Event>
{
    public Expression<Func<Event, bool>> ToExpression()
    {
        return e => e.DeletedAt == null;
    }
}
```

### Repository Methods Naming Convention
- `GetByXAsync` - Single item retrieval
- `GetXAsync` - Multiple items retrieval
- `FindXAsync` - Search operations
- `ExistsAsync` - Existence checks
- `CountXAsync` - Count operations

## Dependencies

This task depends on:
- Tasks 1-6 (All entities and database setup)

This task must be completed before:
- Task 8 (Services need repositories)
- Phase 2 (Admin portal needs data access)

## Notes

- Consider implementing repository caching for frequently accessed data
- Add logging to repository methods for debugging
- Consider implementing audit logging for data changes
- Use transactions for operations that modify multiple entities
- Implement retry logic for transient database failures