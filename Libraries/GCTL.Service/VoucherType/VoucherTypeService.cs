using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.VoucherType;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Service.VoucherType
{
    public class VoucherTypeService : AppService<AccVoucherType>, IVoucherType
    {
        private readonly IRepository<AccVoucherType> _repository;
        private readonly IRepository<AccDurationType> _durationType;
        public VoucherTypeService(
            IRepository<AccVoucherType> genericRepository,
            IRepository<AccDurationType> durationType
            ) 
    : base(genericRepository)
        {
            _repository = genericRepository;
            _durationType = durationType;
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

        public async Task<List<VoucherTypeVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new VoucherTypeVM
            {
                VoucherType_Code = model.VoucherTypeCode,
                Voucher_TypeName = model.VoucherTypeName,
                Description = model.Description,
                numberingMethod = model.NumberingMethod,
                startingNumber = model.StartingNumber,
                resetDuration = model.ResetDuration,
                resetDurationType_Code = model.ResetDurationTypeCode,
                prefix = model.Prefix,
                suffix = model.Suffix,
                padding = model.Padding
            }).ToList();

            return result;
        }

        public async Task<VoucherTypeVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.AutoId == id)
                        .Select(pt => new VoucherTypeVM
                        {

                            VoucherType_Code = pt.VoucherTypeCode,
                            Voucher_TypeName = pt.VoucherTypeName,
                            Description = pt.Description,
                            numberingMethod = pt.NumberingMethod,
                            startingNumber = pt.StartingNumber,
                            resetDuration = pt.ResetDuration,
                            resetDurationType_Code = pt.ResetDurationTypeCode,
                            prefix = pt.Prefix,
                            suffix = pt.Suffix,
                            padding = pt.Padding,
                            LDate = pt.Ldate,
                            ModifyDate = pt.ModifyDate,
                        }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> GetLastVoucherTypeCodeAsync()
        {
            var allCodes = await _repository.All()
                                 .Select(c => c.VoucherTypeCode)
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

            int nextCode = maxCode + 1;

            return nextCode.ToString(); 
        }

        //public async Task<PaginationService<Acc_VoucherType, VoucherTypeVM>.PaginationResult<VoucherTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "VoucherType_Code", string sortOrder = "desc")
        //{
        //    var query = _repository.All();

        //    // Apply search first (can still be translated to SQL)
        //    if (!string.IsNullOrEmpty(searchTerm))
        //    {
        //        searchTerm = searchTerm.Trim().ToLower();
        //        query = query.Where(sc =>
        //            EF.Functions.Like(sc.VoucherType_Code ?? "", $"%{searchTerm}%") ||
        //            EF.Functions.Like(sc.Voucher_TypeName ?? "", $"{searchTerm}%") ||
        //            EF.Functions.Like(sc.prefix ?? "", $"{searchTerm}%") ||
        //            EF.Functions.Like(sc.Description ?? "", $"%{searchTerm}%"));
        //    }

        //    // Force client-side for numeric string sorting
        //    var list = query.AsEnumerable(); // Data fetched into memory

        //    list = sortColumn == "VoucherType_Code"
        //        ? (sortOrder.ToLower() == "desc"
        //            ? list.OrderByDescending(x => int.Parse(x.VoucherType_Code))
        //            : list.OrderBy(x => int.Parse(x.VoucherType_Code)))
        //        : (sortOrder.ToLower() == "desc"
        //            ? list.OrderByDescending(x => EF.Property<object>(x, sortColumn))
        //            : list.OrderBy(x => EF.Property<object>(x, sortColumn)));

        //    // Pagination
        //    if (pageSize == -1)
        //    {
        //        pageSize = list.Count();
        //        pageNumber = 1;
        //    }

        //    var paginatedData = list
        //        .Skip((pageNumber - 1) * pageSize)
        //        .Take(pageSize)
        //        .Select(pt => new VoucherTypeVM
        //        {
        //            autoId = pt.autoId,
        //            VoucherType_Code = pt.VoucherType_Code,
        //            Voucher_TypeName = pt.Voucher_TypeName,
        //            Description = pt.Description,
        //            numberingMethod = pt.numberingMethod,
        //            startingNumber = pt.startingNumber,
        //            resetDuration = pt.resetDuration,
        //            resetDurationType_Code = pt.resetDurationType_Code,
        //            prefix = pt.prefix,
        //            suffix = pt.suffix,
        //            padding = pt.padding,
        //            LIP = pt.LIP,
        //            LMAC = pt.LMAC,
        //            LDate = pt.LDate,
        //            ModifyDate = pt.ModifyDate,
        //        }).ToList();

        //    // Pagination Info
        //    var totalItems = list.Count();
        //    var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        //    var startItem = totalItems == 0 ? 0 : (pageNumber - 1) * pageSize + 1;
        //    var endItem = Math.Min(pageNumber * pageSize, totalItems);
        //    var pageNumbers = Enumerable.Range(1, totalPages).ToList();

        //    return new PaginationService<Acc_VoucherType, VoucherTypeVM>.PaginationResult<VoucherTypeVM>
        //    {
        //        Data = paginatedData,
        //        TotalCount = totalItems,
        //        PaginationInfo = new PaginationService<Acc_VoucherType, VoucherTypeVM>.PaginationInfo
        //        {
        //            StartItem = startItem,
        //            EndItem = endItem,
        //            TotalItems = totalItems,
        //            PageNumbers = pageNumbers,
        //            TotalPages = totalPages,
        //            CurrentPage = pageNumber
        //        }
        //    };
        //}

        public async Task<PaginationService<AccVoucherType, VoucherTypeVM>.PaginationResult<VoucherTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "VoucherType_Code", string sortOrder = "desc")
        {
            // Base query
            var query = from v in _repository.All()
                        join d in _durationType.All() // Acc_Duration_Type repository
                            on v.ResetDurationTypeCode equals d.DurationTypeCode into vd
                        from duration in vd.DefaultIfEmpty() // Left join
                        select new
                        {
                            Voucher = v,
                            DurationName = duration.DurationType
                        };

            // Apply search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(x =>
                    EF.Functions.Like(x.Voucher.VoucherTypeCode ?? "", $"%{searchTerm}%") ||
                    EF.Functions.Like(x.Voucher.VoucherTypeName ?? "", $"{searchTerm}%") ||
                    EF.Functions.Like(x.Voucher.Prefix ?? "", $"{searchTerm}%") ||
                    EF.Functions.Like(x.Voucher.Description ?? "", $"%{searchTerm}%") ||
                    EF.Functions.Like(x.DurationName ?? "", $"%{searchTerm}%")
                );
            }

            // Fetch data into memory for numeric string sort
            var list = await query.AsNoTracking().ToListAsync();

            // Sorting
            list = sortColumn switch
            {
                "VoucherType_Code" => sortOrder.ToLower() == "desc"
                    ? list.OrderByDescending(x => int.Parse(x.Voucher.VoucherTypeCode)).ToList()
                    : list.OrderBy(x => int.Parse(x.Voucher.VoucherTypeCode)).ToList(),

                _ => sortOrder.ToLower() == "desc"
                    ? list.OrderByDescending(x => EF.Property<object>(x.Voucher, sortColumn)).ToList()
                    : list.OrderBy(x => EF.Property<object>(x.Voucher, sortColumn)).ToList()
            };

            // Pagination
            if (pageSize == -1)
            {
                pageSize = list.Count();
                pageNumber = 1;
            }

            var paginatedData = list
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new VoucherTypeVM
                {
                    autoId = x.Voucher.AutoId,
                    VoucherType_Code = x.Voucher.VoucherTypeCode,
                    Voucher_TypeName = x.Voucher.VoucherTypeName,
                    Description = x.Voucher.Description,
                    numberingMethod = x.Voucher.NumberingMethod,
                    startingNumber = x.Voucher.StartingNumber,
                    resetDuration = x.Voucher.ResetDuration,
                    resetDurationType_Code = x.Voucher.ResetDurationTypeCode,
                    DurationTypeName = x.DurationName,
                    prefix = x.Voucher.Prefix,
                    suffix = x.Voucher.Suffix,
                    padding = x.Voucher.Padding,
                    LIP = x.Voucher.Lip,
                    LMAC = x.Voucher.Lmac,
                    LDate = x.Voucher.Ldate,
                    ModifyDate = x.Voucher.ModifyDate
                }).ToList();

            // Pagination Info
            var totalItems = list.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var startItem = totalItems == 0 ? 0 : (pageNumber - 1) * pageSize + 1;
            var endItem = Math.Min(pageNumber * pageSize, totalItems);
            var pageNumbers = Enumerable.Range(1, totalPages).ToList();

            return new PaginationService<AccVoucherType, VoucherTypeVM>.PaginationResult<VoucherTypeVM>
            {
                Data = paginatedData,
                TotalCount = totalItems,
                PaginationInfo = new PaginationService<AccVoucherType, VoucherTypeVM>.PaginationInfo
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

        public async Task<bool> IsDuplicateAsync(VoucherTypeVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var alldata = await _repository.All().ToListAsync();

            var isDuplicate = alldata.Any(pm =>
            string.Equals(pm.VoucherTypeName, model.Voucher_TypeName, StringComparison.OrdinalIgnoreCase) &&
             pm.AutoId != model.autoId

            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(VoucherTypeVM model)
        {
            try
            {
                string newVoucherCode = await GetLastVoucherTypeCodeAsync();

                var entity = new AccVoucherType
                {
                    VoucherTypeCode = newVoucherCode,
                    VoucherTypeName = model.Voucher_TypeName,
                    Description = model.Description,
                    NumberingMethod = model.numberingMethod,
                    StartingNumber = model.startingNumber ?? 0,
                    ResetDuration = model.resetDuration,
                    ResetDurationTypeCode = model.resetDurationType_Code,
                    Prefix = model.prefix,
                    Suffix = model.suffix,
                    Padding = model.padding,
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

        public async Task<bool> UpdateAsync(VoucherTypeVM model)
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
                //string newVoucherCode = await GetLastVoucherTypeCodeAsync();

                //entity.VoucherType_Code = newVoucherCode;
                entity.VoucherTypeName = model.Voucher_TypeName;
                entity.Description = model.Description;
                entity.NumberingMethod = model.numberingMethod;
                entity.StartingNumber = model.startingNumber ?? 0;
                entity.ResetDuration = model.resetDuration;
                entity.ResetDurationTypeCode = model.resetDurationType_Code;
                entity.Prefix = model.prefix;
                entity.Suffix = model.suffix;
                entity.Padding = model.padding;
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

        #region Reset Duration Type Dropdown

        public async Task<List<CommonChoiceVM>> DurationTypeDropdown()
        {
            var dropdowndata = await _durationType.All().Select(e => new CommonChoiceVM
            {
                Id = e.DurationTypeCode,
                Name = e.DurationType,
            }).ToListAsync();

            return dropdowndata;
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
    }
}
