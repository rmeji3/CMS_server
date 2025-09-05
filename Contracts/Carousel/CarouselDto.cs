namespace CMS.Contracts.Info
{
    // Full DTO for a carousel (with items)
    public class CarouselDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;

        public List<CarouselItemDto> Items { get; set; } = new();
    }

    // DTO for individual items inside the carousel
    public class CarouselItemDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // For PATCH – nullable so missing means “don’t change”
    public class CarouselPatchDto
    {
        public List<CarouselItemPatchDto>? Items { get; set; }
    }

    public class CarouselItemPatchDto
    {
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
