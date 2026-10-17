using System;
using System.Collections.Generic;
using GCTL.Core.ViewModels.Finance.ChartOfAccountsReports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.Finance.ChartOfAccountsReports
{
    public class ChartOfAccountsReportPdf : IDocument
    {
        private readonly ChartOfAccountsReportResponseVM _model;
        private readonly string _viewMode; // "tree" or "list"

        private static class PdfColors
        {
            //public static readonly string Primary = "#0B4D37";
            public static readonly string Primary = "#ffffff";
            public static readonly string HeaderBg = "#F1F5F2";
            public static readonly string AltRowBg = "#F9FBF9";
            public static readonly string Border = "#D4DCD7";
            public static readonly string TextDark = "#18262B";
            public static readonly string TextMuted = "#5D6D68";
            public static readonly string White = "#FFFFFF";
        }

        public ChartOfAccountsReportPdf(ChartOfAccountsReportResponseVM model, string viewMode = "tree")
        {
            _model = model;
            _viewMode = string.Equals(viewMode, "list", StringComparison.OrdinalIgnoreCase) ? "list" : "tree";
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        private static readonly int[] Cuts = { 1, 3, 6, 11, 16 };
        private static readonly (string Bg, string Fg)[] SegmentStyles = new[]
        {
            ("#CFE3FF", "#0A3D62"), // s0: Level 0 (Group) - light blue
            ("#D3EFDB", "#1E392A"), // s1: Level 1 (Control) - light green
            ("#FBEEBA", "#533F03"), // s2: Level 2 (Sub-Control) - light yellow
            ("#F9D5CB", "#5C1D0D"), // s3: Level 3 (Sub-Subsidiary) - light peach
            ("#E3D7F4", "#3B1B68")  // s4: Level 4 (General Ledger) - light lavender
        };

        private static List<(string Text, string Bg, string Fg)> GetCodeSegments(string? code)
        {
            var segments = new List<(string Text, string Bg, string Fg)>();
            if (string.IsNullOrWhiteSpace(code)) return segments;

            var cleanCode = code.Trim();
            int prev = 0;
            for (int i = 0; i < 5 && prev < cleanCode.Length; i++)
            {
                int cut = Math.Min(Cuts[i], cleanCode.Length);
                if (cut > prev)
                {
                    var text = cleanCode.Substring(prev, cut - prev);
                    var style = SegmentStyles[i];
                    segments.Add((text, style.Bg, style.Fg));
                    prev = cut;
                }
            }

            if (prev < cleanCode.Length)
            {
                var text = cleanCode.Substring(prev);
                segments.Add((text, SegmentStyles[4].Bg, SegmentStyles[4].Fg));
            }

            return segments;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(_viewMode == "tree" ? PageSizes.A4 : PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI").FontColor(PdfColors.TextDark));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(_viewMode == "tree" ? ComposeTreeContent : ComposeListContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var title = _viewMode == "tree"
                ? "Chart of Accounts · Tree Hierarchy"
                : "Chart of Accounts · List View (5-Level Hierarchy)";

            container.PaddingBottom(8).Column(col =>
            {
                col.Item().AlignCenter().Text(_model?.CompanyName ?? "Chart of Accounts").FontSize(14).Bold().FontColor(PdfColors.Primary);
                col.Item().AlignCenter().Text(title).FontSize(11).Bold();
                col.Item().AlignCenter().Text($"Total Ledgers: {_model?.Stats?.TotalGeneralLedgers ?? 0}").FontSize(8.5f).FontColor(PdfColors.TextMuted);

                if (_viewMode == "list")
                {
                    // KPI summary strip only for List View
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(4).AlignCenter().Text($"Total: {_model?.Stats?.TotalGeneralLedgers ?? 0}").Bold();
                        row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(4).AlignCenter().Text($"Assets: {_model?.Stats?.AssetsGLCount ?? 0}").Bold();
                        row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(4).AlignCenter().Text($"Liabilities & Equity: {_model?.Stats?.EquityGLCount ?? 0}").Bold();
                        row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(4).AlignCenter().Text($"Income: {_model?.Stats?.IncomeGLCount ?? 0}").Bold();
                        row.RelativeItem().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.HeaderBg).Padding(4).AlignCenter().Text($"Expenses: {_model?.Stats?.ExpenseGLCount ?? 0}").Bold();
                    });
                }
            });
        }

        #region Tree Hierarchy Content (No Table - Hierarchical Document View)
        private void ComposeTreeContent(IContainer container)
        {
            var nodes = (_model?.VisibleTree != null && _model.VisibleTree.Count > 0)
                ? _model.VisibleTree
                : GetFlattenedTree(_model?.Tree);

            container.PaddingTop(4).Column(col =>
            {
                foreach (var node in nodes)
                {
                    var segments = GetCodeSegments(node.Code);
                    float indent = node.Level * 18f;
                    float topPad = node.Level == 0 ? 5f : 1.5f;
                    float btmPad = 1.5f;

                    col.Item().PaddingLeft(indent).PaddingTop(topPad).PaddingBottom(btmPad).Row(row =>
                    {
                        // 1. Arrow / Toggle Indicator
                        if (node.Level < 4)
                        {
                            if (node.IsOpen)
                            {
                                // Expanded: Grey down arrow ▼
                                row.AutoItem()
                                   .Width(13)
                                   .PaddingRight(3)
                                   .AlignMiddle()
                                   .Text("▼")
                                   .FontSize(7.5f)
                                   .FontColor("#556068");
                            }
                            else
                            {
                                // Collapsed: Blue right arrow in subtle light blue box [ ▶ ]
                                row.AutoItem()
                                   .PaddingRight(3)
                                   .AlignMiddle()
                                   .Background("#E0EDFD")
                                   .Border(0.8f)
                                   .BorderColor("#93C5FD")
                                   .PaddingHorizontal(2f)
                                   .PaddingVertical(0.5f)
                                   .Text("▶")
                                   .FontSize(6f)
                                   .Bold()
                                   .FontColor("#2563EB");
                            }
                        }
                        else
                        {
                            // Leaf node (5th layer General Ledger)
                            row.AutoItem()
                               .Width(13)
                               .PaddingRight(3)
                               .AlignMiddle()
                               .Text("•")
                               .FontSize(7.5f)
                               .FontColor("#94A3B8");
                        }

                        // 2. Segmented Code Chips (s0-s4 colors)
                        foreach (var seg in segments)
                        {
                            row.AutoItem()
                               .PaddingRight(2)
                               .AlignMiddle()
                               .Background(seg.Bg)
                               .PaddingHorizontal(4f)
                               .PaddingVertical(1f)
                               .Text(seg.Text)
                               .FontSize(7.5f)
                               .Bold()
                               .FontColor(seg.Fg);
                        }

                        // 3. Ledger Name
                        row.AutoItem()
                           .PaddingLeft(4)
                           .AlignMiddle()
                           .Text(text =>
                           {
                               var nameSpan = text.Span(node.Name ?? "").FontSize(8.8f).FontColor("#1E293B");
                               if (node.Level <= 1)
                               {
                                   nameSpan.Bold();
                               }
                               else
                               {
                                   nameSpan.SemiBold();
                               }

                               if (!string.IsNullOrWhiteSpace(node.ShortName))
                               {
                                   text.Span($" ({node.ShortName})").FontSize(8f).FontColor("#64748B");
                               }
                           });

                        // 4. Inline GL Count Chip (or Account Type for leaf)
                        if (node.Level < 4)
                        {
                            row.AutoItem()
                               .PaddingLeft(6)
                               .AlignMiddle()
                               .Background("#DCEFE3")
                               .PaddingHorizontal(5f)
                               .PaddingVertical(1f)
                               .Text($"{node.GLCount} GL")
                               .FontSize(7.5f)
                               .Bold()
                               .FontColor("#0F5132");
                        }
                        else
                        {
                            row.AutoItem()
                               .PaddingLeft(6)
                               .AlignMiddle()
                               .Background("#DCEFE3")
                               .PaddingHorizontal(5f)
                               .PaddingVertical(1f)
                               .Text(node.AccountType ?? "Asset")
                               .FontSize(7.5f)
                               .Bold()
                               .FontColor("#0F5132");
                        }

                        if (!node.IsActive)
                        {
                            row.AutoItem()
                               .PaddingLeft(4)
                               .AlignMiddle()
                               .Background("#FEE2E2")
                               .PaddingHorizontal(4f)
                               .PaddingVertical(1f)
                               .Text("Archived")
                               .FontSize(7f)
                               .Bold()
                               .FontColor("#991B1B");
                        }
                    });
                }
            });
        }

        private static List<ChartOfAccountsReportTreeDto> GetFlattenedTree(List<ChartOfAccountsReportTreeDto>? rootNodes)
        {
            var result = new List<ChartOfAccountsReportTreeDto>();
            FlattenTree(rootNodes, result);
            return result;
        }

        private static void FlattenTree(List<ChartOfAccountsReportTreeDto>? nodes, List<ChartOfAccountsReportTreeDto> result)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                result.Add(node);
                if (node.Children != null && node.Children.Count > 0)
                {
                    FlattenTree(node.Children, result);
                }
            }
        }
        #endregion

        #region List View Content
        private void ComposeListContent(IContainer container)
        {
            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.4f); // L1 Group
                    columns.RelativeColumn(1.6f); // L2 Control
                    columns.RelativeColumn(1.8f); // L3 Sub-Control
                    columns.RelativeColumn(2.0f); // L4 Sub-Subsidiary
                    columns.RelativeColumn(1.4f); // Account Code
                    columns.RelativeColumn(2.2f); // Account Head (5th Layer)
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.Primary).Padding(4).Text("Group Ledger (1st Layer)").Bold().FontColor(PdfColors.White);
                    header.Cell().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.Primary).Padding(4).Text("Control Ledger (2nd Layer)").Bold().FontColor(PdfColors.White);
                    header.Cell().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.Primary).Padding(4).Text("Sub-Control Ledger (3rd Layer)").Bold().FontColor(PdfColors.White);
                    header.Cell().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.Primary).Padding(4).Text("Sub-Subsidiary Ledger (4th Layer)").Bold().FontColor(PdfColors.White);
                    header.Cell().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.Primary).Padding(4).Text("Account Code").Bold().FontColor(PdfColors.White);
                    header.Cell().Border(1).BorderColor(PdfColors.Border).Background(PdfColors.Primary).Padding(4).Text("Account Head (5th Layer)").Bold().FontColor(PdfColors.White);
                });

                int idx = 0;
                var list = _model?.List ?? new List<ChartOfAccountsReportItemDto>();
                foreach (var item in list)
                {
                    string bg = (idx++ % 2 == 0) ? PdfColors.White : PdfColors.AltRowBg;

                    table.Cell().Border(0.5f).BorderColor(PdfColors.Border).Background(bg).Padding(3).Text($"{item.GroupCode}-{item.GroupName}");
                    table.Cell().Border(0.5f).BorderColor(PdfColors.Border).Background(bg).Padding(3).Text($"{item.ControlCode}-{item.ControlName}");
                    table.Cell().Border(0.5f).BorderColor(PdfColors.Border).Background(bg).Padding(3).Text($"{item.SubControlCode}-{item.SubControlName}");
                    table.Cell().Border(0.5f).BorderColor(PdfColors.Border).Background(bg).Padding(3).Text($"{item.SubSubsidiaryCode}-{item.SubSubsidiaryName}");
                    table.Cell().Border(0.5f).BorderColor(PdfColors.Border).Background(bg).Padding(3).Text(item.GeneralLedgerCode ?? "").Bold();
                    var headName = item.GeneralLedgerName ?? "";
                    table.Cell().Border(0.5f).BorderColor(PdfColors.Border).Background(bg).Padding(3).Text(headName);
                }
            });
        }
        #endregion

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(6).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text($"Print Date & Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(8f).FontColor(PdfColors.TextMuted);
                row.RelativeItem().AlignCenter().Text("GCTL Infosys").FontSize(8.5f).Bold().FontColor(PdfColors.TextMuted);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8f).FontColor(PdfColors.TextMuted);
                    text.CurrentPageNumber().FontSize(8f).Bold().FontColor(PdfColors.TextDark);
                    text.Span(" / ").FontSize(8f).FontColor(PdfColors.TextMuted);
                    text.TotalPages().FontSize(8f).Bold().FontColor(PdfColors.TextDark);
                });
            });
        }
    }
}
