using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.TrialBalance;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.TrialBalance
{
    public class TrialBalanceService : ITrialBalanceService
    {
        private readonly IDbConnection _dbConnection;
        private readonly GCTL_ERP_DBContext _context;

        public TrialBalanceService(
            IDbConnection dbConnection,
            GCTL_ERP_DBContext context)
        {
            _dbConnection = dbConnection;
            _context = context;
        }

        public async Task<TrialBalanceReportVM> GetInitialViewModelAsync()
        {
            var filter = new TrialBalanceFilterVM();
            var companies = await GetCompaniesAsync();
            var firstOrg = companies.FirstOrDefault();
            if (firstOrg != null && !string.IsNullOrWhiteSpace(firstOrg.Code))
            {
                filter.Main_CompanyCode = firstOrg.Code;
            }

            var report = new TrialBalanceReportVM
            {
                Filter = filter,
                Companies = companies,
                Branches = await GetBranchesAsync(filter.Main_CompanyCode)
            };

            if (firstOrg != null && !string.IsNullOrWhiteSpace(firstOrg.Name))
            {
                report.CompanyName = firstOrg.Name;
            }

            return report;
        }

        public async Task<TrialBalanceReportVM> GetTrialBalanceReportAsync(TrialBalanceFilterVM filter)
        {
            filter ??= new TrialBalanceFilterVM();
            var report = new TrialBalanceReportVM
            {
                Filter = filter
            };

            var branchParam = (filter.BranchCode == "All" || string.IsNullOrEmpty(filter.BranchCode)) ? string.Empty : filter.BranchCode;
            var companyParam = filter.Main_CompanyCode ?? string.Empty;

            if (!string.IsNullOrEmpty(companyParam) && companyParam.Length < 3 && int.TryParse(companyParam, out var cNum))
            {
                companyParam = cNum.ToString("D3");
            }

            var parameters = new DynamicParameters();
            parameters.Add("@Main_CompanyCode", companyParam);
            parameters.Add("@BranchCode", branchParam);
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);

            List<TrialBalanceSpResultDto> spList = new List<TrialBalanceSpResultDto>();
            try
            {
                var conn = _dbConnection ?? _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    if (conn is System.Data.Common.DbConnection dbConn)
                        await dbConn.OpenAsync();
                    else
                        conn.Open();
                }

                var results = await conn.QueryAsync<TrialBalanceSpResultDto>(
                    "Rpt_proc_TrialBalanceReportF2",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                spList = results.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SP TrialBalance error: {ex.Message}");
                spList = new List<TrialBalanceSpResultDto>();
            }

            report.RawItems = spList;

            // Check if Stored Procedure already provided the hierarchical roll-up (Levels 1 to 5)
            if (spList.Any(x => x.Level == 1))
            {
                report.Items = spList
                    .Where(x => x.Level >= 1 && x.Level <= 5)
                    .Select(x => new TrialBalanceHierarchyItemDto
                    {
                        Code = x.Code,
                        Name = x.ParticularName,
                        Level = x.Level,
                        ParentCode = x.ParentCode,
                        OpeningDebit = x.OpeningDebit,
                        OpeningCredit = x.OpeningCredit,
                        PeriodDebit = x.PeriodDebit,
                        PeriodCredit = x.PeriodCredit,
                        ClosingDebit = x.ClosingDebit,
                        ClosingCredit = x.ClosingCredit
                    })
                    .ToList();

                var grandTotal = spList.FirstOrDefault(x => x.Level == 0 || (x.ParticularName ?? "").Contains("Grand Total"));
                if (grandTotal != null)
                {
                    report.Summary.TotalOpeningDebit = grandTotal.OpeningDebit;
                    report.Summary.TotalOpeningCredit = grandTotal.OpeningCredit;
                    report.Summary.TotalPeriodDebit = grandTotal.PeriodDebit;
                    report.Summary.TotalPeriodCredit = grandTotal.PeriodCredit;
                    report.Summary.TotalClosingDebit = grandTotal.ClosingDebit;
                    report.Summary.TotalClosingCredit = grandTotal.ClosingCredit;
                    report.Summary.Difference = Math.Abs(grandTotal.ClosingDebit - grandTotal.ClosingCredit);
                }
                else
                {
                    var l1 = report.Items.Where(x => x.Level == 1).ToList();
                    report.Summary.TotalOpeningDebit = l1.Sum(x => x.OpeningDebit);
                    report.Summary.TotalOpeningCredit = l1.Sum(x => x.OpeningCredit);
                    report.Summary.TotalPeriodDebit = l1.Sum(x => x.PeriodDebit);
                    report.Summary.TotalPeriodCredit = l1.Sum(x => x.PeriodCredit);
                    report.Summary.TotalClosingDebit = l1.Sum(x => x.ClosingDebit);
                    report.Summary.TotalClosingCredit = l1.Sum(x => x.ClosingCredit);
                    report.Summary.Difference = Math.Abs(report.Summary.TotalClosingDebit - report.Summary.TotalClosingCredit);
                }
            }
            else
            {
                // Fallback: build hierarchy from Acc_GeneralLedger if SP only returned raw/level-5 items
                var groups = new Dictionary<string, string>();
                var controls = new Dictionary<string, string>();
                var subControls = new Dictionary<string, string>();
                var subSubs = new Dictionary<string, string>();

                try { groups = await _context.AccGroupLedgers.AsNoTracking().ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName); } catch { }
                try { controls = await _context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName); } catch { }
                try { subControls = await _context.AccSubControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubControlLedgerCodeNo, x => x.SubControlLedgerName); } catch { }
                try { subSubs = await _context.AccSubSubsidiaryLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubSubsidiaryLedgerCodeNo, x => x.SubSubsidiaryLedgerName); } catch { }

                var glHierarchy = await _context.AccGeneralLedgers
                    .Where(x => x.IsActive == null || x.IsActive == "1")
                    .AsNoTracking()
                    .ToListAsync();

                var hierarchyMap = new Dictionary<string, TrialBalanceHierarchyItemDto>(StringComparer.OrdinalIgnoreCase);

                foreach (var gl in glHierarchy)
                {
                    var matched = spList.FirstOrDefault(s =>
                        string.Equals(s.Code?.Trim(), gl.GeneralLedgerCodeNo?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.ParticularName?.Trim(), gl.GeneralLedgerName?.Trim(), StringComparison.OrdinalIgnoreCase));

                    decimal opDr = matched?.OpeningDebit ?? 0;
                    decimal opCr = matched?.OpeningCredit ?? 0;
                    decimal pDr = matched?.PeriodDebit ?? 0;
                    decimal pCr = matched?.PeriodCredit ?? 0;
                    decimal cDr = matched?.ClosingDebit ?? 0;
                    decimal cCr = matched?.ClosingCredit ?? 0;

                    // Level 5: General Ledger
                    string glKey = $"L5_{gl.GeneralLedgerCodeNo}";
                    if (!hierarchyMap.ContainsKey(glKey))
                    {
                        hierarchyMap[glKey] = new TrialBalanceHierarchyItemDto
                        {
                            Code = gl.GeneralLedgerCodeNo,
                            Name = gl.GeneralLedgerName,
                            Level = 5,
                            ParentCode = gl.SubSubsidiaryLedgerCodeNo,
                            OpeningDebit = opDr,
                            OpeningCredit = opCr,
                            PeriodDebit = pDr,
                            PeriodCredit = pCr,
                            ClosingDebit = cDr,
                            ClosingCredit = cCr
                        };
                    }

                    // Level 4: Sub-Subsidiary Ledger
                    string l4Code = gl.SubSubsidiaryLedgerCodeNo ?? string.Empty;
                    string l4Key = $"L4_{l4Code}";
                    if (!hierarchyMap.ContainsKey(l4Key))
                    {
                        string ssName = subSubs.TryGetValue(l4Code, out var ssn) ? ssn : "Sub-Subsidiary Ledger";
                        hierarchyMap[l4Key] = new TrialBalanceHierarchyItemDto
                        {
                            Code = l4Code,
                            Name = ssName,
                            Level = 4,
                            ParentCode = gl.SubControlLedgerCodeNo
                        };
                    }

                    // Level 3: Sub-Control Ledger
                    string l3Code = gl.SubControlLedgerCodeNo ?? string.Empty;
                    string l3Key = $"L3_{l3Code}";
                    if (!hierarchyMap.ContainsKey(l3Key))
                    {
                        string scName = subControls.TryGetValue(l3Code, out var scn) ? scn : "Sub-Control Ledger";
                        hierarchyMap[l3Key] = new TrialBalanceHierarchyItemDto
                        {
                            Code = l3Code,
                            Name = scName,
                            Level = 3,
                            ParentCode = gl.ControlLedgerCodeNo
                        };
                    }

                    // Level 2: Control Ledger
                    string l2Code = gl.ControlLedgerCodeNo ?? string.Empty;
                    string l2Key = $"L2_{l2Code}";
                    if (!hierarchyMap.ContainsKey(l2Key))
                    {
                        string cName = controls.TryGetValue(l2Code, out var cn) ? cn : "Control Ledger";
                        hierarchyMap[l2Key] = new TrialBalanceHierarchyItemDto
                        {
                            Code = l2Code,
                            Name = cName,
                            Level = 2,
                            ParentCode = gl.GroupLedgerCodeNo
                        };
                    }

                    // Level 1: Group Ledger
                    string l1Code = gl.GroupLedgerCodeNo ?? string.Empty;
                    string l1Key = $"L1_{l1Code}";
                    if (!hierarchyMap.ContainsKey(l1Key))
                    {
                        string gName = groups.TryGetValue(l1Code, out var gn) ? gn : "Group Ledger";
                        hierarchyMap[l1Key] = new TrialBalanceHierarchyItemDto
                        {
                            Code = l1Code,
                            Name = gName,
                            Level = 1,
                            ParentCode = null
                        };
                    }
                }

                var allItems = hierarchyMap.Values.ToList();
                RollupAmounts(allItems);
                report.Items = OrderHierarchyItems(allItems);

                var l1Items = allItems.Where(x => x.Level == 1).ToList();
                report.Summary.TotalClosingDebit = l1Items.Sum(x => x.ClosingDebit);
                report.Summary.TotalClosingCredit = l1Items.Sum(x => x.ClosingCredit);
                report.Summary.TotalPeriodDebit = l1Items.Sum(x => x.PeriodDebit);
                report.Summary.TotalPeriodCredit = l1Items.Sum(x => x.PeriodCredit);
                report.Summary.TotalOpeningDebit = l1Items.Sum(x => x.OpeningDebit);
                report.Summary.TotalOpeningCredit = l1Items.Sum(x => x.OpeningCredit);
                report.Summary.Difference = Math.Abs(report.Summary.TotalClosingDebit - report.Summary.TotalClosingCredit);
            }

            return report;
        }

        private void RollupAmounts(List<TrialBalanceHierarchyItemDto> items)
        {
            foreach (var l4 in items.Where(x => x.Level == 4))
            {
                var children = items.Where(x => x.Level == 5 && x.ParentCode == l4.Code).ToList();
                l4.OpeningDebit = children.Sum(c => c.OpeningDebit);
                l4.OpeningCredit = children.Sum(c => c.OpeningCredit);
                l4.PeriodDebit = children.Sum(c => c.PeriodDebit);
                l4.PeriodCredit = children.Sum(c => c.PeriodCredit);
                l4.ClosingDebit = children.Sum(c => c.ClosingDebit);
                l4.ClosingCredit = children.Sum(c => c.ClosingCredit);
            }

            foreach (var l3 in items.Where(x => x.Level == 3))
            {
                var children = items.Where(x => x.Level == 4 && x.ParentCode == l3.Code).ToList();
                l3.OpeningDebit = children.Sum(c => c.OpeningDebit);
                l3.OpeningCredit = children.Sum(c => c.OpeningCredit);
                l3.PeriodDebit = children.Sum(c => c.PeriodDebit);
                l3.PeriodCredit = children.Sum(c => c.PeriodCredit);
                l3.ClosingDebit = children.Sum(c => c.ClosingDebit);
                l3.ClosingCredit = children.Sum(c => c.ClosingCredit);
            }

            foreach (var l2 in items.Where(x => x.Level == 2))
            {
                var children = items.Where(x => x.Level == 3 && x.ParentCode == l2.Code).ToList();
                l2.OpeningDebit = children.Sum(c => c.OpeningDebit);
                l2.OpeningCredit = children.Sum(c => c.OpeningCredit);
                l2.PeriodDebit = children.Sum(c => c.PeriodDebit);
                l2.PeriodCredit = children.Sum(c => c.PeriodCredit);
                l2.ClosingDebit = children.Sum(c => c.ClosingDebit);
                l2.ClosingCredit = children.Sum(c => c.ClosingCredit);
            }

            foreach (var l1 in items.Where(x => x.Level == 1))
            {
                var children = items.Where(x => x.Level == 2 && x.ParentCode == l1.Code).ToList();
                l1.OpeningDebit = children.Sum(c => c.OpeningDebit);
                l1.OpeningCredit = children.Sum(c => c.OpeningCredit);
                l1.PeriodDebit = children.Sum(c => c.PeriodDebit);
                l1.PeriodCredit = children.Sum(c => c.PeriodCredit);
                l1.ClosingDebit = children.Sum(c => c.ClosingDebit);
                l1.ClosingCredit = children.Sum(c => c.ClosingCredit);
            }
        }

        private List<TrialBalanceHierarchyItemDto> OrderHierarchyItems(List<TrialBalanceHierarchyItemDto> items)
        {
            var ordered = new List<TrialBalanceHierarchyItemDto>();

            var l1List = items.Where(x => x.Level == 1).OrderBy(x => x.Code).ToList();
            foreach (var l1 in l1List)
            {
                ordered.Add(l1);
                var l2List = items.Where(x => x.Level == 2 && x.ParentCode == l1.Code).OrderBy(x => x.Code).ToList();
                foreach (var l2 in l2List)
                {
                    ordered.Add(l2);
                    var l3List = items.Where(x => x.Level == 3 && x.ParentCode == l2.Code).OrderBy(x => x.Code).ToList();
                    foreach (var l3 in l3List)
                    {
                        ordered.Add(l3);
                        var l4List = items.Where(x => x.Level == 4 && x.ParentCode == l3.Code).OrderBy(x => x.Code).ToList();
                        foreach (var l4 in l4List)
                        {
                            ordered.Add(l4);
                            var l5List = items.Where(x => x.Level == 5 && x.ParentCode == l4.Code).OrderBy(x => x.Code).ToList();
                            ordered.AddRange(l5List);
                        }
                    }
                }
            }

            return ordered;
        }

        public async Task<List<CommonSelectVM>> GetCompaniesAsync()
        {
            try
            {
                return await _context.CoreCompanies
                    .OrderBy(c => c.CompanyName)
                    .Select(o => new CommonSelectVM
                    {
                        Id = 1,
                        Code = o.CompanyCode,
                        Name = o.CompanyName
                    }).ToListAsync();
            }
            catch
            {
                return new List<CommonSelectVM>
                {
                    new() { Id = 1, Code = "001", Name = "Formosa Logistics Limited" }
                };
            }
        }

        public async Task<List<CommonSelectVM>> GetBranchesAsync(string? companyCode = null)
        {
            try
            {
                var query = _context.CoreBranches.AsQueryable();
                if (!string.IsNullOrEmpty(companyCode) && companyCode != "0" && companyCode != "All")
                {
                    query = query.Where(b => b.CompanyCode == companyCode);
                }

                return await query
                    .OrderBy(b => b.BranchName)
                    .Select(b => new CommonSelectVM
                    {
                        Id = (int)b.AutoId,
                        Code = b.BranchCode,
                        Name = b.BranchName
                    }).ToListAsync();
            }
            catch
            {
                return new List<CommonSelectVM>();
            }
        }

        public async Task<byte[]> GeneratePdfAsync(TrialBalanceFilterVM filter)
        {
            var report = await GetTrialBalanceReportAsync(filter);
            report.Companies = await GetCompaniesAsync();
            report.Branches = await GetBranchesAsync(filter.Main_CompanyCode);

            if (!string.IsNullOrEmpty(filter.Main_CompanyCode))
            {
                var comp = report.Companies.FirstOrDefault(c => c.Code == filter.Main_CompanyCode);
                if (comp != null && !string.IsNullOrWhiteSpace(comp.Name))
                {
                    report.CompanyName = comp.Name;
                }
            }

            var branchParam = filter.BranchCode;
            if (!string.IsNullOrEmpty(branchParam) && branchParam != "All")
            {
                var br = report.Branches.FirstOrDefault(b => b.Code == branchParam);
                if (br != null && !string.IsNullOrWhiteSpace(br.Name))
                {
                    report.Filter.BranchCode = br.Name;
                }
            }

            var doc = new TrialBalancePdf(report);
            return doc.GeneratePdf();
        }
    }
}
