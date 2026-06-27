using System.Net;
using System.Text.Json;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Models;

namespace HotelBilling.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(
                    ex,
                    "An exception occurred after the response started. TraceId: {TraceId}",
                    context.TraceIdentifier);

                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
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
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Handled exception with status code {StatusCode} for {Method} {Path}. TraceId: {TraceId}",
                (int)statusCode,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions), context.RequestAborted);
    }
}
