namespace CMS.Contracts.Info
{
    public class AddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zipcode { get; set; } = string.Empty;
    }

    // For PATCH – all nullable so “missing” means “don’t change”
    public class AddressPatchDto
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zipcode { get; set; }
    }
}
