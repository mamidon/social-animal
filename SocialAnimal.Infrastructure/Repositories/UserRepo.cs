using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class UserRepo : CrudRepo, IUserRepo
{
    private readonly ApplicationContext _context;
    
    public UserRepo(ApplicationContext context, IClock clock) : base(context, clock)
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