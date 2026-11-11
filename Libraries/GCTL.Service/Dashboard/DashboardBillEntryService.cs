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
    public class DashboardBillEntryService : IDashboardBillEntryService
    {
        private readonly string _connectionString;

        public DashboardBillEntryService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DashboardBillEntryDTO> GetBillEntryDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardBillEntryDTO();

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
                "dbo.SP_Dashboard_BillEntry",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            // 1. KPIs
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardBillEntryKPIs>()) ?? new DashboardBillEntryKPIs();

            // 2. Service Compositions
            result.ServiceValueCompositions = (await multi.ReadAsync<DashboardBillEntryServiceComp>()).ToList();

            // 3. Mode Billings
            result.ModeBillings = (await multi.ReadAsync<DashboardBillEntryModeBilling>()).ToList();

            // 4. Collection Statuses
            result.CollectionStatuses = (await multi.ReadAsync<DashboardBillEntryCollectionStatus>()).ToList();

            // 5. Monthly Trend
            result.MonthlyTrends = (await multi.ReadAsync<DashboardBillEntryMonthlyTrend>()).ToList();

            // 6. Top Customers
            result.TopCustomers = (await multi.ReadAsync<DashboardBillEntryTopCustomer>()).ToList();

            // 7. Bill List
            result.BillList = (await multi.ReadAsync<DashboardBillEntryRow>()).ToList();

            return result;
        }
    }
}
