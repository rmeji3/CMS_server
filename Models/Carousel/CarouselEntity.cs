namespace CMS.Models.Info 
{ 
    public class CarouselEntity 
    { 
        public int Id { get; set; } 
        public string TenantId { get; set; } = string.Empty; 
        public List<CarouselItem> Items { get; set; } = new List<CarouselItem>(); 
    } 
    public class CarouselItem 
    { 
        public int Id { get; set; } 
        public string ImageUrl { get; set; } = string.Empty; 
        public string? Description { get; set; } // FK to parent carousel
        public int CarouselEntityId { get; set; } 
        public CarouselEntity Carousel { get; set; } = null!; 
    } 
}