# Task 4: Create Reservation Entity and Domain Model

## Objective
Create a new Reservation entity to track RSVPs with foreign key relationships to both Invitation and User, including party size support where 0 indicates regrets.

## Requirements
- Primary key `Id`
- Foreign key to Invitation
- Foreign key to User
- `PartySize` field (unsigned int, 0 = sends regrets)
- No soft delete fields per specification (unlike other entities)
- Follow existing patterns for entity/record mapping

## Implementation Steps

### Step 1: Create ReservationRecord Domain Model
Location: `/SocialAnimal.Core/Domain/ReservationRecord.cs`

```csharp
using NodaTime;

namespace SocialAnimal.Core.Domain;

public record ReservationRecord : BaseRecord
{
    public required long InvitationId { get; init; }
    public required long UserId { get; init; }
    public required uint PartySize { get; init; } // 0 = sends regrets
    
    // Computed properties
    public bool IsAttending => PartySize > 0;
    public bool HasDeclined => PartySize == 0;
    
    // Related entity records (populated when needed)
    public InvitationRecord? Invitation { get; init; }
    public UserRecord? User { get; init; }
}
```

### Step 2: Create Reservation Entity
Location: `/SocialAnimal.Infrastructure/Db/Entities/Reservation.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(InvitationId), nameof(UserId), IsUnique = true)]
[Index(nameof(InvitationId))]
[Index(nameof(UserId))]
public class Reservation : BaseEntity, IInto<ReservationRecord>, IFrom<Reservation, ReservationRecord>
{
    [Required]
    public required long InvitationId { get; set; }
    
    [Required]
    public required long UserId { get; set; }
    
    [Required]
    [Range(0, uint.MaxValue)]
    public required uint PartySize { get; set; } // 0 = sends regrets
    
    // Navigation properties
    public virtual Invitation Invitation { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    
    // Mapping implementations
    public ReservationRecord Into()
    {
        return new ReservationRecord
        {
            Id = Id,
            InvitationId = InvitationId,
            UserId = UserId,
            PartySize = PartySize,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken,
            Invitation = Invitation?.Into(),
            User = User?.Into()
        };
    }
    
    public static ReservationRecord From(Reservation entity)
    {
        return entity.Into();
    }
    
    public static Reservation FromRecord(ReservationRecord record)
    {
        return new Reservation
        {
            Id = record.Id,
            InvitationId = record.InvitationId,
            UserId = record.UserId,
            PartySize = record.PartySize,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}
```

### Step 3: Create ReservationStub for Creation/Updates
Location: `/SocialAnimal.Core/Stubs/ReservationStub.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record ReservationStub
{
    [Required]
    public required long InvitationId { get; init; }
    
    [Required]
    public required long UserId { get; init; }
    
    [Required]
    [Range(0, 100)] // Reasonable max party size
    public required uint PartySize { get; init; }
}

public record ReservationUpdateStub
{
    [Required]
    [Range(0, 100)]
    public required uint PartySize { get; init; }
}
```

### Step 4: Create ReservationConfiguration
Location: `/SocialAnimal.Infrastructure/Db/Context/Configuration/ReservationConfiguration.cs`

```csharp
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
```

### Step 5: Create IReservationRepo Interface
Location: `/SocialAnimal.Core/Repositories/IReservationRepo.cs`

```csharp
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IReservationRepo
{
    ICrudQueries<ReservationRecord> Reservations { get; }
    Task<ReservationRecord?> GetByInvitationAndUserAsync(long invitationId, long userId);
    Task<IEnumerable<ReservationRecord>> GetByInvitationIdAsync(long invitationId);
    Task<IEnumerable<ReservationRecord>> GetByUserIdAsync(long userId);
    Task<IEnumerable<ReservationRecord>> GetAcceptedReservationsAsync(long invitationId);
    Task<IEnumerable<ReservationRecord>> GetDeclinedReservationsAsync(long invitationId);
    Task<int> GetTotalPartySizeForInvitationAsync(long invitationId);
    Task<int> GetAcceptedCountForInvitationAsync(long invitationId);
    Task<int> GetDeclinedCountForInvitationAsync(long invitationId);
}
```

