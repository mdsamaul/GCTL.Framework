using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCTL.Core.ViewModels.ManageNoticeEntries
{
    public class ManageNoticeSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public List<int>? Tcs { get; set; }
        public string NoticeId { get; set; }
        public string NoticeTitle { get; set; }
        public string NoticeDesc { get; set; }
        public string Status { get; set; }
        public DateTime? EntryDate { get; set; }

        public bool HasFile { get; set; }
        public long? FileSize { get; set; }
        public string FileType { get; set; }
        public decimal? DocumentTc { get; set; }

        public string? FilePath { get; set; }
        [NotMapped]
        public IFormFile? formFile { get; set; }
        public int? PriorityLevel { get; set; }
    }

    public class LookupItemDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
    }
    public class EmployeeFilterResultDto
    {
        public Dictionary<string, List<LookupItemDto>> LookupData { get; set; } = new();
        public List<EmployeeListItemViewModel> Employees { get; set; } = new();
    }

    public class EmployeeFilterViewModel
    {
        public List<string>? CompanyCodes { get; set; }
        public List<string>? BranchCodes { get; set; }
        public List<string>? DepartmentCodes { get; set; }
        public List<string>? DesignationCodes { get; set; }
        public List<string>? EmployeeCodes { get; set; }
    }

    public class EmployeeListItemViewModel
    {
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? JoiningDate { get; set; }
        public string? DesignationName { get; set; }
        public string? BranchName { get; set; }
        public string? DepartmentName { get; set; }
        public string? CompanyName { get; set; }
        public string? EmployeeTypeName { get; set; }
        public string? EmployeeStatus { get; set; }
        public string? EmploymentNature { get; set; }
    }

    public class EmailSentParamDTO
    {
        public List<string> EmployeeIds { get; set; }
        public List<int> Tcs { get; set; }
    }
}
