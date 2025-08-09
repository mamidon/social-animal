namespace SocialAnimal.Web.Configuration.Modules;

public interface IServiceModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    int Order => 0; // Allows ordering of module registration
}