### Step 6: Create ReservationRepo Implementation
Location: `/SocialAnimal.Infrastructure/Repositories/ReservationRepo.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class ReservationRepo : IReservationRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public ReservationRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Reservations = new CrudQueries<ApplicationContext, Reservation, ReservationRecord>(
            unitOfWork, c => c.Reservations);
    }
    
    public ICrudQueries<ReservationRecord> Reservations { get; }
    
    public async Task<ReservationRecord?> GetByInvitationAndUserAsync(long invitationId, long userId)
    {
        using var context = _unitOfWork();
        var reservation = await context.Reservations
            .Include(r => r.Invitation)
                .ThenInclude(i => i.Event)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.InvitationId == invitationId && r.UserId == userId);
        return reservation?.Into();
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetByInvitationIdAsync(long invitationId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.User)
            .Where(r => r.InvitationId == invitationId)
            .OrderBy(r => r.User.LastName)
            .ThenBy(r => r.User.FirstName)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetByUserIdAsync(long userId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.Invitation)
                .ThenInclude(i => i.Event)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedOn)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetAcceptedReservationsAsync(long invitationId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.User)
            .Where(r => r.InvitationId == invitationId && r.PartySize > 0)
            .OrderBy(r => r.User.LastName)
            .ThenBy(r => r.User.FirstName)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetDeclinedReservationsAsync(long invitationId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.User)
            .Where(r => r.InvitationId == invitationId && r.PartySize == 0)
            .OrderBy(r => r.User.LastName)
            .ThenBy(r => r.User.FirstName)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<int> GetTotalPartySizeForInvitationAsync(long invitationId)
    {
        using var context = _unitOfWork();
        return await context.Reservations
            .Where(r => r.InvitationId == invitationId && r.PartySize > 0)
            .SumAsync(r => (int)r.PartySize);
    }
    
    public async Task<int> GetAcceptedCountForInvitationAsync(long invitationId)
    {
        using var context = _unitOfWork();
        return await context.Reservations
            .CountAsync(r => r.InvitationId == invitationId && r.PartySize > 0);
    }
    
    public async Task<int> GetDeclinedCountForInvitationAsync(long invitationId)
    {
        using var context = _unitOfWork();
        return await context.Reservations
            .CountAsync(r => r.InvitationId == invitationId && r.PartySize == 0);
    }
}
```

### Step 7: Create ReservationService
Location: `/SocialAnimal.Core/Services/ReservationService.cs`

```csharp
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Core.Services;

public class ReservationService
{
    private readonly ICrudRepo _crudRepo;
    private readonly IReservationRepo _reservationRepo;
    private readonly IInvitationRepo _invitationRepo;
    private readonly IUserRepo _userRepo;
    private readonly IClockPortal _clock;
    private readonly ILoggerPortal _logger;
    
    public ReservationService(
        ICrudRepo crudRepo,
        IReservationRepo reservationRepo,
        IInvitationRepo invitationRepo,
        IUserRepo userRepo,
        IClockPortal clock,
        ILoggerPortal logger)
    {
        _crudRepo = crudRepo;
        _reservationRepo = reservationRepo;
        _invitationRepo = invitationRepo;
        _userRepo = userRepo;
        _clock = clock;
        _logger = logger;
    }
    
    public async Task<ReservationRecord> CreateOrUpdateReservationAsync(ReservationStub stub)
    {
        // Validate invitation exists
        var invitation = await _invitationRepo.Invitations.GetAsync(stub.InvitationId);
        if (invitation == null)
        {
            throw new ArgumentException($"Invitation with ID {stub.InvitationId} not found");
        }
        
        // Validate user exists
        var user = await _userRepo.Users.GetAsync(stub.UserId);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {stub.UserId} not found");
        }
        
        // Check if reservation already exists
        var existing = await _reservationRepo.GetByInvitationAndUserAsync(stub.InvitationId, stub.UserId);
        
        if (existing != null)
        {
            // Update existing reservation
            var updated = existing with 
            { 
                PartySize = stub.PartySize,
                UpdatedOn = _clock.GetCurrentInstant()
            };
            
            await _crudRepo.UpdateAsync(updated);
            
            _logger.LogInformation($"Updated reservation for user {user.Slug} on invitation {invitation.Slug}: party size {stub.PartySize}");
            return updated;
        }
        else
        {
            // Create new reservation
            var reservation = new ReservationRecord
            {
                Id = 0, // Will be set by database
                InvitationId = stub.InvitationId,
                UserId = stub.UserId,
                PartySize = stub.PartySize,
                CreatedOn = _clock.GetCurrentInstant(),
                UpdatedOn = null,
                ConcurrencyToken = DateTime.UtcNow
            };
            
            var created = await _crudRepo.CreateAsync(reservation);
            
            var action = stub.PartySize > 0 ? "accepted" : "declined";
            _logger.LogInformation($"User {user.Slug} {action} invitation {invitation.Slug} with party size {stub.PartySize}");
            
            return created;
        }
    }
    
    public async Task<ReservationRecord?> GetReservationAsync(long invitationId, long userId)
    {
        return await _reservationRepo.GetByInvitationAndUserAsync(invitationId, userId);
    }
    
    public async Task<ReservationSummary> GetReservationSummaryAsync(long invitationId)
    {
        var accepted = await _reservationRepo.GetAcceptedCountForInvitationAsync(invitationId);
        var declined = await _reservationRepo.GetDeclinedCountForInvitationAsync(invitationId);
        var totalPartySize = await _reservationRepo.GetTotalPartySizeForInvitationAsync(invitationId);
        
        return new ReservationSummary
        {
            InvitationId = invitationId,
            AcceptedCount = accepted,
            DeclinedCount = declined,
            TotalPartySize = (uint)totalPartySize,
            TotalResponses = accepted + declined
        };
    }
    
    public async Task<bool> DeleteReservationAsync(long id)
    {
        return await _crudRepo.DeleteAsync<ReservationRecord>(id);
    }
}

public record ReservationSummary
{
    public required long InvitationId { get; init; }
    public required int AcceptedCount { get; init; }
    public required int DeclinedCount { get; init; }
    public required uint TotalPartySize { get; init; }
    public required int TotalResponses { get; init; }
    
    public int PendingCount(int totalInvitations) => totalInvitations - TotalResponses;
}
```

