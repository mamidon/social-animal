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