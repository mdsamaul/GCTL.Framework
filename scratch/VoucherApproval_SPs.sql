-- =========================================================================
-- Stored Procedure: sp_GetVoucherApprovalList
-- Purpose: Get dynamic vouchers for Approval Entry from Acc_VoucherEntry
-- Matches: PeriodCodeNo = 'Adjust Approval' / General Vouchers with InvoiceNo = JobNo
-- Default Filter: Shows vouchers where IsApproved IS NULL OR IsApproved = '' OR IsApproved NOT IN ('Y', '1', 'Approved')
-- =========================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetVoucherApprovalList]
    @CustomerCode   NVARCHAR(50) = NULL,
    @FromDate       DATETIME = NULL,
    @ToDate         DATETIME = NULL,
    @VoucherStatus  NVARCHAR(50) = 'Not Approved' -- 'Not Approved', 'Approved', 'All'
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        v.AutoId,
        v.VoucherEntryCodeNo,
        v.VoucherNo,
        CONVERT(VARCHAR(10), v.VoucherDate, 103) AS VoucherDate,
        ISNULL(v.Narration, '') AS Narration,
        ISNULL(b.BranchName, 'Chattogram Branch') AS Branch,
        ISNULL(d.TotalAmount, 0) AS Amount,
        ISNULL(v.InvoiceNo, '') AS JobNo,
        ISNULL(s.Hawb, '') AS HAWB,
        ISNULL(s.Beno, '') AS BENo,
        CASE 
            WHEN v.IsApproved IN ('Y', '1', 'Approved', 'Posted') THEN 'Approved'
            WHEN v.IsApproved IN ('Disapproved', 'Rejected') THEN 'Disapproved'
            ELSE 'Not Approved'
        END AS IsApproved,
        ISNULL(v.ApprovedBy, '') AS ApprovedBy,
        CASE 
            WHEN v.ApprovedDate IS NOT NULL THEN CONVERT(VARCHAR(10), v.ApprovedDate, 103)
            ELSE ''
        END AS ApprovedDate
    FROM [dbo].[Acc_VoucherEntry] v WITH (NOLOCK)
    LEFT JOIN [dbo].[Core_Branch] b WITH (NOLOCK) 
        ON v.BranchCode = b.BranchCode
    LEFT JOIN (
        SELECT VoucherEntryCodeNo, SUM(ISNULL(DebitAmount, 0)) AS TotalAmount
        FROM [dbo].[Acc_VoucherEntryDetails] WITH (NOLOCK)
        GROUP BY VoucherEntryCodeNo
    ) d ON v.VoucherEntryCodeNo = d.VoucherEntryCodeNo
    LEFT JOIN [dbo].[Sep_Documentation] s WITH (NOLOCK) 
        ON v.InvoiceNo = s.JobNo
    WHERE 
        (@FromDate IS NULL OR v.VoucherDate >= @FromDate)
        AND (@ToDate IS NULL OR v.VoucherDate <= DATEADD(DAY, 1, @ToDate))
        AND (
            @CustomerCode IS NULL 
            OR @CustomerCode = '' 
            OR s.CustomerId = @CustomerCode 
            OR s.CustomerID_Importer = @CustomerCode
        )
        AND (
            @VoucherStatus = 'All'
            OR (@VoucherStatus = 'Approved' AND v.IsApproved IN ('Y', '1', 'Approved', 'Posted'))
            OR (@VoucherStatus = 'Not Approved' AND (v.IsApproved IS NULL OR v.IsApproved = '' OR v.IsApproved NOT IN ('Y', '1', 'Approved', 'Posted', 'Disapproved')))
            OR (@VoucherStatus = 'Disapproved' AND v.IsApproved IN ('Disapproved', 'Rejected'))
        )
    ORDER BY 
        v.VoucherDate DESC, 
        v.AutoId DESC;
END
GO

-- =========================================================================
-- Stored Procedure: sp_UpdateVoucherApprovalStatus
-- Purpose: Bulk approve or disapprove vouchers
-- =========================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateVoucherApprovalStatus]
    @VoucherNos     NVARCHAR(MAX), -- Comma separated Voucher Nos
    @Status         NVARCHAR(50),  -- 'Approved' or 'Disapproved'
    @ApprovedBy     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE v
    SET 
        v.IsApproved = CASE WHEN @Status = 'Approved' THEN 'Y' ELSE 'Disapproved' END,
        v.ApprovedBy = @ApprovedBy,
        v.ApprovedDate = GETDATE(),
        v.ModifyDate = GETDATE()
    FROM [dbo].[Acc_VoucherEntry] v
    WHERE v.VoucherNo IN (SELECT TRIM(value) FROM STRING_SPLIT(@VoucherNos, ','));

    SELECT @@ROWCOUNT AS UpdatedCount;
END
GO
