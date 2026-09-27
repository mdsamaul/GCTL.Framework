using ClosedXML.Excel;
using GCTL.Core.ViewModels.JobStatusReport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.JobStatusReport
{
    public class JobStatusReportService : IJobStatusReportService
    {
        private readonly string _connectionString;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _webHostEnvironment;
        private byte[]? _logoBytes;
        private bool _logoLoaded;

        public JobStatusReportService(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null)
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
            _webHostEnvironment = env;
        }

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

        #region 1. Summary Counts (Using prc_JobPortal_JobStatusExceptAll)
        public async Task<JobStatusSummaryDTO> GetSummaryAsync(string? customerId, string? shipmentModeId = null)
        {
            var summary = new JobStatusSummaryDTO();
            if (string.IsNullOrWhiteSpace(_connectionString)) return summary;

            string sModeId = (shipmentModeId ?? "").Trim();
            if (string.Equals(sModeId, "Air", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sModeId, "Sea", StringComparison.OrdinalIgnoreCase))
            {
                sModeId = "";
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("prc_JobPortal_JobStatusExceptAll", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(customerId) ? "" : customerId.Trim()));
                cmd.Parameters.Add(new SqlParameter("@ShipmentModeId", sModeId));

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.FieldCount == 0 && await reader.NextResultAsync()) { }

                if (reader.FieldCount > 0 && await reader.ReadAsync())
                {
                    summary.TotalJobs = reader["TotalJob"] != DBNull.Value ? Convert.ToInt32(reader["TotalJob"]) : 0;
                    summary.PendingJobs = reader["TotalPending"] != DBNull.Value ? Convert.ToInt32(reader["TotalPending"]) : 0;
                    summary.TransitJobs = reader["totalInTransit"] != DBNull.Value ? Convert.ToInt32(reader["totalInTransit"]) : 0;
                    summary.DeliveredJobs = reader["TotalDelivered"] != DBNull.Value ? Convert.ToInt32(reader["TotalDelivered"]) : 0;
                }
            }
            catch
            {
                // Fallback will calculate from datasets
            }

            return summary;
        }
        #endregion

        #region 2. Sea Freight Jobs (Stored Procedures: Pending, InTransit, Delivered)
        public async Task<List<SeaJobReportItemDTO>> GetSeaJobsAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<SeaJobReportItemDTO>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            var status = (filter.JobStatus ?? "").Trim().ToLower();

            try
            {
                if (status == "pending" || status.Contains("pend"))
                {
                    list = await ExecuteSeaProcAsync("Proc_CustomerJobStatusPendingReportForSea", "pending", filter);
                }
                else if (status == "in transit" || status == "transit")
                {
                    list = await ExecuteSeaProcAsync("Proc_CustomerJobStatusInTransitSea", "transit", filter);
                }
                else if (status == "delivered" || status.Contains("deliver"))
                {
                    list = await ExecuteSeaProcAsync("Proc_CustomerJobStatusDeliveredSea", "delivered", filter);
                }
                else
                {
                    var pendingTask = ExecuteSeaProcAsync("Proc_CustomerJobStatusPendingReportForSea", "pending", filter);
                    var transitTask = ExecuteSeaProcAsync("Proc_CustomerJobStatusInTransitSea", "transit", filter);
                    var deliveryTask = ExecuteSeaProcAsync("Proc_CustomerJobStatusDeliveredSea", "delivered", filter);

                    await Task.WhenAll(pendingTask, transitTask, deliveryTask);

                    list.AddRange(pendingTask.Result);
                    list.AddRange(transitTask.Result);
                    list.AddRange(deliveryTask.Result);
                }

                list = list.OrderBy(x => x.JobNo).ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].SlNo = i + 1;
                }
            }
            catch
            {
                // Fail gracefully
            }

            return list;
        }

        private async Task<List<SeaJobReportItemDTO>> ExecuteSeaProcAsync(string procName, string statusClass, JobStatusReportFilterVM filter)
        {
            var list = new List<SeaJobReportItemDTO>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);

            string shipmentTypeId = (filter.ShipmentMode ?? "").Trim();
            if (string.Equals(shipmentTypeId, "Air", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shipmentTypeId, "Sea", StringComparison.OrdinalIgnoreCase))
            {
                shipmentTypeId = "";
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(procName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;
            cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
            cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ToDate", toDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
            cmd.Parameters.Add(new SqlParameter("@PortTypeId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
            cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", shipmentTypeId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.FieldCount == 0 && await reader.NextResultAsync()) { }

            // Pre-cache all column ordinals once (0 overhead per row)
            int ordJobNo = GetColOrdinal(reader, "JobNo");
            int ordJobDate = GetColOrdinal(reader, "JobDate");
            int ordExpenseType = GetColOrdinal(reader, "ExpenseType");
            int ordPortName = GetColOrdinal(reader, "PortName");
            int ordDocReceivedDate = GetColOrdinal(reader, "DocReceivedDate");
            int ordInvoiceNo = GetColOrdinal(reader, "InvoiceNo");
            int ordInvoiceValue = GetColOrdinal(reader, "InvoiceValue");
            int ordContainerPkgs = GetColOrdinal(reader, "ContainerPkgs");
            int ordShipper = GetColOrdinal(reader, "Shipper");
            int ordBillNo = GetColOrdinal(reader, "BillNo");
            int ordShipmentStatus = GetColOrdinal(reader, "ShipmentStatus");
            int ordEtdDate = GetColOrdinal(reader, "EtdDate");
            int ordFinalIgmRecDt = GetColOrdinal(reader, "FinalIgmRecDt");
            int ordShipArrivedDate = GetColOrdinal(reader, "ShipArrivedDate");
            int ordDischargeDate = GetColOrdinal(reader, "DischargeDate");
            int ordDocsBankSignDate = GetColOrdinal(reader, "DocsBankSignDate");
            int ordBeNo = GetColOrdinal(reader, "BeNo");
            int ordBeDate = GetColOrdinal(reader, "BeDate");
            int ordEstDeliveryDate = GetColOrdinal(reader, "EstimatedDeliveryDate");
            int ordActualDeliveryDate = GetColOrdinal(reader, "ActualDeliveryDate");
            int ordRemarks = GetColOrdinal(reader, "Remarks");
            int ordTerm = GetColOrdinal(reader, "Term");
            int ordVesselName = GetColOrdinal(reader, "VesselName");
            int ordContainerNo = GetColOrdinal(reader, "ContainerNo");
            int ordFreightForwarder = GetColOrdinal(reader, "FreightForwarder");
            int ordCustomerId = GetColOrdinal(reader, "CustomerID");

            while (await reader.ReadAsync())
            {
                string rawStatus = ReadCol(reader, ordShipmentStatus);
                string resolvedStatus = !string.IsNullOrEmpty(rawStatus) ? rawStatus :
                    (statusClass == "delivered" ? "Delivered" : (statusClass == "transit" ? "In Transit" : "Pending"));

                string expType = ReadCol(reader, ordExpenseType);
                string custId = ReadCol(reader, ordCustomerId);

                list.Add(new SeaJobReportItemDTO
                {
                    JobNo = ReadCol(reader, ordJobNo),
                    JobDate = ReadCol(reader, ordJobDate),
                    ShipmentMode = !string.IsNullOrEmpty(expType) ? expType : "Sea Freight",
                    Port = ReadCol(reader, ordPortName),
                    DocReceivedDate = ReadCol(reader, ordDocReceivedDate),
                    InvoiceNo = ReadCol(reader, ordInvoiceNo),
                    InvoiceValue = ReadCol(reader, ordInvoiceValue),
                    ContainerPkgs = ReadCol(reader, ordContainerPkgs),
                    Shipper = ReadCol(reader, ordShipper),
                    BLNo = ReadCol(reader, ordBillNo),
                    ShipmentStatus = resolvedStatus,
                    ETDDate = ReadCol(reader, ordEtdDate),
                    FinalIgmRecDt = ReadCol(reader, ordFinalIgmRecDt),
                    ShipArrivedDate = ReadCol(reader, ordShipArrivedDate),
                    DischargeDate = ReadCol(reader, ordDischargeDate),
                    DocsBankSignDate = ReadCol(reader, ordDocsBankSignDate),
                    BENo = ReadCol(reader, ordBeNo),
                    BEDate = ReadCol(reader, ordBeDate),
                    EstDeliveryDate = ReadCol(reader, ordEstDeliveryDate),
                    ActualDeliveryDate = ReadCol(reader, ordActualDeliveryDate),
                    Remarks = ReadCol(reader, ordRemarks),
                    Term = ReadCol(reader, ordTerm),
                    VesselName = ReadCol(reader, ordVesselName),
                    ContainerNo = ReadCol(reader, ordContainerNo),
                    FreightForwarder = ReadCol(reader, ordFreightForwarder),
                    CustomerId = string.IsNullOrWhiteSpace(filter.CustomerId) ? custId : filter.CustomerId.Trim(),
                    StatusClass = statusClass
                });
            }

            return list;
        }
        #endregion

        #region 3. Air Freight Jobs (Stored Procedures: Pending, InTransit, Delivered)
        public async Task<List<AirJobReportItemDTO>> GetAirJobsAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<AirJobReportItemDTO>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            var status = (filter.JobStatus ?? "").Trim().ToLower();

            try
            {
                if (status == "pending" || status.Contains("pend"))
                {
                    list = await ExecuteAirProcAsync("Proc_CustomerJobStatusPendingExceptAll", "pending", filter);
                }
                else if (status == "in transit" || status == "transit")
                {
                    list = await ExecuteAirProcAsync("Proc_CustomerJobStatusInTransitExceptAll", "transit", filter);
                }
                else if (status == "delivered" || status.Contains("deliver"))
                {
                    list = await ExecuteAirProcAsync("Proc_CustomerJobStatusDeliveryExceptAll", "delivered", filter);
                }
                else
                {
                    var pendingTask = ExecuteAirProcAsync("Proc_CustomerJobStatusPendingExceptAll", "pending", filter);
                    var transitTask = ExecuteAirProcAsync("Proc_CustomerJobStatusInTransitExceptAll", "transit", filter);
                    var deliveryTask = ExecuteAirProcAsync("Proc_CustomerJobStatusDeliveryExceptAll", "delivered", filter);

                    await Task.WhenAll(pendingTask, transitTask, deliveryTask);

                    list.AddRange(pendingTask.Result);
                    list.AddRange(transitTask.Result);
                    list.AddRange(deliveryTask.Result);
                }

                list = list.OrderBy(x => x.JobNo).ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].SlNo = i + 1;
                }
            }
            catch
            {
                // Fail gracefully
            }

            return list;
        }

        private async Task<List<AirJobReportItemDTO>> ExecuteAirProcAsync(string procName, string statusClass, JobStatusReportFilterVM filter)
        {
            var list = new List<AirJobReportItemDTO>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);

            string shipmentTypeId = (filter.ShipmentMode ?? "").Trim();
            if (string.Equals(shipmentTypeId, "Air", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shipmentTypeId, "Sea", StringComparison.OrdinalIgnoreCase))
            {
                shipmentTypeId = "";
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(procName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;
            cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
            cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ToDate", toDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
            cmd.Parameters.Add(new SqlParameter("@PortTypeId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
            cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", shipmentTypeId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.FieldCount == 0 && await reader.NextResultAsync()) { }

            // Pre-cache all column ordinals once (0 overhead per row)
            int ordJobNo = GetColOrdinal(reader, "JobNo");
            int ordJobDate = GetColOrdinal(reader, "JobDate");
            int ordExpenseType = GetColOrdinal(reader, "ExpenseType");
            int ordPortName = GetColOrdinal(reader, "PortName");
            int ordDocReceivedDate = GetColOrdinal(reader, "DocReceivedDate");
            int ordInvoiceNo = GetColOrdinal(reader, "InvoiceNo");
            int ordInvoiceValue = GetColOrdinal(reader, "InvoiceValue");
            int ordMaterialNamePkgsQty = GetColOrdinal(reader, "MaterialNamePkgsQty");
            int ordExpressNameCountry = GetColOrdinal(reader, "ExpressNameCountry");
            int ordLcNo = GetColOrdinal(reader, "LcNo");
            int ordHawbNo = GetColOrdinal(reader, "HawbNo");
            int ordMawbNo = GetColOrdinal(reader, "MawbNo");
            int ordEtdDate = GetColOrdinal(reader, "EtdDate");
            int ordFlightActualDeliveryDate = GetColOrdinal(reader, "FlightActualDeliveryDate");
            int ordArrivalNoticeRcvdExpressDate = GetColOrdinal(reader, "ArrivalNoticeRcvdExpressDate");
            int ordBeNo = GetColOrdinal(reader, "BeNo");
            int ordBeDate = GetColOrdinal(reader, "BeDate");
            int ordActualDeliveryDate = GetColOrdinal(reader, "ActualDeliveryDate");
            int ordRemarks = GetColOrdinal(reader, "Remarks");
            int ordCustomerId = GetColOrdinal(reader, "CustomerID");
            int ordShipmentStatus = GetColOrdinal(reader, "ShipmentStatus");

            while (await reader.ReadAsync())
            {
                string rawStatus = ReadCol(reader, ordShipmentStatus);
                string resolvedStatus = !string.IsNullOrEmpty(rawStatus) ? rawStatus :
                    (statusClass == "delivered" ? "Delivered" : (statusClass == "transit" ? "In Transit" : "Pending"));

                string expType = ReadCol(reader, ordExpenseType);
                string custId = ReadCol(reader, ordCustomerId);

                list.Add(new AirJobReportItemDTO
                {
                    JobNo = ReadCol(reader, ordJobNo),
                    JobDate = ReadCol(reader, ordJobDate),
                    ShipmentMode = !string.IsNullOrEmpty(expType) ? expType : "Air Freight",
                    Port = ReadCol(reader, ordPortName),
                    DocReceivedDate = ReadCol(reader, ordDocReceivedDate),
                    InvoiceNo = ReadCol(reader, ordInvoiceNo),
                    InvoiceValue = ReadCol(reader, ordInvoiceValue),
                    MaterialNamePkgsQty = ReadCol(reader, ordMaterialNamePkgsQty),
                    ExpressNameCountry = ReadCol(reader, ordExpressNameCountry),
                    LCNo = ReadCol(reader, ordLcNo),
                    HawbNo = ReadCol(reader, ordHawbNo),
                    MawbNo = ReadCol(reader, ordMawbNo),
                    ShipmentStatus = resolvedStatus,
                    ETDDate = ReadCol(reader, ordEtdDate),
                    FlightActualDeliveryDate = ReadCol(reader, ordFlightActualDeliveryDate),
                    ArrivalNoticeRcvdExpressDate = ReadCol(reader, ordArrivalNoticeRcvdExpressDate),
                    BENo = ReadCol(reader, ordBeNo),
                    BEDate = ReadCol(reader, ordBeDate),
                    ActualDeliveryDate = ReadCol(reader, ordActualDeliveryDate),
                    Remarks = ReadCol(reader, ordRemarks),
                    CustomerId = string.IsNullOrWhiteSpace(filter.CustomerId) ? custId : filter.CustomerId.Trim(),
                    StatusClass = statusClass
                });
            }

            return list;
        }
        #endregion

        #region 4. Generate Excel Report (Grouped by Customer, Sea & Air Separate)
        public async Task<byte[]> GenerateExcelReportAsync(JobStatusReportFilterVM filter)
        {
            var seaTask = GetSeaJobsAsync(filter);
            var airTask = GetAirJobsAsync(filter);
            var custNameTask = GetCustomerNameAsync(filter.CustomerId);
            var custDictTask = GetCustomerNamesDictAsync();

            await Task.WhenAll(seaTask, airTask, custNameTask, custDictTask);

            var seaJobs = seaTask.Result;
            var airJobs = airTask.Result;
            string selectedCustomerName = custNameTask.Result;
            var custDict = custDictTask.Result;

            foreach (var item in seaJobs)
            {
                if (string.IsNullOrWhiteSpace(item.CustomerName))
                {
                    if (!string.IsNullOrWhiteSpace(item.CustomerId) && custDict.TryGetValue(item.CustomerId, out var name))
                        item.CustomerName = name;
                    else if (!string.IsNullOrWhiteSpace(selectedCustomerName))
                        item.CustomerName = selectedCustomerName;
                    else
                        item.CustomerName = "General Customer";
                }
            }
            foreach (var item in airJobs)
            {
                if (string.IsNullOrWhiteSpace(item.CustomerName))
                {
                    if (!string.IsNullOrWhiteSpace(item.CustomerId) && custDict.TryGetValue(item.CustomerId, out var name))
                        item.CustomerName = name;
                    else if (!string.IsNullOrWhiteSpace(selectedCustomerName))
                        item.CustomerName = selectedCustomerName;
                    else
                        item.CustomerName = "General Customer";
                }
            }

            string activeTab = (filter.ActiveTab ?? "").Trim().ToLower();

            using var workbook = new XLWorkbook();

            if (activeTab != "air")
            {
                BuildSeaExcelWorksheet(workbook, seaJobs, filter);
            }

            if (activeTab != "sea")
            {
                BuildAirExcelWorksheet(workbook, airJobs, filter);
            }

            if (workbook.Worksheets.Count == 0)
            {
                var ws = workbook.Worksheets.Add("No Records");
                ws.Cell(1, 1).Value = "No job status records found for the selected filter criteria.";
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static void BuildSeaExcelWorksheet(XLWorkbook workbook, List<SeaJobReportItemDTO> seaJobs, JobStatusReportFilterVM filter)
        {
            var ws = workbook.Worksheets.Add("Sea Port Jobs");
            const int totalCols = 26;

            ws.Range(1, 1, 1, totalCols).Merge();
            var titleCell = ws.Cell(1, 1);
            titleCell.Value = "Formosa Logistics Limited";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            titleCell.Style.Font.FontName = "Times New Roman";
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(2, 1, 2, totalCols).Merge();
            var subCell = ws.Cell(2, 1);
            subCell.Value = "Job Status Report - Formosa Logistics (Sea Port)";
            subCell.Style.Font.Bold = true;
            subCell.Style.Font.FontSize = 11;
            subCell.Style.Font.FontName = "Times New Roman";
            subCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(3, 1, 3, totalCols).Merge();
            var infoCell = ws.Cell(3, 1);
            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "Date Range: All Dates";
            infoCell.Value = dateRangeStr;
            infoCell.Style.Font.FontSize = 9.5;
            infoCell.Style.Font.Bold = true;
            infoCell.Style.Font.FontName = "Times New Roman";
            infoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] seaHeaders = new string[]
            {
                "Sl. No.", "Job No.", "Job Date", "Shipment Mode", "Port", "Doc's Rec. Dt.",
                "Invoice No", "Invoice Value", "Container /Pkgs", "Shipper", "B/L No.", "ETD",
                "Final IGM Rec. Dt.", "Ship Arrived", "Unstaff/Discharge", "Docs. Bank Sign",
                "B/E No.", "B/E Date", "Est. Delivery Dt.", "Actual Delivery Dt.", "Shipment Status",
                "Remarks", "Term", "Vessel Name & R-No", "Container No.", "Forwarder Name & Phone No."
            };

            int currRow = 5;
            var customerGroups = seaJobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            if (customerGroups.Count == 0)
            {
                WriteHeaders(ws, currRow, seaHeaders, totalCols);
                currRow++;
                ws.Cell(currRow, 1).Value = "No Sea Freight records found.";
                ws.Range(currRow, 1, currRow, totalCols).Merge();
                ws.Cell(currRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                AutoFitColumnsToContent(ws, totalCols);
                return;
            }

            foreach (var group in customerGroups)
            {
                ws.Range(currRow, 1, currRow, totalCols).Merge();
                var custHeaderCell = ws.Cell(currRow, 1);
                custHeaderCell.Value = $"CUSTOMER: {group.Key.ToUpper()}  (Total Jobs: {group.Count()})";
                custHeaderCell.Style.Font.Bold = true;
                custHeaderCell.Style.Font.FontSize = 10;
                custHeaderCell.Style.Font.FontColor = XLColor.Black;
                custHeaderCell.Style.Fill.PatternType = XLFillPatternValues.None;
                custHeaderCell.Style.Border.OutsideBorder = XLBorderStyleValues.None;
                custHeaderCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                custHeaderCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(currRow).Height = 22;
                currRow++;

                WriteHeaders(ws, currRow, seaHeaders, totalCols);
                ws.Row(currRow).Height = 25;
                currRow++;

                int customerSl = 1;
                foreach (var item in group)
                {
                    ws.Cell(currRow, 1).Value = customerSl++;
                    ws.Cell(currRow, 2).Value = item.JobNo;
                    ws.Cell(currRow, 3).Value = item.JobDate;
                    ws.Cell(currRow, 4).Value = item.ShipmentMode;
                    ws.Cell(currRow, 5).Value = item.Port;
                    ws.Cell(currRow, 6).Value = item.DocReceivedDate;
                    ws.Cell(currRow, 7).Value = item.InvoiceNo;

                    string rawInv = Convert.ToString(item.InvoiceValue)?.Replace(",", "").Trim() ?? "";
                    if (decimal.TryParse(rawInv, NumberStyles.Any, CultureInfo.InvariantCulture, out var decInv))
                    {
                        ws.Cell(currRow, 8).Value = decInv;
                        ws.Cell(currRow, 8).Style.NumberFormat.Format = decInv % 1 == 0 ? "#,##0" : "#,##0.00";
                    }
                    else
                    {
                        ws.Cell(currRow, 8).Value = item.InvoiceValue;
                    }
                    ws.Cell(currRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    ws.Cell(currRow, 9).Value = item.ContainerPkgs;
                    ws.Cell(currRow, 10).Value = item.Shipper;
                    ws.Cell(currRow, 11).Value = item.BLNo;
                    ws.Cell(currRow, 12).Value = item.ETDDate;
                    ws.Cell(currRow, 13).Value = item.FinalIgmRecDt;
                    ws.Cell(currRow, 14).Value = item.ShipArrivedDate;
                    ws.Cell(currRow, 15).Value = item.DischargeDate;
                    ws.Cell(currRow, 16).Value = item.DocsBankSignDate;
                    ws.Cell(currRow, 17).Value = item.BENo;
                    ws.Cell(currRow, 18).Value = item.BEDate;
                    ws.Cell(currRow, 19).Value = item.EstDeliveryDate;
                    ws.Cell(currRow, 20).Value = item.ActualDeliveryDate;
                    ws.Cell(currRow, 21).Value = item.ShipmentStatus;
                    ws.Cell(currRow, 22).Value = item.Remarks;
                    ws.Cell(currRow, 23).Value = item.Term;
                    ws.Cell(currRow, 24).Value = item.VesselName;
                    ws.Cell(currRow, 25).Value = item.ContainerNo;
                    ws.Cell(currRow, 26).Value = item.FreightForwarder;

                    for (int c = 1; c <= totalCols; c++)
                    {
                        var cell = ws.Cell(currRow, c);
                        cell.Style.Font.FontSize = 8.5;
                        cell.Style.Font.FontName = "Times New Roman";
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CCCCCC");
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        if (c == 8)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else if (c == 1 || c == 2 || c == 3 || c == 6 || c == 11 || c == 12 || c == 13 || c == 14 || c == 15 || c == 16 || c == 17 || c == 18 || c == 19 || c == 20 || c == 21 || c == 23)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                    }

                    currRow++;
                }

                currRow++;
            }

            AutoFitColumnsToContent(ws, totalCols);
        }

        private static void BuildAirExcelWorksheet(XLWorkbook workbook, List<AirJobReportItemDTO> airJobs, JobStatusReportFilterVM filter)
        {
            var ws = workbook.Worksheets.Add("Air Port Jobs");
            const int totalCols = 21;

            ws.Range(1, 1, 1, totalCols).Merge();
            var titleCell = ws.Cell(1, 1);
            titleCell.Value = "Formosa Logistics Limited";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            titleCell.Style.Font.FontName = "Times New Roman";
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(2, 1, 2, totalCols).Merge();
            var subCell = ws.Cell(2, 1);
            subCell.Value = "Job Status Report - Formosa Logistics (Air Port)";
            subCell.Style.Font.Bold = true;
            subCell.Style.Font.FontSize = 11;
            subCell.Style.Font.FontName = "Times New Roman";
            subCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(3, 1, 3, totalCols).Merge();
            var infoCell = ws.Cell(3, 1);
            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "Date Range: All Dates";
            infoCell.Value = dateRangeStr;
            infoCell.Style.Font.FontSize = 9.5;
            infoCell.Style.Font.Bold = true;
            infoCell.Style.Font.FontName = "Times New Roman";
            infoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] airHeaders = new string[]
            {
                "SL. No", "Job No.", "Job Date", "Shipment Mode", "Port", "Doc's Receive Date",
                "Invoice No.", "Invoice Value", "Material Name & Pkgs Qty", "Express Name, Country",
                "L/C No.", "Express/HAWB No.", "MAWB No.", "ETD", "Flight Actual Arrival Dt.",
                "Arrival Notice Rcvd From Express Dt.", "Bill of Entry No", "Bill of Entry Date",
                "Actual Delivery Date", "Shipment Status", "Remarks"
            };

            int currRow = 5;
            var customerGroups = airJobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            if (customerGroups.Count == 0)
            {
                WriteHeaders(ws, currRow, airHeaders, totalCols);
                currRow++;
                ws.Cell(currRow, 1).Value = "No Air Freight records found.";
                ws.Range(currRow, 1, currRow, totalCols).Merge();
                ws.Cell(currRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                AutoFitColumnsToContent(ws, totalCols);
                return;
            }

            foreach (var group in customerGroups)
            {
                ws.Range(currRow, 1, currRow, totalCols).Merge();
                var custHeaderCell = ws.Cell(currRow, 1);
                custHeaderCell.Value = $"CUSTOMER: {group.Key.ToUpper()}  (Total Jobs: {group.Count()})";
                custHeaderCell.Style.Font.Bold = true;
                custHeaderCell.Style.Font.FontSize = 10;
                custHeaderCell.Style.Font.FontColor = XLColor.Black;
                custHeaderCell.Style.Fill.PatternType = XLFillPatternValues.None;
                custHeaderCell.Style.Border.OutsideBorder = XLBorderStyleValues.None;
                custHeaderCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                custHeaderCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(currRow).Height = 22;
                currRow++;

                WriteHeaders(ws, currRow, airHeaders, totalCols);
                ws.Row(currRow).Height = 25;
                currRow++;

                int customerSl = 1;
                foreach (var item in group)
                {
                    ws.Cell(currRow, 1).Value = customerSl++;
                    ws.Cell(currRow, 2).Value = item.JobNo;
                    ws.Cell(currRow, 3).Value = item.JobDate;
                    ws.Cell(currRow, 4).Value = item.ShipmentMode;
                    ws.Cell(currRow, 5).Value = item.Port;
                    ws.Cell(currRow, 6).Value = item.DocReceivedDate;
                    ws.Cell(currRow, 7).Value = item.InvoiceNo;

                    string rawInvAir = Convert.ToString(item.InvoiceValue)?.Replace(",", "").Trim() ?? "";
                    if (decimal.TryParse(rawInvAir, NumberStyles.Any, CultureInfo.InvariantCulture, out var decInvAir))
                    {
                        ws.Cell(currRow, 8).Value = decInvAir;
                        ws.Cell(currRow, 8).Style.NumberFormat.Format = decInvAir % 1 == 0 ? "#,##0" : "#,##0.00";
                    }
                    else
                    {
                        ws.Cell(currRow, 8).Value = item.InvoiceValue;
                    }
                    ws.Cell(currRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    ws.Cell(currRow, 9).Value = item.MaterialNamePkgsQty;
                    ws.Cell(currRow, 10).Value = item.ExpressNameCountry;
                    ws.Cell(currRow, 11).Value = item.LCNo;
                    ws.Cell(currRow, 12).Value = item.HawbNo;
                    ws.Cell(currRow, 13).Value = item.MawbNo;
                    ws.Cell(currRow, 14).Value = item.ETDDate;
                    ws.Cell(currRow, 15).Value = item.FlightActualDeliveryDate;
                    ws.Cell(currRow, 16).Value = item.ArrivalNoticeRcvdExpressDate;
                    ws.Cell(currRow, 17).Value = item.BENo;
                    ws.Cell(currRow, 18).Value = item.BEDate;
                    ws.Cell(currRow, 19).Value = item.ActualDeliveryDate;
                    ws.Cell(currRow, 20).Value = item.ShipmentStatus;
                    ws.Cell(currRow, 21).Value = item.Remarks;

                    for (int c = 1; c <= totalCols; c++)
                    {
                        var cell = ws.Cell(currRow, c);
                        cell.Style.Font.FontSize = 8.5;
                        cell.Style.Font.FontName = "Times New Roman";
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CCCCCC");
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        if (c == 8)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else if (c == 1 || c == 2 || c == 3 || c == 6 || c == 11 || c == 12 || c == 13 || c == 14 || c == 15 || c == 16 || c == 17 || c == 18 || c == 19 || c == 20)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                    }

                    currRow++;
                }

                currRow++;
            }

            AutoFitColumnsToContent(ws, totalCols);
        }

        private static void WriteHeaders(IXLWorksheet ws, int row, string[] headers, int totalCols)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 8.5;
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Fill.PatternType = XLFillPatternValues.None;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#999999");
            }
        }

        private static void AutoFitColumnsToContent(IXLWorksheet ws, int totalCols)
        {
            ws.Columns(1, totalCols).AdjustToContents();
        }
        #endregion

        #region 5. Generate PDF Report (Grouped by Customer, Sea & Air Separate)
        public async Task<byte[]> GeneratePdfReportAsync(JobStatusReportFilterVM filter)
        {
            var seaTask = GetSeaJobsAsync(filter);
            var airTask = GetAirJobsAsync(filter);
            var custNameTask = GetCustomerNameAsync(filter.CustomerId);
            var custDictTask = GetCustomerNamesDictAsync();

            await Task.WhenAll(seaTask, airTask, custNameTask, custDictTask);

            var seaJobs = seaTask.Result;
            var airJobs = airTask.Result;
            string selectedCustomerName = custNameTask.Result;
            var custDict = custDictTask.Result;

            // Populate CustomerName on all items
            foreach (var item in seaJobs)
            {
                if (string.IsNullOrWhiteSpace(item.CustomerName))
                {
                    if (!string.IsNullOrWhiteSpace(item.CustomerId) && custDict.TryGetValue(item.CustomerId, out var name))
                        item.CustomerName = name;
                    else if (!string.IsNullOrWhiteSpace(selectedCustomerName))
                        item.CustomerName = selectedCustomerName;
                    else
                        item.CustomerName = "General Customer";
                }
            }
            foreach (var item in airJobs)
            {
                if (string.IsNullOrWhiteSpace(item.CustomerName))
                {
                    if (!string.IsNullOrWhiteSpace(item.CustomerId) && custDict.TryGetValue(item.CustomerId, out var name))
                        item.CustomerName = name;
                    else if (!string.IsNullOrWhiteSpace(selectedCustomerName))
                        item.CustomerName = selectedCustomerName;
                    else
                        item.CustomerName = "General Customer";
                }
            }

            string activeTab = (filter.ActiveTab ?? "").Trim().ToLower();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.MarginHorizontal(8);
                    page.MarginVertical(8);
                    page.Size(PageSizes.A4.Landscape());

                    page.Header().Column(headerCol =>
                    {
                        var logoBytes = GetLogoBytes();

                        headerCol.Item().Row(r =>
                        {
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                // Smaller logo height & width per user request
                                r.ConstantItem(60).AlignMiddle().Height(18).Image(logoBytes).FitArea();
                            }
                            else
                            {
                                r.ConstantItem(60);
                            }

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Formosa Logistics Limited").FontSize(11).Bold();
                                c.Item().AlignCenter().Text("Job Status Report - Formosa Logistics").FontSize(8.5f).Bold();

                                string dateRange = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                                    ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "Date Range: All Dates";
                                c.Item().AlignCenter().Text(dateRange).FontSize(6.8f).SemiBold();
                            });

                            r.ConstantItem(60);
                        });

                        // Note: NO LineHorizontal here, ensuring NO top border above customer name on the first page!
                    });

                    page.Content().Column(col =>
                    {
                        if (activeTab != "air" && seaJobs.Count > 0)
                        {
                            RenderSeaPdfSection(col, seaJobs);
                        }

                        if (activeTab != "sea" && airJobs.Count > 0)
                        {
                            if (activeTab != "air" && seaJobs.Count > 0)
                            {
                                col.Item().PageBreak();
                            }
                            RenderAirPdfSection(col, airJobs);
                        }

                        if (seaJobs.Count == 0 && airJobs.Count == 0)
                        {
                            col.Item().Padding(20).AlignCenter().Text("No job status records found for the selected filter criteria.").FontSize(9);
                        }
                    });

                    //page.Footer().PaddingTop(2).Row(row =>
                    //{
                    //    // Smaller font size for Printed and Page X of Y per user request
                    //    row.RelativeItem().AlignLeft().Text($"Printed: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                    //    row.RelativeItem().AlignRight().Text(txt =>
                    //    {
                    //        txt.Span("Page ").FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                    //        txt.CurrentPageNumber().FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                    //        txt.Span(" of ").FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                    //        txt.TotalPages().FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                    //    });
                    //});

                    page.Footer().PaddingTop(2).Row(row =>
                    {
                        // Smaller font size for Printed, System Name and Page X of Y
                        row.RelativeItem().AlignLeft().Text($"Printed: {DateTime.Now:dd/MM/yyyy hh:mm tt}")
                            .FontSize(5.5f)
                            .FontColor(Colors.Grey.Darken1);

                        row.RelativeItem().AlignCenter().Text("GCTL Infosys – C&F System")
                            .FontSize(5.5f)
                            .FontColor(Colors.Grey.Darken1);

                        row.RelativeItem().AlignRight().Text(txt =>
                        {
                            txt.Span("Page ").FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                            txt.CurrentPageNumber().FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                            txt.Span(" of ").FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                            txt.TotalPages().FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf();

            return pdfBytes;
        }

        private static void RenderSeaPdfSection(ColumnDescriptor col, List<SeaJobReportItemDTO> seaJobs)
        {
            var customerGroups = seaJobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            for (int groupIdx = 0; groupIdx < customerGroups.Count; groupIdx++)
            {
                var group = customerGroups[groupIdx];

                if (groupIdx > 0)
                {
                    col.Item().PageBreak();
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1.4f); // Sl
                        cols.RelativeColumn(3.2f); // Job No
                        cols.RelativeColumn(2.6f); // Job Date
                        cols.RelativeColumn(2.8f); // Mode
                        cols.RelativeColumn(2.8f); // Port
                        cols.RelativeColumn(2.6f); // Doc Rec
                        cols.RelativeColumn(2.8f); // Inv No
                        cols.RelativeColumn(2.8f); // Inv Val
                        cols.RelativeColumn(3.2f); // Cont/Pkgs
                        cols.RelativeColumn(2.8f); // Shipper
                        cols.RelativeColumn(2.6f); // BL No
                        cols.RelativeColumn(2.4f); // ETD
                        cols.RelativeColumn(2.6f); // Final IGM
                        cols.RelativeColumn(2.4f); // Ship Arrived
                        cols.RelativeColumn(2.6f); // Unstaff
                        cols.RelativeColumn(2.6f); // Bank Sign
                        cols.RelativeColumn(2.4f); // BE No
                        cols.RelativeColumn(2.4f); // BE Date
                        cols.RelativeColumn(2.6f); // Est Deliv
                        cols.RelativeColumn(2.6f); // Act Deliv
                        cols.RelativeColumn(2.4f); // Status
                        cols.RelativeColumn(2.6f); // Remarks
                        cols.RelativeColumn(1.8f); // Term
                        cols.RelativeColumn(2.8f); // Vessel
                        cols.RelativeColumn(2.8f); // Cont No
                        cols.RelativeColumn(3.0f); // Forwarder
                    });

                    table.Header(h =>
                    {
                        // Customer Header inside table.Header: repeats automatically on every new page where this customer's rows appear!
                        h.Cell().ColumnSpan(26).PaddingTop(2).PaddingBottom(2).Row(r =>
                        {
                            r.RelativeItem().AlignLeft().Text($"CUSTOMER: {group.Key.ToUpper()} ({group.Count()} Jobs)").FontSize(7.5f).Bold().FontColor(Colors.Black);
                            r.RelativeItem().AlignRight().Text("Sea Freight").FontSize(7f).Bold().FontColor(Colors.Black);
                        });

                        void Head(string text)
                        {
                            h.Cell().Element(c => c
                                .Border(0.4f)
                                .BorderColor(Colors.Grey.Medium)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1.5f)
                                .AlignMiddle()
                                .AlignCenter() // All headers centered horizontally & vertically
                                .Text(text).Bold().FontSize(4.6f).WrapAnywhere());
                        }

                        Head("SL");
                        Head("Job No.");
                        Head("Job Date");
                        Head("Mode");
                        Head("Port");
                        Head("Doc Rec.");
                        Head("Inv No");
                        Head("Inv Val");
                        Head("Cont/Pkgs");
                        Head("Shipper");
                        Head("B/L No.");
                        Head("ETD");
                        Head("Final IGM");
                        Head("Ship Arr");
                        Head("Unstaff");
                        Head("Bank Sign");
                        Head("B/E No.");
                        Head("B/E Date");
                        Head("Est Del.");
                        Head("Act Del.");
                        Head("Status");
                        Head("Remarks");
                        Head("Term");
                        Head("Vessel/R-No");
                        Head("Cont No.");
                        Head("Forwarder");
                    });

                    int sl = 1;
                    foreach (var item in group)
                    {
                        void Cell(string val, bool center = true, bool right = false)
                        {
                            table.Cell().Element(c => c
                                .Border(0.3f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1)
                                .AlignMiddle()
                                .Element(e => right ? e.AlignRight() : (center ? e.AlignCenter() : e.AlignLeft()))
                                .Text(val ?? "").FontSize(4.4f).WrapAnywhere());
                        }

                        Cell(sl++.ToString(), center: true);
                        Cell(item.JobNo, center: true);
                        Cell(item.JobDate, center: true);
                        Cell(item.ShipmentMode, center: true);
                        Cell(item.Port, center: true);
                        Cell(item.DocReceivedDate, center: true);
                        Cell(item.InvoiceNo, center: true);
                        Cell(FormatAmount(item.InvoiceValue), right: true);
                        Cell(item.ContainerPkgs, center: true);
                        Cell(item.Shipper, center: true);
                        Cell(item.BLNo, center: true);
                        Cell(item.ETDDate, center: true);
                        Cell(item.FinalIgmRecDt, center: true);
                        Cell(item.ShipArrivedDate, center: true);
                        Cell(item.DischargeDate, center: true);
                        Cell(item.DocsBankSignDate, center: true);
                        Cell(item.BENo, center: true);
                        Cell(item.BEDate, center: true);
                        Cell(item.EstDeliveryDate, center: true);
                        Cell(item.ActualDeliveryDate, center: true);
                        Cell(item.ShipmentStatus, center: true);
                        Cell(item.Remarks, center: true);
                        Cell(item.Term, center: true);
                        Cell(item.VesselName, center: true);
                        Cell(item.ContainerNo, center: true);
                        Cell(item.FreightForwarder, center: true);
                    }
                });
            }
        }

        private static void RenderAirPdfSection(ColumnDescriptor col, List<AirJobReportItemDTO> airJobs)
        {
            var customerGroups = airJobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            for (int groupIdx = 0; groupIdx < customerGroups.Count; groupIdx++)
            {
                var group = customerGroups[groupIdx];

                if (groupIdx > 0)
                {
                    col.Item().PageBreak();
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1.4f); // SL
                        cols.RelativeColumn(3.4f); // Job No
                        cols.RelativeColumn(2.6f); // Job Date
                        cols.RelativeColumn(2.8f); // Mode
                        cols.RelativeColumn(2.8f); // Port
                        cols.RelativeColumn(2.6f); // Doc Rec
                        cols.RelativeColumn(2.8f); // Inv No
                        cols.RelativeColumn(2.8f); // Inv Val
                        cols.RelativeColumn(3.5f); // Material/Qty
                        cols.RelativeColumn(3.0f); // Express/Country
                        cols.RelativeColumn(2.8f); // LC No
                        cols.RelativeColumn(3.0f); // HAWB No
                        cols.RelativeColumn(3.0f); // MAWB No
                        cols.RelativeColumn(2.6f); // ETD
                        cols.RelativeColumn(2.6f); // Flight Arr
                        cols.RelativeColumn(2.8f); // Notice Rec
                        cols.RelativeColumn(2.6f); // BE No
                        cols.RelativeColumn(2.6f); // BE Date
                        cols.RelativeColumn(2.6f); // Act Del
                        cols.RelativeColumn(2.6f); // Status
                        cols.RelativeColumn(3.0f); // Remarks
                    });

                    table.Header(h =>
                    {
                        // Customer Header inside table.Header: repeats automatically on every new page where this customer's rows appear!
                        h.Cell().ColumnSpan(21).PaddingTop(2).PaddingBottom(2).Row(r =>
                        {
                            r.RelativeItem().AlignLeft().Text($"CUSTOMER: {group.Key.ToUpper()} ({group.Count()} Jobs)").FontSize(7.5f).Bold().FontColor(Colors.Black);
                            r.RelativeItem().AlignRight().Text("Air Freight").FontSize(7f).Bold().FontColor(Colors.Black);
                        });

                        void Head(string text)
                        {
                            h.Cell().Element(c => c
                                .Border(0.4f)
                                .BorderColor(Colors.Grey.Medium)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1.5f)
                                .AlignMiddle()
                                .AlignCenter() // All headers centered horizontally & vertically
                                .Text(text).Bold().FontSize(4.8f).WrapAnywhere());
                        }

                        Head("SL");
                        Head("Job No.");
                        Head("Job Date");
                        Head("Mode");
                        Head("Port");
                        Head("Doc Rec.");
                        Head("Inv No.");
                        Head("Inv Val");
                        Head("Material & Qty");
                        Head("Express/Country");
                        Head("L/C No.");
                        Head("HAWB No.");
                        Head("MAWB No.");
                        Head("ETD");
                        Head("Flight Arr.");
                        Head("Arrival Notice");
                        Head("B/E No.");
                        Head("B/E Date");
                        Head("Act Del.");
                        Head("Status");
                        Head("Remarks");
                    });

                    int sl = 1;
                    foreach (var item in group)
                    {
                        void Cell(string val, bool center = true, bool right = false)
                        {
                            table.Cell().Element(c => c
                                .Border(0.3f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1)
                                .AlignMiddle()
                                .Element(e => right ? e.AlignRight() : (center ? e.AlignCenter() : e.AlignLeft()))
                                .Text(val ?? "").FontSize(4.6f).WrapAnywhere());
                        }

                        Cell(sl++.ToString(), center: true);
                        Cell(item.JobNo, center: true);
                        Cell(item.JobDate, center: true);
                        Cell(item.ShipmentMode, center: true);
                        Cell(item.Port, center: true);
                        Cell(item.DocReceivedDate, center: true);
                        Cell(item.InvoiceNo, center: true);
                        Cell(FormatAmount(item.InvoiceValue), right: true);
                        Cell(item.MaterialNamePkgsQty, center: true);
                        Cell(item.ExpressNameCountry, center: true);
                        Cell(item.LCNo, center: true);
                        Cell(item.HawbNo, center: true);
                        Cell(item.MawbNo, center: true);
                        Cell(item.ETDDate, center: true);
                        Cell(item.FlightActualDeliveryDate, center: true);
                        Cell(item.ArrivalNoticeRcvdExpressDate, center: true);
                        Cell(item.BENo, center: true);
                        Cell(item.BEDate, center: true);
                        Cell(item.ActualDeliveryDate, center: true);
                        Cell(item.ShipmentStatus, center: true);
                        Cell(item.Remarks, center: true);
                    }
                });
            }
        }
        #endregion

        #region Helper Date Parsers & Formatters
        private async Task<string> GetCustomerNameAsync(string? customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return "";
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT TOP 1 CustomerName FROM Sales_Customer WHERE CustomerID = @CustomerId", conn);
                cmd.Parameters.Add(new SqlParameter("@CustomerId", customerId.Trim()));
                var res = await cmd.ExecuteScalarAsync();
                return res?.ToString()?.Trim() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private async Task<Dictionary<string, string>> GetCustomerNamesDictAsync()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT CustomerID, CustomerName FROM Sales_Customer", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string id = reader["CustomerID"]?.ToString()?.Trim() ?? "";
                    string name = reader["CustomerName"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(id) && !dict.ContainsKey(id))
                    {
                        dict[id] = name;
                    }
                }
            }
            catch { }
            return dict;
        }

        private static string FormatAmount(object? val)
        {
            if (val == null) return "";
            string str = val.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(str)) return "";
            if (decimal.TryParse(str.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            {
                return dec.ToString("#,##0.00", CultureInfo.InvariantCulture);
            }
            return str;
        }
        private static string FormatDt(object? val, string fmt = "dd/MM/yyyy hh:mm tt")
        {
            if (val == null || val == DBNull.Value) return "";
            if (val is DateTime dt)
            {
                if (dt == DateTime.MinValue || dt.Year <= 1900) return "";
                return dt.ToString(fmt);
            }
            if (DateTime.TryParse(val.ToString(), out var parsed))
            {
                if (parsed == DateTime.MinValue || parsed.Year <= 1900) return "";
                return parsed.ToString(fmt);
            }
            return val.ToString()?.Trim() ?? "";
        }

        private static string FormatDateOnly(object? val)
        {
            return FormatDt(val, "dd/MM/yyyy");
        }

        private static int GetColOrdinal(SqlDataReader reader, string colName)
        {
            try { return reader.GetOrdinal(colName); }
            catch { return -1; }
        }

        private static string ReadCol(SqlDataReader reader, int ord)
        {
            if (ord < 0 || reader.IsDBNull(ord)) return "";
            return reader.GetValue(ord).ToString()?.Trim() ?? "";
        }

        private static (string dateUse, object fromDate, object toDate) ParseDates(string? fromStr, string? toStr)
        {
            // Default range for DateUse = 'N' avoids SQL Server NULL comparison plan regression
            DateTime minDate = new DateTime(1900, 1, 1);
            DateTime maxDate = new DateTime(2099, 12, 31);

            if (string.IsNullOrWhiteSpace(fromStr) || string.IsNullOrWhiteSpace(toStr))
            {
                return ("N", minDate, maxDate);
            }

            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy" };
            if (DateTime.TryParseExact(fromStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fDate) &&
                DateTime.TryParseExact(toStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tDate))
            {
                return ("Y", fDate.Date, tDate.Date.AddDays(1).AddTicks(-1));
            }

            if (DateTime.TryParse(fromStr.Trim(), out var parsedFrom) && DateTime.TryParse(toStr.Trim(), out var parsedTo))
            {
                return ("Y", parsedFrom.Date, parsedTo.Date.AddDays(1).AddTicks(-1));
            }

            return ("N", minDate, maxDate);
        }
        #endregion
    }
}
