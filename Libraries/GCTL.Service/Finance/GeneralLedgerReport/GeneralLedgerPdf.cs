using System;
using System.Linq;
using GCTL.Core.ViewModels.Finance.GeneralLedgerReport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.GeneralLedgerReport
{
    public class GeneralLedgerPdf : IDocument
    {
        private readonly GeneralLedgerReportVM _model;

        private static class PdfColors
        {
            public static readonly string HeaderBg = "#F1F5F2";
            public static readonly string OpeningBg = "#EFF4F1";
            public static readonly string Primary = "#ffffff";
            //public static readonly string Primary = "#0E5A4A";
            public static readonly string Border = "#D4DCD7";
            public static readonly string BorderDark = "#222222";
            public static readonly string TextDark = "#18262B";
            public static readonly string TextMuted = "#5D6D68";
            public static readonly string White = "#FFFFFF";
            public static readonly string CardBorder = "#E2E8E4";
        }

        public GeneralLedgerPdf(GeneralLedgerReportVM model)
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
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor(PdfColors.TextDark));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var branch = string.IsNullOrWhiteSpace(_model.BranchName) ? "All branches" : _model.BranchName;

            container.PaddingBottom(10).Column(column =>
            {
                // Centered School & Report Title
                column.Item().AlignCenter().Column(col =>
                {
                    col.Item().AlignCenter().Text(_model.CompanyName ?? "School Management ERP")
                        .FontSize(15).Bold().FontColor(PdfColors.Primary);

                    col.Item().AlignCenter().Text("General Ledger")
                        .FontSize(13).Bold().FontColor(PdfColors.TextDark);

                    col.Item().AlignCenter().Text($"{_model.AccountCode} – {_model.AccountName}")
                        .FontSize(11).Bold().FontColor(PdfColors.TextDark);

                    if (!string.IsNullOrWhiteSpace(_model.HierarchyPath))
                    {
                        col.Item().AlignCenter().Text(_model.HierarchyPath)
                            .FontSize(8f).FontColor(PdfColors.TextMuted);
                    }

                    col.Item().AlignCenter().Text($"{_model.FromDateFormatted} to {_model.ToDateFormatted} · {branch} · Amounts in Bangladeshi Taka (৳)")
                        .FontSize(8.5f).FontColor(PdfColors.TextMuted);
                });

            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.0f); // Date
                    columns.RelativeColumn(1.3f); // Voucher No
                    columns.RelativeColumn(1.1f); // Invoice No
                    columns.RelativeColumn(3.2f); // Narration
                    columns.RelativeColumn(1.1f); // Debit
                    columns.RelativeColumn(1.1f); // Credit
                    columns.RelativeColumn(1.2f); // Balance
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Date").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Voucher No").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Invoice No").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Narration").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Debit (৳)").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Credit (৳)").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Balance (৳)").Bold();
                });

                // Row 1: Opening balance row
                table.Cell().ColumnSpan(4).Element(DataCellStyle).Text("Opening balance").Bold();
                table.Cell().Element(DataCellStyle).Text("");
                table.Cell().Element(DataCellStyle).Text("");
                table.Cell().Element(DataCellStyle).AlignRight().Text(FormatBalance(_model.OpeningBalance)).Bold();

                // Transaction Rows
                if (_model.Items != null && _model.Items.Any())
                {
                    foreach (var tx in _model.Items)
                    {
                        table.Cell().Element(DataCellStyle).Text(tx.DateFormatted);
                        table.Cell().Element(DataCellStyle).Text(tx.VoucherNo).Bold().FontColor(PdfColors.Primary);
                        table.Cell().Element(DataCellStyle).Text(tx.InvoiceNo ?? "–");
                        table.Cell().Element(DataCellStyle).Text(tx.Narration ?? "");
                        table.Cell().Element(DataCellStyle).AlignRight().Text(tx.Debit > 0 ? $"{tx.Debit:N2}" : "");
                        table.Cell().Element(DataCellStyle).AlignRight().Text(tx.Credit > 0 ? $"{tx.Credit:N2}" : "");
                        table.Cell().Element(DataCellStyle).AlignRight().Text(FormatBalance(tx.RunningBalance)).SemiBold();
                    }
                }
                else
                {
                    table.Cell().ColumnSpan(7).Element(DataCellStyle).AlignCenter().Text("No transactions in this period.").Italic();
                }

                // Total / Closing row
                table.Cell().ColumnSpan(4).Element(TotalCellStyle).Text("Total / Closing").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.TotalDebit:N2}").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_model.TotalCredit:N2}").Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text(FormatBalance(_model.ClosingBalance)).Bold();
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

        private static string FormatBalance(decimal amount)
        {
            if (Math.Abs(amount) < 0.005m) return "0.00";
            return $"{Math.Abs(amount):N2} {(amount >= 0 ? "Dr" : "Cr")}";
        }

        private static IContainer HeaderCellStyle(IContainer container) =>
            container.Border(1).BorderColor(PdfColors.Border).Padding(4);

        private static IContainer DataCellStyle(IContainer container) =>
            container.BorderBottom(1).BorderColor(PdfColors.Border).PaddingVertical(3).PaddingHorizontal(4);

        private static IContainer TotalCellStyle(IContainer container) =>
            container.Padding(4);
    }
}
