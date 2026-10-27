using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.CustomerRateReport;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.CustomerRateReport
{
    public class CustomerRateReportService : ICustomerRateReportService
    {
        private readonly IRepository<SalesCustomer> _customerRepo;
        private readonly IRepository<CustomerWiseCostingEntry> _cwcRepo;
        private readonly IRepository<CustomerWiseCostingDetailsEntry> _cwcdeRepo;
        private readonly IRepository<CustomerHandilingCharge> _chcRepo;
        private readonly IRepository<SalesContactPerson> _contactPersonRepo;
        private readonly IRepository<HrmDefDesignation> _designationRepo;

        public CustomerRateReportService(
            IRepository<SalesCustomer> customerRepo,
            IRepository<CustomerWiseCostingEntry> cwcRepo,
            IRepository<CustomerWiseCostingDetailsEntry> cwcdeRepo,
            IRepository<CustomerHandilingCharge> chcRepo,
            IRepository<SalesContactPerson> contactPersonRepo,
            IRepository<HrmDefDesignation> designationRepo)
        {
            _customerRepo = customerRepo;
            _cwcRepo = cwcRepo;
            _cwcdeRepo = cwcdeRepo;
            _chcRepo = chcRepo;
            _contactPersonRepo = contactPersonRepo;
            _designationRepo = designationRepo;
        }

        public async Task<List<CommonSelectModel>> GetCustomerListAsync()
        {
            return await _customerRepo.All().AsNoTracking()
                .OrderBy(c => c.CustomerName)
                .Select(c => new CommonSelectModel
                {
                    Code = c.CustomerId,
                    Name = c.CustomerName
                })
                .ToListAsync();
        }

        public async Task<(List<CustomerRateReportCustomerRowDTO> Data, int TotalCount, int FilteredCount)> GetGridDataAsync(CustomerRateReportFilterVM filter)
        {
            filter ??= new CustomerRateReportFilterVM();
            var customerIds = ParseCustomerIds(filter.CustomerIds, filter.CustomerId);

            if (!customerIds.Any())
            {
                customerIds = await _customerRepo.All().AsNoTracking()
                    .Where(c => !string.IsNullOrEmpty(c.CustomerId))
                    .OrderBy(c => c.CustomerId)
                    .Select(c => c.CustomerId)
                    .ToListAsync();
            }

            if (!customerIds.Any())
            {
                return (new List<CustomerRateReportCustomerRowDTO>(), 0, 0);
            }

            var rows = await BuildCustomerRowsAsync(customerIds);

            int totalCount = rows.Count;
            var query = rows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(x =>
                    x.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CustomerId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CustomerAddress.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Fax.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.ContactPerson.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.RateItems.Any(r => r.Particulars.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            var list = query.OrderBy(x => x.CustomerId, StringComparer.OrdinalIgnoreCase).ToList();
            int filteredCount = list.Count;

            if (filter.Length.HasValue && filter.Length.Value > 0)
            {
                int start = filter.Start ?? 0;
                list = list.Skip(start).Take(filter.Length.Value).ToList();
            }

            return (list, totalCount, filteredCount);
        }

        public async Task<CustomerRateReportSummaryDTO> GetSummaryAsync(string? customerIds)
        {
            var summary = new CustomerRateReportSummaryDTO();
            var parsedIds = ParseCustomerIds(customerIds, null);
            if (!parsedIds.Any())
            {
                parsedIds = await _customerRepo.All().AsNoTracking()
                    .Where(c => !string.IsNullOrEmpty(c.CustomerId))
                    .Select(c => c.CustomerId)
                    .ToListAsync();
            }

            if (!parsedIds.Any())
                return summary;

            summary.TotalCustomers = parsedIds.Count;

            var costings = await _cwcRepo.All().AsNoTracking()
                .Where(c => parsedIds.Contains(c.CustomerId))
                .ToListAsync();

            var latestCostings = costings
                .GroupBy(c => c.CustomerId)
                .Select(g => g.OrderByDescending(x => x.Tc).First())
                .ToList();

            var costingIds = latestCostings.Select(x => x.CostingId).Where(x => !string.IsNullOrEmpty(x)).ToList();

            var details = await _cwcdeRepo.All().AsNoTracking()
                .Where(d => (costingIds.Contains(d.CostingId) || parsedIds.Contains(d.CustomerId)))
                .ToListAsync();

            summary.TotalChcItems = details.Count;
            summary.TotalHandlingAmount = details.Sum(d => d.BillAmount ?? 0m);
            summary.MinAgencyCommission = latestCostings.Sum(c => c.MinimumAgencyCommission);
            summary.OnInvoiceValue = latestCostings.Any() ? latestCostings.Average(c => c.OnInvoiceValue) : 0m;

            return summary;
        }

        public async Task<byte[]> GeneratePdfAsync(CustomerRateReportFilterVM filter)
        {
            filter ??= new CustomerRateReportFilterVM();
            var customerIds = ParseCustomerIds(filter.CustomerIds, filter.CustomerId);

            var pdfModel = new CustomerRateReportPdfModel
            {
                PrintDate = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")
            };

            if (customerIds.Any())
            {
                var customers = await _customerRepo.All().AsNoTracking()
                    .Where(c => customerIds.Contains(c.CustomerId))
                    .ToDictionaryAsync(c => c.CustomerId, StringComparer.OrdinalIgnoreCase);

                var chcList = await _chcRepo.All().AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.Chcid))
                    .ToListAsync();
                var chcDict = chcList
                    .GroupBy(x => x.Chcid)
                    .ToDictionary(g => g.Key, g => g.First().Chchead ?? g.Key);

                foreach (var custId in customerIds)
                {
                    customers.TryGetValue(custId, out var customer);

                    var costing = await _cwcRepo.All().AsNoTracking()
                        .OrderByDescending(c => c.Tc)
                        .FirstOrDefaultAsync(c => c.CustomerId == custId);

                    var detailsList = await _cwcdeRepo.All().AsNoTracking()
                        .Where(d => (costing != null && d.CostingId == costing.CostingId) || d.CustomerId == custId)
                        .ToListAsync();

                    var groupModel = new CustomerRateReportCustomerGroupPdfModel
                    {
                        CustomerId = custId,
                        CustomerName = customer?.CustomerName ?? custId,
                        CustomerAddress = customer?.CustomerAddress ?? string.Empty,
                        MinAgencyCommission = costing?.MinimumAgencyCommission ?? 0m,
                        OnInvoiceValue = costing?.OnInvoiceValue ?? 0m,
                        TotalHandlingAmount = detailsList.Sum(d => d.BillAmount ?? 0m)
                    };

                    // Section A: Custom Handling Charge
                    groupModel.Items.Add(new CustomerRateReportItemPdfModel
                    {
                        Section = "A.",
                        Particulars = "Custom Handling Charge:",
                        Amount = string.Empty,
                        Remarks = string.Empty,
                        IsSectionHeader = true
                    });

                    foreach (var detail in detailsList)
                    {
                        string headName = (!string.IsNullOrEmpty(detail.Chcid) && chcDict.TryGetValue(detail.Chcid, out var h))
                            ? h
                            : (detail.Chcid ?? "-");

                        groupModel.Items.Add(new CustomerRateReportItemPdfModel
                        {
                            Section = string.Empty,
                            Particulars = headName,
                            Amount = detail.BillAmount.HasValue ? detail.BillAmount.Value.ToString("#,##0.00", CultureInfo.InvariantCulture) : "0.00",
                            Remarks = string.Empty,
                            IsSectionHeader = false
                        });
                    }

                    // Section B: Agency Commission
                    groupModel.Items.Add(new CustomerRateReportItemPdfModel
                    {
                        Section = "B.",
                        Particulars = "Agency Commission:",
                        Amount = string.Empty,
                        Remarks = string.Empty,
                        IsSectionHeader = true
                    });

                    groupModel.Items.Add(new CustomerRateReportItemPdfModel
                    {
                        Section = string.Empty,
                        Particulars = "Minimum Agency Commission",
                        Amount = costing != null ? costing.MinimumAgencyCommission.ToString("#,##0.00", CultureInfo.InvariantCulture) : "0.00",
                        Remarks = string.Empty,
                        IsSectionHeader = false
                    });

                    groupModel.Items.Add(new CustomerRateReportItemPdfModel
                    {
                        Section = string.Empty,
                        Particulars = "On Invoice Value",
                        Amount = costing != null ? $"{costing.OnInvoiceValue.ToString("0.00", CultureInfo.InvariantCulture)}%" : "0.00%",
                        Remarks = string.Empty,
                        IsSectionHeader = false
                    });

                    pdfModel.CustomerGroups.Add(groupModel);
                }
            }

            return CustomerRateReportPdfGenerator.GeneratePdf(pdfModel);
        }

        public async Task<byte[]> GenerateExcelAsync(CustomerRateReportFilterVM filter)
        {
            filter ??= new CustomerRateReportFilterVM();
            var customerIds = ParseCustomerIds(filter.CustomerIds, filter.CustomerId);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Customer Rate Report");
            ws.ShowGridLines = true;

            // Column Widths
            ws.Column(1).Width = 8;
            ws.Column(2).Width = 45;
            ws.Column(3).Width = 20;
            ws.Column(4).Width = 25;

            // Company Title Header
            ws.Range(1, 1, 1, 4).Merge();
            ws.Cell(1, 1).Value = "Formosa Logistics Limited";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(2, 1, 2, 4).Merge();
            ws.Cell(2, 1).Value = "9th Floor, TCB Bhaban, 1 Kawran Bazar Rd, Dhaka 1215";
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(3, 1, 3, 4).Merge();
            ws.Cell(3, 1).Value = "Customer Rate Report";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 12;
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 5;

            if (customerIds.Any())
            {
                var customers = await _customerRepo.All().AsNoTracking()
                    .Where(c => customerIds.Contains(c.CustomerId))
                    .ToDictionaryAsync(c => c.CustomerId, StringComparer.OrdinalIgnoreCase);

                var chcList = await _chcRepo.All().AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.Chcid))
                    .ToListAsync();
                var chcDict = chcList
                    .GroupBy(x => x.Chcid)
                    .ToDictionary(g => g.Key, g => g.First().Chchead ?? g.Key);

                foreach (var custId in customerIds)
                {
                    customers.TryGetValue(custId, out var customer);
                    string custName = customer?.CustomerName ?? custId;
                    string address = customer?.CustomerAddress ?? string.Empty;

                    var costing = await _cwcRepo.All().AsNoTracking()
                        .OrderByDescending(c => c.Tc)
                        .FirstOrDefaultAsync(c => c.CustomerId == custId);

                    var detailsList = await _cwcdeRepo.All().AsNoTracking()
                        .Where(d => (costing != null && d.CostingId == costing.CostingId) || d.CustomerId == custId)
                        .ToListAsync();

                    // Customer Banner
                    ws.Range(row, 1, row, 3).Merge();
                    ws.Cell(row, 1).Value = $"Customer: {custName}";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Font.FontSize = 11;
                    ws.Cell(row, 4).Value = $"Date: {DateTime.Now:dd/MM/yyyy}";
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    row++;

                    if (!string.IsNullOrWhiteSpace(address))
                    {
                        ws.Range(row, 1, row, 4).Merge();
                        ws.Cell(row, 1).Value = $"Address: {address}";
                        ws.Cell(row, 1).Style.Font.FontSize = 9;
                        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
                        row++;
                    }

                    // Table Header
                    ws.Cell(row, 1).Value = "";
                    ws.Cell(row, 2).Value = "Particulars";
                    ws.Cell(row, 3).Value = "Amount (BDT)";
                    ws.Cell(row, 4).Value = "Remarks";

                    for (int c = 1; c <= 4; c++)
                    {
                        var cell = ws.Cell(row, c);
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#e2e8f0");
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#94a3b8");
                        if (c == 3) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        else if (c == 1) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    row++;

                    // Section A Header
                    ws.Cell(row, 1).Value = "A.";
                    ws.Cell(row, 2).Value = "Custom Handling Charge:";
                    ws.Cell(row, 3).Value = "";
                    ws.Cell(row, 4).Value = "";
                    for (int c = 1; c <= 4; c++)
                    {
                        var cell = ws.Cell(row, c);
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9");
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#cbd5e1");
                        if (c == 1) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    row++;

                    // Section A Items
                    foreach (var detail in detailsList)
                    {
                        string headName = (!string.IsNullOrEmpty(detail.Chcid) && chcDict.TryGetValue(detail.Chcid, out var h))
                            ? h
                            : (detail.Chcid ?? "-");

                        ws.Cell(row, 1).Value = "";
                        ws.Cell(row, 2).Value = headName;
                        if (detail.BillAmount.HasValue)
                        {
                            ws.Cell(row, 3).Value = detail.BillAmount.Value;
                            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else
                        {
                            ws.Cell(row, 3).Value = "0.00";
                            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        ws.Cell(row, 4).Value = "";

                        for (int c = 1; c <= 4; c++)
                        {
                            var cell = ws.Cell(row, c);
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e2e8f0");
                        }
                        row++;
                    }

                    // Section B Header
                    ws.Cell(row, 1).Value = "B.";
                    ws.Cell(row, 2).Value = "Agency Commission:";
                    ws.Cell(row, 3).Value = "";
                    ws.Cell(row, 4).Value = "";
                    for (int c = 1; c <= 4; c++)
                    {
                        var cell = ws.Cell(row, c);
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9");
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#cbd5e1");
                        if (c == 1) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    row++;

                    // Minimum Agency Commission
                    ws.Cell(row, 1).Value = "";
                    ws.Cell(row, 2).Value = "Minimum Agency Commission";
                    ws.Cell(row, 3).Value = costing != null ? costing.MinimumAgencyCommission : 0m;
                    ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 4).Value = "";
                    for (int c = 1; c <= 4; c++)
                    {
                        var cell = ws.Cell(row, c);
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e2e8f0");
                    }
                    row++;

                    // On Invoice Value
                    ws.Cell(row, 1).Value = "";
                    ws.Cell(row, 2).Value = "On Invoice Value";
                    ws.Cell(row, 3).Value = costing != null ? $"{costing.OnInvoiceValue:0.00}%" : "0.00%";
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 4).Value = "";
                    for (int c = 1; c <= 4; c++)
                    {
                        var cell = ws.Cell(row, c);
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e2e8f0");
                    }
                    row += 2; // Spacer
                }
            }
            else
            {
                ws.Range(row, 1, row, 4).Merge();
                ws.Cell(row, 1).Value = "No rate records found for the selected customer(s).";
                ws.Cell(row, 1).Style.Font.Italic = true;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row += 2;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private async Task<List<CustomerRateReportCustomerRowDTO>> BuildCustomerRowsAsync(List<string> customerIds)
        {
            var result = new List<CustomerRateReportCustomerRowDTO>();

            customerIds = customerIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();

            var custList = await _customerRepo.All().AsNoTracking()
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToListAsync();
            var customers = custList
                .Where(c => !string.IsNullOrEmpty(c.CustomerId))
                .GroupBy(c => c.CustomerId)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var chcList = await _chcRepo.All().AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.Chcid))
                .ToListAsync();
            var chcDict = chcList
                .GroupBy(x => x.Chcid)
                .ToDictionary(g => g.Key, g => g.First().Chchead ?? g.Key);

            var desigList = await _designationRepo.All().AsNoTracking()
                .Where(d => !string.IsNullOrEmpty(d.DesignationCode))
                .ToListAsync();
            var designations = desigList
                .GroupBy(d => d.DesignationCode)
                .ToDictionary(g => g.Key, g => g.First().DesignationName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var cpList = await _contactPersonRepo.All().AsNoTracking()
                .Where(cp => !string.IsNullOrEmpty(cp.Cpid))
                .ToListAsync();
            var contactPersons = cpList
                .GroupBy(cp => cp.Cpid)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var custId in customerIds)
            {
                customers.TryGetValue(custId, out var customer);
                string custName = customer?.CustomerName ?? custId;

                var costing = await _cwcRepo.All().AsNoTracking()
                    .OrderByDescending(c => c.Tc)
                    .FirstOrDefaultAsync(c => c.CustomerId == custId);

                var detailsList = await _cwcdeRepo.All().AsNoTracking()
                    .Where(d => (costing != null && d.CostingId == costing.CostingId) || d.CustomerId == custId)
                    .ToListAsync();

                decimal totalHandling = detailsList.Sum(d => d.BillAmount ?? 0m);
                decimal minComm = costing?.MinimumAgencyCommission ?? 0m;
                decimal onInvVal = costing?.OnInvoiceValue ?? 0m;

                string photoUrl = string.Empty;
                if (!string.IsNullOrWhiteSpace(customer?.CustomerPhoto))
                {
                    photoUrl = customer.CustomerPhoto.StartsWith("/") || customer.CustomerPhoto.StartsWith("http") || customer.CustomerPhoto.StartsWith("data:")
                        ? customer.CustomerPhoto
                        : $"/uploads/CRM/Image/{customer.CustomerPhoto}";
                }

                string contactPersonFormatted = string.Empty;
                if (!string.IsNullOrWhiteSpace(customer?.ContactPerson))
                {
                    var cpIds = customer.ContactPerson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    var lines = new List<string>();
                    foreach (var cpId in cpIds)
                    {
                        if (contactPersons.TryGetValue(cpId, out var cp))
                        {
                            string desig = (!string.IsNullOrEmpty(cp.DesignationCode) && designations.TryGetValue(cp.DesignationCode, out var dName))
                                ? dName
                                : string.Empty;

                            var parts = new[] { cp.ContactPersonName, desig, cp.ContactPersonMobile, cp.ContactPersonEmail }
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => s.Trim());

                            var line = string.Join(", ", parts);
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                lines.Add(line);
                            }
                        }
                    }

                    if (lines.Any())
                    {
                        contactPersonFormatted = string.Join(",\r\n", lines);
                    }
                }

                if (string.IsNullOrWhiteSpace(contactPersonFormatted))
                {
                    contactPersonFormatted = customer?.ContatPerson1 ?? string.Empty;
                }

                var customerRow = new CustomerRateReportCustomerRowDTO
                {
                    CustomerId = custId,
                    Name = custName,
                    CustomerName = custName,
                    CustomerCompany = customer?.CustomerCompany ?? string.Empty,
                    Address = customer?.CustomerAddress ?? string.Empty,
                    CustomerAddress = customer?.CustomerAddress ?? string.Empty,
                    Phone = customer?.Phone ?? string.Empty,
                    Fax = customer?.Fax ?? string.Empty,
                    Email = customer?.Email ?? string.Empty,
                    Photo = photoUrl,
                    ContactPerson = contactPersonFormatted,
                    TotalChcItems = detailsList.Count,
                    TotalHandlingAmount = totalHandling,
                    FormattedHandlingAmount = totalHandling.ToString("#,##0.00", CultureInfo.InvariantCulture),
                    MinAgencyCommission = minComm,
                    FormattedMinAgencyCommission = minComm.ToString("#,##0.00", CultureInfo.InvariantCulture),
                    OnInvoiceValue = onInvVal,
                    FormattedOnInvoiceValue = $"{onInvVal.ToString("0.00", CultureInfo.InvariantCulture)}%",
                    RateItems = new List<CustomerRateReportItemDTO>()
                };

                // Section A: Custom Handling Charge
                customerRow.RateItems.Add(new CustomerRateReportItemDTO
                {
                    CustomerId = custId,
                    CustomerName = custName,
                    Section = "A.",
                    Particulars = "Custom Handling Charge:",
                    Amount = null,
                    FormattedAmount = string.Empty,
                    Remarks = string.Empty,
                    IsHeader = true
                });

                foreach (var detail in detailsList)
                {
                    string headName = (!string.IsNullOrEmpty(detail.Chcid) && chcDict.TryGetValue(detail.Chcid, out var h))
                        ? h
                        : (detail.Chcid ?? "-");

                    customerRow.RateItems.Add(new CustomerRateReportItemDTO
                    {
                        CustomerId = custId,
                        CustomerName = custName,
                        Section = string.Empty,
                        Particulars = headName,
                        Amount = detail.BillAmount,
                        FormattedAmount = detail.BillAmount.HasValue ? detail.BillAmount.Value.ToString("#,##0.00", CultureInfo.InvariantCulture) : "0.00",
                        Remarks = string.Empty,
                        IsHeader = false
                    });
                }

                // Section B: Agency Commission
                customerRow.RateItems.Add(new CustomerRateReportItemDTO
                {
                    CustomerId = custId,
                    CustomerName = custName,
                    Section = "B.",
                    Particulars = "Agency Commission:",
                    Amount = null,
                    FormattedAmount = string.Empty,
                    Remarks = string.Empty,
                    IsHeader = true
                });

                customerRow.RateItems.Add(new CustomerRateReportItemDTO
                {
                    CustomerId = custId,
                    CustomerName = custName,
                    Section = string.Empty,
                    Particulars = "Minimum Agency Commission",
                    Amount = costing?.MinimumAgencyCommission,
                    FormattedAmount = costing != null ? costing.MinimumAgencyCommission.ToString("#,##0.00", CultureInfo.InvariantCulture) : "0.00",
                    Remarks = string.Empty,
                    IsHeader = false
                });

                customerRow.RateItems.Add(new CustomerRateReportItemDTO
                {
                    CustomerId = custId,
                    CustomerName = custName,
                    Section = string.Empty,
                    Particulars = "On Invoice Value",
                    Amount = costing?.OnInvoiceValue,
                    FormattedAmount = costing != null ? $"{costing.OnInvoiceValue.ToString("0.00", CultureInfo.InvariantCulture)}%" : "0.00%",
                    Remarks = string.Empty,
                    IsHeader = false
                });

                result.Add(customerRow);
            }

            return result.OrderBy(x => x.CustomerId, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> ParseCustomerIds(string? customerIds, string? singleCustomerId)
        {
            var raw = !string.IsNullOrWhiteSpace(customerIds) ? customerIds : singleCustomerId;
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x) && x != "null" && x != "undefined")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
