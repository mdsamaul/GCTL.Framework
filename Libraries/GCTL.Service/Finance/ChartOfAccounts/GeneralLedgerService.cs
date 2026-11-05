using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public class GeneralLedgerService : IGeneralLedgerService
    {
        private readonly GCTL_ERP_DBContext context;

        public GeneralLedgerService(GCTL_ERP_DBContext context)
        {
            this.context = context;
        }

        public async Task<PaginationService<AccGeneralLedger, GetAllGeneralLedgerVM>.PaginationResult<GetAllGeneralLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? subSubsidiaryCode = null, string? subControlCode = null, string? controlCode = null, string? groupCode = null, bool showArchived = false)
        {
            try
            {
                // 1. High Performance Stored Procedure Execution with Customer Name & Short Name Resolution
                try
                {
                    var conn = context.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                    {
                        await conn.OpenAsync();
                    }

                    var parameters = new DynamicParameters();
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", pageSize > 0 ? pageSize : 10);
                    parameters.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim());
                    parameters.Add("@SortColumn", string.IsNullOrWhiteSpace(currentSortColumn) ? "code" : currentSortColumn.Trim());
                    parameters.Add("@SortOrder", string.IsNullOrWhiteSpace(currentSortOrder) ? "ASC" : currentSortOrder.Trim());
                    parameters.Add("@SubSubsidiaryCode", string.IsNullOrWhiteSpace(subSubsidiaryCode) ? null : subSubsidiaryCode.Trim());
                    parameters.Add("@SubControlCode", string.IsNullOrWhiteSpace(subControlCode) ? null : subControlCode.Trim());
                    parameters.Add("@ControlCode", string.IsNullOrWhiteSpace(controlCode) ? null : controlCode.Trim());
                    parameters.Add("@GroupCode", string.IsNullOrWhiteSpace(groupCode) ? null : groupCode.Trim());
                    parameters.Add("@ShowArchived", showArchived ? 1 : 0);

                    var rows = (await conn.QueryAsync<dynamic>(
                        "dbo.sp_GetGeneralLedgerData",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    )).ToList();

                    int totalCount = 0;
                    var spItems = new List<GetAllGeneralLedgerVM>();

                    if (rows.Count > 0)
                    {
                        totalCount = (int)(rows[0].TotalCount ?? 0);
                        foreach (var r in rows)
                        {
                            spItems.Add(new GetAllGeneralLedgerVM
                            {
                                AutoId = (decimal)(r.AutoId ?? 0),
                                GeneralLedgerCodeNo = (string)(r.GeneralLedgerCodeNo ?? string.Empty),
                                SubsidiaryLedgerCodeNo = (string)(r.SubsidiaryLedgerCodeNo ?? string.Empty),
                                SubSubsidiaryLedgerName = (string)(r.SubSubsidiaryLedgerName ?? string.Empty),
                                SubControlLedgerCodeNo = (string)(r.SubControlLedgerCodeNo ?? string.Empty),
                                SubControlLedgerName = (string)(r.SubControlLedgerName ?? string.Empty),
                                ControlLedgerCodeNo = (string)(r.ControlLedgerCodeNo ?? string.Empty),
                                ControlLedgerName = (string)(r.ControlLedgerName ?? string.Empty),
                                GroupLedgerCodeNo = (string)(r.GroupLedgerCodeNo ?? string.Empty),
                                GroupLedgerName = (string)(r.GroupLedgerName ?? string.Empty),
                                GeneralLedgerName = (string)(r.GeneralLedgerName ?? string.Empty),
                                ShortName = (string?)r.ShortName,
                                IsActive = (r.IsActive is int iActive ? iActive == 1 : (r.IsActive is bool bActive ? bActive : true))
                            });
                        }
                    }

                    var spTotalPages = (int)Math.Ceiling((double)totalCount / (pageSize > 0 ? pageSize : 10));

                    return new PaginationService<AccGeneralLedger, GetAllGeneralLedgerVM>.PaginationResult<GetAllGeneralLedgerVM>
                    {
                        Data = spItems,
                        TotalCount = totalCount,
                        PaginationInfo = new PaginationService<AccGeneralLedger, GetAllGeneralLedgerVM>.PaginationInfo
                        {
                            CurrentPage = pageNumber,
                            TotalPages = spTotalPages,
                            TotalItems = totalCount,
                            StartItem = totalCount == 0 ? 0 : (pageNumber - 1) * pageSize + 1,
                            EndItem = Math.Min(pageNumber * pageSize, totalCount)
                        }
                    };
                }
                catch (Exception spEx)
                {
                    Console.WriteLine($"[sp_GetGeneralLedgerData fallback to EF Core]: {spEx.Message}");
                }

                // 2. EF Core Fallback
                var query = context.AccGeneralLedgers.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(subSubsidiaryCode))
                {
                    query = query.Where(x => x.SubsidiaryLedgerCodeNo == subSubsidiaryCode);
                }
                else if (!string.IsNullOrWhiteSpace(subControlCode))
                {
                    query = query.Where(x => x.SubControlLedgerCodeNo == subControlCode);
                }
                else if (!string.IsNullOrWhiteSpace(controlCode))
                {
                    query = query.Where(x => x.ControlLedgerCodeNo == controlCode);
                }
                else if (!string.IsNullOrWhiteSpace(groupCode))
                {
                    query = query.Where(x => x.GroupLedgerCodeNo == groupCode);
                }

                if (!showArchived)
                {
                    query = query.Where(x => x.IsActive == "1" || x.IsActive == null);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = Regex.Replace(searchTerm, @"\s+", " ").Trim().ToLower();
                    query = query.Where(x =>
                        EF.Functions.Like(x.GeneralLedgerCodeNo, $"%{term}%") ||
                        EF.Functions.Like(x.GeneralLedgerName, $"%{term}%") ||
                        EF.Functions.Like(x.ShortName ?? "", $"%{term}%"));
                }

                Expression<Func<AccGeneralLedger, object>> orderByExpression = currentSortColumn?.ToLower() switch
                {
                    "generalledgercode" or "code" => x => x.GeneralLedgerCodeNo,
                    "generalledgername" or "name" => x => x.GeneralLedgerName,
                    "shortname" => x => x.ShortName ?? "",
                    "status" or "isactive" => x => x.IsActive ?? "1",
                    _ => x => x.GeneralLedgerCodeNo,
                };

                query = currentSortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(orderByExpression)
                    : query.OrderBy(orderByExpression);

                var totalItems = await query.CountAsync();

                var pagedEntities = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var sslCodes = pagedEntities.Select(x => x.SubsidiaryLedgerCodeNo).Distinct().ToList();
                var sslNames = await context.AccSubsidiaryLedgers
                    .Where(x => sslCodes.Contains(x.SubsidiaryLedgerCodeNo))
                    .ToDictionaryAsync(x => x.SubsidiaryLedgerCodeNo, x => x.SubsidiaryLedgerName);

                var sclCodes = pagedEntities.Select(x => x.SubControlLedgerCodeNo).Distinct().ToList();
                var sclNames = await context.AccSubControlLedgers
                    .Where(x => sclCodes.Contains(x.SubControlLedgerCodeNo))
                    .ToDictionaryAsync(x => x.SubControlLedgerCodeNo, x => x.SubControlLedgerName);

                var ctrlCodes = pagedEntities.Select(x => x.ControlLedgerCodeNo).Distinct().ToList();
                var ctrlNames = await context.AccControlLedgers
                    .Where(x => ctrlCodes.Contains(x.ControlLedgerCodeNo))
                    .ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName);

                var grpCodes = pagedEntities.Select(x => x.GroupLedgerCodeNo).Distinct().ToList();
                var grpNames = await context.AccGroupLedgers
                    .Where(x => grpCodes.Contains(x.GroupLedgerCodeNo))
                    .ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);

                // Customer resolution for raw CUS IDs if any
                var cusRawIds = pagedEntities
                    .Where(x => !string.IsNullOrEmpty(x.GeneralLedgerName) && x.GeneralLedgerName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase) && !x.GeneralLedgerName.Contains("("))
                    .Select(x => x.GeneralLedgerName.Trim())
                    .Distinct()
                    .ToList();

                Dictionary<string, (string Name, string? Short)> customerMap = new(StringComparer.OrdinalIgnoreCase);
                if (cusRawIds.Count > 0)
                {
                    var custs = await context.SalesCustomers.AsNoTracking()
                        .Where(c => cusRawIds.Contains(c.CustomerId))
                        .Select(c => new { c.CustomerId, c.CustomerName, c.ShortName })
                        .ToListAsync();
                    foreach (var c in custs)
                    {
                        if (!string.IsNullOrEmpty(c.CustomerId) && !customerMap.ContainsKey(c.CustomerId))
                        {
                            customerMap[c.CustomerId] = (c.CustomerName ?? c.CustomerId, c.ShortName);
                        }
                    }
                }

                var items = pagedEntities.Select(x => {
                    string glName = x.GeneralLedgerName ?? string.Empty;
                    string? shortName = x.ShortName;

                    if (glName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase) && !glName.Contains("(") && customerMap.TryGetValue(glName, out var cInfo))
                    {
                        glName = $"{cInfo.Name} ({glName})";
                        if (string.IsNullOrWhiteSpace(shortName))
                        {
                            shortName = cInfo.Short;
                        }
                    }

                    return new GetAllGeneralLedgerVM
                    {
                        AutoId = x.AutoId,
                        GeneralLedgerCodeNo = x.GeneralLedgerCodeNo,
                        SubsidiaryLedgerCodeNo = x.SubsidiaryLedgerCodeNo,
                        SubSubsidiaryLedgerName = sslNames.TryGetValue(x.SubsidiaryLedgerCodeNo, out var ssName) ? ssName : string.Empty,
                        SubControlLedgerCodeNo = x.SubControlLedgerCodeNo,
                        SubControlLedgerName = sclNames.TryGetValue(x.SubControlLedgerCodeNo, out var scName) ? scName : string.Empty,
                        ControlLedgerCodeNo = x.ControlLedgerCodeNo,
                        ControlLedgerName = ctrlNames.TryGetValue(x.ControlLedgerCodeNo, out var cName) ? cName : string.Empty,
                        GroupLedgerCodeNo = x.GroupLedgerCodeNo,
                        GroupLedgerName = grpNames.TryGetValue(x.GroupLedgerCodeNo, out var gName) ? gName : string.Empty,
                        GeneralLedgerName = glName,
                        ShortName = shortName,
                        IsActive = x.IsActive == "1" || x.IsActive == null
                    };
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalItems / (pageSize > 0 ? pageSize : 10));

                return new PaginationService<AccGeneralLedger, GetAllGeneralLedgerVM>.PaginationResult<GetAllGeneralLedgerVM>
                {
                    Data = items,
                    TotalCount = totalItems,
                    PaginationInfo = new PaginationService<AccGeneralLedger, GetAllGeneralLedgerVM>.PaginationInfo
                    {
                        CurrentPage = pageNumber,
                        TotalPages = totalPages,
                        TotalItems = totalItems,
                        StartItem = totalItems == 0 ? 0 : (pageNumber - 1) * pageSize + 1,
                        EndItem = Math.Min(pageNumber * pageSize, totalItems)
                    }
                };
            }
            catch
            {
                throw;
            }
        }

        public async Task<CommonReturnViewModel> GetByIdAsync(string code)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await context.AccGeneralLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GeneralLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "General Ledger not found.";
                    return res;
                }

                string glName = entity.GeneralLedgerName ?? string.Empty;
                string? shortName = entity.ShortName;

                if (entity.SubsidiaryLedgerCodeNo == "10200100001" || (glName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase) && !glName.Contains("(")))
                {
                    var cust = await context.SalesCustomers.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CustomerId == glName);
                    if (cust != null)
                    {
                        glName = $"{cust.CustomerName} ({cust.CustomerId})";
                        if (string.IsNullOrWhiteSpace(shortName))
                        {
                            shortName = cust.ShortName;
                        }
                    }
                }
                else if (entity.SubsidiaryLedgerCodeNo == "10200400003")
                {
                    var bank = await (from ba in context.CoreBankAccountInformations.AsNoTracking()
                                      join bi in context.SalesDefBankInfos.AsNoTracking() on ba.BankId equals bi.BankId into biJoin
                                      from bi in biJoin.DefaultIfEmpty()
                                      where ba.AccountNo == glName
                                      select new { ba.AccountName, bi.ShortName }).FirstOrDefaultAsync();
                    if (bank != null)
                    {
                        glName = $"{bank.AccountName} ({glName})";
                        if (string.IsNullOrWhiteSpace(shortName))
                        {
                            shortName = bank.ShortName;
                        }
                    }
                }

                res.Success = true;
                res.Data = new GeneralLedgerAddVM
                {
                    GeneralLedgerCodeNo = entity.GeneralLedgerCodeNo,
                    SubsidiaryLedgerCodeNo = entity.SubsidiaryLedgerCodeNo,
                    SubControlLedgerCodeNo = entity.SubControlLedgerCodeNo,
                    ControlLedgerCodeNo = entity.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = entity.GroupLedgerCodeNo,
                    GeneralLedgerName = glName,
                    ShortName = shortName,
                    IsActive = entity.IsActive == "1" || entity.IsActive == null
                };
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }
            return res;
        }

        public async Task<string> GetNextCodeAsync(string subSubsidiaryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subSubsidiaryCode))
                    return "00001";

                subSubsidiaryCode = subSubsidiaryCode.Trim();

                var codes = await context.AccGeneralLedgers
                    .Where(x => x.SubsidiaryLedgerCodeNo == subSubsidiaryCode)
                    .Select(x => x.GeneralLedgerCodeNo)
                    .ToListAsync();

                int maxSuffix = 0;
                foreach (var c in codes)
                {
                    if (c != null && c.StartsWith(subSubsidiaryCode) && c.Length > subSubsidiaryCode.Length)
                    {
                        var suffix = c.Substring(subSubsidiaryCode.Length);
                        if (int.TryParse(suffix, out int n) && n > maxSuffix)
                            maxSuffix = n;
                    }
                }

                int next = maxSuffix + 1;
                return $"{subSubsidiaryCode}{next:D5}";
            }
            catch
            {
                return $"{subSubsidiaryCode}00001";
            }
        }

        public async Task<CommonReturnViewModel> AddAsync(GeneralLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(model.SubsidiaryLedgerCodeNo))
                {
                    res.Success = false;
                    res.Message = "Sub-Subsidiary Ledger is required.";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(model.GeneralLedgerName))
                {
                    res.Success = false;
                    res.Message = "General Ledger Name is required.";
                    return res;
                }

                // Customer (10200100001) - GeneralLedgerName stores CustomerID
                if (model.SubsidiaryLedgerCodeNo == "10200100001")
                {
                    var match = Regex.Match(model.GeneralLedgerName, @"\((CUS\d+)\)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        model.GeneralLedgerName = match.Groups[1].Value.ToUpper();
                    }
                    else if (model.GeneralLedgerName.Trim().StartsWith("CUS", StringComparison.OrdinalIgnoreCase))
                    {
                        model.GeneralLedgerName = model.GeneralLedgerName.Trim().ToUpper();
                    }

                    var cust = await context.SalesCustomers.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CustomerId == model.GeneralLedgerName.Trim());
                    if (cust != null && string.IsNullOrWhiteSpace(model.ShortName))
                    {
                        model.ShortName = cust.ShortName ?? string.Empty;
                    }
                }
                // Bank Account (10200400003) - GeneralLedgerName stores AccountNo
                else if (model.SubsidiaryLedgerCodeNo == "10200400003")
                {
                    var bankMatch = Regex.Match(model.GeneralLedgerName, @"\(([0-9\-]+)\)");
                    if (bankMatch.Success)
                    {
                        model.GeneralLedgerName = bankMatch.Groups[1].Value;
                    }
                    else
                    {
                        model.GeneralLedgerName = model.GeneralLedgerName.Trim();
                    }

                    if (string.IsNullOrWhiteSpace(model.ShortName))
                    {
                        var bank = await (from ba in context.CoreBankAccountInformations.AsNoTracking()
                                          join bi in context.SalesDefBankInfos.AsNoTracking() on ba.BankId equals bi.BankId into biJoin
                                          from bi in biJoin.DefaultIfEmpty()
                                          where ba.AccountNo == model.GeneralLedgerName.Trim()
                                          select new { bi.ShortName }).FirstOrDefaultAsync();
                        if (bank != null && !string.IsNullOrWhiteSpace(bank.ShortName))
                        {
                            model.ShortName = bank.ShortName;
                        }
                    }
                }

                var ssl = await context.AccSubsidiaryLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SubsidiaryLedgerCodeNo == model.SubsidiaryLedgerCodeNo);

                if (ssl == null)
                {
                    res.Success = false;
                    res.Message = "Selected Subsidiary Ledger does not exist.";
                    return res;
                }

                var nameExists = await context.AccGeneralLedgers
                    .AnyAsync(x => x.SubsidiaryLedgerCodeNo == model.SubsidiaryLedgerCodeNo &&
                                   x.GeneralLedgerName.ToLower() == model.GeneralLedgerName.Trim().ToLower());
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "A General Ledger with this name already exists in the selected Sub-Subsidiary Ledger.";
                    return res;
                }

                var code = string.IsNullOrWhiteSpace(model.GeneralLedgerCodeNo)
                    ? await GetNextCodeAsync(model.SubsidiaryLedgerCodeNo)
                    : model.GeneralLedgerCodeNo.Trim();

                var codeExists = await context.AccGeneralLedgers.AnyAsync(x => x.GeneralLedgerCodeNo == code);
                if (codeExists)
                {
                    code = await GetNextCodeAsync(model.SubsidiaryLedgerCodeNo);
                }

                var entity = new AccGeneralLedger
                {
                    GeneralLedgerCodeNo = code,
                    SubsidiaryLedgerCodeNo = model.SubsidiaryLedgerCodeNo.Trim(),
                    SubControlLedgerCodeNo = ssl.SubControlLedgerCodeNo,
                    ControlLedgerCodeNo = ssl.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = ssl.GroupLedgerCodeNo,
                    GeneralLedgerName = model.GeneralLedgerName.Trim(),
                    ShortName = model.ShortName?.Trim() ?? string.Empty,
                    IsActive = model.IsActive ? "1" : "0",
                    Ldate = DateTime.Now
                };

                await context.AccGeneralLedgers.AddAsync(entity);
                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "General Ledger added successfully.";
                res.Data = entity;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to add General Ledger: " + ex.Message;
            }
            return res;
        }

        public async Task<CommonReturnViewModel> UpdateAsync(GeneralLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await context.AccGeneralLedgers
                    .FirstOrDefaultAsync(x => x.GeneralLedgerCodeNo == model.GeneralLedgerCodeNo);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "General Ledger not found.";
                    return res;
                }

                if (entity.SubsidiaryLedgerCodeNo == "10200100001" || model.SubsidiaryLedgerCodeNo == "10200100001")
                {
                    var match = Regex.Match(model.GeneralLedgerName, @"\((CUS\d+)\)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        model.GeneralLedgerName = match.Groups[1].Value.ToUpper();
                    }
                    else if (model.GeneralLedgerName.Trim().StartsWith("CUS", StringComparison.OrdinalIgnoreCase))
                    {
                        model.GeneralLedgerName = model.GeneralLedgerName.Trim().ToUpper();
                    }
                }
                else if (entity.SubsidiaryLedgerCodeNo == "10200400003" || model.SubsidiaryLedgerCodeNo == "10200400003")
                {
                    var bankMatch = Regex.Match(model.GeneralLedgerName, @"\(([0-9\-]+)\)");
                    if (bankMatch.Success)
                    {
                        model.GeneralLedgerName = bankMatch.Groups[1].Value;
                    }
                    else
                    {
                        model.GeneralLedgerName = model.GeneralLedgerName.Trim();
                    }
                }

                var nameExists = await context.AccGeneralLedgers
                    .AnyAsync(x => x.SubsidiaryLedgerCodeNo == entity.SubsidiaryLedgerCodeNo &&
                                   x.GeneralLedgerName.ToLower() == model.GeneralLedgerName.Trim().ToLower() &&
                                   x.GeneralLedgerCodeNo != model.GeneralLedgerCodeNo);
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "Another General Ledger with this name already exists in this Sub-Subsidiary Ledger.";
                    return res;
                }

                entity.GeneralLedgerName = model.GeneralLedgerName.Trim();
                entity.ShortName = model.ShortName?.Trim() ?? string.Empty;
                entity.IsActive = model.IsActive ? "1" : "0";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "General Ledger updated successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to update General Ledger: " + ex.Message;
            }
            return res;
        }

        public async Task<CommonReturnViewModel> SoftDeleteAsync(DeleteLedgerRequestVM requestVM)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (requestVM?.Codes == null || !requestVM.Codes.Any())
                {
                    res.Success = false;
                    res.Message = "No items selected to delete.";
                    return res;
                }

                var entities = await context.AccGeneralLedgers
                    .Where(x => requestVM.Codes.Contains(x.GeneralLedgerCodeNo))
                    .ToListAsync();

                if (!entities.Any())
                {
                    res.Success = false;
                    res.Message = "Selected items not found.";
                    return res;
                }

                var hasVouchers = await context.AccVoucherEntryDetails
                    .AnyAsync(x => requestVM.Codes.Contains(x.AccCode));

                if (hasVouchers)
                {
                    res.Success = false;
                    res.Message = "Cannot delete: One or more selected General Ledgers have recorded voucher transactions.";
                    return res;
                }

                foreach (var entity in entities)
                {
                    entity.IsActive = "0";
                    entity.ModifyDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = $"{entities.Count} General Ledger(s) archived successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to delete: " + ex.Message;
            }
            return res;
        }

        public async Task<CommonReturnViewModel> ToggleArchiveAsync(string code)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await context.AccGeneralLedgers
                    .FirstOrDefaultAsync(x => x.GeneralLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "General Ledger not found.";
                    return res;
                }

                bool currentlyActive = entity.IsActive == "1" || entity.IsActive == null;
                entity.IsActive = currentlyActive ? "0" : "1";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = currentlyActive ? "General Ledger archived." : "General Ledger restored.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }
            return res;
        }

        public async Task<List<LedgerDropdownItemVM>> GetDropdownAsync(string? subSubsidiaryCode = null)
        {
            var q = context.AccGeneralLedgers
                .AsNoTracking()
                .Where(x => x.IsActive == "1" || x.IsActive == null);

            if (!string.IsNullOrWhiteSpace(subSubsidiaryCode))
            {
                q = q.Where(x => x.SubsidiaryLedgerCodeNo == subSubsidiaryCode);
            }

            var gls = await q
                .OrderBy(x => x.GeneralLedgerCodeNo)
                .ToListAsync();

            var custDict = await context.SalesCustomers.AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.CustomerId))
                .ToDictionaryAsync(c => c.CustomerId.Trim(), c => c.CustomerName != null ? c.CustomerName.Trim() : c.CustomerId.Trim(), StringComparer.OrdinalIgnoreCase);

            var expDict = await context.CfDefExpenseHeads.AsNoTracking()
                .Where(e => !string.IsNullOrEmpty(e.ExpenseHeadId))
                .ToDictionaryAsync(e => e.ExpenseHeadId.Trim(), e => e.ExpenseHead != null ? e.ExpenseHead.Trim() : e.ExpenseHeadId.Trim(), StringComparer.OrdinalIgnoreCase);

            return gls.Select(x => {
                string name = x.GeneralLedgerName?.Trim() ?? "";
                if (!name.Contains("(") && custDict.TryGetValue(name, out var cName))
                {
                    name = $"{cName} ({name})";
                }
                else if (!name.Contains("(") && expDict.TryGetValue(name, out var eName))
                {
                    name = $"{eName} ({name})";
                }

                return new LedgerDropdownItemVM
                {
                    Code = x.GeneralLedgerCodeNo,
                    Name = name,
                    ParentCode = x.SubsidiaryLedgerCodeNo
                };
            }).ToList();
        }
    }
}
