using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BackendAssignment.Infrastructure.Helpers;

/// <summary>
/// Helper class to manage PostgreSQL database connections using connection strings from configuration.
/// </summary>
/// <remarks>
/// Design Decision:
/// Uses dependency injection to access configuration and safely retrieves the connection string.
/// Provides a centralized place to manage DB connections for easier testing and maintainability.
/// </remarks>
public class DatabaseHelper : IDatabaseHelper
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes the helper with configuration to access the connection string.
    /// </summary>
    /// <param name="configuration">Injected configuration provider</param>
    /// <exception cref="ArgumentNullException">Thrown if the connection string is not configured</exception>
    public DatabaseHelper(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException(nameof(configuration),
                                "Database connection string is missing.");
    }

    /// <summary>
    /// Creates and returns a new PostgreSQL database connection.
    /// </summary>
    /// <returns>Open connection to the database</returns>
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}