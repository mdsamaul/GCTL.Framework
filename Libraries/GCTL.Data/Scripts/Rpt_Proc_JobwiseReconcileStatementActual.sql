SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =========================================================================================
-- Stored Procedure: dbo.Rpt_Proc_JobwiseReconcileStatementActual
-- Description: Retrieves Job Number Wise Reconcile Statement (Actual).
--              Pivots all actual expenses per expense head and computes Revenue, TotalExpense, and Profit.
--              Fully aligned with C&F Management Dashboard (SP_Dashboard_JobReconcile).
-- =========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[Rpt_Proc_JobwiseReconcileStatementActual]
    @ExpenseTypeID NVARCHAR(MAX) = '',
    @FromDate      DATETIME      = NULL,
    @ToDate        DATETIME      = NULL,
    @CustomerId    NVARCHAR(MAX) = '',
    @JobNo         NVARCHAR(MAX) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);

    -- 1. Parse Multi-Select / Comma-separated parameters into indexed temp tables
    CREATE TABLE #SelectedJobs (JobNo NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @JobNo IS NOT NULL AND LTRIM(RTRIM(@JobNo)) <> ''
    BEGIN
        INSERT INTO #SelectedJobs (JobNo)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@JobNo, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END;

    CREATE TABLE #SelectedCustomers (CustomerId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @CustomerId IS NOT NULL AND LTRIM(RTRIM(@CustomerId)) <> ''
    BEGIN
        INSERT INTO #SelectedCustomers (CustomerId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@CustomerId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END;

    CREATE TABLE #SelectedExpenseTypes (ExpenseTypeId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @ExpenseTypeID IS NOT NULL AND LTRIM(RTRIM(@ExpenseTypeID)) <> ''
    BEGIN
        INSERT INTO #SelectedExpenseTypes (ExpenseTypeId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ExpenseTypeID, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END;

    -- 2. Build distinct dynamic ExpenseHead column list
    DECLARE @cols NVARCHAR(MAX), 
            @colsIsNull NVARCHAR(MAX),
            @sql NVARCHAR(MAX);

    SELECT @cols = STUFF((
        SELECT DISTINCT ',' + QUOTENAME(E.ExpenseHead)
        FROM CF_OperationalFundRequisitionDetails D WITH (NOLOCK)
        INNER JOIN CF_BillEntry B WITH (NOLOCK) ON B.JobNo = D.JobNo
        INNER JOIN CF_Def_ExpenseHead E WITH (NOLOCK) ON D.ExpenseHeadID = E.ExpenseHeadID
        WHERE CAST(B.BillDate AS DATE) >= CAST(@FromDate AS DATE) 
          AND CAST(B.BillDate AS DATE) <= CAST(@ToDate AS DATE)
          AND (NOT EXISTS (SELECT 1 FROM #SelectedExpenseTypes) OR B.ExpenseTypeID IN (SELECT ExpenseTypeId FROM #SelectedExpenseTypes))
          AND (NOT EXISTS (SELECT 1 FROM #SelectedCustomers) OR B.CustomerID IN (SELECT CustomerId FROM #SelectedCustomers))
          AND (NOT EXISTS (SELECT 1 FROM #SelectedJobs) OR B.JobNo IN (SELECT JobNo FROM #SelectedJobs))
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

    SELECT @colsIsNull = STUFF((
        SELECT DISTINCT ' + ISNULL(' + QUOTENAME(E.ExpenseHead) + ',0)'
        FROM CF_OperationalFundRequisitionDetails D WITH (NOLOCK)
        INNER JOIN CF_BillEntry B WITH (NOLOCK) ON B.JobNo = D.JobNo
        INNER JOIN CF_Def_ExpenseHead E WITH (NOLOCK) ON D.ExpenseHeadID = E.ExpenseHeadID
        WHERE CAST(B.BillDate AS DATE) >= CAST(@FromDate AS DATE) 
          AND CAST(B.BillDate AS DATE) <= CAST(@ToDate AS DATE)
          AND (NOT EXISTS (SELECT 1 FROM #SelectedExpenseTypes) OR B.ExpenseTypeID IN (SELECT ExpenseTypeId FROM #SelectedExpenseTypes))
          AND (NOT EXISTS (SELECT 1 FROM #SelectedCustomers) OR B.CustomerID IN (SELECT CustomerId FROM #SelectedCustomers))
          AND (NOT EXISTS (SELECT 1 FROM #SelectedJobs) OR B.JobNo IN (SELECT JobNo FROM #SelectedJobs))
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');

    -- 3. Extract bills and line item expenses
    SELECT 
        B.BillNo,
        B.JobNo,
        B.CustomerID,
        COALESCE(NULLIF(s.CustomerName, ''), B.CustomerID, 'Unknown') AS CustomerName,
        ISNULL(B.BillAmount, 0) AS BillAmount,
        E.ExpenseHead,
        ISNULL(COALESCE(NULLIF(D.AdjustApprovalAmount, 0), NULLIF(D.BillAdjustAmount, 0), D.ActualAmount, 0), 0) AS HeadAmount
    INTO #TempBillTable
    FROM CF_BillEntry B WITH (NOLOCK)
    LEFT JOIN Sales_Customer s WITH (NOLOCK) ON B.CustomerID = s.CustomerID
    LEFT JOIN CF_OperationalFundRequisitionDetails D WITH (NOLOCK) ON B.JobNo = D.JobNo
    LEFT JOIN CF_Def_ExpenseHead E WITH (NOLOCK) ON D.ExpenseHeadID = E.ExpenseHeadID
    WHERE ISNULL(B.BillNo, '') <> '' 
      AND ISNULL(B.JobNo, '') <> '' 
      AND CAST(B.BillDate AS DATE) >= CAST(@FromDate AS DATE) 
      AND CAST(B.BillDate AS DATE) <= CAST(@ToDate AS DATE) 
      AND (NOT EXISTS (SELECT 1 FROM #SelectedExpenseTypes) OR B.ExpenseTypeID IN (SELECT ExpenseTypeId FROM #SelectedExpenseTypes))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedCustomers) OR B.CustomerID IN (SELECT CustomerId FROM #SelectedCustomers))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedJobs) OR B.JobNo IN (SELECT JobNo FROM #SelectedJobs));

    -- 4. If no dynamic expense heads exist, return bills with 0 total expense
    IF @cols IS NULL OR @cols = ''
    BEGIN
        SELECT 
            JobNo,
            CustomerName,
            BillAmount,
            CAST(0 AS DECIMAL(18,2)) AS TotalExpense,
            BillAmount AS Profit
        FROM (
            SELECT DISTINCT JobNo, CustomerName, BillAmount 
            FROM #TempBillTable
        ) T
        ORDER BY BillAmount DESC;
    END
    ELSE
    BEGIN
        SET @sql = '
        ;WITH ExpenseData AS
        (
            SELECT JobNo, CustomerName, BillAmount, ExpenseHead, HeadAmount FROM #TempBillTable
        )
        SELECT 
            JobNo,
            CustomerName,
            BillAmount,
            ' + @cols + ',
            (' + @colsIsNull + ') AS TotalExpense,
            BillAmount - (' + @colsIsNull + ') AS Profit
        FROM ExpenseData
        PIVOT
        (
            SUM(HeadAmount) 
            FOR ExpenseHead IN (' + @cols + ')
        ) AS P
        ORDER BY BillAmount DESC;';

        EXEC sp_executesql @sql;
    END

    DROP TABLE #TempBillTable;
    DROP TABLE #SelectedJobs;
    DROP TABLE #SelectedCustomers;
    DROP TABLE #SelectedExpenseTypes;
END;
GO
