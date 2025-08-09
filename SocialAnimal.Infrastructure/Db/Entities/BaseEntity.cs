using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

public abstract class BaseEntity
{
    [Key]
    public long Id { get; set; }
    
    public required Instant CreatedOn { get; set; }
    
    public Instant? UpdatedOn { get; set; }
    
    [Timestamp]
    public DateTime? ConcurrencyToken { get; set; }
}