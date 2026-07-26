using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.VendorInfo;
using GCTL.Core.ViewModels.VendorPrefix;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.VendorInfo
{
    public class VendorInfoService : AppService<CoreVendorInformation>, IVendorInfoService

    {
        #region Service & Repository
        private readonly IRepository<CoreVendorInformation> vendorInforepository;
        private readonly IRepository<SalesContactPerson> contactPersonrepository;
        private readonly IRepository<CaDefCountry> caDefCountryRepository;
        private readonly IRepository<CfDefVendorPrefix> vendorPrefixrepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Core_VendorInformation";
        private const string ColumnName = "VendorID";

        public VendorInfoService(
            IRepository<CoreVendorInformation> vendorInforepository,
            IRepository<SalesContactPerson> contactPersonrepository,
            IRepository<CaDefCountry> caDefCountryRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            IRepository<CfDefVendorPrefix> vendorPrefixrepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
            
    : base(vendorInforepository)
        {
            this.vendorInforepository = vendorInforepository;
            this.contactPersonrepository = contactPersonrepository;
            this.caDefCountryRepository = caDefCountryRepository;
            this.vendorPrefixrepository = vendorPrefixrepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<VendorInfoSetupViewModel>> GetAllAsync()
        {
            var entity = await vendorInforepository.GetAllAsync();
            return entity.Select(entityVM => new VendorInfoSetupViewModel
            {
                Tc = entityVM.Tc,
                VendorId = entityVM.VendorId,
                Name = entityVM.Name,
                Address = entityVM.Address,
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

        public async Task<VendorInfoSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await vendorInforepository.GetByIdAsync(code);

            if (entity == null)
                return null;

            return new VendorInfoSetupViewModel
            {
                Tc = entity.Tc,
                VendorId = entity.VendorId,
                VendorCode = entity.VendorCode,
                VendorPrefix = entity.VendorPrefix,
                Name = entity.Name,
                Address = entity.Address,
                CountryId = entity.CountryId,
                City = entity.City,
                StateOrProvince = entity.StateOrProvince,
                ZipCode = entity.ZipCode,
                VatRegNo = entity.VatRegNo,
                Phone = entity.Phone,
                Fax = entity.Fax,
                Email = entity.Email,
                Website = entity.Website,
                Cpid = !string.IsNullOrEmpty(entity.Cpid) ? entity.Cpid.Split(',').ToList() : new List<string>(),
                Remarks = entity.Remarks,
                CompanyCode = entity.CompanyCode,
                EmployeeId = entity.EmployeeId,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(VendorInfoSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 4);
                await vendorInforepository.BeginTransactionAsync();

                var entity = new CoreVendorInformation
                {
                    VendorId = strMaxNO,
                    VendorCode = entityVM.VendorCode ?? string.Empty,
                    VendorPrefix = entityVM.VendorPrefix ?? string.Empty,
                    Name = entityVM.Name,
                    Address = entityVM.Address ?? string.Empty,
                    CountryId = entityVM.CountryId ?? string.Empty,
                    City = entityVM.City ?? string.Empty,
                    StateOrProvince = entityVM.StateOrProvince ?? string.Empty,
                    ZipCode = entityVM.ZipCode ?? string.Empty,
                    VatRegNo = entityVM.VatRegNo ?? string.Empty,
                    Phone = entityVM.Phone ?? string.Empty,
                    Fax = entityVM.Fax ?? string.Empty,
                    Email = entityVM.Email ?? string.Empty,
                    Website = entityVM.Website ?? string.Empty,
                    Cpid = entityVM.Cpid != null ? string.Join(",", entityVM.Cpid) : "",
                    Remarks = entityVM.Remarks ?? string.Empty,
                    CompanyCode = entityVM.UserCompany ?? string.Empty,
                    EmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await vendorInforepository.AddAsync(entity);
                await vendorInforepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await vendorInforepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(VendorInfoSetupViewModel entityVM)
        {
            await vendorInforepository.BeginTransactionAsync();

            try
            {
                var entity = await vendorInforepository.GetByIdAsync(entityVM.VendorId);

                if (entity == null)
                {
                    await vendorInforepository.RollbackTransactionAsync();
                    return false;
                }

                // Don't update VendorId (Primary Key)

                entity.VendorCode = entityVM.VendorCode ?? string.Empty;
                entity.VendorPrefix = entityVM.VendorPrefix ?? string.Empty;
                entity.Name = entityVM.Name;
                entity.Address = entityVM.Address ?? string.Empty;
                entity.CountryId = entityVM.CountryId ?? string.Empty;
                entity.City = entityVM.City ?? string.Empty;
                entity.StateOrProvince = entityVM.StateOrProvince ?? string.Empty;
                entity.ZipCode = entityVM.ZipCode ?? string.Empty;
                entity.VatRegNo = entityVM.VatRegNo ?? string.Empty;
                entity.Phone = entityVM.Phone ?? string.Empty;
                entity.Fax = entityVM.Fax ?? string.Empty;
                entity.Email = entityVM.Email ?? string.Empty;
                entity.Website = entityVM.Website ?? string.Empty;
                entity.Cpid = entityVM.Cpid != null ? string.Join(",", entityVM.Cpid) : string.Empty;
                entity.Remarks = entityVM.Remarks ?? string.Empty;
                entity.CompanyCode = entityVM.UserCompany ?? string.Empty;
                entity.EmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.ModifyDate = DateTime.Now; // or Mdate if that's your column name

                await vendorInforepository.UpdateAsync(entity);
                await vendorInforepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await vendorInforepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionVendorInfoAsync()
        {

            var data = await vendorInforepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.VendorId,
                           Name = x.Name,
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
                vendorInforepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await vendorInforepository.BeginTransactionAsync();

            try
            {
                var entity = await vendorInforepository.All().Where(x => ids.Contains(x.VendorId)).ToListAsync();

                vendorInforepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await vendorInforepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await vendorInforepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await vendorInforepository.All().AnyAsync(x => x.VendorId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await vendorInforepository.All().AnyAsync(x => x.Name == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await vendorInforepository.All().AnyAsync(x => x.Name == name && x.VendorId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Info" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Info" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Info" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Vendor Info" && x.CheckDelete);
        }

        public Task UpdateAsync(VendorPrefixSetupViewModel modelVM)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(VendorPrefixSetupViewModel modelVM)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
