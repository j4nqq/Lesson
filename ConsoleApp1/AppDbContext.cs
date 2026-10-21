using lesson19.Models;
using Microsoft.EntityFrameworkCore;

class AppDbContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Group> Groups => Set<Group>();

    public AppDbContext(DbContextOptions opt) : base(opt)
    {
    }
}
