using System;
using System.Collections.Generic;
using System.Linq;
using GCTL.Core.ViewModels.Finance.OpeningBalancesVM;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.OpeningBalance
{
    public class OpeningBalancePdfModel
    {
        public string CompanyName { get; set; } = "Company";
        public string ReportTitle { get; set; } = "Opening Balance Report";
        public string? FinancialYearName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<OpeningBalanceVM> Items { get; set; } = new List<OpeningBalanceVM>();

        public decimal TotalDebit => Items
            .Where(x => string.Equals(x.TrType?.Trim(), "Dr", StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.OpeningBalance ?? 0m);

        public decimal TotalCredit => Items
            .Where(x => string.Equals(x.TrType?.Trim(), "Cr", StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.OpeningBalance ?? 0m);

        public decimal Difference => Math.Abs(TotalDebit - TotalCredit);
        public bool IsBalanced => TotalDebit == TotalCredit;
    }

    public class OpeningBalancePdf : IDocument
    {
        private readonly OpeningBalancePdfModel _model;

        private static class PdfColors
        {
            public static readonly string HeaderBg = "#13523C"; // ERP standard dark green matching other reports
            public static readonly string HeaderText = "#FFFFFF";
            public static readonly string BorderColor = "#D9D9D9";
            public static readonly string TextDark = "#18262B";
            public static readonly string TextMuted = "#4B5563";
            public static readonly string TextLight = "#6B7280";
            public static readonly string AltRowBg = "#F9FAFB";
            public static readonly string DrColor = "#1E40AF";
            public static readonly string CrColor = "#991B1B";
        }

        public OpeningBalancePdf(OpeningBalancePdfModel model)
        {
            _model = model ?? new OpeningBalancePdfModel();
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4); // A4 Portrait
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI").FontColor(PdfColors.TextDark));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(10).AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text(_model.CompanyName ?? "Company")
                    .FontSize(14).Bold().FontColor(PdfColors.TextDark);

                col.Item().AlignCenter().Text(_model.ReportTitle ?? "Opening Balance Report")
                    .FontSize(11).Bold().FontColor(PdfColors.TextDark);

                // Financial Year & Date Subtitles
                var subDetails = new List<string>();
                if (!string.IsNullOrWhiteSpace(_model.FinancialYearName))
                {
                    subDetails.Add($"Financial Year: {_model.FinancialYearName}");
                }

                if (_model.FromDate.HasValue || _model.ToDate.HasValue)
                {
                    var fromStr = _model.FromDate.HasValue ? _model.FromDate.Value.ToString("dd/MM/yyyy") : "Start";
                    var toStr = _model.ToDate.HasValue ? _model.ToDate.Value.ToString("dd/MM/yyyy") : "End";
                    subDetails.Add($"Period: {fromStr} to {toStr}");
                }

                if (subDetails.Any())
                {
                    col.Item().AlignCenter().Text(string.Join("  |  ", subDetails))
                        .FontSize(8.5f).FontColor(PdfColors.TextMuted);
                }
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.3f);  // OB Code
                    columns.RelativeColumn(3.6f);  // Account Head & Details
                    columns.RelativeColumn(1.3f);  // Opening Balance
                    columns.RelativeColumn(0.7f);  // Type (Dr/Cr)
                    columns.RelativeColumn(1.0f);  // Opening Date
                    columns.RelativeColumn(1.0f);  // Financial Year
                    columns.RelativeColumn(1.1f);  // Reference
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).AlignLeft().Text("OB Code").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignLeft().Text("Account Head").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Balance (৳)").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Type").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Date").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("FY").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Reference").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                });

                // Rows
                if (_model.Items != null && _model.Items.Any())
                {
                    int index = 0;
                    foreach (var item in _model.Items)
                    {
                        var isAlt = index++ % 2 == 1;

                        // OB Code
                        table.Cell().Element(c => DataCellStyle(c, isAlt)).AlignLeft().Text(item.ComOpeningBalanceCode ?? "").Bold().FontSize(8f);

                        // Account Head & Description
                        table.Cell().Element(c => DataCellStyle(c, isAlt)).AlignLeft().Column(c =>
                        {
                            var headText = !string.IsNullOrWhiteSpace(item.AccountHeadDisplay)
                                ? item.AccountHeadDisplay
                                : (!string.IsNullOrWhiteSpace(item.GeneralLedgerCodeNo)
                                    ? $"{item.GeneralLedgerCodeNo} - {item.GeneralLedgerName}"
                                    : (item.GeneralLedgerName ?? ""));

                            c.Item().Text(headText).FontSize(8f);

                            if (!string.IsNullOrWhiteSpace(item.Description))
                            {
                                c.Item().Text(item.Description).FontSize(7f).FontColor(PdfColors.TextMuted);
                            }
                        });

                        // Opening Balance (৳)
                        var amtStr = (item.OpeningBalance ?? 0m).ToString("#,##0.00");
                        table.Cell().Element(c => DataCellStyle(c, isAlt)).AlignRight().Text(amtStr).FontSize(8f);

                        // Tr. Type (Dr / Cr)
                        var trType = item.TrType ?? "";
                        var typeColor = string.Equals(trType.Trim(), "Dr", StringComparison.OrdinalIgnoreCase)
                            ? PdfColors.DrColor
                            : PdfColors.CrColor;

                        table.Cell().Element(c => DataCellStyle(c, isAlt)).AlignCenter().Text(trType).Bold().FontColor(typeColor).FontSize(8f);

                        // Opening Date
                        var dateStr = item.OpeningDateFormatted ?? (item.OpeningDate.HasValue ? item.OpeningDate.Value.ToString("dd/MM/yyyy") : "-");
                        table.Cell().Element(c => DataCellStyle(c, isAlt)).AlignCenter().Text(dateStr).FontSize(8f);

                        // Financial Year
                        table.Cell().Element(c => DataCellStyle(c, isAlt)).AlignCenter().Text(item.FinancialCodeNo ?? "-").FontSize(8f);

                        // Reference
                        table.Cell().Element(c => DataCellStyle(c, isAlt)).AlignCenter().Text(item.Reference ?? "-").FontSize(8f);
                    }
                }
                else
                {
                    table.Cell().ColumnSpan(7).Element(c => DataCellStyle(c, false)).AlignCenter().Text("No opening balances found matching the filters.").Italic();
                }

                // Table Summary Row (Totals)
                table.Cell().ColumnSpan(2).Element(TotalCellStyle).AlignRight().Text(text =>
                {
                    text.Span("Total Debit: ").Bold().FontSize(8.5f);
                    text.Span(_model.TotalDebit.ToString("#,##0.00") + " Dr").Bold().FontColor(PdfColors.DrColor).FontSize(8.5f);
                    text.Span("   |   Total Credit: ").Bold().FontSize(8.5f);
                    text.Span(_model.TotalCredit.ToString("#,##0.00") + " Cr").Bold().FontColor(PdfColors.CrColor).FontSize(8.5f);
                    if (!_model.IsBalanced)
                    {
                        text.Span("   |   Diff: ").Bold().FontSize(8.5f);
                        text.Span(_model.Difference.ToString("#,##0.00")).Bold().FontColor(PdfColors.CrColor).FontSize(8.5f);
                    }
                });

                table.Cell().ColumnSpan(5).Element(TotalCellStyle);
            });
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(PdfColors.HeaderBg)
                .Border(0.5f)
                .BorderColor(PdfColors.HeaderBg)
                .PaddingVertical(5)
                .PaddingHorizontal(4);
        }

        private static IContainer DataCellStyle(IContainer container, bool isAlt)
        {
            var cell = container
                .Border(0.5f)
                .BorderColor(PdfColors.BorderColor)
                .PaddingVertical(4)
                .PaddingHorizontal(4);

            return isAlt ? cell.Background(PdfColors.AltRowBg) : cell;
        }

        private static IContainer TotalCellStyle(IContainer container)
        {
            return container
                .BorderTop(1f)
                .BorderBottom(2f)
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .BorderColor("#000000")
                .PaddingVertical(5)
                .PaddingHorizontal(4);
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Generated on: ").FontSize(7.5f).FontColor(PdfColors.TextLight);
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy hh:mm tt")).FontSize(7.5f).FontColor(PdfColors.TextLight);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(7.5f).FontColor(PdfColors.TextLight);
                    text.CurrentPageNumber().FontSize(7.5f).FontColor(PdfColors.TextLight);
                    text.Span(" of ").FontSize(7.5f).FontColor(PdfColors.TextLight);
                    text.TotalPages().FontSize(7.5f).FontColor(PdfColors.TextLight);
                });
            });
        }
    }
}
