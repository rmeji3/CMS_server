using Microsoft.AspNetCore.Identity;


namespace CMS.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int PageVisits { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        // add more custom properties if needed later
    }
}