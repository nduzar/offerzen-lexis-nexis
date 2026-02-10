using System.Net;

namespace ProductCatalog.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = $"{{\"error\":\"internal_server_error\",\"correlationId\":\"{context.TraceIdentifier}\"}}";
            await context.Response.WriteAsync(payload);
        }
    }
}
