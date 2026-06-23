using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class DapperContext(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
