namespace CMS.Models
{ 
    public class TenantDomain
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

}
