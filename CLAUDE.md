# Codebase Patterns and Conventions

This document outlines the architectural patterns, naming conventions, and coding standards used in this .NET/C# codebase. These patterns should be followed when writing new code to maintain consistency and architectural integrity.

## Architectural Patterns

### Clean Architecture / Hexagonal Architecture
- **Core Domain Separation**: Business logic resides in a `Core/` layer, separated from infrastructure concerns
- **Infrastructure Layer**: Database access, external services, and I/O operations in dedicated layers (`Db/`, `CloudEnvironment/`)
- **Portals Pattern**: Use interfaces (typically in `Core/Portals/`) to define contracts between layers
- **Dependency Direction**: Dependencies point inward toward the core domain

### CQRS-like Pattern
- **Read/Write Separation**: Use `ICrudQueries<T>` for read operations and dedicated repositories for writes
- **Specialized Repositories**: Create domain-specific repositories (`IDashboardRepo`, `IAnalyticsRepo`) for complex queries
- **Generic CRUD**: Use generic `ICrudRepo` interface for basic entity operations

### Message-Driven Architecture
- **Publishers and Subscribers**: Use `IMessagePublisher` and `IMessageSubscriber<T>` interfaces
- **Async Processing**: Handle messages asynchronously with dedicated subscriber classes
- **Message Types**: Create strongly-typed message classes for different operations

### State Machine Pattern
- **Complex Workflows**: Use state machine pattern for multi-step business processes
- **State Interfaces**: Define `IState` or similar interfaces for state implementations
- **State Transitions**: Handle state transitions through dedicated service methods

## Naming Conventions

### Suffix-Based Classification
- **`*Record`**: Domain models/DTOs (UserRecord, OrderRecord, ProductRecord)
- **`*Service`**: Business logic services (UserService, OrderService, PaymentService)
- **`*Message`**: Message types for async processing (ProcessOrderMessage, SendNotificationMessage)
- **`*Repo`**: Repository interfaces (IUserRepo, IOrderRepo, ICrudRepo)
- **`*Controller`**: MVC controllers (UsersController, OrdersController)
- **`*Stub`**: Data transfer objects for creation/updates (UserStub, OrderStub)
- **`*Aggregate`**: Domain aggregates that combine multiple related entities
- **`*Context`**: Rich objects containing related data for business operations

### Identifier Patterns
- **Prefixed External IDs**: Use descriptive prefixes for external identifiers (`user_abc123`, `order_xyz789`)
- **Database IDs**: Use `long` type with descriptive names (`UserId`, `OrderId`, `CustomerId`)
- **Reference Fields**: Use `Reference` property for external system identifiers
- **Handle Fields**: Use `Handle` for user-friendly identifiers (URL slugs, usernames)

### Method and Property Naming
- **Boolean Properties**: Use `Is*`, `Has*`, `Can*` prefixes (`IsActive`, `HasPermission`, `CanEdit`)
- **Action Methods**: Use verb-noun pattern (`CreateUser`, `ProcessOrder`, `SendEmail`)
- **Query Methods**: Use `Fetch*`, `Find*`, `Get*` for different retrieval patterns
  - Fetch for network (or otherwise failable) queries
  - Find for in memory queries
  - Get for queries that'll only fail if we have an invalid ID, which should be rare
- **Async Methods**: Suffix with `Async` (`CreateUserAsync`, `ProcessOrderAsync`)

## Data Access Patterns

### Repository Pattern
- **Generic Repository**: Implement `ICrudQueries<T>` for basic CRUD operations
- **Specialized Repositories**: Create domain-specific repositories for complex queries
- **Unit of Work**: Use `Func<TContext>` pattern for database context management
- **Interface Segregation**: Separate read and write operations into different interfaces

### Entity-Record Mapping
- **Conversion Interfaces**: Implement `IInto<TRecord>` and `IFrom<TEntity, TRecord>`
- **Bidirectional Mapping**: Support conversion between database entities and domain records
- **Static Factory Methods**: Use `From()` static methods for entity creation
- **Instance Conversion**: Use `Into()` instance methods for record conversion

