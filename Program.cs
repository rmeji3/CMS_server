using System.Security.Claims;
using CMS.Data;
using CMS.Models;
using CMS.Middleware;
using CMS.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---- Rate Limiting ----
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter("metrics",
        options => {
            options.PermitLimit = 30;             // 30 hits
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 0;
        }
    )
);


// ---- DB Contexts ----

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "app_data");
Directory.CreateDirectory(dataDir);  // ensure folder exists

var csApi = $"Data Source={Path.Combine(dataDir, "cms.db")}";
var csAuth = $"Data Source={Path.Combine(dataDir, "auth.db")}";


builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseSqlite(csAuth));                // swap from InMemory -> Sqlite

builder.Services.AddDbContext<ApiContext>(opt =>
    opt.UseSqlite(csApi));


// ---- Identity / Auth ----
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("TenantMatch", policy =>
        policy.RequireAssertion(ctx =>
        {
            var http = ctx.Resource as HttpContext ??
                       (ctx.Resource as DefaultHttpContext);
            var resolved = http?.Items["TenantId"] as string;
            var claim = ctx.User.FindFirst("tenant_id")?.Value;
            return !string.IsNullOrEmpty(resolved) && claim == resolved;
        }));
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 2;
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

// ---- CORS (dynamic per tenant) ----
builder.Services.AddCors();
// IMPORTANT: make the provider SCOPED so it can use scoped ApiContext safely
builder.Services.AddScoped<ICorsPolicyProvider, TenantCorsPolicyProvider>();

// ---- Controllers / API ----
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.WriteIndented = true;
});

// ---- Antiforgery ----
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "XSRF-TOKEN";
    o.Cookie.SameSite = SameSiteMode.None;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ---- Multipart limits (uploads) ----
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20_000_000; // 20 MB
});

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- Tenant services ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
    scope.ServiceProvider.GetRequiredService<ApiContext>().Database.Migrate();
}

// ---- Metrics endpoint with rate limiting ----
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Static files first (doesn't participate in endpoint routing/CORS)
app.UseStaticFiles();

app.UseRouting();

// CORS must be after UseRouting and before anything that might block requests
app.UseCors("Dev");

// Your tenant resolver AFTER CORS so preflights aren’t intercepted
app.UseTenantResolution();

// Auth after tenant resolution (if auth depends on tenant), before endpoints
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public record RegisterDto(string Email, string Password, string FirstName, string LastName);
public record LoginDto(string Email, string Password);
