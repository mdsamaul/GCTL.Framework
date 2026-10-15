#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCTL.Data.Models
{
    [Table("Acc_VoucherInvoicePaymentReceived")]
    public partial class Acc_VoucherInvoicePaymentReceived
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("autoId")]
        public decimal AutoId { get; set; }

        public string? VoucherNo { get; set; }

        public string? InvoiceNo { get; set; }

        public decimal? Amount { get; set; }

        public string? IsActive { get; set; }

        [Column("LUser")]
        public string? LUser { get; set; }

        [Column("LDate")]
        public DateTime? LDate { get; set; }

        [Column("LIP")]
        public string? LIP { get; set; }

        [Column("LMAC")]
        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }
    }
}
