using System.Data;

namespace BackendAssignment.Tests.Infrastructure.Repositories;

public interface IFakeDbConnection : IDbConnection
{
    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null);
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null);
    Task<int> ExecuteAsync(string sql, object param = null);
    Task<int> ExecuteScalarAsync<T>(string sql, object param = null);
}

