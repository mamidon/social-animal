using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IEventRepo
{
    ICrudQueries<EventRecord> Events { get; }
    Task<EventRecord?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug);
    Task<IEnumerable<EventRecord>> GetActiveEventsAsync(int skip = 0, int take = 20);
    Task<IEnumerable<EventRecord>> GetDeletedEventsAsync(int skip = 0, int take = 20);
    Task<IEnumerable<EventRecord>> GetEventsByStateAsync(string state, int skip = 0, int take = 20);
    Task<IEnumerable<EventRecord>> GetEventsByCityAsync(string city, string state, int skip = 0, int take = 20);
}