// TenantResolutionMiddleware.cs
using System.Text.RegularExpressions;
using CMS.Data;
using Microsoft.EntityFrameworkCore;

namespace CMS.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private const string TenantItemKey = "TenantId";

        public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ApiContext db)
        {
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            if (context.Items.ContainsKey(TenantItemKey))
            {
                await _next(context);
                return;
            }

            // Explicit dev overrides
            if (context.Request.Headers.TryGetValue("X-Tenant", out var tidHeader) && !string.IsNullOrWhiteSpace(tidHeader))
            {
                context.Items[TenantItemKey] = tidHeader.ToString();
                await _next(context);
                return;
            }
            if (context.Request.Cookies.TryGetValue("tenant_id", out var tidCookie) && !string.IsNullOrWhiteSpace(tidCookie))
            {
                context.Items[TenantItemKey] = tidCookie;
                await _next(context);
                return;
            }

            string? resolvedTenantId = null;

            // 1) Try Origin host first (CORS scenario)
            var originHost = NormalizeHostFromOrigin(context.Request.Headers.Origin);
            if (!string.IsNullOrEmpty(originHost))
            {
                resolvedTenantId = await db.TenantDomains
                    .AsNoTracking()
                    .Where(d => d.Hostname == originHost)
                    .Select(d => d.TenantId)
                    .FirstOrDefaultAsync();
            }

            // 2) Fallback: API Host
            if (resolvedTenantId is null)
            {
                var apiHost = NormalizeHost(context.Request.Host.Host);
                if (!string.IsNullOrEmpty(apiHost))
                {
                    resolvedTenantId = await db.TenantDomains
                        .AsNoTracking()
                        .Where(d => d.Hostname == apiHost)
                        .Select(d => d.TenantId)
                        .FirstOrDefaultAsync();
                }
            }

            if (!string.IsNullOrEmpty(resolvedTenantId))
            {
                context.Items[TenantItemKey] = resolvedTenantId;
            }

            await _next(context);
        }


        // e.g. "http://sub.example.com:3000" -> "sub.example.com"
        private static string? NormalizeHostFromOrigin(string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return null;
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                return NormalizeHost(uri.Host);

            var s = origin.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"^https?://", "");
            var slash = s.IndexOf('/');
            if (slash >= 0) s = s[..slash];
            var colon = s.IndexOf(':');
            if (colon >= 0) s = s[..colon];
            return NormalizeHost(s);
        }

        // Lowercase, no port; NO localhost/127.0.0.1 auto-mapping
        private static string? NormalizeHost(string? host)
            => string.IsNullOrWhiteSpace(host) ? null : host.Trim().ToLowerInvariant();

        private static bool IsLoopbackHost(string? host)
            => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }

    public static class TenantResolutionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
            => app.UseMiddleware<TenantResolutionMiddleware>();
    }
}
