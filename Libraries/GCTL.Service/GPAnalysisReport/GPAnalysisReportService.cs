using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using GCTL.Core.DataTables;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using GCTL.Service.ShipmentMode;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Core.ViewModels.Common;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.GPAnalysisReport
{
    public class GPAnalysisReportService : IGPAnalysisReportService
    {
        #region Private Fields
        private readonly string _connectionString;
        private readonly IMemoryCache? _cache;
        private readonly IShipmentMode? _shipmentModeService;
        private readonly ISalesCustomerService? _customerService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _webHostEnvironment;
        private byte[]? _logoBytes;
        private bool _logoLoaded;

        public GPAnalysisReportService(
            IConfiguration configuration, 
            IMemoryCache? cache = null,
            IShipmentMode? shipmentModeService = null,
            ISalesCustomerService? customerService = null,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null)
        {
            _connectionString = configuration.GetConnectionString("connection") 
                ?? throw new InvalidOperationException("Connection string 'connection' not found.");
            _cache = cache;
            _shipmentModeService = shipmentModeService;
            _customerService = customerService;
            _webHostEnvironment = env;
        }

        #endregion

        #region Helper Method to Load Logo Bytes (Same as CustomerReport)

        private byte[]? GetLogoBytes()
        {
            if (_logoLoaded) return _logoBytes;
            _logoLoaded = true;

            try
            {
                var candidatePaths = new List<string?>
                {
                    _webHostEnvironment?.WebRootPath != null ? Path.Combine(_webHostEnvironment.WebRootPath, "images", "fermosa-wX200.png") : null,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "UIs", "GCTL.UI.Core", "wwwroot", "images", "fermosa-wX200.png"),
                    @"D:\Project\GCTL.Framework\UIs\GCTL.UI.Core\wwwroot\images\fermosa-wX200.png",
                    @"D:\GCTL\GCTL\Project\CNF\GCTL.Framework\UIs\GCTL.UI.Core\wwwroot\images\fermosa-wX200.png"
                };

                foreach (var path in candidatePaths)
                {
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        _logoBytes = File.ReadAllBytes(path);
                        break;
                    }
                }
            }
            catch
            {
                // Fallback gracefully without throwing
            }

            return _logoBytes;
        }
        #endregion

        #region Get Filter Options for Shipment Modes and Customers

        public async Task<GPAnalysisReportFilterOptionsDTO> GetFilterOptionsAsync()
        {
            var result = new GPAnalysisReportFilterOptionsDTO();

            try
            {
                if (_shipmentModeService != null)
                {
                    var modes = await _shipmentModeService.GetAllExpenseTypesAsync();
                    result.ShipmentModes = modes
                        .Where(m => !string.IsNullOrEmpty(m.ExpenseTypeID))
                        .Select(m => new CommonSelectModel
                        {
                            Code = m.ExpenseTypeID,
                            Name = m.ExpenseType ?? m.ExpenseTypeID
                        })
                        .ToList();
                }
                else
                {
                    using var connection = new SqlConnection(_connectionString);
                    var modes = (await connection.QueryAsync<dynamic>(
                        "SELECT ExpenseTypeID AS Code, ExpenseType AS Name FROM Core_ExpenseType WHERE IsActive = 1 ORDER BY ExpenseType"
                    )).Select(m => new CommonSelectModel
                    {
                        Code = (string)m.Code,
                        Name = (string)m.Name
                    }).ToList();
                    result.ShipmentModes = modes;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPAnalysisReportService.GetFilterOptionsAsync - ShipmentModes error: {ex.Message}");
            }

            try
            {
                if (_customerService != null)
                {
                    var customers = await _customerService.SelectionCustomerAsync();
                    result.Customers = customers
                        .Where(c => !string.IsNullOrEmpty(c.Code))
                        .Select(c => new CommonSelectModel
                        {
                            Code = c.Code,
                            Name = c.Name ?? c.Code
                        })
                        .ToList();
                }
                else
                {
                    using var connection = new SqlConnection(_connectionString);
                    var customers = (await connection.QueryAsync<dynamic>(
                        "SELECT CustomerCode AS Code, CustomerName AS Name FROM Sales_Customer WHERE IsActive = 1 ORDER BY CustomerName"
                    )).Select(c => new CommonSelectModel
                    {
                        Code = (string)c.Code,
                        Name = (string)c.Name
                    }).ToList();
                    result.Customers = customers;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPAnalysisReportService.GetFilterOptionsAsync - Customers error: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Export Report (PDF/Excel)

        public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(GPAnalysisReportFilter filter, string exportFormat)
        {
            filter ??= new GPAnalysisReportFilter();
            exportFormat = (exportFormat ?? "PDF").Trim();

            if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
            {
                var excelBytes = await GenerateExcelReportAsync(filter);
                string fileName = $"GP_Analysis_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return (excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            else
            {
                var pdfBytes = await GeneratePdfReportAsync(filter);
                string fileName = $"GP_Analysis_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return (pdfBytes, "application/pdf", fileName);
            }
        }

        #endregion

        #region Date Parsing Helper

        private (DateTime fromDate, DateTime toDate, string dateUse) ParseDates(GPAnalysisReportFilter filter)
        {
            string dateUse = "Y";
            // Default FromDate to first day of previous month so default load always finds data
            DateTime now = DateTime.Now;
            DateTime fromDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            DateTime toDate = now.Date;

            string[] formats = { "dd/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "d/M/yyyy" };

            if (!string.IsNullOrWhiteSpace(filter.FromDate))
            {
                if (DateTime.TryParseExact(filter.FromDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fDate) ||
                    DateTime.TryParse(filter.FromDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out fDate))
                {
                    fromDate = fDate.Date;
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.ToDate))
            {
                if (DateTime.TryParseExact(filter.ToDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tDate) ||
                    DateTime.TryParse(filter.ToDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out tDate))
                {
                    toDate = tDate.Date;
                }
            }

            if (!string.IsNullOrEmpty(filter.DateUse))
            {
                dateUse = filter.DateUse;
            }

            return (fromDate, toDate, dateUse);
        }

        #endregion

        #region Execute Stored Procedure and In-Memory Filtering

        private async Task<List<GPAnalysisReportDTO>> ExecuteStoredProcedureAsync(GPAnalysisReportFilter filter)
        {
            var (fromDate, toDate, dateUse) = ParseDates(filter);

            var customerIds = (filter.CustomerId ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var shipmentModes = (filter.ShipmentMode ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string baseCacheKey = $"GPAnalysedReport2_BASE_{dateUse}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";

            List<GPAnalysisReportDTO>? baseRows = null;
            if (_cache != null && _cache.TryGetValue(baseCacheKey, out List<GPAnalysisReportDTO>? cached) && cached != null)
            {
                baseRows = cached;
            }

            if (baseRows == null)
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@ShipmentMode", string.Empty);
                parameters.Add("@CustomerID", string.Empty);
                parameters.Add("@DateUse", dateUse);
                parameters.Add("@FromDate", fromDate, DbType.Date);
                parameters.Add("@ToDate", toDate, DbType.Date);

                baseRows = (await connection.QueryAsync<GPAnalysisReportDTO>(
                    "GPAnalysedReport2",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180
                )).ToList();

                _cache?.Set(baseCacheKey, baseRows, TimeSpan.FromMinutes(3));
            }

            // Work on a copy of the cached list
            var rows = baseRows.ToList();

            // 1. In-memory filter for Customer (supports single and multiple)
            if (customerIds.Count > 0)
            {
                var custSet = new HashSet<string>(customerIds, StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(r => 
                    (!string.IsNullOrEmpty(r.CustomerID) && custSet.Contains(r.CustomerID.Trim())) ||
                    (!string.IsNullOrEmpty(r.CustomerName) && custSet.Contains(r.CustomerName.Trim()))
                ).ToList();
            }

            // 2. In-memory filter for Shipment Mode (supports single and multiple)
            if (shipmentModes.Count > 0)
            {
                var modeSet = new HashSet<string>(shipmentModes, StringComparer.OrdinalIgnoreCase);
                if (_shipmentModeService != null)
                {
                    try
                    {
                        var allModes = await _shipmentModeService.GetAllExpenseTypesAsync();
                        foreach (var m in allModes)
                        {
                            if (!string.IsNullOrEmpty(m.ExpenseTypeID) && modeSet.Contains(m.ExpenseTypeID.Trim()))
                            {
                                if (!string.IsNullOrWhiteSpace(m.ExpenseType)) modeSet.Add(m.ExpenseType.Trim());
                            }
                            else if (!string.IsNullOrEmpty(m.ExpenseType) && modeSet.Contains(m.ExpenseType.Trim()))
                            {
                                if (!string.IsNullOrWhiteSpace(m.ExpenseTypeID)) modeSet.Add(m.ExpenseTypeID.Trim());
                            }
                        }
                    }
                    catch { }
                }

                rows = rows.Where(r => 
                    (!string.IsNullOrEmpty(r.ExpenseTypeID) && modeSet.Contains(r.ExpenseTypeID.Trim())) ||
                    (!string.IsNullOrEmpty(r.ExpenseType) && modeSet.Contains(r.ExpenseType.Trim()))
                ).ToList();
            }

            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].SlNo = i + 1;
                if (rows[i].TotalBill == 0 && rows[i].TotalBilling != 0)
                {
                    rows[i].TotalBill = rows[i].TotalBilling;
                }
            }

            return rows;
        }

        #endregion

        #region Grid Data Retrieval with Filtering, Sorting, and Pagination

        public async Task<PaginationResponse<GPAnalysisReportDTO>> GetGridDataAsync(GPAnalysisReportFilter filter)
        {
            try
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
            catch (Exception)
            {

                throw;
            }
        }

        #endregion

        #region Summary Calculation

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

        #endregion

        #region PDF Report Generation (QuestPDF)
        public async Task<byte[]> GeneratePdfReportAsync(GPAnalysisReportFilter filter)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var data = await ExecuteStoredProcedureAsync(filter);

            var customerGroups = data
                .GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "Unknown Customer" : x.CustomerName.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"{filter.FromDate} - {filter.ToDate}" : "All Dates";

            var logoBytes = GetLogoBytes();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(10);
                    page.Size(PageSizes.A4.Landscape());

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(row =>
                        {
                            // Left slot: logo (same as CustomerReport)
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                row.ConstantItem(75).AlignMiddle().Image(logoBytes).FitArea();
                            }

                            // Center: company name / address / report title / date
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Formosa Logistics Limited").FontSize(13).Bold();
                                c.Item().AlignCenter().Text("21, Hazaribagh, Dhaka-1209").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().AlignCenter().Text("GP Analysis Report").FontSize(10.5f).Bold().Underline();
                                c.Item().AlignCenter().Text($"Date : {dateRangeStr}").FontSize(8f).SemiBold();
                            });

                            // Right slot: empty, same width as logo, to keep center visually aligned
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                row.ConstantItem(75);
                            }
                        });
                        headerCol.Item().PaddingBottom(4);
                    });

                    page.Content().Column(col =>
                    {
                        if (customerGroups.Count == 0)
                        {
                            col.Item().Padding(20).AlignCenter().Text("No records found for the selected criteria.").FontSize(9).Italic();
                            return;
                        }

                        foreach (var group in customerGroups)
                        {
                            col.Item().PaddingTop(4).PaddingBottom(2)
                                .Text($"Customer: {group.Key}").FontSize(8f).Bold();

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(0.9f); // 1. Sl.No.
                                    cols.RelativeColumn(1.7f); // 2. Billing Date
                                    cols.RelativeColumn(2.6f); // 3. Bill No
                                    cols.RelativeColumn(1.8f); // 4. Total Bill
                                    cols.RelativeColumn(1.4f); // 5. Comm.
                                    cols.RelativeColumn(1.5f); // 6. Others
                                    cols.RelativeColumn(1.6f); // 7. TOT (C&F)
                                    cols.RelativeColumn(1.4f); // 8. Transport
                                    cols.RelativeColumn(1.6f); // 9. TOT Bill
                                    cols.RelativeColumn(1.4f); // 10. Rece
                                    cols.RelativeColumn(1.4f); // 11. NON Rece
                                    cols.RelativeColumn(1.6f); // 12. TOT Cost
                                    cols.RelativeColumn(1.2f); // 13. Swift
                                    cols.RelativeColumn(1.3f); // 14. Hair
                                    cols.RelativeColumn(1.4f); // 15. TOT
                                    cols.RelativeColumn(1.6f); // 16. TOT Cost
                                    cols.RelativeColumn(1.6f); // 17. C & F
                                    cols.RelativeColumn(1.4f); // 18. TRANS
                                    cols.RelativeColumn(1.6f); // 19. TOT
                                    cols.RelativeColumn(1.3f); // 20. C & F %
                                    cols.RelativeColumn(1.3f); // 21. TRANS %
                                    cols.RelativeColumn(1.3f); // 22. G. GP %
                                });

                                // Header Row 1
                                table.Cell().Row(1).Column(1).RowSpan(3).Element(HCell).Text("Sl.No.");
                                table.Cell().Row(1).Column(2).RowSpan(3).Element(HCell).Text("Billing\nDate");
                                table.Cell().Row(1).Column(3).RowSpan(3).Element(HCell).Text("Bill No");
                                table.Cell().Row(1).Column(4).RowSpan(3).Element(HCell).Text("Total Bill\n(BDT)");
                                table.Cell().Row(1).Column(5).ColumnSpan(5).Element(HCell).Text("Billing");
                                table.Cell().Row(1).Column(10).ColumnSpan(7).Element(HCell).Text("Costing");
                                table.Cell().Row(1).Column(17).ColumnSpan(6).Element(HCell).Text("PNL");

                                // Header Row 2
                                table.Cell().Row(2).Column(5).ColumnSpan(3).Element(HCell).Text("C & F");
                                table.Cell().Row(2).Column(8).RowSpan(2).Element(HCell).Text("Transport");
                                table.Cell().Row(2).Column(9).RowSpan(2).Element(HCell).Text("TOT Bill");
                                table.Cell().Row(2).Column(10).ColumnSpan(3).Element(HCell).Text("C & F Cost");
                                table.Cell().Row(2).Column(13).ColumnSpan(3).Element(HCell).Text("Transport Cost");
                                table.Cell().Row(2).Column(16).RowSpan(2).Element(HCell).Text("TOT Cost");
                                table.Cell().Row(2).Column(17).ColumnSpan(3).Element(HCell).Text("GP");
                                table.Cell().Row(2).Column(20).ColumnSpan(3).Element(HCell).Text("GP%");

                                // Header Row 3
                                table.Cell().Row(3).Column(5).Element(HCell).Text("Comm.");
                                table.Cell().Row(3).Column(6).Element(HCell).Text("Others");
                                table.Cell().Row(3).Column(7).Element(HCell).Text("TOT");
                                table.Cell().Row(3).Column(10).Element(HCell).Text("Rece");
                                table.Cell().Row(3).Column(11).Element(HCell).Text("NON Rece");
                                table.Cell().Row(3).Column(12).Element(HCell).Text("TOT Cost");
                                table.Cell().Row(3).Column(13).Element(HCell).Text("Swift");
                                table.Cell().Row(3).Column(14).Element(HCell).Text("Hair");
                                table.Cell().Row(3).Column(15).Element(HCell).Text("TOT");
                                table.Cell().Row(3).Column(17).Element(HCell).Text("C & F");
                                table.Cell().Row(3).Column(18).Element(HCell).Text("TRANS");
                                table.Cell().Row(3).Column(19).Element(HCell).Text("TOT");
                                table.Cell().Row(3).Column(20).Element(HCell).Text("C & F");
                                table.Cell().Row(3).Column(21).Element(HCell).Text("TRANS");
                                table.Cell().Row(3).Column(22).Element(HCell).Text("G. GP");

                                // Data Rows
                                int sl = 1;
                                foreach (var r in group)
                                {
                                    table.Cell().Element(DCell).AlignCenter().Text(sl++.ToString());
                                    table.Cell().Element(DCell).AlignCenter().Text(r.BillDate ?? "");
                                    table.Cell().Element(DCell).AlignLeft().Text(r.BillNo ?? "");
                                    table.Cell().Element(DCell).AlignRight().Text(r.TotalBill.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.BillingCommission.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.OthersBill.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.TotalCNFBilling.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.BillingTransportBill.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.TotalBilling.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CostingReceptableAmount.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CostingNonReceptableAmount.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CNFCosting.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CostingSwiftTransportAmount.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CostingHairTransportAmount.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CostingTransport.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.TotalCosting.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CNFGP.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.TransportGP.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.TotalGP.ToString("N2"));
                                    table.Cell().Element(DCell).AlignRight().Text(r.CNFGPPercent ?? "0.00%");
                                    table.Cell().Element(DCell).AlignRight().Text(r.TransportGPPercent ?? "0.00%");
                                    table.Cell().Element(DCell).AlignRight().Text(r.TotalGPPercent ?? "0.00%");
                                }

                                // Customer Subtotal Row
                                var cTotalBill = group.Sum(x => x.TotalBill);
                                var cComm = group.Sum(x => x.BillingCommission);
                                var cOthers = group.Sum(x => x.OthersBill);
                                var cTotCnf = group.Sum(x => x.TotalCNFBilling);
                                var cTransBill = group.Sum(x => x.BillingTransportBill);
                                var cTotBill = group.Sum(x => x.TotalBilling);
                                var cCostRece = group.Sum(x => x.CostingReceptableAmount);
                                var cCostNonRece = group.Sum(x => x.CostingNonReceptableAmount);
                                var cCnfCosting = group.Sum(x => x.CNFCosting);
                                var cCostSwift = group.Sum(x => x.CostingSwiftTransportAmount);
                                var cCostHair = group.Sum(x => x.CostingHairTransportAmount);
                                var cCostTrans = group.Sum(x => x.CostingTransport);
                                var cTotCost = group.Sum(x => x.TotalCosting);
                                var cCnfGp = group.Sum(x => x.CNFGP);
                                var cTransGp = group.Sum(x => x.TransportGP);
                                var cTotGp = group.Sum(x => x.TotalGP);
                                string cCnfGpPct = cTotCnf > 0 ? $"{(cCnfGp * 100 / cTotCnf):F2}%" : "0.00%";
                                string cTransGpPct = cTransBill > 0 ? $"{(cTransGp * 100 / cTransBill):F2}%" : "0.00%";
                                string cTotGpPct = cTotBill > 0 ? $"{(cTotGp * 100 / cTotBill):F2}%" : "0.00%";

                                table.Cell().ColumnSpan(3).Element(SubtotalCell).AlignRight().Text("Sub Total:").Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTotalBill.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cComm.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cOthers.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTotCnf.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTransBill.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTotBill.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCostRece.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCostNonRece.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCnfCosting.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCostSwift.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCostHair.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCostTrans.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTotCost.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCnfGp.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTransGp.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTotGp.ToString("N2")).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cCnfGpPct).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTransGpPct).Bold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(cTotGpPct).Bold();
                            });
                        }

                        // Grand Total Table
                        if (data.Count > 0)
                        {
                            var gTotalBill = data.Sum(x => x.TotalBill);
                            var gComm = data.Sum(x => x.BillingCommission);
                            var gOthers = data.Sum(x => x.OthersBill);
                            var gTotCnf = data.Sum(x => x.TotalCNFBilling);
                            var gTransBill = data.Sum(x => x.BillingTransportBill);
                            var gTotBill = data.Sum(x => x.TotalBilling);
                            var gCostRece = data.Sum(x => x.CostingReceptableAmount);
                            var gCostNonRece = data.Sum(x => x.CostingNonReceptableAmount);
                            var gCnfCosting = data.Sum(x => x.CNFCosting);
                            var gCostSwift = data.Sum(x => x.CostingSwiftTransportAmount);
                            var gCostHair = data.Sum(x => x.CostingHairTransportAmount);
                            var gCostTrans = data.Sum(x => x.CostingTransport);
                            var gTotCost = data.Sum(x => x.TotalCosting);
                            var gCnfGp = data.Sum(x => x.CNFGP);
                            var gTransGp = data.Sum(x => x.TransportGP);
                            var gTotGp = data.Sum(x => x.TotalGP);
                            string gCnfGpPct = gTotCnf > 0 ? $"{(gCnfGp * 100 / gTotCnf):F2}%" : "0.00%";
                            string gTransGpPct = gTransBill > 0 ? $"{(gTransGp * 100 / gTransBill):F2}%" : "0.00%";
                            string gTotGpPct = gTotBill > 0 ? $"{(gTotGp * 100 / gTotBill):F2}%" : "0.00%";

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(0.9f);
                                    cols.RelativeColumn(1.7f);
                                    cols.RelativeColumn(2.6f);
                                    cols.RelativeColumn(1.8f);
                                    cols.RelativeColumn(1.4f);
                                    cols.RelativeColumn(1.5f);
                                    cols.RelativeColumn(1.6f);
                                    cols.RelativeColumn(1.4f);
                                    cols.RelativeColumn(1.6f);
                                    cols.RelativeColumn(1.4f);
                                    cols.RelativeColumn(1.4f);
                                    cols.RelativeColumn(1.6f);
                                    cols.RelativeColumn(1.2f);
                                    cols.RelativeColumn(1.3f);
                                    cols.RelativeColumn(1.4f);
                                    cols.RelativeColumn(1.6f);
                                    cols.RelativeColumn(1.6f);
                                    cols.RelativeColumn(1.4f);
                                    cols.RelativeColumn(1.6f);
                                    cols.RelativeColumn(1.3f);
                                    cols.RelativeColumn(1.3f);
                                    cols.RelativeColumn(1.3f);
                                });

                                table.Cell().ColumnSpan(3).Element(GrandTotalCell).AlignRight().Text("Grand Total:").Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTotalBill.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gComm.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gOthers.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTotCnf.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTransBill.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTotBill.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCostRece.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCostNonRece.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCnfCosting.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCostSwift.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCostHair.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCostTrans.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTotCost.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCnfGp.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTransGp.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTotGp.ToString("N2")).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gCnfGpPct).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTransGpPct).Bold();
                                table.Cell().Element(GrandTotalCell).AlignRight().Text(gTotGpPct).Bold();
                            });
                        }
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().Text($"Print Date : {DateTime.Now:d/M/yyyy}").FontSize(6.5f);
                        r.RelativeItem().AlignCenter().Text($"Print By: User").FontSize(6.5f);
                        r.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Page ");
                            t.CurrentPageNumber();
                            t.Span(" of ");
                            t.TotalPages();
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static IContainer HCell(IContainer container) =>
            container.Border(0.4f).BorderColor(Colors.Grey.Medium)
                     .Background(Colors.Grey.Lighten4)
                     .PaddingVertical(1.5f).PaddingHorizontal(1)
                     .AlignCenter().AlignMiddle()
                     .DefaultTextStyle(x => x.FontSize(5.5f).Bold().FontColor(Colors.Black));

        private static IContainer DCell(IContainer container) =>
            container.Border(0.3f).BorderColor(Colors.Grey.Lighten2)
                     .PaddingVertical(1.2f).PaddingHorizontal(1)
                     .AlignMiddle()
                     .DefaultTextStyle(x => x.FontSize(5.2f).FontColor(Colors.Black));

        private static IContainer SubtotalCell(IContainer container) =>
            container.Border(0.4f).BorderColor(Colors.Grey.Medium)
                     .Background(Colors.Grey.Lighten3)
                     .PaddingVertical(1.5f).PaddingHorizontal(1)
                     .AlignMiddle()
                     .DefaultTextStyle(x => x.FontSize(5.4f).Bold().FontColor(Colors.Black));

        private static IContainer GrandTotalCell(IContainer container) =>
            container.Border(0.5f).BorderColor(Colors.Black)
                     .Background(Colors.Grey.Lighten2)
                     .PaddingVertical(1.8f).PaddingHorizontal(1)
                     .AlignMiddle()
                     .DefaultTextStyle(x => x.FontSize(5.6f).Bold().FontColor(Colors.Black));
        #endregion

        #region Excel Report Generation (ClosedXML)
        public async Task<byte[]> GenerateExcelReportAsync(GPAnalysisReportFilter filter)
        {
            var data = await ExecuteStoredProcedureAsync(filter);

            var customerGroups = data
                .GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "Unknown Customer" : x.CustomerName.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"{filter.FromDate} - {filter.ToDate}" : "All Dates";

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("GP Analysis Report");

            // Page title block
            ws.Cell(1, 1).Value = "Formosa Logistics Limited";
            ws.Range(1, 1, 1, 22).Merge().Style.Font.Bold = true;
            ws.Range(1, 1, 1, 22).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(2, 1).Value = "21, Hazaribagh, Dhaka-1209";
            ws.Range(2, 1, 2, 22).Merge().Style.Font.FontSize = 10;
            ws.Range(2, 1, 2, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(3, 1).Value = "GP Analysis Report";
            ws.Range(3, 1, 3, 22).Merge().Style.Font.Bold = true;
            ws.Range(3, 1, 3, 22).Style.Font.FontSize = 12;
            ws.Range(3, 1, 3, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(4, 1).Value = $"Date : {dateRangeStr}";
            ws.Range(4, 1, 4, 22).Merge().Style.Font.Bold = true;
            ws.Range(4, 1, 4, 22).Style.Font.FontSize = 10;
            ws.Range(4, 1, 4, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Embed logo at top-left (same as CustomerReport)
            var logoBytes = GetLogoBytes();
            if (logoBytes != null && logoBytes.Length > 0)
            {
                ws.Row(1).Height = 20;
                ws.Row(2).Height = 16;
                ws.Row(3).Height = 18;

                try
                {
                    using var logoStream = new MemoryStream(logoBytes);
                    var logoPicture = ws.AddPicture(logoStream)
                        .MoveTo(ws.Cell(1, 1), 4, 14);
                    logoPicture.Width = 90;
                    logoPicture.Height = 32;
                }
                catch
                {
                    // Fallback gracefully without throwing if the image can't be embedded
                }
            }

            int rowIdx = 6;

            foreach (var group in customerGroups)
            {
                // Customer row
                ws.Cell(rowIdx, 1).Value = $"Customer: {group.Key}";
                var custRange = ws.Range(rowIdx, 1, rowIdx, 22);
                custRange.Merge();
                custRange.Style.Font.Bold = true;
                custRange.Style.Font.FontSize = 11;
                custRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                custRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                rowIdx++;

                // Table 3-tier header
                int h1 = rowIdx;
                int h2 = rowIdx + 1;
                int h3 = rowIdx + 2;

                ws.Range(h1, 1, h3, 1).Merge().Value = "SN.";
                ws.Range(h1, 2, h3, 2).Merge().Value = "Billing Date";
                ws.Range(h1, 3, h3, 3).Merge().Value = "Bill No";
                ws.Range(h1, 4, h3, 4).Merge().Value = "Total Bill (BDT)";

                ws.Range(h1, 5, h1, 9).Merge().Value = "Billing";
                ws.Range(h1, 10, h1, 16).Merge().Value = "Costing";
                ws.Range(h1, 17, h1, 22).Merge().Value = "PNL";

                ws.Range(h2, 5, h2, 7).Merge().Value = "C & F";
                ws.Range(h2, 8, h3, 8).Merge().Value = "Transport";
                ws.Range(h2, 9, h3, 9).Merge().Value = "TOT Bill";

                ws.Range(h2, 10, h2, 12).Merge().Value = "C & F Cost";
                ws.Range(h2, 13, h2, 15).Merge().Value = "Transport Cost";
                ws.Range(h2, 16, h3, 16).Merge().Value = "TOT Cost";

                ws.Range(h2, 17, h2, 19).Merge().Value = "GP";
                ws.Range(h2, 20, h2, 22).Merge().Value = "GP%";

                ws.Cell(h3, 5).Value = "Comm.";
                ws.Cell(h3, 6).Value = "Others";
                ws.Cell(h3, 7).Value = "TOT";
                ws.Cell(h3, 10).Value = "Rece";
                ws.Cell(h3, 11).Value = "NON Rece";
                ws.Cell(h3, 12).Value = "TOT Cost";
                ws.Cell(h3, 13).Value = "Swift";
                ws.Cell(h3, 14).Value = "Hair";
                ws.Cell(h3, 15).Value = "TOT";
                ws.Cell(h3, 17).Value = "C & F";
                ws.Cell(h3, 18).Value = "TRANS";
                ws.Cell(h3, 19).Value = "TOT";
                ws.Cell(h3, 20).Value = "C & F";
                ws.Cell(h3, 21).Value = "TRANS";
                ws.Cell(h3, 22).Value = "G. GP";

                var headerRange = ws.Range(h1, 1, h3, 22);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontSize = 9;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                rowIdx += 3;

                // Data rows
                int sl = 1;
                foreach (var r in group)
                {
                    ws.Cell(rowIdx, 1).Value = sl++;
                    ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(rowIdx, 2).Value = r.BillDate;
                    ws.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(rowIdx, 3).Value = r.BillNo;
                    ws.Cell(rowIdx, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    SetNum(ws.Cell(rowIdx, 4), r.TotalBill);
                    SetNum(ws.Cell(rowIdx, 5), r.BillingCommission);
                    SetNum(ws.Cell(rowIdx, 6), r.OthersBill);
                    SetNum(ws.Cell(rowIdx, 7), r.TotalCNFBilling);
                    SetNum(ws.Cell(rowIdx, 8), r.BillingTransportBill);
                    SetNum(ws.Cell(rowIdx, 9), r.TotalBilling);
                    SetNum(ws.Cell(rowIdx, 10), r.CostingReceptableAmount);
                    SetNum(ws.Cell(rowIdx, 11), r.CostingNonReceptableAmount);
                    SetNum(ws.Cell(rowIdx, 12), r.CNFCosting);
                    SetNum(ws.Cell(rowIdx, 13), r.CostingSwiftTransportAmount);
                    SetNum(ws.Cell(rowIdx, 14), r.CostingHairTransportAmount);
                    SetNum(ws.Cell(rowIdx, 15), r.CostingTransport);
                    SetNum(ws.Cell(rowIdx, 16), r.TotalCosting);
                    SetNum(ws.Cell(rowIdx, 17), r.CNFGP);
                    SetNum(ws.Cell(rowIdx, 18), r.TransportGP);
                    SetNum(ws.Cell(rowIdx, 19), r.TotalGP);

                    ws.Cell(rowIdx, 20).Value = r.CNFGPPercent ?? "0.00%";
                    ws.Cell(rowIdx, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(rowIdx, 21).Value = r.TransportGPPercent ?? "0.00%";
                    ws.Cell(rowIdx, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(rowIdx, 22).Value = r.TotalGPPercent ?? "0.00%";
                    ws.Cell(rowIdx, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    ws.Range(rowIdx, 1, rowIdx, 22).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Range(rowIdx, 1, rowIdx, 22).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    ws.Range(rowIdx, 1, rowIdx, 22).Style.Font.FontSize = 8.5;

                    rowIdx++;
                }

                // Customer Subtotal
                var subRange = ws.Range(rowIdx, 1, rowIdx, 3);
                subRange.Merge().Value = "Customer Total:";
                subRange.Style.Font.Bold = true;
                subRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                var cTotalBill = group.Sum(x => x.TotalBill);
                var cComm = group.Sum(x => x.BillingCommission);
                var cOthers = group.Sum(x => x.OthersBill);
                var cTotCnf = group.Sum(x => x.TotalCNFBilling);
                var cTransBill = group.Sum(x => x.BillingTransportBill);
                var cTotBill = group.Sum(x => x.TotalBilling);
                var cCostRece = group.Sum(x => x.CostingReceptableAmount);
                var cCostNonRece = group.Sum(x => x.CostingNonReceptableAmount);
                var cCnfCosting = group.Sum(x => x.CNFCosting);
                var cCostSwift = group.Sum(x => x.CostingSwiftTransportAmount);
                var cCostHair = group.Sum(x => x.CostingHairTransportAmount);
                var cCostTrans = group.Sum(x => x.CostingTransport);
                var cTotCost = group.Sum(x => x.TotalCosting);
                var cCnfGp = group.Sum(x => x.CNFGP);
                var cTransGp = group.Sum(x => x.TransportGP);
                var cTotGp = group.Sum(x => x.TotalGP);
                string cCnfGpPct = cTotCnf > 0 ? $"{(cCnfGp * 100 / cTotCnf):F2}%" : "0.00%";
                string cTransGpPct = cTransBill > 0 ? $"{(cTransGp * 100 / cTransBill):F2}%" : "0.00%";
                string cTotGpPct = cTotBill > 0 ? $"{(cTotGp * 100 / cTotBill):F2}%" : "0.00%";

                SetNum(ws.Cell(rowIdx, 4), cTotalBill);
                SetNum(ws.Cell(rowIdx, 5), cComm);
                SetNum(ws.Cell(rowIdx, 6), cOthers);
                SetNum(ws.Cell(rowIdx, 7), cTotCnf);
                SetNum(ws.Cell(rowIdx, 8), cTransBill);
                SetNum(ws.Cell(rowIdx, 9), cTotBill);
                SetNum(ws.Cell(rowIdx, 10), cCostRece);
                SetNum(ws.Cell(rowIdx, 11), cCostNonRece);
                SetNum(ws.Cell(rowIdx, 12), cCnfCosting);
                SetNum(ws.Cell(rowIdx, 13), cCostSwift);
                SetNum(ws.Cell(rowIdx, 14), cCostHair);
                SetNum(ws.Cell(rowIdx, 15), cCostTrans);
                SetNum(ws.Cell(rowIdx, 16), cTotCost);
                SetNum(ws.Cell(rowIdx, 17), cCnfGp);
                SetNum(ws.Cell(rowIdx, 18), cTransGp);
                SetNum(ws.Cell(rowIdx, 19), cTotGp);

                ws.Cell(rowIdx, 20).Value = cCnfGpPct;
                ws.Cell(rowIdx, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(rowIdx, 21).Value = cTransGpPct;
                ws.Cell(rowIdx, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(rowIdx, 22).Value = cTotGpPct;
                ws.Cell(rowIdx, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                var subRowRange = ws.Range(rowIdx, 1, rowIdx, 22);
                subRowRange.Style.Font.Bold = true;
                subRowRange.Style.Font.FontSize = 9;
                subRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                subRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                subRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                rowIdx += 2;
            }

            // Grand Total Row
            if (data.Count > 0)
            {
                var gRange = ws.Range(rowIdx, 1, rowIdx, 3);
                gRange.Merge().Value = "Grand Total:";
                gRange.Style.Font.Bold = true;
                gRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                var gTotalBill = data.Sum(x => x.TotalBill);
                var gComm = data.Sum(x => x.BillingCommission);
                var gOthers = data.Sum(x => x.OthersBill);
                var gTotCnf = data.Sum(x => x.TotalCNFBilling);
                var gTransBill = data.Sum(x => x.BillingTransportBill);
                var gTotBill = data.Sum(x => x.TotalBilling);
                var gCostRece = data.Sum(x => x.CostingReceptableAmount);
                var gCostNonRece = data.Sum(x => x.CostingNonReceptableAmount);
                var gCnfCosting = data.Sum(x => x.CNFCosting);
                var gCostSwift = data.Sum(x => x.CostingSwiftTransportAmount);
                var gCostHair = data.Sum(x => x.CostingHairTransportAmount);
                var gCostTrans = data.Sum(x => x.CostingTransport);
                var gTotCost = data.Sum(x => x.TotalCosting);
                var gCnfGp = data.Sum(x => x.CNFGP);
                var gTransGp = data.Sum(x => x.TransportGP);
                var gTotGp = data.Sum(x => x.TotalGP);
                string gCnfGpPct = gTotCnf > 0 ? $"{(gCnfGp * 100 / gTotCnf):F2}%" : "0.00%";
                string gTransGpPct = gTransBill > 0 ? $"{(gTransGp * 100 / gTransBill):F2}%" : "0.00%";
                string gTotGpPct = gTotBill > 0 ? $"{(gTotGp * 100 / gTotBill):F2}%" : "0.00%";

                SetNum(ws.Cell(rowIdx, 4), gTotalBill);
                SetNum(ws.Cell(rowIdx, 5), gComm);
                SetNum(ws.Cell(rowIdx, 6), gOthers);
                SetNum(ws.Cell(rowIdx, 7), gTotCnf);
                SetNum(ws.Cell(rowIdx, 8), gTransBill);
                SetNum(ws.Cell(rowIdx, 9), gTotBill);
                SetNum(ws.Cell(rowIdx, 10), gCostRece);
                SetNum(ws.Cell(rowIdx, 11), gCostNonRece);
                SetNum(ws.Cell(rowIdx, 12), gCnfCosting);
                SetNum(ws.Cell(rowIdx, 13), gCostSwift);
                SetNum(ws.Cell(rowIdx, 14), gCostHair);
                SetNum(ws.Cell(rowIdx, 15), gCostTrans);
                SetNum(ws.Cell(rowIdx, 16), gTotCost);
                SetNum(ws.Cell(rowIdx, 17), gCnfGp);
                SetNum(ws.Cell(rowIdx, 18), gTransGp);
                SetNum(ws.Cell(rowIdx, 19), gTotGp);

                ws.Cell(rowIdx, 20).Value = gCnfGpPct;
                ws.Cell(rowIdx, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(rowIdx, 21).Value = gTransGpPct;
                ws.Cell(rowIdx, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(rowIdx, 22).Value = gTotGpPct;
                ws.Cell(rowIdx, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                var gRowRange = ws.Range(rowIdx, 1, rowIdx, 22);
                gRowRange.Style.Font.Bold = true;
                gRowRange.Style.Font.FontSize = 9.5;
                gRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#CBD5E1");
                gRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                gRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Column widths (explicit; AdjustToContents is skewed by merged title/customer rows)
            ws.Column(1).Width = 6;    // SN.
            ws.Column(2).Width = 11;   // Billing Date
            ws.Column(3).Width = 24;   // Bill No
            ws.Column(4).Width = 12;   // Total Bill (BDT)

            // Billing
            ws.Column(5).Width = 11;   // Comm.
            ws.Column(6).Width = 11;   // Others
            ws.Column(7).Width = 12;   // C&F TOT
            ws.Column(8).Width = 10;   // Transport
            ws.Column(9).Width = 12;   // TOT Bill

            // Costing
            ws.Column(10).Width = 12;  // Rece
            ws.Column(11).Width = 11;  // NON Rece
            ws.Column(12).Width = 13;  // C&F TOT Cost
            ws.Column(13).Width = 8;  // Swift
            ws.Column(14).Width = 8;  // Hair
            ws.Column(15).Width = 10;  // Transport TOT
            ws.Column(16).Width = 12;  // TOT Cost

            // PNL
            ws.Column(17).Width = 13;  // GP C&F
            ws.Column(18).Width = 12;  // GP TRANS
            ws.Column(19).Width = 13;  // GP TOT
            ws.Column(20).Width = 9;  // GP% C&F
            ws.Column(21).Width = 9;  // GP% TRANS
            ws.Column(22).Width = 9;  // G. GP%

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static void SetNum(IXLCell cell, decimal value)
        {
            cell.Value = value;
            cell.Style.NumberFormat.Format = "#,##0.00";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        #endregion

        //#region Excel Report Generation (ClosedXML)
        //public async Task<byte[]> GenerateExcelReportAsync(GPAnalysisReportFilter filter)
        //{
        //    var data = await ExecuteStoredProcedureAsync(filter);

        //    var customerGroups = data
        //        .GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "Unknown Customer" : x.CustomerName.Trim())
        //        .OrderBy(g => g.Key)
        //        .ToList();

        //    string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
        //        ? $"{filter.FromDate} - {filter.ToDate}" : "All Dates";

        //    using var workbook = new XLWorkbook();
        //    var ws = workbook.Worksheets.Add("GP Analysis Report");

        //    // Page title block
        //    ws.Cell(1, 1).Value = "Formosa Logistics Limited";
        //    ws.Range(1, 1, 1, 22).Merge().Style.Font.Bold = true;
        //    ws.Range(1, 1, 1, 22).Style.Font.FontSize = 14;
        //    ws.Range(1, 1, 1, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        //    ws.Cell(2, 1).Value = "21, Hazaribagh, Dhaka-1209";
        //    ws.Range(2, 1, 2, 22).Merge().Style.Font.FontSize = 10;
        //    ws.Range(2, 1, 2, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        //    ws.Cell(3, 1).Value = "GP Analysis Report";
        //    ws.Range(3, 1, 3, 22).Merge().Style.Font.Bold = true;
        //    ws.Range(3, 1, 3, 22).Style.Font.FontSize = 12;
        //    ws.Range(3, 1, 3, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        //    ws.Cell(4, 1).Value = $"Date : {dateRangeStr}";
        //    ws.Range(4, 1, 4, 22).Merge().Style.Font.Bold = true;
        //    ws.Range(4, 1, 4, 22).Style.Font.FontSize = 10;
        //    ws.Range(4, 1, 4, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        //    // Embed logo at top-left (same as CustomerReport)
        //    var logoBytes = GetLogoBytes();
        //    if (logoBytes != null && logoBytes.Length > 0)
        //    {
        //        ws.Row(1).Height = 20;
        //        ws.Row(2).Height = 16;
        //        ws.Row(3).Height = 18;

        //        try
        //        {
        //            using var logoStream = new MemoryStream(logoBytes);
        //            var logoPicture = ws.AddPicture(logoStream)
        //                .MoveTo(ws.Cell(1, 1), 4, 14);
        //            logoPicture.Width = 90;
        //            logoPicture.Height = 32;
        //        }
        //        catch
        //        {
        //            // Fallback gracefully without throwing if the image can't be embedded
        //        }
        //    }

        //    int rowIdx = 6;

        //    foreach (var group in customerGroups)
        //    {
        //        // Customer row
        //        ws.Cell(rowIdx, 1).Value = $"Customer: {group.Key}";
        //        var custRange = ws.Range(rowIdx, 1, rowIdx, 22);
        //        custRange.Merge();
        //        custRange.Style.Font.Bold = true;
        //        custRange.Style.Font.FontSize = 11;
        //        custRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        //        custRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        //        rowIdx++;

        //        // Table 3-tier header
        //        int h1 = rowIdx;
        //        int h2 = rowIdx + 1;
        //        int h3 = rowIdx + 2;

        //        ws.Range(h1, 1, h3, 1).Merge().Value = "SN.";
        //        ws.Range(h1, 2, h3, 2).Merge().Value = "Billing Date";
        //        ws.Range(h1, 3, h3, 3).Merge().Value = "Bill No";
        //        ws.Range(h1, 4, h3, 4).Merge().Value = "Total Bill (BDT)";

        //        ws.Range(h1, 5, h1, 9).Merge().Value = "Billing";
        //        ws.Range(h1, 10, h1, 16).Merge().Value = "Costing";
        //        ws.Range(h1, 17, h1, 22).Merge().Value = "PNL";

        //        ws.Range(h2, 5, h2, 7).Merge().Value = "C & F";
        //        ws.Range(h2, 8, h3, 8).Merge().Value = "Transport";
        //        ws.Range(h2, 9, h3, 9).Merge().Value = "TOT Bill";

        //        ws.Range(h2, 10, h2, 12).Merge().Value = "C & F Cost";
        //        ws.Range(h2, 13, h2, 15).Merge().Value = "Transport Cost";
        //        ws.Range(h2, 16, h3, 16).Merge().Value = "TOT Cost";

        //        ws.Range(h2, 17, h2, 19).Merge().Value = "GP";
        //        ws.Range(h2, 20, h2, 22).Merge().Value = "GP%";

        //        ws.Cell(h3, 5).Value = "Comm.";
        //        ws.Cell(h3, 6).Value = "Others";
        //        ws.Cell(h3, 7).Value = "TOT";
        //        ws.Cell(h3, 10).Value = "Rece";
        //        ws.Cell(h3, 11).Value = "NON Rece";
        //        ws.Cell(h3, 12).Value = "TOT Cost";
        //        ws.Cell(h3, 13).Value = "Swift";
        //        ws.Cell(h3, 14).Value = "Hair";
        //        ws.Cell(h3, 15).Value = "TOT";
        //        ws.Cell(h3, 17).Value = "C & F";
        //        ws.Cell(h3, 18).Value = "TRANS";
        //        ws.Cell(h3, 19).Value = "TOT";
        //        ws.Cell(h3, 20).Value = "C & F";
        //        ws.Cell(h3, 21).Value = "TRANS";
        //        ws.Cell(h3, 22).Value = "G. GP";

        //        var headerRange = ws.Range(h1, 1, h3, 22);
        //        headerRange.Style.Font.Bold = true;
        //        headerRange.Style.Font.FontSize = 9;
        //        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        //        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        //        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        //        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        //        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        //        rowIdx += 3;

        //        // Data rows
        //        int sl = 1;
        //        foreach (var r in group)
        //        {
        //            ws.Cell(rowIdx, 1).Value = sl++;
        //            ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        //            ws.Cell(rowIdx, 2).Value = r.BillDate;
        //            ws.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        //            ws.Cell(rowIdx, 3).Value = r.BillNo;
        //            ws.Cell(rowIdx, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        //            SetNum(ws.Cell(rowIdx, 4), r.TotalBill);
        //            SetNum(ws.Cell(rowIdx, 5), r.BillingCommission);
        //            SetNum(ws.Cell(rowIdx, 6), r.OthersBill);
        //            SetNum(ws.Cell(rowIdx, 7), r.TotalCNFBilling);
        //            SetNum(ws.Cell(rowIdx, 8), r.BillingTransportBill);
        //            SetNum(ws.Cell(rowIdx, 9), r.TotalBilling);
        //            SetNum(ws.Cell(rowIdx, 10), r.CostingReceptableAmount);
        //            SetNum(ws.Cell(rowIdx, 11), r.CostingNonReceptableAmount);
        //            SetNum(ws.Cell(rowIdx, 12), r.CNFCosting);
        //            SetNum(ws.Cell(rowIdx, 13), r.CostingSwiftTransportAmount);
        //            SetNum(ws.Cell(rowIdx, 14), r.CostingHairTransportAmount);
        //            SetNum(ws.Cell(rowIdx, 15), r.CostingTransport);
        //            SetNum(ws.Cell(rowIdx, 16), r.TotalCosting);
        //            SetNum(ws.Cell(rowIdx, 17), r.CNFGP);
        //            SetNum(ws.Cell(rowIdx, 18), r.TransportGP);
        //            SetNum(ws.Cell(rowIdx, 19), r.TotalGP);

        //            ws.Cell(rowIdx, 20).Value = r.CNFGPPercent ?? "0.00%";
        //            ws.Cell(rowIdx, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //            ws.Cell(rowIdx, 21).Value = r.TransportGPPercent ?? "0.00%";
        //            ws.Cell(rowIdx, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //            ws.Cell(rowIdx, 22).Value = r.TotalGPPercent ?? "0.00%";
        //            ws.Cell(rowIdx, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        //            ws.Range(rowIdx, 1, rowIdx, 22).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        //            ws.Range(rowIdx, 1, rowIdx, 22).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        //            ws.Range(rowIdx, 1, rowIdx, 22).Style.Font.FontSize = 8.5;

        //            rowIdx++;
        //        }

        //        // Customer Subtotal
        //        var subRange = ws.Range(rowIdx, 1, rowIdx, 3);
        //        subRange.Merge().Value = "Customer Total:";
        //        subRange.Style.Font.Bold = true;
        //        subRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        //        var cTotalBill = group.Sum(x => x.TotalBill);
        //        var cComm = group.Sum(x => x.BillingCommission);
        //        var cOthers = group.Sum(x => x.OthersBill);
        //        var cTotCnf = group.Sum(x => x.TotalCNFBilling);
        //        var cTransBill = group.Sum(x => x.BillingTransportBill);
        //        var cTotBill = group.Sum(x => x.TotalBilling);
        //        var cCostRece = group.Sum(x => x.CostingReceptableAmount);
        //        var cCostNonRece = group.Sum(x => x.CostingNonReceptableAmount);
        //        var cCnfCosting = group.Sum(x => x.CNFCosting);
        //        var cCostSwift = group.Sum(x => x.CostingSwiftTransportAmount);
        //        var cCostHair = group.Sum(x => x.CostingHairTransportAmount);
        //        var cCostTrans = group.Sum(x => x.CostingTransport);
        //        var cTotCost = group.Sum(x => x.TotalCosting);
        //        var cCnfGp = group.Sum(x => x.CNFGP);
        //        var cTransGp = group.Sum(x => x.TransportGP);
        //        var cTotGp = group.Sum(x => x.TotalGP);
        //        string cCnfGpPct = cTotCnf > 0 ? $"{(cCnfGp * 100 / cTotCnf):F2}%" : "0.00%";
        //        string cTransGpPct = cTransBill > 0 ? $"{(cTransGp * 100 / cTransBill):F2}%" : "0.00%";
        //        string cTotGpPct = cTotBill > 0 ? $"{(cTotGp * 100 / cTotBill):F2}%" : "0.00%";

        //        SetNum(ws.Cell(rowIdx, 4), cTotalBill);
        //        SetNum(ws.Cell(rowIdx, 5), cComm);
        //        SetNum(ws.Cell(rowIdx, 6), cOthers);
        //        SetNum(ws.Cell(rowIdx, 7), cTotCnf);
        //        SetNum(ws.Cell(rowIdx, 8), cTransBill);
        //        SetNum(ws.Cell(rowIdx, 9), cTotBill);
        //        SetNum(ws.Cell(rowIdx, 10), cCostRece);
        //        SetNum(ws.Cell(rowIdx, 11), cCostNonRece);
        //        SetNum(ws.Cell(rowIdx, 12), cCnfCosting);
        //        SetNum(ws.Cell(rowIdx, 13), cCostSwift);
        //        SetNum(ws.Cell(rowIdx, 14), cCostHair);
        //        SetNum(ws.Cell(rowIdx, 15), cCostTrans);
        //        SetNum(ws.Cell(rowIdx, 16), cTotCost);
        //        SetNum(ws.Cell(rowIdx, 17), cCnfGp);
        //        SetNum(ws.Cell(rowIdx, 18), cTransGp);
        //        SetNum(ws.Cell(rowIdx, 19), cTotGp);

        //        ws.Cell(rowIdx, 20).Value = cCnfGpPct;
        //        ws.Cell(rowIdx, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //        ws.Cell(rowIdx, 21).Value = cTransGpPct;
        //        ws.Cell(rowIdx, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //        ws.Cell(rowIdx, 22).Value = cTotGpPct;
        //        ws.Cell(rowIdx, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        //        var subRowRange = ws.Range(rowIdx, 1, rowIdx, 22);
        //        subRowRange.Style.Font.Bold = true;
        //        subRowRange.Style.Font.FontSize = 9;
        //        subRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        //        subRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        //        subRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        //        rowIdx += 2;
        //    }

        //    // Grand Total Row
        //    if (data.Count > 0)
        //    {
        //        var gRange = ws.Range(rowIdx, 1, rowIdx, 3);
        //        gRange.Merge().Value = "Grand Total:";
        //        gRange.Style.Font.Bold = true;
        //        gRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        //        var gTotalBill = data.Sum(x => x.TotalBill);
        //        var gComm = data.Sum(x => x.BillingCommission);
        //        var gOthers = data.Sum(x => x.OthersBill);
        //        var gTotCnf = data.Sum(x => x.TotalCNFBilling);
        //        var gTransBill = data.Sum(x => x.BillingTransportBill);
        //        var gTotBill = data.Sum(x => x.TotalBilling);
        //        var gCostRece = data.Sum(x => x.CostingReceptableAmount);
        //        var gCostNonRece = data.Sum(x => x.CostingNonReceptableAmount);
        //        var gCnfCosting = data.Sum(x => x.CNFCosting);
        //        var gCostSwift = data.Sum(x => x.CostingSwiftTransportAmount);
        //        var gCostHair = data.Sum(x => x.CostingHairTransportAmount);
        //        var gCostTrans = data.Sum(x => x.CostingTransport);
        //        var gTotCost = data.Sum(x => x.TotalCosting);
        //        var gCnfGp = data.Sum(x => x.CNFGP);
        //        var gTransGp = data.Sum(x => x.TransportGP);
        //        var gTotGp = data.Sum(x => x.TotalGP);
        //        string gCnfGpPct = gTotCnf > 0 ? $"{(gCnfGp * 100 / gTotCnf):F2}%" : "0.00%";
        //        string gTransGpPct = gTransBill > 0 ? $"{(gTransGp * 100 / gTransBill):F2}%" : "0.00%";
        //        string gTotGpPct = gTotBill > 0 ? $"{(gTotGp * 100 / gTotBill):F2}%" : "0.00%";

        //        SetNum(ws.Cell(rowIdx, 4), gTotalBill);
        //        SetNum(ws.Cell(rowIdx, 5), gComm);
        //        SetNum(ws.Cell(rowIdx, 6), gOthers);
        //        SetNum(ws.Cell(rowIdx, 7), gTotCnf);
        //        SetNum(ws.Cell(rowIdx, 8), gTransBill);
        //        SetNum(ws.Cell(rowIdx, 9), gTotBill);
        //        SetNum(ws.Cell(rowIdx, 10), gCostRece);
        //        SetNum(ws.Cell(rowIdx, 11), gCostNonRece);
        //        SetNum(ws.Cell(rowIdx, 12), gCnfCosting);
        //        SetNum(ws.Cell(rowIdx, 13), gCostSwift);
        //        SetNum(ws.Cell(rowIdx, 14), gCostHair);
        //        SetNum(ws.Cell(rowIdx, 15), gCostTrans);
        //        SetNum(ws.Cell(rowIdx, 16), gTotCost);
        //        SetNum(ws.Cell(rowIdx, 17), gCnfGp);
        //        SetNum(ws.Cell(rowIdx, 18), gTransGp);
        //        SetNum(ws.Cell(rowIdx, 19), gTotGp);

        //        ws.Cell(rowIdx, 20).Value = gCnfGpPct;
        //        ws.Cell(rowIdx, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //        ws.Cell(rowIdx, 21).Value = gTransGpPct;
        //        ws.Cell(rowIdx, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //        ws.Cell(rowIdx, 22).Value = gTotGpPct;
        //        ws.Cell(rowIdx, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        //        var gRowRange = ws.Range(rowIdx, 1, rowIdx, 22);
        //        gRowRange.Style.Font.Bold = true;
        //        gRowRange.Style.Font.FontSize = 9.5;
        //        gRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#CBD5E1");
        //        gRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        //        gRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        //    }

        //    ws.Columns().AdjustToContents();

        //    using var ms = new MemoryStream();
        //    workbook.SaveAs(ms);
        //    return ms.ToArray();
        //}

        //private static void SetNum(IXLCell cell, decimal value)
        //{
        //    cell.Value = value;
        //    cell.Style.NumberFormat.Format = "#,##0.00";
        //    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //}
        //#endregion

    }
}
