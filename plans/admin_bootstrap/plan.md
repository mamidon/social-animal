# Social Animal Admin Bootstrap - Implementation Plan

## Overview

This implementation plan details the steps to refactor the existing Social Animal event management system to align with new requirements while maintaining the established Clean Architecture patterns. The plan is divided into two phases:

1. **Phase 1**: Refactor existing database schema and domain models to match new requirements
2. **Phase 2**: Build a server-side rendered admin portal with HTMX for interactivity

## Prerequisites

- .NET 9 SDK installed
- PostgreSQL database server running
- Entity Framework Core CLI tools installed (`dotnet tool install --global dotnet-ef`)
- Existing project structure with Clean Architecture setup
- NodaTime for time handling already configured

## Technical Considerations

### Architecture Decisions
- Maintain Clean Architecture with Core/Infrastructure separation
- Keep CQRS-like patterns with `ICrudQueries<T>` and `ICrudRepo`
- Use Entity Framework Core with PostgreSQL and NodaTime
- Follow existing naming conventions (Record suffix for domain models, Repo for repositories)
- Implement server-side rendering with Razor Pages for admin portal
- Use HTMX for progressive enhancement without full page refreshes

### Key Patterns to Follow
- Domain models as `*Record` types in Core layer
- Entity classes in Infrastructure layer with `IInto<T>` and `IFrom<T,U>` mapping
- Repository pattern with generic CRUD operations
- Service classes for business logic
- Portal interfaces for infrastructure concerns
- Dependency injection with module-based registration

## Implementation Phases

### Phase 1: Database Schema Refactoring

#### Task 1: Update User Entity and Domain Model
- Modify existing `User` entity to match new requirements
- Add `Phone` field instead of `Email`
- Replace `Handle` with `Slug` naming
- Add soft delete support (`DeletedAt`)
- Update `UserRecord` domain model accordingly
- Update mapping implementations

#### Task 2: Refactor Event Entity
- Update `Event` entity to include address fields
- Replace `Handle` with `Slug`
- Add `AddressLine1`, `AddressLine2`, `City`, `State`, `Postal` fields
- Remove unnecessary fields (Description, Location, Organizer relationships)
- Add soft delete support
- Create `EventRecord` domain model with mapping

#### Task 3: Create Invitation Entity and Domain Model
- Create new `Invitation` entity class
- Implement `InvitationRecord` domain model
- Add foreign key relationship to Event
- Implement `IInto<T>` and `IFrom<T,U>` interfaces
- Configure Entity Framework mappings

#### Task 4: Create Reservation Entity and Domain Model
- Create new `Reservation` entity class
- Implement `ReservationRecord` domain model
- Add foreign key relationships to Invitation and User
- Implement party size logic (0 = regrets)
- Configure Entity Framework mappings

#### Task 5: Update Database Context and Configurations
- Update `ApplicationContext` with new DbSets
- Create Entity Framework configurations for new entities
- Update existing configurations for modified entities
- Configure snake_case naming and indexes
- Ensure soft delete query filters are in place

#### Task 6: Create and Apply Database Migrations
- Generate migration for schema changes
- Review migration for correctness
- Apply migration to development database
- Create rollback plan if needed

#### Task 7: Implement Repository Layer
- Create `IEventRepo` and `EventRepo` implementations
- Create `IInvitationRepo` and `InvitationRepo` implementations  
- Create `IReservationRepo` and `ReservationRepo` implementations
- Update `UserRepo` for new schema
- Ensure all repositories follow existing patterns

#### Task 8: Create Service Layer
- Implement `EventService` for event management
- Implement `InvitationService` for invitation logic
- Implement `ReservationService` for RSVP handling
- Update `UserService` if needed
- Add business validation and rules

### Phase 2: Admin Portal Implementation

#### Task 9: Setup MVC Infrastructure for Admin Portal
- Create new Area for Admin portal
- Configure routing for admin routes
- Setup Razor Pages structure
- Configure static file serving
- Add HTMX library and configuration

#### Task 10: Create Admin Layout and Navigation
- Create `_AdminLayout.cshtml` master layout
- Implement navigation menu component
- Setup CSS framework (Bootstrap or Tailwind)
- Configure HTMX default behaviors
- Create shared partial views

