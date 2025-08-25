using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _users;
        private readonly SignInManager<IdentityUser> _signIn;

        public AuthController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn)
        {
            _users = users;
            _signIn = signIn;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };
            var result = await _users.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.GivenName, dto.FirstName ?? string.Empty),
                new(System.Security.Claims.ClaimTypes.Surname, dto.LastName ?? string.Empty)
            };
            await _users.AddClaimsAsync(user, claims);

            await _signIn.SignInAsync(user, isPersistent: true);
            return Ok(new { ok = true });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _signIn.PasswordSignInAsync(
                dto.Email, dto.Password, isPersistent: true, lockoutOnFailure: false);
            return result.Succeeded ? Ok(new { ok = true }) : Unauthorized();
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return Ok(new { ok = true });
        }
    }
}
