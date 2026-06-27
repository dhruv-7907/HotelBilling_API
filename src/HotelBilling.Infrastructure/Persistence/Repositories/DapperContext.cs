using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' cannot be empty.");
        }
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
