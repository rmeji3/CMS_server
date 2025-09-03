using CMS.Data;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CMS.Services
{
    // Scoped provider: safe to consume scoped ApiContext
    public class TenantCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly ApiContext _db;

        // Dev origins for Vite, etc.
        private static readonly string[] DevOrigins = new[]
        {
            "http://localhost:5173", "https://localhost:5173",
            "http://127.0.0.1:5173", "https://127.0.0.1:5173",
            "http://localhost:5174", "https://localhost:5174",
            "http://127.0.0.1:5174", "https://127.0.0.1:5174",
        };

        public TenantCorsPolicyProvider(ApiContext db) => _db = db;

        public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            var origin = context.Request.Headers.Origin.ToString();
            if (string.IsNullOrWhiteSpace(origin))
                return new CorsPolicy(); // not a CORS request

            CorsPolicy BuildAllow(string allowOrigin) =>
                new CorsPolicyBuilder()
                    .WithOrigins(allowOrigin)  // echo exact origin
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()        // cookies
                    .Build();

            // Allow dev quickly
            if (DevOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return BuildAllow(origin);

            try
            {
                var host = new Uri(origin).Host.ToLowerInvariant();

                // match exact tenant domain host
                var exists = await _db.TenantDomains
                    .AsNoTracking()
                    .AnyAsync(d => d.Hostname == host);

                if (exists)
                    return BuildAllow(origin);
            }
            catch
            {
                // fall through to deny
            }

            // Deny: return empty policy => no CORS headers
            return new CorsPolicy();
        }
    }
}
