using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Context;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
        
        // Use environment variable or default connection string for migrations
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
            ?? "Host=localhost;Database=socialanimal_dev;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.UseNodaTime();
        });
        
        return new ApplicationContext(optionsBuilder.Options, SystemClock.Instance);
    }
}