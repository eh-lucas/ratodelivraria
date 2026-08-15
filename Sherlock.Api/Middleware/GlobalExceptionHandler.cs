using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Sherlock.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env, 
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autorizado"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            // Busca cancelada pelo cliente ou estourou o timeout dos scrapers
            OperationCanceledException => (StatusCodes.Status408RequestTimeout, "Requisição cancelada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        var level = status >= StatusCodes.Status500InternalServerError ? 
            LogLevel.Error : 
            LogLevel.Warning;

        logger.Log(level, exception, "Falha em {Method} {Path} respondendo {Status}",
            httpContext.Request.Method, httpContext.Request.Path, status);
        
        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            // Mensagem real só em dev: em produção ela vazaria nome de tabela, URL de provider, etc.
            Detail = env.IsDevelopment() ? exception.Message : "Ocorreu um erro ao processar a requisição."
        };
        
        // TraceIdentifier foi setado pelo CorrelationIdMiddleware — liga o corpo do erro ao log
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}