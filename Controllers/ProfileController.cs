using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class ProfileController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMyEmail()
    {
        // The email claim is usually present if you use Identity
        var email = User.Identity?.Name ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        if (email == null)
            return NotFound("Email not found.");

        return Ok(new { email });
    }
}