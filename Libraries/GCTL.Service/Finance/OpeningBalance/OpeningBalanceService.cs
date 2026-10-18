using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.OpeningBalancesVM;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.OpeningBalance
{
    public class OpeningBalanceService : IOpeningBalanceService
    {
        private readonly GCTL_ERP_DBContext _context;
        private readonly IDbConnection _dbConnection;

        public OpeningBalanceService(
            GCTL_ERP_DBContext context,
            IDbConnection dbConnection)
        {
            _context = context;
            _dbConnection = dbConnection;
        }

        #region Get All (Paged, Filtered, Sorted, Summary)
        public async Task<OpeningBalanceListResultVM> GetAllAsync(OpeningBalanceFilterVM filter)
        {
            var result = new OpeningBalanceListResultVM
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            try
            {
                var query = _context.AccCompanyOpeningBalances
                    .Where(x => x.DeletedAt == null)
                    .AsQueryable();

                DateTime? filterFrom = filter.FromDate;
                if (!filterFrom.HasValue && !string.IsNullOrWhiteSpace(filter.FromDateStr))
                {
                    if (DateTime.TryParseExact(filter.FromDateStr.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                        System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                    {
                        filterFrom = d;
                    }
                    else if (DateTime.TryParse(filter.FromDateStr.Trim(), out var d2))
                    {
                        filterFrom = d2;
                    }
                }

                DateTime? filterTo = filter.ToDate;
                if (!filterTo.HasValue && !string.IsNullOrWhiteSpace(filter.ToDateStr))
                {
                    if (DateTime.TryParseExact(filter.ToDateStr.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                        System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                    {
                        filterTo = d;
                    }
                    else if (DateTime.TryParse(filter.ToDateStr.Trim(), out var d2))
                    {
                        filterTo = d2;
                    }
                }

                if (!string.IsNullOrWhiteSpace(filter.FinancialCodeNo))
                {
                    var targetFy = await _context.AccFinancialYears
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f => f.FinancialCodeNo == filter.FinancialCodeNo);

                    if (targetFy != null && targetFy.StartDate.HasValue && targetFy.EndDate.HasValue)
                    {
                        var sDate = targetFy.StartDate.Value.Date;
                        var eDate = targetFy.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                        query = query.Where(x => x.FinancialCodeNo == filter.FinancialCodeNo ||
                                                ((x.FinancialCodeNo == null || x.FinancialCodeNo == "") &&
                                                 (x.OpeningDate ?? x.CreatedAt) >= sDate &&
                                                 (x.OpeningDate ?? x.CreatedAt) <= eDate));
                    }
                    else
                    {
                        query = query.Where(x => x.FinancialCodeNo == filter.FinancialCodeNo);
                    }
                }

                if (!string.IsNullOrWhiteSpace(filter.GeneralLedgerCodeNo))
                {
                    query = query.Where(x => x.GeneralLedgerCodeNo == filter.GeneralLedgerCodeNo);
                }

                if (filterFrom.HasValue)
                {
                    var from = filterFrom.Value.Date;
                    query = query.Where(x => (x.OpeningDate ?? x.CreatedAt) >= from);
                }

                if (filterTo.HasValue)
                {
                    var to = filterTo.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => (x.OpeningDate ?? x.CreatedAt) <= to);
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    string term = $"%{filter.SearchTerm.Trim()}%";
                    query = query.Where(x =>
                        EF.Functions.Like(x.ComOpeningBalanceCode, term) ||
                        (x.GeneralLedgerCodeNo != null && EF.Functions.Like(x.GeneralLedgerCodeNo, term)) ||
                        (x.FinancialCodeNo != null && EF.Functions.Like(x.FinancialCodeNo, term)) ||
                        (x.Reference != null && EF.Functions.Like(x.Reference, term)) ||
                        (x.Description != null && EF.Functions.Like(x.Description, term))
                    );
                }

                result.TotalCount = await query.CountAsync();
                result.Summary.TotalDebit = await query
                    .Where(x => x.TrType == "Dr")
                    .SumAsync(x => x.OpeningBalance) ?? 0m;
                result.Summary.TotalCredit = await query
                    .Where(x => x.TrType == "Cr")
                    .SumAsync(x => x.OpeningBalance) ?? 0m;

                bool isDesc = string.Equals(filter.SortOrder, "asc", StringComparison.OrdinalIgnoreCase) == false;
                query = (filter.SortColumn?.ToLower()) switch
                {
                    "code" or "comopeningbalancecode" => isDesc ? query.OrderByDescending(x => x.ComOpeningBalanceCode) : query.OrderBy(x => x.ComOpeningBalanceCode),
                    "amount" or "openingbalance" => isDesc ? query.OrderByDescending(x => x.OpeningBalance) : query.OrderBy(x => x.OpeningBalance),
                    "type" or "trtype" => isDesc ? query.OrderByDescending(x => x.TrType) : query.OrderBy(x => x.TrType),
                    "date" or "openingdate" or "openingdateformatted" => isDesc ? query.OrderByDescending(x => (x.OpeningDate ?? x.CreatedAt)) : query.OrderBy(x => (x.OpeningDate ?? x.CreatedAt)),
                    "year" or "financialcodeno" => isDesc ? query.OrderByDescending(x => x.FinancialCodeNo) : query.OrderBy(x => x.FinancialCodeNo),
                    "reference" => isDesc ? query.OrderByDescending(x => x.Reference) : query.OrderBy(x => x.Reference),
                    "description" => isDesc ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
                    _ => isDesc ? query.OrderByDescending(x => x.ComOpeningBalanceCode) : query.OrderBy(x => x.ComOpeningBalanceCode)
                };

                int offset = (result.PageNumber - 1) * result.PageSize;
                var rawList = await query
                    .Skip(offset)
                    .Take(result.PageSize)
                    .AsNoTracking()
                    .ToListAsync();

                var glCodes = rawList.Select(x => x.GeneralLedgerCodeNo).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                var branchCodes = rawList.Select(x => x.BranchCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                var compCodes = rawList.Select(x => x.MainCompanyCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

                var glDict = new Dictionary<string, string>();
                try
                {
                    var subSubList = await _context.AccSubSubsidiaryLedgers
                        .Where(s => glCodes.Contains(s.SubSubsidiaryLedgerCodeNo) || glCodes.Contains(s.SubSusidiaryLedgerCodeNo))
                        .AsNoTracking()
                        .Select(s => new { 
                            Code = s.SubSubsidiaryLedgerCodeNo ?? s.SubSusidiaryLedgerCodeNo, 
                            Name = s.SubSubsidiaryLedgerName 
                        })
                        .ToListAsync();

                    foreach (var s in subSubList)
                    {
                        if (!string.IsNullOrEmpty(s.Code) && !string.IsNullOrEmpty(s.Name))
                            glDict[s.Code] = s.Name;
                    }
                }
                catch { }

                try
                {
                    var glList = await _context.AccGeneralLedgers
                        .Where(g => glCodes.Contains(g.GeneralLedgerCodeNo))
                        .AsNoTracking()
                        .Select(g => new { g.GeneralLedgerCodeNo, g.GeneralLedgerName })
                        .ToListAsync();

                    foreach (var g in glList)
                    {
                        if (!string.IsNullOrEmpty(g.GeneralLedgerCodeNo) && !glDict.ContainsKey(g.GeneralLedgerCodeNo))
                            glDict[g.GeneralLedgerCodeNo] = g.GeneralLedgerName;
                    }
                }
                catch { }

                var allFys = await _context.AccFinancialYears
                    .AsNoTracking()
                    .ToListAsync();

                Dictionary<string, string> branchDict = new();
                try
                {
                    var branchList = await _context.CoreBranches
                        .Where(b => branchCodes.Contains(b.BranchCode))
                        .AsNoTracking()
                        .Select(b => new { b.BranchCode, b.BranchName })
                        .ToListAsync();
                    branchDict = branchList
                        .GroupBy(b => b.BranchCode)
                        .ToDictionary(b => b.Key, b => b.First().BranchName);
                }
                catch { }

                Dictionary<string, string> compDict = new();
                try
                {
                    var compList = await _context.CoreCompanies
                        .Where(c => compCodes.Contains(c.CompanyCode))
                        .AsNoTracking()
                        .Select(c => new { c.CompanyCode, c.CompanyName })
                        .ToListAsync();
                    compDict = compList
                        .GroupBy(c => c.CompanyCode)
                        .ToDictionary(c => c.Key, c => c.First().CompanyName);
                }
                catch { }

                result.Items = rawList.Select(ob =>
                {
                    var resolvedDate = ob.OpeningDate ?? ob.CreatedAt;

                    string fyDisplay = "";
                    if (!string.IsNullOrEmpty(ob.FinancialCodeNo))
                    {
                        var fy = allFys.FirstOrDefault(f => f.FinancialCodeNo == ob.FinancialCodeNo);
                        fyDisplay = fy?.Name ?? ob.FinancialCodeNo;
                    }
                    else if (resolvedDate.HasValue)
                    {
                        var matchingFy = allFys.FirstOrDefault(f => f.StartDate <= resolvedDate && f.EndDate >= resolvedDate);
                        fyDisplay = matchingFy?.Name ?? "";
                    }

                    string glCode = ob.GeneralLedgerCodeNo ?? string.Empty;
                    string glName = glCode;
                    if (!string.IsNullOrEmpty(glCode) && glDict.TryGetValue(glCode, out var gName))
                    {
                        glName = gName;
                    }

                    return new OpeningBalanceVM
                    {
                        AutoId = ob.AutoId,
                        ComOpeningBalanceCode = ob.ComOpeningBalanceCode,
                        Main_CompanyCode = int.TryParse(ob.MainCompanyCode, out int cId) ? cId : (int?)null,
                        CompanyCodeRaw = ob.MainCompanyCode ?? "001",
                        CompanyName = (ob.MainCompanyCode != null && compDict.TryGetValue(ob.MainCompanyCode, out var cName)) ? cName : "Formosa Logistics Limited",
                        BranchCode = int.TryParse(ob.BranchCode, out int bId) ? bId : (int?)null,
                        BranchCodeRaw = ob.BranchCode ?? "01",
                        BranchName = (ob.BranchCode != null && branchDict.TryGetValue(ob.BranchCode, out var bName)) ? bName : "Head Office (Dhaka)",
                        FinancialCodeNo = fyDisplay,
                        GeneralLedgerCodeNo = glCode,
                        GeneralLedgerName = glName,
                        OpeningBalance = ob.OpeningBalance ?? 0m,
                        TrType = ob.TrType ?? string.Empty,
                        OpeningDate = resolvedDate,
                        Reference = !string.IsNullOrWhiteSpace(ob.Reference) ? ob.Reference : "-",
                        Description = !string.IsNullOrWhiteSpace(ob.Description) ? ob.Description : "-"
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpeningBalance GetAllAsync error: {ex.Message}");
            }

            return result;
        }
        #endregion

        #region Get By Id
        public async Task<CommonReturnViewModel> GetByIdAsync(string code)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    res.Success = false;
                    res.Message = "Invalid Opening Balance code.";
                    return res;
                }

                var record = await _context.AccCompanyOpeningBalances
                    .Where(x => x.DeletedAt == null && x.ComOpeningBalanceCode == code)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (record == null)
                {
                    res.Success = false;
                    res.Message = "Opening balance record not found.";
                    return res;
                }

                string? glName = null;
                if (!string.IsNullOrWhiteSpace(record.GeneralLedgerCodeNo))
                {
                    try
                    {
                        var subSub = await _context.AccSubSubsidiaryLedgers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.SubSubsidiaryLedgerCodeNo == record.GeneralLedgerCodeNo || s.SubSusidiaryLedgerCodeNo == record.GeneralLedgerCodeNo);
                        if (subSub != null && !string.IsNullOrWhiteSpace(subSub.SubSubsidiaryLedgerName))
                        {
                            glName = subSub.SubSubsidiaryLedgerName;
                        }
                        else
                        {
                            var gl = await _context.AccGeneralLedgers.FirstOrDefaultAsync(g => g.GeneralLedgerCodeNo == record.GeneralLedgerCodeNo);
                            glName = gl?.GeneralLedgerName;
                        }
                    }
                    catch
                    {
                        var gl = await _context.AccGeneralLedgers.FirstOrDefaultAsync(g => g.GeneralLedgerCodeNo == record.GeneralLedgerCodeNo);
                        glName = gl?.GeneralLedgerName;
                    }
                }

                string? branchName = null;
                if (!string.IsNullOrWhiteSpace(record.BranchCode))
                {
                    try
                    {
                        var br = await _context.CoreBranches
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.BranchCode == record.BranchCode || b.AutoId.ToString() == record.BranchCode);
                        branchName = br?.BranchName;
                    }
                    catch { }
                }

                string? compName = null;
                string? compCode = record.MainCompanyCode;
                if (!string.IsNullOrWhiteSpace(compCode))
                {
                    try
                    {
                        var cp = await _context.CoreCompanies
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CompanyCode == compCode);
                        compName = cp?.CompanyName;
                    }
                    catch { }
                }

                string? fyCode = record.FinancialCodeNo;
                var resolvedDate = record.OpeningDate ?? record.CreatedAt;
                if (string.IsNullOrWhiteSpace(fyCode) && resolvedDate.HasValue)
                {
                    try
                    {
                        var allFys = await _context.AccFinancialYears.AsNoTracking().ToListAsync();
                        var matchingFy = allFys.FirstOrDefault(f => f.StartDate <= resolvedDate && f.EndDate >= resolvedDate);
                        fyCode = matchingFy?.FinancialCodeNo;
                    }
                    catch { }
                }

                var vm = new OpeningBalanceVM
                {
                    AutoId = record.AutoId,
                    ComOpeningBalanceCode = record.ComOpeningBalanceCode,
                    Main_CompanyCode = int.TryParse(record.MainCompanyCode, out int cId) ? cId : (int?)null,
                    CompanyCodeRaw = record.MainCompanyCode ?? "001",
                    CompanyName = compName ?? "Formosa Logistics Limited",
                    BranchCode = int.TryParse(record.BranchCode, out int bId) ? bId : (int?)null,
                    BranchCodeRaw = record.BranchCode ?? "01",
                    BranchName = branchName ?? "Head Office (Dhaka)",
                    FinancialCodeNo = fyCode,
                    GeneralLedgerCodeNo = record.GeneralLedgerCodeNo ?? string.Empty,
                    GeneralLedgerName = glName,
                    OpeningBalance = record.OpeningBalance,
                    TrType = record.TrType ?? string.Empty,
                    OpeningDate = resolvedDate,
                    Reference = record.Reference,
                    Description = record.Description
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
        #endregion

        #region Save (Add or Update)
        public async Task<CommonReturnViewModel> SaveAsync(OpeningBalanceVM model, int? userId = null, string? ip = null, string? mac = null)
        {
            var res = new CommonReturnViewModel();

            try
            {
                if (string.IsNullOrWhiteSpace(model.FinancialCodeNo))
                {
                    res.Success = false;
                    res.Message = "Financial Year is required.";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(model.GeneralLedgerCodeNo))
                {
                    res.Success = false;
                    res.Message = "Account Head is required.";
                    return res;
                }

                if (!model.OpeningBalance.HasValue || model.OpeningBalance.Value < 0)
                {
                    res.Success = false;
                    res.Message = "Please enter a valid Opening Balance amount.";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(model.TrType))
                {
                    res.Success = false;
                    res.Message = "Transaction Type (Dr/Cr) is required.";
                    return res;
                }

                if (!model.OpeningDate.HasValue && !string.IsNullOrWhiteSpace(model.OpeningDateRaw))
                {
                    if (DateTime.TryParseExact(model.OpeningDateRaw.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                        System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                    {
                        model.OpeningDate = d;
                    }
                    else if (DateTime.TryParse(model.OpeningDateRaw.Trim(), out var d2))
                    {
                        model.OpeningDate = d2;
                    }
                }
                model.OpeningDate ??= DateTime.Today;

                using var transaction = await _context.Database.BeginTransactionAsync();

                AccCompanyOpeningBalance? existingEntity = null;
                if (!string.IsNullOrWhiteSpace(model.ComOpeningBalanceCode))
                {
                    existingEntity = await _context.AccCompanyOpeningBalances
                        .FirstOrDefaultAsync(x => x.DeletedAt == null && x.ComOpeningBalanceCode == model.ComOpeningBalanceCode);
                }
                if (existingEntity == null && model.AutoId.HasValue && model.AutoId > 0)
                {
                    existingEntity = await _context.AccCompanyOpeningBalances
                        .FirstOrDefaultAsync(x => x.DeletedAt == null && x.AutoId == model.AutoId.Value);
                }

                if (existingEntity != null)
                {
                    bool duplicate = await _context.AccCompanyOpeningBalances.AnyAsync(x =>
                        x.DeletedAt == null &&
                        x.GeneralLedgerCodeNo == model.GeneralLedgerCodeNo &&
                        x.FinancialCodeNo == model.FinancialCodeNo &&
                        x.ComOpeningBalanceCode != existingEntity.ComOpeningBalanceCode &&
                        x.AutoId != existingEntity.AutoId);

                    if (duplicate)
                    {
                        res.Success = false;
                        res.Message = $"Account {model.GeneralLedgerCodeNo} already has an opening balance for financial year {model.FinancialCodeNo}.";
                        await transaction.RollbackAsync();
                        return res;
                    }

                    string comp = !string.IsNullOrWhiteSpace(model.CompanyCodeRaw) ? model.CompanyCodeRaw : (model.Main_CompanyCode?.ToString() ?? existingEntity.MainCompanyCode ?? "001");
                    string branch = !string.IsNullOrWhiteSpace(model.BranchCodeRaw) ? model.BranchCodeRaw : (model.BranchCode?.ToString() ?? existingEntity.BranchCode ?? "01");
                    if (int.TryParse(branch, out int bNum) && bNum < 10 && branch.Length == 1)
                    {
                        branch = bNum.ToString("D2");
                    }

                    existingEntity.MainCompanyCode = comp;
                    existingEntity.BranchCode = branch;
                    existingEntity.CompanyCode = comp;
                    existingEntity.FinancialCodeNo = model.FinancialCodeNo;
                    existingEntity.GeneralLedgerCodeNo = model.GeneralLedgerCodeNo;
                    existingEntity.SubSusidiaryLedgerCodeNo = model.GeneralLedgerCodeNo;
                    existingEntity.OpeningBalance = model.OpeningBalance;
                    existingEntity.TrType = model.TrType;
                    existingEntity.OpeningDate = model.OpeningDate;
                    existingEntity.Reference = model.Reference;
                    existingEntity.Description = model.Description;
                    existingEntity.UpdatedBy = userId;
                    existingEntity.UpdatedAt = DateTime.UtcNow;
                    existingEntity.Luser = userId?.ToString() ?? "superadmin";
                    existingEntity.Ldate = DateTime.Now;
                    existingEntity.ModifyDate = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(ip)) existingEntity.Lip = ip;
                    if (!string.IsNullOrWhiteSpace(mac)) existingEntity.Lmac = mac;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    res.Success = true;
                    res.Message = "Opening Balance updated successfully.";
                    res.Data = existingEntity.ComOpeningBalanceCode;
                }
                else
                {
                    bool duplicate = await _context.AccCompanyOpeningBalances.AnyAsync(x =>
                        x.DeletedAt == null &&
                        x.GeneralLedgerCodeNo == model.GeneralLedgerCodeNo &&
                        x.FinancialCodeNo == model.FinancialCodeNo);

                    if (duplicate)
                    {
                        res.Success = false;
                        res.Message = $"Account {model.GeneralLedgerCodeNo} already has an opening balance for financial year {model.FinancialCodeNo}. Edit the existing entry instead.";
                        await transaction.RollbackAsync();
                        return res;
                    }

                    if (string.IsNullOrWhiteSpace(model.ComOpeningBalanceCode))
                    {
                        model.ComOpeningBalanceCode = await GetNextCodeAsync(model.FinancialCodeNo);
                    }

                    string comp = !string.IsNullOrWhiteSpace(model.CompanyCodeRaw) ? model.CompanyCodeRaw : (model.Main_CompanyCode?.ToString() ?? "001");
                    string branch = !string.IsNullOrWhiteSpace(model.BranchCodeRaw) ? model.BranchCodeRaw : (model.BranchCode?.ToString() ?? "01");
                    if (int.TryParse(branch, out int bNum2) && bNum2 < 10 && branch.Length == 1)
                    {
                        branch = bNum2.ToString("D2");
                    }

                    var entity = new AccCompanyOpeningBalance
                    {
                        ComOpeningBalanceCode = model.ComOpeningBalanceCode,
                        MainCompanyCode = comp,
                        BranchCode = branch,
                        CompanyCode = comp,
                        UserInfoEmployeeId = userId?.ToString() ?? "1",
                        GeneralLedgerCodeNo = model.GeneralLedgerCodeNo,
                        SubSusidiaryLedgerCodeNo = model.GeneralLedgerCodeNo,
                        OpeningBalance = model.OpeningBalance,
                        TrType = model.TrType,
                        OpeningDate = model.OpeningDate ?? DateTime.Today,
                        FinancialCodeNo = model.FinancialCodeNo,
                        Reference = model.Reference,
                        Description = model.Description,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        Luser = userId?.ToString() ?? "superadmin",
                        Ldate = DateTime.Now,
                        ModifyDate = DateTime.Now,
                        Lip = ip,
                        Lmac = mac
                    };

                    _context.AccCompanyOpeningBalances.Add(entity);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    res.Success = true;
                    res.Message = "Opening Balance created successfully.";
                    res.Data = model.ComOpeningBalanceCode;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }

            return res;
        }
        #endregion

        #region Soft Delete
        public async Task<CommonReturnViewModel> DeleteAsync(string code, int? userId = null)
        {
            var res = new CommonReturnViewModel();

            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    res.Success = false;
                    res.Message = "Invalid Opening Balance code.";
                    return res;
                }

                var entity = await _context.AccCompanyOpeningBalances
                    .FirstOrDefaultAsync(x => x.DeletedAt == null && x.ComOpeningBalanceCode == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Record not found or already deleted.";
                    return res;
                }

                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = userId;

                await _context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Opening Balance deleted successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }

            return res;
        }

        public async Task<CommonReturnViewModel> BulkDeleteAsync(List<string> codes, int? userId = null)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (codes == null || codes.Count == 0)
                {
                    res.Success = false;
                    res.Message = "No items selected to delete.";
                    return res;
                }

                var entities = await _context.AccCompanyOpeningBalances
                    .Where(x => x.DeletedAt == null && codes.Contains(x.ComOpeningBalanceCode))
                    .ToListAsync();

                if (entities.Count == 0)
                {
                    res.Success = false;
                    res.Message = "No matching records found to delete.";
                    return res;
                }

                var now = DateTime.UtcNow;
                foreach (var entity in entities)
                {
                    entity.DeletedAt = now;
                    entity.DeletedBy = userId;
                }

                await _context.SaveChangesAsync();
                res.Success = true;
                res.Message = $"{entities.Count} opening balance record(s) deleted successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }

            return res;
        }
        #endregion

        #region Get Next Code
        public async Task<string> GetNextCodeAsync(string financialYear)
        {
            try
            {
                string year = "2026";
                var fy = await _context.AccFinancialYears
                    .FirstOrDefaultAsync(f => f.FinancialCodeNo == financialYear || f.Name == financialYear);
                if (fy != null && !string.IsNullOrWhiteSpace(fy.Name) && fy.Name.Length >= 4)
                {
                    year = fy.Name.Substring(0, 4);
                }
                else if (!string.IsNullOrWhiteSpace(financialYear) && financialYear.Length >= 4)
                {
                    year = financialYear.Substring(0, 4);
                }

                string pattern = $"OB-{year}-%";
                var allMatchingCodes = await _context.AccCompanyOpeningBalances
                    .Where(x => EF.Functions.Like(x.ComOpeningBalanceCode, pattern))
                    .Select(x => x.ComOpeningBalanceCode)
                    .ToListAsync();

                int maxSeq = 0;
                foreach (var c in allMatchingCodes)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    var parts = c.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int seq))
                    {
                        if (seq > maxSeq) maxSeq = seq;
                    }
                }

                return $"OB-{year}-{(maxSeq + 1):D4}";
            }
            catch
            {
                return "OB-2026-0001";
            }
        }
        #endregion

        #region Get Dropdowns
        public async Task<OpeningBalanceDropdownsVM> GetDropdownsAsync()
        {
            var vm = new OpeningBalanceDropdownsVM();

            // 1. Financial Years directly from Acc_FinancialYear table
            try
            {
                var allFys = await _context.AccFinancialYears
                    .OrderBy(x => x.FinancialCodeNo)
                    .AsNoTracking()
                    .ToListAsync();

                var now = DateTime.Today;
                var currentFy = allFys.FirstOrDefault(x => x.StartDate <= now && x.EndDate >= now)
                                ?? allFys.OrderByDescending(x => x.EndDate).FirstOrDefault();

                vm.FinancialYears = allFys.Select(x => new DropdownStringItemVM
                {
                    Code = x.FinancialCodeNo,
                    Name = x.Name,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    IsCurrent = (currentFy != null && x.FinancialCodeNo == currentFy.FinancialCodeNo)
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Financial Years load error: {ex.Message}");
            }

            // 2. Account Heads directly from Acc_GeneralLedger table (49 rows)
            try
            {
                Dictionary<string, string> groups = new();
                try
                {
                    groups = await _context.AccGroupLedgers.AsNoTracking().ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);
                }
                catch { }

                Dictionary<string, string> controls = new();
                try
                {
                    controls = await _context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName);
                }
                catch { }

                var gls = await _context.AccGeneralLedgers
                    .Where(x => x.IsActive == "1" || x.IsActive == null)
                    .OrderBy(x => x.GeneralLedgerCodeNo)
                    .AsNoTracking()
                    .ToListAsync();

                vm.AccountHeads = gls.Select(gl =>
                {
                    string grpName = gl.GroupLedgerCodeNo != null && groups.TryGetValue(gl.GroupLedgerCodeNo, out var gn) ? gn : "";
                    string ctlName = gl.ControlLedgerCodeNo != null && controls.TryGetValue(gl.ControlLedgerCodeNo, out var cn) ? cn : "";
                    string groupLabel = !string.IsNullOrEmpty(grpName) && !string.IsNullOrEmpty(ctlName)
                        ? $"{grpName} - {ctlName}"
                        : (!string.IsNullOrEmpty(grpName) ? grpName : (!string.IsNullOrEmpty(ctlName) ? ctlName : "General Ledger"));

                    return new AccountHeadDropdownVM
                    {
                        Code = gl.GeneralLedgerCodeNo?.Trim() ?? "",
                        Name = gl.GeneralLedgerName?.Trim() ?? "",
                        GroupName = groupLabel,
                        DefaultTrType = (gl.GroupLedgerCodeNo == "1" || gl.GroupLedgerCodeNo == "4") ? "Dr" : "Cr"
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Account Heads load error: {ex.Message}");
            }

            // 3. Companies & Branches (isolated so missing table in School_DB_GCTL_2 never crashes dropdowns)
            try
            {
                vm.Companies = await _context.CoreCompanies
                    .OrderBy(x => x.CompanyName)
                    .Select(x => new DropdownItemVM
                    {
                        Id = 1,
                        Code = x.CompanyCode,
                        Name = x.CompanyName
                    })
                    .ToListAsync();
            }
            catch
            {
                vm.Companies = new List<DropdownItemVM>
                {
                    new() { Id = 1, Code = "001", Name = "Formosa Logistics Limited" }
                };
            }

            try
            {
                vm.Branches = await _context.CoreBranches
                    .OrderBy(x => x.BranchName)
                    .Select(x => new DropdownItemVM
                    {
                        Id = (int)x.AutoId,
                        Code = x.BranchCode,
                        Name = x.BranchName
                    })
                    .ToListAsync();
            }
            catch
            {
                vm.Branches = new List<DropdownItemVM>
                {
                    new() { Id = 10016, Code = "01", Name = "Head Office (Dhaka)" },
                    new() { Id = 10017, Code = "02", Name = "Chattagram Branch" },
                    new() { Id = 10018, Code = "03", Name = "Dhaka Airport (Uttara Office)" },
                    new() { Id = 10019, Code = "04", Name = "Pangaon Division" }
                };
            }

            return vm;
        }
        #endregion

        #region Import CSV
        public async Task<CommonReturnViewModel> ImportCsvAsync(List<OpeningBalanceVM> items, int? userId = null, string? ip = null, string? mac = null)
        {
            var res = new CommonReturnViewModel();
            int imported = 0;
            int skipped = 0;

            try
            {
                var validCodes = (await _context.AccGeneralLedgers
                    .Where(x => x.IsActive == "1" || x.IsActive == null)
                    .Select(x => x.GeneralLedgerCodeNo)
                    .ToListAsync())
                    .ToHashSet();

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.GeneralLedgerCodeNo) ||
                        !validCodes.Contains(item.GeneralLedgerCodeNo) ||
                        !item.OpeningBalance.HasValue ||
                        string.IsNullOrWhiteSpace(item.TrType))
                    {
                        skipped++;
                        continue;
                    }

                    var saveResult = await SaveAsync(item, userId, ip, mac);
                    if (saveResult.Success)
                    {
                        imported++;
                    }
                    else
                    {
                        skipped++;
                    }
                }

                res.Success = true;
                res.Message = $"Successfully imported {imported} row(s)" + (skipped > 0 ? $", skipped {skipped} invalid/duplicate row(s)." : ".");
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }

            return res;
        }
        #endregion

        #region Export PDF (QuestPDF)
        public async Task<byte[]> GeneratePdfAsync(OpeningBalanceFilterVM filter)
        {
            filter ??= new OpeningBalanceFilterVM();
            filter.PageNumber = 1;
            filter.PageSize = int.MaxValue;

            var result = await GetAllAsync(filter);
            var items = result.Items ?? new List<OpeningBalanceVM>();

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

            string? fyName = null;
            if (!string.IsNullOrWhiteSpace(filter.FinancialCodeNo))
            {
                try
                {
                    var fy = await _context.AccFinancialYears
                        .FirstOrDefaultAsync(x => x.FinancialCodeNo == filter.FinancialCodeNo);
                    if (fy != null)
                    {
                        fyName = fy.Name;
                    }
                }
                catch { }
            }

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var pdfModel = new OpeningBalancePdfModel
            {
                CompanyName = companyName,
                ReportTitle = "Opening Balance Report",
                FinancialYearName = fyName ?? filter.FinancialCodeNo,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                Items = items
            };

            var document = new OpeningBalancePdf(pdfModel);
            return document.GeneratePdf();
        }
        #endregion
    }
}
