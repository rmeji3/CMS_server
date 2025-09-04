using CMS.Contracts.Info;
using CMS.Controllers.Base;
using CMS.Data;
using CMS.Models.Info;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.Controllers.Info
{
    [ApiController]
    [Route("api/[controller]")]
    public class SocialsController : TenantControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApiContext _db;

        public SocialsController(IWebHostEnvironment env, ApiContext db)
        {
            _env = env;
            _db = db;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<SocialsDto>> Get()
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            // Global filter already applies TenantId
            var socials = await _db.Socials.AsNoTracking().SingleOrDefaultAsync();

            if (socials is null)
            {
                var seed = new SocialsEntity
                {
                    TenantId = TenantId!,           // <-- set TenantId
                    Email = string.Empty,
                    Phone = string.Empty,
                    Facebook = string.Empty,
                };
                _db.Socials.Add(seed);
                await _db.SaveChangesAsync();
                return new SocialsDto { Email = string.Empty, Phone = string.Empty, Facebook = string.Empty };
            }

            return new SocialsDto
            {
                Email = socials.Email,
                Phone = socials.Phone,
                Facebook = socials.Facebook,
            };
        }

        [Authorize(Policy = "TenantMatch")]
        [HttpPatch]
        public async Task<ActionResult<SocialsDto>> Patch([FromBody] SocialsPatchDto patch)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var socials = await _db.Socials.SingleOrDefaultAsync();
            if (socials is null)
            {
                socials = new SocialsEntity { TenantId = TenantId! };   // <-- set TenantId
                _db.Socials.Add(socials);
            }

            if (patch.Email is not null) socials.Email = patch.Email.Trim();
            if (patch.Phone is not null) socials.Phone = patch.Phone.Trim();
            if (patch.Facebook is not null) socials.Facebook = patch.Facebook.Trim();

            await _db.SaveChangesAsync();

            return new SocialsDto {
                Email = socials.Email,
                Phone = socials.Phone,
                Facebook = socials.Facebook,
            };
        }
    }
}
