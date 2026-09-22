using GCTL.Core.Data;
using GCTL.Core.ViewModels.DocumentationReport;
using GCTL.Data.Models;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.DocumentationReport;
using GCTL.Service.PortType;
using GCTL.Service.ShipmentMode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class JobStatusReportController : BaseController
    {
        private readonly string _connectionString;
        private readonly IDocumentationReportService _reportService;
        private readonly IShipmentMode _shipmentMode;
        private readonly IPortTypeService _portTypeService;
        private readonly ISalesCustomerService _customerService;

        public JobStatusReportController(
            IConfiguration configuration,
            IDocumentationReportService reportService,
            IShipmentMode shipmentMode,
            IPortTypeService portTypeService,
            ISalesCustomerService customerService) : base()
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
            _reportService = reportService;
            _shipmentMode = shipmentMode;
            _portTypeService = portTypeService;
            _customerService = customerService;
        }

        #region Index View
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await PopulateDropdowns();
            return View();
        }

        private async Task PopulateDropdowns()
        {
            try
            {
                var rawShipmentModes = await _shipmentMode.GetAllExpenseTypesAsync();
                var shipmentModes = rawShipmentModes?
                    .Where(x => !string.IsNullOrWhiteSpace(x.ExpenseTypeID) && !string.IsNullOrWhiteSpace(x.ExpenseType))
                    .ToList();
                ViewBag.ShipmentModes = new SelectList(shipmentModes ?? new(), "ExpenseTypeID", "ExpenseType");

                var rawPortTypes = await _portTypeService.SelectionPortTypeAsync();
                var portTypes = rawPortTypes?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Name))
                    .ToList();
                ViewBag.PortTypes = new SelectList(portTypes ?? Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                var customers = await _customerService.SelectionCustomerAsync();
                ViewBag.Customers = new SelectList(customers ?? Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                ViewBag.JobStatuses = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Job Status --" },
                    new SelectListItem { Value = "Pending", Text = "Pending" },
                    new SelectListItem { Value = "In Transit", Text = "In Transit" },
                    new SelectListItem { Value = "Delivered", Text = "Delivered" }
                }, "Value", "Text");

                ViewBag.ExportFormats = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "PDF", Text = "PDF Format" },
                    new SelectListItem { Value = "Excel", Text = "Excel Format" }
                }, "Value", "Text");
            }
            catch
            {
                ViewBag.ShipmentModes = new SelectList(new List<string>());
                ViewBag.PortTypes = new SelectList(new List<string>());
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.JobStatuses = new SelectList(new List<string>());
                ViewBag.ExportFormats = new SelectList(new List<string>());
            }
        }
        #endregion

        #region AJAX Data Endpoint (Sea & Air DataTables)
        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DocumentationReportFilterVM filter)
        {
            try
            {
                filter ??= new DocumentationReportFilterVM();

                // 1. Fetch Summary Counts
                var (summaryTotal, summaryPending, summaryTransit, summaryDelivered) = await FetchSummaryAsync(filter.CustomerId);

                // 2. Fetch Sea Freight Jobs
                var seaJobs = await FetchSeaJobsAsync(filter);

                // 3. Fetch Air Freight Jobs
                var airJobs = await FetchAirJobsAsync(filter);

                // If specific filters (dates, shipment mode, port) narrowed down the counts, compute real totals from datasets
                int calculatedTotal = seaJobs.Count + airJobs.Count;
                int calculatedDelivered = CountByStatus(seaJobs, "delivered") + CountByStatus(airJobs, "delivered");
                int calculatedTransit = CountByStatus(seaJobs, "transit") + CountByStatus(airJobs, "transit");
                int calculatedPending = CountByStatus(seaJobs, "pending") + CountByStatus(airJobs, "pending");

                bool hasSpecificFilter = !string.IsNullOrWhiteSpace(filter.FromDate) || 
                                         !string.IsNullOrWhiteSpace(filter.ShipmentMode) || 
                                         !string.IsNullOrWhiteSpace(filter.PortType) || 
                                         !string.IsNullOrWhiteSpace(filter.JobStatus);

                int totalCount = hasSpecificFilter ? calculatedTotal : (summaryTotal > 0 ? summaryTotal : calculatedTotal);
                int pendingCount = hasSpecificFilter ? calculatedPending : (summaryPending > 0 ? summaryPending : calculatedPending);
                int transitCount = hasSpecificFilter ? calculatedTransit : (summaryTransit > 0 ? summaryTransit : calculatedTransit);
                int deliveredCount = hasSpecificFilter ? calculatedDelivered : (summaryDelivered > 0 ? summaryDelivered : calculatedDelivered);

                var unifiedList = new List<object>();
                unifiedList.AddRange(seaJobs);
                unifiedList.AddRange(airJobs);

                // Fallback to _reportService if both returned 0 records and no customer filter
                if (unifiedList.Count == 0 && string.IsNullOrWhiteSpace(filter.CustomerId))
                {
                    try
                    {
                        var rawData = await _reportService.GetDocumentationReportDataAsync(filter);
                        if (rawData != null && rawData.Count > 0)
                        {
                            int fbSl = 1;
                            foreach (var item in rawData)
                            {
                                string stCls = "pending";
                                string st = (item.ShipmentStatus ?? "").Trim();
                                if (st.Contains("Deliver", StringComparison.OrdinalIgnoreCase) || st.Contains("Complete", StringComparison.OrdinalIgnoreCase))
                                    stCls = "delivered";
                                else if (st.Contains("Transit", StringComparison.OrdinalIgnoreCase) || st.Contains("Arrived", StringComparison.OrdinalIgnoreCase) || st.Contains("Port", StringComparison.OrdinalIgnoreCase))
                                    stCls = "transit";

                                unifiedList.Add(new
                                {
                                    sl = fbSl++,
                                    jobNo = item.JobNo ?? "",
                                    serviceType = !string.IsNullOrWhiteSpace(item.ExpenseType) ? item.ExpenseType : "General",
                                    portType = !string.IsNullOrWhiteSpace(item.PortName) ? item.PortName : (!string.IsNullOrWhiteSpace(item.LoadingPort) ? item.LoadingPort : "-"),
                                    dateReceived = !string.IsNullOrWhiteSpace(item.DocReceivedDate) ? item.DocReceivedDate : (!string.IsNullOrWhiteSpace(item.JobDate) ? item.JobDate : "-"),
                                    vesselName = !string.IsNullOrWhiteSpace(item.VesselRotNo) ? item.VesselRotNo : (!string.IsNullOrWhiteSpace(item.ForwarderName) ? item.ForwarderName : "-"),
                                    weight = !string.IsNullOrWhiteSpace(item.Weight) ? item.Weight : "-",
                                    container = !string.IsNullOrWhiteSpace(item.ContainerPackages) ? item.ContainerPackages : (!string.IsNullOrWhiteSpace(item.ContainerNo) ? item.ContainerNo : "-"),
                                    status = !string.IsNullOrEmpty(st) ? st : (stCls == "delivered" ? "Delivered" : (stCls == "transit" ? "In Transit" : "Pending")),
                                    statusClass = stCls
                                });
                            }
                            if (!hasSpecificFilter)
                            {
                                totalCount = unifiedList.Count;
                                pendingCount = unifiedList.Count(x => ((dynamic)x).statusClass == "pending");
                                transitCount = unifiedList.Count(x => ((dynamic)x).statusClass == "transit");
                                deliveredCount = unifiedList.Count(x => ((dynamic)x).statusClass == "delivered");
                            }
                        }
                    }
                    catch { }
                }

                return Json(new
                {
                    success = true,
                    total = totalCount,
                    pending = pendingCount,
                    transit = transitCount,
                    delivered = deliveredCount,
                    data = unifiedList,
                    seaData = seaJobs,
                    airData = airJobs
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    total = 0,
                    pending = 0,
                    transit = 0,
                    delivered = 0,
                    data = new List<object>(),
                    seaData = new List<object>(),
                    airData = new List<object>()
                });
            }
        }

        private static int CountByStatus(List<object> list, string targetStatus)
        {
            int c = 0;
            foreach (var item in list)
            {
                var prop = item.GetType().GetProperty("statusClass");
                if (prop?.GetValue(item)?.ToString()?.Equals(targetStatus, StringComparison.OrdinalIgnoreCase) == true)
                {
                    c++;
                }
            }
            return c;
        }

        private async Task<(int total, int pending, int transit, int delivered)> FetchSummaryAsync(string? customerId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("proc_CustomerJobSummery", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(customerId) ? "" : customerId.Trim()));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int.TryParse(reader["TotalJob"]?.ToString(), out int total);
                    int.TryParse(reader["PendingJob"]?.ToString(), out int pending);
                    int.TryParse(reader["TransitJob"]?.ToString(), out int transit);
                    int.TryParse(reader["DeliveredJob"]?.ToString(), out int delivered);
                    return (total, pending, transit, delivered);
                }
            }
            catch { }

            return (0, 0, 0, 0);
        }

        private async Task<List<object>> FetchSeaJobsAsync(DocumentationReportFilterVM filter)
        {
            var list = new List<object>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);
            var status = (filter.JobStatus ?? "").Trim().ToLower();

            var procedures = new List<string>();
            if (status == "pending")
            {
                procedures.Add("Proc_CustomerJobStatusPendingReportForSea");
            }
            else if (status == "in transit" || status == "transit")
            {
                procedures.Add("Proc_CustomerJobStatusInTransitSea");
            }
            else if (status == "delivered")
            {
                procedures.Add("Proc_CustomerJobStatusDeliveredSea");
            }
            else
            {
                procedures.Add("Proc_CustomerJobStatusPendingReportForSea");
                procedures.Add("Proc_CustomerJobStatusInTransitSea");
                procedures.Add("Proc_CustomerJobStatusDeliveredSea");
            }

            int sl = 1;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var proc in procedures)
            {
                try
                {
                    using var cmd = new SqlCommand(proc, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
                    cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate));
                    cmd.Parameters.Add(new SqlParameter("@ToDate", toDate));
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
                    cmd.Parameters.Add(new SqlParameter("@PortTypeId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
                    cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", string.IsNullOrWhiteSpace(filter.ShipmentMode) ? "" : filter.ShipmentMode.Trim()));

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string GetStr(string col)
                        {
                            try
                            {
                                int idx = reader.GetOrdinal(col);
                                return reader.IsDBNull(idx) ? "" : reader.GetValue(idx).ToString()?.Trim() ?? "";
                            }
                            catch { return ""; }
                        }

                        string rawStatus = GetStr("ShipmentStatus");
                        string statusCls = "pending";
                        if (rawStatus.Contains("Deliver", StringComparison.OrdinalIgnoreCase) || proc.Contains("Delivered", StringComparison.OrdinalIgnoreCase))
                        {
                            statusCls = "delivered";
                            rawStatus = "Delivered";
                        }
                        else if (rawStatus.Contains("Transit", StringComparison.OrdinalIgnoreCase) || proc.Contains("InTransit", StringComparison.OrdinalIgnoreCase))
                        {
                            statusCls = "transit";
                            rawStatus = "In Transit";
                        }
                        else
                        {
                            statusCls = "pending";
                            rawStatus = "Pending";
                        }

                        string expType = !string.IsNullOrEmpty(GetStr("ExpenseType")) ? GetStr("ExpenseType") : "Sea Freight";
                        string pName = GetStr("PortName");
                        string dRec = !string.IsNullOrEmpty(GetStr("DocReceivedDate")) ? GetStr("DocReceivedDate") : GetStr("JobDate");
                        string vName = GetStr("VesselName");
                        string wt = !string.IsNullOrEmpty(GetStr("InvoiceValue")) ? GetStr("InvoiceValue") : "-";
                        string cont = !string.IsNullOrEmpty(GetStr("ContainerNo")) ? GetStr("ContainerNo") : GetStr("ContainerPkgs");

                        list.Add(new
                        {
                            sl = sl++,
                            jobNo = GetStr("JobNo"),
                            jobDate = GetStr("JobDate"),
                            expenseType = expType,
                            serviceType = expType,
                            portName = pName,
                            portType = pName,
                            docReceivedDate = dRec,
                            dateReceived = dRec,
                            invoiceNo = GetStr("InvoiceNo"),
                            invoiceValue = GetStr("InvoiceValue"),
                            containerPkgs = GetStr("ContainerPkgs"),
                            billNo = GetStr("BillNo"),
                            vesselName = vName,
                            containerNo = GetStr("ContainerNo"),
                            weight = wt,
                            container = cont,
                            etdDate = GetStr("EtdDate"),
                            shipArrivedDate = GetStr("ShipArrivedDate"),
                            dischargeDate = GetStr("DischargeDate"),
                            actualDeliveryDate = GetStr("ActualDeliveryDate"),
                            shipmentStatus = rawStatus,
                            status = rawStatus,
                            statusClass = statusCls
                        });
                    }
                }
                catch { }
            }

            return list;
        }

        private async Task<List<object>> FetchAirJobsAsync(DocumentationReportFilterVM filter)
        {
            var list = new List<object>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);
            var status = (filter.JobStatus ?? "").Trim().ToLower();

            var procedures = new List<string>();
            if (status == "pending")
            {
                procedures.Add("Proc_CustomerJobStatusPendingExceptAll");
            }
            else if (status == "in transit" || status == "transit")
            {
                procedures.Add("Proc_CustomerJobStatusInTransitExceptAll");
            }
            else if (status == "delivered")
            {
                procedures.Add("Proc_CustomerJobStatusDeliveryExceptAll");
            }
            else
            {
                procedures.Add("Proc_CustomerJobStatusPendingExceptAll");
                procedures.Add("Proc_CustomerJobStatusInTransitExceptAll");
                procedures.Add("Proc_CustomerJobStatusDeliveryExceptAll");
            }

            int sl = 1;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var proc in procedures)
            {
                try
                {
                    using var cmd = new SqlCommand(proc, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
                    cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate));
                    cmd.Parameters.Add(new SqlParameter("@ToDate", toDate));
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
                    cmd.Parameters.Add(new SqlParameter("@PortTypeId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
                    cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", string.IsNullOrWhiteSpace(filter.ShipmentMode) ? "" : filter.ShipmentMode.Trim()));

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string GetStr(string col)
                        {
                            try
                            {
                                int idx = reader.GetOrdinal(col);
                                return reader.IsDBNull(idx) ? "" : reader.GetValue(idx).ToString()?.Trim() ?? "";
                            }
                            catch { return ""; }
                        }

                        string rawStatus = GetStr("ShipmentStatus");
                        string statusCls = "pending";
                        if (rawStatus.Contains("Deliver", StringComparison.OrdinalIgnoreCase) || proc.Contains("Delivery", StringComparison.OrdinalIgnoreCase))
                        {
                            statusCls = "delivered";
                            rawStatus = "Delivered";
                        }
                        else if (rawStatus.Contains("Transit", StringComparison.OrdinalIgnoreCase) || proc.Contains("InTransit", StringComparison.OrdinalIgnoreCase))
                        {
                            statusCls = "transit";
                            rawStatus = "In Transit";
                        }
                        else
                        {
                            statusCls = "pending";
                            rawStatus = "Pending";
                        }

                        string expType = !string.IsNullOrEmpty(GetStr("ExpenseType")) ? GetStr("ExpenseType") : "Air Freight";
                        string pName = GetStr("PortName");
                        string dRec = !string.IsNullOrEmpty(GetStr("DocReceivedDate")) ? GetStr("DocReceivedDate") : GetStr("JobDate");
                        string vName = !string.IsNullOrEmpty(GetStr("HawbNo")) ? "HAWB: " + GetStr("HawbNo") : (!string.IsNullOrEmpty(GetStr("MawbNo")) ? "MAWB: " + GetStr("MawbNo") : "-");
                        string wt = !string.IsNullOrEmpty(GetStr("InvoiceValue")) ? GetStr("InvoiceValue") : "-";
                        string cont = GetStr("MaterialNamePkgsQty");

                        list.Add(new
                        {
                            sl = sl++,
                            jobNo = GetStr("JobNo"),
                            jobDate = GetStr("JobDate"),
                            expenseType = expType,
                            serviceType = expType,
                            portName = pName,
                            portType = pName,
                            docReceivedDate = dRec,
                            dateReceived = dRec,
                            invoiceNo = GetStr("InvoiceNo"),
                            invoiceValue = GetStr("InvoiceValue"),
                            materialNamePkgsQty = GetStr("MaterialNamePkgsQty"),
                            lcNo = GetStr("LcNo"),
                            hawbNo = GetStr("HawbNo"),
                            mawbNo = GetStr("MawbNo"),
                            vesselName = vName,
                            weight = wt,
                            container = cont,
                            etdDate = GetStr("EtdDate"),
                            flightActualDeliveryDate = GetStr("FlightActualDeliveryDate"),
                            actualDeliveryDate = GetStr("ActualDeliveryDate"),
                            shipmentStatus = rawStatus,
                            status = rawStatus,
                            statusClass = statusCls
                        });
                    }
                }
                catch { }
            }

            return list;
        }

        private static (string dateUse, object fromDate, object toDate) ParseDates(string? fromStr, string? toStr)
        {
            if (string.IsNullOrWhiteSpace(fromStr) || string.IsNullOrWhiteSpace(toStr))
            {
                return ("N", DBNull.Value, DBNull.Value);
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

            return ("N", DBNull.Value, DBNull.Value);
        }
        #endregion

        #region Export Action
        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] DocumentationReportFilterVM filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                filter ??= new DocumentationReportFilterVM();

                if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await _reportService.GenerateExcelReportAsync(filter);
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"JobStatusReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                else
                {
                    var bytes = await _reportService.GeneratePdfReportAsync(filter);
                    return File(bytes, "application/pdf", $"JobStatusReport_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Report export failed: {ex.Message}");
            }
        }
        #endregion
    }
}
