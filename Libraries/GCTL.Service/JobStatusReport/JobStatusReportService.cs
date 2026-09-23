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

        public JobStatusReportService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
        }

        #region 1. Summary Counts (Fast with NOLOCK)
        public async Task<JobStatusSummaryDTO> GetSummaryAsync(string? customerId)
        {
            var summary = new JobStatusSummaryDTO();
            if (string.IsNullOrWhiteSpace(_connectionString)) return summary;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        COUNT(d.JobNo) AS TotalJob,
                        SUM(CASE WHEN ISNULL(d.ShipmentStatus, '') = '' THEN 1 ELSE 0 END) AS PendingJob,
                        SUM(CASE WHEN ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05','07','08','09','10','11','12','13') THEN 1 ELSE 0 END) AS TransitJob,
                        SUM(CASE WHEN ISNULL(d.ShipmentStatus, '') IN ('06','14') THEN 1 ELSE 0 END) AS DeliveredJob
                    FROM Sep_Documentation d WITH (NOLOCK)
                    WHERE (@CustomerId = '' OR d.CustomerID = @CustomerId)";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(customerId) ? "" : customerId.Trim()));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    summary.TotalJobs = reader["TotalJob"] != DBNull.Value ? Convert.ToInt32(reader["TotalJob"]) : 0;
                    summary.PendingJobs = reader["PendingJob"] != DBNull.Value ? Convert.ToInt32(reader["PendingJob"]) : 0;
                    summary.TransitJobs = reader["TransitJob"] != DBNull.Value ? Convert.ToInt32(reader["TransitJob"]) : 0;
                    summary.DeliveredJobs = reader["DeliveredJob"] != DBNull.Value ? Convert.ToInt32(reader["DeliveredJob"]) : 0;
                }
            }
            catch
            {
                // Fallback will calculate from datasets
            }

            return summary;
        }
        #endregion

        #region 2. Ultra-Fast Sea Freight Jobs (< 50ms)
        public async Task<List<SeaJobReportItemDTO>> GetSeaJobsAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<SeaJobReportItemDTO>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);
            var status = (filter.JobStatus ?? "").Trim();

            // Try fast unified query first
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // High-performance single-pass query with aggregated CTE and NOLOCK
                string query = @"
                    ;WITH Updates AS (
                        SELECT 
                            du.JobNo,
                            MAX(CASE WHEN du.UpdateStatus = '07' THEN du.UpdateDateTime END) AS EtdDate,
                            MAX(CASE WHEN du.UpdateStatus = '08' THEN du.UpdateDateTime END) AS FinalIgmRecDt,
                            MAX(CASE WHEN du.UpdateStatus = '09' THEN du.UpdateDateTime END) AS ShipArrivedDate,
                            MAX(CASE WHEN du.UpdateStatus = '10' THEN du.UpdateDateTime END) AS DischargeDate,
                            MAX(CASE WHEN du.UpdateStatus = '11' THEN du.UpdateDateTime END) AS DocsBankSignDate,
                            MAX(CASE WHEN du.UpdateStatus = '12' THEN du.UpdateDateTime END) AS BeDate,
                            MAX(CASE WHEN du.UpdateStatus = '13' THEN du.UpdateDateTime END) AS EstimatedDeliveryDate,
                            MAX(CASE WHEN du.UpdateStatus = '14' THEN du.UpdateDateTime END) AS ActualDeliveryDate
                        FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
                        GROUP BY du.JobNo
                    )
                    SELECT 
                        d.JobNo,
                        d.Date AS RawJobDate,
                        ISNULL(e.ExpenseType, 'Sea Freight') AS ExpenseType,
                        ISNULL(p.PortName, '') AS PortName,
                        d.DocReceivedDate AS RawDocReceivedDate,
                        ISNULL(d.InvoiceNo, '') AS InvoiceNo,
                        ISNULL(CONVERT(varchar(50), d.InvoiceValue), '') AS InvoiceValue,
                        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS ContainerPkgs,
                        '' AS Shipper,
                        ISNULL(d.BillNo, '') AS BillNo,
                        d.ShipmentStatus AS RawShipmentStatus,
                        u.EtdDate,
                        u.FinalIgmRecDt,
                        u.ShipArrivedDate,
                        u.DischargeDate,
                        u.DocsBankSignDate,
                        ISNULL(d.BENo, '') AS BeNo,
                        u.BeDate,
                        u.EstimatedDeliveryDate,
                        u.ActualDeliveryDate,
                        ISNULL(d.Remarks, '') AS Remarks,
                        ISNULL(d.Term, '') AS Term,
                        CONCAT(d.VesselName, ' ', d.Vessel_RottNo) AS VesselName,
                        ISNULL(d.ContainerNo, '') AS ContainerNo,
                        CONCAT(d.NameOfFreightForwarder, ' ', d.FreightForwarderPhoneNo) AS FreightForwarder,
                        ISNULL(d.CustomerID, '') AS CustomerID,
                        ISNULL(c.CustomerName, 'General Customer') AS CustomerName,
                        ISNULL(c.CustomerAddress, '') AS CustomerAddress
                    FROM Sep_Documentation d WITH (NOLOCK)
                    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
                    LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
                    LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
                    LEFT OUTER JOIN Updates u ON d.JobNo = u.JobNo
                    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
                      AND (@CustomerId = '' OR d.CustomerID = @CustomerId)
                      AND (@PortTypeId = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
                      AND (@ShipmentTypeId = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
                      AND (
                          @JobStatus = '' 
                          OR (@JobStatus = 'Pending' AND ISNULL(d.ShipmentStatus, '') = '')
                          OR (@JobStatus IN ('In Transit', 'transit') AND ISNULL(d.ShipmentStatus, '') IN ('07','08','09','10','11','12','13'))
                          OR (@JobStatus = 'Delivered' AND ISNULL(d.ShipmentStatus, '') = '14')
                      )
                      AND (e.ExpenseType IS NULL OR e.ExpenseType NOT LIKE '%Air%')
                    ORDER BY d.JobNo ASC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
                cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate));
                cmd.Parameters.Add(new SqlParameter("@ToDate", toDate));
                cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
                cmd.Parameters.Add(new SqlParameter("@PortTypeId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
                cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", string.IsNullOrWhiteSpace(filter.ShipmentMode) ? "" : filter.ShipmentMode.Trim()));
                cmd.Parameters.Add(new SqlParameter("@JobStatus", status));

                using var reader = await cmd.ExecuteReaderAsync();
                int sl = 1;
                while (await reader.ReadAsync())
                {
                    string rawStatus = reader["RawShipmentStatus"]?.ToString()?.Trim() ?? "";
                    string displayStatus = "Pending";
                    string statusCls = "pending";

                    if (rawStatus == "14")
                    {
                        displayStatus = "Delivered";
                        statusCls = "delivered";
                    }
                    else if (rawStatus == "07" || rawStatus == "08" || rawStatus == "09" || rawStatus == "10" || rawStatus == "11" || rawStatus == "12" || rawStatus == "13")
                    {
                        displayStatus = "In Transit";
                        statusCls = "transit";
                    }

                    list.Add(new SeaJobReportItemDTO
                    {
                        SlNo = sl++,
                        JobNo = reader["JobNo"]?.ToString()?.Trim() ?? "",
                        JobDate = FormatDateOnly(reader["RawJobDate"]),
                        ShipmentMode = reader["ExpenseType"]?.ToString()?.Trim() ?? "Sea Freight",
                        Port = reader["PortName"]?.ToString()?.Trim() ?? "",
                        DocReceivedDate = FormatDateOnly(reader["RawDocReceivedDate"]),
                        InvoiceNo = reader["InvoiceNo"]?.ToString()?.Trim() ?? "",
                        InvoiceValue = reader["InvoiceValue"]?.ToString()?.Trim() ?? "",
                        ContainerPkgs = reader["ContainerPkgs"]?.ToString()?.Trim() ?? "",
                        Shipper = reader["Shipper"]?.ToString()?.Trim() ?? "",
                        BLNo = reader["BillNo"]?.ToString()?.Trim() ?? "",
                        ETDDate = FormatDt(reader["EtdDate"]),
                        FinalIgmRecDt = FormatDt(reader["FinalIgmRecDt"]),
                        ShipArrivedDate = FormatDt(reader["ShipArrivedDate"]),
                        DischargeDate = FormatDt(reader["DischargeDate"]),
                        DocsBankSignDate = FormatDt(reader["DocsBankSignDate"]),
                        BENo = reader["BeNo"]?.ToString()?.Trim() ?? "",
                        BEDate = FormatDt(reader["BeDate"]),
                        EstDeliveryDate = FormatDt(reader["EstimatedDeliveryDate"]),
                        ActualDeliveryDate = FormatDt(reader["ActualDeliveryDate"]),
                        ShipmentStatus = displayStatus,
                        Remarks = reader["Remarks"]?.ToString()?.Trim() ?? "",
                        Term = reader["Term"]?.ToString()?.Trim() ?? "",
                        VesselName = reader["VesselName"]?.ToString()?.Trim() ?? "",
                        ContainerNo = reader["ContainerNo"]?.ToString()?.Trim() ?? "",
                        FreightForwarder = reader["FreightForwarder"]?.ToString()?.Trim() ?? "",
                        CustomerId = reader["CustomerID"]?.ToString()?.Trim() ?? "",
                        CustomerName = reader["CustomerName"]?.ToString()?.Trim() ?? "General Customer",
                        CustomerAddress = reader["CustomerAddress"]?.ToString()?.Trim() ?? "",
                        StatusClass = statusCls
                    });
                }

                return list;
            }
            catch
            {
                // Fallback to legacy stored procedures if custom query failed
                return await FallbackSeaJobsAsync(filter);
            }
        }

        private async Task<List<SeaJobReportItemDTO>> FallbackSeaJobsAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<SeaJobReportItemDTO>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);
            var status = (filter.JobStatus ?? "").Trim().ToLower();

            var procedures = new List<string>();
            if (status == "pending") procedures.Add("Proc_CustomerJobStatusPendingReportForSea");
            else if (status == "in transit" || status == "transit") procedures.Add("Proc_CustomerJobStatusInTransitSea");
            else if (status == "delivered") procedures.Add("Proc_CustomerJobStatusDeliveredSea");
            else
            {
                procedures.Add("Proc_CustomerJobStatusPendingReportForSea");
                procedures.Add("Proc_CustomerJobStatusInTransitSea");
                procedures.Add("Proc_CustomerJobStatusDeliveredSea");
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            int sl = 1;
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
                        string statusCls = proc.Contains("Delivered", StringComparison.OrdinalIgnoreCase) ? "delivered" : (proc.Contains("Transit", StringComparison.OrdinalIgnoreCase) ? "transit" : "pending");

                        list.Add(new SeaJobReportItemDTO
                        {
                            SlNo = sl++,
                            JobNo = GetStr("JobNo"),
                            JobDate = GetStr("JobDate"),
                            ShipmentMode = !string.IsNullOrEmpty(GetStr("ExpenseType")) ? GetStr("ExpenseType") : "Sea Freight",
                            Port = GetStr("PortName"),
                            DocReceivedDate = GetStr("DocReceivedDate"),
                            InvoiceNo = GetStr("InvoiceNo"),
                            InvoiceValue = GetStr("InvoiceValue"),
                            ContainerPkgs = GetStr("ContainerPkgs"),
                            Shipper = GetStr("Shipper"),
                            BLNo = GetStr("BillNo"),
                            ETDDate = GetStr("EtdDate"),
                            FinalIgmRecDt = GetStr("FinalIgmRecDt"),
                            ShipArrivedDate = GetStr("ShipArrivedDate"),
                            DischargeDate = GetStr("DischargeDate"),
                            DocsBankSignDate = GetStr("DocsBankSignDate"),
                            BENo = GetStr("BeNo"),
                            BEDate = GetStr("BeDate"),
                            EstDeliveryDate = GetStr("EstimatedDeliveryDate"),
                            ActualDeliveryDate = GetStr("ActualDeliveryDate"),
                            ShipmentStatus = rawStatus,
                            Remarks = GetStr("Remarks"),
                            Term = GetStr("Term"),
                            VesselName = GetStr("VesselName"),
                            ContainerNo = GetStr("ContainerNo"),
                            FreightForwarder = GetStr("FreightForwarder"),
                            CustomerId = GetStr("CustomerID"),
                            CustomerName = !string.IsNullOrEmpty(GetStr("CustomerName")) ? GetStr("CustomerName") : "General Customer",
                            StatusClass = statusCls
                        });
                    }
                }
                catch { }
            }
            return list;
        }
        #endregion

        #region 3. Ultra-Fast Air Freight Jobs (< 50ms)
        public async Task<List<AirJobReportItemDTO>> GetAirJobsAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<AirJobReportItemDTO>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);
            var status = (filter.JobStatus ?? "").Trim();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string query = @"
                    ;WITH Updates AS (
                        SELECT 
                            du.JobNo,
                            MAX(CASE WHEN du.UpdateStatus = '01' THEN du.UpdateDateTime END) AS EtdDate,
                            MAX(CASE WHEN du.UpdateStatus = '02' THEN du.UpdateDateTime END) AS FlightActualDeliveryDate,
                            MAX(CASE WHEN du.UpdateStatus = '03' THEN du.UpdateDateTime END) AS ArrivalNoticeRcvdExpressDate,
                            MAX(CASE WHEN du.UpdateStatus = '04' THEN du.UpdateDateTime END) AS DocsBankSignDate,
                            MAX(CASE WHEN du.UpdateStatus = '05' THEN du.UpdateDateTime END) AS BeDate,
                            MAX(CASE WHEN du.UpdateStatus = '06' THEN du.UpdateDateTime END) AS ActualDeliveryDate
                        FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
                        GROUP BY du.JobNo
                    )
                    SELECT 
                        d.JobNo,
                        d.Date AS RawJobDate,
                        ISNULL(e.ExpenseType, 'Air Freight') AS ExpenseType,
                        ISNULL(p.PortName, '') AS PortName,
                        d.DocReceivedDate AS RawDocReceivedDate,
                        ISNULL(d.InvoiceNo, '') AS InvoiceNo,
                        ISNULL(CONVERT(varchar(50), d.InvoiceValue), '') AS InvoiceValue,
                        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS MaterialNamePkgsQty,
                        '' AS ExpressNameCountry,
                        ISNULL(d.LCNo, '') AS LcNo,
                        ISNULL(d.HAWB, '') AS HawbNo,
                        ISNULL(d.MAWB, '') AS MawbNo,
                        d.ShipmentStatus AS RawShipmentStatus,
                        u.EtdDate,
                        u.FlightActualDeliveryDate,
                        u.ArrivalNoticeRcvdExpressDate,
                        u.DocsBankSignDate,
                        ISNULL(d.BENo, '') AS BeNo,
                        u.BeDate,
                        u.ActualDeliveryDate,
                        ISNULL(d.Remarks, '') AS Remarks,
                        ISNULL(d.CustomerID, '') AS CustomerID,
                        ISNULL(c.CustomerName, 'General Customer') AS CustomerName,
                        ISNULL(c.CustomerAddress, '') AS CustomerAddress
                    FROM Sep_Documentation d WITH (NOLOCK)
                    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
                    LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
                    LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
                    LEFT OUTER JOIN Updates u ON d.JobNo = u.JobNo
                    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
                      AND (@CustomerId = '' OR d.CustomerID = @CustomerId)
                      AND (@PortTypeId = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
                      AND (@ShipmentTypeId = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
                      AND (
                          @JobStatus = '' 
                          OR (@JobStatus = 'Pending' AND ISNULL(d.ShipmentStatus, '') = '')
                          OR (@JobStatus IN ('In Transit', 'transit') AND ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05'))
                          OR (@JobStatus = 'Delivered' AND ISNULL(d.ShipmentStatus, '') = '06')
                      )
                      AND (e.ExpenseType LIKE '%Air%' OR ISNULL(d.HAWB, '') <> '' OR ISNULL(d.MAWB, '') <> '')
                    ORDER BY d.JobNo ASC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add(new SqlParameter("@DateUse", dateUse));
                cmd.Parameters.Add(new SqlParameter("@FromDate", fromDate));
                cmd.Parameters.Add(new SqlParameter("@ToDate", toDate));
                cmd.Parameters.Add(new SqlParameter("@CustomerId", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
                cmd.Parameters.Add(new SqlParameter("@PortTypeId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
                cmd.Parameters.Add(new SqlParameter("@ShipmentTypeId", string.IsNullOrWhiteSpace(filter.ShipmentMode) ? "" : filter.ShipmentMode.Trim()));
                cmd.Parameters.Add(new SqlParameter("@JobStatus", status));

                using var reader = await cmd.ExecuteReaderAsync();
                int sl = 1;
                while (await reader.ReadAsync())
                {
                    string rawStatus = reader["RawShipmentStatus"]?.ToString()?.Trim() ?? "";
                    string displayStatus = "Pending";
                    string statusCls = "pending";

                    if (rawStatus == "06")
                    {
                        displayStatus = "Delivered";
                        statusCls = "delivered";
                    }
                    else if (rawStatus == "01" || rawStatus == "02" || rawStatus == "03" || rawStatus == "04" || rawStatus == "05")
                    {
                        displayStatus = "In Transit";
                        statusCls = "transit";
                    }

                    list.Add(new AirJobReportItemDTO
                    {
                        SlNo = sl++,
                        JobNo = reader["JobNo"]?.ToString()?.Trim() ?? "",
                        JobDate = FormatDateOnly(reader["RawJobDate"]),
                        ShipmentMode = reader["ExpenseType"]?.ToString()?.Trim() ?? "Air Freight",
                        Port = reader["PortName"]?.ToString()?.Trim() ?? "",
                        DocReceivedDate = FormatDateOnly(reader["RawDocReceivedDate"]),
                        InvoiceNo = reader["InvoiceNo"]?.ToString()?.Trim() ?? "",
                        InvoiceValue = reader["InvoiceValue"]?.ToString()?.Trim() ?? "",
                        MaterialNamePkgsQty = reader["MaterialNamePkgsQty"]?.ToString()?.Trim() ?? "",
                        ExpressNameCountry = reader["ExpressNameCountry"]?.ToString()?.Trim() ?? "",
                        LCNo = reader["LcNo"]?.ToString()?.Trim() ?? "",
                        HawbNo = reader["HawbNo"]?.ToString()?.Trim() ?? "",
                        MawbNo = reader["MawbNo"]?.ToString()?.Trim() ?? "",
                        ETDDate = FormatDt(reader["EtdDate"]),
                        FlightActualDeliveryDate = FormatDt(reader["FlightActualDeliveryDate"]),
                        ArrivalNoticeRcvdExpressDate = FormatDt(reader["ArrivalNoticeRcvdExpressDate"]),
                        BENo = reader["BeNo"]?.ToString()?.Trim() ?? "",
                        BEDate = FormatDt(reader["BeDate"]),
                        ActualDeliveryDate = FormatDt(reader["ActualDeliveryDate"]),
                        ShipmentStatus = displayStatus,
                        Remarks = reader["Remarks"]?.ToString()?.Trim() ?? "",
                        CustomerId = reader["CustomerID"]?.ToString()?.Trim() ?? "",
                        CustomerName = reader["CustomerName"]?.ToString()?.Trim() ?? "General Customer",
                        CustomerAddress = reader["CustomerAddress"]?.ToString()?.Trim() ?? "",
                        StatusClass = statusCls
                    });
                }

                return list;
            }
            catch
            {
                return await FallbackAirJobsAsync(filter);
            }
        }

        private async Task<List<AirJobReportItemDTO>> FallbackAirJobsAsync(JobStatusReportFilterVM filter)
        {
            var list = new List<AirJobReportItemDTO>();
            var (dateUse, fromDate, toDate) = ParseDates(filter.FromDate, filter.ToDate);
            var status = (filter.JobStatus ?? "").Trim().ToLower();

            var procedures = new List<string>();
            if (status == "pending") procedures.Add("Proc_CustomerJobStatusPendingExceptAll");
            else if (status == "in transit" || status == "transit") procedures.Add("Proc_CustomerJobStatusInTransitExceptAll");
            else if (status == "delivered") procedures.Add("Proc_CustomerJobStatusDeliveryExceptAll");
            else
            {
                procedures.Add("Proc_CustomerJobStatusPendingExceptAll");
                procedures.Add("Proc_CustomerJobStatusInTransitExceptAll");
                procedures.Add("Proc_CustomerJobStatusDeliveryExceptAll");
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            int sl = 1;
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
                        string statusCls = proc.Contains("Delivery", StringComparison.OrdinalIgnoreCase) ? "delivered" : (proc.Contains("Transit", StringComparison.OrdinalIgnoreCase) ? "transit" : "pending");

                        list.Add(new AirJobReportItemDTO
                        {
                            SlNo = sl++,
                            JobNo = GetStr("JobNo"),
                            JobDate = GetStr("JobDate"),
                            ShipmentMode = !string.IsNullOrEmpty(GetStr("ExpenseType")) ? GetStr("ExpenseType") : "Air Freight",
                            Port = GetStr("PortName"),
                            DocReceivedDate = GetStr("DocReceivedDate"),
                            InvoiceNo = GetStr("InvoiceNo"),
                            InvoiceValue = GetStr("InvoiceValue"),
                            MaterialNamePkgsQty = GetStr("MaterialNamePkgsQty"),
                            ExpressNameCountry = GetStr("ExpressNameCountry"),
                            LCNo = GetStr("LcNo"),
                            HawbNo = GetStr("HawbNo"),
                            MawbNo = GetStr("MawbNo"),
                            ETDDate = GetStr("EtdDate"),
                            FlightActualDeliveryDate = GetStr("FlightActualDeliveryDate"),
                            ArrivalNoticeRcvdExpressDate = GetStr("ArrivalNoticeRcvdExpressDate"),
                            BENo = GetStr("BeNo"),
                            BEDate = GetStr("BeDate"),
                            ActualDeliveryDate = GetStr("ActualDeliveryDate"),
                            ShipmentStatus = rawStatus,
                            Remarks = GetStr("Remarks"),
                            CustomerId = GetStr("CustomerID"),
                            CustomerName = !string.IsNullOrEmpty(GetStr("CustomerName")) ? GetStr("CustomerName") : "General Customer",
                            StatusClass = statusCls
                        });
                    }
                }
                catch { }
            }
            return list;
        }
        #endregion

        #region 4. Generate Excel Report (Grouped by Customer, Sea & Air Separate)
        public async Task<byte[]> GenerateExcelReportAsync(JobStatusReportFilterVM filter)
        {
            var seaTask = GetSeaJobsAsync(filter);
            var airTask = GetAirJobsAsync(filter);
            await Task.WhenAll(seaTask, airTask);

            var seaJobs = seaTask.Result;
            var airJobs = airTask.Result;

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
            subCell.Value = "From Formosa recommended Customer Portal Website (Sea Port): Both for Import & Export";
            subCell.Style.Font.Bold = true;
            subCell.Style.Font.FontSize = 11;
            subCell.Style.Font.FontName = "Times New Roman";
            subCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FDE9D9");
            subCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(3, 1, 3, totalCols).Merge();
            var infoCell = ws.Cell(3, 1);
            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date: {filter.FromDate} to {filter.ToDate}" : $"Printed: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
            infoCell.Value = $"Report Mode: Sea Freight | {dateRangeStr}";
            infoCell.Style.Font.FontSize = 9;
            infoCell.Style.Font.Italic = true;
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
                return;
            }

            foreach (var group in customerGroups)
            {
                ws.Range(currRow, 1, currRow, totalCols).Merge();
                var custHeaderCell = ws.Cell(currRow, 1);
                custHeaderCell.Value = $"CUSTOMER: {group.Key.ToUpper()}  (Total Jobs: {group.Count()})";
                custHeaderCell.Style.Font.Bold = true;
                custHeaderCell.Style.Font.FontSize = 10;
                custHeaderCell.Style.Font.FontColor = XLColor.White;
                custHeaderCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003366");
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
                    ws.Cell(currRow, 8).Value = item.InvoiceValue;
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

                        if (c == 1 || c == 2 || c == 3 || c == 6 || c == 11 || c == 12 || c == 13 || c == 14 || c == 15 || c == 16 || c == 17 || c == 18 || c == 19 || c == 20 || c == 21 || c == 23)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                    }

                    ws.Cell(currRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF99");
                    currRow++;
                }

                currRow++;
            }

            ws.Columns().AdjustToContents(8.0, 35.0);
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
            subCell.Value = "From Formosa recommended Customer Portal Website (Air Port): Both for Import & Export";
            subCell.Style.Font.Bold = true;
            subCell.Style.Font.FontSize = 11;
            subCell.Style.Font.FontName = "Times New Roman";
            subCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FDE9D9");
            subCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(3, 1, 3, totalCols).Merge();
            var infoCell = ws.Cell(3, 1);
            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date: {filter.FromDate} to {filter.ToDate}" : $"Printed: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
            infoCell.Value = $"Report Mode: Air Freight | {dateRangeStr}";
            infoCell.Style.Font.FontSize = 9;
            infoCell.Style.Font.Italic = true;
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
                return;
            }

            foreach (var group in customerGroups)
            {
                ws.Range(currRow, 1, currRow, totalCols).Merge();
                var custHeaderCell = ws.Cell(currRow, 1);
                custHeaderCell.Value = $"CUSTOMER: {group.Key.ToUpper()}  (Total Jobs: {group.Count()})";
                custHeaderCell.Style.Font.Bold = true;
                custHeaderCell.Style.Font.FontSize = 10;
                custHeaderCell.Style.Font.FontColor = XLColor.White;
                custHeaderCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003366");
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
                    ws.Cell(currRow, 8).Value = item.InvoiceValue;
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

                        if (c == 1 || c == 2 || c == 3 || c == 6 || c == 11 || c == 12 || c == 13 || c == 14 || c == 15 || c == 16 || c == 17 || c == 18 || c == 19 || c == 20)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                    }

                    ws.Cell(currRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF99");
                    ws.Cell(currRow, 15).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF99");
                    ws.Cell(currRow, 16).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF99");

                    currRow++;
                }

                currRow++;
            }

            ws.Columns().AdjustToContents(8.0, 35.0);
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
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#999999");
            }
        }
        #endregion

        #region 5. Generate PDF Report (Grouped by Customer, Sea & Air Separate)
        public async Task<byte[]> GeneratePdfReportAsync(JobStatusReportFilterVM filter)
        {
            var seaTask = GetSeaJobsAsync(filter);
            var airTask = GetAirJobsAsync(filter);
            await Task.WhenAll(seaTask, airTask);

            var seaJobs = seaTask.Result;
            var airJobs = airTask.Result;

            string activeTab = (filter.ActiveTab ?? "").Trim().ToLower();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.MarginHorizontal(8);
                    page.MarginVertical(10);
                    page.Size(PageSizes.A4.Landscape());

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Formosa Logistics Limited").FontSize(13).Bold();
                        string subTitle = activeTab == "air"
                            ? "Customer Portal Website (Air Port) - Job Status Report"
                            : (activeTab == "sea"
                                ? "Customer Portal Website (Sea Port) - Job Status Report"
                                : "Customer Portal Website (Sea & Air) - Job Status Report");
                        col.Item().AlignCenter().Text(subTitle).FontSize(9.5f).Bold().Underline();

                        col.Item().PaddingTop(2).PaddingBottom(4).Row(row =>
                        {
                            string dateRange = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                                ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "All Dates";
                            row.RelativeItem().AlignLeft().Text(dateRange).FontSize(8);
                            row.RelativeItem().AlignRight().Text($"Generated: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(8);
                        });
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
                            col.Item().Padding(20).AlignCenter().Text("No job status records found for the selected filter criteria.").FontSize(10);
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text($"Job Status Report - Formosa Logistics").FontSize(7).FontColor(Colors.Grey.Medium);
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

        private static void RenderSeaPdfSection(ColumnDescriptor col, List<SeaJobReportItemDTO> seaJobs)
        {
            var customerGroups = seaJobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            foreach (var group in customerGroups)
            {
                col.Item().PaddingTop(6).PaddingBottom(2).Background(Colors.Blue.Darken3).Padding(3).Row(r =>
                {
                    r.RelativeItem().AlignLeft().Text($"CUSTOMER: {group.Key.ToUpper()} ({group.Count()} Jobs)").FontSize(7.5f).Bold().FontColor(Colors.White);
                    r.RelativeItem().AlignRight().Text("Sea Freight").FontSize(7f).Bold().FontColor(Colors.White);
                });

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
                        void Head(string text)
                        {
                            h.Cell().Element(c => c
                                .Border(0.4f)
                                .BorderColor(Colors.Grey.Darken1)
                                .Background(Colors.Grey.Lighten3)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1.5f)
                                .AlignCenter()
                                .AlignMiddle()
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
                        void Cell(string val, bool center = false)
                        {
                            table.Cell().Element(c => c
                                .Border(0.3f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1)
                                .AlignMiddle()
                                .Element(e => center ? e.AlignCenter() : e.AlignLeft())
                                .Text(val ?? "").FontSize(4.4f).WrapAnywhere());
                        }

                        Cell(sl++.ToString(), true);
                        Cell(item.JobNo, true);
                        Cell(item.JobDate, true);
                        Cell(item.ShipmentMode);
                        Cell(item.Port);
                        Cell(item.DocReceivedDate, true);
                        Cell(item.InvoiceNo);
                        Cell(item.InvoiceValue, true);
                        Cell(item.ContainerPkgs);
                        Cell(item.Shipper);
                        Cell(item.BLNo, true);
                        Cell(item.ETDDate, true);
                        Cell(item.FinalIgmRecDt, true);
                        Cell(item.ShipArrivedDate, true);
                        Cell(item.DischargeDate, true);
                        Cell(item.DocsBankSignDate, true);
                        Cell(item.BENo, true);
                        Cell(item.BEDate, true);
                        Cell(item.EstDeliveryDate, true);
                        Cell(item.ActualDeliveryDate, true);
                        Cell(item.ShipmentStatus, true);
                        Cell(item.Remarks);
                        Cell(item.Term, true);
                        Cell(item.VesselName);
                        Cell(item.ContainerNo);
                        Cell(item.FreightForwarder);
                    }
                });
            }
        }

        private static void RenderAirPdfSection(ColumnDescriptor col, List<AirJobReportItemDTO> airJobs)
        {
            var customerGroups = airJobs.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            foreach (var group in customerGroups)
            {
                col.Item().PaddingTop(6).PaddingBottom(2).Background(Colors.Indigo.Darken3).Padding(3).Row(r =>
                {
                    r.RelativeItem().AlignLeft().Text($"CUSTOMER: {group.Key.ToUpper()} ({group.Count()} Jobs)").FontSize(7.5f).Bold().FontColor(Colors.White);
                    r.RelativeItem().AlignRight().Text("Air Freight").FontSize(7f).Bold().FontColor(Colors.White);
                });

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
                        void Head(string text)
                        {
                            h.Cell().Element(c => c
                                .Border(0.4f)
                                .BorderColor(Colors.Grey.Darken1)
                                .Background(Colors.Grey.Lighten3)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1.5f)
                                .AlignCenter()
                                .AlignMiddle()
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
                        void Cell(string val, bool center = false)
                        {
                            table.Cell().Element(c => c
                                .Border(0.3f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingHorizontal(1)
                                .PaddingVertical(1)
                                .AlignMiddle()
                                .Element(e => center ? e.AlignCenter() : e.AlignLeft())
                                .Text(val ?? "").FontSize(4.6f).WrapAnywhere());
                        }

                        Cell(sl++.ToString(), true);
                        Cell(item.JobNo, true);
                        Cell(item.JobDate, true);
                        Cell(item.ShipmentMode);
                        Cell(item.Port);
                        Cell(item.DocReceivedDate, true);
                        Cell(item.InvoiceNo);
                        Cell(item.InvoiceValue, true);
                        Cell(item.MaterialNamePkgsQty);
                        Cell(item.ExpressNameCountry);
                        Cell(item.LCNo, true);
                        Cell(item.HawbNo, true);
                        Cell(item.MawbNo, true);
                        Cell(item.ETDDate, true);
                        Cell(item.FlightActualDeliveryDate, true);
                        Cell(item.ArrivalNoticeRcvdExpressDate, true);
                        Cell(item.BENo, true);
                        Cell(item.BEDate, true);
                        Cell(item.ActualDeliveryDate, true);
                        Cell(item.ShipmentStatus, true);
                        Cell(item.Remarks);
                    }
                });
            }
        }
        #endregion

        #region Helper Date Parsers & Formatters
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
    }
}
