using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentReceiveEntry;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.PaymentReceiveEntry
{
    public class PaymentReceiveEntryService : IPaymentReceiveEntryService
    {
        private readonly string _connectionString;
        private readonly IRepository<CoreAccessCode> _accessCodeRepository;
        private readonly ICommonService _commonService;
        private readonly IDeleteHistoryService _deleteHistoryService;
        private readonly IMemoryCache _cache;

        private const string TableName = "PaymentReceive";
        private const string DetailsTableName = "PaymentReceiveDetails";
        private const string CustomerListCacheKey = "PaymentReceive_CustomerList";

        public PaymentReceiveEntryService(
            IConfiguration configuration,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService,
            IMemoryCache cache)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? throw new InvalidOperationException("Connection string 'connection' not found.");
            _accessCodeRepository = accessCodeRepository;
            _commonService = commonService;
            _deleteHistoryService = deleteHistoryService;
            _cache = cache;
        }

        #region Stored Procedures
        public async Task<List<CustomerPaymentListViewModel>> GetCustomerListForPaymentReceiveAsync()
        {
            if (_cache.TryGetValue(CustomerListCacheKey, out List<CustomerPaymentListViewModel>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                using var con = new SqlConnection(_connectionString);
                var list = (await con.QueryAsync<CustomerPaymentListViewModel>(
                    "Proc_CustomerListForPaymentReceive",
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 15
                )).ToList();

                _cache.Set(CustomerListCacheKey, list, TimeSpan.FromSeconds(30));
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Proc_CustomerListForPaymentReceive: {ex.Message}");
                return new List<CustomerPaymentListViewModel>();
            }
        }

        public async Task<List<CustomerDueInvoiceViewModel>> GetCustomerDueInvoiceListAsync(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                    return new List<CustomerDueInvoiceViewModel>();

                var cleanId = customerId.Trim();
                var cacheKey = $"DueInvoices_{cleanId}";
                if (_cache.TryGetValue(cacheKey, out List<CustomerDueInvoiceViewModel>? cached) && cached != null)
                {
                    return cached;
                }

                using var con = new SqlConnection(_connectionString);
                var list = (await con.QueryAsync<CustomerDueInvoiceViewModel>(
                    "prc_GetCustomerDueInvoiceList",
                    new { CustomerId = cleanId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 15
                )).ToList();

                _cache.Set(cacheKey, list, TimeSpan.FromSeconds(30));
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in prc_GetCustomerDueInvoiceList: {ex.Message}");
                return new List<CustomerDueInvoiceViewModel>();
            }
        }

        public async Task<List<PaymentReceivedGridItemViewModel>> GetGridPaymentReceivedAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var cacheKey = $"GridPaymentReceived_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";
                if (_cache.TryGetValue(cacheKey, out List<PaymentReceivedGridItemViewModel>? cached) && cached != null)
                {
                    return cached;
                }

                using var con = new SqlConnection(_connectionString);
                var list = (await con.QueryAsync<PaymentReceivedGridItemViewModel>(
                    "prc_LoadGridPaymentReceived",
                    new { FromDate = fromDate, ToDate = toDate },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 15
                )).ToList();

                _cache.Set(cacheKey, list, TimeSpan.FromSeconds(15));
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in prc_LoadGridPaymentReceived: {ex.Message}");
                return new List<PaymentReceivedGridItemViewModel>();
            }
        }

        public async Task<PaymentHistorySummaryViewModel?> GetPaymentHistorySummaryAsync(string customerCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerCode))
                    return null;

                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<PaymentHistorySummaryViewModel>(
                    "Prc_Api_PaymentHistorySummery",
                    new { CustomerCode = customerCode },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Prc_Api_PaymentHistorySummery: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PaymentListItemViewModel>> GetPaymentListAsync(string customerCode, DateTime fromDate, DateTime toDate, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerCode))
                    return new List<PaymentListItemViewModel>();

                using var con = new SqlConnection(_connectionString);
                var list = await con.QueryAsync<PaymentListItemViewModel>(
                    "Prc_Api_PaymentList",
                    new
                    {
                        CustomerCode = customerCode,
                        FromDate = fromDate,
                        ToDate = toDate,
                        PageIndex = pageIndex,
                        PageSize = pageSize
                    },
                    commandType: CommandType.StoredProcedure
                );
                return list.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Prc_Api_PaymentList: {ex.Message}");
                return new List<PaymentListItemViewModel>();
            }
        }
        #endregion

        #region CRUD
        public async Task<List<PaymentReceiveEntrySetupViewModel>> GetAllAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        p.AutoID as Tc,
                        p.PaymentReceiveID,
                        p.PaymentReceiveDate,
                        p.MRNo,
                        p.MRDate,
                        p.CustomerID,
                        c.CustomerName,
                        c.CustomerAddress as BillingAddress,
                        p.AgainstBillList,
                        p.PaymentTypeID,
                        pt.PaymentType as PaymentTypeName,
                        p.PaymentModeCode,
                        pm.PaymentModeName,
                        p.Amount,
                        p.BankFeeOnlineCharge,
                        p.AIT,
                        p.AITChallanNo,
                        p.AITInvoiceNo,
                        p.AITChallanUpload,
                        p.Remarks,
                        p.Receivedby,
                        FORMAT(p.LDate, 'dd/MM/yyyy') as CreationDate,
                        FORMAT(p.ModifyDate, 'dd/MM/yyyy') as LastUpdateDate,
                        p.LDate,
                        p.ModifyDate,
                        p.LUser,
                        p.CompanyCode,
                        p.EmployeeID
                    FROM PaymentReceive p
                    LEFT OUTER JOIN Sales_Customer c ON p.CustomerID = c.CustomerID
                    LEFT OUTER JOIN Sales_Def_PaymentType pt ON p.PaymentTypeID = pt.PaymentTypeID
                    LEFT OUTER JOIN Sales_Def_PaymentMode pm ON p.PaymentModeCode = pm.PaymentModeID
                    ORDER BY p.AutoID DESC";

                var list = await con.QueryAsync<PaymentReceiveEntrySetupViewModel>(sql);
                return list.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                return new List<PaymentReceiveEntrySetupViewModel>();
            }
        }

        public async Task<PaymentReceiveEntrySetupViewModel?> GetByIdAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return null;

                using var con = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        p.AutoID as Tc,
                        p.PaymentReceiveID,
                        p.PaymentReceiveDate,
                        p.MRNo,
                        p.MRDate,
                        p.CustomerID,
                        c.CustomerName,
                        c.CustomerAddress as BillingAddress,
                        p.AgainstBillList,
                        p.PaymentTypeID,
                        pt.PaymentType as PaymentTypeName,
                        p.PaymentModeCode,
                        pm.PaymentModeName,
                        p.Amount,
                        p.BankFeeOnlineCharge,
                        p.AIT,
                        p.AITChallanNo,
                        p.AITInvoiceNo,
                        p.AITChallanUpload,
                        p.Remarks,
                        p.Receivedby,
                        FORMAT(p.LDate, 'dd/MM/yyyy') as CreationDate,
                        FORMAT(p.ModifyDate, 'dd/MM/yyyy') as LastUpdateDate,
                        p.LDate,
                        p.ModifyDate,
                        p.LUser,
                        p.LIP,
                        p.LMAC,
                        p.CompanyCode,
                        p.EmployeeID
                    FROM PaymentReceive p
                    LEFT OUTER JOIN Sales_Customer c ON p.CustomerID = c.CustomerID
                    LEFT OUTER JOIN Sales_Def_PaymentType pt ON p.PaymentTypeID = pt.PaymentTypeID
                    LEFT OUTER JOIN Sales_Def_PaymentMode pm ON p.PaymentModeCode = pm.PaymentModeID
                    WHERE p.PaymentReceiveID = @Code OR CAST(p.AutoID as nvarchar) = @Code";

                var model = await con.QueryFirstOrDefaultAsync<PaymentReceiveEntrySetupViewModel>(sql, new { Code = code });
                if (model != null)
                {
                    const string detailsSql = @"
                        SELECT InvoiceNo, Amount 
                        FROM PaymentReceiveDetails 
                        WHERE PaymentReceiveID = @ReceiveId";

                    var details = await con.QueryAsync<PaymentReceiveInvoiceItem>(detailsSql, new { ReceiveId = model.PaymentReceiveId });
                    model.InvoiceItems = details.ToList();
                }

                return model;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SaveAsync(PaymentReceiveEntrySetupViewModel entityVM)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.OpenAsync();
                using var tran = con.BeginTransaction();

                try
                {
                    if (string.IsNullOrWhiteSpace(entityVM.PaymentReceiveId))
                    {
                        entityVM.PaymentReceiveId = await GetNextPaymentReceiveNoAsync(con, tran);
                    }

                    if (string.IsNullOrWhiteSpace(entityVM.MrNo))
                    {
                        entityVM.MrNo = await GetNextMrNoAsync(con, tran);
                    }

                    const string insertHeaderSql = @"
                        INSERT INTO PaymentReceive (
                            PaymentReceiveID, PaymentReceiveDate, MRNo, MRDate, CustomerID,
                            AgainstBillList, PaymentTypeID, PaymentModeCode, Amount, BankFeeOnlineCharge,
                            AIT, AITChallanNo, AITInvoiceNo, AITChallanUpload,
                            Remarks, Receivedby, LUser, LDate, LIP, LMAC, CompanyCode, EmployeeID,
                            ChequeNo, ChequeBank, IsChequeVerified, TransferredBank, TransferredACName,
                            TransferredACNo, DepositedACNo, TransfterMobileNo, DepositMobileNo
                        ) VALUES (
                            @PaymentReceiveId, @PaymentReceiveDate, @MrNo, @MrDate, @CustomerId,
                            @AgainstBillList, @PaymentTypeId, @PaymentModeCode, @Amount, @BankFeeOnlineCharge,
                            @AIT, @AITChallanNo, @AITInvoiceNo, @AITChallanUpload,
                            @Remarks, @Receivedby, @Luser, GETDATE(), @Lip, @Lmac, @CompanyCode, @EmployeeId,
                            @ChequeNo, @ChequeBank, @IsChequeVerified, @TransferredBank, @TransferredACName,
                            @TransferredACNo, @DepositedACNo, @TransfterMobileNo, @DepositMobileNo
                        );
                        SELECT SCOPE_IDENTITY();";

                    var newId = await con.ExecuteScalarAsync<decimal>(insertHeaderSql, new
                    {
                        PaymentReceiveId = entityVM.PaymentReceiveId ?? string.Empty,
                        PaymentReceiveDate = entityVM.PaymentReceiveDate ?? DateTime.Now,
                        MrNo = entityVM.MrNo ?? string.Empty,
                        MrDate = entityVM.MrDate ?? DateTime.Now,
                        CustomerId = entityVM.CustomerId ?? string.Empty,
                        AgainstBillList = entityVM.AgainstBillList ?? string.Empty,
                        PaymentTypeId = string.IsNullOrWhiteSpace(entityVM.PaymentTypeId) ? "01" : entityVM.PaymentTypeId,
                        PaymentModeCode = string.IsNullOrWhiteSpace(entityVM.PaymentModeCode) ? "001" : entityVM.PaymentModeCode,
                        Amount = entityVM.Amount ?? 0,
                        BankFeeOnlineCharge = entityVM.BankFeeOnlineCharge ?? 0,
                        AIT = entityVM.AIT ?? 0,
                        AITChallanNo = entityVM.AITChallanNo ?? string.Empty,
                        AITInvoiceNo = entityVM.AITInvoiceNo ?? string.Empty,
                        AITChallanUpload = entityVM.AITChallanUpload ?? string.Empty,
                        Remarks = entityVM.Remarks ?? string.Empty,
                        Receivedby = entityVM.Receivedby ?? string.Empty,
                        Luser = entityVM.Luser ?? string.Empty,
                        Lip = entityVM.Lip ?? string.Empty,
                        Lmac = entityVM.Lmac ?? string.Empty,
                        CompanyCode = string.IsNullOrWhiteSpace(entityVM.CompanyCode) ? "001" : entityVM.CompanyCode,
                        EmployeeId = entityVM.EmployeeId ?? string.Empty,
                        ChequeNo = string.Empty,
                        ChequeBank = string.Empty,
                        IsChequeVerified = string.Empty,
                        TransferredBank = string.Empty,
                        TransferredACName = string.Empty,
                        TransferredACNo = string.Empty,
                        DepositedACNo = string.Empty,
                        TransfterMobileNo = string.Empty,
                        DepositMobileNo = string.Empty
                    }, tran);

                    entityVM.Tc = newId;

                    // Insert detail invoices
                    await SaveDetailsAsync(con, tran, entityVM);

                    tran.Commit();
                    _cache.Remove(CustomerListCacheKey);
                    if (!string.IsNullOrWhiteSpace(entityVM.CustomerId))
                    {
                        _cache.Remove($"DueInvoices_{entityVM.CustomerId.Trim()}");
                    }
                    return true;
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(PaymentReceiveEntrySetupViewModel entityVM)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.OpenAsync();
                using var tran = con.BeginTransaction();

                try
                {
                    const string updateHeaderSql = @"
                        UPDATE PaymentReceive SET
                            PaymentReceiveDate = @PaymentReceiveDate,
                            MRNo = @MrNo,
                            MRDate = @MrDate,
                            CustomerID = @CustomerId,
                            AgainstBillList = @AgainstBillList,
                            PaymentTypeID = @PaymentTypeId,
                            PaymentModeCode = @PaymentModeCode,
                            Amount = @Amount,
                            BankFeeOnlineCharge = @BankFeeOnlineCharge,
                            AIT = @AIT,
                            AITChallanNo = @AITChallanNo,
                            AITInvoiceNo = @AITInvoiceNo,
                            AITChallanUpload = @AITChallanUpload,
                            Remarks = @Remarks,
                            Receivedby = @Receivedby,
                            ModifyDate = GETDATE(),
                            LUser = @Luser,
                            LIP = @Lip,
                            LMAC = @Lmac,
                            CompanyCode = @CompanyCode,
                            EmployeeID = @EmployeeId,
                            ChequeNo = ISNULL(ChequeNo, ''),
                            ChequeBank = ISNULL(ChequeBank, ''),
                            IsChequeVerified = ISNULL(IsChequeVerified, ''),
                            TransferredBank = ISNULL(TransferredBank, ''),
                            TransferredACName = ISNULL(TransferredACName, ''),
                            TransferredACNo = ISNULL(TransferredACNo, ''),
                            DepositedACNo = ISNULL(DepositedACNo, ''),
                            TransfterMobileNo = ISNULL(TransfterMobileNo, ''),
                            DepositMobileNo = ISNULL(DepositMobileNo, '')
                        WHERE PaymentReceiveID = @PaymentReceiveId OR (AutoID = @Tc AND @Tc > 0)";

                    await con.ExecuteAsync(updateHeaderSql, new
                    {
                        PaymentReceiveId = entityVM.PaymentReceiveId ?? string.Empty,
                        Tc = entityVM.Tc ?? 0,
                        PaymentReceiveDate = entityVM.PaymentReceiveDate ?? DateTime.Now,
                        MrNo = entityVM.MrNo ?? string.Empty,
                        MrDate = entityVM.MrDate ?? DateTime.Now,
                        CustomerId = entityVM.CustomerId ?? string.Empty,
                        AgainstBillList = entityVM.AgainstBillList ?? string.Empty,
                        PaymentTypeId = string.IsNullOrWhiteSpace(entityVM.PaymentTypeId) ? "01" : entityVM.PaymentTypeId,
                        PaymentModeCode = string.IsNullOrWhiteSpace(entityVM.PaymentModeCode) ? "001" : entityVM.PaymentModeCode,
                        Amount = entityVM.Amount ?? 0,
                        BankFeeOnlineCharge = entityVM.BankFeeOnlineCharge ?? 0,
                        AIT = entityVM.AIT ?? 0,
                        AITChallanNo = entityVM.AITChallanNo ?? string.Empty,
                        AITInvoiceNo = entityVM.AITInvoiceNo ?? string.Empty,
                        AITChallanUpload = entityVM.AITChallanUpload ?? string.Empty,
                        Remarks = entityVM.Remarks ?? string.Empty,
                        Receivedby = entityVM.Receivedby ?? string.Empty,
                        Luser = entityVM.Luser ?? string.Empty,
                        Lip = entityVM.Lip ?? string.Empty,
                        Lmac = entityVM.Lmac ?? string.Empty,
                        CompanyCode = string.IsNullOrWhiteSpace(entityVM.CompanyCode) ? "001" : entityVM.CompanyCode,
                        EmployeeId = entityVM.EmployeeId ?? string.Empty
                    }, tran);

                    // Delete existing details
                    const string deleteDetailsSql = @"DELETE FROM PaymentReceiveDetails WHERE PaymentReceiveID = @PaymentReceiveId";
                    await con.ExecuteAsync(deleteDetailsSql, new { PaymentReceiveId = entityVM.PaymentReceiveId ?? string.Empty }, tran);

                    // Re-insert detail invoices
                    await SaveDetailsAsync(con, tran, entityVM);

                    tran.Commit();
                    _cache.Remove(CustomerListCacheKey);
                    if (!string.IsNullOrWhiteSpace(entityVM.CustomerId))
                    {
                        _cache.Remove($"DueInvoices_{entityVM.CustomerId.Trim()}");
                    }
                    return true;
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                return false;
            }
        }

        private async Task SaveDetailsAsync(SqlConnection con, SqlTransaction tran, PaymentReceiveEntrySetupViewModel entityVM)
        {
            var invoiceItems = new List<PaymentReceiveInvoiceItem>();

            if (!string.IsNullOrWhiteSpace(entityVM.SelectedInvoicesJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<PaymentReceiveInvoiceItem>>(entityVM.SelectedInvoicesJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (parsed != null && parsed.Count > 0)
                    {
                        invoiceItems.AddRange(parsed);
                    }
                }
                catch { }
            }

            if (invoiceItems.Count == 0 && entityVM.InvoiceItems != null && entityVM.InvoiceItems.Count > 0)
            {
                invoiceItems.AddRange(entityVM.InvoiceItems);
            }

            if (invoiceItems.Count == 0 && !string.IsNullOrWhiteSpace(entityVM.AgainstBillList))
            {
                var bills = entityVM.AgainstBillList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                decimal perBill = bills.Length > 0 ? (entityVM.Amount ?? 0) / bills.Length : (entityVM.Amount ?? 0);
                foreach (var b in bills)
                {
                    invoiceItems.Add(new PaymentReceiveInvoiceItem
                    {
                        InvoiceNo = b.Trim(),
                        Amount = perBill
                    });
                }
            }

            const string insertDetailSql = @"
                INSERT INTO PaymentReceiveDetails (PaymentReceiveID, InvoiceNo, Amount)
                VALUES (@PaymentReceiveId, @InvoiceNo, @Amount)";

            foreach (var item in invoiceItems)
            {
                if (!string.IsNullOrWhiteSpace(item.InvoiceNo))
                {
                    await con.ExecuteAsync(insertDetailSql, new
                    {
                        PaymentReceiveId = entityVM.PaymentReceiveId ?? string.Empty,
                        InvoiceNo = item.InvoiceNo?.Trim() ?? string.Empty,
                        Amount = item.Amount
                    }, tran);
                }
            }
        }

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No data found to delete");

            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.OpenAsync();
                using var tran = con.BeginTransaction();

                try
                {
                    var cleanIds = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
                    if (!cleanIds.Any())
                        return (false, "No data found to delete");

                    // 1. Resolve matching PaymentReceiveIDs (both from PaymentReceiveID directly and from AutoID)
                    var directIds = (await con.QueryAsync<string>(
                        @"SELECT DISTINCT PaymentReceiveID FROM PaymentReceive WHERE PaymentReceiveID IN @Ids",
                        new { Ids = cleanIds }, tran)).ToList();

                    var autoIds = (await con.QueryAsync<string>(
                        @"SELECT DISTINCT PaymentReceiveID FROM PaymentReceive WHERE CAST(AutoID as nvarchar(50)) IN @Ids",
                        new { Ids = cleanIds }, tran)).ToList();

                    var allReceiveIds = cleanIds.Union(directIds).Union(autoIds)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();

                    if (allReceiveIds.Any())
                    {
                        const string deleteDetailsSql = @"DELETE FROM PaymentReceiveDetails WHERE PaymentReceiveID IN @AllIds";
                        await con.ExecuteAsync(deleteDetailsSql, new { AllIds = allReceiveIds }, tran);

                        const string deleteHeaderSql = @"DELETE FROM PaymentReceive WHERE PaymentReceiveID IN @AllIds OR CAST(AutoID as nvarchar(50)) IN @CleanIds";
                        await con.ExecuteAsync(deleteHeaderSql, new { AllIds = allReceiveIds, CleanIds = cleanIds }, tran);
                    }

                    tran.Commit();
                    _cache.Remove(CustomerListCacheKey);
                    return (true, "Deleted Successfully");
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteTab: {ex.Message}");
                return (false, $"Delete Failed: {ex.Message}");
            }
        }

        public async Task<bool> IsExistAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                using var con = new SqlConnection(_connectionString);
                const string sql = @"SELECT COUNT(1) FROM PaymentReceive WHERE PaymentReceiveID = @Code";
                var count = await con.ExecuteScalarAsync<int>(sql, new { Code = code });
                return count > 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Sequence Generators
        public async Task<string> GetNextPaymentReceiveNoAsync()
        {
            using var con = new SqlConnection(_connectionString);
            return await GetNextPaymentReceiveNoAsync(con, null);
        }

        private async Task<string> GetNextPaymentReceiveNoAsync(SqlConnection con, SqlTransaction? tran)
        {
            try
            {
                var yearPrefix = DateTime.Now.ToString("yy"); // e.g. "26"
                var prefix = $"PRN{yearPrefix}";

                const string sql = @"
                    SELECT TOP 1 PaymentReceiveID 
                    FROM PaymentReceive 
                    WHERE PaymentReceiveID LIKE @Prefix + '%' 
                    ORDER BY PaymentReceiveID DESC";

                var maxCode = await con.QueryFirstOrDefaultAsync<string>(sql, new { Prefix = prefix }, tran);
                if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length < 9)
                {
                    return $"{prefix}000001";
                }

                var numPart = maxCode.Substring(prefix.Length);
                if (int.TryParse(numPart, out int seq))
                {
                    return $"{prefix}{(seq + 1):D6}";
                }

                return $"{prefix}000001";
            }
            catch
            {
                var yearPrefix = DateTime.Now.ToString("yy");
                return $"PRN{yearPrefix}000001";
            }
        }

        public async Task<string> GetNextMrNoAsync()
        {
            using var con = new SqlConnection(_connectionString);
            return await GetNextMrNoAsync(con, null);
        }

        private async Task<string> GetNextMrNoAsync(SqlConnection con, SqlTransaction? tran)
        {
            try
            {
                var yearPrefix = DateTime.Now.ToString("yy"); // e.g. "26"
                var prefix = $"MRN{yearPrefix}";

                const string sql = @"
                    SELECT TOP 1 MRNo 
                    FROM PaymentReceive 
                    WHERE MRNo LIKE @Prefix + '%' 
                    ORDER BY MRNo DESC";

                var maxCode = await con.QueryFirstOrDefaultAsync<string>(sql, new { Prefix = prefix }, tran);
                if (string.IsNullOrWhiteSpace(maxCode) || maxCode.Length < 9)
                {
                    return $"{prefix}000001";
                }

                var numPart = maxCode.Substring(prefix.Length);
                if (int.TryParse(numPart, out int seq))
                {
                    return $"{prefix}{(seq + 1):D6}";
                }

                return $"{prefix}000001";
            }
            catch
            {
                var yearPrefix = DateTime.Now.ToString("yy");
                return $"MRN{yearPrefix}000001";
            }
        }
        #endregion

        #region Permissions
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            return await _accessCodeRepository.All()
                .AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Payment Receive Entry" || x.Title == "Payment Receive" || x.MenuId == "079" || x.MenuId == "79" || x.Title.Contains("Payment Receive")) && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            return await _accessCodeRepository.All()
                .AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Payment Receive Entry" || x.Title == "Payment Receive" || x.MenuId == "079" || x.MenuId == "79" || x.Title.Contains("Payment Receive")) && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            return await _accessCodeRepository.All()
                .AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Payment Receive Entry" || x.Title == "Payment Receive" || x.MenuId == "079" || x.MenuId == "79" || x.Title.Contains("Payment Receive")) && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            return await _accessCodeRepository.All()
                .AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Payment Receive Entry" || x.Title == "Payment Receive" || x.MenuId == "079" || x.MenuId == "79" || x.Title.Contains("Payment Receive")) && x.CheckDelete);
        }
        #endregion
    }
}
