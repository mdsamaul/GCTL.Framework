using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace GCTL.Data.DapperContext;

/// <summary>
/// Lightweight Dapper connection factory, matching your existing pattern
/// of using Dapper directly for stored-procedure-heavy reports
/// (RPT_GetDailyAttendanceDetailsReport, usp_GetAttendanceMovement, etc.)
/// alongside EF Core for CRUD.
/// </summary>
public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("connection")
            ?? throw new InvalidOperationException("Connection string 'connection' not found.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
