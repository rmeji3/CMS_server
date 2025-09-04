namespace CMS.Models.Info
{
    public class AddressEntity
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zipcode { get; set; } = string.Empty;
    }
}