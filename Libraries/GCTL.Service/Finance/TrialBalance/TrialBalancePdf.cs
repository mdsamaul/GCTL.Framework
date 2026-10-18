using GCTL.Core.ViewModels.Finance.TrialBalance;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.TrialBalance
{
    public class TrialBalancePdf : IDocument
    {
        private readonly TrialBalanceReportVM _model;

        private static class PdfColors
        {
            public static readonly string HeaderBg = "#F7F8F9";
            public static readonly string Level1Bg = "#F2F4F5";
            public static readonly string Border = "#DCDFE3";
            public static readonly string BorderDark = "#222222";
            public static readonly string TextDark = "#18262B";
            public static readonly string TextMuted = "#5D6D68";
            public static readonly string White = "#FFFFFF";
        }

        public TrialBalancePdf(TrialBalanceReportVM model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI").FontColor(PdfColors.TextDark));

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
                // Top-left title
               // column.Item().Text("Trial Balance").FontSize(11).Bold().FontColor(PdfColors.TextDark);

                // Centered School Name and Details
                column.Item().AlignCenter().Column(col =>
                {
                    col.Item().AlignCenter().Text(_model.CompanyName ?? "School Management ERP")
                        .FontSize(15).Bold().FontColor(PdfColors.TextDark);

                    col.Item().AlignCenter().Text("Trial Balance")
                        .FontSize(13).Bold().FontColor(PdfColors.TextDark);

                    col.Item().AlignCenter().Text($"{fromStr} to {toStr}")
                        .FontSize(9.5f).FontColor(PdfColors.TextMuted);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            var maxLevel = _model.Filter.Level;
            var displayItems = _model.Items.Where(x => x.Level <= maxLevel).ToList();

            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3.4f); // Particulars
                    columns.RelativeColumn(1.1f); // Opening Dr
                    columns.RelativeColumn(1.1f); // Opening Cr
                    columns.RelativeColumn(1.1f); // Trans Dr
                    columns.RelativeColumn(1.1f); // Trans Cr
                    columns.RelativeColumn(1.1f); // Closing Dr
                    columns.RelativeColumn(1.1f); // Closing Cr
                });

                table.Header(header =>
                {
                    // Row 1
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).AlignMiddle().Text("Particulars").Bold();
                    header.Cell().ColumnSpan(2).Element(HeaderGroupCellStyle).AlignCenter().Text("Opening Balance").Bold();
                    header.Cell().ColumnSpan(2).Element(HeaderGroupCellStyle).AlignCenter().Text("Transactions").Bold();
                    header.Cell().ColumnSpan(2).Element(HeaderGroupCellStyle).AlignCenter().Text("Closing Balance").Bold();

                    // Row 2
                    header.Cell().Element(HeaderSubCellStyle).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(HeaderSubCellStyle).AlignRight().Text("Credit").Bold();
                    header.Cell().Element(HeaderSubCellStyle).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(HeaderSubCellStyle).AlignRight().Text("Credit").Bold();
                    header.Cell().Element(HeaderSubCellStyle).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(HeaderSubCellStyle).AlignRight().Text("Credit").Bold();
                });

                foreach (var item in displayItems)
                {
                    string bgColor = PdfColors.White;
                    bool isBold = item.Level <= 2;
                    if (item.Level == 1) bgColor = PdfColors.Level1Bg;

                    var indent = (item.Level - 1) * 10;
                    var displayName = string.IsNullOrWhiteSpace(item.Code) ? item.Name : $"{item.Code}-{item.Name}";

                    var t1 = table.Cell().Element(e => DataCellStyle(e, bgColor))
                        .PaddingLeft(indent).Text(displayName);
                    if (isBold) t1.Bold();

                    var t2 = table.Cell().Element(e => DataCellStyle(e, bgColor)).AlignRight()
                        .Text(item.OpeningDebit > 0 ? $"{item.OpeningDebit:N2}" : "0.00");
                    if (isBold) t2.Bold();

                    var t3 = table.Cell().Element(e => DataCellStyle(e, bgColor)).AlignRight()
                        .Text(item.OpeningCredit > 0 ? $"{item.OpeningCredit:N2}" : "0.00");
                    if (isBold) t3.Bold();

                    var t4 = table.Cell().Element(e => DataCellStyle(e, bgColor)).AlignRight()
                        .Text(item.PeriodDebit > 0 ? $"{item.PeriodDebit:N2}" : "0.00");
                    if (isBold) t4.Bold();

                    var t5 = table.Cell().Element(e => DataCellStyle(e, bgColor)).AlignRight()
                        .Text(item.PeriodCredit > 0 ? $"{item.PeriodCredit:N2}" : "0.00");
                    if (isBold) t5.Bold();

                    var t6 = table.Cell().Element(e => DataCellStyle(e, bgColor)).AlignRight()
                        .Text(item.ClosingDebit > 0 ? $"{item.ClosingDebit:N2}" : "0.00");
                    if (isBold) t6.Bold();

                    var t7 = table.Cell().Element(e => DataCellStyle(e, bgColor)).AlignRight()
                        .Text(item.ClosingCredit > 0 ? $"{item.ClosingCredit:N2}" : "0.00");
                    if (isBold) t7.Bold();
                }

                // Grand Total Row
                table.Cell().Element(TotalCellStyle).Text("Grand Total").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.Summary.TotalOpeningDebit:N2}").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.Summary.TotalOpeningCredit:N2}").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.Summary.TotalPeriodDebit:N2}").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.Summary.TotalPeriodCredit:N2}").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.Summary.TotalClosingDebit:N2}").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.Summary.TotalClosingCredit:N2}").Bold();
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(8).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text($"Print Date: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                row.RelativeItem().AlignCenter().Text("GCTL-Infosys").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                    text.CurrentPageNumber().FontSize(7.5f).Bold().FontColor(PdfColors.TextDark);
                    text.Span(" of ").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                    text.TotalPages().FontSize(7.5f).Bold().FontColor(PdfColors.TextDark);
                });
            });
        }

        private static IContainer HeaderCellStyle(IContainer container) =>
            container.Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(4);

        private static IContainer HeaderGroupCellStyle(IContainer container) =>
            container.Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(4);

        private static IContainer HeaderSubCellStyle(IContainer container) =>
            container.Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(3);

        private static IContainer DataCellStyle(IContainer container, string bgColor) =>
            container.BorderBottom(1).BorderColor(PdfColors.Border).Background(bgColor).PaddingVertical(3).PaddingHorizontal(4);

        private static IContainer TotalCellStyle(IContainer container) =>
            container.BorderTop(1.5f).BorderBottom(2.5f).BorderColor(PdfColors.BorderDark).Background(PdfColors.HeaderBg).Padding(4);
    }
}

