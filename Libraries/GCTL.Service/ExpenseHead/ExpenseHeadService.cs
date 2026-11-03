using Dapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.Common;

using GCTL.Core.ViewModels.ExpenseHead;
using GCTL.Data.Models;
using GCTL.Service.CoreServiceType;
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
using Core_ServiceType = GCTL.Data.Models.CoreServiceType;
using SpreadsheetColumn = DocumentFormat.OpenXml.Spreadsheet.Column;



namespace GCTL.Service.ExpenseHead
{
    public class ExpenseHeadService : AppService<CfDefExpenseHead>, IExpenseHead
    {
        #region
        private readonly IRepository<CfDefExpenseHead> _repository;
        private readonly IRepository<Core_ServiceType> _serviceTypeRepository;
        private readonly IRepository<CfDefExpenseType> _expenseTypeRepository;
        private readonly GCTL_ERP_DBContext _context;
        private readonly string _connectionString;

        public ExpenseHeadService(IRepository<CfDefExpenseHead> genericRepository, GCTL_ERP_DBContext context, IConfiguration configuration, IRepository<Core_ServiceType> serviceTypeRepository, IRepository<CfDefExpenseType> expenseTypeRepository) : base(genericRepository)
        {
            _repository = genericRepository;
            _context = context;
            _connectionString = configuration.GetConnectionString("connection");
            _serviceTypeRepository = serviceTypeRepository;
            _expenseTypeRepository = expenseTypeRepository;
        }
        #endregion

