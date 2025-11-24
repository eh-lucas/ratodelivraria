using Microsoft.EntityFrameworkCore;
using Sherlock.Domain.Entities;

namespace Sherlock.Data.Context;

public class SherlockDbContext : DbContext
{
    public SherlockDbContext(DbContextOptions<SherlockDbContext> options) : base(options)
    {
    }

    // Entidades principais
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookPrice> BookPrices => Set<BookPrice>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Query> Queries => Set<Query>();

    // Entidades de suporte
    public DbSet<ResultType> ResultTypes => Set<ResultType>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<Scraper> Scrapers => Set<Scraper>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Book
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Isbn).HasMaxLength(20);
            entity.Property(e => e.Author).HasMaxLength(300);
            entity.HasIndex(e => e.Isbn);
            entity.HasIndex(e => e.Title);
        });

        // BookPrice
        modelBuilder.Entity<BookPrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Discount).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.BookId, e.ProviderId, e.QueryDateTime });
        });

        // Provider
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Url).IsUnique();
        });

        // Query
        modelBuilder.Entity<Query>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InputParameters).HasColumnType("jsonb");
            entity.Property(e => e.Result).HasColumnType("jsonb");
            entity.HasIndex(e => e.StartDateTime);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // ResultType
        modelBuilder.Entity<ResultType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50);
        });
    }
}
