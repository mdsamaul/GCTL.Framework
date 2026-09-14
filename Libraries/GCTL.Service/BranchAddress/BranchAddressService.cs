using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.BranchAddress;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SupplierInformation;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.BranchAddress
{
    public class BranchAddressService : AppService<VendorBranchInfo>, IBranchAddressService
    {
        #region Service & Repository
        private readonly IRepository<VendorBranchInfo> branchAddressrepository;
        private readonly IRepository<CoreVendorInformation> coreVendorinformationrepository;
        private readonly IRepository<SalesContactPerson> contactPersonrepository;
        private readonly IRepository<CaDefCountry> caDefCountryRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;
        private readonly IRepository<HrmDefDesignation> desiRepo;
        string strMaxNO = string.Empty;
        private const string TableName = "VendorBranchInfo";
        private const string ColumnName = "VendorBranchID";

        public BranchAddressService(
            IRepository<VendorBranchInfo> branchAddressrepository,
            IRepository<CoreVendorInformation> coreVendorinformationrepository,
            IRepository<SalesContactPerson> contactPersonrepository,
            IRepository<CaDefCountry> caDefCountryRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService,
            IRepository<HrmDefDesignation> desiRepo

            ) 
            
    : base(branchAddressrepository)
        {
            this.branchAddressrepository = branchAddressrepository;
            this.coreVendorinformationrepository = coreVendorinformationrepository;
            this.contactPersonrepository = contactPersonrepository;
            this.caDefCountryRepository = caDefCountryRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
            this.desiRepo = desiRepo;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<BranchAddressSetupViewModel>> GetAllAsync()
        {
            var branchList = await branchAddressrepository.All().AsNoTracking()
     .Select(sup => new
     {
         sup.Tc,
         sup.VendorBranchId,
         sup.VendorId,
         sup.LocationAddress,
         sup.Phone,
         sup.Luser,
         sup.Ldate,
         sup.ContactPerson
     })
     .ToListAsync();

            var vendors = await coreVendorinformationrepository.All().AsNoTracking().ToListAsync();
            var contacts = await contactPersonrepository.All().AsNoTracking().ToListAsync();

            var data = branchList.Select(sup =>
            {
                var vendor = vendors.FirstOrDefault(v => v.VendorId == sup.VendorId);
                var ids = !string.IsNullOrEmpty(sup.ContactPerson)
                    ? sup.ContactPerson.Split(',').Select(x => x.Trim()).ToList()
                    : new List<string>();

                var matchedNames = contacts
                    .Where(c => ids.Contains(c.Cpid))
                    .Select(c => c.ContactPersonName)
                    .ToList();

                return new BranchAddressSetupViewModel
                {
                    Tc = sup.Tc,
                    VendorBranchId = sup.VendorBranchId,
                    VendorId = sup.VendorId,
                    Name = vendor?.Name,
                    VendorName = vendor?.Name,
                    LocationAddress = sup.LocationAddress,
                    ContactPerson = matchedNames,
                    ContactPersonName = string.Join(", ", matchedNames),
                    Phone = sup.Phone,
                    Luser = sup.Luser,
                    Ldate = sup.Ldate
                };
            }).ToList();

            return data;
        }

        #endregion

        #region GetByIdAsync

        public async Task<BranchAddressSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await branchAddressrepository.GetByIdAsync(code);

            if (entity == null)
                return null;

            return new BranchAddressSetupViewModel
            {
                Tc = entity.Tc,
                VendorBranchId = entity.VendorBranchId,
               VendorId = entity.VendorId,
               LocationAddress = entity.LocationAddress,
               CountryId = entity.CountryId ?? string.Empty,
               City = entity.City ?? string.Empty,
               StateOrProvince = entity.StateOrProvince ?? string.Empty,
               ZipCode = entity.ZipCode ?? string.Empty,
               Phone = entity.Phone ?? string.Empty,
               Email = entity.Email ?? string.Empty,
                ContactPerson = !string.IsNullOrEmpty(entity.ContactPerson) ? entity.ContactPerson.Split(',').ToList() : new List<string>(),
                Remarks = entity.Remarks,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(BranchAddressSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 5);
                await branchAddressrepository.BeginTransactionAsync();

                var entity = new VendorBranchInfo
                {
                    VendorBranchId = strMaxNO,
                    VendorId = entityVM.VendorId ?? string.Empty,
                    LocationAddress = entityVM.LocationAddress,
                    CountryId = entityVM.CountryId ?? string.Empty,
                    City = entityVM.City ?? string.Empty,
                    StateOrProvince = entityVM.StateOrProvince ?? string.Empty,
                    ZipCode = entityVM.ZipCode ?? string.Empty,
                    Phone = entityVM.Phone ?? string.Empty,
                    Email = entityVM.Email ?? string.Empty,
                    ContactPerson = entityVM.ContactPerson != null ? string.Join(",", entityVM.ContactPerson) : "",
                    Remarks = entityVM.Remarks ?? string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await branchAddressrepository.AddAsync(entity);
                await branchAddressrepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await branchAddressrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(BranchAddressSetupViewModel entityVM)
        {
            await branchAddressrepository.BeginTransactionAsync();

            try
            {
                var entity = await branchAddressrepository.GetByIdAsync(entityVM.VendorBranchId);

                if (entity == null)
                {
                    await branchAddressrepository.RollbackTransactionAsync();
                    return false;
                }

                // Don't update VendorId (Primary Key)

                entity.LocationAddress = entityVM.LocationAddress;
                entity.CountryId = entityVM.CountryId ?? string.Empty;
                entity.City = entityVM.City ?? string.Empty;
                entity.StateOrProvince = entityVM.StateOrProvince ?? string.Empty;
                entity.ZipCode = entityVM.ZipCode ?? string.Empty;
                entity.Phone = entityVM.Phone ?? string.Empty;
                entity.Email = entityVM.Email ?? string.Empty;
                entity.ContactPerson = entityVM.ContactPerson != null ? string.Join(",", entityVM.ContactPerson) : string.Empty;
                entity.Remarks = entityVM.Remarks ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.ModifyDate = DateTime.Now; // or Mdate if that's your column name

                await branchAddressrepository.UpdateAsync(entity);
                await branchAddressrepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await branchAddressrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionBranchAddressAsync()
        {

            var data = await branchAddressrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.VendorBranchId,
                           Name = x.LocationAddress,
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
                branchAddressrepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await branchAddressrepository.BeginTransactionAsync();

            try
            {
                var entity = await branchAddressrepository.All().Where(x => ids.Contains(x.VendorBranchId)).ToListAsync();

                branchAddressrepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await branchAddressrepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await branchAddressrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await branchAddressrepository.All().AnyAsync(x => x.VendorBranchId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await branchAddressrepository.All().AnyAsync(x => x.LocationAddress == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await branchAddressrepository.All().AnyAsync(x => x.LocationAddress == name && x.VendorBranchId != typeCode);
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
        #endregion

        #region GetContactPersonsForTable

        public async Task<CommonReturnViewModel> GetContactPersonsForTableAsync()
        {
            try
            {
                var data = await (
            from cp in contactPersonrepository.All()
            join d in desiRepo.All()
                on cp.DesignationCode equals d.DesignationCode into dg
            from d in dg.DefaultIfEmpty()
            select new ContactPersonTableDto1
            {
                Id = cp.Cpid,
                Name = cp.ContactPersonName ?? "",
                Designation = d != null ? d.DesignationName : "",
                Phone = cp.ContactPersonMobile ?? "",
                Email = cp.ContactPersonEmail ?? ""
            }).ToListAsync();

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Contact persons fetched successfully",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Error fetching contact persons: " + ex.Message
                };
            }
        }

        #endregion

    }
}
