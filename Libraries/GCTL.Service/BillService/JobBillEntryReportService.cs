using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Core.ViewModels.Common;
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
        private readonly IRepository<GCTL.Data.Models.SepDocumentation>? _sepDocumentationRepository;
        private readonly IRepository<CfDefExpenseHead>? _expHeadRepository;
        private readonly IRepository<RmgProdDefUnitType>? _unitRepository;
        private readonly IRepository<SalesDefPortName>? _portNameRepository;
        private readonly IRepository<CaDefCurrency>? _currencyRepository;

        public JobBillEntryReportService(
            IRepository<CfBillEntry> billEntryRepository,
            IRepository<CfBillEntryDetailsChchead> billChcRepository,
            IRepository<CfBillEntryDetail> billDetailRepository,
            IRepository<SalesCustomer> customerRepository,
            IRepository<CfDefExpenseType> shipmentModeRepository,
            IRepository<GCTL.Data.Models.SepDocumentation>? sepDocumentationRepository = null,
            IRepository<CfDefExpenseHead>? expHeadRepository = null,
            IRepository<RmgProdDefUnitType>? unitRepository = null,
            IRepository<SalesDefPortName>? portNameRepository = null,
            IRepository<CaDefCurrency>? currencyRepository = null)
        {
            _billEntryRepository = billEntryRepository;
            _billChcRepository = billChcRepository;
            _billDetailRepository = billDetailRepository;
            _customerRepository = customerRepository;
            _shipmentModeRepository = shipmentModeRepository;
            _sepDocumentationRepository = sepDocumentationRepository;
            _expHeadRepository = expHeadRepository;
            _unitRepository = unitRepository;
            _portNameRepository = portNameRepository;
            _currencyRepository = currencyRepository;
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

        public async Task<JobBillEntryReportFilterOptionsDTO> GetFilterOptionsAsync(string? fromDate, string? toDate, string? shipmentModes = null, string? customerIds = null)
        {
            var result = new JobBillEntryReportFilterOptionsDTO();
            try
            {
                var query = _billEntryRepository.All().AsNoTracking();
                var (from, to) = ParseDates(fromDate, toDate);
                if (from.HasValue)
                {
                    query = query.Where(b => b.BillDate >= from.Value.Date);
                }
                if (to.HasValue)
                {
                    var nextDay = to.Value.Date.AddDays(1);
                    query = query.Where(b => b.BillDate < nextDay);
                }

                // 1. Shipment Modes from available bills in date range
                var modeIds = await query
                    .Where(b => !string.IsNullOrEmpty(b.ExpenseTypeId))
                    .Select(b => b.ExpenseTypeId)
                    .Distinct()
                    .ToListAsync();

                if (modeIds.Any())
                {
                    var modes = await _shipmentModeRepository.All().AsNoTracking()
                        .Where(m => modeIds.Contains(m.ExpenseTypeId))
                        .Select(m => new CommonSelectModel
                        {
                            Code = m.ExpenseTypeId,
                            Name = !string.IsNullOrEmpty(m.ExpenseType) ? m.ExpenseType : m.ExpenseTypeId
                        })
                        .ToListAsync();

                    var foundIds = new HashSet<string>(modes.Select(m => m.Code), StringComparer.OrdinalIgnoreCase);
                    foreach (var id in modeIds)
                    {
                        if (!foundIds.Contains(id))
                        {
                            modes.Add(new CommonSelectModel { Code = id, Name = id });
                        }
                    }

                    result.ShipmentModes = modes.OrderBy(m => m.Name).ToList();
                }

                // 2. Customers: based on Date range + selected Shipment Mode(s) (if any)
                var custQuery = query;
                var selectedModes = ParseMultiSelect(shipmentModes);
                if (selectedModes.Any() && modeIds.Any(m => selectedModes.Contains(m, StringComparer.OrdinalIgnoreCase)))
                {
                    custQuery = custQuery.Where(b => b.ExpenseTypeId != null && selectedModes.Contains(b.ExpenseTypeId));
                }

                var custIds = await custQuery
                    .Where(b => !string.IsNullOrEmpty(b.CustomerId))
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .ToListAsync();

                if (custIds.Any())
                {
                    var customers = await _customerRepository.All().AsNoTracking()
                        .Where(c => custIds.Contains(c.CustomerId))
                        .Select(c => new CommonSelectModel
                        {
                            Code = c.CustomerId,
                            Name = !string.IsNullOrEmpty(c.CustomerName) ? c.CustomerName : c.CustomerId
                        })
                        .ToListAsync();

                    var foundCustIds = new HashSet<string>(customers.Select(c => c.Code), StringComparer.OrdinalIgnoreCase);
                    foreach (var id in custIds)
                    {
                        if (!foundCustIds.Contains(id))
                        {
                            customers.Add(new CommonSelectModel { Code = id, Name = id });
                        }
                    }

                    result.Customers = customers.OrderBy(c => c.Name).ToList();
                }

                // 3. Bill Nos: based on Date range + selected Shipment Mode(s) (if any) + selected Customer(s) (if any)
                var billQuery = custQuery;
                var selectedCusts = ParseMultiSelect(customerIds);
                if (selectedCusts.Any() && custIds.Any(c => selectedCusts.Contains(c, StringComparer.OrdinalIgnoreCase)))
                {
                    billQuery = billQuery.Where(b => b.CustomerId != null && selectedCusts.Contains(b.CustomerId));
                }

                result.BillNos = await billQuery
                    .Where(b => !string.IsNullOrEmpty(b.BillNo))
                    .Select(b => b.BillNo)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync();

                return result;
            }
            catch
            {
                return result;
            }
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

                // Last service value and date of the most recent bill
                decimal lastVal = 0m;
                string lastServiceDate = "-";
                var latestBill = await query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Tc).FirstOrDefaultAsync();
                if (latestBill != null)
                {
                    lastVal = (latestBill.BillAmount ?? 0m) - (latestBill.SpecialDiscount ?? 0m);
                    if (latestBill.BillDate.HasValue)
                    {
                        var formattedDate = latestBill.BillDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
                        lastServiceDate = !string.IsNullOrEmpty(latestBill.BillNo)
                            ? $"{formattedDate} - {latestBill.BillNo}"
                            : formattedDate;
                    }
                    else if (!string.IsNullOrEmpty(latestBill.BillNo))
                    {
                        lastServiceDate = latestBill.BillNo;
                    }
                }

                return new JobBillEntryReportSummaryDTO
                {
                    TotalBillCount = count,
                    NetServiceValue = netTotal,
                    TotalDueOutstanding = 0.00m, // Default as requested
                    LastServiceValue = lastVal,
                    LastServiceDate = lastServiceDate
                };
            }
            catch
            {
                return new JobBillEntryReportSummaryDTO
                {
                    TotalBillCount = 0,
                    NetServiceValue = 0m,
                    TotalDueOutstanding = 0m,
                    LastServiceValue = 0m,
                    LastServiceDate = "-"
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

                // Fetch expense heads for the paged details
                Dictionary<string, CfDefExpenseHead> headDict = new();
                if (_expHeadRepository != null)
                {
                    var expHeadIds = detailList.Select(d => d.ExpenseHeadId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                    if (expHeadIds.Any())
                    {
                        var heads = await _expHeadRepository.All().AsNoTracking()
                            .Where(h => h.ExpenseHeadId != null && expHeadIds.Contains(h.ExpenseHeadId))
                            .ToListAsync();
                        headDict = heads.ToDictionary(h => h.ExpenseHeadId!, h => h);
                    }
                }

                // Fetch related SepDocumentation and Units for the paged records
                var pagedJobNos = pagedBills.Select(b => b.JobNo).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                Dictionary<string, GCTL.Data.Models.SepDocumentation> docDict = new();
                if (_sepDocumentationRepository != null && pagedJobNos.Any())
                {
                    var docs = await _sepDocumentationRepository.All().AsNoTracking()
                        .Where(d => d.JobNo != null && pagedJobNos.Contains(d.JobNo))
                        .ToListAsync();
                    docDict = docs.GroupBy(d => d.JobNo!).ToDictionary(g => g.Key, g => g.First());
                }

                Dictionary<string, string> unitDict = new();
                if (_unitRepository != null)
                {
                    var units = await _unitRepository.All().AsNoTracking().ToListAsync();
                    unitDict = units.Where(u => !string.IsNullOrEmpty(u.UnitTypId))
                                    .ToDictionary(u => u.UnitTypId, u => u.UnitTypeName ?? u.ShortName ?? u.UnitTypId);
                }

                int sl = pageSize > 0 ? ((pageNumber - 1) * pageSize + 1) : 1;
                var items = new List<JobBillEntryReportItemDTO>();

                foreach (var b in pagedBills)
                {
                    var bBillNo = b.BillNo ?? string.Empty;
                    var bChcs = chcList.Where(c => c.BillNo == bBillNo).ToList();
                    var bDetails = detailList.Where(d => d.BillNo == bBillNo).ToList();

                    decimal receiptable = 0m;
                    decimal nonReceiptable = 0m;
                    decimal transport = 0m;
                    decimal agencyFromHeads = 0m;

                    foreach (var chc in bChcs)
                    {
                        var head = chc.BillHead ?? chc.ChcheadDescription ?? string.Empty;
                        decimal amt = chc.BillAmount ?? 0m;
                        if (amt == 0 && string.IsNullOrWhiteSpace(head)) continue;

                        if (head.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                        {
                            transport += amt;
                        }
                        else if (head.Contains("Agency", StringComparison.OrdinalIgnoreCase))
                        {
                            agencyFromHeads += amt;
                        }
                        else if (string.Equals(chc.IsReceiptable, "Y", StringComparison.OrdinalIgnoreCase))
                        {
                            receiptable += amt;
                        }
                        else
                        {
                            nonReceiptable += amt;
                        }
                    }

                    foreach (var det in bDetails)
                    {
                        headDict.TryGetValue(det.ExpenseHeadId ?? string.Empty, out var eh);
                        var head = !string.IsNullOrWhiteSpace(det.BillHead) ? det.BillHead : (eh?.ExpenseHead ?? string.Empty);
                        decimal amt = det.ExpenseBillAmount ?? det.ActualAmount ?? 0m;
                        if (amt == 0 && string.IsNullOrWhiteSpace(head)) continue;

                        string isRec = eh?.IsReceiptable ?? string.Empty;

                        if (head.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                        {
                            transport += amt;
                        }
                        else if (head.Contains("Agency", StringComparison.OrdinalIgnoreCase))
                        {
                            agencyFromHeads += amt;
                        }
                        else if (string.Equals(isRec, "Y", StringComparison.OrdinalIgnoreCase))
                        {
                            receiptable += amt;
                        }
                        else
                        {
                            nonReceiptable += amt;
                        }
                    }

                    decimal agencyComm = agencyFromHeads > 0 ? agencyFromHeads : (b.MinimumAgencyCommission ?? 0m);
                    decimal total = b.BillAmount ?? 0m;
                    decimal deduction = b.SpecialDiscount ?? 0m;
                    decimal net = total - deduction;

                    docDict.TryGetValue(b.JobNo ?? string.Empty, out var doc);

                    string qUnitId = b.QunitTypeId ?? doc?.Unit1 ?? string.Empty;
                    string qUnitName = unitDict.TryGetValue(qUnitId, out var qun) ? qun : qUnitId;
                    decimal? qty = b.Quantity ?? doc?.Quntity1;
                    string qtyStr = (qty.HasValue && qty.Value != 0)
                        ? (!string.IsNullOrWhiteSpace(qUnitName) ? $"{qty.Value:0.##} {qUnitName}".Trim() : qty.Value.ToString("0.##"))
                        : "-";

                    string wUnitId = b.WunitTypeId ?? doc?.Unit2 ?? string.Empty;
                    string wUnitName = unitDict.TryGetValue(wUnitId, out var wun) ? wun : wUnitId;
                    decimal? weight = b.DocWeight ?? doc?.Quntity2;
                    string weightStr = (weight.HasValue && weight.Value != 0)
                        ? (!string.IsNullOrWhiteSpace(wUnitName) ? $"{weight.Value:0.##} {wUnitName}".Trim() : weight.Value.ToString("0.##"))
                        : "-";

                    items.Add(new JobBillEntryReportItemDTO
                    {
                        Sl = sl++,
                        BillNo = bBillNo,
                        BillDate = b.BillDate.HasValue ? b.BillDate.Value.ToString("dd/MM/yyyy") : "-",
                        JobNo = b.JobNo ?? "-",
                        MaterialDescription = b.MaterialDescription ?? "-",
                        Quantity = qtyStr,
                        Weight = weightStr,
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
            var selectedBills = ParseMultiSelect(filter?.SelectedBillNos);
            IQueryable<CfBillEntry> query;
            if (selectedBills.Any())
            {
                query = _billEntryRepository.All().AsNoTracking()
                    .Where(b => b.BillNo != null && selectedBills.Contains(b.BillNo));
            }
            else
            {
                query = BuildBaseQuery(filter ?? new JobBillEntryReportFilterVM());
            }

            query = query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Tc);
            var bills = await query.ToListAsync();

            if (!bills.Any())
            {
                return JobBillPdfGenerator.GeneratePdf(new List<JobBillPrintVM>());
            }

            var billNos = bills.Select(b => b.BillNo).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            var jobNos = bills.Select(b => b.JobNo).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

            // Load docs
            List<GCTL.Data.Models.SepDocumentation> docs = new();
            if (_sepDocumentationRepository != null && jobNos.Any())
            {
                docs = await _sepDocumentationRepository.All().AsNoTracking()
                    .Where(d => d.JobNo != null && jobNos.Contains(d.JobNo))
                    .ToListAsync();
            }
            var docDict = docs.Where(d => !string.IsNullOrEmpty(d.JobNo)).ToDictionary(d => d.JobNo, d => d);

            // Load customers
            var customers = await _customerRepository.All().AsNoTracking().ToListAsync();
            var custDict = new Dictionary<string, SalesCustomer>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in customers)
            {
                if (!string.IsNullOrWhiteSpace(c.CustomerId))
                    custDict[c.CustomerId.Trim()] = c;
                if (!string.IsNullOrWhiteSpace(c.CustomerCode))
                    custDict[c.CustomerCode.Trim()] = c;
            }

            // Units lookup
            Dictionary<string, string> unitDict = new();
            if (_unitRepository != null)
            {
                var units = await _unitRepository.All().AsNoTracking().ToListAsync();
                unitDict = units.Where(u => !string.IsNullOrEmpty(u.UnitTypId))
                                .ToDictionary(u => u.UnitTypId, u => u.UnitTypeName ?? u.ShortName ?? u.UnitTypId);
            }

            // Currency lookup
            Dictionary<string, string> currDict = new();
            if (_currencyRepository != null)
            {
                var currs = await _currencyRepository.All().AsNoTracking().ToListAsync();
                currDict = currs.Where(c => !string.IsNullOrEmpty(c.CurrencyId))
                                .ToDictionary(c => c.CurrencyId, c => c.ShortName ?? c.CurrencyName ?? c.CurrencyId);
            }

            // Port lookup
            Dictionary<string, string> portDict = new();
            if (_portNameRepository != null)
            {
                var ports = await _portNameRepository.All().AsNoTracking().ToListAsync();
                portDict = ports.Where(p => !string.IsNullOrEmpty(p.PortNameId))
                                .ToDictionary(p => p.PortNameId, p => p.PortName ?? p.ShortName ?? p.PortNameId);
            }

            // CHC items
            var chcList = await _billChcRepository.All().AsNoTracking()
                .Where(c => c.BillNo != null && billNos.Contains(c.BillNo))
                .ToListAsync();

            // Detail items
            var detailList = await _billDetailRepository.All().AsNoTracking()
                .Where(d => d.BillNo != null && billNos.Contains(d.BillNo))
                .ToListAsync();

            // Expense Heads
            Dictionary<string, CfDefExpenseHead> headDict = new();
            if (_expHeadRepository != null)
            {
                var heads = await _expHeadRepository.All().AsNoTracking().ToListAsync();
                headDict = heads.Where(h => !string.IsNullOrEmpty(h.ExpenseHeadId))
                                .ToDictionary(h => h.ExpenseHeadId, h => h);
            }

            var printModels = new List<JobBillPrintVM>();

            foreach (var b in bills)
            {
                var bBillNo = b.BillNo ?? string.Empty;
                var bJobNo = b.JobNo ?? string.Empty;
                docDict.TryGetValue(bJobNo, out var doc);

                string customerCode = (b.CustomerId ?? doc?.CustomerId ?? string.Empty).Trim();
                custDict.TryGetValue(customerCode, out var customer);

                string custName = customer?.CustomerName ?? customer?.CustomerCompany ?? customerCode;
                string custAddress = !string.IsNullOrWhiteSpace(b.Address) ? b.Address : (customer?.CustomerAddress ?? doc?.CustomerAddress ?? string.Empty);

                string qUnitId = b.QunitTypeId ?? doc?.Unit1 ?? string.Empty;
                string qUnitName = unitDict.TryGetValue(qUnitId, out var qun) ? qun : qUnitId;
                decimal? qty = b.Quantity ?? doc?.Quntity1;
                string qtyStr = qty.HasValue ? $"{qty.Value:0.00} {qUnitName}".Trim() : string.Empty;

                string packUnitId = doc?.PackingUnitType ?? string.Empty;
                string packUnitName = unitDict.TryGetValue(packUnitId, out var pun) ? pun : packUnitId;
                decimal? packQty = doc?.PackingQuantity;
                string packQtyStr = packQty.HasValue ? $"{packQty.Value:0.00} {packUnitName}".Trim() : "0.00";

                string wUnitId = b.WunitTypeId ?? doc?.Unit2 ?? string.Empty;
                string wUnitName = unitDict.TryGetValue(wUnitId, out var wun) ? wun : wUnitId;
                decimal? weight = b.DocWeight ?? doc?.Quntity2;
                string weightStr = weight.HasValue ? $"{weight.Value:0.00} {wUnitName}".Trim() : string.Empty;

                string currId = b.InvoiceCurrencyId ?? doc?.InvoiceCurrencyId ?? "USD";
                string currName = currDict.TryGetValue(currId, out var cn) ? cn : currId;

                string destId = b.Destination ?? string.Empty;
                string destName = portDict.TryGetValue(destId, out var dn) ? dn : destId;

                var bChcs = chcList.Where(c => c.BillNo == bBillNo).ToList();
                var bDetails = detailList.Where(d => d.BillNo == bBillNo).ToList();

                var receiptableList = new List<JobBillPrintChargeItem>();
                var nonReceiptableList = new List<JobBillPrintChargeItem>();
                var transportList = new List<JobBillPrintChargeItem>();
                var agencyList = new List<JobBillPrintChargeItem>();

                foreach (var chc in bChcs)
                {
                    var head = chc.BillHead ?? chc.ChcheadDescription ?? string.Empty;
                    decimal amt = chc.BillAmount ?? 0m;
                    if (amt == 0 && string.IsNullOrWhiteSpace(head)) continue;

                    if (head.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                    {
                        transportList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                    else if (head.Contains("Agency", StringComparison.OrdinalIgnoreCase))
                    {
                        agencyList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                    else if (string.Equals(chc.IsReceiptable, "Y", StringComparison.OrdinalIgnoreCase))
                    {
                        receiptableList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                    else
                    {
                        nonReceiptableList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                }

                foreach (var det in bDetails)
                {
                    headDict.TryGetValue(det.ExpenseHeadId ?? string.Empty, out var eh);
                    var head = !string.IsNullOrWhiteSpace(det.BillHead) ? det.BillHead : (eh?.ExpenseHead ?? string.Empty);
                    decimal amt = det.ExpenseBillAmount ?? det.ActualAmount ?? 0m;
                    if (amt == 0 && string.IsNullOrWhiteSpace(head)) continue;

                    string isRec = eh?.IsReceiptable ?? string.Empty;

                    if (head.Contains("Transport", StringComparison.OrdinalIgnoreCase))
                    {
                        transportList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                    else if (head.Contains("Agency", StringComparison.OrdinalIgnoreCase))
                    {
                        agencyList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                    else if (string.Equals(isRec, "Y", StringComparison.OrdinalIgnoreCase))
                    {
                        receiptableList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                    else
                    {
                        nonReceiptableList.Add(new JobBillPrintChargeItem { Particulars = head, Amount = amt });
                    }
                }

                decimal agComm = b.MinimumAgencyCommission ?? 0m;
                if (agComm > 0 && !agencyList.Any())
                {
                    agencyList.Add(new JobBillPrintChargeItem { Particulars = "Agency Commission", Amount = agComm });
                }

                decimal recSub = receiptableList.Sum(x => x.Amount);
                decimal nonRecSub = nonReceiptableList.Sum(x => x.Amount);
                decimal transSub = transportList.Sum(x => x.Amount);
                decimal agencySub = agencyList.Sum(x => x.Amount);

                decimal grandTotal = b.BillAmount ?? (recSub + nonRecSub + transSub + agencySub);
                decimal deductions = b.SpecialDiscount ?? 0m;
                decimal netDue = grandTotal - deductions;

                printModels.Add(new JobBillPrintVM
                {
                    BillNo = b.BillNo ?? string.Empty,
                    BillDate = b.BillDate.HasValue ? b.BillDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                    JobNo = b.JobNo ?? (doc?.JobNo ?? string.Empty),
                    CustomerName = custName,
                    CustomerAddress = custAddress,
                    MaterialDescription = b.MaterialDescription ?? (doc?.MaterialDescription ?? string.Empty),
                    Quantity = qtyStr,
                    PackingQuantity = packQtyStr,
                    BlNo = b.Blno ?? (doc?.BillNo ?? string.Empty),
                    BlDate = b.Bldate.HasValue ? b.Bldate.Value.ToString("dd/MM/yyyy") : (doc?.Bldate.HasValue == true ? doc.Bldate.Value.ToString("dd/MM/yyyy") : string.Empty),
                    BeNo = b.Beno ?? (doc?.Beno ?? string.Empty),
                    BeDate = b.Bedate.HasValue ? b.Bedate.Value.ToString("dd/MM/yyyy") : (doc?.Bedate.HasValue == true ? doc.Bedate.Value.ToString("dd/MM/yyyy") : string.Empty),
                    Weight = weightStr,
                    InvoiceNo = b.Invno ?? (doc?.InvoiceNo ?? string.Empty),
                    InvoiceDate = b.Invdate.HasValue ? b.Invdate.Value.ToString("dd/MM/yyyy") : (doc?.InvoiceDate.HasValue == true ? doc.InvoiceDate.Value.ToString("dd/MM/yyyy") : string.Empty),
                    ExpNo = doc?.ExpNo ?? string.Empty,
                    ExpDate = doc?.ExpDate.HasValue == true ? doc.ExpDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                    HawbNo = doc?.Hawb ?? string.Empty,
                    Destination = destName,
                    DocRecvDate = b.DocRecvDate.HasValue ? b.DocRecvDate.Value.ToString("dd/MM/yyyy") : (doc?.DocReceivedDate.HasValue == true ? doc.DocReceivedDate.Value.ToString("dd/MM/yyyy") : string.Empty),
                    ArrivalDate = b.ArrivalDate.HasValue ? b.ArrivalDate.Value.ToString("dd/MM/yyyy") : (doc?.Ata.HasValue == true ? doc.Ata.Value.ToString("dd/MM/yyyy") : string.Empty),
                    DeliveryDate = b.DeliveryDate.HasValue ? b.DeliveryDate.Value.ToString("dd/MM/yyyy") : (doc?.DateOfClearence.HasValue == true ? doc.DateOfClearence.Value.ToString("dd/MM/yyyy") : string.Empty),
                    LcNo = doc?.Lcno ?? (doc?.Lcano ?? string.Empty),
                    LcDate = doc?.Lcdate.HasValue == true ? doc.Lcdate.Value.ToString("dd/MM/yyyy") : string.Empty,
                    Currency = currName,
                    InvoiceValue = b.InvoiceValue ?? doc?.InvoiceValue,
                    AssessableValue = b.AssessableValue ?? doc?.AssessableValue,
                    ReceiptableItems = receiptableList,
                    ReceiptableSubtotal = recSub,
                    NonReceiptableItems = nonReceiptableList,
                    NonReceiptableSubtotal = nonRecSub,
                    TransportItems = transportList,
                    TransportSubtotal = transSub,
                    AgencyCommissionItems = agencyList,
                    AgencyCommissionSubtotal = agencySub,
                    TotalAmount = grandTotal,
                    Deductions = deductions > 0 ? deductions : null,
                    NetDue = netDue,
                    Remarks = b.Remarks ?? string.Empty,
                    InWords = JobBillPdfGenerator.ConvertToWords(netDue)
                });
            }

            return JobBillPdfGenerator.GeneratePdf(printModels);
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
                var nextDay = toDate.Value.Date.AddDays(1);
                query = query.Where(b => b.BillDate < nextDay);
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
