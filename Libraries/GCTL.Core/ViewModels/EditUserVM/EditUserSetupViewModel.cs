using GCTL.Core.ViewModels;

namespace GCTL.Core.ViewModels.EditUserVM
{
    public class EditUserSetupViewModel : BaseViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? SecureCode { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmployeeId { get; set; }
        public string? Dob { get; set; }
        public string? OffPhone { get; set; }
        public string? PerPhone { get; set; }
        public string? OffEmail { get; set; }
        public string? PerEmail { get; set; }
        public string? WorkStation { get; set; }
        public string? Regulation { get; set; }
    }

    public class EditUserGridViewModel : BaseViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string EntryDate { get; set; } = string.Empty;
    }
}

