using System;
using CMS.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseInMemoryDatabase("AuthDb"));


builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddSignInManager();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = ".cms.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.None; 
    o.SlidingExpiration = true;
});


const string cors = "DevCors";
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(cors, p => p
        .WithOrigins(
        "https://localhost:5173",
        "https://127.0.0.1:5173",
        "http://localhost:5173",
        "http://127.0.0.1:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddControllers();


builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";    
    o.Cookie.Name = "XSRF-TOKEN";     
    o.Cookie.SameSite = SameSiteMode.None;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(cors);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/antiforgery/token", (IAntiforgery af, HttpContext ctx) =>
{
    var tokens = af.GetAndStoreTokens(ctx); 
    return Results.Ok(new { ok = true });
}).AllowAnonymous();


app.MapPost("/auth/register", async (
    UserManager<IdentityUser> users,
    SignInManager<IdentityUser> signIn,
    RegisterDto dto) =>
{
    var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };
    var result = await users.CreateAsync(user, dto.Password);
    if (!result.Succeeded) return Results.BadRequest(result.Errors);

    // Add first and last name as claims
    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.GivenName, dto.FirstName ?? string.Empty),
        new(System.Security.Claims.ClaimTypes.Surname, dto.LastName ?? string.Empty)
    };
    await users.AddClaimsAsync(user, claims);

    await signIn.SignInAsync(user, isPersistent: true);
    return Results.Ok(new { ok = true });
}).AllowAnonymous();

app.MapPost("/auth/login", async (
    SignInManager<IdentityUser> signIn,
    LoginDto dto) =>
{
    var result = await signIn.PasswordSignInAsync(
        dto.Email, dto.Password, isPersistent: true, lockoutOnFailure: false);
    return result.Succeeded ? Results.Ok(new { ok = true }) : Results.Unauthorized();
}).AllowAnonymous();

app.MapPost("/auth/logout", async (SignInManager<IdentityUser> signIn) =>
{
    await signIn.SignOutAsync();
    return Results.Ok(new { ok = true });
}).RequireAuthorization();


// Controllers are protected by default
app.MapControllers().RequireAuthorization();

app.Run();

public record RegisterDto(string Email, string Password, string FirstName, string LastName);
public record LoginDto(string Email, string Password);
