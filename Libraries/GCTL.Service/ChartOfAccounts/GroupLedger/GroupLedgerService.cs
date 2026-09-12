using GCTL.Core.Data;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Service.ChartOfAccounts.GroupLedger
{
    public class GroupLedgerService : AppService<AccControlLedger>,IGroupLedger
    {
        private readonly IRepository<AccControlLedger> _repository;
        public GroupLedgerService(IRepository<AccControlLedger> genericRepository) : base(genericRepository)
        {
            _repository = genericRepository;
        }

        public async Task<bool> BulkDeleteAsync(List<decimal> ids)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.All().Where(c => ids.Contains(c.AutoId)).ToListAsync();

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

                await _repository.DeleteAsync(entity.AutoId);

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

        public async Task<List<GroupLedgerVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new GroupLedgerVM
            {
                autoId = model.AutoId,
                ControlLedgerCodeNo = model.ControlLedgerCodeNo,
                ControlLedgerName = model.ControlLedgerName,
                ShortName = model.ShortName
            }).ToList();

            return result;
        }

        public async Task<GroupLedgerVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.AutoId == id)
                        .Select(pt => new GroupLedgerVM
                        {
                            autoId = pt.AutoId,
                            ControlLedgerCodeNo = pt.ControlLedgerCodeNo,
                            ControlLedgerName = pt.ControlLedgerName,
                            ShortName = pt.ShortName,
                            LDate = pt.Ldate,
                            ModifyDate = pt.ModifyDate,
                        }).FirstOrDefaultAsync();
            return data;
        }

        //public async Task<string> GetLastGroupLedgerCodeAsync()
        //{
        //    var last = await _repository.All()
        //            .OrderByDescending(c => c.ControlLedgerCodeNo)
        //            .FirstOrDefaultAsync();

        //    return last?.ControlLedgerCodeNo;
        //}


        public async Task<string> GetLastGroupLedgerCodeAsync()
        {
            var allCodes = await _repository.All()
                                 .Select(c => c.ControlLedgerCodeNo)
                                 .ToListAsync();

            int maxCode = 0;

            foreach (var code in allCodes)
            {
                if (int.TryParse(code, out int numericCode))
                {
                    if (numericCode > maxCode)
                        maxCode = numericCode;
                }
            }

            return maxCode.ToString(); // latest numeric code
        }

        public async Task<PaginationService<AccControlLedger, GroupLedgerVM>.PaginationResult<GroupLedgerVM>> GetPaginatedAsync(int pageNumber = 1,int pageSize = 10,string searchTerm = "",string sortColumn = "ControlLedgerCodeNo",string sortOrder = "desc")
        {
            var query = _repository.All();

            // Apply search first (can still be translated to SQL)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(sc =>
                    EF.Functions.Like(sc.ControlLedgerCodeNo ?? "", $"%{searchTerm}%") ||
                    EF.Functions.Like(sc.ControlLedgerName ?? "", $"{searchTerm}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{searchTerm}%"));
            }

            // Force client-side for numeric string sorting
            var list = query.AsEnumerable(); // Data fetched into memory

            list = sortColumn == "ControlLedgerCodeNo"
                ? (sortOrder.ToLower() == "desc"
                    ? list.OrderByDescending(x => int.Parse(x.ControlLedgerCodeNo))
                    : list.OrderBy(x => int.Parse(x.ControlLedgerCodeNo)))
                : (sortOrder.ToLower() == "desc"
                    ? list.OrderByDescending(x => EF.Property<object>(x, sortColumn))
                    : list.OrderBy(x => EF.Property<object>(x, sortColumn)));

            // Pagination
            if (pageSize == -1)
            {
                pageSize = list.Count();
                pageNumber = 1;
            }

            var paginatedData = list
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(pt => new GroupLedgerVM
                {
                    autoId = pt.AutoId,
                    ControlLedgerCodeNo = pt.ControlLedgerCodeNo,
                    ControlLedgerName = pt.ControlLedgerName,
                    ShortName = pt.ShortName,
                    LIP = pt.Lip,
                    LMAC = pt.Lmac,
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                }).ToList();

            // Pagination Info
            var totalItems = list.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var startItem = totalItems == 0 ? 0 : (pageNumber - 1) * pageSize + 1;
            var endItem = Math.Min(pageNumber * pageSize, totalItems);
            var pageNumbers = Enumerable.Range(1, totalPages).ToList();

            return new PaginationService<AccControlLedger, GroupLedgerVM>.PaginationResult<GroupLedgerVM>
            {
                Data = paginatedData,
                TotalCount = totalItems,
                PaginationInfo = new PaginationService<AccControlLedger, GroupLedgerVM>.PaginationInfo
                {
                    StartItem = startItem,
                    EndItem = endItem,
                    TotalItems = totalItems,
                    PageNumbers = pageNumbers,
                    TotalPages = totalPages,
                    CurrentPage = pageNumber
                }
            };
        }

        public async Task<bool> IsDuplicateAsync(GroupLedgerVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var alldata = await _repository.All().ToListAsync();

            var isDuplicate = alldata.Any(pm =>
            string.Equals(pm.ControlLedgerName, model.ControlLedgerName, StringComparison.OrdinalIgnoreCase) &&
             pm.AutoId != model.autoId

            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(GroupLedgerVM model)
        {
            try
            {
                var entity = new AccControlLedger
                {
                    ControlLedgerCodeNo = model.ControlLedgerCodeNo,
                    ControlLedgerName = model.ControlLedgerName,
                    ShortName = model.ShortName ?? "",
                    Luser = model.LUser,
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP() ?? "",
                    Lmac = GetMacAddress() ?? ""
                };

                await _repository.AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Insertion Failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(GroupLedgerVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.GetByIdAsync(model.autoId);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.ControlLedgerCodeNo = model.ControlLedgerCodeNo;
                entity.ControlLedgerName = model.ControlLedgerName;
                entity.ShortName = model.ShortName ?? "";
                entity.Luser = model.LUser;
                entity.ModifyDate = DateTime.Now;
                entity.Lip = GetLocalIP() ?? "";
                entity.Lmac = GetMacAddress() ?? "";

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
