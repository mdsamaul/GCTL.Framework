using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.BloodGroups;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.BloodGroups
{
    public class BloodGroupService : AppService<HrmDefBloodGroup>, IBloodGroupService
    {
        #region Service & Repository
        private readonly IRepository<HrmDefBloodGroup> bloodGroupRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "HRM_Def_BloodGroup";
        private const string ColumnName = "BloodGroupCode";

        public BloodGroupService(
            IRepository<HrmDefBloodGroup> bloodGroupRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
        )
        : base(bloodGroupRepository)
        {
            this.bloodGroupRepository = bloodGroupRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<BloodGroupSetupViewModel>> GetAllAsync()
        {
            var entity = await bloodGroupRepository.GetAllAsync();
            return entity.Select(entityVM => new BloodGroupSetupViewModel
            {
                AutoId = entityVM.AutoId,
                Tc = (int)entityVM.AutoId,
                BloodGroupCode = entityVM.BloodGroupCode,
                BloodGroup = entityVM.BloodGroup,
                BanglaBloodGroup = entityVM.BanglaBloodGroup,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,
            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<BloodGroupSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await bloodGroupRepository.GetByIdAsync(code);
            if (entity == null) return null;

            BloodGroupSetupViewModel entityVM = new BloodGroupSetupViewModel();
            entityVM.AutoId = entity.AutoId;
            entityVM.Tc = (int)entity.AutoId;
            entityVM.BloodGroupCode = entity.BloodGroupCode;
            entityVM.BloodGroup = entity.BloodGroup;
            entityVM.BanglaBloodGroup = entity.BanglaBloodGroup;
            entityVM.Luser = entity.Luser;
            entityVM.Ldate = entity.Ldate;
            entityVM.ModifyDate = entity.ModifyDate;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;

            return entityVM;
        }

        #endregion

        #region SelectionBloodGroupTypeAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionBloodGroupTypeAsync()
        {
            var data = await bloodGroupRepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.BloodGroupCode,
                           Name = x.BloodGroup,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(BloodGroupSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 2);
            await bloodGroupRepository.BeginTransactionAsync();
            try
            {
                HrmDefBloodGroup entity = new HrmDefBloodGroup();
                entity.BloodGroupCode = strMaxNO;
                entity.BloodGroup = entityVM.BloodGroup;
                entity.BanglaBloodGroup = entityVM.BanglaBloodGroup ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await bloodGroupRepository.AddAsync(entity);
                await bloodGroupRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await bloodGroupRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(BloodGroupSetupViewModel entityVM)
        {
            await bloodGroupRepository.BeginTransactionAsync();
            try
            {
                var entity = await bloodGroupRepository.GetByIdAsync(entityVM.BloodGroupCode);
                if (entity == null)
                {
                    await bloodGroupRepository.RollbackTransactionAsync();
                    return false;
                }
                entity.BloodGroupCode = entityVM.BloodGroupCode;
                entity.BloodGroup = entityVM.BloodGroup;
                entity.BanglaBloodGroup = entityVM.BanglaBloodGroup ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await bloodGroupRepository.UpdateAsync(entity);
                await bloodGroupRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await bloodGroupRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "BloodGroupId", "BloodGroup" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                bloodGroupRepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await bloodGroupRepository.BeginTransactionAsync();

            try
            {
                var entity = await bloodGroupRepository.All().Where(x => ids.Contains(x.BloodGroupCode)).ToListAsync();

                bloodGroupRepository.Delete(entity);

                model.tableName = bloodGroupRepository.GetTableName();

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await bloodGroupRepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await bloodGroupRepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await bloodGroupRepository.All().AnyAsync(x => x.BloodGroupCode == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await bloodGroupRepository.All().AnyAsync(x => x.BloodGroup == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await bloodGroupRepository.All().AnyAsync(x => x.BloodGroup == name && x.BloodGroupCode != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Blood Group" || x.Title == "BloodGroups" || x.Title == "Blood Group "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Blood Group" || x.Title == "BloodGroups" || x.Title == "Blood Group "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Blood Group" || x.Title == "BloodGroups" || x.Title == "Blood Group "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Blood Group" || x.Title == "BloodGroups" || x.Title == "Blood Group "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }
        #endregion

        #region Legacy Methods
        public List<HrmDefBloodGroup> GetBloodGroups()
        {
            return GetAll();
        }

        public HrmDefBloodGroup GetBloodGroup(string id)
        {
            return bloodGroupRepository.GetById(id);
        }

        public HrmDefBloodGroup GetBloodGroup(decimal id)
        {
            return bloodGroupRepository.All().FirstOrDefault(x => x.AutoId == id);
        }

        public HrmDefBloodGroup SaveBloodGroup(HrmDefBloodGroup entity)
        {
            if (entity.AutoId > 0)
                Update(entity);
            else
                Add(entity);

            return entity;
        }

        public async Task<(bool success, bool refSuccess, string message)> DeleteBloodGroup(List<string> ids, DeleteHistoryViewModel model)
        {
            var res = await DeleteTab(ids, model);
            return (res.succses, !res.succses, res.messege);
        }

        public bool IsBloodGroupExistByCode(string code)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroupCode == code);
        }

        public bool IsBloodGroupExist(string name)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroup == name);
        }

        public bool IsBloodGroupExist(string name, string typeCode)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroup == name && x.BloodGroupCode != typeCode);
        }

        public bool IsBloodGroupExist(string name, decimal autoId)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroup == name && x.AutoId != autoId);
        }

        public IEnumerable<CommonSelectModel> BloodGroupSelection()
        {
            return bloodGroupRepository.All()
                .Select(x => new CommonSelectModel
                {
                    Code = x.BloodGroupCode,
                    Name = x.BloodGroup
                });
        }

        public bool SavePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Blood Group" || x.Title == "BloodGroups") && x.CheckAdd);
        }

        public bool UpdatePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Blood Group" || x.Title == "BloodGroups") && x.CheckEdit);
        }

        public bool DeletePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Blood Group" || x.Title == "BloodGroups") && x.CheckDelete);
        }
        #endregion
    }
}
