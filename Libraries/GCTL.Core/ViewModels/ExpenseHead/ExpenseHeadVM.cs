using System;
using System.Text.Json.Serialization;
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.ExpenseHead
{
    public class ExpenseHeadVM : BaseViewModel
    {
        [JsonIgnore]
        public new decimal? Id { get; set; }

        [JsonPropertyName("tc")]
        public int? TC { get; set; }

        [Searchable]
        [JsonPropertyName("expenseHeadId")]
        public string ExpenseHeadID { get; set; } = string.Empty;

        [Searchable]
        [JsonPropertyName("expenseHead")]
        public string ExpenseHead { get; set; } = string.Empty;

        [Searchable]
        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [Searchable]
        [JsonPropertyName("serialNo")]
        public new string SerialNo { get; set; } = string.Empty;

        [JsonPropertyName("expenseTypeId")]
        public string ExpenseTypeID { get; set; } = string.Empty;

        [Searchable]
        [JsonPropertyName("isReceiptable")]
        public string? IsReceiptable { get; set; }

        [JsonPropertyName("rec")]
        public int Rec
        {
            get => (IsReceiptable == "Y" || IsReceiptable == "Yes" || IsReceiptable == "YES" || IsReceiptable == "1") ? 1 : 0;
            set => IsReceiptable = value == 1 ? "Y" : "N";
        }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("serviceTypeId")]
        public string ServiceTypeID { get; set; } = string.Empty;

        [JsonPropertyName("entryType")]
        public string? EntryType { get; set; }

        [JsonPropertyName("status")]
        public string Status
        {
            get => string.IsNullOrWhiteSpace(EntryType) || EntryType.Equals("Active", StringComparison.OrdinalIgnoreCase) ? "Active" : EntryType;
            set => EntryType = value;
        }

        [Searchable]
        [JsonPropertyName("expenseType")]
        public string? ExpenseType { get; set; }

        [Searchable]
        [JsonPropertyName("serviceTypeName")]
        public string? ServiceTypeName { get; set; }
    }
}
