using System.Diagnostics;

namespace AI_LMS.API.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var watch = Stopwatch.StartNew();
        await next(context);
        watch.Stop();
        logger.LogInformation("{Method} {Path} => {StatusCode} in {ElapsedMs}ms",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, watch.ElapsedMilliseconds);
    }
}
