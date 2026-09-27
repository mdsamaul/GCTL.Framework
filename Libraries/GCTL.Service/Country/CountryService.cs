using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.Country;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.Country
{
    public class CountryService : AppService<CaDefCountry>, ICountryService
    {
        #region Service & Repository
        private readonly IRepository<CaDefCountry> countryrepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;

        private const string TableName = "CA_Def_Country";
        private const string ColumnName = "CountryId";
        public CountryService(
            IRepository<CaDefCountry> countryrepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
    : base(countryrepository)
        {
            this.countryrepository = countryrepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<CountrySetuoViewModel>> GetAllAsync()
        {
            var entity = await countryrepository.GetAllAsync();
            return entity.Select(entityVM => new CountrySetuoViewModel
            {
                Tc = (int)entityVM.Tc,
                CountryId = entityVM.CountryId,
                CountryName = entityVM.CountryName,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,
            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<CountrySetuoViewModel> GetByIdAsync(string code)
        {
            var entity = await countryrepository.GetByIdAsync(code);
            if (entity == null) return null;

            CountrySetuoViewModel entityVM = new CountrySetuoViewModel();
            entityVM.Tc = (int)entity.Tc;
            entityVM.CountryId = entity.CountryId;
            entityVM.CountryName = entity.CountryName;
            entityVM.Luser = entity.Luser;
            entityVM.Ldate = entity.Ldate;
            entityVM.ModifyDate = entity.ModifyDate;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;

            return entityVM;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(CountrySetuoViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
            await countryrepository.BeginTransactionAsync();
            try
            {
                CaDefCountry entity = new CaDefCountry
                {
                    CountryId = strMaxNO,
                    CountryName = entityVM.CountryName,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now,
                };

               
                await countryrepository.AddAsync(entity);
                await countryrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await countryrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync
        public async Task<bool> UpdateAsync(CountrySetuoViewModel entityVM)
        {
            await countryrepository.BeginTransactionAsync();
            try
            {
                var entity = await countryrepository.GetByIdAsync(entityVM.CountryId);
                if (entity == null)
                {
                    await countryrepository.RollbackTransactionAsync();
                    return false;
                }
                entity.CountryId = entityVM.CountryId;
                entity.CountryName = entityVM.CountryName;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await countryrepository.UpdateAsync(entity);
                await countryrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await countryrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionCountryAsync()
        {

            var data = await countryrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.CountryId,
                           Name = x.CountryName,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "CountryId", "Country" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                countryrepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await countryrepository.BeginTransactionAsync();

            try
            {
                var entity = await countryrepository.All().Where(x => ids.Contains(x.CountryId)).ToListAsync();

                countryrepository.Delete(entity);
                model.tableName = countryrepository.GetTableName();
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await countryrepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await countryrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await countryrepository.All().AnyAsync(x => x.CountryId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await countryrepository.All().AnyAsync(x => x.CountryName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await countryrepository.All().AnyAsync(x => x.CountryName == name && x.CountryId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Country" || x.Title == "Countries" || x.Title == "Country "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Country" || x.Title == "Countries" || x.Title == "Country "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Country" || x.Title == "Countries" || x.Title == "Country "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Country" || x.Title == "Countries" || x.Title == "Country "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }
        #endregion
    }
}
