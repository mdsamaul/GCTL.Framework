using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.CustomerWiseApprovalRate
{
    public class CustomerWiseApprovalRateViewModel : BaseViewModel
    {
        public decimal Tc { get; set; }
        public string? CostingId { get; set; } // CWAR ID
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal? MinimumAgencyCommission { get; set; }
        public decimal? OnInvoiceValue { get; set; }
        public string? ExpenseTypeId { get; set; }

        public List<CustomerWiseApprovalRateDetailViewModel> Details { get; set; } = new();
    }

    public class CustomerWiseApprovalRateDetailViewModel
    {
        public decimal Tc { get; set; }
        public string? CostingDetailsId { get; set; }
        public string? CostingId { get; set; }
        public string? CustomerId { get; set; }
        public string? Chcid { get; set; }
        public string? Chchead { get; set; }
        public string? ExpenseTypeId { get; set; }
        public string? ExpenseTypeName { get; set; }
        public decimal? BillAmount { get; set; }
    }

    public class CustomerWiseApprovalRateMasterGridModel
    {
        public string Id { get; set; } = string.Empty; // CostingId
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string MinimumAgencyCommission { get; set; } = string.Empty;
        public string OnInvoiceValue { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
    }
}
