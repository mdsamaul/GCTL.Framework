using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortName;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.PortName
{
    public class PortNameService : AppService<SalesDefPortName>, IPortNameService
    {
        #region Service & Repository
        private readonly IRepository<SalesDefPortName> portNamerepository;
        private readonly IRepository<SalesDefPortType> protaltyperepo;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Sales_Def_PortName";
        private const string ColumnName = "PortNameId";

        public PortNameService(
            IRepository<SalesDefPortName> portNamerepository,
            IRepository<SalesDefPortType> protaltyperepo,
            IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
            
    : base(portNamerepository)
        {
            this.portNamerepository = portNamerepository;
            this.protaltyperepo = protaltyperepo;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<PortNameSetupViewModel>> GetAllAsync()
        {
            var query = await (from per in portNamerepository.All()
                               join por in protaltyperepo.All()
                               on per.PortId equals por.PortId into portGroup
                               from por in portGroup.DefaultIfEmpty()
                               select new PortNameSetupViewModel
                               {
                                   Tc = per.Tc,
                                   PortNameId = per.PortNameId,
                                   PortName = per.PortName,
                                   PortId = per.PortId,
                                   PortType = por.PortType,
                                   ShortName = per.ShortName,
                                   Ldate = per.Ldate,
                                   ModifyDate = per.ModifyDate,
                                   Luser = per.Luser,
                                   Lip = per.Lip,
                                   Lmac = per.Lmac
                               }).ToListAsync();

            return query;
        }


        #endregion

        #region GetByIdAsync

        public async Task<PortNameSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await portNamerepository.GetByIdAsync(code);

            if (entity == null)
                return null;

            return new PortNameSetupViewModel
            {
                Tc = entity.Tc,
                PortNameId = entity.PortNameId,
                PortName = entity.PortName,
                ShortName = entity.ShortName,
                PortId = entity.PortId,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(PortNameSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 4);
                await portNamerepository.BeginTransactionAsync();

                var entity = new SalesDefPortName
                {
                    PortNameId = strMaxNO,
                    PortName = entityVM.PortName,
                    ShortName = entityVM.ShortName ?? string.Empty,
                    PortId = entityVM.PortId,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await portNamerepository.AddAsync(entity);
                await portNamerepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await portNamerepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(PortNameSetupViewModel entityVM)
        {
            await portNamerepository.BeginTransactionAsync();
            try
            {
                var entity = await portNamerepository.GetByIdAsync(entityVM.PortNameId);
                if (entity == null)
                {
                    await portNamerepository.RollbackTransactionAsync();
                    return false;
                }
                //entity.PortNameId = entityVM.PortNameId;
                entity.PortName = entityVM.PortName;
                entity.ShortName = entityVM.ShortName;
                entity.PortId = entityVM.PortId;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await portNamerepository.UpdateAsync(entity);
                await portNamerepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await portNamerepository.RollbackTransactionAsync();
                return false;
            }
        }


        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionPortNameAsync()
        {

            var data = await portNamerepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.PortNameId,
                           Name = x.PortName,
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
                portNamerepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await portNamerepository.BeginTransactionAsync();

            try
            {
                var entity = await portNamerepository.All().Where(x => ids.Contains(x.PortNameId)).ToListAsync();

                portNamerepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await portNamerepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await portNamerepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await portNamerepository.All().AnyAsync(x => x.PortNameId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await portNamerepository.All().AnyAsync(x => x.PortName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await portNamerepository.All().AnyAsync(x => x.PortName == name && x.PortNameId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Name" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Name" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Name" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Port Name" && x.CheckDelete);
        }
        #endregion

    }
}
