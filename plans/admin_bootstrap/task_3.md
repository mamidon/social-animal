# Task 3: Create Invitation Entity and Domain Model

## Objective
Create a new Invitation entity and domain model to represent event invitations with a foreign key relationship to Event.

## Requirements
- Primary key `Id`
- `Slug` field as opaque public identifier with unique index
- Foreign key relationship to Event
- Soft delete support with `CreatedAt`, `UpdatedAt`, `DeletedAt` timestamps
- Follow existing patterns for entity/record mapping

## Implementation Steps

### Step 1: Create InvitationRecord Domain Model
Location: `/SocialAnimal.Core/Domain/InvitationRecord.cs`

```csharp
using NodaTime;

namespace SocialAnimal.Core.Domain;

public record InvitationRecord : BaseRecord
{
    public required string Slug { get; init; }
    public required long EventId { get; init; }
    public Instant? DeletedAt { get; init; }
    
    // Computed properties
    public bool IsDeleted => DeletedAt.HasValue;
    
    // Related entity records (populated when needed)
    public EventRecord? Event { get; init; }
    public ICollection<ReservationRecord>? Reservations { get; init; }
}
```

### Step 2: Create Invitation Entity
Location: `/SocialAnimal.Infrastructure/Db/Entities/Invitation.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(EventId))]
[Index(nameof(DeletedAt))]
public class Invitation : BaseEntity, IInto<InvitationRecord>, IFrom<Invitation, InvitationRecord>
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; } // Opaque public identifier
    
    [Required]
    public required long EventId { get; set; }
    
    public Instant? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual Event Event { get; set; } = null!;
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    
    // Mapping implementations
    public InvitationRecord Into()
    {
        return new InvitationRecord
        {
            Id = Id,
            Slug = Slug,
            EventId = EventId,
            DeletedAt = DeletedAt,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken,
            Event = Event?.Into(),
            Reservations = Reservations?.Select(r => r.Into()).ToList()
        };
    }
    
    public static InvitationRecord From(Invitation entity)
    {
        return entity.Into();
    }
    
    public static Invitation FromRecord(InvitationRecord record)
    {
        return new Invitation
        {
            Id = record.Id,
            Slug = record.Slug,
            EventId = record.EventId,
            DeletedAt = record.DeletedAt,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}
```

### Step 3: Create InvitationStub for Creation/Updates
Location: `/SocialAnimal.Core/Stubs/InvitationStub.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record InvitationStub
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; init; }
    
    [Required]
    public required long EventId { get; init; }
}

public record InvitationUpdateStub
{
    // Currently no updatable fields beyond soft delete
    // This stub exists for future extensibility
}
```

### Step 4: Create InvitationConfiguration
Location: `/SocialAnimal.Infrastructure/Db/Context/Configuration/InvitationConfiguration.cs`

```csharp
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
```

### Step 5: Create IInvitationRepo Interface
Location: `/SocialAnimal.Core/Repositories/IInvitationRepo.cs`

```csharp
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IInvitationRepo
{
    ICrudQueries<InvitationRecord> Invitations { get; }
    Task<InvitationRecord?> GetBySlugAsync(string slug);
    Task<InvitationRecord?> GetBySlugWithDetailsAsync(string slug);
    Task<bool> SlugExistsAsync(string slug);
    Task<IEnumerable<InvitationRecord>> GetByEventIdAsync(long eventId, int skip = 0, int take = 20);
    Task<IEnumerable<InvitationRecord>> GetActiveInvitationsAsync(int skip = 0, int take = 20);
    Task<IEnumerable<InvitationRecord>> GetDeletedInvitationsAsync(int skip = 0, int take = 20);
    Task<int> GetInvitationCountForEventAsync(long eventId);
}
```

### Step 6: Create InvitationRepo Implementation
Location: `/SocialAnimal.Infrastructure/Repositories/InvitationRepo.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class InvitationRepo : IInvitationRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public InvitationRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Invitations = new CrudQueries<ApplicationContext, Invitation, InvitationRecord>(
            unitOfWork, c => c.Invitations);
    }
    
    public ICrudQueries<InvitationRecord> Invitations { get; }
    
    public async Task<InvitationRecord?> GetBySlugAsync(string slug)
    {
        using var context = _unitOfWork();
        var invitation = await context.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Slug == slug);
        return invitation?.Into();
    }
    
    public async Task<InvitationRecord?> GetBySlugWithDetailsAsync(string slug)
    {
        using var context = _unitOfWork();
        var invitation = await context.Invitations
            .IgnoreQueryFilters()
            .Include(i => i.Event)
            .Include(i => i.Reservations)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(i => i.Slug == slug);
        return invitation?.Into();
    }
    
    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var context = _unitOfWork();
        return await context.Invitations
            .IgnoreQueryFilters()
            .AnyAsync(i => i.Slug == slug);
    }
    
    public async Task<IEnumerable<InvitationRecord>> GetByEventIdAsync(long eventId, int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var invitations = await context.Invitations
            .Where(i => i.EventId == eventId)
            .OrderByDescending(i => i.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return invitations.Select(i => i.Into());
    }
    
    public async Task<IEnumerable<InvitationRecord>> GetActiveInvitationsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var invitations = await context.Invitations
            .Include(i => i.Event)
            .OrderByDescending(i => i.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return invitations.Select(i => i.Into());
    }
    
    public async Task<IEnumerable<InvitationRecord>> GetDeletedInvitationsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var invitations = await context.Invitations
            .IgnoreQueryFilters()
            .Include(i => i.Event)
            .Where(i => i.DeletedAt != null)
            .OrderByDescending(i => i.DeletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return invitations.Select(i => i.Into());
    }
    
    public async Task<int> GetInvitationCountForEventAsync(long eventId)
    {
        using var context = _unitOfWork();
        return await context.Invitations
            .CountAsync(i => i.EventId == eventId);
    }
}
```

