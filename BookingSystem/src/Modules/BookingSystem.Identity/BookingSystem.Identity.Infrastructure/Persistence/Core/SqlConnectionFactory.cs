using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BookingSystem.Identity.Infrastructure.Persistence.Core;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ApplicationDb")
            ?? throw new InvalidOperationException("Connection string 'ApplicationDb' is missing.");
    }

    public DbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}
