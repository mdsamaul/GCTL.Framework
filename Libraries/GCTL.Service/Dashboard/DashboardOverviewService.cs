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
    public class DashboardOverviewService : IDashboardOverviewService
    {
        private readonly string _connectionString;

        public DashboardOverviewService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DashboardOverviewDTO> GetOverviewDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardOverviewDTO();

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
                "dbo.SP_Dashboard_Overview",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            // 1. KPIs
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardOverviewKPIs>()) ?? new DashboardOverviewKPIs();

            // 2. Monthly Trend
            result.MonthlyTrends = (await multi.ReadAsync<DashboardOverviewMonthlyTrend>()).ToList();

            // 3. Status Share
            result.StatusShares = (await multi.ReadAsync<DashboardOverviewStatusShare>()).ToList();

            // 4. Top Customers
            result.TopCustomers = (await multi.ReadAsync<DashboardOverviewTopCustomer>()).ToList();

            // 5. Ageing Brackets
            result.AgeingBrackets = (await multi.ReadFirstOrDefaultAsync<DashboardOverviewAgeingBrackets>()) ?? new DashboardOverviewAgeingBrackets();

            // 6. Requisition Breakdown
            result.RequisitionBreakdowns = (await multi.ReadAsync<DashboardOverviewReqBreakdown>()).ToList();

            // 7. Overdue Bills
            result.OverdueBills = (await multi.ReadAsync<DashboardOverviewOverdueBill>()).ToList();

            // 8. Upcoming Arrivals
            result.UpcomingArrivals = (await multi.ReadAsync<DashboardOverviewUpcomingArrival>()).ToList();

            return result;
        }
    }
}
