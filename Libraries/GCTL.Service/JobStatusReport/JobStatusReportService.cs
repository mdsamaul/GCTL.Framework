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

        #region 2. Sea Freight Jobs (Ultra-Fast Single-Pass CTE with Fallback)
        public async Task<List<SeaJobReportItemDTO>> GetSeaJobsAsync(JobStatusReportFilterVM filter)
        {
            filter ??= new JobStatusReportFilterVM();
            if (string.IsNullOrWhiteSpace(_connectionString)) return new List<SeaJobReportItemDTO>();

            try
            {
                return await FetchSeaJobsDirectAsync(filter);
            }
            catch
            {
                return await ExecuteSeaProcsLegacyAsync(filter);
            }
        }

        private async Task<List<SeaJobReportItemDTO>> FetchSeaJobsDirectAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<SeaJobReportItemDTO>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);

            string customerId = (filter.CustomerId ?? "").Trim();
            string portTypeId = (filter.PortType ?? "").Trim();
            string shipmentTypeId = (filter.ShipmentMode ?? "").Trim();
            if (string.Equals(shipmentTypeId, "Air", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shipmentTypeId, "Sea", StringComparison.OrdinalIgnoreCase))
            {
                shipmentTypeId = "";
            }

            const string sql = @"
;WITH FilteredJobs AS (
    SELECT 
        d.JobNo,
        d.Date,
        d.DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        d.CurrencyId,
        d.Quntity1,
        d.MaterialDescription,
        d.BillNo,
        d.ShipmentStatus,
        d.BENo,
        d.BEDate,
        d.Remarks,
        d.Term,
        d.VesselName,
        d.Vessel_RottNo,
        d.ContainerNo,
        d.NameOfFreightForwarder,
        d.FreightForwarderPhoneNo,
        d.CustomerID,
        d.ExpenseTypeID,
        d.PortNameId,
        ISNULL(c.CustomerName, 'General Customer') AS CustomerName,
        ISNULL(c.CustomerAddress, '') AS CustomerAddress,
        ISNULL(curr.Symbol, ISNULL(curr.ShortName, '')) AS CurrencySymbol,
        ISNULL(curr.ShortName, '') AS CurrencyName,
        ISNULL(e.ExpenseType, 'Sea Freight') AS ExpenseType,
        ISNULL(p.PortName, '') AS PortName
    FROM Sep_Documentation d WITH (NOLOCK)
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
    LEFT JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
    LEFT JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (@CustomerId = '' OR d.CustomerID = @CustomerId)
      AND (@PortTypeId = '' OR p.PortId = @PortTypeId OR (@PortTypeId LIKE '%,%' AND p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))))
      AND (@ShipmentTypeId = '' OR d.ExpenseTypeID = @ShipmentTypeId OR (@ShipmentTypeId LIKE '%,%' AND d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ','))))
      AND (e.ExpenseType IS NULL OR (e.ExpenseType NOT LIKE '%Air%' AND ISNULL(d.HAWB, '') = '' AND ISNULL(d.MAWB, '') = ''))
),
Updates AS (
    SELECT 
        du.JobNo,
        MAX(CASE WHEN du.UpdateStatus = '07' THEN du.UpdateDateTime END) AS EtdDate,
        MAX(CASE WHEN du.UpdateStatus = '08' THEN du.UpdateDateTime END) AS FinalIgmRecDt,
        MAX(CASE WHEN du.UpdateStatus = '09' THEN du.UpdateDateTime END) AS ShipArrivedDate,
        MAX(CASE WHEN du.UpdateStatus = '10' THEN du.UpdateDateTime END) AS DischargeDate,
        MAX(CASE WHEN du.UpdateStatus = '11' THEN du.UpdateDateTime END) AS DocsBankSignDate,
        MAX(CASE WHEN du.UpdateStatus = '12' THEN du.UpdateDateTime END) AS BeDateFromUpdate,
        MAX(CASE WHEN du.UpdateStatus = '13' THEN du.UpdateDateTime END) AS EstimatedDeliveryDate,
        MAX(CASE WHEN du.UpdateStatus = '14' THEN du.UpdateDateTime END) AS ActualDeliveryDate
    FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
    INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
    GROUP BY du.JobNo
)
SELECT 
    fj.JobNo,
    fj.Date AS RawJobDate,
    fj.ExpenseType,
    fj.PortName,
    fj.DocReceivedDate AS RawDocReceivedDate,
    fj.InvoiceNo,
    fj.InvoiceValue,
    fj.CurrencySymbol,
    fj.CurrencyName,
    fj.CurrencyId,
    fj.Quntity1,
    fj.MaterialDescription,
    fj.BillNo,
    fj.ShipmentStatus AS RawShipmentStatus,
    u.EtdDate,
    u.FinalIgmRecDt,
    u.ShipArrivedDate,
    u.DischargeDate,
    u.DocsBankSignDate,
    fj.BENo,
    u.BeDateFromUpdate,
    fj.BEDate AS RawBeDate,
    u.EstimatedDeliveryDate,
    u.ActualDeliveryDate,
    fj.Remarks,
    fj.Term,
    fj.VesselName,
    fj.Vessel_RottNo,
    fj.ContainerNo,
    fj.NameOfFreightForwarder,
    fj.FreightForwarderPhoneNo,
    fj.CustomerID,
    fj.CustomerName,
    fj.CustomerAddress