### Step 7: Create InvitationService
Location: `/SocialAnimal.Core/Services/InvitationService.cs`

```csharp
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Core.Services;

public class InvitationService
{
    private readonly ICrudRepo _crudRepo;
    private readonly IInvitationRepo _invitationRepo;
    private readonly IEventRepo _eventRepo;
    private readonly IClockPortal _clock;
    private readonly ILoggerPortal _logger;
    
    public InvitationService(
        ICrudRepo crudRepo,
        IInvitationRepo invitationRepo,
        IEventRepo eventRepo,
        IClockPortal clock,
        ILoggerPortal logger)
    {
        _crudRepo = crudRepo;
        _invitationRepo = invitationRepo;
        _eventRepo = eventRepo;
        _clock = clock;
        _logger = logger;
    }
    
    public async Task<InvitationRecord> CreateInvitationAsync(InvitationStub stub)
    {
        // Validate event exists
        var eventRecord = await _eventRepo.Events.GetAsync(stub.EventId);
        if (eventRecord == null)
        {
            throw new ArgumentException($"Event with ID {stub.EventId} not found");
        }
        
        // Check if slug is unique
        if (await _invitationRepo.SlugExistsAsync(stub.Slug))
        {
            throw new ArgumentException($"Invitation with slug '{stub.Slug}' already exists");
        }
        
        var invitation = new InvitationRecord
        {
            Id = 0, // Will be set by database
            Slug = stub.Slug,
            EventId = stub.EventId,
            CreatedOn = _clock.GetCurrentInstant(),
            UpdatedOn = null,
            DeletedAt = null,
            ConcurrencyToken = DateTime.UtcNow
        };
        
        var created = await _crudRepo.CreateAsync(invitation);
        _logger.LogInformation($"Created invitation {created.Slug} for event {eventRecord.Slug}");
        
        return created;
    }
    
    public async Task<InvitationRecord?> GetInvitationBySlugAsync(string slug)
    {
        return await _invitationRepo.GetBySlugWithDetailsAsync(slug);
    }
    
    public async Task<bool> DeleteInvitationAsync(string slug)
    {
        var invitation = await _invitationRepo.GetBySlugAsync(slug);
        if (invitation == null)
        {
            return false;
        }
        
        invitation = invitation with { DeletedAt = _clock.GetCurrentInstant() };
        await _crudRepo.UpdateAsync(invitation);
        
        _logger.LogInformation($"Soft deleted invitation {slug}");
        return true;
    }
    
    public async Task<IEnumerable<InvitationRecord>> GetInvitationsForEventAsync(long eventId, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        return await _invitationRepo.GetByEventIdAsync(eventId, skip, pageSize);
    }
    
    public async Task<string> GenerateUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        var counter = 1;
        
        while (await _invitationRepo.SlugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }
        
        return slug;
    }
}
```

## Testing Checklist

- [ ] Unit test for InvitationRecord creation and property access
- [ ] Unit test for Invitation entity to InvitationRecord mapping
- [ ] Unit test for InvitationRecord to Invitation entity mapping  
- [ ] Integration test for InvitationRepo CRUD operations
- [ ] Integration test for soft delete functionality
- [ ] Integration test for unique slug constraint
- [ ] Integration test for foreign key relationship to Event
- [ ] Test cascade behavior when Event is deleted
- [ ] Test query filters exclude deleted invitations by default
- [ ] Service test for creating invitations with validation
- [ ] Service test for slug generation uniqueness

## Database Schema

```sql
CREATE TABLE invitations (
    id BIGSERIAL PRIMARY KEY,
    slug VARCHAR(100) NOT NULL,
    event_id BIGINT NOT NULL,
    created_on TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_on TIMESTAMP WITH TIME ZONE,
    deleted_at TIMESTAMP WITH TIME ZONE,
    concurrency_token TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    
    CONSTRAINT fk_invitations_event 
        FOREIGN KEY (event_id) 
        REFERENCES events(id) 
        ON DELETE RESTRICT,
    
    CONSTRAINT uq_invitations_slug UNIQUE (slug)
);

CREATE INDEX ix_invitations_slug ON invitations(slug);
CREATE INDEX ix_invitations_event_id ON invitations(event_id);
CREATE INDEX ix_invitations_deleted_at ON invitations(deleted_at);
```

## Validation Rules

- Slug: Required, max 100 chars, globally unique (including soft-deleted)
- EventId: Required, must reference existing Event
- DeletedAt: Optional, when set marks record as soft-deleted

## Business Rules

1. An invitation must be associated with an existing event
2. Invitation slugs must be globally unique across all invitations
3. Deleting an event should be restricted if invitations exist (or handle appropriately)
4. Soft-deleted invitations should not appear in normal queries
5. Multiple invitations can be created for the same event

## Dependencies

This task depends on:
- Task 2 (Event entity must exist for FK relationship)

This task must be completed before:
- Task 4 (Reservation entity needs Invitation FK)
- Task 5 (Database context update)
- Task 6 (Database migration generation)
- Task 7 (Repository layer updates)

## Notes

- Consider adding invitation code generation for unique, shareable links
- The slug could be used as part of the invitation URL (e.g., `/invite/{slug}`)
- Future enhancement: Add expiration dates for invitations
- Future enhancement: Add maximum uses per invitation
- Consider adding invitation status (sent, viewed, responded)