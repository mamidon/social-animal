using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Core.Services;

public interface IInvitationService
{
    /// <summary>
    /// Creates a new invitation with validation
    /// </summary>
    Task<InvitationRecord> CreateInvitationAsync(InvitationStub stub);
    
    /// <summary>
    /// Creates multiple invitations for an event
    /// </summary>
    Task<List<InvitationRecord>> CreateBulkInvitationsAsync(long eventId, List<InvitationStub> stubs);
    
    /// <summary>
    /// Updates an existing invitation
    /// </summary>
    Task<InvitationRecord> UpdateInvitationAsync(long invitationId, InvitationStub stub);
    
    /// <summary>
    /// Soft deletes an invitation (only if no existing reservation)
    /// </summary>
    Task DeleteInvitationAsync(long invitationId);
    
    /// <summary>
    /// Generates a unique invitation slug within an event
    /// </summary>
    Task<string> GenerateInvitationSlugAsync(string name, long eventId);
    
    /// <summary>
    /// Gets invitation by slug
    /// </summary>
    Task<InvitationRecord?> GetInvitationBySlugAsync(string slug);
    
    /// <summary>
    /// Gets all invitations for an event with pagination
    /// </summary>
    Task<List<InvitationRecord>> GetEventInvitationsAsync(long eventId, int skip = 0, int take = 20);
    
    /// <summary>
    /// Gets invitation with its reservation data
    /// </summary>
    Task<InvitationRecord?> GetInvitationWithReservationAsync(string slug);
    
    /// <summary>
    /// Marks invitation as sent
    /// </summary>
    Task MarkInvitationSentAsync(long invitationId);
    
    /// <summary>
    /// Handles resending invitation logic
    /// </summary>
    Task ResendInvitationAsync(long invitationId);
    
    /// <summary>
    /// Validates invitation contact information (email or phone required)
    /// </summary>
    Task<bool> ValidateContactInfoAsync(InvitationStub stub);
}