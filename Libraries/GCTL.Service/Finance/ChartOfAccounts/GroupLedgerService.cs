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
    public class GroupLedgerService : IGroupLedgerService
    {
        private readonly GCTL_ERP_DBContext context;

        public GroupLedgerService(GCTL_ERP_DBContext context)
        {
            this.context = context;
        }

        public async Task<PaginationService<AccGroupLedger, GetAllGroupLedgerVM>.PaginationResult<GetAllGroupLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", bool showArchived = false)
        {
            try
            {
                var query = context.AccGroupLedgers.AsNoTracking().AsQueryable();

                if (!showArchived)
                {
                    query = query.Where(x => x.IsActive == "1" || x.IsActive == null);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = Regex.Replace(searchTerm, @"\s+", " ").Trim().ToLower();
                    query = query.Where(x =>
                        EF.Functions.Like(x.GroupLedgerCodeNo, $"%{term}%") ||
                        EF.Functions.Like(x.GroupLedgerName, $"%{term}%") ||
                        EF.Functions.Like(x.ShortName ?? "", $"%{term}%"));
                }

                Expression<Func<AccGroupLedger, object>> orderByExpression = currentSortColumn?.ToLower() switch
                {
                    "groupledgercode" or "groupledgercodeno" or "code" => x => x.GroupLedgerCodeNo,
                    "groupledgername" or "name" => x => x.GroupLedgerName,
                    "shortname" => x => x.ShortName ?? "",
                    "status" or "isactive" => x => x.IsActive ?? "1",
                    _ => x => x.GroupLedgerCodeNo,
                };

                query = currentSortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(orderByExpression)
                    : query.OrderBy(orderByExpression);

                var totalItems = await query.CountAsync();

                var pagedEntities = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var groupCodes = pagedEntities.Select(x => x.GroupLedgerCodeNo).ToList();
                var childCounts = await context.AccControlLedgers
                    .Where(x => groupCodes.Contains(x.GroupLedgerCodeNo))
                    .GroupBy(x => x.GroupLedgerCodeNo)
                    .Select(g => new { Code = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Code, x => x.Count);

                var items = pagedEntities.Select(x => new GetAllGroupLedgerVM
                {
                    AutoId = x.AutoId,
                    GroupLedgerCodeNo = x.GroupLedgerCodeNo,
                    GroupLedgerName = x.GroupLedgerName,
                    ShortName = x.ShortName,
                    IsActive = x.IsActive == "1" || x.IsActive == null,
                    ChildCount = childCounts.TryGetValue(x.GroupLedgerCodeNo, out int cnt) ? cnt : 0
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalItems / (pageSize > 0 ? pageSize : 10));

                return new PaginationService<AccGroupLedger, GetAllGroupLedgerVM>.PaginationResult<GetAllGroupLedgerVM>
                {
                    Data = items,
                    TotalCount = totalItems,
                    PaginationInfo = new PaginationService<AccGroupLedger, GetAllGroupLedgerVM>.PaginationInfo
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
                var entity = await context.AccGroupLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GroupLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Group Ledger not found.";
                    return res;
                }

                res.Success = true;
                res.Data = new GroupLedgerAddVM
                {
                    GroupLedgerCodeNo = entity.GroupLedgerCodeNo,
                    GroupLedgerName = entity.GroupLedgerName,
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

        public async Task<string> GetNextCodeAsync()
        {
            try
            {
                var codes = await context.AccGroupLedgers
                    .Select(x => x.GroupLedgerCodeNo)
                    .ToListAsync();

                int max = 0;
                foreach (var c in codes)
                {
                    if (int.TryParse(c, out int n) && n > max)
                        max = n;
                }

                return (max + 1).ToString();
            }
            catch
            {
                return "1";
            }
        }

        public async Task<CommonReturnViewModel> AddAsync(GroupLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(model.GroupLedgerName))
                {
                    res.Success = false;
                    res.Message = "Group Ledger Name is required.";
                    return res;
                }

                var nameExists = await context.AccGroupLedgers
                    .AnyAsync(x => x.GroupLedgerName.ToLower() == model.GroupLedgerName.Trim().ToLower());
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "A Group Ledger with this name already exists.";
                    return res;
                }

                var code = string.IsNullOrWhiteSpace(model.GroupLedgerCodeNo)
                    ? await GetNextCodeAsync()
                    : model.GroupLedgerCodeNo.Trim();

                var codeExists = await context.AccGroupLedgers.AnyAsync(x => x.GroupLedgerCodeNo == code);
                if (codeExists)
                {
                    code = await GetNextCodeAsync();
                }

                var entity = new AccGroupLedger
                {
                    GroupLedgerCodeNo = code,
                    GroupLedgerName = model.GroupLedgerName.Trim(),
                    ShortName = model.ShortName?.Trim() ?? string.Empty,
                    IsActive = model.IsActive ? "1" : "0",
                    Ldate = DateTime.Now
                };

                await context.AccGroupLedgers.AddAsync(entity);
                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Group Ledger added successfully.";
                res.Data = entity;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to add Group Ledger: " + ex.Message;
            }
            return res;
        }

        public async Task<CommonReturnViewModel> UpdateAsync(GroupLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await context.AccGroupLedgers
                    .FirstOrDefaultAsync(x => x.GroupLedgerCodeNo == model.GroupLedgerCodeNo);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Group Ledger not found.";
                    return res;
                }

                var nameExists = await context.AccGroupLedgers
                    .AnyAsync(x => x.GroupLedgerName.ToLower() == model.GroupLedgerName.Trim().ToLower()
                                   && x.GroupLedgerCodeNo != model.GroupLedgerCodeNo);
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "Another Group Ledger with this name already exists.";
                    return res;
                }

                entity.GroupLedgerName = model.GroupLedgerName.Trim();
                entity.ShortName = model.ShortName?.Trim() ?? string.Empty;
                entity.IsActive = model.IsActive ? "1" : "0";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Group Ledger updated successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to update Group Ledger: " + ex.Message;
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

                var entities = await context.AccGroupLedgers
                    .Where(x => requestVM.Codes.Contains(x.GroupLedgerCodeNo))
                    .ToListAsync();

                if (!entities.Any())
                {
                    res.Success = false;
                    res.Message = "Selected items not found.";
                    return res;
                }

                var hasChildren = await context.AccControlLedgers
                    .AnyAsync(x => requestVM.Codes.Contains(x.GroupLedgerCodeNo));

                if (hasChildren)
                {
                    res.Success = false;
                    res.Message = "Cannot delete: One or more selected Group Ledgers have child Control Ledgers. Delete child ledgers first.";
                    return res;
                }

                foreach (var entity in entities)
                {
                    entity.IsActive = "0";
                    entity.ModifyDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = $"{entities.Count} Group Ledger(s) archived successfully.";
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
                var entity = await context.AccGroupLedgers
                    .FirstOrDefaultAsync(x => x.GroupLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Group Ledger not found.";
                    return res;
                }

                bool currentlyActive = entity.IsActive == "1" || entity.IsActive == null;
                entity.IsActive = currentlyActive ? "0" : "1";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = currentlyActive ? "Group Ledger archived." : "Group Ledger restored.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }
            return res;
        }

        public async Task<List<LedgerDropdownItemVM>> GetDropdownAsync()
        {
            return await context.AccGroupLedgers
                .AsNoTracking()
                .Where(x => x.IsActive == "1" || x.IsActive == null)
                .OrderBy(x => x.GroupLedgerCodeNo)
                .Select(x => new LedgerDropdownItemVM
                {
                    Code = x.GroupLedgerCodeNo,
                    Name = x.GroupLedgerName
                })
                .ToListAsync();
        }
    }
}
