using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace GCTL.Service.TransportInformation
{
    public class TransportInformationService : AppService<SalesDefVehicle>, ITransportInformationService
    {
        #region Service & Repository
        private readonly IRepository<SalesDefVehicle> transportInformationrepository;
        private readonly IRepository<SalesDefVehicleType> transportTyperepository;
        private readonly IRepository<SalesDefPortName> portNamerepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "Sales_Def_Vehicle";
        private const string ColumnName = "VehicleID";


        public TransportInformationService(
            IRepository<SalesDefVehicle> transportInformationrepository,
            IRepository<SalesDefVehicleType> transportTyperepository,
            IRepository<SalesDefPortName> portNamerepository,
            IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService

            ) 
            
    : base(transportInformationrepository)
        {
            this.transportInformationrepository = transportInformationrepository;
            this.transportTyperepository = transportTyperepository;
            this.portNamerepository = portNamerepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<TransportInformationSetupViewModel>> GetAllAsync()
        {
            var query = await (from per in transportInformationrepository.All()
                               join por in transportTyperepository.All()
                               on per.VehicleTypeId equals por.VehicleTypeId into portGroup
                               from por in portGroup.DefaultIfEmpty()
                               join pna in portNamerepository.All()
                               on per.PortNameId equals pna.PortNameId into portNameGroup
                               from pna in portNameGroup.DefaultIfEmpty()
                               select new TransportInformationSetupViewModel
                               {
                                   Tc = per.Tc,
                                   VehicleId = per.VehicleId,
                                   VehicleNo = per.VehicleNo,
                                   VehicleTypeId = per.VehicleTypeId,
                                   VehicleType = por.VehicleType,
                                   PortNameId = per.PortNameId,
                                   PortName = pna.PortName,
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

        public async Task<TransportInformationSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await transportInformationrepository.GetByIdAsync(code);

            if (entity == null)
                return null;

            return new TransportInformationSetupViewModel
            {
                Tc = entity.Tc,
                VehicleId = entity.VehicleId,
                VehicleNo = entity.VehicleNo,
                VehicleTypeId = entity.VehicleTypeId,
                PortNameId = entity.PortNameId,
                Remarks = entity.Remarks,
                CompanyCode = entity.CompanyCode,
                EmployeeId = entity.EmployeeId,
                Phone = entity.Phone,
                VendorId = entity.VendorId,
                FactoryId = entity.FactoryId,
                Ldate = entity.Ldate,
                ModifyDate = entity.ModifyDate,
                Luser = entity.Luser,
                Lip = entity.Lip,
                Lmac = entity.Lmac
            };
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(TransportInformationSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 4);
                await transportInformationrepository.BeginTransactionAsync();

                var entity = new SalesDefVehicle
                {
                    VehicleId = strMaxNO,
                    VehicleNo = entityVM.VehicleNo,
                    VehicleTypeId = entityVM.VehicleTypeId,
                    PortNameId = entityVM.PortNameId,
                    Phone = entityVM.Phone ?? string.Empty,
                    CompanyCode = entityVM.UserCompany ?? string.Empty,
                    EmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty,
                    VendorId = entityVM.VendorId ?? string.Empty,
                    FactoryId = entityVM.FactoryId ?? string.Empty,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await transportInformationrepository.AddAsync(entity);
                await transportInformationrepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await transportInformationrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(TransportInformationSetupViewModel entityVM)
        {
            await transportInformationrepository.BeginTransactionAsync();

            try
            {
                var entity = await transportInformationrepository.GetByIdAsync(entityVM.VehicleId);

                if (entity == null)
                {
                    await transportInformationrepository.RollbackTransactionAsync();
                    return false;
                }

                // entity.VehicleId = entityVM.VehicleId; // Don't update primary key

                entity.VehicleNo = entityVM.VehicleNo;
                entity.VehicleTypeId = entityVM.VehicleTypeId;
                entity.PortNameId = entityVM.PortNameId;
                entity.Phone = entityVM.Phone ?? string.Empty;
                entity.CompanyCode = entityVM.UserCompany ?? string.Empty;
                entity.EmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty;
                entity.VendorId = entityVM.VendorId ?? string.Empty;
                entity.FactoryId = entityVM.FactoryId ?? string.Empty;

                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.ModifyDate = DateTime.Now;

                await transportInformationrepository.UpdateAsync(entity);
                await transportInformationrepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await transportInformationrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionTransportInformationAsync()
        {

            var data = await transportInformationrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.VehicleId,
                           Name = x.VehicleNo,
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
                transportInformationrepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await transportInformationrepository.BeginTransactionAsync();

            try
            {
                var entity = await transportInformationrepository.All().Where(x => ids.Contains(x.VehicleId)).ToListAsync();

                transportInformationrepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await transportInformationrepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await transportInformationrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await transportInformationrepository.All().AnyAsync(x => x.VendorId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await transportInformationrepository.All().AnyAsync(x => x.VehicleNo == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await transportInformationrepository.All().AnyAsync(x => x.VehicleNo == name && x.VehicleId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Information" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Information" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Information" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Transport Information" && x.CheckDelete);
        }
        #endregion

    }
}
