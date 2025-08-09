using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Key), IsUnique = true)]
[Index(nameof(ExpiresOn))]
public class IdempotencyBarrier : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public required string Key { get; set; }
    
    [MaxLength(100)]
    public required string Operation { get; set; }
    
    public required Instant ExpiresOn { get; set; }
    
    public string? Result { get; set; } // JSON serialized result
}