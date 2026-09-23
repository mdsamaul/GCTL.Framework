using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GCTL.Service.Pagination;

namespace GCTL.Service.BillService
{
    public class JobBillEntryReportService : IJobBillEntryReportService
    {
        private readonly IRepository<CfBillEntry> _billEntryRepository;
        private readonly IRepository<CfBillEntryDetailsChchead> _billChcRepository;
        private readonly IRepository<CfBillEntryDetail> _billDetailRepository;
        private readonly IRepository<SalesCustomer> _customerRepository;
        private readonly IRepository<CfDefExpenseType> _shipmentModeRepository;

        public JobBillEntryReportService(
            IRepository<CfBillEntry> billEntryRepository,
            IRepository<CfBillEntryDetailsChchead> billChcRepository,
            IRepository<CfBillEntryDetail> billDetailRepository,
            IRepository<SalesCustomer> customerRepository,
            IRepository<CfDefExpenseType> shipmentModeRepository)
        {
            _billEntryRepository = billEntryRepository;
            _billChcRepository = billChcRepository;
            _billDetailRepository = billDetailRepository;
            _customerRepository = customerRepository;
            _shipmentModeRepository = shipmentModeRepository;
        }

        public async Task<List<string>> GetBillNumbersAsync()
        {
            return await _billEntryRepository.All().AsNoTracking()
                .Where(b => !string.IsNullOrEmpty(b.BillNo))
                .Select(b => b.BillNo)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        public async Task<JobBillEntryReportSummaryDTO> GetSummaryAsync(JobBillEntryReportFilterVM filter)
        {
            try
            {
                var query = BuildBaseQuery(filter);

                int count = await query.CountAsync();
                decimal netTotal = 0m;
                if (count > 0)
                {
                    netTotal = await query.SumAsync(b => (b.BillAmount ?? 0m) - (b.SpecialDiscount ?? 0m));
                }

                // Last service value of the most recent bill
                decimal lastVal = 0m;
                var latestBill = await query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Tc).FirstOrDefaultAsync();
                if (latestBill != null)
                {
                    lastVal = (latestBill.BillAmount ?? 0m) - (latestBill.SpecialDiscount ?? 0m);
                }

                return new JobBillEntryReportSummaryDTO
                {
                    TotalBillCount = count,
                    NetServiceValue = netTotal,
                    TotalDueOutstanding = 0.00m, // Default as requested
                    LastServiceValue = lastVal
                };
            }
            catch
            {
                return new JobBillEntryReportSummaryDTO
                {
                    TotalBillCount = 0,
                    NetServiceValue = 0m,
                    TotalDueOutstanding = 0m,
                    LastServiceValue = 0m
                };
            }
        }

        public async Task<PaginationResponse<JobBillEntryReportItemDTO>> GetGridDataAsync(JobBillEntryReportFilterVM filter)
        {
            try
            {
                int totalRecords = await _billEntryRepository.All().CountAsync();
                var query = BuildBaseQuery(filter);
                query = query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Tc);

                // Determine paging
                int pageSize = 10;
                int pageNumber = 1;
                if (filter.Length.HasValue)
                {
                    if (filter.Length.Value == -1)
                    {
                        pageSize = -1;
                        pageNumber = 1;
                    }
                    else if (filter.Length.Value > 0)
                    {
                        pageSize = filter.Length.Value;
                        pageNumber = (filter.Start ?? 0) / pageSize + 1;
                    }
                }
                else if (filter.PageSize != 0)
                {
                    pageSize = filter.PageSize;
                    pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
                }

                // Map client sort column name to entity property name
                string? mappedSortCol = filter.SortColumn?.ToLower() switch
                {
                    "totalamount" or "billamount" => nameof(CfBillEntry.BillAmount),
                    "deduction" or "specialdiscount" => nameof(CfBillEntry.SpecialDiscount),
                    "weight" or "docweight" => nameof(CfBillEntry.DocWeight),
                    "quantity" => nameof(CfBillEntry.Quantity),
                    "billno" => nameof(CfBillEntry.BillNo),
                    "billdate" or "date" => nameof(CfBillEntry.BillDate),
                    "jobno" => nameof(CfBillEntry.JobNo),
                    "materialdescription" => nameof(CfBillEntry.MaterialDescription),
                    _ => filter.SortColumn
                };

                var paginationRequest = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = filter.SearchTerm,
                    SortColumn = mappedSortCol,
                    SortOrder = filter.SortOrder
                };

