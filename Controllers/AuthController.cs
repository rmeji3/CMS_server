using System.Security.Claims;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly ApiContext _db;

        private const string TenantClaimType = "tenant_id";

        public AuthController(UserManager<ApplicationUser> users,
                              SignInManager<ApplicationUser> signIn,
                              ApiContext db)
        {
            _users = users;
            _signIn = signIn;
            _db = db;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName ?? string.Empty,
                LastName = dto.LastName ?? string.Empty,
                // TenantId stays null for now
            };

            var result = await _users.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await _users.AddClaimsAsync(user, new[]
            {
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname,   user.LastName)
        });

            await _signIn.SignInAsync(user, isPersistent: true);
            return Ok(new { ok = true });
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _signIn.PasswordSignInAsync(dto.Email, dto.Password, isPersistent: true, lockoutOnFailure: false);
            if (!result.Succeeded) return Unauthorized();

            // ensure the tenant_id claim exists if user already has a tenant
            var user = await _users.FindByEmailAsync(dto.Email);
            if (user is not null && !string.IsNullOrWhiteSpace(user.TenantId))
            {
                var claims = await _users.GetClaimsAsync(user);
                if (!claims.Any(c => c.Type == TenantClaimType))
                {
                    await _users.AddClaimAsync(user, new Claim(TenantClaimType, user.TenantId));
                    await _signIn.RefreshSignInAsync(user);
                }
            }

            return Ok(new { ok = true });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return Ok(new { ok = true });
        }

        // Handy for debugging session/claims
        [HttpGet("me")]
        public IActionResult Me()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Ok(new { authenticated = false });

            return Ok(new
            {
                authenticated = true,
                name = User.Identity?.Name,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            });
        }
    }
}
