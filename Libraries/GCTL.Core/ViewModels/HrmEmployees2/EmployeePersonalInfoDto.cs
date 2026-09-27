using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.HrmEmployees2
{
    public class DataTableRequestDto
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch? Search { get; set; }
        public List<DataTableOrder>? Order { get; set; }
        public List<DataTableColumn>? Columns { get; set; }
        public int PageNumber => (Length > 0) ? (Start / Length) + 1 : 1;
        public int PageSize => (Length > 0) ? Length : 10;
        public string SearchValue => Search?.Value?.Trim() ?? string.Empty;
        public string SortColumnName
        {
            get
            {
                if (Order == null || Order.Count == 0) return "AutoId";
                var colIdx = Order[0].Column;
                if (Columns != null && colIdx < Columns.Count)
                    return Columns[colIdx].Name ?? "AutoId";
                return "AutoId";
            }
        }
        public string SortDirection => (Order != null && Order.Count > 0 && Order[0].Dir == "asc") ? "ASC" : "DESC";
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "desc";
    }

    public class DataTableColumn
    {
        public string? Data { get; set; }
        public string? Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch? Search { get; set; }
    }

    public class DataTableResponseDto<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Data { get; set; } = new();
        public string? Error { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
    }

    public class SpEmployeeRow
    {
        public long AutoId { get; set; }
        public string? EmployeeID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FirstNameBangla { get; set; }
        public string? LastNameBangla { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public DateTime? DateOfBirthCertificate { get; set; }
        public DateTime? DateOfBirthOrginal { get; set; }
        public string? BirthCertificateNo { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? NationalIDNo { get; set; }
        public string? PersonalEmail { get; set; }
        public string? Telephone { get; set; }
        public string? SexCode { get; set; }
        public string? NationalityCode { get; set; }
        public int HasPhoto { get; set; }
        public string? PhotoImgType { get; set; }
        public byte[]? PhotoBytes { get; set; }
        public int HasSignature { get; set; }
        public string? SignImgType { get; set; }
        public byte[]? SignBytes { get; set; }
        public int TotalRecords { get; set; }
    }
}
