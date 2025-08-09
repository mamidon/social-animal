Implement repository pattern and entity-record mapping

This task creates the repository implementations in the Infrastructure project that implement the interfaces defined in Core, including the generic CRUD operations and entity-to-record mapping following the patterns in CLAUDE.md.

## Work to be Done

### Generic CRUD Queries Implementation
Create `CrudQueries.cs` in `SocialAnimal.Infrastructure/Repositories/CrudQueries.cs`:

```csharp
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class CrudQueries<TContext, TEntity, TRecord> : ICrudQueries<TRecord>
    where TContext : DbContext
    where TEntity : BaseEntity, IInto<TRecord>, new()
    where TRecord : class
{
    private readonly Func<TContext> _contextFactory;
    private readonly Func<TContext, DbSet<TEntity>> _dbSetSelector;
    
    public CrudQueries(
        Func<TContext> contextFactory,
        Func<TContext, DbSet<TEntity>> dbSetSelector)
    {
        _contextFactory = contextFactory;
        _dbSetSelector = dbSetSelector;
    }
    
    public async Task<TRecord?> FindByIdAsync(long id)
    {
        using var context = _contextFactory();
        var entity = await _dbSetSelector(context)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
        
        return entity?.Into();
    }
    
    public async Task<TRecord?> FindAsync(Expression<Func<TRecord, bool>> predicate)
    {
        using var context = _contextFactory();
        var entityExpression = TranslateExpression(predicate);
        
        var entity = await _dbSetSelector(context)
            .AsNoTracking()
            .FirstOrDefaultAsync(entityExpression);
        
        return entity?.Into();
    }
    
    public async Task<IEnumerable<TRecord>> FetchAllAsync()
    {
        using var context = _contextFactory();
        var entities = await _dbSetSelector(context)
            .AsNoTracking()
            .ToListAsync();
        
        return entities.Select(e => e.Into());
    }
    
    public async Task<IEnumerable<TRecord>> FetchAsync(Expression<Func<TRecord, bool>> predicate)
    {
        using var context = _contextFactory();
        var entityExpression = TranslateExpression(predicate);
        
        var entities = await _dbSetSelector(context)
            .AsNoTracking()
            .Where(entityExpression)
            .ToListAsync();
        
        return entities.Select(e => e.Into());
    }
    
    public async Task<int> CountAsync(Expression<Func<TRecord, bool>>? predicate = null)
    {
        using var context = _contextFactory();
        var query = _dbSetSelector(context).AsNoTracking();
        
        if (predicate != null)
        {
            var entityExpression = TranslateExpression(predicate);
            query = query.Where(entityExpression);
        }
        
        return await query.CountAsync();
    }
    
    public async Task<bool> AnyAsync(Expression<Func<TRecord, bool>> predicate)
    {
        using var context = _contextFactory();
        var entityExpression = TranslateExpression(predicate);
        
        return await _dbSetSelector(context)
            .AsNoTracking()
            .AnyAsync(entityExpression);
    }
    
    // Helper to translate Record expressions to Entity expressions
    private Expression<Func<TEntity, bool>> TranslateExpression(Expression<Func<TRecord, bool>> recordExpression)
    {
        // For now, this is a simplified implementation
        // In production, use a proper expression visitor to translate between types
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var body = new RecordToEntityExpressionVisitor(parameter).Visit(recordExpression.Body);
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }
    
    private class RecordToEntityExpressionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;
        
        public RecordToEntityExpressionVisitor(ParameterExpression parameter)
        {
            _parameter = parameter;
        }
        
        protected override Expression VisitMember(MemberExpression node)
        {
            // Map record properties to entity properties
            // This is simplified - implement full mapping logic as needed
            if (node.Member.DeclaringType == typeof(TRecord))
            {
                var entityProperty = typeof(TEntity).GetProperty(node.Member.Name);
                if (entityProperty != null)
                {
                    return Expression.MakeMemberAccess(_parameter, entityProperty);
                }
            }
            
            return base.VisitMember(node);
        }
    }
}
```

### Base CRUD Repository Implementation
Create `CrudRepo.cs` in `SocialAnimal.Infrastructure/Repositories/CrudRepo.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NodaTime;
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
    
    public async Task<TRecord> CreateAsync<TRecord>(TRecord record) where TRecord : class
    {
        var entityType = GetEntityTypeForRecord<TRecord>();
        var entity = CreateEntityFromRecord(record);
        
        _context.Add(entity);
        await _context.SaveChangesAsync();
        
        return ((IInto<TRecord>)entity).Into();
    }
    
    public async Task<TRecord?> GetByIdAsync<TRecord>(long id) where TRecord : class
    {
        var entityType = GetEntityTypeForRecord<TRecord>();
        var entity = await _context.FindAsync(entityType, id);
        
        return entity != null ? ((IInto<TRecord>)entity).Into() : null;
    }
    
    public async Task<TRecord> UpdateAsync<TRecord>(TRecord record) where TRecord : class
    {
        var entityType = GetEntityTypeForRecord<TRecord>();
        var entity = UpdateEntityFromRecord(record);
        
        _context.Update(entity);
        await _context.SaveChangesAsync();
        
        return ((IInto<TRecord>)entity).Into();
    }
    
    public async Task DeleteAsync<TRecord>(long id) where TRecord : class
    {
        var entityType = GetEntityTypeForRecord<TRecord>();
        var entity = await _context.FindAsync(entityType, id);
        
        if (entity != null)
        {
            _context.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<bool> ExistsAsync<TRecord>(long id) where TRecord : class
    {
        var entityType = GetEntityTypeForRecord<TRecord>();
        var entity = await _context.FindAsync(entityType, id);
        return entity != null;
    }
    
    private Type GetEntityTypeForRecord<TRecord>() where TRecord : class
    {
        // Map record types to entity types
        var recordType = typeof(TRecord);
        var recordName = recordType.Name.Replace("Record", "");
        
        var entityType = typeof(BaseEntity).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == recordName && t.IsSubclassOf(typeof(BaseEntity)));
        
        if (entityType == null)
        {
            throw new InvalidOperationException($"No entity type found for record type {recordType.Name}");
        }
        
        return entityType;
    }
    
    private object CreateEntityFromRecord<TRecord>(TRecord record) where TRecord : class
    {
        var entityType = GetEntityTypeForRecord<TRecord>();
        
        // Use the IFrom interface if available
        var fromMethod = entityType.GetMethod("From", new[] { typeof(TRecord) });
        if (fromMethod != null)
        {
            return fromMethod.Invoke(null, new object[] { record })!;
        }
        
        throw new InvalidOperationException($"Entity type {entityType.Name} does not implement IFrom<{typeof(TRecord).Name}>");
    }
    
    private object UpdateEntityFromRecord<TRecord>(TRecord record) where TRecord : class
    {
        // Similar to CreateEntityFromRecord but for updates
        return CreateEntityFromRecord(record);
    }
}
```

