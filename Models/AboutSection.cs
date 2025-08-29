namespace CMS.Models
{
    public class AboutSection
    {
        public int Id { get; set; }
        public string UserId { get; set; }   // foreign key to ApplicationUser
        public string Title { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }

        // Navigation property (optional, lets you load the user)
        public ApplicationUser User { get; set; }
    }
}
