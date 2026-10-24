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

                var items = pagedEntities.Select(x => new GetAllGeneralLedgerVM
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
                    GeneralLedgerName = x.GeneralLedgerName,
                    ShortName = x.ShortName,
                    IsActive = x.IsActive == "1" || x.IsActive == null
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

                res.Success = true;
                res.Data = new GeneralLedgerAddVM
                {
                    GeneralLedgerCodeNo = entity.GeneralLedgerCodeNo,
                    SubsidiaryLedgerCodeNo = entity.SubsidiaryLedgerCodeNo,
                    SubControlLedgerCodeNo = entity.SubControlLedgerCodeNo,
                    ControlLedgerCodeNo = entity.ControlLedgerCodeNo,
                    GroupLedgerCodeNo = entity.GroupLedgerCodeNo,
                    GeneralLedgerName = entity.GeneralLedgerName,
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

            return await q
                .OrderBy(x => x.GeneralLedgerCodeNo)
                .Select(x => new LedgerDropdownItemVM
                {
                    Code = x.GeneralLedgerCodeNo,
                    Name = x.GeneralLedgerName,
                    ParentCode = x.SubsidiaryLedgerCodeNo
                })
                .ToListAsync();
        }
    }
}
