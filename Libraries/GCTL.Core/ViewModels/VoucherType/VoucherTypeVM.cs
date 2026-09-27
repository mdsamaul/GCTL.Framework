

using GCTL.Core.DataTables;
using System;

namespace GCTL.Core.ViewModels.VoucherType
{
    public class VoucherTypeVM
    {
        [Searchable]
        public decimal? autoId { get; set; }

        [Searchable]
        public string? VoucherType_Code { get; set; }

        [Searchable]
        public string Voucher_TypeName { get; set; }

        [Searchable]
        public string Description { get; set; }

        [Searchable]
        public string numberingMethod { get; set; }

        public decimal? startingNumber { get; set; }
        public decimal resetDuration { get; set; }
        public string? resetDurationType_Code { get; set; }

        [Searchable]
        public string prefix { get; set; }

        [Searchable]
        public string? suffix { get; set; }

        public decimal padding { get; set; }
        public string? LUser { get; set; }
        public DateTime? LDate { get; set; }
        public string? LIP { get; set; }
        public string? LMAC { get; set; }
        public DateTime? ModifyDate { get; set; }

        // Joining Field from Acc_Duration_Type
        [Searchable]
        public string? DurationTypeName { get; set; }
    }
}
