using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Gender;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.Gender
{
    public class GenderService : AppService<HrmDefSex>, IGenderService
    {
        #region Service & Repository
        private readonly IRepository<HrmDefSex> sexrepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "HRM_Def_Sex";
        private const string ColumnName = "SexCode";
        public GenderService(IRepository<HrmDefSex> sexrepository, 
            IRepository<
            CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
            )
        : base(sexrepository)
        {
            this.sexrepository = sexrepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<GenderSetupViewModel>> GetAllAsync()
        {
            var entity = await sexrepository.GetAllAsync();
            return entity.Select(entityVM => new GenderSetupViewModel
            {
                AutoId = entityVM.AutoId,
                SexCode = entityVM.SexCode,
                Sex = entityVM.Sex,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,
            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<GenderSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await sexrepository.GetByIdAsync(code);
            if (entity == null) return null;

            GenderSetupViewModel entityVM = new GenderSetupViewModel();
            entityVM.AutoId = entity.AutoId;
            entityVM.SexCode = entity.SexCode;
            entityVM.Sex = entity.Sex;
            entityVM.Luser = entity.Luser;
            entityVM.Ldate = entity.Ldate;
            entityVM.ModifyDate = entity.ModifyDate;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;

            return entityVM;
        }

        #endregion

        #region SelectionGenderTypeAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionGenderTypeAsync()
        {

            var data = await sexrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.SexCode,
                           Name = x.Sex,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(GenderSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 1);
            await sexrepository.BeginTransactionAsync();
            try
            {

                HrmDefSex entity = new HrmDefSex();
                entity.SexCode = strMaxNO;
                entity.Sex = entityVM.Sex;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await sexrepository.AddAsync(entity);
                await sexrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await sexrepository.RollbackTransactionAsync();

                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(GenderSetupViewModel entityVM)
        {
            await sexrepository.BeginTransactionAsync();
            try
            {

                var entity = await sexrepository.GetByIdAsync(entityVM.SexCode);
                if (entity == null)
                {
                    await sexrepository.RollbackTransactionAsync();
                    return false;
                }
                entity.SexCode = entityVM.SexCode;
                entity.Sex = entityVM.Sex;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await sexrepository.UpdateAsync(entity);
                await sexrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await sexrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region Delete

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                sexrepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await sexrepository.BeginTransactionAsync();

            try
            {
                var entity = await sexrepository.All().Where(x => ids.Contains(x.SexCode)).ToListAsync();


                sexrepository.Delete(entity);

                model.tableName = sexrepository.GetTableName();

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                    );

                await sexrepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await sexrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await sexrepository.All().AnyAsync(x => x.SexCode == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await sexrepository.All().AnyAsync(x => x.Sex == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await sexrepository.All().AnyAsync(x => x.Sex == name && x.SexCode != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Gender" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Gender" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Gender" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Gender" && x.CheckDelete);
        }
        #endregion

    }
}