### Concurrency Control
- **Optimistic Concurrency**: Use `[Timestamp]` attributes for version control
- **DateTime Tokens**: Use `DateTime?` fields for concurrency tokens where needed
- **Unique Constraints**: Apply database constraints via Entity Framework attributes

## Service Organization

### Feature-Based Structure
- **Domain Grouping**: Organize code by business capabilities/features
- **Service Classes**: One service class per major business capability
- **Dependency Injection**: Register services using reflection-based discovery
- **Module Pattern**: Use static module classes for DI registration

### Service Registration Patterns
```csharp
// Convention-based service discovery
var types = assembly.GetTypes()
    .Where(t => t.Name.EndsWith("Service"))
    .Where(t => t.IsClass);

foreach (var type in types)
{
    services.AddScoped(type, type);
}

// Register by naming convention
if (portal.Name.EndsWith("Dispatcher"))
{
    services.AddSingleton(portal, implementation);
}
else
{
    services.AddScoped(portal, implementation);
}
```

## Domain Modeling

### Rich Domain Objects
- **Record Types**: Use `record` types for immutable data structures
- **Required Properties**: Use `required` keyword for mandatory fields
- **Value Objects**: Create domain-specific types for complex values
- **Domain Enums**: Use enums for controlled vocabularies

### Time and Date Handling
- **Precise Timestamps**: Use NodaTime `Instant` for UTC timestamps
- **Custom Converters**: Implement custom Entity Framework converters for time types
- **Consistent Patterns**: Use same time handling patterns throughout codebase

### Idempotency Patterns
- **Barrier Pattern**: Implement idempotency barriers for critical operations
- **Unique Keys**: Use meaningful idempotency keys with expiration
- **Duration-Based Expiry**: Set appropriate expiration times for idempotency records

## Configuration Management

### Environment-Based Configuration
- **Separate Files**: Use different configuration files per environment
- **Naming Pattern**: `Config.{environment}.json` and `Secrets.{environment}.json`
- **Runtime Detection**: Detect environment at runtime via environment variables
- **Type-Safe Configuration**: Use strongly-typed configuration classes

### Multi-Tenant Support (if applicable)
- **Tenant Scoping**: Scope operations by tenant identifier
- **URL Patterns**: Use tenant-aware routing patterns (`/tenant/{handle}/...`)
- **Data Isolation**: Ensure proper data isolation between tenants

## Code Quality Patterns

### Error Handling
- **Null Checks**: Perform explicit null checks where nullable types are used
- **Exception Types**: Use specific exception types for different error conditions
- **Logging**: Use structured logging with meaningful context

### Testing Patterns
- **Integration Tests**: Test full workflows with real database connections
- **Mock Objects**: Use mock implementations for external dependencies
- **Test Data**: Create reusable test data builders and scenarios

### Documentation
- **XML Comments**: Document public APIs with XML documentation comments
- **README Files**: Maintain README files for each major component
- **Domain Documentation**: Document complex business rules and processes

## Example Code Templates

### Service Class Template
```csharp
public class ExampleService
{
    private readonly ICrudRepo _crudRepo;
    private readonly IClock _clock;
    private readonly ILogger _logger;

    public ExampleService(ICrudRepo crudRepo, IClock clock, ILogger logger)
    {
        _crudRepo = crudRepo;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ExampleRecord> CreateExampleAsync(ExampleStub stub)
    {
        // Implementation
    }
}
```

### Repository Implementation Template
```csharp
public class ExampleRepo : IExampleRepo
{
    public ExampleRepo(Func<ApplicationContext> unitOfWork)
    {
        Examples = new CrudQueries<ApplicationContext, Example, ExampleRecord>(
            unitOfWork, c => c.Examples);
    }

    public ICrudQueries<ExampleRecord> Examples { get; }
}
```

### Domain Record Template
```csharp
public record ExampleRecord
{
    public required long ExampleId { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required Instant CreatedOn { get; init; }
    public Instant? UpdatedOn { get; init; }
}
```

These patterns ensure consistency, maintainability, and adherence to clean architecture principles throughout the codebase.