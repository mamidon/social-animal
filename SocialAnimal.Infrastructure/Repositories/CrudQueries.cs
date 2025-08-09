using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class CrudQueries<TContext, TEntity, TRecord> : ICrudQueries<TRecord>
    where TContext : DbContext
    where TEntity : BaseEntity, IInto<TRecord>
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
        
        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Replace the record parameter with the entity parameter
            return node.Type == typeof(TRecord) ? _parameter : base.VisitParameter(node);
        }
    }
}