using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GCTL.Core.ViewModels.Dashboard;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Dashboard
{
    public static class DashboardOverviewPdfGenerator
    {
        public static byte[] Generate(DashboardOverviewDTO data, DashboardFilterDTO filter, string printUser = "", IWebHostEnvironment? env = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var logoBytes = GetLogoBytes(env);

            string dateRangeStr = (filter.FromDate.HasValue && filter.ToDate.HasValue)
                ? $"{filter.FromDate.Value:dd-MMM-yyyy} to {filter.ToDate.Value:dd-MMM-yyyy}"
                : "Active Period";

            var kpis = data?.KPIs ?? new DashboardOverviewKPIs();
            var monthlyTrends = data?.MonthlyTrends ?? new List<DashboardOverviewMonthlyTrend>();
            var topCustomers = data?.TopCustomers ?? new List<DashboardOverviewTopCustomer>();
            var ageing = data?.AgeingBrackets ?? new DashboardOverviewAgeingBrackets();
            var reqs = data?.RequisitionBreakdowns ?? new List<DashboardOverviewReqBreakdown>();
            var statusShares = data?.StatusShares ?? new List<DashboardOverviewStatusShare>();
            var overdueBills = data?.OverdueBills ?? new List<DashboardOverviewOverdueBill>();
            var upcoming = data?.UpcomingArrivals ?? new List<DashboardOverviewUpcomingArrival>();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(12);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(7.2f));

                    // Header
                    page.Header().Column(h =>
                    {
                        h.Item().Row(r =>
                        {
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                r.ConstantItem(85).AlignTop().Image(logoBytes).FitArea();
                            }
                            else
                            {
                                r.ConstantItem(85);
                            }

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Formosa Logistics Limited").FontSize(13.5f).Bold().FontColor("#0F172A");
                                c.Item().AlignCenter().Text("21, Hazaribagh, Dhaka-1209").FontSize(8f).FontColor(Colors.Grey.Darken2);
                                c.Item().AlignCenter().PaddingTop(1).Text("C&F Management System — Executive Overview Report").FontSize(10.5f).Bold().FontColor("#0B57D0");
                                c.Item().AlignCenter().PaddingTop(1).Text($"Period: {dateRangeStr}").FontSize(8f).SemiBold();
                            });

                            r.ConstantItem(85);
                        });

                        h.Item().PaddingBottom(4);
                    });

                    // Content
                    page.Content().Column(col =>
                    {
                        // 1. Executive KPI Summary Block
                        col.Item().Element(c => RenderSectionHeader(c, "1. EXECUTIVE KEY PERFORMANCE INDICATORS (KPIs)"));
                        col.Item().PaddingTop(2).Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            // Row 1
                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Total Jobs").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text(kpis.TotalJobs.ToString("N0")).FontSize(9.5f).Bold().FontColor("#0B57D0");
                                c.Item().Text($"P: {kpis.PendingJobs} · T: {kpis.TransitJobs} · D: {kpis.DeliveredJobs}").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Net Service Value").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.NetServiceValue:N2}").FontSize(9.5f).Bold().FontColor("#16A34A");
                                c.Item().Text($"{kpis.TotalBills:N0} bills issued").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Direct Cost").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.TotalDirectCost:N2}").FontSize(9.5f).Bold().FontColor("#EA580C");
                                c.Item().Text("operational expense").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Gross Profit").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.GrossProfit:N2}").FontSize(9.5f).Bold().FontColor("#7C3AED");
                                c.Item().Text($"GP Margin: {kpis.GPMargin:F2}%").FontSize(6f).Bold().FontColor("#7C3AED");
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Cash in Hand").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.CashInHand:N2}").FontSize(9.5f).Bold().FontColor("#0284C7");
                                c.Item().Text("liquid cash funds").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Bank Balance").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.BankBalance:N2}").FontSize(9.5f).Bold().FontColor("#0D9488");
                                c.Item().Text("liquid bank balance").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            // Row 2
                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Net Profit (Accounts)").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.NetProfitAccounts:N2}").FontSize(9f).Bold().FontColor("#16A34A");
                                decimal npMargin = kpis.NetServiceValue > 0 ? (kpis.NetProfitAccounts / kpis.NetServiceValue * 100) : 0;
                                c.Item().Text($"NP Margin: {npMargin:F2}%").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Total Due Outstanding").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.TotalDueOutstanding:N2}").FontSize(9f).Bold().FontColor("#DC2626");
                                decimal duePct = kpis.NetServiceValue > 0 ? (kpis.TotalDueOutstanding / kpis.NetServiceValue * 100) : 0;
                                c.Item().Text($"{duePct:F1}% of billed value").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Collection Rate").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"{kpis.CollectionRate:F1}%").FontSize(9f).Bold().FontColor("#2563EB");
                                c.Item().Text($"Coll: ৳ {kpis.CollectedAmount:N2}").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Avg Clearance Time").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"{kpis.AvgClearanceDays:F1} Days").FontSize(9f).Bold().FontColor("#4F46E5");
                                c.Item().Text($"On-Time (≤10d): {kpis.OnTimeDeliveryPct:F1}%").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Documents Pending").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text(kpis.DocsPendingCount.ToString("N0")).FontSize(9f).Bold().FontColor("#D97706");
                                c.Item().Text("incomplete job files").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });

                            tbl.Cell().Element(KpiCell).Column(c =>
                            {
                                c.Item().Text("Pending Requisitions").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"৳ {kpis.PendingReqAmount:N2}").FontSize(9f).Bold().FontColor("#B91C1C");
                                c.Item().Text($"{kpis.PendingReqCount} of {kpis.TotalReqCount} reqs").FontSize(6f).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        col.Item().PaddingTop(5);

                        // 2. Financial Trends (Left) & Top Customers / Status (Right)
                        col.Item().Row(r =>
                        {
                            // Left 56%: Monthly Financial Trends + Receivable Ageing
                            r.RelativeItem(5.6f).Column(leftCol =>
                            {
                                leftCol.Item().Element(c => RenderSectionHeader(c, "2. MONTHLY FINANCIAL PERFORMANCE (REVENUE VS COST VS PROFIT)"));
                                leftCol.Item().PaddingTop(2).Table(tbl =>
                                {
                                    tbl.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(2.0f); // Month
                                        cols.RelativeColumn(2.5f); // Revenue
                                        cols.RelativeColumn(2.5f); // Cost
                                        cols.RelativeColumn(2.5f); // Profit
                                        cols.RelativeColumn(1.8f); // Margin %
                                    });

                                    tbl.Header(h =>
                                    {
                                        h.Cell().Element(HCell).Text("Month");
                                        h.Cell().Element(HCell).Text("Net Service Value (BDT)");
                                        h.Cell().Element(HCell).Text("Direct Cost (BDT)");
                                        h.Cell().Element(HCell).Text("Gross Profit (BDT)");
                                        h.Cell().Element(HCell).Text("GP Margin %");
                                    });

                                    if (monthlyTrends.Count == 0)
                                    {
                                        tbl.Cell().ColumnSpan(5).Element(DCell).AlignCenter().Text("No monthly data available").Italic();
                                    }
                                    else
                                    {
                                        decimal totRev = 0, totCost = 0, totProfit = 0;
                                        foreach (var m in monthlyTrends)
                                        {
                                            totRev += m.Revenue;
                                            totCost += m.DirectCost;
                                            totProfit += m.Profit;

                                            tbl.Cell().Element(DCell).AlignLeft().Text(m.MonthLabel ?? m.MonthKey ?? "-");
                                            tbl.Cell().Element(DCell).AlignRight().Text(m.Revenue.ToString("N2", CultureInfo.InvariantCulture));
                                            tbl.Cell().Element(DCell).AlignRight().Text(m.DirectCost.ToString("N2", CultureInfo.InvariantCulture));
                                            tbl.Cell().Element(DCell).AlignRight().Text(m.Profit.ToString("N2", CultureInfo.InvariantCulture));
                                            tbl.Cell().Element(DCell).AlignRight().Text(m.GPMargin.ToString("F2") + "%");
                                        }

                                        // Total Row
                                        decimal avgMargin = totRev > 0 ? (totProfit / totRev * 100) : 0;
                                        tbl.Cell().Element(SubtotalCell).AlignLeft().Text("Total").Bold();
                                        tbl.Cell().Element(SubtotalCell).AlignRight().Text(totRev.ToString("N2", CultureInfo.InvariantCulture)).Bold();
                                        tbl.Cell().Element(SubtotalCell).AlignRight().Text(totCost.ToString("N2", CultureInfo.InvariantCulture)).Bold();
                                        tbl.Cell().Element(SubtotalCell).AlignRight().Text(totProfit.ToString("N2", CultureInfo.InvariantCulture)).Bold();
                                        tbl.Cell().Element(SubtotalCell).AlignRight().Text(avgMargin.ToString("F2") + "%").Bold();
                                    }
                                });

                                leftCol.Item().PaddingTop(4);

                                // Receivable Ageing
                                leftCol.Item().Element(c => RenderSectionHeader(c, "3. RECEIVABLE AGEING SUMMARY (DUE OUTSTANDING)"));
                                leftCol.Item().PaddingTop(2).Table(tbl =>
                                {
                                    tbl.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                    });

                                    tbl.Header(h =>
                                    {
                                        h.Cell().Element(HCell).Text("0–30 Days");
                                        h.Cell().Element(HCell).Text("31–60 Days");
                                        h.Cell().Element(HCell).Text("61–90 Days");
                                        h.Cell().Element(HCell).Text("90+ Days");
                                        h.Cell().Element(HCell).Text("Total Due (BDT)");
                                    });

                                    decimal totalDue = ageing.Bracket_0_30 + ageing.Bracket_31_60 + ageing.Bracket_61_90 + ageing.Bracket_90_Plus;
                                    tbl.Cell().Element(DCell).AlignRight().Text(ageing.Bracket_0_30.ToString("N2", CultureInfo.InvariantCulture));
                                    tbl.Cell().Element(DCell).AlignRight().Text(ageing.Bracket_31_60.ToString("N2", CultureInfo.InvariantCulture));
                                    tbl.Cell().Element(DCell).AlignRight().Text(ageing.Bracket_61_90.ToString("N2", CultureInfo.InvariantCulture));
                                    tbl.Cell().Element(DCell).AlignRight().Text(ageing.Bracket_90_Plus.ToString("N2", CultureInfo.InvariantCulture)).FontColor("#DC2626");
                                    tbl.Cell().Element(SubtotalCell).AlignRight().Text(totalDue.ToString("N2", CultureInfo.InvariantCulture)).Bold();
                                });
                            });

                            r.ConstantItem(8);

                            // Right 44%: Top Customers + Job Status Share
                            r.RelativeItem(4.4f).Column(rightCol =>
                            {
                                rightCol.Item().Element(c => RenderSectionHeader(c, "4. TOP CUSTOMERS BY SERVICE VALUE"));
                                rightCol.Item().PaddingTop(2).Table(tbl =>
                                {
                                    tbl.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(20);
                                        cols.RelativeColumn(3.5f);
                                        cols.RelativeColumn(2.0f);
                                        cols.RelativeColumn(1.2f);
                                    });

                                    tbl.Header(h =>
                                    {
                                        h.Cell().Element(HCell).Text("SL");
                                        h.Cell().Element(HCell).Text("Customer Name");
                                        h.Cell().Element(HCell).Text("Service Value (BDT)");
                                        h.Cell().Element(HCell).Text("Share %");
                                    });

                                    if (topCustomers.Count == 0)
                                    {
                                        tbl.Cell().ColumnSpan(4).Element(DCell).AlignCenter().Text("No customer data available").Italic();
                                    }
                                    else
                                    {
                                        decimal totCustVal = topCustomers.Sum(c => c.TotalBillAmount);
                                        int sl = 1;
                                        foreach (var c in topCustomers.Take(7))
                                        {
                                            decimal share = kpis.NetServiceValue > 0 ? (c.TotalBillAmount / kpis.NetServiceValue * 100) : 0;
                                            tbl.Cell().Element(DCell).AlignCenter().Text(sl++.ToString());
                                            tbl.Cell().Element(DCell).AlignLeft().Text(c.CustomerName ?? "Unknown");
                                            tbl.Cell().Element(DCell).AlignRight().Text(c.TotalBillAmount.ToString("N2", CultureInfo.InvariantCulture));
                                            tbl.Cell().Element(DCell).AlignRight().Text(share.ToString("F1") + "%");
                                        }

                                        decimal totalShare = kpis.NetServiceValue > 0 ? (totCustVal / kpis.NetServiceValue * 100) : 0;
                                        tbl.Cell().ColumnSpan(2).Element(SubtotalCell).AlignLeft().Text("Top Customers Total").Bold();
                                        tbl.Cell().Element(SubtotalCell).AlignRight().Text(totCustVal.ToString("N2", CultureInfo.InvariantCulture)).Bold();
                                        tbl.Cell().Element(SubtotalCell).AlignRight().Text(totalShare.ToString("F1") + "%").Bold();
                                    }
                                });

                                rightCol.Item().PaddingTop(4);

                                // Job Status & Requisitions
                                rightCol.Item().Element(c => RenderSectionHeader(c, "5. JOB WORKLOAD & REQUISITIONS BREAKDOWN"));
                                rightCol.Item().PaddingTop(2).Table(tbl =>
                                {
                                    tbl.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(1.5f);
                                        cols.RelativeColumn(1.2f);
                                        cols.RelativeColumn(1.2f);
                                        cols.RelativeColumn(1.5f);
                                        cols.RelativeColumn(1.8f);
                                    });

                                    tbl.Header(h =>
                                    {
                                        h.Cell().Element(HCell).Text("Job Status");
                                        h.Cell().Element(HCell).Text("Count");
                                        h.Cell().Element(HCell).Text("Share %");
                                        h.Cell().Element(HCell).Text("Requisition");
                                        h.Cell().Element(HCell).Text("Amount (BDT)");
                                    });

                                    int totJobs = kpis.TotalJobs > 0 ? kpis.TotalJobs : 1;
                                    var appReq = reqs.FirstOrDefault(r => r.StatusName?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true);
                                    var pendReq = reqs.FirstOrDefault(r => r.StatusName?.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true);

                                    // Row 1: Pending
                                    tbl.Cell().Element(DCell).AlignLeft().Text("Pending").FontColor("#EA580C");
                                    tbl.Cell().Element(DCell).AlignCenter().Text(kpis.PendingJobs.ToString());
                                    tbl.Cell().Element(DCell).AlignRight().Text((kpis.PendingJobs * 100.0 / totJobs).ToString("F1") + "%");
                                    tbl.Cell().Element(DCell).AlignLeft().Text("Approved Req");
                                    tbl.Cell().Element(DCell).AlignRight().Text((appReq?.TotalAmount ?? 0).ToString("N2", CultureInfo.InvariantCulture));

                                    // Row 2: In Transit
                                    tbl.Cell().Element(DCell).AlignLeft().Text("In Transit").FontColor("#7C3AED");
                                    tbl.Cell().Element(DCell).AlignCenter().Text(kpis.TransitJobs.ToString());
                                    tbl.Cell().Element(DCell).AlignRight().Text((kpis.TransitJobs * 100.0 / totJobs).ToString("F1") + "%");
                                    tbl.Cell().Element(DCell).AlignLeft().Text("Pending Req");
                                    tbl.Cell().Element(DCell).AlignRight().Text((pendReq?.TotalAmount ?? kpis.PendingReqAmount).ToString("N2", CultureInfo.InvariantCulture));

                                    // Row 3: Delivered
                                    tbl.Cell().Element(DCell).AlignLeft().Text("Delivered").FontColor("#16A34A");
                                    tbl.Cell().Element(DCell).AlignCenter().Text(kpis.DeliveredJobs.ToString());
                                    tbl.Cell().Element(DCell).AlignRight().Text((kpis.DeliveredJobs * 100.0 / totJobs).ToString("F1") + "%");
                                    tbl.Cell().Element(SubtotalCell).AlignLeft().Text("Total Req").Bold();
                                    decimal totalReqAmt = (appReq?.TotalAmount ?? 0) + (pendReq?.TotalAmount ?? kpis.PendingReqAmount);
                                    tbl.Cell().Element(SubtotalCell).AlignRight().Text(totalReqAmt.ToString("N2", CultureInfo.InvariantCulture)).Bold();
                                });
                            });
                        });

                        col.Item().PaddingTop(5);

                        // 3. Needs Attention - Overdue Bills (Due 30+ Days)
                        col.Item().Element(c => RenderSectionHeader(c, "6. NEEDS ATTENTION – OVERDUE BILLS (DUE 30+ DAYS)"));
                        col.Item().PaddingTop(2).Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(22); // SL
                                cols.RelativeColumn(2.0f); // Bill No
                                cols.RelativeColumn(1.4f); // Date
                                cols.RelativeColumn(3.5f); // Customer
                                cols.RelativeColumn(1.2f); // Days Overdue
                                cols.RelativeColumn(2.0f); // Due Amount
                            });

                            tbl.Header(h =>
                            {
                                h.Cell().Element(HCell).Text("SL");
                                h.Cell().Element(HCell).Text("Bill No");
                                h.Cell().Element(HCell).Text("Bill Date");
                                h.Cell().Element(HCell).Text("Customer Name");
                                h.Cell().Element(HCell).Text("Age (Days)");
                                h.Cell().Element(HCell).Text("Due Amount (BDT)");
                            });

                            if (overdueBills.Count == 0)
                            {
                                tbl.Cell().ColumnSpan(6).Element(DCell).AlignCenter().Text("No overdue bills found.").Italic();
                            }
                            else
                            {
                                int sl = 1;
                                decimal sumOverdue = 0;
                                foreach (var ob in overdueBills.Take(15))
                                {
                                    sumOverdue += ob.DueAmount;
                                    tbl.Cell().Element(DCell).AlignCenter().Text(sl++.ToString());
                                    tbl.Cell().Element(DCell).AlignLeft().Text(ob.BillNo ?? "");
                                    tbl.Cell().Element(DCell).AlignCenter().Text(ob.BillDate ?? "");
                                    tbl.Cell().Element(DCell).AlignLeft().Text(ob.CustomerName ?? "");
                                    tbl.Cell().Element(DCell).AlignCenter().Text(ob.DaysOverdue.ToString());
                                    tbl.Cell().Element(DCell).AlignRight().Text(ob.DueAmount.ToString("N2", CultureInfo.InvariantCulture));
                                }

                                tbl.Cell().ColumnSpan(5).Element(SubtotalCell).AlignRight().Text("Overdue Bills Subtotal:").Bold();
                                tbl.Cell().Element(SubtotalCell).AlignRight().Text(sumOverdue.ToString("N2", CultureInfo.InvariantCulture)).Bold();
                            }
                        });

                        col.Item().PaddingTop(5);

                        // 4. Upcoming Arrivals (In Transit)
                        col.Item().Element(c => RenderSectionHeader(c, "7. UPCOMING ARRIVALS (IN TRANSIT SHIPMENTS)"));
                        col.Item().PaddingTop(2).Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(22); // SL
                                cols.RelativeColumn(2.2f); // Job No
                                cols.RelativeColumn(1.4f); // ETA
                                cols.RelativeColumn(3.2f); // Customer
                                cols.RelativeColumn(1.5f); // Mode
                                cols.RelativeColumn(1.8f); // Port
                                cols.RelativeColumn(1.2f); // Status
                            });

                            tbl.Header(h =>
                            {
                                h.Cell().Element(HCell).Text("SL");
                                h.Cell().Element(HCell).Text("Job No");
                                h.Cell().Element(HCell).Text("ETA");
                                h.Cell().Element(HCell).Text("Customer Name");
                                h.Cell().Element(HCell).Text("Mode");
                                h.Cell().Element(HCell).Text("Port");
                                h.Cell().Element(HCell).Text("Status");
                            });

                            if (upcoming.Count == 0)
                            {
                                tbl.Cell().ColumnSpan(7).Element(DCell).AlignCenter().Text("No in-transit shipments found.").Italic();
                            }
                            else
                            {
                                int sl = 1;
                                foreach (var ua in upcoming.Take(15))
                                {
                                    tbl.Cell().Element(DCell).AlignCenter().Text(sl++.ToString());
                                    tbl.Cell().Element(DCell).AlignLeft().Text(ua.JobNo ?? "");
                                    tbl.Cell().Element(DCell).AlignCenter().Text(ua.ETA ?? "");
                                    tbl.Cell().Element(DCell).AlignLeft().Text(ua.CustomerName ?? "");
                                    tbl.Cell().Element(DCell).AlignLeft().Text(ua.ShipmentMode ?? "");
                                    tbl.Cell().Element(DCell).AlignLeft().Text(ua.PortName ?? "");
                                    tbl.Cell().Element(DCell).AlignCenter().Text(ua.Status ?? "In Transit");
                                }
                            }
                        });
                    });

                    // Footer
                    page.Footer().Column(fc =>
                    {
                        fc.Item().PaddingBottom(2).LineHorizontal(0.4f).LineColor(Colors.Grey.Lighten2);
                        fc.Item().Row(r =>
                        {
                            // Left: Print Date, Time, User
                            r.RelativeItem().AlignLeft().AlignMiddle().Text(
                                $"Print Date: {DateTime.Now:dd-MMM-yyyy}   Time: {DateTime.Now:hh:mm tt}   User: {(string.IsNullOrWhiteSpace(printUser) ? "admin" : printUser)}"
                            ).FontSize(6.8f).FontColor(Colors.Grey.Darken2);

                            // Center: GCTL Infosys
                            r.RelativeItem().AlignCenter().AlignMiddle().Text("GCTL Infosys").FontSize(7.2f).Bold().FontColor(Colors.Grey.Darken2);

                            // Right: Page number
                            r.RelativeItem().AlignRight().AlignMiddle().Text(x =>
                            {
                                x.Span("Page ").FontSize(6.8f).FontColor(Colors.Grey.Darken2);
                                x.CurrentPageNumber().FontSize(6.8f).FontColor(Colors.Grey.Darken2);
                                x.Span(" of ").FontSize(6.8f).FontColor(Colors.Grey.Darken2);
                                x.TotalPages().FontSize(6.8f).FontColor(Colors.Grey.Darken2);
                            });
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void RenderSectionHeader(IContainer container, string title)
        {
            container.ShowEntire()
                     .Background("#F1F5F9")
                     .Border(0.4f).BorderColor("#CBD5E1")
                     .PaddingVertical(2).PaddingHorizontal(4)
                     .Text(title).FontSize(7.2f).Bold().FontColor("#0F172A");
        }

        private static IContainer KpiCell(IContainer container) =>
            container.ShowEntire()
                     .Border(0.4f).BorderColor(Colors.Grey.Lighten2)
                     .Background(Colors.White)
                     .Padding(3.5f);

        private static IContainer HCell(IContainer container) =>
            container.ShowEntire()
                     .Border(0.4f).BorderColor(Colors.Grey.Lighten2)
                     .Background(Colors.Grey.Lighten4)
                     .PaddingVertical(2.5f).PaddingHorizontal(2)
                     .AlignCenter().AlignMiddle()
                     .DefaultTextStyle(x => x.FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken3));

        private static IContainer DCell(IContainer container) =>
            container.ShowEntire()
                     .Border(0.3f).BorderColor(Colors.Grey.Lighten3)
                     .PaddingVertical(2f).PaddingHorizontal(2)
                     .AlignMiddle()
                     .DefaultTextStyle(x => x.FontSize(6.3f).FontColor(Colors.Black));

        private static IContainer SubtotalCell(IContainer container) =>
            container.ShowEntire()
                     .Border(0.4f).BorderColor(Colors.Grey.Lighten2)
                     .Background(Colors.Grey.Lighten3)
                     .PaddingVertical(2f).PaddingHorizontal(2)
                     .AlignMiddle()
                     .DefaultTextStyle(x => x.FontSize(6.5f).Bold().FontColor(Colors.Black));

        private static byte[]? GetLogoBytes(IWebHostEnvironment? env = null)
        {
            try
            {
                var candidates = new List<string?>
                {
                    env?.WebRootPath != null ? Path.Combine(env.WebRootPath, "images", "fermosa-wX200.png") : null,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "UIs", "GCTL.UI.Core", "wwwroot", "images", "fermosa-wX200.png"),
                    @"D:\Project\School\GCTL.Framework\UIs\GCTL.UI.Core\wwwroot\images\fermosa-wX200.png"
                };

                foreach (var path in candidates)
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        return File.ReadAllBytes(path);
                    }
                }
            }
            catch
            {
                // Fallback gracefully without throwing
            }
            return null;
        }
    }
}
