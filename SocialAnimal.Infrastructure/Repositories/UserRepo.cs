using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class UserRepo : IUserRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public UserRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Users = new CrudQueries<ApplicationContext, User, UserRecord>(
            unitOfWork, c => c.Users);
    }
    
    public ICrudQueries<UserRecord> Users { get; }
    
    public async Task<UserRecord?> GetBySlugAsync(string slug)
    {
        using var context = _unitOfWork();
        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Slug == slug);
        return user?.Into();
    }
    
    public async Task<UserRecord?> GetByPhoneAsync(string phone)
    {
        using var context = _unitOfWork();
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone);
        return user?.Into();
    }
    
    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var context = _unitOfWork();
        return await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Slug == slug);
    }
    
    public async Task<IEnumerable<UserRecord>> GetActiveUsersAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var users = await context.Users
            .OrderByDescending(u => u.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return users.Select(u => u.Into());
    }
    
    public async Task<IEnumerable<UserRecord>> GetDeletedUsersAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var users = await context.Users
            .IgnoreQueryFilters()
            .Where(u => u.DeletedAt != null)
            .OrderByDescending(u => u.DeletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return users.Select(u => u.Into());
    }
}