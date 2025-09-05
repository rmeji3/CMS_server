using CMS.Contracts;
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
    public class CarouselController : TenantControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApiContext _db;

        public CarouselController(IWebHostEnvironment env, ApiContext db)
        {
            _env = env;
            _db = db;
        }

        // GET /api/carousel
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<CarouselDto>> Get()
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var carousel = await _db.Carousels
                .AsNoTracking()
                .Include(c => c.Items)
                .SingleOrDefaultAsync();

            if (carousel is null)
            {
                var seed = new CarouselEntity
                {
                    TenantId = TenantId!,
                    Items = new List<CarouselItem>()
                };
                _db.Carousels.Add(seed);
                await _db.SaveChangesAsync();
                return ToDto(seed);
            }

            return ToDto(carousel);
        }

        // PATCH /api/carousel  -> replace entire items list if provided
        [Authorize(Policy = "TenantMatch")]
        [HttpPatch]
        public async Task<ActionResult<CarouselDto>> Patch([FromBody] CarouselPatchDto patch)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var carousel = await _db.Carousels
                .Include(c => c.Items)
                .SingleOrDefaultAsync();

            if (carousel is null)
            {
                carousel = new CarouselEntity { TenantId = TenantId! };
                _db.Carousels.Add(carousel);
            }

            if (patch.Items is not null)
            {
                // Replace items deterministically
                carousel.Items.Clear();
                foreach (var it in patch.Items)
                {
                    if (string.IsNullOrWhiteSpace(it.ImageUrl)) continue;
                    carousel.Items.Add(new CarouselItem
                    {
                        ImageUrl = it.ImageUrl!,
                        Description = it.Description
                    });
                }
            }

            await _db.SaveChangesAsync();
            return ToDto(carousel);
        }

        // POST /api/carousel/images  -> upload only, return { imageUrl }
        [Authorize(Policy = "TenantMatch")]
        [HttpPost("images")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)]
        public async Task<ActionResult<object>> UploadImage([FromForm] ImageForm form)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");
            if (form.Image is null || form.Image.Length == 0) return BadRequest("No image uploaded.");

            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            var ext = Path.GetExtension(form.Image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest("Unsupported file type.");

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads", TenantId);
            Directory.CreateDirectory(uploadsDir);

            var safeBase = Path.GetFileNameWithoutExtension(form.Image.FileName);
            var fileName = $"{safeBase}_{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsDir, fileName);

            await using (var fs = System.IO.File.Create(savePath))
            {
                await form.Image.CopyToAsync(fs);
            }

            var publicUrl = $"/uploads/{TenantId}/{fileName}";
            return Ok(new { imageUrl = publicUrl });
        }

        // --- mapping helper ---
        private static CarouselDto ToDto(CarouselEntity e) => new CarouselDto
        {
            Id = e.Id,
            // (Optional) omit TenantId in responses if you don't want to expose it
            Items = e.Items
                .OrderBy(i => i.Id)
                .Select(i => new CarouselItemDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    Description = i.Description
                })
                .ToList()
        };
    }
}
