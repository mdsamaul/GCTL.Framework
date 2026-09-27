using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.Companies;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.Companies
{
    public class CompanyService : AppService<CoreCompany>, ICompanyService
    {
        private readonly IRepository<CoreCompany> companyRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Core_Company";
        private const string ColumnName = "CompanyCode";

        public CompanyService(
            IRepository<CoreCompany> companyRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
        )
        : base(companyRepository)
        {
            this.companyRepository = companyRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #region GetAllAsync

        public async Task<List<CompanySetupViewModel>> GetAllAsync()
        {
            var entities = await companyRepository.GetAllAsync();
            return entities.Select(entity => new CompanySetupViewModel
            {
                CompanyCode = entity.CompanyCode,
                CompanyName = entity.CompanyName,
                CompanyShortName = entity.CompanyShortName,
                Address1 = entity.Address1,
                Address2 = entity.Address2,
                Phone1 = entity.Phone1,
                Phone2 = entity.Phone2,
                Fax = entity.Fax,
                HotLine = entity.HotLine,
                Url = entity.Url,
                Email = entity.Email,
                ImgTitle = entity.ImgTitle,
                ZipCode = entity.ZipCode,
                City = entity.City,
                State = entity.State,
                Country = entity.Country,
                RegNo = entity.RegNo,
                Tin = entity.Tin,
                BackImage = entity.BackImage,
                BaseCurrency = entity.BaseCurrency,
                CompanyBanglaName = entity.CompanyBanglaName,
                AddressBangla = entity.AddressBangla,
                Luser = entity.Luser,
                Ldate = entity.Ldate,
                Lip = entity.Lip,
                Lmac = entity.Lmac,
                ModifyDate = entity.ModifyDate
            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<CompanySetupViewModel> GetByIdAsync(string code)
        {
            var entity = await companyRepository.GetByIdAsync(code);
            if (entity == null) return null;

            return new CompanySetupViewModel
            {
                CompanyCode = entity.CompanyCode,
                CompanyName = entity.CompanyName,
                CompanyShortName = entity.CompanyShortName,
                Address1 = entity.Address1,
                Address2 = entity.Address2,
                Phone1 = entity.Phone1,
                Phone2 = entity.Phone2,
                Fax = entity.Fax,
                HotLine = entity.HotLine,
                Url = entity.Url,
                Email = entity.Email,
                ImgTitle = entity.ImgTitle,
                ZipCode = entity.ZipCode,
                City = entity.City,
                State = entity.State,
                Country = entity.Country,
                RegNo = entity.RegNo,
                Tin = entity.Tin,
                BackImage = entity.BackImage,
                BaseCurrency = entity.BaseCurrency,
                CompanyBanglaName = entity.CompanyBanglaName,
                AddressBangla = entity.AddressBangla,
                Luser = entity.Luser,
                Ldate = entity.Ldate,
                Lip = entity.Lip,
                Lmac = entity.Lmac,
                ModifyDate = entity.ModifyDate
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(CompanySetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
            await companyRepository.BeginTransactionAsync();
            try
            {
                CoreCompany entity = new CoreCompany();
                entity.CompanyCode = strMaxNO;
                entity.CompanyName = entityVM.CompanyName;
                entity.CompanyShortName = entityVM.CompanyShortName;
                entity.Address1 = entityVM.Address1;
                entity.Address2 = entityVM.Address2;
                entity.Phone1 = entityVM.Phone1;
                entity.Phone2 = entityVM.Phone2;
                entity.Fax = entityVM.Fax;
                entity.HotLine = entityVM.HotLine;
                entity.Url = entityVM.Url;
                entity.Email = entityVM.Email;
                entity.ImgTitle = entityVM.ImgTitle;
                entity.ZipCode = entityVM.ZipCode;
                entity.City = entityVM.City;
                entity.State = entityVM.State;
                entity.Country = entityVM.Country;
                entity.RegNo = entityVM.RegNo;
                entity.Tin = entityVM.Tin;
                entity.BackImage = entityVM.BackImage;
                entity.BaseCurrency = entityVM.BaseCurrency;
                entity.CompanyBanglaName = entityVM.CompanyBanglaName ?? string.Empty;
                entity.AddressBangla = entityVM.AddressBangla ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;

                await companyRepository.AddAsync(entity);
                await companyRepository.CommitTransactionAsync();
                entityVM.CompanyCode = strMaxNO;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await companyRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(CompanySetupViewModel entityVM)
        {
            await companyRepository.BeginTransactionAsync();
            try
            {
                var entity = await companyRepository.GetByIdAsync(entityVM.CompanyCode);
                if (entity == null)
                {
                    await companyRepository.RollbackTransactionAsync();
                    return false;
                }

                entity.CompanyName = entityVM.CompanyName;
                entity.CompanyShortName = entityVM.CompanyShortName;
                entity.Address1 = entityVM.Address1;
                entity.Address2 = entityVM.Address2;
                entity.Phone1 = entityVM.Phone1;
                entity.Phone2 = entityVM.Phone2;
                entity.Fax = entityVM.Fax;
                entity.HotLine = entityVM.HotLine;
                entity.Url = entityVM.Url;
                entity.Email = entityVM.Email;
                entity.ImgTitle = entityVM.ImgTitle;
                entity.ZipCode = entityVM.ZipCode;
                entity.City = entityVM.City;
                entity.State = entityVM.State;
                entity.Country = entityVM.Country;
                entity.RegNo = entityVM.RegNo;
                entity.Tin = entityVM.Tin;
                entity.BackImage = entityVM.BackImage;
                entity.BaseCurrency = entityVM.BaseCurrency;
                entity.CompanyBanglaName = entityVM.CompanyBanglaName ?? string.Empty;
                entity.AddressBangla = entityVM.AddressBangla ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;

                await companyRepository.UpdateAsync(entity);
                await companyRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await companyRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                companyRepository.GetTableName(),
                ColumnName,
                ids
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await companyRepository.BeginTransactionAsync();

            try
            {
                var entities = await companyRepository.All().Where(x => ids.Contains(x.CompanyCode)).ToListAsync();

                companyRepository.Delete(entities);

                model.tableName = companyRepository.GetTableName();

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entities, model
                );

                await companyRepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await companyRepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check

        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await companyRepository.All().AnyAsync(x => x.CompanyCode == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await companyRepository.All().AnyAsync(x => x.CompanyName == name);
        }

        public async Task<bool> IsExistAsync(string name, string companyCode)
        {
            return await companyRepository.All().AnyAsync(x => x.CompanyName == name && x.CompanyCode != companyCode);
        }

        #endregion

        #region Permission Checks

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Company" || x.Title == "Companies") && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Company" || x.Title == "Companies") && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Company" || x.Title == "Companies") && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Company" || x.Title == "Companies") && x.CheckDelete);
        }

        #endregion

        #region Legacy Methods

        public List<CoreCompany> GetCompanies()
        {
            return GetAll();
        }

        public CoreCompany GetCompany(string id)
        {
            return companyRepository.GetById(id);
        }

        public CompanyModel GetCompanyByCode(string code)
        {
            return companyRepository.All().Where(x => x.CompanyCode == code)
                    .Select(company => new CompanyModel
                    {
                        CompanyName = company.CompanyName,
                        Address1 = company.Address1,
                        Email = company.Email,
                        Phone1 = company.Phone1,
                        HotLine = company.HotLine,
                        Url = company.Url

                    }).FirstOrDefault();
        }

        public CoreCompany SaveCompany(CoreCompany entity)
        {
            if (IsCompanyExistByCode(entity.CompanyCode))
                Update(entity);
            else
                Add(entity);

            return entity;
        }

        public async Task<(bool success, bool refSuccess, string message)> DeleteCompany(List<string> ids, DeleteHistoryViewModel model)
        {
            var res = await DeleteTab(ids, model);
            return (res.succses, !res.succses, res.messege);
        }

        public async Task<IEnumerable<CommonSelectModel>> GetCompanyDropDown()
        {
            return await companyRepository.All()
                 .Select(x => new CommonSelectModel
                 {
                     Code = x.CompanyCode,
                     Name = x.CompanyName
                 }).ToListAsync();
        }

        public bool IsCompanyExistByCode(string code)
        {
            return companyRepository.All().Any(x => x.CompanyCode == code);
        }

        public bool IsCompanyExist(string name)
        {
            return companyRepository.All().Any(x => x.CompanyName == name);
        }

        public bool IsCompanyExist(string name, string companyCode)
        {
            return companyRepository.All().Any(x => x.CompanyName == name && x.CompanyCode != companyCode);
        }

        public bool SavePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Company" || x.Title == "Companies") && x.CheckAdd);
        }

        public bool UpdatePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Company" || x.Title == "Companies") && x.CheckEdit);
        }

        public bool DeletePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Company" || x.Title == "Companies") && x.CheckDelete);
        }

        #endregion
    }
}

