-- =========================================================================
-- Expense Head PROCEDURES (Formosa Logistics Limited / C&F Management)
-- =========================================================================

-- 1. High Performance Data Retrieval for Dashboard, Charts & Millisecond Grid
CREATE OR ALTER PROCEDURE [dbo].[sp_CF_GetExpenseHeadData]
    @ExpenseTypeId NVARCHAR(100) = NULL,
    @ServiceTypeId NVARCHAR(50) = NULL,
    @IsReceiptable NVARCHAR(50) = NULL,
    @Status NVARCHAR(50) = NULL,
    @SearchTerm NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        eh.TC,
        eh.ExpenseHeadID AS Id,
        ISNULL(eh.ExpenseHead, '') AS Name,
        ISNULL(eh.ShortName, '') AS Short,
        ISNULL(eh.SerialNo, '') AS Serial,
        CASE 
            WHEN eh.IsReceiptable IN ('Y', 'Yes', 'YES', '1', 'true') THEN 1 
            ELSE 0 
        END AS Rec,
        ISNULL(st.ServiceTypeName, '') AS Svc,
        ISNULL(st.ServiceTypeID, eh.ServiceTypeID) AS ServiceTypeId,
        ISNULL(et.ExpenseType, '') AS Mode,
        ISNULL(et.ExpenseTypeID, eh.ExpenseTypeID) AS ExpenseTypeId,
        eh.Amount AS Amt,
        CASE 
            WHEN eh.EntryType IS NULL OR eh.EntryType = '' OR eh.EntryType = 'Active' THEN 'Active' 
            ELSE eh.EntryType 
        END AS [Status],
        eh.LDate,
        eh.ModifyDate
    FROM dbo.CF_Def_ExpenseHead eh WITH (NOLOCK)
    LEFT JOIN dbo.CF_Def_ExpenseType et WITH (NOLOCK) 
        ON eh.ExpenseTypeID = et.ExpenseTypeID
    LEFT JOIN dbo.Core_ServiceType st WITH (NOLOCK) 
        ON eh.ServiceTypeID = st.ServiceTypeID
    WHERE (@ExpenseTypeId IS NULL OR @ExpenseTypeId = '' OR eh.ExpenseTypeID = @ExpenseTypeId OR et.ExpenseType = @ExpenseTypeId)
      AND (@ServiceTypeId IS NULL OR @ServiceTypeId = '' OR eh.ServiceTypeID = @ServiceTypeId OR st.ServiceTypeName = @ServiceTypeId)
      AND (
          @IsReceiptable IS NULL OR @IsReceiptable = '' OR
          (@IsReceiptable IN ('1', 'Y', 'Yes') AND eh.IsReceiptable IN ('Y', 'Yes', 'YES', '1')) OR
          (@IsReceiptable IN ('0', 'N', 'No') AND (eh.IsReceiptable IS NULL OR eh.IsReceiptable NOT IN ('Y', 'Yes', 'YES', '1')))
      )
      AND (
          @Status IS NULL OR @Status = '' OR
          (@Status = 'Active' AND (eh.EntryType IS NULL OR eh.EntryType = '' OR eh.EntryType = 'Active')) OR
          (@Status = 'Inactive' AND eh.EntryType = 'Inactive')
      )
      AND (
          @SearchTerm IS NULL OR @SearchTerm = '' OR
          eh.ExpenseHeadID LIKE '%' + @SearchTerm + '%' OR
          eh.ExpenseHead LIKE '%' + @SearchTerm + '%' OR
          eh.ShortName LIKE '%' + @SearchTerm + '%' OR
          et.ExpenseType LIKE '%' + @SearchTerm + '%' OR
          st.ServiceTypeName LIKE '%' + @SearchTerm + '%'
      )
    ORDER BY 
        TRY_CAST(eh.ExpenseHeadID AS INT) DESC,
        eh.ExpenseHeadID DESC;
END;
GO

