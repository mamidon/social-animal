# Task 6: Create and Apply Database Migrations

## Objective
Generate Entity Framework Core migrations for the updated schema, review them for correctness, and apply them to the development database.

## Prerequisites
- All entity classes and configurations completed (Tasks 1-5)
- PostgreSQL database server running
- Entity Framework Core CLI tools installed
- Connection string configured in appsettings

## Implementation Steps

### Step 1: Clean Up Existing Migrations (if any)
If there are existing migrations that conflict with the new schema:

```bash
# List existing migrations
dotnet ef migrations list --project SocialAnimal.Infrastructure --startup-project SocialAnimal.Web

# Remove last migration if not applied
dotnet ef migrations remove --project SocialAnimal.Infrastructure --startup-project SocialAnimal.Web

# Or reset completely (CAUTION: This will delete all data)
dotnet ef database drop --project SocialAnimal.Infrastructure --startup-project SocialAnimal.Web
```

### Step 2: Generate Initial Migration

```bash
# Generate the migration
dotnet ef migrations add RefactorToEventManagementSchema \
  --project SocialAnimal.Infrastructure \
  --startup-project SocialAnimal.Web \
  --context ApplicationContext \
  --output-dir Db/Migrations
```

### Step 3: Review Generated Migration
The migration should create/modify the following:

