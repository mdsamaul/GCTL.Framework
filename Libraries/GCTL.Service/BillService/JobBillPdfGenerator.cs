using ClosedXML.Excel;
using SkiaSharp;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GCTL.Service.BillService
{
    public static class JobBillPdfGenerator
    {
        public static byte[] GeneratePdf(JobBillPrintVM model)
        {
            return GeneratePdf(new List<JobBillPrintVM> { model });
        }

        public static byte[] GeneratePdf(List<JobBillPrintVM> models)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                if (models == null || models.Count == 0)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.Content().AlignCenter().AlignMiddle().Text("No bills found matching the selected criteria.").FontSize(12).Bold();
                    });
                    return;
                }

                foreach (var model in models)
                {
                    container.Page(page =>
                    {
                        RenderSingleBillPage(page, model);
                    });
                }
            });

            return document.GeneratePdf();
        }

        const float B = 0.5f;
        const string Arial = "Arial";
        const string Times = "Times New Roman";
        const string Cambria = "Cambria";
        const string Century = "Century";
        const string Georgia = "Georgia";

        const float W0 = 7302f, W1 = 1701f, W2 = 1791f;

        const float TableBudget = 636.3f;
        const float FixedRows = 255.85f;

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static string F2(decimal v) => v.ToString("0.00", Inv);
        static string N2(decimal v) => v.ToString("#,##0.00", Inv);

        static void Blank(IContainer c) => c.Text("");

    private static void RenderSingleBillPage(PageDescriptor page, JobBillPrintVM model)
    {
        // ---- 0. PAGE SETUP ------------------------------------------------------
        page.Size(PageSizes.A4);
        page.MarginLeft(37.4f);      // Word table starts at x = 37.4
        page.MarginRight(18.2f);     // -> content width = 539.7 (Word table width)
        page.MarginTop(23.5f);       // company-name box top in Word
        page.MarginBottom(15.3f);    // room for the "Cheques..." line at y ~ 814.6
        page.DefaultTextStyle(x => x.FontFamily(Arial).FontSize(9).FontColor(Colors.Black));

        page.Content().Column(col =>
        {
            // ---- 1. ROW A: company name + address (height 51.35 -> Bill To at y=74.85)
            col.Item().Height(51.35f).Row(row =>
            {
                row.RelativeItem().PaddingLeft(10.4f)                       // Word x = 47.8
                   .Text(model.CompanyName ?? "Formosa Logistics Limited")
                   .FontFamily(Arial).FontSize(20.5f).Bold();

                row.ConstantItem(129.2f).PaddingTop(5f)                     // Word x = 467.9, left aligned
                   .Text(model.CompanyAddress ?? "21, Hazaribagh, Dhaka-1209")
                   .FontFamily(Century).FontSize(8.5f).LetterSpacing(0.05f);  // approximates Word's 110% width
            });

            // ---- 2. ROW B: Bill To | BILL | meta labels | meta values (height 104.45 -> table top y=179.3)
            col.Item().Height(84.45f).Row(row =>
            {
                // Bill To (content x = 6.6, width 152.9)
                row.ConstantItem(229f).PaddingLeft(6.6f).Width(152.9f).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Bill To").FontFamily(Century).FontSize(8.5f).SemiBold();
                        t.Span(":").FontFamily(Times).FontSize(8.5f).Bold();
                    });
                    c.Item().PaddingTop(2.75f).Text(model.CustomerName ?? "")
                        .FontFamily(Cambria).FontSize(8f).LetterSpacing(0.04f);
                    if (!string.IsNullOrWhiteSpace(model.CustomerAddress))
                    {
                        c.Item().PaddingTop(4.55f).Text(model.CustomerAddress)
                            .FontFamily(Cambria).FontSize(8f).LineHeight(1.09f).LetterSpacing(0.05f);
                    }
                });

                // BILL (Georgia 20.5, NOT bold; left edge at content x = 229)
                row.ConstantItem(123.1f).PaddingTop(46.25f)
                   .Text("BILL").FontFamily("Engravers MT").FontSize(20.5f);

                // Meta labels: OUTSIDE the border, colon pushed right (Word justifies them)
                row.ConstantItem(33.9f).PaddingTop(13.5f+B).Column(l =>
                {
                    foreach (var s in new[] { "Bill No.", "Date", "JOB No." })
                    {
                        l.Item().Height(12.7f).Row(r =>
                        {
                            r.RelativeItem().Text(s).FontFamily(Century).FontSize(7.5f);
                            r.AutoItem().Text(":").FontFamily(Arial).FontSize(9f);
                        });
                    }
                });

                // Meta values: bordered box around VALUES ONLY (no vertical divider)
                row.ConstantItem(150.7f).PaddingTop(13.5f).Height(35f).Border(B).Column(m =>
                {
                    void Cell(string? v, float h, bool line)
                    {
                        var c = m.Item();
                        if (line) c = c.BorderBottom(B);
                        c.Height(h).PaddingLeft(1.8f).AlignMiddle()
                         .Text(v ?? "").FontFamily(Century).FontSize(7.5f);
                    }
                    Cell(model.BillNo, 11.5f, true);
                    Cell(model.BillDate, 11.5f, true);
                    Cell(model.JobNo, 11.0f, false);
                });
            });

            // ---- 3. JOB DETAILS BOX (height 127.5 = 18.1 + 88.2 + 21.2) -------------
            col.Item().Border(B).Column(box =>
            {
                // Material description row
                box.Item().MinHeight(18.1f).PaddingLeft(3.45f).PaddingTop(1.5f).Row(r =>
                {
                    r.AutoItem().Text("Material Description:").FontFamily(Arial).FontSize(9f).Bold();
                    r.RelativeItem().PaddingLeft(10.5f)
                     .Text((model.MaterialDescription ?? "").ToUpper()).FontSize(9f);   // regular, not bold
                });

                // Six-line block. Word tab grid: 0-162.3 | 162.3-334.15 | 334.15-539.7
                void PropRow(ColumnDescriptor c, string label, string? val, float labelW, float colonW)
                {
                    c.Item().MinHeight(13.4f).Row(r =>
                    {
                        r.ConstantItem(labelW).Text(label).FontSize(9f);
                        r.ConstantItem(colonW).Text(":").FontFamily(Arial).FontSize(8.5f).Bold();
                        r.RelativeItem().Text(val ?? "").FontSize(9f);
                    });
                }

                box.Item().MinHeight(88.2f).PaddingTop(6.4f).Row(row =>
                {
                    // Group 1: label at 4.85, colon at 73.7
                    row.ConstantItem(162.3f).PaddingLeft(4.85f).Column(c =>
                    {
                        PropRow(c, "Quantity", model.Quantity, 68.85f, 5.5f);
                        PropRow(c, "Packing Quantity", model.PackingQuantity, 68.85f, 5.5f);
                        PropRow(c, "B/L NO", model.BlNo, 68.85f, 5.5f);
                        PropRow(c, "B/L DT", model.BlDate, 68.85f, 5.5f);
                        PropRow(c, "B/E NO", model.BeNo, 68.85f, 5.5f);
                        PropRow(c, "B/E DT", model.BeDate, 68.85f, 5.5f);
                    });

                    // Group 2: label at 162.3, colon at ~206.6, value at ~213
                    row.ConstantItem(171.85f).Column(c =>
                    {
                        PropRow(c, "Weight", model.Weight, 44.3f, 6.4f);
                        PropRow(c, "INV. NO", model.InvoiceNo, 44.3f, 6.4f);
                        PropRow(c, "INV. DT", model.InvoiceDate, 44.3f, 6.4f);
                        PropRow(c, "Exp NO", model.ExpNo, 44.3f, 6.4f);      // Word has typo "Exp N0"
                        PropRow(c, "Exp DT", model.ExpDate, 44.3f, 6.4f);
                        PropRow(c, "HAWB No", model.HawbNo, 44.3f, 6.4f);
                    });

                    // Group 3: label at 338.25, colon at 400.85
                    row.RelativeItem().PaddingLeft(4.1f).Column(c =>
                    {
                        PropRow(c, "Destination", model.Destination, 62.6f, 5.5f);
                        PropRow(c, "Doc Recv Date", model.DocRecvDate, 62.6f, 5.5f);
                        PropRow(c, "Arrival Date", model.ArrivalDate, 62.6f, 5.5f);
                        PropRow(c, "Delivery Date", model.DeliveryDate, 62.6f, 5.5f);
                        PropRow(c, "LC/TT/BBR No", model.LcNo, 62.6f, 5.5f);
                        PropRow(c, "LC/TT/BBR DT", model.LcDate, 62.6f, 5.5f);
                    });
                });

                // Values strip. NO BorderTop here (Word has no line above it).
                box.Item().MinHeight(21.2f).PaddingTop(8.6f).Row(row =>
                {
                    row.ConstantItem(334.15f).PaddingLeft(4.55f).Row(r =>
                    {
                        r.ConstantItem(102.2f - 4.55f)
                         .Text($"Invoice Value ({model.Currency})").FontFamily(Times).FontSize(9f).Bold();
                        r.RelativeItem()
                         .Text(model.InvoiceValue.HasValue ? N2(model.InvoiceValue.Value) : "").FontSize(9f);
                    });
                    row.RelativeItem().PaddingLeft(4.5f).Row(r =>
                    {
                        r.ConstantItem(112.7f - 4.5f)
                         .Text("Assessable Value (BDT)").FontFamily(Times).FontSize(9f).Bold();
                        r.RelativeItem()
                         .Text(model.AssessableValue.HasValue ? N2(model.AssessableValue.Value) : "").FontSize(9f);
                    });
                });
            });

            // ---- 4/5. CHARGES TABLE + SUMMARY + IN WORDS ----------------------------
            col.Item().BorderLeft(B).BorderRight(B).BorderBottom(B).Column(tableCol =>
            {
                // Header row (15.05)
                tableCol.Item().BorderBottom(B).MinHeight(15.05f).Row(row =>
                {
                    row.RelativeItem(W0).BorderRight(B).PaddingTop(4.75f).PaddingRight(44.85f)
                       .AlignCenter().Text("PARTICULARS").FontFamily(Times).FontSize(8.5f).Bold();
                    row.RelativeItem(W1).BorderRight(B).PaddingTop(4.75f)
                       .AlignCenter().Text("TAKA").FontFamily(Times).FontSize(8.5f).Bold();
                    row.RelativeItem(W2).PaddingTop(4.75f).PaddingLeft(27.75f)
                       .Text("Remarks").FontFamily(Times).FontSize(8.5f).Bold();
                });

                // Body
                tableCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(W0);
                        cols.RelativeColumn(W1);
                        cols.RelativeColumn(W2);
                    });

                    float used = 0f;   // running height of everything above the spacer

                    void Row3(float h, Action<IContainer> c0, Action<IContainer> c1, Action<IContainer> c2)
                    {
                        c0(table.Cell().BorderRight(B).MinHeight(h));
                        c1(table.Cell().BorderRight(B).MinHeight(h));
                        c2(table.Cell().MinHeight(h));
                        used += h;
                    }

                    void Category(string name, List<JobBillPrintChargeItem>? items, decimal subtotal,
                                  float titleH, float titleTop)
                    {
                        // Title row
                        Row3(titleH,
                            c => c.PaddingLeft(4.85f).PaddingTop(titleTop)
                                  .Text(name + ":").FontSize(9.5f).Bold(),
                            Blank, Blank);

                        bool first = true;
                        foreach (var itm in items ?? new List<JobBillPrintChargeItem>())
                        {
                            float h = first ? 16.65f : 10.9f;        // first item has +5.75 gap under the title
                            float padTop = first ? 5.75f : 0f;
                            var amount = F2(itm.Amount);
                            var text = itm.Particulars ?? "";
                            Row3(h,
                                c => c.PaddingTop(padTop).Row(r =>
                                {
                                    r.RelativeItem().PaddingLeft(14.8f).Text(text).FontSize(9f);
                                    r.AutoItem().PaddingRight(3.8f).Text(amount).FontFamily(Times).FontSize(9f);
                                }),
                                Blank, Blank);
                            first = false;
                        }

                        // Subtotal sits in the TAKA column, in its own row under the last item
                        if ((items != null && items.Count > 0) || subtotal > 0)
                        {
                            Row3(11.1f, Blank,
                                c => c.PaddingRight(7.6f).AlignRight()
                                      .Text(F2(subtotal)).FontFamily(Times).FontSize(9.5f).Bold(),
                                Blank);
                        }
                        // Empty group (e.g. Transport): title row only, no blank row (matches Word)
                    }

                    Category("Receiptable", model.ReceiptableItems, model.ReceiptableSubtotal, 19.25f, 2.15f);

                    // Port Charge: NEW row, straight after the Receiptable subtotal
                    //Row3(16.5f,
                    //    c => c.PaddingLeft(14.8f).Text("Port Charge").FontSize(9f),
                    //    c => c.PaddingRight(7.6f).AlignRight()
                    //          .Text(F2(model.PortCharge)).FontFamily(Times).FontSize(9.5f).Bold(),
                    //    Blank);

                    Category("Non Receiptable", model.NonReceiptableItems, model.NonReceiptableSubtotal, 22.5f, 5.4f);
                    Category("Transport", model.TransportItems, model.TransportSubtotal, 17.1f, 0f);
                    Category("Agency Commission", model.AgencyCommissionItems, model.AgencyCommissionSubtotal, 23.3f, 6.25f);

                    // Spacer: fills the rest of Word's 637.3pt table
                    float spacer = Math.Max(0f, TableBudget - FixedRows - used);
                    Row3(spacer, Blank, Blank, Blank);
                });

                // Summary rows: Remarks | Total/Deductions/Net Due | amounts | remarks col
                tableCol.Item().BorderTop(B).Row(sum =>
                {
                    sum.RelativeItem(W0).BorderRight(B).Row(part =>
                    {
                        // Remarks + E. & O.E.   (Word region = 4092 twips)
                        part.RelativeItem(4092f).Column(n =>
                        {
                            n.Item().MinHeight(14.85f).PaddingLeft(4.8f).PaddingTop(1.1f).Text(t =>
                            {
                                t.Span("Remarks: ").FontFamily(Times).FontSize(8.5f).Bold();
                                t.Span(model.Remarks ?? "").FontFamily(Times).FontSize(9f);
                            });
                            n.Item().MinHeight(16.25f).Text("");
                            n.Item().MinHeight(13.3f).PaddingLeft(4.8f)
                             .Text("E. & O.E.").FontFamily(Times).FontSize(8.5f).Bold();
                        });

                        // Labels: LEFT aligned, start 109.85 into the region, dash at +45.65
                        part.RelativeItem(3210f).Column(l =>
                        {
                            void Lbl(string text, float h, float top)
                            {
                                l.Item().MinHeight(h).PaddingLeft(109.85f).PaddingTop(top).Row(r =>
                                {
                                    r.ConstantItem(45.65f).Text(text).FontFamily(Times).FontSize(9f);
                                    r.AutoItem().Text("-").FontFamily(Arial).FontSize(8.5f).Bold();
                                });
                            }
                            Lbl("Total", 14.85f, 0f);
                            Lbl("Deductions", 16.25f, 2.5f);
                            Lbl("Net Due", 13.3f, 2f);
                        });
                    });

                    // Amounts under TAKA (Arial bold 8.5, right aligned; commas here only)
                    sum.RelativeItem(W1).BorderRight(B).Column(t =>
                    {
                        t.Item().BorderBottom(B).MinHeight(14.85f).PaddingTop(1f).PaddingRight(9.85f).AlignRight()
                         .Text(N2(model.TotalAmount)).FontSize(8.5f).Bold();

                        bool hasDed = model.Deductions.HasValue && model.Deductions.Value > 0;
                        t.Item().BorderBottom(B).MinHeight(16.25f).PaddingTop(1.7f)
                         .PaddingRight(hasDed ? 9.85f : 7.4f).AlignRight()
                         .Text(hasDed ? N2(model.Deductions!.Value) : "-").FontSize(8.5f).Bold();

                        t.Item().MinHeight(13.3f).PaddingTop(1f).PaddingRight(9.85f).AlignRight()
                         .Text(N2(model.NetDue)).FontSize(8.5f).Bold();
                    });

                    // Remarks column: empty with matching dividers
                    sum.RelativeItem(W2).Column(r =>
                    {
                        r.Item().BorderBottom(B).MinHeight(14.85f).Text("");
                        r.Item().BorderBottom(B).MinHeight(16.25f).Text("");
                        r.Item().MinHeight(13.3f).Text("");
                    });
                });

                // In Words (16.6). Line under it = tableCol's own BorderBottom.
                tableCol.Item().BorderTop(B).MinHeight(16.6f).PaddingLeft(4.8f).PaddingTop(1.8f).Text(t =>
                {
                    t.Span("In Words: ").FontFamily(Times).FontSize(8.5f).Bold();
                    t.Span(model.InWords ?? "").FontSize(9f);
                });
            });

            // ---- 6. TERMS + SIGNATURE ROW (last row of the table in Word; 52.3) ------
            col.Item().MinHeight(52.3f).Row(row =>
            {
                // Terms: no left/bottom border. One paragraph, wraps at 202.5pt.
                row.RelativeItem(6601f).PaddingLeft(5.05f).PaddingRight(117.5f).PaddingTop(24f)
                   .Text("Terms: Service required as per our Trading Conditions. " +
                         "The account will be correct unless queried within 7 days")
                   .FontFamily(Times).FontSize(9f).LineHeight(1.14f);

                const float SigHeight = 120f; // height of this signature section

                row.RelativeItem(4293f).Border(B).Layers(layers =>
                {
                    // Primary layer: defines the box size, holds "For Formosa Logistics Limited."
                    layers.PrimaryLayer().ExtendVertical().PaddingLeft(3.85f).PaddingTop(2f).Text(t =>
                    {
                        t.Span("For ").FontFamily(Arial).FontSize(9.5f).Bold();
                        t.Span("Formosa Logistics Limited.").FontFamily(Times).FontSize(9.5f).Bold();
                    });

                    // Dashed line: 10% up from the bottom of the section
                    layers.Layer()
                        .AlignBottom()
                        .PaddingBottom(SigHeight * 0.10f)
                        .PaddingLeft(21.05f).PaddingRight(15.4f)
                        .Row(r =>
                        {
                            for (int i = 0; i < 40; i++)
                            {
                                r.RelativeItem().PaddingRight(3f).Height(B).Background(Colors.Black);
                            }
                        });

                    // "Authorised Signatory": below the line, centered, at the bottom of the box
                    layers.Layer()
                        .AlignBottom().AlignCenter()
                        .PaddingBottom(1f)
                        .Text("Authorised Signatory").FontFamily(Times).FontSize(9f);
                });

                // Signature box (full border)
                //row.RelativeItem(4293f).Border(B).Column(sig =>
                //{
                //    sig.Item().PaddingLeft(3.85f).PaddingTop(2f).Text(t =>
                //    {
                //        t.Span("For ").FontFamily(Arial).FontSize(9.5f).Bold();
                //        t.Span("Formosa Logistics Limited.").FontFamily(Times).FontSize(9.5f).Bold();
                //    });

                //    // Real solid line: 178.2 wide, 21.05 in, ~18.15 below the box top
                //    sig.Item().PaddingTop(5.2f).PaddingLeft(21.05f).PaddingRight(15.4f).LineHorizontal(B);

                //    // Authorised Signatory: ~38.2 below the box top
                //    sig.Item().PaddingTop(19f).AlignCenter()
                //       .Text("Authorised Signatory").FontFamily(Times).FontSize(9f);
                //});
            });
        });

        // ---- 7. FOOTER: only the cheque line (Word x = 40.9 -> padding 3.5) ---------
        page.Footer().PaddingLeft(3.5f)
            .Text("Cheques must be crossed and drawn in favour of Formosa Logistics Limited")
            .FontFamily(Times).FontSize(9f);
    }

    //private static void RenderSingleBillPage(PageDescriptor page, JobBillPrintVM model)
    //    {
    //        page.Size(PageSizes.A4);
    //        page.MarginLeft(37.4f);
    //        page.MarginRight(18.2f);
    //        page.MarginTop(23.5f);
    //        page.MarginBottom(15.5f);

    //        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(7.5f).FontColor(Colors.Black));

    //        page.Content().Column(col =>
    //        {
    //            // 1. Header (Company Name on Left, Address on Right)
    //            col.Item().Height(51.35f).Row(row =>
    //            {
    //                row.RelativeItem().PaddingLeft(10.4f)
    //                .Text(model.CompanyName ?? "Formosa Logistics Limited")
    //                   .FontFamily(Arial).FontSize(20.5f).Bold();
    //                row.ConstantItem(109.2f).PaddingTop(5f)
    //                //row.RelativeItem(3.2f).AlignRight().AlignBottom()
    //                .Text(model.CompanyAddress ?? "21, Hazaribagh, Dhaka-1209")
    //                   .FontFamily(Cambria).FontSize(8f).LetterSpacing(0.05f);
    //            });

    //            // 2. Middle Row: Bill To (Left), "BILL" Title (Center), Bill Meta Box (Right)
    //            col.Item().Height(104.45f).Row(row =>
    //            {
    //                // Left: Customer details
    //                row.ConstantItem(229f).Padding(6.6f).Width(152.9f).Column(c =>
    //                {
    //                    c.Item().Text(t=> {
    //                        t.Span("Bill To").FontFamily(Cambria).FontSize(8f).Bold();
    //                        t.Span(":").FontFamily(Arial).FontSize(8f).Bold();
    //                    });

    //                    c.Item().PaddingTop(2.75f)
    //                        .Text(model.CustomerName ?? "").FontSize(8.5f).LetterSpacing(0.04f);

    //                    if (!string.IsNullOrWhiteSpace(model.CustomerAddress))
    //                    {
    //                        c.Item().PaddingTop(4.55f).Text(BreakAddress(model.CustomerAddress, 4))
    //                            .LineHeight(1.09f).FontSize(8f).FontFamily(Cambria).LetterSpacing(0.05f);
    //                    }
    //                });

    //                // Center: "BILL" title (Large serif font)
    //                row.ConstantItem(123.1f).AlignCenter().AlignMiddle().PaddingTop(43.25f)
    //                    .Text("BILL").FontFamily(Georgia).FontSize(20.5f).LetterSpacing(0.2f);

    //                // Right: Bill meta box (table-like bordered grid)
    //                row.ConstantItem(33.9f).PaddingTop(15f).Column(l =>
    //                {
    //                    foreach (var s in new[] { "Bill No.", "Date", "JOB No." })
    //                    {
    //                        l.Item().Height(11.7f).Row(r =>
    //                        {
    //                            r.RelativeItem().Text(s).FontFamily(Cambria).FontSize(7.5f);
    //                            r.AutoItem().Text(":").FontFamily(Cambria).FontSize(7.5f);
    //                        });
    //                    }
    //                });

    //                row.ConstantItem(150.7f).PaddingTop(9.95f).Border(BorderWidth).Column(m =>
    //                {
    //                    void Cell(string? v, float h, bool line)
    //                    {
    //                        var c = m.Item();
    //                        if (line) c = c.BorderBottom(BorderWidth);
    //                        c.Height(h).PaddingLeft(1.8f).AlignMiddle()
    //                         .Text(v ?? "").FontFamily(Cambria).FontSize(7.5f);
    //                    }
    //                    Cell(model.BillNo, 11.5f, true);
    //                    Cell(model.BillDate, 11.5f, true);
    //                    Cell(model.JobNo, 11.0f, false);
    //                });
    //                //row.ConstantItem(33.9f).Border(BorderWidth).BorderColor(Colors.Black).Table(metaTable =>
    //                //{
    //                //    metaTable.ColumnsDefinition(cols =>
    //                //    {
    //                //        cols.ConstantColumn(52); // Label
    //                //        cols.RelativeColumn();   // Value
    //                //    });

    //                //    void RenderMetaCell(string label, string? val, bool isLast = false)
    //                //    {
    //                //        var lbl = metaTable.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black);
    //                //        var v = metaTable.Cell();

    //                //        if (!isLast)
    //                //        {
    //                //            lbl.BorderBottom(BorderWidth).BorderColor(Colors.Black)
    //                //               .PaddingVertical(1.5f).PaddingHorizontal(3).Text(label).FontSize(7.5f);
    //                //            v.BorderBottom(BorderWidth).BorderColor(Colors.Black)
    //                //             .PaddingVertical(1.5f).PaddingHorizontal(3).Text(val ?? "").FontSize(7.5f);
    //                //        }
    //                //        else
    //                //        {
    //                //            lbl.PaddingVertical(1.5f).PaddingHorizontal(3).Text(label).FontSize(7.5f);
    //                //            v.PaddingVertical(1.5f).PaddingHorizontal(3).Text(val ?? "").FontSize(7.5f);
    //                //        }
    //                //    }

    //                //    RenderMetaCell("Bill No.  :", model.BillNo);
    //                //    RenderMetaCell("Date       :", model.BillDate);
    //                //    RenderMetaCell("JOB No. :", model.JobNo, isLast: true);
    //                //});
    //            });

    //            // 4. Job Details Box (Material Description, 3-Column Shipment Details, Values Strip)
    //            col.Item().Border(BorderWidth).Column(boxCol =>
    //            {
    //                // Material Description
    //                boxCol.Item().PaddingTop(3.5f).PaddingLeft(3.45f).MinHeight(18.1f).Row(row =>
    //                {
    //                    row.AutoItem().Text("Material Description: ").FontFamily(Arial).FontSize(8.5f).Bold();
    //                    row.RelativeItem().PaddingLeft(10.5f)
    //                        .Text((model.MaterialDescription ?? "").ToUpper()).FontSize(9f);
    //                });

    //                void PropRow(ColumnDescriptor c, string label, string? val, float labelW, float colonW)
    //                {
    //                    c.Item().MinHeight(13.4f).Row(r =>
    //                    {
    //                        r.ConstantItem(labelW).Text(label).FontSize(9f);
    //                        r.ConstantItem(colonW).Text(":").FontFamily(Arial).FontSize(8.5f).Bold();
    //                        r.RelativeItem().Text(val ?? "").FontSize(9f);
    //                    });
    //                }

    //                // 3-Column Shipment Particulars
    //                boxCol.Item().MinHeight(88.2f).PaddingTop(6.4f).Row(row =>
    //                {
    //                    //void RenderPropRow(ColumnDescriptor c, string label, string? val, float labelWidth)
    //                    //{
    //                    //    c.Item().PaddingVertical(0.5f).Row(r =>
    //                    //    {
    //                    //        r.AutoItem().Width(labelWidth).Text(label).FontSize(7.5f);
    //                    //        r.AutoItem().Width(6).Text(":").FontSize(7.5f);
    //                    //        r.RelativeItem().PaddingLeft(2).Text(val ?? "").FontSize(7.5f);
    //                    //    });
    //                    //}

    //                    // Column 1
    //                    row.ConstantItem(162.3f).PaddingLeft(4.85f).Column(c =>
    //                    {
    //                        PropRow(c, "Quantity", model.Quantity, 68.85f, 5.5f);
    //                        PropRow(c, "Packing Quantity", model.PackingQuantity, 68.85f, 5.5f);
    //                        PropRow(c, "B/L NO", model.BlNo, 68.85f, 5.5f);
    //                        PropRow(c, "B/L DT", model.BlDate, 68.85f, 5.5f);
    //                        PropRow(c, "B/E NO", model.BeNo, 68.85f, 5.5f);
    //                        PropRow(c, "B/E DT", model.BeDate, 68.85f, 5.5f);
    //                    });

    //                    // Column 2
    //                    row.RelativeItem(171.85f).Column(c =>
    //                    {
    //                        PropRow(c, "Weight", model.Weight, 44.3f, 6.4f);
    //                        PropRow(c, "INV. NO", model.InvoiceNo, 44.3f, 6.4f);
    //                        PropRow(c, "INV. DT", model.InvoiceDate, 44.3f, 6.4f);
    //                        PropRow(c, "Exp NO", model.ExpNo, 44.3f, 6.4f);
    //                        PropRow(c, "Exp DT", model.ExpDate, 44.3f, 6.4f);
    //                        PropRow(c, "HAWB No", model.HawbNo, 44.3f, 6.4f);
    //                    });

    //                    // Column 3
    //                    row.RelativeItem(3.5f).Column(c =>
    //                    {
    //                        PropRow(c, "Destination", model.Destination, 62.6f, 5.5f);
    //                        PropRow(c, "Doc Recv Date", model.DocRecvDate, 62.6f, 5.5f);
    //                        PropRow(c, "Arrival Date", model.ArrivalDate, 62.6f, 5.5f);
    //                        PropRow(c, "Delivery Date", model.DeliveryDate, 62.6f, 5.5f);
    //                        PropRow(c, "LC/TT/BBR No", model.LcNo, 62.6f, 5.5f);
    //                        PropRow(c, "LC/TT/BBR DT", model.LcDate, 62.6f, 5.5f);
    //                    });
    //                });

    //                // Values Strip (Separated by horizontal top and bottom border)
    //                boxCol.Item().MinHeight(21.2f).PaddingTop(8.6f).Row(row =>
    //                {
    //                    // Invoice Value
    //                    row.ConstantItem(334.15f).PaddingLeft(4.55f).Row(r =>
    //                    {
    //                        r.ConstantItem(102.2f-4.55f)
    //                            .Text($"Invoice Value ({model.Currency})").FontFamily(Times).FontSize(8.5f).Bold();
    //                        r.RelativeItem()
    //                            .Text(model.InvoiceValue.HasValue ? N2(model.InvoiceValue.Value) : "").FontSize(9f);
    //                    });

    //                    // Assessable Value
    //                    row.RelativeItem().PaddingLeft(4.5f).Row(r =>
    //                    {
    //                        r.ConstantItem(112.7f-4.5f)
    //                            .Text("Assessable Value (BDT)").FontFamily(Times).FontSize(8.5f).Bold();
    //                        r.RelativeItem()
    //                            .Text(model.AssessableValue.HasValue ? N2(model.AssessableValue.Value) : "").FontSize(9f);
    //                    });
    //                });
    //            });

    //            // Calculate dynamic spacer height so charges table spans page gracefully
    //            int totalItemRows = (model.ReceiptableItems?.Count ?? 0) +
    //                                (model.NonReceiptableItems?.Count ?? 0) +
    //                                (model.TransportItems?.Count ?? 0) +
    //                                (model.AgencyCommissionItems?.Count ?? 0) + 4;
    //            float dynamicSpacerHeight = Math.Max(120f, 410f - (totalItemRows * 13.5f));

    //            // 5. Main Charges Table
    //            col.Item().BorderLeft(BorderWidth).BorderRight(BorderWidth).BorderBottom(BorderWidth).Column(tableCol =>
    //            {
    //                tableCol.Item().BorderBottom(BorderWidth).MinHeight(15.05f).BorderColor(Colors.Black).Row(row =>
    //                {
    //                    row.RelativeItem(W0).BorderRight(BorderWidth).PaddingTop(4.75f).PaddingRight(44.85f).AlignCenter().Text("PARTICULARS").FontFamily(Times).FontSize(8.5f).Bold();
    //                    row.RelativeItem(W1).BorderRight(BorderWidth).PaddingTop(4.75f).AlignCenter().Text("TAKA").FontFamily(Times).FontSize(8.5f).Bold();
    //                    row.RelativeItem(W2).PaddingTop(4.75f).PaddingLeft(27.75f).AlignCenter().Text("Remarks").FontFamily(Times).FontSize(8.5f).Bold();
    //                });

    //                // Table Body
    //                tableCol.Item().Table(table =>
    //                {
    //                    table.ColumnsDefinition(cols =>
    //                    {
    //                        cols.RelativeColumn(W0); // PARTICULARS
    //                        cols.RelativeColumn(W1); // TAKA
    //                        cols.RelativeColumn(W2); // Remarks
    //                    });

    //                    float used = 0f;

    //                    void Row3(float h, Action<IContainer> c0, Action<IContainer> c1, Action<IContainer> c2)
    //                    {
    //                        c0(table.Cell().BorderRight(BorderWidth).MinHeight(h));
    //                        c1(table.Cell().BorderRight(BorderWidth).MinHeight(h));
    //                        c2(table.Cell().MinHeight(h));
    //                        used += h;
    //                    }

    //                    void RenderCategory(string name, List<JobBillPrintChargeItem> items, decimal subtotal, float titleH, float titleTop)
    //                    {
    //                        Row3(titleH,
    //                            c => c.PaddingLeft(4.85f).PaddingTop(titleTop)
    //                                .Text(name + ":").FontSize(9.5f).Bold(),
    //                            Blank, Blank);

    //                        bool first = true;
    //                        foreach(var itm in items ?? new List<JobBillPrintChargeItem>())
    //                        {
    //                            float h = first ? 16.65f : 10.9f;

    //                            float padTop = first ? 5.75f : 0f;
    //                            var amount = F2(itm.Amount);
    //                            var text = itm.Particulars ?? "";
    //                            Row3(h,
    //                                c => c.PaddingTop(padTop).Row(r =>
    //                                {
    //                                    r.RelativeItem().PaddingLeft(14.8f).Text(text).FontSize(9f);
    //                                    r.AutoItem().PaddingRight(3.8f).Text(amount).FontFamily(Times).FontSize(9f);
    //                                }),
    //                                Blank, Blank);
    //                            first = false;
    //                        }

    //                        if((items != null &&  items.Count>0)||subtotal > 0)
    //                        {
    //                            Row3(11.1f, Blank,
    //                                c => c.PaddingRight(7.6f).AlignRight()
    //                                      .Text(F2(subtotal)).FontFamily(Times).FontSize(9.5f).Bold(),
    //                                Blank);
    //                        }
    //                    }

    //                    RenderCategory("Receiptable", model.ReceiptableItems, model.ReceiptableSubtotal, 19.25f, 2.15f);
    //                    Row3(16.5f,
    //                        c => c.PaddingLeft(14.8f).Text("Port Charge").FontSize(9f),
    //                        c => c.PaddingRight(7.6f).AlignRight()
    //                              .Text(F2(model.)).FontFamily(Times).FontSize(9.5f).Bold(),
    //                        Blank);

    //                    RenderCategory("Non Receiptable", model.NonReceiptableItems, model.NonReceiptableSubtotal, forceShow: true);
    //                    RenderCategory("Transport", model.TransportItems, model.TransportSubtotal, forceShow: true);
    //                    RenderCategory("Agency Commission", model.AgencyCommissionItems, model.AgencyCommissionSubtotal, forceShow: true);

    //                    // Vertical spacing to span the table down the page
    //                    table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).MinHeight(dynamicSpacerHeight).Text("");
    //                    table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).MinHeight(dynamicSpacerHeight).Text("");
    //                    table.Cell().MinHeight(dynamicSpacerHeight).Text("");
    //                });

    //                // Divider before Grand Totals & Remarks
    //                tableCol.Item().BorderTop(BorderWidth).BorderColor(Colors.Black).Row(summaryRow =>
    //                {
    //                    // Left Column (6.8f): Remarks & E. & O.E. on left, Total/Deductions/Net Due labels right-aligned
    //                    summaryRow.RelativeItem(6.8f).BorderRight(BorderWidth).BorderColor(Colors.Black).Row(partRow =>
    //                    {
    //                        // Notes area (aligned with Total, Deductions, and Net Due)
    //                        partRow.RelativeItem(4.0f).PaddingHorizontal(3).Column(notesCol =>
    //                        {
    //                            // Row 1 (aligned with Total)
    //                            notesCol.Item().PaddingVertical(2).Row(r =>
    //                            {
    //                                r.AutoItem().Text("Remarks: ").FontSize(7.5f).Bold();
    //                                r.RelativeItem().Text(model.Remarks ?? "").FontSize(7.5f);
    //                            });

    //                            // Row 2 (aligned with Deductions)
    //                            notesCol.Item().PaddingVertical(2).Text("");

    //                            // Row 3 (aligned with Net Due)
    //                            notesCol.Item().PaddingVertical(2).Text("E. & O.E.").FontSize(7.5f).Bold();
    //                        });

    //                        // Summary Labels right-aligned with trailing dash '-'
    //                        partRow.RelativeItem(2.8f).Column(labelCol =>
    //                        {
    //                            labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).Row(r =>
    //                            {
    //                                r.RelativeItem().AlignRight().Text("Total").FontSize(7.5f);
    //                                r.AutoItem().PaddingLeft(6).Text("-").FontSize(7.5f);
    //                            });
    //                            labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).Row(r =>
    //                            {
    //                                r.RelativeItem().AlignRight().Text("Deductions").FontSize(7.5f);
    //                                r.AutoItem().PaddingLeft(6).Text("-").FontSize(7.5f);
    //                            });
    //                            labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).Row(r =>
    //                            {
    //                                r.RelativeItem().AlignRight().Text("Net Due").FontSize(7.5f);
    //                                r.AutoItem().PaddingLeft(6).Text("-").FontSize(7.5f);
    //                            });
    //                        });
    //                    });

    //                    // Middle Column (1.6f): Totals amounts directly under TAKA
    //                    summaryRow.RelativeItem(1.6f).BorderRight(BorderWidth).BorderColor(Colors.Black).Column(takaCol =>
    //                    {
    //                        takaCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(4).AlignRight()
    //                               .Text(model.TotalAmount.ToString("#,##0.00")).FontSize(7.5f).Bold();
    //                        takaCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(4).AlignCenter()
    //                               .Text(model.Deductions.HasValue && model.Deductions.Value > 0 ? model.Deductions.Value.ToString("#,##0.00") : "-").FontSize(7.5f);
    //                        takaCol.Item().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
    //                               .Text(model.NetDue.ToString("#,##0.00")).FontSize(7.5f).Bold();
    //                    });

    //                    // Right Column (1.6f): Remarks column with matching horizontal dividers
    //                    summaryRow.RelativeItem(1.6f).Column(remCol =>
    //                    {
    //                        remCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).Text("");
    //                        remCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).Text("");
    //                        remCol.Item().PaddingVertical(2).Text("");
    //                    });
    //                });
    //            });

    //            // 6. In Words Box
    //            col.Item().BorderLeft(BorderWidth).BorderRight(BorderWidth).BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(4).Row(row =>
    //            {
    //                row.AutoItem().Text("In Words: ").FontSize(7.5f).Bold();
    //                row.RelativeItem().Text(model.InWords ?? "").FontSize(7.5f);
    //            });
    //        });

    //        // 7. Footer
    //        page.Footer().PaddingTop(6).Row(footerRow =>
    //        {
    //            // Left: Terms & Cheque notice
    //            footerRow.RelativeItem(6.4f).Column(termsCol =>
    //            {
    //                termsCol.Item().Text("Terms: Service required as per our Trading Conditions.").FontSize(7.5f);
    //                termsCol.Item().Text("The account will be correct unless queried within 7 days").FontSize(7.5f);

    //                termsCol.Item().PaddingTop(16).Text("Cheques must be crossed and drawn in favour of Formosa Logistics Limited").FontSize(7.5f);
    //            });

    //            // Right: Signature Box
    //            footerRow.RelativeItem(3.6f).Border(BorderWidth).BorderColor(Colors.Black).Padding(4).Column(sigCol =>
    //            {
    //                sigCol.Item().Text("For Formosa Logistics Limited.").FontSize(8f).Bold();
    //                sigCol.Item().MinHeight(30).Text("");
    //                sigCol.Item().AlignCenter().Text("....................................................................").FontSize(7.5f);
    //                sigCol.Item().AlignCenter().Text("Authorised Signatory").FontSize(8f);
    //            });
    //        });
    //    }

    private static string BreakAddress(string? address, int wordsPerLine = 4)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;

            if (address.Contains("\n") || address.Contains("\r"))
                return address.Trim();

            var words = address.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= wordsPerLine)
                return address.Trim();

            var lines = new List<string>();
            for (int i = 0; i < words.Length; i += wordsPerLine)
            {
                lines.Add(string.Join(" ", words.Skip(i).Take(wordsPerLine)));
            }
            return string.Join("\n", lines);
        }

        public static string ConvertToWords(decimal amount)
        {
            if (amount <= 0) return "Taka Zero Only.";

            long whole = (long)Math.Truncate(amount);
            int paisa = (int)Math.Round((amount - whole) * 100);

            string words = ConvertWholeNumber(whole);
            if (paisa > 0)
            {
                string paisaWords = ConvertWholeNumber(paisa);
                return $"Taka {words} and {paisaWords} Paisa Only.";
            }

            return $"Taka {words} Only.";
        }

        private static string ConvertWholeNumber(long number)
        {
            if (number == 0) return "Zero";
            if (number < 0) return "Minus " + ConvertWholeNumber(Math.Abs(number));

            string[] units = {
                "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
                "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
            };

            string[] tens = {
                "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
            };

            var sb = new StringBuilder();

            if (number >= 10000000) // 1 Crore
            {
                sb.Append(ConvertWholeNumber(number / 10000000)).Append(" Crore ");
                number %= 10000000;
            }

            if (number >= 100000) // 1 Lakh
            {
                sb.Append(ConvertWholeNumber(number / 100000)).Append(" Lakh ");
                number %= 100000;
            }

            if (number >= 1000)
            {
                sb.Append(ConvertWholeNumber(number / 1000)).Append(" Thousand ");
                number %= 1000;
            }

            if (number >= 100)
            {
                sb.Append(ConvertWholeNumber(number / 100)).Append(" Hundred ");
                number %= 100;
            }

            if (number > 0)
            {
                if (number < 20)
                {
                    sb.Append(units[number]);
                }
                else
                {
                    sb.Append(tens[number / 10]);
                    if (number % 10 > 0)
                    {
                        sb.Append("-").Append(units[number % 10]);
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}
