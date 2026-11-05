using Dapper;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.CoreBankAccountInformations;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.CoreBankAccountInformations
{
    public class CoreBankAccountInformationService : AppService<CoreBankAccountInformation>, ICoreBankAccountInformationService
    {
        private readonly IRepository<CoreBankAccountInformation> bankAccountepository;
        private readonly IRepository<SalesDefBankInfo> bankRepository;
        private readonly IRepository<SalesDefBankBranchInfo> branchBankRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;
        private readonly IConfiguration? _configuration;

        private const string TableName = "Core_BankAccountInformation";
        private const string ColumnName = "AccInfoID";

        public CoreBankAccountInformationService(
            IRepository<CoreBankAccountInformation> bankAccountepository,
            ICommonService commonService,
            IRepository<SalesDefBankBranchInfo> branchBankRepository,
            IRepository<SalesDefBankInfo> bankRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            IDeleteHistoryService deleteHistoryService,
            IConfiguration? configuration = null
        ) : base(bankAccountepository)
        {
            this.bankAccountepository = bankAccountepository;
            this.bankRepository = bankRepository;
            this.branchBankRepository = branchBankRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
            this._configuration = configuration;
        }

        #region GettALLById
        public async Task<List<CoreBankAccountInformationSetupViewModel>> GetAllAsync()
        {
            var data = await (from bA in bankAccountepository.All().AsNoTracking()
                              join b in bankRepository.All().AsNoTracking()
                              on bA.BankId equals b.BankId into bAaJoin
                              from b in bAaJoin.DefaultIfEmpty()
                              join braBank in branchBankRepository.All().AsNoTracking()
                              on bA.BranchId equals braBank.BankBranchId into bAaBranchJoin
                              from braBank in bAaBranchJoin.DefaultIfEmpty()
                              select new CoreBankAccountInformationSetupViewModel
                              {
                                  AutoId = bA.AutoId,
                                  Tc = (int)bA.AutoId,
                                  AccInfoId = bA.AccInfoId,
                                  BankId = bA.BankId,
                                  BankName = b.BankName,
                                  UserInfoEmployeeId = bA.UserInfoEmployeeId,
                                  BranchId = braBank.BankBranchId,
                                  BranchName = braBank.BankBranchName,
                                  AccountName = bA.AccountName,
                                  AccountNo = bA.AccountNo,
                                  Luser = bA.Luser,
                                  Ldate = bA.Ldate,
                                  Lmac = bA.Lmac,
                                  ModifyDate = bA.ModifyDate,
                              }).ToListAsync();
            return data;
        }

        public async Task<CoreBankAccountInformationSetupViewModel> GetByIdAsync(string code)
        {
            var data = await (from bA in bankAccountepository.All().AsNoTracking()
                              join b in bankRepository.All().AsNoTracking()
                              on bA.BankId equals b.BankId into bAaJoin
                              from b in bAaJoin.DefaultIfEmpty()
                              join braBank in branchBankRepository.All().AsNoTracking()
                              on bA.BranchId equals braBank.BankBranchId into bAaBranchJoin
                              from braBank in bAaBranchJoin.DefaultIfEmpty()
                              where bA.AccInfoId == code
                              select new CoreBankAccountInformationSetupViewModel
                              {
                                  AutoId = bA.AutoId,
                                  Tc = (int)bA.AutoId,
                                  AccInfoId = bA.AccInfoId,
                                  BankId = bA.BankId,
                                  BankName = b.BankName,
                                  UserInfoEmployeeId = bA.UserInfoEmployeeId,
                                  BranchId = braBank.BankBranchId,
                                  BranchName = braBank.BankBranchName,
                                  AccountName = bA.AccountName,
                                  AccountNo = bA.AccountNo,
                                  Luser = bA.Luser,
                                  Ldate = bA.Ldate,
                                  Lmac = bA.Lmac,
                                  ModifyDate = bA.ModifyDate,
                              }).FirstOrDefaultAsync();
            return data;
        }
        #endregion

        #region Save Update 
        public async Task<bool> SaveAsync(CoreBankAccountInformationSetupViewModel entityVM, string CompanyCode)
        {
            await bankAccountepository.BeginTransactionAsync();
            try
            {
                CoreBankAccountInformation entity = new CoreBankAccountInformation();
                entity.AccInfoId = await GenerateNexCode();
                entity.AccountName = entityVM.AccountName;
                entity.BankId = entityVM.BankId;
                entity.BranchId = entityVM.BranchId;
                entity.AccountNo = entityVM.AccountNo;
                entity.CompanyCode = CompanyCode;
                entity.UserInfoEmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await bankAccountepository.AddAsync(entity);
                await bankAccountepository.CommitTransactionAsync();
                await EnsureBankAccountGeneralLedgerAsync(entityVM.AccountNo, entityVM.BankId, entityVM.Luser, null);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                await bankAccountepository.RollbackTransactionAsync();
                return false;
            }
            finally
            {
                await bankAccountepository.DisposeTransactionAsync();
            }
        }

        public async Task<bool> UpdateAsync(CoreBankAccountInformationSetupViewModel entityVM)
        {
            await bankAccountepository.BeginTransactionAsync();
            try
            {
                var entity = await bankAccountepository.GetByIdAsync(entityVM.AccInfoId);
                if (entity == null)
                {
                    await bankAccountepository.RollbackTransactionAsync();
                    return false;
                }
                var oldAccountNo = entity.AccountNo;
                entity.AccInfoId = entityVM.AccInfoId;
                entity.AccountName = entityVM.AccountName;
                entity.BankId = entityVM.BankId;
                entity.BranchId = entityVM.BranchId;
                entity.AccountNo = entityVM.AccountNo;
                entity.UserInfoEmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await bankAccountepository.UpdateAsync(entity);
                await bankAccountepository.CommitTransactionAsync();
                await EnsureBankAccountGeneralLedgerAsync(entityVM.AccountNo, entityVM.BankId, entityVM.Luser, oldAccountNo);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                await bankAccountepository.RollbackTransactionAsync();
                return false;
            }
            finally
            {
                await bankAccountepository.DisposeTransactionAsync();
            }
        }
        #endregion

        private async Task EnsureBankAccountGeneralLedgerAsync(string? accountNo, string? bankId, string? luser, string? oldAccountNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accountNo)) return;

                var connStr = _configuration?.GetConnectionString("connection")
                    ?? _configuration?.GetConnectionString("DefaultConnection")
                    ?? "Server=192.168.0.221,1433;Database=GCTL_ERP_DB_Copy;User Id=sa;Password=GCTL#123;TrustServerCertificate=True;";

                using var connection = new SqlConnection(connStr);
                await connection.OpenAsync();

                string accNo = accountNo.Trim();
                string user = luser ?? "admin";

                string shortName = string.Empty;
                if (!string.IsNullOrWhiteSpace(bankId))
                {
                    shortName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ShortName FROM SALES_Def_BankInfo WHERE BankID = @BankId",
                        new { BankId = bankId.Trim() }) ?? string.Empty;
                }

                string? existingCode = null;
                if (!string.IsNullOrWhiteSpace(oldAccountNo))
                {
                    existingCode = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT TOP 1 GeneralLedgerCodeNo 
                          FROM Acc_GeneralLedger 
                          WHERE SubsidiaryLedgerCodeNo = '10200400003' 
                            AND (GeneralLedgerName = @OldAccNo OR GeneralLedgerName LIKE '%(' + @OldAccNo + ')')",
                        new { OldAccNo = oldAccountNo.Trim() });
                }

                if (string.IsNullOrEmpty(existingCode))
                {
                    existingCode = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT TOP 1 GeneralLedgerCodeNo 
                          FROM Acc_GeneralLedger 
                          WHERE SubsidiaryLedgerCodeNo = '10200400003' 
                            AND (GeneralLedgerName = @AccNo OR GeneralLedgerName LIKE '%(' + @AccNo + ')')",
                        new { AccNo = accNo });
                }

                if (string.IsNullOrEmpty(existingCode))
                {
                    var nextSeq = await connection.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(MAX(TRY_CAST(RIGHT(GeneralLedgerCodeNo, 5) AS INT)), 0) + 1 
                          FROM Acc_GeneralLedger 
                          WHERE SubsidiaryLedgerCodeNo = '10200400003'");

                    string nextCode = $"10200400003{nextSeq:D5}";

                    await connection.ExecuteAsync(
                        @"INSERT INTO Acc_GeneralLedger (
                            GroupLedgerCodeNo,
                            ControlLedgerCodeNo,
                            SubControlLedgerCodeNo,
                            SubsidiaryLedgerCodeNo,
                            GeneralLedgerCodeNo,
                            GeneralLedgerName,
                            ShortName,
                            OpeningBalance,
                            TrType,
                            OpeningDate,
                            IsActive,
                            Luser,
                            Ldate
                        ) VALUES (
                            '1',
                            '102',
                            '102004',
                            '10200400003',
                            @GeneralLedgerCodeNo,
                            @GeneralLedgerName,
                            @ShortName,
                            0,
                            'DR',
                            GETDATE(),
                            '1',
                            @Luser,
                            GETDATE()
                        )",
                        new
                        {
                            GeneralLedgerCodeNo = nextCode,
                            GeneralLedgerName = accNo,
                            ShortName = shortName,
                            Luser = user
                        });
                }
                else
                {
                    await connection.ExecuteAsync(
                        @"UPDATE Acc_GeneralLedger 
                          SET GeneralLedgerName = @AccNo,
                              ShortName = @ShortName,
                              ModifyDate = GETDATE()
                          WHERE GeneralLedgerCodeNo = @Code",
                        new
                        {
                            AccNo = accNo,
                            ShortName = shortName,
                            Code = existingCode
                        });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error EnsureBankAccountGeneralLedgerAsync: {ex}");
            }
        }

        #region Delete
        public bool DeleteLeaveType(string id)
        {
            var entity = GetLeaveType(id);
            if (entity != null)
            {
                bankAccountepository.Delete(entity);
                return true;
            }
            return false;
        }

        public CoreBankAccountInformation GetLeaveType(string code)
        {
            return bankAccountepository.GetById(code);
        }

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "AccInfoID", "AccountName", "AccountNo" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                bankAccountepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await bankAccountepository.BeginTransactionAsync();

            try
            {
                var entity = await bankAccountepository.All().Where(x => ids.Contains(x.AccInfoId)).ToListAsync();

                bankAccountepository.Delete(entity);
                model.tableName = bankAccountepository.GetTableName();
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await bankAccountepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await bankAccountepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }
        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await bankAccountepository.All().AnyAsync(x => x.AccInfoId == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await bankAccountepository.All().AnyAsync(x => x.AccountName == name);
        }

        public async Task<bool> IsExistAsync(string accountName, string typeCode, string accountNo, string bankId, string branchId)
        {
            return await bankAccountepository.All().AnyAsync(x => x.AccountName == accountName && x.AccountNo == accountNo && x.BankId == bankId && x.BranchId == branchId &&
                (string.IsNullOrEmpty(typeCode) || x.AccInfoId != typeCode));
        }
        #endregion

        #region Permission
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Account Information" || x.Title == "BankAccountInfo" || x.Title == "Bank Account" || x.Title == "A/C Info" || x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Account Information" || x.Title == "BankAccountInfo" || x.Title == "Bank Account" || x.Title == "A/C Info" || x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Account Information" || x.Title == "BankAccountInfo" || x.Title == "Bank Account" || x.Title == "A/C Info" || x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Bank Account Information" || x.Title == "BankAccountInfo" || x.Title == "Bank Account" || x.Title == "A/C Info" || x.Title == "Bank Information" || x.Title == "Bank Informtion " || x.Title == "BankInformations" || x.Title == "Bank Information "))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }

        public async Task<string> GenerateNexCode()
        {
            var code = await bankAccountepository.GetAllAsync();
            var lastCode = code.Max(x => x.AccInfoId);
            int nextCode = 1;
            if (!string.IsNullOrEmpty(lastCode))
            {
                int lastNumber = int.Parse(lastCode.TrimStart('0'));
                lastNumber++;
                nextCode = lastNumber;
            }
            return nextCode.ToString("D2");
        }
        #endregion
    }
}