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