                // Use DeferredPaginationService for high-performance index-seek pagination over 17,000+ records
                var paginatedResult = await DeferredPaginationService.GetPaginatedDataAsync(query, paginationRequest, b => b.Tc);
                var pagedBills = paginatedResult.Data?.ToList() ?? new List<CfBillEntry>();
                var pagedBillNos = pagedBills.Select(b => b.BillNo).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

                // Fetch related CHC heads and Details in batch only for the paged 10 records
                var chcList = await _billChcRepository.All().AsNoTracking()
                    .Where(c => c.BillNo != null && pagedBillNos.Contains(c.BillNo))
                    .ToListAsync();

                var detailList = await _billDetailRepository.All().AsNoTracking()
                    .Where(d => d.BillNo != null && pagedBillNos.Contains(d.BillNo))
                    .ToListAsync();

                int sl = pageSize > 0 ? ((pageNumber - 1) * pageSize + 1) : 1;
                var items = new List<JobBillEntryReportItemDTO>();

                foreach (var b in pagedBills)
                {
                    var bBillNo = b.BillNo ?? string.Empty;
                    var bChcs = chcList.Where(c => c.BillNo == bBillNo).ToList();
                    var bDetails = detailList.Where(d => d.BillNo == bBillNo).ToList();

                    decimal receiptable = bChcs.Where(c => string.Equals(c.IsReceiptable, "Y", StringComparison.OrdinalIgnoreCase))
                                               .Sum(c => c.BillAmount ?? 0m);

                    decimal nonReceiptable = bChcs.Where(c => string.Equals(c.IsReceiptable, "N", StringComparison.OrdinalIgnoreCase))
                                                  .Sum(c => c.BillAmount ?? 0m);

                    decimal transportFromChc = bChcs.Where(c => !string.IsNullOrEmpty(c.BillHead) && c.BillHead.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                                                    .Sum(c => c.BillAmount ?? 0m);

                    decimal transportFromDetail = bDetails.Where(d => !string.IsNullOrEmpty(d.BillHead) && d.BillHead.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                                                          .Sum(d => d.ActualAmount ?? d.ExpenseBillAmount ?? 0m);

                    decimal transport = transportFromChc > 0 ? transportFromChc : transportFromDetail;
                    decimal agencyComm = b.MinimumAgencyCommission ?? 0m;
                    decimal total = b.BillAmount ?? 0m;
                    decimal deduction = b.SpecialDiscount ?? 0m;
                    decimal net = total - deduction;

                    items.Add(new JobBillEntryReportItemDTO
                    {
                        Sl = sl++,
                        BillNo = bBillNo,
                        BillDate = b.BillDate.HasValue ? b.BillDate.Value.ToString("dd/MM/yyyy") : "-",
                        JobNo = b.JobNo ?? "-",
                        MaterialDescription = b.MaterialDescription ?? "-",
                        Quantity = b.Quantity.HasValue ? b.Quantity.Value.ToString("0.##") : "-",
                        Weight = b.DocWeight.HasValue ? b.DocWeight.Value.ToString("0.##") : "-",
                        ReceiptableAmount = receiptable,
                        NonReceiptableAmount = nonReceiptable,
                        TransportAmount = transport,
                        AgencyCommission = agencyComm,
                        TotalAmount = total,
                        Deduction = deduction,
                        NetAmount = net
                    });
                }

                return new PaginationResponse<JobBillEntryReportItemDTO>
                {
                    Data = items,
                    TotalCount = totalRecords,
                    FilteredCount = paginatedResult.FilteredCount,
                    PaginationInfo = paginatedResult.PaginationInfo == null ? null : new PaginationResponse<JobBillEntryReportItemDTO>.PaginationInfo2
                    {
                        StartItem = paginatedResult.PaginationInfo.StartItem,
                        EndItem = paginatedResult.PaginationInfo.EndItem,
                        TotalItems = paginatedResult.PaginationInfo.TotalItems,
                        TotalPages = paginatedResult.PaginationInfo.TotalPages,
                        CurrentPage = paginatedResult.PaginationInfo.CurrentPage
                    }
                };
            }
            catch (Exception)
            {
                return new PaginationResponse<JobBillEntryReportItemDTO>
                {
                    Data = new List<JobBillEntryReportItemDTO>(),
                    TotalCount = 0,
                    FilteredCount = 0
                };
            }
        }