-- 2. Fast Save or Update Stored Procedure
CREATE OR ALTER PROCEDURE [dbo].[sp_CF_SaveOrUpdateExpenseHead]
    @TC INT = 0,
    @ExpenseHeadID NVARCHAR(50) = NULL OUTPUT,
    @ExpenseHead NVARCHAR(150),
    @ShortName NVARCHAR(50) = NULL,
    @SerialNo NVARCHAR(50) = NULL,
    @IsReceiptable NVARCHAR(50) = 'N',
    @Amount DECIMAL(18,2) = NULL,
    @ServiceTypeID NVARCHAR(50),
    @ExpenseTypeID NVARCHAR(100),
    @Status NVARCHAR(50) = 'Active',
    @User NVARCHAR(50) = NULL,
    @IP NVARCHAR(50) = NULL,
    @MAC NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @IsReceiptable IN ('1', 'true', 'Y', 'Yes', 'YES')
            SET @IsReceiptable = 'Y';
        ELSE
            SET @IsReceiptable = 'N';

        IF @Status IS NULL OR @Status = ''
            SET @Status = 'Active';

        -- Resolve ExpenseTypeID if passed as Name
        IF EXISTS (SELECT 1 FROM dbo.CF_Def_ExpenseType WITH (NOLOCK) WHERE LTRIM(RTRIM(ExpenseType)) = LTRIM(RTRIM(@ExpenseTypeID)))
        BEGIN
            SELECT TOP 1 @ExpenseTypeID = ExpenseTypeID 
            FROM dbo.CF_Def_ExpenseType WITH (NOLOCK) 
            WHERE LTRIM(RTRIM(ExpenseType)) = LTRIM(RTRIM(@ExpenseTypeID));
        END

        -- Resolve ServiceTypeID if passed as Name
        IF EXISTS (SELECT 1 FROM dbo.Core_ServiceType WITH (NOLOCK) WHERE LTRIM(RTRIM(ServiceTypeName)) = LTRIM(RTRIM(@ServiceTypeID)))
        BEGIN
            SELECT TOP 1 @ServiceTypeID = ServiceTypeID 
            FROM dbo.Core_ServiceType WITH (NOLOCK) 
            WHERE LTRIM(RTRIM(ServiceTypeName)) = LTRIM(RTRIM(@ServiceTypeID));
        END

        IF ISNULL(@TC, 0) <= 0
        BEGIN
            IF @ExpenseHeadID IS NULL OR LTRIM(RTRIM(@ExpenseHeadID)) = ''
            BEGIN
                DECLARE @MaxId INT = 0;
                SELECT @MaxId = MAX(TRY_CAST(ExpenseHeadID AS INT)) FROM dbo.CF_Def_ExpenseHead;
                SET @ExpenseHeadID = RIGHT('0000' + CAST(ISNULL(@MaxId, 0) + 1 AS VARCHAR(10)), 4);
            END
            ELSE
            BEGIN
                -- If provided ID already exists, generate next unique ID
                IF EXISTS (SELECT 1 FROM dbo.CF_Def_ExpenseHead WHERE ExpenseHeadID = @ExpenseHeadID)
                BEGIN
                    DECLARE @MaxIdExisting INT = 0;
                    SELECT @MaxIdExisting = MAX(TRY_CAST(ExpenseHeadID AS INT)) FROM dbo.CF_Def_ExpenseHead;
                    SET @ExpenseHeadID = RIGHT('0000' + CAST(ISNULL(@MaxIdExisting, 0) + 1 AS VARCHAR(10)), 4);
                END
            END

            IF @SerialNo IS NULL OR LTRIM(RTRIM(@SerialNo)) = '' OR TRY_CAST(@SerialNo AS INT) <= 0
            BEGIN
                DECLARE @NextSerial INT = 1;
                SELECT @NextSerial = ISNULL(MAX(TRY_CAST(SerialNo AS INT)), 0) + 1
                FROM dbo.CF_Def_ExpenseHead
                WHERE ExpenseTypeID = @ExpenseTypeID
                  AND (ServiceTypeID = @ServiceTypeID OR @ServiceTypeID IS NULL)
                  AND IsReceiptable = @IsReceiptable;

                SET @SerialNo = CAST(@NextSerial AS NVARCHAR(50));
            END

            INSERT INTO dbo.CF_Def_ExpenseHead
            (
                ExpenseHeadID,
                ExpenseHead,
                ShortName,
                SerialNo,
                IsReceiptable,
                Amount,
                ServiceTypeID,
                ExpenseTypeID,
                EntryType,
                LUser,
                LDate,
                LIP,
                LMAC
            )
            VALUES
            (
                @ExpenseHeadID,
                @ExpenseHead,
                ISNULL(@ShortName, ''),
                @SerialNo,
                @IsReceiptable,
                @Amount,
                @ServiceTypeID,
                @ExpenseTypeID,
                @Status,
                @User,
                GETDATE(),
                @IP,
                @MAC
            );

            SET @TC = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE dbo.CF_Def_ExpenseHead
            SET
                ExpenseHead = @ExpenseHead,
                ShortName = ISNULL(@ShortName, ''),
                SerialNo = ISNULL(@SerialNo, SerialNo),
                IsReceiptable = @IsReceiptable,
                Amount = @Amount,
                ServiceTypeID = @ServiceTypeID,
                ExpenseTypeID = @ExpenseTypeID,
                EntryType = @Status,
                ModifyDate = GETDATE(),
                LIP = ISNULL(@IP, LIP),
                LMAC = ISNULL(@MAC, LMAC)
            WHERE TC = @TC;

            SELECT @ExpenseHeadID = ExpenseHeadID FROM dbo.CF_Def_ExpenseHead WHERE TC = @TC;
        END

        COMMIT TRANSACTION;

        SELECT 
            eh.TC,
            eh.ExpenseHeadID,
            eh.ExpenseHeadID AS Id,
            ISNULL(eh.ExpenseHead, '') AS ExpenseHead,
            ISNULL(eh.ExpenseHead, '') AS Name,
            ISNULL(eh.ShortName, '') AS ShortName,
            ISNULL(eh.ShortName, '') AS Short,
            ISNULL(eh.SerialNo, '') AS SerialNo,
            ISNULL(eh.SerialNo, '') AS Serial,
            eh.IsReceiptable,
            CASE 
                WHEN eh.IsReceiptable IN ('Y', 'Yes', 'YES', '1', 'true') THEN 1 
                ELSE 0 
            END AS Rec,
            ISNULL(st.ServiceTypeName, '') AS ServiceTypeName,
            ISNULL(st.ServiceTypeName, '') AS Svc,
            ISNULL(st.ServiceTypeID, eh.ServiceTypeID) AS ServiceTypeId,
            ISNULL(et.ExpenseType, '') AS ExpenseType,
            ISNULL(et.ExpenseType, '') AS Mode,
            ISNULL(et.ExpenseTypeID, eh.ExpenseTypeID) AS ExpenseTypeId,
            eh.Amount,
            eh.Amount AS Amt,
            CASE 
                WHEN eh.EntryType IS NULL OR eh.EntryType = '' OR eh.EntryType = 'Active' THEN 'Active' 
                ELSE eh.EntryType 
            END AS EntryType,
            CASE 
                WHEN eh.EntryType IS NULL OR eh.EntryType = '' OR eh.EntryType = 'Active' THEN 'Active' 
                ELSE eh.EntryType 
            END AS [Status],
            eh.LDate,
            eh.ModifyDate
        FROM dbo.CF_Def_ExpenseHead eh WITH (NOLOCK)
        LEFT JOIN dbo.CF_Def_ExpenseType et WITH (NOLOCK) ON eh.ExpenseTypeID = et.ExpenseTypeID
        LEFT JOIN dbo.Core_ServiceType st WITH (NOLOCK) ON eh.ServiceTypeID = st.ServiceTypeID
        WHERE eh.TC = @TC;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 3. High Performance Bulk Delete Procedure
