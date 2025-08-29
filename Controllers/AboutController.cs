using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using CMS.Data;
using CMS.DTOs;

[ApiController]
[Route("api/[controller]")]
public class AboutController : ControllerBase
{
    private readonly AuthDbContext _context;

    public AboutController(AuthDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SaveAbout([FromForm] AboutSectionDto dto)
    {
        var about = new AboutSection
        {
            Title = dto.Title,
            Content = dto.Description,
            UserId = "user123" // later replace with actual logged-in user
        };

        if (dto.Image != null)
        {
            var filePath = Path.Combine("wwwroot/images", dto.Image.FileName);
            using var stream = System.IO.File.Create(filePath);
            await dto.Image.CopyToAsync(stream);
            about.ImageUrl = $"/images/{dto.Image.FileName}";
        }

        _context.AboutSections.Add(about);
        await _context.SaveChangesAsync();

        return Ok(about);
    }
}
