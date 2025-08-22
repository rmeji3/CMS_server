using Microsoft.EntityFrameworkCore;
using CMS.Data;
using Microsoft.AspNetCore.Identity;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AuthDbContext>
    (opt => opt.UseInMemoryDatabase("AuthDb"));

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
        .AddEntityFrameworkStores<AuthDbContext>();

// CORS: allow your Vite dev origin(s)
var cors = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(cors, policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173") // add others if needed
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapIdentityApi<IdentityUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(cors);

app.UseAuthorization();

app.MapControllers();

app.Run();