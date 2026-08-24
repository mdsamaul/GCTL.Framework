using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentType;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.PaymentType
{
    public class PaymentTypeService : AppService<SalesDefPaymentType>, IPaymentTypeService
    {
        #region Service & Repository
        private readonly IRepository<SalesDefPaymentType> paymentTyperepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        private string strMaxNO = string.Empty;
        private const string TableName = "Sales_Def_PaymentType";
        private const string ColumnName = "PaymentTypeID";

        public PaymentTypeService(
            IRepository<SalesDefPaymentType> paymentTyperepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService)
            : base(paymentTyperepository)
        {
            this.paymentTyperepository = paymentTyperepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<PaymentTypeSetupViewModel>> GetAllAsync()
        {
            var entity = await paymentTyperepository.GetAllAsync();
            return entity.Select(entityVM => new PaymentTypeSetupViewModel
            {
                Tc = entityVM.Tc,
                AutoId = entityVM.Tc,
                PaymentTypeId = entityVM.PaymentTypeId,
                PaymentType = entityVM.PaymentType,
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

        public async Task<PaymentTypeSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await paymentTyperepository.All().FirstOrDefaultAsync(x => x.PaymentTypeId == code || x.Tc.ToString() == code);
            if (entity == null) return null;

            return new PaymentTypeSetupViewModel
            {
                Tc = entity.Tc,
                AutoId = entity.Tc,
                PaymentTypeId = entity.PaymentTypeId,
                PaymentType = entity.PaymentType,
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

        public async Task<bool> SaveAsync(PaymentTypeSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
            await paymentTyperepository.BeginTransactionAsync();
            try
            {
                SalesDefPaymentType entity = new SalesDefPaymentType
                {
                    PaymentTypeId = string.IsNullOrWhiteSpace(entityVM.PaymentTypeId) ? strMaxNO : entityVM.PaymentTypeId,
                    PaymentType = entityVM.PaymentType,
                    ShortName = entityVM.ShortName,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };
                await paymentTyperepository.AddAsync(entity);
                await paymentTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message {ex.Message}");
                await paymentTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(PaymentTypeSetupViewModel entityVM)
        {
            await paymentTyperepository.BeginTransactionAsync();
            try
            {
                var entity = await paymentTyperepository.All().FirstOrDefaultAsync(x => x.Tc == entityVM.Tc || x.PaymentTypeId == entityVM.PaymentTypeId);
                if (entity == null)
                {
                    await paymentTyperepository.RollbackTransactionAsync();
                    return false;
                }
                entity.PaymentTypeId = entityVM.PaymentTypeId;
                entity.PaymentType = entityVM.PaymentType;
                entity.ShortName = entityVM.ShortName;
                entity.Luser = entityVM.Luser ?? entity.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await paymentTyperepository.UpdateAsync(entity);
                await paymentTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await paymentTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionPaymentTypeAsync()
        {
            return await paymentTyperepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.PaymentTypeId,
                           Name = x.PaymentType,
                       }).ToListAsync();
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "PaymentTypeId", "PaymentType", "Payment" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                paymentTyperepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await paymentTyperepository.BeginTransactionAsync();

            try
            {
                var entity = await paymentTyperepository.All().Where(x => ids.Contains(x.PaymentTypeId) || ids.Contains(x.Tc.ToString())).ToListAsync();

                paymentTyperepository.Delete(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await paymentTyperepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await paymentTyperepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check 

        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await paymentTyperepository.All().AnyAsync(x => x.PaymentTypeId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await paymentTyperepository.All().AnyAsync(x => x.PaymentType == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await paymentTyperepository.All().AnyAsync(x => x.PaymentType == name && x.PaymentTypeId != typeCode);
        }

        #endregion

        #region Permission all type

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Type" || x.Title == "PaymentType" || x.Title == "Payment" || x.Title == "Payment Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Type" || x.Title == "PaymentType" || x.Title == "Payment" || x.Title == "Payment Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Type" || x.Title == "PaymentType" || x.Title == "Payment" || x.Title == "Payment Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Type" || x.Title == "PaymentType" || x.Title == "Payment" || x.Title == "Payment Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }

        #endregion
    }
}
