using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels.Finance.ChartOfAccountsReports;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.ChartOfAccountsReports
{
    public class ChartOfAccountsReportsService : IChartOfAccountsReportsService
    {
        private readonly IDbConnection _dbConnection;
        private readonly GCTL_ERP_DBContext _context;

        public ChartOfAccountsReportsService(
            IDbConnection dbConnection,
            GCTL_ERP_DBContext context)
        {
            _dbConnection = dbConnection;
            _context = context;
        }

        #region Get Report Data (Stored Procedure + In-Memory Tree Hierarchy)
        public async Task<ChartOfAccountsReportResponseVM> GetReportDataAsync(ChartOfAccountsReportFilterVM filter)
        {
            filter ??= new ChartOfAccountsReportFilterVM();
            var response = new ChartOfAccountsReportResponseVM();

            // 1. Fetch Company Name
            try
            {
                var comp = await _context.CoreCompanies.FirstOrDefaultAsync();
                if (comp != null && !string.IsNullOrWhiteSpace(comp.CompanyName))
                {
                    response.CompanyName = comp.CompanyName;
                }
            }
            catch
            {
                response.CompanyName = "Cantonment Public School & College, Rangpur.";
            }

            // 2. Query Data using Stored Procedure (with graceful auto-creation & EF fallback)
            List<ChartOfAccountsReportItemDto> flatItems = new();
            ChartOfAccountsReportStatsDto stats = new();

            try
            {
                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SearchTerm", string.IsNullOrWhiteSpace(filter.SearchTerm) ? null : filter.SearchTerm.Trim());
                parameters.Add("@ShowArchived", filter.ShowArchived ? 1 : 0);

                try
                {
                    using var multi = await _dbConnection.QueryMultipleAsync(
                        "dbo.sp_GetChartOfAccountsReport",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var rows = (await multi.ReadAsync<dynamic>()).ToList();
                    var statsRow = await multi.ReadFirstOrDefaultAsync<dynamic>();

                    if (statsRow != null)
                    {
                        stats.TotalGeneralLedgers = (int)(statsRow.TotalGeneralLedgers ?? 0);
                        stats.TotalGroups = (int)(statsRow.TotalGroups ?? 0);
                        stats.TotalControls = (int)(statsRow.TotalControls ?? 0);
                        stats.TotalSubControls = (int)(statsRow.TotalSubControls ?? 0);
                        stats.TotalSubSubsidiaries = (int)(statsRow.TotalSubSubsidiaries ?? 0);
                        stats.AssetsGLCount = (int)(statsRow.AssetsGLCount ?? 0);
                        stats.EquityGLCount = (int)(statsRow.EquityGLCount ?? 0);
                        stats.IncomeGLCount = (int)(statsRow.IncomeGLCount ?? 0);
                        stats.ExpenseGLCount = (int)(statsRow.ExpenseGLCount ?? 0);
                    }

                    foreach (var r in rows)
                    {
                        flatItems.Add(new ChartOfAccountsReportItemDto
                        {
                            GroupCode = (string)r.GroupLedgerCodeNo ?? string.Empty,
                            GroupName = (string)r.GroupLedgerName ?? string.Empty,
                            ControlCode = (string)r.ControlLedgerCodeNo ?? string.Empty,
                            ControlName = (string)r.ControlLedgerName ?? string.Empty,
                            SubControlCode = (string)r.SubControlLedgerCodeNo ?? string.Empty,
                            SubControlName = (string)r.SubControlLedgerName ?? string.Empty,
                            SubSubsidiaryCode = (string)r.SubSubsidiaryLedgerCodeNo ?? string.Empty,
                            SubSubsidiaryName = (string)r.SubSubsidiaryLedgerName ?? string.Empty,
                            GeneralLedgerCode = (string)r.GeneralLedgerCodeNo ?? string.Empty,
                            GeneralLedgerName = (string)r.GeneralLedgerName ?? string.Empty,
                            ShortName = (string)r.GeneralLedgerShortName,
                            AccountType = (string)r.AccountType ?? "Asset",
                            IsActive = (r.IsActive == "1" || r.IsActive == true || r.IsActive == null)
                        });
                    }
                }
                catch (Exception)
                {
                    // Fallback to EF Core if SP execution encounters an issue
                    flatItems = await FetchViaEfCoreAsync(filter);
                    stats = await ComputeStatsViaEfCoreAsync(filter.ShowArchived);
                }
            }
            catch (Exception)
            {
                flatItems = await FetchViaEfCoreAsync(filter);
                stats = await ComputeStatsViaEfCoreAsync(filter.ShowArchived);
            }

            response.List = flatItems;
            response.Stats = stats;

            // 3. Build Tree Nodes from 5 layers
            response.Tree = BuildTreeHierarchy(flatItems);

            return response;
        }
        #endregion

        #region Tree Hierarchy Construction
        private List<ChartOfAccountsReportTreeDto> BuildTreeHierarchy(List<ChartOfAccountsReportItemDto> items)
        {
            var tree = new List<ChartOfAccountsReportTreeDto>();

            // Group by Group Ledger (Level 0)
            var groups = items
                .Where(x => !string.IsNullOrEmpty(x.GroupCode))
                .GroupBy(x => new { x.GroupCode, x.GroupName, x.AccountType })
                .OrderBy(g => g.Key.GroupCode);

            foreach (var gGroup in groups)
            {
                var gNode = new ChartOfAccountsReportTreeDto
                {
                    Code = gGroup.Key.GroupCode,
                    Name = gGroup.Key.GroupName,
                    Level = 0,
                    ParentCode = string.Empty,
                    AccountType = gGroup.Key.AccountType,
                    IsActive = true
                };

                // Group by Control Ledger (Level 1)
                var controls = gGroup
                    .Where(x => !string.IsNullOrEmpty(x.ControlCode))
                    .GroupBy(x => new { x.ControlCode, x.ControlName })
                    .OrderBy(c => c.Key.ControlCode);

                foreach (var cGroup in controls)
                {
                    var cNode = new ChartOfAccountsReportTreeDto
                    {
                        Code = cGroup.Key.ControlCode,
                        Name = cGroup.Key.ControlName,
                        Level = 1,
                        ParentCode = gNode.Code,
                        AccountType = gNode.AccountType,
                        IsActive = true
                    };

                    // Group by Sub-Control Ledger (Level 2)
                    var subControls = cGroup
                        .Where(x => !string.IsNullOrEmpty(x.SubControlCode))
                        .GroupBy(x => new { x.SubControlCode, x.SubControlName })
                        .OrderBy(sc => sc.Key.SubControlCode);

                    foreach (var scGroup in subControls)
                    {
                        var scNode = new ChartOfAccountsReportTreeDto
                        {
                            Code = scGroup.Key.SubControlCode,
                            Name = scGroup.Key.SubControlName,
                            Level = 2,
                            ParentCode = cNode.Code,
                            AccountType = gNode.AccountType,
                            IsActive = true
                        };

                        // Group by Sub-Subsidiary Ledger (Level 3)
                        var subSubs = scGroup
                            .Where(x => !string.IsNullOrEmpty(x.SubSubsidiaryCode))
                            .GroupBy(x => new { x.SubSubsidiaryCode, x.SubSubsidiaryName })
                            .OrderBy(ss => ss.Key.SubSubsidiaryCode);

                        foreach (var ssGroup in subSubs)
                        {
                            var ssNode = new ChartOfAccountsReportTreeDto
                            {
                                Code = ssGroup.Key.SubSubsidiaryCode,
                                Name = ssGroup.Key.SubSubsidiaryName,
                                Level = 3,
                                ParentCode = scNode.Code,
                                AccountType = gNode.AccountType,
                                IsActive = true
                            };

                            // Leaf nodes: General Ledgers (Level 4)
                            var generalLedgers = ssGroup
                                .Where(x => !string.IsNullOrEmpty(x.GeneralLedgerCode))
                                .OrderBy(gl => gl.GeneralLedgerCode);

                            foreach (var gl in generalLedgers)
                            {
                                ssNode.Children.Add(new ChartOfAccountsReportTreeDto
                                {
                                    Code = gl.GeneralLedgerCode,
                                    Name = gl.GeneralLedgerName,
                                    ShortName = gl.ShortName,
                                    Level = 4,
                                    ParentCode = ssNode.Code,
                                    AccountType = gl.AccountType,
                                    IsActive = gl.IsActive,
                                    GLCount = 1
                                });
                            }

                            ssNode.GLCount = ssNode.Children.Count;
                            scNode.Children.Add(ssNode);
                        }

                        scNode.GLCount = scNode.Children.Sum(x => x.GLCount);
                        cNode.Children.Add(scNode);
                    }

                    cNode.GLCount = cNode.Children.Sum(x => x.GLCount);
                    gNode.Children.Add(cNode);
                }

                gNode.GLCount = gNode.Children.Sum(x => x.GLCount);
                tree.Add(gNode);
            }

            return tree;
        }
        #endregion

        #region EF Core Fallback Queries
        private async Task<List<ChartOfAccountsReportItemDto>> FetchViaEfCoreAsync(ChartOfAccountsReportFilterVM filter)
        {
            var search = (filter.SearchTerm ?? "").Trim().ToLower();

            var glQuery = _context.AccGeneralLedgers
                .AsNoTracking()
                .Where(gl => filter.ShowArchived || gl.IsActive == "1" || gl.IsActive == null);

            var groups = await _context.AccGroupLedgers.AsNoTracking().ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);
            var controls = await _context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName);
            var subControls = await _context.AccSubControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubControlLedgerCodeNo, x => x.SubControlLedgerName);
            var subSubs = await _context.AccSubSubsidiaryLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubSubsidiaryLedgerCodeNo, x => x.SubSubsidiaryLedgerName);

            var glList = await glQuery.ToListAsync();

            var result = glList.Select(gl => new ChartOfAccountsReportItemDto
            {
                GroupCode = gl.GroupLedgerCodeNo ?? "",
                GroupName = groups.TryGetValue(gl.GroupLedgerCodeNo ?? "", out var gn) ? gn : "",
                ControlCode = gl.ControlLedgerCodeNo ?? "",
                ControlName = controls.TryGetValue(gl.ControlLedgerCodeNo ?? "", out var cn) ? cn : "",
                SubControlCode = gl.SubControlLedgerCodeNo ?? "",
                SubControlName = subControls.TryGetValue(gl.SubControlLedgerCodeNo ?? "", out var scn) ? scn : "",
                SubSubsidiaryCode = gl.SubSubsidiaryLedgerCodeNo ?? "",
                SubSubsidiaryName = subSubs.TryGetValue(gl.SubSubsidiaryLedgerCodeNo ?? "", out var ssn) ? ssn : "",
                GeneralLedgerCode = gl.GeneralLedgerCodeNo ?? "",
                GeneralLedgerName = gl.GeneralLedgerName ?? "",
                ShortName = gl.ShortName,
                IsActive = gl.IsActive == "1" || gl.IsActive == null,
                AccountType = gl.GroupLedgerCodeNo == "1" ? "Asset" :
                              gl.GroupLedgerCodeNo == "2" ? "Liability & Equity" :
                              gl.GroupLedgerCodeNo == "3" ? "Income" :
                              gl.GroupLedgerCodeNo == "4" ? "Expense" : "Other"
            });

            if (!string.IsNullOrEmpty(search))
            {
                result = result.Where(x =>
                    x.GeneralLedgerCode.ToLower().Contains(search) ||
                    x.GeneralLedgerName.ToLower().Contains(search) ||
                    x.SubSubsidiaryName.ToLower().Contains(search) ||
                    x.SubControlName.ToLower().Contains(search) ||
                    x.ControlName.ToLower().Contains(search) ||
                    x.GroupName.ToLower().Contains(search));
            }

            return result
                .OrderBy(x => x.GroupCode)
                .ThenBy(x => x.ControlCode)
                .ThenBy(x => x.SubControlCode)
                .ThenBy(x => x.SubSubsidiaryCode)
                .ThenBy(x => x.GeneralLedgerCode)
                .ToList();
        }

        private async Task<ChartOfAccountsReportStatsDto> ComputeStatsViaEfCoreAsync(bool showArchived)
        {
            var glQuery = _context.AccGeneralLedgers.AsNoTracking().Where(x => showArchived || x.IsActive == "1" || x.IsActive == null);

            var stats = new ChartOfAccountsReportStatsDto
            {
                TotalGeneralLedgers = await glQuery.CountAsync(),
                TotalGroups = await _context.AccGroupLedgers.AsNoTracking().Where(x => showArchived || x.IsActive == "1" || x.IsActive == null).CountAsync(),
                TotalControls = await _context.AccControlLedgers.AsNoTracking().Where(x => showArchived || x.IsActive == "1" || x.IsActive == null).CountAsync(),
                TotalSubControls = await _context.AccSubControlLedgers.AsNoTracking().Where(x => showArchived || x.IsActive == "1" || x.IsActive == null).CountAsync(),
                TotalSubSubsidiaries = await _context.AccSubSubsidiaryLedgers.AsNoTracking().Where(x => showArchived || x.IsActive == "1" || x.IsActive == null).CountAsync(),
                AssetsGLCount = await glQuery.CountAsync(x => x.GroupLedgerCodeNo == "1"),
                EquityGLCount = await glQuery.CountAsync(x => x.GroupLedgerCodeNo == "2"),
                IncomeGLCount = await glQuery.CountAsync(x => x.GroupLedgerCodeNo == "3"),
                ExpenseGLCount = await glQuery.CountAsync(x => x.GroupLedgerCodeNo == "4")
            };

            return stats;
        }
        #endregion

        #region Export Excel (EPPlus)
        public async Task<byte[]> ExportExcelAsync(ChartOfAccountsReportFilterVM filter)
        {
            filter ??= new ChartOfAccountsReportFilterVM();
            var data = await GetReportDataAsync(filter);
            PopulateVisibleTreeIfProvided(data, filter);
            ExcelPackage.License.SetNonCommercialOrganization("GCTL");

            if (string.Equals(filter?.ViewMode, "tree", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateTreeExcel(data);
            }
            return GenerateListExcel(data);
        }

        private byte[] GenerateTreeExcel(ChartOfAccountsReportResponseVM data)
        {
            var nodes = (data?.VisibleTree != null && data.VisibleTree.Count > 0)
                ? data.VisibleTree
                : new List<ChartOfAccountsReportTreeDto>();

            if (nodes.Count == 0)
            {
                FlattenTreeNodes(data?.Tree, nodes);
            }

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("COA - Tree");
            ws.Cells.Style.Font.Name = "Segoe UI";
            ws.Cells.Style.Font.Size = 10;

            // Title Block
            ws.Cells["A1:D1"].Merge = true;
            ws.Cells["A1"].Value = data?.CompanyName ?? "Chart of Accounts";
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:D2"].Merge = true;
            ws.Cells["A2"].Value = "Chart of Accounts · Tree Hierarchy";
            ws.Cells["A2"].Style.Font.Size = 11;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A3:D3"].Merge = true;
            ws.Cells["A3"].Value = $"Total Ledgers: {data?.Stats?.TotalGeneralLedgers ?? 0}";
            ws.Cells["A3"].Style.Font.Size = 9;
            ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Header Row (Tree hierarchy headers)
            int row = 5;
            ws.Cells[row, 1].Value = "Account Code";
            ws.Cells[row, 2].Value = "Ledger Hierarchy & Account Head";
            ws.Cells[row, 3].Value = "Layer";
            ws.Cells[row, 4].Value = "Type / GL Count";

            using (var rng = ws.Cells[row, 1, row, 4])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(11, 77, 55));
                rng.Style.Font.Color.SetColor(Color.White);
            }

            foreach (var node in nodes)
            {
                row++;
                var treePrefix = node.Level switch
                {
                    0 => "■ ",
                    1 => "    ├── ",
                    2 => "        ├── ",
                    3 => "            ├── ",
                    4 => "                • ",
                    _ => ""
                };

                var layerText = node.Level switch
                {
                    0 => "Group Ledger (1st Layer)",
                    1 => "Control Ledger (2nd Layer)",
                    2 => "Sub-Control Ledger (3rd Layer)",
                    3 => "Sub-Subsidiary Ledger (4th Layer)",
                    4 => "General Ledger (5th Layer)",
                    _ => node.LevelName
                };

                var nameWithShort = string.IsNullOrWhiteSpace(node.ShortName)
                    ? node.Name
                    : $"{node.Name} ({node.ShortName})";

                var detailText = node.Level < 4
                    ? (node.IsOpen ? $"{node.GLCount} Child GL" : $"[+] {node.GLCount} Child GL (Collapsed)")
                    : (node.AccountType ?? "Asset");

                ws.Cells[row, 1].Value = node.Code;
                ws.Cells[row, 2].Value = $"{treePrefix}{nameWithShort}";
                ws.Cells[row, 3].Value = layerText;
                ws.Cells[row, 4].Value = detailText;

                if (node.Level == 0)
                {
                    ws.Cells[row, 1, row, 4].Style.Font.Bold = true;
                    ws.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(235, 243, 239));
                }
                else if (node.Level == 1)
                {
                    ws.Cells[row, 1, row, 4].Style.Font.Bold = true;
                }
                else if (row % 2 == 0)
                {
                    ws.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(250, 252, 250));
                }
            }

            // Bottom In-Sheet Footer Row
            row += 2;
            ws.Cells[row, 1, row, 2].Merge = true;
            ws.Cells[row, 1].Value = $"Print Date & Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
            ws.Cells[row, 1].Style.Font.Size = 9;
            ws.Cells[row, 1].Style.Font.Italic = true;

            ws.Cells[row, 3].Value = "GCTL Infosys";
            ws.Cells[row, 3].Style.Font.Size = 9;
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[row, 4].Value = "End of Report";
            ws.Cells[row, 4].Style.Font.Size = 9;
            ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.HeaderFooter.OddFooter.LeftAlignedText = $"Print Date & Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
            ws.HeaderFooter.OddFooter.CenteredText = "GCTL Infosys";
            ws.HeaderFooter.OddFooter.RightAlignedText = "Page " + ExcelHeaderFooter.PageNumber + " of " + ExcelHeaderFooter.NumberOfPages;

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        private byte[] GenerateListExcel(ChartOfAccountsReportResponseVM data)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("COA - List");
            ws.Cells.Style.Font.Name = "Segoe UI";
            ws.Cells.Style.Font.Size = 10;

            // Title Block
            ws.Cells["A1:F1"].Merge = true;
            ws.Cells["A1"].Value = data?.CompanyName ?? "Chart of Accounts";
            ws.Cells["A1"].Style.Font.Size = 15;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:F2"].Merge = true;
            ws.Cells["A2"].Value = "Chart of Accounts · List View (5-Level Hierarchy)";
            ws.Cells["A2"].Style.Font.Size = 12;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A3:F3"].Merge = true;
            ws.Cells["A3"].Value = $"Total Ledgers: {data?.Stats?.TotalGeneralLedgers ?? 0}";
            ws.Cells["A3"].Style.Font.Size = 9;
            ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Summary Stats Strip Row
            int statRow = 5;
            ws.Cells[statRow, 1].Value = "Total Ledgers:";
            ws.Cells[statRow, 2].Value = data?.Stats?.TotalGeneralLedgers ?? 0;
            ws.Cells[statRow, 3].Value = "Assets:";
            ws.Cells[statRow, 4].Value = data?.Stats?.AssetsGLCount ?? 0;
            ws.Cells[statRow, 5].Value = "Liabilities & Equity:";
            ws.Cells[statRow, 6].Value = data?.Stats?.EquityGLCount ?? 0;

            using (var rng = ws.Cells[statRow, 1, statRow, 6])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 235, 230));
            }

            // Headers
            int row = 7;
            ws.Cells[row, 1].Value = "Group Ledger (1st Layer)";
            ws.Cells[row, 2].Value = "Control Ledger (2nd Layer)";
            ws.Cells[row, 3].Value = "Sub-Control Ledger (3rd Layer)";
            ws.Cells[row, 4].Value = "Sub-Subsidiary Ledger (4th Layer)";
            ws.Cells[row, 5].Value = "Account Code";
            ws.Cells[row, 6].Value = "Account Head (5th Layer)";

            using (var rng = ws.Cells[row, 1, row, 6])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(11, 77, 55));
                rng.Style.Font.Color.SetColor(Color.White);
            }

            // Data Rows
            var list = data?.List ?? new List<ChartOfAccountsReportItemDto>();
            foreach (var item in list)
            {
                row++;
                ws.Cells[row, 1].Value = $"{item.GroupCode}-{item.GroupName}";
                ws.Cells[row, 2].Value = $"{item.ControlCode}-{item.ControlName}";
                ws.Cells[row, 3].Value = $"{item.SubControlCode}-{item.SubControlName}";
                ws.Cells[row, 4].Value = $"{item.SubSubsidiaryCode}-{item.SubSubsidiaryName}";
                ws.Cells[row, 5].Value = item.GeneralLedgerCode ?? "";
                var glNameWithShort = item.GeneralLedgerName ?? "";
                ws.Cells[row, 6].Value = glNameWithShort;

                if (row % 2 == 0)
                {
                    ws.Cells[row, 1, row, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(249, 251, 249));
                }
            }

            // Borders & Column auto-fit
            using (var rng = ws.Cells[7, 1, row, 6])
            {
                rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Top.Color.SetColor(Color.FromArgb(212, 220, 215));
                rng.Style.Border.Bottom.Color.SetColor(Color.FromArgb(212, 220, 215));
                rng.Style.Border.Left.Color.SetColor(Color.FromArgb(212, 220, 215));
                rng.Style.Border.Right.Color.SetColor(Color.FromArgb(212, 220, 215));
            }

            // Bottom In-Sheet Footer Row
            row += 2;
            ws.Cells[row, 1, row, 2].Merge = true;
            ws.Cells[row, 1].Value = $"Print Date & Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
            ws.Cells[row, 1].Style.Font.Size = 9;
            ws.Cells[row, 1].Style.Font.Italic = true;

            ws.Cells[row, 3, row, 4].Merge = true;
            ws.Cells[row, 3].Value = "GCTL Infosys";
            ws.Cells[row, 3].Style.Font.Size = 9;
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[row, 5, row, 6].Merge = true;
            ws.Cells[row, 5].Value = "End of Report";
            ws.Cells[row, 5].Style.Font.Size = 9;
            ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            // Header & Footer for Print in Excel
            ws.HeaderFooter.OddFooter.LeftAlignedText = $"Print Date & Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
            ws.HeaderFooter.OddFooter.CenteredText = "GCTL Infosys";
            ws.HeaderFooter.OddFooter.RightAlignedText = "Page " + ExcelHeaderFooter.PageNumber + " of " + ExcelHeaderFooter.NumberOfPages;

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }
        #endregion

        #region Export PDF (QuestPDF)
        public async Task<byte[]> ExportPdfAsync(ChartOfAccountsReportFilterVM filter)
        {
            filter ??= new ChartOfAccountsReportFilterVM();
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var data = await GetReportDataAsync(filter);
            PopulateVisibleTreeIfProvided(data, filter);
            var document = new ChartOfAccountsReportPdf(data, filter.ViewMode);
            return document.GeneratePdf();
        }
        #endregion

        #region Export CSV
        public async Task<byte[]> ExportCsvAsync(ChartOfAccountsReportFilterVM filter)
        {
            filter ??= new ChartOfAccountsReportFilterVM();
            var data = await GetReportDataAsync(filter);
            PopulateVisibleTreeIfProvided(data, filter);

            if (string.Equals(filter?.ViewMode, "tree", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateTreeCsv(data);
            }
            return GenerateListCsv(data);
        }

        private byte[] GenerateTreeCsv(ChartOfAccountsReportResponseVM data)
        {
            var nodes = (data?.VisibleTree != null && data.VisibleTree.Count > 0)
                ? data.VisibleTree
                : new List<ChartOfAccountsReportTreeDto>();

            if (nodes.Count == 0)
            {
                FlattenTreeNodes(data?.Tree, nodes);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Account Code,Tree Hierarchy & Account Head,Layer,Type / GL Count,Status");

            foreach (var node in nodes)
            {
                var treePrefix = node.Level switch
                {
                    0 => "■ ",
                    1 => "    ├── ",
                    2 => "        ├── ",
                    3 => "            ├── ",
                    4 => "                • ",
                    _ => ""
                };

                var layerText = node.Level switch
                {
                    0 => "Group Ledger (1st Layer)",
                    1 => "Control Ledger (2nd Layer)",
                    2 => "Sub-Control Ledger (3rd Layer)",
                    3 => "Sub-Subsidiary Ledger (4th Layer)",
                    4 => "General Ledger (5th Layer)",
                    _ => node.LevelName
                };

                var nameWithShort = string.IsNullOrWhiteSpace(node.ShortName)
                    ? node.Name
                    : $"{node.Name} ({node.ShortName})";

                var detailText = node.Level < 4
                    ? (node.IsOpen ? $"{node.GLCount} Child GL" : $"[+] {node.GLCount} Child GL (Collapsed)")
                    : (node.AccountType ?? "Asset");

                var statusText = node.IsActive ? "Active" : "Archived";

                sb.AppendLine($"\"{EscapeCsv(node.Code)}\",\"{EscapeCsv(treePrefix + nameWithShort)}\",\"{EscapeCsv(layerText)}\",\"{EscapeCsv(detailText)}\",\"{statusText}\"");
            }

            sb.AppendLine();
            sb.AppendLine($"\"Print Date & Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}\",\"\",\"GCTL Infosys\",\"End of Report\"");

            var preamble = Encoding.UTF8.GetPreamble();
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return preamble.Concat(bytes).ToArray();
        }

        private static void PopulateVisibleTreeIfProvided(ChartOfAccountsReportResponseVM data, ChartOfAccountsReportFilterVM filter)
        {
            if (data == null || filter == null || string.IsNullOrWhiteSpace(filter.VisibleTreeJson)) return;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var visibleNodes = System.Text.Json.JsonSerializer.Deserialize<List<ChartOfAccountsReportTreeDto>>(filter.VisibleTreeJson, options);
                if (visibleNodes != null && visibleNodes.Count > 0)
                {
                    data.VisibleTree = visibleNodes;
                }
            }
            catch
            {
                // Fallback to full tree
            }
        }

        private byte[] GenerateListCsv(ChartOfAccountsReportResponseVM data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Group Ledger (1st Layer) Code,Group Ledger (1st Layer) Name,Control Ledger (2nd Layer) Code,Control Ledger (2nd Layer) Name,Sub-Control Ledger (3rd Layer) Code,Sub-Control Ledger (3rd Layer) Name,Sub-Subsidiary Ledger (4th Layer) Code,Sub-Subsidiary Ledger (4th Layer) Name,Account Code,Account Head (5th Layer)");

            var list = data?.List ?? new List<ChartOfAccountsReportItemDto>();
            foreach (var item in list)
            {
                var glNameWithShort = item.GeneralLedgerName ?? "";
                sb.AppendLine($"\"{EscapeCsv(item.GroupCode)}\",\"{EscapeCsv(item.GroupName)}\",\"{EscapeCsv(item.ControlCode)}\",\"{EscapeCsv(item.ControlName)}\",\"{EscapeCsv(item.SubControlCode)}\",\"{EscapeCsv(item.SubControlName)}\",\"{EscapeCsv(item.SubSubsidiaryCode)}\",\"{EscapeCsv(item.SubSubsidiaryName)}\",\"{EscapeCsv(item.GeneralLedgerCode)}\",\"{EscapeCsv(glNameWithShort)}\"");
            }

            sb.AppendLine();
            sb.AppendLine($"\"Print Date & Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}\",\"\",\"\",\"GCTL Infosys\",\"\",\"End of Report\"");

            var preamble = Encoding.UTF8.GetPreamble();
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return preamble.Concat(bytes).ToArray();
        }

        private static void FlattenTreeNodes(List<ChartOfAccountsReportTreeDto>? nodes, List<ChartOfAccountsReportTreeDto> result)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                result.Add(node);
                if (node.Children != null && node.Children.Count > 0)
                {
                    FlattenTreeNodes(node.Children, result);
                }
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "\"\"");
        }
        #endregion
    }
}
