using GCTL.Core.Data;
using GCTL.Core.ViewModels.CurrencyType;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GCTL.Service.CurrencyType
{
    public class CurrencyTypeService : AppService<CaDefCurrency>, ICurrencyType
    {
        private readonly IRepository<CaDefCurrency> _repository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        private const string TableName = "CA_Def_Currency";
        private const string ColumnName = "CurrencyId";

        public CurrencyTypeService(
            IRepository<CaDefCurrency> repository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
        ) : base(repository)
        {
            _repository = repository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #region Setup Methods for DynamicGrid

        public async Task<List<CurrencyTypeSetupViewModel>> GetAllSetupAsync()
        {
            var entity = await _repository.All().ToListAsync();
            return entity.Select(model => new CurrencyTypeSetupViewModel
            {
                AutoId = model.Tc,
                Tc = (int)model.Tc,
                CurrencyId = model.CurrencyId,
                CurrencyName = model.CurrencyName,
                ShortName = model.ShortName,
                Symbol = model.Symbol,
                DecimalPlaces = model.DecimalPlaces,
                NegativeFormat = model.NegativeFormat,
                Luser = model.Luser,
                Ldate = model.Ldate,
                Lip = model.Lip,
                Lmac = model.Lmac,
                ModifyDate = model.ModifyDate
            }).ToList();
        }

        public async Task<CurrencyTypeSetupViewModel> GetSetupByIdAsync(string id)
        {
            var model = await _repository.All().FirstOrDefaultAsync(x => x.CurrencyId == id);
            if (model == null && decimal.TryParse(id, out decimal tcId))
            {
                model = await _repository.All().FirstOrDefaultAsync(x => x.Tc == tcId);
            }

            if (model == null) return null;

            return new CurrencyTypeSetupViewModel
            {
                AutoId = model.Tc,
                Tc = (int)model.Tc,
                CurrencyId = model.CurrencyId,
                CurrencyName = model.CurrencyName,
                ShortName = model.ShortName,
                Symbol = model.Symbol,
                DecimalPlaces = model.DecimalPlaces,
                NegativeFormat = model.NegativeFormat,
                Luser = model.Luser,
                Ldate = model.Ldate,
                Lip = model.Lip,
                Lmac = model.Lmac,
                ModifyDate = model.ModifyDate
            };
        }

        public async Task<bool> SaveSetupAsync(CurrencyTypeSetupViewModel modelVM)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = new CaDefCurrency
                {
                    CurrencyId = await GenerateNextCode(),
                    CurrencyName = modelVM.CurrencyName,
                    ShortName = modelVM.ShortName ?? string.Empty,
                    Symbol = modelVM.Symbol ?? string.Empty,
                    DecimalPlaces = modelVM.DecimalPlaces,
                    NegativeFormat = modelVM.NegativeFormat ?? string.Empty,
                    Luser = modelVM.Luser,
                    Ldate = DateTime.Now,
                    Lip = modelVM.Lip ?? GetLocalIP(),
                    Lmac = modelVM.Lmac ?? GetMacAddress()
                };

                await _repository.AddAsync(entity);
                await _repository.CommitTransactionAsync();
                modelVM.CurrencyId = entity.CurrencyId;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                await _repository.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> UpdateSetupAsync(CurrencyTypeSetupViewModel modelVM)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.All().FirstOrDefaultAsync(x => x.CurrencyId == modelVM.CurrencyId || x.Tc == modelVM.AutoId || x.Tc == modelVM.Tc);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.CurrencyId = modelVM.CurrencyId;
                entity.CurrencyName = modelVM.CurrencyName;
                entity.ShortName = modelVM.ShortName ?? string.Empty;
                entity.Symbol = modelVM.Symbol ?? string.Empty;
                entity.DecimalPlaces = modelVM.DecimalPlaces;
                entity.NegativeFormat = modelVM.NegativeFormat ?? string.Empty;
                entity.Luser = modelVM.Luser;
                entity.ModifyDate = DateTime.Now;
                entity.Lip = modelVM.Lip ?? GetLocalIP();
                entity.Lmac = modelVM.Lmac ?? GetMacAddress();

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

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (ids == null || !ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "CurrencyId", "CurrencyName", "Currency" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                _repository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.All().Where(x => ids.Contains(x.CurrencyId) || ids.Contains(x.Tc.ToString())).ToListAsync();

                _repository.Delete(entity);
                model.tableName = _repository.GetTableName();
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await _repository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        public async Task<string> GenerateNextCode()
        {
            var code = await _repository.GetAllAsync();
            var lastCode = code.Max(x => x.CurrencyId);
            int nextCode = 1;
            if (!string.IsNullOrEmpty(lastCode))
            {
                int lastNumber = int.Parse(lastCode.TrimStart('0'));
                lastNumber++;
                nextCode = lastNumber;
            }
            return nextCode.ToString("D3");
        }

        public async Task<bool> IsExistByNameAsync(string name, string code)
        {
            return await _repository.All().AnyAsync(x => x.CurrencyName == name && x.CurrencyId != code);
        }

        #endregion

        #region Permissions

        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Currency Type" || x.Title == "CurrencyType" || x.Title == "Currency" || x.Title == "Currency Setup" || x.Title == "Currency Information" || x.Title == "Currency Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Currency Type" || x.Title == "CurrencyType" || x.Title == "Currency" || x.Title == "Currency Setup" || x.Title == "Currency Information" || x.Title == "Currency Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Currency Type" || x.Title == "CurrencyType" || x.Title == "Currency" || x.Title == "Currency Setup" || x.Title == "Currency Information" || x.Title == "Currency Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode)) return true;
            var permissions = await accessCodeRepository.All()
                .Where(x => x.AccessCodeId == accessCode && (x.Title == "Currency Type" || x.Title == "CurrencyType" || x.Title == "Currency" || x.Title == "Currency Setup" || x.Title == "Currency Information" || x.Title == "Currency Type Setup"))
                .ToListAsync();

            if (!permissions.Any()) return true;
            return permissions.Any(x => x.CheckDelete);
        }

        #endregion

        #region Legacy Methods

        public async Task<bool> BulkDeleteAsync(List<decimal> ids)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.All().Where(c => ids.Contains(c.Tc)).ToListAsync();

                if (entity == null || entity.Count == 0)
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

        public async Task<bool> DeleteAsync(decimal id)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.GetByIdAsync(id);

                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                await _repository.DeleteAsync(entity.Tc);
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

        public async Task<CurrencyTypeVM> GetByIdAsync(decimal id)
        {
            var data = await _repository.All().Where(pt => pt.Tc == id)
            .Select(pt => new CurrencyTypeVM
            {
                TC = pt.Tc,
                CurrencyId = pt.CurrencyId ?? "",
                CurrencyName = pt.CurrencyName ?? "",
                ShortName = pt.ShortName ?? "",
                Symbol = pt.Symbol ?? "",
                DecimalPlaces = pt.DecimalPlaces,
                NegativeFormat = pt.NegativeFormat,
                LUser = pt.Luser,
                LDate = pt.Ldate,
                LIP = pt.Lip,
                LMAC = pt.Lmac,
                ModifyDate = pt.ModifyDate,
            }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> GetLastCurrencyAsync()
        {
            var last = await _repository.All()
            .OrderByDescending(c => c.Tc)
            .FirstOrDefaultAsync();
            return last?.CurrencyId;
        }

        public async Task<PaginationService<CaDefCurrency, CurrencyTypeVM>.PaginationResult<CurrencyTypeVM>> GetPaginatedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string searchTerm = "",
        string sortColumn = "CurrencyId",
        string sortOrder = "desc")
        {
            var query = _repository.All();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(term =>
                    EF.Functions.Like(term.CurrencyId ?? "", $"%{searchTerm}%") ||
                    EF.Functions.Like(term.CurrencyName ?? "", $"{searchTerm}%") ||
                    EF.Functions.Like(term.ShortName ?? "", $"%{searchTerm}%") ||
                    EF.Functions.Like(term.Symbol ?? "", $"%{searchTerm}%")
                );
            }

            if (!string.IsNullOrEmpty(sortColumn))
            {
                var propertyInfo = typeof(CaDefCurrency).GetProperty(sortColumn);
                if (propertyInfo != null)
                {
                    query = sortOrder.ToLower() == "asc"
                        ? query.OrderBy(e => EF.Property<object>(e, sortColumn))
                        : query.OrderByDescending(e => EF.Property<object>(e, sortColumn));
                }
            }

            if (pageSize == -1)
            {
                var allData = query
                    .AsEnumerable()
                    .Select(pt => new CurrencyTypeVM
                    {
                        TC = pt.Tc,
                        CurrencyId = pt.CurrencyId ?? " ",
                        CurrencyName = pt.CurrencyName ?? " ",
                        ShortName = pt.ShortName ?? " ",
                        Symbol = pt.Symbol ?? " ",
                        DecimalPlaces = pt.DecimalPlaces,
                        NegativeFormat = pt.NegativeFormat ?? " ",
                        LUser = pt.Luser ?? " ",
                        LDate = pt.Ldate,
                        ModifyDate = pt.ModifyDate,
                    })
                    .ToList();

                return new PaginationService<CaDefCurrency, CurrencyTypeVM>.PaginationResult<CurrencyTypeVM>
                {
                    Data = allData,
                    TotalCount = allData.Count,
                    PaginationInfo = new PaginationService<CaDefCurrency, CurrencyTypeVM>.PaginationInfo
                    {
                        StartItem = allData.Count == 0 ? 0 : 1,
                        EndItem = allData.Count,
                        TotalItems = allData.Count,
                        PageNumbers = new List<int> { 1 },
                        TotalPages = 1,
                        CurrentPage = 1
                    }
                };
            }
            else
            {
                return await PaginationService<CaDefCurrency, CurrencyTypeVM>.GetPaginatedData(
                    query,
                    pageNumber,
                    pageSize,
                    searchTerm,
                    sortColumn,
                    sortOrder,
                    term => sc => EF.Functions.Like(sc.CurrencyId ?? "", $"%{term}%") ||
                                  EF.Functions.Like(sc.CurrencyName ?? "", $"{term}%") ||
                                  EF.Functions.Like(sc.ShortName ?? "", $"%{term}%") ||
                                  EF.Functions.Like(sc.Symbol ?? "", $"%{term}%"),
                    pt => new CurrencyTypeVM
                    {
                        TC = pt.Tc,
                        CurrencyId = pt.CurrencyId ?? " ",
                        CurrencyName = pt.CurrencyName ?? " ",
                        ShortName = pt.ShortName ?? " ",
                        Symbol = pt.Symbol ?? " ",
                        DecimalPlaces = pt.DecimalPlaces,
                        NegativeFormat = pt.NegativeFormat ?? " ",
                        LDate = pt.Ldate,
                        ModifyDate = pt.ModifyDate,
                    });
            }
        }

        public async Task<bool> IsDuplicateAsync(CurrencyTypeVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var paymentMode = await _repository.All().ToListAsync();

            var isDuplicate = paymentMode.Any(pm =>
                pm.CurrencyName.ToLower() == model.CurrencyName.ToLower()
                && pm.Tc != model.TC
            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(decimal id)
        {
            return await _repository.All().AnyAsync(x => x.Tc == id);
        }

        public async Task<bool> SaveAsync(CurrencyTypeVM model)
        {
            try
            {
                var entity = new CaDefCurrency
                {
                    CurrencyId = model.CurrencyId,
                    CurrencyName = model.CurrencyName,
                    ShortName = model.ShortName,
                    Symbol = model.Symbol,
                    DecimalPlaces = model.DecimalPlaces,
                    NegativeFormat = model.NegativeFormat,
                    Luser = "",
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP(),
                    Lmac = GetMacAddress()
                };

                await _repository.AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(CurrencyTypeVM model)
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

                entity.CurrencyId = model.CurrencyId;
                entity.CurrencyName = model.CurrencyName;
                entity.ShortName = model.ShortName;
                entity.Symbol = model.Symbol;
                entity.DecimalPlaces = model.DecimalPlaces;
                entity.NegativeFormat = model.NegativeFormat;
                entity.Luser = "";
                entity.ModifyDate = DateTime.Now;
                entity.Lip = GetLocalIP();
                entity.Lmac = GetMacAddress();

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

        public async Task<List<CurrencyTypeVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new CurrencyTypeVM
            {
                CurrencyId = model.CurrencyId,
                CurrencyName = model.CurrencyName,
                ShortName = model.ShortName,
                Symbol = model.Symbol,
                DecimalPlaces = model.DecimalPlaces,
                NegativeFormat = model.NegativeFormat,
            }).ToList();

            return result;
        }

        public async Task<List<CurrencyTypeVM>> GetAllCurrencyAsync()
        {
            var data = await _repository.All().ToListAsync();
            return data.Select(x => new CurrencyTypeVM
            {
                CurrencyId = x.CurrencyId,
                CurrencyName = x.CurrencyName
            }).ToList();
        }

        #region IP & Mac Address
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

        #endregion
    }
}
