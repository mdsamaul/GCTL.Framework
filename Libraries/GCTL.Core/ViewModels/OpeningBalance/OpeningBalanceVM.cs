

namespace GCTL.Core.ViewModels.OpeningBalance
{
    public class OpeningBalanceVM
    {
        public decimal? autoId { get; set; }

        public string? ComOpeningBalanceCode { get; set; }

        public string? Main_CompanyCode { get; set; }

        public string BranchCode { get; set; }

        public string SubSusidiaryLedgerCodeNo { get; set; }

        public decimal? OpeningBalance { get; set; }

        public string TrType { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }

        public string CompanyCode { get; set; }

        public string? UserInfoEmployeeID { get; set; }

        //Joining filed Core_Company, Core_Branch, General ledger
        public string? CompanyName { get; set; }
        public string? BranchName { get; set; }
        public string? AccountHead { get; set; }
    }
}
