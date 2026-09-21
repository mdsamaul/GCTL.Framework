using GCTL.Core.Data;

using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Service.ChartOfAccounts.SubControlLedger
{
    public class SubControlLedgerService : AppService<AccGeneralLedger>, ISubControlLedger
    {
        private readonly IRepository<AccGeneralLedger> _repository;
        private readonly IRepository<AccSubControlLedger> _controlLedger;
        private readonly IRepository<AccControlLedger> _groupLedger;

        public SubControlLedgerService(IRepository<AccGeneralLedger> genericRepository, IRepository<AccSubControlLedger> controlLedger, IRepository<AccControlLedger> groupLedger) : base(genericRepository)
        {
            _repository = genericRepository;
            _controlLedger = controlLedger;
            _groupLedger = groupLedger;
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

        public async Task<List<SubControlLedgerVM>> GetAllAsync(string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "")
        {
            IQueryable<AccGeneralLedger> subcrlQuery;

            if (string.IsNullOrEmpty(groupLedgerCode) &&
                string.IsNullOrEmpty(controlLedgerCode) &&
                string.IsNullOrEmpty(subcontrolLedgerCode))
            {
                subcrlQuery = _repository.All();
            }
            else
            {
                var query = from subcrl in _repository.All()
                            join cl in _controlLedger.All() on subcrl.SubControlLedgerCodeNo equals cl.SubControlLedgerCodeNo
                            join gr in _groupLedger.All() on cl.ControlLedgerCodeNo equals gr.ControlLedgerCodeNo
                            select new { subcrl, cl, gr };

                if (!string.IsNullOrEmpty(groupLedgerCode))
                {
                    query = query.Where(x => x.gr.ControlLedgerCodeNo == groupLedgerCode);
                }

                if (!string.IsNullOrEmpty(controlLedgerCode))
                {
                    query = query.Where(x => x.cl.SubControlLedgerCodeNo == controlLedgerCode);
                }

                if (!string.IsNullOrEmpty(subcontrolLedgerCode))
                {
                    query = query.Where(x => x.subcrl.GeneralLedgerCodeNo == subcontrolLedgerCode);
                }

                subcrlQuery = query.Select(x => x.subcrl);
            }

            var entity = await subcrlQuery.ToListAsync();

            var result = entity.Select(model => new SubControlLedgerVM
            {
                autoId = model.AutoId,
                SubControlLedgerCodeNo = model.SubControlLedgerCodeNo,
                GeneralLedgerCodeNo = model.GeneralLedgerCodeNo,
                GeneralLedgerName = model.GeneralLedgerName,
                ShortName = model.ShortName
            }).ToList();

            return result;
        }


        public async Task<SubControlLedgerVM> GetByIdAsync(int id)
        {
            var data = await (from subcl in _repository.All()             
                              join crl in _controlLedger.All()        
                                  on subcl.SubControlLedgerCodeNo equals crl.SubControlLedgerCodeNo
                              join grl in _groupLedger.All()          
                                  on crl.ControlLedgerCodeNo equals grl.ControlLedgerCodeNo
                              where subcl.AutoId == id
                              select new SubControlLedgerVM
                              {
                                  autoId = subcl.AutoId,
                                  GeneralLedgerCodeNo = subcl.GeneralLedgerCodeNo,
                                  GeneralLedgerName = subcl.GeneralLedgerName,
                                  ShortName = subcl.ShortName,

                                  SubControlLedgerCodeNo = crl.SubControlLedgerCodeNo,
                                  //CRLName = crl.SubControlLedgerName,
                                  CRLShortname = crl.ShortName,

                                  GRLCode = grl.ControlLedgerCodeNo,
                                  GRLShortname = grl.ShortName,

                                  LDate = subcl.Ldate,
                                  ModifyDate = subcl.ModifyDate
                              }).FirstOrDefaultAsync();

            return data;
        }


        //public async Task<string> GetLastSubControlLedgerCodeAsync(string controlLedgercode)
        //{
        //    if (string.IsNullOrWhiteSpace(controlLedgercode))
        //        throw new ArgumentException("Control Ledger Code is required.", nameof(controlLedgercode));

        //    // Get Code 
        //    var lastCode = await _repository.All()
        //        .Where(x => x.SubControlLedgerCodeNo == controlLedgercode)
        //        .OrderByDescending(x => x.GeneralLedgerCodeNo)
        //        .Select(x => x.GeneralLedgerCodeNo)
        //        .FirstOrDefaultAsync();

        //    string nextCode;

        //    if (!string.IsNullOrEmpty(lastCode))
        //    {
        //        // Extract last 3 digits and increment
        //        if (!int.TryParse(lastCode.Substring(lastCode.Length - 3), out int lastSeq))
        //            throw new Exception($"Invalid SubControlLedgerCode format: {lastCode}");

        //        int newSeq = lastSeq + 1;

        //        // Limit to 999 per code
        //        if (newSeq > 999)
        //            throw new Exception($"Maximum SubControlLedgerCode (999) reached for Group {controlLedgercode}.");


        //        nextCode = controlLedgercode + newSeq.ToString("D3"); 
        //    }
        //    else
        //    {
        //        // First entry 
        //        nextCode = controlLedgercode + "001";
        //    }

        //    return nextCode;
        //}
        public async Task<string> GetLastSubControlLedgerCodeAsync(string controlLedgercode)
        {
            if (string.IsNullOrWhiteSpace(controlLedgercode))
                throw new ArgumentException("Control Ledger Code is required.", nameof(controlLedgercode));

            // Fastest way to get max code
            var lastCode = await _repository.All()
                .Where(x => x.SubControlLedgerCodeNo == controlLedgercode)
                .MaxAsync(x => x.GeneralLedgerCodeNo);

            int newSeq = 1;

            if (!string.IsNullOrEmpty(lastCode))
            {
                if (!int.TryParse(lastCode.Substring(lastCode.Length - 3), out int lastSeq))
                    throw new Exception($"Invalid SubControlLedgerCode format: {lastCode}");

                newSeq = lastSeq + 1;

                if (newSeq > 999)
                    throw new Exception($"Maximum SubControlLedgerCode (999) reached for Group {controlLedgercode}.");
            }

            return controlLedgercode + newSeq.ToString("D3");
        }


        public async Task<PaginationService<AccGeneralLedger, SubControlLedgerVM>.PaginationResult<SubControlLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "GeneralLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "")
        {
            var query = from subcrl in _repository.All() // Sub-Control Ledger
                        join cl in _controlLedger.All() // Control Ledger
                            on subcrl.SubControlLedgerCodeNo equals cl.SubControlLedgerCodeNo
                        join gr in _groupLedger.All() // Group Ledger
                            on cl.ControlLedgerCodeNo equals gr.ControlLedgerCodeNo
                        select new { subcrl, cl, gr };

            // Filter by groupLedgerCode
            if (!string.IsNullOrEmpty(groupLedgerCode))
            {
                query = query.Where(x => x.gr.ControlLedgerCodeNo == groupLedgerCode);
            }

            // Filter by controlLedgerCode 
            if (!string.IsNullOrEmpty(controlLedgerCode))
            {
                query = query.Where(x => x.cl.SubControlLedgerCodeNo == controlLedgerCode);
            }


            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            var paginatedResult = await PaginationService<AccGeneralLedger, SubControlLedgerVM>.GetPaginatedData(query.Select(x => x.subcrl), pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.GeneralLedgerCodeNo ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.GeneralLedgerName ?? "", $"{term}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{term}%"),
                pt => new SubControlLedgerVM
                {
                    autoId = pt.AutoId,
                    SubControlLedgerCodeNo = pt.SubControlLedgerCodeNo,
                    GeneralLedgerCodeNo = pt.GeneralLedgerCodeNo,
                    GeneralLedgerName = pt.GeneralLedgerName,
                    ShortName = pt.ShortName,
                    LIP = pt.Lip,
                    LMAC = pt.Lmac,
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,

                }
            );


            return paginatedResult;
        }

        public async Task<bool> IsDuplicateAsync(SubControlLedgerVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var alldata = await _repository.All().ToListAsync();

            var isDuplicate = alldata.Any(pm =>
            string.Equals(pm.GeneralLedgerName, model.GeneralLedgerName, StringComparison.OrdinalIgnoreCase) &&
             pm.AutoId != model.autoId

            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(SubControlLedgerVM model)
        {
            try
            {
                var entity = new AccGeneralLedger
                {
                    SubControlLedgerCodeNo = model.SubControlLedgerCodeNo,
                    GeneralLedgerCodeNo = model.GeneralLedgerCodeNo,
                    GeneralLedgerName = model.GeneralLedgerName,
                    ShortName = model.ShortName ?? "",
                    IsSameNameGl = "",
                    IsSameNameSl = "",
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

        public async Task<bool> UpdateAsync(SubControlLedgerVM model)
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

                entity.SubControlLedgerCodeNo = model.SubControlLedgerCodeNo;
                entity.GeneralLedgerCodeNo = model.GeneralLedgerCodeNo;
                entity.GeneralLedgerName = model.GeneralLedgerName;
                entity.ShortName = model.ShortName ?? "";
                entity.IsSameNameGl = "";
                entity.IsSameNameSl = "";
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


        #region Dropdown Section
        public async Task<List<CommonChoiceVM>> ControlLedgerDropdown(string groupcode)
        {
            var controlLedger = await _controlLedger.All()
                .Where(g => g.ControlLedgerCodeNo == groupcode)
                .Select(e => new CommonChoiceVM
                {
                    Id = e.SubControlLedgerCodeNo,
                    Name = e.SubControlLedgerName,
                }).ToListAsync();

            return controlLedger;
        }




        public async Task<AccSubControlLedger> GetAllControlInfo(string contrlcode)
        {
            var ledger = await _controlLedger.All()
           .Where(e => e.SubControlLedgerCodeNo == contrlcode)
           .Select(e => new AccSubControlLedger
           {
               SubControlLedgerCodeNo = e.SubControlLedgerCodeNo,
               SubControlLedgerName = e.SubControlLedgerName,
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
