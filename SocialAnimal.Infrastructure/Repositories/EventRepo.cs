using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class EventRepo : IEventRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public EventRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Events = new CrudQueries<ApplicationContext, Event, EventRecord>(
            unitOfWork, c => c.Events);
    }
    
    public ICrudQueries<EventRecord> Events { get; }
    
    public async Task<EventRecord?> GetBySlugAsync(string slug)
    {
        using var context = _unitOfWork();
        var evt = await context.Events
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Slug == slug);
        return evt?.Into();
    }
    
    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var context = _unitOfWork();
        return await context.Events
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Slug == slug);
    }
    
    public async Task<IEnumerable<EventRecord>> GetActiveEventsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .OrderByDescending(e => e.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
    
    public async Task<IEnumerable<EventRecord>> GetDeletedEventsAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .IgnoreQueryFilters()
            .Where(e => e.DeletedAt != null)
            .OrderByDescending(e => e.DeletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
    
    public async Task<IEnumerable<EventRecord>> GetEventsByStateAsync(string state, int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .Where(e => e.State == state.ToUpper())
            .OrderByDescending(e => e.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
    
    public async Task<IEnumerable<EventRecord>> GetEventsByCityAsync(string city, string state, int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var events = await context.Events
            .Where(e => e.City.ToLower() == city.ToLower() && e.State == state.ToUpper())
            .OrderByDescending(e => e.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return events.Select(e => e.Into());
    }
}