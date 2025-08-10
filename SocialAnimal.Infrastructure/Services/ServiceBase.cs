using System.Text;
using System.Text.RegularExpressions;
using NodaTime;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Services;

/// <summary>
/// Base class for service implementations with common functionality
/// </summary>
public abstract class ServiceBase
{
    protected readonly ILoggerPortal Logger;
    protected readonly IClock Clock;
    
    protected ServiceBase(ILoggerPortal logger, IClock clock)
    {
        Logger = logger;
        Clock = clock;
    }
    
    /// <summary>
    /// Normalizes phone number to a standard format
    /// </summary>
    protected string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number cannot be empty", nameof(phone));
        
        // Remove all non-digits
        var digitsOnly = Regex.Replace(phone, @"[^\d]", "");
        
        // Handle US numbers (10 or 11 digits)
        if (digitsOnly.Length == 10)
        {
            return $"+1{digitsOnly}";
        }
        
        if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
        {
            return $"+{digitsOnly}";
        }
        
        throw new ArgumentException($"Invalid phone number format: {phone}", nameof(phone));
    }
    
    /// <summary>
    /// Validates email format
    /// </summary>
    protected bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        try
        {
            var emailRegex = new Regex(
                @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Generates a URL-friendly slug from input text
    /// </summary>
    protected string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty", nameof(input));
        
        var slug = new StringBuilder();
        
        foreach (var c in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                slug.Append(c);
            }
            else if (char.IsWhiteSpace(c) || c == '-' || c == '_')
            {
                if (slug.Length > 0 && slug[^1] != '-')
                {
                    slug.Append('-');
                }
            }
        }
        
        // Remove trailing dash
        if (slug.Length > 0 && slug[^1] == '-')
        {
            slug.Length--;
        }
        
        var result = slug.ToString();
        
        if (string.IsNullOrEmpty(result))
            throw new ArgumentException($"Unable to generate slug from input: {input}", nameof(input));
        
        return result;
    }
    
    /// <summary>
    /// Ensures slug is unique by appending numbers if needed
    /// </summary>
    protected async Task<string> EnsureUniqueSlugAsync(string baseSlug, Func<string, Task<bool>> existsCheck)
    {
        var slug = baseSlug;
        var counter = 1;
        
        while (await existsCheck(slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }
        
        return slug;
    }
    
    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    protected void ValidatePageParameters(int skip, int take)
    {
        if (skip < 0)
            throw new ArgumentException("Skip must be non-negative", nameof(skip));
        
        if (take <= 0 || take > 100)
            throw new ArgumentException("Take must be between 1 and 100", nameof(take));
    }
    
    /// <summary>
    /// Validates US state code
    /// </summary>
    protected bool ValidateStateCode(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return false;
        
        if (state.Length != 2)
            return false;
        
        var validStates = new HashSet<string>
        {
            "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
            "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
            "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
            "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
            "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
            "DC" // District of Columbia
        };
        
        return validStates.Contains(state.ToUpperInvariant());
    }
    
    /// <summary>
    /// Validates postal code format (US ZIP codes)
    /// </summary>
    protected bool ValidatePostalCode(string postal)
    {
        if (string.IsNullOrWhiteSpace(postal))
            return false;
        
        // Support both 5-digit and 5+4 format
        var zipRegex = new Regex(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);
        return zipRegex.IsMatch(postal);
    }
    
    /// <summary>
    /// Logs method entry with parameters
    /// </summary>
    protected IDisposable LogMethodEntry(string methodName, Dictionary<string, object>? parameters = null)
    {
        var props = new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Timestamp"] = Clock.GetCurrentInstant().ToString()
        };
        
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                props[param.Key] = param.Value;
            }
        }
        
        Logger.LogDebug("Entering method {Method}", methodName);
        return Logger.BeginScope(methodName, props);
    }
    
    /// <summary>
    /// Logs business rule violation
    /// </summary>
    protected void LogBusinessRuleViolation(string rule, string context, object? data = null)
    {
        var props = new Dictionary<string, object>
        {
            ["Rule"] = rule,
            ["Context"] = context
        };
        
        if (data != null)
        {
            props["Data"] = data;
        }
        
        Logger.LogWithContext(LogLevel.Warning, "Business rule violation: {Rule} in {Context}", props);
    }
    
    /// <summary>
    /// Logs service operation completion
    /// </summary>
    protected void LogOperationComplete(string operation, object result)
    {
        var props = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["Result"] = result
        };
        
        Logger.LogWithContext(LogLevel.Information, "Operation completed: {Operation}", props);
    }
}