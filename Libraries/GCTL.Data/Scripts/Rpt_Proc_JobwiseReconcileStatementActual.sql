CREATE OR ALTER PROC [dbo].[Rpt_Proc_JobwiseReconcileStatementActual]
@ExpenseTypeID nvarchar(50),
@FromDate Datetime,
@ToDate Datetime,
@CustomerId nvarchar(50),
@JobNo nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cols NVARCHAR(MAX), 
            @colsIsNull NVARCHAR(MAX),
            @sql NVARCHAR(MAX);

    -- Get distinct ExpenseHead values as column list (quoted)
    SELECT @cols = STUFF((
        SELECT DISTINCT ',' + QUOTENAME(E.ExpenseHead)
        FROM CF_OperationalFundRequisitionDetails D
        INNER JOIN CF_BillEntry B ON B.JobNo = D.JobNo
        INNER JOIN CF_Def_ExpenseHead E ON D.ExpenseHeadID = E.ExpenseHeadID
        WHERE CAST(B.BillDate AS DATE) BETWEEN CAST(@FromDate AS DATE) AND CAST(@ToDate AS DATE)
          AND (ISNULL(@ExpenseTypeID, '') = '' OR B.ExpenseTypeID = @ExpenseTypeID)
          AND (ISNULL(@CustomerId, '') = '' OR B.CustomerID = @CustomerId)
          AND (ISNULL(@JobNo, '') = '' OR B.JobNo IN (SELECT * FROM SplitString(@JobNo, ',')))
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

    -- Build ISNULL([col],0) for summing
    SELECT @colsIsNull = STUFF((
        SELECT DISTINCT ' + ISNULL(' + QUOTENAME(E.ExpenseHead) + ',0)'
        FROM CF_OperationalFundRequisitionDetails D
        INNER JOIN CF_BillEntry B ON B.JobNo = D.JobNo
        INNER JOIN CF_Def_ExpenseHead E ON D.ExpenseHeadID = E.ExpenseHeadID
        WHERE CAST(B.BillDate AS DATE) BETWEEN CAST(@FromDate AS DATE) AND CAST(@ToDate AS DATE)
          AND (ISNULL(@ExpenseTypeID, '') = '' OR B.ExpenseTypeID = @ExpenseTypeID)
          AND (ISNULL(@CustomerId, '') = '' OR B.CustomerID = @CustomerId)
          AND (ISNULL(@JobNo, '') = '' OR B.JobNo IN (SELECT * FROM SplitString(@JobNo, ',')))
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');  

    SELECT 
        B.BillNo,
        B.JobNo,
        B.CustomerID,
        s.CustomerName,
        B.BillAmount,
        E.ExpenseHead,
        D.AdjustApprovalAmount
    INTO #TempBillTable
    FROM CF_BillEntry B
    LEFT OUTER JOIN Sales_Customer s ON B.CustomerID = s.CustomerID
    LEFT JOIN CF_OperationalFundRequisitionDetails D 
           ON B.JobNo = D.JobNo
    LEFT JOIN CF_Def_ExpenseHead E 
           ON D.ExpenseHeadID = E.ExpenseHeadID
    WHERE ISNULL(B.BillNo, '') != '' 
      AND ISNULL(B.JobNo, '') != '' 
      AND CAST(B.BillDate AS DATE) BETWEEN CAST(@FromDate AS DATE) AND CAST(@ToDate AS DATE) 
      AND (ISNULL(@ExpenseTypeID, '') = '' OR B.ExpenseTypeID = @ExpenseTypeID)
      AND (ISNULL(@CustomerId, '') = '' OR B.CustomerID = @CustomerId)
      AND (ISNULL(@JobNo, '') = '' OR B.JobNo IN (SELECT * FROM SplitString(@JobNo, ',')));

    -- If no dynamic expense heads exist, return bills with 0 total expense
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
        ) T;
    END
    ELSE
    BEGIN
        -- Build dynamic SQL
        SET @sql = '
        ;WITH ExpenseData AS
        (
            SELECT * FROM #TempBillTable
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
            SUM(AdjustApprovalAmount) 
            FOR ExpenseHead IN (' + @cols + ')
        ) AS P;';

        EXEC sp_executesql @sql;
    END

    DROP TABLE #TempBillTable;
END
