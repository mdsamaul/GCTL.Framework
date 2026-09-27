using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortType;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.PortType
{
    public class PortTypeService : AppService<SalesDefPortType>, IPortTypeService
    {
        #region Service & Repository
        private IRepository<SalesDefPortType> portTyperepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;

        private const string TableName = "Sales_Def_PortType";
        private const string ColumnName = "PortId";

        public PortTypeService(
            IRepository<SalesDefPortType> portTyperepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            )
    : base(portTyperepository)
        {
            this.portTyperepository = portTyperepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<PortTypeSetupViewModel>> GetAllAsync()
        {
            var entity = await portTyperepository.GetAllAsync();
            return entity.Select(entityVM => new PortTypeSetupViewModel
            {
                Tc = entityVM.Tc,
                PortId = entityVM.PortId,
                PortType = entityVM.PortType,
                ShortName = entityVM.ShortName,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,

            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<PortTypeSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await portTyperepository.GetByIdAsync(code);

            if (entity == null)
            {
                entity = await portTyperepository.All().FirstOrDefaultAsync(x => x.PortId == code || (x.PortId != null && x.PortId.Trim() == (code != null ? code.Trim() : "")));
            }

            if (entity == null)
                return null;

            return new PortTypeSetupViewModel
            {
                Tc = entity.Tc,
                PortId = entity.PortId,
                PortType = entity.PortType,
                ShortName = entity.ShortName,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(PortTypeSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 4);
                await portTyperepository.BeginTransactionAsync();

                var entity = new SalesDefPortType
                {
                    PortId = strMaxNO,
                    PortType = entityVM.PortType,
                    ShortName = entityVM.ShortName ?? string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await portTyperepository.AddAsync(entity);
                await portTyperepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await portTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync
        public async Task<bool> UpdateAsync(PortTypeSetupViewModel entityVM)
        {
            await portTyperepository.BeginTransactionAsync();
            try
            {
                var entity = await portTyperepository.GetByIdAsync(entityVM.PortId);
                if (entity == null)
                {
                    entity = await portTyperepository.All().FirstOrDefaultAsync(x => x.PortId == entityVM.PortId || (x.PortId != null && x.PortId.Trim() == (entityVM.PortId != null ? entityVM.PortId.Trim() : "")));
                }
                if (entity == null)
                {
                    await portTyperepository.RollbackTransactionAsync();
                    return false;
                }
                entity.PortId = entityVM.PortId;
                entity.PortType = entityVM.PortType;
                entity.ShortName = entityVM.ShortName;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await portTyperepository.UpdateAsync(entity);
                await portTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await portTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionPortTypeAsync()
        {

            var data = await portTyperepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.PortId,
                           Name = x.PortType,
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
                portTyperepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await portTyperepository.BeginTransactionAsync();

            try
            {
                var entity = await portTyperepository.All().Where(x => ids.Contains(x.PortId)).ToListAsync();

                portTyperepository.Delete(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                   entity, model
                );

                await portTyperepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await portTyperepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await portTyperepository.All().AnyAsync(x => x.PortId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await portTyperepository.All().AnyAsync(x => x.PortType == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await portTyperepository.All().AnyAsync(x => x.PortType == name && x.PortId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Type" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Type" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Type" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Type" && x.CheckDelete);
        }
        #endregion

    }
}
