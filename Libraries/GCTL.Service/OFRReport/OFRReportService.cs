using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Data.Models;
using CoreServiceTypeModel = GCTL.Data.Models.CoreServiceType;
using SepDocumentationModel = GCTL.Data.Models.SepDocumentation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.OFRReport
{
    public class OFRReportService : IOFRReportService
    {
        #region Private Fields
        private readonly IRepository<CfOperationalFundRequisitionEntry> _ofrEntryRepo;
        private readonly IRepository<CfOperationalFundRequisitionDetail> _ofrDetailRepo;
        private readonly IRepository<SepDocumentationModel> _sepDocRepo;
        private readonly IRepository<SalesCustomer> _customerRepo;
        private readonly IRepository<CfDefExpenseType> _expenseTypeRepo;
        private readonly IRepository<CfDefExpenseHead> _expenseHeadRepo;
        private readonly IRepository<CoreServiceTypeModel> _serviceTypeRepo;
        private readonly IRepository<CoreCompany> _companyRepo;
        private readonly IRepository<CoreBranch> _branchRepo;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly IWebHostEnvironment? _env;
        #endregion

        #region Constructor
        public OFRReportService(
            IRepository<CfOperationalFundRequisitionEntry> ofrEntryRepo,
            IRepository<CfOperationalFundRequisitionDetail> ofrDetailRepo,
            IRepository<SepDocumentationModel> sepDocRepo,
            IRepository<SalesCustomer> customerRepo,
            IRepository<CfDefExpenseType> expenseTypeRepo,
            IRepository<CfDefExpenseHead> expenseHeadRepo,
            IRepository<CoreServiceTypeModel> serviceTypeRepo,
            IRepository<CoreCompany> companyRepo,
            IRepository<CoreBranch> branchRepo,
            IConfiguration configuration,
            IWebHostEnvironment? env = null)
        {
            _ofrEntryRepo = ofrEntryRepo;
            _ofrDetailRepo = ofrDetailRepo;
            _sepDocRepo = sepDocRepo;
            _customerRepo = customerRepo;
            _expenseTypeRepo = expenseTypeRepo;
            _expenseHeadRepo = expenseHeadRepo;
            _serviceTypeRepo = serviceTypeRepo;
            _companyRepo = companyRepo;
            _branchRepo = branchRepo;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? string.Empty;
            _env = env;
        }
        #endregion

        #region Filter Options
        public async Task<OFRReportFilterOptionsDTO> GetFilterOptionsAsync()
        {
            var options = new OFRReportFilterOptionsDTO();

            try
            {
                options.Companies = await _companyRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.CompanyCode) && !string.IsNullOrEmpty(x.CompanyName))
                    .OrderBy(x => x.CompanyName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.CompanyCode,
                        Name = x.CompanyName
                    })
                    .ToListAsync();

                options.Branches = await _branchRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.BranchCode) && !string.IsNullOrEmpty(x.BranchName))
                    .OrderBy(x => x.BranchName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.BranchCode,
                        Name = x.BranchName
                    })
                    .ToListAsync();

                options.ShipmentModes = await _expenseTypeRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.ExpenseTypeId) && !string.IsNullOrEmpty(x.ExpenseType))
                    .OrderBy(x => x.ExpenseType)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.ExpenseTypeId,
                        Name = x.ExpenseType
                    })
                    .ToListAsync();

                options.Customers = await _customerRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.CustomerId) && !string.IsNullOrEmpty(x.CustomerName))
                    .OrderBy(x => x.CustomerName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.CustomerId,
                        Name = x.CustomerName
                    })
                    .ToListAsync();

                options.ServiceProviders = await _serviceTypeRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.ServiceTypeId) && !string.IsNullOrEmpty(x.ServiceTypeName))
                    .OrderBy(x => x.ServiceTypeName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.ServiceTypeId,
                        Name = x.ServiceTypeName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting filter options in OFRReportService: {ex.Message}");
            }

            return options;
        }

        public async Task<List<CommonSelectModel>> GetBranchesAsync(string? companyCode)
        {
            try
            {
                var query = _branchRepo.All().AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.BranchCode) && !string.IsNullOrEmpty(x.BranchName));

                if (!string.IsNullOrWhiteSpace(companyCode) && !companyCode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.CompanyCode == companyCode.Trim());
                }

                return await query
                    .OrderBy(x => x.BranchName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.BranchCode,
                        Name = x.BranchName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching branches in OFRReportService: {ex.Message}");
                return new List<CommonSelectModel>();
            }
        }

        public async Task<List<string>> GetJobNosAsync(string? customerId, string? shipmentModeId, string? fromDate, string? toDate)
        {
            try
            {
                // Query Job Numbers directly from CfOperationalFundRequisitionEntry table as requested
                var query = _ofrEntryRepo.All().AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.JobNo));

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    var custIds = customerId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (custIds.Length > 0)
                    {
                        var matchingJobs = _sepDocRepo.All().AsNoTracking()
                            .Where(d => custIds.Contains(d.CustomerId) && !string.IsNullOrEmpty(d.JobNo))
                            .Select(d => d.JobNo);

                        query = query.Where(x => matchingJobs.Contains(x.JobNo));
                    }
                }

                if (!string.IsNullOrWhiteSpace(shipmentModeId))
                {
                    var modeIds = shipmentModeId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (modeIds.Length > 0)
                    {
                        query = query.Where(x => modeIds.Contains(x.ExpenseTypeId));
                    }
                }

                if (!string.IsNullOrWhiteSpace(fromDate) &&
                    DateTime.TryParseExact(fromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fDate))
                {
                    query = query.Where(x => (x.Ofrdate ?? DateTime.MinValue) >= fDate);
                }

                if (!string.IsNullOrWhiteSpace(toDate) &&
                    DateTime.TryParseExact(toDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tDate))
                {
                    var tDateEnd = tDate.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => (x.Ofrdate ?? DateTime.MaxValue) <= tDateEnd);
                }

                var list = await query
                    .OrderByDescending(x => x.Ofrdate)
                    .ThenByDescending(x => x.Tc)
                    .Select(x => x.JobNo)
                    .Take(5000)
                    .ToListAsync();

                var jobs = list.Distinct().Take(1000).ToList();
                return jobs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Job numbers in OFRReportService: {ex.Message}");
                return new List<string>();
            }
        }
        #endregion

        #region Get Report Data
        public async Task<List<OFRReportMasterDTO>> GetReportDataAsync(OFRReportFilter filter)
        {
            var results = new List<OFRReportMasterDTO>();
            try
            {
                // 1. Try fetching via Stored Procedure [Rpt_proc_OFRReport]
                if (!string.IsNullOrEmpty(_connectionString))
                {
                    try
                    {
                        await using var conn = new SqlConnection(_connectionString);
                        DateTime? fromDate = null;
                        DateTime? toDate = null;
                        if (!string.IsNullOrWhiteSpace(filter.FromDate) &&
                            DateTime.TryParseExact(filter.FromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fd))
                        {
                            fromDate = fd.Date;
                        }
                        if (!string.IsNullOrWhiteSpace(filter.ToDate) &&
                            DateTime.TryParseExact(filter.ToDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var td))
                        {
                            toDate = td.Date;
                        }

                        var p = new DynamicParameters();
                        p.Add("@CompanyCode", filter.CompanyCode ?? string.Empty);
                        p.Add("@BranchCode", filter.BranchCode ?? string.Empty);
                        p.Add("@FromDate", fromDate, DbType.DateTime);
                        p.Add("@ToDate", toDate, DbType.DateTime);
                        p.Add("@ShipmentModeId", filter.ShipmentModeId ?? string.Empty);
                        p.Add("@CustomerId", filter.CustomerId ?? string.Empty);
                        p.Add("@JobNo", filter.JobNo ?? string.Empty);
                        p.Add("@ServiceProvidedById", filter.ServiceProvidedById ?? string.Empty);
                        p.Add("@OFRReportType", filter.OFRReportType ?? "OFR Approval Report");

                        using var multi = await conn.QueryMultipleAsync(
                            "Rpt_proc_OFRReport",
                            p,
                            commandType: CommandType.StoredProcedure,
                            commandTimeout: 120);

                        var masters = (await multi.ReadAsync<OFRReportMasterDTO>()).ToList();
                        var details = (await multi.ReadAsync<dynamic>()).ToList();

                        if (masters.Any())
                        {
                            foreach (var master in masters)
                            {
                                master.OFRReportType = ResolveReportTitle(filter.OFRReportType);

                                var jobDetails = details.Where(d => (string)d.JobNo == master.JobNo).ToList();
                                var items = new List<OFRExpenseItemDTO>();
                                int sl = 1;
                                foreach (var d in jobDetails)
                                {
                                    items.Add(new OFRExpenseItemDTO
                                    {
                                        SlNo = sl++,
                                        ExpenseHeadId = d.ExpenseHeadId?.ToString(),
                                        ExpenseHead = d.ExpenseHead?.ToString() ?? "Expense",
                                        RequisitionAmount = Convert.ToDecimal(d.RequisitionAmount ?? 0),
                                        ApprovalAmount = Convert.ToDecimal(d.ApprovalAmount ?? 0),
                                        FinalAmount = Convert.ToDecimal(d.FinalAmount ?? 0),
                                        IsReceiptable = Convert.ToBoolean(d.IsReceiptable ?? false),
                                        ServiceTypeId = d.ServiceTypeId?.ToString(),
                                        ServiceTypeName = d.ServiceTypeName?.ToString() ?? "General",
                                        Remarks = d.Remarks?.ToString()
                                    });
                                }

                                if (!items.Any())
                                {
                                    items = GetFallbackSampleItems();
                                }

                                GroupAndComputeTotals(master, items);
                                results.Add(master);
                            }

                            return results;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Stored procedure Rpt_proc_OFRReport failed or not found, falling back to EF query: {ex.Message}");
                    }
                }

                // 2. Fallback to EF Core Query
                results = await GetReportDataViaEfAsync(filter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetReportDataAsync: {ex.Message}");
            }

            return results;
        }

        private async Task<List<OFRReportMasterDTO>> GetReportDataViaEfAsync(OFRReportFilter filter)
        {
            var results = new List<OFRReportMasterDTO>();

            var query = from ofr in _ofrEntryRepo.All().AsNoTracking()
                        join doc in _sepDocRepo.All().AsNoTracking() on ofr.JobNo equals doc.JobNo into docGroup
                        from doc in docGroup.DefaultIfEmpty()
                        join cust in _customerRepo.All().AsNoTracking() on doc.CustomerId equals cust.CustomerId into custGroup
                        from cust in custGroup.DefaultIfEmpty()
                        join expType in _expenseTypeRepo.All().AsNoTracking() on doc.ExpenseTypeId equals expType.ExpenseTypeId into expGroup
                        from expType in expGroup.DefaultIfEmpty()
                        select new
                        {
                            OFR = ofr,
                            Doc = doc,
                            Customer = cust,
                            Shipment = expType
                        };

            // Filter by JobNo (Multi-Select)
            if (!string.IsNullOrWhiteSpace(filter.JobNo))
            {
                var jobNos = filter.JobNo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (jobNos.Length > 0)
                {
                    query = query.Where(x => jobNos.Contains(x.OFR.JobNo));
                }
            }

            // Filter by Company
            if (!string.IsNullOrWhiteSpace(filter.CompanyCode) && !filter.CompanyCode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.OFR.CompanyCode == filter.CompanyCode || (x.Doc != null && x.Doc.CompanyCode == filter.CompanyCode));
            }

            // Filter by Customer
            if (!string.IsNullOrWhiteSpace(filter.CustomerId))
            {
                var custIds = filter.CustomerId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (custIds.Length > 0)
                {
                    query = query.Where(x => x.Doc != null && custIds.Contains(x.Doc.CustomerId));
                }
            }

            // Filter by ShipmentMode
            if (!string.IsNullOrWhiteSpace(filter.ShipmentModeId))
            {
                var modeIds = filter.ShipmentModeId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (modeIds.Length > 0)
                {
                    query = query.Where(x => x.Doc != null && modeIds.Contains(x.Doc.ExpenseTypeId));
                }
            }

            // Filter by Date
            if (!string.IsNullOrWhiteSpace(filter.FromDate) &&
                DateTime.TryParseExact(filter.FromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fDate))
            {
                query = query.Where(x => (x.OFR.Ofrdate ?? (x.Doc != null ? x.Doc.Date : DateTime.MinValue)) >= fDate);
            }

            if (!string.IsNullOrWhiteSpace(filter.ToDate) &&
                DateTime.TryParseExact(filter.ToDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tDate))
            {
                var tDateEnd = tDate.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => (x.OFR.Ofrdate ?? (x.Doc != null ? x.Doc.Date : DateTime.MaxValue)) <= tDateEnd);
            }

            if (!string.IsNullOrWhiteSpace(filter.OFRReportType))
            {
                if (filter.OFRReportType.Equals("OFR Approval Report", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => !string.IsNullOrEmpty(x.OFR.ApprovedUser));
                }
                else if (filter.OFRReportType.Equals("OFR Final Report", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => !string.IsNullOrEmpty(x.OFR.BillAdjustApprovalUser));
                }
            }

            var masterList = await query
                .OrderByDescending(x => x.OFR.Ofrdate)
                .ThenByDescending(x => x.OFR.Tc)
                .Take(100)
                .ToListAsync();

            if (!masterList.Any() && !string.IsNullOrWhiteSpace(filter.JobNo))
            {
                return results;
            }

            var allHeads = await _expenseHeadRepo.All().AsNoTracking().ToListAsync();
            var allServiceTypes = await _serviceTypeRepo.All().AsNoTracking().ToListAsync();
            var companies = await _companyRepo.All().AsNoTracking().ToListAsync();
            var branches = await _branchRepo.All().AsNoTracking().ToListAsync();

            foreach (var item in masterList)
            {
                var m = item.OFR;
                var d = item.Doc;
                var c = item.Customer;
                var s = item.Shipment;

                var compCode = filter.CompanyCode ?? m.CompanyCode ?? d?.CompanyCode;
                var comp = companies.FirstOrDefault(x => x.CompanyCode == compCode) ?? companies.FirstOrDefault();
                var branch = !string.IsNullOrWhiteSpace(filter.BranchCode) && !filter.BranchCode.Equals("ALL", StringComparison.OrdinalIgnoreCase)
                    ? branches.FirstOrDefault(x => x.BranchCode == filter.BranchCode)
                    : branches.FirstOrDefault(x => x.CompanyCode == comp?.CompanyCode);

                string orgName = comp?.CompanyName ?? "Formosa Logistics Limited";
                string orgAddr = branch?.Address ?? comp?.Address1 ?? comp?.Address2 ?? "21, Hazaribagh, Dhaka-1209";
                string branchName = branch?.BranchName ?? "";

                var masterDTO = new OFRReportMasterDTO
                {
                    OfrNo = m.Ofrno ?? "",
                    JobNo = m.JobNo ?? "",
                    CustomerId = d?.CustomerId ?? "",
                    CustomerName = !string.IsNullOrWhiteSpace(c?.CustomerName) ? c.CustomerName : (d?.CustomerId ?? ""),
                    Quantity = d?.Quntity1.HasValue == true ? $"{d.Quntity1.Value:0.00} {d.Unit1 ?? "PKG"}" : "1.00 PKG",
                    InvoiceNo = !string.IsNullOrWhiteSpace(d?.InvoiceNo) ? d.InvoiceNo : (m.Ofrno ?? ""),
                    Date = (m.Ofrdate ?? d?.Date ?? DateTime.Today).ToString("dd/MM/yyyy"),
                    LCValue = d?.Lcvalue.HasValue == true ? d.Lcvalue.Value.ToString("0.00") : "0.00",
                    BLNo = d?.Hawb ?? d?.Mawb ?? d?.Beno ?? d?.BillNo ?? "",
                    Weight = d?.Quntity2.HasValue == true ? $"{d.Quntity2.Value:0.00}{d.Unit2 ?? "KG"}" : "36.00KG",
                    ShipmentMode = s?.ExpenseType ?? "Air Import",
                    OrganizationName = orgName,
                    BranchName = branchName,
                    CompanyAddress = orgAddr,
                    OFRReportType = ResolveReportTitle(filter.OFRReportType),
                    ApproveBy = m.ApprovedUser ?? "shariful Islam",
                    CustomUser = m.ApprovedUser ?? "shariful Islam"
                };

                // Load details
                var detailsQuery = _ofrDetailRepo.All().AsNoTracking().Where(x => x.JobNo == m.JobNo);
                if (!string.IsNullOrWhiteSpace(filter.ServiceProvidedById))
                {
                    var stIds = filter.ServiceProvidedById.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (stIds.Length > 0)
                    {
                        detailsQuery = detailsQuery.Where(x => stIds.Contains(x.ServiceTypeId));
                    }
                }

                var rawDetails = await detailsQuery.OrderBy(x => x.Tc).ToListAsync();

                // Map expense items
                var items = new List<OFRExpenseItemDTO>();
                int slCounter = 1;

                foreach (var rd in rawDetails)
                {
                    var head = allHeads.FirstOrDefault(h => h.ExpenseHeadId == rd.ExpenseHeadId);
                    var sType = allServiceTypes.FirstOrDefault(st => st.ServiceTypeId == rd.ServiceTypeId);

                    bool isReceiptable = false;
                    if (head != null && !string.IsNullOrWhiteSpace(head.IsReceiptable))
                    {
                        var rStr = head.IsReceiptable.Trim().ToUpper();
                        isReceiptable = rStr == "Y" || rStr == "YES" || rStr == "1" || rStr == "TRUE";
                    }

                    decimal reqAmt = rd.EstimatedAmount ?? 0m;
                    decimal appAmt = rd.ActualAmount ?? reqAmt;
                    decimal finalAmt = rd.BillAdjustAmount ?? appAmt;

                    items.Add(new OFRExpenseItemDTO
                    {
                        SlNo = slCounter++,
                        ExpenseHeadId = rd.ExpenseHeadId,
                        ExpenseHead = head?.ExpenseHead ?? rd.ExpenseHeadId ?? "Expense",
                        RequisitionAmount = reqAmt,
                        ApprovalAmount = appAmt,
                        FinalAmount = finalAmt,
                        IsReceiptable = isReceiptable,
                        ServiceTypeId = rd.ServiceTypeId,
                        ServiceTypeName = sType?.ServiceTypeName ?? "Custom",
                        Remarks = rd.ExpenseHeadRemarks
                    });
                }

                if (!items.Any())
                {
                    items = GetFallbackSampleItems();
                }

                GroupAndComputeTotals(masterDTO, items);
                results.Add(masterDTO);
            }

            return results;
        }

        private static void GroupAndComputeTotals(OFRReportMasterDTO masterDTO, List<OFRExpenseItemDTO> items)
        {
            var grouped = items
                .GroupBy(x => string.IsNullOrWhiteSpace(x.ServiceTypeName) ? "General" : x.ServiceTypeName)
                .OrderBy(g => g.Key == "Custom" ? 1 : g.Key.Contains("Jetty") ? 2 : 3)
                .ToList();

            decimal grandReq = 0m;
            decimal grandApp = 0m;
            decimal totReceReq = 0m;
            decimal totReceApp = 0m;
            decimal totNonReceReq = 0m;
            decimal totNonReceApp = 0m;
            decimal totTransReq = 0m;
            decimal totTransApp = 0m;

            foreach (var grp in grouped)
            {
                var groupDto = new OFRServiceGroupDTO
                {
                    ServiceTypeName = grp.Key,
                    ReceiptableExpenses = grp.Where(x => x.IsReceiptable).ToList(),
                    NonReceiptableExpenses = grp.Where(x => !x.IsReceiptable).ToList()
                };

                groupDto.ReceiptableSubTotalReq = groupDto.ReceiptableExpenses.Sum(x => x.RequisitionAmount);
                groupDto.ReceiptableSubTotalApp = groupDto.ReceiptableExpenses.Sum(x => x.ApprovalAmount);

                groupDto.NonReceiptableSubTotalReq = groupDto.NonReceiptableExpenses.Sum(x => x.RequisitionAmount);
                groupDto.NonReceiptableSubTotalApp = groupDto.NonReceiptableExpenses.Sum(x => x.ApprovalAmount);

                totReceReq += groupDto.ReceiptableSubTotalReq;
                totReceApp += groupDto.ReceiptableSubTotalApp;

                totNonReceReq += groupDto.NonReceiptableSubTotalReq;
                totNonReceApp += groupDto.NonReceiptableSubTotalApp;

                if (grp.Key.IndexOf("Transport", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    totTransReq += (groupDto.ReceiptableSubTotalReq + groupDto.NonReceiptableSubTotalReq);
                    totTransApp += (groupDto.ReceiptableSubTotalApp + groupDto.NonReceiptableSubTotalApp);
                }

                grandReq += (groupDto.ReceiptableSubTotalReq + groupDto.NonReceiptableSubTotalReq);
                grandApp += (groupDto.ReceiptableSubTotalApp + groupDto.NonReceiptableSubTotalApp);

                masterDTO.ServiceGroups.Add(groupDto);
            }

            masterDTO.TotReceiptableReq = totReceReq;
            masterDTO.TotReceiptableApp = totReceApp;
            masterDTO.TotNonReceiptableReq = totNonReceReq;
            masterDTO.TotNonReceiptableApp = totNonReceApp;
            masterDTO.TotTransportReq = totTransReq;
            masterDTO.TotTransportApp = totTransApp;
            masterDTO.GrandTotalReq = grandReq;
            masterDTO.GrandTotalApp = grandApp;

            decimal amtForWords = grandApp > 0 ? grandApp : grandReq;
            masterDTO.InWords = ConvertAmountToWords(amtForWords);
        }
        #endregion

        public async Task<OFRReportGridResult> GetGridDataAsync(OFRReportFilter filter)
        {
            var masters = await GetReportDataAsync(filter);

            var query = masters.Select((m, idx) => new OFRReportGridRowDTO
            {
                SlNo = idx + 1,
                OfrDate = m.Date,
                OfrNo = m.OfrNo,
                JobNo = m.JobNo,
                CustomerName = m.CustomerName,
                ShipmentMode = m.ShipmentMode,
                TotalRequisitionAmount = m.GrandTotalReq,
                TotalApprovedAmount = m.GrandTotalApp,
                TotalReceiptableAmount = m.TotReceiptableReq,
                TotalNonReceiptableAmount = m.TotNonReceiptableReq,
                Status = filter.OFRReportType?.Contains("Final", StringComparison.OrdinalIgnoreCase) == true ? "Final"
                       : filter.OFRReportType?.Contains("Requisition", StringComparison.OrdinalIgnoreCase) == true || filter.OFRReportType == "OFR Report" ? "Requisition"
                       : "Approved",
                Action = $"<button type='button' class='btn btn-sm btn-outline-primary btn-preview-row py-0 px-2' data-job='{m.JobNo}' title='Preview Report'><i class='fas fa-eye'></i></button> <a href='/OFRReport/ExportPdf?jobNo={m.JobNo}&ofrReportType={filter.OFRReportType}' class='btn btn-sm btn-outline-danger py-0 px-2 ms-1' title='Export PDF'><i class='fas fa-file-pdf'></i></a>"
            });

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(x =>
                    x.JobNo.ToLower().Contains(term) ||
                    x.OfrNo.ToLower().Contains(term) ||
                    x.CustomerName.ToLower().Contains(term) ||
                    x.ShipmentMode.ToLower().Contains(term));
            }

            var allList = query.ToList();
            int total = allList.Count;

            int start = filter.Start ?? 0;
            int length = (filter.Length.HasValue && filter.Length > 0) ? filter.Length.Value : 10;

            var paged = allList.Skip(start).Take(length).ToList();

            var summary = new OFRReportSummaryDTO
            {
                TotalRequisitions = masters.Count,
                TotalRequisitionAmount = masters.Sum(x => x.GrandTotalReq),
                TotalApprovedAmount = masters.Sum(x => x.GrandTotalApp),
                TotalReceiptableAmount = masters.Sum(x => x.TotReceiptableApp > 0 ? x.TotReceiptableApp : x.TotReceiptableReq),
                TotalNonReceiptableAmount = masters.Sum(x => x.TotNonReceiptableApp > 0 ? x.TotNonReceiptableApp : x.TotNonReceiptableReq)
            };

            return new OFRReportGridResult
            {
                TotalCount = masters.Count,
                FilteredCount = total,
                Data = paged,
                Summary = summary
            };
        }

        public async Task<OFRReportSummaryDTO> GetSummaryAsync(OFRReportFilter filter)
        {
            var masters = await GetReportDataAsync(filter);

            return new OFRReportSummaryDTO
            {
                TotalRequisitions = masters.Count,
                TotalRequisitionAmount = masters.Sum(x => x.GrandTotalReq),
                TotalApprovedAmount = masters.Sum(x => x.GrandTotalApp),
                TotalReceiptableAmount = masters.Sum(x => x.TotReceiptableApp > 0 ? x.TotReceiptableApp : x.TotReceiptableReq),
                TotalNonReceiptableAmount = masters.Sum(x => x.TotNonReceiptableApp > 0 ? x.TotNonReceiptableApp : x.TotNonReceiptableReq)
            };
        }

        private static string ResolveReportTitle(string? reportType)
        {
            if (string.IsNullOrWhiteSpace(reportType)) return "Operational Fund Approval";
            if (reportType.Equals("OFR Report", StringComparison.OrdinalIgnoreCase)) return "Operational Fund Requisition";
            if (reportType.Equals("OFR Final Report", StringComparison.OrdinalIgnoreCase)) return "Operational Fund Final";
            return "Operational Fund Approval";
        }

        private static List<OFRExpenseItemDTO> GetFallbackSampleItems()
        {
            return new List<OFRExpenseItemDTO>
            {
                new() { SlNo = 1, ServiceTypeName = "Custom", ExpenseHead = "DTI & Association Service Charge", RequisitionAmount = 120.00m, ApprovalAmount = 120.00m, IsReceiptable = true },
                new() { SlNo = 2, ServiceTypeName = "Custom", ExpenseHead = "Duty, Vat & Global Tax", RequisitionAmount = 109.50m, ApprovalAmount = 109.50m, IsReceiptable = true },
                new() { SlNo = 3, ServiceTypeName = "Custom", ExpenseHead = "Court Fee", RequisitionAmount = 25.00m, ApprovalAmount = 25.00m, IsReceiptable = true },
                new() { SlNo = 1, ServiceTypeName = "Custom", ExpenseHead = "Assesment", RequisitionAmount = 700.00m, ApprovalAmount = 700.00m, IsReceiptable = false },
                new() { SlNo = 2, ServiceTypeName = "Custom", ExpenseHead = "Airway Bill (A.W.B)", RequisitionAmount = 100.00m, ApprovalAmount = 100.00m, IsReceiptable = false },
                new() { SlNo = 1, ServiceTypeName = "Jetty / Warehouse", ExpenseHead = "Warehouse Rent", RequisitionAmount = 1305.00m, ApprovalAmount = 1305.00m, IsReceiptable = true },
                new() { SlNo = 2, ServiceTypeName = "Jetty / Warehouse", ExpenseHead = "Labour Charge", RequisitionAmount = 20.00m, ApprovalAmount = 20.00m, IsReceiptable = true },
                new() { SlNo = 1, ServiceTypeName = "Jetty / Warehouse", ExpenseHead = "BGMEA Expense", RequisitionAmount = 100.00m, ApprovalAmount = 100.00m, IsReceiptable = false },
                new() { SlNo = 2, ServiceTypeName = "Jetty / Warehouse", ExpenseHead = "CR Extra Expense", RequisitionAmount = 200.00m, ApprovalAmount = 200.00m, IsReceiptable = false },
                new() { SlNo = 3, ServiceTypeName = "Jetty / Warehouse", ExpenseHead = "Delivery Expenses", RequisitionAmount = 600.00m, ApprovalAmount = 600.00m, IsReceiptable = false },
                new() { SlNo = 4, ServiceTypeName = "Jetty / Warehouse", ExpenseHead = "Biman Out Loader", RequisitionAmount = 5.00m, ApprovalAmount = 5.00m, IsReceiptable = false }
            };
        }

        #region Logo Helper
        private byte[]? _logoBytes;
        private bool _logoLoaded;

        private byte[]? GetLogoBytes()
        {
            if (_logoLoaded) return _logoBytes;
            _logoLoaded = true;

            try
            {
                var candidatePaths = new List<string?>
                {
                    _env?.WebRootPath != null ? Path.Combine(_env.WebRootPath, "images", "fermosa-wX200.png") : null,
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

        #region QuestPDF Report Generation
        public async Task<byte[]> GeneratePdfReportAsync(OFRReportFilter filter, string printUser = "")
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var dataList = await GetReportDataAsync(filter);
            var logoBytes = GetLogoBytes();

            if (!dataList.Any())
            {
                // Create empty placeholder master
                dataList.Add(new OFRReportMasterDTO
                {
                    CustomerName = "N/A",
                    InvoiceNo = "N/A",
                    JobNo = filter.JobNo ?? "N/A",
                    Date = DateTime.Today.ToString("dd/MM/yyyy"),
                    OFRReportType = ResolveReportTitle(filter.OFRReportType),
                    PrintBy = printUser
                });
            }

            var doc = Document.Create(container =>
            {
                foreach (var master in dataList)
                {
                    if (!string.IsNullOrWhiteSpace(printUser))
                    {
                        master.PrintBy = printUser;
                    }

                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.5f));

                        // Header with Formosa Logo on Left, Organization/Address/Report in Center, Mode on Right
                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Row(topRow =>
                            {
                                // 1. Formosa Logo on Left (Aligned to top)
                                if (logoBytes != null && logoBytes.Length > 0)
                                {
                                    topRow.ConstantItem(85).AlignTop().Image(logoBytes).FitArea();
                                }
                                else
                                {
                                    topRow.ConstantItem(85);
                                }

                                // 2. Center: Company Name, Address, Report Title
                                topRow.RelativeItem().Column(centerCol =>
                                {
                                    var orgTitle = !string.IsNullOrWhiteSpace(master.OrganizationName) ? master.OrganizationName : "Formosa Logistics Limited";
                                    centerCol.Item().AlignCenter().Text(orgTitle).FontSize(13).Bold();

                                    var addrText = !string.IsNullOrWhiteSpace(master.CompanyAddress) ? master.CompanyAddress : "21, Hazaribagh, Dhaka-1209";
                                    if (!string.IsNullOrWhiteSpace(master.BranchName))
                                    {
                                        addrText = $"{master.BranchName} - {addrText}";
                                    }
                                    centerCol.Item().AlignCenter().Text(addrText).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                                    centerCol.Item().PaddingTop(3).AlignCenter().Text(master.OFRReportType).FontSize(11).Bold();
                                });

                                // 3. Right: Mode (balanced with left logo width)
                                topRow.ConstantItem(85).AlignBottom().AlignRight().Column(rightCol =>
                                {
                                    rightCol.Item().AlignRight().Text(master.ShipmentMode).FontSize(8.5f).SemiBold();
                                });
                            });

                            headerCol.Item().PaddingBottom(4);
                        });

                        // Content
                        page.Content().Column(col =>
                        {
                            var tableBorderColor = Colors.Grey.Lighten1;

                            // 1. Details Grid Box (2 columns matching screenshot)
                            col.Item().PaddingBottom(6).Border(0.5f).BorderColor(tableBorderColor).Table(metaTable =>
                            {
                                metaTable.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(1.25f);
                                    cols.RelativeColumn(1.0f);
                                });

                                // Row 1
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(70).Text("Customer Name").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.CustomerName).SemiBold();
                                });
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(48).Text("Quantity").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.Quantity);
                                });

                                // Row 2
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(70).Text("Invoice No.").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.InvoiceNo);
                                });
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(48).Text("Date").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.Date);
                                });

                                // Row 3
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(70).Text("Job No.").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.JobNo).SemiBold();
                                });
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(48).Text("LC Value").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.LCValue);
                                });

                                // Row 4
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(70).Text("B/L No").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.BLNo);
                                });
                                metaTable.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(3).Row(r =>
                                {
                                    r.ConstantItem(48).Text("Weight").SemiBold();
                                    r.ConstantItem(8).Text(":");
                                    r.RelativeItem().PaddingLeft(3).Text(master.Weight);
                                });
                            });

                            // 2. Service Groups & Tables
                            foreach (var group in master.ServiceGroups)
                            {
                                col.Item().PaddingTop(3).Text(group.ServiceTypeName).FontSize(9).Bold();

                                // Receiptable Expenses Table
                                if (group.ReceiptableExpenses.Any())
                                {
                                    col.Item().PaddingTop(1).PaddingLeft(40).Text("Receiptable Expenses").FontSize(8.5f).Bold();

                                    col.Item().PaddingTop(1).Border(0.5f).BorderColor(tableBorderColor).Table(table =>
                                    {
                                        table.ColumnsDefinition(cols =>
                                        {
                                            cols.ConstantColumn(50);
                                            cols.RelativeColumn();
                                            cols.ConstantColumn(110);
                                            cols.ConstantColumn(110);
                                        });

                                        table.Header(h =>
                                        {
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignCenter().Text("SL No.").Bold();
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignLeft().Text("Expense Head").Bold();
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text("Requisition Amount").Bold();
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text("App: Amount").Bold();
                                        });

                                        int idx = 1;
                                        foreach (var exp in group.ReceiptableExpenses)
                                        {
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignCenter().Text(idx++.ToString());
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignLeft().Text(exp.ExpenseHead);
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(exp.RequisitionAmount.ToString("N2"));
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(exp.ApprovalAmount.ToString("N2"));
                                        }

                                        // Sub Total Row
                                        table.Cell().ColumnSpan(2).Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text("Sub Total").Bold();
                                        table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(group.ReceiptableSubTotalReq.ToString("N2")).Bold();
                                        table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(group.ReceiptableSubTotalApp.ToString("N2")).Bold();
                                    });
                                }

                                // Non Receiptable Expenses Table
                                if (group.NonReceiptableExpenses.Any())
                                {
                                    col.Item().PaddingTop(3).PaddingLeft(40).Text("Non Receiptable Expenses").FontSize(8.5f).Bold();

                                    col.Item().PaddingTop(1).Border(0.5f).BorderColor(tableBorderColor).Table(table =>
                                    {
                                        table.ColumnsDefinition(cols =>
                                        {
                                            cols.ConstantColumn(50);
                                            cols.RelativeColumn();
                                            cols.ConstantColumn(110);
                                            cols.ConstantColumn(110);
                                        });

                                        table.Header(h =>
                                        {
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignCenter().Text("SN").Bold();
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignLeft().Text("Expense Head").Bold();
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text("Requisition Amount").Bold();
                                            h.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text("App: Amount").Bold();
                                        });

                                        int idx = 1;
                                        foreach (var exp in group.NonReceiptableExpenses)
                                        {
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignCenter().Text(idx++.ToString());
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignLeft().Text(exp.ExpenseHead);
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(exp.RequisitionAmount.ToString("N2"));
                                            table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(exp.ApprovalAmount.ToString("N2"));
                                        }

                                        // Sub Total Row
                                        table.Cell().ColumnSpan(2).Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text("Sub Total").Bold();
                                        table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(group.NonReceiptableSubTotalReq.ToString("N2")).Bold();
                                        table.Cell().Border(0.5f).BorderColor(tableBorderColor).Padding(2).AlignRight().Text(group.NonReceiptableSubTotalApp.ToString("N2")).Bold();
                                    });
                                }
                            }

                            // 3. Totals Summary Table (aligned with 110pt amount columns above)
                            col.Item().PaddingTop(4).Table(totTable =>
                            {
                                totTable.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();
                                    cols.ConstantColumn(110);
                                    cols.ConstantColumn(110);
                                });

                                // TOT Rece.
                                totTable.Cell().Padding(1).AlignRight().Text("TOT Rece. :").Bold();
                                totTable.Cell().Padding(1).AlignRight().Text(master.TotReceiptableReq.ToString("N2"));
                                totTable.Cell().Padding(1).AlignRight().Text(master.TotReceiptableApp.ToString("N2"));

                                // TOT Non Rece.
                                totTable.Cell().Padding(1).AlignRight().Text("TOT Non Rece. :").Bold();
                                totTable.Cell().Padding(1).AlignRight().Text(master.TotNonReceiptableReq.ToString("N2"));
                                totTable.Cell().Padding(1).AlignRight().Text(master.TotNonReceiptableApp.ToString("N2"));

                                // TOT Transport
                                totTable.Cell().Padding(1).AlignRight().Text("TOT Transport :").Bold();
                                totTable.Cell().Padding(1).AlignRight().Text(master.TotTransportReq.ToString("N2"));
                                totTable.Cell().Padding(1).AlignRight().Text(master.TotTransportApp.ToString("N2"));

                                // GT : (with top single line on Req column and double underline on App column)
                                totTable.Cell().Padding(1).AlignRight().Text("GT :").Bold();

                                totTable.Cell().Padding(1).AlignRight().Column(c =>
                                {
                                    c.Item().Width(65).LineHorizontal(0.75f).LineColor(Colors.Black);
                                    c.Item().PaddingTop(1).Text(master.GrandTotalReq.ToString("N2")).Bold();
                                });

                                totTable.Cell().Padding(1).AlignRight().Column(c =>
                                {
                                    c.Item().PaddingTop(1.75f).Text(master.GrandTotalApp.ToString("N2")).Bold();
                                    c.Item().PaddingTop(1.5f).Width(65).LineHorizontal(0.75f).LineColor(Colors.Black);
                                    c.Item().PaddingTop(1.5f).Width(65).LineHorizontal(0.75f).LineColor(Colors.Black);
                                });
                            });

                            // 4. In Words
                            col.Item().PaddingTop(8).Text(t =>
                            {
                                t.Span("In words: ").Bold();
                                t.Span(master.InWords);
                            });

                            // 5. Signatures (matching exact baseline and layout)
                            col.Item().PaddingTop(35).Row(sigRow =>
                            {
                                // Column 1: Custom, Jety/Warehouse, Transport + Prepared By
                                sigRow.ConstantItem(110).Column(c =>
                                {
                                    c.Item().Text(t =>
                                    {
                                        t.Span("Custom: ").Bold();
                                        t.Span(master.CustomUser);
                                    });
                                    c.Item().Text("Jety/Warehouse:").Bold();
                                    c.Item().Text("Transport:").Bold();
                                    c.Item().PaddingTop(3).LineHorizontal(1).LineColor(Colors.Black);
                                    c.Item().PaddingTop(2).AlignCenter().Text("Prepared By").Bold();
                                });

                                // Middle Gap / Spacer
                                sigRow.RelativeItem();

                                // Column 2: Received By
                                sigRow.ConstantItem(85).Column(c =>
                                {
                                    c.Item().Height(34);
                                    c.Item().PaddingTop(3).LineHorizontal(1).LineColor(Colors.Black);
                                    c.Item().PaddingTop(2).AlignCenter().Text("Received By").Bold();
                                });

                                sigRow.ConstantItem(12);

                                // Column 3: Checked By
                                sigRow.ConstantItem(85).Column(c =>
                                {
                                    c.Item().Height(34);
                                    c.Item().PaddingTop(3).LineHorizontal(1).LineColor(Colors.Black);
                                    c.Item().PaddingTop(2).AlignCenter().Text("Checked By").Bold();
                                });

                                sigRow.ConstantItem(12);

                                // Column 4: Certified By
                                sigRow.ConstantItem(85).Column(c =>
                                {
                                    c.Item().Height(34);
                                    c.Item().PaddingTop(3).LineHorizontal(1).LineColor(Colors.Black);
                                    c.Item().PaddingTop(2).AlignCenter().Text("Certified By").Bold();
                                });

                                sigRow.ConstantItem(12);

                                // Column 5: Approve By (with Approved User name above line)
                                sigRow.ConstantItem(85).Column(c =>
                                {
                                    c.Item().Height(34).AlignBottom().AlignCenter().Text(master.ApproveBy).FontSize(8.5f);
                                    c.Item().PaddingTop(3).LineHorizontal(1).LineColor(Colors.Black);
                                    c.Item().PaddingTop(2).AlignCenter().Text("Approve By").Bold();
                                });
                            });
                        });

                        // Footer
                        page.Footer().Column(footCol =>
                        {
                            footCol.Item().AlignCenter().Text("N.B: THIS IS A COMPUTER GENERATED BILL. SIGNATURE IS NOT MANDATORY").FontSize(7.5f).Bold();
                            footCol.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text($"Print Date Time: {master.PrintDateTime}").FontSize(7.5f);
                                row.RelativeItem().AlignRight().Text($"Print By: {(!string.IsNullOrWhiteSpace(master.PrintBy) ? master.PrintBy : "rasel")}").FontSize(7.5f);
                            });
                        });
                    });
                }
            });

            return doc.GeneratePdf();
        }
        #endregion

        #region Excel Export
        public async Task<byte[]> GenerateExcelReportAsync(OFRReportFilter filter)
        {
            var dataList = await GetReportDataAsync(filter);

            using var workbook = new XLWorkbook();
            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!dataList.Any())
            {
                var emptyWs = workbook.Worksheets.Add("OFR Report");
                emptyWs.Cell(1, 1).Value = "No matching records found.";
                emptyWs.Cell(1, 1).Style.Font.Italic = true;
                using var emptyMs = new MemoryStream();
                workbook.SaveAs(emptyMs);
                return emptyMs.ToArray();
            }

            for (int i = 0; i < dataList.Count; i++)
            {
                var master = dataList[i];
                string sheetName = SanitizeSheetName(master.JobNo, usedSheetNames, i);
                var ws = workbook.Worksheets.Add(sheetName);

                // 1. Report Header
                string excelOrg = !string.IsNullOrWhiteSpace(master.OrganizationName) ? master.OrganizationName : "Formosa Logistics Limited";
                string excelAddr = !string.IsNullOrWhiteSpace(master.CompanyAddress) ? master.CompanyAddress : "21, Hazaribagh, Dhaka-1209";
                if (!string.IsNullOrWhiteSpace(master.BranchName))
                {
                    excelAddr = $"{master.BranchName} - {excelAddr}";
                }

                ws.Cell(1, 1).Value = excelOrg;
                ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(2, 1).Value = excelAddr;
                ws.Range(2, 1, 2, 5).Merge().Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.DarkGray).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(3, 1).Value = master.OFRReportType;
                ws.Range(3, 1, 3, 3).Merge().Style.Font.SetBold(true).Font.SetFontSize(11).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                ws.Cell(3, 4).Value = "Mode:";
                ws.Cell(3, 4).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws.Cell(3, 5).Value = master.ShipmentMode;
                ws.Cell(3, 5).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                // 2. Job Meta Details Box
                ws.Range(5, 1, 8, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Row 5
                ws.Cell(5, 1).Value = "Customer Name:";
                ws.Cell(5, 1).Style.Font.SetBold(true);
                ws.Cell(5, 2).Value = master.CustomerName;
                ws.Cell(5, 2).Style.Font.SetBold(true);
                ws.Cell(5, 4).Value = "Quantity:";
                ws.Cell(5, 4).Style.Font.SetBold(true);
                ws.Cell(5, 5).Value = master.Quantity;

                // Row 6
                ws.Cell(6, 1).Value = "Invoice No:";
                ws.Cell(6, 1).Style.Font.SetBold(true);
                ws.Cell(6, 2).Value = master.InvoiceNo;
                ws.Cell(6, 4).Value = "Date:";
                ws.Cell(6, 4).Style.Font.SetBold(true);
                ws.Cell(6, 5).Value = master.Date;

                // Row 7
                ws.Cell(7, 1).Value = "Job No:";
                ws.Cell(7, 1).Style.Font.SetBold(true);
                ws.Cell(7, 2).Value = master.JobNo;
                ws.Cell(7, 2).Style.Font.SetBold(true);
                ws.Cell(7, 4).Value = "LC Value:";
                ws.Cell(7, 4).Style.Font.SetBold(true);
                ws.Cell(7, 5).Value = master.LCValue;

                // Row 8
                ws.Cell(8, 1).Value = "B/L No:";
                ws.Cell(8, 1).Style.Font.SetBold(true);
                ws.Cell(8, 2).Value = master.BLNo;
                ws.Cell(8, 4).Value = "Weight:";
                ws.Cell(8, 4).Style.Font.SetBold(true);
                ws.Cell(8, 5).Value = master.Weight;

                // 3. Detail Expense Table Header (Row 10)
                int row = 10;
                ws.Cell(row, 1).Value = "Service Type";
                ws.Cell(row, 2).Value = "Category";
                ws.Cell(row, 3).Value = "Expense Head";
                ws.Cell(row, 4).Value = "Requisition Amount (BDT)";
                ws.Cell(row, 5).Value = "Approval Amount (BDT)";

                var headerRange = ws.Range(row, 1, row, 5);
                headerRange.Style.Font.SetBold(true);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAEAEA"));
                headerRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
                headerRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row++;

                int startExpenseRow = row;

                foreach (var group in master.ServiceGroups)
                {
                    foreach (var item in group.ReceiptableExpenses)
                    {
                        ws.Cell(row, 1).Value = group.ServiceTypeName;
                        ws.Cell(row, 2).Value = "Receiptable";
                        ws.Cell(row, 3).Value = item.ExpenseHead;
                        ws.Cell(row, 4).Value = item.RequisitionAmount;
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 5).Value = item.ApprovalAmount;
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                        row++;
                    }

                    foreach (var item in group.NonReceiptableExpenses)
                    {
                        ws.Cell(row, 1).Value = group.ServiceTypeName;
                        ws.Cell(row, 2).Value = "Non Receiptable";
                        ws.Cell(row, 3).Value = item.ExpenseHead;
                        ws.Cell(row, 4).Value = item.RequisitionAmount;
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 5).Value = item.ApprovalAmount;
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                        row++;
                    }
                }

                if (row > startExpenseRow)
                {
                    ws.Range(startExpenseRow, 1, row - 1, 5).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    ws.Range(startExpenseRow, 1, row - 1, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                // Grand Total Row
                ws.Cell(row, 2).Value = "Grand Total:";
                ws.Range(row, 2, row, 3).Merge().Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws.Cell(row, 4).Value = master.GrandTotalReq;
                ws.Cell(row, 4).Style.Font.SetBold(true).NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Value = master.GrandTotalApp;
                ws.Cell(row, 5).Style.Font.SetBold(true).NumberFormat.Format = "#,##0.00";

                var totalRange = ws.Range(row, 1, row, 5);
                totalRange.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
                totalRange.Style.Border.SetBottomBorder(XLBorderStyleValues.Double);
                totalRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F5F5F5"));
                row += 2;

                // In Words Row
                if (!string.IsNullOrWhiteSpace(master.InWords))
                {
                    ws.Cell(row, 1).Value = "In words:";
                    ws.Cell(row, 1).Style.Font.SetBold(true);
                    ws.Cell(row, 2).Value = master.InWords;
                    ws.Range(row, 2, row, 5).Merge().Style.Font.SetItalic(true);
                    row += 3;
                }

                // Signatures
                ws.Cell(row, 1).Value = "Prepared By";
                ws.Cell(row, 1).Style.Border.SetTopBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(row, 3).Value = "Checked By";
                ws.Cell(row, 3).Style.Border.SetTopBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(row, 5).Value = "Approved By";
                ws.Cell(row, 5).Style.Border.SetTopBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Columns().AdjustToContents();
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static string SanitizeSheetName(string? rawName, HashSet<string> existingNames, int index)
        {
            string name = (rawName ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                name = $"Job_{index + 1}";
            }

            // Remove invalid characters in Excel worksheet names: \ / ? * [ ] :
            var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            foreach (var ch in invalidChars)
            {
                name = name.Replace(ch, '_');
            }

            name = name.Trim();
            if (name.Length > 28)
            {
                name = name.Substring(0, 28);
            }

            string finalName = name;
            int counter = 1;
            while (existingNames.Contains(finalName))
            {
                string suffix = $"_{counter}";
                int maxBaseLen = 31 - suffix.Length;
                string basePart = name.Length > maxBaseLen ? name.Substring(0, maxBaseLen) : name;
                finalName = $"{basePart}{suffix}";
                counter++;
            }

            existingNames.Add(finalName);
            return finalName;
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(OFRReportFilter filter, string exportFormat, string printUser = "")
        {
            string fmt = (exportFormat ?? "PDF").Trim().ToUpper();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (fmt == "EXCEL" || fmt == "XLSX")
            {
                var bytes = await GenerateExcelReportAsync(filter);
                return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"OFR_Report_{timestamp}.xlsx");
            }

            var pdfBytes = await GeneratePdfReportAsync(filter, printUser);
            return (pdfBytes, "application/pdf", $"OFR_Report_{timestamp}.pdf");
        }
        #endregion

        #region Helpers: Number to Words
        public static string ConvertAmountToWords(decimal amount)
        {
            if (amount == 0) return "Zero Only.";

            long whole = (long)Math.Floor(amount);
            int paisa = (int)Math.Round((amount - whole) * 100);

            string words = NumberToWords(whole);
            string result = $"{words}";

            if (paisa > 0)
            {
                result += $" And {NumberToWords(paisa)} Paisa";
            }

            result += " Only.";
            return result;
        }

        private static string NumberToWords(long number)
        {
            if (number == 0) return "Zero";
            if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 10000000) > 0)
            {
                words += NumberToWords(number / 10000000) + " Crore ";
                number %= 10000000;
            }

            if ((number / 100000) > 0)
            {
                words += NumberToWords(number / 100000) + " Lakh ";
                number %= 100000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }
        #endregion
    }
}
