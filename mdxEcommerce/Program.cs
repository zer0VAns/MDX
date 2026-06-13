using mdxEcommerce.Client.Pages;
using mdxEcommerce.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using mdxEcommerce.Data;
using mdxEcommerce.Models;
using mdxEcommerce.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' or 'DATABASE_URL' environment variable not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── JWT ───────────────────────────────────────────────────────────────────
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key no configurada.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "mdxEcommerce",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "mdxEcommerce",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5230/") });
}
else
{
    var renderUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL") ?? "http://localhost:10000";
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(renderUrl) });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();   // ← antes de Authorization
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(mdxEcommerce.Client._Imports).Assembly);
app.MapControllers();

// ── Seed ──────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // Productos iniciales
        if (!context.Products.Any())
        {
            context.Products.AddRange(
                new mdxEcommerce.Models.Products { Name = "Remera MDX",        Price = 25.99m,  ImageUrl = "https://unsplash.com", StockQuantity = 50  },
                new mdxEcommerce.Models.Products { Name = "Zapatillas Running", Price = 89.99m,  ImageUrl = "https://unsplash.com", StockQuantity = 20  },
                new mdxEcommerce.Models.Products { Name = "Gorra Trucker",      Price = 15.50m,  ImageUrl = "https://unsplash.com", StockQuantity = 100 }
            );
            context.SaveChanges();
            Console.WriteLine("--> Productos iniciales insertados con éxito en PostgreSQL.");
        }

        // Admin inicial
        if (!context.Users.Any(u => u.Role == Role.Admin))
        {
            context.Users.Add(new User
            {
                Email           = "admin@mdxecommerce.com",
                Username        = "admin",
                PasswordHash    = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
                Role            = Role.Admin,
                IsActive        = true,
                IsEmailVerified = true
            });
            context.SaveChanges();
            Console.WriteLine("--> Admin inicial creado. Cambiá el password en producción.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Error en seed: {ex.Message}");
    }
}

app.Run();