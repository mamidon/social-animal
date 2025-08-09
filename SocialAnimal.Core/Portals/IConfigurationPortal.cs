namespace SocialAnimal.Core.Portals;

public interface IConfigurationPortal
{
    T GetConfiguration<T>(string sectionName) where T : class, new();
    string GetConnectionString(string name);
    string GetValue(string key);
    bool GetBool(string key, bool defaultValue = false);
    int GetInt(string key, int defaultValue = 0);
    
    // Environment detection
    bool IsDevelopment();
    bool IsProduction();
    string EnvironmentName { get; }
}