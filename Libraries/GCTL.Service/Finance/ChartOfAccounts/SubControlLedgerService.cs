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
    public class SubControlLedgerService : ISubControlLedgerService
    {
        private readonly GCTL_ERP_DBContext context;

        public SubControlLedgerService(GCTL_ERP_DBContext context)
        {
            this.context = context;
        }

        public async Task<PaginationService<AccSubControlLedger, GetAllSubControlLedgerVM>.PaginationResult<GetAllSubControlLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? controlCode = null, string? groupCode = null, bool showArchived = false)
        {
            try
            {
                var query = context.AccSubControlLedgers.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(controlCode))
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
                        EF.Functions.Like(x.SubControlLedgerCodeNo, $"%{term}%") ||
                        EF.Functions.Like(x.SubControlLedgerName, $"%{term}%") ||
                        EF.Functions.Like(x.ShortName ?? "", $"%{term}%"));
                }

                Expression<Func<AccSubControlLedger, object>> orderByExpression = currentSortColumn?.ToLower() switch
                {
                    "subcontrolledgercode" or "code" => x => x.SubControlLedgerCodeNo,
                    "subcontrolledgername" or "name" => x => x.SubControlLedgerName,
                    "shortname" => x => x.ShortName ?? "",
                    "status" or "isactive" => x => x.IsActive ?? "1",
                    _ => x => x.SubControlLedgerCodeNo,
                };

                query = currentSortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(orderByExpression)
                    : query.OrderBy(orderByExpression);

                var totalItems = await query.CountAsync();

                var pagedEntities = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var sclCodes = pagedEntities.Select(x => x.SubControlLedgerCodeNo).ToList();
                var childCounts = await context.AccSubSubsidiaryLedgers
                    .Where(x => sclCodes.Contains(x.SubControlLedgerCodeNo))
                    .GroupBy(x => x.SubControlLedgerCodeNo)
                    .Select(g => new { Code = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Code, x => x.Count);

                var ctrlCodes = pagedEntities.Select(x => x.ControlLedgerCodeNo).Distinct().ToList();
                var ctrlNames = await context.AccControlLedgers
                    .Where(x => ctrlCodes.Contains(x.ControlLedgerCodeNo))
                    .ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName);

                var grpCodes = pagedEntities.Select(x => x.GroupLedgerCodeNo).Distinct().ToList();
                var grpNames = await context.AccGroupLedgers
                    .Where(x => grpCodes.Contains(x.GroupLedgerCodeNo))
                    .ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);

                var items = pagedEntities.Select(x => new GetAllSubControlLedgerVM
                {
                    AutoId = x.AutoId,
                    SubControlLedgerCodeNo = x.SubControlLedgerCodeNo,
                    ControlLedgerCodeNo = x.ControlLedgerCodeNo,
                    ControlLedgerName = ctrlNames.TryGetValue(x.ControlLedgerCodeNo, out var cName) ? cName : string.Empty,
                    GroupLedgerCodeNo = x.GroupLedgerCodeNo,
                    GroupLedgerName = grpNames.TryGetValue(x.GroupLedgerCodeNo, out var gName) ? gName : string.Empty,
                    SubControlLedgerName = x.SubControlLedgerName,
                    ShortName = x.ShortName,
                    IsActive = x.IsActive == "1" || x.IsActive == null,
                    ChildCount = childCounts.TryGetValue(x.SubControlLedgerCodeNo, out int cnt) ? cnt : 0
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalItems / (pageSize > 0 ? pageSize : 10));

                return new PaginationService<AccSubControlLedger, GetAllSubControlLedgerVM>.PaginationResult<GetAllSubControlLedgerVM>
                {
                    Data = items,
                    TotalCount = totalItems,
                    PaginationInfo = new PaginationService<AccSubControlLedger, GetAllSubControlLedgerVM>.PaginationInfo
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
                var entity = await context.AccSubControlLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SubControlLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Sub-Control Ledger not found.";
                    return res;
                }

                res.Success = true;
                res.Data = new SubControlLedgerAddVM
                {
                    SubControlLedgerCodeNo = entity.SubControlLedgerCodeNo,
                    ControlLedgerCodeNo = entity.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = entity.GroupLedgerCodeNo,
                    SubControlLedgerName = entity.SubControlLedgerName,
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

        public async Task<string> GetNextCodeAsync(string controlCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(controlCode))
                    return "001";

                controlCode = controlCode.Trim();

                var codes = await context.AccSubControlLedgers
                    .Where(x => x.ControlLedgerCodeNo == controlCode)
                    .Select(x => x.SubControlLedgerCodeNo)
                    .ToListAsync();

                int maxSuffix = 0;
                foreach (var c in codes)
                {
                    if (c != null && c.StartsWith(controlCode) && c.Length > controlCode.Length)
                    {
                        var suffix = c.Substring(controlCode.Length);
                        if (int.TryParse(suffix, out int n) && n > maxSuffix)
                            maxSuffix = n;
                    }
                }

                int next = maxSuffix + 1;
                return $"{controlCode}{next:D3}";
            }
            catch
            {
                return $"{controlCode}001";
            }
        }

        public async Task<CommonReturnViewModel> AddAsync(SubControlLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(model.ControlLedgerCodeNo))
                {
                    res.Success = false;
                    res.Message = "Control Ledger is required.";
                    return res;
                }

                if (string.IsNullOrWhiteSpace(model.SubControlLedgerName))
                {
                    res.Success = false;
                    res.Message = "Sub-Control Ledger Name is required.";
                    return res;
                }

                var ctrl = await context.AccControlLedgers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ControlLedgerCodeNo == model.ControlLedgerCodeNo);

                if (ctrl == null)
                {
                    res.Success = false;
                    res.Message = "Selected Control Ledger does not exist.";
                    return res;
                }

                var nameExists = await context.AccSubControlLedgers
                    .AnyAsync(x => x.ControlLedgerCodeNo == model.ControlLedgerCodeNo &&
                                   x.SubControlLedgerName.ToLower() == model.SubControlLedgerName.Trim().ToLower());
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "A Sub-Control Ledger with this name already exists in the selected Control Ledger.";
                    return res;
                }

                var code = string.IsNullOrWhiteSpace(model.SubControlLedgerCodeNo)
                    ? await GetNextCodeAsync(model.ControlLedgerCodeNo)
                    : model.SubControlLedgerCodeNo.Trim();

                var codeExists = await context.AccSubControlLedgers.AnyAsync(x => x.SubControlLedgerCodeNo == code);
                if (codeExists)
                {
                    code = await GetNextCodeAsync(model.ControlLedgerCodeNo);
                }

                var entity = new AccSubControlLedger
                {
                    SubControlLedgerCodeNo = code,
                    ControlLedgerCodeNo = model.ControlLedgerCodeNo.Trim(),
                    GroupLedgerCodeNo = ctrl.GroupLedgerCodeNo,
                    SubControlLedgerName = model.SubControlLedgerName.Trim(),
                    ShortName = model.ShortName?.Trim() ?? string.Empty,
                    IsActive = model.IsActive ? "1" : "0",
                    Ldate = DateTime.Now
                };

                await context.AccSubControlLedgers.AddAsync(entity);
                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Sub-Control Ledger added successfully.";
                res.Data = entity;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to add Sub-Control Ledger: " + ex.Message;
            }
            return res;
        }

        public async Task<CommonReturnViewModel> UpdateAsync(SubControlLedgerAddVM model)
        {
            var res = new CommonReturnViewModel();
            try
            {
                var entity = await context.AccSubControlLedgers
                    .FirstOrDefaultAsync(x => x.SubControlLedgerCodeNo == model.SubControlLedgerCodeNo);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Sub-Control Ledger not found.";
                    return res;
                }

                var nameExists = await context.AccSubControlLedgers
                    .AnyAsync(x => x.ControlLedgerCodeNo == entity.ControlLedgerCodeNo &&
                                   x.SubControlLedgerName.ToLower() == model.SubControlLedgerName.Trim().ToLower() &&
                                   x.SubControlLedgerCodeNo != model.SubControlLedgerCodeNo);
                if (nameExists)
                {
                    res.Success = false;
                    res.Message = "Another Sub-Control Ledger with this name already exists in this Control Ledger.";
                    return res;
                }

                entity.SubControlLedgerName = model.SubControlLedgerName.Trim();
                entity.ShortName = model.ShortName?.Trim() ?? string.Empty;
                entity.IsActive = model.IsActive ? "1" : "0";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = "Sub-Control Ledger updated successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Failed to update Sub-Control Ledger: " + ex.Message;
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

                var entities = await context.AccSubControlLedgers
                    .Where(x => requestVM.Codes.Contains(x.SubControlLedgerCodeNo))
                    .ToListAsync();

                if (!entities.Any())
                {
                    res.Success = false;
                    res.Message = "Selected items not found.";
                    return res;
                }

                var hasChildren = await context.AccSubSubsidiaryLedgers
                    .AnyAsync(x => requestVM.Codes.Contains(x.SubControlLedgerCodeNo));

                if (hasChildren)
                {
                    res.Success = false;
                    res.Message = "Cannot delete: One or more selected Sub-Control Ledgers have child Sub-Subsidiary Ledgers. Delete child ledgers first.";
                    return res;
                }

                foreach (var entity in entities)
                {
                    entity.IsActive = "0";
                    entity.ModifyDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = $"{entities.Count} Sub-Control Ledger(s) archived successfully.";
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
                var entity = await context.AccSubControlLedgers
                    .FirstOrDefaultAsync(x => x.SubControlLedgerCodeNo == code);

                if (entity == null)
                {
                    res.Success = false;
                    res.Message = "Sub-Control Ledger not found.";
                    return res;
                }

                bool currentlyActive = entity.IsActive == "1" || entity.IsActive == null;
                entity.IsActive = currentlyActive ? "0" : "1";
                entity.ModifyDate = DateTime.Now;

                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = currentlyActive ? "Sub-Control Ledger archived." : "Sub-Control Ledger restored.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
            }
            return res;
        }

        public async Task<List<LedgerDropdownItemVM>> GetDropdownAsync(string? controlCode = null)
        {
            var q = context.AccSubControlLedgers
                .AsNoTracking()
                .Where(x => x.IsActive == "1" || x.IsActive == null);

            if (!string.IsNullOrWhiteSpace(controlCode))
            {
                q = q.Where(x => x.ControlLedgerCodeNo == controlCode);
            }

            return await q
                .OrderBy(x => x.SubControlLedgerCodeNo)
                .Select(x => new LedgerDropdownItemVM
                {
                    Code = x.SubControlLedgerCodeNo,
                    Name = x.SubControlLedgerName,
                    ParentCode = x.ControlLedgerCodeNo
                })
                .ToListAsync();
        }
    }
}
