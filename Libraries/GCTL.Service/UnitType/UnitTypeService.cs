using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.UnitType;
using GCTL.Data.Models;
using GCTL.Service.DeleteHistories;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GCTL.Service.UnitType
{
    public class UnitTypeService : AppService<RmgProdDefUnitType>, IUnitType
    {
        private const string ColumnName = "UnitTypID";
        private readonly IRepository<RmgProdDefUnitType> _repository;
        private readonly IDeleteHistoryService _deleteHistoryService;
        private readonly IRepository<CoreAccessCode> _accessCodeRepository;

        public UnitTypeService(
            IRepository<RmgProdDefUnitType> repository,
            IDeleteHistoryService deleteHistoryService,
            IRepository<CoreAccessCode> accessCodeRepository) : base(repository)
        {
            _repository = repository;
            _deleteHistoryService = deleteHistoryService;
            _accessCodeRepository = accessCodeRepository;
        }

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "UnitTypID", "UnitTypeName", "UnitType" };
            var dependencyCheck = await _deleteHistoryService.CheckDependenciesAsync(
                _repository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.All().Where(x => ids.Contains(x.UnitTypId) || ids.Contains(x.Tc.ToString())).ToListAsync();

                _repository.Delete(entity);
                model.tableName = _repository.GetTableName();
                await _deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await _repository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        public async Task<bool> BulkDeleteAsync(List<int> ids)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.All().Where(c => ids.Contains(c.Tc)).ToListAsync();
                if (entity == null || !entity.Any())
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                await _repository.DeleteRangeAsync(entity);
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Bulk delete error: {ex}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                await _repository.DeleteAsync(entity.Tc);
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Delete error: {ex}");
                return false;
            }
        }

        public async Task<UnitTypeSetupViewModel> GetByIdAsync(int id)
        {
            var pt = await _repository.All().FirstOrDefaultAsync(x => x.Tc == id);
            if (pt == null) return null;

            return new UnitTypeSetupViewModel
            {
                Tc = pt.Tc,
                AutoId = pt.Tc,
                UnitTypID = pt.UnitTypId,
                UnitTypeName = pt.UnitTypeName ?? "",
                ShortName = pt.ShortName ?? "",
                DecimalPlaces = pt.DecimalPlaces,
                Ldate = pt.Ldate,
                Luser = pt.Luser ?? "",
                Lip = pt.Lip ?? "",
                Lmac = pt.Lmac ?? "",
                ModifyDate = pt.ModifyDate
            };
        }

        public async Task<UnitTypeSetupViewModel> GetByIdAsync(string code)
        {
            var pt = await _repository.All().FirstOrDefaultAsync(x => x.UnitTypId == code || x.Tc.ToString() == code);
            if (pt == null) return null;

            return new UnitTypeSetupViewModel
            {
                Tc = pt.Tc,
                AutoId = pt.Tc,
                UnitTypID = pt.UnitTypId,
                UnitTypeName = pt.UnitTypeName ?? "",
                ShortName = pt.ShortName ?? "",
                DecimalPlaces = pt.DecimalPlaces,
                Ldate = pt.Ldate,
                Luser = pt.Luser ?? "",
                Lip = pt.Lip ?? "",
                Lmac = pt.Lmac ?? "",
                ModifyDate = pt.ModifyDate
            };
        }

        public async Task<string> GetLastUnitAsync()
        {
            var last = await _repository.All()
                        .OrderByDescending(c => c.Tc)
                        .FirstOrDefaultAsync();

            return last?.UnitTypId;
        }

        public async Task<PaginationService<RmgProdDefUnitType, UnitTypeVM>.PaginationResult<UnitTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "UnitTypID", string sortOrder = "desc")
        {
            var query = _repository.All();

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            var paginatedResult = await PaginationService<RmgProdDefUnitType, UnitTypeVM>.GetPaginatedData(
                query,
                pageNumber,
                pageSize,
                searchTerm,
                currentSortColumn,
                sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.UnitTypId ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.UnitTypeName ?? "", $"{term}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{term}%"),
                pt => new UnitTypeVM
                {
                    TC = pt.Tc,
                    UnitTypID = pt.UnitTypId,
                    UnitTypeName = pt.UnitTypeName ?? "",
                    ShortName = pt.ShortName ?? "",
                    DecimalPlaces = pt.DecimalPlaces,
                    LUser = pt.Luser ?? "",
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                });

            return paginatedResult;
        }

        public async Task<bool> IsDuplicateAsync(UnitTypeSetupViewModel model)
        {
            if (model == null) return false;

            return await _repository.All().AnyAsync(x =>
                x.UnitTypeName.Trim().ToLower() == model.UnitTypeName.Trim().ToLower() &&
                (model.Tc == 0 || x.Tc != model.Tc));
        }

        public async Task<bool> IsDuplicateAsync(UnitTypeVM model)
        {
            if (model == null) return false;

            return await _repository.All().AnyAsync(x =>
                x.UnitTypeName.Trim().ToLower() == model.UnitTypeName.Trim().ToLower() &&
                (model.TC == null || model.TC == 0 || x.Tc != model.TC));
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.Tc == id);
        }

        public async Task<bool> SaveAsync(UnitTypeSetupViewModel model)
        {
            try
            {
                var entity = new RmgProdDefUnitType
                {
                    UnitTypId = model.UnitTypID,
                    UnitTypeName = model.UnitTypeName,
                    ShortName = model.ShortName,
                    DecimalPlaces = model.DecimalPlaces,
                    Luser = model.Luser ?? "",
                    Ldate = DateTime.Now,
                    Lip = model.Lip ?? GetLocalIP(),
                    Lmac = model.Lmac ?? GetMacAddress(),
                    ModifyDate = model.ModifyDate
                };

                await _repository.AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(UnitTypeSetupViewModel model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.GetByIdAsync(model.Tc);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.UnitTypId = model.UnitTypID;
                entity.UnitTypeName = model.UnitTypeName;
                entity.ShortName = model.ShortName;
                entity.DecimalPlaces = model.DecimalPlaces;
                entity.Luser = model.Luser ?? entity.Luser;
                entity.Ldate = DateTime.Now;
                entity.Lip = model.Lip ?? GetLocalIP();
                entity.Lmac = model.Lmac ?? GetMacAddress();
                entity.ModifyDate = DateTime.Now;

                await _repository.UpdateAsync(entity);
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await _repository.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<List<UnitTypeSetupViewModel>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            return entity.Select(model => new UnitTypeSetupViewModel
            {
                Tc = model.Tc,
                AutoId = model.Tc,
                UnitTypID = model.UnitTypId,
                UnitTypeName = model.UnitTypeName,
                ShortName = model.ShortName,
                DecimalPlaces = model.DecimalPlaces,
                Ldate = model.Ldate,
                Luser = model.Luser,
                Lip = model.Lip,
                Lmac = model.Lmac,
                ModifyDate = model.ModifyDate
            }).ToList();
        }

        #region Ip & Mac
        public string GetLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public string GetMacAddress()
        {
            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var adapter in nics)
                {
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        return adapter.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch { }
            return string.Empty;
        }
        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionUnitTypeAsync()
        {
            return await _repository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.UnitTypId,
                           Name = x.UnitTypeName,
                       }).ToListAsync();
        }

        #endregion

        #region Permissions

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await _accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Unit Type" || x.Title == "UnitType" || x.Title == "Unit" || x.Title == "Unit Type Setup" || x.Title == "Unit Information"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await _accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Unit Type" || x.Title == "UnitType" || x.Title == "Unit" || x.Title == "Unit Type Setup" || x.Title == "Unit Information"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await _accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Unit Type" || x.Title == "UnitType" || x.Title == "Unit" || x.Title == "Unit Type Setup" || x.Title == "Unit Information"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await _accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Unit Type" || x.Title == "UnitType" || x.Title == "Unit" || x.Title == "Unit Type Setup" || x.Title == "Unit Information"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }

        #endregion
    }
}
