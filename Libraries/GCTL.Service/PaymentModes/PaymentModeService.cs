using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentModes;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.PaymentModes
{
    public class PaymentModeService : AppService<SalesDefPaymentMode>, IPaymentModeService
    {
        private readonly IRepository<SalesDefPaymentMode> paymentModeRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        private string strMaxNO = string.Empty;
        private const string TableName = "Sales_Def_PaymentMode";
        private const string ColumnName = "PaymentModeID";

        public PaymentModeService(
            IRepository<SalesDefPaymentMode> paymentModeRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService)
            : base(paymentModeRepository)
        {
            this.paymentModeRepository = paymentModeRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #region GetAllAsync & GetByIdAsync

        public async Task<List<PaymentModeSetupViewModel>> GetAllAsync()
        {
            var entityList = await paymentModeRepository.GetAllAsync();
            return entityList.Select(entity => new PaymentModeSetupViewModel
            {
                Tc = entity.AutoId,
                AutoId = entity.AutoId,
                PaymentModeId = entity.PaymentModeId,
                PaymentModeName = entity.PaymentModeName,
                PaymentModeShortName = entity.PaymentModeShortName,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac,
            }).ToList();
        }

        public async Task<PaymentModeSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await paymentModeRepository.All().FirstOrDefaultAsync(x => x.PaymentModeId == code || x.AutoId.ToString() == code);
            if (entity == null) return null;

            return new PaymentModeSetupViewModel
            {
                Tc = entity.AutoId,
                AutoId = entity.AutoId,
                PaymentModeId = entity.PaymentModeId,
                PaymentModeName = entity.PaymentModeName,
                PaymentModeShortName = entity.PaymentModeShortName,
                Luser = entity.Luser,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync & UpdateAsync

        public async Task<bool> SaveAsync(PaymentModeSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
            await paymentModeRepository.BeginTransactionAsync();
            try
            {
                SalesDefPaymentMode entity = new SalesDefPaymentMode
                {
                    PaymentModeId = string.IsNullOrWhiteSpace(entityVM.PaymentModeId) ? strMaxNO : entityVM.PaymentModeId,
                    PaymentModeName = entityVM.PaymentModeName,
                    PaymentModeShortName = entityVM.PaymentModeShortName,
                    CompanyCode = string.Empty,
                    EmployeeId = string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };
                await paymentModeRepository.AddAsync(entity);
                await paymentModeRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message {ex.Message}");
                await paymentModeRepository.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> UpdateAsync(PaymentModeSetupViewModel entityVM)
        {
            await paymentModeRepository.BeginTransactionAsync();
            try
            {
                var entity = await paymentModeRepository.All().FirstOrDefaultAsync(x => x.AutoId == entityVM.AutoId || x.AutoId == entityVM.Tc || x.PaymentModeId == entityVM.PaymentModeId);
                if (entity == null)
                {
                    await paymentModeRepository.RollbackTransactionAsync();
                    return false;
                }
                entity.PaymentModeId = entityVM.PaymentModeId;
                entity.PaymentModeName = entityVM.PaymentModeName;
                entity.PaymentModeShortName = entityVM.PaymentModeShortName;
                entity.Luser = entityVM.Luser ?? entity.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await paymentModeRepository.UpdateAsync(entity);
                await paymentModeRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await paymentModeRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "PaymentModeId", "PaymentModeName", "PaymentMode" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                paymentModeRepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await paymentModeRepository.BeginTransactionAsync();

            try
            {
                var entityList = await paymentModeRepository.All().Where(x => ids.Contains(x.PaymentModeId) || ids.Contains(x.AutoId.ToString())).ToListAsync();

                paymentModeRepository.Delete(entityList);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entityList, model
                );

                await paymentModeRepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await paymentModeRepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Checks

        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await paymentModeRepository.All().AnyAsync(x => x.PaymentModeId == code);
        }

        public async Task<bool> IsPaymentModeExistAsync(string name)
        {
            return await paymentModeRepository.All().AnyAsync(x => x.PaymentModeName == name);
        }

        public async Task<bool> IsPaymentModeExistAsync(string name, string typeCode)
        {
            return await paymentModeRepository.All().AnyAsync(x => x.PaymentModeName == name && x.PaymentModeId != typeCode);
        }

        #endregion

        #region Permissions

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Mode" || x.Title == "PaymentMode" || x.Title == "PaymentModes" || x.Title == "Payment Mode Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Mode" || x.Title == "PaymentMode" || x.Title == "PaymentModes" || x.Title == "Payment Mode Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Mode" || x.Title == "PaymentMode" || x.Title == "PaymentModes" || x.Title == "Payment Mode Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Payment Mode" || x.Title == "PaymentMode" || x.Title == "PaymentModes" || x.Title == "Payment Mode Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }

        #endregion

        #region Legacy Methods

        public List<SalesDefPaymentMode> GetPaymentModes()
        {
            return GetAll();
        }

        public SalesDefPaymentMode GetPaymentMode(string id)
        {
            return paymentModeRepository.GetById(id);
        }

        public SalesDefPaymentMode SavePaymentMode(SalesDefPaymentMode entity)
        {
            if (IsPaymentModeExistByCode(entity.PaymentModeId))
                Update(entity);
            else
                Add(entity);

            return entity;
        }

        public bool DeletePaymentMode(string id)
        {
            var entity = GetPaymentMode(id);
            if (entity != null)
            {
                paymentModeRepository.Delete(entity);
                return true;
            }
            return false;
        }

        public bool IsPaymentModeExistByCode(string code)
        {
            return paymentModeRepository.All().Any(x => x.PaymentModeId == code);
        }

        public bool IsPaymentModeExist(string name)
        {
            return paymentModeRepository.All().Any(x => x.PaymentModeName == name);
        }

        public bool IsPaymentModeExist(string name, string typeCode)
        {
            return paymentModeRepository.All().Any(x => x.PaymentModeName == name && x.PaymentModeId != typeCode);
        }

        public IEnumerable<CommonSelectModel> PaymentModeSelection()
        {
            return paymentModeRepository.All()
                .Select(x => new CommonSelectModel
                {
                    Code = x.PaymentModeId,
                    Name = x.PaymentModeName
                });
        }

        public IEnumerable<CommonSelectModel> PaymentModeSelectionOnlyCash()
        {
            return paymentModeRepository.All().Where(x => x.PaymentModeId == "001")
                .Select(x => new CommonSelectModel
                {
                    Code = x.PaymentModeId,
                    Name = x.PaymentModeName
                });
        }

        #endregion
    }
}
