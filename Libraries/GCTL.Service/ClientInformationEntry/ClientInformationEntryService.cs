using GCTL.Core.Data;
using GCTL.Core.ViewModels.ClientInformationEntry;
using GCTL.Core.ViewModels.Common;
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

namespace GCTL.Service.ClientInformationEntry
{
    public class ClientInformationEntryService : AppService<HrmClientInfo>, IClientInformationEntryService
    {
        #region Service & Repository
        private readonly IRepository<HrmClientInfo> clientInformationRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly IRepository<CaDefCountry> countryRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;

        private const string TableName = "HRM_ClientInfo";
        private const string ColumnName = "ClientID";
        public ClientInformationEntryService(
            IRepository<HrmClientInfo> clientInformationRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            IRepository<CaDefCountry> countryRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            )

    : base(clientInformationRepository)
        {
            this.clientInformationRepository = clientInformationRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.countryRepository = countryRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAll
        public async Task<List<ClientInformationEntrySetupViewModel>> GetAllAsync()
        {
            var entity = await clientInformationRepository.GetAllAsync();
            return entity.Select(entityVM => new ClientInformationEntrySetupViewModel
            {
                AutoId = entityVM.AutoId,
                ClientId = entityVM.ClientId,
                ClientName = entityVM.ClientName,
                ClientAddress = entityVM.ClientAddress,
                Phone = entityVM.Phone,
                Email = entityVM.Email,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,
                Ldate = entityVM.Ldate,

            }).ToList();
        }

        #endregion

        #region GetById
        public async Task<ClientInformationEntrySetupViewModel> GetByIdAsync(string code)
        {
            var entity = await clientInformationRepository.GetByIdAsync(code);
            if (entity == null && decimal.TryParse(code, out decimal autoId))
            {
                entity = await clientInformationRepository.All().FirstOrDefaultAsync(x => x.AutoId == autoId);
            }
            if (entity == null) return null;

            ClientInformationEntrySetupViewModel entityVM = new ClientInformationEntrySetupViewModel();

            entityVM.AutoId = entity.AutoId;
            entityVM.ClientId = entity.ClientId;
            entityVM.ClientName = entity.ClientName;
            entityVM.ShortName = entity.ShortName;
            entityVM.ClientAddress = entity.ClientAddress;
            entityVM.CountryId = entity.CountryId;
            entityVM.City = entity.City;
            entityVM.State = entity.State;
            entityVM.ZipCode = entity.ZipCode;
            entityVM.Phone = entity.Phone;
            entityVM.Fax = entity.Fax;
            entityVM.Email = entity.Email;
            entityVM.Url = entity.Url;
            entityVM.Bin = entity.Bin;
            entityVM.Vat = entity.Vat;
            entityVM.Tin = entity.Tin;
            entityVM.ContatPerson1 = entity.ContatPerson1;
            entityVM.Designation1 = entity.Designation1;
            entityVM.Phone1 = entity.Phone1;
            entityVM.Email1 = entity.Email1;
            entityVM.ContatPerson2 = entity.ContatPerson2;
            entityVM.Designation2 = entity.Designation2;
            entityVM.Phone2 = entity.Phone2;
            entityVM.Email2 = entity.Email2;
            entityVM.Luser = entity.Luser;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;
            entityVM.Ldate = entity.Ldate;
           // entityVM.ModifyDate = entity.ModifyDate;
            entityVM.UserEmployeeId = entity.UserEmployeeId;
            entityVM.CompanyCode = entity.CompanyCode;

            return entityVM;
        }

        #endregion

        #region Save

        public async Task<bool> SaveAsync(ClientInformationEntrySetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 4);
            await clientInformationRepository.BeginTransactionAsync();
            try
            {
                HrmClientInfo entity = new HrmClientInfo();
                entity.ClientId = strMaxNO;
                entity.ClientName = entityVM.ClientName;
                entity.ShortName = entityVM.ShortName ?? string.Empty;
                entity.ClientAddress = entityVM.ClientAddress ?? string.Empty;
                entity.CountryId = entityVM.CountryId;
                entity.City = entityVM.City ?? string.Empty;
                entity.State = entityVM.State ?? string.Empty;
                entity.ZipCode = entityVM.ZipCode ?? string.Empty;
                entity.Phone = entityVM.Phone ?? string.Empty;
                entity.Fax = entityVM.Fax ?? string.Empty;
                entity.Email = entityVM.Email ?? string.Empty;
                entity.Url = entityVM.Url ?? string.Empty;
                entity.Bin = entityVM.Bin ?? string.Empty;
                entity.Vat = entityVM.Vat ?? string.Empty;
                entity.Tin = entityVM.Tin ?? string.Empty;
                entity.ContatPerson1 = entityVM.ContatPerson1 ?? string.Empty;
                entity.Designation1 = entityVM.Designation1 ?? string.Empty;
                entity.Phone1 = entityVM.Phone1 ?? string.Empty;
                entity.Email1 = entityVM.Email1 ?? string.Empty;
                entity.ContatPerson2 = entityVM.ContatPerson2 ?? string.Empty;
                entity.Designation2 = entityVM.Designation2 ?? string.Empty;
                entity.Phone2 = entityVM.Phone2 ?? string.Empty;
                entity.Email2 = entityVM.Email2 ?? string.Empty;
                entity.UserEmployeeId = entityVM.UserInfoEmployeeId;
                entity.CompanyCode = entityVM.CompanyCode;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;

                await clientInformationRepository.AddAsync(entity);
                await clientInformationRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await clientInformationRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region Update

        public async Task<bool> UpdateAsync(ClientInformationEntrySetupViewModel entityVM)
        {
            await clientInformationRepository.BeginTransactionAsync();
            try
            {
                var entity = await clientInformationRepository.GetByIdAsync(entityVM.ClientId);
                if (entity == null)
                {
                    await clientInformationRepository.RollbackTransactionAsync();
                    return false;
                }

                entity.ClientId = entityVM.ClientId;
                entity.ClientName = entityVM.ClientName;
                entity.ShortName = entityVM.ShortName ?? string.Empty;
                entity.ClientAddress = entityVM.ClientAddress ?? string.Empty;
                entity.CountryId = entityVM.CountryId;
                entity.City = entityVM.City ?? string.Empty;
                entity.State = entityVM.State ?? string.Empty;
                entity.ZipCode = entityVM.ZipCode ?? string.Empty;
                entity.Phone = entityVM.Phone ?? string.Empty;
                entity.Fax = entityVM.Fax ?? string.Empty;
                entity.Email = entityVM.Email ?? string.Empty;
                entity.Url = entityVM.Url ?? string.Empty;
                entity.Bin = entityVM.Bin ?? string.Empty;
                entity.Vat = entityVM.Vat ?? string.Empty;
                entity.Tin = entityVM.Tin ?? string.Empty;
                entity.ContatPerson1 = entityVM.ContatPerson1 ?? string.Empty;
                entity.Designation1 = entityVM.Designation1 ?? string.Empty;
                entity.Phone1 = entityVM.Phone1 ?? string.Empty;
                entity.Email1 = entityVM.Email1 ?? string.Empty;
                entity.ContatPerson2 = entityVM.ContatPerson2 ?? string.Empty;
                entity.Designation2 = entityVM.Designation2 ?? string.Empty;
                entity.Phone2 = entityVM.Phone2 ?? string.Empty;
                entity.Email2 = entityVM.Email2 ?? string.Empty;
                entity.UserEmployeeId = entityVM.UserInfoEmployeeId;
                entity.CompanyCode = entityVM.CompanyCode;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
               // entity.ModifyDate = DateTime.Now;

                await clientInformationRepository.UpdateAsync(entity);
                await clientInformationRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await clientInformationRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync
        public async Task<IEnumerable<CommonSelectModel>> SelectionClientInformationAsync()
        {

            var data = await clientInformationRepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.ClientId,
                           Name = x.ClientName,
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
                clientInformationRepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await clientInformationRepository.BeginTransactionAsync();

            try
            {
                var entity = await clientInformationRepository.All().Where(x => ids.Contains(x.ClientId)).ToListAsync();

                clientInformationRepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await clientInformationRepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await clientInformationRepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await clientInformationRepository.All().AnyAsync(x => x.ClientId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await clientInformationRepository.All().AnyAsync(x => x.ClientName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await clientInformationRepository.All().AnyAsync(x => x.ClientName == name && x.ClientId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Client Information Entry" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Client Information Entry" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Client Information Entry" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Client Information Entry" && x.CheckDelete);
        }
        #endregion

    }
}
