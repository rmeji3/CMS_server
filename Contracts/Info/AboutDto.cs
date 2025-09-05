namespace CMS.Contracts.Info
{
    public class AboutDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    // For PATCH – all nullable so “missing” means “don’t change”
    public class AboutPatchDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
