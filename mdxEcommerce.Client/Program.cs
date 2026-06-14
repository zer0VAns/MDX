using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using mdxEcommerce.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<CartService>();  
await builder.Build().RunAsync();