using System.Net;
using System.Text.Json;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Models;
using Serilog;
namespace HotelBilling.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ctx, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext ctx, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException nfe           => (HttpStatusCode.NotFound,          nfe.Message,           Array.Empty<string>()),
            ValidationException ve          => (HttpStatusCode.UnprocessableEntity, "Validation failed.", ve.Errors.SelectMany(e => e.Value).ToArray()),
            UnauthorizedException uae        => (HttpStatusCode.Unauthorized,      uae.Message,           Array.Empty<string>()),
            ForbiddenException fe           => (HttpStatusCode.Forbidden,          fe.Message,            Array.Empty<string>()),
            ConflictException ce            => (HttpStatusCode.Conflict,           ce.Message,            Array.Empty<string>()),
            _                               => (HttpStatusCode.InternalServerError,"An unexpected error occurred.", Array.Empty<string>()),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            Log.Error(exception, "Unhandled exception: {Message}", exception.Message);
        else
            Log.Warning("Handled exception [{StatusCode}]: {Message}", (int)statusCode, exception.Message);

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, errors);
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
