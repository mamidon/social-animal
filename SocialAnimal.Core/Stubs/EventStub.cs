using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record EventStub
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; init; }
    
    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }
    
    [Required]
    [MaxLength(200)]
    public required string AddressLine1 { get; init; }
    
    [MaxLength(200)]
    public string? AddressLine2 { get; init; }
    
    [Required]
    [MaxLength(100)]
    public required string City { get; init; }
    
    [Required]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "State must be a two-letter code")]
    public required string State { get; init; }
    
    [Required]
    [MaxLength(20)]
    public required string Postal { get; init; }
}