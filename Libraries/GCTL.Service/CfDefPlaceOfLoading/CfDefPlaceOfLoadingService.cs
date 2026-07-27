using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.CfDefPlaceOfLoading;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.CfDefPlaceOfLoading
{
    public class CfDefPlaceOfLoadingService : AppService<GCTL.Data.Models.CfDefPlaceOfLoading>, ICfDefPlaceOfLoadingService
    {
        #region Service

        private readonly IRepository<GCTL.Data.Models.CfDefPlaceOfLoading> repository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        private string strMaxNO = "";

        private const string TableName = "CF_Def_PlaceOfLoading";
        private const string ColumnName = "PlaceOfLoadingID";
        private const string PermissionTitle = "Place Of Loading"; // Updated permission title

        public CfDefPlaceOfLoadingService(
            IRepository<GCTL.Data.Models.CfDefPlaceOfLoading> repository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService)
            : base(repository)
        {
            this.repository = repository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region Get All

        public async Task<List<CfDefPlaceOfLoadingSetupViewModel>> GetAllAsync()
        {
            try
            {
                var entity = await repository.GetAllAsync();

                return entity
                    .OrderByDescending(x => x.AutoId)
                    .Select(x => new CfDefPlaceOfLoadingSetupViewModel
                    {
                        Tc = x.AutoId,
                        PlaceOfLoadingId = x.PlaceOfLoadingId,
                        PlaceOfLoading = x.PlaceOfLoadingName,
                        ShortName = x.ShortName,

                        Ldate = x.Ldate,
                        ModifyDate = x.ModifyDate,
                        Luser = x.Luser,
                        Lip = x.Lip,
                        Lmac = x.Lmac
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Get By Id

        public async Task<CfDefPlaceOfLoadingSetupViewModel> GetByIdAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return null;
                }
                var entitye =  repository.All().ToList();
                var entity = await repository.All().Where(x=> x.PlaceOfLoadingId == code.Trim()).FirstOrDefaultAsync();
                //var entity = repository.GetById( int.Parse(code.Trim()));

                if (entity == null)
                    return null;

                return new CfDefPlaceOfLoadingSetupViewModel
                {
                    Tc = entity.AutoId,
                    PlaceOfLoadingId = entity.PlaceOfLoadingId,
                    PlaceOfLoading = entity.PlaceOfLoadingName,
                    ShortName = entity.ShortName,

                    Ldate = entity.Ldate,
                    ModifyDate = entity.ModifyDate,
                    Luser = entity.Luser,
                    Lip = entity.Lip,
                    Lmac = entity.Lmac
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Save

        public async Task<bool> SaveAsync(CfDefPlaceOfLoadingSetupViewModel vm)
        {
            try
            {
                // Validate input
                if (vm == null || string.IsNullOrWhiteSpace(vm.PlaceOfLoading))
                {
                    throw new ArgumentException("Place of Loading name is required.");
                }

                // Get next ID
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);

                await repository.BeginTransactionAsync();

                var entity = new GCTL.Data.Models.CfDefPlaceOfLoading
                {
                    PlaceOfLoadingId = strMaxNO,
                    PlaceOfLoadingName = vm.PlaceOfLoading.Trim(),
                    ShortName = vm.ShortName?.Trim() ?? string.Empty,

                    Ldate = DateTime.Now,
                    Luser = vm.Luser,
                    Lip = vm.Lip,
                    Lmac = vm.Lmac
                };

                await repository.AddAsync(entity);
                await repository.CommitTransactionAsync();

                // Update the viewmodel with the assigned ID
                vm.PlaceOfLoadingId = strMaxNO;

                return true;
            }
            catch (Exception ex)
            {
                await repository.RollbackTransactionAsync();
                Console.WriteLine($"Error in SaveAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Update

        public async Task<bool> UpdateAsync(CfDefPlaceOfLoadingSetupViewModel vm)
        {
            try
            {
                // Validate input
                if (vm == null || string.IsNullOrWhiteSpace(vm.PlaceOfLoadingId))
                {
                    throw new ArgumentException("Place of Loading ID is required.");
                }

                await repository.BeginTransactionAsync();

                var entity = await repository.All().Where(x=> x.PlaceOfLoadingId == vm.PlaceOfLoadingId.Trim()).FirstOrDefaultAsync();

                if (entity == null)
                {
                    await repository.RollbackTransactionAsync();
                    return false;
                }

                entity.PlaceOfLoadingName = vm.PlaceOfLoading?.Trim() ?? string.Empty;
                entity.ShortName = vm.ShortName?.Trim() ?? string.Empty;

                entity.Luser = vm.Luser;
                entity.Lip = vm.Lip;
                entity.Lmac = vm.Lmac;
                entity.ModifyDate = DateTime.Now;

                await repository.UpdateAsync(entity);
                await repository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await repository.RollbackTransactionAsync();
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Dropdown/Selection

        public async Task<IEnumerable<CommonSelectModel>> SelectionPlaceOfLoadingAsync()
        {
            try
            {
                return await repository.All()
                    .OrderBy(x => x.PlaceOfLoadingName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.PlaceOfLoadingId,
                        Name = x.PlaceOfLoadingName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SelectionPlaceOfLoadingAsync: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Duplicate Check

        public async Task<bool> IsExistByCodeAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return false;

                return await repository.All()
                    .AnyAsync(x => x.PlaceOfLoadingId == code.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsExistByCodeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsExistAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                return await repository.All()
                    .AnyAsync(x => x.PlaceOfLoadingName == name.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsExistAsync (name only): {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsExistAsync(string name, string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                return await repository.All()
                    .AnyAsync(x => x.PlaceOfLoadingName == name.Trim() &&
                                   x.PlaceOfLoadingId != code.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsExistAsync (name and code): {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Delete

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return (false, "No items provided for deletion.");
                }

                await repository.BeginTransactionAsync();

                try
                {
                    foreach (var id in ids)
                    {
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        var entity = await repository.All().Where(x => x.PlaceOfLoadingId == id.Trim()).FirstOrDefaultAsync();

                        if (entity != null)
                        {
                            // Log to delete history
                            if (deleteHistoryService != null)
                            {
                                model.tableName = TableName;
                                //model.RecordId = id;
                                // Uncomment when DeleteHistoryService.SaveAsync is available
                                // await deleteHistoryService.SaveAsync(model);
                            }

                            await repository.DeleteAsync(entity);
                        }
                    }

                    await repository.CommitTransactionAsync();

                    return (true, "Deleted successfully.");
                }
                catch (Exception ex)
                {
                    await repository.RollbackTransactionAsync();
                    return (false, $"Error during deletion: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteTab: {ex.Message}");
                return (false, "An unexpected error occurred during deletion.");
            }
        }

        #endregion

        #region Permission Methods

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessCode))
                    return false;

                return await accessCodeRepository.All()
                    .AnyAsync(x => x.AccessCodeId == accessCode &&
                                   x.Title == PermissionTitle &&
                                   x.TitleCheck);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PagePermissionAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessCode))
                    return false;

                return await accessCodeRepository.All()
                    .AnyAsync(x => x.AccessCodeId == accessCode &&
                                   x.Title == PermissionTitle &&
                                   x.CheckAdd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SavePermissionAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessCode))
                    return false;

                return await accessCodeRepository.All()
                    .AnyAsync(x => x.AccessCodeId == accessCode &&
                                   x.Title == PermissionTitle &&
                                   x.CheckEdit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdatePermissionAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessCode))
                    return false;

                return await accessCodeRepository.All()
                    .AnyAsync(x => x.AccessCodeId == accessCode &&
                                   x.Title == PermissionTitle &&
                                   x.CheckDelete);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeletePermissionAsync: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}