CREATE OR ALTER PROCEDURE [dbo].[sp_CF_DeleteExpenseHead]
    @TCs NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.CF_Def_ExpenseHead
        WHERE TC IN (
            SELECT TRY_CAST(value AS INT) 
            FROM STRING_SPLIT(@TCs, ',') 
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        );

        DECLARE @Count INT = @@ROWCOUNT;
        COMMIT TRANSACTION;
        SELECT @Count AS DeletedRows;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 4. Report Procedure with Company & Formatting
CREATE OR ALTER PROCEDURE [dbo].[GetExpenseHeadReport]
    @ExpenseTypeId NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ISNULL(c.CompanyName, 'Formosa Logistics Limited') AS CompanyName,
        ISNULL(c.Address1, '21, Hazaribagh, Dhaka-1209') AS Address1,
        eh.ExpenseHeadID,
        eh.ExpenseHead,
        ISNULL(eh.ShortName, '') AS ShortName,
        ISNULL(eh.SerialNo, '') AS SerialNo,
        CASE WHEN eh.IsReceiptable IN ('Y', 'Yes', 'YES', '1') THEN 'Yes' ELSE 'No' END AS IsReceiptable,
        ISNULL(st.ServiceTypeName, '') AS ServiceProvidedBy,
        ISNULL(et.ExpenseType, '') AS ExpenseType,
        eh.Amount,
        CASE WHEN eh.EntryType IS NULL OR eh.EntryType = '' OR eh.EntryType = 'Active' THEN 'Active' ELSE eh.EntryType END AS [Status]
    FROM dbo.CF_Def_ExpenseHead eh WITH (NOLOCK)
    LEFT JOIN dbo.CF_Def_ExpenseType et WITH (NOLOCK) 
        ON eh.ExpenseTypeID = et.ExpenseTypeID
    LEFT JOIN dbo.Core_ServiceType st WITH (NOLOCK) 
        ON eh.ServiceTypeID = st.ServiceTypeID
    CROSS JOIN (SELECT TOP 1 CompanyName, Address1 FROM dbo.Core_Company WITH (NOLOCK)) c
    WHERE (@ExpenseTypeId IS NULL OR @ExpenseTypeId = '' OR et.ExpenseTypeID = @ExpenseTypeId)
    ORDER BY 
        TRY_CAST(eh.ExpenseHeadID AS INT) DESC,
        eh.ExpenseHeadID DESC;
