using Dapper;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.VoucherApproval;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.VoucherApproval
{
    public class VoucherApprovalService : IVoucherApprovalService
    {
        private readonly GCTL_ERP_DBContext _context;

        public VoucherApprovalService(GCTL_ERP_DBContext context)
        {
            _context = context;
        }

        public async Task<List<CommonSelectVM>> GetCustomerDropdownAsync()
        {
            try
            {
                return await _context.SalesCustomers
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.CustomerId) && !string.IsNullOrEmpty(x.CustomerName))
                    .OrderBy(x => x.CustomerName)
                    .Select(x => new CommonSelectVM
                    {
                        Code = x.CustomerId,
                        Name = x.CustomerName
                    })
                    .ToListAsync();
            }
            catch
            {
                try
                {
                    var conn = _context.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                        await conn.OpenAsync();

                    const string sql = @"
                        SELECT CustomerId AS Code, CustomerName AS Name 
                        FROM Sales_Customer 
                        WHERE CustomerName IS NOT NULL AND CustomerName <> ''
                        ORDER BY CustomerName";

                    var result = await conn.QueryAsync<CommonSelectVM>(sql);
                    return result.ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VoucherApprovalService.GetCustomerDropdownAsync Error]: {ex.Message}");
                    return new List<CommonSelectVM>();
                }
            }
        }

        private (DateTime? fromDt, DateTime? toDt) ParseDates(string? fromStr, string? toStr)
        {
            DateTime? fromDt = null;
            if (!string.IsNullOrWhiteSpace(fromStr) &&
                DateTime.TryParseExact(fromStr, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fDate))
            {
                fromDt = fDate;
            }

            DateTime? toDt = null;
            if (!string.IsNullOrWhiteSpace(toStr) &&
                DateTime.TryParseExact(toStr, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var tDate))
            {
                toDt = tDate;
            }

            return (fromDt, toDt);
        }

        public async Task<(List<VoucherApprovalItemVM> Items, int TotalCount, int FilteredCount)> GetApprovalListPagedAsync(VoucherApprovalFilterVM filter)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var (fromDt, toDt) = ParseDates(filter.FromDate, filter.ToDate);
                var status = string.IsNullOrWhiteSpace(filter.VoucherStatus) ? "Not Approved" : filter.VoucherStatus;

                // Call user's exact Stored Procedure: sp_GetVoucherApprovalList
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerCode", string.IsNullOrWhiteSpace(filter.CustomerCode) ? null : filter.CustomerCode, DbType.String);
                parameters.Add("@FromDate", fromDt, DbType.DateTime);
                parameters.Add("@ToDate", toDt, DbType.DateTime);
                parameters.Add("@VoucherStatus", status, DbType.String);

                var allItems = (await conn.QueryAsync<VoucherApprovalItemVM>(
                    "sp_GetVoucherApprovalList",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                var totalCount = allItems.Count;

                // Client-side search within SP result if SearchValue exists
                var filtered = allItems;
                if (!string.IsNullOrWhiteSpace(filter.SearchValue))
                {
                    var s = filter.SearchValue.Trim().ToLower();
                    filtered = allItems.Where(x =>
                        (!string.IsNullOrEmpty(x.VoucherNo) && x.VoucherNo.ToLower().Contains(s)) ||
                        (!string.IsNullOrEmpty(x.JobNo) && x.JobNo.ToLower().Contains(s)) ||
                        (!string.IsNullOrEmpty(x.Narration) && x.Narration.ToLower().Contains(s)) ||
                        (!string.IsNullOrEmpty(x.Branch) && x.Branch.ToLower().Contains(s)) ||
                        (!string.IsNullOrEmpty(x.HAWB) && x.HAWB.ToLower().Contains(s)) ||
                        (!string.IsNullOrEmpty(x.BENo) && x.BENo.ToLower().Contains(s))
                    ).ToList();
                }

                var filteredCount = filtered.Count;

                // Apply paging
                var pagedItems = filtered
                    .Skip(Math.Max(0, filter.Start))
                    .Take(filter.Length > 0 ? filter.Length : 10)
                    .ToList();

                return (pagedItems, totalCount, filteredCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VoucherApprovalService.GetApprovalListPagedAsync SP Error]: {ex.Message} {ex.StackTrace}");
                return (new List<VoucherApprovalItemVM>(), 0, 0);
            }
        }

        public async Task<VoucherApprovalSummaryVM> GetSummaryAsync(VoucherApprovalFilterVM filter)
        {
            var summary = new VoucherApprovalSummaryVM();
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var (fromDt, toDt) = ParseDates(filter.FromDate, filter.ToDate);
                var status = string.IsNullOrWhiteSpace(filter.VoucherStatus) ? "Not Approved" : filter.VoucherStatus;

                // Summary using SP with exact selected VoucherStatus filter
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerCode", string.IsNullOrWhiteSpace(filter.CustomerCode) ? null : filter.CustomerCode, DbType.String);
                parameters.Add("@FromDate", fromDt, DbType.DateTime);
                parameters.Add("@ToDate", toDt, DbType.DateTime);
                parameters.Add("@VoucherStatus", status, DbType.String);

                var list = (await conn.QueryAsync<VoucherApprovalItemVM>(
                    "sp_GetVoucherApprovalList",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                summary.TotalVouchers = list.Count;
                summary.TotalAmount = list.Sum(x => x.Amount);
                summary.PendingApproval = list.Count(x => string.IsNullOrWhiteSpace(x.IsApproved) || x.IsApproved == "Not Approved");
                summary.ApprovedVouchers = list.Count(x => x.IsApproved == "Approved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VoucherApprovalService.GetSummaryAsync Error]: {ex.Message} {ex.StackTrace}");
            }
            return summary;
        }

        public async Task<bool> UpdateApprovalStatusAsync(List<string> voucherNos, string status, string username)
        {
            if (voucherNos == null || !voucherNos.Any())
                return false;

            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                // Call user's exact Stored Procedure: sp_UpdateVoucherApprovalStatus
                var parameters = new DynamicParameters();
                parameters.Add("@VoucherNos", string.Join(",", voucherNos), DbType.String);
                parameters.Add("@Status", status, DbType.String);
                parameters.Add("@ApprovedBy", username, DbType.String);

                var rows = await conn.ExecuteScalarAsync<int>(
                    "sp_UpdateVoucherApprovalStatus",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VoucherApprovalService.UpdateApprovalStatusAsync SP Error]: {ex.Message}");
                return false;
            }
        }
    }
}
