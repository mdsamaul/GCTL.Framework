using GCTL.Core.DataTables;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Core.ViewModels.UserAccesses
{
    public class UserViewModel
    {
        [Searchable]
        public int Id { get; set; }

        [Searchable]
        public string? Username { get; set; }

        [Searchable]
        public string? FullName { get; set; }

        public string? Password { get; set; }

        [Searchable]
        public string? Status { get; set; }

        [Searchable]
        public string? AccessCode { get; set; }

        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        [Searchable]
        public string? UserType { get; set; }

        [Searchable]
        public string? Role { get; set; }

        [Searchable]
        public DateTime? EntryDate { get; set; }

        [Searchable]
        public string? IsMobileAppUser { get; set; }

        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? NationalId { get; set; }
        public string? Company { get; set; }
        public string? Branch { get; set; }
        public string? EmpType { get; set; }
        public string? EmpNature { get; set; }
        public string? OffPhone { get; set; }
        public string? OffEmail { get; set; }
        public string? JoiningDate { get; set; }
        public string? WorkStation { get; set; }
    }
}
