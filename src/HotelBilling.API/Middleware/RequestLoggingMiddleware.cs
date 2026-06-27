using System.Diagnostics;

namespace HotelBilling.API.Middleware;

public class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);

            stopwatch.Stop();

            logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "{Method} {Path} failed after {ElapsedMs}ms. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);

            throw;
        }
    }
}
