using Microsoft.EntityFrameworkCore;
using mdxEcommerce.Models;

namespace mdxEcommerce.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // This creates your SQL table named "Products" automatically
    public DbSet<Products> Products { get; set; }
}
