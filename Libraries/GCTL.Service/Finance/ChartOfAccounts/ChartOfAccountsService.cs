using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public class ChartOfAccountsService : IChartOfAccountsService
    {
        private readonly GCTL_ERP_DBContext context;
        private readonly ILogger<ChartOfAccountsService>? _logger;

        public ChartOfAccountsService(GCTL_ERP_DBContext context, ILogger<ChartOfAccountsService>? logger = null)
        {
            this.context = context;
            this._logger = logger;
        }

        public async Task<List<ChartOfAccountsTreeItemVM>> GetTreeAsync(bool showArchived = false, string search = "")
        {
            var groups = await context.AccGroupLedgers
                .AsNoTracking()
                .Where(x => showArchived || x.IsActive == "1" || x.IsActive == null)
                .OrderBy(x => x.GroupLedgerCodeNo)
                .ToListAsync();

            var controls = await context.AccControlLedgers
                .AsNoTracking()
                .Where(x => showArchived || x.IsActive == "1" || x.IsActive == null)
                .OrderBy(x => x.ControlLedgerCodeNo)
                .ToListAsync();

            var subControls = await context.AccSubControlLedgers
                .AsNoTracking()
                .Where(x => showArchived || x.IsActive == "1" || x.IsActive == null)
                .OrderBy(x => x.SubControlLedgerCodeNo)
                .ToListAsync();

            var subsidiaries = await context.AccSubsidiaryLedgers
                .AsNoTracking()
                .Where(x => showArchived || x.IsActive == "1" || x.IsActive == null)
                .OrderBy(x => x.SubsidiaryLedgerCodeNo)
                .ToListAsync();

            var generalLedgers = await context.AccGeneralLedgers
                .AsNoTracking()
                .Where(x => showArchived || x.IsActive == "1" || x.IsActive == null)
                .OrderBy(x => x.GeneralLedgerCodeNo)
                .ToListAsync();

            // Lookup GL count by Subsidiary
            var glBySL = generalLedgers
                .GroupBy(x => x.SubsidiaryLedgerCodeNo ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            var slBySCL = subsidiaries
                .GroupBy(x => x.SubControlLedgerCodeNo ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            var sclByCL = subControls
                .GroupBy(x => x.ControlLedgerCodeNo ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            var clByGRL = controls
                .GroupBy(x => x.GroupLedgerCodeNo ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<ChartOfAccountsTreeItemVM>();

            foreach (var g in groups)
            {
                var gNode = new ChartOfAccountsTreeItemVM
                {
                    Code = g.GroupLedgerCodeNo,
                    Name = g.GroupLedgerName,
                    ShortName = g.ShortName,
                    Level = 0,
                    ParentCode = string.Empty,
                    IsActive = g.IsActive == "1" || g.IsActive == null
                };

                if (clByGRL.TryGetValue(g.GroupLedgerCodeNo, out var groupControls))
                {
                    foreach (var c in groupControls)
                    {
                        var cNode = new ChartOfAccountsTreeItemVM
                        {
                            Code = c.ControlLedgerCodeNo,
                            Name = c.ControlLedgerName,
                            ShortName = c.ShortName,
                            Level = 1,
                            ParentCode = g.GroupLedgerCodeNo,
                            IsActive = c.IsActive == "1" || c.IsActive == null
                        };

                        if (sclByCL.TryGetValue(c.ControlLedgerCodeNo, out var ctrlSubControls))
                        {
                            foreach (var sc in ctrlSubControls)
                            {
                                var scNode = new ChartOfAccountsTreeItemVM
                                {
                                    Code = sc.SubControlLedgerCodeNo,
                                    Name = sc.SubControlLedgerName,
                                    ShortName = sc.ShortName,
                                    Level = 2,
                                    ParentCode = c.ControlLedgerCodeNo,
                                    IsActive = sc.IsActive == "1" || sc.IsActive == null
                                };

                                if (slBySCL.TryGetValue(sc.SubControlLedgerCodeNo, out var scSubs))
                                {
                                    foreach (var sl in scSubs)
                                    {
                                        var slCode = sl.SubsidiaryLedgerCodeNo ?? sl.SusidiaryLedgerCodeNo ?? string.Empty;
                                        var slNode = new ChartOfAccountsTreeItemVM
                                        {
                                            Code = slCode,
                                            Name = sl.SubsidiaryLedgerName,
                                            ShortName = sl.ShortName,
                                            Level = 3,
                                            ParentCode = sc.SubControlLedgerCodeNo,
                                            IsActive = sl.IsActive == "1" || sl.IsActive == null
                                        };

                                        if (glBySL.TryGetValue(slCode, out var slGLs))
                                        {
                                            foreach (var gl in slGLs)
                                            {
                                                slNode.Children.Add(new ChartOfAccountsTreeItemVM
                                                {
                                                    Code = gl.GeneralLedgerCodeNo,
                                                    Name = gl.GeneralLedgerName,
                                                    ShortName = gl.ShortName,
                                                    Level = 4,
                                                    ParentCode = slCode,
                                                    IsActive = gl.IsActive == "1" || gl.IsActive == null,
                                                    GLCount = 1
                                                });
                                            }
                                        }

                                        slNode.GLCount = slNode.Children.Count;
                                        scNode.Children.Add(slNode);
                                    }
                                }

                                scNode.GLCount = scNode.Children.Sum(x => x.GLCount);
                                cNode.Children.Add(scNode);
                            }
                        }

                        cNode.GLCount = cNode.Children.Sum(x => x.GLCount);
                        gNode.Children.Add(cNode);
                    }
                }

                gNode.GLCount = gNode.Children.Sum(x => x.GLCount);
                result.Add(gNode);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                result = FilterTree(result, term);
            }

            return result;
        }

        private List<ChartOfAccountsTreeItemVM> FilterTree(List<ChartOfAccountsTreeItemVM> nodes, string term)
        {
            var filtered = new List<ChartOfAccountsTreeItemVM>();
            foreach (var node in nodes)
            {
                var matchingChildren = FilterTree(node.Children, term);
                bool selfMatch = node.Code.ToLower().Contains(term) ||
                                 node.Name.ToLower().Contains(term) ||
                                 (node.ShortName != null && node.ShortName.ToLower().Contains(term));

                if (selfMatch || matchingChildren.Any())
                {
                    var clone = new ChartOfAccountsTreeItemVM
                    {
                        Code = node.Code,
                        Name = node.Name,
                        ShortName = node.ShortName,
                        Level = node.Level,
                        ParentCode = node.ParentCode,
                        IsActive = node.IsActive,
                        GLCount = node.GLCount,
                        Children = matchingChildren.Any() ? matchingChildren : node.Children
                    };
                    filtered.Add(clone);
                }
            }
            return filtered;
        }

        public async Task<ChartOfAccountsStatsVM> GetStatsAsync()
        {
            var groupCount = await context.AccGroupLedgers.CountAsync();
            var controlCount = await context.AccControlLedgers.CountAsync();
            var subControlCount = await context.AccSubControlLedgers.CountAsync();
            var subsidiaryCount = await context.AccSubsidiaryLedgers.CountAsync();
            var glCount = await context.AccGeneralLedgers.CountAsync();

            var groups = await context.AccGroupLedgers
                .AsNoTracking()
                .OrderBy(x => x.GroupLedgerCodeNo)
                .Select(x => new
                {
                    x.GroupLedgerCodeNo,
                    x.GroupLedgerName
                })
                .ToListAsync();

            var glGroupCounts = await context.AccGeneralLedgers
                .GroupBy(x => x.GroupLedgerCodeNo ?? string.Empty)
                .Select(g => new { GroupCode = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GroupCode, x => x.Count);

            var groupStats = groups.Select(g => new GroupStatItemVM
            {
                GroupCode = g.GroupLedgerCodeNo,
                GroupName = g.GroupLedgerName,
                GLCount = glGroupCounts.ContainsKey(g.GroupLedgerCodeNo) ? glGroupCounts[g.GroupLedgerCodeNo] : 0
            }).ToList();

            return new ChartOfAccountsStatsVM
            {
                TotalLedgers = glCount,
                TotalGroups = groupCount,
                TotalControls = controlCount,
                TotalSubControls = subControlCount,
                TotalSubsidiaries = subsidiaryCount,
                TotalSubSubsidiaries = subsidiaryCount,
                TotalGeneralLedgers = glCount,
                GroupStats = groupStats
            };
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var gls = await context.AccGeneralLedgers
                .AsNoTracking()
                .OrderBy(x => x.GeneralLedgerCodeNo)
                .ToListAsync();

            var groups = await context.AccGroupLedgers.AsNoTracking().ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName);
            var controls = await context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName);
            var subControls = await context.AccSubControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubControlLedgerCodeNo, x => x.SubControlLedgerName);
            var subsidiaries = await context.AccSubsidiaryLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubsidiaryLedgerCodeNo ?? x.SusidiaryLedgerCodeNo ?? string.Empty, x => x.SubsidiaryLedgerName);

            var sb = new StringBuilder();
            sb.AppendLine("GL Code,GL Name,Group,Control,Sub-Control,Subsidiary,Status");

            foreach (var x in gls)
            {
                var glCode = EscapeCsv(x.GeneralLedgerCodeNo);
                var glName = EscapeCsv(x.GeneralLedgerName);
                var grp = EscapeCsv(x.GroupLedgerCodeNo != null && groups.TryGetValue(x.GroupLedgerCodeNo, out var gn) ? gn : "");
                var ctrl = EscapeCsv(x.ControlLedgerCodeNo != null && controls.TryGetValue(x.ControlLedgerCodeNo, out var cn) ? cn : "");
                var subCtrl = EscapeCsv(x.SubControlLedgerCodeNo != null && subControls.TryGetValue(x.SubControlLedgerCodeNo, out var scn) ? scn : "");
                var slCode = x.SubsidiaryLedgerCodeNo ?? "";
                var sub = EscapeCsv(subsidiaries.TryGetValue(slCode, out var sn) ? sn : "");
                var status = x.IsActive == "1" || x.IsActive == null ? "Active" : "Archived";

                sb.AppendLine($"{glCode},{glName},{grp},{ctrl},{subCtrl},{sub},{status}");
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return $"\"{value}\"";
        }

        public async Task<CommonReturnViewModel> SeedDefaultChartOfAccountsAsync()
        {
            var res = new CommonReturnViewModel();
            try
            {
                if (await context.AccGroupLedgers.AnyAsync())
                {
                    res.Success = false;
                    res.Message = "Chart of Accounts already has data. Seeding was cancelled to preserve existing ledgers.";
                    return res;
                }

                const string rawSeed = @"
Assets
-Non-Current Assets
--Property Plant & Equipment/Fixed Assets
---Property Plant & Equipment/Fixed Assets
----School campus land
----School Buildings
----Furniture and Fixture
----Office & Electric Equipment
----Computer & IT Equipment
----Laboratory Equipment
----Library Equipment
----Sports Equipment
----Vehicles
---Accumulated Depreciation
----Accu. Depreciation on School Buildings
----Accu. Depreciation on Computers & IT Equipment
----Accu. Depreciation on Laboratory Equipment
----Accu. Depreciation on Vehicles
--Intangible Assets
---Software Application
----School Management ERP
---Website Development
----School Website
-Current Assets
--Accounts Receivable
---Accounts Receivable
----Student Fees
--Cash and Cash Equivalents
---Cash in Hand
----Cash in Hand
----MFS Clearing / Mobile Financial Services
----Cheques in Hand
---Bank Account
----DBBL
--Inventory
---Inventory Asset
----Stationery Materials
----Printing Materials
----Teaching Materials
----Examination Materials
----Cleaning Materials
----Sports Materials
Equity & Liabilities
-Equity
--Capital & Funds
---Capital & Funds
----Owner's Equity / School Fund
--Retained Earnings
---Retained Earnings
----Retained Earnings
-Non-Current Liabilities
--Long Term Loan
---Long Term Loan- Bank A/C
----Long Term Loan- Bank A/C
-Current Liabilities
--Trade Payables
---Trade Payables
----Accounts Payable – Suppliers
--Statutory Payables
---Statutory Payables
----Salary Payable
----Provident Fund Payable
----Tax Payable
Income
-Operating Income
--Student Revenue
---Student Revenue
----Tuition Fee Income
----Admission Fee Income
----Transport Fee Income
----Exam Fee Income
-Non-Operating Income
--Other Income
---Other Income
----Fine Income
----Other Income
Expenses
-Direct Expenses
--Payroll Expenses
---Payroll Expenses
----Salary Expense
--Facility Expenses
---Facility Expenses
----Utility Expense
----Maintenance Expense
--Administrative Expenses
---Administrative Expenses
----Office Supplies Expense
--Transport Expense
---Transport Expense
----Transport Expense
--Depreciation Expense
---Depreciation Expense
----Depreciation Expense
--Other Expense
---Other Expense
----Other Expense
-Indirect Expenses
--Bank Charges
---Bank Charges
----Bank Charges
--Insurance
---Insurance
----Insurance
--Other Administrative Expenses
---Other Administrative Expenses
----Other Administrative Expenses
";

                var widths = new[] { 1, 2, 3, 5, 5 };
                var stack = new string[5];
                var counter = new int[5];

                var lines = rawSeed.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var groups = new List<AccGroupLedger>();
                var controls = new List<AccControlLedger>();
                var subControls = new List<AccSubControlLedger>();
                var subsidiaries = new List<AccSubsidiaryLedger>();
                var generalLedgers = new List<AccGeneralLedger>();

                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"^(-*)(.*)$");
                    if (!match.Success) continue;

                    int level = match.Groups[1].Length;
                    string name = match.Groups[2].Value.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    string parent = level > 0 ? stack[level - 1] : "";
                    counter[level]++;
                    for (int i = level + 1; i < 5; i++) counter[i] = 0;

                    string code = level > 0
                        ? parent + counter[level].ToString().PadLeft(widths[level], '0')
                        : counter[level].ToString();

                    stack[level] = code;

                    switch (level)
                    {
                        case 0:
                            groups.Add(new AccGroupLedger
                            {
                                GroupLedgerCodeNo = code,
                                GroupLedgerName = name,
                                ShortName = name.Length > 20 ? name.Substring(0, 20) : name,
                                IsActive = "1",
                                Ldate = DateTime.Now
                            });
                            break;
                        case 1:
                            controls.Add(new AccControlLedger
                            {
                                ControlLedgerCodeNo = code,
                                GroupLedgerCodeNo = stack[0],
                                ControlLedgerName = name,
                                ShortName = name.Length > 20 ? name.Substring(0, 20) : name,
                                IsActive = "1",
                                Ldate = DateTime.Now
                            });
                            break;
                        case 2:
                            subControls.Add(new AccSubControlLedger
                            {
                                SubControlLedgerCodeNo = code,
                                GroupLedgerCodeNo = stack[0],
                                ControlLedgerCodeNo = stack[1],
                                SubControlLedgerName = name,
                                ShortName = name.Length > 20 ? name.Substring(0, 20) : name,
                                IsActive = "1",
                                Ldate = DateTime.Now
                            });
                            break;
                        case 3:
                            subsidiaries.Add(new AccSubsidiaryLedger
                            {
                                SubsidiaryLedgerCodeNo = code,
                                SusidiaryLedgerCodeNo = code,
                                GroupLedgerCodeNo = stack[0],
                                ControlLedgerCodeNo = stack[1],
                                SubControlLedgerCodeNo = stack[2],
                                SubsidiaryLedgerName = name,
                                ShortName = name.Length > 20 ? name.Substring(0, 20) : name,
                                IsActive = "1",
                                Ldate = DateTime.Now
                            });
                            break;
                        case 4:
                            generalLedgers.Add(new AccGeneralLedger
                            {
                                GeneralLedgerCodeNo = code,
                                GroupLedgerCodeNo = stack[0],
                                ControlLedgerCodeNo = stack[1],
                                SubControlLedgerCodeNo = stack[2],
                                SubsidiaryLedgerCodeNo = stack[3],
                                GeneralLedgerName = name,
                                ShortName = name.Length > 20 ? name.Substring(0, 20) : name,
                                IsActive = "1",
                                Ldate = DateTime.Now
                            });
                            break;
                    }
                }

                await context.AccGroupLedgers.AddRangeAsync(groups);
                await context.SaveChangesAsync();

                await context.AccControlLedgers.AddRangeAsync(controls);
                await context.SaveChangesAsync();

                await context.AccSubControlLedgers.AddRangeAsync(subControls);
                await context.SaveChangesAsync();

                await context.AccSubsidiaryLedgers.AddRangeAsync(subsidiaries);
                await context.SaveChangesAsync();

                await context.AccGeneralLedgers.AddRangeAsync(generalLedgers);
                await context.SaveChangesAsync();

                res.Success = true;
                res.Message = $"Default Chart of Accounts seeded successfully with {groups.Count} Groups, {controls.Count} Controls, {subControls.Count} Sub-Controls, {subsidiaries.Count} Subsidiaries, and {generalLedgers.Count} General Ledgers.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
                _logger?.LogError(ex, "Error seeding default Chart of Accounts");
            }
            return res;
        }
    }
}
