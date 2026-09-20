using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;
using Samagra.Application.Interfaces;
using Samagra.Infrastructure.Data;
using Samagra.Infrastructure.Identity;
using Samagra.Infrastructure.Repositories;  
using Samagra.AI;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

 var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section is missing from configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IBuyerRepository, BuyerRepository>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
 builder.Services.AddIdentityCore<ApplicationUser>(o =>
{
    o.Password.RequiredLength = 2;
    o.User.RequireUniqueEmail = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<ApplicationRole>()
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // Vite का default port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();                    // ← ज़रूरी
    });
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "Samagra:";   // keys का prefix
});
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(30),       // L2 (Redis)
        LocalCacheExpiration = TimeSpan.FromSeconds(10)  // L1 (memory)
    };
});
builder.Services.AddOutputCache();
builder.Services.AddAiServices(builder.Configuration);
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    foreach (var role in new[] { Roles.Buyer, Roles.Admin })
    {
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new ApplicationRole(role));
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// middleware में
app.UseCors("AllowReact");
app.MapControllers();
app.UseOutputCache();
app.MapGet("/api/fast", () => Results.Ok("fast"));

// हर request slow — ये p50 को ऊपर उठाएगा
app.MapGet("/api/always-slow", async () =>
{
    await Task.Delay(2000);
    return Results.Ok("always slow");
});

// सिर्फ 5% requests slow — ये सिर्फ p99 को ऊपर उठाएगा
app.MapGet("/api/sometimes-slow", async () =>
{
    if (Random.Shared.Next(100) < 5)
        await Task.Delay(4000);
    return Results.Ok("sometimes slow");
});

app.Run();