        #region
        public async Task<bool> BulkDeleteAsync(List<int> ids)
        {
            if (ids == null || !ids.Any()) return false;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                var csv = string.Join(",", ids);
                var p = new DynamicParameters();
                p.Add("@TCs", csv);
                var deleted = await conn.ExecuteScalarAsync<int>(
                    "dbo.sp_CF_DeleteExpenseHead",
                    p,
                    commandType: CommandType.StoredProcedure
                );
                return deleted > 0;
            }
            catch
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
                    return false;
                }
            }
        }

        public async Task<List<ExpenseHeadVM>> GetAllDataAsync(string expenseType = "", string serviceType = "", string isReceiptable = "", string status = "", string searchTerm = "")
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@ExpenseTypeId", string.IsNullOrWhiteSpace(expenseType) ? null : expenseType);
                p.Add("@ServiceTypeId", string.IsNullOrWhiteSpace(serviceType) ? null : serviceType);
                p.Add("@IsReceiptable", string.IsNullOrWhiteSpace(isReceiptable) ? null : isReceiptable);
                p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status);
                p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm);

                var rows = await conn.QueryAsync<ExpenseHeadVM>(
                    "dbo.sp_CF_GetExpenseHeadData",
                    p,
                    commandType: CommandType.StoredProcedure
                );
                return rows.AsList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllDataAsync SP error, falling back to EF: {ex.Message}");
                var query = _repository.All().AsQueryable();

                if (!string.IsNullOrWhiteSpace(expenseType)) query = query.Where(x => x.ExpenseTypeId == expenseType);
                if (!string.IsNullOrWhiteSpace(serviceType)) query = query.Where(x => x.ServiceTypeId == serviceType);
                if (!string.IsNullOrWhiteSpace(isReceiptable)) query = query.Where(x => x.IsReceiptable == isReceiptable);
                if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => (x.EntryType ?? "Active") == status);

                var list = await query.ToListAsync();
                var modes = await _expenseTypeRepository.All().ToListAsync();
                var svcs = await _serviceTypeRepository.All().ToListAsync();
                var modeDict = modes.ToDictionary(m => m.ExpenseTypeId, m => m.ExpenseType);
                var svcDict = svcs.ToDictionary(s => s.ServiceTypeId, s => s.ServiceTypeName);

                return list.OrderByDescending(x => int.TryParse(x.ExpenseHeadId, out var idVal) ? idVal : 0).Select(m => new ExpenseHeadVM
                {
                    TC = m.Tc,
                    ExpenseHeadID = m.ExpenseHeadId,
                    ExpenseHead = m.ExpenseHead,
                    ShortName = m.ShortName ?? "",
                    SerialNo = m.SerialNo,
                    IsReceiptable = m.IsReceiptable,
                    Amount = m.Amount,
                    ServiceTypeID = m.ServiceTypeId,
                    ServiceTypeName = (m.ServiceTypeId != null && svcDict.ContainsKey(m.ServiceTypeId)) ? svcDict[m.ServiceTypeId] : "",
                    ExpenseTypeID = m.ExpenseTypeId,
                    ExpenseType = (m.ExpenseTypeId != null && modeDict.ContainsKey(m.ExpenseTypeId)) ? modeDict[m.ExpenseTypeId] : "",
                    EntryType = m.EntryType ?? "Active",
                    Ldate = m.Ldate,
                    ModifyDate = m.ModifyDate
                }).ToList();
            }
        }

        public async Task<ExpenseHeadVM?> SaveOrUpdateSpAsync(ExpenseHeadVM model, string? user = null, string? ip = null, string? mac = null)
        {
            using var conn = new SqlConnection(_connectionString);
            var p = new DynamicParameters();
            p.Add("@TC", model.TC ?? 0);
            p.Add("@ExpenseHeadID", model.ExpenseHeadID, direction: ParameterDirection.InputOutput, size: 50);
            p.Add("@ExpenseHead", model.ExpenseHead);
            p.Add("@ShortName", model.ShortName);
            p.Add("@SerialNo", model.SerialNo);
            p.Add("@IsReceiptable", model.Rec == 1 || model.IsReceiptable == "Y" || model.IsReceiptable == "Yes" ? "Y" : "N");
            p.Add("@Amount", model.Amount);
            p.Add("@ServiceTypeID", model.ServiceTypeID);
            p.Add("@ExpenseTypeID", model.ExpenseTypeID);
            p.Add("@Status", string.IsNullOrWhiteSpace(model.Status) ? "Active" : model.Status);
            p.Add("@User", user ?? model.Luser);
            p.Add("@IP", ip ?? model.Lip ?? GetLocalIP());
            p.Add("@MAC", mac ?? model.Lmac ?? GetMacAddress());

            var saved = await conn.QueryFirstOrDefaultAsync<ExpenseHeadVM>(
                "dbo.sp_CF_SaveOrUpdateExpenseHead",
                p,
                commandType: CommandType.StoredProcedure
            );
            return saved;
        }

        public async Task<ExpenseHeadDropdownsVM> GetDropdownsSpAsync()
        {
            var vm = new ExpenseHeadDropdownsVM();
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var multi = await conn.QueryMultipleAsync("dbo.sp_CF_GetExpenseHeadDropdowns", commandType: CommandType.StoredProcedure);
                vm.ShipmentModes = (await multi.ReadAsync<CommonSelectModel>()).AsList();
                vm.ServiceProviders = (await multi.ReadAsync<CommonSelectModel>()).AsList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching dropdowns via SP: {ex.Message}");
                var modes = await _expenseTypeRepository.All().OrderBy(m => m.ExpenseType).ToListAsync();
                var svcs = await _serviceTypeRepository.All().OrderBy(s => s.ServiceTypeName).ToListAsync();
                vm.ShipmentModes = modes.Select(m => new CommonSelectModel { Code = m.ExpenseTypeId, Name = m.ExpenseType }).ToList();
                vm.ServiceProviders = svcs.Select(s => new CommonSelectModel { Code = s.ServiceTypeId, Name = s.ServiceTypeName }).ToList();
            }
            return vm;
        }
        public async Task<ExpenseHeadVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.Tc == id)
                       .Select(pt => new ExpenseHeadVM
                       {
                           TC = pt.Tc,
                           ExpenseHeadID = pt.ExpenseHeadId,
                           ExpenseHead = pt.ExpenseHead,
                           ShortName = pt.ShortName,
                           Amount = pt.Amount,
                           SerialNo = pt.SerialNo,
                           IsReceiptable = pt.IsReceiptable,
                           ServiceTypeID = pt.ServiceTypeId,
                           ExpenseTypeID = pt.ExpenseTypeId,
                           Ldate = pt.Ldate,
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

        public async Task<PaginationResponse<ExpenseHeadVM>> GetPaginatedAsync(PaginationRequest request, string expenseType = "", string isReceiptable = "", string serviceType = "")
        {
            var totalUnfilteredCount = await _repository.All().CountAsync();

            var query = from eh in _repository.All().AsNoTracking()
                        join st in _serviceTypeRepository.All().AsNoTracking()
                            on eh.ServiceTypeId equals st.ServiceTypeId into stGroup
                        from st in stGroup.DefaultIfEmpty()

                        join et in _expenseTypeRepository.All().AsNoTracking()

                            on eh.ExpenseTypeId equals et.ExpenseTypeId into etGroup
                        from et in etGroup.DefaultIfEmpty()

                        select new ExpenseHeadVM
                        {
                            TC = eh.Tc,
                            ExpenseHeadID = eh.ExpenseHeadId,
                            ExpenseHead = eh.ExpenseHead,
                            ShortName = eh.ShortName,
                            Amount = eh.Amount,
                            SerialNo = eh.SerialNo,
                            IsReceiptable = (eh.IsReceiptable == "Y" || eh.IsReceiptable == "Yes" || eh.IsReceiptable == "YES") ? "Yes" : "No",
                            ExpenseTypeID = eh.ExpenseTypeId,
                            Ldate = eh.Ldate,
                            ModifyDate = eh.ModifyDate,
                            ServiceTypeID = eh.ServiceTypeId,
                            ServiceTypeName = st != null ? st.ServiceTypeName : "",
                            ExpenseType = et != null ? et.ExpenseType : (eh.ExpenseTypeId ?? "")
                        };

            if (!string.IsNullOrEmpty(expenseType))
                query = query.Where(x => x.ExpenseTypeID == expenseType);

            if (!string.IsNullOrEmpty(isReceiptable))
            {
                var target = (isReceiptable.Equals("Y", StringComparison.OrdinalIgnoreCase) || isReceiptable.Equals("Yes", StringComparison.OrdinalIgnoreCase)) ? "Yes" : "No";
                query = query.Where(x => x.IsReceiptable == target);
            }

            if (!string.IsNullOrEmpty(serviceType))
                query = query.Where(x => x.ServiceTypeID == serviceType);

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn) || request.SortColumn.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(x => x.ExpenseType)
                             .ThenBy(x => x.ServiceTypeName)
                             .ThenByDescending(x => x.IsReceiptable)
                             .ThenBy(x => (x.SerialNo == null ? "" : x.SerialNo).Length)
                             .ThenBy(x => x.SerialNo);
                request.SortColumn = "";
            }
            else if (request.SortColumn.Equals("SerialNo", StringComparison.OrdinalIgnoreCase))
            {
                if (request.SortOrder?.ToLower() == "desc")
                {
                    query = query.OrderByDescending(x => (x.SerialNo == null ? "" : x.SerialNo).Length)
                                 .ThenByDescending(x => x.SerialNo);
                }
                else
                {
                    query = query.OrderBy(x => (x.SerialNo == null ? "" : x.SerialNo).Length)
                                 .ThenBy(x => x.SerialNo);
                }
                request.SortColumn = "";
            }

            var result = await PaginationService<ExpenseHeadVM>.GetPaginatedData(query, request);
            result.TotalCount = totalUnfilteredCount;

            if (result.Data != null)
            {
                foreach (var item in result.Data)
                {
                    item.IsReceiptable = (item.IsReceiptable == "Y" || item.IsReceiptable == "Yes" || item.IsReceiptable == "YES") ? "Yes" : "No";
                }
            }

            return result;
        }

        public async Task<bool> IsDuplicateAsync(ExpenseHeadVM model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return await _repository.All()
                .AnyAsync(eh =>
                    eh.Tc != model.TC &&
                    eh.ExpenseHead.Trim().ToLower() == model.ExpenseHead.Trim().ToLower() &&
                    eh.ExpenseTypeId == model.ExpenseTypeID &&
                    eh.ServiceTypeId == model.ServiceTypeID &&
                    eh.IsReceiptable == model.IsReceiptable
                );
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.Tc == id);
        }

        public async Task<bool> SaveAsync(ExpenseHeadVM model)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var lastId = await GetLastExpenseHeadAsync();
                int.TryParse(model.SerialNo, out int targetSerial);
                if (targetSerial <= 0)
                {
                    targetSerial = await GetNextSerialNoAsync(model.ExpenseTypeID, model.ServiceTypeID, model.IsReceiptable);
                }
                else
                {
                    // Shift existing records with serial >= targetSerial up by 1
                    await ShiftSerialsAsync(model.ExpenseTypeID, model.ServiceTypeID, model.IsReceiptable, targetSerial, excludeTc: null);
                }

                string newId;
                if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId, out int lastNumericId))
                {
                    int nextId = lastNumericId + 1;
                    newId = nextId.ToString("D4");
                }
                else
                {
                    newId = "001";
                }

                var entity = new CfDefExpenseHead
                {
                    ExpenseHeadId = newId,
                    ExpenseHead = model.ExpenseHead,
                    ShortName = model.ShortName ?? "",
                    Amount = model.Amount,
                    SerialNo = targetSerial.ToString(),
                    IsReceiptable = model.IsReceiptable,
                    ServiceTypeId = model.ServiceTypeID,
                    ExpenseTypeId = model.ExpenseTypeID,
                    Luser = model.Luser,
                    Ldate = model.Ldate ?? DateTime.Now,
                    Lip = !string.IsNullOrEmpty(model.Lip) ? model.Lip : (GetLocalIP() ?? ""),
                    Lmac = !string.IsNullOrEmpty(model.Lmac) ? model.Lmac : (GetMacAddress() ?? ""),
                    EntryType = model.EntryType ?? "",
                };

                await _repository.AddAsync(entity);
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
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

                int.TryParse(model.SerialNo, out int targetSerial);
                if (targetSerial <= 0)
                {
                    targetSerial = await GetNextSerialNoAsync(model.ExpenseTypeID, model.ServiceTypeID, model.IsReceiptable);
                }
                else
                {
                    // Shift existing other records with serial >= targetSerial up by 1
                    await ShiftSerialsAsync(model.ExpenseTypeID, model.ServiceTypeID, model.IsReceiptable, targetSerial, excludeTc: model.TC);
                }

                entity.ExpenseHeadId = model.ExpenseHeadID;
                entity.ExpenseHead = model.ExpenseHead;
                entity.ShortName = model.ShortName ?? "";
                entity.Amount = model.Amount;
                entity.SerialNo = targetSerial.ToString();
                entity.IsReceiptable = model.IsReceiptable;
                entity.ServiceTypeId = model.ServiceTypeID;
                entity.ExpenseTypeId = model.ExpenseTypeID;
                entity.ModifyDate = model.ModifyDate ?? DateTime.Now;
                entity.Lip = !string.IsNullOrEmpty(model.Lip) ? model.Lip : (GetLocalIP() ?? "");
                entity.Lmac = !string.IsNullOrEmpty(model.Lmac) ? model.Lmac : (GetMacAddress() ?? "");
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
            if (isReceiptable != "Y" && isReceiptable != "N")
                throw new ArgumentException("IsReceiptable must be 'Y' or 'N'.");

            var allRecords = await _repository.GetAllAsync();

            var filtered = allRecords.Where(e =>
                e.ExpenseTypeId == expenseType &&
                (string.IsNullOrEmpty(serviceType) || e.ServiceTypeId == serviceType) &&
                e.IsReceiptable?.Trim().ToUpper() == isReceiptable
            ).ToList();

            if (requestedSerial.HasValue && requestedSerial.Value > 0)
            {
                return requestedSerial.Value;
            }

            if (filtered.Any())
            {
                return filtered.Max(e => int.TryParse(e.SerialNo, out int s) ? s : 0) + 1;
            }

            return 1;
        }

        private async Task ShiftSerialsAsync(string expenseType, string serviceType, string isReceiptable, int targetSerial, int? excludeTc = null)
        {
            if (targetSerial <= 0) return;

            var allRecords = await _repository.GetAllAsync();

            var toShift = allRecords.Where(e =>
                e.ExpenseTypeId == expenseType &&
                (string.IsNullOrEmpty(serviceType) || e.ServiceTypeId == serviceType) &&
                e.IsReceiptable?.Trim().ToUpper() == isReceiptable?.Trim().ToUpper() &&
                (!excludeTc.HasValue || e.Tc != excludeTc.Value) &&
                int.TryParse(e.SerialNo, out int s) && s >= targetSerial
            ).OrderByDescending(e => int.TryParse(e.SerialNo, out int s) ? s : 0).ToList();

            foreach (var record in toShift)
            {
                int currentSerial = int.TryParse(record.SerialNo, out int cs) ? cs : 0;
                record.SerialNo = (currentSerial + 1).ToString();
                await _repository.UpdateAsync(record);
            }
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
        private byte[]? _logoBytes;
        private byte[]? GetLogoBytes()
        {
            if (_logoBytes != null && _logoBytes.Length > 0) return _logoBytes;
            try
            {
                var candidatePaths = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "UIs", "GCTL.UI.Core", "wwwroot", "images", "fermosa-wX200.png"),
                    @"D:\Project\School\GCTL.Framework\UIs\GCTL.UI.Core\wwwroot\images\fermosa-wX200.png"
                };

                foreach (var p in candidatePaths)
                {
                    if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                    {
                        _logoBytes = File.ReadAllBytes(p);
                        break;
                    }
                }
            }
            catch { }
            return _logoBytes;
        }

        public async Task<List<ExpenseHeadReportVM>> GetExpenseHeadReportAsync(
            string expenseTypeId = "",
            string serviceType = "",
            string isReceiptable = "",
            string status = "",
            string search = "")
        {
            var result = new List<ExpenseHeadReportVM>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand("GetExpenseHeadReport", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@ExpenseTypeId", string.IsNullOrEmpty(expenseTypeId) ? (object)DBNull.Value : expenseTypeId));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            decimal? amt = null;
                            if (reader["Amount"] != DBNull.Value && decimal.TryParse(reader["Amount"].ToString(), out decimal parsedAmt))
                            {
                                amt = parsedAmt;
                            }

                            string recVal = reader["IsReceiptable"]?.ToString() ?? "";
                            recVal = (recVal == "Y" || recVal == "Yes" || recVal == "YES" || recVal == "1") ? "Yes" : "No";

                            result.Add(new ExpenseHeadReportVM
                            {
                                Company = reader["CompanyName"]?.ToString() ?? "Formosa Logistics Limited",
                                Address = reader["Address1"]?.ToString() ?? "21, Hazaribagh, Dhaka-1209",
                                ExpenseHeadID = reader["ExpenseHeadID"]?.ToString() ?? "",
                                ExpenseHead = reader["ExpenseHead"]?.ToString() ?? "",
                                ShortName = reader["ShortName"]?.ToString() ?? "",
                                SerialNo = reader["SerialNo"]?.ToString() ?? "",
                                IsReceiptable = recVal,
                                ServiceProvidedBy = reader["ServiceProvidedBy"]?.ToString() ?? "",
                                ExpenseType = reader["ExpenseType"]?.ToString() ?? "",
                                Amount = amt,
                                Status = reader["Status"]?.ToString() ?? "Active"
                            });
                        }
                    }
                }
            }

            // In-memory filters
            if (!string.IsNullOrWhiteSpace(serviceType))
            {
                result = result.Where(x => string.Equals(x.ServiceProvidedBy?.Trim(), serviceType.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(isReceiptable))
            {
                string normRec = (isReceiptable == "1" || isReceiptable.Equals("Yes", StringComparison.OrdinalIgnoreCase)) ? "Yes" : "No";
                result = result.Where(x => string.Equals(x.IsReceiptable, normRec, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                result = result.Where(x => string.Equals(x.Status?.Trim(), status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                result = result.Where(x =>
                    (x.ExpenseHeadID ?? "").ToLower().Contains(q) ||
                    (x.ExpenseHead ?? "").ToLower().Contains(q) ||
                    (x.ShortName ?? "").ToLower().Contains(q) ||
                    (x.ExpenseType ?? "").ToLower().Contains(q) ||
                    (x.ServiceProvidedBy ?? "").ToLower().Contains(q)
                ).ToList();
            }

            return result
                .OrderBy(x => x.ExpenseType)
                .ThenBy(x => x.ServiceProvidedBy)
                .ThenByDescending(x => x.IsReceiptable)
                .ThenBy(x => int.TryParse(x.SerialNo, out int s) ? s : int.MaxValue)
                .ThenBy(x => x.ExpenseHeadID)
                .ToList();
        }
        #endregion

        #region Generate Excel Report
        public async Task<byte[]> GenerateExcelReportAsync(
            string expenseTypeId = "",
            string serviceType = "",
            string isReceiptable = "",
            string status = "",
            string search = "")
        {
            var data = await GetExpenseHeadReportAsync(expenseTypeId, serviceType, isReceiptable, status, search);
            using var ms = new MemoryStream();
            using (var spreadsheet = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                // --- Styles ---
                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = new Stylesheet();

                // Fonts: 0-default, 1-bold, 2-title bold+large, 3-subtitle, 4-header white bold
                var fonts = new DocumentFormat.OpenXml.Spreadsheet.Fonts(
                    new Font(),
                    new Font(new Bold()),
                    new Font(new Bold(), new FontSize() { Val = 16 }),
                    new Font(new FontSize() { Val = 11 }),
                    new Font(new FontSize() { Val = 10 })
                );
                stylesPart.Stylesheet.Fonts = fonts;

                // Fills: 0-none, 1-gray125, 2-header blue, 3-light blue
                var fills = new Fills(
                    new Fill(new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(new PatternFill() { PatternType = PatternValues.Gray125 })
                );
                stylesPart.Stylesheet.Fills = fills;

                stylesPart.Stylesheet.Borders = new Borders(
                    new Border(), // 0: none
                    new Border( // 1: thin box
                        new LeftBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new RightBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new TopBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new BottomBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()
                    )
                );

                // CellFormats
                var cellFormats = new CellFormats(
                    new CellFormat(), // 0: default
                    new CellFormat() { FontId = 1, ApplyFont = true }, // 1: bold
                    new CellFormat() // 2: title (center horizontal & vertical)
                    {
                        FontId = 2,
                        ApplyFont = true,
                        Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    },
                    new CellFormat() // 3: center only (horizontal & vertical)
                    {
                        BorderId = 1,
                        ApplyBorder = true,
                        Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    },
                    new CellFormat() // 4: wrap+left (horizontal & vertical)
                    {
                        BorderId = 1,
                        ApplyBorder = true,
                        Alignment = new Alignment() { WrapText = true, Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center }
                    },
                    new CellFormat() // 5: wrap+left padding
                    {
                        BorderId = 1,
                        ApplyBorder = true,
                        Alignment = new Alignment() { WrapText = true, Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center, Indent = 1 }
                    },
                    new CellFormat() // 6: subtitle centered
                    {
                        FontId = 3,
                        ApplyFont = true,
                        Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    },
                    new CellFormat() // 7: table header (no background color, bold text, centered, thin border)
                    {
                        FontId = 4,
                        ApplyFont = true,
                        FillId = 0,
                        ApplyFill = false,
                        BorderId = 1,
                        ApplyBorder = true,
                        Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    },
                    new CellFormat() // 8: table header (no background color, bold text, left, thin border)
                    {
                        FontId = 4,
                        ApplyFont = true,
                        FillId = 0,
                        ApplyFill = false,
                        BorderId = 1,
                        ApplyBorder = true,
                        Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center, Indent = 1 }
                    },
                    new CellFormat() // 9: number right-aligned
                    {
                        BorderId = 1,
                        ApplyBorder = true,
                        Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center }
                    }
                );

                stylesPart.Stylesheet.CellFormats = cellFormats;
                stylesPart.Stylesheet.Save();

                uint titleStyleIndex = 2;
                uint centerStyleIndex = 3;
                uint wrapLeftPaddingStyleIndex = 5;
                uint subtitleStyleIndex = 6;
                uint tableHeaderCenterStyleIndex = 7;
                uint tableHeaderLeftStyleIndex = 8;
                uint rightStyleIndex = 9;

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "ExpenseHead Report" });

                int totalColumns = 9;

                string company = data.FirstOrDefault()?.Company ?? "Formosa Logistics Limited";
                string address = data.FirstOrDefault()?.Address ?? "21, Hazaribagh, Dhaka-1209";
                string reportTitle = "Expense Head Report";

                var filterParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(expenseTypeId)) filterParts.Add($"Shipment Mode: {expenseTypeId}");
                if (!string.IsNullOrWhiteSpace(serviceType)) filterParts.Add($"Service By: {serviceType}");
                if (!string.IsNullOrWhiteSpace(isReceiptable)) filterParts.Add($"Receiptable: {isReceiptable}");
                if (!string.IsNullOrWhiteSpace(status)) filterParts.Add($"Status: {status}");
                if (!string.IsNullOrWhiteSpace(search)) filterParts.Add($"Search: {search}");
                string filterSummary = filterParts.Count > 0 ? string.Join(" | ", filterParts) + $" • {data.Count} record(s)" : $"All records • {data.Count} record(s)";

                // Helper for merged title rows (horizontally and vertically centered!)
                Row CreateMergedRow(string text, uint styleIndex, double height = 24)
                {
                    var row = new Row() { Height = height, CustomHeight = true };
                    var cell = new Cell()
                    {
                        CellValue = new CellValue(text),
                        DataType = CellValues.String,
                        StyleIndex = styleIndex
                    };
                    row.Append(cell);
                    for (int i = 1; i < totalColumns; i++)
                    {
                        row.Append(new Cell() { StyleIndex = styleIndex });
                    }
                    return row;
                }

                // --- Title rows (All horizontally & vertically centered) ---
                sheetData.Append(CreateMergedRow(company, titleStyleIndex, 32));
                sheetData.Append(CreateMergedRow(address, subtitleStyleIndex, 20));
                sheetData.Append(CreateMergedRow(reportTitle, titleStyleIndex, 26));
                sheetData.Append(CreateMergedRow(filterSummary, subtitleStyleIndex, 20));

                // Empty row
                sheetData.Append(new Row());

                // --- Header Row (9 Columns) ---
                var headerRow = new Row() { Height = 25, CustomHeight = true };
                headerRow.Append(
                    new Cell() { CellValue = new CellValue("Expense Head ID"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Expense Head"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Short Name"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Serial No"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Is Receiptable?"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Service Provided By"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Shipment Mode"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Amount"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex },
                    new Cell() { CellValue = new CellValue("Status"), DataType = CellValues.String, StyleIndex = tableHeaderCenterStyleIndex }
                );
                sheetData.Append(headerRow);

                // --- Data Rows ---
                foreach (var d in data)
                {
                    var row = new Row() { Height = 20, CustomHeight = true };
                    row.Append(
                        new Cell() { CellValue = new CellValue(d.ExpenseHeadID ?? ""), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ExpenseHead ?? ""), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ShortName ?? ""), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                        new Cell() { CellValue = new CellValue(d.SerialNo ?? ""), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                        new Cell() { CellValue = new CellValue(d.IsReceiptable ?? "No"), DataType = CellValues.String, StyleIndex = centerStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ServiceProvidedBy ?? ""), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                        new Cell() { CellValue = new CellValue(d.ExpenseType ?? ""), DataType = CellValues.String, StyleIndex = wrapLeftPaddingStyleIndex },
                        new Cell() { CellValue = new CellValue(d.Amount.HasValue ? d.Amount.Value.ToString("N2") : ""), DataType = CellValues.String, StyleIndex = rightStyleIndex },
                        new Cell() { CellValue = new CellValue(d.Status ?? "Active"), DataType = CellValues.String, StyleIndex = centerStyleIndex }
                    );
                    sheetData.Append(row);
                }

                // Merge title rows A1:I1, A2:I2, A3:I3, A4:I4
                var mergeCells = new MergeCells();
                mergeCells.Append(new MergeCell() { Reference = new StringValue("A1:I1") });
                mergeCells.Append(new MergeCell() { Reference = new StringValue("A2:I2") });
                mergeCells.Append(new MergeCell() { Reference = new StringValue("A3:I3") });
                mergeCells.Append(new MergeCell() { Reference = new StringValue("A4:I4") });
                worksheetPart.Worksheet.InsertAfter(mergeCells, sheetData);

                // Column widths for 9 columns
                var columns = new Columns(
                    new SpreadsheetColumn() { Min = 1, Max = 1, Width = 18, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 2, Max = 2, Width = 38, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 3, Max = 3, Width = 18, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 4, Max = 4, Width = 12, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 5, Max = 5, Width = 16, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 6, Max = 6, Width = 24, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 7, Max = 7, Width = 24, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 8, Max = 8, Width = 16, CustomWidth = true },
                    new SpreadsheetColumn() { Min = 9, Max = 9, Width = 14, CustomWidth = true }
                );
                worksheetPart.Worksheet.InsertAt(columns, 0);

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            return ms.ToArray();
        }
        #endregion

        #region Generate PDF Report
        public async Task<byte[]> GeneratePdfReportAsync(
            string expenseTypeId = "",
            string serviceType = "",
            string isReceiptable = "",
            string status = "",
            string search = "")
        {
            var data = await GetExpenseHeadReportAsync(expenseTypeId, serviceType, isReceiptable, status, search);

            string company = data.FirstOrDefault()?.Company ?? "Formosa Logistics Limited";
            string address = data.FirstOrDefault()?.Address ?? "21, Hazaribagh, Dhaka-1209";
            string reportTitle = "Expense Head Report";

            var filterParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(expenseTypeId)) filterParts.Add($"Shipment Mode: {expenseTypeId}");
            if (!string.IsNullOrWhiteSpace(serviceType)) filterParts.Add($"Service By: {serviceType}");
            if (!string.IsNullOrWhiteSpace(isReceiptable)) filterParts.Add($"Receiptable: {isReceiptable}");
            if (!string.IsNullOrWhiteSpace(status)) filterParts.Add($"Status: {status}");
            if (!string.IsNullOrWhiteSpace(search)) filterParts.Add($"Search: {search}");
            string filterSummary = filterParts.Count > 0 ? string.Join(" | ", filterParts) + $" • {data.Count} record(s)" : $"All records • {data.Count} record(s)";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.MarginHorizontal(16);
                    page.MarginVertical(14);
                    page.Size(PageSizes.A4.Landscape());

                    // Header
                    page.Header().Column(headerCol =>
                    {
                        var logo = GetLogoBytes();
                        headerCol.Item().Row(r =>
                        {
                            if (logo != null && logo.Length > 0)
                            {
                                r.ConstantItem(85).AlignMiddle().Height(30).Image(logo).FitArea();
                            }
                            else
                            {
                                r.ConstantItem(85);
                            }

                            r.RelativeItem().Column(col =>
                            {
                                col.Item().AlignCenter().Text(company).FontSize(16).Bold().FontColor("#000000");
                                col.Item().AlignCenter().Text(address).FontSize(9).FontColor("#555555");
                                col.Item().AlignCenter().Text(reportTitle).FontSize(12).Bold().FontColor("#000000");
                                col.Item().AlignCenter().Text(filterSummary).FontSize(8.5f).FontColor("#666666");
                            });

                            r.ConstantItem(85);
                        });

                        // removed header line
                    });

                    // Content Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);   // Expense Head ID
                            columns.RelativeColumn(3.5f); // Expense Head
                            columns.RelativeColumn(1.8f); // Short Name
                            columns.ConstantColumn(45);   // Serial No
                            columns.ConstantColumn(65);   // Is Receiptable?
                            columns.RelativeColumn(2.0f); // Service Provided By
                            columns.RelativeColumn(2.0f); // Shipment Mode
                            columns.ConstantColumn(65);   // Amount
                            columns.ConstantColumn(50);   // Status
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Expense Head ID").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Expense Head").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Short Name").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Serial No").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Is Receiptable?").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Service Provided By").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Shipment Mode").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Amount").FontSize(8.5f).FontColor("#000000");
                            header.Cell().Border(0.5f).BorderColor("#cccccc").Padding(4).AlignCenter().AlignMiddle().Text("Status").FontSize(8.5f).FontColor("#000000");
                        });

                        foreach (var d in data)
                        {
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignCenter().AlignMiddle().Text(d.ExpenseHeadID ?? "").FontSize(8);
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignLeft().AlignMiddle().Text(d.ExpenseHead ?? "").FontSize(8).WrapAnywhere();
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignLeft().AlignMiddle().Text(d.ShortName ?? "").FontSize(8).WrapAnywhere();
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignCenter().AlignMiddle().Text(d.SerialNo ?? "").FontSize(8);
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignCenter().AlignMiddle().Text(d.IsReceiptable ?? "No").FontSize(8);
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignLeft().AlignMiddle().Text(d.ServiceProvidedBy ?? "").FontSize(8).WrapAnywhere();
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignLeft().AlignMiddle().Text(d.ExpenseType ?? "").FontSize(8).WrapAnywhere();
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignRight().AlignMiddle().Text(d.Amount.HasValue ? d.Amount.Value.ToString("N2") : "").FontSize(8);
                            table.Cell().Border(0.5f).BorderColor("#cccccc").Padding(3).AlignCenter().AlignMiddle().Text(d.Status ?? "Active").FontSize(8);
                        }
                    });

                    // Footer
                    page.Footer().PaddingTop(4).Column(footerCol =>
                    {
                        // removed footer top line
                        footerCol.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text($"Printed: {DateTime.Now:dd/MM/yyyy hh:mm tt}")
                                .FontSize(7.5f).FontColor("#555555");

                            row.RelativeItem().AlignCenter().Text("Formosa Logistics Limited - C&F Management System")
                                .FontSize(7.5f).FontColor("#555555");

                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Page ").FontSize(7.5f).FontColor("#555555");
                                txt.CurrentPageNumber().FontSize(7.5f).FontColor("#555555");
                                txt.Span(" of ").FontSize(7.5f).FontColor("#555555");
                                txt.TotalPages().FontSize(7.5f).FontColor("#555555");
                            });
                        });
                    });
                });
            }).GeneratePdf();

            return pdfBytes;
        }
        #endregion
    }
}
