namespace Sherlock.Domain.Entities;

/// <summary>
/// Representa uma consulta individual a um provider específico.
/// Cada Query pertence a uma Transaction.
/// </summary>
public class Query
{
    public int Id { get; set; }

    /// <summary>
    /// Transação à qual esta query pertence
    /// </summary>
    public int TransactionId { get; set; }

    /// <summary>
    /// Provider consultado
    /// </summary>
    public int ProviderId { get; set; }

    /// <summary>
    /// Livro encontrado (opcional, preenchido se houver match)
    /// </summary>
    public int? BookId { get; set; }

    /// <summary>
    /// Momento da consulta
    /// </summary>
    public DateTime QueriedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tempo de resposta em milissegundos
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Se a consulta foi bem-sucedida (retornou resultado válido)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Título encontrado
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Autor encontrado
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Preço encontrado
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Desconto encontrado (percentual)
    /// </summary>
    public int? Discount { get; set; }

    /// <summary>
    /// URL do produto encontrado
    /// </summary>
    public string? ProductUrl { get; set; }

    /// <summary>
    /// Mensagem de erro (se houve falha)
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navegação
    public Transaction? Transaction { get; set; }
    public Provider? Provider { get; set; }
    public Book? Book { get; set; }
}
