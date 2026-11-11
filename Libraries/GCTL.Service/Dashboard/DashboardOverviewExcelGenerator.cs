using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using GCTL.Core.ViewModels.Dashboard;
using Microsoft.AspNetCore.Hosting;

namespace GCTL.Service.Dashboard
{
    public static class DashboardOverviewExcelGenerator
    {
        public static byte[] Generate(DashboardOverviewDTO data, DashboardFilterDTO filter, string printUser = "", IWebHostEnvironment? env = null)
        {
            var kpis = data?.KPIs ?? new DashboardOverviewKPIs();
            var monthlyTrends = data?.MonthlyTrends ?? new List<DashboardOverviewMonthlyTrend>();
            var topCustomers = data?.TopCustomers ?? new List<DashboardOverviewTopCustomer>();
            var ageing = data?.AgeingBrackets ?? new DashboardOverviewAgeingBrackets();
            var overdueBills = data?.OverdueBills ?? new List<DashboardOverviewOverdueBill>();
            var upcoming = data?.UpcomingArrivals ?? new List<DashboardOverviewUpcomingArrival>();

            string dateRangeStr = (filter.FromDate.HasValue && filter.ToDate.HasValue)
                ? $"{filter.FromDate.Value:dd-MMM-yyyy} to {filter.ToDate.Value:dd-MMM-yyyy}"
                : "Active Period";

            using var workbook = new XLWorkbook();

            // -------------------------------------------------------------
            // SHEET 1: Executive Summary
            // -------------------------------------------------------------
            var ws1 = workbook.Worksheets.Add("Executive Summary");
            ws1.ShowGridLines = true;

            int r = AddStandardHeader(ws1, 6, dateRangeStr, printUser);

            // SECTION 1: Key Performance Indicators
            ws1.Cell(r, 1).Value = "1. EXECUTIVE KPI SUMMARY";
            ws1.Range(r, 1, r, 6).Merge().Style.Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B57D0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            r++;

            var kpiList = new List<(string Name, object Value, string Format, string Sub)>
            {
                ("Total Jobs Handled", kpis.TotalJobs, "#,##0", $"Pending: {kpis.PendingJobs}, In Transit: {kpis.TransitJobs}, Delivered: {kpis.DeliveredJobs}"),
                ("Net Service Value", kpis.NetServiceValue, "#,##0.00", $"{kpis.TotalBills} bills issued"),
                ("Direct Cost (Operational)", kpis.TotalDirectCost, "#,##0.00", "Direct operational expenses"),
                ("Gross Profit", kpis.GrossProfit, "#,##0.00", $"GP Margin: {kpis.GPMargin:F2}%"),
                ("Net Profit (Accounts)", kpis.NetProfitAccounts, "#,##0.00", "Live accounts income minus expenses"),
                ("Total Due Outstanding", kpis.TotalDueOutstanding, "#,##0.00", "Customer receivables"),
                ("Collected Amount", kpis.CollectedAmount, "#,##0.00", $"Collection Rate: {kpis.CollectionRate:F1}%"),
                ("Cash in Hand", kpis.CashInHand, "#,##0.00", "Liquid cash balances"),
                ("Bank Balance", kpis.BankBalance, "#,##0.00", "Liquid bank balances"),
                ("Avg Clearance Time", kpis.AvgClearanceDays, "0.0", $"On-Time Delivery (≤10d): {kpis.OnTimeDeliveryPct:F1}%"),
                ("Documents Pending", kpis.DocsPendingCount, "#,##0", "Jobs with incomplete documents"),
                ("Pending Fund Requisitions", kpis.PendingReqAmount, "#,##0.00", $"{kpis.PendingReqCount} of {kpis.TotalReqCount} requests")
            };

            ws1.Cell(r, 1).Value = "Metric";
            ws1.Range(r, 1, r, 2).Merge();
            ws1.Cell(r, 3).Value = "Value (BDT / Unit)";
            ws1.Cell(r, 4).Value = "Details / Context";
            ws1.Range(r, 4, r, 6).Merge();
            ws1.Range(r, 1, r, 6).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9")).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetOutsideBorderColor(XLColor.FromHtml("#CBD5E1"));
            r++;

            foreach (var kpi in kpiList)
            {
                ws1.Cell(r, 1).Value = kpi.Name;
                ws1.Range(r, 1, r, 2).Merge();
                var valCell = ws1.Cell(r, 3);
                if (kpi.Value is decimal decVal)
                {
                    valCell.Value = decVal;
                    valCell.Style.NumberFormat.Format = kpi.Format;
                }
                else if (kpi.Value is int intVal)
                {
                    valCell.Value = intVal;
                    valCell.Style.NumberFormat.Format = kpi.Format;
                }
                else
                {
                    valCell.Value = kpi.Value?.ToString() ?? "";
                }
                valCell.Style.Font.SetBold(true);

                ws1.Cell(r, 4).Value = kpi.Sub;
                ws1.Range(r, 4, r, 6).Merge().Style.Font.SetFontColor(XLColor.FromHtml("#64748B"));

                ws1.Range(r, 1, r, 6).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.FromHtml("#E2E8F0"));
                r++;
            }

