using System;
using System.Linq;
using GCTL.Core.ViewModels.Finance.CashFlow;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.CashFlow
{
    public class CashFlowPdf : IDocument
    {
        private readonly CashFlowReportVM _model;

        private static class PdfColors
        {
            public static readonly string HeaderBg = "#F1F5F2";
            public static readonly string GroupHeaderBg = "#E3EDE8";
            public static readonly string SubTotalBg = "#EFF4F1";
            public static readonly string Border = "#E4E8E6";
            public static readonly string BorderLight = "#D4DCD7";
            public static readonly string BorderMedium = "#8C9D96";
            public static readonly string TextDark = "#18262B";
            public static readonly string TextMuted = "#5D6D68";
            public static readonly string White = "#FFFFFF";
            public static readonly string Primary = "#ffffff";
            //public static readonly string Primary = "#0E5A4A";
        }

        public CashFlowPdf(CashFlowReportVM model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Segoe UI").FontColor(PdfColors.TextDark));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var fromStr = _model.Filter.FromDate.HasValue ? _model.Filter.FromDate.Value.ToString("dd/MM/yyyy") : "Start";
            var toStr = _model.Filter.ToDate.HasValue ? _model.Filter.ToDate.Value.ToString("dd/MM/yyyy") : "End";

            container.PaddingBottom(12).Column(column =>
            {
                column.Item().AlignCenter().Column(col =>
                {
                    col.Item().AlignCenter().Text(_model.CompanyName ?? "Greenfield International School")
                        .FontSize(15).Bold().FontColor(PdfColors.Primary);

                    col.Item().AlignCenter().Text("Cash Flow Statement (Direct Method)")
                        .FontSize(12).Bold().FontColor(PdfColors.TextDark);

                    col.Item().AlignCenter().Text($"{fromStr} to {toStr}")
                        .FontSize(9f).FontColor(PdfColors.TextMuted);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3.5f); // Particulars
                    columns.RelativeColumn(1.0f); // Receipts
                    columns.RelativeColumn(1.0f); // Payments
                    columns.RelativeColumn(1.2f); // Net Amount (৳)
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Particulars").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Receipts (৳)").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Payments (৳)").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Net Cash (৳)").Bold();
                });

                foreach (var group in _model.Activities)
                {
                    // Activity Group Header
                    table.Cell().ColumnSpan(4).Element(e => GroupHeaderCellStyle(e))
                        .Text(group.Title).Bold();

                    if (group.Items.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).Element(e => DataCellStyle(e, PdfColors.White))
                            .PaddingLeft(14).Text("No cash transactions in this activity period.").Italic().FontColor(PdfColors.TextMuted);
                    }
                    else
                    {
                        foreach (var item in group.Items)
                        {
                            var netStr = item.NetAmount < 0 ? $"({Math.Abs(item.NetAmount):N2})" : $"{item.NetAmount:N2}";
                            var rcptStr = item.Receipts > 0 ? $"{item.Receipts:N2}" : "-";
                            var pmtStr = item.Payments > 0 ? $"{item.Payments:N2}" : "-";

                            table.Cell().Element(e => DataCellStyle(e, PdfColors.White))
                                .PaddingLeft(14).Text(item.DisplayLabel);

                            table.Cell().Element(e => DataCellStyle(e, PdfColors.White)).AlignRight()
                                .Text(rcptStr);

                            table.Cell().Element(e => DataCellStyle(e, PdfColors.White)).AlignRight()
                                .Text(pmtStr);

                            table.Cell().Element(e => DataCellStyle(e, PdfColors.White)).AlignRight()
                                .Text(netStr);
                        }
                    }

                    // Activity Group Subtotal
                    var subtotalNetStr = group.NetCashFlow < 0 ? $"({Math.Abs(group.NetCashFlow):N2})" : $"{group.NetCashFlow:N2}";
                    table.Cell().Element(SubTotalCellStyle)
                        .PaddingLeft(8).Text($"Net cash from {group.ActivityType.ToLower()} activities").Bold();
                    table.Cell().Element(SubTotalCellStyle).AlignRight().Text($"{group.TotalReceipts:N2}").Bold();
                    table.Cell().Element(SubTotalCellStyle).AlignRight().Text($"{group.TotalPayments:N2}").Bold();
                    table.Cell().Element(SubTotalCellStyle).AlignRight().Text(subtotalNetStr).Bold();
                }

                // Summary / Reconciliation Rows
                var netChangeStr = _model.Summary.NetCashFlow < 0 
                    ? $"({Math.Abs(_model.Summary.NetCashFlow):N2})" 
                    : $"{_model.Summary.NetCashFlow:N2}";

                table.Cell().ColumnSpan(3).Element(TotalCellStyle).Text("Net increase / (decrease) in cash & bank").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text(netChangeStr).Bold();

                table.Cell().ColumnSpan(3).Element(e => DataCellStyle(e, PdfColors.SubTotalBg)).Text("Cash & bank at beginning of period").Bold();
                table.Cell().Element(e => DataCellStyle(e, PdfColors.SubTotalBg)).AlignRight().Text($"{_model.Summary.OpeningCashAndBank:N2}").Bold();

                table.Cell().ColumnSpan(3).Element(GrandTotalCellStyle).Text("Cash & bank at end of period").Bold();
                table.Cell().Element(GrandTotalCellStyle).AlignRight().Text($"{_model.Summary.ClosingCashAndBank:N2}").Bold();
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text($"Generated on: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                row.ConstantItem(120).AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                    text.CurrentPageNumber().FontSize(7.5f).Bold().FontColor(PdfColors.TextDark);
                    text.Span(" of ").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                    text.TotalPages().FontSize(7.5f).Bold().FontColor(PdfColors.TextDark);
                });
            });
        }

        private static IContainer HeaderCellStyle(IContainer container) =>
            container.Border(0.5f).BorderColor(PdfColors.BorderLight).Background(PdfColors.HeaderBg).Padding(5);

        private static IContainer GroupHeaderCellStyle(IContainer container) =>
            container.BorderBottom(0.5f).BorderColor(PdfColors.BorderLight).Background(PdfColors.GroupHeaderBg).PaddingVertical(4).PaddingHorizontal(6);

        private static IContainer DataCellStyle(IContainer container, string bgColor) =>
            container.BorderBottom(0.5f).BorderColor(PdfColors.Border).Background(bgColor).PaddingVertical(3.5f).PaddingHorizontal(5);

        private static IContainer SubTotalCellStyle(IContainer container) =>
            container.BorderTop(0.5f).BorderBottom(0.5f).BorderColor(PdfColors.BorderMedium).Background(PdfColors.SubTotalBg).PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer TotalCellStyle(IContainer container) =>
            container.BorderTop(0.75f).BorderBottom(0.5f).BorderColor(PdfColors.BorderMedium).Background(PdfColors.HeaderBg).PaddingVertical(5).PaddingHorizontal(5);

        private static IContainer GrandTotalCellStyle(IContainer container) =>
            container.BorderTop(0.5f).BorderBottom(1.25f).BorderColor(PdfColors.BorderMedium).Background(PdfColors.GroupHeaderBg).PaddingVertical(5).PaddingHorizontal(5);
    }
}
