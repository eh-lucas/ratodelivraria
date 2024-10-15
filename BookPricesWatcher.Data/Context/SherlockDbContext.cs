using Microsoft.EntityFrameworkCore;

namespace Sherlock.Data.Context;
public class SherlockDbContext : DbContext
{
    public SherlockDbContext(DbContextOptions<SherlockDbContext> options) : base( options )
    {
    }

    //public DbSet<>
}
