using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ImporterInfo;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.ImporterInfo
{
    public class ImporterInfoService : AppService<CoreImporterInformation>, IImporterInfoService
    {
        #region Service & Repository
        private readonly IRepository<CoreImporterInformation> importerInforepository;
        private readonly IRepository<CaDefCountry> caDefCountryRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Core_ImporterInformation";
        private const string ColumnName = "ImporterID";

        public ImporterInfoService(
            IRepository<CoreImporterInformation> importerInforepository,
            IRepository<CaDefCountry> caDefCountryRepository,
             IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
    : base(importerInforepository)
        {
            this.importerInforepository = importerInforepository;
            this.caDefCountryRepository = caDefCountryRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<ImporterInfoSetupViewModel>> GetAllAsync()
        {
            var entity = await importerInforepository.GetAllAsync();
            return entity.Select(entityVM => new ImporterInfoSetupViewModel
            {
                Tc = entityVM.Tc,
                ImporterId = entityVM.ImporterId,
                ImporterName = entityVM.ImporterName,
                ImporterAddress = entityVM.ImporterAddress,
                Phone = entityVM.Phone,
                Website = entityVM.Website,
                Email = entityVM.Email,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,

            }).ToList();
        }


        #endregion

        #region GetByIdAsync

        public async Task<ImporterInfoSetupViewModel> GetByIdAsync(string code)
        {
            try
            {
                var entity = await importerInforepository.GetByIdAsync(code);

                if (entity == null)
                    return null;

                return new ImporterInfoSetupViewModel
                {
                    Tc = entity.Tc,
                    ImporterId = entity.ImporterId,
                    ImporterCode = entity.ImporterCode,
                    ImporterName = entity.ImporterName,
                    ImporterAddress = entity.ImporterAddress,
                    CountryId = entity.CountryId,
                    City = entity.City,
                    StateOrProvince = entity.StateOrProvince,
                    ZipCode = entity.ZipCode,
                    VatRegNo = entity.VatRegNo,
                    Phone = entity.Phone,
                    Email = entity.Email,
                    Fax = entity.Fax,
                    Website = entity.Website,
                    Remarks = entity.Remarks,
                    ContactPerson1 = entity.ContactPerson1,
                    ContactPerson2 = entity.ContactPerson2,
                    Designation1 = entity.Designation1,
                    Designation2 = entity.Designation2,
                    Phone1 = entity.Phone1,
                    Phone2 = entity.Phone2,
                    Email1 = entity.Email1,
                    Email2 = entity.Email2,
                    CompanyCode = entity.CompanyCode,
                    EmployeeId = entity.EmployeeId,
                    Ldate = entity.Ldate,
                    ModifyDate = entity.ModifyDate,
                    Luser = entity.Luser,
                    Lip = entity.Lip,
                    Lmac = entity.Lmac
                };
            }
            catch (Exception ex)
            {
                return null;

            }
        }


        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(ImporterInfoSetupViewModel entityVM)
        {
            try
            {
                //commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName );
                //var nextCode = commonService.GenerateCode("ImporterID", "Core_ImporterInformation", "IMP_", 6);

                await importerInforepository.BeginTransactionAsync();

                var entity = new CoreImporterInformation
                {
                    ImporterId = entityVM.ImporterId,
                    ImporterCode = entityVM.ImporterCode ?? string.Empty,
                    ImporterName = entityVM.ImporterName,
                    ImporterAddress = entityVM.ImporterAddress ?? string.Empty,
                    CountryId = entityVM.CountryId ?? string.Empty,
                    City = entityVM.City ?? string.Empty,
                    VatRegNo = entityVM.VatRegNo ?? string.Empty,
                    Phone = entityVM.Phone ?? string.Empty,
                    Fax = entityVM.Fax ?? string.Empty,
                    Email = entityVM.Email ?? string.Empty,
                    Website = entityVM.Website ?? string.Empty,
                    Remarks = entityVM.Remarks ?? string.Empty,
                    ContactPerson1 = entityVM.ContactPerson1 ?? string.Empty,
                    ContactPerson2 = entityVM.ContactPerson2 ?? string.Empty,
                    Designation1 = entityVM.Designation1 ?? string.Empty,
                    Designation2 = entityVM.Designation2 ?? string.Empty,
                    Phone1 = entityVM.Phone1 ?? string.Empty,
                    Phone2 = entityVM.Phone2 ?? string.Empty,
                    Email1 = entityVM.Email1 ?? string.Empty,
                    Email2 = entityVM.Email2 ?? string.Empty,
                    CompanyCode = entityVM.UserCompany ?? string.Empty,
                    EmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await importerInforepository.AddAsync(entity);
                await importerInforepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await importerInforepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(ImporterInfoSetupViewModel entityVM)
        {
            await importerInforepository.BeginTransactionAsync();

            try
            {
                var entity = await importerInforepository.GetByIdAsync(entityVM.ImporterId);

                if (entity == null)
                {
                    await importerInforepository.RollbackTransactionAsync();
                    return false;
                }

                // Do not update primary key
                // entity.ImporterId = entityVM.ImporterId;

                entity.ImporterCode = entityVM.ImporterCode ?? string.Empty;
                entity.ImporterName = entityVM.ImporterName ?? string.Empty;
                entity.ImporterAddress = entityVM.ImporterAddress ?? string.Empty;
                entity.CountryId = entityVM.CountryId ?? string.Empty;
                entity.City = entityVM.City ?? string.Empty;
                entity.VatRegNo = entityVM.VatRegNo ?? string.Empty;
                entity.Phone = entityVM.Phone ?? string.Empty;
                entity.Fax = entityVM.Fax ?? string.Empty;
                entity.Email = entityVM.Email ?? string.Empty;
                entity.Website = entityVM.Website ?? string.Empty;
                entity.Remarks = entityVM.Remarks ?? string.Empty;

                entity.ContactPerson1 = entityVM.ContactPerson1 ?? string.Empty;
                entity.ContactPerson2 = entityVM.ContactPerson2 ?? string.Empty;
                entity.Designation1 = entityVM.Designation1 ?? string.Empty;
                entity.Designation2 = entityVM.Designation2 ?? string.Empty;

                entity.Phone1 = entityVM.Phone1 ?? string.Empty;
                entity.Phone2 = entityVM.Phone2 ?? string.Empty;
                entity.Email1 = entityVM.Email1 ?? string.Empty;
                entity.Email2 = entityVM.Email2 ?? string.Empty;

                entity.CompanyCode = entityVM.UserCompany ?? string.Empty;
                entity.EmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty;

                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.ModifyDate = DateTime.Now;

                await importerInforepository.UpdateAsync(entity);
                await importerInforepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await importerInforepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionImporterInfoAsync()
        {

            var data = await importerInforepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.ImporterId,
                           Name = x.ImporterName,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                importerInforepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await importerInforepository.BeginTransactionAsync();

            try
            {
                var entity = await importerInforepository.All().Where(x => ids.Contains(x.ImporterId)).ToListAsync();

                importerInforepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await importerInforepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await importerInforepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await importerInforepository.All().AnyAsync(x => x.ImporterId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await importerInforepository.All().AnyAsync(x => x.ImporterName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await importerInforepository.All().AnyAsync(x => x.ImporterName == name && x.ImporterId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Importer Info" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Importer Info" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Importer Info" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Importer Info" && x.CheckDelete);
        }

        #endregion

        #region Next Id Substring

        public Task<string> Autoid()
        {
            var appList = importerInforepository.All().OrderByDescending(x => x.Tc).FirstOrDefault();

            int newId = 1;
            if (appList != null)
            {
                string lastId = appList.ImporterId;
                string numericPart = lastId.Substring(4);
                if (int.TryParse(numericPart, out int parsedId))
                {
                    newId = parsedId + 1;
                }
            }

            string formattedId = $"IMP_{newId:D6}";

            return Task.FromResult(formattedId);
        }

        #endregion

    }
}
