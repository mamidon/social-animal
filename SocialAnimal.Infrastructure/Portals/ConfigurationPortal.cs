using Microsoft.Extensions.Configuration;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Portals;

public class ConfigurationPortal : IConfigurationPortal
{
    private readonly IConfiguration _configuration;
    private readonly string _environmentName;
    
    public ConfigurationPortal(IConfiguration configuration, string environmentName)
    {
        _configuration = configuration;
        _environmentName = environmentName;
    }
    
    public string EnvironmentName => _environmentName;
    
    public T GetConfiguration<T>(string sectionName) where T : class, new()
    {
        var config = new T();
        var section = _configuration.GetSection(sectionName);
        
        // For now, we'll return a new instance and let the calling code handle binding
        // In a production implementation, you would add Microsoft.Extensions.Configuration.Binder
        // and use section.Bind(config) or use section.Get<T>()
        return config;
    }
    
    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name) 
            ?? throw new InvalidOperationException($"Connection string '{name}' not found");
    }
    
    public string GetValue(string key)
    {
        return _configuration[key] 
            ?? throw new InvalidOperationException($"Configuration key '{key}' not found");
    }
    
    public bool GetBool(string key, bool defaultValue = false)
    {
        var value = _configuration[key];
        return value != null ? bool.Parse(value) : defaultValue;
    }
    
    public int GetInt(string key, int defaultValue = 0)
    {
        var value = _configuration[key];
        return value != null ? int.Parse(value) : defaultValue;
    }
    
    public bool IsDevelopment()
    {
        return _environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase);
    }
    
    public bool IsProduction()
    {
        return _environmentName.Equals("Production", StringComparison.OrdinalIgnoreCase);
    }
}