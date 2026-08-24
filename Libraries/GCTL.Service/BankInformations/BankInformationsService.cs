using GCTL.Core.Data;
using GCTL.Core.ViewModels.BankInformations;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.BankInformations
{
    public class BankInformationsService : AppService<SalesDefBankInfo>, IBankInformationsService
    {
        private readonly IRepository<SalesDefBankInfo> repository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        private const string TableName = "SALES_Def_BankInfo";
        private const string ColumnName = "BankId";

        public BankInformationsService(
            IRepository<SalesDefBankInfo> repository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
        ) : base(repository)
        {
            this.repository = repository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #region GetAllById
        public async Task<BankInformationsSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await repository.GetByIdAsync(code);
            if (entity == null)
            {
                return null;
            }
            return new BankInformationsSetupViewModel
            {
                AutoId = entity.AutoId,
                Tc = (int)entity.AutoId,
                BankId = entity.BankId,
                BankName = entity.BankName,
                ShortName = entity.ShortName,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        public async Task<List<BankInformationsSetupViewModel>> GetAllAsync()
        {
            var entity = await repository.GetAllAsync();
            return entity.Select(entityVM => new BankInformationsSetupViewModel
            {
                AutoId = entityVM.AutoId,
                Tc = (int)entityVM.AutoId,
                BankId = entityVM.BankId,
                BankName = entityVM.BankName,
                ShortName = entityVM.ShortName,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac
            }).ToList();
        }

        #endregion

        #region Save Data Update
        public async Task<bool> SaveAsync(BankInformationsSetupViewModel entityVM)
        {
            await repository.BeginTransactionAsync();
            try
            {
                SalesDefBankInfo entity = new SalesDefBankInfo();
                entity.BankId = await GenerateNextCode();
                entity.BankName = entityVM.BankName;
                entity.ShortName = entityVM.ShortName ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await repository.AddAsync(entity);
                await repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                await repository.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> UpdateAsync(BankInformationsSetupViewModel entityVM)
        {
            await repository.BeginTransactionAsync();
            try
            {
                var entity = await repository.GetByIdAsync(entityVM.BankId);
                if (entity == null)
                {
                    await repository.RollbackTransactionAsync();
                    return false;
                }
                entity.BankId = entityVM.BankId;
                entity.BankName = entityVM.BankName;
                entity.ShortName = entityVM.ShortName ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await repository.UpdateAsync(entity);
                await repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                await repository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region Delete
        public bool DeleteBank(string id)
        {
            var entity = GetBankById(id);
            if (entity != null)
            {
                repository.Delete(entity);
                return true;
            }
            return false;
        }

        public SalesDefBankInfo GetBankById(string code)
        {
            return repository.GetById(code);
        }

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "BankId", "BankName", "Bank" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                repository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await repository.BeginTransactionAsync();

            try
            {
                var entity = await repository.All().Where(x => ids.Contains(x.BankId)).ToListAsync();

                repository.Delete(entity);
                model.tableName = repository.GetTableName();
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await repository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await repository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Drop Down
        public IEnumerable<CommonSelectModel> BankDropSelectionAsync()
        {
            return repository.All()
                      .Select(x => new CommonSelectModel
                      {
                          Code = x.BankId,
                          Name = x.BankName
                      });
        }

        #endregion

        #region Next Code
        public async Task<string> GenerateNextCode()
        {
            var code = await repository.GetAllAsync();
            var lastCode = code.Max(x => x.BankId);
            int nextCode = 1;
            if (!string.IsNullOrEmpty(lastCode))
            {
                int lastNumber = int.Parse(lastCode.TrimStart('0'));
                lastNumber++;
                nextCode = lastNumber;
            }
            return nextCode.ToString("D2");
        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await repository.All().AnyAsync(x => x.BankId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await repository.All().AnyAsync(x => x.BankName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await repository.All().AnyAsync(x => x.BankName == name && x.BankId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await repository.All()
                .Where(x => false)
                .ToListAsync(); // dummy to get list
            var accessList = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information " || x.Title == "Bank"))
                .ToListAsync();

            if (!accessList.Any()) return true;
            return accessList.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var accessList = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information " || x.Title == "Bank"))
                .ToListAsync();

            if (!accessList.Any()) return true;
            return accessList.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var accessList = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information " || x.Title == "Bank"))
                .ToListAsync();

            if (!accessList.Any()) return true;
            return accessList.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var accessList = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information " || x.Title == "Bank"))
                .ToListAsync();

            if (!accessList.Any()) return true;
            return accessList.Any(x => x.CheckDelete);
        }
        #endregion
    }
}
