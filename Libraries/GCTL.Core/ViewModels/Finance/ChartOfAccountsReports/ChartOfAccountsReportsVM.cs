using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.ChartOfAccountsReports
{
    #region Filter ViewModel
    public class ChartOfAccountsReportFilterVM
    {
        public string? SearchTerm { get; set; }
        public bool ShowArchived { get; set; } = false;
        public string ViewMode { get; set; } = "tree";
        public bool Download { get; set; } = false;
        public string? VisibleTreeJson { get; set; }
    }
    #endregion

    #region Flat List DTO (5-Layer Table Row)
    public class ChartOfAccountsReportItemDto
    {
        // Layer 1 - Group Ledger
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;

        // Layer 2 - Control Ledger
        public string ControlCode { get; set; } = string.Empty;
        public string ControlName { get; set; } = string.Empty;

        // Layer 3 - Sub-Control Ledger
        public string SubControlCode { get; set; } = string.Empty;
        public string SubControlName { get; set; } = string.Empty;

        // Layer 4 - Sub-Subsidiary Ledger
        public string SubSubsidiaryCode { get; set; } = string.Empty;
        public string SubSubsidiaryName { get; set; } = string.Empty;

        // Layer 5 - General Ledger
        public string GeneralLedgerCode { get; set; } = string.Empty;
        public string GeneralLedgerName { get; set; } = string.Empty;
        public string? ShortName { get; set; }

        public string AccountType { get; set; } = "Asset";
        public bool IsActive { get; set; } = true;
        public string StatusText => IsActive ? "Active" : "Archived";

        public string FullHierarchyPath => $"{GroupCode}-{GroupName} › {ControlCode}-{ControlName} › {SubControlCode}-{SubControlName} › {SubSubsidiaryCode}-{SubSubsidiaryName}";
    }
    #endregion

    #region Tree Hierarchy Node DTO
    public class ChartOfAccountsReportTreeDto
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
        public string AccountType { get; set; } = "Asset";
        public bool IsActive { get; set; } = true;
        public int GLCount { get; set; }
        public bool IsOpen { get; set; } = true;
        public bool HasChildren { get; set; } = false;
        public List<ChartOfAccountsReportTreeDto> Children { get; set; } = new List<ChartOfAccountsReportTreeDto>();
    }
    #endregion

    #region Statistical Summary DTO
    public class ChartOfAccountsReportStatsDto
    {
        public int TotalLedgers { get; set; }
        public int TotalGroups { get; set; }
        public int TotalControls { get; set; }
        public int TotalSubControls { get; set; }
        public int TotalSubSubsidiaries { get; set; }
        public int TotalGeneralLedgers { get; set; }
        public int AssetsGLCount { get; set; }
        public int EquityGLCount { get; set; }
        public int IncomeGLCount { get; set; }
        public int ExpenseGLCount { get; set; }
    }
    #endregion

    #region Combined Response ViewModel
    public class ChartOfAccountsReportResponseVM
    {
        public string CompanyName { get; set; } = "Cantonment Public School & College, Rangpur.";
        public ChartOfAccountsReportStatsDto Stats { get; set; } = new ChartOfAccountsReportStatsDto();
        public List<ChartOfAccountsReportTreeDto> Tree { get; set; } = new List<ChartOfAccountsReportTreeDto>();
        public List<ChartOfAccountsReportTreeDto> VisibleTree { get; set; } = new List<ChartOfAccountsReportTreeDto>();
        public List<ChartOfAccountsReportItemDto> List { get; set; } = new List<ChartOfAccountsReportItemDto>();
    }
    #endregion
}