### User Entity with Mapping
Update `User.cs` in `SocialAnimal.Infrastructure/Db/Entities/User.cs` to implement mapping:

```csharp
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

// ... existing User class definition ...

public partial class User : IInto<UserRecord>, IFrom<User, UserRecord>
{
    public UserRecord Into()
    {
        return new UserRecord
        {
            Id = Id,
            Handle = Handle,
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            Reference = Reference,
            IsActive = IsActive,
            IsEmailVerified = IsEmailVerified,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken
        };
    }
    
    public static UserRecord From(User entity)
    {
        return entity.Into();
    }
    
    public static User FromRecord(UserRecord record)
    {
        return new User
        {
            Id = record.Id,
            Handle = record.Handle,
            Email = record.Email,
            FirstName = record.FirstName,
            LastName = record.LastName,
            Reference = record.Reference,
            IsActive = record.IsActive,
            IsEmailVerified = record.IsEmailVerified,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}
```

### User Record Domain Model
Create `UserRecord.cs` in `SocialAnimal.Core/Domain/UserRecord.cs`:

```csharp
using NodaTime;

namespace SocialAnimal.Core.Domain;

public record UserRecord : BaseRecord
{
    public required string Handle { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Reference { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsEmailVerified { get; init; }
    
    public string FullName => $"{FirstName} {LastName}";
}
```

### User Repository Implementation
Create `UserRepo.cs` in `SocialAnimal.Infrastructure/Repositories/UserRepo.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class UserRepo : CrudRepo, IUserRepo
{
    private readonly ApplicationContext _context;
    
    public UserRepo(ApplicationContext context, NodaTime.IClock clock) : base(context, clock)
    {
        _context = context;
        Users = new CrudQueries<ApplicationContext, User, UserRecord>(
            () => _context,
            ctx => ctx.Users
        );
    }
    
    public ICrudQueries<UserRecord> Users { get; }
    
    public async Task<UserRecord?> FindByEmailAsync(string email)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
        
        return user?.Into();
    }
    
    public async Task<UserRecord?> FindByHandleAsync(string handle)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Handle == handle);
        
        return user?.Into();
    }
    
    public async Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Email == email);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return !await query.AnyAsync();
    }
    
    public async Task<bool> IsHandleUniqueAsync(string handle, long? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Handle == handle);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return !await query.AnyAsync();
    }
}
```

### Unit of Work Implementation
Create `UnitOfWork.cs` in `SocialAnimal.Infrastructure/Repositories/UnitOfWork.cs`:

```csharp
using Microsoft.EntityFrameworkCore.Storage;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;

namespace SocialAnimal.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationContext _context;
    private IDbContextTransaction? _transaction;
    
    public UnitOfWork(ApplicationContext context)
    {
        _context = context;
    }
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }
    
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

## Relevant Patterns from CLAUDE.md

- **Repository Pattern**: Generic repository with ICrudQueries<T> for reads, ICrudRepo for writes
- **Entity-Record Mapping**: IInto<TRecord> and IFrom<TEntity, TRecord> for bidirectional conversion
- **Unit of Work**: Func<TContext> pattern for database context management
- **CQRS-like Pattern**: Read/Write separation with specialized query methods
- **Method Naming**: Fetch* for network queries, Find* for in-memory, Get* for ID lookups
- **Specialized Repositories**: Domain-specific repositories extending base functionality

## Deliverables

1. `CrudQueries.cs` - Generic read operations implementation
2. `CrudRepo.cs` - Generic CRUD operations implementation
3. Updated `User.cs` entity with IInto/IFrom implementations
4. `UserRecord.cs` - User domain record model
5. `UserRepo.cs` - User-specific repository with custom queries
6. `UnitOfWork.cs` - Transaction management implementation

## Acceptance Criteria

- All repository implementations compile without errors
- Entity-to-record mapping works bidirectionally
- Generic CRUD operations support any entity/record pair
- Specialized repository methods follow naming conventions
- Unit of Work properly manages database transactions
- Expression translation handles basic property mapping
- Repository implementations use proper async/await patterns
- No tracking is used for read operations (performance optimization)
- Concurrency tokens are properly handled in updates