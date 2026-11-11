using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels.Dashboard;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.Dashboard
{
    public class DashboardJobStatusService : IDashboardJobStatusService
    {
        private readonly string _connectionString;

        public DashboardJobStatusService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DashboardJobStatusDTO> GetJobStatusDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardJobStatusDTO();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate, DbType.Date);
            parameters.Add("@ToDate", filter.ToDate, DbType.Date);
            parameters.Add("@BranchCode", filter.BranchCode ?? string.Empty, DbType.String);
            parameters.Add("@ShipmentMode", filter.ShipmentMode ?? string.Empty, DbType.String);
            parameters.Add("@Port", filter.Port ?? string.Empty, DbType.String);
            parameters.Add("@CustomerID", filter.CustomerID ?? string.Empty, DbType.String);
            parameters.Add("@JobStatus", filter.JobStatus ?? string.Empty, DbType.String);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.SP_Dashboard_JobStatus",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            // 1. KPIs
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardJobStatusKPIs>()) ?? new DashboardJobStatusKPIs();

            // 2. Status Share
            result.StatusShares = (await multi.ReadAsync<DashboardJobStatusShare>()).ToList();

            // 3. Mode Distribution
            result.ModeDistributions = (await multi.ReadAsync<DashboardJobStatusMode>()).ToList();

            // 4. Port Workloads
            result.PortWorkloads = (await multi.ReadAsync<DashboardJobStatusPort>()).ToList();

            // 5. Monthly Trend
            result.MonthlyTrends = (await multi.ReadAsync<DashboardJobStatusMonthlyTrend>()).ToList();

            // 6. Top Customers
            result.TopCustomers = (await multi.ReadAsync<DashboardJobStatusTopCustomer>()).ToList();

            // 7. Job List
            result.JobList = (await multi.ReadAsync<DashboardJobStatusRow>()).ToList();

            return result;
        }
    }
}
