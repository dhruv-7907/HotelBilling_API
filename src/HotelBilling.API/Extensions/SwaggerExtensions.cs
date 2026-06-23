using Microsoft.OpenApi.Models;
namespace HotelBilling.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Hotel Billing Pro API",
                Version     = "v1",
                Description = "Full hotel billing & management REST API — Clean Architecture + CQRS + Dapper",
                Contact     = new OpenApiContact { Name = "HotelBill Team", Email = "api@hotelbilling.com" }
            });

            // JWT bearer security definition
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.ApiKey,
                Scheme       = "Bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Enter: Bearer {your JWT token}",
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }
}
