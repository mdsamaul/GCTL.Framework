using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.ShipmentMode
{
    public class ShipmentModeService : AppService<CfDefExpenseType>, IShipmentMode
    {
        private readonly IRepository<CfDefExpenseType> _repository;
        public ShipmentModeService(IRepository<CfDefExpenseType> genericRepository) : base(genericRepository)
        {
            _repository = genericRepository;
        }
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

        public async Task<bool> DeleteAsync(int id)
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
                return (false);
            }
        }

        public async Task<ShipmentModeVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.Tc == id)
                        .Select(pt => new ShipmentModeVM
                        {
                            ExpenseTypeID = pt.ExpenseTypeId,
                            ExpenseType = pt.ExpenseType ?? "",
                            ShortName = pt.ShortName ?? "",
                            LUser = pt.Luser,                         
                            LIP = pt.Lip,
                            LMAC = pt.Lmac ?? "",
                            LDate = pt.Ldate,
                            ModifyDate = pt.ModifyDate,
                            ExpenseLedgerCodeNo = pt.ExpenseLedgerCodeNo,
                            RevenueLedgerCodeNo = pt.RevenueLedgerCodeNo,
                            AdvanceLedgerCode = pt.AdvanceLedgerCode
                        }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> GetLastUnitAsync()
        {

            var last = await _repository.All()
                        .OrderByDescending(c => c.ExpenseTypeId)
                        .FirstOrDefaultAsync();

            return last?.ExpenseTypeId;
        }

        public async Task<PaginationService<CfDefExpenseType, ShipmentModeVM>.PaginationResult<ShipmentModeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "ExpenseTypeID", string sortOrder = "desc")
        {
            var query = _repository.All();

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            var paginatedResult = await PaginationService<CfDefExpenseType, ShipmentModeVM>.GetPaginatedData(
                query,
                pageNumber,
                pageSize,
                searchTerm,
                currentSortColumn,
                sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.ExpenseTypeId ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.ExpenseType ?? "", $"{term}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{term}%"),
                pt => new ShipmentModeVM
                {
                    TC = pt.Tc,
                    ExpenseTypeID = pt.ExpenseTypeId,
                    ExpenseType = pt.ExpenseType ?? "",
                    ShortName = pt.ShortName ?? "",                  
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                });

            return paginatedResult;
        }

        public async Task<bool> IsDuplicateAsync(ShipmentModeVM model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // Compare directly in the DB with trimming and case-insensitivity
            return await _repository.All()
                .AnyAsync(pm =>
                    pm.ExpenseType.Trim().ToLower() == model.ExpenseType.Trim().ToLower()
                    && pm.Tc != model.TC
                );
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.Tc == id);
        }

        public async Task<bool> SaveAsync(ShipmentModeVM model)
        {
            try
            {
                var entity = new CfDefExpenseType
                {
                    ExpenseTypeId = model.ExpenseTypeID,
                    ExpenseType = model.ExpenseType,
                    ShortName = model.ShortName ?? "",
                    Luser = "", // Set based on your auth system, e.g., User.Identity.Name
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP() ?? "",
                    Lmac = GetMacAddress() ?? "",
                    ExpenseLedgerCodeNo = model.ExpenseLedgerCodeNo?? "",
                    RevenueLedgerCodeNo = model.RevenueLedgerCodeNo ?? "",
                    AdvanceLedgerCode = model.AdvanceLedgerCode ?? ""
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

        public async Task<bool> UpdateAsync(ShipmentModeVM model)
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

                entity.ExpenseTypeId = model.ExpenseTypeID;
                entity.ExpenseType = model.ExpenseType;
                entity.ShortName = model.ShortName ?? "";
                entity.Luser = ""; // Set based on your auth system, e.g., User.Identity.Name
                entity.Lip = GetLocalIP() ?? "";
                entity.Lmac = GetMacAddress() ?? "";
                entity.ModifyDate = DateTime.Now;

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

        public async Task<List<ShipmentModeVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new ShipmentModeVM
            {
                ExpenseTypeID = model.ExpenseTypeId,
                ExpenseType = model.ExpenseType,
                ShortName = model.ShortName,
            }).ToList();

            return result;
        }

        public async Task<List<ShipmentModeVM>> GetAllExpenseTypesAsync()
        {
            var data = await _repository.All().ToListAsync();
            return data.Select(x => new ShipmentModeVM
            {
                ExpenseTypeID = x.ExpenseTypeId,
                ExpenseType = x.ExpenseType
            }).ToList();
        }


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
    }
}
