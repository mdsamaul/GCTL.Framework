using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.CustomerType;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.CustomerType
{
    public class CustomerTypeService : AppService<SalesDefCustomerType>, ICustomerTypeService
    {
        #region Service & Repository
        private readonly IRepository<SalesDefCustomerType> customerTyperepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        private string strMaxNO = string.Empty;
        private const string TableName = "Sales_Def_CustomerType";
        private const string ColumnName = "CustomerTypeID";

        public CustomerTypeService(
            IRepository<SalesDefCustomerType> customerTyperepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            )
            
    : base(customerTyperepository)
        {
            this.customerTyperepository = customerTyperepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<CustomerTypeSetupViewModel>> GetAllAsync()
        {
            var entity = await customerTyperepository.GetAllAsync();
            return entity.Select(entityVM => new CustomerTypeSetupViewModel
            {
                AutoId = entityVM.AutoId,
                CustomerTypeId = entityVM.CustomerTypeId,
                CustomerType = entityVM.CustomerType,
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

        public async Task<CustomerTypeSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await customerTyperepository.All().FirstOrDefaultAsync(x => x.CustomerTypeId == code || x.AutoId.ToString() == code);
            if (entity == null) return null;

            return new CustomerTypeSetupViewModel
            {
                AutoId = entity.AutoId,
                CustomerTypeId = entity.CustomerTypeId,
                CustomerType = entity.CustomerType,
                ShortName = entity.ShortName,
                Luser = entity.Luser,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(CustomerTypeSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
            await customerTyperepository.BeginTransactionAsync();
            try
            {
                SalesDefCustomerType entity = new SalesDefCustomerType
                {
                    CustomerTypeId = string.IsNullOrWhiteSpace(entityVM.CustomerTypeId) ? strMaxNO : entityVM.CustomerTypeId,
                    CustomerType = entityVM.CustomerType,
                    ShortName = entityVM.ShortName ?? string.Empty,
                    EmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty,
                    CompanyId = entityVM.CompanyId ?? string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };
                await customerTyperepository.AddAsync(entity);
                await customerTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message {ex.Message}");
                await customerTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(CustomerTypeSetupViewModel entityVM)
        {
            await customerTyperepository.BeginTransactionAsync();
            try
            {
                var entity = await customerTyperepository.All().FirstOrDefaultAsync(x => x.AutoId == entityVM.AutoId || x.CustomerTypeId == entityVM.CustomerTypeId);
                if (entity == null)
                {
                    await customerTyperepository.RollbackTransactionAsync();
                    return false;
                }
                entity.CustomerTypeId = entityVM.CustomerTypeId;
                entity.CustomerType = entityVM.CustomerType;
                entity.ShortName = entityVM.ShortName;
                entity.CompanyId = entityVM.CompanyId ?? entity.CompanyId;
                entity.EmployeeId = entityVM.UserInfoEmployeeId ?? entity.EmployeeId;
                entity.Luser = entityVM.Luser ?? entity.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await customerTyperepository.UpdateAsync(entity);
                await customerTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await customerTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionCustomerTypeAsync()
        {
            return await customerTyperepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.CustomerTypeId,
                           Name = x.CustomerType,
                       }).ToListAsync();
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "CustomerTypeId", "CustomerType", "Customer" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                customerTyperepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await customerTyperepository.BeginTransactionAsync();

            try
            {
                var entity = await customerTyperepository.All().Where(x => ids.Contains(x.CustomerTypeId) || ids.Contains(x.AutoId.ToString())).ToListAsync();

                customerTyperepository.Delete(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await customerTyperepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await customerTyperepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check 

        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await customerTyperepository.All().AnyAsync(x => x.CustomerTypeId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await customerTyperepository.All().AnyAsync(x => x.CustomerType == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await customerTyperepository.All().AnyAsync(x => x.CustomerType == name && x.CustomerTypeId != typeCode);
        }

        #endregion

        #region Permission all type

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Customer Type" || x.Title == "CustomerType" || x.Title == "Customer" || x.Title == "Customer Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Customer Type" || x.Title == "CustomerType" || x.Title == "Customer" || x.Title == "Customer Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Customer Type" || x.Title == "CustomerType" || x.Title == "Customer" || x.Title == "Customer Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Customer Type" || x.Title == "CustomerType" || x.Title == "Customer" || x.Title == "Customer Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }

        #endregion

    }
}