## Testing Checklist

- [ ] Unit test for ReservationRecord creation and property access
- [ ] Unit test for IsAttending and HasDeclined computed properties
- [ ] Unit test for Reservation entity to ReservationRecord mapping
- [ ] Unit test for ReservationRecord to Reservation entity mapping
- [ ] Integration test for ReservationRepo CRUD operations
- [ ] Integration test for unique constraint on invitation+user combination
- [ ] Integration test for foreign key relationships
- [ ] Test party size 0 correctly indicates declined invitation
- [ ] Test reservation summary calculations
- [ ] Service test for create/update reservation logic
- [ ] Service test for preventing duplicate reservations

## Database Schema

```sql
CREATE TABLE reservations (
    id BIGSERIAL PRIMARY KEY,
    invitation_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    party_size INTEGER NOT NULL CHECK (party_size >= 0),
    created_on TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_on TIMESTAMP WITH TIME ZONE,
    concurrency_token TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    
    CONSTRAINT fk_reservations_invitation 
        FOREIGN KEY (invitation_id) 
        REFERENCES invitations(id) 
        ON DELETE CASCADE,
    
    CONSTRAINT fk_reservations_user 
        FOREIGN KEY (user_id) 
        REFERENCES users(id) 
        ON DELETE RESTRICT,
    
    CONSTRAINT uq_reservations_invitation_user 
        UNIQUE (invitation_id, user_id)
);

CREATE INDEX ix_reservations_invitation_id ON reservations(invitation_id);
CREATE INDEX ix_reservations_user_id ON reservations(user_id);
CREATE INDEX ix_reservations_party_size ON reservations(party_size);
```

## Validation Rules

- InvitationId: Required, must reference existing Invitation
- UserId: Required, must reference existing User
- PartySize: Required, non-negative integer (0-100 reasonable range)
- Combination of InvitationId + UserId must be unique

## Business Rules

1. A user can only have one reservation per invitation
2. Party size of 0 means the user is declining the invitation
3. Party size > 0 means the user is accepting with that many attendees
4. Users can update their reservation (change party size or decline)
5. Deleting an invitation cascades to delete its reservations
6. Users with reservations cannot be deleted (restrict delete)

## Special Considerations

### No Soft Delete
Unlike other entities, Reservation does not have a `DeletedAt` field. This is intentional:
- Reservations represent current state of RSVP
- Historical tracking can be done through audit logs if needed
- Simplifies queries and business logic

### Party Size Semantics
- 0: User explicitly declined the invitation
- 1: User is attending alone
- 2+: User is bringing guests (party size includes the user)
- No reservation record: User hasn't responded yet

## Dependencies

This task depends on:
- Task 1 (User entity must exist for FK relationship)
- Task 3 (Invitation entity must exist for FK relationship)

This task must be completed before:
- Task 5 (Database context update)
- Task 6 (Database migration generation)
- Task 7 (Repository layer updates)

## Notes

- Consider adding validation for maximum party size per event
- Future enhancement: Track changes to reservations (audit log)
- Future enhancement: Add dietary restrictions or special requests
- Future enhancement: Add plus-one naming functionality
- The unique constraint prevents duplicate RSVPs from the same user