Expected Up Migration Structure:
```csharp
public partial class RefactorToEventManagementSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop existing constraints and tables if needed
        migrationBuilder.DropForeignKey(
            name: "fk_events_users_organizer_id",
            table: "events");
            
        migrationBuilder.DropTable(
            name: "event_attendances");
        
        // Modify Users table
        migrationBuilder.DropIndex(
            name: "ix_users_email",
            table: "users");
            
        migrationBuilder.DropColumn(
            name: "email",
            table: "users");
            
        migrationBuilder.DropColumn(
            name: "handle",
            table: "users");
            
        migrationBuilder.DropColumn(
            name: "reference",
            table: "users");
            
        migrationBuilder.DropColumn(
            name: "password_hash",
            table: "users");
            
        migrationBuilder.DropColumn(
            name: "is_email_verified",
            table: "users");
            
        migrationBuilder.AddColumn<string>(
            name: "slug",
            table: "users",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false);
            
        migrationBuilder.AddColumn<string>(
            name: "phone",
            table: "users",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false);
            
        migrationBuilder.AddColumn<Instant>(
            name: "deleted_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);
            
        migrationBuilder.CreateIndex(
            name: "ix_users_slug",
            table: "users",
            column: "slug",
            unique: true);
            
        migrationBuilder.CreateIndex(
            name: "ix_users_phone",
            table: "users",
            column: "phone");
            
        migrationBuilder.CreateIndex(
            name: "ix_users_deleted_at",
            table: "users",
            column: "deleted_at");
        
        // Modify Events table
        migrationBuilder.DropColumn(
            name: "handle",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "reference",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "description",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "starts_on",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "ends_on",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "location",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "max_attendees",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "is_public",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "is_cancelled",
            table: "events");
            
        migrationBuilder.DropColumn(
            name: "organizer_id",
            table: "events");
            
        migrationBuilder.AddColumn<string>(
            name: "slug",
            table: "events",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false);
            
        migrationBuilder.AddColumn<string>(
            name: "address_line1",
            table: "events",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false);
            
        migrationBuilder.AddColumn<string>(
            name: "address_line2",
            table: "events",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
            
        migrationBuilder.AddColumn<string>(
            name: "city",
            table: "events",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false);
            
        migrationBuilder.AddColumn<string>(
            name: "state",
            table: "events",
            type: "character(2)",
            fixedLength: true,
            maxLength: 2,
            nullable: false);
            
        migrationBuilder.AddColumn<string>(
            name: "postal",
            table: "events",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false);
            
        migrationBuilder.AddColumn<Instant>(
            name: "deleted_at",
            table: "events",
            type: "timestamp with time zone",
            nullable: true);
            
        migrationBuilder.CreateIndex(
            name: "ix_events_slug",
            table: "events",
            column: "slug",
            unique: true);
            
        migrationBuilder.CreateIndex(
            name: "ix_events_deleted_at",
            table: "events",
            column: "deleted_at");
            
        migrationBuilder.CreateIndex(
            name: "ix_events_state",
            table: "events",
            column: "state");
            
        migrationBuilder.CreateIndex(
            name: "ix_events_city",
            table: "events",
            column: "city");
        
        // Create Invitations table
        migrationBuilder.CreateTable(
            name: "invitations",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", 
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                slug = table.Column<string>(type: "character varying(100)", 
                    maxLength: 100, nullable: false),
                event_id = table.Column<long>(type: "bigint", nullable: false),
                created_on = table.Column<Instant>(type: "timestamp with time zone", 
                    nullable: false),
                updated_on = table.Column<Instant>(type: "timestamp with time zone", 
                    nullable: true),
                deleted_at = table.Column<Instant>(type: "timestamp with time zone", 
                    nullable: true),
                concurrency_token = table.Column<DateTime>(type: "timestamp without time zone", 
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_invitations", x => x.id);
                table.ForeignKey(
                    name: "fk_invitations_events_event_id",
                    column: x => x.event_id,
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });
            
        migrationBuilder.CreateIndex(
            name: "ix_invitations_slug",
            table: "invitations",
            column: "slug",
            unique: true);
            
        migrationBuilder.CreateIndex(
            name: "ix_invitations_event_id",
            table: "invitations",
            column: "event_id");
            
        migrationBuilder.CreateIndex(
            name: "ix_invitations_deleted_at",
            table: "invitations",
            column: "deleted_at");
        
        // Create Reservations table
        migrationBuilder.CreateTable(
            name: "reservations",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", 
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                invitation_id = table.Column<long>(type: "bigint", nullable: false),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                party_size = table.Column<long>(type: "bigint", nullable: false),
                created_on = table.Column<Instant>(type: "timestamp with time zone", 
                    nullable: false),
                updated_on = table.Column<Instant>(type: "timestamp with time zone", 
                    nullable: true),
                concurrency_token = table.Column<DateTime>(type: "timestamp without time zone", 
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_reservations", x => x.id);
                table.ForeignKey(
                    name: "fk_reservations_invitations_invitation_id",
                    column: x => x.invitation_id,
                    principalTable: "invitations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_reservations_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });
            
        migrationBuilder.CreateIndex(
            name: "ix_reservations_invitation_user",
            table: "reservations",
            columns: new[] { "invitation_id", "user_id" },
            unique: true);
            
        migrationBuilder.CreateIndex(
            name: "ix_reservations_invitation_id",
            table: "reservations",
            column: "invitation_id");
            
        migrationBuilder.CreateIndex(
            name: "ix_reservations_user_id",
            table: "reservations",
            column: "user_id");
            
        migrationBuilder.CreateIndex(
            name: "ix_reservations_party_size",
            table: "reservations",
            column: "party_size");
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse all changes
    }
}
```

### Step 4: Create Data Migration Script (if existing data)
Location: `/SocialAnimal.Infrastructure/Db/Migrations/Scripts/MigrateExistingData.sql`

```sql
-- This script migrates existing data to new schema structure
-- Run this BEFORE applying the EF migration if you have existing data

BEGIN TRANSACTION;

-- Backup existing data
CREATE TABLE users_backup AS SELECT * FROM users;
CREATE TABLE events_backup AS SELECT * FROM events;

-- Generate slugs for existing users
UPDATE users 
SET slug = LOWER(REPLACE(handle, ' ', '-')) || '-' || id
WHERE handle IS NOT NULL;

-- Set default phone numbers (will need manual update)
UPDATE users 
SET phone = '+10000000000'
WHERE phone IS NULL;

-- Generate slugs for existing events
UPDATE events 
SET slug = LOWER(REPLACE(SUBSTRING(title, 1, 30), ' ', '-')) || '-' || id;

-- Set default address values for events
UPDATE events
SET 
    address_line1 = COALESCE(location, 'TBD'),
    city = 'TBD',
    state = 'CA',
    postal = '00000'
WHERE address_line1 IS NULL;

COMMIT;
```

