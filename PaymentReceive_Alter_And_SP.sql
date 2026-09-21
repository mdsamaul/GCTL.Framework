USE [GCTL_ERP_DB]
GO

/****** 1. Add 4 Fields to [dbo].[PaymentReceive] ******/
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentReceive' AND COLUMN_NAME = 'AIT')
BEGIN
    ALTER TABLE [dbo].[PaymentReceive] ADD [AIT] DECIMAL(18, 5) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentReceive' AND COLUMN_NAME = 'AITChallanNo')
BEGIN
    ALTER TABLE [dbo].[PaymentReceive] ADD [AITChallanNo] NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentReceive' AND COLUMN_NAME = 'AITInvoiceNo')
BEGIN
    ALTER TABLE [dbo].[PaymentReceive] ADD [AITInvoiceNo] NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentReceive' AND COLUMN_NAME = 'AITChallanUpload')
BEGIN
    ALTER TABLE [dbo].[PaymentReceive] ADD [AITChallanUpload] NVARCHAR(250) NULL;
END
GO

/****** 2. Stored Procedure [dbo].[prc_LoadGridPaymentReceived] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[prc_LoadGridPaymentReceived]
    @FromDate DATETIME,
    @ToDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.PaymentReceiveID,
        FORMAT(p.PaymentReceiveDate, 'dd/MM/yyyy') AS PaymentReceiveDate,
        c.CustomerID,
        c.CustomerName,
        c.CustomerAddress,
        pt.PaymentType,
        'Mobile Apps' AS ReceiveVia,
        pm.PaymentModeName,
        p.Amount,
        ISNULL(p.BankFeeOnlineCharge, 0.00) AS BankCharge,
        COALESCE(NULLIF(p.AgainstBillList, ''), (
            SELECT STRING_AGG(d.InvoiceNo, ', ') 
            FROM PaymentReceiveDetails d 
            WHERE d.PaymentReceiveID = p.PaymentReceiveID
        ), '') AS BillList,
        ISNULL(NULLIF(LTRIM(RTRIM(CONCAT(emp.FirstName, ' ', ISNULL(emp.LastName, '')))), ''), ISNULL(p.Receivedby, '')) AS Receivedby,
        'Approved' AS [Status],
        p.AIT,
        p.AITChallanNo,
        p.AITInvoiceNo,
        p.AITChallanUpload
    FROM PaymentReceive p
    LEFT OUTER JOIN Sales_Customer c ON p.CustomerID = c.CustomerID
    LEFT OUTER JOIN Sales_Def_PaymentType pt ON p.PaymentTypeID = pt.PaymentTypeID
    LEFT OUTER JOIN Sales_Def_PaymentMode pm ON p.PaymentModeCode = pm.PaymentModeID
    LEFT OUTER JOIN HRM_Employee emp ON p.Receivedby = emp.EmployeeID
    WHERE CAST(p.PaymentReceiveDate AS DATE) BETWEEN CAST(@FromDate AS DATE) AND CAST(@ToDate AS DATE)
    ORDER BY p.PaymentReceiveDate DESC, p.AutoID DESC;
END
GO
