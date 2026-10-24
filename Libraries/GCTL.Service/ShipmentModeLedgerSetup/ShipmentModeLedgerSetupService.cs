using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ShipmentModeLedgerSetup;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.ShipmentModeLedgerSetup
{
    public class ShipmentModeLedgerSetupService : AppService<CfDefExpenseType>, IShipmentModeLedgerSetupService
    {
        #region Service & Repository
        private readonly IRepository<CfDefExpenseType> shipmentModeLedgerrepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;
        private readonly IRepository<AccSubsidiaryLedger> subLedgerRepo;
        private readonly IRepository<AccGeneralLedger> subSubLedgerRepo;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;

        string strMaxNO = string.Empty;
        private const string TableName = "CF_Def_ExpenseType";
        private const string ColumnName = "ExpenseTypeID";

        public ShipmentModeLedgerSetupService(
            IRepository<CfDefExpenseType> shipmentModeLedgerrepository,
            IRepository<CoreAccessCode> accessCodeRepository,
             ICommonService commonService,
            IDeleteHistoryService deleteHistoryService,
            IRepository<AccSubsidiaryLedger> subLedgerRepo,
            IRepository<AccGeneralLedger> subSubLedgerRepo

            )
    : base(shipmentModeLedgerrepository)
        {
            this.shipmentModeLedgerrepository = shipmentModeLedgerrepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
            this.subLedgerRepo = subLedgerRepo;
            this.subSubLedgerRepo = subSubLedgerRepo;
            this.accessCodeRepository = accessCodeRepository;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<ShipmentModeLedgerSetupSetupViewModel>> GetAllAsync()
        {
            var query = 
                    from sml in shipmentModeLedgerrepository.All().AsNoTracking()
                    join a in subSubLedgerRepo.All().AsNoTracking()
                    on sml.AdvanceLedgerCode equals a.SubSusidiaryLedgerCodeNo into advanceGroup
                    from a in advanceGroup.DefaultIfEmpty()
                    join e in subLedgerRepo.All().AsNoTracking()
                    on sml.ExpenseLedgerCodeNo equals e.SusidiaryLedgerCodeNo into expenseGroup
                    from e in expenseGroup.DefaultIfEmpty()
                    join r in subSubLedgerRepo.All().AsNoTracking()
                    on sml.RevenueLedgerCodeNo equals r.SubSusidiaryLedgerCodeNo into revenueGroup
                    from r in revenueGroup.DefaultIfEmpty()
                    select new ShipmentModeLedgerSetupSetupViewModel
                    {
                        Tc = sml.Tc,
                        ExpenseTypeId = sml.ExpenseTypeId ?? string.Empty,
                        ExpenseType = sml.ExpenseType ?? string.Empty,
                        ExpenseLedgerCodeNo = e.SubsidiaryLedgerName ?? string.Empty,
                        RevenueLedgerCodeNo = r.SubSubsidiaryLedgerName ?? string.Empty,
                        AdvanceLedgerCode = a.SubSubsidiaryLedgerName ?? string.Empty,
                        ShortName = sml.ShortName ?? string.Empty,
                        Ldate = sml.Ldate,
                        ModifyDate = sml.ModifyDate,
                        Luser = sml.Luser,
                        Lip = sml.Lip,
                        Lmac = sml.Lmac
                    };

            return await query.ToListAsync();
        }

        #endregion

        #region GetByIdAsync
        
        public async Task<ShipmentModeLedgerSetupSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await shipmentModeLedgerrepository.GetByIdAsync(code);
            
            if (entity == null)
                return null;

            return new ShipmentModeLedgerSetupSetupViewModel
            {
                Tc = entity.Tc,
                ExpenseTypeId = entity.ExpenseTypeId,
                ExpenseType = entity.ExpenseType,
                ExpenseLedgerCodeNo = entity.ExpenseLedgerCodeNo,
                RevenueLedgerCodeNo = entity.RevenueLedgerCodeNo,
                AdvanceLedgerCode = entity.AdvanceLedgerCode,
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

        public async Task<bool> SaveAsync(ShipmentModeLedgerSetupSetupViewModel entityVM)
        {
            try
            {
                commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 4);
                await shipmentModeLedgerrepository.BeginTransactionAsync();

                var entity = new CfDefExpenseType
                {
                    ExpenseTypeId = strMaxNO,
                    ExpenseType = entityVM.ExpenseType,
                    ExpenseLedgerCodeNo = entityVM.ExpenseLedgerCodeNo,
                    RevenueLedgerCodeNo = entityVM.RevenueLedgerCodeNo,
                    AdvanceLedgerCode = entityVM.AdvanceLedgerCode,
                    ShortName = entityVM.ShortName,
                    Luser = entityVM.Luser,
                    Lip = entityVM.Lip,
                    Lmac = entityVM.Lmac ?? string.Empty,
                    Ldate = DateTime.Now
                };

                await shipmentModeLedgerrepository.AddAsync(entity);
                await shipmentModeLedgerrepository.CommitTransactionAsync();

                entityVM.ExpenseTypeId = strMaxNO;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProductionType: {ex.Message}");
                await shipmentModeLedgerrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(ShipmentModeLedgerSetupSetupViewModel entityVM)
        {
            await shipmentModeLedgerrepository.BeginTransactionAsync();

            try
            {
                var entity = await shipmentModeLedgerrepository.GetByIdAsync(entityVM.ExpenseTypeId);

                if (entity == null)
                {
                    await shipmentModeLedgerrepository.RollbackTransactionAsync();
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(entityVM.ExpenseType))
                {
                    entity.ExpenseType = entityVM.ExpenseType;
                }
                entity.ExpenseLedgerCodeNo = entityVM.ExpenseLedgerCodeNo;
                entity.RevenueLedgerCodeNo = entityVM.RevenueLedgerCodeNo;
                entity.AdvanceLedgerCode = entityVM.AdvanceLedgerCode;
                entity.ShortName = entityVM.ShortName;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.ModifyDate = DateTime.Now; 

                await shipmentModeLedgerrepository.UpdateAsync(entity);
                await shipmentModeLedgerrepository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                await shipmentModeLedgerrepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region SelectionAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionShipmentModeLedgerAsync()
        {

            var data = await shipmentModeLedgerrepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.ExpenseTypeId,
                           Name = x.ExpenseType,
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
                shipmentModeLedgerrepository.GetTableName(),
                ColumnName,
                ids.Cast<string>().ToList()
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await shipmentModeLedgerrepository.BeginTransactionAsync();

            try
            {
                var entity = await shipmentModeLedgerrepository.All().Where(x => ids.Contains(x.ExpenseTypeId)).ToListAsync();

                shipmentModeLedgerrepository.Delete(entity);
                model.tableName = TableName;

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await shipmentModeLedgerrepository.CommitTransactionAsync();
                return (true, "Delete Successfully");
            }
            catch (Exception ex)
            {
                await shipmentModeLedgerrepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");

            }

        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return await shipmentModeLedgerrepository.All().AnyAsync(x => x.ExpenseTypeId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return await shipmentModeLedgerrepository.All().AnyAsync(x => x.ExpenseType == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return await shipmentModeLedgerrepository.All().AnyAsync(x => x.ExpenseType == name && x.ExpenseTypeId != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Shipment Mode Ledger Setup" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Shipment Mode Ledger Setup" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Shipment Mode Ledger Setup" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Shipment Mode Ledger Setup" && x.CheckDelete);
        }
        #endregion

        #region Dropdown

        public async Task<IEnumerable<CommonSelectModel>> SelectAdvanceLedger()
        {
            var data = await subSubLedgerRepo.All().AsNoTracking()
                .Where(x => x.SubsidiaryLedgerCodeNo == "10200300001")
                .Select(x => new CommonSelectModel
                {
                    Code = x.SubSusidiaryLedgerCodeNo,
                    Name = x.SubSubsidiaryLedgerName
                }).ToListAsync();
            return data;
        }

        public async Task<IEnumerable<CommonSelectModel>> SelectExpenseLedger()
        {
            var code = new[] { "301001", "301002" };
            var data = await subLedgerRepo.All().AsNoTracking()
                .Where(x => code.Contains(x.GeneralLedgerCodeNo))
                .Select(x => new CommonSelectModel
                {
                    Code = x.SusidiaryLedgerCodeNo,
                    Name = x.SubsidiaryLedgerName
                }).ToListAsync();
            return data;
        }

        public async Task<IEnumerable<CommonSelectModel>> SelectRevenueLedger()
        {
            var code = new[] { "401001", "401002" };
            var data = await (
                    from ssl in subSubLedgerRepo.All().AsNoTracking()
                    join sl in subLedgerRepo.All()
                    on ssl.SubsidiaryLedgerCodeNo equals sl.SusidiaryLedgerCodeNo
                    where code.Contains(sl.GeneralLedgerCodeNo)
                    select new CommonSelectModel
                    {
                        Code = ssl.SubSusidiaryLedgerCodeNo,
                        Name = ssl.SubSubsidiaryLedgerName
                    }).ToListAsync();
            return data;
        }

        public async Task<IEnumerable<CommonSelectModel>> SelectShipmentMode()
        {
            var list = await shipmentModeLedgerrepository.All().AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.ExpenseType))
                .Select(x => x.ExpenseType.Trim())
                .Distinct()
                .ToListAsync();

            if (!list.Any())
            {
                list = new List<string> { "AIR", "SEA", "ROAD", "RAIL" };
            }

            return list.Select(name => new CommonSelectModel
            {
                Code = name,
                Name = name
            }).ToList();
        }
        #endregion

    }
}
