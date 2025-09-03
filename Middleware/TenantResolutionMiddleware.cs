// TenantResolutionMiddleware.cs
using CMS.Data;
using Microsoft.EntityFrameworkCore;

namespace CMS.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ApiContext db)
        {
            // 1) Dev/test override via header
            if (context.Request.Headers.TryGetValue("X-Tenant", out var tid) &&
                !string.IsNullOrWhiteSpace(tid))
            {
                context.Items["TenantId"] = tid.ToString();
                await _next(context);
                return;
            }

            // 2) Normal: resolve from host
            var host = context.Request.Host.Host.ToLowerInvariant();

            // normalize common dev hosts (optional)
            if (host == "127.0.0.1") host = "localhost";

            var domain = await db.TenantDomains
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(d => d.Hostname == host);

            if (domain is not null)
                context.Items["TenantId"] = domain.TenantId;

            await _next(context);
        }
    }

    public static class TenantResolutionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
            => app.UseMiddleware<TenantResolutionMiddleware>();
    }
}
