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
    public class DashboardFilterService : IDashboardFilterService
    {
        private readonly string _connectionString;

        public DashboardFilterService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DashboardInitialDataVM> GetInitialFilterDataAsync()
        {
            var vm = new DashboardInitialDataVM
            {
                DefaultFromDate = DateTime.Now.AddMonths(-3).ToString("dd/MM/yyyy"),
                DefaultToDate = DateTime.Now.ToString("dd/MM/yyyy")
            };

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Branches
            var branches = await conn.QueryAsync<DashboardSelectOption>(
                "SELECT BranchCode AS [Value], BranchName AS [Text] FROM Core_Branch WHERE ISNULL(BranchName,'') <> '' ORDER BY BranchName");
            vm.Branches = branches.ToList();

            // Shipment Modes
            var modes = await conn.QueryAsync<DashboardSelectOption>(
                "SELECT ExpenseTypeID AS [Value], ExpenseType AS [Text] FROM CF_Def_ExpenseType ORDER BY ExpenseType");
            vm.ShipmentModes = modes.ToList();

            // Ports
            var ports = await conn.QueryAsync<DashboardSelectOption>(
                "SELECT PortNameID AS [Value], PortName AS [Text] FROM Sales_Def_PortName ORDER BY PortName");
            vm.Ports = ports.ToList();

            // Customers
            var customers = await conn.QueryAsync<DashboardSelectOption>(
                "SELECT CustomerID AS [Value], CustomerName AS [Text] FROM Sales_Customer WHERE ISNULL(CustomerName,'') <> '' ORDER BY CustomerName");
            vm.Customers = customers.ToList();

            // Statuses
            vm.Statuses = new System.Collections.Generic.List<DashboardSelectOption>
            {
                new() { Value = "Pending", Text = "Pending" },
                new() { Value = "In Transit", Text = "In Transit" },
                new() { Value = "Delivered", Text = "Delivered" }
            };

            return vm;
        }
    }
}
