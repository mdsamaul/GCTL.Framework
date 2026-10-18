using System;
using GCTL.Core.ViewModels.Finance.BalanceSheet;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.BalanceSheet
{
    public class BalanceSheetPdf : IDocument
    {
        private readonly BalanceSheetReportVM _model;

        private static class PdfColors
        {
            public static readonly string HeaderBg = "#F1F5F2";
            //public static readonly string Primary = "#0E5A4A";
            public static readonly string Primary = "#ffffff";
            public static readonly string Border = "#D4DCD7";
            public static readonly string BorderDark = "#222222";
            public static readonly string TextDark = "#18262B";
            public static readonly string TextMuted = "#5D6D68";
            public static readonly string TotalBg = "#EFF4F1";
            public static readonly string Ok = "#1B7A4B";
            public static readonly string Bad = "#B3261E";
        }

        public BalanceSheetPdf(BalanceSheetReportVM model)
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
                page.Margin(24);
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
                column.Item().AlignCenter().Text(_model.CompanyName ?? "School Management ERP")
                    .FontSize(15).Bold().FontColor(PdfColors.Primary);

                column.Item().AlignCenter().Text("Balance Sheet")
                    .FontSize(13).Bold().FontColor(PdfColors.TextDark);

                column.Item().AlignCenter().Text($"As at {_model.ToDateFormatted} · {branch} · Amounts in Bangladeshi Taka (৳)")
                    .FontSize(8.5f).FontColor(PdfColors.TextMuted);

                // KPI Summary Cards
                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Padding(5).Column(c =>
                    {
                        c.Item().Text("Total Assets").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                        c.Item().Text(_model.TotalAssetsFormatted).FontSize(10.5f).Bold().FontColor(PdfColors.Primary);
                    });

                    row.Spacing(6);

                    row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Padding(5).Column(c =>
                    {
                        c.Item().Text("Total Liabilities").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                        c.Item().Text(_model.TotalLiabilitiesFormatted).FontSize(10.5f).Bold().FontColor(PdfColors.TextDark);
                    });

                    row.Spacing(6);

                    row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Padding(5).Column(c =>
                    {
                        c.Item().Text("Equity (incl. surplus)").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                        c.Item().Text(_model.TotalEquityFormatted).FontSize(10.5f).Bold().FontColor(PdfColors.Primary);
                    });

                    row.Spacing(6);

                    row.RelativeItem().Border(1).BorderColor(_model.IsBalanced ? PdfColors.Ok : PdfColors.Bad).Padding(5).Column(c =>
                    {
                        c.Item().Text("Balance Check").FontSize(7.5f).FontColor(PdfColors.TextMuted);
                        c.Item().Text(_model.BalanceCheckLabel).FontSize(10.5f).Bold().FontColor(_model.IsBalanced ? PdfColors.Ok : PdfColors.Bad);
                        c.Item().Text(_model.BalanceCheckSub).FontSize(7f).FontColor(PdfColors.TextMuted);
                    });
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(4).Row(row =>
            {
                // Left Column: Assets
                row.RelativeItem().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3.5f);
                        cols.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("Assets").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount (৳)").Bold();
                    });

                    foreach (var item in _model.AssetsItems)
                    {
                        RenderTableRow(table, item);
                    }

                    // Total Assets Row
                    table.Cell().Element(TotalCellStyle).Text("Total Assets").Bold();
                    table.Cell().Element(TotalCellStyle).AlignRight().Text(_model.TotalAssetsAmountFormatted).Bold();
                });

                row.Spacing(12);

                // Right Column: Equities & Liabilities
                row.RelativeItem().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3.5f);
                        cols.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("Equities & Liabilities").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount (৳)").Bold();
                    });

                    foreach (var item in _model.EquitiesAndLiabilitiesItems)
                    {
                        RenderTableRow(table, item);
                    }

                    // Total Equities & Liabilities Row
                    table.Cell().Element(TotalCellStyle).Text("Total Equities & Liabilities").Bold();
                    table.Cell().Element(TotalCellStyle).AlignRight().Text(_model.TotalEquitiesAndLiabilitiesAmountFormatted).Bold();
                });
            });
        }

        private static void RenderTableRow(TableDescriptor table, BalanceSheetItemVM item)
        {
            if (item.Level == 1)
            {
                table.Cell().Element(SectionHeaderStyle).Text(item.DisplayText).Bold();
                table.Cell().Element(SectionHeaderStyle).Text("");
            }
            else if (item.IsTotal)
            {
                table.Cell().Element(TotalCellStyle).Text(item.DisplayText).Bold();
                table.Cell().Element(TotalCellStyle).AlignRight().Text(item.AmountFormatted).Bold();
            }
            else
            {
                float indent = item.Level switch
                {
                    2 => 6f,
                    3 => 14f,
                    _ => 22f
                };

                var descCell = table.Cell().Element(DataCellStyle).PaddingLeft(indent).Text(item.DisplayText)
                    .FontColor(item.Level == 2 ? PdfColors.TextDark : (item.Level == 3 ? PdfColors.TextDark : PdfColors.TextMuted));
                if (item.Level <= 2) descCell.Bold();

                var amtCell = table.Cell().Element(DataCellStyle).AlignRight().Text(item.AmountFormatted);
                if (item.Level <= 2) amtCell.Bold();
            }
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text(x =>
                {
                    x.Span("Generated on: ").FontColor(PdfColors.TextMuted);
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold();
                });

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(PdfColors.HeaderBg)
                .BorderBottom(1.5f)
                .BorderColor(PdfColors.Primary)
                .PaddingVertical(4)
                .PaddingHorizontal(5);
        }

        private static IContainer SectionHeaderStyle(IContainer container)
        {
            return container
                .Background("#E3EDE8")
                .BorderBottom(0.5f)
                .BorderColor(PdfColors.Border)
                .PaddingVertical(3)
                .PaddingHorizontal(5);
        }

        private static IContainer DataCellStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(PdfColors.Border)
                .PaddingVertical(2.5f)
                .PaddingHorizontal(5);
        }

        private static IContainer TotalCellStyle(IContainer container)
        {
            return container
                .Background(PdfColors.TotalBg)
                .BorderTop(1.2f)
                .BorderBottom(1.2f)
                .BorderColor(PdfColors.BorderDark)
                .PaddingVertical(3.5f)
                .PaddingHorizontal(5);
        }
    }
}