### Step 5: Apply Migration to Database

```bash
# Check pending migrations
dotnet ef migrations list --project SocialAnimal.Infrastructure --startup-project SocialAnimal.Web

# Generate SQL script for review (optional)
dotnet ef migrations script \
  --project SocialAnimal.Infrastructure \
  --startup-project SocialAnimal.Web \
  --output migration.sql

# Apply migration
dotnet ef database update \
  --project SocialAnimal.Infrastructure \
  --startup-project SocialAnimal.Web \
  --context ApplicationContext
```

### Step 6: Verify Migration Success

Create verification script:
Location: `/SocialAnimal.Infrastructure/Db/Migrations/Scripts/VerifySchema.sql`

```sql
-- Verify Users table
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;

-- Verify Events table
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_name = 'events'
ORDER BY ordinal_position;

-- Verify Invitations table exists
SELECT COUNT(*) as invitation_table_exists
FROM information_schema.tables
WHERE table_name = 'invitations';

-- Verify Reservations table exists
SELECT COUNT(*) as reservation_table_exists
FROM information_schema.tables
WHERE table_name = 'reservations';

-- Check indexes
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
AND tablename IN ('users', 'events', 'invitations', 'reservations')
ORDER BY tablename, indexname;

-- Check foreign key constraints
SELECT
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
      AND tc.table_schema = kcu.table_schema
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
      AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY' 
AND tc.table_name IN ('invitations', 'reservations');
```

### Step 7: Create Rollback Plan

```bash
# Generate rollback script
dotnet ef migrations script \
  RefactorToEventManagementSchema \
  0 \
  --project SocialAnimal.Infrastructure \
  --startup-project SocialAnimal.Web \
  --output rollback.sql

# Or use EF to rollback
dotnet ef database update <PreviousMigrationName> \
  --project SocialAnimal.Infrastructure \
  --startup-project SocialAnimal.Web
```

## Common Issues and Solutions

### Issue 1: NodaTime Type Mapping Errors
**Solution**: Ensure NpgsqlDataSource is configured with UseNodaTime() before creating DbContext.

### Issue 2: Snake Case Naming Not Applied
**Solution**: Verify UseSnakeCaseNamingConvention() is called in OnConfiguring.

### Issue 3: Migration Fails Due to Existing Data
**Solution**: 
1. Create data migration script to handle existing data
2. Add default values for new non-nullable columns
3. Consider making migration in multiple steps

### Issue 4: Foreign Key Constraint Violations
**Solution**: 
1. Ensure proper order of table creation/modification
2. Use Restrict instead of Cascade where appropriate
3. Clean up orphaned records before migration

## Testing Checklist

- [ ] Migration generates without errors
- [ ] Migration SQL script is reviewed and correct
- [ ] Migration applies successfully to empty database
- [ ] Migration handles existing data correctly (if applicable)
- [ ] All tables are created with correct columns
- [ ] All indexes are created correctly
- [ ] All foreign key constraints are in place
- [ ] Soft delete filters work after migration
- [ ] Rollback script works correctly
- [ ] Application can connect and query after migration

## Post-Migration Verification

```csharp
// Test in application
using var context = serviceProvider.GetRequiredService<ApplicationContext>();

// Verify tables exist and are queryable
var userCount = await context.Users.CountAsync();
var eventCount = await context.Events.CountAsync();
var invitationCount = await context.Invitations.CountAsync();
var reservationCount = await context.Reservations.CountAsync();

// Test soft delete filters
var activeUsers = await context.Users.ToListAsync();
var allUsers = await context.Users.IgnoreQueryFilters().ToListAsync();

Console.WriteLine($"Migration successful: {userCount} users, {eventCount} events");
```

## Dependencies

This task depends on:
- Tasks 1-5 (All entity and context configurations)

This task must be completed before:
- Task 7 (Repositories need database schema)
- Task 8 (Services need working database)
- Phase 2 (Admin portal needs database)

## Notes

- Always backup database before applying migrations
- Review generated SQL before applying to production
- Consider using migration bundles for production deployments
- Test migrations on a copy of production data
- Document any manual data migration steps required
- Keep migration files in source control