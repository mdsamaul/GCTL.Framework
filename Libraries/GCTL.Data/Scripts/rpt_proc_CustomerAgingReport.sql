-- ============================================================================
-- Procedure: rpt_proc_CustomerAgingReport
-- Purpose  : Highly optimized single-pass Customer Aging Summary Report
-- ============================================================================
IF OBJECT_ID('dbo.rpt_proc_CustomerAgingReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.rpt_proc_CustomerAgingReport;
GO

CREATE PROCEDURE dbo.rpt_proc_CustomerAgingReport
    @AgingDate DATETIME,
    @CustomerID NVARCHAR(MAX) = ''
AS
BEGIN
    SET NOCOUNT ON;

    -- Pre-parse customer ids if provided
    DECLARE @CustFilter TABLE (CustomerID NVARCHAR(50) PRIMARY KEY);
    IF @CustomerID IS NOT NULL AND LTRIM(RTRIM(@CustomerID)) <> ''
    BEGIN
        INSERT INTO @CustFilter (CustomerID)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@CustomerID, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    DECLARE @HasCustFilter BIT = CASE WHEN EXISTS (SELECT 1 FROM @CustFilter) THEN 1 ELSE 0 END;
    DECLARE @NextDay DATETIME = DATEADD(DAY, 1, @AgingDate);
    DECLARE @DateLabel NVARCHAR(100) = CONCAT('Debtors Aging as on ', FORMAT(@AgingDate, 'dd-MMM-yyyy'));

    SELECT 
        LTRIM(RTRIM(ISNULL(cu.CustomerName, 'Unknown Customer'))) AS CustomerName,
        LTRIM(RTRIM(bi.CustomerID)) AS CustomerNo,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 0 AND 30 THEN bi.BillAmount ELSE 0 END) AS Ag_0_30,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 31 AND 60 THEN bi.BillAmount ELSE 0 END) AS Ag_31_60,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 61 AND 90 THEN bi.BillAmount ELSE 0 END) AS Ag_61_90,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 91 AND 120 THEN bi.BillAmount ELSE 0 END) AS Ag_91_120,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 121 AND 150 THEN bi.BillAmount ELSE 0 END) AS Ag_121_150,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 151 AND 180 THEN bi.BillAmount ELSE 0 END) AS Ag_151_180,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 181 AND 210 THEN bi.BillAmount ELSE 0 END) AS Ag_181_210,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 211 AND 240 THEN bi.BillAmount ELSE 0 END) AS Ag_211_240,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 241 AND 270 THEN bi.BillAmount ELSE 0 END) AS Ag_241_270,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 271 AND 300 THEN bi.BillAmount ELSE 0 END) AS Ag_271_300,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 301 AND 330 THEN bi.BillAmount ELSE 0 END) AS Ag_301_330,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) BETWEEN 331 AND 360 THEN bi.BillAmount ELSE 0 END) AS Ag_331_360,
        SUM(CASE WHEN DATEDIFF(DAY, bi.BillDate, @NextDay) > 360 THEN bi.BillAmount ELSE 0 END) AS Ag_Over360,
        SUM(bi.BillAmount) AS Total,
        @DateLabel AS NameofDate
    FROM CF_BillEntry bi WITH (NOLOCK)
    LEFT JOIN Sales_Customer cu WITH (NOLOCK) ON bi.CustomerID = cu.CustomerID
    WHERE ISNULL(bi.IsCollectBill, '') != 'Y'
      AND bi.BillNo IS NOT NULL
      AND (@HasCustFilter = 0 OR bi.CustomerID IN (SELECT CustomerID FROM @CustFilter))
    GROUP BY cu.CustomerName, bi.CustomerID
    HAVING SUM(bi.BillAmount) > 0
    ORDER BY cu.CustomerName ASC;
END;
GO
