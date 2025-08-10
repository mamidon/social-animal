# Task 2: Refactor Event Entity

## Objective
Refactor the existing Event entity to match new requirements with detailed address fields, removing unnecessary fields, and adding soft delete support.

## Requirements Changes
- Replace `Handle` with `Slug` for consistency
- Remove `Reference`, `Description`, `StartsOn`, `EndsOn`, `Location`, `MaxAttendees`, `IsPublic`, `IsCancelled` fields
- Remove `OrganizerId` foreign key and relationship
- Add address fields: `AddressLine1`, `AddressLine2`, `City`, `State`, `Postal`
- Add `DeletedAt` field for soft delete support
- Keep only required fields per specification

## Implementation Steps

### Step 1: Create EventRecord Domain Model
Location: `/SocialAnimal.Core/Domain/EventRecord.cs`

```csharp
using NodaTime;

namespace SocialAnimal.Core.Domain;

public record EventRecord : BaseRecord
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; } // Two-letter state code
    public required string Postal { get; init; }
    public Instant? DeletedAt { get; init; }
    
    public bool IsDeleted => DeletedAt.HasValue;
    
    public string FullAddress => string.IsNullOrWhiteSpace(AddressLine2) 
        ? $"{AddressLine1}, {City}, {State} {Postal}"
        : $"{AddressLine1}, {AddressLine2}, {City}, {State} {Postal}";
}
```

### Step 2: Refactor Event Entity
Location: `/SocialAnimal.Infrastructure/Db/Entities/Event.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(DeletedAt))]
public class Event : BaseEntity, IInto<EventRecord>, IFrom<Event, EventRecord>
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; } // Opaque public identifier
    
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }
    
    [Required]
    [MaxLength(200)]
    public required string AddressLine1 { get; set; }
    
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string City { get; set; }
    
    [Required]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "State must be a two-letter code")]
    public required string State { get; set; }
    
    [Required]
    [MaxLength(20)]
    public required string Postal { get; set; }
    
    public Instant? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    
    // Mapping implementations
    public EventRecord Into()
    {
        return new EventRecord
        {
            Id = Id,
            Slug = Slug,
            Title = Title,
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            City = City,
            State = State,
            Postal = Postal,
            DeletedAt = DeletedAt,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken
        };
    }
    
    public static EventRecord From(Event entity)
    {
        return entity.Into();
    }
    
    public static Event FromRecord(EventRecord record)
    {
        return new Event
        {
            Id = record.Id,
            Slug = record.Slug,
            Title = record.Title,
            AddressLine1 = record.AddressLine1,
            AddressLine2 = record.AddressLine2,
            City = record.City,
            State = record.State,
            Postal = record.Postal,
            DeletedAt = record.DeletedAt,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}
```

### Step 3: Create EventStub for Creation/Updates
Location: `/SocialAnimal.Core/Stubs/EventStub.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record EventStub
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; init; }
    
    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }
    
    [Required]
    [MaxLength(200)]
    public required string AddressLine1 { get; init; }
    
    [MaxLength(200)]
    public string? AddressLine2 { get; init; }
    
    [Required]
    [MaxLength(100)]
    public required string City { get; init; }
    
    [Required]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "State must be a two-letter code")]
    public required string State { get; init; }
    
    [Required]
    [MaxLength(20)]
    public required string Postal { get; init; }
}
```

### Step 4: Create EventConfiguration
Location: `/SocialAnimal.Infrastructure/Db/Context/Configuration/EventConfiguration.cs`

```csharp
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
```

### Step 5: Create IEventRepo Interface
Location: `/SocialAnimal.Core/Repositories/IEventRepo.cs`

```csharp
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IEventRepo
{
    ICrudQueries<EventRecord> Events { get; }
    Task<EventRecord?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug);
    Task<IEnumerable<EventRecord>> GetActiveEventsAsync(int skip = 0, int take = 20);
    Task<IEnumerable<EventRecord>> GetDeletedEventsAsync(int skip = 0, int take = 20);
    Task<IEnumerable<EventRecord>> GetEventsByStateAsync(string state, int skip = 0, int take = 20);
    Task<IEnumerable<EventRecord>> GetEventsByCityAsync(string city, string state, int skip = 0, int take = 20);
}
```

