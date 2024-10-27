using Microsoft.EntityFrameworkCore;
using Sherlock.Domain.Entities;

namespace Sherlock.Data.Context;
public class SherlockDbContext : DbContext
{
    public SherlockDbContext(DbContextOptions<SherlockDbContext> options) : base( options )
    {
    }
    public DbSet<User>? Users { get; set; }
    public DbSet<Result>? Results { get; set; }
    public DbSet<Token>? Tokens { get; set; }
    public DbSet<SearchType>? SearchTypes { get; set; }
    public DbSet<Search>? Searches { get; set; }
}
