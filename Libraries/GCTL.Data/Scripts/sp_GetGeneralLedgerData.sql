-- =========================================================================
-- General Ledger Procedures & Data Retrieval
-- =========================================================================

CREATE OR ALTER PROCEDURE dbo.sp_GetGeneralLedgerData
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @SearchTerm NVARCHAR(200) = NULL,
    @SortColumn NVARCHAR(50) = NULL,
    @SortOrder NVARCHAR(10) = NULL,
    @SubSubsidiaryCode NVARCHAR(50) = NULL,
    @SubControlCode NVARCHAR(50) = NULL,
    @ControlCode NVARCHAR(50) = NULL,
    @GroupCode NVARCHAR(50) = NULL,
    @ShowArchived BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Search NVARCHAR(202) = NULL;
    IF @SearchTerm IS NOT NULL AND LTRIM(RTRIM(@SearchTerm)) <> ''
        SET @Search = '%' + LTRIM(RTRIM(@SearchTerm)) + '%';

    IF @PageNumber IS NULL OR @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 10;
    IF @SortColumn IS NULL OR @SortColumn = '' SET @SortColumn = 'code';
    IF @SortOrder IS NULL OR @SortOrder = '' SET @SortOrder = 'ASC';

    ;WITH BaseData AS (
        SELECT 
            gl.autoId AS AutoId,
            gl.GeneralLedgerCodeNo,
            gl.SubsidiaryLedgerCodeNo,
            ISNULL(ssl.SubsidiaryLedgerName, '') AS SubSubsidiaryLedgerName,
            gl.SubControlLedgerCodeNo,
            ISNULL(sc.SubControlLedgerName, '') AS SubControlLedgerName,
            gl.ControlLedgerCodeNo,
            ISNULL(c.ControlLedgerName, '') AS ControlLedgerName,
            gl.GroupLedgerCodeNo,
            ISNULL(g.GroupLedgerName, '') AS GroupLedgerName,
            CASE 
                WHEN cust.CustomerID IS NOT NULL 
                    THEN CONCAT(cust.CustomerName, ' (', cust.CustomerID, ')')
                WHEN bank.AccountNO IS NOT NULL
                    THEN CONCAT(bank.AccountName, ' (', bank.AccountNO, ')')
                WHEN exphead.ExpenseHeadID IS NOT NULL
                    THEN CONCAT(exphead.ExpenseHead, ' (', exphead.ExpenseHeadID, ')')
                ELSE gl.GeneralLedgerName 
            END AS GeneralLedgerName,
            ISNULL(NULLIF(gl.ShortName, ''), ISNULL(cust.ShortName, ISNULL(bki.ShortName, ''))) AS ShortName,
            CASE WHEN gl.IsActive = '1' OR gl.IsActive IS NULL THEN 1 ELSE 0 END AS IsActive
        FROM dbo.Acc_GeneralLedger gl WITH (NOLOCK)
        LEFT JOIN dbo.Sales_Customer cust WITH (NOLOCK) 
            ON (gl.GeneralLedgerName = cust.CustomerID OR gl.GeneralLedgerName LIKE '%(' + cust.CustomerID + ')')
        LEFT JOIN dbo.Core_BankAccountInformation bank WITH (NOLOCK)
            ON (gl.GeneralLedgerName = bank.AccountNO OR gl.GeneralLedgerName LIKE '%(' + bank.AccountNO + ')')
        LEFT JOIN dbo.SALES_Def_BankInfo bki WITH (NOLOCK)
            ON bank.BankID = bki.BankID
        LEFT JOIN dbo.CF_Def_ExpenseHead exphead WITH (NOLOCK)
            ON (gl.GeneralLedgerName = exphead.ExpenseHeadID OR gl.GeneralLedgerName LIKE '%(' + exphead.ExpenseHeadID + ')')
        LEFT JOIN dbo.Acc_SubsidiaryLedger ssl WITH (NOLOCK) 
            ON gl.SubsidiaryLedgerCodeNo = ssl.SubsidiaryLedgerCodeNo
        LEFT JOIN dbo.Acc_SubControlLedger sc WITH (NOLOCK) 
            ON gl.SubControlLedgerCodeNo = sc.SubControlLedgerCodeNo
        LEFT JOIN dbo.Acc_ControlLedger c WITH (NOLOCK) 
            ON gl.ControlLedgerCodeNo = c.ControlLedgerCodeNo
        LEFT JOIN dbo.Acc_GroupLedger g WITH (NOLOCK) 
            ON gl.GroupLedgerCodeNo = g.GroupLedgerCodeNo
        WHERE (@SubSubsidiaryCode IS NULL OR @SubSubsidiaryCode = '' OR gl.SubsidiaryLedgerCodeNo = @SubSubsidiaryCode)
          AND (@SubControlCode IS NULL OR @SubControlCode = '' OR gl.SubControlLedgerCodeNo = @SubControlCode)
          AND (@ControlCode IS NULL OR @ControlCode = '' OR gl.ControlLedgerCodeNo = @ControlCode)
          AND (@GroupCode IS NULL OR @GroupCode = '' OR gl.GroupLedgerCodeNo = @GroupCode)
          AND (@ShowArchived = 1 OR gl.IsActive = '1' OR gl.IsActive IS NULL)
          AND (
              @Search IS NULL OR 
              gl.GeneralLedgerCodeNo LIKE @Search OR 
              gl.GeneralLedgerName LIKE @Search OR 
              gl.ShortName LIKE @Search OR 
              (cust.CustomerName IS NOT NULL AND cust.CustomerName LIKE @Search) OR
              (cust.CustomerID IS NOT NULL AND cust.CustomerID LIKE @Search) OR
              (cust.ShortName IS NOT NULL AND cust.ShortName LIKE @Search) OR
              (bank.AccountName IS NOT NULL AND bank.AccountName LIKE @Search) OR
              (bank.AccountNO IS NOT NULL AND bank.AccountNO LIKE @Search) OR
              (bki.BankName IS NOT NULL AND bki.BankName LIKE @Search) OR
              (bki.ShortName IS NOT NULL AND bki.ShortName LIKE @Search) OR
              (exphead.ExpenseHead IS NOT NULL AND exphead.ExpenseHead LIKE @Search) OR
              (exphead.ExpenseHeadID IS NOT NULL AND exphead.ExpenseHeadID LIKE @Search) OR
              ssl.SubsidiaryLedgerName LIKE @Search OR
              sc.SubControlLedgerName LIKE @Search OR
              c.ControlLedgerName LIKE @Search OR
              g.GroupLedgerName LIKE @Search
          )
    )
    SELECT 
        AutoId,
        GeneralLedgerCodeNo,
        SubsidiaryLedgerCodeNo,
        SubSubsidiaryLedgerName,
        SubControlLedgerCodeNo,
        SubControlLedgerName,
        ControlLedgerCodeNo,
        ControlLedgerName,
        GroupLedgerCodeNo,
        GroupLedgerName,
        GeneralLedgerName,
        ShortName,
        IsActive,
        COUNT(*) OVER() AS TotalCount
    FROM BaseData
    ORDER BY 
        CASE WHEN @SortOrder = 'ASC' AND @SortColumn IN ('generalledgercode', 'code') THEN GeneralLedgerCodeNo END ASC,
        CASE WHEN @SortOrder = 'DESC' AND @SortColumn IN ('generalledgercode', 'code') THEN GeneralLedgerCodeNo END DESC,
        CASE WHEN @SortOrder = 'ASC' AND @SortColumn IN ('generalledgername', 'name') THEN GeneralLedgerName END ASC,
        CASE WHEN @SortOrder = 'DESC' AND @SortColumn IN ('generalledgername', 'name') THEN GeneralLedgerName END DESC,
        CASE WHEN @SortOrder = 'ASC' AND @SortColumn = 'shortname' THEN ShortName END ASC,
        CASE WHEN @SortOrder = 'DESC' AND @SortColumn = 'shortname' THEN ShortName END DESC,
        CASE WHEN @SortOrder = 'ASC' AND @SortColumn IN ('status', 'isactive') THEN IsActive END ASC,
        CASE WHEN @SortOrder = 'DESC' AND @SortColumn IN ('status', 'isactive') THEN IsActive END DESC,
        GeneralLedgerCodeNo ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
