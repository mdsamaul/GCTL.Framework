using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.RequisitionSummaryReport;
using GCTL.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.RequisitionSummaryReport
{
    public class RequisitionSummaryReportService : IRequisitionSummaryReportService
    {
        private readonly IRepository<SalesCustomer> _customerRepo;
        private readonly IRepository<SalesDefPortType> _portTypeRepo;
        private readonly IRepository<CfDefExpenseType> _expenseTypeRepo;
        private readonly IRepository<GCTL.Data.Models.SepDocumentation>? _sepDocRepo;
        private readonly string _connectionString;

        public RequisitionSummaryReportService(
            IRepository<SalesCustomer> customerRepo,
            IRepository<SalesDefPortType> portTypeRepo,
            IRepository<CfDefExpenseType> expenseTypeRepo,
            IConfiguration configuration,
            IRepository<GCTL.Data.Models.SepDocumentation>? sepDocRepo = null)
        {
            _customerRepo = customerRepo;
            _portTypeRepo = portTypeRepo;
            _expenseTypeRepo = expenseTypeRepo;
            _sepDocRepo = sepDocRepo;
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? string.Empty;
        }

        #region Stored Procedure Execution
        private async Task<List<SpRequisitionSummaryRow>> ExecuteSpAsync(RequisitionSummaryReportFilterVM filter)
        {
            var (from, to) = ParseDates(filter.FromDate, filter.ToDate);
            string dateUse = (from.HasValue && to.HasValue) ? "Y" : "N";

            var parameters = new DynamicParameters();
            parameters.Add("@JobNoId", CleanCommaSeparated(filter.JobNos));
            parameters.Add("@FromDate", from.HasValue ? from.Value.Date : (object)DBNull.Value, DbType.Date);
            parameters.Add("@ToDate", to.HasValue ? to.Value.Date : (object)DBNull.Value, DbType.Date);
            parameters.Add("@DateUse", dateUse);
            parameters.Add("@CustomerId", CleanCommaSeparated(filter.CustomerIds));
            parameters.Add("@ExpenseTypeID", CleanCommaSeparated(filter.ShipmentModeIds));
            parameters.Add("@PortId", CleanCommaSeparated(filter.PortIds));

            using var connection = new SqlConnection(_connectionString);
            var results = await connection.QueryAsync<SpRequisitionSummaryRow>(
                "GetJobRequisitionSummary3",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            return results.ToList();
        }
        #endregion

        #region Grid Data
        public async Task<(List<RequisitionSummaryReportItemDTO> Data, int TotalCount, int FilteredCount)> GetGridDataAsync(RequisitionSummaryReportFilterVM filter)
        {
            filter ??= new RequisitionSummaryReportFilterVM();
            var rawList = await ExecuteSpAsync(filter);

            var allItems = rawList.Select(r => new RequisitionSummaryReportItemDTO
            {
                JobNo = r.JobNo?.Trim() ?? string.Empty,
                JobDate = FormatDateDisplay(r.JobDate),
                CustomerName = r.CustomerName?.Trim() ?? string.Empty,
                PortType = r.PortType?.Trim() ?? string.Empty,
                ShipmentMode = r.ShipmentMode?.Trim() ?? string.Empty,
                Receiptable = ParseDecimal(r.Receiptable),
                NonReceiptable = ParseDecimal(r.NonReceiptable),
                Total = ParseDecimal(r.TotalCost)
            }).ToList();

            int totalCount = allItems.Count;

            // Global search
            var query = allItems.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(x =>
                    x.JobNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.JobDate.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.PortType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.ShipmentMode.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var filteredItems = query.ToList();
            int filteredCount = filteredItems.Count;

            // Sorting
            filteredItems = ApplySorting(filteredItems, filter.SortColumn, filter.SortOrder);

            // Pagination
            int start = filter.Start ?? 0;
            int length = filter.Length ?? (filter.PageSize > 0 ? filter.PageSize : 10);
            if (length <= 0) length = 10;

            var paged = filteredItems.Skip(start).Take(length).ToList();
            for (int i = 0; i < paged.Count; i++)
            {
                paged[i].Sl = start + i + 1;
            }

            return (paged, totalCount, filteredCount);
        }
        #endregion

        #region KPI Summary
        public async Task<RequisitionSummaryReportSummaryDTO> GetSummaryAsync(RequisitionSummaryReportFilterVM filter)
        {
            filter ??= new RequisitionSummaryReportFilterVM();
            var rawList = await ExecuteSpAsync(filter);

            decimal totalRec = 0;
            decimal totalNonRec = 0;
            decimal grandTotal = 0;

            foreach (var r in rawList)
            {
                totalRec += ParseDecimal(r.Receiptable);
                totalNonRec += ParseDecimal(r.NonReceiptable);
                grandTotal += ParseDecimal(r.TotalCost);
            }

            return new RequisitionSummaryReportSummaryDTO
            {
                TotalJobs = rawList.Count,
                TotalReceiptable = totalRec,
                TotalNonReceiptable = totalNonRec,
                GrandTotal = grandTotal
            };
        }
        #endregion

        #region Filter Options
        public async Task<RequisitionSummaryReportFilterOptionsDTO> GetFilterOptionsAsync(
            string? fromDate, string? toDate,
            string? customerIds = null, string? portIds = null, string? shipmentModes = null)
        {
            var options = new RequisitionSummaryReportFilterOptionsDTO();

            try
            {
                // Customers
                options.Customers = await _customerRepo.All().AsNoTracking()
                    .Where(c => !string.IsNullOrEmpty(c.CustomerId) && !string.IsNullOrEmpty(c.CustomerName))
                    .OrderBy(c => c.CustomerName)
                    .Select(c => new CommonSelectModel
                    {
                        Code = c.CustomerId,
                        Name = c.CustomerName
                    })
                    .ToListAsync();

                // Port Types
                options.PortTypes = await _portTypeRepo.All().AsNoTracking()
                    .Where(p => !string.IsNullOrEmpty(p.PortId) && !string.IsNullOrEmpty(p.PortType))
                    .OrderBy(p => p.PortType)
                    .Select(p => new CommonSelectModel
                    {
                        Code = p.PortId,
                        Name = p.PortType
                    })
                    .ToListAsync();

                // Shipment Modes (Expense Types)
                options.ShipmentModes = await _expenseTypeRepo.All().AsNoTracking()
                    .Where(e => !string.IsNullOrEmpty(e.ExpenseTypeId) && !string.IsNullOrEmpty(e.ExpenseType))
                    .OrderBy(e => e.ExpenseType)
                    .Select(e => new CommonSelectModel
                    {
                        Code = e.ExpenseTypeId,
                        Name = e.ExpenseType
                    })
                    .ToListAsync();

                // Distinct Job Numbers from SepDocumentation
                if (_sepDocRepo != null)
                {
                    options.JobNos = await _sepDocRepo.All().AsNoTracking()
                        .Where(d => !string.IsNullOrEmpty(d.JobNo))
                        .Select(d => d.JobNo!)
                        .Distinct()
                        .OrderBy(j => j)
                        .Take(500)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading filter options: {ex.Message}");
            }

            return options;
        }
        #endregion

        #region PDF Generation
        public async Task<byte[]> GeneratePdfAsync(RequisitionSummaryReportFilterVM filter)
        {
            filter ??= new RequisitionSummaryReportFilterVM();
            var rawList = await ExecuteSpAsync(filter);

            // Filter by selected jobs if user ticked checkboxes
            var selectedJobs = ParseMultiSelect(filter.SelectedJobNos);
            if (selectedJobs.Any())
            {
                rawList = rawList.Where(r => !string.IsNullOrEmpty(r.JobNo) && selectedJobs.Contains(r.JobNo.Trim(), StringComparer.OrdinalIgnoreCase)).ToList();
            }

            var pdfModel = new RequisitionSummaryPdfModel
            {
                CompanyName = "Formosa Logistics Limited",
                CompanyAddress = "9th Floor, TCB Bhaban, 1 Kawran Bazar\nRd, Dhaka 1215",
                ReportTitle = "Requisition Summary Report",
                PrintDate = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt")
            };

            // Group by CustomerName
            var customerGroups = rawList
                .GroupBy(r => !string.IsNullOrWhiteSpace(r.CustomerName) ? r.CustomerName.Trim() : "Other")
                .OrderBy(g => g.Key);

            decimal grandRec = 0;
            decimal grandNonRec = 0;
            decimal grandCost = 0;
            int totalJobs = 0;

            foreach (var group in customerGroups)
            {
                var groupModel = new RequisitionSummaryCustomerGroupPdfModel
                {
                    CustomerName = group.Key
                };

                int sl = 1;
                foreach (var item in group)
                {
                    decimal rec = ParseDecimal(item.Receiptable);
                    decimal nonRec = ParseDecimal(item.NonReceiptable);
                    decimal tot = ParseDecimal(item.TotalCost);

                    groupModel.Items.Add(new RequisitionSummaryItemPdfModel
                    {
                        SlNo = sl++,
                        JobNo = item.JobNo?.Trim() ?? string.Empty,
                        JobDate = FormatDateDisplay(item.JobDate),
                        ShipmentMode = item.ShipmentMode?.Trim() ?? string.Empty,
                        Receiptable = rec,
                        NonReceiptable = nonRec,
                        Total = tot
                    });

                    groupModel.SubtotalReceiptable += rec;
                    groupModel.SubtotalNonReceiptable += nonRec;
                    groupModel.SubtotalTotal += tot;

                    grandRec += rec;
                    grandNonRec += nonRec;
                    grandCost += tot;
                    totalJobs++;
                }

                pdfModel.CustomerGroups.Add(groupModel);
            }

            pdfModel.TotalJobsCount = totalJobs;
            pdfModel.GrandTotalReceiptable = grandRec;
            pdfModel.GrandTotalNonReceiptable = grandNonRec;
            pdfModel.GrandTotalCost = grandCost;

            return RequisitionSummaryReportPdfGenerator.GeneratePdf(pdfModel);
        }
        #endregion

        #region Helpers
        private static (DateTime? from, DateTime? to) ParseDates(string? fromDate, string? toDate)
        {
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "yyyy/MM/dd", "MM/dd/yyyy", "M/d/yyyy" };
            DateTime? f = null, t = null;

            if (!string.IsNullOrWhiteSpace(fromDate))
            {
                if (DateTime.TryParseExact(fromDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fd))
                    f = fd.Date;
                else if (DateTime.TryParse(fromDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var df))
                    f = df.Date;
            }

            if (!string.IsNullOrWhiteSpace(toDate))
            {
                if (DateTime.TryParseExact(toDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var td))
                    t = td.Date;
                else if (DateTime.TryParse(toDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    t = dt.Date;
            }

            return (f, t);
        }

        private static string FormatDateDisplay(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return string.Empty;
            string[] formats = { "yyyy/MM/dd", "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy" };
            if (DateTime.TryParseExact(dateStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ||
                DateTime.TryParse(dateStr.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ||
                DateTime.TryParse(dateStr.Trim(), out d))
            {
                return d.ToString("dd/MM/yyyy");
            }
            return dateStr.Trim();
        }

        private static string CleanCommaSeparated(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(x => x.Trim())
                             .Where(x => !string.IsNullOrEmpty(x) && x != "null" && x != "undefined");
            return string.Join(",", parts);
        }

        private static HashSet<string> ParseMultiSelect(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return new HashSet<string>(
                input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())
                     .Where(s => !string.IsNullOrEmpty(s) && s != "null" && s != "undefined"),
                StringComparer.OrdinalIgnoreCase);
        }

        private static decimal ParseDecimal(object? val)
        {
            if (val == null) return 0m;
            if (val is decimal d) return d;
            if (val is double db) return (decimal)db;
            if (decimal.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return 0m;
        }

        private static List<RequisitionSummaryReportItemDTO> ApplySorting(
            List<RequisitionSummaryReportItemDTO> list, string? col, string? dir)
        {
            bool desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            return (col?.ToLowerInvariant()) switch
            {
                "jobno" => desc ? list.OrderByDescending(x => x.JobNo).ToList() : list.OrderBy(x => x.JobNo).ToList(),
                "jobdate" => desc ? list.OrderByDescending(x => x.JobDate).ToList() : list.OrderBy(x => x.JobDate).ToList(),
                "customername" => desc ? list.OrderByDescending(x => x.CustomerName).ToList() : list.OrderBy(x => x.CustomerName).ToList(),
                "porttype" => desc ? list.OrderByDescending(x => x.PortType).ToList() : list.OrderBy(x => x.PortType).ToList(),
                "shipmentmode" => desc ? list.OrderByDescending(x => x.ShipmentMode).ToList() : list.OrderBy(x => x.ShipmentMode).ToList(),
                "receiptable" => desc ? list.OrderByDescending(x => x.Receiptable).ToList() : list.OrderBy(x => x.Receiptable).ToList(),
                "nonreceiptable" => desc ? list.OrderByDescending(x => x.NonReceiptable).ToList() : list.OrderBy(x => x.NonReceiptable).ToList(),
                "total" => desc ? list.OrderByDescending(x => x.Total).ToList() : list.OrderBy(x => x.Total).ToList(),
                _ => list
            };
        }
        #endregion
    }

    internal class SpRequisitionSummaryRow
    {
        public int? ROWNUMBER { get; set; }
        public string? CustomerName { get; set; }
        public string? JobNo { get; set; }
        public string? JobDate { get; set; }
        public string? PortType { get; set; }
        public string? ShipmentMode { get; set; }
        public object? Receiptable { get; set; }
        public object? NonReceiptable { get; set; }
        public object? TotalCost { get; set; }
        public decimal? GrandTotal { get; set; }
        public int? TotalCount { get; set; }
    }
}
