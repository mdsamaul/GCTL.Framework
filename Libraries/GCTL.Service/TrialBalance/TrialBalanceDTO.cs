using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.Service.TrialBalance
{
    public class TrialBalanceFilter
    {
        [JsonPropertyName("companyCode")]
        public string? CompanyCode { get; set; } = string.Empty;

        [JsonPropertyName("branchCode")]
        public string? BranchCode { get; set; } = string.Empty;

        [JsonPropertyName("fromDate")]
        public string? FromDate { get; set; }

        [JsonPropertyName("toDate")]
        public string? ToDate { get; set; }

        // DataTables pagination parameters
        [JsonPropertyName("draw")]
        public int? Draw { get; set; }

        [JsonPropertyName("start")]
        public int? Start { get; set; }

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("searchTerm")]
        public string? SearchTerm { get; set; }

        [JsonPropertyName("sortColumn")]
        public string? SortColumn { get; set; }

        [JsonPropertyName("sortOrder")]
        public string? SortOrder { get; set; }
    }

    public class TrialBalanceItemDTO
    {
        private string? _code;
        [JsonPropertyName("code")]
        public string? Code { get => _code; set => _code = value; }

        [JsonPropertyName("glCode")]
        public string? GlCode { get => _code; set => _code = value; }

        [JsonPropertyName("generalLedgerCodeNo")]
        public string? GeneralLedgerCodeNo { get => _code; set => _code = value; }

        private string? _accountHead;
        [JsonPropertyName("accountHead")]
        public string? AccountHead { get => _accountHead; set => _accountHead = value; }

        [JsonPropertyName("generalLedgerName")]
        public string? GeneralLedgerName { get => _accountHead; set => _accountHead = value; }

        private string? _group;
        [JsonPropertyName("group")]
        public string? Group
        {
            get
            {
                if (!string.IsNullOrEmpty(_group)) return _group;
                if (!string.IsNullOrEmpty(_code))
                {
                    if (_code.StartsWith("1")) return "Assets";
                    if (_code.StartsWith("2")) return "Equities & Liabilities";
                    if (_code.StartsWith("3")) return "Expenses";
                    if (_code.StartsWith("4")) return "Income / Revenue";
                }
                return "General";
            }
            set => _group = value;
        }

        [JsonPropertyName("openingDebit")]
        public decimal OpeningDebit { get; set; }

        [JsonPropertyName("openingCredit")]
        public decimal OpeningCredit { get; set; }

        [JsonPropertyName("periodDebit")]
        public decimal PeriodDebit { get; set; }

        [JsonPropertyName("periodCredit")]
        public decimal PeriodCredit { get; set; }

        [JsonPropertyName("debit")]
        public decimal Debit { get; set; }

        [JsonPropertyName("credit")]
        public decimal Credit { get; set; }

        [JsonPropertyName("debitBalance")]
        public decimal DebitBalance { get; set; }

        [JsonPropertyName("creditBalance")]
        public decimal CreditBalance { get; set; }

        [JsonPropertyName("branchCode")]
        public string? BranchCode { get; set; }
    }

    public class TrialBalanceSummaryDTO
    {
        [JsonPropertyName("totalDebit")]
        public decimal TotalDebit { get; set; }

        [JsonPropertyName("totalCredit")]
        public decimal TotalCredit { get; set; }

        [JsonPropertyName("difference")]
        public decimal Difference { get; set; }

        [JsonPropertyName("accountsWithBalance")]
        public int AccountsWithBalance { get; set; }
    }

    public class TrialBalanceFilterOptionsDTO
    {
        public List<SelectListItem> Companies { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
    }
}
