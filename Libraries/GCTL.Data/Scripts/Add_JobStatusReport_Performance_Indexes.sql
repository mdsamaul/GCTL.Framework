USE [GCTL_ERP_DB_Copy];
GO

-- =========================================================================
-- PERFORMANCE OPTIMIZATION INDEXES FOR JOB STATUS REPORT & EXPORTS
-- Target Tables: Sep_Documentation, CF_BillEntry, 
--                CF_OperationalFundRequisitionEntry, Acc_VoucherEntry
-- =========================================================================

-- 1. Sep_Documentation: Covering index on Date, CustomerID, ExpenseTypeID, PortNameId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SepDoc_Date_JobStatus_Cover' AND object_id = OBJECT_ID('dbo.Sep_Documentation'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SepDoc_Date_JobStatus_Cover
    ON [dbo].[Sep_Documentation] ([Date], [CustomerID], [ExpenseTypeID], [PortNameId])
    INCLUDE (
        [JobNo], [DocReceivedDate], [InvoiceNo], [InvoiceValue], [CurrencyId], 
        [Quntity1], [MaterialDescription], [BillNo], [ShipmentStatus], [BENo], 
        [BEDate], [Remarks], [Term], [VesselName], [Vessel_RottNo], [ContainerNo], 
        [NameOfFreightForwarder], [FreightForwarderPhoneNo], [EmployeeID], 
        [HAWB], [MAWB], [LCNo], [DateOfClearence], [ETA], [LCValue]
    );
    PRINT 'Created index IX_SepDoc_Date_JobStatus_Cover on Sep_Documentation.';
END
ELSE
BEGIN
    PRINT 'Index IX_SepDoc_Date_JobStatus_Cover already exists on Sep_Documentation.';
END
GO

-- 2. CF_BillEntry: Covering index on JobNo, TC DESC for instant OUTER APPLY lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_BillEntry_JobNo_TC_Perf' AND object_id = OBJECT_ID('dbo.CF_BillEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_BillEntry_JobNo_TC_Perf
    ON [dbo].[CF_BillEntry] ([JobNo], [TC] DESC)
    INCLUDE ([BillNo], [BillDate]);
    PRINT 'Created index IX_CF_BillEntry_JobNo_TC_Perf on CF_BillEntry.';
END
ELSE
BEGIN
    PRINT 'Index IX_CF_BillEntry_JobNo_TC_Perf already exists on CF_BillEntry.';
END
GO

-- 3. CF_OperationalFundRequisitionEntry: Covering index on JobNo, TC DESC for instant OFR lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_OFREntry_JobNo_TC_Perf' AND object_id = OBJECT_ID('dbo.CF_OperationalFundRequisitionEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_OFREntry_JobNo_TC_Perf
    ON [dbo].[CF_OperationalFundRequisitionEntry] ([JobNo], [TC] DESC)
    INCLUDE ([OFRNo], [OFRDate], [ApprovedUser], [BillAdjustApprovalUser]);
    PRINT 'Created index IX_CF_OFREntry_JobNo_TC_Perf on CF_OperationalFundRequisitionEntry.';
END
ELSE
BEGIN
    PRINT 'Index IX_CF_OFREntry_JobNo_TC_Perf already exists on CF_OperationalFundRequisitionEntry.';
END
GO

-- 4. Acc_VoucherEntry: Covering index on InvoiceNo, autoId DESC for instant Voucher status lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Acc_VoucherEntry_InvoiceNo_autoId_Perf' AND object_id = OBJECT_ID('dbo.Acc_VoucherEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Acc_VoucherEntry_InvoiceNo_autoId_Perf
    ON [dbo].[Acc_VoucherEntry] ([InvoiceNo], [autoId] DESC)
    INCLUDE ([VoucherNo], [IsApproved]);
    PRINT 'Created index IX_Acc_VoucherEntry_InvoiceNo_autoId_Perf on Acc_VoucherEntry.';
END
ELSE
BEGIN
    PRINT 'Index IX_Acc_VoucherEntry_InvoiceNo_autoId_Perf already exists on Acc_VoucherEntry.';
END
GO

