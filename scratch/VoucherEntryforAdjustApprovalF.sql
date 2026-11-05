CREATE OR ALTER PROCEDURE [dbo].[VoucherEntryforAdjustApprovalF]

    @JobNo NVARCHAR(50),

    @LUser NVARCHAR(50) = NULL

AS

BEGIN

    SET NOCOUNT ON;



    DECLARE @Narration NVARCHAR(500) = CONCAT('Paid From Bank/Cash for Expenses (Job No: ', @JobNo, ')');

    DECLARE @VoucherEntryCodeNo NVARCHAR(50);

    DECLARE @VoucherNo NVARCHAR(50);

    DECLARE @BranchCode NVARCHAR(50) = '02';

    DECLARE @AdvanceLedgerCode NVARCHAR(50);

    DECLARE @AdvanceLedgerName NVARCHAR(250);

    DECLARE @ExpenseLedgerCodeNo NVARCHAR(50);

    DECLARE @CustomerID NVARCHAR(50);

    DECLARE @CustGLCode NVARCHAR(50);

    DECLARE @CustGLName NVARCHAR(250);

    DECLARE @EmployeeID NVARCHAR(50);

    DECLARE @ExpenseTypeID NVARCHAR(50);

    DECLARE @TotalAdvance DECIMAL(18,2) = 0;

    DECLARE @TotalAdjust DECIMAL(18,2) = 0;

    DECLARE @MaxDetailCode INT;



    SELECT TOP 1 

        @ExpenseTypeID = d.ExpenseTypeID,

        @CustomerID = d.CustomerID,

        @EmployeeID = COALESCE(r.EmployeeID, @LUser)

    FROM Sep_Documentation d WITH (NOLOCK)

    LEFT JOIN CF_OperationalFundRequisitionEntry r WITH (NOLOCK) ON d.JobNo = r.JobNo

    WHERE d.JobNo = @JobNo;



    IF @ExpenseTypeID IS NULL

    BEGIN

        SELECT TOP 1 

            @ExpenseTypeID = ExpenseTypeId,

            @EmployeeID = COALESCE(EmployeeId, @LUser)

        FROM CF_OperationalFundRequisitionEntry WITH (NOLOCK)

        WHERE JobNo = @JobNo;

    END



    SELECT TOP 1 

        @AdvanceLedgerCode = AdvanceLedgerCode,

        @ExpenseLedgerCodeNo = ExpenseLedgerCodeNo

    FROM CF_Def_ExpenseType WITH (NOLOCK)

    WHERE ExpenseTypeID = @ExpenseTypeID;



    SELECT TOP 1 @AdvanceLedgerName = GeneralLedgerName 

    FROM Acc_GeneralLedger WITH (NOLOCK)

    WHERE GeneralLedgerCodeNo = @AdvanceLedgerCode;



    IF @AdvanceLedgerName IS NULL

    BEGIN

        SELECT TOP 1 @AdvanceLedgerName = SubSubsidiaryLedgerName 

        FROM Acc_SubSubsidiaryLedger WITH (NOLOCK)

        WHERE SubSusidiaryLedgerCodeNo = @AdvanceLedgerCode;

    END

    IF @AdvanceLedgerName IS NULL SET @AdvanceLedgerName = 'Advance Against C&F Expense';



    IF @CustomerID IS NOT NULL AND @CustomerID <> ''

    BEGIN

        SELECT TOP 1 

            @CustGLCode = GeneralLedgerCodeNo,

            @CustGLName = GeneralLedgerName

        FROM Acc_GeneralLedger WITH (NOLOCK)

        WHERE SubsidiaryLedgerCodeNo = '10200100001'

          AND (GeneralLedgerName LIKE '%' + @CustomerID + '%' OR GeneralLedgerCodeNo = @CustomerID);



        IF @CustGLCode IS NULL

        BEGIN

            SELECT TOP 1 

                @CustGLCode = GeneralLedgerCodeNo,

                @CustGLName = GeneralLedgerName

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE GeneralLedgerName LIKE '%' + @CustomerID + '%';

        END

        DECLARE @CustNameAdj NVARCHAR(250);
        SELECT TOP 1 @CustNameAdj = CustomerName
        FROM Sales_Customer WITH (NOLOCK)
        WHERE CustomerID = @CustomerID;

        IF @CustNameAdj IS NOT NULL AND @CustNameAdj <> ''
        BEGIN
            SET @CustGLName = CONCAT(@CustNameAdj, ' (', @CustomerID, ')');
        END

    END



    IF @CustGLCode IS NULL

    BEGIN

        SET @CustGLCode = @AdvanceLedgerCode;

        SET @CustGLName = @AdvanceLedgerName;

    END



    IF @EmployeeID IS NOT NULL

    BEGIN

        SELECT TOP 1 @BranchCode = BranchCode 

        FROM HRM_Employee WITH (NOLOCK)

        WHERE EmployeeID = @EmployeeID;

    END

    IF @BranchCode IS NULL OR @BranchCode = '' SET @BranchCode = '02';



    IF OBJECT_ID('tempdb..#NormDetails') IS NOT NULL DROP TABLE #NormDetails;



    SELECT 

        d.OFR_DetailsID,

        d.ExpenseHeadID,

        ISNULL(d.ActualAmount, 0) AS ActualAmount,

        ISNULL(CASE WHEN ISNULL(d.AdjustApprovalAmount, 0) > 0 THEN d.AdjustApprovalAmount ELSE d.BillAdjustAmount END, 0) AS AdjustAmount,

        CASE 

            WHEN NULLIF(LTRIM(RTRIM(d.AdjustDetailsCashBank)), '') IS NOT NULL THEN LTRIM(RTRIM(d.AdjustDetailsCashBank))

            WHEN NULLIF(LTRIM(RTRIM(d.DetailsCashBank)), '') IS NOT NULL THEN LTRIM(RTRIM(d.DetailsCashBank))

            ELSE 'Cash'

        END AS EffectiveCashBank,

        CASE 

            WHEN NULLIF(LTRIM(RTRIM(d.AdjustDetailsCashBank)), '') IS NOT NULL THEN LTRIM(RTRIM(d.AdjustDetailsBankAccount))

            WHEN NULLIF(LTRIM(RTRIM(d.DetailsCashBank)), '') IS NOT NULL THEN LTRIM(RTRIM(d.DetailsBankAccount))

            ELSE ''

        END AS EffectiveBankAccount

    INTO #NormDetails

    FROM CF_OperationalFundRequisitionDetails d WITH (NOLOCK)

    WHERE d.JobNo = @JobNo;



    SELECT 

        @TotalAdvance = ISNULL(SUM(ActualAmount), 0),

        @TotalAdjust = ISNULL(SUM(AdjustAmount), 0)

    FROM #NormDetails;



    IF @TotalAdjust <= 0 AND @TotalAdvance <= 0 

    BEGIN

        IF OBJECT_ID('tempdb..#NormDetails') IS NOT NULL DROP TABLE #NormDetails;

        RETURN;

    END



    SELECT TOP 1 @VoucherEntryCodeNo = VoucherEntryCodeNo 

    FROM Acc_VoucherEntry WITH (NOLOCK)

    WHERE InvoiceNo = @JobNo AND PeriodCodeNo = 'Adjust Approval';



    IF @VoucherEntryCodeNo IS NOT NULL

    BEGIN

        DELETE FROM Acc_VoucherEntryDetails WHERE VoucherEntryCodeNo = @VoucherEntryCodeNo;

        

        UPDATE Acc_VoucherEntry 

        SET VoucherDate = GETDATE(),

            BranchCode = @BranchCode,

            UserInfoEmployeeID = ISNULL(@EmployeeID, UserInfoEmployeeID),

            Narration = @Narration,

            IsApproved = 'Y',

            ApprovedBy = ISNULL(@LUser, ApprovedBy),

            ModifyDate = GETDATE()

        WHERE VoucherEntryCodeNo = @VoucherEntryCodeNo;

    END

    ELSE

    BEGIN

        -- Fast seek for next VoucherEntryCodeNo

        SELECT @VoucherEntryCodeNo = CAST(ISNULL(MAX(TRY_CAST(VoucherEntryCodeNo AS BIGINT)), 0) + 1 AS NVARCHAR(50))

        FROM Acc_VoucherEntry WITH (UPDLOCK, HOLDLOCK)

        WHERE TRY_CAST(VoucherEntryCodeNo AS BIGINT) IS NOT NULL;



        IF @VoucherEntryCodeNo IS NULL OR @VoucherEntryCodeNo = '0' SET @VoucherEntryCodeNo = '1';



        -- Fast seek for next VoucherNo

        DECLARE @Year2DigitAdj VARCHAR(2) = RIGHT(CONVERT(VARCHAR(8), GETDATE(), 1), 2);

        DECLARE @PrefixAdj VARCHAR(10) = 'JV_' + @Year2DigitAdj + '_';

        DECLARE @MaxSeqAdj INT;



        SELECT @MaxSeqAdj = MAX(TRY_CAST(RIGHT(VoucherNo, 6) AS INT))

        FROM Acc_VoucherEntry WITH (UPDLOCK, HOLDLOCK)

        WHERE VoucherType_Code = '5' 

          AND VoucherNo LIKE 'JV[_]' + @Year2DigitAdj + '[_]%'

          AND LEN(VoucherNo) = 12;



        SET @VoucherNo = @PrefixAdj + RIGHT('000000' + CAST(ISNULL(@MaxSeqAdj, 0) + 1 AS VARCHAR(10)), 6);



        INSERT INTO [dbo].[Acc_VoucherEntry] (

            [VoucherEntryCodeNo], [CostCenterCodeNo], [FinancialYear], [PeriodCodeNo],

            [VoucherType_Code], [VoucherDate], [VoucherNo], [Narration], [LUser], [LDate],

            [LIP], [LMAC], [CompanyCode], [UserInfoEmployeeID], [Main_CompanyCode],

            [BranchCode], [DivisionCode], [InvoiceNo], [IsApproved], [ApprovedBy], [IsActive]

        )

        VALUES (

            @VoucherEntryCodeNo, '', YEAR(GETDATE()), 'Adjust Approval',

            '5', GETDATE(), @VoucherNo, @Narration, ISNULL(@LUser, 'admin'), GETDATE(),

            '', '', '001', @EmployeeID, '001',

            @BranchCode, '', @JobNo, 'Y', ISNULL(@LUser, 'admin'), '1'

        );

    END



    -- Fast seek for next detail code

    SELECT @MaxDetailCode = ISNULL(MAX(TRY_CAST(VoucherEntryDetailsCodeNo AS INT)), 0)

    FROM Acc_VoucherEntryDetails WITH (UPDLOCK, HOLDLOCK)

    WHERE TRY_CAST(VoucherEntryDetailsCodeNo AS INT) IS NOT NULL;



    -- 1. DEBIT: Every Expense Head with AdjustAmount > 0

    DECLARE @headCur CURSOR;

    DECLARE @curExpHeadId NVARCHAR(50);

    DECLARE @curExpAmt DECIMAL(18,2);

    DECLARE @curExpHeadName NVARCHAR(250);

    DECLARE @curExpGLCode NVARCHAR(50);



    SET @headCur = CURSOR LOCAL FAST_FORWARD FOR

        SELECT d.ExpenseHeadId, SUM(d.AdjustAmount), MAX(h.ExpenseHead)

        FROM #NormDetails d

        LEFT JOIN CF_Def_ExpenseHead h WITH (NOLOCK) ON d.ExpenseHeadId = h.ExpenseHeadID

        WHERE d.AdjustAmount > 0

        GROUP BY d.ExpenseHeadId;



    OPEN @headCur;

    FETCH NEXT FROM @headCur INTO @curExpHeadId, @curExpAmt, @curExpHeadName;

    WHILE @@FETCH_STATUS = 0

    BEGIN

        SET @curExpGLCode = NULL;



        SELECT TOP 1 @curExpGLCode = GeneralLedgerCodeNo

        FROM Acc_GeneralLedger WITH (NOLOCK)

        WHERE GeneralLedgerName = @curExpHeadId AND SubsidiaryLedgerCodeNo = @ExpenseLedgerCodeNo;



        IF @curExpGLCode IS NULL

        BEGIN

            SELECT TOP 1 @curExpGLCode = GeneralLedgerCodeNo

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE GeneralLedgerName = @curExpHeadId;

        END



        IF @curExpGLCode IS NULL

        BEGIN

            SELECT TOP 1 @curExpGLCode = SubSusidiaryLedgerCodeNo

            FROM Acc_SubSubsidiaryLedger WITH (NOLOCK)

            WHERE SubSubsidiaryLedgerName = @curExpHeadId;

        END



        SET @MaxDetailCode = @MaxDetailCode + 1;

        INSERT INTO Acc_VoucherEntryDetails (

            [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

            [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

            [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

        )

        VALUES (

            CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, ISNULL(@curExpGLCode, @curExpHeadId), 'Dr',

            ISNULL(@curExpHeadName, @curExpHeadId), @curExpAmt, 0, '', '1900-01-01',

            '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

        );



        FETCH NEXT FROM @headCur INTO @curExpHeadId, @curExpAmt, @curExpHeadName;

    END

    CLOSE @headCur;

    DEALLOCATE @headCur;



    -- 2. CREDIT: Total Advance Cleared

    IF @TotalAdvance > 0

    BEGIN

        SET @MaxDetailCode = @MaxDetailCode + 1;

        INSERT INTO Acc_VoucherEntryDetails (

            [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

            [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

            [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

        )

        VALUES (

            CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, @CustGLCode, 'Cr',

            @CustGLName, 0, @TotalAdvance, '', '1900-01-01',

            '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

        );

    END



    -- 3. DIFFERENCES

    -- A) Adjust > Advance (Additional payment)

    DECLARE @extraCur CURSOR;

    DECLARE @extraSource NVARCHAR(50);

    DECLARE @extraAccount NVARCHAR(50);

    DECLARE @extraAmount DECIMAL(18,2);

    DECLARE @extraGLCode NVARCHAR(50);

    DECLARE @extraDesc NVARCHAR(250);



    SET @extraCur = CURSOR LOCAL FAST_FORWARD FOR

        SELECT 

            EffectiveCashBank,

            CASE WHEN EffectiveCashBank = 'Cash' THEN 'Cash In Hand' ELSE EffectiveBankAccount END,

            SUM(AdjustAmount - ActualAmount)

        FROM #NormDetails

        WHERE (AdjustAmount - ActualAmount) > 0

        GROUP BY EffectiveCashBank, CASE WHEN EffectiveCashBank = 'Cash' THEN 'Cash In Hand' ELSE EffectiveBankAccount END;



    OPEN @extraCur;

    FETCH NEXT FROM @extraCur INTO @extraSource, @extraAccount, @extraAmount;

    WHILE @@FETCH_STATUS = 0

    BEGIN

        SET @extraGLCode = NULL;

        SET @extraDesc = NULL;



        IF @extraSource = 'Cash'

        BEGIN

            SET @extraGLCode = '1020040000100001';

            SET @extraDesc = 'Cash In Hand';

        END

        ELSE IF @extraSource = 'Bank'

        BEGIN

            SELECT TOP 1 @extraGLCode = GeneralLedgerCodeNo 

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE SubsidiaryLedgerCodeNo = '10200400003' AND GeneralLedgerName = @extraAccount;



            IF @extraGLCode IS NULL

            BEGIN

                SELECT TOP 1 @extraGLCode = SubSusidiaryLedgerCodeNo 

                FROM Acc_SubSubsidiaryLedger WITH (NOLOCK)

                WHERE SubSubsidiaryLedgerName = @extraAccount;

            END



            SELECT TOP 1 @extraDesc = CONCAT('Bank Account:', AccountName, ' (', AccountNO, ')')

            FROM Core_BankAccountInformation WITH (NOLOCK)

            WHERE AccountNO = @extraAccount;



            IF @extraDesc IS NULL SET @extraDesc = CONCAT('Bank Account: (', @extraAccount, ')');

        END

        ELSE IF @extraSource = 'Employee'

        BEGIN

            SET @extraGLCode = @extraAccount;



            SELECT TOP 1 @extraDesc = GeneralLedgerName

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE GeneralLedgerCodeNo = @extraAccount;



            IF @extraDesc IS NULL SET @extraDesc = 'Advance to Employee';

        END



        SET @MaxDetailCode = @MaxDetailCode + 1;

        INSERT INTO Acc_VoucherEntryDetails (

            [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

            [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

            [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

        )

        VALUES (

            CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, ISNULL(@extraGLCode, @extraAccount), 'Cr',

            ISNULL(@extraDesc, 'Additional Payment'), 0, @extraAmount, '', '1900-01-01',

            '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

        );



        FETCH NEXT FROM @extraCur INTO @extraSource, @extraAccount, @extraAmount;

    END

    CLOSE @extraCur;

    DEALLOCATE @extraCur;



    -- B) Advance > Adjust (Refund)

    DECLARE @refundCur CURSOR;

    DECLARE @refSource NVARCHAR(50);

    DECLARE @refAccount NVARCHAR(50);

    DECLARE @refAmount DECIMAL(18,2);

    DECLARE @refGLCode NVARCHAR(50);

    DECLARE @refDesc NVARCHAR(250);



    SET @refundCur = CURSOR LOCAL FAST_FORWARD FOR

        SELECT 

            EffectiveCashBank,

            CASE WHEN EffectiveCashBank = 'Cash' THEN 'Cash In Hand' ELSE EffectiveBankAccount END,

            SUM(ActualAmount - AdjustAmount)

        FROM #NormDetails

        WHERE (ActualAmount - AdjustAmount) > 0

        GROUP BY EffectiveCashBank, CASE WHEN EffectiveCashBank = 'Cash' THEN 'Cash In Hand' ELSE EffectiveBankAccount END;



    OPEN @refundCur;

    FETCH NEXT FROM @refundCur INTO @refSource, @refAccount, @refAmount;

    WHILE @@FETCH_STATUS = 0

    BEGIN

        SET @refGLCode = NULL;

        SET @refDesc = NULL;



        IF @refSource = 'Cash'

        BEGIN

            SET @refGLCode = '1020040000100001';

            SET @refDesc = 'Cash In Hand';

        END

        ELSE IF @refSource = 'Bank'

        BEGIN

            SELECT TOP 1 @refGLCode = GeneralLedgerCodeNo 

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE SubsidiaryLedgerCodeNo = '10200400003' AND GeneralLedgerName = @refAccount;



            IF @refGLCode IS NULL

            BEGIN

                SELECT TOP 1 @refGLCode = SubSusidiaryLedgerCodeNo 

                FROM Acc_SubSubsidiaryLedger WITH (NOLOCK)

                WHERE SubSubsidiaryLedgerName = @refAccount;

            END



            SELECT TOP 1 @refDesc = CONCAT('Bank Account:', AccountName, ' (', AccountNO, ')')

            FROM Core_BankAccountInformation WITH (NOLOCK)

            WHERE AccountNO = @refAccount;



            IF @refDesc IS NULL SET @refDesc = CONCAT('Bank Account: (', @refAccount, ')');

        END

        ELSE IF @refSource = 'Employee'

        BEGIN

            SET @refGLCode = @refAccount;



            SELECT TOP 1 @refDesc = GeneralLedgerName

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE GeneralLedgerCodeNo = @refAccount;



            IF @refDesc IS NULL SET @refDesc = 'Advance to Employee';

        END



        SET @MaxDetailCode = @MaxDetailCode + 1;

        INSERT INTO Acc_VoucherEntryDetails (

            [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

            [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

            [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

        )

        VALUES (

            CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, ISNULL(@refGLCode, @refAccount), 'Dr',

            ISNULL(@refDesc, 'Refund Received'), @refAmount, 0, '', '1900-01-01',

            '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

        );



        FETCH NEXT FROM @refundCur INTO @refSource, @refAccount, @refAmount;

    END

    CLOSE @refundCur;

    DEALLOCATE @refundCur;



    IF OBJECT_ID('tempdb..#NormDetails') IS NOT NULL DROP TABLE #NormDetails;

END

