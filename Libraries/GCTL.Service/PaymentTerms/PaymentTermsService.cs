using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentTerms;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.PaymentTerms
{
    public class PaymentTermsService : AppService<SalesDefPaymentTerm>, IPaymentTermsService
    {
        #region Service & Repository
        private readonly IRepository<SalesDefPaymentTerm> paymentTermsrepository;
        private readonly IRepository<SalesDefPaymentType> paymentTyperepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        private string strMaxNO = string.Empty;
        private const string TableName = "Sales_Def_PaymentTerms";
        private const string ColumnName = "PaymentTermsId";

        public PaymentTermsService(
            IRepository<SalesDefPaymentTerm> paymentTermsrepository,
            IRepository<SalesDefPaymentType> paymentTyperepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService)
            : base(paymentTermsrepository)
        {
            this.paymentTermsrepository = paymentTermsrepository;
            this.paymentTyperepository = paymentTyperepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<PaymentTermsSetupViewModel>> GetAllAsync()
        {
            var query = await (from per in paymentTermsrepository.All()
                               join hrm in paymentTyperepository.All()
                                   on per.Type equals hrm.PaymentTypeId into JobGroup
                               from hrm in JobGroup.DefaultIfEmpty()
                               select new PaymentTermsSetupViewModel
                               {
                                   Tc = per.Tc,
                                   AutoId = per.Tc,
                                   PaymentTermsId = per.PaymentTermsId,
                                   PaymentTermsName = per.PaymentTermsName,
                                   Type = per.Type,
                                   PaymentType = hrm.PaymentType,
                                   CreditDays = per.CreditDays,
                                   Percentise = per.Percentise,
                                   Ldate = per.Ldate,
                                   ModifyDate = per.ModifyDate,
                                   Luser = per.Luser,
                                   Lip = per.Lip,
                                   Lmac = per.Lmac
                               }).ToListAsync();

            return query;
        }

        #endregion

        #region GetByIdAsync

        public async Task<PaymentTermsSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await paymentTermsrepository.All().FirstOrDefaultAsync(x => x.PaymentTermsId == code || x.Tc.ToString() == code);
            if (entity == null) return null;

            var paymentTypeEntity = await paymentTyperepository.All().FirstOrDefaultAsync(x => x.PaymentTypeId == entity.Type);

            return new PaymentTermsSetupViewModel
            {
                Tc = entity.Tc,
                AutoId = entity.Tc,
                PaymentTermsId = entity.PaymentTermsId,
                Type = entity.Type,
                PaymentType = paymentTypeEntity?.PaymentType,
                Percentise = entity.Percentise,
                CreditDays = entity.CreditDays,
                PaymentTermsName = entity.PaymentTermsName,
                Luser = entity.Luser,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(PaymentTermsSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 2);
            await paymentTermsrepository.BeginTransactionAsync();
            try
            {
                SalesDefPaymentTerm entity = new SalesDefPaymentTerm
                {
                    PaymentTermsId = string.IsNullOrWhiteSpace(entityVM.PaymentTermsId) ? strMaxNO : entityVM.PaymentTermsId,
                    Type = entityVM.Type,
                    Percentise = entityVM.Percentise,
                    CreditDays = entityVM.CreditDays,
                    PaymentTermsName = entityVM.PaymentTermsName,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };
                await paymentTermsrepository.AddAsync(entity);
                await paymentTermsrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message {ex.Message}");
                await paymentTermsrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(PaymentTermsSetupViewModel entityVM)
        {
            await paymentTermsrepository.BeginTransactionAsync();
            try
            {
                var entity = await paymentTermsrepository.All().FirstOrDefaultAsync(x => x.Tc == entityVM.Tc || x.PaymentTermsId == entityVM.PaymentTermsId);
                if (entity == null)
                {
                    await paymentTermsrepository.RollbackTransactionAsync();
                    return false;
                }
                entity.PaymentTermsId = entityVM.PaymentTermsId;
                entity.Type = entityVM.Type ?? string.Empty;
                entity.Percentise = entityVM.Percentise ?? string.Empty;
                entity.CreditDays = entityVM.CreditDays;
                entity.PaymentTermsName = entityVM.PaymentTermsName ?? string.Empty;
                entity.Luser = entityVM.Luser ?? entity.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await paymentTermsrepository.UpdateAsync(entity);
                await paymentTermsrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await paymentTermsrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionPaymentTermsAsync()
        {
            return await paymentTermsrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.PaymentTermsId,
                           Name = x.PaymentTermsName,
                       }).ToListAsync();
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "PaymentTermsId", "PaymentTermsName", "PaymentTerms" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                paymentTermsrepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await paymentTermsrepository.BeginTransactionAsync();

            try
            {
                var entityList = await paymentTermsrepository.All().Where(x => ids.Contains(x.PaymentTermsId) || ids.Contains(x.Tc.ToString())).ToListAsync();

                paymentTermsrepository.Delete(entityList);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entityList, model
                );

                await paymentTermsrepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await paymentTermsrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Checks

        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await paymentTermsrepository.All().AnyAsync(x => x.PaymentTermsId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await paymentTermsrepository.All().AnyAsync(x => x.Type == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await paymentTermsrepository.All().AnyAsync(x => x.PaymentTermsName == name && x.PaymentTermsId != typeCode);
        }

        #endregion

        #region Permissions

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Terms" || x.Title == "PaymentTerms" || x.Title == "Payment Terms Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Terms" || x.Title == "PaymentTerms" || x.Title == "Payment Terms Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Terms" || x.Title == "PaymentTerms" || x.Title == "Payment Terms Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Terms" || x.Title == "PaymentTerms" || x.Title == "Payment Terms Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }

        #endregion
    }
}
