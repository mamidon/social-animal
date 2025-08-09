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
        
        // Use the FromRecord static method if available
        var fromRecordMethod = entityType.GetMethod("FromRecord", new[] { typeof(TRecord) });
        if (fromRecordMethod != null)
        {
            return fromRecordMethod.Invoke(null, new object[] { record })!;
        }
        
        throw new InvalidOperationException($"Entity type {entityType.Name} does not implement FromRecord<{typeof(TRecord).Name}>");
    }
    
    private object UpdateEntityFromRecord<TRecord>(TRecord record) where TRecord : class
    {
        // For updates, we create a new entity from the record
        // The context will handle the update tracking
        return CreateEntityFromRecord(record);
    }
}