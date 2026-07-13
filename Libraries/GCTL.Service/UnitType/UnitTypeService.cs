using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using GCTL.Service.Pagination;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.UnitType;

namespace GCTL.Service.UnitType
{
    public class UnitTypeService : AppService<RmgProdDefUnitType>, IUnitType
    {
        private readonly IRepository<RmgProdDefUnitType> _repository;
        public UnitTypeService(IRepository<RmgProdDefUnitType> repository) : base(repository)
        {            
                _repository = repository;            
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

        public async Task<UnitTypeVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.Tc == id)
                        .Select(pt => new UnitTypeVM
                        {
                            UnitTypID = pt.UnitTypId,
                            UnitTypeName = pt.UnitTypeName ?? "",
                            ShortName = pt.ShortName ?? "",
                            LIP = pt.Lip,
                            DecimalPlaces = pt.DecimalPlaces,
                            LMAC = pt.Lmac ?? "",
                            LDate = pt.Ldate,
                            ModifyDate = pt.ModifyDate,
                        }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> GetLastUnitAsync()
        {

            var last = await _repository.All()
                        .OrderByDescending(c => c.UnitTypId)
                        .FirstOrDefaultAsync();

            return last?.UnitTypId;
        }

        public async Task<PaginationService<RmgProdDefUnitType, UnitTypeVM>.PaginationResult<UnitTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "UnitTypID", string sortOrder = "desc")
        {
            var query = _repository.All();

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            var paginatedResult = await PaginationService<RmgProdDefUnitType, UnitTypeVM>.GetPaginatedData(
                query,
                pageNumber,
                pageSize,
                searchTerm,
                currentSortColumn,
                sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.UnitTypId ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.UnitTypeName ?? "", $"{term}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{term}%"),
                pt => new UnitTypeVM
                {
                    TC = pt.Tc,
                    UnitTypID = pt.UnitTypId,
                    UnitTypeName = pt.UnitTypeName ?? "",
                    ShortName = pt.ShortName ?? "",
                    DecimalPlaces = pt.DecimalPlaces ,
                    LUser = pt.Luser ?? "",
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                });

            return paginatedResult;
        }

        public async Task<bool> IsDuplicateAsync(UnitTypeVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var paymentMode = await _repository.All().ToListAsync();

            var isDuplicate = paymentMode.Any(pm =>
            string.Equals(pm.UnitTypeName, model.UnitTypeName, StringComparison.OrdinalIgnoreCase) &&
             pm.Tc != model.TC

            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.Tc == id);
        }

        public async Task<bool> SaveAsync(UnitTypeVM model)
        {
            try
            {
                var entity = new RmgProdDefUnitType
                {
                    UnitTypId = model.UnitTypID,
                    UnitTypeName = model.UnitTypeName,
                    ShortName = model.ShortName,
                    DecimalPlaces = model.DecimalPlaces,
                    Luser = "", // Set based on your auth system, e.g., User.Identity.Name
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP(),
                    Lmac = GetMacAddress(),
                    ModifyDate = model.ModifyDate
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

        public async Task<bool> UpdateAsync(UnitTypeVM model)
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

                entity.UnitTypId = model.UnitTypID;
                entity.UnitTypeName = model.UnitTypeName;
                entity.ShortName = model.ShortName;
                entity.DecimalPlaces = model.DecimalPlaces;
                entity.Luser = ""; // Set based on your auth system, e.g., User.Identity.Name
                entity.Ldate = DateTime.Now;
                entity.Lip = GetLocalIP();
                entity.Lmac = GetMacAddress();
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

        public async  Task<List<UnitTypeVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new UnitTypeVM
            {
                UnitTypID = model.UnitTypId,
                UnitTypeName = model.UnitTypeName,
                ShortName = model.ShortName,
                DecimalPlaces = model.DecimalPlaces,
                

            }).ToList();

            return result;
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
