CREATE OR ALTER PROCEDURE [dbo].[VoucherEntryForOperationalFundRequisitionApprovalF]

    @JobNo NVARCHAR(50),

    @LUser NVARCHAR(50) = NULL

AS

BEGIN

    SET NOCOUNT ON;



    DECLARE @Narration NVARCHAR(500);

    DECLARE @CashAmount DECIMAL(18,2) = 0;

    DECLARE @BankAmount DECIMAL(18,2) = 0;

    DECLARE @EmployeeAmount DECIMAL(18,2) = 0;

    DECLARE @TotalApproved DECIMAL(18,2) = 0;

    DECLARE @VoucherEntryCodeNo NVARCHAR(50);

    DECLARE @VoucherNo NVARCHAR(50);

    DECLARE @BranchCode NVARCHAR(50) = '02';

    DECLARE @AdvanceLedgerCode NVARCHAR(50);

    DECLARE @AdvanceLedgerName NVARCHAR(250);

    DECLARE @CustomerID NVARCHAR(50);

    DECLARE @CustGLCode NVARCHAR(50);

    DECLARE @CustGLName NVARCHAR(250);

    DECLARE @EmployeeID NVARCHAR(50);

    DECLARE @ExpenseTypeID NVARCHAR(50);

    DECLARE @MaxDetailCode INT;



    -- Fast indexed lookups

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

        @AdvanceLedgerCode = AdvanceLedgerCode

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



    SELECT @CashAmount = ISNULL(SUM(ISNULL(ActualAmount, 0)), 0)

    FROM CF_OperationalFundRequisitionDetails WITH (NOLOCK)

    WHERE JobNo = @JobNo AND DetailsCashBank = 'Cash';



    SELECT @BankAmount = ISNULL(SUM(ISNULL(ActualAmount, 0)), 0)

    FROM CF_OperationalFundRequisitionDetails WITH (NOLOCK)

    WHERE JobNo = @JobNo AND DetailsCashBank = 'Bank';



    SELECT @EmployeeAmount = ISNULL(SUM(ISNULL(ActualAmount, 0)), 0)

    FROM CF_OperationalFundRequisitionDetails WITH (NOLOCK)

    WHERE JobNo = @JobNo AND DetailsCashBank = 'Employee';



    SELECT @TotalApproved = ISNULL(SUM(ISNULL(ActualAmount, 0)), 0)

    FROM CF_OperationalFundRequisitionDetails WITH (NOLOCK)

    WHERE JobNo = @JobNo AND ISNULL(DetailsCashBank, '') <> '';



    IF @TotalApproved <= 0 RETURN;



    SET @Narration = CONCAT('Recorded paid  for ', @AdvanceLedgerName, ' (Job No: ', @JobNo, ')');



    SELECT TOP 1 @VoucherEntryCodeNo = VoucherEntryCodeNo 

    FROM Acc_VoucherEntry WITH (NOLOCK)

    WHERE InvoiceNo = @JobNo AND PeriodCodeNo = 'Requisition Approval';



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

        DECLARE @Year2Digit VARCHAR(2) = RIGHT(CONVERT(VARCHAR(8), GETDATE(), 1), 2);

        DECLARE @Prefix VARCHAR(10) = 'JV_' + @Year2Digit + '_';

        DECLARE @MaxSeq INT;



        SELECT @MaxSeq = MAX(TRY_CAST(RIGHT(VoucherNo, 6) AS INT))

        FROM Acc_VoucherEntry WITH (UPDLOCK, HOLDLOCK)

        WHERE VoucherType_Code = '5' 

          AND VoucherNo LIKE 'JV[_]' + @Year2Digit + '[_]%'

          AND LEN(VoucherNo) = 12;



        SET @VoucherNo = @Prefix + RIGHT('000000' + CAST(ISNULL(@MaxSeq, 0) + 1 AS VARCHAR(10)), 6);



        INSERT INTO [dbo].[Acc_VoucherEntry] (

            [VoucherEntryCodeNo], [CostCenterCodeNo], [FinancialYear], [PeriodCodeNo],

            [VoucherType_Code], [VoucherDate], [VoucherNo], [Narration], [LUser], [LDate],

            [LIP], [LMAC], [CompanyCode], [UserInfoEmployeeID], [Main_CompanyCode],

            [BranchCode], [DivisionCode], [InvoiceNo], [IsApproved], [ApprovedBy], [IsActive]

        )

        VALUES (

            @VoucherEntryCodeNo, '', YEAR(GETDATE()), 'Requisition Approval',

            '5', GETDATE(), @VoucherNo, @Narration, ISNULL(@LUser, 'admin'), GETDATE(),

            '', '', '001', @EmployeeID, '001',

            @BranchCode, '', @JobNo, 'Y', ISNULL(@LUser, 'admin'), '1'

        );

    END



    -- Fast seek for next detail code

    SELECT @MaxDetailCode = ISNULL(MAX(TRY_CAST(VoucherEntryDetailsCodeNo AS INT)), 0)

    FROM Acc_VoucherEntryDetails WITH (UPDLOCK, HOLDLOCK)

    WHERE TRY_CAST(VoucherEntryDetailsCodeNo AS INT) IS NOT NULL;



    -- 1. DEBIT: Customer Account

    SET @MaxDetailCode = @MaxDetailCode + 1;

    INSERT INTO Acc_VoucherEntryDetails (

        [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

        [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

        [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

    )

    VALUES (

        CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, @CustGLCode, 'Dr',

        @CustGLName, @TotalApproved, 0, '', '1900-01-01',

        '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

    );



    -- 2. CREDIT: Cash

    IF @CashAmount > 0

    BEGIN

        SET @MaxDetailCode = @MaxDetailCode + 1;

        INSERT INTO Acc_VoucherEntryDetails (

            [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

            [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

            [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

        )

        VALUES (

            CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, '1020040000100001', 'Cr',

            'Cash In Hand', 0, @CashAmount, '', '1900-01-01',

            '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

        );

    END



    -- 3. CREDIT: Bank Accounts

    IF @BankAmount > 0

    BEGIN

        DECLARE @bankCur CURSOR;

        DECLARE @curBankAmount DECIMAL(18,2);

        DECLARE @curBankAccount NVARCHAR(50);

        DECLARE @curBankGLCode NVARCHAR(50);

        DECLARE @curBankName NVARCHAR(250);



        SET @bankCur = CURSOR LOCAL FAST_FORWARD FOR

            SELECT DetailsBankAccount, SUM(ActualAmount)

            FROM CF_OperationalFundRequisitionDetails WITH (NOLOCK)

            WHERE JobNo = @JobNo AND DetailsCashBank = 'Bank'

            GROUP BY DetailsBankAccount;



        OPEN @bankCur;

        FETCH NEXT FROM @bankCur INTO @curBankAccount, @curBankAmount;

        WHILE @@FETCH_STATUS = 0

        BEGIN

            SET @curBankGLCode = NULL;

            SET @curBankName = NULL;



            SELECT TOP 1 @curBankGLCode = GeneralLedgerCodeNo 

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE SubsidiaryLedgerCodeNo = '10200400003' AND GeneralLedgerName = @curBankAccount;



            IF @curBankGLCode IS NULL

            BEGIN

                SELECT TOP 1 @curBankGLCode = SubSusidiaryLedgerCodeNo 

                FROM Acc_SubSubsidiaryLedger WITH (NOLOCK)

                WHERE SubSubsidiaryLedgerName = @curBankAccount;

            END



            SELECT TOP 1 @curBankName = CONCAT('Bank Account:', AccountName, ' (', AccountNO, ')')

            FROM Core_BankAccountInformation WITH (NOLOCK)

            WHERE AccountNO = @curBankAccount;



            IF @curBankName IS NULL 

                SET @curBankName = CONCAT('Bank Account: (', @curBankAccount, ')');



            SET @MaxDetailCode = @MaxDetailCode + 1;

            INSERT INTO Acc_VoucherEntryDetails (

                [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

                [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

                [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

            )

            VALUES (

                CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, ISNULL(@curBankGLCode, @curBankAccount), 'Cr',

                @curBankName, 0, @curBankAmount, '', '1900-01-01',

                '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

            );



            FETCH NEXT FROM @bankCur INTO @curBankAccount, @curBankAmount;

        END

        CLOSE @bankCur;

        DEALLOCATE @bankCur;

    END



    -- 4. CREDIT: Employee

    IF @EmployeeAmount > 0

    BEGIN

        DECLARE @empCur CURSOR;

        DECLARE @curEmpAmount DECIMAL(18,2);

        DECLARE @curEmpAccount NVARCHAR(50);

        DECLARE @curEmpDesc NVARCHAR(250);



        SET @empCur = CURSOR LOCAL FAST_FORWARD FOR

            SELECT DetailsBankAccount, SUM(ActualAmount)

            FROM CF_OperationalFundRequisitionDetails WITH (NOLOCK)

            WHERE JobNo = @JobNo AND DetailsCashBank = 'Employee'

            GROUP BY DetailsBankAccount;



        OPEN @empCur;

        FETCH NEXT FROM @empCur INTO @curEmpAccount, @curEmpAmount;

        WHILE @@FETCH_STATUS = 0

        BEGIN

            SELECT TOP 1 @curEmpDesc = GeneralLedgerName

            FROM Acc_GeneralLedger WITH (NOLOCK)

            WHERE GeneralLedgerCodeNo = @curEmpAccount;



            IF @curEmpDesc IS NULL SET @curEmpDesc = 'Advance to Employee';



            SET @MaxDetailCode = @MaxDetailCode + 1;

            INSERT INTO Acc_VoucherEntryDetails (

                [VoucherEntryDetailsCodeNo], [VoucherEntryCodeNo], [AccCode], [TrType],

                [Description], [DebitAmount], [CreditAmount], [ChequeNo], [ChequeDate],

                [BankName], [Branch], [LUser], [LDate], [LIP], [LMAC], [IsActive]

            )

            VALUES (

                CAST(@MaxDetailCode AS NVARCHAR(50)), @VoucherEntryCodeNo, @curEmpAccount, 'Cr',

                @curEmpDesc, 0, @curEmpAmount, '', '1900-01-01',

                '', '', ISNULL(@LUser, ''), GETDATE(), '', '', '1'

            );



            FETCH NEXT FROM @empCur INTO @curEmpAccount, @curEmpAmount;

        END

        CLOSE @empCur;

        DEALLOCATE @empCur;

    END

END

