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
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Query> Queries => Set<Query>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();

    // Entidades de suporte
    public DbSet<TransactionResult> TransactionResults => Set<TransactionResult>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<WatchedBook> WatchedBooks => Set<WatchedBook>();
    public DbSet<Scraper> Scrapers => Set<Scraper>();

    // Entidades de créditos
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<CreditPackage> CreditPackages => Set<CreditPackage>();

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

        // CatalogItem — espelho do catálogo das lojas, base das sugestões por nome
        modelBuilder.Entity<CatalogItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.NameNormalized).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Authors).HasMaxLength(500);
            entity.Property(e => e.Href).HasMaxLength(1000);
            entity.Property(e => e.Isbn).HasMaxLength(20);
            entity.Property(e => e.Price).HasPrecision(18, 2);

            // Um produto por loja: reexecutar o crawl atualiza em vez de duplicar
            entity.HasIndex(e => new { e.ProviderId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.NameNormalized);
            entity.HasIndex(e => e.Isbn);
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

            // Seed data - usa Provider.AllSources
            entity.HasData(Provider.AllSources.ToArray());
        });

        // Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InputParameters).HasColumnType("jsonb");
            entity.Property(e => e.Errors).HasColumnType("jsonb");

            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.UserId);

            // Relacionamentos
            entity.HasOne(e => e.TransactionResult)
                .WithMany()
                .HasForeignKey(e => e.ResultTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BestQuery)
                .WithMany()
                .HasForeignKey(e => e.BestQueryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Queries)
                .WithOne(e => e.Transaction)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Query
        modelBuilder.Entity<Query>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(300);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.ProductUrl).HasMaxLength(1000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.SearchIsbn).HasMaxLength(20);

            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => new { e.TransactionId, e.ProviderId });
            entity.HasIndex(e => new { e.SearchIsbn, e.ProviderId, e.QueriedAt })
                .HasFilter("search_isbn IS NOT NULL"); // Índice para cache por ISBN

            // Relacionamentos
            entity.HasOne(e => e.Provider)
                .WithMany()
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Book)
                .WithMany()
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GoogleId).HasMaxLength(64);
            entity.Property(e => e.AvailableCredits).HasDefaultValue(100);
            entity.Property(e => e.TotalCreditsUsed).HasDefaultValue(0);
            entity.HasIndex(e => e.Username).IsUnique();

            // O e-mail é a identidade: é por ele que as duas portas de entrada se
            // encontram (§1.4 do docs/PLANO-OBSERVADOS.md). Até aqui a unicidade era
            // só uma consulta no AuthController antes do insert — duas requisições
            // simultâneas com o mesmo e-mail passavam as duas pela conferência e
            // criavam duas contas. Entrar pelo Google torna isso mais provável, porque
            // achar-ou-criar roda em todo login, não só no cadastro.
            entity.HasIndex(e => e.Email).IsUnique();

            // Nulo para conta criada por senha. No Postgres vários NULL convivem num
            // índice único, então isto restringe só quem veio do Google.
            entity.HasIndex(e => e.GoogleId).IsUnique();

            entity.HasMany(e => e.Transactions)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.CreditTransactions)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CreditTransaction
        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExternalPaymentId).HasMaxLength(200);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);

            entity.HasOne(e => e.CreditPackage)
                .WithMany()
                .HasForeignKey(e => e.CreditPackageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SearchTransaction)
                .WithMany()
                .HasForeignKey(e => e.SearchTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // CreditPackage
        modelBuilder.Entity<CreditPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);

            // Seed data com pacotes padrão
            entity.HasData(CreditPackage.DefaultPackages.ToArray());
        });

        // TransactionResult
        modelBuilder.Entity<TransactionResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.ToTable("result_types"); // Mantém nome da tabela para compatibilidade

            // Seed data
            entity.HasData(
                new TransactionResult { Id = 1, Name = "Success", Description = "Busca realizada com sucesso", IsSuccess = true, IsBillable = true },
                new TransactionResult { Id = 2, Name = "PartialSuccess", Description = "Busca parcialmente realizada - alguns providers falharam", IsSuccess = true, IsBillable = true },
                new TransactionResult { Id = 3, Name = "NoResults", Description = "Nenhum resultado encontrado", IsSuccess = false, IsBillable = false },
                new TransactionResult { Id = 4, Name = "AllFailed", Description = "Todos os providers falharam", IsSuccess = false, IsBillable = false }
            );
        });

        // Token — refresh tokens (§8.1). A tabela existia desde o InitialCreate e
        // nunca tinha sido usada; agora ela guarda a sessão longa.
        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ReplacedByHash).HasMaxLength(64);

            entity.Ignore(e => e.Ativo);

            // O refresh chega com o valor, não com o id: esta é a única busca que
            // acontece no caminho quente, e ela precisa ser única — dois tokens
            // com o mesmo hash seria colisão de sessão.
            entity.HasIndex(e => e.TokenHash).IsUnique();

            // "Sair de todos os aparelhos" e a limpeza do que expirou.
            entity.HasIndex(e => new { e.UserId, e.RevokedAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WatchedBook — os livros que cada pessoa pediu para vigiar
        modelBuilder.Entity<WatchedBook>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Isbn).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(300);
            entity.Property(e => e.ImageUrl).HasMaxLength(1000);
            entity.Property(e => e.LastProviderName).HasMaxLength(200);
            entity.Property(e => e.LastProductUrl).HasMaxLength(1000);
            entity.Property(e => e.DropPercent).HasDefaultValue(15);
            entity.Property(e => e.Active).HasDefaultValue(true);

            // A tela de uma pessoa
            entity.HasIndex(e => new { e.UserId, e.Active });

            // O `SELECT DISTINCT isbn ... WHERE active` do vigia noturno. É a
            // consulta que define o custo da feature inteira (§4.1).
            entity.HasIndex(e => e.Isbn);

            // Pedir o mesmo livro duas vezes não cria duas linhas — a segunda
            // chamada vira edição da regra. Sem isto, varrer um ISBN duplicado
            // seria trabalho repetido para a mesma pessoa.
            entity.HasIndex(e => new { e.UserId, e.Isbn }).IsUnique();

            // Apagar a conta leva a lista junto: ela não significa nada sem o dono,
            // e é o que a exclusão de conta da LGPD (§2.4) precisa que aconteça.
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
