using CMS.Controllers.Base;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;

namespace CMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DomainsController : TenantControllerBase
    {
        private readonly ApiContext _db;

        public DomainsController(ApiContext db) => _db = db;

        public record DomainDto(int Id, string Hostname, bool IsPrimary, DateTime CreatedUtc);
        public record AddDomainDto([Required] string Hostname);
        public record SetPrimaryDto([Required] int DomainId);

        private static string NormalizeHost(string input)
        {
            var s = input.Trim().ToLowerInvariant();

            // Strip scheme if present
            if (s.StartsWith("http://")) s = s.Substring(7);
            if (s.StartsWith("https://")) s = s.Substring(8);

            // Strip trailing slash + path
            var slash = s.IndexOf('/');
            if (slash >= 0) s = s[..slash];

            // Strip port if present
            var colon = s.IndexOf(':');
            if (colon >= 0) s = s[..colon];

            // Strip common "www." (optional)
            if (s.StartsWith("www.")) s = s.Substring(4);

            return s;
        }

        // GET /api/domains
        // List domains for current tenant
        [Authorize(Policy = "TenantMatch")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DomainDto>>> List()
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var list = await _db.TenantDomains
                .Where(d => d.TenantId == TenantId)
                .OrderByDescending(d => d.IsPrimary).ThenBy(d => d.Hostname)
                .Select(d => new DomainDto(d.Id, d.Hostname, d.IsPrimary, d.CreatedUtc))
                .ToListAsync();

            return Ok(list);
        }

        // POST /api/domains
        // Add a domain to current tenant
        [Authorize] // or [Authorize(Policy="TenantMatch")] once user has a tenant
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddDomainDto dto,
                                      [FromServices] UserManager<ApplicationUser> users,
                                      [FromServices] SignInManager<ApplicationUser> signIn)
        {
            if (User?.Identity?.IsAuthenticated != true) return Unauthorized();

            var user = await users.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var host = NormalizeHost(dto.Hostname);
            if (string.IsNullOrWhiteSpace(host)) return BadRequest("Invalid hostname.");

            // hostname must be unique across all tenants
            var exists = await _db.TenantDomains.AnyAsync(d => d.Hostname == host);
            if (exists) return Conflict("Hostname is already in use.");

            // If user has no tenant yet: create one and link user
            if (string.IsNullOrWhiteSpace(user.TenantId))
            {
                var tenant = new Tenant { Name = $"{user.FirstName} {user.LastName}".Trim() };
                _db.Tenants.Add(tenant);
                await _db.SaveChangesAsync();

                user.TenantId = tenant.Id;
                await users.UpdateAsync(user);

                // add claim so policy-based endpoints work immediately
                await users.AddClaimAsync(user, new Claim("tenant_id", tenant.Id));
                await signIn.RefreshSignInAsync(user);
            }

            // First domain for this tenant becomes primary
            var hasAny = await _db.TenantDomains.AnyAsync(d => d.TenantId == user.TenantId);
            var domain = new TenantDomain
            {
                TenantId = user.TenantId!,
                Hostname = host,
                IsPrimary = !hasAny
            };

            _db.TenantDomains.Add(domain);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(List), new { }, new { id = domain.Id, host = domain.Hostname, primary = domain.IsPrimary });
        }

        // PUT /api/domains/primary
        // Set which domain is primary
        [Authorize(Policy = "TenantMatch")]
        [HttpPut("primary")]
        public async Task<IActionResult> SetPrimary([FromBody] SetPrimaryDto dto)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var target = await _db.TenantDomains.SingleOrDefaultAsync(d => d.Id == dto.DomainId && d.TenantId == TenantId);
            if (target is null) return NotFound("Domain not found.");

            var current = await _db.TenantDomains.Where(d => d.TenantId == TenantId && d.IsPrimary).ToListAsync();
            foreach (var d in current) d.IsPrimary = false;

            target.IsPrimary = true;
            await _db.SaveChangesAsync();

            return Ok(new { ok = true, primary = target.Hostname });
        }

        // DELETE /api/domains/{id}
        [Authorize(Policy = "TenantMatch")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var d = await _db.TenantDomains.SingleOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);
            if (d is null) return NotFound();

            if (d.IsPrimary)
                return BadRequest("Cannot delete the primary domain. Set another domain as primary first.");

            _db.TenantDomains.Remove(d);
            await _db.SaveChangesAsync();

            return Ok(new { ok = true });
        }
    }
}
