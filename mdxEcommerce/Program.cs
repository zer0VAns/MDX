using mdxEcommerce.Client.Pages;
using mdxEcommerce.Components;
using Microsoft.EntityFrameworkCore;
using mdxEcommerce.Data;

var builder = WebApplication.CreateBuilder(args);


var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");


if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' or 'DATABASE_URL' environment variable not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Configuración dinámica del HttpClient para el servidor
if (builder.Environment.IsDevelopment())
{
    // En tu computadora usa el puerto local actual
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5230/") });
}
else
{
    // En Render toma automáticamente la URL pública que te asigne la plataforma
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
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(mdxEcommerce.Client._Imports).Assembly);


app.MapControllers(); 


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<mdxEcommerce.Data.ApplicationDbContext>();
        
        // Ejecuta todas las migraciones pendientes y crea las tablas reales
        context.Database.Migrate(); 

        // Si la tabla de productos está vacía, agregamos los iniciales
        if (!context.Products.Any()) 
        {
            context.Products.AddRange(
                new mdxEcommerce.Models.Products 
                { 
                    Name = "Remera MDX", 
                    Price = 25.99m, 
                    ImageUrl = "https://unsplash.com", 
                    StockQuantity = 50 
                },
                new mdxEcommerce.Models.Products 
                { 
                    Name = "Zapatillas Running", 
                    Price = 89.99m, 
                    ImageUrl = "https://unsplash.com", 
                    StockQuantity = 20 
                },
                new mdxEcommerce.Models.Products 
                { 
                    Name = "Gorra Trucker", 
                    Price = 15.50m, 
                    ImageUrl = "https://unsplash.com", 
                    StockQuantity = 100 
                }
            );
            
            context.SaveChanges();
            Console.WriteLine("--> Productos iniciales insertados con éxito en PostgreSQL.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Error al insertar productos por defecto: {ex.Message}");
    }
}
app.Run();
