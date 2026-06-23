using Serilog;
using System.Diagnostics;
namespace HotelBilling.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next(ctx);
            sw.Stop();
            Log.Information("{Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            Log.Error("{Method} {Path} failed after {ElapsedMs}ms",
                ctx.Request.Method, ctx.Request.Path, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