#### Task 11: Implement Admin Dashboard
- Create `/admin/index` page
- Display summary statistics for each entity type
- Add navigation links to entity list pages
- Implement responsive design
- Add basic styling

#### Task 12: Create Entity List Views
- Implement `/admin/entities/events` list page
- Implement `/admin/entities/users` list page
- Implement `/admin/entities/invitations` list page
- Implement `/admin/entities/reservations` list page
- Add pagination using HTMX
- Implement filter and search functionality
- Add sorting capabilities

#### Task 13: Create Entity Detail Views
- Implement `/admin/entities/events/{slug}` detail page
- Implement `/admin/entities/users/{slug}` detail page
- Implement `/admin/entities/invitations/{slug}` detail page
- Implement `/admin/entities/reservations/{id}` detail page
- Display all entity properties
- Add navigation links for related entities
- Implement HTMX-based inline editing

#### Task 14: Implement Entity Create/Edit Forms
- Create forms for new Event creation
- Create forms for new User creation
- Create forms for new Invitation creation
- Create forms for new Reservation creation
- Implement HTMX form submissions
- Add validation and error handling
- Implement success notifications

#### Task 15: Add Admin Portal Services and Controllers
- Create admin-specific controllers
- Implement view models and DTOs
- Add admin service layer if needed
- Configure dependency injection
- Implement error handling middleware

## Testing Strategy

### Phase 1 Testing
- Unit tests for domain model mappings
- Integration tests for repository operations
- Database migration testing
- Verify data integrity after schema changes

### Phase 2 Testing
- UI component testing with Playwright or Selenium
- HTMX interaction testing
- Form validation testing
- Navigation and routing tests
- Performance testing for list views with large datasets

## Risk Mitigation

### Potential Risks and Contingencies

1. **Data Migration Complexity**
   - Risk: Existing data may not map cleanly to new schema
   - Mitigation: Create data migration scripts, backup existing data
   - Contingency: Implement gradual migration with compatibility layer

2. **Breaking Changes to Existing API**
   - Risk: Schema changes may break existing API consumers
   - Mitigation: Version the API, maintain backward compatibility
   - Contingency: Create adapter layer for legacy API support

3. **HTMX Learning Curve**
   - Risk: Team unfamiliar with HTMX patterns
   - Mitigation: Create example implementations, document patterns
   - Contingency: Fall back to traditional form submissions if needed

4. **Performance Issues with Large Datasets**
   - Risk: Admin portal may be slow with many records
   - Mitigation: Implement efficient pagination, add database indexes
   - Contingency: Add caching layer, optimize queries

5. **Soft Delete Complexity**
   - Risk: Soft deletes may complicate queries and relationships
   - Mitigation: Use global query filters, test thoroughly
   - Contingency: Implement hard delete option for admins

## Success Criteria

### Phase 1 Success Metrics
- All entities match specified schema requirements
- Database migrations apply cleanly
- All existing tests pass after refactoring
- Repository operations work correctly
- Soft delete functionality works as expected

### Phase 2 Success Metrics
- Admin portal accessible at `/admin` routes
- All CRUD operations functional for each entity
- HTMX interactions work without full page refreshes
- Pagination and filtering work correctly
- Related entity navigation works seamlessly
- Portal is responsive and performant

## Estimated Timeline

### Phase 1: 3-4 days
- Tasks 1-4: 1 day (Entity and domain model updates)
- Tasks 5-6: 1 day (Database context and migrations)
- Tasks 7-8: 1-2 days (Repository and service implementation)

### Phase 2: 4-5 days
- Tasks 9-11: 1 day (MVC setup and dashboard)
- Tasks 12-13: 2 days (List and detail views)
- Tasks 14-15: 1-2 days (Forms and admin services)

**Total Estimated Time: 7-9 days**

## Next Steps

1. Review and approve this implementation plan
2. Create individual task files with detailed implementation guidance
3. Set up development environment and branch
4. Begin Phase 1 implementation
5. Conduct phase review before proceeding to Phase 2