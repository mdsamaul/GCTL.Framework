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
            page.MarginTop(18);
            page.MarginBottom(16);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(7.5f).FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                // 1. Header (Company Name on Left, Address on Right)
                col.Item().Row(row =>
                {
                    row.RelativeItem(6.8f).Text(model.CompanyName ?? "Formosa Logistics Limited")
                       .FontSize(20).Bold();
                    row.RelativeItem(3.2f).AlignRight().AlignBottom().Text(model.CompanyAddress ?? "21, Hazaribagh, Dhaka-1209")
                       .FontSize(8f);
                });

                // 2. Middle Row: Bill To (Left), "BILL" Title (Center), Bill Meta Box (Right)
                col.Item().PaddingTop(14).Row(row =>
                {
                    // Left: Customer details
                    row.RelativeItem(3.5f).Column(c =>
                    {
                        c.Item().Text("Bill To:").FontSize(8.5f).Bold();
                        c.Item().PaddingTop(1).Text(model.CustomerName ?? "").FontSize(8.5f).SemiBold();
                        if (!string.IsNullOrWhiteSpace(model.CustomerAddress))
                        {
                            c.Item().PaddingTop(1).Text(BreakAddress(model.CustomerAddress, 4)).FontSize(7.5f);
                        }
                    });

                    // Center: "BILL" title (Large serif font)
                    row.RelativeItem(3.0f).AlignCenter().AlignMiddle().Text("BILL")
                       .FontFamily("Times New Roman").FontSize(24).Bold();

                    // Right: Bill meta box (table-like bordered grid)
                    row.RelativeItem(3.5f).Border(BorderWidth).BorderColor(Colors.Black).Table(metaTable =>
                    {
                        metaTable.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(52); // Label
                            cols.RelativeColumn();   // Value
                        });

                        void RenderMetaCell(string label, string? val, bool isLast = false)
                        {
                            var lbl = metaTable.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black);
                            var v = metaTable.Cell();

                            if (!isLast)
                            {
                                lbl.BorderBottom(BorderWidth).BorderColor(Colors.Black)
                                   .PaddingVertical(1.5f).PaddingHorizontal(3).Text(label).FontSize(7.5f);
                                v.BorderBottom(BorderWidth).BorderColor(Colors.Black)
                                 .PaddingVertical(1.5f).PaddingHorizontal(3).Text(val ?? "").FontSize(7.5f);
                            }
                            else
                            {
                                lbl.PaddingVertical(1.5f).PaddingHorizontal(3).Text(label).FontSize(7.5f);
                                v.PaddingVertical(1.5f).PaddingHorizontal(3).Text(val ?? "").FontSize(7.5f);
                            }
                        }

                        RenderMetaCell("Bill No.  :", model.BillNo);
                        RenderMetaCell("Date       :", model.BillDate);
                        RenderMetaCell("JOB No. :", model.JobNo, isLast: true);
                    });
                });

                // 3. Spacing before Job Details Box
                col.Item().PaddingTop(10);

                // 4. Job Details Box (Material Description, 3-Column Shipment Details, Values Strip)
                col.Item().Border(BorderWidth).BorderColor(Colors.Black).Column(boxCol =>
                {
                    // Material Description
                    boxCol.Item().PaddingTop(3.5f).PaddingBottom(4).PaddingHorizontal(4).Row(row =>
                    {
                        row.AutoItem().Text("Material Description: ").FontSize(8f).Bold();
                        row.RelativeItem().Text((model.MaterialDescription ?? "").ToUpper()).FontSize(8f).SemiBold();
                    });

                    // 3-Column Shipment Particulars
                    boxCol.Item().PaddingBottom(4).PaddingHorizontal(4).Row(row =>
                    {
                        void RenderPropRow(ColumnDescriptor c, string label, string? val, float labelWidth)
                        {
                            c.Item().PaddingVertical(0.5f).Row(r =>
                            {
                                r.AutoItem().Width(labelWidth).Text(label).FontSize(7.5f);
                                r.AutoItem().Width(6).Text(":").FontSize(7.5f);
                                r.RelativeItem().PaddingLeft(2).Text(val ?? "").FontSize(7.5f);
                            });
                        }

                        // Column 1
                        row.RelativeItem(3.4f).Column(c =>
                        {
                            RenderPropRow(c, "Quantity", model.Quantity, 72);
                            RenderPropRow(c, "Packing Quantity", model.PackingQuantity, 72);
                            RenderPropRow(c, "B/L NO", model.BlNo, 72);
                            RenderPropRow(c, "B/L DT", model.BlDate, 72);
                            RenderPropRow(c, "B/E NO", model.BeNo, 72);
                            RenderPropRow(c, "B/E DT", model.BeDate, 72);
                        });

                        // Column 2
                        row.RelativeItem(3.1f).Column(c =>
                        {
                            RenderPropRow(c, "Weight", model.Weight, 50);
                            RenderPropRow(c, "INV. NO", model.InvoiceNo, 50);
                            RenderPropRow(c, "INV. DT", model.InvoiceDate, 50);
                            RenderPropRow(c, "Exp NO", model.ExpNo, 50);
                            RenderPropRow(c, "Exp DT", model.ExpDate, 50);
                            RenderPropRow(c, "HAWB No", model.HawbNo, 50);
                        });

                        // Column 3
                        row.RelativeItem(3.5f).Column(c =>
                        {
                            RenderPropRow(c, "Destination", model.Destination, 72);
                            RenderPropRow(c, "Doc Recv Date", model.DocRecvDate, 72);
                            RenderPropRow(c, "Arrival Date", model.ArrivalDate, 72);
                            RenderPropRow(c, "Delivery Date", model.DeliveryDate, 72);
                            RenderPropRow(c, "LC/TT/BBR No", model.LcNo, 72);
                            RenderPropRow(c, "LC/TT/BBR DT", model.LcDate, 72);
                        });
                    });

                    // Values Strip (Separated by horizontal top and bottom border)
                    boxCol.Item().BorderTop(BorderWidth).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(4).Row(row =>
                    {
                        // Invoice Value
                        row.RelativeItem(5.5f).Row(r =>
                        {
                            r.AutoItem().Text($"Invoice Value ({model.Currency})").FontSize(7.5f).Bold();
                            r.RelativeItem().PaddingLeft(15).Text(model.InvoiceValue.HasValue ? model.InvoiceValue.Value.ToString("#,##0.00") : "").FontSize(7.5f).Bold();
                        });

                        // Assessable Value
                        row.RelativeItem(4.5f).Row(r =>
                        {
                            r.AutoItem().Text("Assessable Value (BDT)").FontSize(7.5f).Bold();
                            r.RelativeItem().PaddingLeft(15).Text(model.AssessableValue.HasValue ? model.AssessableValue.Value.ToString("#,##0.00") : "").FontSize(7.5f).Bold();
                        });
                    });
                });

                // Calculate dynamic spacer height so charges table spans page gracefully
                int totalItemRows = (model.ReceiptableItems?.Count ?? 0) +
                                    (model.NonReceiptableItems?.Count ?? 0) +
                                    (model.TransportItems?.Count ?? 0) +
                                    (model.AgencyCommissionItems?.Count ?? 0) + 4;
                float dynamicSpacerHeight = Math.Max(120f, 410f - (totalItemRows * 13.5f));

                // 5. Main Charges Table
                col.Item().BorderLeft(BorderWidth).BorderRight(BorderWidth).BorderBottom(BorderWidth).BorderColor(Colors.Black).Column(tableCol =>
                {
                    // Table Header
                    tableCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).Row(row =>
                    {
                        row.RelativeItem(6.8f).BorderRight(BorderWidth).BorderColor(Colors.Black).Padding(2.5f).AlignCenter().Text("PARTICULARS").FontSize(7.5f).Bold();
                        row.RelativeItem(1.6f).BorderRight(BorderWidth).BorderColor(Colors.Black).Padding(2.5f).AlignCenter().Text("TAKA").FontSize(7.5f).Bold();
                        row.RelativeItem(1.6f).Padding(2.5f).AlignCenter().Text("Remarks").FontSize(7.5f).Bold();
                    });

                    // Table Body
                    tableCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(6.8f); // PARTICULARS
                            cols.RelativeColumn(1.6f); // TAKA
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
                                    // Particulars: Item name on left, item rate/amount at right end of PARTICULARS column
                                    table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).PaddingLeft(14).PaddingRight(6).Row(r =>
                                    {
                                        r.RelativeItem().Text(itm.Particulars ?? "").FontSize(7.5f);
                                        r.AutoItem().AlignRight().Text(itm.Amount.ToString("#,##0.00")).FontSize(7.5f);
                                    });

                                    // TAKA: empty for individual item
                                    table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).PaddingVertical(0.5f).Text("");

                                    // Remarks: empty
                                    table.Cell().Text("");
                                }

                                // Sub Total in TAKA column
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

                        // Vertical spacing to span the table down the page
                        table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).MinHeight(dynamicSpacerHeight).Text("");
                        table.Cell().BorderRight(BorderWidth).BorderColor(Colors.Black).MinHeight(dynamicSpacerHeight).Text("");
                        table.Cell().MinHeight(dynamicSpacerHeight).Text("");
                    });

                    // Divider before Grand Totals & Remarks
                    tableCol.Item().BorderTop(BorderWidth).BorderColor(Colors.Black).Row(summaryRow =>
                    {
                        // Left Column (6.8f): Remarks & E. & O.E. on left, Total/Deductions/Net Due labels right-aligned
                        summaryRow.RelativeItem(6.8f).BorderRight(BorderWidth).BorderColor(Colors.Black).Row(partRow =>
                        {
                            // Notes area (aligned with Total, Deductions, and Net Due)
                            partRow.RelativeItem(4.0f).PaddingHorizontal(3).Column(notesCol =>
                            {
                                // Row 1 (aligned with Total)
                                notesCol.Item().PaddingVertical(2).Row(r =>
                                {
                                    r.AutoItem().Text("Remarks: ").FontSize(7.5f).Bold();
                                    r.RelativeItem().Text(model.Remarks ?? "").FontSize(7.5f);
                                });

                                // Row 2 (aligned with Deductions)
                                notesCol.Item().PaddingVertical(2).Text("");

                                // Row 3 (aligned with Net Due)
                                notesCol.Item().PaddingVertical(2).Text("E. & O.E.").FontSize(7.5f).Bold();
                            });

                            // Summary Labels right-aligned with trailing dash '-'
                            partRow.RelativeItem(2.8f).Column(labelCol =>
                            {
                                labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).Row(r =>
                                {
                                    r.RelativeItem().AlignRight().Text("Total").FontSize(7.5f);
                                    r.AutoItem().PaddingLeft(6).Text("-").FontSize(7.5f);
                                });
                                labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).Row(r =>
                                {
                                    r.RelativeItem().AlignRight().Text("Deductions").FontSize(7.5f);
                                    r.AutoItem().PaddingLeft(6).Text("-").FontSize(7.5f);
                                });
                                labelCol.Item().PaddingVertical(2).PaddingHorizontal(4).Row(r =>
                                {
                                    r.RelativeItem().AlignRight().Text("Net Due").FontSize(7.5f);
                                    r.AutoItem().PaddingLeft(6).Text("-").FontSize(7.5f);
                                });
                            });
                        });

                        // Middle Column (1.6f): Totals amounts directly under TAKA
                        summaryRow.RelativeItem(1.6f).BorderRight(BorderWidth).BorderColor(Colors.Black).Column(takaCol =>
                        {
                            takaCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                   .Text(model.TotalAmount.ToString("#,##0.00")).FontSize(7.5f).Bold();
                            takaCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(4).AlignCenter()
                                   .Text(model.Deductions.HasValue && model.Deductions.Value > 0 ? model.Deductions.Value.ToString("#,##0.00") : "-").FontSize(7.5f);
                            takaCol.Item().PaddingVertical(2).PaddingHorizontal(4).AlignRight()
                                   .Text(model.NetDue.ToString("#,##0.00")).FontSize(7.5f).Bold();
                        });

                        // Right Column (1.6f): Remarks column with matching horizontal dividers
                        summaryRow.RelativeItem(1.6f).Column(remCol =>
                        {
                            remCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).Text("");
                            remCol.Item().BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(2).Text("");
                            remCol.Item().PaddingVertical(2).Text("");
                        });
                    });
                });

                // 6. In Words Box
                col.Item().BorderLeft(BorderWidth).BorderRight(BorderWidth).BorderBottom(BorderWidth).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(4).Row(row =>
                {
                    row.AutoItem().Text("In Words: ").FontSize(7.5f).Bold();
                    row.RelativeItem().Text(model.InWords ?? "").FontSize(7.5f);
                });
            });

            // 7. Footer
            page.Footer().PaddingTop(6).Row(footerRow =>
            {
                // Left: Terms & Cheque notice
                footerRow.RelativeItem(6.4f).Column(termsCol =>
                {
                    termsCol.Item().Text("Terms: Service required as per our Trading Conditions.").FontSize(7.5f);
                    termsCol.Item().Text("The account will be correct unless queried within 7 days").FontSize(7.5f);

                    termsCol.Item().PaddingTop(16).Text("Cheques must be crossed and drawn in favour of Formosa Logistics Limited").FontSize(7.5f);
                });

                // Right: Signature Box
                footerRow.RelativeItem(3.6f).Border(BorderWidth).BorderColor(Colors.Black).Padding(4).Column(sigCol =>
                {
                    sigCol.Item().Text("For Formosa Logistics Limited.").FontSize(8f).Bold();
                    sigCol.Item().MinHeight(30).Text("");
                    sigCol.Item().AlignCenter().Text("....................................................................").FontSize(7.5f);
                    sigCol.Item().AlignCenter().Text("Authorised Signatory").FontSize(8f);
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
