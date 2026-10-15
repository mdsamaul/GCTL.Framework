using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.Finance.ChartOfAccounts
{
    #region Delete & Common Request DTOs
    public class DeleteLedgerRequestVM
    {
        public List<string> Codes { get; set; } = new List<string>();
    }

    public class LedgerDropdownItemVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayText => $"{Code} - {Name}";
        public string? ParentCode { get; set; }
    }
    #endregion

    #region Group Ledger (Level 0 - GRL)
    public class GroupLedgerAddVM
    {
        [Display(Name = "Group Ledger Code")]
        public string? GroupLedgerCodeNo { get; set; }

        [Required(ErrorMessage = "Group Ledger Name is required!")]
        [StringLength(100, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Group Ledger Name")]
        public string GroupLedgerName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class GetAllGroupLedgerVM
    {
        public decimal AutoId { get; set; }
        public string GroupLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerName { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Active" : "Archived";
        public int ChildCount { get; set; }
    }
    #endregion

    #region Control Ledger (Level 1 - CL)
    public class ControlLedgerAddVM
    {
        [Display(Name = "Control Ledger Code")]
        public string? ControlLedgerCodeNo { get; set; }

        [Required(ErrorMessage = "Group Ledger is required!")]
        [Display(Name = "Group Ledger")]
        public string GroupLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Control Ledger Name is required!")]
        [StringLength(100, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Control Ledger Name")]
        public string ControlLedgerName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class GetAllControlLedgerVM
    {
        public decimal AutoId { get; set; }
        public string ControlLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerName { get; set; } = string.Empty;
        public string ControlLedgerName { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string HierarchyPath => $"{GroupLedgerCodeNo} - {GroupLedgerName}";
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Active" : "Archived";
        public int ChildCount { get; set; }
    }
    #endregion

    #region Sub-Control Ledger (Level 2 - SCL)
    public class SubControlLedgerAddVM
    {
        [Display(Name = "Sub-Control Ledger Code")]
        public string? SubControlLedgerCodeNo { get; set; }

        [Required(ErrorMessage = "Group Ledger is required!")]
        [Display(Name = "Group Ledger")]
        public string GroupLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Control Ledger is required!")]
        [Display(Name = "Control Ledger")]
        public string ControlLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sub-Control Ledger Name is required!")]
        [StringLength(100, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Sub-Control Ledger Name")]
        public string SubControlLedgerName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class GetAllSubControlLedgerVM
    {
        public decimal AutoId { get; set; }
        public string SubControlLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerName { get; set; } = string.Empty;
        public string ControlLedgerCodeNo { get; set; } = string.Empty;
        public string ControlLedgerName { get; set; } = string.Empty;
        public string SubControlLedgerName { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string HierarchyPath => $"{GroupLedgerCodeNo} - {GroupLedgerName} › {ControlLedgerCodeNo} - {ControlLedgerName}";
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Active" : "Archived";
        public int ChildCount { get; set; }
    }
    #endregion

    #region Sub-Subsidiary Ledger (Level 3 - SSL)
    public class SubSubsidiaryLedgerAddVM
    {
        [Display(Name = "Sub-Subsidiary Ledger Code")]
        public string? SubSubsidiaryLedgerCodeNo { get; set; }

        [Required(ErrorMessage = "Group Ledger is required!")]
        [Display(Name = "Group Ledger")]
        public string GroupLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Control Ledger is required!")]
        [Display(Name = "Control Ledger")]
        public string ControlLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sub-Control Ledger is required!")]
        [Display(Name = "Sub-Control Ledger")]
        public string SubControlLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sub-Subsidiary Ledger Name is required!")]
        [StringLength(100, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Sub-Subsidiary Ledger Name")]
        public string SubSubsidiaryLedgerName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }

        [Display(Name = "Opening Balance")]
        public decimal? OpeningBalance { get; set; }

        [Display(Name = "Transaction Type")]
        [StringLength(2)]
        public string? TrType { get; set; } // Dr / Cr

        [Display(Name = "Opening Date")]
        public DateTime? OpeningDate { get; set; }

        [Display(Name = "Cash Flow Type")]
        [StringLength(50)]
        public string? CashFlowTypeID { get; set; }

        [Display(Name = "Cost Center")]
        [StringLength(50)]
        public string? CostCenterCodeNo { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class GetAllSubSubsidiaryLedgerVM
    {
        public decimal AutoId { get; set; }
        public string SubSubsidiaryLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerName { get; set; } = string.Empty;
        public string ControlLedgerCodeNo { get; set; } = string.Empty;
        public string ControlLedgerName { get; set; } = string.Empty;
        public string SubControlLedgerCodeNo { get; set; } = string.Empty;
        public string SubControlLedgerName { get; set; } = string.Empty;
        public string SubSubsidiaryLedgerName { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public decimal? OpeningBalance { get; set; }
        public string? TrType { get; set; }
        public DateTime? OpeningDate { get; set; }
        public string HierarchyPath => $"{GroupLedgerCodeNo} - {GroupLedgerName} › {ControlLedgerCodeNo} - {ControlLedgerName} › {SubControlLedgerCodeNo} - {SubControlLedgerName}";
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Active" : "Archived";
        public int ChildCount { get; set; }
    }
    #endregion

    #region General Ledger (Level 4 - GL)
    public class GeneralLedgerAddVM
    {
        [Display(Name = "General Ledger Code")]
        public string? GeneralLedgerCodeNo { get; set; }

        [Required(ErrorMessage = "Group Ledger is required!")]
        [Display(Name = "Group Ledger")]
        public string GroupLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Control Ledger is required!")]
        [Display(Name = "Control Ledger")]
        public string ControlLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sub-Control Ledger is required!")]
        [Display(Name = "Sub-Control Ledger")]
        public string SubControlLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sub-Subsidiary Ledger is required!")]
        [Display(Name = "Sub-Subsidiary Ledger")]
        public string SubSubsidiaryLedgerCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "General Ledger Name is required!")]
        [StringLength(100, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "General Ledger Name")]
        public string GeneralLedgerName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "{0} must be at most {1} characters long.")]
        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }

        public string? IsSameNameGL { get; set; }
        public string? IsSameNameSL { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class GetAllGeneralLedgerVM
    {
        public decimal AutoId { get; set; }
        public string GeneralLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerCodeNo { get; set; } = string.Empty;
        public string GroupLedgerName { get; set; } = string.Empty;
        public string ControlLedgerCodeNo { get; set; } = string.Empty;
        public string ControlLedgerName { get; set; } = string.Empty;
        public string SubControlLedgerCodeNo { get; set; } = string.Empty;
        public string SubControlLedgerName { get; set; } = string.Empty;
        public string SubSubsidiaryLedgerCodeNo { get; set; } = string.Empty;
        public string SubSubsidiaryLedgerName { get; set; } = string.Empty;
        public string GeneralLedgerName { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string HierarchyPath => $"{GroupLedgerCodeNo} - {GroupLedgerName} › {ControlLedgerCodeNo} - {ControlLedgerName} › {SubControlLedgerCodeNo} - {SubControlLedgerName} › {SubSubsidiaryLedgerCodeNo} - {SubSubsidiaryLedgerName}";
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Active" : "Archived";
    }
    #endregion

    #region Tree & Statistics DTOs
    public class ChartOfAccountsTreeItemVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public int Level { get; set; } // 0: GRL, 1: CL, 2: SCL, 3: SSL, 4: GL
        public string LevelName => Level switch
        {
            0 => "Group Ledger",
            1 => "Control Ledger",
            2 => "Sub-Control Ledger",
            3 => "Sub-Subsidiary Ledger",
            4 => "General Ledger",
            _ => "Ledger"
        };
        public string ParentCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int GLCount { get; set; }
        public List<ChartOfAccountsTreeItemVM> Children { get; set; } = new List<ChartOfAccountsTreeItemVM>();
    }

    public class GroupStatItemVM
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int GLCount { get; set; }
    }

    public class ChartOfAccountsStatsVM
    {
        public int TotalLedgers { get; set; }
        public int TotalGroups { get; set; }
        public int TotalControls { get; set; }
        public int TotalSubControls { get; set; }
        public int TotalSubSubsidiaries { get; set; }
        public int TotalGeneralLedgers { get; set; }
        public List<GroupStatItemVM> GroupStats { get; set; } = new List<GroupStatItemVM>();
    }
    #endregion
}