            r += 2;

            // SECTION 2: Monthly Financial Trends
            ws1.Cell(r, 1).Value = "2. MONTHLY FINANCIAL TRENDS (REVENUE VS COST VS PROFIT)";
            ws1.Range(r, 1, r, 5).Merge().Style.Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B57D0"));
            r++;

            ws1.Cell(r, 1).Value = "Month";
            ws1.Cell(r, 2).Value = "Net Service Value (BDT)";
            ws1.Cell(r, 3).Value = "Direct Cost (BDT)";
            ws1.Cell(r, 4).Value = "Gross Profit (BDT)";
            ws1.Cell(r, 5).Value = "GP Margin %";
            ws1.Range(r, 1, r, 5).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            r++;

            int monthStartRow = r;
            if (monthlyTrends.Count == 0)
            {
                ws1.Cell(r, 1).Value = "No data available";
                ws1.Range(r, 1, r, 5).Merge().Style.Font.SetItalic(true);
                r++;
            }
            else
            {
                foreach (var m in monthlyTrends)
                {
                    ws1.Cell(r, 1).Value = m.MonthLabel ?? m.MonthKey ?? "-";
                    ws1.Cell(r, 2).Value = m.Revenue;
                    ws1.Cell(r, 2).Style.NumberFormat.Format = "#,##0.00";
                    ws1.Cell(r, 3).Value = m.DirectCost;
                    ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws1.Cell(r, 4).Value = m.Profit;
                    ws1.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                    ws1.Cell(r, 5).Value = m.GPMargin / 100m;
                    ws1.Cell(r, 5).Style.NumberFormat.Format = "0.00%";
                    ws1.Range(r, 1, r, 5).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.FromHtml("#E2E8F0"));
                    r++;
                }

                // Total Row
                int monthEndRow = r - 1;
                ws1.Cell(r, 1).Value = "Total";
                ws1.Cell(r, 2).FormulaA1 = $"=SUM(B{monthStartRow}:B{monthEndRow})";
                ws1.Cell(r, 2).Style.NumberFormat.Format = "#,##0.00";
                ws1.Cell(r, 3).FormulaA1 = $"=SUM(C{monthStartRow}:C{monthEndRow})";
                ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                ws1.Cell(r, 4).FormulaA1 = $"=SUM(D{monthStartRow}:D{monthEndRow})";
                ws1.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                ws1.Cell(r, 5).FormulaA1 = $"=IF(B{r}>0, D{r}/B{r}, 0)";
                ws1.Cell(r, 5).Style.NumberFormat.Format = "0.00%";
                ws1.Range(r, 1, r, 5).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                r++;
            }

            r += 2;

            // SECTION 3: Top Customers by Service Value
            ws1.Cell(r, 1).Value = "3. TOP CUSTOMERS BY SERVICE VALUE";
            ws1.Range(r, 1, r, 4).Merge().Style.Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B57D0"));
            r++;

            ws1.Cell(r, 1).Value = "Rank";
            ws1.Cell(r, 2).Value = "Customer Name";
            ws1.Cell(r, 3).Value = "Service Value (BDT)";
            ws1.Cell(r, 4).Value = "Share of Revenue %";
            ws1.Range(r, 1, r, 4).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            r++;

