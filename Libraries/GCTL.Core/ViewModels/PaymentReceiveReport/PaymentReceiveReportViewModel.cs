using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.Core.ViewModels.PaymentReceiveReport
{
    public class PaymentReceiveReportFilter
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CustomerTypeId { get; set; }
        public string District { get; set; }
        public string Thana { get; set; }
        public string EmployeeId { get; set; }
        public string PaymentTypeId { get; set; }
        public string PaymentModeId { get; set; }
        public string PaymentStatus { get; set; }
    }

    public class PaymentReceiveReportItemDTO
    {
        public long SLNo { get; set; }
        public string PaymentReceiveNo { get; set; }
        public string PaymentReceiveDate { get; set; }
        public string CustomerID { get; set; }
        public string Customer { get; set; }
        public string CustomerType { get; set; }
        public string BillingAddress { get; set; }
        public string Employee { get; set; }
        public string InvoiceNoList { get; set; }
        public string PaymentType { get; set; }
        public string PaymentMode { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal BankFeeOnlineCharge { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeDate { get; set; }
        public string ChequeBank { get; set; }
        public string DepositBankAccount { get; set; }
        public string MaturityDate { get; set; }
        public string PaymentStatus { get; set; }
    }

    public class PaymentReceiveReportSummaryDTO
    {
        public decimal TotalReceivedAmount { get; set; }
        public decimal TotalBankFee { get; set; }
        public decimal RegularAmount { get; set; }
        public decimal AdvanceAmount { get; set; }
        public int TotalTransactions { get; set; }
    }

    public class PaymentReceiveReportViewModel
    {
        public PaymentReceiveReportFilter Filter { get; set; } = new PaymentReceiveReportFilter();
        public PaymentReceiveReportSummaryDTO Summary { get; set; } = new PaymentReceiveReportSummaryDTO();
        public List<PaymentReceiveReportItemDTO> Items { get; set; } = new List<PaymentReceiveReportItemDTO>();

        public List<SelectListItem> CustomerTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Districts { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Thanas { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PaymentTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PaymentModes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PaymentStatuses { get; set; } = new List<SelectListItem>();
    }
}

