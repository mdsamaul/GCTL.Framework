

namespace GCTL.Core.ViewModels.VoucherType
{
    public class VoucherTypeVM
    {
        public decimal? autoId { get; set; }

        public string? VoucherType_Code { get; set; }

        public string Voucher_TypeName { get; set; }

        public string Description { get; set; }

        public string numberingMethod { get; set; }

        public decimal? startingNumber { get; set; }

        public decimal resetDuration { get; set; }

        public string? resetDurationType_Code { get; set; }

        public string prefix { get; set; }

        public string? suffix { get; set; }

        public decimal padding { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }

        //Joinning Field from Acc_Duration_Type
        public string? DurationTypeName { get; set; }

    }
}
