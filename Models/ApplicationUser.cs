namespace CMS.Models;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    //  extra fields:
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public int WeeklyViews  { get; set; }
    public DateTime? LastSeenUtc { get; set; }
    public string? TenantId { get; set; }
}

