using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Infrastructure.Authentication;
using HotelBilling.Infrastructure.Persistence.Repositories;
using HotelBilling.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
namespace HotelBilling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Dapper
        services.AddSingleton<DapperContext>();

        // Repositories
        services.AddScoped<IUserRepository,         UserRepository>();
        services.AddScoped<IGuestRepository,        GuestRepository>();
        services.AddScoped<IRoomRepository,         RoomRepository>();
        services.AddScoped<IReservationRepository,  ReservationRepository>();
        services.AddScoped<IInvoiceRepository,      InvoiceRepository>();
        services.AddScoped<IDashboardRepository,    DashboardRepository>();
        services.AddScoped<IReportRepository,       ReportRepository>();
        services.AddScoped<IHousekeepingRepository, HousekeepingRepository>();

        // Services
        services.AddScoped<IJwtService,             JwtService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService,     CurrentUserService>();

        // JWT Authentication
        var jwtKey     = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
        var jwtIssuer   = config["Jwt:Issuer"];
        var jwtAudience = config["Jwt:Audience"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer           = true,
                ValidIssuer              = jwtIssuer,
                ValidateAudience         = true,
                ValidAudience            = jwtAudience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero,
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    if (ctx.Exception is SecurityTokenExpiredException)
                        ctx.Response.Headers.Append("Token-Expired", "true");
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly",       p => p.RequireRole("SuperAdmin","Admin"));
            options.AddPolicy("FrontDeskUp",     p => p.RequireRole("SuperAdmin","Admin","FrontDesk"));
            options.AddPolicy("AccountsUp",      p => p.RequireRole("SuperAdmin","Admin","AccountsManager"));
            options.AddPolicy("HousekeepingUp",  p => p.RequireRole("SuperAdmin","Admin","FrontDesk","Housekeeping"));
        });

        return services;
    }
}
