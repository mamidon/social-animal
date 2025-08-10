namespace SocialAnimal.Web.Areas.Admin.Models.ViewModels;

/// <summary>
/// Base view model for admin pages
/// </summary>
public abstract record AdminViewModel
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
}