using System;
using System.Globalization;
using System.Linq;
using GCTL.Core.ViewModels.CustomerRateReport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.CustomerRateReport
{
    public static class CustomerRateReportPdfGenerator
    {
        public static byte[] GeneratePdf(CustomerRateReportPdfModel model)
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
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9.5f).FontColor(Colors.Black));

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
                                    .FontSize(9.5f).FontFamily("Arial");
                            });
                        });

                        col.Item().PaddingTop(4).PaddingBottom(6).AlignCenter()
                            .Text(model.ReportTitle ?? "Customer Rate Report")
                            .Bold().FontSize(12f).FontFamily("Arial");
                    });

                    // Body Content
                    page.Content().Column(col =>
                    {
                        if (model.CustomerGroups == null || !model.CustomerGroups.Any())
                        {
                            col.Item().PaddingTop(30).AlignCenter()
                                .Text("No rate records found for the selected customer(s).")
                                .FontSize(10).Bold();
                            return;
                        }

                        foreach (var group in model.CustomerGroups)
                        {
                            // Customer Banner for each group
                            col.Item().PaddingTop(8).PaddingBottom(4).Row(r =>
                            {
                                r.RelativeItem(7).Column(c =>
                                {
                                    c.Item().Row(cr =>
                                    {
                                        //cr.ConstantItem(65).Text("Customer:").Bold().FontSize(8.5f);
                                        cr.RelativeItem().Text(string.IsNullOrWhiteSpace(group.CustomerName) ? "-" : group.CustomerName).Bold().FontSize(8.5f);
                                    });
                                    if (!string.IsNullOrWhiteSpace(group.CustomerAddress))
                                    {
                                        c.Item().Row(cr =>
                                        {
                                            //cr.ConstantItem(65).Text("Address:").FontSize(8f).FontColor(Colors.Grey.Darken2);
                                            cr.RelativeItem().Text(group.CustomerAddress).FontSize(8f).FontColor(Colors.Grey.Darken2);
                                        });
                                    }
                                });

                                //r.RelativeItem(3).AlignRight().Column(c =>
                                //{
                                //    c.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(8.5f);
                                //});
                            });

                            // 4-Column Rate Table
                            col.Item().PaddingBottom(12).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(28);   // Column 1: A. / B.
                                    columns.RelativeColumn(5.2f); // Particulars
                                    columns.RelativeColumn(2.5f); // Amount (BDT)
                                    columns.RelativeColumn(2.3f); // Remarks
                                });

                                // Main Table Header
                                table.Header(header =>
                                {
                                    header.Cell().Border(0.4f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().AlignMiddle()
                                        .Text("").SemiBold().FontSize(8.5f);

                                    header.Cell().Border(0.4f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().AlignMiddle()
                                        .Text("Particulars").SemiBold().FontSize(8.5f);

                                    header.Cell().Border(0.4f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().AlignMiddle()
                                        .Text("Amount (BDT)").SemiBold().FontSize(8.5f);

                                    header.Cell().Border(0.4f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().AlignMiddle()
                                        .Text("Remarks").SemiBold().FontSize(8.5f);
                                });

                                if (group.Items != null && group.Items.Any())
                                {
                                    foreach (var item in group.Items)
                                    {
                                        if (item.IsSectionHeader)
                                        {
                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignCenter().Text(item.Section).Bold().FontSize(8.5f);

                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignLeft().Text(item.Particulars).Bold().FontSize(8.5f);

                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignRight().Text("").FontSize(8.5f);

                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignLeft().Text("").FontSize(8.5f);
                                        }
                                        else
                                        {
                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignCenter().Text("").FontSize(8f);

                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignLeft().Text(item.Particulars ?? "").FontSize(8f);

                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignRight().Text(item.Amount ?? "").FontSize(8f);

                                            table.Cell().Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                                .AlignLeft().Text(item.Remarks ?? "").FontSize(8f);
                                        }
                                    }
                                }
                            });
                        }
                    });

                    // Footer on every page (Exact match to RequisitionSummaryReport)
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
