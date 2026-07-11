using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SupplierType;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.SupplierType
{
    public class SupplierTypeService : AppService<InvDefSupplierType>, ISupplierTypeService
    {
        #region Service & Repository
        private readonly IRepository<InvDefSupplierType> supplierTyperepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;

        private const string TableName = "Inv_Def_SupplierType";
        private const string ColumnName = "SupplierTypeID";
        public SupplierTypeService(
            IRepository<InvDefSupplierType> supplierTyperepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
            
    : base(supplierTyperepository)
        {
            this.supplierTyperepository = supplierTyperepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<SupplierTypeSetupViewModel>> GetAllAsync()
        {
            var entity = await supplierTyperepository.GetAllAsync();
            return entity.Select(entityVM => new SupplierTypeSetupViewModel
            {
                SupplierTypeCode = entityVM.SupplierTypeCode,
                SupplierTypeId = entityVM.SupplierTypeId,
                SupplierTypeName = entityVM.SupplierTypeName,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,

            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<SupplierTypeSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await supplierTyperepository.GetByIdAsync(code);
            if (entity == null) return null;

            SupplierTypeSetupViewModel entityVM = new SupplierTypeSetupViewModel();
            entityVM.SupplierTypeCode = entity.SupplierTypeCode;
            entityVM.SupplierTypeId = entity.SupplierTypeId;
            entityVM.SupplierTypeName = entity.SupplierTypeName;
            entityVM.Luser = entity.Luser;
            entityVM.Ldate = entity.Ldate;
            entityVM.ModifyDate = entity.ModifyDate;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;

            return entityVM;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(SupplierTypeSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
            await supplierTyperepository.BeginTransactionAsync();
            try
            {
                InvDefSupplierType entity = new InvDefSupplierType();
                entity.SupplierTypeId = strMaxNO;
                entity.SupplierTypeName = entityVM.SupplierTypeName;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await supplierTyperepository.AddAsync(entity);
                await supplierTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await supplierTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync
        public async Task<bool> UpdateAsync(SupplierTypeSetupViewModel entityVM)
        {
            await supplierTyperepository.BeginTransactionAsync();
            try
            {
                var entity = await supplierTyperepository.GetByIdAsync(entityVM.SupplierTypeId);
                if (entity == null)
                {
                    await supplierTyperepository.RollbackTransactionAsync();
                    return false;
                }
                entity.SupplierTypeId = entityVM.SupplierTypeId;
                entity.SupplierTypeName = entityVM.SupplierTypeName;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await supplierTyperepository.UpdateAsync(entity);
                await supplierTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await supplierTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync
        public async Task<IEnumerable<CommonSelectModel>> SelectionSupplierTypeAsync()
        {

            var data = await supplierTyperepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.SupplierTypeId,
                           Name = x.SupplierTypeName,
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
                supplierTyperepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await supplierTyperepository.BeginTransactionAsync();

            try
            {
                var entity = await supplierTyperepository.All().Where(x => ids.Contains(x.SupplierTypeId)).ToListAsync();

                supplierTyperepository.Delete(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await supplierTyperepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await supplierTyperepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await supplierTyperepository.All().AnyAsync(x => x.SupplierTypeId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await supplierTyperepository.All().AnyAsync(x => x.SupplierTypeName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await supplierTyperepository.All().AnyAsync(x => x.SupplierTypeName == name && x.SupplierTypeId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Supplier Type" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Supplier Type" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Supplier Type" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Supplier Type" && x.CheckDelete);
        }
        #endregion
    }
}
