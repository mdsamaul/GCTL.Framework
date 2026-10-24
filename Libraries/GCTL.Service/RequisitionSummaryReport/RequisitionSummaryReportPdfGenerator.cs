using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GCTL.Core.ViewModels.RequisitionSummaryReport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.RequisitionSummaryReport
{
    public static class RequisitionSummaryReportPdfGenerator
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F2(decimal v) => v.ToString("#,##0.00", Inv);

        public static byte[] GeneratePdf(RequisitionSummaryPdfModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(24);
                    page.MarginBottom(20);
                    page.MarginLeft(24);
                    page.MarginRight(24);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Black));

                    // Header on every page
                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(model.CompanyName ?? "Formosa Logistics Limited")
                                    .Bold().FontSize(14).FontFamily("Arial");
                                c.Item().Text(model.CompanyAddress ?? "9th Floor, TCB Bhaban, 1 Kawran Bazar\nRd, Dhaka 1215")
                                    .FontSize(10).FontFamily("Arial");
                            });
                        });

                        col.Item().PaddingTop(4).PaddingBottom(6).AlignCenter()
                            .Text(model.ReportTitle ?? "Requisition Summary Report")
                            .Bold().FontSize(12f).FontFamily("Arial");
                    });

                    // Body Content
                    page.Content().Column(col =>
                    {
                        if (model.CustomerGroups == null || !model.CustomerGroups.Any())
                        {
                            col.Item().PaddingTop(30).AlignCenter()
                                .Text("No requisition records found matching the selected criteria.")
                                .FontSize(11).Bold();
                            return;
                        }

                        foreach (var group in model.CustomerGroups)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);   // SL No.
                                    columns.RelativeColumn(3.2f); // Job No
                                    columns.RelativeColumn(1.7f); // Job Date
                                    columns.RelativeColumn(2.6f); // Shipment Mode
                                    columns.RelativeColumn(1.8f); // Receiptable
                                    columns.RelativeColumn(1.8f); // Non Receiptable
                                    columns.RelativeColumn(1.9f); // Total
                                });

                                // Header row (Repeats on every page this customer table spans)
                                table.Header(header =>
                                {
                                    header.Cell().ColumnSpan(7).PaddingTop(4).PaddingBottom(2)
                                        .Text(group.CustomerName ?? "-")
                                        .Bold().FontSize(8f);

                                    header.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().AlignMiddle().Text("SN.")
                                    .SemiBold().FontSize(8f);
                                    header.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().AlignMiddle().Text("Job No").SemiBold().FontSize(8f);
                                    header.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().AlignMiddle().Text("Job Date").SemiBold().FontSize(8f);
                                    header.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().AlignMiddle().Text("Shipment Mode").SemiBold().FontSize(8f);
                                    header.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().AlignMiddle().Text("Receiptable").SemiBold().FontSize(8f);
                                    header.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().Text("Non Receiptable").SemiBold().FontSize(8f);
                                    header.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().AlignMiddle().Text("Total").SemiBold().FontSize(8f);
                                });

                                // Data rows
                                foreach (var item in group.Items)
                                {
                                    table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().Text(item.SlNo.ToString()).FontSize(8f);
                                    table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignLeft().Text(item.JobNo ?? "").FontSize(8f);
                                    table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().Text(item.JobDate ?? "").FontSize(8f);
                                    table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignCenter().Text(item.ShipmentMode ?? "").FontSize(8f);
                                    table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignRight().Text(F2(item.Receiptable)).FontSize(8f);
                                    table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignRight().Text(F2(item.NonReceiptable)).FontSize(8f);
                                    table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignRight().Text(F2(item.Total)).FontSize(8f);
                                }

                                // Customer Subtotal Row
                                table.Cell().ColumnSpan(4).Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignRight().Text("Total:").SemiBold().FontSize(8f);
                                table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignRight().Text(F2(group.SubtotalReceiptable)).SemiBold().FontSize(8f);
                                table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignRight().Text(F2(group.SubtotalNonReceiptable)).SemiBold().FontSize(8f);
                                table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(2).AlignRight().Text(F2(group.SubtotalTotal)).SemiBold().FontSize(8f);
                            });
                        }

                        // Overall Grand Total Row at bottom
                        col.Item().PaddingTop(1).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);   // Count
                                columns.RelativeColumn(7.5f); // "Grand Total:"
                                columns.RelativeColumn(1.8f); // Receiptable
                                columns.RelativeColumn(1.8f); // Non Receiptable
                                columns.RelativeColumn(1.9f); // Total
                            });

                            table.Cell().Border(0f).Padding(2).AlignCenter().Text(model.TotalJobsCount.ToString()).SemiBold().FontSize(8f);
                            table.Cell().Border(0f).Padding(2).AlignRight().Text("Grand Total:").SemiBold().FontSize(8f);
                            table.Cell().Border(0f).Padding(2).AlignRight().Text(F2(model.GrandTotalReceiptable)).SemiBold().FontSize(8f);
                            table.Cell().Border(0f).Padding(2).AlignRight().Text(F2(model.GrandTotalNonReceiptable)).SemiBold().FontSize(8f);
                            table.Cell().Border(0f).Padding(2).AlignRight().Text(F2(model.GrandTotalCost)).SemiBold().FontSize(8f);
                        });
                    });

                    // Footer on every page
                    page.Footer().Column(fCol =>
                    {
                        fCol.Item().Row(r =>
                        {
                            r.RelativeItem().PaddingHorizontal(16).Column(c =>
                            {
                                c.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                                c.Item().PaddingTop(2).AlignCenter().Text("Prepared By").FontSize(7.5f);
                            });
                            r.RelativeItem().PaddingHorizontal(16).Column(c =>
                            {
                                c.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                                c.Item().PaddingTop(2).AlignCenter().Text("Checked By").FontSize(7.5f);
                            });
                            r.RelativeItem().PaddingHorizontal(16).Column(c =>
                            {
                                c.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                                c.Item().PaddingTop(2).AlignCenter().Text("Certified By").FontSize(7.5f);
                            });
                            r.RelativeItem().PaddingHorizontal(16).Column(c =>
                            {
                                c.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                                c.Item().PaddingTop(2).AlignCenter().Text("Approve By").FontSize(7.5f);
                            });
                        });

                        fCol.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem().Text($"Print Date: {model.PrintDate}").FontSize(7f);
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
