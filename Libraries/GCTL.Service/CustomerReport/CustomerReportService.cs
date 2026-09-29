using ClosedXML.Excel;
using GCTL.Core.ViewModels.CustomerReport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Data;

namespace GCTL.Service.CustomerReport
{
    public class CustomerReportService : ICustomerReportService
    {
        #region Fields & Constructor

        private readonly string _connectionString;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _webHostEnvironment;
        private byte[]? _logoBytes;
        private bool _logoLoaded;

        public CustomerReportService(
            IConfiguration configuration,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null

            )
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
            _webHostEnvironment = env;
        }

        #endregion

        #region Helper Method to Load Logo Bytes

        private byte[]? GetLogoBytes()
        {
            if (_logoLoaded) return _logoBytes;
            _logoLoaded = true;

            try
            {
                var candidatePaths = new List<string?>
                {
                    _webHostEnvironment?.WebRootPath != null ? Path.Combine(_webHostEnvironment.WebRootPath, "images", "fermosa-wX200.png") : null,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "UIs", "GCTL.UI.Core", "wwwroot", "images", "fermosa-wX200.png"),
                    @"D:\Project\GCTL.Framework\UIs\GCTL.UI.Core\wwwroot\images\fermosa-wX200.png"
                };

                foreach (var path in candidatePaths)
                {
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        _logoBytes = File.ReadAllBytes(path);
                        break;
                    }
                }
            }
            catch
            {
                // Fallback gracefully without throwing
            }

            return _logoBytes;
        }

        #endregion

        #region Fetch Customer Report Data

        private static readonly object _idxLock = new();
        private static readonly Dictionary<string, (DateTime At, Dictionary<string, string> ByName, Dictionary<string, string> ById)> _fileIdx = new();

        private static (Dictionary<string, string> ByName, Dictionary<string, string> ById) GetFileIndex(string folder)
        {
            lock (_idxLock)
            {
                if (_fileIdx.TryGetValue(folder, out var e) && DateTime.UtcNow - e.At < TimeSpan.FromMinutes(2))
                    return (e.ByName, e.ById);

                var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(folder))
                {
                    foreach (var f in Directory.EnumerateFiles(folder))
                    {
                        byName.TryAdd(Path.GetFileName(f), f);
                        byId.TryAdd(Path.GetFileNameWithoutExtension(f), f);
                    }
                }
                _fileIdx[folder] = (DateTime.UtcNow, byName, byId);
                return (byName, byId);
            }
        }

        public async Task<List<CustomerReportResultDto>> GetCustomerReportDataAsync(CustomerReportFilterDto filter, bool loadBinaryContent = true)
        {
            var result = new List<CustomerReportResultDto>();
            if (string.IsNullOrEmpty(_connectionString)) return result;

            var webRoot = _webHostEnvironment?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var photoFolder = Path.Combine(webRoot, "uploads", "CRM", "Image");
            var sigFolder = Path.Combine(webRoot, "uploads", "CRM", "Signature");
            var photoIdx = GetFileIndex(photoFolder);
            var sigIdx = GetFileIndex(sigFolder);

            string Join(List<string>? l) => l != null && l.Any() ? string.Join(",", l.Select(x => x.Trim())) : string.Empty;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand("Sales_GetCustomerReport", connection)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };

            command.Parameters.Add("@CustomerID", SqlDbType.NVarChar, -1).Value = Join(filter.CustomerId);
            command.Parameters.Add("@CustomerTypeID", SqlDbType.NVarChar, -1).Value = Join(filter.CustomerTypeId);
            command.Parameters.Add("@CompanyCode", SqlDbType.NVarChar, 50).Value = DBNull.Value;
            command.Parameters.Add("@IncludePhoto", SqlDbType.Bit).Value = loadBinaryContent;

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess == 0 ? CommandBehavior.Default : CommandBehavior.Default);

            // column ordinal ek bar, exception nai
            var ord = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++) ord[reader.GetName(i)] = i;

            string S(string col) => ord.TryGetValue(col, out var i) && !reader.IsDBNull(i)
                ? Convert.ToString(reader.GetValue(i))?.Trim() ?? string.Empty : string.Empty;
            byte[]? B(string col) => ord.TryGetValue(col, out var i) && !reader.IsDBNull(i)
                ? reader.GetValue(i) as byte[] : null;

            while (await reader.ReadAsync())
            {
                var customerId = S("CustomerID");

                // photo
                string? photoUrl = null; string? photoPath = null;
                var pName = S("CustomerPhoto");
                if (!string.IsNullOrEmpty(pName) && pName != "System.Byte[]") photoIdx.ByName.TryGetValue(pName, out photoPath);
                if (photoPath == null && customerId != "") photoIdx.ById.TryGetValue(customerId, out photoPath);
                if (photoPath != null) photoUrl = $"/uploads/CRM/Image/{Path.GetFileName(photoPath)}";

                // signature
                string? sigUrl = null; string? sigPath = null;
                var sName = S("CustomerDigitalSignature");
                if (string.IsNullOrEmpty(sName)) sName = S("Signature");
                if (!string.IsNullOrEmpty(sName) && sName != "System.Byte[]") sigIdx.ByName.TryGetValue(sName, out sigPath);
                if (sigPath == null && customerId != "") sigIdx.ById.TryGetValue(customerId, out sigPath);
                if (sigPath != null) sigUrl = $"/uploads/CRM/Signature/{Path.GetFileName(sigPath)}";

                byte[]? photoBytes = null, sigBytes = null;
                if (loadBinaryContent)   // shudhu export er somoy
                {
                    if (photoPath != null) { try { photoBytes = await File.ReadAllBytesAsync(photoPath); } catch { } }
                    photoBytes ??= B("Photo") ?? B("CustomerPhoto");
                    if (sigPath != null) { try { sigBytes = await File.ReadAllBytesAsync(sigPath); } catch { } }
                    sigBytes ??= B("CustomerDigitalSignature") ?? B("Signature");
                }

                result.Add(new CustomerReportResultDto
                {
                    CustomerId = customerId,
                    CustomerName = S("CustomerName"),
                    CustomerAddress = S("Address"),
                    Phone = S("Phone"),
                    Fax = S("FAX"),
                    Email = S("Email"),
                    CustomerPhoto = photoBytes,
                    CustomerDigitalSignature = sigBytes,
                    PhotoUrl = photoUrl,
                    SignatureUrl = sigUrl,
                    ContactPerson = S("ContactPerson"),
                    Designation = S("Designation"),
                    CustomerTypeId = S("CustomerTypeID"),
                    CustomerType = S("CustomerTypeName"),
                    CompanyCode = S("CompanyCode"),
                    CompanyName = S("CompanyName"),
                    CompanyAddress1 = S("CompanyAddress1")
                });
            }
            return result;
        }

        // cascading dropdown er jonno
        public record CustomerLookupDto(string Value, string Text);

        public async Task<List<CustomerLookupDto>> GetCustomerLookupAsync(IEnumerable<string>? typeIds)
        {
            var list = new List<CustomerLookupDto>();
            if (string.IsNullOrEmpty(_connectionString)) return list;

            var types = typeIds != null && typeIds.Any() ? string.Join(",", typeIds.Select(x => x.Trim())) : string.Empty;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
            SELECT CustomerID, CustomerName
            FROM dbo.Sales_Customer
            WHERE @t = '' OR CustomerType IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@t, ','))
            ORDER BY CustomerName", conn);
            cmd.Parameters.Add("@t", SqlDbType.NVarChar, -1).Value = types;

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new CustomerLookupDto(r.GetValue(0).ToString()!.Trim(), r.IsDBNull(1) ? "" : r.GetString(1)));
            return list;
        }

        #endregion

        #region Generate PDF Report

        public async Task<byte[]> GeneratePdfReportAsync(CustomerReportFilterDto filter)
        {
            try
            {
                var data = await GetCustomerReportDataAsync(filter);

                if (filter.SelectedCustomerIds != null && filter.SelectedCustomerIds.Any())
                {
                    var selectedSet = new HashSet<string>(
                        filter.SelectedCustomerIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                        StringComparer.OrdinalIgnoreCase);
                    if (selectedSet.Count > 0)
                    {
                        data = data.Where(x => !string.IsNullOrEmpty(x.CustomerId) && selectedSet.Contains(x.CustomerId.Trim())).ToList();
                    }
                }

                var firstItem = data.FirstOrDefault();
                string companyName = !string.IsNullOrWhiteSpace(firstItem?.CompanyName) ? firstItem.CompanyName : "Formosa Logistics Limited";
                string companyAddress = !string.IsNullOrWhiteSpace(firstItem?.CompanyAddress1) ? firstItem.CompanyAddress1 : "21, Hazaribagh, Dhaka-1209";

                // Load logo bytes once (uses the existing GetLogoBytes() helper on this service)
                var logoBytes = GetLogoBytes();

                string GetTypeLabel(string typeId)
                {
                    if (string.IsNullOrWhiteSpace(typeId)) return "N/A";
                    return typeId.Trim() switch
                    {
                        "0" => "General",
                        "1" => "In House",
                        _ => typeId.Trim()
                    };
                }

                var groupedData = data.GroupBy(x => GetTypeLabel(x.CustomerTypeId)).OrderBy(g => g.Key).ToList();

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.MarginHorizontal(10);
                        page.MarginVertical(12);
                        page.Size(PageSizes.A4.Landscape());

                        page.Header().Row(row =>
                        {
                            // Left slot: logo (only rendered if the file was found)
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                row.ConstantItem(70).AlignMiddle().Image(logoBytes).FitArea();
                            }

                            // Center: company name / address / report title
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().AlignCenter().Text(companyName).FontSize(13).Bold();
                                col.Item().AlignCenter().Text(companyAddress).FontSize(9).FontColor(Colors.Grey.Darken2);
                                col.Item().AlignCenter().Text("Customer Report").FontSize(11).Bold().Underline();
                                //col.Item().PaddingTop(2).PaddingBottom(4).Row(row =>
                                //{
                                //    row.RelativeItem().AlignRight().Text($"Print Date: {DateTime.Now:dd/MM/yyyy}").FontSize(8);
                                //});
                            });

                            // Right slot: empty, same width as the logo, so the center column stays visually centered
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                row.ConstantItem(70);
                            }
                        });

                        page.Content().Column(mainCol =>
                        {
                            // int sl = 1;
                            bool isFirstGroup = true;

                            foreach (var group in groupedData)
                            {
                                //if (!isFirstGroup)
                                //{
                                //    mainCol.Item().PageBreak();
                                //}
                                //isFirstGroup = false;
                                int sl = 1;
                                mainCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(1.3f);
                                        cols.RelativeColumn(2.8f);
                                        cols.RelativeColumn(6.0f);
                                        cols.RelativeColumn(7.0f);
                                        cols.RelativeColumn(2.8f);
                                        cols.RelativeColumn(3.2f);
                                        cols.RelativeColumn(6.0f);
                                        cols.RelativeColumn(2.0f);
                                        cols.RelativeColumn(9.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().ColumnSpan(9).Element(c => c
                                            .PaddingTop(6)
                                            .PaddingBottom(3)
                                            .Text(text =>
                                            {
                                                text.Span("Customer Type: ").FontSize(10).Bold();
                                                text.Span(group.Key).FontSize(10).Bold();
                                            }));

                                        void BuildHeaderCell(string title)
                                        {
                                            header.Cell().Element(c => c
                                                .Border(0.5f)
                                                .BorderColor(Colors.Grey.Darken1)
                                                .Background(Colors.Grey.Lighten3)
                                                .PaddingHorizontal(2)
                                                .PaddingVertical(3)
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Text(title)
                                                .Bold()
                                                .FontSize(8f)
                                                .WrapAnywhere());
                                        }

                                        BuildHeaderCell("SN.");
                                        BuildHeaderCell("Customer ID");
                                        BuildHeaderCell("Name");
                                        BuildHeaderCell("Address");
                                        BuildHeaderCell("Phone");
                                        BuildHeaderCell("Fax");
                                        BuildHeaderCell("Email");
                                        BuildHeaderCell("Photo");
                                        BuildHeaderCell("Contact Person");
                                    });

                                    foreach (var d in group)
                                    {
                                        void BuildDataCell(string text, bool center = false)
                                        {
                                            table.Cell().Element(c => c
                                                .Border(0.5f)
                                                .BorderColor(Colors.Grey.Lighten1)
                                                .PaddingHorizontal(2)
                                                .PaddingVertical(2)
                                                .AlignMiddle()
                                                .Element(e => center ? e.AlignCenter() : e.AlignLeft())
                                                .Text(text ?? "")
                                                .FontSize(7.5f)
                                                .WrapAnywhere());
                                        }

                                        BuildDataCell(sl.ToString(), true);
                                        BuildDataCell(d.CustomerId, true);
                                        BuildDataCell(d.CustomerName);
                                        BuildDataCell(d.CustomerAddress);
                                        BuildDataCell(d.Phone, true);
                                        BuildDataCell(d.Fax, true);
                                        BuildDataCell(d.Email, true);

                                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                                            .PaddingHorizontal(2).PaddingVertical(2)
                                            .AlignCenter().AlignMiddle()
                                            .Height(28)
                                            .Column(imgCol =>
                                            {
                                                if (d.CustomerPhoto != null && d.CustomerPhoto.Length > 0)
                                                {
                                                    imgCol.Item().Image(d.CustomerPhoto).FitArea();
                                                }
                                                else
                                                {
                                                    imgCol.Item().AlignCenter().Text("-").FontSize(6.5f);
                                                }
                                            });

                                        BuildDataCell(d.ContactPerson);

                                        sl++;
                                    }
                                });
                            }
                        });

                        page.Footer().Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text($"Print Datetime: {DateTime.Now:dd/MM/yyyy hh:mm:ss tt}").FontSize(7).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                                txt.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                                txt.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium);
                                txt.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });
                }).GeneratePdf();

                return pdfBytes;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Generate Excel Report

        public async Task<byte[]> GenerateExcelReportAsync(CustomerReportFilterDto filter)
        {
            var data = await GetCustomerReportDataAsync(filter);

            if (filter.SelectedCustomerIds != null && filter.SelectedCustomerIds.Any())
            {
                var selectedSet = new HashSet<string>(
                    filter.SelectedCustomerIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                    StringComparer.OrdinalIgnoreCase);
                if (selectedSet.Count > 0)
                {
                    data = data.Where(x => !string.IsNullOrEmpty(x.CustomerId) && selectedSet.Contains(x.CustomerId.Trim())).ToList();
                }
            }

            var firstItem = data.FirstOrDefault();
            string companyName = !string.IsNullOrWhiteSpace(firstItem?.CompanyName) ? firstItem.CompanyName : "Formosa Logistics Limited";
            string companyAddress = !string.IsNullOrWhiteSpace(firstItem?.CompanyAddress1) ? firstItem.CompanyAddress1 : "21, Hazaribagh, Dhaka-1209";

            // Load logo bytes once (uses the existing GetLogoBytes() helper on this service)
            var logoBytes = GetLogoBytes();

            string GetTypeLabel(string typeId)
            {
                if (string.IsNullOrWhiteSpace(typeId)) return "N/A";
                return typeId.Trim() switch
                {
                    "0" => "General",
                    "1" => "In House",
                    _ => typeId.Trim()
                };
            }

            // Helper: Excel column width (character units) -> pixels (approx, standard Calibri 11 metric)
            double ColumnWidthToPixels(double columnWidth)
            {
                return (double)((int)((columnWidth * 7) + 5));
            }

            // Helper: row height in points -> pixels (96 DPI)
            double RowHeightToPixels(double rowHeightPoints)
            {
                return rowHeightPoints * 1.333;
            }

            var groupedData = data.GroupBy(x => GetTypeLabel(x.CustomerTypeId)).OrderBy(g => g.Key).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Customer Report");

            // Hide gridlines so no cell border shows through around the floated logo image
            ws.ShowGridLines = false;

            const int totalCols = 9;

            // SN, ID, Name, Address, Phone, Fax, Email, Photo, Contact
            double[] colWidths = { 6, 10, 30, 35, 10, 15, 25, 9, 50 };

            for (int i = 0; i < colWidths.Length; i++)
            {
                ws.Column(i + 1).Width = colWidths[i];
            }

            var r1 = ws.Range(1, 1, 1, totalCols);
            r1.Merge();
            ws.Cell(1, 1).Value = companyName;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var r2 = ws.Range(2, 1, 2, totalCols);
            r2.Merge();
            ws.Cell(2, 1).Value = companyAddress;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var r3 = ws.Range(3, 1, 3, totalCols);
            r3.Merge();
            ws.Cell(3, 1).Value = "Customer Report";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 12;
            ws.Cell(3, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(3, 1).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            if (logoBytes != null && logoBytes.Length > 0)
            {
                ws.Row(1).Height = 20;
                ws.Row(2).Height = 16;
                ws.Row(3).Height = 18;

                try
                {
                    using var logoStream = new MemoryStream(logoBytes);
                    var logoPicture = ws.AddPicture(logoStream)
                        .MoveTo(ws.Cell(1, 1), 4, 14);
                    logoPicture.Width = 90;
                    logoPicture.Height = 32;
                }
                catch
                {
                    // Fallback gracefully without throwing if the image can't be embedded
                }
            }

            string[] headers = new string[]
            {
               "SN.", "Customer ID", "Name", "Address", "Phone", "Fax", "Email", "Photo", "Contact Person"
            };

            int currRow = 5;
            const int photoColIndex = 8;
            const double rowHeightForPhoto = 40;
            const double baseRowHeight = 15;
            const double lineHeight = 12;
            const double photoImgWidth = 50;
            const double photoImgHeight = 40;     // font 9 er jonno (age 14.5 chhilo)
            const double charsPerWidthUnit = 1.2; // Times New Roman 9 e column e beshi char dhore

            double EstimateRowHeightForColumn(string text, int columnIndex)
            {
                if (string.IsNullOrWhiteSpace(text)) return baseRowHeight;

                double colWidthChars = Math.Max(6, ws.Column(columnIndex).Width * charsPerWidthUnit);

                var explicitLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                int totalWrappedLines = 0;

                foreach (var line in explicitLines)
                {
                    int wrapped = (int)Math.Ceiling(line.Trim().Length / colWidthChars);
                    totalWrappedLines += Math.Max(1, wrapped);
                }

                return Math.Max(baseRowHeight, totalWrappedLines * lineHeight + 4);
            }

            var groupPageBreakRows = new List<int>();

            foreach (var group in groupedData)
            {
                int sl = 1;
                if (groupPageBreakRows.Count > 0 || currRow > 5)
                {
                    groupPageBreakRows.Add(currRow);
                }

                var groupRange = ws.Range(currRow, 1, currRow, totalCols);
                groupRange.Merge();
                ws.Cell(currRow, 1).Value = $"Customer Type: {group.Key}";
                ws.Cell(currRow, 1).Style.Font.Bold = true;
                ws.Cell(currRow, 1).Style.Font.FontSize = 10;
                ws.Cell(currRow, 1).Style.Font.FontName = "Times New Roman";
                ws.Cell(currRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
                ws.Cell(currRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                currRow++;

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(currRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 9;
                    cell.Style.Font.FontName = "Times New Roman";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                currRow++;

                foreach (var item in group)
                {
                    ws.Cell(currRow, 1).Value = sl;
                    ws.Cell(currRow, 2).Value = item.CustomerId;
                    ws.Cell(currRow, 3).Value = item.CustomerName;
                    ws.Cell(currRow, 4).Value = item.CustomerAddress;
                    ws.Cell(currRow, 5).Value = item.Phone;
                    ws.Cell(currRow, 6).Value = item.Fax;
                    ws.Cell(currRow, 7).Value = item.Email;
                    var contactClean = string.Join("\n",
                    (item.ContactPerson ?? "")
                   .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(l => l.Trim()));
                    ws.Cell(currRow, 9).Value = contactClean;

                    // ---- Row height must be finalized BEFORE we calculate photo centering offsets ----
                    double neededHeight = baseRowHeight;
                    neededHeight = Math.Max(neededHeight, EstimateRowHeightForColumn(item.CustomerName, 3));
                    neededHeight = Math.Max(neededHeight, EstimateRowHeightForColumn(item.CustomerAddress, 4));
                   // neededHeight = Math.Max(neededHeight, EstimateRowHeightForColumn(item.ContactPerson, 9));
                    neededHeight = Math.Max(neededHeight, EstimateRowHeightForColumn(contactClean, 9));

                    bool hasPhoto = item.CustomerPhoto != null && item.CustomerPhoto.Length > 0;
                    if (hasPhoto)
                    {
                        neededHeight = Math.Max(neededHeight, rowHeightForPhoto);
                    }

                    ws.Row(currRow).Height = neededHeight;

                    if (hasPhoto)
                    {
                        try
                        {
                            using var imgStream = new MemoryStream(item.CustomerPhoto);

                            double colWidthPx = ColumnWidthToPixels(ws.Column(photoColIndex).Width);
                            double rowHeightPx = RowHeightToPixels(ws.Row(currRow).Height);

                            int xOffset = (int)Math.Max(0, (colWidthPx - photoImgWidth) / 2);
                            int yOffset = (int)Math.Max(0, (rowHeightPx - photoImgHeight) / 2);

                            var picture = ws.AddPicture(imgStream)
                                .MoveTo(ws.Cell(currRow, photoColIndex), xOffset, yOffset);
                            picture.Width = (int)photoImgWidth;
                            picture.Height = (int)photoImgHeight;

                            ws.Cell(currRow, photoColIndex).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(currRow, photoColIndex).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        catch
                        {
                            ws.Cell(currRow, photoColIndex).Value = "-";
                            ws.Cell(currRow, photoColIndex).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(currRow, photoColIndex).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                    }
                    else
                    {
                        ws.Cell(currRow, photoColIndex).Value = "-";
                        ws.Cell(currRow, photoColIndex).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(currRow, photoColIndex).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }

                    for (int c = 1; c <= totalCols; c++)
                    {
                        var cell = ws.Cell(currRow, c);
                        cell.Style.Font.FontSize = 9;
                        cell.Style.Font.FontName = "Times New Roman";
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Alignment.WrapText = true;

                        if (c == 1 || c == 2 || c == 5 || c == 6 || c == 7 || c == 8)
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        else
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    currRow++;
                    sl++;
                }

                currRow++;
            }

            foreach (var breakRow in groupPageBreakRows)
            {
                ws.PageSetup.AddHorizontalPageBreak(breakRow - 1);
                ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                ws.PageSetup.FitToPages(1, 0);
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        #endregion

        #region Generate CSV Report

        public async Task<byte[]> GenerateCsvReportAsync(CustomerReportFilterDto filter)
        {
            var data = await GetCustomerReportDataAsync(filter, loadBinaryContent: false);

            if (filter.SelectedCustomerIds != null && filter.SelectedCustomerIds.Any())
            {
                var selectedSet = new HashSet<string>(
                    filter.SelectedCustomerIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                    StringComparer.OrdinalIgnoreCase);
                if (selectedSet.Count > 0)
                {
                    data = data.Where(x => !string.IsNullOrEmpty(x.CustomerId) && selectedSet.Contains(x.CustomerId.Trim())).ToList();
                }
            }

            string GetTypeLabel(string? typeId)
            {
                if (string.IsNullOrWhiteSpace(typeId)) return "N/A";
                return typeId.Trim() switch
                {
                    "0" => "General",
                    "1" => "In House",
                    _ => typeId.Trim()
                };
            }

            var groupedData = data.GroupBy(x => GetTypeLabel(x.CustomerTypeId)).OrderBy(g => g.Key).ToList();

            var firstItem = data.FirstOrDefault();
            string companyName = !string.IsNullOrWhiteSpace(firstItem?.CompanyName) ? firstItem.CompanyName : "Formosa Logistics Limited";
            string companyAddress = !string.IsNullOrWhiteSpace(firstItem?.CompanyAddress1) ? firstItem.CompanyAddress1 : "21, Hazaribagh, Dhaka-1209";

            var headers = new[]
            {
               "SN.", "Customer ID", "Name", "Address", "Phone", "Fax", "Email", "Photo", "Contact Person"
            };

            int totalCols = headers.Length;
            int[] colWidths = new int[totalCols];

            for (int c = 0; c < totalCols; c++)
            {
                colWidths[c] = headers[c].Length;
            }

            var preparedGroups = new List<(string TypeLabel, int Count, List<string[]> Rows)>();

            foreach (var group in groupedData)
            {
                var groupRows = new List<string[]>();
                int sl = 1;
                foreach (var item in group)
                {
                    bool hasPhoto = !string.IsNullOrEmpty(item.PhotoUrl) || (item.CustomerPhoto != null && item.CustomerPhoto.Length > 0);

                    var row = new[]
                    {
                (sl++).ToString(),
                item.CustomerId ?? "",
                item.CustomerName ?? "",
                item.CustomerAddress ?? "",
                item.Phone ?? "",
                item.Fax ?? "",
                item.Email ?? "",
                hasPhoto ? "" : "-",     // Excel er moto: photo thakle blank, na thakle "-"
                item.ContactPerson ?? ""
            };

                    for (int c = 0; c < totalCols; c++)
                    {
                        string cleanVal = (row[c] ?? "").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                        row[c] = cleanVal;
                        if (cleanVal.Length > colWidths[c])
                        {
                            colWidths[c] = cleanVal.Length;
                        }
                    }

                    groupRows.Add(row);
                }

                preparedGroups.Add((group.Key, group.Count(), groupRows));
            }

            int[] minCustomerWidths = new int[]
            {
                 6,  // SN
                 40, // Customer ID
                 80, // Customer Name
                 90, // Address
                 40, // Phone
                 40, // Fax
                 55, // Email
                 15,  // Photo
                 100  // Contact Person
            };

            for (int c = 0; c < totalCols; c++)
            {
                colWidths[c] = Math.Max(minCustomerWidths[c], colWidths[c] + 2);
            }

            int totalTableWidth = colWidths.Sum() + (totalCols - 1);

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(true)))
            {
                await writer.WriteLineAsync(BuildCenteredCsvHeaderRow(companyName, totalCols, totalTableWidth));
                await writer.WriteLineAsync(BuildCenteredCsvHeaderRow(companyAddress, totalCols, totalTableWidth));
                await writer.WriteLineAsync(BuildCenteredCsvHeaderRow("Customer Report", totalCols, totalTableWidth));
                await writer.WriteLineAsync(new string(',', totalCols - 1)); // Blank row (Excel er row 4 er moto)

                if (preparedGroups.Count == 0)
                {
                    var paddedHeaders = headers.Select((h, i) => EscapeCsv(FormatCustomerCsvCell(h, i, colWidths[i], isHeader: true)));
                    await writer.WriteLineAsync(string.Join(",", paddedHeaders));

                    await writer.WriteLineAsync($"\"No customer records found.\"{new string(',', totalCols - 1)}");
                }
                else
                {
                    foreach (var group in preparedGroups)
                    {
                        await writer.WriteLineAsync($"\"Customer Type: {group.TypeLabel} [Total: {group.Count}]\"{new string(',', totalCols - 1)}");

                        var paddedHeaders = headers.Select((h, i) => EscapeCsv(FormatCustomerCsvCell(h, i, colWidths[i], isHeader: true)));
                        await writer.WriteLineAsync(string.Join(",", paddedHeaders));

                        foreach (var row in group.Rows)
                        {
                            var formattedCells = row.Select((val, i) => EscapeCsv(FormatCustomerCsvCell(val, i, colWidths[i], isHeader: false)));
                            await writer.WriteLineAsync(string.Join(",", formattedCells));
                        }

                        await writer.WriteLineAsync(new string(',', totalCols - 1)); // Blank line between groups
                    }
                }

                await writer.FlushAsync();
            }

            return memoryStream.ToArray();
        }

        private static string PadCenter(string text, int width)
        {
            text ??= "";
            if (text.Length >= width) return text;
            int total = width - text.Length;
            int left = total / 2;
            int right = total - left;
            return new string(' ', left) + text + new string(' ', right);
        }

        private static string PadLeftAligned(string text, int width)
        {
            text ??= "";
            if (text.Length >= width) return text;
            return text.PadRight(width);
        }

        private static string FormatCustomerCsvCell(string? text, int colIdx, int width, bool isHeader = false)
        {
            text = (text ?? "").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
            if (isHeader)
            {
                return PadCenter(text, width);
            }

            if (colIdx == 2 || colIdx == 3 || colIdx == 8) // Name, Address, Contact Person -> Left
            {
                return PadLeftAligned(text, width);
            }
            else // SN, Customer ID, Phone, Fax, Email, Photo -> Center
            {
                return PadCenter(text, width);
            }
        }

        private static string BuildCenteredCsvHeaderRow(string text, int totalCols, int totalTableWidth)
        {
            string centered = PadCenter(text, totalTableWidth);
            return $"{EscapeCsv(centered)}{new string(',', totalCols - 1)}";
        }

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            val = val.Replace("\"", "\"\"");
            return $"\"{val}\"";
        }

        #endregion

    }
}
