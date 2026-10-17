using System;
using GCTL.Core.ViewModels.Finance.IncomeStatement;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.IncomeStatement
{
    public class IncomeStatementPdf : IDocument
    {
        private readonly IncomeStatementReportVM _model;

        private static class PdfColors
        {
            public static readonly string HeaderBg = "#F1F5F2";
            public static readonly string Primary = "#ffffff";
            //public static readonly string Primary = "#0E5A4A";
            public static readonly string Border = "#D4DCD7";
            public static readonly string BorderDark = "#222222";
            public static readonly string TextDark = "#18262B";
            public static readonly string TextMuted = "#5D6D68";
            public static readonly string TotalBg = "#EFF4F1";
            public static readonly string Ok = "#1B7A4B";
            public static readonly string Bad = "#B3261E";
        }

        public IncomeStatementPdf(IncomeStatementReportVM model)
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
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Segoe UI").FontColor(PdfColors.TextDark));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var branch = string.IsNullOrWhiteSpace(_model.BranchName) ? "All branches" : _model.BranchName;

            container.PaddingBottom(12).Column(column =>
            {
                column.Item().AlignCenter().Text(_model.CompanyName ?? "School Management ERP")
                    .FontSize(15).Bold().FontColor(PdfColors.Primary);

                column.Item().AlignCenter().Text("Income Statement")
                    .FontSize(13).Bold().FontColor(PdfColors.TextDark);

                column.Item().AlignCenter().Text($"{_model.FromDateFormatted} to {_model.ToDateFormatted} · {branch} · Amounts in Bangladeshi Taka (৳)")
                    .FontSize(8.5f).FontColor(PdfColors.TextMuted);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4.5f); // Particulars
                    columns.RelativeColumn(1.5f); // Amount (৳)
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Particulars").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount (৳)").Bold();
                });

                foreach (var item in _model.Items)
                {
                    if (item.IsTotal) // Subtotals or Grand Total (Total Income, Total Expenses, Net Surplus)
                    {
                        table.Cell().Element(TotalCellStyle).Text(item.Name).Bold();
                        table.Cell().Element(TotalCellStyle).AlignRight().Text(item.AmountFormatted).Bold();
                    }
                    else if (item.Level == 1) // Section Header (Income / Expenses)
                    {
                        table.Cell().Element(SectionHeaderStyle).Text(item.Name).Bold();
                        table.Cell().Element(SectionHeaderStyle).Text("");
                    }
                    else // Hierarchical lines
                    {
                        float indent = item.Level switch
                        {
                            2 => 8f,
                            3 => 18f,
                            _ => 28f
                        };

                        var descCell = table.Cell().Element(DataCellStyle).PaddingLeft(indent).Text(item.DisplayText)
                            .FontColor(item.Level == 2 ? PdfColors.TextDark : (item.Level == 3 ? PdfColors.TextDark : PdfColors.TextMuted));
                        if (item.Level == 2) descCell.Bold();

                        var amtCell = table.Cell().Element(DataCellStyle).AlignRight().Text(item.AmountFormatted);
                        if (item.Level == 2) amtCell.Bold();
                    }
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(10).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text($"Print Date: {DateTime.Now:dd/MM/yyyy hh:mm tt}")
                    .FontSize(7.5f).FontColor(PdfColors.TextMuted);

                row.RelativeItem().AlignCenter().Text("GCTL-Infosys")
                    .FontSize(7.5f).FontColor(PdfColors.TextMuted);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                    text.CurrentPageNumber().FontSize(7.5f).FontColor(PdfColors.TextMuted);
                    text.Span(" of ").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                    text.TotalPages().FontSize(7.5f).FontColor(PdfColors.TextMuted);
                });
            });
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(PdfColors.HeaderBg)
                .BorderBottom(2)
                .BorderColor(PdfColors.Primary)
                .PaddingVertical(5)
                .PaddingHorizontal(8);
        }

        private static IContainer SectionHeaderStyle(IContainer container)
        {
            return container
                .Background("#E3EDE8")
                .PaddingVertical(5)
                .PaddingHorizontal(8);
        }

        private static IContainer TotalCellStyle(IContainer container)
        {
            return container
                .Background(PdfColors.TotalBg)
                .BorderTop(1.5f)
                .BorderBottom(2.5f)
                .BorderColor(PdfColors.BorderDark)
                .PaddingVertical(5)
                .PaddingHorizontal(8);
        }

        private static IContainer DataCellStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor("#E6ECE8")
                .PaddingVertical(4)
                .PaddingHorizontal(8);
        }
    }
}
