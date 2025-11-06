using Microsoft.EntityFrameworkCore;
using Sherlock.Domain.Entities;

namespace Sherlock.Data.Context;
public class SherlockDbContext : DbContext
{
    public SherlockDbContext(DbContextOptions<SherlockDbContext> options) : base( options )
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ResultType> Results => Set<ResultType>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<Scraper> SearchTypes => Set<Scraper>();
    public DbSet<Query> Searches => Set<Query>();
}