FROM FilteredJobs fj
LEFT JOIN Updates u ON fj.JobNo = u.JobNo
ORDER BY fj.JobNo ASC;
";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
            cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ToDate", toDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CustomerId", customerId));
            cmd.Parameters.Add(new SqlParameter("@PortTypeId", portTypeId));
            cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", shipmentTypeId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.FieldCount == 0 && await reader.NextResultAsync()) { }

            int ordJobNo = GetColOrdinal(reader, "JobNo");
            int ordJobDate = GetColOrdinal(reader, "RawJobDate");
            int ordExpenseType = GetColOrdinal(reader, "ExpenseType");
            int ordPortName = GetColOrdinal(reader, "PortName");
            int ordDocReceivedDate = GetColOrdinal(reader, "RawDocReceivedDate");
            int ordInvoiceNo = GetColOrdinal(reader, "InvoiceNo");
            int ordInvoiceValue = GetColOrdinal(reader, "InvoiceValue");
            int ordCurrencySymbol = GetColOrdinal(reader, "CurrencySymbol");
            int ordCurrencyName = GetColOrdinal(reader, "CurrencyName");
            int ordCurrencyId = GetColOrdinal(reader, "CurrencyId");
            int ordQuntity1 = GetColOrdinal(reader, "Quntity1");
            int ordMaterialDescription = GetColOrdinal(reader, "MaterialDescription");
            int ordBillNo = GetColOrdinal(reader, "BillNo");
            int ordRawShipmentStatus = GetColOrdinal(reader, "RawShipmentStatus");
            int ordEtdDate = GetColOrdinal(reader, "EtdDate");
            int ordFinalIgmRecDt = GetColOrdinal(reader, "FinalIgmRecDt");
            int ordShipArrivedDate = GetColOrdinal(reader, "ShipArrivedDate");
            int ordDischargeDate = GetColOrdinal(reader, "DischargeDate");
            int ordDocsBankSignDate = GetColOrdinal(reader, "DocsBankSignDate");
            int ordBENo = GetColOrdinal(reader, "BENo");
            int ordBeDateFromUpdate = GetColOrdinal(reader, "BeDateFromUpdate");
            int ordRawBeDate = GetColOrdinal(reader, "RawBeDate");
            int ordEstDeliveryDate = GetColOrdinal(reader, "EstimatedDeliveryDate");
            int ordActualDeliveryDate = GetColOrdinal(reader, "ActualDeliveryDate");
            int ordRemarks = GetColOrdinal(reader, "Remarks");
            int ordTerm = GetColOrdinal(reader, "Term");
            int ordVesselName = GetColOrdinal(reader, "VesselName");
            int ordVesselRottNo = GetColOrdinal(reader, "Vessel_RottNo");
            int ordContainerNo = GetColOrdinal(reader, "ContainerNo");
            int ordNameOfFreightForwarder = GetColOrdinal(reader, "NameOfFreightForwarder");
            int ordFreightForwarderPhoneNo = GetColOrdinal(reader, "FreightForwarderPhoneNo");
            int ordCustomerID = GetColOrdinal(reader, "CustomerID");
            int ordCustomerName = GetColOrdinal(reader, "CustomerName");
            int ordCustomerAddress = GetColOrdinal(reader, "CustomerAddress");

            int sl = 1;
            while (await reader.ReadAsync())
            {
                string rawStatus = ReadCol(reader, ordRawShipmentStatus);
                var (statusText, statusClass) = ResolveSeaStatus(rawStatus);

                string rawSym = ReadCol(reader, ordCurrencySymbol);
                string rawName = ReadCol(reader, ordCurrencyName);
                string rawCurrId = ReadCol(reader, ordCurrencyId);
                var (curSym, curName) = ResolveCurrency(rawSym, rawName, rawCurrId);

                string q1 = ReadCol(reader, ordQuntity1);
                string matDesc = ReadCol(reader, ordMaterialDescription);
                string containerPkgs = string.Join(" ", new[] { q1, matDesc }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                string vName = ReadCol(reader, ordVesselName);
                string vRott = ReadCol(reader, ordVesselRottNo);
                string fullVessel = string.Join(" ", new[] { vName, vRott }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                string ffName = ReadCol(reader, ordNameOfFreightForwarder);
                string ffPhone = ReadCol(reader, ordFreightForwarderPhoneNo);
                string fullFf = string.Join(" ", new[] { ffName, ffPhone }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                string beDate = ReadDt(reader, ordBeDateFromUpdate, "dd/MM/yyyy hh:mm tt");
                if (string.IsNullOrEmpty(beDate))
                {
                    beDate = ReadDateOnly(reader, ordRawBeDate);
                }

                list.Add(new SeaJobReportItemDTO
                {
                    SlNo = sl++,
                    JobNo = ReadCol(reader, ordJobNo),
                    JobDate = ReadDateOnly(reader, ordJobDate),
                    ShipmentMode = ReadCol(reader, ordExpenseType),
                    Port = ReadCol(reader, ordPortName),
                    DocReceivedDate = ReadDateOnly(reader, ordDocReceivedDate),
                    InvoiceNo = ReadCol(reader, ordInvoiceNo),
                    InvoiceValue = ReadCol(reader, ordInvoiceValue),
                    CurrencySymbol = curSym,
                    CurrencyName = curName,
                    ContainerPkgs = containerPkgs,
                    Shipper = "",
                    BLNo = ReadCol(reader, ordBillNo),
                    ETDDate = ReadDt(reader, ordEtdDate, "dd/MM/yyyy hh:mm tt"),
                    FinalIgmRecDt = ReadDt(reader, ordFinalIgmRecDt, "dd/MM/yyyy hh:mm tt"),
                    ShipArrivedDate = ReadDt(reader, ordShipArrivedDate, "dd/MM/yyyy hh:mm tt"),
                    DischargeDate = ReadDt(reader, ordDischargeDate, "dd/MM/yyyy hh:mm tt"),
                    DocsBankSignDate = ReadDt(reader, ordDocsBankSignDate, "dd/MM/yyyy hh:mm tt"),
                    BENo = ReadCol(reader, ordBENo),
                    BEDate = beDate,
                    EstDeliveryDate = ReadDt(reader, ordEstDeliveryDate, "dd/MM/yyyy hh:mm tt"),
                    ActualDeliveryDate = ReadDt(reader, ordActualDeliveryDate, "dd/MM/yyyy hh:mm tt"),
                    ShipmentStatus = statusText,
                    Remarks = ReadCol(reader, ordRemarks),
                    Term = ReadCol(reader, ordTerm),
                    VesselName = fullVessel,
                    ContainerNo = ReadCol(reader, ordContainerNo),
                    FreightForwarder = fullFf,
                    CustomerId = ReadCol(reader, ordCustomerID),
                    CustomerName = ReadCol(reader, ordCustomerName),
                    CustomerAddress = ReadCol(reader, ordCustomerAddress),
                    StatusClass = statusClass
                });
            }

            string filterStatus = (filter.JobStatus ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(filterStatus))
            {
                if (filterStatus.Contains("pend")) list = list.Where(x => x.StatusClass == "pending").ToList();
                else if (filterStatus.Contains("transit")) list = list.Where(x => x.StatusClass == "transit").ToList();
                else if (filterStatus.Contains("deliver")) list = list.Where(x => x.StatusClass == "delivered").ToList();
                for (int i = 0; i < list.Count; i++) list[i].SlNo = i + 1;
            }

            return list;
        }

        private async Task<List<SeaJobReportItemDTO>> ExecuteSeaProcsLegacyAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<SeaJobReportItemDTO>();
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
            catch { }

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
            int ordCurrencySymbol = GetColOrdinal(reader, "CurrencySymbol");
            if (ordCurrencySymbol < 0) ordCurrencySymbol = GetColOrdinal(reader, "Symbol");
            int ordCurrencyName = GetColOrdinal(reader, "CurrencyName");
            if (ordCurrencyName < 0) ordCurrencyName = GetColOrdinal(reader, "ShortName");
            int ordCurrencyId = GetColOrdinal(reader, "CurrencyId");

            while (await reader.ReadAsync())
            {
                string rawStatus = ReadCol(reader, ordShipmentStatus);
                string resolvedStatus = !string.IsNullOrEmpty(rawStatus) ? rawStatus :
                    (statusClass == "delivered" ? "Delivered" : (statusClass == "transit" ? "In Transit" : "Pending"));

                string expType = ReadCol(reader, ordExpenseType);
                string custId = ReadCol(reader, ordCustomerId);
                string rawSym = ReadCol(reader, ordCurrencySymbol);
                string rawName = ReadCol(reader, ordCurrencyName);
                string rawCurrId = ReadCol(reader, ordCurrencyId);
                var (curSym, curName) = ResolveCurrency(rawSym, rawName, rawCurrId);

                list.Add(new SeaJobReportItemDTO
                {
                    JobNo = ReadCol(reader, ordJobNo),
                    JobDate = ReadCol(reader, ordJobDate),
                    ShipmentMode = !string.IsNullOrEmpty(expType) ? expType : "Sea Freight",
                    Port = ReadCol(reader, ordPortName),
                    DocReceivedDate = ReadCol(reader, ordDocReceivedDate),
                    InvoiceNo = ReadCol(reader, ordInvoiceNo),
                    InvoiceValue = ReadCol(reader, ordInvoiceValue),
                    CurrencySymbol = curSym,
                    CurrencyName = curName,
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

            if (ordCurrencySymbol < 0 && ordCurrencyName < 0 && list.Count > 0)
            {
                await PopulateSeaCurrenciesAsync(list, conn);
            }

            return list;
        }
        #endregion

        #region 3. Air Freight Jobs (Ultra-Fast Single-Pass CTE with Fallback)
        public async Task<List<AirJobReportItemDTO>> GetAirJobsAsync(JobStatusReportFilterVM filter)
        {
            filter ??= new JobStatusReportFilterVM();
            if (string.IsNullOrWhiteSpace(_connectionString)) return new List<AirJobReportItemDTO>();

            try
            {
                return await FetchAirJobsDirectAsync(filter);
            }
            catch
            {
                return await ExecuteAirProcsLegacyAsync(filter);
            }
        }

        private async Task<List<AirJobReportItemDTO>> FetchAirJobsDirectAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<AirJobReportItemDTO>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);

            string customerId = (filter.CustomerId ?? "").Trim();
            string portTypeId = (filter.PortType ?? "").Trim();
            string shipmentTypeId = (filter.ShipmentMode ?? "").Trim();
            if (string.Equals(shipmentTypeId, "Air", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shipmentTypeId, "Sea", StringComparison.OrdinalIgnoreCase))
            {
                shipmentTypeId = "";
            }

            const string sql = @"
;WITH FilteredJobs AS (
    SELECT 
        d.JobNo,
        d.Date,
        d.DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        d.CurrencyId,
        d.Quntity1,
        d.MaterialDescription,
        d.LCNo,
        d.HAWB,
        d.MAWB,
        d.ShipmentStatus,
        d.BENo,
        d.BEDate,
        d.Remarks,
        d.CustomerID,
        d.ExpenseTypeID,
        d.PortNameId,
        ISNULL(c.CustomerName, 'General Customer') AS CustomerName,
        ISNULL(c.CustomerAddress, '') AS CustomerAddress,
        ISNULL(curr.Symbol, ISNULL(curr.ShortName, '')) AS CurrencySymbol,
        ISNULL(curr.ShortName, '') AS CurrencyName,
        ISNULL(e.ExpenseType, 'Air Freight') AS ExpenseType,
        ISNULL(p.PortName, '') AS PortName
    FROM Sep_Documentation d WITH (NOLOCK)
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
    LEFT JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
    LEFT JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (@CustomerId = '' OR d.CustomerID = @CustomerId)
      AND (@PortTypeId = '' OR p.PortId = @PortTypeId OR (@PortTypeId LIKE '%,%' AND p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))))
      AND (@ShipmentTypeId = '' OR d.ExpenseTypeID = @ShipmentTypeId OR (@ShipmentTypeId LIKE '%,%' AND d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ','))))
      AND (e.ExpenseType LIKE '%Air%' OR ISNULL(d.HAWB, '') <> '' OR ISNULL(d.MAWB, '') <> '' OR d.ExpenseTypeID IN ('0001','0002','0003'))
),
Updates AS (
    SELECT 
        du.JobNo,
        MAX(CASE WHEN du.UpdateStatus = '01' THEN du.UpdateDateTime END) AS EtdDate,
        MAX(CASE WHEN du.UpdateStatus = '02' THEN du.UpdateDateTime END) AS FlightActualDeliveryDate,
        MAX(CASE WHEN du.UpdateStatus = '03' THEN du.UpdateDateTime END) AS ArrivalNoticeRcvdExpressDate,
        MAX(CASE WHEN du.UpdateStatus = '04' THEN du.UpdateDateTime END) AS DocsBankSignDate,
        MAX(CASE WHEN du.UpdateStatus = '05' THEN du.UpdateDateTime END) AS BeDateFromUpdate,
        MAX(CASE WHEN du.UpdateStatus = '06' THEN du.UpdateDateTime END) AS ActualDeliveryDate
    FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
    INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
    GROUP BY du.JobNo
)
SELECT 
    fj.JobNo,
    fj.Date AS RawJobDate,
    fj.ExpenseType,
    fj.PortName,
    fj.DocReceivedDate AS RawDocReceivedDate,
    fj.InvoiceNo,
    fj.InvoiceValue,
    fj.CurrencySymbol,
    fj.CurrencyName,
    fj.CurrencyId,
    fj.Quntity1,
    fj.MaterialDescription,
    fj.LCNo,
    fj.HAWB,
    fj.MAWB,
    fj.ShipmentStatus AS RawShipmentStatus,
    u.EtdDate,
    u.FlightActualDeliveryDate,
    u.ArrivalNoticeRcvdExpressDate,
    u.DocsBankSignDate,
    fj.BENo,
    u.BeDateFromUpdate,
    fj.BEDate AS RawBeDate,
    u.ActualDeliveryDate,
    fj.Remarks,
    fj.CustomerID,
    fj.CustomerName,
    fj.CustomerAddress
