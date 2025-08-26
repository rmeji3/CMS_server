namespace CMS.Models;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    //  extra fields:
    public string FirstName { get; set; } = "";
    public string LastName  { get; set; } = "";
    public int WeeklyViews  { get; set; }
    public DateTime? LastSeenUtc { get; set; }
}

