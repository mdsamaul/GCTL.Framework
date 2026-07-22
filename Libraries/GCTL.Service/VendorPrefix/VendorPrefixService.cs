using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportType;
using GCTL.Core.ViewModels.VendorPrefix;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.VendorPrefix
{
    public class VendorPrefixService : AppService<CfDefVendorPrefix>, IVendorPrefixService
    {
        #region Service & Repository
        private readonly IRepository<CfDefVendorPrefix> vendorPrefixrepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "CF_Def_VendorPrefix";
        private const string ColumnName = "VendorPrifixId";

        public VendorPrefixService(
            IRepository<CfDefVendorPrefix> vendorPrefixrepository,
             IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
    : base(vendorPrefixrepository)
        {
            this.vendorPrefixrepository = vendorPrefixrepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<VendorPrefixSetupViewModel>> GetAllAsync()
        {
            var entity = await vendorPrefixrepository.GetAllAsync();
            return entity.Select(entityVM => new VendorPrefixSetupViewModel
            {
                Tc = entityVM.Tc,
                VendorPrifixId = entityVM.VendorPrifixId,
                PrifixName = entityVM.PrifixName,
                ShortName = entityVM.ShortName,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,

            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<VendorPrefixSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await vendorPrefixrepository.GetByIdAsync(code);

            if (entity == null)
                return null;

            return new VendorPrefixSetupViewModel
            {
                Tc = entity.Tc,
                VendorPrifixId = entity.VendorPrifixId,
                PrifixName= entity.PrifixName,
                ShortName = entity.ShortName,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(VendorPrefixSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 4);
                await vendorPrefixrepository.BeginTransactionAsync();

                var entity = new CfDefVendorPrefix
                {
                    VendorPrifixId = strMaxNO,
                    PrifixName = entityVM.PrifixName,
                    ShortName = entityVM.ShortName ?? string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await vendorPrefixrepository.AddAsync(entity);
                await vendorPrefixrepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await vendorPrefixrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync
        public async Task<bool> UpdateAsync(VendorPrefixSetupViewModel entityVM)
        {
            await vendorPrefixrepository.BeginTransactionAsync();
            try
            {
                var entity = await vendorPrefixrepository.GetByIdAsync(entityVM.VendorPrifixId);
                if (entity == null)
                {
                    await vendorPrefixrepository.RollbackTransactionAsync();
                    return false;
                }
                entityVM.VendorPrifixId = entity.VendorPrifixId;
                entityVM.PrifixName = entity.PrifixName ?? string.Empty;
                entityVM.ShortName = entity.ShortName ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await vendorPrefixrepository.UpdateAsync(entity);
                await vendorPrefixrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await vendorPrefixrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync
        public async Task<IEnumerable<CommonSelectModel>> SelectionVendorPrefixAsync()
        {

            var data = await vendorPrefixrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.VendorPrifixId,
                           Name = x.PrifixName,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                vendorPrefixrepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await vendorPrefixrepository.BeginTransactionAsync();

            try
            {
                var entity = await vendorPrefixrepository.All().Where(x => ids.Contains(x.VendorPrifixId)).ToListAsync();


                vendorPrefixrepository.Delete(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await vendorPrefixrepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await vendorPrefixrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }


        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await vendorPrefixrepository.All().AnyAsync(x => x.VendorPrifixId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await vendorPrefixrepository.All().AnyAsync(x => x.PrifixName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await vendorPrefixrepository.All().AnyAsync(x => x.PrifixName == name && x.VendorPrifixId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Prefix" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Prefix" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Prefix" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Prefix" && x.CheckDelete);
        }
        #endregion
    }
}
