using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentType;
using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Core.ViewModels.TransportType;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.TransportType
{
    public class TransportTypeService : AppService<SalesDefVehicleType>, ITransportTypeService
    {
        #region Service & Repository
        private readonly IRepository<SalesDefVehicleType> transportTypetrepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Sales_Def_VehicleType";
        private const string ColumnName = "VehicleTypeID";
        public TransportTypeService(
            IRepository<SalesDefVehicleType> transportTypetrepository,
            IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
            
    : base(transportTypetrepository)
        {
            this.transportTypetrepository = transportTypetrepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<TransportTypeSetupViewModel>> GetAllAsync()
        {
            var entity = await transportTypetrepository.GetAllAsync();
            return entity.Select(entityVM => new TransportTypeSetupViewModel
            {
                Tc = entityVM.Tc,
                VehicleTypeId = entityVM.VehicleTypeId,
                VehicleType = entityVM.VehicleType,
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

        public async Task<TransportTypeSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await transportTypetrepository.GetByIdAsync(code);

            if (entity == null)
                return null;

            return new TransportTypeSetupViewModel
            {
                Tc = entity.Tc,
                VehicleTypeId = entity.VehicleTypeId,
                VehicleType = entity.VehicleType,
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

        public async Task<bool> SaveAsync(TransportTypeSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 2);
                await transportTypetrepository.BeginTransactionAsync();

                var entity = new SalesDefVehicleType
                {
                    VehicleTypeId = strMaxNO,
                    VehicleType = entityVM.VehicleType,
                    ShortName = entityVM.ShortName,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await transportTypetrepository.AddAsync(entity);
                await transportTypetrepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await transportTypetrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync
        public async Task<bool> UpdateAsync(TransportTypeSetupViewModel entityVM)
        {
            await transportTypetrepository.BeginTransactionAsync();
            try
            {
                var entity = await transportTypetrepository.GetByIdAsync(entityVM.VehicleTypeId);
                if (entity == null)
                {
                    await transportTypetrepository.RollbackTransactionAsync();
                    return false;
                }
                entity.VehicleTypeId = entityVM.VehicleTypeId;
                entity.VehicleType = entityVM.VehicleType;
                entity.ShortName = entityVM.ShortName;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await transportTypetrepository.UpdateAsync(entity);
                await transportTypetrepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await transportTypetrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync
        public async Task<IEnumerable<CommonSelectModel>> SelectionTransportTypeAsync()
        {

            var data = await transportTypetrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.VehicleTypeId,
                           Name = x.VehicleType,
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
                transportTypetrepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await transportTypetrepository.BeginTransactionAsync();

            try
            {
                var entity = await transportTypetrepository.All().Where(x => ids.Contains(x.VehicleTypeId)).ToListAsync();


                transportTypetrepository.Delete(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await transportTypetrepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await transportTypetrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }


        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await transportTypetrepository.All().AnyAsync(x => x.VehicleTypeId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await transportTypetrepository.All().AnyAsync(x => x.VehicleType == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await transportTypetrepository.All().AnyAsync(x => x.VehicleType == name && x.VehicleTypeId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Type" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Type" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Type" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Type" && x.CheckDelete);
        }
        #endregion

    }
}
