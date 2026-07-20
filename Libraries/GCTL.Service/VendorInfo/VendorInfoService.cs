using GCTL.Core.Data;
using GCTL.Core.ViewModels.PortType;
using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Core.ViewModels.VendorInfo;
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

    }
}
