using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace GCTL.Service.ChartOfAccounts.ControlLedger
{
    public class ControlLedgerService : AppService<AccSubControlLedger>, IContrlLedger
    {
        private readonly IRepository<AccSubControlLedger> _repository;
        private readonly IRepository<AccControlLedger> _groupledger;

        public ControlLedgerService(IRepository<AccSubControlLedger> repository, IRepository<AccControlLedger> groupledger) : base(repository)
        {
            _repository = repository;
            _groupledger = groupledger;
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
                Console.WriteLine($"Bulk Delete Error: {ex}");
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
                Console.WriteLine($"Single Delete Error: {ex}");
                return (false);
            }
        }

        public async Task<List<ControlLedgerVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new ControlLedgerVM
            {
                autoId = model.AutoId,
                ControlLedgerCodeNo = model.ControlLedgerCodeNo,
                SubControlLedgerCodeNo = model.SubControlLedgerCodeNo,
                SubControlLedgerName = model.SubControlLedgerName,
                ShortName = model.ShortName
            }).ToList();

            return result;
        }

        public async Task<ControlLedgerVM> GetByIdAsync(int id)
        {
            var data = await (from control in _repository.All()
                              join gro in _groupledger.All()
                                  on control.ControlLedgerCodeNo equals gro.ControlLedgerCodeNo
                              where control.AutoId == id
                              select new ControlLedgerVM
                              {
                                  autoId = control.AutoId,
                                  SubControlLedgerCodeNo = control.SubControlLedgerCodeNo,
                                  SubControlLedgerName = control.SubControlLedgerName,
                                  ShortName = control.ShortName,
                                  ControlLedgerCodeNo = control.ControlLedgerCodeNo,
                                  LDate = control.Ldate,
                                  ModifyDate = control.ModifyDate,

                                  // From parent (Acc_ControlLedger)
                                  GroupLedgerName = gro.ControlLedgerName,
                                  GroupLedgerShortName = gro.ShortName
                              }).FirstOrDefaultAsync();

            return data;
        }

        //public async Task<string> GetLastControlLedgerCodeAsync(string groupledgercode)
        //{
        //    if (string.IsNullOrWhiteSpace(groupledgercode))
        //        throw new ArgumentException("Group Ledger Code is required.", nameof(groupledgercode));

        //    // Get last SubControlLedgerCode for this group
        //    var lastSubCode = await _repository.All()
        //        .Where(x => x.ControlLedgerCodeNo == groupledgercode)
        //        .OrderByDescending(x => x.SubControlLedgerCodeNo)
        //        .Select(x => x.SubControlLedgerCodeNo)
        //        .FirstOrDefaultAsync();

        //    string nextSubCode;

        //    if (!string.IsNullOrEmpty(lastSubCode))
        //    {
        //        // Extract last 2 digits and increment
        //        if (!int.TryParse(lastSubCode.Substring(lastSubCode.Length - 2), out int lastSeq))
        //            throw new Exception($"Invalid Control Ledger Code format: {lastSubCode}");

        //        int newSeq = lastSeq + 1;

        //        // Limit to 99 per group
        //        if (newSeq > 99)
        //            throw new Exception($"Maximum Control Ledger Code (99) reached for Group {groupledgercode}.");

        //        // Concatenate GroupCode + new sequence as 2-digit string
        //        nextSubCode = groupledgercode + newSeq.ToString("D2"); // D2 → 2 digits
        //    }
        //    else
        //    {
        //        // First entry for this group
        //        nextSubCode = groupledgercode + "01";
        //    }

        //    return nextSubCode;
        //}

        public async Task<string> GetLastControlLedgerCodeAsync(string groupledgercode)
        {
            if (string.IsNullOrWhiteSpace(groupledgercode))
                throw new ArgumentException("Group Ledger Code is required.", nameof(groupledgercode));

            // Fastest way to get the max SubCode for this group
            var lastSubCode = await _repository.All()
                .Where(x => x.ControlLedgerCodeNo == groupledgercode)
                .MaxAsync(x => x.SubControlLedgerCodeNo);

            int newSeq = 1;

            if (!string.IsNullOrEmpty(lastSubCode))
            {
                if (!int.TryParse(lastSubCode.Substring(lastSubCode.Length - 2), out int lastSeq))
                    throw new Exception($"Invalid Control Ledger Code format: {lastSubCode}");

                newSeq = lastSeq + 1;

                if (newSeq > 99)
                    throw new Exception($"Maximum Control Ledger Code (99) reached for Group {groupledgercode}.");
            }

            string nextSubCode = groupledgercode + newSeq.ToString("D2");

            return nextSubCode;
        }


        public async Task<PaginationService<AccSubControlLedger, ControlLedgerVM>.PaginationResult<ControlLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SubControlLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "")
        {
            var query = _repository.All();

            if (!string.IsNullOrEmpty(groupLedgerCode))
            {
                query = query.Where(x => x.ControlLedgerCodeNo == groupLedgerCode);
            }

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            var paginatedResult = await PaginationService<AccSubControlLedger, ControlLedgerVM>.GetPaginatedData(
                query,
                pageNumber,
                pageSize,
                searchTerm,
                sortColumn,
                sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.SubControlLedgerName ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.SubControlLedgerCodeNo ?? "", $"{term}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{term}%"),
                pt => new ControlLedgerVM
                {
                    autoId = pt.AutoId,
                    ControlLedgerCodeNo = pt.ControlLedgerCodeNo,
                    SubControlLedgerCodeNo = pt.SubControlLedgerCodeNo,
                    SubControlLedgerName = pt.SubControlLedgerName,
                    ShortName = pt.ShortName,
                    LIP = pt.Lip,
                    LMAC = pt.Lmac,
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                });

            return paginatedResult;
        }

        public async Task<bool> IsDuplicateAsync(ControlLedgerVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var alldata = await _repository.All().ToListAsync();

            var isDuplicate = alldata.Any(pm =>
            string.Equals(pm.SubControlLedgerName, model.SubControlLedgerName, StringComparison.OrdinalIgnoreCase) &&
             pm.AutoId != model.autoId

            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(ControlLedgerVM model)
        {
            try
            {
                var entity = new AccSubControlLedger
                {
                    SubControlLedgerCodeNo = model.SubControlLedgerCodeNo,
                    SubControlLedgerName = model.SubControlLedgerName,
                    ControlLedgerCodeNo = model.ControlLedgerCodeNo,
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

        public async Task<bool> UpdateAsync(ControlLedgerVM model)
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
                entity.SubControlLedgerCodeNo = model.SubControlLedgerCodeNo;
                entity.SubControlLedgerName = model.SubControlLedgerName;
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


        #region Group Ledger Dropdown
        public async Task<List<CommonChoiceVM>> GroupLedgerDropdown()
        {
            var groupledger = await _groupledger.All().Select(e => new CommonChoiceVM
            {
                Id = e.ControlLedgerCodeNo,
                Name = e.ControlLedgerName,
            }).ToListAsync();

            return groupledger;
        }

        public async Task<AccControlLedger> GetAllGroupLedger(string groupcode)
        {
            var ledger = await _groupledger.All()
                .Where(e => e.ControlLedgerCodeNo == groupcode)
                .Select(e => new AccControlLedger
                {
                    ControlLedgerCodeNo = e.ControlLedgerCodeNo,
                    ControlLedgerName = e.ControlLedgerName,
                    ShortName = e.ShortName
                })
                .FirstOrDefaultAsync();

            return ledger;
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
