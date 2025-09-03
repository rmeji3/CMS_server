namespace CMS.Models
{
    public interface ITenantScoped
    {
        string? TenantId { get; set; }
    }
}
