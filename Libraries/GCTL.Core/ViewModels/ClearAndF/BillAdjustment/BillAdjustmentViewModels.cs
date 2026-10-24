using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GCTL.Core.ViewModels.ClearAndF.BillAdjustment
{
    // =========================================================================
    // 0. Robust Json Converters for Loose/Polymorphic Client Payloads
    // =========================================================================
    public class StringOrNumberConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long l)) return l.ToString();
                    if (reader.TryGetDecimal(out decimal d)) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (reader.TryGetDouble(out double dbl)) return dbl.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return null;
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.True:
                    return "true";
                case JsonTokenType.False:
                    return "false";
                default:
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value);
        }
    }

    public class FlexibleDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    return null;

                if (DateTime.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                    return dt;

                string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd", "dd-MM-yyyy", "o", "s" };
                if (DateTime.TryParseExact(str, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtExact))
                    return dtExact;

                return null;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
            else
                writer.WriteNullValue();
        }
    }

    // =========================================================================
    // 1. Header DTO (FinalBillInfo_ForBillAdjust + CF_BillAdjustmentEntry)
    // =========================================================================
    public class BillAdjustmentMasterDto
    {
        public decimal? Tc { get; set; }
        public string AdjustmentNo { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = "Credit Note"; // "Credit Note" or "Debit Note"
        public DateTime? AdjustmentDate { get; set; } = DateTime.Today;

        public string BillNo { get; set; } = string.Empty;
        public string BillNoPrefix { get; set; } = "SLSL";
        public DateTime? BillDate { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public DateTime? JobDate { get; set; }
        public string Branch { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerDeliveryAddress { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;

        public DateTime? DocRecvDate { get; set; }
        public string LcNo { get; set; } = string.Empty;
        public decimal LcValue { get; set; }
        public string LcCurrency { get; set; } = "USD";
        public decimal BdtRate { get; set; }
        public int NumberOfTransport { get; set; }
        public int NumberOfDemerageDay { get; set; }

        public string IpNo { get; set; } = string.Empty;
        public DateTime? IpDate { get; set; }
        public string UndertakingNo { get; set; } = string.Empty;
        public DateTime? ArrivalDate { get; set; }
        public string InvNo { get; set; } = string.Empty;
        public DateTime? InvDate { get; set; }
        public string BlNo { get; set; } = string.Empty;
        public DateTime? BlDate { get; set; }
        public string BeNo { get; set; } = string.Empty;
        public DateTime? BeDate { get; set; }
        public string EfrNo { get; set; } = string.Empty;
        public DateTime? EfrDate { get; set; }
        public string RotNo { get; set; } = string.Empty;
        public string PoNo { get; set; } = string.Empty;
        public string ContainerNo { get; set; } = string.Empty;

        public string MaterialDescription { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public string QUnitType { get; set; } = string.Empty;
        public decimal DocWeight { get; set; }
        public string WUnitType { get; set; } = string.Empty;

        public string Fwdr { get; set; } = string.Empty;
        public string VesselNo { get; set; } = string.Empty;
        public DateTime? EtaDate { get; set; }
        public DateTime? AtaDate { get; set; }
        public DateTime? EtbDate { get; set; }
        public DateTime? AtbDate { get; set; }
        public DateTime? UnStuffingDate { get; set; }
        public DateTime? DateOfClearance { get; set; }
        public string ConsoleNo { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;

        public DateTime? DeliveryDate { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal AdvanceAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalCfBillAmount { get; set; }
        public decimal TotalTransportBillAmount { get; set; }

        public decimal Itc { get; set; }
        public decimal Cv { get; set; }
        public decimal VatCurrAccTransport { get; set; }
        public decimal VatCurrAccCf { get; set; }
        public decimal VatCurrAccTransportAdjust { get; set; }
        public decimal VatCurrAccCfAdjust { get; set; }

        public decimal MinimumAgencyCommission { get; set; }
        public decimal OnInvoiceValue { get; set; }
        public decimal AgencyCommissionAmount { get; set; }

        public decimal AssessableRate { get; set; }
        public decimal AssessableValue { get; set; }
        public decimal InvoiceValue { get; set; }
        public string InvoiceCurrency { get; set; } = string.Empty;

        public string Terms { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string NotesReasonForCredit { get; set; } = string.Empty;

        public DateTime? CreationDate { get; set; } = DateTime.Today;
        public DateTime? LastUpdateDate { get; set; }
    }

    // =========================================================================
    // 2. Grid 1: Custom Handling Charges (CF_BillAdjustDetails / CF_BillEntryDetails_CHCHead_ForBillAdjust)
    // =========================================================================
    public class BillAdjustChcDetailDto
    {
        public decimal? Tc { get; set; }

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string AdjustmentNo { get; set; } = string.Empty;

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string ChcId { get; set; } = string.Empty;

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string BillHead { get; set; } = string.Empty;

        public string ChcHeadDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsReceiptable { get; set; }

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string EmployeeId { get; set; } = string.Empty;
    }

    // =========================================================================
    // 3. Grid 2: Expenses in Detail (CF_BillEntryDetails_ForBillAdjust)
    // =========================================================================
    public class BillAdjustExpenseDetailDto
    {
        public decimal? Tc { get; set; }

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string BillDetailsId { get; set; } = string.Empty;

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string BillNo { get; set; } = string.Empty;

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string ExpenseHeadId { get; set; } = string.Empty;

        public string ExpenseHead { get; set; } = string.Empty;
        public decimal EstimatedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal DifferentAmount { get; set; }
        public bool IsReceiptable { get; set; }
    }

    // =========================================================================
    // 4. Bottom Grid: Approved Bill List (Proc_LoadApprovedBillListForAdjust)
    // =========================================================================
    public class ApprovedBillListDto
    {
        public string BillNo { get; set; } = string.Empty;
        public DateTime? BillDate { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string BeNo { get; set; } = string.Empty;
        public DateTime? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AdvanceAmount { get; set; }
        public decimal NetPayable { get; set; }
        public string VoucherNo { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = string.Empty;
        public string AdjustmentNo { get; set; } = string.Empty;
        public DateTime? AdjustmentDate { get; set; }
    }

    // =========================================================================
    // 4b. CHC Head Lookup Option DTO (CustomerHandilingCharge)
    // =========================================================================
    public class ChcHeadOptionDto
    {
        public string ChcId { get; set; } = string.Empty;
        public string ChcHead { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsReceiptable { get; set; } = true;
    }

    // =========================================================================
    // 5. Composite Screen Data Response
    // =========================================================================
    public class BillAdjustmentScreenDataResponse
    {
        public BillAdjustmentMasterDto Header { get; set; } = new();
        public List<BillAdjustChcDetailDto> ChcCharges { get; set; } = new();
        public List<BillAdjustExpenseDetailDto> ExpenseDetails { get; set; } = new();
    }

    // =========================================================================
    // 6. Save Request DTO
    // =========================================================================
    public class BillAdjustmentSaveRequestDto
    {
        public decimal? Tc { get; set; }

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string AdjustmentNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adjustment Type is required.")]
        public string AdjustmentType { get; set; } = "Credit Note";

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? AdjustmentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Bill No is required.")]
        [JsonConverter(typeof(StringOrNumberConverter))]
        public string BillNo { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;
        public string NotesReasonForCredit { get; set; } = string.Empty;

        public List<BillAdjustChcDetailDto> ChcDetails { get; set; } = new();
        public List<BillAdjustExpenseDetailDto> ExpenseDetails { get; set; } = new();

        // Audit context
        [JsonConverter(typeof(StringOrNumberConverter))]
        public string CompanyCode { get; set; } = string.Empty;

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string BranchCode { get; set; } = string.Empty;

        [JsonConverter(typeof(StringOrNumberConverter))]
        public string EmployeeId { get; set; } = string.Empty;

        public string Luser { get; set; } = string.Empty;
        public string Lip { get; set; } = string.Empty;
        public string Lmac { get; set; } = string.Empty;
    }
}
