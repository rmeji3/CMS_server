namespace CMS.Models.Info
{
    public class MenuEntity : ITenantScoped
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;

        public List<MenuCategory> Categories { get; set; } = new();

        // Concurrency token
        // in MenuEntity
        public byte[]? RowVersion { get; set; }   // make nullable

    }

    public class MenuCategory : ITenantScoped
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public int MenuEntityId { get; set; }
        public MenuEntity Menu { get; set; } = null!;

        public List<MenuItem> Items { get; set; } = new();
    }

    public class MenuItem : ITenantScoped
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public int MenuCategoryId { get; set; }
        public MenuCategory Category { get; set; } = null!;
    }
}
