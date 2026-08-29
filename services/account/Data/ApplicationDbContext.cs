using Microsoft.EntityFrameworkCore;
using account.Models;

namespace account.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserModel> Accounts { get; set; } = null!;
}