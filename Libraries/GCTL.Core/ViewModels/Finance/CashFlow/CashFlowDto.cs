using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.CashFlow
{
    /// <summary>
    /// Stored Procedure [SP_Rpt_CashFlowStatement] detail item mapping DTO
    /// </summary>
    public class CashFlowSpResultDto
    {
        public string ActivityType { get; set; } = string.Empty;
        public int ActivityOrder { get; set; }
        public string GeneralLedgerCodeNo { get; set; } = string.Empty;
        public string GeneralLedgerName { get; set; } = string.Empty;
        public string? SubSubsidiaryLedgerName { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal NetAmount { get; set; }
        public string? DisplayLabel { get; set; }
    }

    /// <summary>
    /// Stored Procedure [SP_Rpt_CashFlowStatement] summary reconciliation DTO
    /// </summary>
    public class CashFlowSpSummaryDto
    {
        public decimal OpeningCashAndBank { get; set; }
        public decimal OperatingCashFlow { get; set; }
        public decimal InvestingCashFlow { get; set; }
        public decimal FinancingCashFlow { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal ClosingCashAndBank { get; set; }
        public decimal Difference { get; set; }
        public bool IsReconciled { get; set; }

        public decimal OpeningCash => OpeningCashAndBank;
        public decimal ClosingCash => ClosingCashAndBank;
    }

    /// <summary>
    /// Line item representing a cash flow transaction item (Receipts/Payments for a specific account)
    /// </summary>
    public class CashFlowLineItemDto
    {
        public string ActivityType { get; set; } = "Operating";
        public int ActivityOrder { get; set; } = 1;
        public string GeneralLedgerCodeNo { get; set; } = string.Empty;
        public string GeneralLedgerName { get; set; } = string.Empty;
        public string? SubSubsidiaryLedgerName { get; set; }
        public decimal Receipts { get; set; }
        public decimal Payments { get; set; }
        public decimal NetAmount { get; set; }

        public string DisplayLabel
        {
            get
            {
                if (NetAmount >= 0)
                    return $"Receipts from {GeneralLedgerName}";
                return $"Payments for {GeneralLedgerName}";
            }
        }
    }

    public class CashFlowItemDto : CashFlowLineItemDto
    {
    }

    /// <summary>
    /// Activity group (Operating, Investing, Financing) containing line items and sub-total
    /// </summary>
    public class CashFlowActivityGroupDto
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<CashFlowLineItemDto> Items { get; set; } = new List<CashFlowLineItemDto>();
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal NetCashFlow { get; set; }
    }

    /// <summary>
    /// Summary cards and cash reconciliation values
    /// </summary>
    public class CashFlowSummaryVM
    {
        public decimal OperatingCashFlow { get; set; }
        public decimal InvestingCashFlow { get; set; }
        public decimal FinancingCashFlow { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal OpeningCashAndBank { get; set; }
        public decimal ClosingCashAndBank { get; set; }
        public decimal Difference { get; set; }
        public bool IsReconciled => Math.Abs(Difference) < 0.01m;
    }
}
