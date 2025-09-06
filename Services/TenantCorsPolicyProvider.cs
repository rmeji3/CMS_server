using CMS.Data;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;

public class TenantCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly ApiContext _db;
    public TenantCorsPolicyProvider(ApiContext db) => _db = db;

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
            return null; // not a CORS request

        CorsPolicy BuildAllow(string allowOrigin) =>
            new CorsPolicyBuilder()
                .WithOrigins(allowOrigin)  // echo exact origin
                .AllowCredentials()
                .WithHeaders(
                    "Content-Type", 
                    "Authorization", 
                    "X-Tenant",
                    "X-Requested-With",           // <-- add this
                    "X-CSRF-Token",               // if you use it
                    "RequestVerificationToken"
                )
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .Build();

        try
        {
            var host = new Uri(origin).Host.ToLowerInvariant();

            // Only allow if this host exists in TenantDomains
            var exists = await _db.TenantDomains
                                  .AsNoTracking()
                                  .AnyAsync(d => d.Hostname == host);

            if (exists)
                return BuildAllow(origin);
        }
        catch
        {
            // fall through
        }

        // Deny by default
        return new CorsPolicy();
    }
}
