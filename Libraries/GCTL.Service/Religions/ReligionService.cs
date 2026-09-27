using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Religions;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.Religions
{
    public class ReligionService : AppService<HrmDefReligion>, IReligionService
    {
        #region Service & Repository
        private readonly IRepository<HrmDefReligion> religionRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "HRM_Def_Religion";
        private const string ColumnName = "ReligionCode";

        public ReligionService(
            IRepository<HrmDefReligion> religionRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
        )
        : base(religionRepository)
        {
            this.religionRepository = religionRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<ReligionSetupViewModel>> GetAllAsync()
        {
            var entity = await religionRepository.GetAllAsync();
            return entity.Select(entityVM => new ReligionSetupViewModel
            {
                AutoId = entityVM.AutoId,
                ReligionCode = entityVM.ReligionCode,
                Religion = entityVM.Religion,
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

        public async Task<ReligionSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await religionRepository.GetByIdAsync(code);
            if (entity == null) return null;

            ReligionSetupViewModel entityVM = new ReligionSetupViewModel();
            entityVM.AutoId = entity.AutoId;
            entityVM.ReligionCode = entity.ReligionCode;
            entityVM.Religion = entity.Religion;
            entityVM.ShortName = entity.ShortName;
            entityVM.Luser = entity.Luser;
            entityVM.Ldate = entity.Ldate;
            entityVM.ModifyDate = entity.ModifyDate;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;

            return entityVM;
        }

        #endregion

        #region SelectionReligionTypeAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionReligionTypeAsync()
        {
            var data = await religionRepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.ReligionCode,
                           Name = x.Religion,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(ReligionSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 1);
            await religionRepository.BeginTransactionAsync();
            try
            {
                HrmDefReligion entity = new HrmDefReligion();
                entity.ReligionCode = strMaxNO;
                entity.Religion = entityVM.Religion;
                entity.ShortName = entityVM.ShortName ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await religionRepository.AddAsync(entity);
                await religionRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await religionRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(ReligionSetupViewModel entityVM)
        {
            await religionRepository.BeginTransactionAsync();
            try
            {
                var entity = await religionRepository.GetByIdAsync(entityVM.ReligionCode);
                if (entity == null)
                {
                    await religionRepository.RollbackTransactionAsync();
                    return false;
                }
                entity.ReligionCode = entityVM.ReligionCode;
                entity.Religion = entityVM.Religion;
                entity.ShortName = entityVM.ShortName ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await religionRepository.UpdateAsync(entity);
                await religionRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await religionRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "ReligionId", "Religion" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                religionRepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await religionRepository.BeginTransactionAsync();

            try
            {
                var entity = await religionRepository.All().Where(x => ids.Contains(x.ReligionCode)).ToListAsync();

                religionRepository.Delete(entity);

                model.tableName = religionRepository.GetTableName();

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await religionRepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await religionRepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await religionRepository.All().AnyAsync(x => x.ReligionCode == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await religionRepository.All().AnyAsync(x => x.Religion == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await religionRepository.All().AnyAsync(x => x.Religion == name && x.ReligionCode != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Religion" || x.Title == "Religions") && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Religion" || x.Title == "Religions") && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Religion" || x.Title == "Religions") && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Religion" || x.Title == "Religions") && x.CheckDelete);
        }
        #endregion

        #region Legacy Methods
        public List<HrmDefReligion> GetReligions()
        {
            return GetAll();
        }

        public HrmDefReligion GetReligion(string id)
        {
            return religionRepository.GetById(id);
        }

        public HrmDefReligion SaveReligion(HrmDefReligion entity)
        {
            if (IsReligionExistByCode(entity.ReligionCode))
                Update(entity);
            else
                Add(entity);

            return entity;
        }

        public async Task<(bool success, bool refSuccess, string message)> DeleteReligion(List<string> ids, DeleteHistoryViewModel model)
        {
            var res = await DeleteTab(ids, model);
            return (res.succses, !res.succses, res.messege);
        }

        public bool IsReligionExistByCode(string code)
        {
            return religionRepository.All().Any(x => x.ReligionCode == code);
        }

        public bool IsReligionExist(string name)
        {
            return religionRepository.All().Any(x => x.Religion == name);
        }

        public bool IsReligionExist(string name, string typeCode)
        {
            return religionRepository.All().Any(x => x.Religion == name && x.ReligionCode != typeCode);
        }

        public IEnumerable<CommonSelectModel> ReligionSelection()
        {
            return religionRepository.All()
                .Select(x => new CommonSelectModel
                {
                    Code = x.ReligionCode,
                    Name = x.Religion
                });
        }

        public bool SavePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Religion" || x.Title == "Religions") && x.CheckAdd);
        }

        public bool UpdatePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Religion" || x.Title == "Religions") && x.CheckEdit);
        }

        public bool DeletePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Religion" || x.Title == "Religions") && x.CheckDelete);
        }
        #endregion
    }
}
