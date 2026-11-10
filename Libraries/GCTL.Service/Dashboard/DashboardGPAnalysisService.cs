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
    public class DashboardGPAnalysisService : IDashboardGPAnalysisService
    {
        private readonly string _connectionString;

        public DashboardGPAnalysisService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DashboardGPAnalysisDTO> GetGPAnalysisDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardGPAnalysisDTO();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate, DbType.Date);
            parameters.Add("@ToDate", filter.ToDate, DbType.Date);
            parameters.Add("@BranchCode", filter.BranchCode ?? string.Empty, DbType.String);
            parameters.Add("@ShipmentMode", filter.ShipmentMode ?? string.Empty, DbType.String);
            parameters.Add("@CustomerID", filter.CustomerID ?? string.Empty, DbType.String);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.SP_Dashboard_GPAnalysis",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            // 1. KPIs
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardGPAnalysisKPIs>()) ?? new DashboardGPAnalysisKPIs();

            // 2. Cost Compositions
            result.CostCompositions = (await multi.ReadAsync<DashboardGPCostComposition>()).ToList();

            // 3. Mode Margins
            result.ModeMargins = (await multi.ReadAsync<DashboardGPModeMargin>()).ToList();

            // 4. Billing vs Costing
            result.BillingVsCostings = (await multi.ReadAsync<DashboardGPBillingVsCosting>()).ToList();

            // 5. Monthly Trend
            result.MonthlyTrends = (await multi.ReadAsync<DashboardGPMonthlyTrend>()).ToList();

            // 6. Top Customers
            result.TopCustomers = (await multi.ReadAsync<DashboardGPTopCustomer>()).ToList();

            // 7. Detail Grid
            result.GPList = (await multi.ReadAsync<DashboardGPRow>()).ToList();

            return result;
        }
    }
}
