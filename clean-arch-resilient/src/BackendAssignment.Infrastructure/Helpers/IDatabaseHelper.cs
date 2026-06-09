using System.Data;

namespace BackendAssignment.Infrastructure.Helpers;

/// <summary>
/// Abstraction for creating database connections.
/// </summary>
/// <remarks>
/// Design Decision:
/// This interface abstracts connection creation logic to support cleaner separation of concerns and easier testing.
/// </remarks>
public interface IDatabaseHelper
{
    /// <summary>
    /// Creates a new open connection to the PostgreSQL database.
    /// </summary>
    /// <returns>An instance of <see cref="IDbConnection"/></returns>
    IDbConnection CreateConnection();
}