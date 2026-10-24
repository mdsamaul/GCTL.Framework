using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public class SubSubsidiaryLedgerService : ISubSubsidiaryLedgerService
    {
        private readonly GCTL_ERP_DBContext context;

        public SubSubsidiaryLedgerService(GCTL_ERP_DBContext context)
        {
            this.context = context;
        }

        public async Task<PaginationService<AccSubsidiaryLedger, GetAllSubSubsidiaryLedgerVM>.PaginationResult<GetAllSubSubsidiaryLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? subControlCode = null, string? controlCode = null, string? groupCode = null, bool showArchived = false)
        {
            try
            {
                var query = context.AccSubsidiaryLedgers.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(subControlCode))
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
                        EF.Functions.Like(x.SubsidiaryLedgerCodeNo, $"%{term}%") ||
                        (x.SusidiaryLedgerCodeNo != null && EF.Functions.Like(x.SusidiaryLedgerCodeNo, $"%{term}%")) ||
                        EF.Functions.Like(x.SubsidiaryLedgerName, $"%{term}%") ||
                        EF.Functions.Like(x.ShortName ?? "", $"%{term}%"));
                }

                Expression<Func<AccSubsidiaryLedger, object>> orderByExpression = currentSortColumn?.ToLower() switch
                {
                    "subsidiaryledgercode" or "subsubsidiaryledgercode" or "subsidiarycode" or "code" or "generalledgercode" => x => x.SubsidiaryLedgerCodeNo ?? x.SusidiaryLedgerCodeNo,
                    "subsidiaryledgername" or "subsubsidiaryledgername" or "subsidiaryname" or "name" or "generalledgername" => x => x.SubsidiaryLedgerName,
                    "shortname" => x => x.ShortName ?? "",
                    "status" or "isactive" => x => x.IsActive ?? "1",
                    _ => x => x.SubsidiaryLedgerCodeNo ?? x.SusidiaryLedgerCodeNo,
                };

                query = currentSortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(orderByExpression)
                    : query.OrderBy(orderByExpression);

                var totalItems = await query.CountAsync();

                var pagedEntities = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var sslCodes = pagedEntities
                    .Select(x => !string.IsNullOrWhiteSpace(x.SubsidiaryLedgerCodeNo) ? x.SubsidiaryLedgerCodeNo : (x.SusidiaryLedgerCodeNo ?? string.Empty))
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                var childCounts = await context.AccGeneralLedgers
                    .Where(x => sslCodes.Contains(x.SubsidiaryLedgerCodeNo))
                    .GroupBy(x => x.SubsidiaryLedgerCodeNo)
                    .Select(g => new { Code = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Code, x => x.Count);

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

                var items = pagedEntities.Select(x =>
                {
                    var code = !string.IsNullOrWhiteSpace(x.SubsidiaryLedgerCodeNo) ? x.SubsidiaryLedgerCodeNo : (x.SusidiaryLedgerCodeNo ?? string.Empty);
                    return new GetAllSubSubsidiaryLedgerVM
                    {
                        AutoId = x.AutoId,
                        SubsidiaryLedgerCodeNo = code,
                        SubControlLedgerCodeNo = x.SubControlLedgerCodeNo ?? string.Empty,
                        SubControlLedgerName = sclNames.TryGetValue(x.SubControlLedgerCodeNo ?? "", out var scName) ? scName : string.Empty,
                        ControlLedgerCodeNo = x.ControlLedgerCodeNo ?? string.Empty,
                        ControlLedgerName = ctrlNames.TryGetValue(x.ControlLedgerCodeNo ?? "", out var cName) ? cName : string.Empty,
                        GroupLedgerCodeNo = x.GroupLedgerCodeNo ?? string.Empty,
                        GroupLedgerName = grpNames.TryGetValue(x.GroupLedgerCodeNo ?? "", out var gName) ? gName : string.Empty,
                        SubSubsidiaryLedgerName = x.SubsidiaryLedgerName ?? string.Empty,
                        ShortName = x.ShortName,
                        IsActive = x.IsActive == "1" || x.IsActive == null,
                        ChildCount = childCounts.TryGetValue(code, out var cnt) ? cnt : 0
                    };
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalItems / (pageSize > 0 ? pageSize : 10));

                return new PaginationService<AccSubsidiaryLedger, GetAllSubSubsidiaryLedgerVM>.PaginationResult<GetAllSubSubsidiaryLedgerVM>
                {
                    Data = items,
                    TotalCount = totalItems,
                    PaginationInfo = new PaginationService<AccSubsidiaryLedger, GetAllSubSubsidiaryLedgerVM>.PaginationInfo
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
                var entity = await context.AccSubsidiaryLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SubsidiaryLedgerCodeNo == code || x.SusidiaryLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Sub-Sidiary Ledger not found.";
                    return res;
                }

                var codeNo = !string.IsNullOrWhiteSpace(entity.SubsidiaryLedgerCodeNo) ? entity.SubsidiaryLedgerCodeNo : (entity.SusidiaryLedgerCodeNo ?? string.Empty);

                res.Success = true;
                res.Data = new SubSubsidiaryLedgerAddVM
                {
                    SubsidiaryLedgerCodeNo = codeNo,
                    SubControlLedgerCodeNo = entity.SubControlLedgerCodeNo,
                    ControlLedgerCodeNo = entity.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = entity.GroupLedgerCodeNo,
                    SubSubsidiaryLedgerName = entity.SubsidiaryLedgerName,
                    ShortName = entity.ShortName,
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

        public async Task<string> GetNextCodeAsync(string subControlCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subControlCode))
                    return "00001";

                subControlCode = subControlCode.Trim();

                var rawCodes = await context.AccSubsidiaryLedgers
                    .Where(x => x.SubControlLedgerCodeNo == subControlCode)
                    .Select(x => !string.IsNullOrWhiteSpace(x.SubsidiaryLedgerCodeNo) ? x.SubsidiaryLedgerCodeNo : x.SusidiaryLedgerCodeNo)
                    .ToListAsync();

                int maxSuffix = 0;
                foreach (var c in rawCodes)
                {
                    if (c != null && c.StartsWith(subControlCode) && c.Length > subControlCode.Length)
                    {
                        var suffix = c.Substring(subControlCode.Length);
                        if (int.TryParse(suffix, out int n) && n > maxSuffix)
                            maxSuffix = n;
                    }
                }

                int next = maxSuffix + 1;
                return $"{subControlCode}{next:D5}";
            }
            catch
            {
                return $"{subControlCode}00001";
            }
        }

        public async Task<CommonReturnViewModel> AddAsync(SubSubsidiaryLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(model.SubControlLedgerCodeNo))
                {
                    res.Success = false;
                    res.Message = "Sub-Control Ledger is required.";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(model.SubSubsidiaryLedgerName))
                {
                    res.Success = false;
                    res.Message = "Sub-Sidiary Ledger Name is required.";
                    return res;
                }

                var scl = await context.AccSubControlLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SubControlLedgerCodeNo == model.SubControlLedgerCodeNo);

                if (scl == null)
                {
                    res.Success = false;
                    res.Message = "Selected Sub-Control Ledger does not exist.";
                    return res;
                }

                var nameExists = await context.AccSubsidiaryLedgers
                    .AnyAsync(x => x.SubControlLedgerCodeNo == model.SubControlLedgerCodeNo &&
                                   x.SubsidiaryLedgerName.ToLower() == model.SubSubsidiaryLedgerName.Trim().ToLower());
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "A Sub-Sidiary Ledger with this name already exists in the selected Sub-Control Ledger.";
                    return res;
                }

                var code = string.IsNullOrWhiteSpace(model.SubsidiaryLedgerCodeNo)
                    ? await GetNextCodeAsync(model.SubControlLedgerCodeNo)
                    : model.SubsidiaryLedgerCodeNo.Trim();

                var codeExists = await context.AccSubsidiaryLedgers.AnyAsync(x => x.SubsidiaryLedgerCodeNo == code || x.SusidiaryLedgerCodeNo == code);
                if (codeExists)
                {
                    code = await GetNextCodeAsync(model.SubControlLedgerCodeNo);
                }

                var entity = new AccSubsidiaryLedger
                {
                    SubsidiaryLedgerCodeNo = code,
                    SusidiaryLedgerCodeNo = code,
                    SubControlLedgerCodeNo = model.SubControlLedgerCodeNo.Trim(),
                    ControlLedgerCodeNo = scl.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = scl.GroupLedgerCodeNo,
                    SubsidiaryLedgerName = model.SubSubsidiaryLedgerName.Trim(),
                    ShortName = model.ShortName?.Trim() ?? string.Empty,
                    IsActive = model.IsActive ? "1" : "0",
                    Ldate = DateTime.Now
                };

                await context.AccSubsidiaryLedgers.AddAsync(entity);
                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Sub-Sidiary Ledger added successfully.";
                res.Data = entity;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to add Sub-Sidiary Ledger: " + ex.Message;
            }
            return res;
        }

        public async Task<CommonReturnViewModel> UpdateAsync(SubSubsidiaryLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await context.AccSubsidiaryLedgers
                    .FirstOrDefaultAsync(x => x.SubsidiaryLedgerCodeNo == model.SubsidiaryLedgerCodeNo || x.SusidiaryLedgerCodeNo == model.SubsidiaryLedgerCodeNo);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Sub-Sidiary Ledger not found.";
                    return res;
                }

                var nameExists = await context.AccSubsidiaryLedgers
                    .AnyAsync(x => x.SubControlLedgerCodeNo == entity.SubControlLedgerCodeNo &&
                                   x.SubsidiaryLedgerName.ToLower() == model.SubSubsidiaryLedgerName.Trim().ToLower() &&
                                   x.SubsidiaryLedgerCodeNo != model.SubsidiaryLedgerCodeNo &&
                                   x.SusidiaryLedgerCodeNo != model.SubsidiaryLedgerCodeNo);
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "Another Sub-Sidiary Ledger with this name already exists in this Sub-Control Ledger.";
                    return res;
                }

                entity.SubsidiaryLedgerCodeNo = model.SubsidiaryLedgerCodeNo;
                entity.SusidiaryLedgerCodeNo = model.SubsidiaryLedgerCodeNo;
                entity.SubsidiaryLedgerName = model.SubSubsidiaryLedgerName.Trim();
                entity.ShortName = model.ShortName?.Trim() ?? string.Empty;
                entity.IsActive = model.IsActive ? "1" : "0";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Sub-Sidiary Ledger updated successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to update Sub-Sidiary Ledger: " + ex.Message;
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

                var hasChildren = await context.AccGeneralLedgers
                    .AnyAsync(x => requestVM.Codes.Contains(x.SubsidiaryLedgerCodeNo));

                if (hasChildren)
                {
                    res.Success = false;
                    res.Message = "Cannot delete: One or more selected Sub-Sidiary Ledgers have child General Ledgers. Delete child ledgers first.";
                    return res;
                }

                var entities = await context.AccSubsidiaryLedgers
                    .Where(x => requestVM.Codes.Contains(x.SubsidiaryLedgerCodeNo) || (x.SusidiaryLedgerCodeNo != null && requestVM.Codes.Contains(x.SusidiaryLedgerCodeNo)))
                    .ToListAsync();

                if (!entities.Any())
                {
                    res.Success = false;
                    res.Message = "Selected items not found.";
                    return res;
                }

                foreach (var entity in entities)
                {
                    entity.IsActive = "0";
                    entity.ModifyDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = $"{entities.Count} Sub-Sidiary Ledger(s) archived successfully.";
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
                var entity = await context.AccSubsidiaryLedgers
                    .FirstOrDefaultAsync(x => x.SubsidiaryLedgerCodeNo == code || x.SusidiaryLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Sub-Sidiary Ledger not found.";
                    return res;
                }

                bool currentlyActive = entity.IsActive == "1" || entity.IsActive == null;
                entity.IsActive = currentlyActive ? "0" : "1";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = currentlyActive ? "Sub-Sidiary Ledger archived." : "Sub-Sidiary Ledger restored.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }
            return res;
        }

        public async Task<List<LedgerDropdownItemVM>> GetDropdownAsync(string? subControlCode = null)
        {
            var q = context.AccSubsidiaryLedgers
                .AsNoTracking()
                .Where(x => x.IsActive == "1" || x.IsActive == null);

            if (!string.IsNullOrWhiteSpace(subControlCode))
            {
                q = q.Where(x => x.SubControlLedgerCodeNo == subControlCode);
            }

            return await q
                .OrderBy(x => x.SubsidiaryLedgerCodeNo ?? x.SusidiaryLedgerCodeNo)
                .Select(x => new LedgerDropdownItemVM
                {
                    Code = !string.IsNullOrWhiteSpace(x.SubsidiaryLedgerCodeNo) ? x.SubsidiaryLedgerCodeNo : (x.SusidiaryLedgerCodeNo ?? string.Empty),
                    Name = x.SubsidiaryLedgerName,
                    ParentCode = x.SubControlLedgerCodeNo
                })
                .ToListAsync();
        }
    }
}