FROM FilteredJobs fj
LEFT JOIN Updates u ON fj.JobNo = u.JobNo
ORDER BY fj.JobNo ASC;
";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
            cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ToDate", toDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CustomerId", customerId));
            cmd.Parameters.Add(new SqlParameter("@PortTypeId", portTypeId));
            cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", shipmentTypeId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.FieldCount == 0 && await reader.NextResultAsync()) { }

            int ordJobNo = GetColOrdinal(reader, "JobNo");
            int ordJobDate = GetColOrdinal(reader, "RawJobDate");
            int ordExpenseType = GetColOrdinal(reader, "ExpenseType");
            int ordPortName = GetColOrdinal(reader, "PortName");
            int ordDocReceivedDate = GetColOrdinal(reader, "RawDocReceivedDate");
            int ordInvoiceNo = GetColOrdinal(reader, "InvoiceNo");
            int ordInvoiceValue = GetColOrdinal(reader, "InvoiceValue");
            int ordCurrencySymbol = GetColOrdinal(reader, "CurrencySymbol");
            int ordCurrencyName = GetColOrdinal(reader, "CurrencyName");
            int ordCurrencyId = GetColOrdinal(reader, "CurrencyId");
            int ordQuntity1 = GetColOrdinal(reader, "Quntity1");
            int ordMaterialDescription = GetColOrdinal(reader, "MaterialDescription");
            int ordLCNo = GetColOrdinal(reader, "LCNo");
            int ordHAWB = GetColOrdinal(reader, "HAWB");
            int ordMAWB = GetColOrdinal(reader, "MAWB");
            int ordRawShipmentStatus = GetColOrdinal(reader, "RawShipmentStatus");
            int ordEtdDate = GetColOrdinal(reader, "EtdDate");
            int ordFlightActualDeliveryDate = GetColOrdinal(reader, "FlightActualDeliveryDate");
            int ordArrivalNoticeRcvdExpressDate = GetColOrdinal(reader, "ArrivalNoticeRcvdExpressDate");
            int ordDocsBankSignDate = GetColOrdinal(reader, "DocsBankSignDate");
            int ordBENo = GetColOrdinal(reader, "BENo");
            int ordBeDateFromUpdate = GetColOrdinal(reader, "BeDateFromUpdate");
            int ordRawBeDate = GetColOrdinal(reader, "RawBeDate");
            int ordActualDeliveryDate = GetColOrdinal(reader, "ActualDeliveryDate");
            int ordRemarks = GetColOrdinal(reader, "Remarks");
            int ordCustomerID = GetColOrdinal(reader, "CustomerID");
            int ordCustomerName = GetColOrdinal(reader, "CustomerName");
            int ordCustomerAddress = GetColOrdinal(reader, "CustomerAddress");

            int sl = 1;
            while (await reader.ReadAsync())
            {
                string rawStatus = ReadCol(reader, ordRawShipmentStatus);
                var (statusText, statusClass) = ResolveAirStatus(rawStatus);

                string rawSym = ReadCol(reader, ordCurrencySymbol);
                string rawName = ReadCol(reader, ordCurrencyName);
                string rawCurrId = ReadCol(reader, ordCurrencyId);
                var (curSym, curName) = ResolveCurrency(rawSym, rawName, rawCurrId);

                string q1 = ReadCol(reader, ordQuntity1);
                string matDesc = ReadCol(reader, ordMaterialDescription);
                string materialAndPkgs = string.Join(" ", new[] { q1, matDesc }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                string beDate = ReadDt(reader, ordBeDateFromUpdate, "dd/MM/yyyy hh:mm tt");
                if (string.IsNullOrEmpty(beDate))
                {
                    beDate = ReadDateOnly(reader, ordRawBeDate);
                }

                list.Add(new AirJobReportItemDTO
                {
                    SlNo = sl++,
                    JobNo = ReadCol(reader, ordJobNo),
                    JobDate = ReadDateOnly(reader, ordJobDate),
                    ShipmentMode = ReadCol(reader, ordExpenseType),
                    Port = ReadCol(reader, ordPortName),
                    DocReceivedDate = ReadDateOnly(reader, ordDocReceivedDate),
                    InvoiceNo = ReadCol(reader, ordInvoiceNo),
                    InvoiceValue = ReadCol(reader, ordInvoiceValue),
                    CurrencySymbol = curSym,
                    CurrencyName = curName,
                    MaterialNamePkgsQty = materialAndPkgs,
                    ExpressNameCountry = "",
                    LCNo = ReadCol(reader, ordLCNo),
                    HawbNo = ReadCol(reader, ordHAWB),
                    MawbNo = ReadCol(reader, ordMAWB),
                    ETDDate = ReadDt(reader, ordEtdDate, "dd/MM/yyyy hh:mm tt"),
                    FlightActualDeliveryDate = ReadDt(reader, ordFlightActualDeliveryDate, "dd/MM/yyyy hh:mm tt"),
                    ArrivalNoticeRcvdExpressDate = ReadDt(reader, ordArrivalNoticeRcvdExpressDate, "dd/MM/yyyy hh:mm tt"),
                    BENo = ReadCol(reader, ordBENo),
                    BEDate = beDate,
                    ActualDeliveryDate = ReadDt(reader, ordActualDeliveryDate, "dd/MM/yyyy hh:mm tt"),
                    ShipmentStatus = statusText,
                    Remarks = ReadCol(reader, ordRemarks),
                    CustomerId = ReadCol(reader, ordCustomerID),
                    CustomerName = ReadCol(reader, ordCustomerName),
                    CustomerAddress = ReadCol(reader, ordCustomerAddress),
                    StatusClass = statusClass
                });
            }

            string filterStatus = (filter.JobStatus ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(filterStatus))
            {
                if (filterStatus.Contains("pend")) list = list.Where(x => x.StatusClass == "pending").ToList();
                else if (filterStatus.Contains("transit")) list = list.Where(x => x.StatusClass == "transit").ToList();
                else if (filterStatus.Contains("deliver")) list = list.Where(x => x.StatusClass == "delivered").ToList();
                for (int i = 0; i < list.Count; i++) list[i].SlNo = i + 1;
            }

            return list;
        }

        private async Task<List<AirJobReportItemDTO>> ExecuteAirProcsLegacyAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<AirJobReportItemDTO>();
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
            catch { }

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
            int ordCurrencySymbol = GetColOrdinal(reader, "CurrencySymbol");
            if (ordCurrencySymbol < 0) ordCurrencySymbol = GetColOrdinal(reader, "Symbol");
            int ordCurrencyName = GetColOrdinal(reader, "CurrencyName");
            if (ordCurrencyName < 0) ordCurrencyName = GetColOrdinal(reader, "ShortName");
            int ordCurrencyId = GetColOrdinal(reader, "CurrencyId");

            while (await reader.ReadAsync())
            {
                string rawStatus = ReadCol(reader, ordShipmentStatus);
                string resolvedStatus = !string.IsNullOrEmpty(rawStatus) ? rawStatus :
                    (statusClass == "delivered" ? "Delivered" : (statusClass == "transit" ? "In Transit" : "Pending"));

                string expType = ReadCol(reader, ordExpenseType);
                string custId = ReadCol(reader, ordCustomerId);
                string rawSym = ReadCol(reader, ordCurrencySymbol);
                string rawName = ReadCol(reader, ordCurrencyName);
                string rawCurrId = ReadCol(reader, ordCurrencyId);
                var (curSym, curName) = ResolveCurrency(rawSym, rawName, rawCurrId);

                list.Add(new AirJobReportItemDTO
                {
                    JobNo = ReadCol(reader, ordJobNo),
                    JobDate = ReadCol(reader, ordJobDate),
                    ShipmentMode = !string.IsNullOrEmpty(expType) ? expType : "Air Freight",
                    Port = ReadCol(reader, ordPortName),
                    DocReceivedDate = ReadCol(reader, ordDocReceivedDate),
                    InvoiceNo = ReadCol(reader, ordInvoiceNo),
                    InvoiceValue = ReadCol(reader, ordInvoiceValue),
                    CurrencySymbol = curSym,
                    CurrencyName = curName,
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

            if (ordCurrencySymbol < 0 && ordCurrencyName < 0 && list.Count > 0)
            {
                await PopulateAirCurrenciesAsync(list, conn);
            }

            return list;
        }
        #endregion

        #region 4. Generate Excel Report (Grouped by Customer, Sea & Air Separate)
        public async Task<byte[]> GenerateExcelReportAsync(JobStatusReportFilterVM filter)
        {
            filter ??= new JobStatusReportFilterVM();
            string activeTab = (filter.ActiveTab ?? "").Trim().ToLower();

            List<SeaJobReportItemDTO> seaJobs = new();
            List<AirJobReportItemDTO> airJobs = new();

            if (activeTab == "sea")
            {
                seaJobs = await GetSeaJobsAsync(filter);
            }
            else if (activeTab == "air")
            {
                airJobs = await GetAirJobsAsync(filter);
            }
            else
            {
                var seaTask = GetSeaJobsAsync(filter);
                var airTask = GetAirJobsAsync(filter);
                await Task.WhenAll(seaTask, airTask);
                seaJobs = seaTask.Result;
                airJobs = airTask.Result;
            }

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

                    string sym = !string.IsNullOrWhiteSpace(item.CurrencySymbol) ? item.CurrencySymbol : "$";
                    string rawInv = Convert.ToString(item.InvoiceValue)?.Replace(",", "").Trim() ?? "";
                    if (decimal.TryParse(rawInv, NumberStyles.Any, CultureInfo.InvariantCulture, out var decInv))
                    {
                        ws.Cell(currRow, 8).Value = decInv;
                        ws.Cell(currRow, 8).Style.NumberFormat.Format = $"\"{sym} \"#,##0.00";
                    }
                    else
                    {
                        ws.Cell(currRow, 8).Value = !string.IsNullOrWhiteSpace(rawInv) ? $"{sym} {item.InvoiceValue}" : "";
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

                        if (c == 8) // Invoice Value (Amount) -> Right
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else if (c == 9) // Container / Packages Quantity (Details) -> Left
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        }
                        else // All other columns -> Center
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

                    string symAir = !string.IsNullOrWhiteSpace(item.CurrencySymbol) ? item.CurrencySymbol : "$";
                    string rawInvAir = Convert.ToString(item.InvoiceValue)?.Replace(",", "").Trim() ?? "";
                    if (decimal.TryParse(rawInvAir, NumberStyles.Any, CultureInfo.InvariantCulture, out var decInvAir))
                    {
                        ws.Cell(currRow, 8).Value = decInvAir;
                        ws.Cell(currRow, 8).Style.NumberFormat.Format = $"\"{symAir} \"#,##0.00";
                    }
                    else
                    {
                        ws.Cell(currRow, 8).Value = !string.IsNullOrWhiteSpace(rawInvAir) ? $"{symAir} {item.InvoiceValue}" : "";
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

                        if (c == 8) // Invoice Value (Amount) -> Right
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else if (c == 9) // Material Name & Packages Quantity (Details) -> Left
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        }
                        else // All other columns -> Center
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
            filter ??= new JobStatusReportFilterVM();
            string activeTab = (filter.ActiveTab ?? "").Trim().ToLower();

            List<SeaJobReportItemDTO> seaJobs = new();
            List<AirJobReportItemDTO> airJobs = new();

            if (activeTab == "sea")
            {
                seaJobs = await GetSeaJobsAsync(filter);
            }
            else if (activeTab == "air")
            {
                airJobs = await GetAirJobsAsync(filter);
            }
            else
            {
                var seaTask = GetSeaJobsAsync(filter);
                var airTask = GetAirJobsAsync(filter);
                await Task.WhenAll(seaTask, airTask);
                seaJobs = seaTask.Result;
                airJobs = airTask.Result;
            }

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
                        Cell(FormatAmountWithCurrency(item.InvoiceValue, item.CurrencySymbol), right: true);
                        Cell(item.ContainerPkgs, center: false);
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
                        Cell(FormatAmountWithCurrency(item.InvoiceValue, item.CurrencySymbol), right: true);
                        Cell(item.MaterialNamePkgsQty, center: false);
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

        private static string FormatAmountWithCurrency(object? val, string? symbol)
        {
            string amt = FormatAmount(val);
            if (string.IsNullOrEmpty(amt)) return "";
            string sym = (symbol ?? "").Trim();
            if (string.IsNullOrEmpty(sym)) sym = "$";
            return $"{sym} {amt}";
        }

        private static (string sym, string name) ResolveCurrency(string? sym, string? name, string? id)
        {
            string s = (sym ?? "").Trim();
            string n = (name ?? "").Trim();
            string i = (id ?? "").Trim();

            if (!string.IsNullOrEmpty(s) && s != "?" && s != "N/A")
            {
                if (s.Equals("USD", StringComparison.OrdinalIgnoreCase)) return ("$", "USD");
                if (s.Equals("BDT", StringComparison.OrdinalIgnoreCase) || s.Equals("TK", StringComparison.OrdinalIgnoreCase)) return ("৳", "BDT");
                if (s.Equals("EUR", StringComparison.OrdinalIgnoreCase)) return ("€", "EUR");
                if (s.Equals("GBP", StringComparison.OrdinalIgnoreCase)) return ("£", "GBP");
                return (s, !string.IsNullOrEmpty(n) ? n : "USD");
            }

            string upper = (n + " " + i).ToUpperInvariant();
            if (upper.Contains("BDT") || upper.Contains("TK") || upper.Contains("TAKA"))
            {
                return ("৳", "BDT");
            }
            if (upper.Contains("USD") || upper.Contains("DOLLAR"))
            {
                return ("$", "USD");
            }
            if (upper.Contains("EUR") || upper.Contains("EURO"))
            {
                return ("€", "EUR");
            }
            if (upper.Contains("GBP") || upper.Contains("POUND"))
            {
                return ("£", "GBP");
            }

            return ("$", "USD");
        }

        private async Task PopulateAirCurrenciesAsync(List<AirJobReportItemDTO> items, SqlConnection conn)
        {
            try
            {
                var jobNos = items.Select(x => x.JobNo).Where(j => !string.IsNullOrWhiteSpace(j)).Distinct().ToList();
                if (jobNos.Count == 0) return;

                for (int i = 0; i < jobNos.Count; i += 500)
                {
                    var chunk = jobNos.Skip(i).Take(500).ToList();
                    string inClause = string.Join(",", chunk.Select((_, idx) => $"@j{idx}"));
                    string sql = $@"
                        SELECT d.JobNo, 
                               ISNULL(c.Symbol, c.ShortName) AS Symbol, 
                               c.ShortName AS CurrencyName,
                               d.CurrencyId
                        FROM Sep_Documentation d
                        LEFT JOIN CA_Def_Currency c ON d.CurrencyId = c.CurrencyId
                        WHERE d.JobNo IN ({inClause})";

                    using var cmd = new SqlCommand(sql, conn);
                    for (int idx = 0; idx < chunk.Count; idx++)
                    {
                        cmd.Parameters.AddWithValue($"@j{idx}", chunk[idx]);
                    }

                    using var rdr = await cmd.ExecuteReaderAsync();
                    var dict = new Dictionary<string, (string sym, string name)>(StringComparer.OrdinalIgnoreCase);
                    while (await rdr.ReadAsync())
                    {
                        string jNo = rdr["JobNo"]?.ToString()?.Trim() ?? "";
                        string sym = rdr["Symbol"]?.ToString()?.Trim() ?? "";
                        string nm = rdr["CurrencyName"]?.ToString()?.Trim() ?? "";
                        string cId = rdr["CurrencyId"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(jNo))
                        {
                            dict[jNo] = ResolveCurrency(sym, nm, cId);
                        }
                    }

                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.JobNo) && dict.TryGetValue(item.JobNo, out var resolved))
                        {
                            item.CurrencySymbol = resolved.sym;
                            item.CurrencyName = resolved.name;
                        }
                    }
                }
            }
            catch { }
        }

        private async Task PopulateSeaCurrenciesAsync(List<SeaJobReportItemDTO> items, SqlConnection conn)
        {
            try
            {
                var jobNos = items.Select(x => x.JobNo).Where(j => !string.IsNullOrWhiteSpace(j)).Distinct().ToList();
                if (jobNos.Count == 0) return;

                for (int i = 0; i < jobNos.Count; i += 500)
                {
                    var chunk = jobNos.Skip(i).Take(500).ToList();
                    string inClause = string.Join(",", chunk.Select((_, idx) => $"@j{idx}"));
                    string sql = $@"
                        SELECT d.JobNo, 
                               ISNULL(c.Symbol, c.ShortName) AS Symbol, 
                               c.ShortName AS CurrencyName,
                               d.CurrencyId
                        FROM Sep_Documentation d
                        LEFT JOIN CA_Def_Currency c ON d.CurrencyId = c.CurrencyId
                        WHERE d.JobNo IN ({inClause})";

                    using var cmd = new SqlCommand(sql, conn);
                    for (int idx = 0; idx < chunk.Count; idx++)
                    {
                        cmd.Parameters.AddWithValue($"@j{idx}", chunk[idx]);
                    }

                    using var rdr = await cmd.ExecuteReaderAsync();
                    var dict = new Dictionary<string, (string sym, string name)>(StringComparer.OrdinalIgnoreCase);
                    while (await rdr.ReadAsync())
                    {
                        string jNo = rdr["JobNo"]?.ToString()?.Trim() ?? "";
                        string sym = rdr["Symbol"]?.ToString()?.Trim() ?? "";
                        string nm = rdr["CurrencyName"]?.ToString()?.Trim() ?? "";
                        string cId = rdr["CurrencyId"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(jNo))
                        {
                            dict[jNo] = ResolveCurrency(sym, nm, cId);
                        }
                    }

                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.JobNo) && dict.TryGetValue(item.JobNo, out var resolved))
                        {
                            item.CurrencySymbol = resolved.sym;
                            item.CurrencyName = resolved.name;
                        }
                    }
                }
            }
            catch { }
        }

        #region 6. Generate CSV Report (With Company Info & Grouping like Excel)
        public async Task<byte[]> GenerateCsvReportAsync(JobStatusReportFilterVM filter)
        {
            filter ??= new JobStatusReportFilterVM();
            string activeTab = (filter.ActiveTab ?? "").Trim().ToLower();

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(true)))
            {
                if (activeTab == "sea")
                {
                    var seaJobs = await GetSeaJobsAsync(filter);
                    await WriteSeaCsvAsync(writer, seaJobs, filter);
                }
                else
                {
                    var airJobs = await GetAirJobsAsync(filter);
                    await WriteAirCsvAsync(writer, airJobs, filter);
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
            string padded = " " + text;
            if (padded.Length >= width) return padded;
            return padded.PadRight(width);
        }

        private static string PadRightAligned(string text, int width)
        {
            text ??= "";
            string padded = text + " ";
            if (padded.Length >= width) return padded;
            return padded.PadLeft(width);
        }

        private static string FormatCsvCell(string? text, int colIdx, int width, bool isHeader = false)
        {
            text = (text ?? "").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
            if (isHeader)
            {
                return PadCenter(text, width);
            }

            if (colIdx == 7) // Invoice Value (Amount) -> Right
            {
                return PadRightAligned(text, width);
            }
            else if (colIdx == 8) // Details (Material/Qty or Container/Pkgs) -> Left
            {
                return PadLeftAligned(text, width);
            }
            else // All other columns -> Center
            {
                return PadCenter(text, width);
            }
        }

        private static string BuildCenteredCsvHeaderRow(string text, int totalCols, int totalTableWidth)
        {
            string centered = PadCenter(text, totalTableWidth);
            return $"{EscapeCsv(centered)}{new string(',', totalCols - 1)}";
        }

        private static string BuildCustomerCsvRow(string customerTitle, int totalCols)
        {
            return $"{EscapeCsv(customerTitle)}{new string(',', totalCols - 1)}";
        }

        private static async Task WriteAirCsvAsync(StreamWriter writer, List<AirJobReportItemDTO> jobs, JobStatusReportFilterVM filter)
        {
            var headers = new[]
            {
                "SL. No", "Job No.", "Job Date", "Shipment Mode", "Port", "Doc's Receive Date",
                "Invoice No.", "Invoice Value", "Material Name & Pkgs Qty", "Express Name, Country",
                "L/C No.", "Express/HAWB No.", "MAWB No.", "ETD", "Flight Actual Arrival Dt.",
                "Arrival Notice Rcvd From Express Dt.", "Bill of Entry No", "Bill of Entry Date",
                "Actual Delivery Date", "Shipment Status", "Remarks"
            };

            int totalCols = headers.Length;
            int[] colWidths = new int[totalCols];

            for (int c = 0; c < totalCols; c++)
            {
                colWidths[c] = headers[c].Length;
            }

            var customerGroups = jobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            var preparedGroups = new List<(string CustomerName, int Count, List<string[]> Rows)>();

            foreach (var group in customerGroups)
            {
                var groupRows = new List<string[]>();
                int customerSl = 1;
                foreach (var item in group)
                {
                    var row = new[]
                    {
                        (customerSl++).ToString(),
                        item.JobNo ?? "",
                        item.JobDate ?? "",
                        item.ShipmentMode ?? "",
                        item.Port ?? "",
                        item.DocReceivedDate ?? "",
                        item.InvoiceNo ?? "",
                        FormatAmountWithCurrency(item.InvoiceValue, item.CurrencySymbol),
                        item.MaterialNamePkgsQty ?? "",
                        item.ExpressNameCountry ?? "",
                        item.LCNo ?? "",
                        item.HawbNo ?? "",
                        item.MawbNo ?? "",
                        item.ETDDate ?? "",
                        item.FlightActualDeliveryDate ?? "",
                        item.ArrivalNoticeRcvdExpressDate ?? "",
                        item.BENo ?? "",
                        item.BEDate ?? "",
                        item.ActualDeliveryDate ?? "",
                        item.ShipmentStatus ?? "",
                        item.Remarks ?? ""
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

            // Exact spacious column widths matching the Excel report layout
            int[] minAirWidths = new int[]
            {
                8,  // SL. No
                28, // Job No. (fits AirExp/Dha_BFL_26_000116 with padding)
                14, // Job Date
                16, // Shipment Mode
                10, // Port
                22, // Doc's Receive Date
                18, // Invoice No.
                16, // Invoice Value
                45, // Material Name & Pkgs Qty (wide column as in Excel screenshot)
                25, // Express Name, Country
                22, // L/C No.
                20, // Express/HAWB No.
                18, // MAWB No.
                14, // ETD
                28, // Flight Actual Arrival Dt.
                38, // Arrival Notice Rcvd From Express Dt.
                20, // Bill of Entry No
                20, // Bill of Entry Date
                22, // Actual Delivery Date
                18, // Shipment Status
                22  // Remarks
            };

            for (int c = 0; c < totalCols; c++)
            {
                colWidths[c] = Math.Max(minAirWidths[c], colWidths[c] + 2);
            }

            int totalTableWidth = colWidths.Sum() + (totalCols - 1);

            // Company Information Header (Centered across the report width like Excel)
            await writer.WriteLineAsync(BuildCenteredCsvHeaderRow("Formosa Logistics Limited", totalCols, totalTableWidth));
            await writer.WriteLineAsync(BuildCenteredCsvHeaderRow("Job Status Report - Formosa Logistics (Air Port)", totalCols, totalTableWidth));

            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "Date Range: All Dates";
            await writer.WriteLineAsync(BuildCenteredCsvHeaderRow(dateRangeStr, totalCols, totalTableWidth));
            await writer.WriteLineAsync(new string(',', totalCols - 1)); // Blank row

            if (preparedGroups.Count == 0)
            {
                var paddedHeaders = headers.Select((h, i) => EscapeCsv(FormatCsvCell(h, i, colWidths[i], isHeader: true)));
                await writer.WriteLineAsync(string.Join(",", paddedHeaders));

                await writer.WriteLineAsync($"\"No Air Freight records found.\"{new string(',', totalCols - 1)}");
                return;
            }

            foreach (var group in preparedGroups)
            {
                await writer.WriteLineAsync(BuildCustomerCsvRow($"CUSTOMER: {group.CustomerName.ToUpper()}  [Total Jobs: {group.Count}]", totalCols));

                var paddedHeaders = headers.Select((h, i) => EscapeCsv(FormatCsvCell(h, i, colWidths[i], isHeader: true)));
                await writer.WriteLineAsync(string.Join(",", paddedHeaders));

                foreach (var row in group.Rows)
                {
                    var formattedCells = row.Select((val, i) => EscapeCsv(FormatCsvCell(val, i, colWidths[i], isHeader: false)));
                    await writer.WriteLineAsync(string.Join(",", formattedCells));
                }

                await writer.WriteLineAsync(new string(',', totalCols - 1)); // Blank line between customer groups
            }
        }

        private static async Task WriteSeaCsvAsync(StreamWriter writer, List<SeaJobReportItemDTO> jobs, JobStatusReportFilterVM filter)
        {
            var headers = new[]
            {
                "Sl. No.", "Job No.", "Job Date", "Shipment Mode", "Port", "Doc's Rec. Dt.",
                "Invoice No", "Invoice Value", "Container /Pkgs", "Shipper", "B/L No.", "ETD",
                "Final IGM Rec. Dt.", "Ship Arrived", "Unstaff/Discharge", "Docs. Bank Sign",
                "B/E No.", "B/E Date", "Est. Delivery Dt.", "Actual Delivery Dt.", "Shipment Status",
                "Remarks", "Term", "Vessel Name & R-No", "Container No.", "Forwarder Name & Phone No."
            };

            int totalCols = headers.Length;
            int[] colWidths = new int[totalCols];

            for (int c = 0; c < totalCols; c++)
            {
                colWidths[c] = headers[c].Length;
            }

            var customerGroups = jobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            var preparedGroups = new List<(string CustomerName, int Count, List<string[]> Rows)>();

            foreach (var group in customerGroups)
            {
                var groupRows = new List<string[]>();
                int customerSl = 1;
                foreach (var item in group)
                {
                    var row = new[]
                    {
                        (customerSl++).ToString(),
                        item.JobNo ?? "",
                        item.JobDate ?? "",
                        item.ShipmentMode ?? "",
                        item.Port ?? "",
                        item.DocReceivedDate ?? "",
                        item.InvoiceNo ?? "",
                        FormatAmountWithCurrency(item.InvoiceValue, item.CurrencySymbol),
                        item.ContainerPkgs ?? "",
                        item.Shipper ?? "",
                        item.BLNo ?? "",
                        item.ETDDate ?? "",
                        item.FinalIgmRecDt ?? "",
                        item.ShipArrivedDate ?? "",
                        item.DischargeDate ?? "",
                        item.DocsBankSignDate ?? "",
                        item.BENo ?? "",
                        item.BEDate ?? "",
                        item.EstDeliveryDate ?? "",
                        item.ActualDeliveryDate ?? "",
                        item.ShipmentStatus ?? "",
                        item.Remarks ?? "",
                        item.Term ?? "",
                        item.VesselName ?? "",
                        item.ContainerNo ?? "",
                        item.FreightForwarder ?? ""
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

            // Exact spacious column widths matching the Excel report layout
            int[] minSeaWidths = new int[]
            {
                8,  // Sl. No.
                28, // Job No.
                14, // Job Date
                16, // Shipment Mode
                12, // Port
                18, // Doc's Rec. Dt.
                18, // Invoice No
                16, // Invoice Value
                45, // Container /Pkgs
                26, // Shipper
                20, // B/L No.
                14, // ETD
                20, // Final IGM Rec. Dt.
                16, // Ship Arrived
                20, // Unstaff/Discharge
                18, // Docs. Bank Sign
                18, // B/E No.
                14, // B/E Date
                18, // Est. Delivery Dt.
                22, // Actual Delivery Dt.
                18, // Shipment Status
                22, // Remarks
                12, // Term
                26, // Vessel Name & R-No
                20, // Container No.
                30  // Forwarder Name & Phone No.
            };

            for (int c = 0; c < totalCols; c++)
            {
                colWidths[c] = Math.Max(minSeaWidths[c], colWidths[c] + 2);
            }

            int totalTableWidth = colWidths.Sum() + (totalCols - 1);

            // Company Information Header (Centered across the report width like Excel)
            await writer.WriteLineAsync(BuildCenteredCsvHeaderRow("Formosa Logistics Limited", totalCols, totalTableWidth));
            await writer.WriteLineAsync(BuildCenteredCsvHeaderRow("Job Status Report - Formosa Logistics (Sea Port)", totalCols, totalTableWidth));

            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "Date Range: All Dates";
            await writer.WriteLineAsync(BuildCenteredCsvHeaderRow(dateRangeStr, totalCols, totalTableWidth));
            await writer.WriteLineAsync(new string(',', totalCols - 1)); // Blank row

            if (preparedGroups.Count == 0)
            {
                var paddedHeaders = headers.Select((h, i) => EscapeCsv(FormatCsvCell(h, i, colWidths[i], isHeader: true)));
                await writer.WriteLineAsync(string.Join(",", paddedHeaders));

                await writer.WriteLineAsync($"\"No Sea Freight records found.\"{new string(',', totalCols - 1)}");
                return;
            }

            foreach (var group in preparedGroups)
            {
                await writer.WriteLineAsync(BuildCustomerCsvRow($"CUSTOMER: {group.CustomerName.ToUpper()}  [Total Jobs: {group.Count}]", totalCols));

                var paddedHeaders = headers.Select((h, i) => EscapeCsv(FormatCsvCell(h, i, colWidths[i], isHeader: true)));
                await writer.WriteLineAsync(string.Join(",", paddedHeaders));

                foreach (var row in group.Rows)
                {
                    var formattedCells = row.Select((val, i) => EscapeCsv(FormatCsvCell(val, i, colWidths[i], isHeader: false)));
                    await writer.WriteLineAsync(string.Join(",", formattedCells));
                }

                await writer.WriteLineAsync(new string(',', totalCols - 1)); // Blank line between customer groups
            }
        }

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            val = val.Replace("\"", "\"\"");
            return $"\"{val}\"";
        }
        #endregion

        private static (string statusText, string statusClass) ResolveAirStatus(string? rawStatus)
        {
            string s = (rawStatus ?? "").Trim();
            if (s == "06" || s.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                return ("Delivered", "delivered");
            if (s == "01" || s == "02" || s == "03" || s == "04" || s == "05" ||
                s.Equals("In Transit", StringComparison.OrdinalIgnoreCase) || s.Equals("transit", StringComparison.OrdinalIgnoreCase))
                return ("In Transit", "transit");
            return ("Pending", "pending");
        }

        private static (string statusText, string statusClass) ResolveSeaStatus(string? rawStatus)
        {
            string s = (rawStatus ?? "").Trim();
            if (s == "14" || s.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                return ("Delivered", "delivered");
            if (s == "01" || s == "02" || s == "03" || s == "04" || s == "05" || s == "07" || s == "08" || s == "09" || s == "10" || s == "11" || s == "12" || s == "13" ||
                s.Equals("In Transit", StringComparison.OrdinalIgnoreCase) || s.Equals("transit", StringComparison.OrdinalIgnoreCase))
                return ("In Transit", "transit");
            return ("Pending", "pending");
        }

        private static string ReadDateOnly(SqlDataReader reader, int ord)
        {
            if (ord < 0 || reader.IsDBNull(ord)) return "";
            var val = reader.GetValue(ord);
            if (val is DateTime dt)
            {
                if (dt == DateTime.MinValue || dt.Year <= 1900) return "";
                return dt.ToString("dd/MM/yyyy");
            }
            return FormatDateOnly(val);
        }

        private static string ReadDt(SqlDataReader reader, int ord, string fmt = "dd/MM/yyyy hh:mm tt")
        {
            if (ord < 0 || reader.IsDBNull(ord)) return "";
            var val = reader.GetValue(ord);
            if (val is DateTime dt)
            {
                if (dt == DateTime.MinValue || dt.Year <= 1900) return "";
                return dt.ToString(fmt);
            }
            return FormatDt(val, fmt);
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
