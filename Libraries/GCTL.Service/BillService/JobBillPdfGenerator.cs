using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        private static void RenderSingleBillPage(PageDescriptor page, JobBillPrintVM model)
        {
            const float BorderWidth = 0.5f;

            page.Size(PageSizes.A4);
            page.MarginHorizontal(24);
            page.MarginVertical(20);
            page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(8f).FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                // 1. Header (Company Name & Address)
                col.Item().Row(row =>
                {
                    row.RelativeItem(7).Text(model.CompanyName ?? "Formosa Logistics Limited")
                       .FontSize(17).Bold();
                    row.RelativeItem(3).AlignRight().Text(model.CompanyAddress ?? "21, Hazaribagh, Dhaka-1209")
                       .FontSize(8.5f);
                });

                // 2. Customer Info (Left) & Bill Metadata (Right) - generous padding after Company Header
                col.Item().PaddingTop(12).Row(row =>
                {
                    // Left: Customer details (Not bold, clean 4-word address wrapping)
                    row.RelativeItem(6.2f).Column(c =>
                    {
                        c.Item().Text("Bill To:").FontSize(8.5f).Bold();
                        c.Item().PaddingTop(1).Text(model.CustomerName ?? "").FontSize(8.5f);
                        if (!string.IsNullOrWhiteSpace(model.CustomerAddress))
                        {
                            c.Item().PaddingTop(1).Text(BreakAddress(model.CustomerAddress, 4)).FontSize(8f);
                        }
                    });

                    // Right: Bill meta box (bordered)
                    row.RelativeItem(3.8f).Border(BorderWidth).BorderColor(Colors.Black).Column(c =>
                    {
                        void RenderMetaRow(string label, string? val, bool isLast = false)
                        {
                            var item = c.Item();
                            if (!isLast) item = item.BorderBottom(BorderWidth).BorderColor(Colors.Black);
                            item.PaddingVertical(1.5f).PaddingHorizontal(4).Row(r =>
                            {
                                r.AutoItem().Width(55).Text(label).FontSize(8f).Bold();
                                r.AutoItem().Width(8).Text(":").FontSize(8f).Bold();
                                r.RelativeItem().Text(val ?? "").FontSize(8f);
                            });
                        }

                        RenderMetaRow("Bill No.", model.BillNo);
                        RenderMetaRow("Date", model.BillDate);
                        RenderMetaRow("JOB No.", model.JobNo, isLast: true);
                    });
                });

                // 3. Title "BILL" (centered, placed below the Bill To and Bill No section)
                col.Item().PaddingTop(4).PaddingBottom(4).AlignCenter().Text("BILL")
                   .FontSize(19).Bold();

                // 4. Job Details Box (Material Description, 3-Column Particulars, and Values Bar)
                col.Item().Border(BorderWidth).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(4).Column(boxCol =>
                {
                    // Material Description (label bold, description text regular)
                    boxCol.Item().Row(row =>
                    {
                        row.AutoItem().Text("Material Description: ").FontSize(8f).Bold();
                        row.RelativeItem().Text(model.MaterialDescription ?? "").FontSize(8f);
                    });

                    // Gap between Material Description and Particulars
                    boxCol.Item().PaddingTop(4);

                    // 3-Column Shipment / Job Particulars
                    boxCol.Item().Row(row =>
                    {
                        void RenderPropRow(ColumnDescriptor c, string label, string? val, float labelWidth = 72)
                        {
                            c.Item().PaddingVertical(0.8f).Row(r =>
                            {
                                r.AutoItem().Width(labelWidth).Text(label).FontSize(7.5f).Bold();
                                r.AutoItem().Width(8).Text(":").FontSize(7.5f).Bold();
                                r.RelativeItem().Text(val ?? "").FontSize(7.5f);
                            });
                        }

                        // Column 1
                        row.RelativeItem(3.4f).Column(c =>
                        {
                            RenderPropRow(c, "Quantity", model.Quantity, 75);
                            RenderPropRow(c, "Packing Quantity", model.PackingQuantity, 75);
                            RenderPropRow(c, "B/L NO", model.BlNo, 75);
                            RenderPropRow(c, "B/L DT", model.BlDate, 75);
                            RenderPropRow(c, "B/E NO", model.BeNo, 75);
                            RenderPropRow(c, "B/E DT", model.BeDate, 75);
                        });

                        // Column 2
                        row.RelativeItem(3.2f).Column(c =>
                        {
                            RenderPropRow(c, "Weight", model.Weight, 55);
                            RenderPropRow(c, "INV. NO", model.InvoiceNo, 55);
                            RenderPropRow(c, "INV. DT", model.InvoiceDate, 55);
                            RenderPropRow(c, "Exp NO", model.ExpNo, 55);
                            RenderPropRow(c, "Exp DT", model.ExpDate, 55);
                            RenderPropRow(c, "HAWB No", model.HawbNo, 55);
                        });

                        // Column 3
                        row.RelativeItem(3.4f).Column(c =>
                        {
                            RenderPropRow(c, "Destination", model.Destination, 75);
                            RenderPropRow(c, "Doc Recv Date", model.DocRecvDate, 75);
                            RenderPropRow(c, "Arrival Date", model.ArrivalDate, 75);
                            RenderPropRow(c, "Delivery Date", model.DeliveryDate, 75);
                            RenderPropRow(c, "LC/TT/BBR No", model.LcNo, 75);
                            RenderPropRow(c, "LC/TT/BBR DT", model.LcDate, 75);
                        });
                    });

                    // Gap between Particulars and Values Bar
                    boxCol.Item().PaddingTop(4);

                    // Values Bar (Invoice Value & Assessable Value - seamlessly aligned with Column 3)
                    boxCol.Item().Row(row =>
                    {
                        // Columns 1 & 2 width (3.4 + 3.2 = 6.6)
                        row.RelativeItem(6.6f).Row(r =>
                        {
                            r.AutoItem().Text($"Invoice Value ({model.Currency})").FontSize(8f).Bold();
                            r.RelativeItem().PaddingLeft(25).Text(model.InvoiceValue.HasValue ? model.InvoiceValue.Value.ToString("#,##0.00") : "").FontSize(8f).Bold();
                        });

                        // Column 3 width (3.4) - starts where LC/TT/BBR DT starts
                        row.RelativeItem(3.4f).Row(r =>
                        {
                            r.AutoItem().Text("Assessable Value (BDT)").FontSize(8f).Bold();
                            r.RelativeItem().PaddingLeft(15).Text(model.AssessableValue.HasValue ? model.AssessableValue.Value.ToString("#,##0.00") : "").FontSize(8f).Bold();
                        });
                    });
                });

                // Calculate dynamic spacer height so the charges table expands to fill the full A4 page seamlessly
                int totalItemRows = (model.ReceiptableItems?.Count ?? 0) +
                                    (model.NonReceiptableItems?.Count ?? 0) +
                                    (model.TransportItems?.Count ?? 0) +
                                    (model.AgencyCommissionItems?.Count ?? 0) + 4;
                float dynamicSpacerHeight = Math.Max(120f, 430f - (totalItemRows * 13.5f));

                // 5. Main Charges Table (NO gap above it, shares bottom border of Job Details Box)
                col.Item().BorderLeft(BorderWidth).BorderRight(BorderWidth).BorderBottom(BorderWidth).BorderColor(Colors.Black).Column(tableCol =>
                {
                    // Table Header
                    tableCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).Row(row =>
                    {
                        row.RelativeItem(6.6f).BorderRight(BorderWidth).BorderColor(Colors.Black).Padding(2.5f).AlignCenter().Text("PARTICULARS").FontSize(8f).Bold();
                        row.RelativeItem(1.8f).BorderRight(BorderWidth).BorderColor(Colors.Black).Padding(2.5f).AlignCenter().Text("TAKA").FontSize(8f).Bold();
                        row.RelativeItem(1.6f).Padding(2.5f).AlignCenter().Text("Remarks").FontSize(8f).Bold();
                    });

                    // Table Body
                    tableCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(6.6f); // PARTICULARS
                            cols.RelativeColumn(1.8f); // TAKA
                            cols.RelativeColumn(1.6f); // Remarks
                        });

                        void RenderCategory(string categoryName, List<JobBillPrintChargeItem> items, decimal subtotal, bool forceShow = false)
                        {
                            if (!forceShow && (items == null || items.Count == 0) && subtotal == 0)
                                return;

                            // Category Header Row
                            table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingTop(2).PaddingBottom(1).PaddingHorizontal(4)
                                 .Text(categoryName + ":").FontSize(8f).Bold();
                            table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).Text("");
                            table.Cell().Text("");

                            if (items != null && items.Count > 0)
                            {
                                foreach (var itm in items)
                                {
                                    // Particulars: Item name on left, item rate/amount at right end of PARTICULARS
                                    table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).PaddingLeft(14).PaddingRight(6).Row(r =>
                                    {
                                        r.RelativeItem().Text(itm.Particulars ?? "").FontSize(7.5f);
                                        r.AutoItem().AlignRight().Text(itm.Amount.ToString("#,##0.00")).FontSize(7.5f);
                                    });

                                    // TAKA: empty for head row
                                    table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).Text("");

                                    // Remarks: empty
                                    table.Cell().Text("");
                                }

                                // Sub Total: one row below the last head
                                table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).Text("");
                                table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).PaddingHorizontal(4).AlignRight()
                                     .Text(subtotal.ToString("#,##0.00")).FontSize(8f).Bold();
                                table.Cell().Text("");
                            }
                            else
                            {
                                // Empty group row (e.g. Transport when empty)
                                table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).Text("");
                                var takaCell = table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).PaddingHorizontal(4).AlignRight();
                                if (subtotal > 0)
                                {
                                    takaCell.Text(subtotal.ToString("#,##0.00")).FontSize(8f).Bold();
                                }
                                else
                                {
                                    takaCell.Text("");
                                }
                                table.Cell().Text("");
                            }
                        }

                        RenderCategory("Receiptable", model.ReceiptableItems, model.ReceiptableSubtotal, forceShow: true);
                        RenderCategory("Non Receiptable", model.NonReceiptableItems, model.NonReceiptableSubtotal, forceShow: true);
                        RenderCategory("Transport", model.TransportItems, model.TransportSubtotal, forceShow: true);
                        RenderCategory("Agency Commission", model.AgencyCommissionItems, model.AgencyCommissionSubtotal, forceShow: true);

                        // Dynamic vertical spacing row to fill the full A4 page gracefully from top to bottom
                        table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).MinHeight(dynamicSpacerHeight).Text("");
                        table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).MinHeight(dynamicSpacerHeight).Text("");
                        table.Cell().MinHeight(dynamicSpacerHeight).Text("");
                    });

                    // Divider before Grand Totals & Remarks
                    tableCol.Item().BorderTop(BorderWidth).BorderColor(Colors.Black).Row(summaryRow =>
                    {
                        // Left Column (6.6f): Remarks & E. & O.E. on left, Total/Deductions/Net Due labels right-aligned at end of Particulars
                        summaryRow.RelativeItem(6.6f).BorderRight(BorderWidth).BorderColor(Colors.Black).Row(partRow =>
                        {
                            // Notes area (3 rows aligned with Total, Deductions, and Net Due so E. & O.E. aligns with Net Due)
                            partRow.RelativeItem(3.6f).PaddingHorizontal(3).Column(notesCol =>
                            {
                                // Row 1 (aligned with Total)
                                notesCol.Item().PaddingVertical(2).Row(r =>
                                {
                                    r.AutoItem().Text("Remarks: ").FontSize(8f).Bold();
                                    r.RelativeItem().Text(model.Remarks ?? "").FontSize(8f);
                                });

                                // Row 2 (aligned with Deductions)
                                notesCol.Item().PaddingVertical(2).Text("");

                                // Row 3 (aligned with Net Due)
                                notesCol.Item().PaddingVertical(2).Text("E. & O.E.").FontSize(8f).Bold();
                            });

                            // Labels at end of Particulars (Clean with NO horizontal dividers)
                            partRow.RelativeItem(3.0f).Column(labelCol =>
                            {
                                labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                        .Text("Total :").FontSize(8f).Bold();
                                labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                        .Text("Deductions :").FontSize(8f);
                                labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                        .Text("Net Due :").FontSize(8f).Bold();
                            });
                        });

                        // Middle Column (1.8f): Totals amounts directly under TAKA
                        summaryRow.RelativeItem(1.8f).BorderRight(BorderWidth).BorderColor(Colors.Black).Column(takaCol =>
                        {
                            takaCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                   .Text(model.TotalAmount.ToString("#,##0.00")).FontSize(8f).Bold();
                            takaCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                   .Text(model.Deductions.HasValue && model.Deductions.Value > 0 ? model.Deductions.Value.ToString("#,##0.00") : "-").FontSize(8f);
                            takaCol.Item().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                   .Text(model.NetDue.ToString("#,##0.00")).FontSize(8f).Bold();
                        });

                        // Right Column (1.6f): Remarks column with horizontal divider lines matching Total and Deductions
                        summaryRow.RelativeItem(1.6f).Column(remCol =>
                        {
                            remCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).Text("");
                            remCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).Text("");
                            remCol.Item().PaddingVertical(2).Text("");
                        });
                    });
                });

                // 6. In Words Box (NO gap above it, shares bottom border of Main Charges Table)
                col.Item().BorderLeft(BorderWidth).BorderRight(BorderWidth).BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(4).Row(row =>
                {
                    row.AutoItem().Text("In Words: ").FontSize(8f).Bold();
                    row.RelativeItem().Text(model.InWords ?? "").FontSize(8f);
                });
            });

            // 7. Footer (Pinned to bottom of full A4 page: Trading Conditions & Signatures)
            page.Footer().PaddingTop(6).Row(footerRow =>
            {
                // Left: Terms
                footerRow.RelativeItem(6.2f).Column(termsCol =>
                {
                    termsCol.Item().Text("Terms: Service required as per our Trading Conditions.").FontSize(7.5f);
                    termsCol.Item().Text("The account will be correct unless queried within 7 days").FontSize(7.5f);

                    termsCol.Item().PaddingTop(18).Text("Cheques must be crossed and drawn in favour of Formosa Logistics Limited").FontSize(7.5f);
                });

                // Right: Signature Box
                footerRow.RelativeItem(3.8f).Border(BorderWidth).BorderColor(Colors.Black).Padding(4).Column(sigCol =>
                {
                    sigCol.Item().Text("For Formosa Logistics Limited.").FontSize(8f).Bold();
                    sigCol.Item().MinHeight(30).Text("");
                    sigCol.Item().AlignCenter().Text("....................................................................").FontSize(7.5f);
                    sigCol.Item().AlignCenter().Text("Authorised Signatory").FontSize(8f).Bold();
                });
            });
        }

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
