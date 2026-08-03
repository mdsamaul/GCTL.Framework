using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ExporterInfo;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.ExporterInfo
{
    public class ExporterInfoService : AppService<CoreExporterInformation>, IExporterInfoService
    {
        #region Service & Repository
        private readonly IRepository<CoreExporterInformation> exporterInforepository;
        private readonly IRepository<CaDefCountry> caDefCountryRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Core_ExporterInformation";
        private const string ColumnName = "ExporterID";

        public ExporterInfoService(
            IRepository<CoreExporterInformation> exporterInforepository,
            IRepository<CaDefCountry> caDefCountryRepository,
             IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
            
    : base(exporterInforepository)
        {
            this.exporterInforepository = exporterInforepository;
            this.caDefCountryRepository = caDefCountryRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<ExporterInfoSetupViewModel>> GetAllAsync()
        {
            var entity = await exporterInforepository.GetAllAsync();
            return entity.Select(entityVM => new ExporterInfoSetupViewModel
            {
                Tc = entityVM.Tc,
                ExporterId = entityVM.ExporterId,
                ExporterName = entityVM.ExporterName,
                ExporterAddress = entityVM.ExporterAddress,
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

        public async Task<ExporterInfoSetupViewModel> GetByIdAsync(string code)
        {
            try
            {
                var entity = await exporterInforepository.GetByIdAsync(code);

                if (entity == null)
                    return null;

                return new ExporterInfoSetupViewModel
                {
                    Tc = entity.Tc,
                    ExporterId = entity.ExporterId,
                    ExporterCode = entity.ExporterCode,
                    ExporterName = entity.ExporterName,
                    ExporterAddress = entity.ExporterAddress,
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

        public async Task<bool> SaveAsync(ExporterInfoSetupViewModel entityVM)
        {
            try
            {
                await exporterInforepository.BeginTransactionAsync();

                var entity = new CoreExporterInformation
                {
                    ExporterId = entityVM.ExporterId,
                    ExporterCode = entityVM.ExporterCode ?? string.Empty,
                    ExporterName = entityVM.ExporterName, 
                    ExporterAddress = entityVM.ExporterAddress ?? string.Empty,
                    CountryId = entityVM.CountryId ?? string.Empty,
                    City = entityVM.City ?? string.Empty,
                    StateOrProvince = entityVM.StateOrProvince ?? string.Empty,
                    ZipCode = entityVM.ZipCode ?? string.Empty,
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

                await exporterInforepository.AddAsync(entity);
                await exporterInforepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await exporterInforepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(ExporterInfoSetupViewModel entityVM)
        {
            await exporterInforepository.BeginTransactionAsync();

            try
            {
                var entity = await exporterInforepository.GetByIdAsync(entityVM.ExporterId);

                if (entity == null)
                {
                    await exporterInforepository.RollbackTransactionAsync();
                    return false;
                }

                entity.ExporterCode = entityVM.ExporterCode ?? string.Empty;
                entity.ExporterName = entityVM.ExporterName ?? string.Empty;
                entity.ExporterAddress = entityVM.ExporterAddress ?? string.Empty;
                entity.CountryId = entityVM.CountryId ?? string.Empty;
                entity.City = entityVM.City ?? string.Empty;
                entity.StateOrProvince = entityVM.StateOrProvince ?? string.Empty;
                entity.ZipCode = entityVM.ZipCode ?? string.Empty;
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

                await exporterInforepository.UpdateAsync(entity);
                await exporterInforepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await exporterInforepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionExporterInfoAsync()
        {

            var data = await exporterInforepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.ExporterId,
                           Name = x.ExporterName,
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
                exporterInforepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await exporterInforepository.BeginTransactionAsync();

            try
            {
                var entity = await exporterInforepository.All().Where(x => ids.Contains(x.ExporterId)).ToListAsync();

                exporterInforepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await exporterInforepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await exporterInforepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await exporterInforepository.All().AnyAsync(x => x.ExporterId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await exporterInforepository.All().AnyAsync(x => x.ExporterName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await exporterInforepository.All().AnyAsync(x => x.ExporterName == name && x.ExporterId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Exporter Info" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Exporter Info" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Exporter Info" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Exporter Info" && x.CheckDelete);
        }

        #endregion

        #region Next Id Substring

        public Task<string> Autoid()
        {
            var appList = exporterInforepository.All().OrderByDescending(x => x.Tc).FirstOrDefault();

            int newId = 1;
            if (appList != null)
            {
                string lastId = appList.ExporterId;
                string numericPart = lastId.Substring(4);
                if (int.TryParse(numericPart, out int parsedId))
                {
                    newId = parsedId + 1;
                }
            }

            string formattedId = $"EXP_{newId:D6}";

            return Task.FromResult(formattedId);
        }

        #endregion

    }
}
