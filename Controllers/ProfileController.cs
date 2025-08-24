using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("[controller]")]
public class ProfileController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMyProfile()
    {
        // Prefer Identity/Claims-based email, then fallback to Name or raw "email" claim
        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.Identity?.Name
                    ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        // Identity uses ClaimTypes.GivenName/Surname. Also support JWT style names as a fallback.
        var firstName = User.FindFirstValue(ClaimTypes.GivenName)
                      ?? User.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value
                      ?? "First Name not provided";

        var lastName = User.FindFirstValue(ClaimTypes.Surname)
                     ?? User.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value
                     ?? "Last Name not provided";

        if (email == null)
            return NotFound("Email not found.");

        int weeklyViews = 6769; // Placeholder for actual data retrieval logic

        return Ok(new { email, firstName, lastName, weeklyViews });
    }
}