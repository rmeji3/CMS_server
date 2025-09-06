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
    public class MenuController : TenantControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApiContext _db;

        public MenuController(IWebHostEnvironment env, ApiContext db)
        {
            _env = env;
            _db = db;
        }

        // GET /api/menu
        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 30, VaryByHeader = "Host")]
        public async Task<ActionResult<MenuDto>> Get()
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var menu = await _db.Menus
                .AsNoTracking()
                .AsSplitQuery()
                .Where(m => m.TenantId == TenantId)
                .Include(m => m.Categories)
                    .ThenInclude(c => c.Items)
                .SingleOrDefaultAsync();

            if (menu is null)
            {
                // Prefer not to create on GET in prod. If you want DX seeding, uncomment:
                // var seed = new MenuEntity { TenantId = TenantId!, Categories = new() };
                // _db.Menus.Add(seed);
                // await _db.SaveChangesAsync();
                // return ToDto(seed);

                // Return empty skeleton instead:
                return Ok(new MenuDto
                {
                    Id = 0,
                    TenantId = TenantId!,
                    Categories = new()
                });
            }

            return Ok(ToDto(menu));
        }

        // PATCH /api/menu  -> replace entire categories tree if provided
        [Authorize(Policy = "TenantMatch")]
        [HttpPatch]
        public async Task<ActionResult<MenuDto>> Patch([FromBody] MenuPatchDto patch)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var menu = await _db.Menus
                .Where(m => m.TenantId == TenantId)
                .Include(m => m.Categories)
                    .ThenInclude(c => c.Items)
                .SingleOrDefaultAsync();

            var isNew = false;
            if (menu is null)
            {
                menu = new MenuEntity { TenantId = TenantId! };
                _db.Menus.Add(menu);
                isNew = true;
            }

            if (patch.Categories is not null)
            {
                // Be explicit to avoid orphans; cascade also works if configured.
                _db.MenuItems.RemoveRange(menu.Categories.SelectMany(c => c.Items));
                _db.MenuCategories.RemoveRange(menu.Categories);
                menu.Categories = new List<MenuCategory>();

                foreach (var cat in patch.Categories)
                {
                    var newCat = new MenuCategory
                    {
                        Name = cat.Name ?? "New Category",
                        SortOrder = cat.SortOrder ?? (menu.Categories.Count + 1),
                        IsVisible = cat.IsVisible ?? true
                    };

                    if (cat.Items is not null)
                    {
                        foreach (var it in cat.Items)
                        {
                            newCat.Items.Add(new MenuItem
                            {
                                ImageUrl = it.ImageUrl ?? string.Empty,
                                Name = it.Name ?? string.Empty,
                                Price = it.Price ?? string.Empty,
                                Description = it.Description ?? string.Empty,
                                SortOrder = it.SortOrder ?? (newCat.Items.Count + 1),
                                IsVisible = it.IsVisible ?? true
                            });
                        }
                    }

                    menu.Categories.Add(newCat);
                }
            }

            await _db.SaveChangesAsync();

            var dto = ToDto(menu);
            if (isNew) return CreatedAtAction(nameof(Get), new { }, dto);
            return Ok(dto);
        }

        // POST /api/menu/images  -> upload only, return { imageUrl }
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
        private static MenuDto ToDto(MenuEntity e) => new MenuDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            Categories = e.Categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new MenuCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    SortOrder = c.SortOrder,
                    IsVisible = c.IsVisible,
                    Items = c.Items
                        .Where(i => i.IsVisible)
                        .OrderBy(i => i.SortOrder)
                        .Select(i => new MenuItemDto
                        {
                            Id = i.Id,
                            ImageUrl = i.ImageUrl,
                            Name = i.Name,
                            Price = i.Price,
                            Description = i.Description
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
