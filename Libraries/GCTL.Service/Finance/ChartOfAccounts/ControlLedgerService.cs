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
    public class ControlLedgerService : IControlLedgerService
    {
        private readonly GCTL_ERP_DBContext context;

        public ControlLedgerService(GCTL_ERP_DBContext context)
        {
            this.context = context;
        }

        public async Task<PaginationService<AccControlLedger, GetAllControlLedgerVM>.PaginationResult<GetAllControlLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? groupCode = null, bool showArchived = false)
        {
            try
            {
                var query = context.AccControlLedgers.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(groupCode))
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
                        EF.Functions.Like(x.ControlLedgerCodeNo, $"%{term}%") ||
                        EF.Functions.Like(x.ControlLedgerName, $"%{term}%") ||
                        EF.Functions.Like(x.ShortName ?? "", $"%{term}%"));
                }

                Expression<Func<AccControlLedger, object>> orderByExpression = currentSortColumn?.ToLower() switch
                {
                    "controlledgercode" or "code" => x => x.ControlLedgerCodeNo,
                    "controlledgername" or "name" => x => x.ControlLedgerName,
                    "shortname" => x => x.ShortName ?? "",
                    "status" or "isactive" => x => x.IsActive ?? "1",
                    _ => x => x.ControlLedgerCodeNo,
                };

                query = currentSortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(orderByExpression)
                    : query.OrderBy(orderByExpression);

                var totalItems = await query.CountAsync();

                var pagedEntities = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var ctrlCodes = pagedEntities.Select(x => x.ControlLedgerCodeNo).ToList();
                var childCounts = await context.AccSubControlLedgers
                    .Where(x => ctrlCodes.Contains(x.ControlLedgerCodeNo))
                    .GroupBy(x => x.ControlLedgerCodeNo)
                    .Select(g => new { Code = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Code, x => x.Count);

                var grpCodes = pagedEntities.Select(x => x.GroupLedgerCodeNo).Distinct().ToList();
                var grpNames = await context.AccGroupLedgers
                    .Where(x => grpCodes.Contains(x.GroupLedgerCodeNo))
                    .ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);

                var items = pagedEntities.Select(x => new GetAllControlLedgerVM
                {
                    AutoId = x.AutoId,
                    ControlLedgerCodeNo = x.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = x.GroupLedgerCodeNo,
                    GroupLedgerName = grpNames.TryGetValue(x.GroupLedgerCodeNo, out var gName) ? gName : string.Empty,
                    ControlLedgerName = x.ControlLedgerName,
                    ShortName = x.ShortName,
                    IsActive = x.IsActive == "1" || x.IsActive == null,
                    ChildCount = childCounts.TryGetValue(x.ControlLedgerCodeNo, out int cnt) ? cnt : 0
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalItems / (pageSize > 0 ? pageSize : 10));

                return new PaginationService<AccControlLedger, GetAllControlLedgerVM>.PaginationResult<GetAllControlLedgerVM>
                {
                    Data = items,
                    TotalCount = totalItems,
                    PaginationInfo = new PaginationService<AccControlLedger, GetAllControlLedgerVM>.PaginationInfo
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
                var entity = await context.AccControlLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ControlLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Control Ledger not found.";
                    return res;
                }

                res.Success = true;
                res.Data = new ControlLedgerAddVM
                {
                    ControlLedgerCodeNo = entity.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = entity.GroupLedgerCodeNo,
                    ControlLedgerName = entity.ControlLedgerName,
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

        public async Task<string> GetNextCodeAsync(string groupCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupCode))
                    return "01";

                groupCode = groupCode.Trim();

                var codes = await context.AccControlLedgers
                    .Where(x => x.GroupLedgerCodeNo == groupCode)
                    .Select(x => x.ControlLedgerCodeNo)
                    .ToListAsync();

                int maxSuffix = 0;
                foreach (var c in codes)
                {
                    if (c != null && c.StartsWith(groupCode) && c.Length > groupCode.Length)
                    {
                        var suffix = c.Substring(groupCode.Length);
                        if (int.TryParse(suffix, out int n) && n > maxSuffix)
                            maxSuffix = n;
                    }
                }

                int next = maxSuffix + 1;
                return $"{groupCode}{next:D2}";
            }
            catch
            {
                return $"{groupCode}01";
            }
        }

        public async Task<CommonReturnViewModel> AddAsync(ControlLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(model.GroupLedgerCodeNo))
                {
                    res.Success = false;
                    res.Message = "Group Ledger is required.";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(model.ControlLedgerName))
                {
                    res.Success = false;
                    res.Message = "Control Ledger Name is required.";
                    return res;
                }

                var nameExists = await context.AccControlLedgers
                    .AnyAsync(x => x.GroupLedgerCodeNo == model.GroupLedgerCodeNo &&
                                   x.ControlLedgerName.ToLower() == model.ControlLedgerName.Trim().ToLower());
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "A Control Ledger with this name already exists in the selected Group.";
                    return res;
                }

                var code = string.IsNullOrWhiteSpace(model.ControlLedgerCodeNo)
                    ? await GetNextCodeAsync(model.GroupLedgerCodeNo)
                    : model.ControlLedgerCodeNo.Trim();

                var codeExists = await context.AccControlLedgers.AnyAsync(x => x.ControlLedgerCodeNo == code);
                if (codeExists)
                {
                    code = await GetNextCodeAsync(model.GroupLedgerCodeNo);
                }

                var entity = new AccControlLedger
                {
                    ControlLedgerCodeNo = code,
                    GroupLedgerCodeNo = model.GroupLedgerCodeNo.Trim(),
                    ControlLedgerName = model.ControlLedgerName.Trim(),
                    ShortName = model.ShortName?.Trim() ?? string.Empty,
                    IsActive = model.IsActive ? "1" : "0",
                    Ldate = DateTime.Now
                };

                await context.AccControlLedgers.AddAsync(entity);
                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Control Ledger added successfully.";
                res.Data = entity;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to add Control Ledger: " + ex.Message;
            }
            return res;
        }

        public async Task<CommonReturnViewModel> UpdateAsync(ControlLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await context.AccControlLedgers
                    .FirstOrDefaultAsync(x => x.ControlLedgerCodeNo == model.ControlLedgerCodeNo);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Control Ledger not found.";
                    return res;
                }

                var nameExists = await context.AccControlLedgers
                    .AnyAsync(x => x.GroupLedgerCodeNo == entity.GroupLedgerCodeNo &&
                                   x.ControlLedgerName.ToLower() == model.ControlLedgerName.Trim().ToLower() &&
                                   x.ControlLedgerCodeNo != model.ControlLedgerCodeNo);
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "Another Control Ledger with this name already exists in this Group.";
                    return res;
                }

                entity.ControlLedgerName = model.ControlLedgerName.Trim();
                entity.ShortName = model.ShortName?.Trim() ?? string.Empty;
                entity.IsActive = model.IsActive ? "1" : "0";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Control Ledger updated successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to update Control Ledger: " + ex.Message;
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

                var entities = await context.AccControlLedgers
                    .Where(x => requestVM.Codes.Contains(x.ControlLedgerCodeNo))
                    .ToListAsync();

                if (!entities.Any())
                {
                    res.Success = false;
                    res.Message = "Selected items not found.";
                    return res;
                }

                var hasChildren = await context.AccSubControlLedgers
                    .AnyAsync(x => requestVM.Codes.Contains(x.ControlLedgerCodeNo));

                if (hasChildren)
                {
                    res.Success = false;
                    res.Message = "Cannot delete: One or more selected Control Ledgers have child Sub-Control Ledgers. Delete child ledgers first.";
                    return res;
                }

                foreach (var entity in entities)
                {
                    entity.IsActive = "0";
                    entity.ModifyDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = $"{entities.Count} Control Ledger(s) archived successfully.";
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
                var entity = await context.AccControlLedgers
                    .FirstOrDefaultAsync(x => x.ControlLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Control Ledger not found.";
                    return res;
                }

                bool currentlyActive = entity.IsActive == "1" || entity.IsActive == null;
                entity.IsActive = currentlyActive ? "0" : "1";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = currentlyActive ? "Control Ledger archived." : "Control Ledger restored.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }
            return res;
        }

        public async Task<List<LedgerDropdownItemVM>> GetDropdownAsync(string? groupCode = null)
        {
            var q = context.AccControlLedgers
                .AsNoTracking()
                .Where(x => x.IsActive == "1" || x.IsActive == null);

            if (!string.IsNullOrWhiteSpace(groupCode))
            {
                q = q.Where(x => x.GroupLedgerCodeNo == groupCode);
            }

            return await q
                .OrderBy(x => x.ControlLedgerCodeNo)
                .Select(x => new LedgerDropdownItemVM
                {
                    Code = x.ControlLedgerCodeNo,
                    Name = x.ControlLedgerName,
                    ParentCode = x.GroupLedgerCodeNo
                })
                .ToListAsync();
        }
    }
}
