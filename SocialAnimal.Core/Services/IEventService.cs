using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Core.Services;

public interface IEventService
{
    /// <summary>
    /// Creates a new event with unique slug generation and address validation
    /// </summary>
    Task<EventRecord> CreateEventAsync(EventStub stub);
    
    /// <summary>
    /// Updates an existing event with validation
    /// </summary>
    Task<EventRecord> UpdateEventAsync(long eventId, EventStub stub);
    
    /// <summary>
    /// Soft deletes an event (only if no existing reservations)
    /// </summary>
    Task DeleteEventAsync(long eventId);
    
    /// <summary>
    /// Restores a soft-deleted event
    /// </summary>
    Task RestoreEventAsync(long eventId);
    
    /// <summary>
    /// Generates a unique slug from the event title
    /// </summary>
    Task<string> GenerateUniqueSlugAsync(string title);
    
    /// <summary>
    /// Gets an event with its invitations by slug
    /// </summary>
    Task<EventRecord?> GetEventWithInvitationsAsync(string slug);
    
    /// <summary>
    /// Gets upcoming events with pagination
    /// </summary>
    Task<List<EventRecord>> GetUpcomingEventsAsync(int skip = 0, int take = 20);
    
    /// <summary>
    /// Gets past events with pagination
    /// </summary>
    Task<List<EventRecord>> GetPastEventsAsync(int skip = 0, int take = 20);
    
    /// <summary>
    /// Gets event by slug
    /// </summary>
    Task<EventRecord?> GetEventBySlugAsync(string slug);
    
    /// <summary>
    /// Validates that the address information is complete and properly formatted
    /// </summary>
    Task<bool> ValidateAddressAsync(EventStub stub);
}