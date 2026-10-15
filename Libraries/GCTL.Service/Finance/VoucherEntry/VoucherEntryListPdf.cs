using System;
using System.Collections.Generic;
using System.Linq;
using GCTL.Core.ViewModels.Finance.VoucherEntryVM;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.VoucherEntry
{
    public class VoucherEntryListPdfModel
    {
        public string CompanyName { get; set; } = "Greenfield International School";
        public string ReportTitle { get; set; } = "Voucher List";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<VoucherEntryVM> Items { get; set; } = new List<VoucherEntryVM>();
        public decimal TotalAmount => Items.Sum(x => x.TotalAmount);
    }

    public class VoucherEntryListPdf : IDocument
    {
        private readonly VoucherEntryListPdfModel _model;

        private static class PdfColors
        {
            public static readonly string HeaderBg = "#13523C"; // Dark Green matching grid
            public static readonly string HeaderText = "#FFFFFF";
            public static readonly string BorderColor = "#D9D9D9";
            public static readonly string TextDark = "#000000";
            public static readonly string TextMuted = "#4B5563";
            public static readonly string TextLight = "#6B7280";
        }

        public VoucherEntryListPdf(VoucherEntryListPdfModel model)
        {
            _model = model ?? new VoucherEntryListPdfModel();
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI").FontColor(PdfColors.TextDark));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var fromStr = _model.FromDate.HasValue ? _model.FromDate.Value.ToString("dd/MM/yyyy") : "01/01/2026";
            var toStr = _model.ToDate.HasValue ? _model.ToDate.Value.ToString("dd/MM/yyyy") : "31/12/2026";
            var dateRangeText = $"{fromStr} to {toStr}";

            container.PaddingBottom(10).AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text(_model.CompanyName ?? "Greenfield International School")
                    .FontSize(14).Bold().FontColor(PdfColors.TextDark);

                col.Item().AlignCenter().Text(_model.ReportTitle ?? "Voucher List")
                    .FontSize(11).Bold().FontColor(PdfColors.TextDark);

                col.Item().AlignCenter().Text(dateRangeText)
                    .FontSize(8.5f).FontColor(PdfColors.TextMuted);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.4f);  // Voucher No.
                    columns.RelativeColumn(0.95f); // Date
                    columns.RelativeColumn(0.55f); // Type
                    columns.RelativeColumn(1.4f);  // Branch
                    columns.RelativeColumn(3.1f);  // Narration
                    columns.RelativeColumn(1.3f);  // Amount (৳)
                    columns.RelativeColumn(1.1f);  // Status
                });

                // Header matching grid
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).AlignLeft().Text("Voucher No.").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Date").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Type").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignLeft().Text("Branch").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignLeft().Text("Narration").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount (৳)").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Status").Bold().FontColor(PdfColors.HeaderText).FontSize(8.5f);
                });

                if (_model.Items != null && _model.Items.Any())
                {
                    foreach (var v in _model.Items)
                    {
                        // Voucher No. (Bold, Left)
                        table.Cell().Element(DataCellStyle).AlignLeft().Text(v.VoucherNo ?? "").Bold().FontSize(8f);

                        // Date (Center)
                        table.Cell().Element(DataCellStyle).AlignCenter().Text(v.VoucherDateFormatted ?? v.VoucherDate.ToString("dd/MM/yyyy")).FontSize(8f);

                        // Type (Bold, Center)
                        table.Cell().Element(DataCellStyle).AlignCenter().Text(v.VoucherType_Code ?? "").Bold().FontSize(8f);

                        // Branch (Left)
                        table.Cell().Element(DataCellStyle).AlignLeft().Text(v.BranchName ?? "Head Office").FontSize(8f);

                        // Narration (Left)
                        table.Cell().Element(DataCellStyle).AlignLeft().Text(v.Narration ?? "").FontSize(8f);

                        // Amount (৳) (Right, formatted #,##0.00)
                        table.Cell().Element(DataCellStyle).AlignRight().Text(v.TotalAmount.ToString("#,##0.00")).FontSize(8f);

                        // Status (Center: Posted -> Approved, Pending Approval -> Pending)
                        var isApproved = string.Equals(v.IsApproved?.Trim(), "Posted", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(v.IsApproved?.Trim(), "Approved", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(v.IsApproved?.Trim(), "Approval", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(v.IsApproved?.Trim(), "Y", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(v.IsApproved?.Trim(), "1", StringComparison.OrdinalIgnoreCase);

                        var statDisplay = isApproved ? "Approved" : "Pending";
                        table.Cell().Element(DataCellStyle).AlignCenter().Text(statDisplay).FontSize(8f);
                    }
                }
                else
                {
                    table.Cell().ColumnSpan(7).Element(DataCellStyle).AlignCenter().Text("No vouchers match the filters.").Italic();
                }

                // Total Row matching grid
                table.Cell().ColumnSpan(5).Element(TotalCellStyle).AlignRight().Text("Total").Bold().FontSize(8.5f);
                table.Cell().Element(TotalCellStyle).AlignRight().Text(_model.TotalAmount.ToString("#,##0.00")).Bold().FontSize(8.5f);
                table.Cell().Element(TotalCellStyle);
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

        private static IContainer DataCellStyle(IContainer container)
        {
            return container
                .Border(0.5f)
                .BorderColor(PdfColors.BorderColor)
                .PaddingVertical(4.5f)
                .PaddingHorizontal(4);
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
