using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportModeType;
using GCTL.Core.ViewModels.TransportType;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.ModeType
{
    public class ModeTypeService : AppService<TransportModeType>, IModeTypeService
    {
        #region Service & Repository
        private readonly IRepository<TransportModeType> modeTyperepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Transport_Mode_Type";
        private const string ColumnName = "TransportModeId";

        public ModeTypeService(
            IRepository<TransportModeType> modeTyperepository,
            IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
    : base(modeTyperepository)
        {
            this.modeTyperepository = modeTyperepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<TransportModeTypeSetupViewModel>> GetAllAsync()
        {
            var entity = await modeTyperepository.GetAllAsync();
            return entity.Select(entityVM => new TransportModeTypeSetupViewModel
            {
                Tc = entityVM.Tc,
                TransportModeId = entityVM.TransportModeId,
                TransportMode = entityVM.TransportMode,
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

        public async Task<TransportModeTypeSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await modeTyperepository.GetByIdAsync(code);

            if (entity == null)
                return null;

            return new TransportModeTypeSetupViewModel
            {
                Tc = entity.Tc,
                TransportModeId = entity.TransportModeId,
                TransportMode = entity.TransportMode, 
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

        public async Task<bool> SaveAsync(TransportModeTypeSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 3);
                await modeTyperepository.BeginTransactionAsync();

                var entity = new TransportModeType
                {
                    TransportModeId = strMaxNO,
                    TransportMode = entityVM.TransportMode,
                    ShortName = entityVM.ShortName,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await modeTyperepository.AddAsync(entity);
                await modeTyperepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await modeTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync
        public async Task<bool> UpdateAsync(TransportModeTypeSetupViewModel entityVM)
        {
            await modeTyperepository.BeginTransactionAsync();
            try
            {
                var entity = await modeTyperepository.GetByIdAsync(entityVM.TransportModeId);
                if (entity == null)
                {
                    await modeTyperepository.RollbackTransactionAsync();
                    return false;
                }
                entity.TransportModeId = entityVM.TransportModeId;
                entity.TransportMode = entityVM.TransportMode;
                entity.ShortName = entityVM.ShortName;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await modeTyperepository.UpdateAsync(entity);
                await modeTyperepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await modeTyperepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync
        public async Task<IEnumerable<CommonSelectModel>> SelectionTransportModeTypeAsync()
        {

            var data = await modeTyperepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.TransportModeId,
                           Name = x.TransportMode,
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
                modeTyperepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await modeTyperepository.BeginTransactionAsync();

            try
            {
                var entity = await modeTyperepository.All().Where(x => ids.Contains(x.TransportModeId)).ToListAsync();


                modeTyperepository.Delete(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await modeTyperepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await modeTyperepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }


        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await modeTyperepository.All().AnyAsync(x => x.TransportModeId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await modeTyperepository.All().AnyAsync(x => x.TransportMode == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await modeTyperepository.All().AnyAsync(x => x.TransportMode == name && x.TransportModeId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Mode Type" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Mode Type" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Mode Type" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Mode Type" && x.CheckDelete);
        }
        #endregion

    }
}
