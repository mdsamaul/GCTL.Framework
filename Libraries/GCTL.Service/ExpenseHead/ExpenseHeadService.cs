using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.ExpenseHead;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;




namespace GCTL.Service.ExpenseHead
{
    public class ExpenseHeadService : AppService<CfDefExpenseHead>, IExpenseHead
    {
        #region
        private readonly IRepository<CfDefExpenseHead> _repository;
        private readonly GCTL_ERP_DBContext _context;
        private readonly string _connectionString;

        public ExpenseHeadService(IRepository<CfDefExpenseHead> genericRepository, GCTL_ERP_DBContext context, IConfiguration configuration) : base(genericRepository)
        {
            _repository = genericRepository;
            _context = context;
            _connectionString = configuration.GetConnectionString("connection");
        }
        #endregion

        #region
        public async Task<bool> BulkDeleteAsync(List<int> ids)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.All().Where(c => ids.Contains(c.Tc)).ToListAsync();

                if (entity == null || !entity.Any())
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                await _repository.DeleteRangeAsync(entity);

                await _repository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Bulk delete error: {ex}");
                return (false);
            }
        }

        public async Task<ExpenseHeadVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.Tc == id)
                       .Select(pt => new ExpenseHeadVM
                       {
                           ExpenseHeadID = pt.ExpenseHeadId,
                           ExpenseHead = pt.ExpenseHead,
                           ShortName = pt.ShortName,
                           Amount = pt.Amount,
                           SerialNo = pt.SerialNo,
                           IsReceiptable = pt.IsReceiptable,
                           ServiceTypeID = pt.ServiceTypeId,
                           ExpenseTypeID = pt.ExpenseTypeId,
                           LDate = pt.Ldate,
                           ModifyDate = pt.ModifyDate,
                       }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> GetLastExpenseHeadAsync()
        {
            var last = await _repository.All()
                        .OrderByDescending(c => c.ExpenseHeadId)
                        .FirstOrDefaultAsync();

            return last?.ExpenseHeadId;
        }

        public async Task<PaginationService<CfDefExpenseHead, ExpenseHeadVM>.PaginationResult<ExpenseHeadVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "ExpenseHeadID", string sortOrder = "desc", string expenseType = "", string isReceiptable = "", string serviceType = "")
        {
            var query = _repository.All();
            if (!string.IsNullOrEmpty(expenseType))
                query = query.Where(x => x.ExpenseTypeId == expenseType);

            if (!string.IsNullOrEmpty(isReceiptable))
                query = query.Where(x => x.IsReceiptable == isReceiptable);

            if (!string.IsNullOrEmpty(serviceType))
                query = query.Where(x => x.ServiceTypeId == serviceType);

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
            }

            var paginatedResult = await PaginationService<CfDefExpenseHead, ExpenseHeadVM>.GetPaginatedData(
                query,
                pageNumber,
                pageSize,
                searchTerm,
                sortColumn,
                sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.ExpenseHeadId ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.ExpenseHead ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.ServiceTypeId ?? "", $"%{term}%"),
                pt => new ExpenseHeadVM
                {
                    TC = pt.Tc,
                    ExpenseHeadID = pt.ExpenseHeadId,
                    ExpenseHead = pt.ExpenseHead,
                    ShortName = pt.ShortName,
                    Amount = pt.Amount,
                    SerialNo = pt.SerialNo,
                    IsReceiptable = pt.IsReceiptable,
                    ExpenseTypeID = pt.ExpenseTypeId,
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                    ServiceTypeID = pt.ServiceTypeId,
                    ServiceTypeName = _context.Set<GCTL.Data.Models.CoreServiceType>()
                                            .Where(cs => cs.ServiceTypeId == pt.ServiceTypeId)
                                            .Select(cs => cs.ServiceTypeName)
                                            .FirstOrDefault(),
                    ExpenseType = _context.Set<CfDefExpenseHead>()
                                          .Where(et => et.ExpenseTypeId == pt.ExpenseTypeId)
                                          .Select(et => et.ExpenseTypeId)
                                          .FirstOrDefault()
                }
            );

            return paginatedResult;
        }

        public async Task<bool> IsDuplicateAsync(ExpenseHeadVM model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return await _repository.All()
                .AnyAsync(eh =>
                    eh.Tc != model.TC &&
                    eh.ExpenseHead.Trim().ToLower() == model.ExpenseHead.Trim().ToLower()
                );
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.Tc == id);
        }

        public async Task<bool> SaveAsync(ExpenseHeadVM model)
        {
            try
            {
                var entity = new CfDefExpenseHead
                {
                    ExpenseHeadId = model.ExpenseHeadID,
                    ExpenseHead = model.ExpenseHead,
                    ShortName = model.ShortName ?? "",
                    Amount = model.Amount,
                    SerialNo = model.SerialNo,
                    IsReceiptable = model.IsReceiptable,
                    ServiceTypeId = model.ServiceTypeID,
                    ExpenseTypeId = model.ExpenseTypeID,
                    Luser = "",
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP() ?? "",
                    Lmac = GetMacAddress() ?? "",
                    EntryType = model.EntryType ?? "",
                };

                await _repository.AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Insertion Failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(ExpenseHeadVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.GetByIdAsync(model.TC);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.ExpenseHeadId = model.ExpenseHeadID;
                entity.ExpenseHead = model.ExpenseHead;
                entity.ShortName = model.ShortName ?? "";
                entity.Amount = model.Amount;
                entity.SerialNo = model.SerialNo;
                entity.IsReceiptable = model.IsReceiptable;
                entity.ServiceTypeId = model.ServiceTypeID;
                entity.ExpenseTypeId = model.ExpenseTypeID;
                entity.Luser = "";
                entity.ModifyDate = DateTime.Now;
                entity.Lip = GetLocalIP() ?? "";
                entity.Lmac = GetMacAddress() ?? "";
                entity.EntryType = model.EntryType ?? "";

                await _repository.UpdateAsync(entity);
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await _repository.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<List<ExpenseHeadVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new ExpenseHeadVM
            {
                ExpenseHeadID = model.ExpenseHeadId,
                ExpenseHead = model.ExpenseHead,
                ShortName = model.ShortName ?? "",
                Amount = model.Amount,
                SerialNo = model.SerialNo,
                IsReceiptable = model.IsReceiptable,
                ServiceTypeID = model.ServiceTypeId,
                ExpenseTypeID = model.ExpenseTypeId
            }).ToList();

            return result;
        }

        public async Task<int> GetNextSerialNoAsync(string expenseType, string serviceType, string isReceiptable, int? requestedSerial = null)
        {
            if (string.IsNullOrWhiteSpace(expenseType) || string.IsNullOrWhiteSpace(isReceiptable))
                throw new ArgumentException("ExpenseType and IsReceiptable are required.");

            isReceiptable = isReceiptable.Trim().ToUpper();
            if (isReceiptable != "YES" && isReceiptable != "NO")
                throw new ArgumentException("IsReceiptable must be 'Yes' or 'No'");

            var allRecords = await _repository.GetAllAsync();

            // Filter for the type & service & receiptable
            var filtered = allRecords.Where(e =>
                e.ExpenseTypeId == expenseType &&
                (string.IsNullOrEmpty(serviceType) || e.ServiceTypeId == serviceType) &&
                e.IsReceiptable?.Trim().ToUpper() == isReceiptable
            ).OrderBy(e => int.TryParse(e.SerialNo, out int s) ? s : 0).ToList();

            int nextSerial = 1;

            if (requestedSerial.HasValue)
            {
                int req = requestedSerial.Value;
                if (req <= 0)
                    throw new ArgumentException("Requested serial must be a positive integer.");

                if (filtered.Any())
                {
                    int minSerial = filtered.Min(e => int.Parse(e.SerialNo));

                    if (req >= minSerial)
                    {
                        // Shift serials >= requestedSerial
                        var toShift = filtered.Where(e => int.TryParse(e.SerialNo, out int s) && s >= req)
                                              .OrderByDescending(e => int.Parse(e.SerialNo))
                                              .ToList();

                        foreach (var record in toShift)
                        {
                            int currentSerial = int.Parse(record.SerialNo);
                            record.SerialNo = (currentSerial + 1).ToString();
                            await _repository.UpdateAsync(record);
                        }
                    }
                }

                nextSerial = req;
            }
            else if (filtered.Any())
            {
                nextSerial = filtered.Max(e => int.TryParse(e.SerialNo, out int s) ? s : 0) + 1;
            }

            return nextSerial;
        }
        #endregion

        #region Ip & Mac
        public string GetLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }

        public string GetMacAddress()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var macAddress = string.Empty;
            foreach (var adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    macAddress = adapter.GetPhysicalAddress().ToString();
                    break;
                }
            }
            return macAddress;
        }

        #endregion

        #region Report
        public async Task<List<ExpenseHeadReportVM>> GetExpenseHeadReportAsync(string expenseTypeId = "")
        {
            var result = new List<ExpenseHeadReportVM>();

            // SqlConnection 
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand("GetExpenseHeadReport", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parameter
                    cmd.Parameters.Add(new SqlParameter("@ExpenseTypeId", string.IsNullOrEmpty(expenseTypeId) ? (object)DBNull.Value : expenseTypeId));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new ExpenseHeadReportVM
                            {
                                Company = reader["CompanyName"].ToString(),
                                Address = reader["Address1"].ToString(),
                                ExpenseHeadID = reader["ExpenseHeadID"].ToString(),
                                ExpenseHead = reader["ExpenseHead"].ToString(),
                                ShortName = reader["ShortName"].ToString(),
                                SerialNo = reader["SerialNo"].ToString(),
                                IsReceiptable = reader["IsReceiptable"].ToString(),
                                ServiceProvidedBy = reader["ServiceProvidedBy"].ToString(),
                                ExpenseType = reader["ExpenseType"].ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        #region Gnereate Excel Report
        public async Task<byte[]> GenerateExcelReportAsync(string expenseTypeId = "")
        {
            var data = await GetExpenseHeadReportAsync(expenseTypeId);
            using var ms = new MemoryStream();
            using (var spreadsheet = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                // --- Styles ---
                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = new Stylesheet();

                // Fonts: 0-default, 1-bold, 2-title bold+large
                var fonts = new DocumentFormat.OpenXml.Spreadsheet.Fonts(
                    new Font(),
                    new Font(new Bold()),
                    new Font(new Bold(), new FontSize() { Val = 16 })
                );
                stylesPart.Stylesheet.Fonts = fonts;

                // Fills 
                var fills = new Fills(
                    new Fill(new PatternFill() { PatternType = PatternValues.None }),    // 0: default
                    new Fill(new PatternFill() { PatternType = PatternValues.Gray125 }), // 1: required
                    new Fill(new PatternFill(
                        new ForegroundColor() { Rgb = "FFF0F8FF" } // LightSkyBlue
                    )
                    { PatternType = PatternValues.Solid }) // 2: solid bg color
                );
                stylesPart.Stylesheet.Fills = fills;

                // Borders
                stylesPart.Stylesheet.Borders = new Borders(new Border());

                // CellFormats
                // CellFormats
                var cellFormats = new CellFormats(
                    new CellFormat(), // 0-default
                    new CellFormat() { FontId = 1, ApplyFont = true }, // 1-bold
                    new CellFormat()
                    {
                        FontId = 2,
                        ApplyFont = true,
                        FillId = 2,
                        ApplyFill = true,
                        Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center }
                    }, // 2-title
                    new CellFormat() { Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center } }, // 3-center only
                    new CellFormat() { Alignment = new Alignment() { WrapText = true, Horizontal = HorizontalAlignmentValues.Left } }, // 4-wrap+left
                    new CellFormat() { Alignment = new Alignment() { WrapText = true, Horizontal = HorizontalAlignmentValues.Left, Indent = 1 } } // 5-left + padding
                );

                stylesPart.Stylesheet.CellFormats = cellFormats;
                stylesPart.Stylesheet.Save();

                uint titleStyleIndex = 2;
                uint boldStyleIndex = 1;
                uint centerStyleIndex = 3;
                uint wrapLeftStyleIndex = 4;
                uint wrapLeftPaddingStyleIndex = 5; // new



                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "ExpenseHead Report" });

                int totalColumns = 7;

                string company = data.FirstOrDefault()?.Company ?? "N/A";
                string address = data.FirstOrDefault()?.Address ?? "N/A";
                string reportTitle = "Expense Head Setup Report";

                // Helper for merged title rows
                Row CreateMergedRow(string text, uint styleIndex)
                {
                    var row = new Row();
                    var cell = new Cell()
                    {
                        CellValue = new CellValue(text),
                        DataType = CellValues.String,
                        StyleIndex = styleIndex
                    };
                    row.Append(cell);
                    for (int i = 1; i < totalColumns; i++)
                        row.Append(new Cell());
                    return row;
                }

                // --- Title rows ---
                sheetData.Append(CreateMergedRow(company, titleStyleIndex));
                sheetData.Append(CreateMergedRow(address, titleStyleIndex));
                sheetData.Append(CreateMergedRow(reportTitle, titleStyleIndex));

                // Empty row
                sheetData.Append(new Row());

                // --- Header row ---
                // Header Row
                var headerRow = new Row();
                headerRow.Append(
                    new Cell() { CellValue = new CellValue("Expense Head ID"), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                    new Cell() { CellValue = new CellValue("Expense Head"), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                    new Cell() { CellValue = new CellValue("Short Name"), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                    new Cell() { CellValue = new CellValue("Serial No"), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                    new Cell() { CellValue = new CellValue("Is Receiptable?"), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                    new Cell() { CellValue = new CellValue("Service Provided By"), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                    new Cell() { CellValue = new CellValue("Service Type"), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex }
                );
                sheetData.Append(headerRow);

                // Data Rows
                foreach (var d in data)
                {
                    var row = new Row();
                    row.Append(
                        new Cell() { CellValue = new CellValue(d.ExpenseHeadID ?? ""), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ExpenseHead ?? ""), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ShortName ?? ""), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                        new Cell() { CellValue = new CellValue(d.SerialNo ?? ""), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                        new Cell() { CellValue = new CellValue(d.IsReceiptable ?? ""), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ServiceProvidedBy ?? ""), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ExpenseType ?? ""), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex }
                    );
                    sheetData.Append(row);
                }


                // Merge title rows
                var mergeCells = new MergeCells();
                mergeCells.Append(new MergeCell() { Reference = new StringValue($"A1:G1") });
                mergeCells.Append(new MergeCell() { Reference = new StringValue($"A2:G2") });
                mergeCells.Append(new MergeCell() { Reference = new StringValue($"A3:G3") });
                worksheetPart.Worksheet.InsertAfter(mergeCells, sheetData);

                // Column widths
                var columns = new Columns(
                    new Column() { Min = 1, Max = 1, Width = 20, CustomWidth = true },
                    new Column() { Min = 2, Max = 2, Width = 40, CustomWidth = true },
                    new Column() { Min = 3, Max = 3, Width = 15, CustomWidth = true },
                    new Column() { Min = 4, Max = 4, Width = 12, CustomWidth = true },
                    new Column() { Min = 5, Max = 5, Width = 15, CustomWidth = true },
                    new Column() { Min = 6, Max = 6, Width = 20, CustomWidth = true },
                    new Column() { Min = 7, Max = 7, Width = 25, CustomWidth = true }
                );
                worksheetPart.Worksheet.InsertAt(columns, 0);

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            return ms.ToArray();
        }
        #endregion

        #region Generate PDF Report
        public async Task<byte[]> GeneratePdfReportAsync(string expenseTypeId = "")
        {
            var data = await GetExpenseHeadReportAsync(expenseTypeId);

            string company = data.FirstOrDefault()?.Company ?? "N/A";
            string address = data.FirstOrDefault()?.Address ?? "N/A";
            string reportTitle = "Expense Head Setup Report";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);

                    // Header
                    page.Header()
                        .Column(col =>
                        {
                            col.Item()
                                .Container()
                                .BorderTop(1)
                                .BorderLeft(1)
                                .BorderRight(1)
                                .BorderColor("#000000")
                                .AlignCenter()
                                .Text(company)
                                .FontSize(18)
                                .Bold();

                            col.Item()
                                .Container()
                                .BorderLeft(1)
                                .BorderRight(1)
                                .BorderColor("#000000")
                                .AlignCenter()
                                .Text(address)
                                .FontSize(14)
                                .Bold();

                            col.Item()
                               .Container()
                               .BorderLeft(1)
                               .BorderRight(1)
                               .BorderBottom(1)
                               .BorderColor("#000000")
                               .PaddingBottom(4)
                               .AlignCenter()
                               .Text(reportTitle)
                               .FontSize(14)
                               .Bold();
                        });

                    // Content Table
                    page.Content()
                        .Table(table =>
                        {
                            // Columns Definition
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);  // Expense Head ID
                                columns.RelativeColumn(4);   // Expense Head
                                columns.ConstantColumn(80);  // Short Name
                                columns.ConstantColumn(40);  // Serial No
                                columns.ConstantColumn(70);  // Is Receiptable
                                columns.ConstantColumn(80); // Service Provided By
                                columns.ConstantColumn(80); // Service Type
                            });

                            // Header Row
                            table.Header(header =>
                            {
                                header.Cell()
                                    .Element(container => container
                                    .Border(0.25f) // 1pt border
                                    .AlignCenter()
                                    .Text("Expense Head ID")
                                    .Bold().FontSize(10) );


                                header.Cell()
                                      .Element(container => container
                                      .Border(0.25f) 
                                      .PaddingLeft(2)
                                      .Element(container => container
                                      .Text("Expense Head").Bold().AlignLeft().FontSize(10)));

                                header.Cell()
                                      .Element(container => container
                                      .Border(0.25f)
                                      .Text("Short Name").Bold().AlignCenter().FontSize(10));

                                header.Cell()
                                      .Element(container => container
                                      .Border(0.25f)
                                      .Text("Serial\nNo").Bold().AlignCenter().FontSize(10));

                                // Is Receiptable header in two lines
                                header.Cell()
                                      .Element(container => container
                                       .Border(0.25f)
                                      .Text("Is\nReceiptable") 
                                      .Bold().AlignCenter().FontSize(10));


                                header.Cell()
                                        .Element(container => container
                                        .Border(0.25f)
                                        .PaddingLeft(2)
                                        .Text("Service\nProvided By")
                                        .Bold().AlignLeft().FontSize(10));

                                header.Cell()
                                .Element(container => container
                                .Border(0.25f)
                                .PaddingLeft(2)
                                .Text("Service Type").Bold().AlignLeft().FontSize(10));
                            });

                            // Data Rows
                            foreach (var d in data)
                            {
                                table.Cell()
                                     .Border(0.25f)
                                             .MinHeight(20)   // min height in points

                                     .BorderColor(QuestPDF.Helpers.Colors.Black)
                                     .Text(d.ExpenseHeadID ?? "")
                                     .AlignCenter()
                                     .FontSize(10);

                                table.Cell()
                                     .Border(0.25f)
                                     .MinHeight(20) 
                                    .PaddingLeft(2)
                                    .BorderColor(QuestPDF.Helpers.Colors.Black)
                                     .Text(d.ExpenseHead ?? "")
                                     .AlignLeft()
                                     .WrapAnywhere()
                                     .FontSize(10);

                                table.Cell()
                                     .Border(0.25f)
                                      .MinHeight(20)   // min height in points
                                     .BorderColor(QuestPDF.Helpers.Colors.Black)
                                     .Text(d.ShortName ?? "")
                                     .AlignCenter()
                                     .FontSize(10);

                                table.Cell()
                                     .Border(0.25f)
                                      .MinHeight(20)   // min height in points
                                     .BorderColor(QuestPDF.Helpers.Colors.Black)
                                     .Text(d.SerialNo ?? "")
                                     .AlignCenter()
                                     .FontSize(10);

                                table.Cell()
                                     .Border(0.25f)
                                      .MinHeight(20)   // min height in points
                                     .BorderColor(QuestPDF.Helpers.Colors.Black)
                                     .Text(d.IsReceiptable ?? "")
                                     .AlignCenter()
                                     .FontSize(10);

                                table.Cell()
                                     .Border(0.25f)
                                     .PaddingLeft(2)
                                     .MinHeight(20)   // min height in points
                                    .BorderColor(QuestPDF.Helpers.Colors.Black)
                                     .Text(d.ServiceProvidedBy ?? "")
                                     .AlignLeft()
                                     .FontSize(9);

                                table.Cell()
                                     .Border(0.25f)
                                     .PaddingLeft(2)
                                     .MinHeight(20)   // min height in points
                                     .BorderColor(QuestPDF.Helpers.Colors.Black)
                                     .Text(d.ExpenseType ?? "")
                                     .AlignLeft()
                                     .FontSize(10);
                            }
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.CurrentPageNumber();
                            txt.Span(" / ");
                            txt.TotalPages();
                        });
                });
            }).GeneratePdf();

            return pdfBytes;
        }
        #endregion
    }
}