        public async Task<byte[]> GeneratePdfAsync(JobBillEntryReportFilterVM filter)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var selectedBills = ParseMultiSelect(filter.SelectedBillNos);
            IQueryable<CfBillEntry> query;
            if (selectedBills.Any())
            {
                // If specific bills were checked, print only those selected bills
                query = _billEntryRepository.All().AsNoTracking()
                    .Where(b => b.BillNo != null && selectedBills.Contains(b.BillNo));
            }
            else
            {
                // If none selected, print all matching the filter criteria
                query = BuildBaseQuery(filter);
            }

            query = query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Tc);

            var bills = await query.ToListAsync();
            var billNos = bills.Select(b => b.BillNo).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

            var chcList = await _billChcRepository.All().AsNoTracking()
                .Where(c => c.BillNo != null && billNos.Contains(c.BillNo))
                .ToListAsync();

            var detailList = await _billDetailRepository.All().AsNoTracking()
                .Where(d => d.BillNo != null && billNos.Contains(d.BillNo))
                .ToListAsync();

            int sl = 1;
            var reportRows = new List<JobBillEntryReportItemDTO>();
            foreach (var b in bills)
            {
                var bBillNo = b.BillNo ?? string.Empty;
                var bChcs = chcList.Where(c => c.BillNo == bBillNo).ToList();
                var bDetails = detailList.Where(d => d.BillNo == bBillNo).ToList();

                decimal receiptable = bChcs.Where(c => string.Equals(c.IsReceiptable, "Y", StringComparison.OrdinalIgnoreCase))
                                           .Sum(c => c.BillAmount ?? 0m);

                decimal nonReceiptable = bChcs.Where(c => string.Equals(c.IsReceiptable, "N", StringComparison.OrdinalIgnoreCase))
                                              .Sum(c => c.BillAmount ?? 0m);

                decimal transport = bChcs.Where(c => !string.IsNullOrEmpty(c.BillHead) && c.BillHead.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                                         .Sum(c => c.BillAmount ?? 0m);
                if (transport == 0)
                {
                    transport = bDetails.Where(d => !string.IsNullOrEmpty(d.BillHead) && d.BillHead.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                                        .Sum(d => d.ActualAmount ?? d.ExpenseBillAmount ?? 0m);
                }

                decimal total = b.BillAmount ?? 0m;
                decimal deduction = b.SpecialDiscount ?? 0m;

                reportRows.Add(new JobBillEntryReportItemDTO
                {
                    Sl = sl++,
                    BillNo = bBillNo,
                    BillDate = b.BillDate.HasValue ? b.BillDate.Value.ToString("dd/MM/yyyy") : "-",
                    JobNo = b.JobNo ?? "-",
                    MaterialDescription = b.MaterialDescription ?? "-",
                    Quantity = b.Quantity.HasValue ? b.Quantity.Value.ToString("0.##") : "-",
                    Weight = b.DocWeight.HasValue ? b.DocWeight.Value.ToString("0.##") : "-",
                    ReceiptableAmount = receiptable,
                    NonReceiptableAmount = nonReceiptable,
                    TransportAmount = transport,
                    AgencyCommission = b.MinimumAgencyCommission ?? 0m,
                    TotalAmount = total,
                    Deduction = deduction,
                    NetAmount = total - deduction
                });
            }

            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date: {filter.FromDate} to {filter.ToDate}"
                : $"Generated: {DateTime.Now:dd/MM/yyyy}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.MarginHorizontal(12);
                    page.MarginVertical(15);
                    page.Size(PageSizes.A4.Landscape());

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Formosa Logistics Limited").FontSize(14).Bold();
                        col.Item().AlignCenter().Text("21, Hazaribagh, Dhaka-1209").FontSize(9).FontColor(Colors.Grey.Darken2);
                        col.Item().AlignCenter().Text("Job Bill Entry Report").FontSize(11).Bold().Underline();
                        col.Item().PaddingTop(3).PaddingBottom(4).Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text($"Total Records: {reportRows.Count}").FontSize(8.5f).Bold();
                            row.RelativeItem().AlignRight().Text(dateRangeStr).FontSize(8.5f);
                        });
                    });

                    // Content Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.2f); // SL
                            cols.RelativeColumn(3.0f); // Bill No
                            cols.RelativeColumn(2.5f); // Date
                            cols.RelativeColumn(3.0f); // Job No
                            cols.RelativeColumn(4.0f); // Material Desc
                            cols.RelativeColumn(2.0f); // Qty
                            cols.RelativeColumn(2.0f); // Weight
                            cols.RelativeColumn(2.8f); // Receiptable
                            cols.RelativeColumn(2.8f); // Non-Receiptable
                            cols.RelativeColumn(2.5f); // Transport
                            cols.RelativeColumn(2.5f); // Agency Comm
                            cols.RelativeColumn(2.8f); // Total
                            cols.RelativeColumn(2.4f); // Deduction
                            cols.RelativeColumn(2.8f); // Net Amount
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            void BuildHCell(string text)
                            {
                                header.Cell().Element(c => c
                                    .Border(0.5f)
                                    .BorderColor(Colors.Grey.Darken1)
                                    .Background(Colors.Grey.Lighten3)
                                    .Padding(2)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text(text).FontSize(6.5f).Bold());
                            }

                            BuildHCell("#");
                            BuildHCell("Bill No");
                            BuildHCell("Date");
                            BuildHCell("Job No");
                            BuildHCell("Material Desc.");
                            BuildHCell("Quantity");
                            BuildHCell("Weight");
                            BuildHCell("Receiptable");
                            BuildHCell("Non-Rec.");
                            BuildHCell("Transport");
                            BuildHCell("Agency Comm.");
                            BuildHCell("Total Amt");
                            BuildHCell("Deduction");
                            BuildHCell("Net Amt");
                        });

                        // Table Rows
                        foreach (var row in reportRows)
                        {
                            void BuildCell(string text, bool right = false, bool center = false)
                            {
                                table.Cell().Element(c => c
                                    .Border(0.5f)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(2)
                                    .AlignMiddle()
                                    .Element(e => right ? e.AlignRight() : (center ? e.AlignCenter() : e.AlignLeft()))
                                    .Text(text ?? "").FontSize(6f));
                            }

                            BuildCell(row.Sl.ToString(), center: true);
                            BuildCell(row.BillNo);
                            BuildCell(row.BillDate, center: true);
                            BuildCell(row.JobNo);
                            BuildCell(row.MaterialDescription);
                            BuildCell(row.Quantity, right: true);
                            BuildCell(row.Weight, right: true);
                            BuildCell(row.ReceiptableAmount.ToString("N2"), right: true);
                            BuildCell(row.NonReceiptableAmount.ToString("N2"), right: true);
                            BuildCell(row.TransportAmount.ToString("N2"), right: true);
                            BuildCell(row.AgencyCommission.ToString("N2"), right: true);
                            BuildCell(row.TotalAmount.ToString("N2"), right: true);
                            BuildCell(row.Deduction.ToString("N2"), right: true);
                            BuildCell(row.NetAmount.ToString("N2"), right: true);
                        }
                    });

                    // Footer
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text($"Print Datetime: {DateTime.Now:dd/MM/yyyy hh:mm:ss tt}").FontSize(7).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(txt =>
                        {
                            txt.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                            txt.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            txt.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium);
                            txt.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private IQueryable<CfBillEntry> BuildBaseQuery(JobBillEntryReportFilterVM filter)
        {
            var query = _billEntryRepository.All().AsNoTracking();

            // Date Filter
            var (fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.BillDate >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(b => b.BillDate <= endOfDay);
            }

            // Shipment Mode Filter (Multi-select)
            var modes = ParseMultiSelect(filter.ShipmentModes);
            if (modes.Any())
            {
                query = query.Where(b => b.ExpenseTypeId != null && modes.Contains(b.ExpenseTypeId));
            }

            // Customer Filter (Multi-select)
            var customers = ParseMultiSelect(filter.CustomerIds);
            if (customers.Any())
            {
                query = query.Where(b => b.CustomerId != null && customers.Contains(b.CustomerId));
            }

            // Bill No Filter (Multi-select)
            var billNos = ParseMultiSelect(filter.BillNos);
            if (billNos.Any())
            {
                query = query.Where(b => b.BillNo != null && billNos.Contains(b.BillNo));
            }

            // Global Search Term (DataTables input)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(b => (b.BillNo != null && b.BillNo.Contains(term)) ||
                                         (b.JobNo != null && b.JobNo.Contains(term)) ||
                                         (b.MaterialDescription != null && b.MaterialDescription.Contains(term)));
            }

            return query;
        }

        private static IQueryable<CfBillEntry> ApplySorting(IQueryable<CfBillEntry> query, string? sortColumn, string? sortOrder)
        {
            bool isAsc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

            return (sortColumn?.ToLower()) switch
            {
                "billno" => isAsc ? query.OrderBy(b => b.BillNo).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.BillNo).ThenByDescending(b => b.Tc),
                "billdate" or "date" => isAsc ? query.OrderBy(b => b.BillDate).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Tc),
                "jobno" => isAsc ? query.OrderBy(b => b.JobNo).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.JobNo).ThenByDescending(b => b.Tc),
                "materialdescription" => isAsc ? query.OrderBy(b => b.MaterialDescription).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.MaterialDescription).ThenByDescending(b => b.Tc),
                "quantity" => isAsc ? query.OrderBy(b => b.Quantity).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.Quantity).ThenByDescending(b => b.Tc),
                "weight" or "docweight" => isAsc ? query.OrderBy(b => b.DocWeight).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.DocWeight).ThenByDescending(b => b.Tc),
                "totalamount" or "billamount" => isAsc ? query.OrderBy(b => b.BillAmount).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.BillAmount).ThenByDescending(b => b.Tc),
                "deduction" or "specialdiscount" => isAsc ? query.OrderBy(b => b.SpecialDiscount).ThenByDescending(b => b.Tc) : query.OrderByDescending(b => b.SpecialDiscount).ThenByDescending(b => b.Tc),
                _ => query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Tc)
            };
        }

        private static List<string> ParseMultiSelect(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<string>();
            return input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
        }

        private static (DateTime? fromDate, DateTime? toDate) ParseDates(string? fromStr, string? toStr)
        {
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy" };
            DateTime? f = null;
            DateTime? t = null;

            if (!string.IsNullOrWhiteSpace(fromStr))
            {
                if (DateTime.TryParseExact(fromStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pf))
                    f = pf;
                else if (DateTime.TryParse(fromStr.Trim(), out var df))
                    f = df;
            }

            if (!string.IsNullOrWhiteSpace(toStr))
            {
                if (DateTime.TryParseExact(toStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pt))
                    t = pt;
                else if (DateTime.TryParse(toStr.Trim(), out var dt))
                    t = dt;
            }

            return (f, t);
        }
    }
}
