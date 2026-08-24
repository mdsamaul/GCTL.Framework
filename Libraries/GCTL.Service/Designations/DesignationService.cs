using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Designations;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.Designations
{
    public class DesignationService : AppService<HrmDefDesignation>, IDesignationService
    {
        #region Service & Repository
        private readonly IRepository<HrmDefDesignation> designationRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "HRM_Def_Designation";
        private const string ColumnName = "DesignationCode";

        public DesignationService(
            IRepository<HrmDefDesignation> designationRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
        )
        : base(designationRepository)
        {
            this.designationRepository = designationRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<DesignationSetupViewModel>> GetAllAsync()
        {
            var entity = await designationRepository.GetAllAsync();
            return entity.Select(entityVM => new DesignationSetupViewModel
            {
                AutoId = entityVM.AutoId,
                DesignationCode = entityVM.DesignationCode,
                DesignationName = entityVM.DesignationName,
                DesignationShortName = entityVM.DesignationShortName,
                BanglaDesignation = entityVM.BanglaDesignation,
                BanglaShortName = entityVM.BanglaShortName,
                StepNoId = entityVM.StepNoId,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,
            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<DesignationSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await designationRepository.GetByIdAsync(code);
            if (entity == null) return null;

            DesignationSetupViewModel entityVM = new DesignationSetupViewModel();
            entityVM.AutoId = entity.AutoId;
            entityVM.DesignationCode = entity.DesignationCode;
            entityVM.DesignationName = entity.DesignationName;
            entityVM.DesignationShortName = entity.DesignationShortName;
            entityVM.BanglaDesignation = entity.BanglaDesignation;
            entityVM.BanglaShortName = entity.BanglaShortName;
            entityVM.StepNoId = entity.StepNoId;
            entityVM.Luser = entity.Luser;
            entityVM.Ldate = entity.Ldate;
            entityVM.ModifyDate = entity.ModifyDate;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;

            return entityVM;
        }

        #endregion

        #region SelectionDesignationTypeAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionDesignationTypeAsync()
        {
            var data = await designationRepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.DesignationCode,
                           Name = x.DesignationName,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(DesignationSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
            await designationRepository.BeginTransactionAsync();
            try
            {
                HrmDefDesignation entity = new HrmDefDesignation();
                entity.DesignationCode = strMaxNO;
                entity.DesignationName = entityVM.DesignationName;
                entity.DesignationShortName = entityVM.DesignationShortName;
                entity.BanglaDesignation = entityVM.BanglaDesignation ?? string.Empty;
                entity.BanglaShortName = entityVM.BanglaShortName ?? string.Empty;
                entity.StepNoId = entityVM.StepNoId;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await designationRepository.AddAsync(entity);
                await designationRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await designationRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(DesignationSetupViewModel entityVM)
        {
            await designationRepository.BeginTransactionAsync();
            try
            {
                var entity = await designationRepository.GetByIdAsync(entityVM.DesignationCode);
                if (entity == null)
                {
                    await designationRepository.RollbackTransactionAsync();
                    return false;
                }
                entity.DesignationCode = entityVM.DesignationCode;
                entity.DesignationName = entityVM.DesignationName;
                entity.DesignationShortName = entityVM.DesignationShortName;
                entity.BanglaDesignation = entityVM.BanglaDesignation ?? string.Empty;
                entity.BanglaShortName = entityVM.BanglaShortName ?? string.Empty;
                entity.StepNoId = entityVM.StepNoId;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await designationRepository.UpdateAsync(entity);
                await designationRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await designationRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "DesignationId", "Designation" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                designationRepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await designationRepository.BeginTransactionAsync();

            try
            {
                var entity = await designationRepository.All().Where(x => ids.Contains(x.DesignationCode)).ToListAsync();

                designationRepository.Delete(entity);

                model.tableName = designationRepository.GetTableName();

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await designationRepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await designationRepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await designationRepository.All().AnyAsync(x => x.DesignationCode == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await designationRepository.All().AnyAsync(x => x.DesignationName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await designationRepository.All().AnyAsync(x => x.DesignationName == name && x.DesignationCode != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Designation" || x.Title == "Designations") && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Designation" || x.Title == "Designations") && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Designation" || x.Title == "Designations") && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Designation" || x.Title == "Designations") && x.CheckDelete);
        }
        #endregion

        #region Legacy Methods
        public List<HrmDefDesignation> GetDesignations()
        {
            return GetAll();
        }

        public HrmDefDesignation GetDesignation(string id)
        {
            return designationRepository.GetById(id);
        }

        public HrmDefDesignation SaveDesignation(HrmDefDesignation entity)
        {
            if (IsDesignationExistByCode(entity.DesignationCode))
                Update(entity);
            else
                Add(entity);

            return entity;
        }

        public async Task<(bool success, bool refSuccess, string message)> DeleteDesignationAsync(List<string> ids, DeleteHistoryViewModel model)
        {
            var res = await DeleteTab(ids, model);
            return (res.succses, !res.succses, res.messege);
        }

        public bool IsDesignationExistByCode(string code)
        {
            return designationRepository.All().Any(x => x.DesignationCode == code);
        }

        public bool IsDesignationExist(string name)
        {
            return designationRepository.All().Any(x => x.DesignationName == name);
        }

        public bool IsDesignationExist(string name, string typeCode)
        {
            return designationRepository.All().Any(x => x.DesignationName == name && x.DesignationCode != typeCode);
        }

        public IEnumerable<CommonSelectModel> DesignationSelection()
        {
            return designationRepository.All()
                .Select(x => new CommonSelectModel
                {
                    Code = x.DesignationCode,
                    Name = x.DesignationName
                });
        }

        public bool SavePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Designation" || x.Title == "Designations") && x.CheckAdd);
        }

        public bool UpdatePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Designation" || x.Title == "Designations") && x.CheckEdit);
        }

        public bool DeletePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Designation" || x.Title == "Designations") && x.CheckDelete);
        }
        #endregion
    }
}
