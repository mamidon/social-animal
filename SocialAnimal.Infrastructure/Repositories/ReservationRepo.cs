using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class ReservationRepo : IReservationRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public ReservationRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Reservations = new CrudQueries<ApplicationContext, Reservation, ReservationRecord>(
            unitOfWork, c => c.Reservations);
    }
    
    public ICrudQueries<ReservationRecord> Reservations { get; }
    
    public async Task<ReservationRecord?> GetByInvitationAndUserAsync(long invitationId, long userId)
    {
        using var context = _unitOfWork();
        var reservation = await context.Reservations
            .Include(r => r.Invitation)
                .ThenInclude(i => i.Event)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.InvitationId == invitationId && r.UserId == userId);
        return reservation?.Into();
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetByInvitationIdAsync(long invitationId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.User)
            .Where(r => r.InvitationId == invitationId)
            .OrderBy(r => r.User.LastName)
            .ThenBy(r => r.User.FirstName)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetByUserIdAsync(long userId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.Invitation)
                .ThenInclude(i => i.Event)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedOn)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetAcceptedReservationsAsync(long invitationId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.User)
            .Where(r => r.InvitationId == invitationId && r.PartySize > 0)
            .OrderBy(r => r.User.LastName)
            .ThenBy(r => r.User.FirstName)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<IEnumerable<ReservationRecord>> GetDeclinedReservationsAsync(long invitationId)
    {
        using var context = _unitOfWork();
        var reservations = await context.Reservations
            .Include(r => r.User)
            .Where(r => r.InvitationId == invitationId && r.PartySize == 0)
            .OrderBy(r => r.User.LastName)
            .ThenBy(r => r.User.FirstName)
            .ToListAsync();
        return reservations.Select(r => r.Into());
    }
    
    public async Task<int> GetTotalPartySizeForInvitationAsync(long invitationId)
    {
        using var context = _unitOfWork();
        return await context.Reservations
            .Where(r => r.InvitationId == invitationId && r.PartySize > 0)
            .SumAsync(r => (int)r.PartySize);
    }
    
    public async Task<int> GetAcceptedCountForInvitationAsync(long invitationId)
    {
        using var context = _unitOfWork();
        return await context.Reservations
            .CountAsync(r => r.InvitationId == invitationId && r.PartySize > 0);
    }
    
    public async Task<int> GetDeclinedCountForInvitationAsync(long invitationId)
    {
        using var context = _unitOfWork();
        return await context.Reservations
            .CountAsync(r => r.InvitationId == invitationId && r.PartySize == 0);
    }
}