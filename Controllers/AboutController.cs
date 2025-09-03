using CMS.Contracts;
using CMS.Controllers.Base;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AboutController : TenantControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApiContext _db;

        public AboutController(IWebHostEnvironment env, ApiContext db)
        {
            _env = env;
            _db = db;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<AboutDto>> Get()
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            // Global filter already applies TenantId
            var about = await _db.About.AsNoTracking().SingleOrDefaultAsync();

            if (about is null)
            {
                var seed = new AboutEntity
                {
                    TenantId = TenantId!,           // <-- set TenantId
                    Title = "",
                    Description = "",
                    ImageUrl = null
                };
                _db.About.Add(seed);
                await _db.SaveChangesAsync();
                return new AboutDto { Title = "", Description = "", ImageUrl = null };
            }

            return new AboutDto
            {
                Title = about.Title,
                Description = about.Description,
                ImageUrl = about.ImageUrl
            };
        }

        [Authorize(Policy = "TenantMatch")]
        [HttpPatch]
        public async Task<ActionResult<AboutDto>> Patch([FromBody] AboutPatchDto patch)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var about = await _db.About.SingleOrDefaultAsync();
            if (about is null)
            {
                about = new AboutEntity { TenantId = TenantId! };   // <-- set TenantId
                _db.About.Add(about);
            }

            if (patch.Title is not null) about.Title = patch.Title.Trim();
            if (patch.Description is not null) about.Description = patch.Description.Trim();

            await _db.SaveChangesAsync();

            return new AboutDto { Title = about.Title, Description = about.Description, ImageUrl = about.ImageUrl };
        }

        [Authorize(Policy = "TenantMatch")]
        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)]
        public async Task<ActionResult<object>> UploadImage([FromForm] AboutImageForm form)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var image = form.Image;
            if (image is null || image.Length == 0) return BadRequest("No image uploaded.");

            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest("Unsupported file type.");

            var about = await _db.About.SingleOrDefaultAsync() ?? new AboutEntity { TenantId = TenantId! }; // <-- set TenantId
            if (about.Id == 0) _db.About.Add(about);

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var safeBase = Path.GetFileNameWithoutExtension(image.FileName);
            var fileName = $"{safeBase}_{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsDir, fileName);

            await using (var fs = System.IO.File.Create(savePath))
            {
                await image.CopyToAsync(fs);
            }

            about.ImageUrl = $"/uploads/{fileName}";
            await _db.SaveChangesAsync();

            return Ok(new { imageUrl = about.ImageUrl });
        }
    }
}