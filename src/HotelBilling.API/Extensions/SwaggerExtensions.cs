using Microsoft.OpenApi.Models;

namespace HotelBilling.API.Extensions;

public static class SwaggerExtensions
{
    private const string ApiVersion = "v1";
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(ApiVersion, new OpenApiInfo
            {
                Title = "Hotel Billing Pro API",
                Version = ApiVersion,
                Description = "Full hotel billing & management REST API - Clean Architecture + CQRS + Dapper",
                Contact = new OpenApiContact
                {
                    Name = "HotelBill Team",
                    Email = "api@hotelbilling.com"
                }
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT bearer token."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = BearerScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
