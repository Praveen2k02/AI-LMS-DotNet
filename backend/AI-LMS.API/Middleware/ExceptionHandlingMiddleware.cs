using System.Text.Json;
using AI_LMS.Application.Exceptions;

namespace AI_LMS.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted) throw;
            var status = exception is LmsException lms ? lms.StatusCode : 500;
            if (status >= 500)
                logger.LogError(exception, "Unhandled request error for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogInformation("Request rejected: {Status} {Message}", status, exception.Message);
            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            var message = exception is LmsException ? exception.Message : "An unexpected server error occurred.";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message, errors = Array.Empty<string>() }));
        }
    }
}
