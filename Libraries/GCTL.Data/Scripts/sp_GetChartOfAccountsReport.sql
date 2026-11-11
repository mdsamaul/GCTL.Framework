SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetChartOfAccountsReport
    @SearchTerm NVARCHAR(100) = NULL,
    @ShowArchived BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Clean search term
    DECLARE @Search NVARCHAR(102) = NULL;
    IF @SearchTerm IS NOT NULL AND RTRIM(LTRIM(@SearchTerm)) <> ''
        SET @Search = '%' + RTRIM(LTRIM(@SearchTerm)) + '%';

    -- 1. Full 5-Level Hierarchy Records (For each 5th-layer General Ledger account)
    SELECT 
        gl.GeneralLedgerCodeNo,
        CASE 
            WHEN cust.CustomerID IS NOT NULL 
                THEN CONCAT(cust.CustomerName, ' (', cust.CustomerID, ')')
            WHEN bank.AccountNO IS NOT NULL
                THEN CONCAT(bank.AccountName, ' (', bank.AccountNO, ')')
            WHEN exphead.ExpenseHeadID IS NOT NULL
                THEN CONCAT(exphead.ExpenseHead, ' (', exphead.ExpenseHeadID, ')')
            WHEN sup.SupplierId IS NOT NULL
                THEN CONCAT(sup.SupplierName, ' (', sup.SupplierId, ')')
            ELSE gl.GeneralLedgerName 
        END AS GeneralLedgerName,
        ISNULL(NULLIF(gl.ShortName, ''), ISNULL(cust.ShortName, ISNULL(bki.ShortName, ISNULL(exphead.ShortName, ISNULL(sup.SupplierCode, ''))))) AS GeneralLedgerShortName,
        ISNULL(gl.IsActive, '1') AS IsActive,
        
        ISNULL(ssl.SubsidiaryLedgerCodeNo, gl.SubsidiaryLedgerCodeNo) AS SubsidiaryLedgerCodeNo,
        ISNULL(ssl.SubsidiaryLedgerName, '') AS SubSubsidiaryLedgerName,
        ssl.ShortName AS SubSubsidiaryShortName,
        
        ISNULL(sc.SubControlLedgerCodeNo, gl.SubControlLedgerCodeNo) AS SubControlLedgerCodeNo,
        ISNULL(sc.SubControlLedgerName, '') AS SubControlLedgerName,
        sc.ShortName AS SubControlShortName,
        
        ISNULL(c.ControlLedgerCodeNo, gl.ControlLedgerCodeNo) AS ControlLedgerCodeNo,
        ISNULL(c.ControlLedgerName, '') AS ControlLedgerName,
        c.ShortName AS ControlShortName,
        
        ISNULL(g.GroupLedgerCodeNo, gl.GroupLedgerCodeNo) AS GroupLedgerCodeNo,
        ISNULL(g.GroupLedgerName, '') AS GroupLedgerName,
        g.ShortName AS GroupShortName,

        -- Account Type Category
        CASE 
            WHEN gl.GroupLedgerCodeNo = '1' OR g.GroupLedgerCodeNo = '1' OR LOWER(g.GroupLedgerName) LIKE '%asset%' THEN 'Asset'
            WHEN gl.GroupLedgerCodeNo = '2' OR g.GroupLedgerCodeNo = '2' OR LOWER(g.GroupLedgerName) LIKE '%liabilit%' OR LOWER(g.GroupLedgerName) LIKE '%equity%' THEN 'Liability & Equity'
            WHEN gl.GroupLedgerCodeNo = '3' OR g.GroupLedgerCodeNo = '3' OR LOWER(g.GroupLedgerName) LIKE '%income%' OR LOWER(g.GroupLedgerName) LIKE '%revenue%' THEN 'Income'
            WHEN gl.GroupLedgerCodeNo = '4' OR g.GroupLedgerCodeNo = '4' OR LOWER(g.GroupLedgerName) LIKE '%expense%' THEN 'Expense'
            ELSE 'Other'
        END AS AccountType
    FROM Acc_GeneralLedger gl
    LEFT JOIN dbo.Sales_Customer cust WITH (NOLOCK) 
        ON (gl.GeneralLedgerName = cust.CustomerID OR gl.GeneralLedgerName LIKE '%(' + cust.CustomerID + ')')
    LEFT JOIN dbo.Core_BankAccountInformation bank WITH (NOLOCK) 
        ON (gl.GeneralLedgerName = bank.AccountNO OR gl.GeneralLedgerName LIKE '%(' + bank.AccountNO + ')')
    LEFT JOIN dbo.SALES_Def_BankInfo bki WITH (NOLOCK) 
        ON bank.BankID = bki.BankID
    LEFT JOIN dbo.CF_Def_ExpenseHead exphead WITH (NOLOCK) 
        ON (gl.GeneralLedgerName = exphead.ExpenseHeadID OR gl.GeneralLedgerName LIKE '%(' + exphead.ExpenseHeadID + ')')
    LEFT JOIN dbo.RMG_Def_Supplier sup WITH (NOLOCK)
        ON (gl.GeneralLedgerName = sup.SupplierId OR gl.GeneralLedgerName LIKE '%(' + sup.SupplierId + ')')
    LEFT JOIN Acc_SubsidiaryLedger ssl WITH (NOLOCK) ON ssl.SubsidiaryLedgerCodeNo = gl.SubsidiaryLedgerCodeNo
    LEFT JOIN Acc_SubControlLedger sc WITH (NOLOCK) ON sc.SubControlLedgerCodeNo = gl.SubControlLedgerCodeNo
    LEFT JOIN Acc_ControlLedger c WITH (NOLOCK) ON c.ControlLedgerCodeNo = gl.ControlLedgerCodeNo
    LEFT JOIN Acc_GroupLedger g WITH (NOLOCK) ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo
    WHERE (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)
      AND (@Search IS NULL OR (
            gl.GeneralLedgerCodeNo LIKE @Search OR 
            gl.GeneralLedgerName LIKE @Search OR 
            (cust.CustomerName IS NOT NULL AND cust.CustomerName LIKE @Search) OR
            (cust.CustomerID IS NOT NULL AND cust.CustomerID LIKE @Search) OR
            (bank.AccountName IS NOT NULL AND bank.AccountName LIKE @Search) OR
            (bank.AccountNO IS NOT NULL AND bank.AccountNO LIKE @Search) OR
            (exphead.ExpenseHead IS NOT NULL AND exphead.ExpenseHead LIKE @Search) OR
            (exphead.ExpenseHeadID IS NOT NULL AND exphead.ExpenseHeadID LIKE @Search) OR
            (sup.SupplierName IS NOT NULL AND sup.SupplierName LIKE @Search) OR
            (sup.SupplierId IS NOT NULL AND sup.SupplierId LIKE @Search) OR
            ssl.SubsidiaryLedgerCodeNo LIKE @Search OR 
            ssl.SubsidiaryLedgerName LIKE @Search OR 
            sc.SubControlLedgerCodeNo LIKE @Search OR 
            sc.SubControlLedgerName LIKE @Search OR 
            c.ControlLedgerCodeNo LIKE @Search OR 
            c.ControlLedgerName LIKE @Search OR 
            g.GroupLedgerCodeNo LIKE @Search OR 
            g.GroupLedgerName LIKE @Search
      ))
    ORDER BY 
        gl.GroupLedgerCodeNo, 
        gl.ControlLedgerCodeNo, 
        gl.SubControlLedgerCodeNo, 
        gl.SubsidiaryLedgerCodeNo, 
        gl.GeneralLedgerCodeNo;

    -- 2. Statistical Summary
    SELECT 
        (SELECT COUNT(*) FROM Acc_GeneralLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalGeneralLedgers,
        (SELECT COUNT(DISTINCT GroupLedgerCodeNo) FROM Acc_GeneralLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalGroups,
        (SELECT COUNT(DISTINCT ControlLedgerCodeNo) FROM Acc_GeneralLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalControls,
        (SELECT COUNT(DISTINCT SubControlLedgerCodeNo) FROM Acc_GeneralLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalSubControls,
        (SELECT COUNT(DISTINCT SubsidiaryLedgerCodeNo) FROM Acc_GeneralLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalSubSubsidiaries,
        
        -- Asset GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (gl.GroupLedgerCodeNo = '1' OR g.GroupLedgerCodeNo = '1' OR LOWER(g.GroupLedgerName) LIKE '%asset%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '1')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS AssetsGLCount,

        -- Equity & Liabilities GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (gl.GroupLedgerCodeNo = '2' OR g.GroupLedgerCodeNo = '2' OR LOWER(g.GroupLedgerName) LIKE '%liabilit%' OR LOWER(g.GroupLedgerName) LIKE '%equity%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '2')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS EquityGLCount,

        -- Income GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (gl.GroupLedgerCodeNo = '3' OR g.GroupLedgerCodeNo = '3' OR LOWER(g.GroupLedgerName) LIKE '%income%' OR LOWER(g.GroupLedgerName) LIKE '%revenue%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '3')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS IncomeGLCount,

        -- Expenses GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (gl.GroupLedgerCodeNo = '4' OR g.GroupLedgerCodeNo = '4' OR LOWER(g.GroupLedgerName) LIKE '%expense%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '4')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS ExpenseGLCount;
END;
GO