END;
GO



-- 4. Get Shipment Modes for Dropdown
CREATE OR ALTER PROCEDURE [dbo].[sp_CF_GetShipmentModes]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ExpenseTypeID AS [Id], ExpenseType AS [Name], ExpenseTypeID AS [Code]
    FROM dbo.CF_Def_ExpenseType WITH (NOLOCK)
    ORDER BY ExpenseType;
END;
GO

-- 5. Get Service Providers for Dropdown
CREATE OR ALTER PROCEDURE [dbo].[sp_CF_GetServiceProviders]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ServiceTypeID AS [Id], ServiceTypeName AS [Name], ServiceTypeID AS [Code]
    FROM dbo.Core_ServiceType WITH (NOLOCK)
    ORDER BY ServiceTypeName;
END;
GO

-- 6. Get All Expense Head Dropdowns at once (Single SP Call)
CREATE OR ALTER PROCEDURE [dbo].[sp_CF_GetExpenseHeadDropdowns]
AS
BEGIN
    SET NOCOUNT ON;

    -- Shipment Modes
    SELECT ExpenseTypeID AS [Id], ExpenseType AS [Name], ExpenseTypeID AS [Code]
    FROM dbo.CF_Def_ExpenseType WITH (NOLOCK)
    ORDER BY ExpenseType;

    -- Service Providers
    SELECT ServiceTypeID AS [Id], ServiceTypeName AS [Name], ServiceTypeID AS [Code]
    FROM dbo.Core_ServiceType WITH (NOLOCK)
    ORDER BY ServiceTypeName;
END;
GO