            int custStartRow = r;
            if (topCustomers.Count == 0)
            {
                ws1.Cell(r, 1).Value = "No customer data available";
                ws1.Range(r, 1, r, 4).Merge().Style.Font.SetItalic(true);
                r++;
            }
            else
            {
                int sl = 1;
                foreach (var c in topCustomers.Take(10))
                {
                    ws1.Cell(r, 1).Value = sl++;
                    ws1.Cell(r, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws1.Cell(r, 2).Value = c.CustomerName ?? "Unknown";
                    ws1.Cell(r, 3).Value = c.TotalBillAmount;
                    ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                    decimal share = kpis.NetServiceValue > 0 ? (c.TotalBillAmount / kpis.NetServiceValue) : 0;
                    ws1.Cell(r, 4).Value = share;
                    ws1.Cell(r, 4).Style.NumberFormat.Format = "0.00%";
                    ws1.Range(r, 1, r, 4).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.FromHtml("#E2E8F0"));
                    r++;
                }

                int custEndRow = r - 1;
                ws1.Cell(r, 1).Value = "Total (Top Customers)";
                ws1.Range(r, 1, r, 2).Merge();
                ws1.Cell(r, 3).FormulaA1 = $"=SUM(C{custStartRow}:C{custEndRow})";
                ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                ws1.Cell(r, 4).FormulaA1 = $"=SUM(D{custStartRow}:D{custEndRow})";
                ws1.Cell(r, 4).Style.NumberFormat.Format = "0.00%";
                ws1.Range(r, 1, r, 4).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                r++;
            }

            r += 2;

            // SECTION 4: Receivable Ageing Summary
            ws1.Cell(r, 1).Value = "4. RECEIVABLE AGEING SUMMARY (DUE OUTSTANDING)";
            ws1.Range(r, 1, r, 5).Merge().Style.Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B57D0"));
            r++;

            ws1.Cell(r, 1).Value = "0–30 Days";
            ws1.Cell(r, 2).Value = "31–60 Days";
            ws1.Cell(r, 3).Value = "61–90 Days";
            ws1.Cell(r, 4).Value = "90+ Days";
            ws1.Cell(r, 5).Value = "Total Due (BDT)";
            ws1.Range(r, 1, r, 5).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            r++;

            ws1.Cell(r, 1).Value = ageing.Bracket_0_30;
            ws1.Cell(r, 1).Style.NumberFormat.Format = "#,##0.00";
            ws1.Cell(r, 2).Value = ageing.Bracket_31_60;
            ws1.Cell(r, 2).Style.NumberFormat.Format = "#,##0.00";
            ws1.Cell(r, 3).Value = ageing.Bracket_61_90;
            ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
            ws1.Cell(r, 4).Value = ageing.Bracket_90_Plus;
            ws1.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
            ws1.Cell(r, 4).Style.Font.SetFontColor(XLColor.FromHtml("#DC2626"));

