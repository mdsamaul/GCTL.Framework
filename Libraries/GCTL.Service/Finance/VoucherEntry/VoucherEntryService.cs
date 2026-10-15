using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.VoucherEntryVM;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.VoucherEntry
{
    public class VoucherEntryService : IVoucherEntryService
    {
        private readonly GCTL_ERP_DBContext _context;
        private readonly IDbConnection _dbConnection;

        public VoucherEntryService(
            GCTL_ERP_DBContext context,
            IDbConnection dbConnection)
        {
            _context = context;
            _dbConnection = dbConnection;
        }

        #region Get All (Filtered, Sorted, Paginated + KPI Summary via Stored Procedure)
        public async Task<VoucherEntryListResultVM> GetAllAsync(VoucherEntryFilterVM filter)
        {
            var result = new VoucherEntryListResultVM
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            try
            {
                var parameters = new DynamicParameters();
                string? vTypeParam = string.IsNullOrWhiteSpace(filter.VoucherType) ? null : filter.VoucherType.Trim();
                string? statusParam = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim();
                string? searchParam = string.IsNullOrWhiteSpace(filter.SearchTerm) ? null : filter.SearchTerm.Trim();

                parameters.Add("@FromDate", filter.FromDate?.Date);
                parameters.Add("@ToDate", filter.ToDate?.Date);
                parameters.Add("@VoucherType", vTypeParam);
                parameters.Add("@Status", statusParam);
                parameters.Add("@SearchTerm", searchParam);
                parameters.Add("@BranchCode", string.IsNullOrWhiteSpace(filter.BranchCode) ? null : filter.BranchCode.Trim());
                parameters.Add("@CompanyCode", string.IsNullOrWhiteSpace(filter.CompanyCode) ? null : filter.CompanyCode.Trim());
                parameters.Add("@PageNumber", result.PageNumber);
                parameters.Add("@PageSize", result.PageSize);
                parameters.Add("@SortColumn", filter.SortColumn ?? "VoucherDate");
                parameters.Add("@SortOrder", filter.SortOrder ?? "DESC");

                using var multi = await _dbConnection.QueryMultipleAsync("dbo.sp_GetVoucherList", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                var pagedRows = (await multi.ReadAsync<dynamic>()).ToList();
                var summaryRow = await multi.ReadFirstOrDefaultAsync<dynamic>();

                static decimal ToDecimal(object? val) => val == null || val is DBNull ? 0m : Convert.ToDecimal(val);
                static int ToInt(object? val) => val == null || val is DBNull ? 0 : Convert.ToInt32(val);
                static string ToStringSafe(object? val) => val == null || val is DBNull ? string.Empty : Convert.ToString(val)!;
                static DateTime ToDateTime(object? val) => val == null || val is DBNull ? DateTime.Today : Convert.ToDateTime(val);

                if (summaryRow != null)
                {
                    var sDict = (IDictionary<string, object>)summaryRow;
                    result.TotalCount = ToInt(sDict.TryGetValue("TotalVouchers", out var tv) ? tv : 0);
                    result.Summary.TotalVouchers = result.TotalCount;
                    result.Summary.VouchersToday = ToInt(sDict.TryGetValue("VouchersToday", out var vtd) ? vtd : 0);
                    result.Summary.VouchersThisMonth = ToInt(sDict.TryGetValue("VouchersThisMonth", out var vtm) ? vtm : 0);
                    result.Summary.TotalDebit = ToDecimal(sDict.TryGetValue("TotalDebit", out var td) ? td : 0);
                    result.Summary.TotalCredit = result.Summary.TotalDebit;
                    result.Summary.CashBalance = ToDecimal(sDict.TryGetValue("CashBalance", out var cb) ? cb : 0);
                    result.Summary.BankBalance = ToDecimal(sDict.TryGetValue("BankBalance", out var bb) ? bb : 0);
                    result.Summary.BrvCount = ToInt(sDict.TryGetValue("BrvCount", out var brvc) ? brvc : 0);
                    result.Summary.BrvAmount = ToDecimal(sDict.TryGetValue("BrvAmount", out var brva) ? brva : 0);
                    result.Summary.BpvCount = ToInt(sDict.TryGetValue("BpvCount", out var bpvc) ? bpvc : 0);
                    result.Summary.BpvAmount = ToDecimal(sDict.TryGetValue("BpvAmount", out var bpva) ? bpva : 0);
                    result.Summary.CrvCount = ToInt(sDict.TryGetValue("CrvCount", out var crvc) ? crvc : 0);
                    result.Summary.CrvAmount = ToDecimal(sDict.TryGetValue("CrvAmount", out var crva) ? crva : 0);
                    result.Summary.CpvCount = ToInt(sDict.TryGetValue("CpvCount", out var cpvc) ? cpvc : 0);
                    result.Summary.CpvAmount = ToDecimal(sDict.TryGetValue("CpvAmount", out var cpva) ? cpva : 0);
                    result.Summary.JvCount = ToInt(sDict.TryGetValue("JvCount", out var jvc) ? jvc : 0);
                    result.Summary.JvAmount = ToDecimal(sDict.TryGetValue("JvAmount", out var jva) ? jva : 0);
                    result.Summary.CvCount = ToInt(sDict.TryGetValue("CvCount", out var cvc) ? cvc : 0);
                    result.Summary.CvAmount = ToDecimal(sDict.TryGetValue("CvAmount", out var cva) ? cva : 0);
                }

                foreach (var row in pagedRows)
                {
                    var rDict = (IDictionary<string, object>)row;
                    string vTypeCode = ToStringSafe(rDict.TryGetValue("VoucherType_Code", out var vtc) ? vtc : null);
                    string vTypeName = ToStringSafe(rDict.TryGetValue("VoucherTypeName", out var vtn) ? vtn : null);
                    if (string.IsNullOrEmpty(vTypeName)) vTypeName = GetVoucherTypeName(vTypeCode);

                    decimal totalAmt = ToDecimal(rDict.TryGetValue("TotalAmount", out var ta) ? ta : 0);
                    string bName = ToStringSafe(rDict.TryGetValue("BranchName", out var bn) ? bn : null);
                    if (string.IsNullOrEmpty(bName)) bName = "Head Office";
                    string status = ToStringSafe(rDict.TryGetValue("IsApproved", out var ia) ? ia : null);
                    if (string.IsNullOrEmpty(status)) status = "Pending Approval";

                    var vm = new VoucherEntryVM
                    {
                        AutoId = rDict.TryGetValue("autoId", out var aid) && aid != null && !(aid is DBNull) ? Convert.ToDecimal(aid) : null,
                        VoucherEntryCodeNo = ToStringSafe(rDict.TryGetValue("VoucherEntryCodeNo", out var vcn) ? vcn : null),
                        VoucherNo = ToStringSafe(rDict.TryGetValue("VoucherNo", out var vn) ? vn : null),
                        VoucherType_Code = vTypeCode,
                        VoucherTypeName = vTypeName,
                        VoucherDate = ToDateTime(rDict.TryGetValue("VoucherDate", out var vd) ? vd : null),
                        CompanyCode = ToStringSafe(rDict.TryGetValue("CompanyCode", out var cc) ? cc : null),
                        Main_CompanyCode = ToStringSafe(rDict.TryGetValue("Main_CompanyCode", out var mcc) ? mcc : null),
                        BranchCode = ToStringSafe(rDict.TryGetValue("BranchCode", out var bc) ? bc : null),
                        BranchName = bName,
                        FinancialYear = ToStringSafe(rDict.TryGetValue("FinancialYear", out var fy) ? fy : null),
                        Reference = ToStringSafe(rDict.TryGetValue("Reference", out var rf) ? rf : null),
                        InvoiceNo = ToStringSafe(rDict.TryGetValue("InvoiceNo", out var inv) ? inv : null),
                        Narration = ToStringSafe(rDict.TryGetValue("Narration", out var nr) ? nr : null),
                        IsApproved = status,
                        TotalDebit = totalAmt,
                        TotalCredit = totalAmt
                    };
                    result.Items.Add(vm);
                }

                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }
        #endregion

        #region Get By Id
        public async Task<CommonReturnViewModel> GetByIdAsync(string voucherEntryCodeNo)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await _context.AccVoucherEntries
                    .Where(x => (x.IsActive == null || x.IsActive != "0") && (x.VoucherEntryCodeNo == voucherEntryCodeNo || x.VoucherNo == voucherEntryCodeNo))
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Voucher not found.";
                    return res;
                }

                var details = await _context.AccVoucherEntryDetails
                    .Where(d => d.VoucherEntryCodeNo == entity.VoucherEntryCodeNo && (d.IsActive == null || d.IsActive != "0"))
                    .OrderBy(d => d.AutoId)
                    .AsNoTracking()
                    .ToListAsync();

                var groups = await _context.AccGroupLedgers.AsNoTracking().ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);
                var controls = await _context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName);
                var subControls = await _context.AccSubControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubControlLedgerCodeNo, x => x.SubControlLedgerName);
                var subSubs = await _context.AccSubSubsidiaryLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubSubsidiaryLedgerCodeNo, x => x.SubSubsidiaryLedgerName);

                var glList = await _context.AccGeneralLedgers
                    .AsNoTracking()
                    .ToListAsync();

                var accountDict = new Dictionary<string, (string Name, string Hierarchy)>(StringComparer.OrdinalIgnoreCase);
                foreach (var g in glList)
                {
                    if (!string.IsNullOrWhiteSpace(g.GeneralLedgerCodeNo))
                    {
                        var key = g.GeneralLedgerCodeNo.Trim();
                        var hier = $"{(g.GroupLedgerCodeNo != null && groups.TryGetValue(g.GroupLedgerCodeNo, out var grpN) ? grpN : "")} › {(g.ControlLedgerCodeNo != null && controls.TryGetValue(g.ControlLedgerCodeNo, out var ctlN) ? ctlN : "")} › {(g.SubControlLedgerCodeNo != null && subControls.TryGetValue(g.SubControlLedgerCodeNo, out var sctlN) ? sctlN : "")} › {(g.SubSubsidiaryLedgerCodeNo != null && subSubs.TryGetValue(g.SubSubsidiaryLedgerCodeNo, out var ssN) ? ssN : "")} › {g.GeneralLedgerName}";
                        accountDict[key] = (g.GeneralLedgerName?.Trim() ?? key, hier);
                    }
                }

                var allSubSubs = await _context.AccSubSubsidiaryLedgers.AsNoTracking().ToListAsync();
                foreach (var ss in allSubSubs)
                {
                    if (!string.IsNullOrWhiteSpace(ss.SubSubsidiaryLedgerCodeNo))
                    {
                        var key = ss.SubSubsidiaryLedgerCodeNo.Trim();
                        if (!accountDict.ContainsKey(key))
                        {
                            accountDict[key] = (ss.SubSubsidiaryLedgerName?.Trim() ?? key, ss.SubSubsidiaryLedgerName?.Trim() ?? key);
                        }
                    }
                }

                var cashCodes = await GetCashAccountCodesAsync();
                var bankCodes = await GetBankAccountCodesAsync();

                decimal debit = details.Sum(d => d.DebitAmount ?? 0m);
                decimal credit = details.Sum(d => d.CreditAmount ?? 0m);

                string companyName = "BAY SCHOOL & COLLEGE";
                string companyAddress = "";
                if (!string.IsNullOrWhiteSpace(entity.CompanyCode))
                {
                    var comp = await _context.CoreCompanies.FirstOrDefaultAsync(c => c.CompanyCode == entity.CompanyCode);
                    if (comp != null)
                    {
                        if (!string.IsNullOrWhiteSpace(comp.CompanyName)) companyName = comp.CompanyName;
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(comp.Address1)) parts.Add(comp.Address1);
                        if (!string.IsNullOrWhiteSpace(comp.Phone1)) parts.Add($"Phone: {comp.Phone1}");
                        if (!string.IsNullOrWhiteSpace(comp.Email)) parts.Add($"Email: {comp.Email}");
                        companyAddress = string.Join(" ", parts);
                    }
                }

                string branchName = "Head Office";
                if (!string.IsNullOrWhiteSpace(entity.BranchCode))
                {
                    var br = await _context.CoreBranches.FirstOrDefaultAsync(b => b.BranchCode == entity.BranchCode);
                    if (br != null && !string.IsNullOrWhiteSpace(br.BranchName)) branchName = br.BranchName;
                }

                string preparedBy = entity.Luser?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(preparedBy))
                {
                    try
                    {
                        var resolvedName = await GetUserFullNameAsync(preparedBy, preparedBy);
                        if (!string.IsNullOrWhiteSpace(resolvedName) && !resolvedName.Contains("@"))
                        {
                            preparedBy = resolvedName;
                        }
                    }
                    catch { }
                }

                var vType = await _context.AccVoucherTypes.FirstOrDefaultAsync(t => t.VoucherTypeCode == entity.VoucherTypeCode);

                var vm = new VoucherEntryVM
                {
                    AutoId = entity.AutoId,
                    VoucherEntryCodeNo = entity.VoucherEntryCodeNo,
                    VoucherNo = entity.VoucherNo,
                    VoucherType_Code = entity.VoucherTypeCode,
                    VoucherTypeName = vType?.Description ?? vType?.VoucherTypeName ?? GetVoucherTypeName(entity.VoucherTypeCode),
                    VoucherDate = entity.VoucherDate,
                    CompanyCode = entity.CompanyCode,
                    Main_CompanyCode = entity.MainCompanyCode,
                    CompanyName = companyName,
                    CompanyAddress = companyAddress,
                    BranchCode = entity.BranchCode,
                    BranchName = branchName,
                    FinancialYear = entity.FinancialYear,
                    Reference = entity.Reference,
                    InvoiceNo = entity.InvoiceNo,
                    Narration = entity.Narration,
                    IsApproved = string.IsNullOrWhiteSpace(entity.IsApproved) ? "Pending Approval" : entity.IsApproved,
                    PreparedBy = preparedBy,
                    ApprovedBy = entity.ApprovedBy,
                    ApprovedDate = entity.ApprovedDate,
                    TotalDebit = debit,
                    TotalCredit = credit,
                    AmountInWords = NumberToWordsHelper.ConvertToTakaWords(debit),
                    Details = details.Select(d =>
                    {
                        var accKey = d.AccCode?.Trim() ?? "";
                        accountDict.TryGetValue(accKey, out var accInfo);
                        string resolvedAccName = !string.IsNullOrWhiteSpace(accInfo.Name) 
                            ? accInfo.Name 
                            : (!string.IsNullOrWhiteSpace(d.Description) ? d.Description.Trim() : accKey);

                        string? lineDesc = d.Description?.Trim();
                        var narr = (entity.Narration ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(lineDesc) && !string.IsNullOrWhiteSpace(narr))
                        {
                            if (string.Equals(lineDesc, narr, StringComparison.OrdinalIgnoreCase) ||
                                (narr.Length >= 200 && lineDesc == narr.Substring(0, 200)) ||
                                narr.StartsWith(lineDesc, StringComparison.OrdinalIgnoreCase))
                            {
                                lineDesc = string.Empty;
                            }
                        }

                        DateTime? resolvedChequeDate = d.ChequeDate;
                        if (resolvedChequeDate.HasValue && resolvedChequeDate.Value.Year <= 1950)
                        {
                            resolvedChequeDate = null;
                        }

                        return new VoucherEntryDetailVM
                        {
                            AutoId = d.AutoId,
                            VoucherEntryDetailsCodeNo = d.VoucherEntryDetailsCodeNo,
                            VoucherEntryCodeNo = d.VoucherEntryCodeNo,
                            AccCode = d.AccCode,
                            AccName = resolvedAccName,
                            AccountPath = accInfo.Hierarchy,
                            TrType = d.TrType,
                            Description = lineDesc ?? string.Empty,
                            DebitAmount = d.DebitAmount ?? 0m,
                            CreditAmount = d.CreditAmount ?? 0m,
                            ChequeNo = d.ChequeNo,
                            ChequeDate = resolvedChequeDate,
                            BankName = d.BankName,
                            Branch = d.Branch,
                            IsCash = cashCodes.Contains(d.AccCode),
                            IsBank = bankCodes.Contains(d.AccCode)
                        };
                    }).ToList()
                };

                res.Success = true;
                res.Data = vm;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }

            return res;
        }

        public async Task<CommonReturnViewModel> GetByVoucherNoAsync(string voucherNo)
        {
            return await GetByIdAsync(voucherNo);
        }

        public async Task<CommonReturnViewModel> GetVoucherPrintAsync(string voucherEntryCodeNo)
        {
            return await GetByIdAsync(voucherEntryCodeNo);
        }
        #endregion

        #region Save (Add or Update)
        public async Task<CommonReturnViewModel> SaveAsync(VoucherEntryVM model, int? userId = null, string? ip = null, string? mac = null)
        {
            var res = new CommonReturnViewModel();

            try
            {
                if (string.IsNullOrWhiteSpace(model.VoucherType_Code))
                {
                    res.Success = false;
                    res.Message = "Please select a Voucher Type.";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(model.Narration))
                {
                    res.Success = false;
                    res.Message = "Narration is required.";
                    return res;
                }

                var validLines = model.Details?
                    .Where(x => !string.IsNullOrWhiteSpace(x.AccCode) && ((x.DebitAmount ?? 0) > 0 || (x.CreditAmount ?? 0) > 0))
                    .ToList() ?? new List<VoucherEntryDetailVM>();

                if (validLines.Count < 2)
                {
                    res.Success = false;
                    res.Message = "A voucher requires at least two transaction lines.";
                    return res;
                }

                decimal totalDebit = validLines.Sum(x => x.DebitAmount ?? 0m);
                decimal totalCredit = validLines.Sum(x => x.CreditAmount ?? 0m);

                if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                {
                    res.Success = false;
                    res.Message = $"Debit (৳{totalDebit:N2}) and Credit (৳{totalCredit:N2}) must be equal.";
                    return res;
                }

                var cashCodes = await GetCashAccountCodesAsync();
                var bankCodes = await GetBankAccountCodesAsync();

                bool hasBankDr = validLines.Any(x => bankCodes.Contains(x.AccCode) && (x.DebitAmount ?? 0) > 0);
                bool hasBankCr = validLines.Any(x => bankCodes.Contains(x.AccCode) && (x.CreditAmount ?? 0) > 0);
                bool hasCashDr = validLines.Any(x => cashCodes.Contains(x.AccCode) && (x.DebitAmount ?? 0) > 0);
                bool hasCashCr = validLines.Any(x => cashCodes.Contains(x.AccCode) && (x.CreditAmount ?? 0) > 0);
                bool anyCashBank = validLines.Any(x => cashCodes.Contains(x.AccCode) || bankCodes.Contains(x.AccCode));
                bool allCashBank = validLines.All(x => cashCodes.Contains(x.AccCode) || bankCodes.Contains(x.AccCode));

                var vType = await _context.AccVoucherTypes
                    .FirstOrDefaultAsync(t => t.VoucherTypeCode == model.VoucherType_Code || t.VoucherTypeName == model.VoucherType_Code || t.Prefix == model.VoucherType_Code);

                string dbTypeCode = vType?.VoucherTypeCode ?? model.VoucherType_Code;
                string typeShort = (vType?.VoucherTypeName ?? vType?.Prefix ?? model.VoucherType_Code).ToUpper();

                switch (typeShort)
                {
                    case "BRV":
                        if (!hasBankDr)
                        {
                            res.Success = false;
                            res.Message = "Bank Receipt Voucher (BRV) requires a Bank account on the Debit side.";
                            return res;
                        }
                        break;
                    case "BPV":
                        if (!hasBankCr)
                        {
                            res.Success = false;
                            res.Message = "Bank Payment Voucher (BPV) requires a Bank account on the Credit side.";
                            return res;
                        }
                        break;
                    case "CRV":
                        if (!hasCashDr)
                        {
                            res.Success = false;
                            res.Message = "Cash Receipt Voucher (CRV) requires a Cash account on the Debit side.";
                            return res;
                        }
                        break;
                    case "CPV":
                        if (!hasCashCr)
                        {
                            res.Success = false;
                            res.Message = "Cash Payment Voucher (CPV) requires a Cash account on the Credit side.";
                            return res;
                        }
                        break;
                    case "CV":
                        if (!allCashBank)
                        {
                            res.Success = false;
                            res.Message = "Contra Voucher (CV) can only contain Cash and Bank accounts.";
                            return res;
                        }
                        break;
                    case "JV":
                        if (anyCashBank)
                        {
                            res.Success = false;
                            res.Message = "Journal Voucher (JV) cannot contain Cash or Bank accounts.";
                            return res;
                        }
                        break;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                var existing = !string.IsNullOrWhiteSpace(model.VoucherEntryCodeNo)
                    ? await _context.AccVoucherEntries
                        .FirstOrDefaultAsync(x => x.VoucherEntryCodeNo == model.VoucherEntryCodeNo || x.VoucherNo == model.VoucherNo)
                    : (!string.IsNullOrWhiteSpace(model.VoucherNo)
                        ? await _context.AccVoucherEntries
                            .FirstOrDefaultAsync(x => x.VoucherNo == model.VoucherNo)
                        : null);

                if (existing != null)
                {
                    existing.VoucherDate = model.VoucherDate.Date;
                    existing.PeriodCodeNo = model.VoucherDate.Month.ToString("D2");
                    existing.Narration = model.Narration.Trim();
                    existing.Reference = model.Reference;
                    existing.InvoiceNo = !string.IsNullOrWhiteSpace(model.InvoiceNo) ? model.InvoiceNo : model.Reference;
                    existing.BranchCode = model.BranchCode;
                    existing.CompanyCode = model.CompanyCode ?? model.Main_CompanyCode ?? "1";
                    existing.MainCompanyCode = model.Main_CompanyCode ?? model.CompanyCode ?? "1";
                    existing.FinancialYear = model.FinancialYear ?? GetFinancialYear(model.VoucherDate);
                    if (existing.IsApproved != "Posted" && existing.IsApproved != "Y" && existing.IsApproved != "Approved")
                    {
                        existing.IsApproved = "Pending Approval";
                    }
                    existing.ModifyDate = DateTime.Now;
                    existing.Luser = userId?.ToString();
                    existing.Lip = ip;
                    existing.Lmac = mac;

                    var oldDetails = await _context.AccVoucherEntryDetails
                        .Where(x => x.VoucherEntryCodeNo == existing.VoucherEntryCodeNo)
                        .ToListAsync();
                    if (oldDetails.Any())
                    {
                        _context.AccVoucherEntryDetails.RemoveRange(oldDetails);
                    }

                    int lineIdx = 1;
                    foreach (var line in validLines)
                    {
                        decimal dr = line.DebitAmount ?? 0m;
                        decimal cr = line.CreditAmount ?? 0m;
                        string trType = dr > 0 ? "Dr" : "Cr";

                        var dEntity = new AccVoucherEntryDetail
                        {
                            VoucherEntryDetailsCodeNo = $"{existing.VoucherEntryCodeNo}-{lineIdx:D3}",
                            VoucherEntryCodeNo = existing.VoucherEntryCodeNo,
                            AccCode = line.AccCode,
                            TrType = trType,
                            Description = line.Description?.Trim() ?? string.Empty,
                            DebitAmount = dr,
                            CreditAmount = cr,
                            ChequeNo = line.ChequeNo,
                            ChequeDate = line.ChequeDate,
                            BankName = line.BankName,
                            Branch = line.Branch,
                            IsActive = "1",
                            Luser = userId?.ToString(),
                            Ldate = DateTime.Now,
                            Lip = ip,
                            Lmac = mac
                        };
                        _context.AccVoucherEntryDetails.Add(dEntity);
                        lineIdx++;
                    }

                    if (!string.IsNullOrWhiteSpace(model.InvoiceNo))
                    {
                        var existPay = await _context.Acc_VoucherInvoicePaymentReceived
                            .FirstOrDefaultAsync(p => p.VoucherNo == existing.VoucherNo);
                        if (existPay != null)
                        {
                            existPay.InvoiceNo = model.InvoiceNo;
                            existPay.Amount = totalDebit;
                            existPay.ModifyDate = DateTime.Now;
                            existPay.LUser = userId?.ToString();
                            existPay.LIP = ip;
                            existPay.LMAC = mac;
                        }
                        else
                        {
                            _context.Acc_VoucherInvoicePaymentReceived.Add(new Acc_VoucherInvoicePaymentReceived
                            {
                                VoucherNo = existing.VoucherNo,
                                InvoiceNo = model.InvoiceNo,
                                Amount = totalDebit,
                                IsActive = "1",
                                LUser = userId?.ToString(),
                                LDate = DateTime.Now,
                                LIP = ip,
                                LMAC = mac
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    res.Success = true;
                    res.Message = $"Voucher {existing.VoucherNo} updated successfully.";
                    res.Data = existing.VoucherNo;
                }
                else
                {
                    string fy = model.FinancialYear ?? GetFinancialYear(model.VoucherDate);
                    string voucherNo = await GetNextVoucherNoAsync(model.VoucherType_Code, fy);
                    string voucherCodeNo = await GetNextVoucherCodeNoAsync(fy);

                    var newVoucher = new AccVoucherEntry
                    {
                        VoucherEntryCodeNo = voucherCodeNo,
                        VoucherNo = voucherNo,
                        VoucherTypeCode = dbTypeCode,
                        VoucherDate = model.VoucherDate.Date,
                        CostCenterCodeNo = "0",
                        PeriodCodeNo = model.VoucherDate.Month.ToString("D2"),
                        FinancialYear = fy,
                        Narration = model.Narration.Trim(),
                        Reference = model.Reference,
                        InvoiceNo = !string.IsNullOrWhiteSpace(model.InvoiceNo) ? model.InvoiceNo : model.Reference,
                        BranchCode = model.BranchCode ?? "1",
                        CompanyCode = model.CompanyCode ?? model.Main_CompanyCode ?? "1",
                        MainCompanyCode = model.Main_CompanyCode ?? model.CompanyCode ?? "1",
                        IsApproved = "Pending Approval",
                        IsActive = "1",
                        Luser = userId?.ToString(),
                        Ldate = DateTime.Now,
                        Lip = ip,
                        Lmac = mac
                    };

                    _context.AccVoucherEntries.Add(newVoucher);

                    int lineIdx = 1;
                    foreach (var line in validLines)
                    {
                        decimal dr = line.DebitAmount ?? 0m;
                        decimal cr = line.CreditAmount ?? 0m;
                        string trType = dr > 0 ? "Dr" : "Cr";

                        var dEntity = new AccVoucherEntryDetail
                        {
                            VoucherEntryDetailsCodeNo = $"{voucherCodeNo}-{lineIdx:D3}",
                            VoucherEntryCodeNo = voucherCodeNo,
                            AccCode = line.AccCode,
                            TrType = trType,
                            Description = line.Description?.Trim() ?? string.Empty,
                            DebitAmount = dr,
                            CreditAmount = cr,
                            ChequeNo = line.ChequeNo,
                            ChequeDate = line.ChequeDate,
                            BankName = line.BankName,
                            Branch = line.Branch,
                            IsActive = "1",
                            Luser = userId?.ToString(),
                            Ldate = DateTime.Now,
                            Lip = ip,
                            Lmac = mac
                        };
                        _context.AccVoucherEntryDetails.Add(dEntity);
                        lineIdx++;
                    }

                    if (!string.IsNullOrWhiteSpace(model.InvoiceNo))
                    {
                        _context.Acc_VoucherInvoicePaymentReceived.Add(new Acc_VoucherInvoicePaymentReceived
                        {
                            VoucherNo = voucherNo,
                            InvoiceNo = model.InvoiceNo,
                            Amount = totalDebit,
                            IsActive = "1",
                            LUser = userId?.ToString(),
                            LDate = DateTime.Now,
                            LIP = ip,
                            LMAC = mac
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    res.Success = true;
                    res.Message = $"Voucher {voucherNo} created successfully.";
                    res.Data = voucherNo;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
            }

            return res;
        }
        #endregion

        #region Post / Approve Voucher
        public async Task<CommonReturnViewModel> PostAsync(string voucherEntryCodeNo, int? userId = null)
        {
            var res = new CommonReturnViewModel();
            if (string.IsNullOrWhiteSpace(voucherEntryCodeNo))
            {
                res.Success = false;
                res.Message = "Invalid voucher ID.";
                return res;
            }

            try
            {
                var voucher = await _context.AccVoucherEntries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.VoucherEntryCodeNo == voucherEntryCodeNo || x.VoucherNo == voucherEntryCodeNo);

                if (voucher == null)
                {
                    res.Success = false;
                    res.Message = "Voucher not found.";
                    return res;
                }

                const string sql = @"
                    UPDATE Acc_VoucherEntry 
                    SET IsApproved = 'Posted', 
                        ApprovedBy = @user, 
                        ApprovedDate = GETDATE(), 
                        ModifyDate = GETDATE() 
                    WHERE VoucherEntryCodeNo = @vCode OR VoucherNo = @vNo;
                ";

                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                await _dbConnection.ExecuteAsync(sql, new
                {
                    user = userId?.ToString(),
                    vCode = voucher.VoucherEntryCodeNo,
                    vNo = voucher.VoucherNo
                });

                res.Success = true;
                res.Message = $"Voucher {voucher.VoucherNo} approved and posted.";
                res.Data = voucher.VoucherNo;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
            }

            return res;
        }
        #endregion

        #region Delete Voucher (Soft Delete)
        public async Task<CommonReturnViewModel> DeleteAsync(string voucherEntryCodeNo, int? userId = null)
        {
            var res = new CommonReturnViewModel();
            if (string.IsNullOrWhiteSpace(voucherEntryCodeNo))
            {
                res.Success = false;
                res.Message = "Invalid voucher ID.";
                return res;
            }

            try
            {
                var voucher = await _context.AccVoucherEntries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.VoucherEntryCodeNo == voucherEntryCodeNo || x.VoucherNo == voucherEntryCodeNo);

                if (voucher == null)
                {
                    res.Success = false;
                    res.Message = "Voucher not found.";
                    return res;
                }

                string vNo = voucher.VoucherNo;
                string vCode = voucher.VoucherEntryCodeNo;

                const string sql = @"
                    UPDATE Acc_VoucherEntry 
                    SET IsActive = '0', ModifyDate = GETDATE() 
                    WHERE VoucherEntryCodeNo = @vCode OR VoucherNo = @vNo;

                    UPDATE Acc_VoucherEntryDetails 
                    SET IsActive = '0', ModifyDate = GETDATE() 
                    WHERE VoucherEntryCodeNo = @vCode;

                    UPDATE Acc_VoucherInvoicePaymentReceived 
                    SET IsActive = '0', ModifyDate = GETDATE() 
                    WHERE VoucherNo = @vNo;
                ";

                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                await _dbConnection.ExecuteAsync(sql, new { vCode, vNo });

                res.Success = true;
                res.Message = $"Voucher {vNo} deleted successfully.";
                res.Data = vNo;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
            }

            return res;
        }
        #endregion

        #region Dropdowns
        public async Task<VoucherDropdownsVM> GetDropdownsAsync()
        {
            var vm = new VoucherDropdownsVM();
            try
            {
                vm.Companies = await _context.CoreCompanies
                    .OrderBy(x => x.CompanyName)
                    .Select(x => new VoucherDropdownItemVM
                    {
                        Id = 1,
                        Code = x.CompanyCode,
                        Name = x.CompanyName
                    })
                    .ToListAsync();

                vm.Branches = await _context.CoreBranches
                    .OrderBy(x => x.BranchName)
                    .Select(x => new VoucherDropdownItemVM
                    {
                        Id = (int)x.AutoId,
                        Code = x.BranchCode,
                        Name = x.BranchName
                    })
                    .ToListAsync();

                vm.FinancialYears = await _context.AccFinancialYears
                    .OrderByDescending(x => x.FinancialCodeNo)
                    .Select(x => new VoucherDropdownStringItemVM
                    {
                        Code = x.FinancialCodeNo,
                        Name = x.Name
                    })
                    .ToListAsync();

                var dbTypes = await _context.AccVoucherTypes
                    .OrderBy(x => x.VoucherTypeCode)
                    .Select(x => new VoucherTypeDropdownVM
                    {
                        Code = x.VoucherTypeCode,
                        TypeName = x.VoucherTypeName,
                        Name = x.Description,
                        Prefix = x.Prefix
                    })
                    .ToListAsync();

                if (dbTypes.Any())
                {
                    vm.VoucherTypes = dbTypes;
                }
                else
                {
                    vm.VoucherTypes = new List<VoucherTypeDropdownVM>
                    {
                        new VoucherTypeDropdownVM { Code = "1", TypeName = "BRV", Name = "Bank Receipt Voucher", Prefix = "BRV" },
                        new VoucherTypeDropdownVM { Code = "2", TypeName = "BPV", Name = "Bank Payment Voucher", Prefix = "BPV" },
                        new VoucherTypeDropdownVM { Code = "3", TypeName = "CRV", Name = "Cash Receipt Voucher", Prefix = "CRV" },
                        new VoucherTypeDropdownVM { Code = "4", TypeName = "CPV", Name = "Cash Payment Voucher", Prefix = "CPV" },
                        new VoucherTypeDropdownVM { Code = "5", TypeName = "JV", Name = "Journal Voucher", Prefix = "JV" },
                        new VoucherTypeDropdownVM { Code = "6", TypeName = "CV", Name = "Contra Voucher", Prefix = "CV" }
                    };
                }

                var cashCodes = await GetCashAccountCodesAsync();
                var bankCodes = await GetBankAccountCodesAsync();

                var groups = await _context.AccGroupLedgers.AsNoTracking().ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);
                var controls = await _context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName);
                var subControls = await _context.AccSubControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubControlLedgerCodeNo, x => x.SubControlLedgerName);
                var subSubs = await _context.AccSubSubsidiaryLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubSubsidiaryLedgerCodeNo, x => x.SubSubsidiaryLedgerName);

                var gls = await _context.AccGeneralLedgers
                    .Where(gl => gl.IsActive == null || gl.IsActive != "0")
                    .OrderBy(gl => gl.GeneralLedgerCodeNo)
                    .AsNoTracking()
                    .ToListAsync();

                vm.AccountHeads = gls.Select(gl =>
                {
                    string grpName = gl.GroupLedgerCodeNo != null && groups.TryGetValue(gl.GroupLedgerCodeNo, out var gn) ? gn : "";
                    string ctlName = gl.ControlLedgerCodeNo != null && controls.TryGetValue(gl.ControlLedgerCodeNo, out var cn) ? cn : "";
                    string sctlName = gl.SubControlLedgerCodeNo != null && subControls.TryGetValue(gl.SubControlLedgerCodeNo, out var scn) ? scn : "";
                    string ssName = gl.SubSubsidiaryLedgerCodeNo != null && subSubs.TryGetValue(gl.SubSubsidiaryLedgerCodeNo, out var ssn) ? ssn : "";

                    return new VoucherAccountHeadDropdownVM
                    {
                        Code = gl.GeneralLedgerCodeNo,
                        Name = gl.GeneralLedgerName,
                        GroupName = $"{grpName} - {ctlName}",
                        HierarchyPath = $"{grpName} › {ctlName} › {sctlName} › {ssName}",
                        DefaultTrType = (gl.GroupLedgerCodeNo == "1" || gl.GroupLedgerCodeNo == "4") ? "Dr" : "Cr",
                        IsCash = cashCodes.Contains(gl.GeneralLedgerCodeNo) ||
                                 (gl.GeneralLedgerName != null && gl.GeneralLedgerName.Contains("Cash")) ||
                                 (gl.SubSubsidiaryLedgerCodeNo == "10200200001"),
                        IsBank = bankCodes.Contains(gl.GeneralLedgerCodeNo) ||
                                 (gl.GeneralLedgerName != null && gl.GeneralLedgerName.Contains("Bank")) ||
                                 (gl.GeneralLedgerName != null && gl.GeneralLedgerName.Contains("DBBL")) ||
                                 (gl.SubSubsidiaryLedgerCodeNo == "10200200002")
                    };
                }).ToList();
            }
            catch { }

            return vm;
        }
        #endregion

        #region Next Voucher No & Code
        public async Task<string> GetNextVoucherNoAsync(string voucherTypeCode, string? financialYear = null)
        {
            try
            {
                string type = string.IsNullOrWhiteSpace(voucherTypeCode) ? "1" : voucherTypeCode.Trim();
                string year = "2026";
                if (!string.IsNullOrWhiteSpace(financialYear) && financialYear.Length >= 4)
                {
                    year = financialYear.Substring(0, 4);
                }
                else
                {
                    year = DateTime.Today.Year.ToString();
                }

                var vType = await _context.AccVoucherTypes
                    .FirstOrDefaultAsync(t => t.VoucherTypeCode == type || t.VoucherTypeName == type || t.Prefix == type);

                string prefix = (!string.IsNullOrWhiteSpace(vType?.Prefix))
                    ? vType.Prefix
                    : (!string.IsNullOrWhiteSpace(vType?.VoucherTypeName) ? vType.VoucherTypeName : type);

                int padding = (vType != null && vType.Padding > 1) ? (int)vType.Padding : 6;
                int startNum = (vType != null && vType.StartingNumber > 0) ? (int)vType.StartingNumber : 1;

                string pattern = $"{prefix}-{year}-%";
                var lastNo = await _context.AccVoucherEntries
                    .Where(x => EF.Functions.Like(x.VoucherNo, pattern))
                    .OrderByDescending(x => x.VoucherNo)
                    .Select(x => x.VoucherNo)
                    .FirstOrDefaultAsync();

                int nextSeq = startNum;
                if (!string.IsNullOrWhiteSpace(lastNo))
                {
                    var parts = lastNo.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int seq))
                    {
                        nextSeq = Math.Max(startNum, seq + 1);
                    }
                }

                return $"{prefix}-{year}-{nextSeq.ToString(new string('0', padding))}";
            }
            catch
            {
                return $"{voucherTypeCode}-{DateTime.Today.Year}-000001";
            }
        }

        private async Task<string> GetNextVoucherCodeNoAsync(string? financialYear = null)
        {
            string year = "2026";
            if (!string.IsNullOrWhiteSpace(financialYear) && financialYear.Length >= 4)
            {
                year = financialYear.Substring(0, 4);
            }
            else
            {
                year = DateTime.Today.Year.ToString();
            }

            string pattern = $"VCH-{year}-%";
            var lastCode = await _context.AccVoucherEntries
                .Where(x => EF.Functions.Like(x.VoucherEntryCodeNo, pattern))
                .OrderByDescending(x => x.VoucherEntryCodeNo)
                .Select(x => x.VoucherEntryCodeNo)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (!string.IsNullOrWhiteSpace(lastCode))
            {
                var parts = lastCode.Split('-');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int seq))
                {
                    nextSeq = seq + 1;
                }
            }

            return $"VCH-{year}-{nextSeq:D6}";
        }
        #endregion

        #region GetUserFullNameAsync
        public async Task<string> GetUserFullNameAsync(string? userId, string? userName)
        {
            try
            {
                const string query = @"
SELECT TOP 1
    COALESCE(
        NULLIF(RTRIM(LTRIM(ISNULL(e.FirstName, '') + ' ' + ISNULL(e.LastName, ''))), ''),
        NULLIF(RTRIM(LTRIM(u.FullName)), '')
    ) AS ResolvedFullName
FROM AspNetUsers u
LEFT JOIN [Employees] e ON e.EmployeeID = u.PersonID
WHERE (@UserId IS NOT NULL AND u.Id = @UserId)
   OR (@UserName IS NOT NULL AND (u.UserName = @UserName OR u.Email = @UserName));
";
                var name = await _dbConnection.QueryFirstOrDefaultAsync<string>(query, new { UserId = userId, UserName = userName });
                if (!string.IsNullOrWhiteSpace(name) && !name.Contains("@"))
                {
                    return name.Trim();
                }
            }
            catch { }

            return "Md. Moklesur Rahman";
        }
        #endregion

        #region Helper Methods
        private async Task<HashSet<string>> GetCashAccountCodesAsync()
        {
            var codes = await _context.AccGeneralLedgers
                .Where(x => (x.IsActive == "1" || x.IsActive == null) &&
                            (x.SubSubsidiaryLedgerCodeNo == "10200200001" ||
                             x.GeneralLedgerName.Contains("Cash in Hand") ||
                             x.GeneralLedgerName.Contains("Cash")))
                .Select(x => x.GeneralLedgerCodeNo)
                .ToListAsync();

            return codes.ToHashSet();
        }

        private async Task<HashSet<string>> GetBankAccountCodesAsync()
        {
            var codes = await _context.AccGeneralLedgers
                .Where(x => (x.IsActive == "1" || x.IsActive == null) &&
                            (x.SubSubsidiaryLedgerCodeNo == "10200200002" ||
                             x.GeneralLedgerName.Contains("Bank Account") ||
                             x.GeneralLedgerName.Contains("Bank") ||
                             x.GeneralLedgerName.Contains("DBBL")))
                .Select(x => x.GeneralLedgerCodeNo)
                .ToListAsync();

            return codes.ToHashSet();
        }

        private static string GetVoucherTypeName(string code)
        {
            return (code?.ToUpper()) switch
            {
                "1" or "BRV" => "Bank Receipt Voucher",
                "2" or "BPV" => "Bank Payment Voucher",
                "3" or "CRV" => "Cash Receipt Voucher",
                "4" or "CPV" => "Cash Payment Voucher",
                "5" or "JV" => "Journal Voucher",
                "6" or "CV" => "Contra Voucher",
                _ => code ?? "Voucher"
            };
        }

        private static string GetFinancialYear(DateTime date)
        {
            int year = date.Month >= 7 ? date.Year : date.Year - 1;
            return $"{year}-{year + 1}";
        }
        #endregion

        #region Export PDF (QuestPDF)
        public async Task<byte[]> GeneratePdfAsync(VoucherEntryFilterVM filter)
        {
            filter ??= new VoucherEntryFilterVM();
            filter.PageNumber = 1;
            filter.PageSize = 100000;

            var result = await GetAllAsync(filter);

            string companyName = "Greenfield International School";
            try
            {
                var comp = await _context.CoreCompanies.FirstOrDefaultAsync();
                if (comp != null && !string.IsNullOrWhiteSpace(comp.CompanyName))
                {
                    companyName = comp.CompanyName;
                }
            }
            catch { }

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var model = new VoucherEntryListPdfModel
            {
                CompanyName = companyName,
                ReportTitle = "Voucher List",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                Items = result?.Items ?? new List<VoucherEntryVM>()
            };

            var document = new VoucherEntryListPdf(model);
            return document.GeneratePdf();
        }
        #endregion
    }
}
