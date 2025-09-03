namespace CMS.Contracts
{
    public class AboutDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? ImageUrl { get; set; }
    }

    // For PATCH – all nullable so “missing” means “don’t change”
    public class AboutPatchDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