            decimal totAge = ageing.Bracket_0_30 + ageing.Bracket_31_60 + ageing.Bracket_61_90 + ageing.Bracket_90_Plus;
            ws1.Cell(r, 5).Value = totAge;
            ws1.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
            ws1.Cell(r, 5).Style.Font.SetBold(true);
            ws1.Range(r, 1, r, 5).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);

            ws1.Columns().AdjustToContents();

            // -------------------------------------------------------------
            // SHEET 2: Overdue Bills
            // -------------------------------------------------------------
            var ws2 = workbook.Worksheets.Add("Overdue Bills");
            ws2.ShowGridLines = true;

            int r2 = AddStandardHeader(ws2, 6, dateRangeStr, printUser);

            ws2.Cell(r2, 1).Value = "NEEDS ATTENTION – OVERDUE BILLS (DUE 30+ DAYS)";
            ws2.Range(r2, 1, r2, 6).Merge().Style.Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B57D0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            r2++;

            ws2.Cell(r2, 1).Value = "SL";
            ws2.Cell(r2, 2).Value = "Bill No";
            ws2.Cell(r2, 3).Value = "Bill Date";
            ws2.Cell(r2, 4).Value = "Customer Name";
            ws2.Cell(r2, 5).Value = "Days Overdue";
            ws2.Cell(r2, 6).Value = "Due Amount (BDT)";
            ws2.Range(r2, 1, r2, 6).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            r2++;

            int obStartRow = r2;
            if (overdueBills.Count == 0)
            {
                ws2.Cell(r2, 1).Value = "No overdue bills found";
                ws2.Range(r2, 1, r2, 6).Merge().Style.Font.SetItalic(true);
                r2++;
            }
            else
            {
                int sl = 1;
                foreach (var ob in overdueBills)
                {
                    ws2.Cell(r2, 1).Value = sl++;
                    ws2.Cell(r2, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws2.Cell(r2, 2).Value = ob.BillNo ?? "";
                    ws2.Cell(r2, 3).Value = ob.BillDate ?? "";
                    ws2.Cell(r2, 4).Value = ob.CustomerName ?? "";
                    ws2.Cell(r2, 5).Value = ob.DaysOverdue;
                    ws2.Cell(r2, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws2.Cell(r2, 6).Value = ob.DueAmount;
                    ws2.Cell(r2, 6).Style.NumberFormat.Format = "#,##0.00";
                    ws2.Range(r2, 1, r2, 6).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.FromHtml("#E2E8F0"));
                    r2++;
                }

                int obEndRow = r2 - 1;
                ws2.Cell(r2, 1).Value = "Total Due Outstanding";
                ws2.Range(r2, 1, r2, 5).Merge();
                ws2.Cell(r2, 6).FormulaA1 = $"=SUM(F{obStartRow}:F{obEndRow})";
                ws2.Cell(r2, 6).Style.NumberFormat.Format = "#,##0.00";
                ws2.Range(r2, 1, r2, 6).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            ws2.Columns().AdjustToContents();

            // -------------------------------------------------------------
            // SHEET 3: Upcoming Arrivals
            // -------------------------------------------------------------
            var ws3 = workbook.Worksheets.Add("Upcoming Arrivals");
            ws3.ShowGridLines = true;

            int r3 = AddStandardHeader(ws3, 7, dateRangeStr, printUser);

            ws3.Cell(r3, 1).Value = "UPCOMING ARRIVALS (IN TRANSIT SHIPMENTS)";
            ws3.Range(r3, 1, r3, 7).Merge().Style.Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B57D0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            r3++;

            ws3.Cell(r3, 1).Value = "SL";
            ws3.Cell(r3, 2).Value = "Job No";
            ws3.Cell(r3, 3).Value = "ETA";
            ws3.Cell(r3, 4).Value = "Customer Name";
            ws3.Cell(r3, 5).Value = "Shipment Mode";
            ws3.Cell(r3, 6).Value = "Port Name";
            ws3.Cell(r3, 7).Value = "Status";
            ws3.Range(r3, 1, r3, 7).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            r3++;

            if (upcoming.Count == 0)
            {
                ws3.Cell(r3, 1).Value = "No in-transit shipments found";
                ws3.Range(r3, 1, r3, 7).Merge().Style.Font.SetItalic(true);
            }
            else
            {
                int sl = 1;
                foreach (var ua in upcoming)
                {
                    ws3.Cell(r3, 1).Value = sl++;
                    ws3.Cell(r3, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws3.Cell(r3, 2).Value = ua.JobNo ?? "";
                    ws3.Cell(r3, 3).Value = ua.ETA ?? "";
                    ws3.Cell(r3, 4).Value = ua.CustomerName ?? "";
                    ws3.Cell(r3, 5).Value = ua.ShipmentMode ?? "";
                    ws3.Cell(r3, 6).Value = ua.PortName ?? "";
                    ws3.Cell(r3, 7).Value = ua.Status ?? "In Transit";
                    ws3.Range(r3, 1, r3, 7).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.FromHtml("#E2E8F0"));
                    r3++;
                }
            }

            ws3.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static int AddStandardHeader(IXLWorksheet ws, int colCount, string dateRangeStr, string printUser)
        {
            ws.Cell(1, 1).Value = "Formosa Logistics Limited";
            ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold(true).Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#0F172A")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "21, Hazaribagh, Dhaka-1209";
            ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#64748B")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = "C&F Management System — Executive Overview Report";
            ws.Range(3, 1, 3, colCount).Merge().Style.Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#0B57D0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            string userStr = string.IsNullOrWhiteSpace(printUser) ? "admin" : printUser;
            ws.Cell(4, 1).Value = $"Period: {dateRangeStr}  |  Printed: {DateTime.Now:dd-MMM-yyyy hh:mm tt}  |  User: {userStr}";
            ws.Range(4, 1, 4, colCount).Merge().Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#334155")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            return 6;
        }
    }
}
