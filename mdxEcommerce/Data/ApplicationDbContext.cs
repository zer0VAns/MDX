using Microsoft.EntityFrameworkCore;
using mdxEcommerce.Models;

namespace mdxEcommerce.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Products> Products { get; set; }
}