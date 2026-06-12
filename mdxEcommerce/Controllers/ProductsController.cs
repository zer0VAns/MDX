using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mdxEcommerce.Data;
using mdxEcommerce.Client.Models; // Accesses your client-side public DTOs

namespace mdxEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var dbProducts = await _context.Products.ToListAsync();
        
        var publicCatalog = dbProducts.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
        }).ToList();

        return Ok(publicCatalog);
    }
}
