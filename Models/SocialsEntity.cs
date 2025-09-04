namespace CMS.Models
{
    public class SocialsEntity
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Facebook { get; set; } = string.Empty;
    }
}

