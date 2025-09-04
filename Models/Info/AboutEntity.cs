namespace CMS.Models.Info
{
    public class AboutEntity
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