### Step 6: Create EventRepo Implementation
Location: `/SocialAnimal.Infrastructure/Repositories/EventRepo.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class EventRepo : IEventRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public EventRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Events = new CrudQueries<ApplicationContext, Event, EventRecord>(
            unitOfWork, c => c.Events);
    }
    
    public ICrudQueries<EventRecord> Events { get; }
    
    public async Task<EventRecord?> GetBySlugAsync(string slug)
    {
        using var context = _unitOfWork();
        var evt = await context.Events
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Slug == slug);
        return evt?.Into();
    }
    
    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var context = _unitOfWork();
        return await context.Events
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Slug == slug);
    }
    
    public async Task<IEnumerable<EventRecord>> GetActiveEventsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .OrderByDescending(e => e.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
    
    public async Task<IEnumerable<EventRecord>> GetDeletedEventsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .IgnoreQueryFilters()
            .Where(e => e.DeletedAt != null)
            .OrderByDescending(e => e.DeletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
    
    public async Task<IEnumerable<EventRecord>> GetEventsByStateAsync(string state, int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .Where(e => e.State == state.ToUpper())
            .OrderByDescending(e => e.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
    
    public async Task<IEnumerable<EventRecord>> GetEventsByCityAsync(string city, string state, int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .Where(e => e.City.ToLower() == city.ToLower() && e.State == state.ToUpper())
            .OrderByDescending(e => e.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
}
```

## Testing Checklist

- [ ] Unit test for EventRecord creation and property access
- [ ] Unit test for Event entity to EventRecord mapping
- [ ] Unit test for EventRecord to Event entity mapping
- [ ] Unit test for FullAddress property formatting
- [ ] Integration test for EventRepo CRUD operations
- [ ] Integration test for soft delete functionality
- [ ] Integration test for unique slug constraint
- [ ] Validation test for state code format (2 uppercase letters)
- [ ] Test query filters exclude deleted events by default
- [ ] Test location-based queries (by state, by city)

## Migration Considerations

Since this is refactoring an existing Event table:
1. **Data Backup**: Create full backup of existing events table
2. **Migration Strategy**:
   - Drop foreign key constraints to User (OrganizerId)
   - Remove EventAttendance relationship temporarily
   - Add new address columns with temporary defaults
   - Migrate existing location data to address fields if possible
   - Generate slugs from existing Handle values
   - Set DeletedAt to null for all records
   - Drop removed columns
3. **Data Mapping**:
   - Handle → Slug (direct copy or regenerate)
   - Location → Parse into address fields if structured
   - Set default values for required address fields if missing

## Validation Rules

- Slug: Required, max 100 chars, globally unique (including soft-deleted)
- Title: Required, max 200 chars
- AddressLine1: Required, max 200 chars
- AddressLine2: Optional, max 200 chars
- City: Required, max 100 chars
- State: Required, exactly 2 uppercase letters (US state codes)
- Postal: Required, max 20 chars (supports US ZIP and ZIP+4)
- DeletedAt: Optional, when set marks record as soft-deleted

## US State Codes Reference
Valid state codes: AL, AK, AZ, AR, CA, CO, CT, DE, FL, GA, HI, ID, IL, IN, IA, KS, KY, LA, ME, MD, MA, MI, MN, MS, MO, MT, NE, NV, NH, NJ, NM, NY, NC, ND, OH, OK, OR, PA, RI, SC, SD, TN, TX, UT, VT, VA, WA, WV, WI, WY, DC

## Dependencies

This task must be completed before:
- Task 3 (Invitation entity needs Event FK)
- Task 5 (Database context update)
- Task 6 (Database migration generation)
- Task 7 (Repository layer updates)

## Notes

- Consider adding postal code validation for US ZIP codes
- State validation could be enhanced with a lookup table
- Address formatting helper methods can be added to EventRecord
- Consider geocoding service integration in future iterations
- The slug should be URL-safe (e.g., "summer-bbq-2024")