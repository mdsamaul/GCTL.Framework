using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.DataTables;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.GPAnalysisReport
{
    public class GPAnalysisReportService : IGPAnalysisReportService
    {
        private readonly string _connectionString;

        public GPAnalysisReportService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection") 
                ?? throw new InvalidOperationException("Connection string 'connection' not found.");
        }

        private (DateTime fromDate, DateTime toDate, string dateUse) ParseDates(GPAnalysisReportFilter filter)
        {
            string dateUse = "N";
            DateTime fromDate = DateTime.Now.Date;
            DateTime toDate = DateTime.Now.Date;

            if (!string.IsNullOrEmpty(filter.FromDate) && !string.IsNullOrEmpty(filter.ToDate))
            {
                if (DateTime.TryParseExact(filter.FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fDate) &&
                    DateTime.TryParseExact(filter.ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tDate))
                {
                    fromDate = fDate.Date;
                    toDate = tDate.Date;
                    dateUse = "Y";
                }
                else if (DateTime.TryParse(filter.FromDate, out fDate) && DateTime.TryParse(filter.ToDate, out tDate))
                {
                    fromDate = fDate.Date;
                    toDate = tDate.Date;
                    dateUse = "Y";
                }
            }

            if (!string.IsNullOrEmpty(filter.DateUse))
            {
                dateUse = filter.DateUse;
            }

            return (fromDate, toDate, dateUse);
        }

        private async Task<List<GPAnalysisReportDTO>> ExecuteStoredProcedureAsync(GPAnalysisReportFilter filter)
        {
            var (fromDate, toDate, dateUse) = ParseDates(filter);

            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ShipmentMode", filter.ShipmentMode ?? string.Empty);
            parameters.Add("@CustomerID", filter.CustomerId ?? string.Empty);
            parameters.Add("@DateUse", dateUse);
            parameters.Add("@FromDate", fromDate, DbType.Date);
            parameters.Add("@ToDate", toDate, DbType.Date);

            var rows = (await connection.QueryAsync<GPAnalysisReportDTO>(
                "GPAnalysedReport2",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 180
            )).ToList();

            return rows;
        }

        public async Task<PaginationResponse<GPAnalysisReportDTO>> GetGridDataAsync(GPAnalysisReportFilter filter)
        {
            var allRows = await ExecuteStoredProcedureAsync(filter);

            var query = allRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(r =>
                    (r.BillNo != null && r.BillNo.ToLower().Contains(term)) ||
                    (r.JobNo != null && r.JobNo.ToLower().Contains(term)) ||
                    (r.CustomerName != null && r.CustomerName.ToLower().Contains(term)) ||
                    (r.ExpenseType != null && r.ExpenseType.ToLower().Contains(term)) ||
                    (r.BillDate != null && r.BillDate.ToLower().Contains(term))
                );
            }

            if (!string.IsNullOrWhiteSpace(filter.SortColumn))
            {
                bool isDesc = string.Equals(filter.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
                query = filter.SortColumn.ToLower() switch
                {
                    "billdate" => isDesc ? query.OrderByDescending(r => r.BillDate) : query.OrderBy(r => r.BillDate),
                    "billno" => isDesc ? query.OrderByDescending(r => r.BillNo) : query.OrderBy(r => r.BillNo),
                    "totalbill" or "totalbilling" => isDesc ? query.OrderByDescending(r => r.TotalBilling) : query.OrderBy(r => r.TotalBilling),
                    "customername" => isDesc ? query.OrderByDescending(r => r.CustomerName) : query.OrderBy(r => r.CustomerName),
                    "billingcommission" => isDesc ? query.OrderByDescending(r => r.BillingCommission) : query.OrderBy(r => r.BillingCommission),
                    "othersbill" => isDesc ? query.OrderByDescending(r => r.OthersBill) : query.OrderBy(r => r.OthersBill),
                    "totalcnfbilling" => isDesc ? query.OrderByDescending(r => r.TotalCNFBilling) : query.OrderBy(r => r.TotalCNFBilling),
                    "billingtransportbill" => isDesc ? query.OrderByDescending(r => r.BillingTransportBill) : query.OrderBy(r => r.BillingTransportBill),
                    "cnfcosting" => isDesc ? query.OrderByDescending(r => r.CNFCosting) : query.OrderBy(r => r.CNFCosting),
                    "costingtransport" => isDesc ? query.OrderByDescending(r => r.CostingTransport) : query.OrderBy(r => r.CostingTransport),
                    "totalcosting" => isDesc ? query.OrderByDescending(r => r.TotalCosting) : query.OrderBy(r => r.TotalCosting),
                    "cnfgp" => isDesc ? query.OrderByDescending(r => r.CNFGP) : query.OrderBy(r => r.CNFGP),
                    "transportgp" => isDesc ? query.OrderByDescending(r => r.TransportGP) : query.OrderBy(r => r.TransportGP),
                    "totalgp" => isDesc ? query.OrderByDescending(r => r.TotalGP) : query.OrderBy(r => r.TotalGP),
                    _ => query
                };
            }

            int totalRecords = allRows.Count;
            int filteredRecords = query.Count();

            int pageSize = filter.Length ?? 10;
            int startIndex = filter.Start ?? 0;

            List<GPAnalysisReportDTO> pagedData;
            if (pageSize == -1)
            {
                pagedData = query.ToList();
            }
            else
            {
                pagedData = query.Skip(startIndex).Take(pageSize).ToList();
            }

            for (int i = 0; i < pagedData.Count; i++)
            {
                pagedData[i].SlNo = startIndex + i + 1;
                if (pagedData[i].TotalBill == 0 && pagedData[i].TotalBilling != 0)
                {
                    pagedData[i].TotalBill = pagedData[i].TotalBilling;
                }
            }

            return new PaginationResponse<GPAnalysisReportDTO>
            {
                Data = pagedData,
                TotalCount = totalRecords,
                FilteredCount = filteredRecords
            };
        }

        public async Task<GPAnalysisReportSummaryDTO> GetSummaryAsync(GPAnalysisReportFilter filter)
        {
            var allRows = await ExecuteStoredProcedureAsync(filter);

            int totalBills = allRows.Count;
            decimal netBillValue = allRows.Sum(r => r.TotalBilling);
            decimal totalCosting = allRows.Sum(r => r.TotalCosting);
            decimal profit = allRows.Sum(r => r.TotalGP);
            string gpPercent = netBillValue > 0 
                ? $"{(profit * 100 / netBillValue):F2}%" 
                : "0.00%";

            return new GPAnalysisReportSummaryDTO
            {
                TotalBills = totalBills,
                NetBillValue = netBillValue,
                TotalCosting = totalCosting,
                Profit = profit,
                GPPercent = gpPercent
            };
        }
    }
}
