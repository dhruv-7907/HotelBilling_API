using Serilog;
using HotelBilling.API.Extensions;
using HotelBilling.Application;
using HotelBilling.Infrastructure;

const string CorsPolicyName = "HotelBillingCors";
const string HealthCheckEndpoint = "/health";

// Bootstrap Serilog immediately so startup errors are captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Hotel Billing Pro API...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithEnvironmentName());

    // CORS
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    allowedOrigins = allowedOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    builder.Services.AddCors(options =>
        options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    // Application & Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerWithJwt();

    // Health Check
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Middleware pipeline
    app.UseGlobalExceptionHandler();
    app.UseRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Billing Pro API v1");
            c.RoutePrefix = string.Empty;   // Swagger at root "/"
            c.DisplayRequestDuration();
        });
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseCors(CorsPolicyName);
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RequestHost", ctx.Request.Host.Value);
            diag.Set("RequestScheme", ctx.Request.Scheme);
            diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
        };
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks(HealthCheckEndpoint);

    app.Lifetime.ApplicationStarted.Register(() =>
        Log.Information("Hotel Billing Pro API ready on {Urls}", string.Join(", ", app.Urls)));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
