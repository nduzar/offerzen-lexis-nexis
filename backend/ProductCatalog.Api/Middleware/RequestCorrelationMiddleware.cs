namespace ProductCatalog.Api.Middleware;

public sealed class RequestCorrelationMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values)
            ? values.ToString()
            : context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        context.Items[HeaderName] = correlationId;

        await next(context);
    }
}
