namespace CMS.Services
{
    public interface ITenantProvider
    {
        string? TenantId { get; }
    }

    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? TenantId =>
            _httpContextAccessor.HttpContext?.Items["TenantId"] as string;
    }
}
