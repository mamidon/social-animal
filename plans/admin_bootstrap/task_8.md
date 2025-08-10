# Task 8: Create Service Layer

## Objective
Implement the business logic layer with service classes that orchestrate operations between the presentation layer and the repository layer. Services will handle business rules, validation, and complex operations that span multiple entities.

## Requirements
- Create service classes for each major domain entity
- Implement business validation and rules
- Handle complex operations and workflows
- Follow existing service patterns in the codebase
- Ensure proper transaction handling for multi-entity operations
- Implement error handling and logging

## Implementation Steps

### Step 1: Create Core Service Interfaces
Define service interfaces in the Core layer to maintain clean architecture separation.

Location: `/SocialAnimal.Core/Services/`

Create the following interfaces:
- `IEventService` - Business operations for events
- `IInvitationService` - Invitation management logic
- `IReservationService` - RSVP and reservation handling
- Update existing `IUserService` if present

Each interface should define methods for:
- Creating entities with business validation
- Updating entities with state rules
- Complex queries that require business logic
- Cross-entity operations
- Soft delete operations

### Step 2: Implement EventService
Location: `/SocialAnimal.Infrastructure/Services/EventService.cs`

The EventService should implement:
- `CreateEventAsync(EventStub stub)` - Create new event with slug generation
- `UpdateEventAsync(long eventId, EventStub stub)` - Update event with validation
- `DeleteEventAsync(long eventId)` - Soft delete an event
- `RestoreEventAsync(long eventId)` - Restore soft-deleted event
- `GenerateUniqueSlugAsync(string title)` - Generate unique slug from title
- `ValidateAddressAsync(EventStub stub)` - Validate address fields
- `GetEventWithInvitationsAsync(string slug)` - Get event with related data
- `GetUpcomingEventsAsync(int skip, int take)` - Get future events
- `GetPastEventsAsync(int skip, int take)` - Get historical events

Business rules to implement:
- Slug must be unique across all events (including soft-deleted)
- Address validation (required fields: AddressLine1, City, State, Postal)
- State must be valid 2-letter code
- Postal code format validation
- Cannot delete event with existing reservations
- Event dates must be in the future for new events

### Step 3: Implement InvitationService
Location: `/SocialAnimal.Infrastructure/Services/InvitationService.cs`

The InvitationService should implement:
- `CreateInvitationAsync(InvitationStub stub)` - Create invitation with validation
- `CreateBulkInvitationsAsync(long eventId, List<InvitationStub> stubs)` - Bulk create
- `UpdateInvitationAsync(long invitationId, InvitationStub stub)` - Update invitation
- `DeleteInvitationAsync(long invitationId)` - Soft delete invitation
- `GenerateInvitationSlugAsync(string name, long eventId)` - Generate unique slug
- `GetInvitationBySlugAsync(string slug)` - Retrieve by slug
- `GetEventInvitationsAsync(long eventId, int skip, int take)` - Get all for event
- `GetInvitationWithReservationAsync(string slug)` - Get with RSVP data
- `SendInvitationAsync(long invitationId)` - Mark as sent
- `ResendInvitationAsync(long invitationId)` - Handle resend logic

Business rules to implement:
- Invitation slug must be unique within an event
- Cannot create invitation for non-existent event
- Cannot delete invitation with existing reservation
- Name and either email or phone required
- Email format validation if provided
- Phone format validation if provided
- Max party size validation (configurable, default 10)
- Cannot send invitation without contact info

### Step 4: Implement ReservationService
Location: `/SocialAnimal.Infrastructure/Services/ReservationService.cs`

The ReservationService should implement:
- `CreateReservationAsync(ReservationStub stub)` - Create RSVP
- `UpdateReservationAsync(long reservationId, ReservationStub stub)` - Update RSVP
- `DeleteReservationAsync(long reservationId)` - Remove reservation
- `GetReservationByInvitationAsync(long invitationId)` - Get for invitation
- `GetEventReservationsAsync(long eventId)` - Get all for event
- `GetUserReservationsAsync(long userId)` - Get all for user
- `CalculateEventAttendanceAsync(long eventId)` - Calculate total attendance
- `SendReminderAsync(long reservationId)` - Send reminder
- `MarkAsAttendedAsync(long reservationId)` - Mark attendance

Business rules to implement:
- Only one reservation per invitation
- Party size 0 indicates "regrets" (not attending)
- Party size cannot exceed invitation max
- Cannot create reservation for non-existent invitation
- Cannot modify reservation after event date
- User association optional (for tracking)
- Reservation updates should track history

### Step 5: Update UserService
Location: `/SocialAnimal.Infrastructure/Services/UserService.cs`

