namespace CMS.Contracts.Info
{
    // Top-level menu
    public class MenuDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;

        // Grouped by category for easy rendering
        public List<MenuCategoryDto> Categories { get; set; } = new();
    }

    public class MenuCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Sandwiches", "Tacos"
        public int SortOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public List<MenuItemDto> Items { get; set; } = new();
    }

    // Unchanged fields for items
    public class MenuItemDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // -------- PATCH contracts --------
    // Keep PATCH nullable = "don’t change" semantics.

    public class MenuPatchDto
    {
        // Optional: patch categories (add/update/delete/reorder) in one request.
        public List<MenuCategoryPatchDto>? Categories { get; set; }
    }

    public class MenuCategoryPatchDto
    {
        public int? Id { get; set; }                 // null => create new category
        public string? Name { get; set; }            // null => leave name alone
        public int? SortOrder { get; set; }
        public bool? IsVisible { get; set; }

        // Optional nested patches for the items in this category
        public List<MenuItemUpsertPatchDto>? Items { get; set; }

        // Optional: IDs to remove from this category in one shot
        public List<int>? RemoveItemIds { get; set; }
    }

    // Upsert for items (within a category)
    public class MenuItemUpsertPatchDto
    {
        public int? Id { get; set; }                 // null => create new item
        public string? ImageUrl { get; set; }
        public string? Name { get; set; }
        public string? Price { get; set; }
        public string? Description { get; set; }

        // Optional reordering inside the category
        public int? SortOrder { get; set; }
        // Optional visibility toggle per item (matches your IsAvailable on entity if you add it later)
        public bool? IsVisible { get; set; }
    }
}