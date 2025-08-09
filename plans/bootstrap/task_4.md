Implement base repository interfaces and CQRS patterns

This task creates the foundational repository interfaces in the Core project following the CQRS-like pattern and repository abstractions defined in CLAUDE.md. These interfaces establish the contract for data access operations while maintaining separation between read and write operations.

## Work to be Done

### Generic CRUD Repository Interface
Create the base `ICrudRepo` interface in `SocialAnimal.Core/Repositories/ICrudRepo.cs` that provides generic CRUD operations for all entities:

```csharp
using NodaTime;

namespace SocialAnimal.Core.Repositories;

public interface ICrudRepo
{
    Task<TRecord> CreateAsync<TRecord>(TRecord record) where TRecord : class;
    Task<TRecord?> GetByIdAsync<TRecord>(long id) where TRecord : class;
    Task<TRecord> UpdateAsync<TRecord>(TRecord record) where TRecord : class;
    Task DeleteAsync<TRecord>(long id) where TRecord : class;
    Task<bool> ExistsAsync<TRecord>(long id) where TRecord : class;
}
```

### Read Operations Interface
Create `ICrudQueries<T>` interface in `SocialAnimal.Core/Repositories/ICrudQueries.cs` for read-only query operations following the CQRS pattern:

```csharp
using System.Linq.Expressions;

namespace SocialAnimal.Core.Repositories;

public interface ICrudQueries<TRecord> where TRecord : class
{
    Task<TRecord?> FindByIdAsync(long id);
    Task<TRecord?> FindAsync(Expression<Func<TRecord, bool>> predicate);
    Task<IEnumerable<TRecord>> FetchAllAsync();
    Task<IEnumerable<TRecord>> FetchAsync(Expression<Func<TRecord, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TRecord, bool>>? predicate = null);
    Task<bool> AnyAsync(Expression<Func<TRecord, bool>> predicate);
}
```

Note the naming conventions from CLAUDE.md:
- `Fetch*` for network/failable queries
- `Find*` for in-memory queries
- `Get*` for queries that only fail with invalid IDs

### Entity-Record Mapping Interfaces
Create conversion interfaces in `SocialAnimal.Core/Repositories/IMapping.cs` for bidirectional mapping between entities and records:

```csharp
namespace SocialAnimal.Core.Repositories;

public interface IInto<out TRecord> where TRecord : class
{
    TRecord Into();
}

public interface IFrom<in TEntity, out TRecord> 
    where TEntity : class 
    where TRecord : class
{
    static abstract TRecord From(TEntity entity);
}
```

These interfaces follow the Entity-Record Mapping pattern from CLAUDE.md, supporting conversion between database entities and domain records.

### Base Domain Record
Create a base record type in `SocialAnimal.Core/Domain/BaseRecord.cs` that all domain models will inherit:

```csharp
using NodaTime;

namespace SocialAnimal.Core.Domain;

public abstract record BaseRecord
{
    public required long Id { get; init; }
    public required Instant CreatedOn { get; init; }
    public Instant? UpdatedOn { get; init; }
    public DateTime? ConcurrencyToken { get; init; }
}
```

This follows the patterns for:
- Using `long` type for database IDs
- NodaTime `Instant` for timestamps
- `DateTime?` for concurrency tokens
- Required properties for mandatory fields

### Unit of Work Pattern Interface
Create `IUnitOfWork` interface in `SocialAnimal.Core/Repositories/IUnitOfWork.cs` for transaction management:

```csharp
namespace SocialAnimal.Core.Repositories;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### Specialized Repository Example
Create an example domain-specific repository interface in `SocialAnimal.Core/Repositories/IUserRepo.cs` to demonstrate the pattern:

```csharp
namespace SocialAnimal.Core.Repositories;

public interface IUserRepo : ICrudRepo
{
    ICrudQueries<UserRecord> Users { get; }
    Task<UserRecord?> FindByEmailAsync(string email);
    Task<UserRecord?> FindByHandleAsync(string handle);
    Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null);
    Task<bool> IsHandleUniqueAsync(string handle, long? excludeUserId = null);
}
```

This demonstrates:
- Inheritance from `ICrudRepo` for write operations
- Property exposing `ICrudQueries<T>` for read operations
- Domain-specific query methods
- Support for the `Handle` field pattern from CLAUDE.md

## Relevant Patterns from CLAUDE.md

- **CQRS-like Pattern**: Read/Write separation with `ICrudQueries<T>` for reads and dedicated repositories for writes
- **Repository Pattern**: Generic repository with `ICrudRepo` interface for basic entity operations
- **Specialized Repositories**: Domain-specific repositories for complex queries
- **Entity-Record Mapping**: Bidirectional conversion interfaces `IInto<T>` and `IFrom<T>`
- **Unit of Work**: Database context management pattern
- **Naming Conventions**: Query methods use `Fetch*`, `Find*`, `Get*` for different retrieval patterns

## Deliverables

1. `ICrudRepo.cs` - Generic repository interface for CRUD operations
2. `ICrudQueries.cs` - Read-only query interface following CQRS pattern
3. `IMapping.cs` - Entity-Record conversion interfaces
4. `BaseRecord.cs` - Base domain record with common properties
5. `IUnitOfWork.cs` - Transaction management interface
6. `IUserRepo.cs` - Example specialized repository interface

## Acceptance Criteria

- All interfaces compile without errors
- Interfaces follow the naming conventions from CLAUDE.md
- Clear separation between read and write operations (CQRS pattern)
- Support for generic CRUD operations on any entity type
- Mapping interfaces enable bidirectional conversion
- Base record includes all common properties (Id, timestamps, concurrency)
- Specialized repository example demonstrates domain-specific patterns
- All types use appropriate nullable reference type annotations