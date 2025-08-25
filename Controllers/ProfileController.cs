using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using CMS.Data;

[ApiController]
[Route("[controller]")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound("User not found.");

        // Optionally increment page visits metric when profile viewed
        // user.PageVisits++;
        // await _userManager.UpdateAsync(user);

        return Ok(new {
            email = user.Email,
            firstName = user.FirstName ?? "First Name not provided",
            lastName = user.LastName ?? "Last Name not provided",
            pageVisits = user.PageVisits
        });
    }
}