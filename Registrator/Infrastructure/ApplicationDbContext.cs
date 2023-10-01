using Microsoft.EntityFrameworkCore;
using Registrator.Model;

namespace Registrator.Infrastructure
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext()
    { }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    { }

    public virtual DbSet<User> Users => Set<User>();
  }
}