Update existing UserService or create new with:
- `CreateUserAsync(UserStub stub)` - Create with phone validation
- `UpdateUserAsync(long userId, UserStub stub)` - Update user
- `DeleteUserAsync(long userId)` - Soft delete user
- `RestoreUserAsync(long userId)` - Restore deleted user
- `GenerateUserSlugAsync(string name)` - Generate unique slug
- `GetUserByPhoneAsync(string phone)` - Find by phone
- `GetUserBySlugAsync(string slug)` - Find by slug
- `GetUserWithReservationsAsync(long userId)` - Get with RSVPs
- `MergeUsersAsync(long sourceId, long targetId)` - Merge duplicate users

Business rules to implement:
- Phone number must be unique (normalized format)
- Slug must be unique across all users
- Phone format validation and normalization
- Cannot delete user with active reservations
- Name required for all users
- Handle user merging for duplicates

### Step 6: Create Service Base Class
Location: `/SocialAnimal.Infrastructure/Services/ServiceBase.cs`

Create base class with common functionality:
- Logging initialization
- Common validation methods
- Transaction handling helpers
- Error handling patterns
- Slug generation utilities
- Pagination helpers

Include methods like:
- `NormalizePhoneNumber(string phone)` - Standardize phone format
- `ValidateEmail(string email)` - Email validation
- `GenerateSlug(string input)` - Basic slug generation
- `EnsureUniqueSlug(string baseSlug, Func<string, Task<bool>> existsCheck)`
- `ValidatePageParameters(int skip, int take)` - Pagination validation

### Step 7: Implement Service Stubs (DTOs)
Location: `/SocialAnimal.Core/Services/Stubs/`

Create stub classes for service operations:
- `EventStub` - Event creation/update data
- `InvitationStub` - Invitation creation/update data
- `ReservationStub` - Reservation creation/update data
- `UserStub` - User creation/update data

Each stub should contain:
- Only the fields needed for create/update operations
- Validation attributes where appropriate
- Comments documenting business rules

### Step 8: Create Service Module for DI
Location: `/SocialAnimal.Web/Configuration/Modules/ServiceModule.cs`

Create or update service registration module:
- Register all service interfaces and implementations
- Use appropriate service lifetimes (Scoped for most services)
- Register any service dependencies
- Consider using reflection-based registration by convention

Pattern to follow:
- Services ending with "Service" registered as Scoped
- Singleton for stateless utility services
- Transient for lightweight, stateless services

### Step 9: Implement Cross-Service Operations
Location: `/SocialAnimal.Infrastructure/Services/Orchestrators/`

Create orchestrator services for complex workflows:
- `EventOrchestrator` - Coordinate event creation with invitations
- `RSVPOrchestrator` - Handle full RSVP workflow

These should handle:
- Multi-entity transactions
- Complex business workflows
- Compensating transactions on failure
- Event publishing for async operations

### Step 10: Add Service Logging
Implement comprehensive logging in all services:
- Log method entry/exit for key operations
- Log business rule violations as warnings
- Log exceptions as errors with context
- Use structured logging with correlation IDs
- Include relevant entity IDs in log context

### Step 11: Create Service Tests
Location: `/SocialAnimal.Tests/Unit/Services/`

Create unit tests for each service:
- Test business rule validation
- Test error handling
- Test slug generation uniqueness
- Test transaction rollback scenarios
- Mock repository dependencies
- Verify logging calls

## Testing Checklist

- [ ] All service interfaces defined in Core layer
- [ ] All service implementations complete
- [ ] Business validation rules enforced
- [ ] Slug generation produces unique values
- [ ] Phone/email validation works correctly
- [ ] Soft delete operations work properly
- [ ] Cross-entity operations maintain consistency
- [ ] Transaction handling works correctly
- [ ] Error handling returns meaningful messages
- [ ] Logging provides adequate debugging info
- [ ] All services registered in DI container
- [ ] Unit tests cover all business rules
- [ ] Integration tests verify end-to-end flows

## Performance Considerations

1. **Batch Operations**: Implement bulk operations efficiently
2. **Caching**: Consider caching frequently accessed data
3. **Async/Await**: Use async operations throughout
4. **Query Optimization**: Minimize database round trips
5. **Validation Caching**: Cache validation results where appropriate

## Security Considerations

1. **Input Validation**: Validate all input data
2. **Authorization**: Add auth checks where needed (future)
3. **Data Sanitization**: Sanitize user input for display
4. **Audit Logging**: Log sensitive operations
5. **Rate Limiting**: Consider rate limits for expensive operations

## Dependencies

This task depends on:
- Task 7: Repository layer must be complete
- Tasks 1-6: All entities and database setup

This task must be completed before:
- Task 15: Admin controllers need services
- Any API or UI implementation

## Notes

- Follow existing service patterns in the codebase
- Use dependency injection for all dependencies
- Keep services focused on single responsibility
- Avoid business logic in repositories or controllers
- Consider using MediatR pattern if already in use
- Document complex business rules in code comments
- Use consistent error messages and codes
- Consider implementing service interfaces for testability
- Add telemetry/metrics collection points for monitoring