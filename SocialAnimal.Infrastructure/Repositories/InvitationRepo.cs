using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class InvitationRepo : IInvitationRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public InvitationRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Invitations = new CrudQueries<ApplicationContext, Invitation, InvitationRecord>(
            unitOfWork, c => c.Invitations);
    }
    
    public ICrudQueries<InvitationRecord> Invitations { get; }
    
    public async Task<InvitationRecord?> GetBySlugAsync(string slug)
    {
        using var context = _unitOfWork();
        var invitation = await context.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Slug == slug);
        return invitation?.Into();
    }
    
    public async Task<InvitationRecord?> GetBySlugWithDetailsAsync(string slug)
    {
        using var context = _unitOfWork();
        var invitation = await context.Invitations
            .IgnoreQueryFilters()
            .Include(i => i.Event)
            .Include(i => i.Reservations)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(i => i.Slug == slug);
        return invitation?.Into();
    }
    
    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var context = _unitOfWork();
        return await context.Invitations
            .IgnoreQueryFilters()
            .AnyAsync(i => i.Slug == slug);
    }
    
    public async Task<IEnumerable<InvitationRecord>> GetByEventIdAsync(long eventId, int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var invitations = await context.Invitations
            .Where(i => i.EventId == eventId)
            .OrderByDescending(i => i.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return invitations.Select(i => i.Into());
    }
    
    public async Task<IEnumerable<InvitationRecord>> GetActiveInvitationsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var invitations = await context.Invitations
            .Include(i => i.Event)
            .OrderByDescending(i => i.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return invitations.Select(i => i.Into());
    }
    
    public async Task<IEnumerable<InvitationRecord>> GetDeletedInvitationsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var invitations = await context.Invitations
            .IgnoreQueryFilters()
            .Include(i => i.Event)
            .Where(i => i.DeletedAt != null)
            .OrderByDescending(i => i.DeletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return invitations.Select(i => i.Into());
    }
    
    public async Task<int> GetInvitationCountForEventAsync(long eventId)
    {
        using var context = _unitOfWork();
        return await context.Invitations
            .CountAsync(i => i.EventId == eventId);
    }
}