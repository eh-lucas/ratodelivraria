using Microsoft.EntityFrameworkCore;
using Sherlock.Domain.Entities;

namespace Sherlock.Data.Context;
public class SherlockDbContext : DbContext
{
    public SherlockDbContext(DbContextOptions<SherlockDbContext> options) : base( options )
    {
    }
    public DbSet<User>? Users { get; set; }
    public DbSet<ResultType>? Results { get; set; }
    public DbSet<Token>? Tokens { get; set; }
    public DbSet<Scraper>? SearchTypes { get; set; }
    public DbSet<Query>? Searches { get; set; }
}
