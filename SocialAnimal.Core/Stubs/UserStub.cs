using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record UserStub
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; init; }
    
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }
    
    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }
    
    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone must be in E164 format")]
    public required string Phone { get; init; }
}