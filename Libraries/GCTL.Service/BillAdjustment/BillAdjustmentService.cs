using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ClearAndF.BillAdjustment;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GCTL.Service.BillAdjustment
{
    public class BillAdjustmentService : IBillAdjustmentService
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<BillAdjustmentService> _logger;

        public BillAdjustmentService(IDbConnection dbConnection, ILogger<BillAdjustmentService> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        // =========================================================================
        // 1. Get Approved Bills (Bottom Grid)
        // =========================================================================
        public async Task<List<ApprovedBillListDto>> GetApprovedBillsAsync(
            DateTime? dateFrom, 
            DateTime? dateTo, 
            string? searchTerm = null, 
            string? companyCode = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@FromDate", dateFrom, DbType.DateTime);
                parameters.Add("@ToDate", dateTo, DbType.DateTime);
                parameters.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String);
                parameters.Add("@CompanyCode", string.IsNullOrWhiteSpace(companyCode) ? null : companyCode.Trim(), DbType.String);

                try
                {
                    var bills = await _dbConnection.QueryAsync<ApprovedBillListDto>(
                        "dbo.Proc_LoadApprovedBillListForAdjust",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 90
                    );

                    return bills.ToList();
                }
                catch (Exception ex) when (IsProcedureNotFound(ex, "Proc_LoadApprovedBillListForAdjust"))
                {
                    _logger.LogWarning("Stored procedure dbo.Proc_LoadApprovedBillListForAdjust not found in database. Executing direct fallback query.");
                    return await GetApprovedBillsDirectQueryAsync(dateFrom, dateTo, searchTerm, companyCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApprovedBillsAsync. DateFrom: {From}, DateTo: {To}", dateFrom, dateTo);
                throw;
            }
        }

        private async Task<List<ApprovedBillListDto>> GetApprovedBillsDirectQueryAsync(
            DateTime? dateFrom, 
            DateTime? dateTo, 
            string? searchTerm, 
            string? companyCode)
        {
            var sql = @"
                IF EXISTS (SELECT 1 FROM dbo.FinalBillInfo_ForBillAdjust WITH (NOLOCK))
                BEGIN
                    SELECT DISTINCT
                        fb.BillNo,
                        COALESCE(TRY_CAST(fb.BillDate AS DATETIME), TRY_CONVERT(DATETIME, fb.BillDate, 103), TRY_CONVERT(DATETIME, fb.BillDate, 120)) AS BillDate,
                        ISNULL(fb.JobNo, '')                            AS JobNo,
                        ISNULL(fb.CustomerId, '')                       AS CustomerId,
                        ISNULL(fb.CustomerName, '')                     AS CustomerName,
                        ISNULL(fb.Beno, '')                             AS BeNo,
                        COALESCE(TRY_CAST(fb.DeliveryDate AS DATETIME), TRY_CONVERT(DATETIME, fb.DeliveryDate, 103), TRY_CONVERT(DATETIME, fb.DeliveryDate, 120)) AS DeliveryDate,
                        ISNULL(fb.TotalAmount, 0)                       AS TotalAmount,
                        ISNULL(fb.AdvanceAmount, 0)                     AS AdvanceAmount,
                        (ISNULL(fb.TotalAmount, 0) - ISNULL(fb.AdvanceAmount, 0)) AS NetPayable,
                        ISNULL(fb.ExpenseType, 'Sea Export (CTG)')      AS ServiceType,
                        ISNULL(v.VoucherNo, '')                         AS VoucherNo,
                        ISNULL(adj.AdjustmentType, '')                  AS AdjustmentType,
                        ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
                        adj.AdjustmentDate                              AS AdjustmentDate
                    FROM dbo.FinalBillInfo_ForBillAdjust fb WITH (NOLOCK)
                    OUTER APPLY (
                        SELECT TOP 1 v.VoucherNo 
                        FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
                        WHERE v.InvoiceNo = fb.BillNo 
                        ORDER BY v.autoId DESC
                    ) v
                    OUTER APPLY (
                        SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate 
                        FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
                        WHERE adj.BillNo = fb.BillNo 
                        ORDER BY adj.TC DESC
                    ) adj
                    WHERE (@FromDate IS NULL OR COALESCE(TRY_CAST(fb.BillDate AS DATE), TRY_CONVERT(DATE, fb.BillDate, 103), TRY_CONVERT(DATE, fb.BillDate, 105), TRY_CONVERT(DATE, fb.BillDate, 120), TRY_CONVERT(DATE, fb.BillDate, 106)) >= CAST(@FromDate AS DATE))
                      AND (@ToDateExcl IS NULL OR COALESCE(TRY_CAST(fb.BillDate AS DATE), TRY_CONVERT(DATE, fb.BillDate, 103), TRY_CONVERT(DATE, fb.BillDate, 105), TRY_CONVERT(DATE, fb.BillDate, 120), TRY_CONVERT(DATE, fb.BillDate, 106)) < @ToDateExcl)
                      AND (
                            @Search IS NULL 
                            OR fb.BillNo LIKE '%' + @Search + '%'
                            OR fb.JobNo LIKE '%' + @Search + '%'
                            OR fb.CustomerName LIKE '%' + @Search + '%'
                            OR fb.Beno LIKE '%' + @Search + '%'
                          )
                      AND (
                            @CompanyCode IS NULL 
                            OR @CompanyCode = '' 
                            OR fb.CompanyName = @CompanyCode 
                            OR fb.CompanyName LIKE '%' + @CompanyCode + '%'
                          )
                    ORDER BY BillDate DESC, fb.BillNo DESC;
                END
                ELSE
                BEGIN
                    SELECT DISTINCT
                        b.BillNo,
                        b.BillDate                                      AS BillDate,
                        ISNULL(b.JobNo, '')                             AS JobNo,
                        ISNULL(b.CustomerID, '')                        AS CustomerId,
                        ISNULL(c.CustomerName, '')                      AS CustomerName,
                        ISNULL(b.BENo, '')                              AS BeNo,
                        COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS DeliveryDate,
                        ISNULL(b.BillAmount, 0)                         AS TotalAmount,
                        0.00                                            AS AdvanceAmount,
                        ISNULL(b.BillAmount, 0)                         AS NetPayable,
                        COALESCE(et.ExpenseType, 'Sea Export (CTG)')    AS ServiceType,
                        ISNULL(v.VoucherNo, '')                         AS VoucherNo,
                        ISNULL(adj.AdjustmentType, '')                  AS AdjustmentType,
                        ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
                        adj.AdjustmentDate                              AS AdjustmentDate
                    FROM dbo.CF_BillEntry b WITH (NOLOCK)
                    LEFT JOIN dbo.Sales_Customer c WITH (NOLOCK) ON b.CustomerID = c.CustomerID
                    LEFT JOIN dbo.CF_Def_ExpenseType et WITH (NOLOCK) ON b.ExpenseTypeID = et.ExpenseTypeID
                    LEFT JOIN dbo.Sep_Documentation s WITH (NOLOCK) ON b.JobNo = s.JobNo
                    OUTER APPLY (
                        SELECT TOP 1 v.VoucherNo 
                        FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
                        WHERE v.InvoiceNo = b.BillNo 
                        ORDER BY v.autoId DESC
                    ) v
                    OUTER APPLY (
                        SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate 
                        FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
                        WHERE adj.BillNo = b.BillNo 
                        ORDER BY adj.TC DESC
                    ) adj
                    WHERE (@FromDate IS NULL OR CAST(b.BillDate AS DATE) >= CAST(@FromDate AS DATE))
                      AND (@ToDateExcl IS NULL OR CAST(b.BillDate AS DATE) < @ToDateExcl)
                      AND (
                            @Search IS NULL 
                            OR b.BillNo LIKE '%' + @Search + '%'
                            OR b.JobNo LIKE '%' + @Search + '%'
                            OR c.CustomerName LIKE '%' + @Search + '%'
                            OR b.BENo LIKE '%' + @Search + '%'
                          )
                      AND (
                            @CompanyCode IS NULL 
                            OR @CompanyCode = '' 
                            OR b.CompanyCode = @CompanyCode
                          )
                    ORDER BY b.BillDate DESC, b.BillNo DESC;
                END";

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", dateFrom, DbType.DateTime);
            parameters.Add("@ToDateExcl", dateTo.HasValue ? dateTo.Value.Date.AddDays(1) : (DateTime?)null, DbType.DateTime);
            parameters.Add("@Search", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String);
            parameters.Add("@CompanyCode", string.IsNullOrWhiteSpace(companyCode) ? null : companyCode.Trim(), DbType.String);

            var bills = await _dbConnection.QueryAsync<ApprovedBillListDto>(
                sql,
                parameters,
                commandType: CommandType.Text,
                commandTimeout: 90
            );

            return bills.ToList();
        }

        // =========================================================================
        // 2. Get Bill Details by BillNo (Header + 2 Grids)
        // =========================================================================
        public async Task<BillAdjustmentScreenDataResponse?> GetBillDetailsByBillNoAsync(string billNo)
        {
            if (string.IsNullOrWhiteSpace(billNo))
                return null;

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@BillNo", billNo.Trim(), DbType.String);

                try
                {
                    using var multi = await _dbConnection.QueryMultipleAsync(
                        "dbo.proc_CF_BillInfoF_BillAdjust",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 90
                    );

                    var header = await multi.ReadFirstOrDefaultAsync<BillAdjustmentMasterDto>();
                    if (header == null)
                        return null;

                    var chcCharges = (await multi.ReadAsync<BillAdjustChcDetailDto>()).ToList();
                    var expenseDetails = (await multi.ReadAsync<BillAdjustExpenseDetailDto>()).ToList();

                    return new BillAdjustmentScreenDataResponse
                    {
                        Header = header,
                        ChcCharges = chcCharges,
                        ExpenseDetails = expenseDetails
                    };
                }
                catch (Exception ex) when (IsProcedureNotFound(ex, "proc_CF_BillInfoF_BillAdjust"))
                {
                    _logger.LogWarning("Stored procedure dbo.proc_CF_BillInfoF_BillAdjust not found in database. Executing direct fallback query.");
                    return await GetBillDetailsDirectQueryAsync(billNo.Trim());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bill adjustment details for BillNo: {BillNo}", billNo);
                throw;
            }
        }

        private async Task<BillAdjustmentScreenDataResponse?> GetBillDetailsDirectQueryAsync(string billNo)
        {
            var headerSql = @"
                IF EXISTS (SELECT 1 FROM dbo.FinalBillInfo_ForBillAdjust WITH (NOLOCK) WHERE BillNo = @BillNo)
                BEGIN
                    SELECT TOP 1
                        fb.BillNo,
                        'SLSL'                                          AS BillNoPrefix,
                        COALESCE(TRY_CAST(fb.BillDate AS DATETIME), TRY_CONVERT(DATETIME, fb.BillDate, 103), TRY_CONVERT(DATETIME, fb.BillDate, 120)) AS BillDate,
                        ISNULL(fb.JobNo, '')                            AS JobNo,
                        COALESCE(TRY_CAST(fb.BillDate AS DATETIME), TRY_CONVERT(DATETIME, fb.BillDate, 103), TRY_CONVERT(DATETIME, fb.BillDate, 120)) AS JobDate,
                        ISNULL(fb.BranchName, 'Main Branch')            AS Branch,
                        ISNULL(fb.ExpenseType, 'C&F')                   AS ServiceType,
                        ISNULL(fb.CustomerId, '')                       AS CustomerId,
                        ISNULL(fb.CustomerName, '')                     AS CustomerName,
                        ISNULL(fb.CustomerAddress, '')                  AS CustomerAddress,
                        ISNULL(fb.Address, '')                          AS CustomerDeliveryAddress,
                        ISNULL(fb.CustomerName, '')                     AS SupplierName,
                        'Chittagong Port'                               AS PortName,
                        COALESCE(TRY_CAST(fb.DocRecvDate AS DATETIME), TRY_CONVERT(DATETIME, fb.DocRecvDate, 103)) AS DocRecvDate,
                        ISNULL(fb.INVNo, '')                            AS LcNo,
                        0.00                                            AS LcValue,
                        'USD'                                           AS LcCurrency,
                        0.00                                            AS BdtRate,
                        1                                               AS NumberOfTransport,
                        0                                               AS NumberOfDemerageDay,
                        ISNULL(fb.IPNo, '')                             AS IpNo,
                        COALESCE(TRY_CAST(fb.IPDate AS DATETIME), TRY_CONVERT(DATETIME, fb.IPDate, 103)) AS IpDate,
                        ISNULL(fb.UndertakingNo, '')                    AS UndertakingNo,
                        COALESCE(TRY_CAST(fb.ArrivalDate AS DATETIME), TRY_CONVERT(DATETIME, fb.ArrivalDate, 103)) AS ArrivalDate,
                        ISNULL(fb.INVNo, '')                            AS InvNo,
                        COALESCE(TRY_CAST(fb.INVDate AS DATETIME), TRY_CONVERT(DATETIME, fb.INVDate, 103)) AS InvDate,
                        ISNULL(fb.Blno, '')                             AS BlNo,
                        COALESCE(TRY_CAST(fb.BLDate AS DATETIME), TRY_CONVERT(DATETIME, fb.BLDate, 103)) AS BlDate,
                        ISNULL(fb.Beno, '')                             AS BeNo,
                        COALESCE(TRY_CAST(fb.Bedate AS DATETIME), TRY_CONVERT(DATETIME, fb.Bedate, 103)) AS BeDate,
                        ISNULL(fb.EFRNo, '')                            AS EfrNo,
                        COALESCE(TRY_CAST(fb.EFRDate AS DATETIME), TRY_CONVERT(DATETIME, fb.EFRDate, 103)) AS EfrDate,
                        ISNULL(fb.ROTNo, '')                            AS RotNo,
                        ISNULL(fb.OFRNo, '')                            AS PoNo,
                        ISNULL(fb.ContainerNo, '')                      AS ContainerNo,
                        ISNULL(fb.MaterialDescription, '')              AS MaterialDescription,
                        ISNULL(fb.Quantity, 0)                          AS Quantity,
                        'PCS'                                           AS QUnitType,
                        ISNULL(fb.docWeight, 0)                         AS DocWeight,
                        'KG'                                            AS WUnitType,
                        ISNULL(fb.JRSNo, '')                            AS Fwdr,
                        ISNULL(fb.ROTNo, '')                            AS VesselNo,
                        COALESCE(TRY_CAST(fb.ArrivalDate AS DATETIME), TRY_CONVERT(DATETIME, fb.ArrivalDate, 103)) AS EtaDate,
                        COALESCE(TRY_CAST(fb.ArrivalDate AS DATETIME), TRY_CONVERT(DATETIME, fb.ArrivalDate, 103)) AS AtaDate,
                        COALESCE(TRY_CAST(fb.ArrivalDate AS DATETIME), TRY_CONVERT(DATETIME, fb.ArrivalDate, 103)) AS EtbDate,
                        COALESCE(TRY_CAST(fb.ArrivalDate AS DATETIME), TRY_CONVERT(DATETIME, fb.ArrivalDate, 103)) AS AtbDate,
                        COALESCE(TRY_CAST(fb.DeliveryDate AS DATETIME), TRY_CONVERT(DATETIME, fb.DeliveryDate, 103)) AS UnStuffingDate,
                        COALESCE(TRY_CAST(fb.DeliveryDate AS DATETIME), TRY_CONVERT(DATETIME, fb.DeliveryDate, 103)) AS DateOfClearance,
                        ISNULL(fb.JRSNo, '')                            AS ConsoleNo,
                        ISNULL(fb.Destination, '')                      AS Destination,
                        COALESCE(TRY_CAST(fb.DeliveryDate AS DATETIME), TRY_CONVERT(DATETIME, fb.DeliveryDate, 103)) AS DeliveryDate,
                        ISNULL(fb.TotalAmount, 0)                       AS GrossAmount,
                        ISNULL(fb.AdvanceAmount, 0)                     AS AdvanceAmount,
                        (ISNULL(fb.TotalAmount, 0) - ISNULL(fb.AdvanceAmount, 0)) AS NetAmount,
                        ISNULL(fb.TotalCfbillAmount, 0)                 AS TotalCfBillAmount,
                        ISNULL(fb.TotalTransportBillAmount, 0)          AS TotalTransportBillAmount,
                        ISNULL(fb.ITC, 0)                               AS Itc,
                        ISNULL(fb.CV, 0)                                AS Cv,
                        0.00                                            AS VatCurrAccTransport,
                        0.00                                            AS VatCurrAccCf,
                        0.00                                            AS VatCurrAccTransportAdjust,
                        0.00                                            AS VatCurrAccCfAdjust,
                        0.00                                            AS MinimumAgencyCommission,
                        0.00                                            AS OnInvoiceValue,
                        0.00                                            AS AgencyCommissionAmount,
                        0.00                                            AS AssessableRate,
                        0.00                                            AS AssessableValue,
                        0.00                                            AS InvoiceValue,
                        'USD'                                           AS InvoiceCurrency,
                        ISNULL(fb.Terms, '')                            AS Terms,
                        COALESCE(NULLIF(adj.Remarks, ''), fb.Remarks, '') AS Remarks,
                        COALESCE(NULLIF(adj.NotesReasonForCredit, ''), NULLIF(adj.Remarks, ''), fb.Remarks, '') AS NotesReasonForCredit,
                        ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
                        ISNULL(adj.AdjustmentType, 'Credit Note')       AS AdjustmentType,
                        adj.AdjustmentDate                              AS AdjustmentDate,
                        ISNULL(v.VoucherNo, '')                         AS VoucherNo,
                        COALESCE(TRY_CAST(fb.ProcessDate AS DATETIME), GETDATE()) AS CreationDate,
                        GETDATE()                                       AS LastUpdateDate
                    FROM dbo.FinalBillInfo_ForBillAdjust fb WITH (NOLOCK)
                    OUTER APPLY (
                        SELECT TOP 1 v.VoucherNo 
                        FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
                        WHERE v.InvoiceNo = fb.BillNo 
                        ORDER BY v.autoId DESC
                    ) v
                    OUTER APPLY (
                        SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate, adj.Remarks, adj.NotesReasonForCredit
                        FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
                        WHERE adj.BillNo = fb.BillNo 
                        ORDER BY adj.TC DESC
                    ) adj
                    WHERE fb.BillNo = @BillNo;
                END
                ELSE
                BEGIN
                    SELECT TOP 1
                        b.BillNo,
                        ISNULL(b.PrifixId, 'SLSL')                      AS BillNoPrefix,
                        b.BillDate                                      AS BillDate,
                        ISNULL(b.JobNo, '')                             AS JobNo,
                        b.BillDate                                      AS JobDate,
                        COALESCE(br.BranchName, comp.CompanyName, 'Head Office (Dhaka)') AS Branch,
                        COALESCE(et.ExpenseType, 'Sea Export (CTG)')    AS ServiceType,
                        ISNULL(b.CustomerID, '')                        AS CustomerId,
                        ISNULL(c.CustomerName, '')                      AS CustomerName,
                        ISNULL(c.CustomerAddress, b.Address)            AS CustomerAddress,
                        ISNULL(b.Address, '')                           AS CustomerDeliveryAddress,
                        ISNULL(c.CustomerName, '')                      AS SupplierName,
                        'Chittagong Port'                               AS PortName,
                        b.DocRecvDate                                   AS DocRecvDate,
                        ISNULL(b.INVNo, '')                             AS LcNo,
                        ISNULL(b.LCValue, 0)                            AS LcValue,
                        ISNULL(b.LCurrencyID, 'USD')                    AS LcCurrency,
                        ISNULL(b.AssessableRate, 0)                     AS BdtRate,
                        ISNULL(b.NumberOfTransport, 1)                  AS NumberOfTransport,
                        ISNULL(b.NumberOfDemerageDay, 0)                AS NumberOfDemerageDay,
                        ISNULL(b.IPNo, '')                              AS IpNo,
                        b.IPDate                                        AS IpDate,
                        ISNULL(b.UndertakingNo, '')                     AS UndertakingNo,
                        b.ArrivalDate                                   AS ArrivalDate,
                        ISNULL(b.INVNo, '')                             AS InvNo,
                        b.INVDate                                       AS InvDate,
                        ISNULL(b.BLNo, '')                              AS BlNo,
                        b.BLDate                                        AS BlDate,
                        ISNULL(b.BENo, '')                              AS BeNo,
                        b.BEDate                                        AS BeDate,
                        ISNULL(b.EFRNo, '')                             AS EfrNo,
                        b.EFRDate                                       AS EfrDate,
                        ISNULL(b.ROTNo, '')                             AS RotNo,
                        ISNULL(b.OFRNo, '')                             AS PoNo,
                        ISNULL(b.ContainerNo, '')                       AS ContainerNo,
                        ISNULL(b.MaterialDescription, '')               AS MaterialDescription,
                        ISNULL(b.Quantity, 0)                           AS Quantity,
                        ISNULL(qu.ShortName, 'PCS')                     AS QUnitType,
                        ISNULL(b.DocWeight, 0)                          AS DocWeight,
                        ISNULL(wu.ShortName, 'KG')                      AS WUnitType,
                        ISNULL(b.JRSNo, '')                             AS Fwdr,
                        ISNULL(b.ROTNo, '')                             AS VesselNo,
                        b.ArrivalDate                                   AS EtaDate,
                        b.ArrivalDate                                   AS AtaDate,
                        b.ArrivalDate                                   AS EtbDate,
                        b.ArrivalDate                                   AS AtbDate,
                        COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS UnStuffingDate,
                        COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS DateOfClearance,
                        ISNULL(b.JRSNo, '')                             AS ConsoleNo,
                        ISNULL(b.Destination, '')                       AS Destination,
                        COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS DeliveryDate,
                        ISNULL(b.BillAmount, 0)                         AS GrossAmount,
                        0.00                                            AS AdvanceAmount,
                        ISNULL(b.BillAmount, 0)                         AS NetAmount,
                        ISNULL(b.BillAmount, 0)                         AS TotalCfBillAmount,
                        0.00                                            AS TotalTransportBillAmount,
                        0.00                                            AS Itc,
                        0.00                                            AS Cv,
                        0.00                                            AS VatCurrAccTransport,
                        0.00                                            AS VatCurrAccCf,
                        0.00                                            AS VatCurrAccTransportAdjust,
                        0.00                                            AS VatCurrAccCfAdjust,
                        ISNULL(b.MinimumAgencyCommission, 0)            AS MinimumAgencyCommission,
                        ISNULL(b.OnInvoiceValue, 0)                     AS OnInvoiceValue,
                        (ISNULL(b.InvoiceValue, 0) * ISNULL(b.OnInvoiceValue, 0) / 100.0) AS AgencyCommissionAmount,
                        ISNULL(b.AssessableRate, 0)                     AS AssessableRate,
                        ISNULL(b.AssessableValue, 0)                    AS AssessableValue,
                        ISNULL(b.InvoiceValue, 0)                       AS InvoiceValue,
                        ISNULL(b.InvoiceCurrencyID, 'USD')              AS InvoiceCurrency,
                        ISNULL(b.Terms, '')                             AS Terms,
                        COALESCE(NULLIF(adj.Remarks, ''), b.Remarks, '') AS Remarks,
                        COALESCE(NULLIF(adj.NotesReasonForCredit, ''), NULLIF(adj.Remarks, ''), b.Remarks, '') AS NotesReasonForCredit,
                        ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
                        ISNULL(adj.AdjustmentType, 'Credit Note')       AS AdjustmentType,
                        adj.AdjustmentDate                              AS AdjustmentDate,
                        ISNULL(v.VoucherNo, '')                         AS VoucherNo,
                        b.LDate                                         AS CreationDate,
                        b.ModifyDate                                    AS LastUpdateDate
                    FROM dbo.CF_BillEntry b WITH (NOLOCK)
                    LEFT JOIN dbo.Sales_Customer c WITH (NOLOCK) ON b.CustomerID = c.CustomerID
                    LEFT JOIN dbo.Core_Branch br WITH (NOLOCK) ON b.CompanyCode = br.BranchCode
                    LEFT JOIN dbo.Core_Company comp WITH (NOLOCK) ON b.CompanyCode = comp.CompanyCode
                    LEFT JOIN dbo.CF_Def_ExpenseType et WITH (NOLOCK) ON b.ExpenseTypeID = et.ExpenseTypeID
                    LEFT JOIN dbo.Sep_Documentation s WITH (NOLOCK) ON b.JobNo = s.JobNo
                    LEFT JOIN dbo.RMG_Prod_Def_UnitType qu WITH (NOLOCK) ON b.QUnitTypeID = qu.UnitTypID
                    LEFT JOIN dbo.RMG_Prod_Def_UnitType wu WITH (NOLOCK) ON b.WUnitTypeID = wu.UnitTypID
                    OUTER APPLY (
                        SELECT TOP 1 v.VoucherNo 
                        FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
                        WHERE v.InvoiceNo = b.BillNo 
                        ORDER BY v.autoId DESC
                    ) v
                    OUTER APPLY (
                        SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate, adj.Remarks, adj.NotesReasonForCredit
                        FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
                        WHERE adj.BillNo = b.BillNo 
                        ORDER BY adj.TC DESC
                    ) adj
                    WHERE b.BillNo = @BillNo;
                END";

            var chcSql = @"
                IF EXISTS (
                    SELECT 1 
                    FROM dbo.CF_BillAdjustDetails ad WITH (NOLOCK)
                    JOIN dbo.CF_BillAdjustmentEntry ae WITH (NOLOCK) ON ad.AdjustmentNo = ae.AdjustmentNo
                    WHERE ae.BillNo = @BillNo
                )
                BEGIN
                    SELECT
                        ad.TC                                             AS Tc,
                        ad.CHCID                                          AS ChcId,
                        COALESCE(NULLIF(ad.BillHead, ''), chcDef.CHCHead, ad.CHCID, '') AS BillHead,
                        COALESCE(NULLIF(ad.CHCHeadDescription, ''), chcDef.CHCHeadDescription, '') AS ChcHeadDescription,
                        ISNULL(ad.BillAmount, 0)                          AS Amount,
                        CASE 
                            WHEN ad.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT)
                            WHEN ad.IsReceiptable IN ('N', '0', 'False', 'false') THEN CAST(0 AS BIT)
                            WHEN chcDef.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT)
                            ELSE CAST(0 AS BIT)
                        END                                               AS IsReceiptable
                    FROM dbo.CF_BillAdjustDetails ad WITH (NOLOCK)
                    JOIN dbo.CF_BillAdjustmentEntry ae WITH (NOLOCK) ON ad.AdjustmentNo = ae.AdjustmentNo
                    LEFT JOIN dbo.CustomerHandilingCharge chcDef WITH (NOLOCK) ON ad.CHCID = chcDef.CHCID
                    WHERE ae.BillNo = @BillNo
                      AND ae.AdjustmentNo = (
                          SELECT TOP 1 AdjustmentNo 
                          FROM dbo.CF_BillAdjustmentEntry WITH (NOLOCK) 
                          WHERE BillNo = @BillNo 
                          ORDER BY TC DESC
                      )
                    ORDER BY ad.TC ASC;
                END
                ELSE IF EXISTS (SELECT 1 FROM dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust WITH (NOLOCK) WHERE BillNo = @BillNo)
                BEGIN
                    SELECT
                        chc.Tc,
                        chc.Chcid                                       AS ChcId,
                        ISNULL(chc.BillHead, '')                        AS BillHead,
                        ISNULL(chc.ChcheadDescription, '')              AS ChcHeadDescription,
                        ISNULL(chc.BillAmount, 0)                       AS Amount,
                        CASE WHEN chc.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable
                    FROM dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust chc WITH (NOLOCK)
                    WHERE chc.BillNo = @BillNo
                    ORDER BY chc.Tc ASC;
                END
                ELSE
                BEGIN
                    SELECT
                        chc.TC                                          AS Tc,
                        chc.CHCID                                       AS ChcId,
                        COALESCE(NULLIF(chc.BillHead, ''), chcDef.CHCHead, chc.CHCID, '') AS BillHead,
                        COALESCE(NULLIF(chc.CHCHeadDescription, ''), chcDef.CHCHeadDescription, '') AS ChcHeadDescription,
                        ISNULL(chc.BillAmount, 0)                       AS Amount,
                        CASE WHEN chc.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable
                    FROM dbo.CF_BillEntryDetails_CHCHead chc WITH (NOLOCK)
                    LEFT JOIN dbo.CustomerHandilingCharge chcDef WITH (NOLOCK) ON chc.CHCID = chcDef.CHCID
                    WHERE chc.BillNo = @BillNo
                    ORDER BY chc.TC ASC;
                END";

            var expenseSql = @"
                IF EXISTS (SELECT 1 FROM dbo.CF_BillEntryDetails_ForBillAdjust WITH (NOLOCK) WHERE BillNo = @BillNo)
                BEGIN
                    SELECT
                        expD.Tc,
                        ISNULL(expD.BillDetailsId, '')                  AS BillDetailsId,
                        expD.BillNo,
                        ISNULL(expD.ExpenseHeadId, '')                  AS ExpenseHeadId,
                        COALESCE(NULLIF(eh.ExpenseHead, ''), NULLIF(expD.BillHead, ''), expD.ExpenseHeadId, '') AS ExpenseHead,
                        ISNULL(expD.EstimatedAmount, 0)                 AS EstimatedAmount,
                        ISNULL(expD.ActualAmount, 0)                    AS ActualAmount,
                        ISNULL(expD.DifferentAmount, 0)                 AS DifferentAmount,
                        CASE WHEN eh.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(1 AS BIT) END AS IsReceiptable
                    FROM dbo.CF_BillEntryDetails_ForBillAdjust expD WITH (NOLOCK)
                    LEFT JOIN dbo.CF_Def_ExpenseHead eh WITH (NOLOCK) ON expD.ExpenseHeadId = eh.ExpenseHeadID
                    WHERE expD.BillNo = @BillNo
                    ORDER BY expD.Tc ASC;
                END
                ELSE
                BEGIN
                    SELECT
                        expD.TC                                         AS Tc,
                        ISNULL(expD.BillDetailsID, '')                  AS BillDetailsId,
                        expD.BillNo,
                        ISNULL(expD.ExpenseHeadID, '')                  AS ExpenseHeadId,
                        COALESCE(NULLIF(eh.ExpenseHead, ''), NULLIF(expD.BillHead, ''), expD.ExpenseHeadID, '') AS ExpenseHead,
                        ISNULL(expD.EstimatedAmount, 0)                 AS EstimatedAmount,
                        ISNULL(expD.ActualAmount, 0)                    AS ActualAmount,
                        ISNULL(expD.DifferentAmount, 0)                 AS DifferentAmount,
                        CASE WHEN eh.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable
                    FROM dbo.CF_BillEntryDetails expD WITH (NOLOCK)
                    LEFT JOIN dbo.CF_Def_ExpenseHead eh WITH (NOLOCK) ON expD.ExpenseHeadID = eh.ExpenseHeadID
                    WHERE expD.BillNo = @BillNo
                    ORDER BY expD.TC ASC;
                END";

            var parameters = new { BillNo = billNo };

            var header = await _dbConnection.QueryFirstOrDefaultAsync<BillAdjustmentMasterDto>(headerSql, parameters);
            if (header == null)
                return null;

            var chcCharges = (await _dbConnection.QueryAsync<BillAdjustChcDetailDto>(chcSql, parameters)).ToList();
            var expenseDetails = (await _dbConnection.QueryAsync<BillAdjustExpenseDetailDto>(expenseSql, parameters)).ToList();

            // Fallback: If CF_BillEntryDetails_ForBillAdjust is empty, read expenses from FinalBillInfo_ForBillAdjust
            if (!expenseDetails.Any())
            {
                var fallbackExpSql = @"
                    SELECT
                        TRY_CAST(fb.Slno AS INT)                        AS Tc,
                        fb.BillNo,
                        ISNULL(fb.ExpenseHead, '')                      AS ExpenseHead,
                        TRY_CAST(fb.Amount AS DECIMAL(18,2))            AS ActualAmount,
                        TRY_CAST(fb.Amount AS DECIMAL(18,2))            AS EstimatedAmount,
                        0                                               AS DifferentAmount,
                        CASE WHEN fb.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable
                    FROM dbo.FinalBillInfo_ForBillAdjust fb WITH (NOLOCK)
                    WHERE fb.BillNo = @BillNo AND ISNULL(fb.ExpenseHead, '') <> ''
                    ORDER BY TRY_CAST(fb.Slno AS INT) ASC;";

                expenseDetails = (await _dbConnection.QueryAsync<BillAdjustExpenseDetailDto>(fallbackExpSql, parameters)).ToList();
            }

            return new BillAdjustmentScreenDataResponse
            {
                Header = header,
                ChcCharges = chcCharges,
                ExpenseDetails = expenseDetails
            };
        }

        // =========================================================================
        // 3. Save Adjustment (Header + Details + Voucher Posting)
        // =========================================================================
        public async Task<CommonReturnViewModel> SaveAdjustmentAsync(BillAdjustmentSaveRequestDto model)
        {
            var response = new CommonReturnViewModel();

            if (model == null)
            {
                response.Success = false;
                response.Message = "Invalid adjustment data provided.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(model.BillNo))
            {
                response.Success = false;
                response.Message = "Bill No is required.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(model.AdjustmentType))
            {
                response.Success = false;
                response.Message = "Adjustment Type (Credit Note / Debit Note) is required.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(model.CompanyCode))
                model.CompanyCode = "001";
            if (string.IsNullOrWhiteSpace(model.EmployeeId))
                model.EmployeeId = "001";
            if (string.IsNullOrWhiteSpace(model.Luser))
                model.Luser = "Admin";

            if ((model.ChcDetails == null || !model.ChcDetails.Any()) && (model.ExpenseDetails == null || !model.ExpenseDetails.Any()))
            {
                response.Success = false;
                response.Message = "Please add at least one Custom Handling Charge or expense item to adjust.";
                return response;
            }

            EnsureConnectionOpen();

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                string finalAdjustmentNo = model.AdjustmentNo;

                try
                {
                    // Step A: Try Stored Procedure Save
                    var detailJson = JsonSerializer.Serialize((model.ChcDetails ?? new List<BillAdjustChcDetailDto>()).Select(x => new
                    {
                        ChcId = x.ChcId,
                        BillHead = x.BillHead,
                        ChcHeadDescription = x.ChcHeadDescription,
                        Amount = x.Amount,
                        IsReceiptable = x.IsReceiptable,
                        EmployeeId = string.IsNullOrWhiteSpace(x.EmployeeId) ? model.EmployeeId : x.EmployeeId
                    }));

                    var saveParams = new DynamicParameters();
                    saveParams.Add("@AdjustmentNo", model.AdjustmentNo, DbType.String, ParameterDirection.InputOutput, size: 50);
                    saveParams.Add("@AdjustmentType", model.AdjustmentType.Trim(), DbType.String);
                    saveParams.Add("@AdjustmentDate", model.AdjustmentDate ?? DateTime.Today, DbType.DateTime);
                    saveParams.Add("@BillNo", model.BillNo.Trim(), DbType.String);
                    saveParams.Add("@CompanyCode", model.CompanyCode, DbType.String);
                    saveParams.Add("@EmployeeId", model.EmployeeId, DbType.String);
                    saveParams.Add("@Luser", model.Luser, DbType.String);
                    saveParams.Add("@Lip", model.Lip, DbType.String);
                    saveParams.Add("@Lmac", model.Lmac, DbType.String);
                    saveParams.Add("@Remarks", model.Remarks, DbType.String);
                    saveParams.Add("@NotesReasonForCredit", model.NotesReasonForCredit, DbType.String);
                    saveParams.Add("@DetailJson", detailJson, DbType.String);

                    await _dbConnection.ExecuteAsync(
                        "dbo.sp_Save_CF_BillAdjustmentEntry",
                        saveParams,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 120
                    );

                    finalAdjustmentNo = saveParams.Get<string>("@AdjustmentNo");
                }
                catch (Exception ex) when (IsProcedureNotFound(ex, "sp_Save_CF_BillAdjustmentEntry"))
                {
                    _logger.LogWarning("Stored procedure dbo.sp_Save_CF_BillAdjustmentEntry not found in database. Executing direct transactional save.");
                    finalAdjustmentNo = await SaveAdjustmentDirectQueryAsync(model, transaction);
                }

                if (string.IsNullOrEmpty(finalAdjustmentNo))
                {
                    transaction.Rollback();
                    response.Success = false;
                    response.Message = "Failed to generate or retrieve Adjustment No.";
                    return response;
                }

                // Step A.2: Sync expense detail adjustments if provided
                if (model.ExpenseDetails != null && model.ExpenseDetails.Any())
                {
                    foreach (var exp in model.ExpenseDetails)
                    {
                        if (string.IsNullOrWhiteSpace(exp.BillDetailsId) && string.IsNullOrWhiteSpace(exp.ExpenseHeadId)) continue;

                        try
                        {
                            await _dbConnection.ExecuteAsync(
                                @"IF EXISTS (SELECT 1 FROM dbo.CF_BillEntryDetails_ForBillAdjust WHERE BillNo = @BillNo AND (BillDetailsID = @BillDetailsID OR ExpenseHeadID = @ExpenseHeadID))
                                  BEGIN
                                      UPDATE dbo.CF_BillEntryDetails_ForBillAdjust
                                      SET ActualAmount = @ActualAmount,
                                          DifferentAmount = @DifferentAmount,
                                          ModifyDate = GETDATE(),
                                          LUser = @Luser
                                      WHERE BillNo = @BillNo AND (BillDetailsID = @BillDetailsID OR ExpenseHeadID = @ExpenseHeadID);
                                  END
                                  ELSE
                                  BEGIN
                                      INSERT INTO dbo.CF_BillEntryDetails_ForBillAdjust
                                      (BillDetailsID, BillNo, ExpenseHeadID, EstimatedAmount, ActualAmount, DifferentAmount, LUser, LDate, CompanyCode, EmployeeID)
                                      VALUES
                                      (ISNULL(@BillDetailsID, '1'), @BillNo, @ExpenseHeadID, @EstimatedAmount, @ActualAmount, @DifferentAmount, @Luser, GETDATE(), @CompanyCode, @EmployeeId);
                                  END",
                                new
                                {
                                    BillDetailsID = exp.BillDetailsId,
                                    BillNo = model.BillNo.Trim(),
                                    ExpenseHeadID = exp.ExpenseHeadId,
                                    EstimatedAmount = exp.EstimatedAmount,
                                    ActualAmount = exp.ActualAmount,
                                    DifferentAmount = exp.DifferentAmount,
                                    Luser = model.Luser,
                                    CompanyCode = model.CompanyCode,
                                    EmployeeId = model.EmployeeId
                                },
                                transaction: transaction);
                        }
                        catch (Exception expEx)
                        {
                            _logger.LogWarning(expEx, "Optional expense detail adjustment sync failed for BillNo {BillNo}", model.BillNo);
                        }
                    }
                }

                // Step B: Trigger Voucher Entry Stored Procedure based on AdjustmentType
                string voucherSpName = model.AdjustmentType.Contains("Credit", StringComparison.OrdinalIgnoreCase)
                    ? "dbo.VoucherEntryforBillAdjust_CreditNote"
                    : "dbo.VoucherEntryforBillAdjust_DebitNote";

                try
                {
                    var voucherParams = new DynamicParameters();
                    voucherParams.Add("@AdjustmentNo", finalAdjustmentNo, DbType.String);
                    voucherParams.Add("@BillNo", model.BillNo.Trim(), DbType.String);
                    voucherParams.Add("@CompanyCode", model.CompanyCode, DbType.String);
                    voucherParams.Add("@EmployeeId", model.EmployeeId, DbType.String);
                    voucherParams.Add("@Luser", model.Luser, DbType.String);

                    await _dbConnection.ExecuteAsync(
                        voucherSpName,
                        voucherParams,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 120
                    );
                }
                catch (Exception vEx)
                {
                    _logger.LogWarning(vEx, "Voucher procedure {SpName} could not be executed or does not exist yet. Details: {Message}", voucherSpName, vEx.Message);
                }

                transaction.Commit();

                response.Success = true;
                response.Message = $"Adjustment '{finalAdjustmentNo}' saved successfully.";
                response.Data = new { AdjustmentNo = finalAdjustmentNo };
                return response;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Transaction failed in SaveAdjustmentAsync for BillNo: {BillNo}", model.BillNo);
                response.Success = false;
                response.Message = $"Failed to save adjustment: {ex.Message}";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        private async Task<string> SaveAdjustmentDirectQueryAsync(
            BillAdjustmentSaveRequestDto model, 
            IDbTransaction transaction)
        {
            string adjustmentNo = model.AdjustmentNo;
            bool isUpdate = false;

            if (!string.IsNullOrWhiteSpace(adjustmentNo) && adjustmentNo != "0")
            {
                var exists = await _dbConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.CF_BillAdjustmentEntry WHERE AdjustmentNo = @AdjustmentNo",
                    new { AdjustmentNo = adjustmentNo },
                    transaction: transaction);
                isUpdate = exists > 0;
            }

            if (!isUpdate)
            {
                string prefix = model.AdjustmentType.Contains("Debit", StringComparison.OrdinalIgnoreCase) ? "DN" : "CN";
                string yearMonth = (model.AdjustmentDate ?? DateTime.Now).ToString("MMyy");
                string searchPattern = $"{prefix}_{yearMonth}_%";

                int lastSeq = await _dbConnection.ExecuteScalarAsync<int>(
                    @"SELECT ISNULL(MAX(TRY_CAST(RIGHT(AdjustmentNo, 6) AS INT)), 0)
                      FROM dbo.CF_BillAdjustmentEntry WITH (UPDLOCK, HOLDLOCK)
                      WHERE AdjustmentNo LIKE @Pattern",
                    new { Pattern = searchPattern },
                    transaction: transaction);

                adjustmentNo = $"{prefix}_{yearMonth}_{(lastSeq + 1):D6}";
            }

            if (isUpdate)
            {
                await _dbConnection.ExecuteAsync(
                    @"UPDATE dbo.CF_BillAdjustmentEntry
                      SET AdjustmentType = @AdjustmentType,
                          AdjustmentDate = @AdjustmentDate,
                          BillNo = @BillNo,
                          Remarks = @Remarks,
                          NotesReasonForCredit = @NotesReasonForCredit,
                          ModifyDate = GETDATE(),
                          Luser = @Luser,
                          Lip = @Lip,
                          Lmac = @Lmac,
                          CompanyCode = ISNULL(@CompanyCode, CompanyCode),
                          EmployeeId = ISNULL(@EmployeeId, EmployeeId)
                      WHERE AdjustmentNo = @AdjustmentNo",
                    new
                    {
                        AdjustmentNo = adjustmentNo,
                        AdjustmentType = model.AdjustmentType.Trim(),
                        AdjustmentDate = model.AdjustmentDate ?? DateTime.Now,
                        BillNo = model.BillNo.Trim(),
                        Remarks = model.Remarks,
                        NotesReasonForCredit = model.NotesReasonForCredit,
                        Luser = model.Luser,
                        Lip = model.Lip,
                        Lmac = model.Lmac,
                        CompanyCode = model.CompanyCode,
                        EmployeeId = model.EmployeeId
                    },
                    transaction: transaction);

                await _dbConnection.ExecuteAsync(
                    "DELETE FROM dbo.CF_BillAdjustDetails WHERE AdjustmentNo = @AdjustmentNo",
                    new { AdjustmentNo = adjustmentNo },
                    transaction: transaction);
            }
            else
            {
                await _dbConnection.ExecuteAsync(
                    @"INSERT INTO dbo.CF_BillAdjustmentEntry
                      (AdjustmentNo, AdjustmentType, AdjustmentDate, BillNo, Remarks, NotesReasonForCredit, Luser, Ldate, Lip, Lmac, CompanyCode, EmployeeId)
                      VALUES
                      (@AdjustmentNo, @AdjustmentType, @AdjustmentDate, @BillNo, @Remarks, @NotesReasonForCredit, @Luser, GETDATE(), @Lip, @Lmac, @CompanyCode, @EmployeeId)",
                    new
                    {
                        AdjustmentNo = adjustmentNo,
                        AdjustmentType = model.AdjustmentType.Trim(),
                        AdjustmentDate = model.AdjustmentDate ?? DateTime.Now,
                        BillNo = model.BillNo.Trim(),
                        Remarks = model.Remarks,
                        NotesReasonForCredit = model.NotesReasonForCredit,
                        Luser = model.Luser,
                        Lip = model.Lip,
                        Lmac = model.Lmac,
                        CompanyCode = model.CompanyCode,
                        EmployeeId = model.EmployeeId
                    },
                    transaction: transaction);
            }

            // Sync CF_BillAdjustDetails
            foreach (var chc in model.ChcDetails)
            {
                await _dbConnection.ExecuteAsync(
                    @"INSERT INTO dbo.CF_BillAdjustDetails 
                      (AdjustmentNo, Chcid, BillHead, CHCHeadDescription, BillAmount, IsReceiptable, EmployeeId)
                      VALUES 
                      (@AdjustmentNo, @Chcid, @BillHead, @CHCHeadDescription, @BillAmount, @IsReceiptable, @EmployeeId)",
                    new
                    {
                        AdjustmentNo = adjustmentNo,
                        Chcid = chc.ChcId,
                        BillHead = chc.BillHead,
                        CHCHeadDescription = chc.ChcHeadDescription,
                        BillAmount = chc.Amount,
                        IsReceiptable = chc.IsReceiptable ? "Y" : "N",
                        EmployeeId = string.IsNullOrWhiteSpace(chc.EmployeeId) ? model.EmployeeId : chc.EmployeeId
                    },
                    transaction: transaction);
            }

            // Sync CF_BillEntryDetails_CHCHead_ForBillAdjust
            await _dbConnection.ExecuteAsync(
                "DELETE FROM dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust WHERE BillNo = @BillNo",
                new { BillNo = model.BillNo.Trim() },
                transaction: transaction);

            foreach (var chc in model.ChcDetails)
            {
                await _dbConnection.ExecuteAsync(
                    @"INSERT INTO dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust
                      (BillNo, JobNo, CHCID, BillHead, CHCHeadDescription, BillAmount, IsReceiptable, CompanyCode, EmployeeID, LUser, LDate, LIP, LMAC)
                      VALUES
                      (@BillNo, @BillNo, @CHCID, @BillHead, @CHCHeadDescription, @BillAmount, @IsReceiptable, @CompanyCode, @EmployeeID, @LUser, GETDATE(), @LIP, @LMAC)",
                    new
                    {
                        BillNo = model.BillNo.Trim(),
                        CHCID = chc.ChcId,
                        BillHead = chc.BillHead,
                        CHCHeadDescription = chc.ChcHeadDescription,
                        BillAmount = chc.Amount,
                        IsReceiptable = chc.IsReceiptable ? "Y" : "N",
                        CompanyCode = model.CompanyCode,
                        EmployeeID = model.EmployeeId,
                        LUser = model.Luser,
                        LIP = model.Lip,
                        LMAC = model.Lmac
                    },
                    transaction: transaction);
            }

            return adjustmentNo;
        }

        public async Task<string> GetNextAdjustmentNoAsync(string adjustmentType)
        {
            string prefix = adjustmentType != null && adjustmentType.Contains("Debit", StringComparison.OrdinalIgnoreCase) ? "DN" : "CN";
            string yearMonth = DateTime.Now.ToString("MMyy");
            string searchPattern = $"{prefix}_{yearMonth}_%";

            try
            {
                int lastSeq = await _dbConnection.ExecuteScalarAsync<int>(
                    @"SELECT ISNULL(MAX(TRY_CAST(RIGHT(AdjustmentNo, 6) AS INT)), 0)
                      FROM dbo.CF_BillAdjustmentEntry WITH (NOLOCK)
                      WHERE AdjustmentNo LIKE @Pattern",
                    new { Pattern = searchPattern });

                return $"{prefix}_{yearMonth}_{(lastSeq + 1):D6}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch next sequence for adjustment pattern {Pattern}. Using fallback.", searchPattern);
                return $"{prefix}_{yearMonth}_000001";
            }
        }

        public async Task<List<ChcHeadOptionDto>> GetChcHeadsAsync()
        {
            try
            {
                var sql = @"
                    SELECT 
                        CHCID                          AS ChcId, 
                        CHCHead                        AS ChcHead, 
                        ISNULL(CHCHeadDescription, '') AS Description, 
                        CASE WHEN IsReceiptable = 'Y' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable 
                    FROM dbo.CustomerHandilingCharge WITH (NOLOCK) 
                    ORDER BY CHCHead ASC";

                var list = await _dbConnection.QueryAsync<ChcHeadOptionDto>(sql);
                return list.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChcHeadsAsync");
                return new List<ChcHeadOptionDto>();
            }
        }

        private static bool IsProcedureNotFound(Exception ex, string procedureName)
        {
            if (ex == null) return false;
            var msg = ex.Message;
            return msg.Contains("Could not find stored procedure", StringComparison.OrdinalIgnoreCase) ||
                   msg.Contains(procedureName, StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureConnectionOpen()
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }
        }
    }
}
