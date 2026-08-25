using Microsoft.EntityFrameworkCore;
using auth.Models;

namespace auth.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Auth> Auths { get; set; } = null!;
}