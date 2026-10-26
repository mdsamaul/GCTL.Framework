using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.JobWiseReconcileStatement;
using GCTL.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.JobWiseReconcileStatement
{
    public class JobWiseReconcileStatementService : IJobWiseReconcileStatementService
    {
        #region Private Fields
        private readonly IRepository<SalesCustomer> _customerRepo;
        private readonly IRepository<CfDefExpenseType> _shipmentModeRepo;
        private readonly IRepository<CfBillEntry> _billEntryRepo;
        private readonly string _connectionString;
        #endregion

        #region Constructor
        public JobWiseReconcileStatementService(
            IRepository<SalesCustomer> customerRepo,
            IRepository<CfDefExpenseType> shipmentModeRepo,
            IRepository<CfBillEntry> billEntryRepo,
            IConfiguration configuration)
        {
            _customerRepo = customerRepo;
            _shipmentModeRepo = shipmentModeRepo;
            _billEntryRepo = billEntryRepo;
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? string.Empty;
        }
        #endregion

        #region Filter Options & Cascading
        public async Task<JobWiseReconcileFilterOptionsVM> GetFilterOptionsAsync()
        {
            var vm = new JobWiseReconcileFilterOptionsVM();

            try
            {
                vm.ShipmentModes = await _shipmentModeRepo.All()
                    .Where(x => !string.IsNullOrEmpty(x.ExpenseTypeId) && !string.IsNullOrEmpty(x.ExpenseType))
                    .OrderBy(x => x.ExpenseType)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.ExpenseTypeId,
                        Name = x.ExpenseType
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching ShipmentModes: {ex.Message}");
            }

            try
            {
                vm.Customers = await _customerRepo.All()
                    .Where(x => !string.IsNullOrEmpty(x.CustomerId) && !string.IsNullOrEmpty(x.CustomerName))
                    .OrderBy(x => x.CustomerName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.CustomerId,
                        Name = x.CustomerName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Customers: {ex.Message}");
            }

            try
            {
                vm.JobNos = await _billEntryRepo.All()
                    .Where(x => !string.IsNullOrEmpty(x.JobNo))
                    .Select(x => x.JobNo)
                    .Distinct()
                    .OrderByDescending(x => x)
                    .Take(100)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching default JobNos: {ex.Message}");
            }

            return vm;
        }

        public async Task<List<string>> GetJobNosAsync(string? expenseTypeId, string? customerId, string? fromDate, string? toDate)
        {
            try
            {
                var query = _billEntryRepo.All().Where(x => !string.IsNullOrEmpty(x.JobNo));

                if (!string.IsNullOrWhiteSpace(expenseTypeId))
                {
                    query = query.Where(x => x.ExpenseTypeId == expenseTypeId.Trim());
                }

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    query = query.Where(x => x.CustomerId == customerId.Trim());
                }

                var dFrom = ParseDate(fromDate);
                if (dFrom.HasValue)
                {
                    query = query.Where(x => x.BillDate >= dFrom.Value);
                }

                var dTo = ParseDate(toDate);
                if (dTo.HasValue)
                {
                    var dToInclusive = dTo.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.BillDate <= dToInclusive);
                }

                return await query
                    .Select(x => x.JobNo)
                    .Distinct()
                    .OrderByDescending(x => x)
                    .Take(200)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetJobNosAsync: {ex.Message}");
                return new List<string>();
            }
        }
        #endregion

        #region Report Data Execution
        public async Task<JobWiseReconcileReportResultVM> GetReportDataAsync(JobWiseReconcileStatementFilterVM filter)
        {
            var result = new JobWiseReconcileReportResultVM();
            var dt = new DataTable();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.Rpt_Proc_JobwiseReconcileStatementActual";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 180;

                var fromDate = ParseDate(filter.FromDate) ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var toDate = ParseDate(filter.ToDate) ?? DateTime.Now.Date;

                cmd.Parameters.Add(new SqlParameter("@ExpenseTypeID", SqlDbType.NVarChar, 50) { Value = (object?)filter.ExpenseTypeId ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate });
                cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate });
                cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.NVarChar, 50) { Value = (object?)filter.CustomerId ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@JobNo", SqlDbType.NVarChar, -1) { Value = (object?)filter.JobNo ?? DBNull.Value });

                await using var reader = await cmd.ExecuteReaderAsync();
                dt.Load(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing Rpt_Proc_JobwiseReconcileStatementActual: {ex.Message}");
            }

            if (dt.Columns.Count == 0)
            {
                return result;
            }

            // Map all column names
            foreach (DataColumn col in dt.Columns)
            {
                result.Headers.Add(col.ColumnName);
                if (!string.Equals(col.ColumnName, "JobNo", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(col.ColumnName, "CustomerName", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(col.ColumnName, "BillAmount", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(col.ColumnName, "Revenue/ Total Bill", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(col.ColumnName, "TotalExpense", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(col.ColumnName, "Profit", StringComparison.OrdinalIgnoreCase))
                {
                    result.ExpenseHeadColumns.Add(col.ColumnName);
                }
            }

            decimal totalRevenue = 0m;
            decimal totalExpense = 0m;
            decimal netProfit = 0m;
            var colSums = new Dictionary<string, decimal>();
            foreach (var h in result.Headers)
            {
                colSums[h] = 0m;
            }

            foreach (DataRow row in dt.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    var val = row[col];
                    if (val == DBNull.Value)
                    {
                        rowDict[col.ColumnName] = null!;
                    }
                    else
                    {
                        rowDict[col.ColumnName] = val;
                        if (decimal.TryParse(val.ToString(), out decimal numVal))
                        {
                            colSums[col.ColumnName] += numVal;
                        }
                    }
                }

                // Calculate summary values
                if (rowDict.TryGetValue("BillAmount", out var bAmtObj) && bAmtObj != null &&
                    decimal.TryParse(bAmtObj.ToString(), out decimal bAmt))
                {
                    totalRevenue += bAmt;
                }
                else if (rowDict.TryGetValue("Revenue/ Total Bill", out var revObj) && revObj != null &&
                         decimal.TryParse(revObj.ToString(), out decimal rev))
                {
                    totalRevenue += rev;
                }

                if (rowDict.TryGetValue("TotalExpense", out var tExpObj) && tExpObj != null &&
                    decimal.TryParse(tExpObj.ToString(), out decimal tExp))
                {
                    totalExpense += tExp;
                }

                if (rowDict.TryGetValue("Profit", out var prfObj) && prfObj != null &&
                    decimal.TryParse(prfObj.ToString(), out decimal prf))
                {
                    netProfit += prf;
                }

                result.Rows.Add(rowDict);
            }

            result.Summary.TotalJobs = result.Rows.Count;
            result.Summary.TotalRevenue = totalRevenue;
            result.Summary.TotalExpense = totalExpense;
            result.Summary.NetProfit = netProfit;
            result.Summary.ColumnTotals = colSums;

            return result;
        }
        #endregion

        #region Export Excel (.xls HTML & .xlsx ClosedXML)
        public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportExcelAsync(JobWiseReconcileStatementFilterVM filter)
        {
            var data = await GetReportDataAsync(filter);
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var fromDate = ParseDate(filter.FromDate) ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var toDate = ParseDate(filter.ToDate) ?? DateTime.Now.Date;
            string periodStr = $"Period From:{fromDate:dd/MM/yyyy} To {toDate:dd/MM/yyyy}";

            var xlsxBytes = GenerateClosedXmlExcel(data, periodStr);
            return (xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"JobWiseReconcileStatement_{timeStamp}.xlsx");
        }

        private static byte[] GenerateHtmlExcel(JobWiseReconcileReportResultVM data, string periodStr)
        {
            int totalCols = Math.Max(data.Headers.Count, 5);
            var sb = new StringBuilder();

            sb.Append("<style>.textmode { } </style>");
            sb.Append("<table style='border-collapse: collapse;display: block;width:100%;table-layout: auto !important;border: Thin solid rgb(236,236,236);' id='tableId' runat='server' class='List table table-bordered table-striped table-hover'>");

            // Title Rows
            sb.Append("<tr style='border: Thin solid rgb(236,236,236);'>");
            sb.Append($"<td colspan='{totalCols}' style='border: Thin solid rgb(236,236,236);text-align:center; font-weight:bold; font-size:17px'>FORMOSA LOGISTICS LTD (C&F)</td>");
            sb.Append("</tr>");

            sb.Append("<tr style='border: Thin solid rgb(236,236,236);'>");
            sb.Append($"<td colspan='{totalCols}' style='border: Thin solid rgb(236,236,236);text-align:center; font-weight:bold; font-size:16px'>JOB NUMBER WISE RECONCILE STATEMENT(Actual)</td>");
            sb.Append("</tr>");

            sb.Append("<tr style='border: Thin solid rgb(236,236,236);'>");
            sb.Append($"<td colspan='{totalCols}' style='border: Thin solid rgb(236,236,236);text-align:center; font-weight:bold; font-size:15px'>{periodStr}</td>");
            sb.Append("</tr>");

            // Header Row
            sb.Append("<tr style='border: Thin solid rgb(236,236,236);'>");
            foreach (var col in data.Headers)
            {
                string headerName = col;
                if (string.Equals(col, "BillAmount", StringComparison.OrdinalIgnoreCase))
                {
                    headerName = "Revenue/ Total Bill";
                }
                else if (string.Equals(col, "JobNo", StringComparison.OrdinalIgnoreCase))
                {
                    headerName = "Job No.";
                }
                else if (string.Equals(col, "CustomerName", StringComparison.OrdinalIgnoreCase))
                {
                    headerName = "Customer Name";
                }

                sb.Append($"<td style='border: Thin solid rgb(236,236,236);text-align:center;font-weight:bold; font-size:13px;vertical-align : middle;width:120px;'>{headerName}</td>");
            }
            sb.Append("</tr>");

            // Data Rows
            foreach (var row in data.Rows)
            {
                sb.Append("<tr style='border: Thin solid rgb(236,236,236);'>");
                foreach (var col in data.Headers)
                {
                    row.TryGetValue(col, out var val);
                    bool isJobOrCustomer = string.Equals(col, "JobNo", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(col, "CustomerName", StringComparison.OrdinalIgnoreCase);

                    if (isJobOrCustomer)
                    {
                        sb.Append($"<td style='text-align:center; border: Thin solid rgb(236,236,236);font-size:13px'>{val ?? ""}</td>");
                    }
                    else
                    {
                        if (val != null && decimal.TryParse(val.ToString(), out decimal decVal) && decVal != 0)
                        {
                            sb.Append($"<td style='text-align:right;border: Thin solid rgb(236,236,236); font-size:12px;mso-number-format:\\#\\,\\#\\#0\\.00;'>{decVal:0.00}</td>");
                        }
                        else
                        {
                            sb.Append("<td style='text-align:right;border: Thin solid rgb(236,236,236); font-size:12px;mso-number-format:\\#\\,\\#\\#0\\.00;'></td>");
                        }
                    }
                }
                sb.Append("</tr>");
            }

            // Total Row
            if (data.Rows.Count > 0)
            {
                sb.Append("<tr style='border: Thin solid rgb(236,236,236);font-weight:bold;background-color:#f1f5f9;'>");
                for (int i = 0; i < data.Headers.Count; i++)
                {
                    var col = data.Headers[i];
                    if (i == 0)
                    {
                        sb.Append("<td style='text-align:center;border: Thin solid rgb(236,236,236);font-size:13px;font-weight:bold;'>TOTAL</td>");
                    }
                    else if (string.Equals(col, "CustomerName", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append("<td style='border: Thin solid rgb(236,236,236);'></td>");
                    }
                    else
                    {
                        data.Summary.ColumnTotals.TryGetValue(col, out decimal totalVal);
                        sb.Append($"<td style='text-align:right;border: Thin solid rgb(236,236,236);font-size:12px;font-weight:bold;mso-number-format:\\#\\,\\#\\#0\\.00;'>{totalVal:0.00}</td>");
                    }
                }
                sb.Append("</tr>");
            }

            sb.Append("</table>");

            // Encode with UTF-8 preamble for proper Excel character display
            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);
            return result;
        }

        private static byte[] GenerateClosedXmlExcel(JobWiseReconcileReportResultVM data, string periodStr)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Job Wise Reconcile");

            int colCount = Math.Max(data.Headers.Count, 5);

            // Title Rows
            ws.Cell(1, 1).Value = "FORMOSA LOGISTICS LTD (C&F)";
            ws.Range(1, 1, 1, colCount).Merge().Style.Font.Bold = true;
            ws.Range(1, 1, 1, colCount).Style.Font.FontSize = 15;
            ws.Range(1, 1, 1, colCount).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(2, 1).Value = "JOB NUMBER WISE RECONCILE STATEMENT(Actual)";
            ws.Range(2, 1, 2, colCount).Merge().Style.Font.Bold = true;
            ws.Range(2, 1, 2, colCount).Style.Font.FontSize = 13;
            ws.Range(2, 1, 2, colCount).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(3, 1).Value = periodStr;
            ws.Range(3, 1, 3, colCount).Merge().Style.Font.Bold = true;
            ws.Range(3, 1, 3, colCount).Style.Font.FontSize = 11;
            ws.Range(3, 1, 3, colCount).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Header Row
            int headerRowIdx = 4;
            for (int c = 0; c < data.Headers.Count; c++)
            {
                string headerName = data.Headers[c];
                if (string.Equals(headerName, "BillAmount", StringComparison.OrdinalIgnoreCase))
                    headerName = "Revenue/ Total Bill";
                else if (string.Equals(headerName, "JobNo", StringComparison.OrdinalIgnoreCase))
                    headerName = "Job No.";
                else if (string.Equals(headerName, "CustomerName", StringComparison.OrdinalIgnoreCase))
                    headerName = "Customer Name";

                var cell = ws.Cell(headerRowIdx, c + 1);
                cell.Value = headerName;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 11;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D1D5DB");
            }

            // Data Rows
            int currentRow = 5;
            foreach (var row in data.Rows)
            {
                for (int c = 0; c < data.Headers.Count; c++)
                {
                    var col = data.Headers[c];
                    var cell = ws.Cell(currentRow, c + 1);
                    row.TryGetValue(col, out var val);

                    bool isJobOrCustomer = string.Equals(col, "JobNo", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(col, "CustomerName", StringComparison.OrdinalIgnoreCase);

                    if (isJobOrCustomer)
                    {
                        cell.Value = val?.ToString() ?? "";
                        cell.Style.Alignment.Horizontal = string.Equals(col, "CustomerName", StringComparison.OrdinalIgnoreCase)
                            ? XLAlignmentHorizontalValues.Left
                            : XLAlignmentHorizontalValues.Center;
                    }
                    else
                    {
                        if (val != null && decimal.TryParse(val.ToString(), out decimal decVal) && decVal != 0)
                        {
                            cell.Value = decVal;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else
                        {
                            cell.Value = "";
                        }
                    }

                    cell.Style.Font.FontSize = 10;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                }
                currentRow++;
            }

            // Total Row
            if (data.Rows.Count > 0)
            {
                for (int c = 0; c < data.Headers.Count; c++)
                {
                    var col = data.Headers[c];
                    var cell = ws.Cell(currentRow, c + 1);

                    if (c == 0)
                    {
                        cell.Value = "TOTAL";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (string.Equals(col, "CustomerName", StringComparison.OrdinalIgnoreCase))
                    {
                        cell.Value = "";
                    }
                    else
                    {
                        data.Summary.ColumnTotals.TryGetValue(col, out decimal totalVal);
                        cell.Value = totalVal;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }

                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 10;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                }
            }

            ws.Columns().AdjustToContents(10, 45);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static DateTime? ParseDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy" };
            if (DateTime.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            if (DateTime.TryParse(input.Trim(), out dt))
            {
                return dt;
            }
            return null;
        }
        #endregion
    }
}
