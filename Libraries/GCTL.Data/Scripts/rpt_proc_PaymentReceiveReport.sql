-- =============================================
-- Author:      GCTL Framework Team
-- Create date: 2026-11-10
-- Description: Payment Receive Report Stored Procedure
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[rpt_proc_PaymentReceiveReport]
    @FromDate           VARCHAR(20) = NULL,
    @ToDate             VARCHAR(20) = NULL,
    @CustomerTypeId     VARCHAR(100) = NULL,
    @District           VARCHAR(100) = NULL,
    @Thana              VARCHAR(100) = NULL,
    @EmployeeId         VARCHAR(100) = NULL,
    @PaymentTypeId      VARCHAR(100) = NULL,
    @PaymentModeId      VARCHAR(100) = NULL,
    @PaymentStatus      VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Clean placeholder values
    IF (@CustomerTypeId LIKE '%Select%' OR @CustomerTypeId LIKE '%All%' OR @CustomerTypeId = '') SET @CustomerTypeId = NULL;
    IF (@District LIKE '%Select%' OR @District LIKE '%All%' OR @District = '') SET @District = NULL;
    IF (@Thana LIKE '%Select%' OR @Thana LIKE '%All%' OR @Thana = '') SET @Thana = NULL;
    IF (@EmployeeId LIKE '%Select%' OR @EmployeeId LIKE '%All%' OR @EmployeeId = '') SET @EmployeeId = NULL;
    IF (@PaymentTypeId LIKE '%Select%' OR @PaymentTypeId LIKE '%All%' OR @PaymentTypeId = '') SET @PaymentTypeId = NULL;
    IF (@PaymentModeId LIKE '%Select%' OR @PaymentModeId LIKE '%All%' OR @PaymentModeId = '') SET @PaymentModeId = NULL;
    IF (@PaymentStatus LIKE '%Select%' OR @PaymentStatus LIKE '%All%' OR @PaymentStatus = '') SET @PaymentStatus = NULL;

    DECLARE @DtFrom DATETIME = NULL,
            @DtTo   DATETIME = NULL;

    IF (@FromDate IS NOT NULL AND @FromDate <> '')
    BEGIN
        SET @DtFrom = TRY_CONVERT(DATETIME, @FromDate, 103);
        IF @DtFrom IS NULL SET @DtFrom = TRY_CONVERT(DATETIME, @FromDate, 120);
        IF @DtFrom IS NULL SET @DtFrom = TRY_CONVERT(DATETIME, @FromDate);
    END

    IF (@ToDate IS NOT NULL AND @ToDate <> '')
    BEGIN
        SET @DtTo = TRY_CONVERT(DATETIME, @ToDate, 103);
        IF @DtTo IS NULL SET @DtTo = TRY_CONVERT(DATETIME, @ToDate, 120);
        IF @DtTo IS NULL SET @DtTo = TRY_CONVERT(DATETIME, @ToDate);
    END

    -- If @DtTo is provided, set to 23:59:59.997
    IF @DtTo IS NOT NULL
        SET @DtTo = DATEADD(MILLISECOND, -3, DATEADD(DAY, 1, CAST(CAST(@DtTo AS DATE) AS DATETIME)));

    -- Pre-aggregate invoices per PaymentReceiveID
    ;WITH InvoicesAgg AS (
        SELECT 
            PaymentReceiveID,
            STRING_AGG(CAST(RTRIM(LTRIM(InvoiceNo)) AS NVARCHAR(MAX)), ', ') WITHIN GROUP (ORDER BY InvoiceNo) AS InvoiceNos
        FROM (
            SELECT DISTINCT PaymentReceiveID, InvoiceNo
            FROM dbo.PaymentReceiveDetails WITH (NOLOCK)
            WHERE InvoiceNo IS NOT NULL AND InvoiceNo <> ''
        ) d
        GROUP BY PaymentReceiveID
    )
    SELECT 
        ROW_NUMBER() OVER(ORDER BY pr.PaymentReceiveDate DESC, pr.AutoID DESC) AS SLNo,
        ISNULL(pr.PaymentReceiveID, '') AS PaymentReceiveNo,
        CONVERT(VARCHAR(10), pr.PaymentReceiveDate, 103) AS PaymentReceiveDate,
        ISNULL(c.CustomerID, '') AS CustomerID,
        ISNULL(c.CustomerName, '') AS Customer,
        CASE 
            WHEN ct.CustomerType IS NOT NULL AND ct.CustomerType <> '' THEN ct.CustomerType
            WHEN LTRIM(RTRIM(c.CustomerType)) = '1' THEN 'In House'
            WHEN LTRIM(RTRIM(c.CustomerType)) = '0' THEN 'General'
            ELSE ISNULL(c.CustomerType, '')
        END AS CustomerType,
        ISNULL(c.CustomerAddress, '') AS BillingAddress,
        ISNULL(NULLIF(LTRIM(RTRIM(CONCAT(e.FirstName, ' ', ISNULL(e.LastName, '')))), ''), ISNULL(pr.Receivedby, '')) AS Employee,
        COALESCE(NULLIF(pr.AgainstBillList, ''), inv.InvoiceNos, '') AS InvoiceNoList,
        ISNULL(pt.PaymentType, '') AS PaymentType,
        ISNULL(pm.PaymentModeName, '') AS PaymentMode,
        ISNULL(pr.Amount, 0.00) AS PaymentAmount,
        ISNULL(pr.BankFeeOnlineCharge, 0.00) AS BankFeeOnlineCharge,
        ISNULL(pr.ChequeNo, '') AS ChequeNo,
        CASE WHEN pr.ChequeDate IS NOT NULL THEN CONVERT(VARCHAR(10), pr.ChequeDate, 103) ELSE '' END AS ChequeDate,
        ISNULL(pr.ChequeBank, '') AS ChequeBank,
        ISNULL(RTRIM(LTRIM(ISNULL(ba.AccountName, '') + CASE WHEN ba.AccountNO IS NOT NULL AND ba.AccountNO <> '' THEN ' - ' + ba.AccountNO ELSE '' END)), ISNULL(pr.DepositedACNo, '')) AS DepositBankAccount,
        CASE WHEN pr.ChequeDate IS NOT NULL THEN CONVERT(VARCHAR(10), pr.ChequeDate, 103) ELSE '' END AS MaturityDate,
        CASE 
            WHEN pr.IsChequeVerified = '1' OR pr.IsChequeVerified = 'True' THEN 'Approved'
            ELSE 'Approved'
        END AS PaymentStatus
    FROM dbo.PaymentReceive pr WITH (NOLOCK)
    LEFT JOIN dbo.Sales_Customer c WITH (NOLOCK) ON pr.CustomerID = c.CustomerID
    LEFT JOIN dbo.Sales_Def_CustomerType ct WITH (NOLOCK) ON c.CustomerType = ct.CustomerTypeID
    LEFT JOIN dbo.HRM_Employee e WITH (NOLOCK) ON pr.Receivedby = e.EmployeeID
    LEFT JOIN dbo.Sales_Def_PaymentType pt WITH (NOLOCK) ON pr.PaymentTypeID = pt.PaymentTypeID
    LEFT JOIN dbo.Sales_Def_PaymentMode pm WITH (NOLOCK) ON pr.PaymentModeCode = pm.PaymentModeID
    LEFT JOIN dbo.Core_BankAccountInformation ba WITH (NOLOCK) ON pr.DepositedACNo = ba.AccInfoID
    LEFT JOIN InvoicesAgg inv ON pr.PaymentReceiveID = inv.PaymentReceiveID
    WHERE 
        (@DtFrom IS NULL OR pr.PaymentReceiveDate >= @DtFrom)
        AND (@DtTo IS NULL OR pr.PaymentReceiveDate <= @DtTo)
        AND (@CustomerTypeId IS NULL OR c.CustomerType = @CustomerTypeId OR ct.CustomerTypeID = @CustomerTypeId OR c.CustomerName LIKE '%' + @CustomerTypeId + '%')
        AND (@District IS NULL OR c.CustomerAddress LIKE '%' + @District + '%')
        AND (@Thana IS NULL OR c.CustomerAddress LIKE '%' + @Thana + '%')
        AND (@EmployeeId IS NULL OR pr.Receivedby = @EmployeeId OR pr.EmployeeID = @EmployeeId OR e.FirstName LIKE '%' + @EmployeeId + '%')
        AND (@PaymentTypeId IS NULL OR pr.PaymentTypeID = @PaymentTypeId OR pt.PaymentType = @PaymentTypeId)
        AND (@PaymentModeId IS NULL OR pr.PaymentModeCode = @PaymentModeId OR pm.PaymentModeName = @PaymentModeId)
    ORDER BY pr.PaymentReceiveDate DESC, pr.AutoID DESC;
END
GO
