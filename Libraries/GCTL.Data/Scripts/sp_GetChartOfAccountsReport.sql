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

    -- 1. Full 5-Level Hierarchy Records
    SELECT 
        gl.GeneralLedgerCodeNo,
        gl.GeneralLedgerName,
        gl.ShortName AS GeneralLedgerShortName,
        ISNULL(gl.IsActive, '1') AS IsActive,
        
        ssl.SubSubsidiaryLedgerCodeNo,
        ssl.SubSubsidiaryLedgerName,
        ssl.ShortName AS SubSubsidiaryShortName,
        
        sc.SubControlLedgerCodeNo,
        sc.SubControlLedgerName,
        sc.ShortName AS SubControlShortName,
        
        c.ControlLedgerCodeNo,
        c.ControlLedgerName,
        c.ShortName AS ControlShortName,
        
        g.GroupLedgerCodeNo,
        g.GroupLedgerName,
        g.ShortName AS GroupShortName,

        -- Account Type Category
        CASE 
            WHEN g.GroupLedgerCodeNo = '1' OR LOWER(g.GroupLedgerName) LIKE '%asset%' THEN 'Asset'
            WHEN g.GroupLedgerCodeNo = '2' OR LOWER(g.GroupLedgerName) LIKE '%liabilit%' OR LOWER(g.GroupLedgerName) LIKE '%equity%' THEN 'Liability & Equity'
            WHEN g.GroupLedgerCodeNo = '3' OR LOWER(g.GroupLedgerName) LIKE '%income%' OR LOWER(g.GroupLedgerName) LIKE '%revenue%' THEN 'Income'
            WHEN g.GroupLedgerCodeNo = '4' OR LOWER(g.GroupLedgerName) LIKE '%expense%' OR LOWER(g.GroupLedgerName) LIKE '%cost%' THEN 'Expense'
            ELSE 'Other'
        END AS AccountType
    FROM Acc_GroupLedger g
    LEFT JOIN Acc_ControlLedger c 
        ON c.GroupLedgerCodeNo = g.GroupLedgerCodeNo 
        AND (@ShowArchived = 1 OR c.IsActive = '1' OR c.IsActive IS NULL)
    LEFT JOIN Acc_SubControlLedger sc 
        ON sc.ControlLedgerCodeNo = c.ControlLedgerCodeNo 
        AND (@ShowArchived = 1 OR sc.IsActive = '1' OR sc.IsActive IS NULL)
    LEFT JOIN Acc_SubSubsidiaryLedger ssl 
        ON ssl.SubControlLedgerCodeNo = sc.SubControlLedgerCodeNo 
        AND (@ShowArchived = 1 OR ssl.IsActive = '1' OR ssl.IsActive IS NULL)
    LEFT JOIN Acc_GeneralLedger gl 
        ON gl.SubSubsidiaryLedgerCodeNo = ssl.SubSubsidiaryLedgerCodeNo 
        AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)
    WHERE (@ShowArchived = 1 OR g.IsActive = '1' OR g.IsActive IS NULL)
      AND (@Search IS NULL OR (
            gl.GeneralLedgerCodeNo LIKE @Search OR 
            gl.GeneralLedgerName LIKE @Search OR 
            ssl.SubSubsidiaryLedgerCodeNo LIKE @Search OR 
            ssl.SubSubsidiaryLedgerName LIKE @Search OR 
            sc.SubControlLedgerCodeNo LIKE @Search OR 
            sc.SubControlLedgerName LIKE @Search OR 
            c.ControlLedgerCodeNo LIKE @Search OR 
            c.ControlLedgerName LIKE @Search OR 
            g.GroupLedgerCodeNo LIKE @Search OR 
            g.GroupLedgerName LIKE @Search
      ))
    ORDER BY 
        g.GroupLedgerCodeNo, 
        c.ControlLedgerCodeNo, 
        sc.SubControlLedgerCodeNo, 
        ssl.SubSubsidiaryLedgerCodeNo, 
        gl.GeneralLedgerCodeNo;

    -- 2. Statistical Summary
    SELECT 
        (SELECT COUNT(*) FROM Acc_GeneralLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalGeneralLedgers,
        (SELECT COUNT(*) FROM Acc_GroupLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalGroups,
        (SELECT COUNT(*) FROM Acc_ControlLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalControls,
        (SELECT COUNT(*) FROM Acc_SubControlLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalSubControls,
        (SELECT COUNT(*) FROM Acc_SubSubsidiaryLedger WHERE (@ShowArchived = 1 OR IsActive = '1' OR IsActive IS NULL)) AS TotalSubSubsidiaries,
        
        -- Asset GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (g.GroupLedgerCodeNo = '1' OR LOWER(g.GroupLedgerName) LIKE '%asset%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '1')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS AssetsGLCount,

        -- Equity & Liabilities GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (g.GroupLedgerCodeNo = '2' OR LOWER(g.GroupLedgerName) LIKE '%liabilit%' OR LOWER(g.GroupLedgerName) LIKE '%equity%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '2')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS EquityGLCount,

        -- Income GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (g.GroupLedgerCodeNo = '3' OR LOWER(g.GroupLedgerName) LIKE '%income%' OR LOWER(g.GroupLedgerName) LIKE '%revenue%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '3')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS IncomeGLCount,

        -- Expenses GL count
        (SELECT COUNT(*) FROM Acc_GeneralLedger gl 
         LEFT JOIN Acc_GroupLedger g ON g.GroupLedgerCodeNo = gl.GroupLedgerCodeNo 
         WHERE (g.GroupLedgerCodeNo = '4' OR LOWER(g.GroupLedgerName) LIKE '%expense%' OR LOWER(g.GroupLedgerName) LIKE '%cost%' OR LEFT(gl.GeneralLedgerCodeNo, 1) = '4')
           AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)) AS ExpenseGLCount;
END;
GO
