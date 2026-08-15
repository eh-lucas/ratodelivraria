using Serilog.Context;

namespace Sherlock.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-ID";
    // Limite defensivo: o valor vem do cliente e vai parar no arquivo de log
    private const int MaxIdLength = 64;
    
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        
        // TraceIdentifier alimenta o traceId que o ProblemDetails devolve em caso de erro
        context.TraceIdentifier = correlationId;
        
        // Roda no instante em que a resposta começa a ser escrita — depois de qualquer
        // middleware que tenha limpado os headers (o handler de exceção faz isso)
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        
        // Cada log emitido daqui pra frente carrega a propriedade CorrelationId
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        // Reaproveita o id do cliente/gateway para rastrear a chamada ponta a ponta
        if (context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            && IsSafe(incoming.ToString()))
        {
            return incoming.ToString();
        }
        
        return Guid.NewGuid().ToString("n");
    }
    
    private static bool IsSafe(string value) =>
        !string.IsNullOrEmpty(value) 
        && value.Length <= MaxIdLength
        && !value.Any(char.IsControl);
}