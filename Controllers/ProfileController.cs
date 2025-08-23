using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class ProfileController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMyProfile()
    {
        // The email claim is usually present if you use Identity
        var email = User.Identity?.Name ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var firstName = User.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "First Name not provided";
        var lastName = User.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "Last Name not provided";

        if (email == null)
            return NotFound("Email not found.");

        int weeklyViews = 6769; // Placeholder for actual data retrieval logic

        return Ok(new { email, firstName, lastName, weeklyViews });
    }
}